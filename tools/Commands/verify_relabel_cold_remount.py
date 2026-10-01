"""Verify fresh ROM boots read the exact persisted Relabel outputs without mutation."""
import hashlib
import json
from pathlib import Path
from prepare_relabel_boot import COLD_REMOUNT_STARTUP
from prepare_relabel_cold_remount import qualified_source
from verify_relabel_boot import digest, inventory, one, require, data_blocks, load_image
from verify_relabel_extended_boot import readback
from verify_relabel_replacement_boot import qualify


def verify_unexecuted_media(report, media, candidate):
    # This is deliberately separate from verify_media's loaded-image checks:
    # cold readback must execute NO Relabel command at all.
    require(report["commandUnderTest"] == "Relabel" and media["scenario"] == "cold-remount" and
            media["binary_role"] == "workbench31-" + ("candidate" if candidate else "reference"), "Cold boot identity")
    require(report["rootInfoReady"] and report["diskBytesUnchanged"] and report["diskWriteProtected"] and
            report["referenceMediaUnmodified"] and not report["cpuStatePatchedAfterReset"] and
            not report["privateDosStructuresWritten"], "Unmodified protected boot and no injected guest state")
    require(not any(report.get(k) for k in ["failure", "boundedStop", "dosObservationOverflow", "returnObservationCollision"]), "Complete boot capture")
    require(report["maximumChunks"] == 32 and report["instructionsPerChunk"] == 250000 and
            any(s.get("originalDosVectorCountWithoutGateway") == 154 for s in report["snapshots"]), "Original DOS boot within existing bound")
    require(not report["copyInvocationOwnership"] and not report["copySegmentGenerations"] and
            not report["copyCpuImageWrites"] and not any(e["Name"] == "Relabel" or
            e["Name"] == "LoadSeg" and e.get("Text") == "C:Ed" for e in report["observedDosCalls"]), "No relabel or resident image load in fresh boot")
    archive = Path(media["reference_archive"])
    require(digest(archive) == report["archiveSha256"].lower(), "Reference archive hash")
    member, before = load_image(archive)
    require(member == media["archive_member"] and hashlib.sha256(before).hexdigest() ==
            media["reference_adf_sha256"] == report["imageSha256"].lower(), "Reference ADF identity")
    after = Path(media["output_path"]).read_bytes()
    require(digest(media["output_path"]) == media["output_adf_sha256"] == report["fixtureImageSha256"].lower() ==
            report["finalImageSha256"].lower(), "Protected derivative identity")
    startup = COLD_REMOUNT_STARTUP.encode("ascii")
    require(media["probe_sha256"] == media["startup_sha256"] == hashlib.sha256(startup).hexdigest(), "Read-only startup identity")
    replacement = one(media["replacements"], "Unused replacement file")
    require(replacement["guest_path"] == "c/ed" and replacement["sha256"] == digest(replacement["local_file"]), "Unused file identity")
    original, modified = inventory.Adf(before), inventory.Adf(after)
    tree, changed = original.walk(), modified.walk()
    require(set(tree) == set(changed), "Boot tree membership")
    allowed = set()
    for path, expected in [("s/startup-sequence", startup), ("c/ed", Path(replacement["local_file"]).read_bytes())]:
        header = tree[path]["block"]
        allowed.update([header, *data_blocks(original, header)])
        require(modified.read_file(changed[path]["block"]) == expected, "Actual boot file " + path)
    require(len(before) == len(after) and all(before[i:i+512] == after[i:i+512]
            for i in range(0, len(before), 512) if i // 512 not in allowed), "No hidden boot-media edits")


def observe(report, media, candidate):
    verify_unexecuted_media(report, media, candidate)
    role = "candidate" if candidate else "reference"
    output = Path(report["dataDiskOutputPath"])
    receipt_path = output.with_name(role + "-data.json")
    receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
    require(digest(receipt_path) == report["dataDiskReceiptSha256"].lower() and receipt["sourceRole"] == role and
            digest(Path(__file__).with_name("prepare_relabel_cold_remount.py")) == receipt["producerSha256"], "Cold input provenance")
    source = Path(receipt["sourceDirectory"])
    comparison, prior = qualified_source(source)
    require(digest(source / "qualified/comparison.json") == receipt["sourceComparisonSha256"] and
            comparison["inputs"][role]["observationsSha256"] == receipt["sourceObservationsSha256"] and
            Path(receipt["inputPath"]).resolve() == Path(prior[role]["dataDiskOutputPath"]).resolve() and
            receipt["inputSha256"] == prior[role]["dataDiskFinalSha256"].lower(), "Exact corresponding persisted source disk")
    require(report["dataDiskColdRemount"] is True and report["connectedFloppyDrives"] == 2 and
            report["dataDiskWriteProtected"] is True and Path(receipt["outputPath"]).resolve() == output.resolve() and
            receipt["inputSha256"] == digest(receipt["inputPath"]) == report["dataDiskInitialSha256"].lower() ==
            report["dataDiskFinalSha256"].lower() == digest(output), "Read-only disk bytes survive cold mount unchanged")
    disk = inventory.Adf(output.read_bytes())
    require(disk.volume == "SavedDisk", "Saved label on actual remounted disk")
    events = report["observedDosCalls"]
    require(not any(e["Name"] == "IntuitionEasyRequestArgs" for e in events), "No unresolved volume requester")
    for path in ["DF1:proof", "SavedDisk:proof"]:
        require(readback(events, path) == b"relabel-payload\n", "Cold proof read: " + path)
    matched = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == "SavedDisk:nested/guard"], "Nested guard lookup")
    require(matched["ReturnedD0"] == 0 and matched["FileSize"] == 1792, "Cold nested file metadata")
    start = events.index(matched) + 1
    opened = one([e for e in events[start:] if e["Name"] == "Open" and e.get("Text") in ["guard", "nested/guard"]], "Nested file open")
    require(opened["ReturnedD0"] != 0, "Nested file handle")
    closed = one([e for e in events[events.index(opened)+1:] if e["Name"] == "Close" and
                  e["D1"] == opened["ReturnedD0"] and e["Task"] == opened["Task"]], "Nested file close")
    guard = one([f for f in comparison["observations"]["persistentDisk"]["files"] if f["path"] == "nested/guard"], "Original guard bytes")
    require(closed["ReturnedD0"] != 0 and closed["IndependentReadbackBytes"] == guard["bytes"] == 1792 and
            closed["IndependentReadbackSha256"].lower() == guard["sha256"], "Actual guest nested file bytes and close")
    wait = one([e for e in events if e["Name"] == "ReadArgs" and e.get("Text") == "/N,SEC=SECS/S,MIN=MINS/S,UNTIL/K"], "Final guest wait")
    require(closed["ReturnRetireCycle"] < wait["EntryRetireCycle"], "Guest progressed past all readbacks")
    return {"volume": "SavedDisk", "proofReads": 2, "guardBytes": 1792, "guardSha256": guard["sha256"],
            "diskUnchanged": True, "writeProtected": True, "relabelInvocations": 0,
            "sourceComparisonSha256": receipt["sourceComparisonSha256"]}


if __name__ == "__main__":
    qualify(observe, "Relabel-cold-remount-original-DOS-boot", __file__,
            "Fresh independent 68000 ROM boots read the exact original/replacement DOS1 exports by saved volume name without Relabel execution or disk mutation. Explicit original Mount and host Exec/device overlays. No automatic drive discovery, OFS, other CPU/launch, full PURE or shipping admission.",
            dependencies=("prepare_relabel_cold_remount.py", "verify_relabel_persistent_boot.py", "prepare_relabel_data_disk.py"))
