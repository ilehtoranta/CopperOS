"""Verify successful idle resident replacement and both Relabel image lifetimes."""
import argparse
import json
from pathlib import Path
from prepare_relabel_boot import IDLE_REPLACE_STARTUP
from verify_relabel_boot import digest, one, read_trace, require, verify_media
from verify_relabel_extended_boot import readback


def observe(report, media, candidate):
    require(media["scenario"] == "idle-replace" and report.get("requireIdleResidentReplacement") is True,
        "Idle replacement scenario")
    verify_media(report, media, media["replacements"][0]["local_file"], candidate, IDLE_REPLACE_STARTUP)
    require(report.get("copyActiveInvocations") == 0 and report.get("copyMaximumActiveInvocations") == 1 and
        report.get("copyCpuImageWrites") == [] and report.get("copyImageUnchangedAtReturn") is True,
        "Completed sequential calls without image writes")
    rows, images, events = report["copyInvocationOwnership"], report["copySegmentGenerations"], report["observedDosCalls"]
    require(len(rows) == 3 and len(images) == 2 and [g["Generation"] for g in images] == [1, 2] and
        [r["Generation"] for r in rows] == [1, 2, 2] and len({r["Task"] for r in rows}) == 1,
        "Two generations, three calls, one task")
    task = rows[0]["Task"]
    loads = [e for e in events if e["Name"] == "LoadSeg" and e.get("Text") == "C:Ed"]
    frees = [e for e in events if e["Name"] == "SegmentFreeMem"]
    require(len(loads) == len(frees) == 2, "Exactly two loads and releases")
    for index, generation in enumerate(images):
        number = index + 1
        load = loads[index]
        free = one([e for e in frees if e.get("Generation") == number], "Generation release")
        require(load.get("Generation") == number and load.get("ReturnedD0") == generation["Segment"] and
            load["EntryRetireCycle"] == generation["LoadEntryCycle"] and
            load["ReturnRetireCycle"] == generation["LoadReturnCycle"] and
            generation["AllocationBase"] == generation["Segment"] * 4 - 4 and
            generation["AllocationBytes"] == generation["CodeBytes"] + 8 and
            free["A1"] == generation["AllocationBase"] and free["D0"] == generation["AllocationBytes"] and
            free["EntryRetireCycle"] == generation["FreeEntryCycle"] and
            free["ReturnRetireCycle"] == generation["FreeReturnCycle"] and
            generation.get("ImageUnchangedAtFree") is True and load["Task"] == free["Task"] == generation["Task"] == task,
            "Generation load/free identity, storage and owner")
        members = [r for r in rows if r["Generation"] == number]
        require(all(r["Segment"] == generation["Segment"] and
            generation["LoadReturnCycle"] < r["EntryCycle"] < r["ReturnCycle"] < generation["FreeEntryCycle"]
            for r in members), "Calls inside their loaded generation lifetime")
        runs = [e for e in events if e["Name"] == "RunCommand" and e["D1"] == generation["Segment"] and
            generation["LoadReturnCycle"] < e["EntryRetireCycle"] < generation["FreeEntryCycle"]]
        require(len(runs) == len(members), "Every loaded-generation invocation accounted for")
    old, new = images
    require(old["Segment"] != new["Segment"] and new["LoadReturnCycle"] < old["FreeEntryCycle"],
        "New image loaded before old release")
    require(old["AllocationBase"] + old["AllocationBytes"] <= new["AllocationBase"] or
        new["AllocationBase"] + new["AllocationBytes"] <= old["AllocationBase"], "Coexisting images do not alias")
    add = one([e for e in events if e["Name"] == "AddSegment" and e["D2"] in [old["Segment"], new["Segment"]]],
        "Single registry registration")
    remove = one([e for e in events if e["Name"] == "RemSegment" and
        e.get("RemovedSegmentList") in [old["Segment"], new["Segment"]]], "Single final registry removal")
    require(add["D2"] == old["Segment"] and add["D3"] == 1 and add.get("ReturnedD0", 0) != 0 and
        old["LoadReturnCycle"] < add["EntryRetireCycle"] < add["ReturnRetireCycle"] < rows[0]["EntryCycle"] and
        remove["RemovedSegmentList"] == new["Segment"] and remove["RemovedUseCount"] == 1 and
        remove.get("ReturnedD0", 0) != 0 and remove.get("ReturnedIoErr") == 202 and
        add["Task"] == remove["Task"] == task, "Registration and final removal")
    def helper(arguments):
        return one([e for e in events if e["Name"] == "RunCommand" and
            bytes.fromhex(e.get("RunArgumentHex", "")) == arguments], "Resident helper")
    replace, final = helper(b"Ed C:Ed REPLACE PURE\n"), helper(b"Ed REMOVE\n")
    require(replace["Task"] == final["Task"] == task and replace["D1"] == final["D1"] and
        replace["D1"] not in [old["Segment"], new["Segment"]] and
        (replace.get("ReturnedD0"), replace.get("ReturnedIoErr")) == (0, 0) and
        (final.get("ReturnedD0"), final.get("ReturnedIoErr")) == (0, 202), "Resident success and ambient errors")
    unload = one([e for e in events if e["Name"] == "UnLoadSeg" and e["D1"] == old["Segment"] and
        replace["EntryRetireCycle"] < e["EntryRetireCycle"] < replace["ReturnRetireCycle"]], "Old image unload")
    require(rows[0]["ReturnCycle"] < replace["EntryRetireCycle"] < new["LoadEntryCycle"] <
        new["LoadReturnCycle"] < unload["EntryRetireCycle"] < old["FreeEntryCycle"] < old["FreeReturnCycle"] <
        unload["ReturnRetireCycle"] < replace["ReturnRetireCycle"] < rows[1]["EntryCycle"] and
        rows[1]["ReturnCycle"] < rows[2]["EntryCycle"] < rows[2]["ReturnCycle"] < final["EntryRetireCycle"] <
        remove["EntryRetireCycle"] < new["FreeEntryCycle"] < new["FreeReturnCycle"] <
        remove["ReturnRetireCycle"] < final["ReturnRetireCycle"] and unload["Task"] == task,
        "Replacement and removal surround the correct calls and releases")
    require(not any(e["Name"] in ["RemSegment", "AddSegment"] and
        replace["EntryRetireCycle"] < e["EntryRetireCycle"] < replace["ReturnRetireCycle"] for e in events),
        "Replacement updates existing node without removal/re-add")
    for first, last, segment in [(replace["EntryRetireCycle"], new["LoadEntryCycle"], old["Segment"]),
        (replace["ReturnRetireCycle"], rows[1]["EntryCycle"], new["Segment"]),
        (final["EntryRetireCycle"], remove["EntryRetireCycle"], new["Segment"])]:
        lookup = one([e for e in events if e["Name"] == "FindSegment" and e["Task"] == task and
            first < e["EntryRetireCycle"] < last and e.get("FoundSegmentList") == segment], "Registry lookup")
        require(lookup.get("ReturnedD0") == remove["D1"] and lookup.get("FoundUseCount") == 1 and
            lookup["ReturnRetireCycle"] < last, "Same registry node with correct segment and idle count")
    cases = verify_calls(report, candidate, replace, final)
    return {"cases": cases, "imageGenerations": 2, "tasks": 1, "registryNodeReused": True,
        "newImageLoadedBeforeOldFree": True, "replaceResult": 0, "replaceIoErr": 0,
        "removeResult": 0, "removeIoErr": 202, "idleUseCount": 1, "segmentReleases": 2}


