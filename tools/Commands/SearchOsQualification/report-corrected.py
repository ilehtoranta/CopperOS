"""Require corrected Search output, resource restoration and three-CPU evidence."""
import hashlib
import json
import pathlib
import struct
import sys
import xml.etree.ElementTree as ET

repo = pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / "tools/Commands"))
from measure_c import identity
from qualify_sizes import comparable

stage = pathlib.Path(sys.argv[1]).resolve()
output = pathlib.Path(sys.argv[2])
previous = repo / "artifacts/search-os-qualification-20261012"
preparation = repo / "artifacts/search-output-ordering-20261012"

def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

freeze = read(stage / "freeze.json")
for row in freeze["files"]:
    assert identity(row["path"]) == row, ("Frozen input changed", row["path"])
for row in freeze["sources"]:
    assert identity(row["original"]["path"]) == row["original"]
    assert identity(row["snapshot"]["path"]) == row["snapshot"]
policy = (repo / "src/Commands/CodeSizePolicies.props").read_text()
assert "SimplifyLoopPhis" not in policy
assert identity(repo / "out/C/Search")["sha256"] == identity(previous / "inputs/Search.baseline")["sha256"]
compilations = read(stage / "compilations.json")
prepared_images = {(row["profile"], row["trial"]): row["image"]
                   for row in read(preparation / "compilations.json")}
for row in compilations:
    assert identity(row["image"]["path"]) == row["image"]
    assert identity(row["response"]["path"]) == row["response"]
    assert identity(row["map"]["path"]) == row["map"]
    prepared = prepared_images[row["profile"], row["trial"]]
    assert identity(prepared["path"]) == prepared
    assert prepared["sha256"] == row["image"]["sha256"]
    response = pathlib.Path(row["response"]["path"]).read_text().splitlines()
    assert (any("SimplifyLoopPhis" in line for line in response)) == (row["trial"] == "candidate")
baseline_flags = (stage / "baseline/compile.rsp").read_text().splitlines()
candidate_flags = (stage / "candidate/compile.rsp").read_text().splitlines()
normalize = lambda lines: [line.replace(",SimplifyLoopPhis", "") for line in lines if not line.startswith("output=")]
assert normalize(baseline_flags) == normalize(candidate_flags)
for trial in ("baseline", "candidate"):
    assert (stage / trial / "C/Search").read_bytes() == (stage / ("publish-" + trial) / "C/Search").read_bytes()

