"""Compare real Relabel calls and independently verify persistent FFS sectors."""
import hashlib
import json
from pathlib import Path
import sys

from prepare_relabel_boot import PERSISTENT_STARTUP
from verify_relabel_boot import digest, inventory, one, require, verify_media
from verify_relabel_extended_boot import readback
from verify_relabel_replacement_boot import qualify

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "DiskBuilder"))
from verify_image import verify_image


def verify_sectors(before, after):
    require(len(before) == len(after) == 901120, "Complete DD floppy sectors")
    original, changed = inventory.Adf(before), inventory.Adf(after)
    require(original.volume == "FixtureDisk" and changed.volume == "SavedDisk" and
            original.root == changed.root, "Persisted final volume name")
    blocks = [n // 512 for n in range(0, len(before), 512) if before[n:n+512] != after[n:n+512]]
    require(blocks == [original.root], "Only the volume root block changes")
    # Label, root/volume modification timestamps and checksum may change.
    # Creation date, hash table, bitmap pointers, allocation state and all file
    # bytes must remain untouched. Never rewrite the captured image to pass.
    allowed = set(range(20, 24)) | set(range(420, 464)) | set(range(472, 484))
    a, b = original.block(original.root), changed.block(changed.root)
    require(all(a[n] == b[n] for n in range(512) if n not in allowed), "Root changes limited to label, dates and checksum")
    changed.header(changed.root, (2,), (1,))  # Independently checks the checksum.
    original_tree, changed_tree = original.walk(), changed.walk()
    require(original_tree == changed_tree, "Directory topology and metadata preserved")
    files = []
    for path, entry in sorted(original_tree.items()):
        if entry["secondary_type"] == -3:
            old = original.read_file(entry["block"])
            new = changed.read_file(changed_tree[path]["block"])
            require(new == old, "File contents preserved: " + path)
            files.append({"path": path, "bytes": len(old), "sha256": hashlib.sha256(old).hexdigest()})
    return {"filesystem": "DOS1", "initialVolume": original.volume, "finalVolume": changed.volume,
            "changedBlocks": blocks, "files": files}


def observe(report, media, candidate):
    require(media["scenario"] == "persistent-volume", "Persistent-volume scenario")
    verify_media(report, media, media["replacements"][0]["local_file"], candidate, PERSISTENT_STARTUP)
    data_output = Path(report["dataDiskOutputPath"])
    data_receipt = data_output.with_name(("candidate" if candidate else "reference") + "-data.json")
    require(report["dataDiskReceiptSha256"].lower() == digest(data_receipt), "Data receipt binding")
    data = json.loads(data_receipt.read_text())
    require(Path(data["outputPath"]).resolve() == data_output.resolve() and
            report["connectedFloppyDrives"] == 2 and
            report["dataDiskWriteProtected"] is False and
            report["dataDiskInitialSha256"].lower() == data["inputSha256"] == digest(data["inputPath"]) and
            report["dataDiskFinalSha256"].lower() == digest(data_output), "Actual input/output sector bindings")
    require(digest(data_receipt.with_name("creation.json")) == data["creationSha256"] and
            digest(Path(__file__).with_name("prepare_relabel_data_disk.py")) == data["producerSha256"], "Owned-data producer binding")
    # Strong filesystem validation of the initial image, plus exact unchanged
    # non-root bytes, carries bitmap and payload integrity into the output.
    initial = verify_image(Path(data["inputPath"]), data["files"], "FixtureDisk")
    persisted = verify_sectors(Path(data["inputPath"]).read_bytes(), data_output.read_bytes())
    persisted["initialSha256"] = initial["image_sha256"]
    persisted["readerSha256"] = initial["reader"]["sha256"]
    persisted["inventoryReaderSha256"] = initial["inventory_reader"]["sha256"]
    rows, events = report["copyInvocationOwnership"], report["observedDosCalls"]
    require(len(rows) == 2 and report["copyActiveInvocations"] == 0 and report["copyMaximumActiveInvocations"] == 1 and
            report["copyCpuImageWrites"] == [] and report["copyImageUnchangedAtReturn"], "Two complete unchanged invocations")
    require(len({r["Segment"] for r in rows}) == len({r["Task"] for r in rows}) == 1, "Same resident image and task")
    segment = rows[0]["Segment"]
    load = one([e for e in events if e["Name"] == "LoadSeg" and e.get("Text") == "C:Ed"], "One image load")
    add = one([e for e in events if e["Name"] == "AddSegment" and e["D2"] == segment], "One registration")
    remove = one([e for e in events if e["Name"] == "RemSegment" and e.get("RemovedSegmentList") == segment], "One removal")
    freed = one([e for e in events if e["Name"] == "SegmentFreeMem"], "One image release")
    require(load["ReturnedD0"] == segment and add["ReturnedD0"] != 0 and remove["ReturnedD0"] != 0 and
            freed["A1"] == report["copySegmentAllocationBase"] and freed["D0"] == report["copySegmentAllocationBytes"] and
            load["ReturnRetireCycle"] < add["EntryRetireCycle"] < add["ReturnRetireCycle"] < rows[0]["EntryCycle"] and
            rows[0]["ReturnCycle"] < rows[1]["EntryCycle"] < rows[1]["ReturnCycle"] < remove["EntryRetireCycle"] <
            freed["EntryRetireCycle"] < freed["ReturnRetireCycle"], "Resident image lifetime surrounds callers")
    cases = []
    for i, (drive, label) in enumerate([("DF1:", "FirstDisk"), ("FirstDisk:", "SavedDisk")]):
        row = rows[i]
        calls = [e for e in events if e["Task"] == row["Task"] and row["EntryCycle"] < e["EntryRetireCycle"] < row["ReturnCycle"]]
        def call(name):
            return one([e for e in calls if e["Name"] == name], name)
        run = one([e for e in events if e["Name"] == "RunCommand" and e["D1"] == segment and
                   e["Task"] == row["Task"] and e["EntryRetireCycle"] == row["EntryCycle"]], "Run owner")
        require(run["ReturnedD0"] == row["ReturnCode"] == 0 and run["ReturnRetireCycle"] == row["ReturnCycle"] and
                row["ImageUnchanged"] and not row["ParserFailed"] and not row["OpenFailed"], "Actual success and intact image")
        opened, parsed, lock, found, unlock, mutation, released, closed = (call(n) for n in
            ["CopyOpenLibrary", "ReadArgs", "LockDosList", "FindDosEntry", "UnLockDosList", "Relabel", "FreeArgs", "CopyCloseLibrary"])
        require(opened["D0"] == 36 and opened["ReturnedD0"] != 0 and closed["A1"] == opened["ReturnedD0"] and
                parsed["Text"] == "DRIVE/A,NAME/A" and parsed["D3"] == 0 and parsed["ReturnedD0"] != 0 and
                released["D1"] == parsed["ReturnedD0"], "Public DOS library/parser ownership")
        require(lock["D1"] == unlock["D1"] == 29 and found["D3"] == 28 and found["LookupText"] == drive[:-1] and
                found["ReturnedD0"] != 0 and mutation["Text"] == drive and mutation["DestinationText"] == label and
                mutation["ReturnedD0"] != 0, "Device/volume lookup and handler relabel")
        ordered = [opened, parsed, lock, found, unlock, mutation, released, closed]
        require(all(a["EntryRetireCycle"] < a["ReturnRetireCycle"] < b["EntryRetireCycle"] for a, b in zip(ordered, ordered[1:])) and
                closed["EntryRetireCycle"] < closed["ReturnRetireCycle"] < row["ReturnCycle"], "Release list lock before mutation and clean up before return")
        require(not any(e["Name"] == "PrintFault" for e in calls), "No success diagnostic")
        allocations = [e for e in calls if e["Name"] == "CopyAllocMem"]
        releases = [e for e in calls if e["Name"] == "CopyFreeMem"]
        require(len(allocations) == len(releases) == row["Allocations"] == row["Frees"] == (2 if candidate else 0), "Invocation storage counts")
        for allocation in allocations:
            free = one([e for e in releases if e["A1"] == allocation["ReturnedD0"] and e["D0"] == allocation["D0"]], "Owned free")
            require(allocation["ReturnedD0"] != 0 and allocation["D1"] == 0x10001 and
                    allocation["ReturnRetireCycle"] < free["EntryRetireCycle"] < free["ReturnRetireCycle"] < row["ReturnCycle"], "Allocation ownership and lifetime")
        if candidate:
            a, b = allocations
            require(a["D0"] == 8 and a["ReturnedD0"] == parsed["D2"] and a["ReturnRetireCycle"] < parsed["EntryRetireCycle"] and
                    (a["ReturnedD0"] + a["D0"] <= b["ReturnedD0"] or b["ReturnedD0"] + b["D0"] <= a["ReturnedD0"]) and
                    released["ReturnRetireCycle"] < one([e for e in releases if e["A1"] == parsed["D2"]], "Slot release")["EntryRetireCycle"],
                    "Distinct live argument/scratch storage and slot lifetime")
        diagnostic = "RAM:rp" + str(i + 1).zfill(2)
        require(readback(events, diagnostic) == b"", "Empty success output")
        output_match = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == diagnostic], "Output readback")
        require(remove["ReturnRetireCycle"] < output_match["EntryRetireCycle"], "Diagnostic read after cleanup")
        proof_match = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == label + ":proof"], "Volume readback")
        boundary = rows[1]["EntryCycle"] if i == 0 else remove["EntryRetireCycle"]
        require(row["ReturnCycle"] < proof_match["EntryRetireCycle"] < proof_match["ReturnRetireCycle"] < boundary and
                readback(events, label + ":proof") == b"relabel-payload\n", "Payload accessible through new volume name")
        cases.append({"drive": drive, "name": label, "result": row["ReturnCode"], "ioErr": row["ReturnIoErr"],
                      "parserIoErr": parsed["ReturnedIoErr"], "lookupIoErr": found["ReturnedIoErr"],
                      "handlerBool": mutation["ReturnedD0"], "handlerIoErr": mutation["ReturnedIoErr"],
                      "cleanupIoErr": released["ReturnedIoErr"], "outputHex": ""})
    initial_match = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == "FixtureDisk:proof"], "Initial payload")
    require(initial_match["ReturnRetireCycle"] < rows[0]["EntryCycle"] and readback(events, "FixtureDisk:proof") == b"relabel-payload\n", "Initial mounted payload")
    return {"cases": cases, "persistentDisk": persisted, "residentInvocations": 2}


if __name__ == "__main__":
    qualify(observe, "Relabel-persistent-FFS-original-DOS-boot", __file__,
            "Two successful relabels of one owned DOS1 floppy by device then volume name, persisted sectors and payload integrity. Original DOS with current host Exec/device overlays on 68000. No cold-remount, OFS, other CPUs, Workbench launch, original PURE classification or shipping admission.",
            dependencies=("prepare_relabel_data_disk.py",))
