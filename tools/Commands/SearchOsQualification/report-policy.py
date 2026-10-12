"""Verify default Search policy selection and its normal-build CPU receipts."""
import hashlib
import json
import pathlib
import struct
import subprocess
import sys
import xml.etree.ElementTree as ET

repo = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / "tools/Commands"))
from measure_c import identity
from qualify_sizes import comparable

stage = pathlib.Path(sys.argv[1]).resolve()
output = pathlib.Path(sys.argv[2])
main_toolchain = sys.argv[3:] == ["--main-toolchain"]
assert not sys.argv[3:] or main_toolchain
previous = repo / "artifacts/search-output-ordering-final-20261012"
reference_path = repo / "docs/Commands/Workbench31MorphOS320/search-output-ordering-20261012.json"

def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

reference = read(reference_path)
assert reference["status"] == "passed-finite-correctness-qualification"
for row in reference["freeze"]["files"]:
    assert identity(row["path"]) == row
policy = repo / "src/Commands/CodeSizePolicies.props"
for row in reference["freeze"]["sources"]:
    assert identity(row["snapshot"]["path"]) == row["snapshot"]
    if pathlib.Path(row["original"]["path"]) != policy:
        assert identity(row["original"]["path"]) == row["original"]
    else:
        old_policy = pathlib.Path(row["snapshot"]["path"]).read_text()
search_group = next(group for group in ET.fromstring(old_policy)
                    if "== 'Search'" in group.attrib.get("Condition", ""))
old_passes = search_group.find("CopperOSCodeSizePasses").text
new_passes = old_passes + ",SimplifyLoopPhis"
assert policy.read_text() == old_policy.replace(old_passes + "</CopperOSCodeSizePasses>",
                                               new_passes + "</CopperOSCodeSizePasses>", 1)
freeze = read(stage / "freeze.json")
assert identity(policy) == freeze["policy"]
for row in freeze["files"]:
    assert identity(row["path"]) == row
build_command = read(stage / "build-command.json")
assert not any("codesize" in value.lower() for value in build_command)
if main_toolchain:
    assert build_command == ["pwsh", "-NoProfile", "-File", "tools/Commands/build-c.ps1", "-Command", "Search"]
    main = repo.parent / "CopperSharp68k"
    for row in freeze["compilerSources"]:
        assert identity(row["original"]["path"]) == row["original"]
        assert identity(row["snapshot"]["path"]) == row["snapshot"]
    compiler_state = read(stage / "compiler-state.json")
    for row in compiler_state["preserved"]:
        assert identity(main / row["path"])["sha256"] == row["expectedSha256"]
    assert len(compiler_state["preserved"]) == 281
    for path in (stage / "compiler").rglob("*"):
        if path.is_file():
            assert path.read_bytes() == (main / "Compiler.Cli/bin/Release/net10.0" / path.relative_to(stage / "compiler")).read_bytes()
    compiler_tests = ET.parse(stage / "tests/compiler-integration-final.trx").find(".//{*}Counters").attrib
    assert int(compiler_tests["failed"]) == 0 and int(compiler_tests["passed"]) == int(compiler_tests["executed"]) == 1156
flags = (stage / "normal/publish.rsp").read_text().splitlines()
assert "code-size-optimizations=on" in flags
assert "code-size-passes=" + new_passes in flags
assert "cpu=68000" in flags
image = identity(stage / "normal/C/Search")
qualified = next(row for row in reference["compilations"]
                 if (row["profile"], row["trial"]) == ("workbench", "candidate"))
if not main_toolchain:
    assert image["sha256"] == qualified["image"]["sha256"] and image["bytes"] == 5396
assert identity(stage / "inputs/Search.candidate")["sha256"] == image["sha256"]
assert (stage / "inputs/Search.baseline").read_bytes() == (previous / "inputs/Search.baseline").read_bytes()
for directory in ("compiler", "runner", "guest", "host", "vector-runner"):
    if main_toolchain and directory == "compiler":
        continue
    for path in (stage / directory).rglob("*"):
        if path.is_file():
            assert path.read_bytes() == (previous / path.relative_to(stage)).read_bytes()