cases = read(stage / "cases.json")
contracts = {row["Name"]: row for row in cases}
long_line = "prefix " + "x" * 8179 + " Needle\n"
# Additional ordering assertions cover headings and fault text as well as the
# unchanged numbered-line expectations from the rejected checkpoint.
strict_outputs = {
    "multiple-from": "   one.txt..\nNeedle\n   two.txt..\nNeedle\n",
    "all-directory": "     SearchQ (dir)\n        spaced name.txt..\n     2> Needle\n"
                     "        empty.txt..\n        long.txt..\n     1> " + long_line +
                     "        one.txt..\n     2> Needle\n          sub (dir)\n"
                     "             two.txt..\n     2> Needle\n",
    "unterminated-quote": "argument line invalid or too long\n",
    "extra-argument": "   one.txt..\nobject not found\n",
    "ctrl-c": "     2> Needle\n***Break\n",
}
matrix = []
observations = []
native_evidence = []
for cpu in ("68000", "68020", "68040"):
    all_rows = []
    for label in ("part1", "part2"):
        directory = stage / "runs" / f"{label}-{cpu}"
        report = read(directory / "observations.json")
        result = report["result"]
        assert report["completed"] and result["StopReason"] == "ProgramReturned"
        assert result["LaunchStatus"] == result["ReturnValue"] == 0
        assert result["CpuModel"] == "M" + cpu and result["ExecutionMode"] == "Interpreter"
        assert result["Frames"] <= 3000 and result["DiskSnapshotError"] is None
        assert result["HunkSha256"].lower() == identity(stage / "guest/SearchQualification")["sha256"]
        for filename, key in (("Search.baseline", "SEARCHB"), ("Search.candidate", "SEARCHC"), ("TEXT.IN", "TEXT.IN"), ("LONG.IN", "LONG.IN")):
            assert report["inputs"][key] == identity(stage / "inputs" / filename)["sha256"]
        fixture = (directory / "CASES.FIX").read_bytes()
        raw = (directory / "receipt.bin").read_bytes()
        rows = report["observations"]
        assert len(rows) == (30 if label == "part1" else 34)
        assert struct.unpack_from(">II", raw) == (0x53515231, len(rows))
        assert hashlib.sha256(fixture).hexdigest() == report["inputs"]["CASES.FIX"]
        cursor = 8
        for index, row in enumerate(rows):
            fields = struct.unpack_from(">28I", raw, cursor)
            request = struct.unpack_from(">5I", fixture, 16 + index * 1040)
            assert request == (index, row["Image"], row["Stack"], int(row["CtrlC"]), len(row["Arguments"]) + 1)
            assert fixture[36 + index * 1040:36 + index * 1040 + request[4]].decode("ascii") == row["Arguments"] + "\n"
            data = raw[cursor + 112:cursor + 112 + fields[5]]
            assert fields[:3] == (index, row["Image"], row["Stack"])
            assert fields[3:5] == (row["returnCode"] & 0xffffffff, row["ioError"])
            assert fields[6:8] == (row["availableBefore"], row["availableAfter"])
            assert list(fields[8:18]) == row["before"] and list(fields[18:28]) == row["after"]
            assert data.decode("latin1") == row["output"] and hashlib.sha256(data).hexdigest() == row["outputSha256"]
            assert row["before"] == row["after"] and row["callerStateRestored"]
            contract = contracts[row["Name"]]
            assert row["Arguments"] == contract["Arguments"] and row["CtrlC"] == contract["CtrlC"]
            assert (row["returnCode"], row["ioError"]) == (contract["expectedReturn"], contract["expectedIoErr"])
            expected = strict_outputs.get(row["Name"], contract["expectedOutput"])
            if expected is not None:
                assert row["output"] == expected, (cpu, label, row["Name"], "output mismatch")
            else:
                assert row["Name"] == "wildcard-from"
                assert sorted(row["output"].splitlines()) == sorted(
                    "Ram Disk:SearchQ/" + name for name in ("spaced name.txt", "long.txt", "one.txt"))
            cursor += 112 + fields[5]
            observations.append(dict(cpu=cpu, run=label, **row))
        assert cursor == len(raw)
        for a, b in zip(rows[::2], rows[1::2]):
            assert {a["Image"], b["Image"]} == {0, 1}
            for key in ("Name", "Arguments", "Stack", "CtrlC", "returnCode", "ioError", "output", "outputSha256"):
                assert a[key] == b[key], (cpu, label, key)
        if label == "part2":
            assert all(row["availableBefore"] == row["availableAfter"] for row in rows[-20:])
        all_rows.extend(rows)
        native_evidence.append(dict(cpu=cpu, label=label, result={k: v for k, v in result.items() if k != "PayloadOutput"},
            files=[identity(directory / name) for name in ("result.json", "observations.json", "receipt.bin", "CASES.FIX", "final.adf")]))
    assert {row["Name"] for row in all_rows} == set(contracts)
    assert {row["Stack"] for row in all_rows} == {4096, 8192}
    matrix.append(dict(cpu=cpu, invocations=len(all_rows), matchedPairs=len(all_rows)//2,
                       exactOutputPassed=True, callerStateRestored=True, repeatMemoryStable=True))

