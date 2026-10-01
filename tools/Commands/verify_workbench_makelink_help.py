"""Compare real ReadArgs help continuation and EOF under original Kickstart DOS.

The fixed fixture redirects both input and output through original Shell. C:Type
reads the input/output files and resulting hard link after resident removal.
This is a bounded command comparison, not full startup/profile/PURE admission.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys

from verify_copy_boot_transfer import one, require
from verify_workbench_makelink_error_refresh import bind, digest, verify_trx
from verify_workbench_makelink_output import readback

sys.path.insert(0, str(Path(__file__).resolve().parent / "Inventory"))
from prepare_execute_fixture_adf import inventory, load_image

REFERENCE_SHA256 = "c24b0af713e6f3a252d964c4545da29535bf6652394879a7dc4c57fdce7c02de"
CANDIDATE_SHA256 = "fd42d840165936c2dc4ff5980319b47d1e1dd2ccb6e466c48c03548c7ae1050c"
TEMPLATE = "FROM/A,TO/A,HARD/S,FORCE/S"
# Captured from the completed original-command boot before candidate execution.
EXPECTED_OUTPUT = (b"FROM/A,TO/A,HARD/S,FORCE/S: ",
                   b"FROM/A,TO/A,HARD/S,FORCE/S: required argument missing\n")
EXPECTED_FINAL_IOERR = (205, 116)
SCRIPT = b'''FailAt 21
Echo >RAM:link-source "help-payload"
Echo >RAM:help-input "FROM RAM:help-alias TO RAM:link-source"
Echo >RAM:eof-input "" NOLINE
Resident C:Ed PURE
Ed <RAM:help-input >RAM:out-help ?
Ed <RAM:eof-input >RAM:out-eof ?
Resident Ed REMOVE
C:Type RAM:help-input
C:Type RAM:eof-input
C:Type RAM:out-help
C:Type RAM:out-eof
C:Type RAM:help-alias
C:Type RAM:link-source
Wait 300
'''


def verify_media(trace, media, script):
    require(script.read_bytes() == SCRIPT and b"\r" not in SCRIPT, "Wrong exact startup script")
    require(media["probe_bytes"] == len(SCRIPT) and
            media["probe_sha256"] == hashlib.sha256(SCRIPT).hexdigest(), "Script receipt mismatch")
    disk_path = Path(media["output_path"])
    require(digest(disk_path) == media["output_adf_sha256"], "Changed derivative disk")
    disk = inventory.Adf(disk_path.read_bytes())
    tree = disk.walk()
    require(disk.read_file(tree["s/startup-sequence"]["block"]) == SCRIPT, "Disk startup mismatch")
    require(hashlib.sha256(disk.read_file(tree["c/ed"]["block"])).hexdigest() ==
            media["replacements"][0]["sha256"], "Disk command mismatch")
    archive = Path(media["reference_archive"])
    member, original = load_image(archive)
    require(digest(archive) == trace["archiveSha256"].lower() and member == media["archive_member"] and
            hashlib.sha256(original).hexdigest() == trace["imageSha256"].lower(), "Reference disk mismatch")
    require(trace.get("referenceMediaUnmodified") and trace.get("diskWriteProtected"), "Unprotected reference media")


def redirected_file(events, start, end, task, path, mode):
    opened = one([v for v in events[start:end] if v["Name"] == "Open" and v.get("Text") == path and
                  v["D2"] == mode and v["Task"] == task], "Redirection open: " + path)
    require(opened.get("ReturnedD0", 0) != 0, "Failed redirection: " + path)
    return opened


def inspect_help(trace, candidate):
    require(trace.get("commandUnderTest") == "MakeLink" and trace.get("rootInfoReady") and
            trace.get("diskBytesUnchanged") and not any(trace.get(k) for k in
            ("failure", "boundedStop", "dosObservationOverflow", "returnObservationCollision")), "Incomplete help trace")
    require(trace.get("scope") == "CC12/disposable-MakeLink-derivative-boot-progress-only" and
            trace.get("explicitCommandInvocations") == 0 and trace.get("explicitDosVectorInvocations") == 0 and
            trace.get("privateDosStructuresWritten") is False and trace.get("cpuStatePatchedAfterReset") is False,
            "Explicit command/vector/state injection")
    require(trace.get("maximumChunks") == 32 and trace.get("instructionsPerChunk") == 250000 and
            trace.get("sampleWholeBound") is True, "Changed execution bound")
    events = trace["observedDosCalls"]
    require(len(events) < 2048, "Truncated help observation")
    rows = trace["copyInvocationOwnership"]
    require(len(rows) == 2, "Expected help continuation and EOF command invocations")
    segment, task = rows[0]["Segment"], rows[0]["Task"]
    require(all(r["Segment"] == segment and r["Task"] == task and r["ImageUnchanged"] and
                r["Allocations"] == r["Frees"] == (1 if candidate else 0) for r in rows), "Resident image/owned allocation lifetime")
    require(trace.get("copyActiveInvocations") == 0 and trace.get("copyCpuImageWrites") == [], "Live invocation or changed resident image")
    loaded = one([v for v in events if v["Name"] == "LoadSeg" and v.get("Text") == "C:Ed"], "Resident image load")
    require(loaded.get("ReturnedD0") == segment, "Resident image/command mismatch")
    runs = [v for v in events if v["Name"] == "RunCommand" and v["D1"] == segment]
    require(len(runs) == 2 and all(v["Task"] == task and v["D2"] == 4096 for v in runs), "Command runs")
    starts = [i for i, v in enumerate(events) if v["Name"] == "CopyOpenLibrary"]
    ends = [i for i, v in enumerate(events) if v["Name"] == "CopyCloseLibrary"]
    require(len(starts) == len(ends) == 2, "DOS leases")
    removal = one([v for v in events if v["Name"] == "RemSegment" and
                   v.get("RemovedSegmentList") == segment], "Resident removal")
    removed = events.index(removal)
    require(removal.get("ReturnedD0") == 0xffffffff and removed > ends[-1], "Resident removal completion")
    signatures = []
    for n, (input_name, output_name) in enumerate((("help-input", "out-help"), ("eof-input", "out-eof"))):
        run = runs[n]
        run_index = events.index(run)
        stop = events.index(runs[n + 1]) if n == 0 else removed
        require(run_index < starts[n] < ends[n] < stop, "Run/lease ordering")
        calls = events[starts[n]:ends[n] + 1]
        require(all(v["Task"] == task for v in calls), "Foreign task in command lease")
        opened, closed = calls[0], calls[-1]
        require(opened.get("ReturnedD0", 0) != 0 and closed["A1"] == opened["ReturnedD0"] and
                "ReturnedD0" in closed, "Incomplete DOS lease")
        parser = one([v for v in calls if v["Name"] == "ReadArgs"], "Real ReadArgs")
        require(parser.get("Text") == TEMPLATE and isinstance(parser.get("ReturnedIoErr"), int) and
                isinstance(parser.get("ReturnedD0"), int), "Template/parser return")
        parsed = parser.get("ReturnedD0", 0) != 0
        require(parsed == (n == 0) and parser["ReturnedIoErr"] == (0 if parsed else 116) and
                rows[n]["ParserFailed"] == (not parsed), "Help continuation/EOF parser result")
        require(run.get("ReturnedIoErr") == EXPECTED_FINAL_IOERR[n] and
                run.get("ReturnedD0") == rows[n]["ReturnCode"] == (0 if n == 0 else 20), "Primary/final IoErr return")
        frees = [v for v in calls if v["Name"] == "FreeArgs"]
        locks = [v for v in calls if v["Name"] == "Lock"]
        links = [v for v in calls if v["Name"] == "MakeLink"]
        faults = [v for v in calls if v["Name"] == "PrintFault"]
        formats = [v["Text"] for v in calls if v["Name"] == "VPrintf"]
        require(not formats, "Unexpected command-specific format")
        if parsed:
            freed, lock, link = one(frees, "Parser cleanup"), one(locks, "Target lock"), one(links, "Hard link")
            require(freed["D1"] == parser["ReturnedD0"] and "ReturnedD0" in freed and
                    lock["Text"] == "RAM:link-source" and lock["D2"] == 0xfffffffe and lock.get("ReturnedD0", 0) != 0 and
                    link["Text"] == "RAM:help-alias" and link["D2"] == lock["ReturnedD0"] and link["D3"] == 0 and
                    link.get("ReturnedD0", 0) != 0 and not faults, "Help argument binding/mutation/cleanup")
        else:
            fault = one(faults, "EOF fault")
            require(not frees and not locks and not links and
                    not any(v["Name"] in ("Examine", "AllocDosObject", "FreeDosObject") for v in calls) and
                    fault["D1"] == parser["ReturnedIoErr"] != 0 and fault["D2"] == 0 and
                    "ReturnedD0" in fault, "EOF parser failure isolation")
        if candidate:
            unlocks = [v for v in calls if v["Name"] == "UnLock"]
            require(Counter(v["ReturnedD0"] for v in locks if v.get("ReturnedD0", 0)) ==
                    Counter(v["D1"] for v in unlocks) and all("ReturnedD0" in v for v in unlocks), "Target ownership")
            fibs = [v for v in calls if v["Name"] == "AllocDosObject"]
            fib_frees = [v for v in calls if v["Name"] == "FreeDosObject"]
            require(len(fibs) == len(fib_frees) == (1 if parsed else 0), "FIB ownership count")
            if parsed:
                require(fibs[0]["D1"] == fib_frees[0]["D1"] == 2 and fibs[0].get("ReturnedD0", 0) != 0 and
                        fib_frees[0]["D2"] == fibs[0]["ReturnedD0"] and "ReturnedD0" in fib_frees[0], "FIB identity")
        for path, mode in (("RAM:" + input_name, 1005), ("RAM:" + output_name, 1006)):
            redirected = redirected_file(events, ends[n - 1] + 1 if n else events.index(loaded), run_index, task, path, mode)
            released = next((v for v in events[ends[n] + 1:stop] if v["Name"] == "Close" and
                             v["D1"] == redirected["ReturnedD0"] and v["Task"] == task), None)
            require(released is not None and released.get("ReturnedD0") == 0xffffffff, "Redirection close: " + path)
        payload = readback(events, removed, "RAM:" + output_name, EXPECTED_OUTPUT[n])
        signatures.append({"case": "help-continuation" if parsed else "help-eof", "primary": run["ReturnedD0"],
                           "final_ioerr": run["ReturnedIoErr"], "parsed": parsed,
                           "parser_ioerr": parser["ReturnedIoErr"], "fault": faults[0]["D1"] if faults else None,
                           "hard_link_created": bool(links), "rendered_hex": payload.hex(),
                           "rendered_latin1": payload.decode("latin1"), "rendered_bytes": len(payload)})
    for name, expected in (("help-input", b"FROM RAM:help-alias TO RAM:link-source\n"), ("eof-input", b""),
                           ("help-alias", b"help-payload\n"), ("link-source", b"help-payload\n")):
        readback(events, removed, "RAM:" + name, expected)
    return signatures


def verify(args):
    reference, rm = bind(args.reference, args.reference_media, "reference", REFERENCE_SHA256)
    candidate, cm = bind(args.candidate, args.candidate_media, "candidate", CANDIDATE_SHA256)
    require(rm["replacements"][0]["bytes"] == 700 and cm["replacements"][0]["bytes"] == 3184, "Wrong binary sizes")
    for trace, media, trx in ((reference, rm, args.reference_trx), (candidate, cm, args.candidate_trx)):
        verify_media(trace, media, args.startup)
        verify_trx(trx, trace)
    for key in ("archiveSha256", "imageSha256", "romSha256", "emulatorAssemblySha256", "testAssemblySha256"):
        require(reference[key].lower() == candidate[key].lower(), "Changed runtime input: " + key)
    for key in ("machine", "provider"):
        require(reference[key] == candidate[key], "Changed runtime configuration: " + key)
    require(digest(args.rom) == candidate["romSha256"].lower(), "Changed ROM")
    require(digest(args.test_assembly) == candidate["testAssemblySha256"].lower(), "Changed observer")
    emulator = args.test_assembly.with_name("CopperMod.Amiga.Emulator.dll")
    require(digest(emulator) == candidate["emulatorAssemblySha256"].lower(), "Changed emulator")
    expected, actual = inspect_help(reference, False), inspect_help(candidate, True)
    require(expected == actual, "ReadArgs help/EOF original/candidate difference")
    paths = {k: getattr(args, k) for k in ("reference", "candidate", "reference_media", "candidate_media",
             "reference_trx", "candidate_trx", "test_assembly", "startup", "rom")}
    paths.update(emulator=emulator, verifier=Path(__file__),
                 binding_verifier=Path(__file__).with_name("verify_workbench_makelink_error_refresh.py"),
                 readback_verifier=Path(__file__).with_name("verify_workbench_makelink_output.py"),
                 common_verifier=Path(__file__).with_name("verify_copy_boot_transfer.py"),
                 media_reader=Path(__file__).parent / "Inventory/inventory.py",
                 media_writer=Path(__file__).parent / "Inventory/prepare_execute_fixture_adf.py")
    return {"status": "workbench-makelink-help-eof-differential-passed", "cases": actual,
            "scope": "Two original-Kickstart-DOS invocations of the known '?' ReadArgs help trigger with real Shell redirected input/output: complete continuation arguments create a hard link; an independently verified empty input produces EOF. Exact independently read output bytes, final RunCommand primary and Process.Result2, parser/mutation decisions, candidate ownership and four input/content readbacks after resident removal. Fixed 68000 HUNK; no interactive console, break handling, full profile or PURE admission.",
            "candidate_binary_sha256": CANDIDATE_SHA256, "reference_binary_sha256": REFERENCE_SHA256,
            "observed_event_counts": {"reference": len(reference["observedDosCalls"]), "candidate": len(candidate["observedDosCalls"])},
            "shipping_qualified": False, "pure_admitted": False, "full_profile_parity": False,
            "evidence": {k: {"path": str(p), "sha256": digest(p)} for k, p in paths.items()}}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("reference", "candidate", "reference_media", "candidate_media", "reference_trx", "candidate_trx",
                 "test_assembly", "startup", "rom"):
        parser.add_argument(name, type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Refusing to overwrite historical evidence")
    result = verify(args)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(result["status"])