matrix = []
native_evidence = []
vectors = []
for cpu in ("68000", "68020", "68040"):
    count = 0
    for part in ("part1", "part2"):
        directory = stage / "runs" / f"{part}-{cpu}"
        observed = read(directory / "observations.json")
        result = observed["result"]
        assert observed["completed"] and result["StopReason"] == "ProgramReturned"
        assert result["ReturnValue"] == result["LaunchStatus"] == 0
        assert result["CpuModel"] == "M" + cpu and result["ExecutionMode"] == "Interpreter"
        assert result["Frames"] <= 3000 and result["DiskSnapshotError"] is None
        assert result["HunkSha256"].lower() == identity(stage / "guest/SearchQualification")["sha256"]
        assert observed["inputs"]["SEARCHC"] == image["sha256"]
        for filename, key in (("Search.baseline", "SEARCHB"), ("TEXT.IN", "TEXT.IN"), ("LONG.IN", "LONG.IN")):
            assert observed["inputs"][key] == identity(stage / "inputs" / filename)["sha256"]
        fixture = (directory / "CASES.FIX").read_bytes()
        assert fixture == (previous / "runs" / f"{part}-{cpu}" / "CASES.FIX").read_bytes()
        assert hashlib.sha256(fixture).hexdigest() == observed["inputs"]["CASES.FIX"]
        rows = observed["observations"]
        expected = [row for row in reference["observations"] if (row["cpu"], row["run"]) == (cpu, part)]
        assert len(rows) == len(expected) == (30 if part == "part1" else 34)
        raw = (directory / "receipt.bin").read_bytes()
        assert struct.unpack_from(">II", raw) == (0x53515231, len(rows))
        cursor = 8
        for index, (row, contract) in enumerate(zip(rows, expected)):
            for key in ("Name", "Image", "Stack", "CtrlC", "Arguments", "returnCode", "ioError", "output", "outputSha256"):
                assert row[key] == contract[key], (cpu, part, index, key)
            fields = struct.unpack_from(">28I", raw, cursor)
            assert fields[:5] == (index, row["Image"], row["Stack"], row["returnCode"] & 0xffffffff, row["ioError"])
            assert fields[6:8] == (row["availableBefore"], row["availableAfter"])
            assert list(fields[8:18]) == row["before"] and list(fields[18:28]) == row["after"]
            data = raw[cursor + 112:cursor + 112 + fields[5]]
            assert data.decode("latin1") == row["output"] and hashlib.sha256(data).hexdigest() == row["outputSha256"]
            assert row["before"] == row["after"] and row["callerStateRestored"]
            cursor += 112 + fields[5]
        assert cursor == len(raw)
        if part == "part2":
            assert all(row["availableBefore"] == row["availableAfter"] for row in rows[-20:])
        count += len(rows)
        native_evidence.append(dict(cpu=cpu, part=part, observations=observed,
            files=[identity(directory / name) for name in ("result.json", "observations.json", "receipt.bin", "CASES.FIX", "final.adf")]))
    suite = "wb31-search-native-entry-vector-fixture"
    controls = []
    for trial in ("baseline", "normal"):
        path = stage / trial / "execution" / f"Search.{cpu}.{suite}.json"
        row = read(path)
        assert row["status"] == "passed" and len(row["cases"]) == 22
        assert row["imageSha256"] == identity(stage / trial / "C/Search")["sha256"]
        assert row["managedExecutorSha256"] == identity(stage / "vector-runner/CopperOS.Commands.NativeExecution.dll")["sha256"]
        controls.append(row)
    assert [comparable(row) for row in controls[0]["cases"]] == [comparable(row) for row in controls[1]["cases"]]
    vectors.append(dict(cpu=cpu, baseline=controls[0], normal=controls[1]))
    matrix.append(dict(cpu=cpu, nativeInvocations=count, vectorInvocations=44, passed=True))
qualification = read(stage / "normal/qualification.json")
assert all(row["passed"] and row["qualified"] for row in qualification)
winuae = read(stage / "winuae/results.json")
assert {row["cpu"] for row in winuae} == {"68000", "68020", "68040"}
assert all(row["completed"] and row["outputsAgree"] and row["numberedOutputCorrect"] and
           row["baseline"] == row["candidate"] == "     2> Needle\n" and
           row["returns"] == "baseline 0 0\ncandidate 0 0\n" for row in winuae)
installed = identity(repo / "out/C/Search")
assert installed["sha256"] == image["sha256"]
installation_command = build_command if main_toolchain else read(stage / "installation-command.json")
assert not any("codesize" in value.lower() or "outputdirectory" in value.lower() for value in installation_command)
if not main_toolchain:
    assert (stage / "installation/publish.rsp").read_bytes() == (stage / "normal/publish.rsp").read_bytes()
report = dict(date="2026-10-12", status="passed-normal-build-policy-qualification", optimizationEnabled=True,
    scope="Finite Workbench syntax-candidate execution; no original Search parity, shipping/PURE or actual MorphOS runtime claim.",
    reference=identity(reference_path), compilerCommit="02a7fa3", buildCommand=build_command,
    installationCommand=installation_command, image=image, installed=installed, freeze=freeze,
    matrix=matrix, nativeEvidence=native_evidence, vectors=vectors, qualification=qualification, winuae=winuae,
    runnerSourceBinding=read(stage / "runner-source-binding.json"),
    files=[identity(p) for directory in ("normal", "installation", "winuae") for p in sorted((stage / directory).rglob("*")) if p.is_file()],
    verifier=identity(pathlib.Path(__file__)))
if main_toolchain:
    report.update(status="passed-main-checkout-default-build-qualification", compilerCommit=None,
        compilerState=compiler_state, compilerTests=compiler_tests,
        previousQualifiedImage=qualified["image"],
        integrationEvidence=[identity(stage / name) for name in (
            "integration-plan.json", "resolved-plan.json", "integration-applied.json", "recovery.json",
            "compiler-state.json", "normal-build.log", "compiler-tests.log", "compiler-tests-final.log")],
        testEvidence=[identity(p) for p in sorted((stage / "tests").iterdir()) if p.is_file()])
    commit_path = stage / "integration-commit.json"
    if commit_path.exists():
        commit = read(commit_path)
        assert subprocess.check_output(["git", "-C", str(main), "rev-parse", "HEAD"]).decode().strip() == commit["commit"]
        assert subprocess.check_output(["git", "-C", str(main), "rev-parse", "HEAD^{tree}"]).decode().strip() == compiler_state["indexTree"]
        report["compilerCommit"] = commit["commit"]
        report["integrationCommit"] = commit
output.with_suffix(".json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(dict(status=report["status"], imageBytes=image["bytes"],
    nativeInvocations=sum(row["nativeInvocations"] for row in matrix),
    vectorInvocations=sum(row["vectorInvocations"] for row in matrix), optimizationEnabled=True)))