vectors = []
for profile, before, after, suite in (
    ("workbench", "baseline", "candidate", "wb31-search-native-entry-vector-fixture"),
    ("morphos-source", "morphos-baseline", "morphos-candidate", "morphos320-search-native-entry-vector-fixture")):
    summary = read(stage / after / "qualification.json")
    assert all(row["qualified"] and row["passed"] for row in summary)
    for cpu in ("68000", "68020", "68040"):
        controls = []
        for trial in (before, after):
            path = stage / trial / "execution" / f"Search.{cpu}.{suite}.json"
            row = read(path)
            assert row["status"] == "passed" and row["imageSha256"] == identity(stage / trial / "C/Search")["sha256"]
            assert row["managedExecutorSha256"] == identity(stage / "vector-runner/CopperOS.Commands.NativeExecution.dll")["sha256"]
            assert {case["name"] for case in row["cases"]} >= {"output-flush-failure", "output-write-failure", "output-short-writes"}
            controls.append(row)
        assert [comparable(row) for row in controls[0]["cases"]] == [comparable(row) for row in controls[1]["cases"]]
        vectors.append(dict(profile=profile, cpu=cpu, baselineInvocations=len(controls[0]["cases"]),
                            candidateInvocations=len(controls[1]["cases"]), passed=True, checks=summary[0]["checks"]))

control_binding = read(stage / "controls/buffering-control.json")
for key in ("runner", "image", "receipt"):
    assert identity(control_binding[key]["path"]) == control_binding[key]
assert control_binding["exitCode"] != 0
assert control_binding["runner"] == identity(stage / "vector-runner/CopperOS.Commands.NativeExecution.dll")
old_control = read(pathlib.Path(control_binding["receipt"]["path"]))
assert old_control["status"] == "failed" and "without flushing" in old_control["failure"]
assert old_control["imageSha256"] == identity(previous / "inputs/Search.candidate")["sha256"]
winuae = read(stage / "winuae/results.json")
assert {row["cpu"] for row in winuae} == {"68000", "68020", "68040"}
assert all(row["completed"] and row["outputsAgree"] and row["numberedOutputCorrect"] and
           row["baseline"] == row["candidate"] == "     2> Needle\n" and
           row["returns"] == "baseline 0 0\ncandidate 0 0\n" for row in winuae)
counters = ET.parse(stage / "tests/search-host.trx").find(".//{*}Counters").attrib
assert int(counters["executed"]) == int(counters["passed"]) == 11 and int(counters["failed"]) == 0

report = dict(schemaVersion=1, date="2026-10-12", status="passed-finite-correctness-qualification",
              optimizationEnabled=False, installedSearchUpdated=False,
              originalOs="Kickstart 3.1 A500 40.63; frozen NativeOsRunner, OFS floppy and native RAM handler",
              shippingQualified=False, pureResidentQualified=False,
              stackScope="Finite public RunCommand execution at 4096 and 8192 bytes; no absolute minimum or high-water claim",
              breakScope="Pending Ctrl-C through public SetSignal; no keyboard or mid-read signal injection",
              matrix=matrix, observations=observations, nativeEvidence=native_evidence,
              vectors=vectors, winuae=winuae, hostTests=counters,
              morphosScope="Supplied source-profile vectors only; no MorphOS runtime claim. Existing per-file continuation policy is preserved for output errors.",
              oldImageNegativeControl=old_control, controlBinding=control_binding,
              freeze=freeze, compilations=compilations,
              retainedPreparationAttempt=dict(
                  directory=str(preparation),
                  reason="Initial new MorphOS failure vectors expected Workbench's FAIL/221 instead of the unchanged source-profile continuation result OK/0. Final fixture corrects that profile expectation and traversal checks; production images are unchanged.",
                  files=[identity(preparation / name) for name in (
                      "freeze.json", "compilations.json", "morphos-candidate/qualification.json")]),
              runnerSourceBinding=read(stage / "runner-source-binding.json"),
              evidence=[identity(p) for directory in ("baseline", "candidate", "morphos-baseline", "morphos-candidate", "winuae", "tests", "controls")
                        for p in sorted((stage / directory).rglob("*")) if p.is_file()],
              sources=[identity(path) for path in sorted(pathlib.Path(__file__).parent.iterdir()) if path.is_file()])
output.with_suffix(".json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print(json.dumps(dict(status=report["status"], nativeInvocations=sum(row["invocations"] for row in matrix),
                     vectorInvocations=sum(row["baselineInvocations"] + row["candidateInvocations"] for row in vectors),
                     optimizationEnabled=False)))
