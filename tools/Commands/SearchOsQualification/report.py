"""Bind native observations and report differential success separately from admission."""
import hashlib
import json
import pathlib
import struct
import sys

stage = pathlib.Path(sys.argv[1]).resolve()
output = pathlib.Path(sys.argv[2])
repo = pathlib.Path(__file__).resolve().parents[3]

def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def bound(path):
    data = path.read_bytes()
    return dict(path=str(path), bytes=len(data), sha256=hashlib.sha256(data).hexdigest())

cases = read(stage / "cases.json")
expected = {item["Name"]: item for item in cases}
matrix = []
failures = []
observations = []
for cpu, labels in (("68000", ("part1", "part2")), ("68020", ("corpus",)), ("68040", ("corpus",))):
    cpu_rows = []
    receipts = []
    for label in labels:
        directory = stage / "runs" / f"{label}-{cpu}"
        report = read(directory / "observations.json")
        assert report["completed"] and report["cpu"] == cpu
        result = report["result"]
        assert result["CpuModel"] == "M" + cpu and result["ExecutionMode"] == "Interpreter"
        assert result["Frames"] <= 3000 and result["StopReason"] == "ProgramReturned"
        assert result["ReturnValue"] == 0 and result["DiskSnapshotError"] is None
        assert result["HunkSha256"].lower() == bound(stage / "guest" / "SearchQualification")["sha256"]
        assert report["inputs"]["SEARCHB"] == bound(stage / "inputs" / "Search.baseline")["sha256"]
        assert report["inputs"]["SEARCHC"] == bound(stage / "inputs" / "Search.candidate")["sha256"]
        # Bind JSON decoding back to the native binary receipt and request file.
        raw = (directory / "receipt.bin").read_bytes()
        fixture = (directory / "CASES.FIX").read_bytes()
        assert struct.unpack_from(">II", raw) == (0x53515231, len(report["observations"]))
        assert hashlib.sha256(fixture).hexdigest() == report["inputs"]["CASES.FIX"]
        cursor = 8
        for index, row in enumerate(report["observations"]):
            fields = struct.unpack_from(">28I", raw, cursor)
            request = struct.unpack_from(">5I", fixture, 16 + index * 1040)
            assert request == (index, row["Image"], row["Stack"], int(row["CtrlC"]), len(row["Arguments"]) + 1)
            assert fixture[36 + index * 1040:36 + index * 1040 + request[4]].decode("ascii") == row["Arguments"] + "\n"
            data = raw[cursor + 112:cursor + 112 + fields[5]]
            assert fields[:3] == (index, row["Image"], row["Stack"])
            assert fields[3:5] == (row["returnCode"] & 0xffffffff, row["ioError"])
            assert fields[6:8] == (row["availableBefore"], row["availableAfter"])
            assert list(fields[8:18]) == row["before"] and list(fields[18:28]) == row["after"]
            assert data.decode("latin1") == row["output"]
            assert hashlib.sha256(data).hexdigest() == row["outputSha256"]
            assert row["callerStateRestored"] and row["before"] == row["after"]
            contract = expected[row["Name"]]
            assert (row["returnCode"], row["ioError"]) == (contract["expectedReturn"], contract["expectedIoErr"])
            if contract["expectedOutput"] is not None and row["output"] != contract["expectedOutput"]:
                failures.append(dict(cpu=cpu, run=label, index=index, name=row["Name"], image=row["Image"],
                                     expected=contract["expectedOutput"], actual=row["output"]))
            cursor += 112 + len(data)
            cpu_rows.append(row)
        assert cursor == len(raw)
        rows = report["observations"]
        assert len(rows) % 2 == 0
        for a, b in zip(rows[::2], rows[1::2]):
            assert {a["Image"], b["Image"]} == {0, 1}
            for key in ("Name", "Arguments", "Stack", "CtrlC", "returnCode", "ioError", "output", "outputSha256"):
                assert a[key] == b[key], (cpu, label, key, a["index"])
        # The final 20 calls exercise both stack sizes after corpus warm-up.
        if label in ("part2", "corpus"):
            assert all(r["availableBefore"] == r["availableAfter"] for r in rows[-20:])
        receipts.append(dict(label=label, result={k: v for k, v in result.items() if k != "PayloadOutput"},
                             evidence=[bound(directory / name) for name in ("result.json", "observations.json", "receipt.bin", "CASES.FIX", "final.adf")]))
    assert {r["Name"] for r in cpu_rows} == set(expected)
    matrix.append(dict(cpu=cpu, invocations=len(cpu_rows), distinctCases=len(expected), pairs=len(cpu_rows)//2,
                       stacks=sorted({r["Stack"] for r in cpu_rows}), callerStateRestored=True,
                       repeatMemoryStable=True, differentialPassed=True, receipts=receipts))
    observations.extend(dict(cpu=cpu, **row) for row in cpu_rows)

