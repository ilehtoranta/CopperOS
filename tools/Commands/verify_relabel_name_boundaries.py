"""Compare FFS name boundaries, intermediate sectors, errors and recovery."""
import hashlib
import json
from pathlib import Path
from prepare_relabel_boot import NAME_BOUNDARIES, SCENARIOS
from verify_relabel_boot import digest, inventory, one, require, verify_media
from verify_relabel_extended_boot import readback
from verify_relabel_persistent_boot import verify_image, verify_sectors
from verify_relabel_replacement_boot import qualify

# Original Relabel/FFS observations, not a frontend name-sanitization policy.
ORIGINAL = {
    "name30": ("N" * 30, 0, 0, b""),
    "name31": ("FixtureDisk", 20, 210, b"object name invalid\n"),
    "name-empty": ("FixtureDisk", 20, 210, b"object name invalid\n"),
    "name-slash": ("FixtureDisk", 20, 210, b"object name invalid\n"),
}


def disk_checkpoint(run, initial, expected_label):
    root = bytes.fromhex(run["DataRootBlockHex"])
    require(len(root) == 512 and run["DataNonRootSha256"].lower() == hashlib.sha256(
        initial[:880*512] + initial[881*512:]).hexdigest(), "No non-root changes at invocation boundary")
    assembled = initial[:880*512] + root + initial[881*512:]
    disk = inventory.Adf(assembled)  # Root checksum/length independently checked.
    require(disk.root == 880 and disk.volume == expected_label, "Intermediate persisted volume label")
    allowed = set(range(20, 24)) | set(range(420, 464)) | set(range(472, 484))
    before = initial[880*512:881*512]
    require(all(before[n] == root[n] for n in range(512) if n not in allowed), "Root changes restricted to name/dates/checksum")
    return disk.volume


