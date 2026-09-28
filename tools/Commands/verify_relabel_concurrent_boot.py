"""Verify two callers of one resident Relabel segment against original DOS boots."""
import argparse
import json
from pathlib import Path
from prepare_relabel_boot import CONCURRENT_STARTUP, ACTIVE_REMOVE_STARTUP, ACTIVE_REPLACE_STARTUP
from verify_relabel_boot import digest, one, read_trace, require, verify_media
from verify_relabel_extended_boot import readback, verify_cancellation


def observe(report, media, candidate):
    require(media["scenario"] in ["concurrent", "active-remove", "active-replace"], "Known concurrent scenario")
    active_removal = media["scenario"] == "active-remove"
    active_replacement = media["scenario"] == "active-replace"
    verify_media(report, media, media["replacements"][0]["local_file"], candidate,
        ACTIVE_REPLACE_STARTUP if active_replacement else ACTIVE_REMOVE_STARTUP if active_removal else CONCURRENT_STARTUP)
    require(report.get("requireConcurrentResident") is True and
        (not active_removal or report.get("requireActiveResidentRemoval") is True) and
        (not active_replacement or report.get("requireActiveResidentReplacement") is True),
        "Concurrent scenario identity")
    rows = report["copyInvocationOwnership"]
    require(len(rows) == 3 and report.get("copyActiveInvocations") == 0 and
        report.get("copyMaximumActiveInvocations") == 2 and report.get("copyCpuImageWrites") == [] and
        report.get("copyImageUnchangedAtReturn"), "Complete unchanged overlapping segment")
    require(len({r["Segment"] for r in rows}) == 1 and len({r["Task"] for r in rows}) == 2,
        "One segment and two actual tasks")
    events = report["observedDosCalls"]
    segment = rows[0]["Segment"]
    load = one([e for e in events if e["Name"] == "LoadSeg" and e.get("Text") == "C:Ed"], "One load")
    add = one([e for e in events if e["Name"] == "AddSegment" and e["D2"] == segment], "One registration")
    removals = [e for e in events if e["Name"] == "RemSegment" and e.get("RemovedSegmentList") == segment]
    require(len(removals) == (2 if active_removal else 1), "Exact removal attempt count")
    remove = one([e for e in removals if e.get("ReturnedD0", 0) != 0], "One successful removal")
    freed = one([e for e in events if e["Name"] == "SegmentFreeMem"], "One segment release")
    require(load.get("ReturnedD0") == segment and add.get("ReturnedD0", 0) != 0 and
        remove.get("ReturnedD0", 0) != 0 and "ReturnRetireCycle" in freed and
        freed["A1"] == report["copySegmentAllocationBase"] and freed["D0"] == report["copySegmentAllocationBytes"],
        "Resident registration/storage identities")
    require(load["ReturnRetireCycle"] < add["EntryRetireCycle"] < add["ReturnRetireCycle"] < min(r["EntryCycle"] for r in rows) and
        max(r["ReturnCycle"] for r in rows) < remove["EntryRetireCycle"] < freed["EntryRetireCycle"],
        "Registration and release surround all callers")
    runs = [e for e in events[events.index(add):events.index(remove)] if e["Name"] == "RunCommand" and e["D1"] == segment]
    require(len(runs) == 3, "Exact command run count before removal")
    specs = [("protected", "DF0:", "MustNotChange", "RAM:rc01", 20, 214),
        ("overlap", "RAM:", "ConcurrentVolume", "RAM:rc02", 0, 210),
        ("recovery", "RAM:", "Recovery", "RAM:rc03", 0, 210)]
    cases, owners, allocations, parsers = [], {}, [], []
    for label, drive, name, path, result, error in specs:
        mutation = one([e for e in events if e["Name"] == "Relabel" and e.get("DestinationText") == name], label + " mutation")
        row = one([r for r in rows if r["Task"] == mutation["Task"] and
            r["EntryCycle"] < mutation["EntryRetireCycle"] < r["ReturnCycle"]], label + " owner")
        owners[label] = row
        run = one([e for e in runs if e["Task"] == row["Task"] and e["EntryRetireCycle"] == row["EntryCycle"]], label + " RunCommand")
        require(run.get("ReturnRetireCycle") == row["ReturnCycle"] and run.get("ReturnedD0") == row["ReturnCode"] == result and
            row["ReturnIoErr"] == error and row["ImageUnchanged"] and not row["ParserFailed"] and not row["OpenFailed"],
            label + " return/error/image")
        calls = [e for e in events if e["Task"] == row["Task"] and row["EntryCycle"] < e["EntryRetireCycle"] < row["ReturnCycle"]]
        def call(which):
            return one([e for e in calls if e["Name"] == which], label + " " + which)
        opened, closed = call("CopyOpenLibrary"), call("CopyCloseLibrary")
        require(opened["D0"] == 36 and opened.get("ReturnedD0", 0) != 0 and closed["A1"] == opened["ReturnedD0"] and
            opened["ReturnRetireCycle"] < closed["EntryRetireCycle"] < closed["ReturnRetireCycle"] < row["ReturnCycle"], label + " DOS lease")
        parsed, released = call("ReadArgs"), call("FreeArgs")
        require(parsed["Text"] == "DRIVE/A,NAME/A" and parsed["D3"] == 0 and parsed.get("ReturnedD0", 0) != 0 and
            released["D1"] == parsed["ReturnedD0"] and parsed["ReturnRetireCycle"] < released["EntryRetireCycle"] and
            opened["ReturnRetireCycle"] < parsed["EntryRetireCycle"] and
            released["ReturnRetireCycle"] < closed["EntryRetireCycle"], label + " parser lease")
        parsers.append((parsed["ReturnedD0"], parsed["ReturnRetireCycle"], released["EntryRetireCycle"], row["Task"]))
        lock, found, unlock = call("LockDosList"), call("FindDosEntry"), call("UnLockDosList")
        require(lock["D1"] == unlock["D1"] == 29 and found["D3"] == 28 and found["LookupText"] == drive[:-1] and
            found.get("ReturnedD0", 0) != 0 and lock["ReturnRetireCycle"] < found["EntryRetireCycle"] and
            found["ReturnRetireCycle"] < unlock["EntryRetireCycle"] and unlock["ReturnRetireCycle"] < mutation["EntryRetireCycle"] and
            mutation["Text"] == drive and mutation.get("ReturnedIoErr") == error and
            (mutation.get("ReturnedD0", 0) != 0) == (result == 0), label + " public list/handler ordering")
        require(parsed["ReturnRetireCycle"] < lock["EntryRetireCycle"] and
            mutation["ReturnRetireCycle"] < released["EntryRetireCycle"], label + " parser lifetime contains handler")
        allocated = [e for e in calls if e["Name"] == "CopyAllocMem"]
        releases = [e for e in calls if e["Name"] == "CopyFreeMem"]
        require(len(allocated) == len(releases) == row["Allocations"] == row["Frees"] == (2 if candidate else 0), label + " direct storage counts")
        if candidate:
            require(parsed["D2"] == allocated[0]["ReturnedD0"] and allocated[0]["D0"] == 8 and
                all(e["D1"] == 0x10001 for e in allocated), label + " invocation-owned ReadArgs slots")
        for allocation in allocated:
            release = one([e for e in releases if e["A1"] == allocation["ReturnedD0"] and e["D0"] == allocation["D0"]], label + " allocation release")
            require(allocation["ReturnRetireCycle"] < release["EntryRetireCycle"] < release["ReturnRetireCycle"] < row["ReturnCycle"], label + " allocation lifetime")
            allocations.append((allocation["ReturnedD0"], allocation["D0"], allocation["ReturnRetireCycle"], release["EntryRetireCycle"], row["Task"]))
        output = readback(events, path)
        require(output == (b"disk is write-protected\n" if result else b""), label + " output bytes")
        matched = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == path], label + " output match")
        require(matched["EntryRetireCycle"] > remove["ReturnRetireCycle"], label + " output after removal")
        faults = [e for e in calls if e["Name"] == "PrintFault"]
        require(len(faults) == (1 if result else 0) and all(e["D1"] == error and e["D2"] == 0 and "ReturnRetireCycle" in e for e in faults), label + " fault")
        operations = []
        for event in calls:
            which = event["Name"]
            if which == "ReadArgs":
                operations.append([which, event["Text"], event["ReturnedD0"] != 0, event["ReturnedIoErr"]])
            elif which in ["LockDosList", "UnLockDosList"]:
                operations.append([which, event["D1"]])
            elif which == "FindDosEntry":
                operations.append([which, event["LookupText"], event["D3"], event["ReturnedD0"] != 0, event["ReturnedIoErr"]])
            elif which == "Relabel":
                operations.append([which, event["Text"], event["DestinationText"], event["ReturnedD0"], event["ReturnedIoErr"]])
            elif which == "PrintFault":
                operations.append([which, event["D1"], event["D2"], event["ReturnedD0"], event["ReturnedIoErr"]])
            elif which == "FreeArgs":
                operations.append([which, event["ReturnedIoErr"]])
        cases.append({"case": label, "result": result, "ioError": error, "outputHex": output.hex(), "operations": operations})
    blocked, overlap, recovery = (owners[k] for k in ["protected", "overlap", "recovery"])
    require(blocked["Task"] != overlap["Task"] == recovery["Task"] and
        blocked["EntryCycle"] < overlap["EntryCycle"] < overlap["ReturnCycle"] < blocked["ReturnCycle"] < recovery["EntryCycle"],
        "Real overlap and subsequent recovery on the other task")
    for i, a in enumerate(parsers):
        for b in parsers[i+1:]:
            if max(a[1], b[1]) < min(a[2], b[2]):
                require(a[0] != b[0], "Concurrent callers share RDArgs")
    overlapping_storage_pairs = 0
    for i, a in enumerate(allocations):
        for b in allocations[i+1:]:
            if max(a[2], b[2]) < min(a[3], b[3]):
                require(a[0] + a[1] <= b[0] or b[0] + b[1] <= a[0], "Live allocations alias")
                if a[4] != b[4]:
                    overlapping_storage_pairs += 1
    require(not candidate or overlapping_storage_pairs == 4, "Both callers must hold their two private allocations concurrently")
    for name in ["ConcurrentVolume", "Recovery"]:
        require(readback(events, name + ":relabel-proof") == b"relabel-payload\n", "Volume payload")
    require(all(k["OverlapObserved"] for k in report["scriptedKeyboardEvents"]), "Input must follow observed overlap")
    result = {"cases": cases, "cancellation": verify_cancellation(report, cases), "tasks": 2,
        "maximumActive": 2, "loadedSegments": 1, "payloadVolumes": ["ConcurrentVolume", "Recovery"]}
    if active_removal:
        result["activeRemoval"] = verify_active_removal(report, owners, segment, removals, remove)
    if active_replacement:
        result["activeReplacement"] = verify_active_replacement(report, owners, segment, remove)
    return result


