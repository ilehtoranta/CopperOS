"""Compare five MakeLink outputs read from RAM files under original Kickstart DOS.

This is a bounded differential, not full profile or resident admission. Both
traces must come from the same script, observer, emulator, ROM and reference
disk. No expected diagnostic is inferred from a format string: every byte is
independently read by C:Type after the command's resident image is removed.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

from compare_workbench_makelink_errors import inspect
from verify_copy_boot_transfer import one, require
from verify_workbench_makelink_error_refresh import bind, digest, verify_trx

sys.path.insert(0, str(Path(__file__).resolve().parent / "Inventory"))
from prepare_execute_fixture_adf import inventory, load_image


REFERENCE_SHA256 = "c24b0af713e6f3a252d964c4545da29535bf6652394879a7dc4c57fdce7c02de"
CANDIDATE_SHA256 = "fd42d840165936c2dc4ff5980319b47d1e1dd2ccb6e466c48c03548c7ae1050c"
OUTPUT_NAMES = ("out-success", "out-parser", "out-missing", "out-duplicate", "out-recovery")
SCRIPT = b'''FailAt 21
Echo >RAM:link-source "hard-link-payload"
Resident C:Ed PURE
Ed >RAM:out-success FROM RAM:first TO RAM:link-source
Ed >RAM:out-parser FROM RAM:parser-failure
Ed >RAM:out-missing FROM RAM:missing-target TO RAM:absent
Ed >RAM:out-duplicate FROM RAM:first TO RAM:link-source
Ed >RAM:out-recovery FROM RAM:second TO RAM:link-source
Resident Ed REMOVE
C:Type RAM:out-success
C:Type RAM:out-parser
C:Type RAM:out-missing
C:Type RAM:out-duplicate
C:Type RAM:out-recovery
C:Type RAM:first
C:Type RAM:second
Wait 300
'''


def verify_media(trace, media, script):
    require(script.read_bytes() == SCRIPT and b"\r" not in SCRIPT, "Wrong exact startup script")
    require(media["probe_bytes"] == len(SCRIPT) and
            media["probe_sha256"] == hashlib.sha256(SCRIPT).hexdigest(), "Script receipt mismatch")
    output = Path(media["output_path"])
    require(digest(output) == media["output_adf_sha256"], "Changed derivative disk")
    disk = inventory.Adf(output.read_bytes())
    tree = disk.walk()
    require(disk.read_file(tree["s/startup-sequence"]["block"]) == SCRIPT, "Disk startup mismatch")
    replacement = media["replacements"][0]
    require(hashlib.sha256(disk.read_file(tree["c/ed"]["block"])).hexdigest() == replacement["sha256"],
            "Disk command mismatch")
    archive = Path(media["reference_archive"])
    member, original = load_image(archive)
    require(digest(archive) == trace["archiveSha256"].lower() and member == media["archive_member"] and
            hashlib.sha256(original).hexdigest() == trace["imageSha256"].lower(), "Reference disk mismatch")
    require(trace.get("referenceMediaUnmodified") and trace.get("diskWriteProtected"), "Unprotected reference media")


def readback(events, removal_index, path, expected_payload=None):
    tail = events[removal_index + 1:]
    name = path.split(":", 1)[1]
    match = one([v for v in tail if v["Name"] == "MatchFirst" and v.get("Text") == path],
                "Missing/duplicate readback path: " + path)
    opened = one([v for v in tail if v["Name"] == "Open" and v.get("Text") == name],
                 "Missing/duplicate readback open: " + path)
    a = events.index(opened)
    handle = opened.get("ReturnedD0", 0)
    require(handle != 0 and opened["D2"] == 1005 and events.index(match) < a and
            match["Task"] == opened["Task"] and match.get("ReturnedD0") == 0 and
            bytes.fromhex(match["FileNameBytes"]).split(b"\0", 1)[0] == name.encode("ascii"),
            "Readback path/open identity: " + path)
    close = next((v for v in events[a + 1:] if v["Name"] == "Close" and
                  v["D1"] == handle and v["Task"] == opened["Task"]), None)
    require(close is not None and close.get("ReturnedD0") == 0xffffffff, "Readback close: " + path)
    reads = [v for v in events[a + 1:events.index(close)] if v["Name"] == "FGetC" and
             v["D1"] == handle and v["Task"] == opened["Task"]]
    # Type performs one FGetC even for an empty file. It returns EOF with IoErr
    # zero; this is affirmative empty-file evidence, not a missing observation.
    reached_eof = bool(reads and reads[-1].get("ReturnedD0") == 0xffffffff)
    if reached_eof:
        require(reads[-1].get("ReturnedIoErr") == 0, "Readback EOF error: " + path)
        reads = reads[:-1]
    require(all(isinstance(v.get("ReturnedD0"), int) and 0 <= v["ReturnedD0"] <= 255 and
                v.get("ReturnedIoErr") == 0 for v in reads),
            "Incomplete readback: " + path)
    payload = bytes(v["ReturnedD0"] for v in reads)
    require(match.get("FileSize") == len(payload), "Readback length/empty-file mismatch: " + path)
    require(payload or reached_eof, "Missing affirmative empty-file read: " + path)
    if expected_payload is not None:
        require(payload == expected_payload, "Readback bytes: " + path)
    # Bind each read to a successful independent C:Type run. A prior command's
    # handle or an empty list of observations cannot stand in for an empty file.
    load = next((v for v in reversed(events[removal_index + 1:a]) if
                 v["Name"] == "LoadSeg" and v.get("Text") == "C:Type"), None)
    require(load is not None and load.get("ReturnedD0", 0) != 0, "Independent reader load: " + path)
    run = one([v for v in events[events.index(load) + 1:a] if v["Name"] == "RunCommand" and
               v["D1"] == load["ReturnedD0"] and v["Task"] == opened["Task"]], "Independent reader run: " + path)
    require(run.get("ReturnedD0") == 0, "Independent reader failure: " + path)
    unload = next((v for v in events[events.index(close) + 1:] if v["Name"] == "UnLoadSeg" and
                   v["D1"] == load["ReturnedD0"] and v["Task"] == opened["Task"]), None)
    require(unload is not None and "ReturnedD0" in unload, "Independent reader cleanup: " + path)
    return payload


def inspect_output(trace, candidate):
    signatures = inspect(trace, candidate)
    events = trace["observedDosCalls"]
    require(trace.get("scope") == "CC12/disposable-MakeLink-derivative-boot-progress-only" and
            trace.get("explicitCommandInvocations") == 0 and trace.get("explicitDosVectorInvocations") == 0 and
            trace.get("privateDosStructuresWritten") is False and trace.get("cpuStatePatchedAfterReset") is False,
            "Explicit command/vector/state injection")
    require(trace.get("maximumChunks") == 32 and trace.get("instructionsPerChunk") == 250000 and
            trace.get("sampleWholeBound") is True, "Changed execution bound")
    require(len(events) < 2048 and not trace.get("dosObservationOverflow"), "Truncated output observation")
    rows = trace["copyInvocationOwnership"]
    segment, task = rows[0]["Segment"], rows[0]["Task"]
    load = one([v for v in events if v["Name"] == "LoadSeg" and v.get("Text") == "C:Ed"], "Resident image load")
    require(load.get("ReturnedD0") == segment, "Resident image/command mismatch")
    runs = [v for v in events if v["Name"] == "RunCommand" and v["D1"] == segment]
    require(len(runs) == 5 and all(v["Task"] == task and v["D2"] == 4096 for v in runs), "Command runs")
    require([v.get("ReturnedD0") for v in runs] == [0, 20, 20, 20, 0], "Primary return sequence")
    require(all(isinstance(v.get("ReturnedIoErr"), int) for v in runs), "Missing final Process.Result2")
    starts = [i for i, v in enumerate(events) if v["Name"] == "CopyOpenLibrary"]
    ends = [i for i, v in enumerate(events) if v["Name"] == "CopyCloseLibrary"]
    removal = one([v for v in events if v["Name"] == "RemSegment" and
                   v.get("RemovedSegmentList") == segment], "Resident removal")
    removal_index = events.index(removal)
    require(removal.get("ReturnedD0") == 0xffffffff and removal_index > ends[-1], "Resident removal completion")
    for n, run in enumerate(runs):
        require(events.index(run) < starts[n] < ends[n] <
                (events.index(runs[n + 1]) if n < 4 else removal_index), "Run/lease ordering")
        # Each output is created once by Shell redirection before the intended
        # command, then closed after its return and before the next command.
        opened = one([v for v in events[:removal_index] if v["Name"] == "Open" and
                      v.get("Text") == "RAM:" + OUTPUT_NAMES[n]], "Output redirection open")
        require(opened.get("ReturnedD0", 0) != 0 and opened["D2"] == 1006 and
                opened["Task"] == task and events.index(opened) < events.index(run) and
                (n == 0 or ends[n - 1] < events.index(opened)), "Output redirection binding")
        closed = next((v for v in events[ends[n] + 1:] if v["Name"] == "Close" and
                       v["D1"] == opened["ReturnedD0"] and v["Task"] == task), None)
        require(closed is not None and closed.get("ReturnedD0") == 0xffffffff and
                events.index(closed) < (events.index(runs[n + 1]) if n < 4 else removal_index),
                "Output redirection close")
        payload = readback(events, removal_index, "RAM:" + OUTPUT_NAMES[n])
        require((len(payload) == 0) == (n in (0, 4)), "Success/failure output presence")
        signatures[n].update(case=OUTPUT_NAMES[n][4:], final_ioerr=run["ReturnedIoErr"],
                             rendered_bytes=len(payload), rendered_hex=payload.hex(),
                             rendered_latin1=payload.decode("latin1"))
    for name in ("first", "second"):
        readback(events, removal_index, "RAM:" + name, b"hard-link-payload\n")
    return signatures


def verify(args):
    reference, reference_media = bind(args.reference, args.reference_media, "reference", REFERENCE_SHA256)
    candidate, candidate_media = bind(args.candidate, args.candidate_media, "candidate", CANDIDATE_SHA256)
    require(reference_media["replacements"][0]["bytes"] == 700 and
            candidate_media["replacements"][0]["bytes"] == 3184, "Wrong version sizes")
    for trace, media, trx in ((reference, reference_media, args.reference_trx),
                              (candidate, candidate_media, args.candidate_trx)):
        verify_media(trace, media, args.startup)
        verify_trx(trx, trace)
    for field in ("archiveSha256", "imageSha256", "romSha256", "emulatorAssemblySha256", "testAssemblySha256"):
        require(reference[field].lower() == candidate[field].lower(), "Changed execution input: " + field)
    for field in ("machine", "provider"):
        require(reference[field] == candidate[field], "Changed execution configuration: " + field)
    require(digest(args.rom) == candidate["romSha256"].lower(), "Changed ROM")
    require(digest(args.test_assembly) == candidate["testAssemblySha256"].lower(), "Changed observer")
    emulator = args.test_assembly.with_name("CopperMod.Amiga.Emulator.dll")
    require(digest(emulator) == candidate["emulatorAssemblySha256"].lower(), "Changed emulator")
    expected = inspect_output(reference, False)
    actual = inspect_output(candidate, True)
    require(expected == actual, "Rendered output/final IoErr behavioral difference")
    paths = {name: getattr(args, name) for name in ("reference", "candidate", "reference_media", "candidate_media",
              "reference_trx", "candidate_trx", "test_assembly", "startup", "rom")}
    paths.update(emulator=emulator, verifier=Path(__file__),
                 behavior_verifier=Path(__file__).with_name("compare_workbench_makelink_errors.py"),
                 binding_verifier=Path(__file__).with_name("verify_workbench_makelink_error_refresh.py"),
                 media_reader=Path(__file__).parent / "Inventory" / "inventory.py")
    return {"status": "workbench-makelink-rendered-output-and-final-ioerr-differential-passed",
            "scope": "Five original-Kickstart-DOS invocations with separate redirected RAM files, independently sized and read after resident removal; exact diagnostic bytes including two empty success outputs, completed RunCommand primary returns and Process.Result2, target/link decisions, balanced candidate resources and alias content. Current 68000 HUNK only; no full profile, startup or pure/resident admission.",
            "cases": actual, "candidate_binary_sha256": CANDIDATE_SHA256,
            "reference_binary_sha256": REFERENCE_SHA256,
            "machine": candidate["machine"], "provider": candidate["provider"],
            "observed_event_counts": {"reference": len(reference["observedDosCalls"]), "candidate": len(candidate["observedDosCalls"])},
            "loaded_image_sha256": {"reference": reference["loadedCopyImageSha256"].lower(),
                                    "candidate": candidate["loadedCopyImageSha256"].lower()},
            "shipping_qualified": False, "pure_admitted": False, "full_profile_parity": False,
            "evidence": {name: {"path": str(path), "sha256": digest(path)} for name, path in paths.items()}}


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
