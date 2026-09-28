"""Verify failed resident replacement leaves the original Relabel segment usable."""
from prepare_relabel_boot import FAILED_REPLACE_STARTUPS
from verify_relabel_boot import one, require, verify_media
from verify_relabel_replacement_boot import qualify, verify_calls


def observe(report, media, candidate):
    scenario = media["scenario"]
    require(scenario in FAILED_REPLACE_STARTUPS and report.get("requireFailedResidentReplacement") is True,
        "Failed replacement scenario")
    verify_media(report, media, media["replacements"][0]["local_file"], candidate, FAILED_REPLACE_STARTUPS[scenario])
    require(report.get("copyActiveInvocations") == 0 and report.get("copyMaximumActiveInvocations") == 1 and
        report.get("copyCpuImageWrites") == [] and report.get("copyImageUnchangedAtReturn") is True,
        "Completed calls and unchanged retained image")
    rows, images, events = report["copyInvocationOwnership"], report["copySegmentGenerations"], report["observedDosCalls"]
    require(len(rows) == 3 and len(images) == 1 and images[0]["Generation"] == 1 and
        [r["Generation"] for r in rows] == [1, 1, 1] and len({r["Task"] for r in rows}) == 1,
        "One retained generation, three calls, one owner")
    image, task = images[0], rows[0]["Task"]
    segment = image["Segment"]
    load = one([e for e in events if e["Name"] == "LoadSeg" and e.get("Text") == "C:Ed"], "One original image load")
    free = one([e for e in events if e["Name"] == "SegmentFreeMem"], "One final image release")
    add = one([e for e in events if e["Name"] == "AddSegment" and e["D2"] == segment], "One original registration")
    remove = one([e for e in events if e["Name"] == "RemSegment" and e.get("RemovedSegmentList") == segment], "One final removal")
    require(load.get("ReturnedD0") == segment and load.get("Generation") == free.get("Generation") == 1 and
        load["EntryRetireCycle"] == image["LoadEntryCycle"] and load["ReturnRetireCycle"] == image["LoadReturnCycle"] and
        image["AllocationBase"] == segment * 4 - 4 and image["AllocationBytes"] == image["CodeBytes"] + 8 and
        free["A1"] == image["AllocationBase"] and free["D0"] == image["AllocationBytes"] and
        free["EntryRetireCycle"] == image["FreeEntryCycle"] and free["ReturnRetireCycle"] == image["FreeReturnCycle"] and
        image.get("ImageUnchangedAtFree") is True and load["Task"] == add["Task"] == remove["Task"] == free["Task"] == image["Task"] == task,
        "Retained image storage, load/free and owner identities")
    require(add["D3"] == remove["RemovedUseCount"] == 1 and add.get("ReturnedD0", 0) != 0 and
        remove.get("ReturnedD0", 0) != 0 and remove.get("ReturnedIoErr") == 202 and
        image["LoadReturnCycle"] < add["EntryRetireCycle"] < add["ReturnRetireCycle"] < rows[0]["EntryCycle"],
        "Registration and final removal contract")
    require(all(r["Segment"] == segment and image["LoadReturnCycle"] < r["EntryCycle"] <
        r["ReturnCycle"] < image["FreeEntryCycle"] for r in rows), "Every call uses retained live image")
    runs = [e for e in events if e["Name"] == "RunCommand" and e["D1"] == segment and
        image["LoadReturnCycle"] < e["EntryRetireCycle"] < image["FreeEntryCycle"]]
    require(len(runs) == 3, "Every retained-segment call accounted for")
    target = "C:MissingRelabel" if scenario == "replace-missing" else "RAM:relabel-proof"
    def helper(arguments):
        return one([e for e in events if e["Name"] == "RunCommand" and
            bytes.fromhex(e.get("RunArgumentHex", "")) == arguments], "Resident helper arguments")
    attempt = helper(("Ed " + target + " REPLACE PURE\n").encode("ascii"))
    final = helper(b"Ed REMOVE\n")
    require(attempt["Task"] == final["Task"] == task and attempt["D1"] == final["D1"] != segment and
        final.get("ReturnedD0") == 0 and final.get("ReturnedIoErr") == 202, "Helper owner and final result")
    require(rows[0]["ReturnCycle"] < attempt["EntryRetireCycle"] < attempt["ReturnRetireCycle"] < rows[1]["EntryCycle"] <
        rows[1]["ReturnCycle"] < rows[2]["EntryCycle"] < rows[2]["ReturnCycle"] < final["EntryRetireCycle"] <
        remove["EntryRetireCycle"] < image["FreeEntryCycle"] < image["FreeReturnCycle"] <
        remove["ReturnRetireCycle"] < final["ReturnRetireCycle"], "Failure, reuse and removal ordering")
    attempted = [e for e in events if e["Task"] == task and
        attempt["EntryRetireCycle"] < e["EntryRetireCycle"] < attempt["ReturnRetireCycle"]]
    require(not any(e["Name"] in ["AddSegment", "RemSegment", "UnLoadSeg", "InternalUnLoadSeg", "SegmentFreeMem"]
        for e in attempted), "Failed replacement cannot remove or unload retained command")
    for first, last in [(attempt["EntryRetireCycle"], attempt["ReturnRetireCycle"]),
        (attempt["ReturnRetireCycle"], rows[1]["EntryCycle"]), (final["EntryRetireCycle"], remove["EntryRetireCycle"])]:
        lookup = one([e for e in events if e["Name"] == "FindSegment" and e["Task"] == task and
            e.get("FoundSegmentList") == segment and first < e["EntryRetireCycle"] < last], "Retained registry lookup")
        require(lookup.get("ReturnedD0") == remove["D1"] and lookup.get("FoundUseCount") == 1 and
            lookup["ReturnRetireCycle"] < last, "Failure preserves registry node, segment and idle count")
    lock = one([e for e in attempted if e["Name"] == "Lock" and e.get("Text") == target], "Replacement source lock")
    parsed = one([e for e in attempted if e["Name"] == "ReadArgs"], "Resident parser")
    released = one([e for e in attempted if e["Name"] == "FreeArgs"], "Resident parser release")
    fault = one([e for e in attempted if e["Name"] == "PrintFault"], "Resident load failure diagnostic")
    require(parsed["Text"] == "NAME,FILE,REMOVE/S,ADD/S,REPLACE/S,PURE=FORCE/S,SYSTEM/S" and
        parsed.get("ReturnedD0", 0) != 0 and released["D1"] == parsed["ReturnedD0"] and
        parsed["ReturnRetireCycle"] < lock["EntryRetireCycle"] < lock["ReturnRetireCycle"] < released["EntryRetireCycle"] <
        released["ReturnRetireCycle"] < fault["EntryRetireCycle"] < fault["ReturnRetireCycle"] < attempt["ReturnRetireCycle"],
        "Original helper parser and fault ordering")
    cleanup_error, failure_stage = verify_load_failure(scenario, attempted, lock, released)
    error, diagnostic = 205, b"object not found\n"
    require(attempt.get("ReturnedD0") == 5 and attempt.get("ReturnedIoErr") == error and
        released.get("ReturnedIoErr") == cleanup_error and fault["D1"] == error and fault["D2"] == 0 and
        fault.get("ReturnedIoErr") == error and fault.get("ReturnedD0", 0) != 0, "Original helper warning and error")
    cases = verify_calls(report, candidate, attempt, final, diagnostic)
    return {"scenario": scenario, "cases": cases, "imageGenerations": 1, "registryNodeRetained": True,
        "failureStage": failure_stage, "sourceIoErr": cleanup_error,
        "replaceResult": 5, "replaceIoErr": error, "diagnosticHex": diagnostic.hex(),
        "removeResult": 0, "removeIoErr": 202, "idleUseCount": 1, "segmentReleases": 1}