def invocation(report, row, label, candidate):
    calls = [e for e in report["observedDosCalls"] if e["Task"] == row["Task"] and row["EntryCycle"] < e["EntryRetireCycle"] < row["ReturnCycle"]]
    def call(name):
        return one([e for e in calls if e["Name"] == name], name)
    opened, parsed, lock, found, unlock, change, released, closed = (call(n) for n in
        ["CopyOpenLibrary", "ReadArgs", "LockDosList", "FindDosEntry", "UnLockDosList", "Relabel", "FreeArgs", "CopyCloseLibrary"])
    require(row["ImageUnchanged"] and not row["ParserFailed"] and not row["OpenFailed"] and
            opened["D0"] == 36 and opened["ReturnedD0"] != 0 and closed["A1"] == opened["ReturnedD0"] and
            parsed["Text"] == "DRIVE/A,NAME/A" and parsed["D3"] == 0 and parsed["ReturnedD0"] != 0 and
            released["D1"] == parsed["ReturnedD0"], "DOS and ReadArgs lifetime")
    require(lock["D1"] == unlock["D1"] == 29 and found["D3"] == 28 and found["LookupText"] == "DF1" and
            found["ReturnedD0"] != 0 and change["Text"] == "DF1:" and change["DestinationText"] == label,
            "Full original name reaches public DOS handler after device lookup")
    ordered = [opened, parsed, lock, found, unlock, change, released, closed]
    require(all(a["EntryRetireCycle"] < a["ReturnRetireCycle"] < b["EntryRetireCycle"] for a, b in zip(ordered, ordered[1:])) and
            closed["EntryRetireCycle"] < closed["ReturnRetireCycle"] < row["ReturnCycle"], "Unlock before mutation; cleanup before return")
    allocations = [e for e in calls if e["Name"] == "CopyAllocMem"]
    frees = [e for e in calls if e["Name"] == "CopyFreeMem"]
    require(len(allocations) == len(frees) == row["Allocations"] == row["Frees"] == (2 if candidate else 0), "Balanced invocation allocations")
    for allocation in allocations:
        free = one([e for e in frees if e["A1"] == allocation["ReturnedD0"] and e["D0"] == allocation["D0"]], "Owned free")
        require(allocation["D1"] == 0x10001 and allocation["ReturnedD0"] != 0 and
                allocation["ReturnRetireCycle"] < free["EntryRetireCycle"] < free["ReturnRetireCycle"] < row["ReturnCycle"], "Owned storage lifetime")
    if candidate:
        a, b = allocations
        require(a["D0"] == 8 and a["ReturnedD0"] == parsed["D2"] and a["ReturnRetireCycle"] < parsed["EntryRetireCycle"] and
                (a["ReturnedD0"] + a["D0"] <= b["ReturnedD0"] or b["ReturnedD0"] + b["D0"] <= a["ReturnedD0"]) and
                released["ReturnRetireCycle"] < one([e for e in frees if e["A1"] == parsed["D2"]], "Slot release")["EntryRetireCycle"], "Separate parser and scratch storage")
    faults = [e for e in calls if e["Name"] == "PrintFault"]
    if change["ReturnedD0"] == 0:
        fault = one(faults, "Handler failure fault")
        require(row["ReturnCode"] == 20 and fault["D1"] == change["ReturnedIoErr"] and fault["D2"] == 0 and
                change["ReturnRetireCycle"] < fault["EntryRetireCycle"] < fault["ReturnRetireCycle"] < released["EntryRetireCycle"], "Original handler error reporting")
    else:
        require(row["ReturnCode"] == 0 and not faults, "Handler success")
    ops = []
    for e in calls:
        n = e["Name"]
        if n == "ReadArgs": ops.append([n, e["Text"], e["ReturnedD0"] != 0, e["ReturnedIoErr"]])
        elif n in ["LockDosList", "UnLockDosList"]: ops.append([n, e["D1"]])
        elif n == "FindDosEntry": ops.append([n, e["LookupText"], e["D3"], e["ReturnedD0"] != 0, e["ReturnedIoErr"]])
        elif n == "Relabel": ops.append([n, e["Text"], e["DestinationText"], e["ReturnedD0"], e["ReturnedIoErr"]])
        elif n == "PrintFault": ops.append([n, e["D1"], e["D2"], e["ReturnedD0"], e["ReturnedIoErr"]])
        elif n == "FreeArgs": ops.append([n, e["ReturnedIoErr"]])
    return {"name": label, "result": row["ReturnCode"], "ioErr": row["ReturnIoErr"], "operations": ops}