def verify_active_removal(report, owners, segment, removals, final):
    events = report["observedDosCalls"]
    blocked, overlap, recovery = (owners[k] for k in ["protected", "overlap", "recovery"])
    refused = one([e for e in removals if e.get("ReturnedD0") == 0], "Refused active removal")
    helpers = [e for e in events if e["Name"] == "RunCommand" and bytes.fromhex(e.get("RunArgumentHex", "")) == b"Ed REMOVE\n"]
    require(len(helpers) == 2, "Both Resident REMOVE invocations")
    attempt, retry = helpers
    require(attempt["Task"] == retry["Task"] == refused["Task"] == overlap["Task"] and
        attempt["D1"] == retry["D1"] != segment and attempt.get("ReturnedD0") == 5 and
        attempt.get("ReturnedIoErr") == 202 and retry.get("ReturnedD0") == 0 and retry.get("ReturnedIoErr") == 202,
        "Resident result and preserved error")
    require(blocked["EntryCycle"] < attempt["EntryRetireCycle"] < refused["EntryRetireCycle"] <
        refused["ReturnRetireCycle"] < attempt["ReturnRetireCycle"] < overlap["EntryCycle"] and
        recovery["ReturnCycle"] < retry["EntryRetireCycle"] < final["EntryRetireCycle"] <
        final["ReturnRetireCycle"] < retry["ReturnRetireCycle"], "Removal attempts bound to the correct owner lifetimes")
    require(refused["D1"] == final["D1"] and refused["RemovedUseCount"] == 2 and
        final["RemovedUseCount"] == 1 and refused.get("ReturnedIoErr") == final.get("ReturnedIoErr") == 202,
        "Registry entry/use count/refusal error")
    def find_between(first, last, count):
        found = one([e for e in events if e["Name"] == "FindSegment" and e["Task"] == overlap["Task"] and
            e.get("FoundSegmentList") == segment and first < e["EntryRetireCycle"] < last], "Registry lookup in removal interval")
        require(found.get("ReturnedD0") == refused["D1"] and found["FoundUseCount"] == count and
            found["ReturnRetireCycle"] < last, "Same retained registry node and count")
        return found
    find_between(attempt["EntryRetireCycle"], refused["EntryRetireCycle"], 2)
    find_between(attempt["ReturnRetireCycle"], overlap["EntryCycle"], 2)
    find_between(retry["EntryRetireCycle"], final["EntryRetireCycle"], 1)
    diagnostic = readback(events, "RAM:rc-remove")
    require(diagnostic == b"object is in use\n", "Active-removal diagnostic bytes")
    read = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == "RAM:rc-remove"], "Active-removal diagnostic readback")
    require(read["EntryRetireCycle"] > retry["ReturnRetireCycle"], "Diagnostic readback after final removal")
    return {"attemptResult": 5, "attemptIoErr": 202, "activeUseCount": 2,
        "retryResult": 0, "retryIoErr": 202, "idleUseCount": 1, "diagnosticHex": diagnostic.hex()}