winuae = read(stage / "winuae" / "results.json")
assert {r["cpu"] for r in winuae} == {"68000", "68020", "68040"}
assert all(r["completed"] and r["outputsAgree"] and not r["numberedOutputCorrect"] for r in winuae)
assert all(r["baseline"] == "Needle\n     2> " and r["candidate"] == r["baseline"] for r in winuae)
assert all(r["returns"] == "baseline 0 0\ncandidate 0 0\n" for r in winuae)
winuae_evidence = [bound(p) for p in sorted((stage / "winuae").rglob("*")) if p.is_file()]
assert bound(stage / "publish-baseline" / "C" / "Search")["sha256"] == bound(stage / "inputs" / "Search.baseline")["sha256"]
assert bound(stage / "publish-candidate" / "C" / "Search")["sha256"] == bound(stage / "inputs" / "Search.candidate")["sha256"]
assert bound(repo / "out" / "C" / "Search")["sha256"] == bound(stage / "inputs" / "Search.baseline")["sha256"]
assert failures and {f["name"] for f in failures} == {"literal", "quick", "pattern", "long-line"}
bindings = read(stage / "runner-source-binding.json")
assert all(d["matches"] for module in bindings[:2] for d in module["documents"])
engine_mismatches = [d["path"] for d in bindings[2]["documents"] if not d["matches"]]
pressure = repo / "artifacts" / "search-pressure-20261011"
compiler_receipt = read(pressure / "final" / "compilation.json")
for entry in compiler_receipt["compilerFiles"]:
    assert bound(pathlib.Path(entry["path"]))["sha256"] == entry["sha256"]
inventory = [bound(p) for directory in ("runner", "inputs", "guest", "host") for p in sorted((stage / directory).rglob("*")) if p.is_file()]
sources = [bound(p) for p in sorted(pathlib.Path(__file__).parent.iterdir()) if p.is_file()]
report = dict(schemaVersion=1, date="2026-10-12", status="not-admitted-output-ordering-defect",
              differentialPassed=True, shippingQualified=False, pureResidentQualified=False,
              stackScope="Public RunCommand requests of 4096 and 8192 bytes; finite corpus, no measured absolute minimum or high-water/canary claim",
              breakScope="Pending Ctrl-C task signal set through public SetSignal; no keyboard or mid-read fault injection",
              profile="User-supplied Kickstart 3.1 40.63, frozen Lightweight PAL OCS 512 KiB chip + 512 KiB slow, three CPU interpreters",
              matrix=matrix, invocations=sum(r["invocations"] for r in matrix), observations=observations,
              outputContractFailures=failures, winuae=winuae, winuaeEvidence=winuae_evidence,
              sourceBinding=bindings, currentEngineSourceMismatches=engine_mismatches,
              compilerPayload=compiler_receipt["compilerFiles"],
              inventory=inventory, sources=sources,
              boundedFailure=dict(evidence=bound(stage / "runs" / "corpus-68000" / "result.json"),
                                  reason="FrameLimitReached at 3000 during long-line portion; zero accepted completion credit; partitioned follow-up retains same bound"),
              publishEvidence=[bound(stage / "publish-baseline.log"), bound(stage / "publish-candidate.log")])