def observe(report, media, candidate):
    scenario = media["scenario"]
    require(scenario in NAME_BOUNDARIES, "Known name boundary")
    verify_media(report, media, media["replacements"][0]["local_file"], candidate, SCENARIOS[scenario])
    rows, events = report["copyInvocationOwnership"], report["observedDosCalls"]
    require(len(rows) == 2 and len({r["Segment"] for r in rows}) == len({r["Task"] for r in rows}) == 1 and
            report["copyActiveInvocations"] == 0 and report["copyMaximumActiveInvocations"] == 1 and
            not report["copyCpuImageWrites"] and report["copyImageUnchangedAtReturn"], "Two complete resident invocations")
    segment = rows[0]["Segment"]
    load = one([e for e in events if e["Name"] == "LoadSeg" and e.get("Text") == "C:Ed"], "Single load")
    add = one([e for e in events if e["Name"] == "AddSegment" and e["D2"] == segment], "Registration")
    remove = one([e for e in events if e["Name"] == "RemSegment" and e.get("RemovedSegmentList") == segment], "Removal")
    free = one([e for e in events if e["Name"] == "SegmentFreeMem"], "Image release")
    require(load["ReturnedD0"] == segment and add["ReturnedD0"] != 0 and remove["ReturnedD0"] != 0 and
            free["A1"] == report["copySegmentAllocationBase"] and free["D0"] == report["copySegmentAllocationBytes"] and
            load["ReturnRetireCycle"] < add["EntryRetireCycle"] < add["ReturnRetireCycle"] < rows[0]["EntryCycle"] <
            rows[0]["ReturnCycle"] < rows[1]["EntryCycle"] < rows[1]["ReturnCycle"] < remove["EntryRetireCycle"] <
            free["EntryRetireCycle"] < free["ReturnRetireCycle"], "Resident image lifetime")
    output = Path(report["dataDiskOutputPath"])
    receipt_path = output.with_name(("candidate" if candidate else "reference") + "-data.json")
    data = json.loads(receipt_path.read_text(encoding="utf-8"))
    require(digest(receipt_path) == report["dataDiskReceiptSha256"].lower() and Path(data["outputPath"]).resolve() == output.resolve() and
            report["dataDiskWriteProtected"] is False and report["connectedFloppyDrives"] == 2 and
            digest(data["inputPath"]) == data["inputSha256"] == report["dataDiskInitialSha256"].lower() and
            digest(output) == report["dataDiskFinalSha256"].lower(), "Writable input and actual output bindings")
    require(digest(receipt_path.with_name("creation.json")) == data["creationSha256"] and
            digest(Path(__file__).with_name("prepare_relabel_data_disk.py")) == data["producerSha256"], "Data creation binding")
    verify_image(Path(data["inputPath"]), data["files"], "FixtureDisk")
    initial = Path(data["inputPath"]).read_bytes()
    persisted = verify_sectors(initial, output.read_bytes())
    expected_label, expected_result, expected_error, expected_output = ORIGINAL[scenario]
    cases = []
    for i, name in enumerate([NAME_BOUNDARIES[scenario], "SavedDisk"]):
        row = rows[i]
        run = one([e for e in events if e["Name"] == "RunCommand" and e["D1"] == segment and
                   e["Task"] == row["Task"] and e["EntryRetireCycle"] == row["EntryCycle"]], "Run binding")
        require(run["ReturnedD0"] == row["ReturnCode"] and run["ReturnRetireCycle"] == row["ReturnCycle"], "Exact observed return")
        disk_checkpoint(run, initial, "FixtureDisk" if i == 0 else expected_label)
        case = invocation(report, row, name, candidate)
        require((case["result"], case["ioErr"]) == ((expected_result, expected_error) if i == 0 else (0, 0)), "Frozen original result/error")
        diagnostic = "RAM:rn" + str(i + 1).zfill(2)
        case["outputHex"] = readback(events, diagnostic).hex()
        require(bytes.fromhex(case["outputHex"]) == (expected_output if i == 0 else b""), "Frozen fault/empty output")
        matched = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == diagnostic], "Diagnostic lookup")
        require(remove["ReturnRetireCycle"] < matched["EntryRetireCycle"], "Diagnostics after cleanup")
        path = "DF1:proof" if i == 0 else "SavedDisk:proof"
        matched = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == path], "Payload lookup")
        require(row["ReturnCycle"] < matched["EntryRetireCycle"] < matched["ReturnRetireCycle"] <
                (rows[1]["EntryCycle"] if i == 0 else remove["EntryRetireCycle"]) and readback(events, path) == b"relabel-payload\n", "Payload remains accessible")
        cases.append(case)
    return {"scenario": scenario, "cases": cases, "boundaryPersistedLabel": expected_label,
            "recoveryPersistedLabel": "SavedDisk", "persistentDisk": persisted}


if __name__ == "__main__":
    qualify(observe, "Relabel-FFS-name-boundary-original-DOS-boot", __file__,
            "One FFS name boundary and subsequent recovery per pair, with intermediate root/non-root observation and final sector readback. 68000 original DOS under host Exec/device overlays; no OFS, other platforms/launch, full original PURE or shipping admission.",
            dependencies=("verify_relabel_persistent_boot.py", "prepare_relabel_data_disk.py"))
