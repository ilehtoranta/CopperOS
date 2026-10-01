"""Verify paired original-DOS Relabel boot effects, diagnostics and ownership."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import xml.etree.ElementTree as ET
from prepare_relabel_boot import STARTUP, inventory
from append_hunk_version import parse_hunk
from Inventory.prepare_execute_fixture_adf import load_image, data_blocks


def require(condition, message):
    if not condition:
        raise ValueError(message)


def one(items, label):
    require(len(items) == 1, label + ": expected exactly one")
    return items[0]


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def read_trace(path):
    root = ET.parse(path).getroot()
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    result = one(root.findall(".//t:UnitTestResult", ns), "Boot test")
    require(result.attrib["outcome"] == "Passed", "Boot prerequisite test failed")
    stdout = one(result.findall(".//t:StdOut", ns), "Boot stdout").text
    return json.loads(stdout.split("DOS passive boot readiness: ", 1)[1])


def verify_media(report, media, binary, candidate, startup=STARTUP):
    require(report.get("scope") == "CC12/disposable-Relabel-derivative-boot-progress-only" and
        report.get("commandUnderTest") == "Relabel", "Wrong command scope")
    require(report.get("rootInfoReady") and report.get("diskBytesUnchanged") and
        report.get("diskWriteProtected") and report.get("referenceMediaUnmodified"), "Boot/media prerequisite")
    require(not any(report.get(k) for k in ["failure", "boundedStop", "dosObservationOverflow",
        "returnObservationCollision"]), "Incomplete boot trace")
    require(report["fixtureImageSha256"].lower() == media["output_adf_sha256"] ==
        report["finalImageSha256"].lower(), "Derivative image identity")
    require(media["probe_sha256"] == hashlib.sha256(startup.encode("ascii")).hexdigest(), "Wrong startup scenario")
    require(media["binary_role"] == "workbench31-" + ("candidate" if candidate else "reference"), "Wrong binary role")
    replacement = one(media["replacements"], "Replacement")
    require(replacement["guest_path"] == "c/ed" and replacement["sha256"] == digest(binary), "HUNK identity")
    hunk = Path(binary).read_bytes()
    parsed_hunk = parse_hunk(hunk)
    require(report.get("copyInvocationOwnership"), "No completed command invocation")
    images = report.get("copySegmentGenerations")
    if images is None:  # Retained single-segment captures predate generation tracking.
        images = [{"Segment": report["copyInvocationOwnership"][0]["Segment"],
            "CodeBytes": report["loadedCopyCodeBytes"], "ImageSha256": report["loadedCopyImageSha256"]}]
    require(images, "No loaded image generations")
    for image in images:
        loaded = bytearray(parsed_hunk.code)
        load_address = image["Segment"] * 4 + 4
        for offset in parsed_hunk.relocations:
            struct.pack_into(">I", loaded, offset, struct.unpack_from(">I", loaded, offset)[0] + load_address)
        require(image["CodeBytes"] == len(loaded) and image["ImageSha256"].lower() ==
            hashlib.sha256(loaded).hexdigest(), "Loaded native code identity")
    require(report["loadedCopyCodeBytes"] == images[-1]["CodeBytes"] and
        report["loadedCopyImageSha256"] == images[-1]["ImageSha256"], "Latest loaded image summary")
    # Independently reopen the private media, rather than trusting receipt fields
    # or a success marker. Only the two declared file allocations may differ.
    archive = Path(media["reference_archive"])
    require(digest(archive) == report["archiveSha256"].lower(), "Original archive identity")
    member, original_bytes = load_image(archive)
    require(member == media["archive_member"] and hashlib.sha256(original_bytes).hexdigest() ==
        report["imageSha256"].lower() == media["reference_adf_sha256"], "Original image identity")
    output_bytes = Path(media["output_path"]).read_bytes()
    require(hashlib.sha256(output_bytes).hexdigest() == media["output_adf_sha256"], "Actual derivative image")
    original_disk, output_disk = inventory.Adf(original_bytes), inventory.Adf(output_bytes)
    tree, output_tree = original_disk.walk(), output_disk.walk()
    require(set(tree) == set(output_tree), "Directory membership changed")
    allowed_blocks = set()
    for path, expected in [("s/startup-sequence", startup.encode("ascii")), ("c/ed", hunk)]:
        header = tree[path]["block"]
        allowed_blocks.update([header, *data_blocks(original_disk, header)])
        require(output_disk.read_file(output_tree[path]["block"]) == expected, "Actual media file differs: " + path)
    require(len(original_bytes) == len(output_bytes) and all(original_bytes[n:n+512] == output_bytes[n:n+512]
        for n in range(0, len(original_bytes), 512) if n // 512 not in allowed_blocks), "Unlisted media blocks changed")


def verify(report, media, binary, candidate):
    verify_media(report, media, binary, candidate)
    require(report.get("copyActiveInvocations") == 0 and report.get("copyMaximumActiveInvocations") == 1 and
        report.get("copyCpuImageWrites") == [] and report.get("copyImageUnchangedAtReturn"), "Shared image lifetime")
    rows = report["copyInvocationOwnership"]
    expected_returns = [0, 0, 0, 20, 20, 20, 20, 0]
    expected_errors = [210, 210, 210, 0, 0, 116, 118, 210]
    require(len(rows) == 8 and [r["ReturnCode"] for r in rows] == expected_returns and
        [r["ReturnIoErr"] for r in rows] == expected_errors, "Results/ambient errors")
    expected_allocations = [2, 2, 2, 2, 1, 1, 1, 2] if candidate else [0] * 8
    for index, row in enumerate(rows):
        require(row["Segment"] == rows[0]["Segment"] and row["Task"] == rows[0]["Task"] and
            row["ImageUnchanged"] and row["Allocations"] == row["Frees"] == expected_allocations[index] and
            row["ParserFailed"] == (index in [5, 6]) and not row["OpenFailed"], "Invocation ownership")
    events = report["observedDosCalls"]
    load = one([e for e in events if e["Name"] == "LoadSeg" and e.get("Text") == "C:Ed"], "Single command load")
    registration = one([e for e in events if e["Name"] == "AddSegment" and e["D2"] == rows[0]["Segment"]], "Resident registration")
    registered_until = events.index(one([e for e in events if e["Name"] == "RemSegment" and
        e.get("RemovedSegmentList") == rows[0]["Segment"]], "Resident lifetime end"))
    runs = [e for e in events[events.index(registration):registered_until] if e["Name"] == "RunCommand" and e["D1"] == rows[0]["Segment"]]
    require(load.get("ReturnedD0") == rows[0]["Segment"] and registration.get("ReturnedD0", 0) != 0 and
        len(runs) == 8 and [e.get("ReturnedD0") for e in runs] == expected_returns and
        events.index(load) < events.index(registration) < events.index(runs[0]), "Resident reuse")
    starts = [i for i, e in enumerate(events) if e["Name"] == "CopyOpenLibrary"]
    ends = [i for i, e in enumerate(events) if e["Name"] == "CopyCloseLibrary"]
    require(len(starts) == len(ends) == 8 and all(a < b for a, b in zip(starts, ends)) and
        all(ends[i] < starts[i+1] for i in range(7)), "Library lease boundaries")
    semantics = []
    expected_lookup = {0: "RAM", 1: "RAM", 2: "RLC", 3: "RA", 7: "RAM"}
    expected_name = {0: "First Volume", 1: "Second Volume", 2: "AliasVolume", 7: "FinalVolume"}
    for index, (a, b) in enumerate(zip(starts, ends)):
        calls = events[a:b+1]
        opened, closed = calls[0], calls[-1]
        require(opened["D0"] == 36 and opened.get("ReturnedD0", 0) != 0 and
            closed["A1"] == opened["ReturnedD0"] and "ReturnedD0" in closed, "DOS library ABI")
        parsed = one([e for e in calls if e["Name"] == "ReadArgs"], "ReadArgs")
        require(parsed["Text"] == "DRIVE/A,NAME/A" and parsed["D3"] == 0 and
            (parsed.get("ReturnedD0", 0) != 0) == (index not in [5, 6]), "Real parser behavior")
        frees = [e for e in calls if e["Name"] == "FreeArgs"]
        if index in [5, 6]:
            require(not frees, "Failed parser was released")
        else:
            freed = one(frees, "FreeArgs")
            require(freed["D1"] == parsed["ReturnedD0"] and "ReturnedD0" in freed and
                calls.index(freed) > calls.index(parsed), "Parser lease")
        locks = [e for e in calls if e["Name"] == "LockDosList"]
        finds = [e for e in calls if e["Name"] == "FindDosEntry"]
        unlocks = [e for e in calls if e["Name"] == "UnLockDosList"]
        mutations = [e for e in calls if e["Name"] == "Relabel"]
        if index in expected_lookup:
            lock, find, unlock = one(locks, "List lock"), one(finds, "List find"), one(unlocks, "List unlock")
            require(lock["D1"] == unlock["D1"] == 29 and find["D1"] == lock["ReturnedD0"] and
                find["D3"] == 28 and find["LookupText"] == expected_lookup[index] and
                (find.get("ReturnedD0", 0) != 0) == (index != 3) and
                calls.index(lock) < calls.index(find) < calls.index(unlock) and "ReturnedD0" in unlock, "DOS-list behavior")
        else:
            require(not locks and not finds and not unlocks, "Rejected input touched DOS list")
        if index in expected_name:
            mutation = one(mutations, "Volume mutation")
            require(mutation["Text"] == expected_lookup[index] + ":" and
                mutation["DestinationText"] == expected_name[index] and mutation["ReturnedD0"] == 0xffffffff and
                calls.index(unlock) < calls.index(mutation) < calls.index(freed), "Relabel ABI/order")
        else:
            require(not mutations, "Rejected input relabeled volume")
        allocations = [e for e in calls if e["Name"] == "CopyAllocMem"]
        releases = [e for e in calls if e["Name"] == "CopyFreeMem"]
        require(len(allocations) == len(releases) == expected_allocations[index] and
            sorted((e["ReturnedD0"], e["D0"]) for e in allocations) ==
            sorted((e["A1"], e["D0"]) for e in releases) and all("ReturnedD0" in e for e in releases), "Exec storage ownership")
        semantic = []
        for e in calls:
            name = e["Name"]
            if name == "ReadArgs":
                semantic.append([name, e["Text"], e["ReturnedD0"] != 0, e["ReturnedIoErr"]])
            elif name in ["LockDosList", "UnLockDosList"]:
                semantic.append([name, e["D1"]])
            elif name == "FindDosEntry":
                semantic.append([name, e["LookupText"], e["D3"], e["ReturnedD0"] != 0, e["ReturnedIoErr"]])
            elif name == "Relabel":
                semantic.append([name, e["Text"], e["DestinationText"], e["ReturnedD0"], e["ReturnedIoErr"]])
            elif name == "PutStr":
                semantic.append([name, e["Text"], e["ReturnedD0"], e["ReturnedIoErr"]])
            elif name == "PrintFault":
                require(e["D2"] == 0, "Fault header changed")
                semantic.append([name, e["D1"], e["ReturnedD0"], e["ReturnedIoErr"]])
            elif name == "FreeArgs":
                semantic.append([name, e["ReturnedIoErr"]])
        semantics.append(semantic)
    removal = one([e for e in events if e["Name"] == "RemSegment" and
        e.get("RemovedSegmentList") == rows[0]["Segment"]], "Resident removal")
    require(removal.get("ReturnedD0", 0) != 0 and events.index(removal) > ends[-1], "Removal result/order")
    segment_free = one([e for e in events if e["Name"] == "SegmentFreeMem"], "Segment release")
    require(segment_free["A1"] == report["copySegmentAllocationBase"] and
        segment_free["D0"] == report["copySegmentAllocationBytes"] and "ReturnedD0" in segment_free and
        events.index(segment_free) > ends[-1], "Segment storage release")
    readbacks = []
    for name in ["relabel-proof", "rl-diag1", "rl-diag2", "rl-diag3", "rl-diag4"]:
        opens = [e for e in events if e["Name"] == "Open" and e.get("Text") == name]
        require(len(opens) == (4 if name == "relabel-proof" else 1), "Independent readback count")
        for number, opened in enumerate(opens):
            handle = opened.get("ReturnedD0", 0)
            require(handle != 0, "Readback open failed")
            position = events.index(opened)
            closed = next(e for e in events[position+1:] if e["Name"] == "Close" and
                e["D1"] == handle and e["Task"] == opened["Task"])
            values = [e["ReturnedD0"] for e in events[position+1:events.index(closed)] if
                e["Name"] == "FGetC" and e["D1"] == handle and e["Task"] == opened["Task"]]
            # Original Type consumes the examined byte length without an extra
            # FGetC at EOF. Require that exact size below, not an invented call.
            require(values and all(v <= 255 for v in values) and
                closed.get("ReturnedD0", 0) != 0, "Readback bytes/close")
            payload = bytes(values)
            if name == "relabel-proof":
                require(payload == b"relabel-payload\n", "Volume content changed")
                owner_index = [0, 1, 2, 7][number]
                path = expected_name[owner_index] + ":relabel-proof"
                matched = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == path], "New volume lookup")
                require(matched.get("ReturnedD0") == 0 and matched["FileSize"] == len(payload) and
                    ends[owner_index] < events.index(matched) < position and
                    (owner_index == 7 or position < starts[owner_index+1]), "Volume readback order/size")
            else:
                require(position > events.index(removal), "Diagnostic readback before removal")
                matched = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == "RAM:" + name], "Diagnostic lookup")
                require(matched.get("ReturnedD0") == 0 and matched["FileSize"] == len(payload) and
                    events.index(removal) < events.index(matched) < position, "Diagnostic readback size/order")
            readbacks.append({"name": name, "bytes": payload.hex()})
    require(bytes.fromhex(readbacks[4]["bytes"]) == b"Invalid device or volume name\n" and
        bytes.fromhex(readbacks[5]["bytes"]) == b"':' not legal character in volume name\n", "Command diagnostics")
    return {"returns": expected_returns, "errors": expected_errors, "semantics": semantics, "readbacks": readbacks}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ["reference-trx", "candidate-trx", "reference-media", "candidate-media", "qualification", "test-assembly", "output-directory"]:
        parser.add_argument("--" + name, type=Path, required=True)
    args = parser.parse_args()
    args.output_directory.mkdir(parents=True, exist_ok=False)
    observations = []
    inputs = {}
    for role in ["reference", "candidate"]:
        trx = getattr(args, role + "_trx")
        media_path = getattr(args, role + "_media")
        media = json.loads(media_path.read_text(encoding="utf-8"))
        report = read_trace(trx)
        require(report["fixtureReceiptSha256"].lower() == digest(media_path), "Receipt binding")
        require(media["native_qualification_sha256"] == digest(args.qualification), "Native qualification binding")
        require(report["testAssemblySha256"].lower() == digest(args.test_assembly) and
            report["emulatorAssemblySha256"].lower() == digest(args.test_assembly.parent / "CopperMod.Amiga.Emulator.dll"), "Executor binding")
        replacement = one(media["replacements"], "Binary")
        binary = Path(replacement["local_file"])
        if role == "reference":
            require(digest(binary) == "163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff", "Original identity")
        else:
            qualification = json.loads(args.qualification.read_text(encoding="utf-8-sig"))
            require(qualification["status"] == "passed" and digest(binary) == one([a for a in qualification["artifacts"]
                if a["cpu"] == "68000"], "Native target")["sha256"], "Replacement qualification")
        observations.append(verify(report, media, binary, role == "candidate"))
        output = args.output_directory / (role + "-observations.json")
        output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        inputs[role] = {"trxSha256": digest(trx), "mediaReceiptSha256": digest(media_path),
            "binarySha256": digest(binary), "observationsSha256": digest(output)}
    require(observations[0] == observations[1], "Original and replacement observations differ")
    result = {"status": "passed", "suite": "Relabel-original-DOS-paired-boot", "cpu": "68000",
        "commandComparisons": 8, "volumeReadbacksPerBinary": 4, "diagnosticReadbacksPerBinary": 4,
        "inputs": inputs, "observations": observations[0], "verifierSha256": digest(__file__),
        "normalization": "Ignore pointer addresses and additional private allocations; validate their ownership independently. Compare ordered operations, result/IoErr and all readback bytes.",
        "limits": "Original DOS/Shell under the assembly-bound emulator's host Exec/device takeover; not an entirely native original OS. Forced PURE sequential reuse is a fixture, not original classification or full resident admission.",
        "shippingOrPureApproval": False}
    path = args.output_directory / "comparison.json"
    path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(path)


if __name__ == "__main__":
    main()