output.with_suffix(".json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
table = "\n".join(f'| {r["cpu"]} | {r["invocations"]} | {r["pairs"]} | 4096 / 8192 | pass |' for r in matrix)
markdown = f'''# Search real-DOS qualification, 2026-10-12

**Admission fails on a shared redirected-output defect.** The frozen baseline
and SimplifyLoopPhis candidate agree on all three CPU interpreters, but both
write `Needle\\n     2> ` instead of `     2> Needle\\n`. Independent WinUAE
native-shell runs reproduce the same defect on 68000, 68020, and 68040.
The pass stays default-off; `out/C/Search` remains the 10,664-byte baseline.

## Native execution

A frozen NativeOsRunner boots an in-memory OFS floppy with the user-supplied
Kickstart 3.1 A500 ROM (40.63, SHA256
`8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`).
The original startup CLI launches its normal guest program using DOS LoadSeg
and RunCommand. The authored qualification program in turn loads both unchanged
Search images through native DOS, alternates those same segment instances,
and captures exact bytes through native RAM and OFS file operations.
No supplied DOS vectors, host HUNK loader, guest result injection, or copied OS
implementation participates. This is a finite original-Kickstart integration
run, not original Workbench Search binary parity or a full OS certification.

| CPU interpreter | Complete invocations | Matched pairs | Requested stack bytes | Caller state |
| --- | ---: | ---: | --- | --- |
{table}

There are 18 distinct cases per CPU: literal/NONUM/QUIET/QUICK, no match,
empty file, DOS line/file patterns, wildcard and multiple FROM inputs,
recursive ALL traversal, missing files, required-argument and unterminated-quote
errors, another positional path, pending Ctrl-C, a line longer than 8192 bytes,
and a quoted filename. Repeat cases alternate both images, options and errors
on their existing LoadSeg instances at 4 KiB and 8 KiB. Every pair agrees on
output, return code and immediate post-RunCommand IoErr. Multiple positional
strings are accepted by FROM/M; `UNKNOWN` in that case is another path rather
than an unrecognized-keyword parser error. Native NameFromLock uses `Ram Disk:`
instead of the supplied-vector fixture's fabricated `RAM:` path spelling.

All {report["invocations"]} complete invocations restore selected input/output,
current directory, CLI, parent stack size/base and bounds, allocated signal bits,
and DOS library open count. The final 20 calls in each complete repeat batch
have zero AvailMem delta. First-use reductions of 208 and 8320 bytes are retained
as observations, consistent with capture/OS buffer growth; they are not credited
as proof of either a leak or complete resource reclamation. No absolute minimum
stack, overflow canary, loaded-image immutability or pure/resident admission is
established. Ctrl-C is a pending public task signal, not keyboard injection.
Separate stderr, read/allocation fault injection and actual MorphOS execution
remain open; supplied-vector error coverage stays separately documented.

The combined 68000 run reached the unchanged 3000-frame limit during the
long-line section, with an incomplete disk write and no terminal record.
That failed receipt gets zero accepted command-completion credit. Two smaller
batches complete under the same 3000-frame/120-second limits, with four extra
warm-up calls. The 68020/68040 complete runs each contain 60 calls.
Boot-inclusive frames/cycles are retained as diagnostics and are not a Search
speed benchmark or exact accelerator timing qualification.

## Defect and decision

`NativeMorphOSSearchCommand.WriteLine` emits the number with buffered VPrintf
and FPuts, then emits the text with unbuffered DOS Write. Buffer flushing moves
the prefix after the text; multiple-file and ALL headings are displaced too.
The standalone NONUM case emits the expected `Needle\\n`. Strict existing
numbered-line expectations are preserved: {len(failures)} observations fail
them across both images. The optimization introduces no difference in this
corpus, but agreement between two defective images does not pass correctness.

WinUAE {winuae[0]["winuaeVersion"]}, JIT disabled, native Kickstart shell,
4 KiB stack and a disposable directory filesystem independently capture the
same exact misordered bytes and Shell return/Result2 0/0 for both images on all
three CPU models. Its provider boundary differs from the OFS/RAM route and is
explicit in the JSON receipt. Next: repair Search's buffered/unbuffered output
ordering, freeze a new correctness baseline, and rerun this integration gate
before selecting SimplifyLoopPhis for Search.

## Publish path and provenance

The normal `build-c.ps1`/dotnet publish route with the retained frozen CLI
reproduces both images exactly: baseline 10,664 bytes
`ae44b99cba79e377c7f95fb82089d0c9ff99e9b806ee65e9e1e202f2a92ac034`,
candidate 5364 bytes
`3a86e40958b950201a96604532672d0de8a52741d233cfa72cdb49aff891f14c`.
Only private publish directories were used; no production policy or installed
binary was changed. The [JSON report]({output.name}.json) binds sources,
frozen runner/compiler dependencies, CIL/guest images, binary request/response
records, exact output, final disk snapshots and publish logs. Raw evidence:
`artifacts/search-os-qualification-20261012`.

Portable PDB IDs match the executed modules' CodeView IDs. All native runner
and launcher document checksums match their current source. Of 50 engine
documents, 47 match and three differ (LightweightA500Machine, LightweightDiskDma,
generated AssemblyInfo). Therefore these results qualify the frozen engine
binary identified in JSON, not the latest dirty CopperScreen source checkout.
Neither primary sibling checkout was modified.
'''
output.with_suffix(".md").write_text(markdown, encoding="utf-8")
print(json.dumps(dict(status=report["status"], invocations=report["invocations"], matchedPairs=sum(r["pairs"] for r in matrix), outputContractFailures=len(failures))))