def verify_calls(report, candidate, replace, final, replacement_diagnostic=b""):
    """Shared public-DOS, ownership and readback checks for three sequential calls."""
    rows, events = report["copyInvocationOwnership"], report["observedDosCalls"]
    task = rows[0]["Task"]
    cases = []
    for i, (row, label) in enumerate(zip(rows, ["BeforeReplace", "AfterReplace", "Recovery"]), 1):
        run = one([e for e in events if e["Name"] == "RunCommand" and e["Task"] == task and
            e["EntryRetireCycle"] == row["EntryCycle"]], "Relabel RunCommand")
        require(run["D1"] == row["Segment"] and run.get("ReturnRetireCycle") == row["ReturnCycle"] and
            run.get("ReturnedD0") == row["ReturnCode"] == 0 and row["ReturnIoErr"] == 210 and
            row["ImageUnchanged"] and not row["ParserFailed"] and not row["OpenFailed"], "Relabel result and image")
        calls = [e for e in events if e["Task"] == task and row["EntryCycle"] < e["EntryRetireCycle"] < row["ReturnCycle"]]
        def call(name):
            return one([e for e in calls if e["Name"] == name], label + " " + name)
        opened, closed, parsed, released = (call(n) for n in ["CopyOpenLibrary", "CopyCloseLibrary", "ReadArgs", "FreeArgs"])
        require(opened["D0"] == 36 and opened.get("ReturnedD0", 0) != 0 and closed["A1"] == opened["ReturnedD0"] and
            parsed["Text"] == "DRIVE/A,NAME/A" and parsed["D3"] == 0 and parsed.get("ReturnedD0", 0) != 0 and
            released["D1"] == parsed["ReturnedD0"] and opened["ReturnRetireCycle"] < parsed["EntryRetireCycle"] <
            parsed["ReturnRetireCycle"] < released["EntryRetireCycle"] < released["ReturnRetireCycle"] <
            closed["EntryRetireCycle"] < closed["ReturnRetireCycle"] < row["ReturnCycle"], "DOS and parser ownership")
        lock, found, unlock, mutation = (call(n) for n in ["LockDosList", "FindDosEntry", "UnLockDosList", "Relabel"])
        require(lock["D1"] == unlock["D1"] == 29 and found["D3"] == 28 and found["LookupText"] == "RAM" and
            found.get("ReturnedD0", 0) != 0 and mutation["Text"] == "RAM:" and mutation["DestinationText"] == label and
            mutation.get("ReturnedD0", 0) != 0 and mutation.get("ReturnedIoErr") == 210 and
            parsed["ReturnRetireCycle"] < lock["EntryRetireCycle"] < lock["ReturnRetireCycle"] <
            found["EntryRetireCycle"] < found["ReturnRetireCycle"] < unlock["EntryRetireCycle"] <
            unlock["ReturnRetireCycle"] < mutation["EntryRetireCycle"] < mutation["ReturnRetireCycle"] <
            released["EntryRetireCycle"], "Public DOS list and handler semantics")
        allocations = [e for e in calls if e["Name"] == "CopyAllocMem"]
        deallocations = [e for e in calls if e["Name"] == "CopyFreeMem"]
        require(len(allocations) == len(deallocations) == row["Allocations"] == row["Frees"] == (2 if candidate else 0),
            "Invocation storage count")
        if candidate:
            require(allocations[0]["D0"] == 8 and allocations[0]["ReturnedD0"] == parsed["D2"] and
                allocations[0]["ReturnRetireCycle"] < parsed["EntryRetireCycle"] and
                all(e["D1"] == 0x10001 and e.get("ReturnedD0", 0) != 0 for e in allocations), "ReadArgs private cleared slots")
        for allocation in allocations:
            free = one([e for e in deallocations if e["A1"] == allocation["ReturnedD0"] and e["D0"] == allocation["D0"]], "Private release")
            require(allocation["ReturnRetireCycle"] < free["EntryRetireCycle"] < free["ReturnRetireCycle"] < row["ReturnCycle"],
                "Private allocation lifetime")
        if candidate:
            a, b = allocations
            require(a["ReturnedD0"] + a["D0"] <= b["ReturnedD0"] or b["ReturnedD0"] + b["D0"] <= a["ReturnedD0"],
                "Live argument and drive storage do not alias")
            require(released["ReturnRetireCycle"] < one([e for e in deallocations if e["A1"] == parsed["D2"]],
                "ReadArgs slot release")["EntryRetireCycle"], "Argument slots outlive FreeArgs")
        require(not any(e["Name"] == "PrintFault" for e in calls), "No success fault output")
        require(readback(events, label + ":relabel-proof") == b"relabel-payload\n", "Volume payload readback")
        match = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == label + ":relabel-proof"], "Payload match")
        boundary = replace["EntryRetireCycle"] if i == 1 else rows[2]["EntryCycle"] if i == 2 else final["EntryRetireCycle"]
        require(row["ReturnCycle"] < match["EntryRetireCycle"] < match["ReturnRetireCycle"] < boundary, "Payload belongs to this label lifetime")
        operations = []
        for e in calls:
            name = e["Name"]
            if name == "ReadArgs":
                operations.append([name, e["Text"], e["ReturnedD0"] != 0, e["ReturnedIoErr"]])
            elif name in ["LockDosList", "UnLockDosList"]:
                operations.append([name, e["D1"]])
            elif name == "FindDosEntry":
                operations.append([name, e["LookupText"], e["D3"], e["ReturnedD0"] != 0, e["ReturnedIoErr"]])
            elif name == "Relabel":
                operations.append([name, e["Text"], e["DestinationText"], e["ReturnedD0"], e["ReturnedIoErr"]])
            elif name == "FreeArgs":
                operations.append([name, e["ReturnedIoErr"]])
        cases.append({"label": label, "generation": row["Generation"], "result": 0, "ioErr": 210, "operations": operations})
    for name in ["rr01", "rr02", "rr03", "rr-replace", "rr-remove"]:
        path = "RAM:" + name
        require(readback(events, path) == (replacement_diagnostic if name == "rr-replace" else b""),
            "Command/helper diagnostic bytes")
        match = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == path], "Diagnostic match")
        require(match["EntryRetireCycle"] > final["ReturnRetireCycle"], "Diagnostic after final cleanup")
    return cases