def verify_active_replacement(report, owners, segment, final):
    events = report["observedDosCalls"]
    blocked, overlap, recovery = (owners[k] for k in ["protected", "overlap", "recovery"])
    def helper(arguments):
        return one([e for e in events if e["Name"] == "RunCommand" and
            bytes.fromhex(e.get("RunArgumentHex", "")) == arguments], "Resident helper arguments")
    attempt = helper(b"Ed C:Ed REPLACE PURE\n")
    remove = helper(b"Ed REMOVE\n")
    require(attempt["Task"] == remove["Task"] == overlap["Task"] and
        attempt["D1"] == remove["D1"] != segment and attempt.get("ReturnedD0") == 10 and
        attempt.get("ReturnedIoErr") == 202 and remove.get("ReturnedD0") == 0 and remove.get("ReturnedIoErr") == 202,
        "Replacement refusal level/error and final removal")
    require(blocked["EntryCycle"] < attempt["EntryRetireCycle"] < attempt["ReturnRetireCycle"] < overlap["EntryCycle"] and
        recovery["ReturnCycle"] < remove["EntryRetireCycle"] < final["EntryRetireCycle"] <
        final["ReturnRetireCycle"] < remove["ReturnRetireCycle"], "Replacement/removal owner intervals")
    attempted_calls = [e for e in events if e["Task"] == attempt["Task"] and
        attempt["EntryRetireCycle"] < e["EntryRetireCycle"] < attempt["ReturnRetireCycle"]]
    require(not any(e["Name"] in ["LoadSeg", "AddSegment", "RemSegment", "UnLoadSeg", "InternalUnLoadSeg", "SegmentFreeMem"]
        for e in attempted_calls), "Replacement must reject before load or registry mutation")
    def lookup(first, last, count):
        found = one([e for e in events if e["Name"] == "FindSegment" and e["Task"] == overlap["Task"] and
            e.get("FoundSegmentList") == segment and first < e["EntryRetireCycle"] < last], "Retained replacement registry lookup")
        require(found.get("ReturnedD0") == final["D1"] and found["FoundUseCount"] == count and
            found["ReturnRetireCycle"] < last, "Replacement preserves registry node/count")
    lookup(attempt["EntryRetireCycle"], attempt["ReturnRetireCycle"], 2)
    lookup(attempt["ReturnRetireCycle"], overlap["EntryCycle"], 2)
    lookup(remove["EntryRetireCycle"], final["EntryRetireCycle"], 1)
    require(final["RemovedUseCount"] == 1 and final.get("ReturnedIoErr") == 202, "Final registry count/error")
    diagnostic = readback(events, "RAM:rc-replace")
    require(diagnostic == b"object is in use\n", "Replacement diagnostic bytes")
    read = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == "RAM:rc-replace"], "Replacement output match")
    require(read["EntryRetireCycle"] > remove["ReturnRetireCycle"], "Replacement output readback after cleanup")
    return {"attemptResult": 10, "attemptIoErr": 202, "activeUseCount": 2,
        "loadsDuringAttempt": 0, "registryMutationsDuringAttempt": 0,
        "finalRemovalResult": 0, "finalRemovalIoErr": 202, "idleUseCount": 1, "diagnosticHex": diagnostic.hex()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ["directory", "test-assembly", "qualification", "output-directory"]:
        parser.add_argument("--" + name, type=Path, required=True)
    args = parser.parse_args()
    args.output_directory.mkdir(parents=True, exist_ok=False)
    inputs, observations = {}, []
    qualification = json.loads(args.qualification.read_text(encoding="utf-8-sig"))
    require(qualification["status"] == "passed", "Native qualification")
    for role in ["reference", "candidate"]:
        trx, media_path = args.directory / (role + ".trx"), args.directory / "media" / (role + "-media.json")
        report, media = read_trace(trx), json.loads(media_path.read_text(encoding="utf-8"))
        require(report["testAssemblySha256"].lower() == digest(args.test_assembly) and
            report["emulatorAssemblySha256"].lower() == digest(args.test_assembly.parent / "CopperMod.Amiga.Emulator.dll") and
            report["fixtureReceiptSha256"].lower() == digest(media_path) and media["native_qualification_sha256"] == digest(args.qualification), "Executor and receipt bindings")
        binary = Path(media["replacements"][0]["local_file"])
        expected = "163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff" if role == "reference" else one([a for a in qualification["artifacts"] if a["cpu"] == "68000"], "CPU")["sha256"]
        require(digest(binary) == expected, "Command identity")
        observations.append(observe(report, media, role == "candidate"))
        path = args.output_directory / (role + "-observations.json")
        path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        inputs[role] = {"trxSha256": digest(trx), "mediaSha256": digest(media_path), "binarySha256": digest(binary), "observationsSha256": digest(path)}
    require(observations[0] == observations[1], "Original/replacement mismatch")
    result = {"status": "passed", "suite": "Relabel-concurrent-original-DOS-boot", "inputs": inputs,
        "scenario": media["scenario"],
        "observations": observations[0], "shippingOrPureApproval": False, "verifierSha256": digest(__file__),
        "mediaVerifierSha256": digest(Path(__file__).with_name("verify_relabel_boot.py")),
        "inputVerifierSha256": digest(Path(__file__).with_name("verify_relabel_extended_boot.py")),
        "limits": "Two callers, one loaded segment, one blocked requester and RAM-volume progress/recovery on 68000. Active removal/replacement refusal is covered only by the corresponding explicit observations. Successful replacement and forced removal are not covered. Host Exec/device takeover remains. No task-death, original purity classification, or whole-profile approval."}
    path = args.output_directory / "comparison.json"
    path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(path)


if __name__ == "__main__":
    main()