def verify_load_failure(scenario, attempted, lock, released):
    if scenario == "replace-missing":
        require(lock["D2"] == 0xfffffffe and lock.get("ReturnedD0") == 0 and lock.get("ReturnedIoErr") == 205 and
            not any(e["Name"] in ["LoadSeg", "Examine", "UnLock"] for e in attempted),
            "Missing source fails at Lock before loading")
        return 205, "Lock"
    require(lock["D2"] == 0xfffffffe and lock.get("ReturnedD0", 0) != 0 and lock.get("ReturnedIoErr") == 0,
        "Invalid executable source exists")
    examined = one([e for e in attempted if e["Name"] == "Examine"], "Existing source metadata")
    unlocked = one([e for e in attempted if e["Name"] == "UnLock"], "Source lock released")
    load = one([e for e in attempted if e["Name"] == "LoadSeg"], "Failed executable load")
    require(examined["D1"] == unlocked["D1"] == lock["ReturnedD0"] and examined.get("ReturnedD0", 0) != 0 and
        examined["FileSize"] == len(b"relabel-payload\n") and load.get("Text") == "RAM:relabel-proof" and
        load.get("ReturnedD0") == 0 and load.get("ReturnedIoErr") == 212 and
        lock["ReturnRetireCycle"] < examined["EntryRetireCycle"] < examined["ReturnRetireCycle"] <
        unlocked["EntryRetireCycle"] < unlocked["ReturnRetireCycle"] < load["EntryRetireCycle"] <
        load["ReturnRetireCycle"] < released["EntryRetireCycle"], "Invalid HUNK load failure after source lock release")
    return 212, "LoadSeg"


if __name__ == "__main__":
    qualify(observe, "Relabel-failed-replacement-original-DOS-boot", __file__,
        "Missing or invalid replacement source with an idle Workbench registry entry on 68000. Original DOS/Shell, host Exec/device takeover. No allocation exhaustion, target death, original PURE classification, other platform/launch or shipping admission.")