def qualify(observe_pair, suite, verifier_path, limits, dependencies=()):
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ["directory", "test-assembly", "qualification", "output-directory"]:
        parser.add_argument("--" + name, type=Path, required=True)
    args = parser.parse_args()
    require(not args.output_directory.exists(), "Use a fresh qualification directory")
    qualification = json.loads(args.qualification.read_text(encoding="utf-8-sig"))
    require(qualification["status"] == "passed" and qualification["suite"] == "CC12-Relabel-wb31-reference-vectors", "Native qualification")
    observations, reports, inputs = [], {}, {}
    for role in ["reference", "candidate"]:
        trx, media_path = args.directory / (role + ".trx"), args.directory / "media" / (role + "-media.json")
        report, media = read_trace(trx), json.loads(media_path.read_text(encoding="utf-8"))
        require(report["testAssemblySha256"].lower() == digest(args.test_assembly) and
            report["emulatorAssemblySha256"].lower() == digest(args.test_assembly.parent / "CopperMod.Amiga.Emulator.dll") and
            report["fixtureReceiptSha256"].lower() == digest(media_path) and
            media["native_qualification_sha256"] == digest(args.qualification), "Executor and receipt bindings")
        binary = Path(media["replacements"][0]["local_file"])
        expected = "163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff" if role == "reference" else one(
            [a for a in qualification["artifacts"] if a["cpu"] == "68000"], "CPU")["sha256"]
        require(digest(binary) == expected, "Command identity")
        observations.append(observe_pair(report, media, role == "candidate"))
        reports[role] = report
        inputs[role] = {"trxSha256": digest(trx), "mediaSha256": digest(media_path), "binarySha256": digest(binary)}
    require(observations[0] == observations[1], "Original/replacement mismatch")
    args.output_directory.mkdir(parents=True)
    for role, report in reports.items():
        path = args.output_directory / (role + "-observations.json")
        path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        inputs[role]["observationsSha256"] = digest(path)
    result = {"status": "passed", "suite": suite, "inputs": inputs,
        "observations": observations[0], "shippingOrPureApproval": False, "verifierSha256": digest(verifier_path),
        "sharedVerifierSha256": digest(__file__),
        "dependencySha256": {name: digest(Path(__file__).with_name(name)) for name in dependencies},
        "mediaVerifierSha256": digest(Path(__file__).with_name("verify_relabel_boot.py")),
        "readbackVerifierSha256": digest(Path(__file__).with_name("verify_relabel_extended_boot.py")),
        "limits": limits}
    path = args.output_directory / "comparison.json"
    path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(path)


if __name__ == "__main__":
    qualify(observe, "Relabel-idle-replacement-original-DOS-boot", __file__,
        "Successful replacement of an idle registry entry by another load of the same HUNK on 68000, with original DOS/Shell and host Exec/device takeover. No failed replacement-load, active successful replacement, task death, other CPU/platform/launch, original PURE classification or shipping admission.")
