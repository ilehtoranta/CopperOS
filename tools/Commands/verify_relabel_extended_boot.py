"""Compare extended Relabel grammar/help/handler cases using actual DOS boots."""
import argparse
import json
from pathlib import Path
from prepare_relabel_boot import SCENARIOS
from verify_relabel_boot import digest, one, read_trace, require, verify_media


def readback(events, path):
    match = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == path], "Readback match " + path)
    require(match.get("ReturnedD0") == 0 and 0 <= match["FileSize"] <= 4096, "Readback file/size")
    name = path.split(":", 1)[1]
    first = events.index(match) + 1
    last = next((i for i in range(first, len(events)) if events[i]["Name"] == "MatchFirst"), len(events))
    opened = [e for e in events[first:last] if e["Name"] == "Open" and e.get("Text") == name]
    if not opened:
        require(match["FileSize"] == 0, "Missing nonempty readback open")
        return b""
    # Equal basenames under later volume labels must not substitute for this
    # Type invocation's readback if an open event is absent from the trace.
    opened = one(opened, "Readback open")
    require(opened.get("ReturnedD0", 0) != 0, "Readback open failed")
    a = events.index(opened)
    closed = next(e for e in events[a+1:] if e["Name"] == "Close" and
        e["D1"] == opened["ReturnedD0"] and e["Task"] == opened["Task"])
    require(events.index(closed) < last, "Readback crossed a later match")
    values = [e["ReturnedD0"] for e in events[a+1:events.index(closed)] if e["Name"] == "FGetC" and
        e["D1"] == opened["ReturnedD0"] and e["Task"] == opened["Task"]]
    # Type may read an extra EOF for an empty file or a final line without LF.
    # Only that single terminal sentinel is excluded; metadata still binds the
    # exact byte count and any earlier/nonterminal sentinel fails below.
    if values and values[-1] == 0xffffffff:
        require(len(values) == match["FileSize"] + 1, "Terminal EOF position")
        values = values[:-1]
    require(len(values) == match["FileSize"] and all(v <= 255 for v in values) and
        closed.get("ReturnedD0", 0) != 0, "Readback size/bytes/close")
    return bytes(values)


def observe(report, media, candidate, scenario):
    binary = media["replacements"][0]["local_file"]
    verify_media(report, media, binary, candidate, SCENARIOS[scenario])
    require(media["scenario"] == scenario, "Scenario identity")
    count = 8 if scenario == "arguments" else (3 if scenario == "help" else 4)
    prefix = "ra" if scenario == "arguments" else "rh"
    rows = report["copyInvocationOwnership"]
    require(len(rows) == count and report.get("copyActiveInvocations") == 0 and
        report.get("copyMaximumActiveInvocations") == 1 and report.get("copyCpuImageWrites") == [], "Complete sequential invocations")
    events = report["observedDosCalls"]
    segment = rows[0]["Segment"]
    load = one([e for e in events if e["Name"] == "LoadSeg" and e.get("Text") == "C:Ed"], "One load")
    add = one([e for e in events if e["Name"] == "AddSegment" and e["D2"] == segment], "Registration")
    remove = one([e for e in events if e["Name"] == "RemSegment" and e.get("RemovedSegmentList") == segment], "Removal")
    segment_free = one([e for e in events if e["Name"] == "SegmentFreeMem"], "Segment free")
    require(load.get("ReturnedD0") == segment and add.get("ReturnedD0", 0) != 0 and remove.get("ReturnedD0", 0) != 0 and
        segment_free["A1"] == report["copySegmentAllocationBase"] and segment_free["D0"] == report["copySegmentAllocationBytes"] and
        "ReturnedD0" in segment_free, "Resident storage identity")
    starts = [i for i, e in enumerate(events) if e["Name"] == "CopyOpenLibrary"]
    ends = [i for i, e in enumerate(events) if e["Name"] == "CopyCloseLibrary"]
    # After removal, Type/Wait can reuse the same allocation address. A BPTR
    # identifies this command only during its registered segment lifetime.
    runs = [e for e in events[events.index(add):events.index(remove)] if e["Name"] == "RunCommand" and e["D1"] == segment]
    require(len(starts) == len(ends) == len(runs) == count and events.index(load) < events.index(add) < starts[0] and
        ends[-1] < events.index(remove) and ends[-1] < events.index(segment_free) and
        all(starts[i] < ends[i] < starts[i+1] for i in range(count-1)), "Resident/library ordering")
    cases = []
    for index, row in enumerate(rows):
        calls = events[starts[index]:ends[index]+1]
        require(row["Segment"] == segment and row["Task"] == rows[0]["Task"] and row["ImageUnchanged"] and
            runs[index].get("ReturnedD0") == row["ReturnCode"] and not row["OpenFailed"], "Invocation result/image")
        require(calls[0]["D0"] == 36 and calls[0].get("ReturnedD0", 0) != 0 and
            calls[-1]["A1"] == calls[0]["ReturnedD0"] and "ReturnedD0" in calls[-1], "DOS lease")
        parsed = one([e for e in calls if e["Name"] == "ReadArgs"], "Parser")
        require(parsed["Text"] == "DRIVE/A,NAME/A" and parsed["D3"] == 0 and
            (parsed.get("ReturnedD0", 0) == 0) == row["ParserFailed"], "Actual ReadArgs result")
        frees = [e for e in calls if e["Name"] == "FreeArgs"]
        if row["ParserFailed"]:
            require(not frees, "Failed parser release")
        else:
            freed = one(frees, "Parser cleanup")
            require(freed["D1"] == parsed["ReturnedD0"] and "ReturnedD0" in freed, "Parser cleanup identity")
        allocations = [e for e in calls if e["Name"] == "CopyAllocMem"]
        releases = [e for e in calls if e["Name"] == "CopyFreeMem"]
        expected_count = (1 if row["ParserFailed"] else 2) if candidate else 0
        require(len(allocations) == len(releases) == row["Allocations"] == row["Frees"] == expected_count and
            sorted((e["ReturnedD0"], e["D0"]) for e in allocations) == sorted((e["A1"], e["D0"]) for e in releases) and
            all("ReturnedD0" in e for e in releases), "Direct allocation ownership")
        ops = []
        for e in calls:
            name = e["Name"]
            if name == "ReadArgs": ops.append([name, e["Text"], e["ReturnedD0"] != 0, e["ReturnedIoErr"]])
            elif name in ["LockDosList", "UnLockDosList"]:
                require(e["D1"] == 29 and "ReturnedD0" in e, "List lock flags/completion")
                ops.append([name, e["D1"]])
            elif name == "FindDosEntry":
                require(e["D3"] == 28, "List find flags")
                ops.append([name, e["LookupText"], e["D3"], e["ReturnedD0"] != 0, e["ReturnedIoErr"]])
            elif name == "Relabel": ops.append([name, e["Text"], e["DestinationText"], e["ReturnedD0"], e["ReturnedIoErr"]])
            elif name in ["PutStr", "VPrintf"]: ops.append([name, e["Text"], e["ReturnedD0"], e["ReturnedIoErr"]])
            elif name == "PrintFault":
                require(e["D2"] == 0, "Fault header")
                ops.append([name, e["D1"], e["ReturnedD0"], e["ReturnedIoErr"]])
            elif name == "FreeArgs": ops.append([name, e["ReturnedIoErr"]])
        path = "RAM:" + prefix + str(index + (2 if scenario == "help" else 1)).zfill(2)
        matched = one([e for e in events if e["Name"] == "MatchFirst" and e.get("Text") == path], "Output readback")
        require(events.index(matched) > events.index(remove), "Output read before resident removal")
        cases.append({"case": index+1, "result": row["ReturnCode"], "ioError": row["ReturnIoErr"],
            "parserFailed": row["ParserFailed"], "operations": ops, "outputHex": readback(events, path).hex()})
    paths = ["Lower", 'Quote"Name', "Star*Name", "Recovery"] if scenario == "arguments" else ["HelpVolume", "Recovery"]
    for label in paths:
        require(readback(events, label + ":relabel-proof") == b"relabel-payload\n", "New volume payload")
    result = {"cases": cases, "payloadVolumeNames": paths}
    if scenario == "help-handler":
        result["cancellation"] = verify_cancellation(report, cases)
    return result


def verify_cancellation(report, cases):
    require(report.get("scriptedKeyboardInput") is True and
        report.get("scriptedKeyboardProvider") == "AmigaBootController.QueueHostKeyDown/Up" and
        report.get("scriptedKeyboardStage") == 4 and report.get("requesterStillPending") is False,
        "Requester cancellation provider/completion")
    require(not report.get("copyClosePacketOverflow"), "Packet observation overflow")
    keys = report["scriptedKeyboardEvents"]
    require([e["Action"] for e in keys] == ["LeftAmigaDown", "BDown", "BUp", "LeftAmigaUp"] and
        all(e["Accepted"] and e["QueuedRawKeys"] == 0 for e in keys) and
        [e["ObservedInputDeviceEvents"] for e in keys] == [0, 1, 2, 3] and
        all(keys[i]["Cycle"] < keys[i+1]["Cycle"] for i in range(3)), "Accepted ordered keyboard sequence")
    require(all(e["RequesterPending"] and e["RequesterTaskWaiting"] for e in keys[:2]) and
        not any(e["RequesterPending"] for e in keys[2:]), "Cancel must resolve between B-down and B-up")
    expected = [(1, 0x66, 0x40), (1, 0x35, 0x40), (1, 0xb5, 0x40), (1, 0xe6, 0)]
    require([(e["Class"], e["Code"], e["Qualifier"]) for e in report["scriptedInputDeviceEvents"]] == expected,
        "Input device key codes/qualifiers")
    events = report["observedDosCalls"]
    handlers = [e for e in events if e["Name"] == "NativeInputHandler"]
    require([(e.get("InputClass"), e.get("InputCode"), e.get("InputQualifier")) for e in handlers] == expected and
        all("ReturnedD0" in e for e in handlers) and handlers[1]["ReturnedD0"] == 0 and
        all(e["EntryRetireCycle"] > key["Cycle"] for e, key in zip(handlers, keys)),
        "Guest handler delivery/Cancel consumption")
    dialog = one([e for e in events if e["Name"] == "IntuitionEasyRequestArgs"], "Requester")
    protected = one([e for e in events if e["Name"] == "Relabel" and e.get("Text") == "DF0:"], "Protected call")
    require(dialog.get("ReturnedD0") == 0 and dialog["Task"] == protected["Task"] and
        all(k["RequesterTask"] == dialog["Task"] for k in keys) and
        dialog["EntryRetireCycle"] < keys[0]["Cycle"] and protected["DestinationText"] == "MustNotChange" and
        protected.get("ReturnedD0") == 0 and protected.get("ReturnedIoErr") == 214, "Requester and DOS result")
    packets = [e for e in report["copyClosePacketEvents"] if e["PacketType"] == 9]
    require(len(packets) >= 2, "Protected handler packet pair")
    request, reply = packets[:2]
    require(request["Name"] == reply["Name"] == "PutMsg" and request["Task"] == protected["Task"] and
        request["Packet"] == reply["Packet"] and request["Message"] == reply["Message"] and
        request["Destination"] == reply["Port"] and request["Port"] == reply["Destination"] and
        request["Task"] != reply["Task"] and reply["Result1"] == 0 and reply["Result2"] == 214,
        "Protected handler reply identity/error")
    require(cases[0]["result"] == 20 and cases[0]["ioError"] == 214 and
        bytes.fromhex(cases[0]["outputHex"]) == b"disk is write-protected\n", "Final protected result/diagnostic")
    return {"provider": report["scriptedKeyboardProvider"], "inputEvents": expected,
        "requesterResult": 0, "handlerError": 214, "commandResult": 20, "commandIoErr": 214}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--media-directory", type=Path, help="Override the scenario media directory without copying receipts")
    parser.add_argument("--reference-trx", type=Path)
    parser.add_argument("--candidate-trx", type=Path)
    parser.add_argument("--scenario", choices=["arguments", "help-handler", "help"], required=True)
    parser.add_argument("--test-assembly", type=Path, required=True)
    parser.add_argument("--qualification", type=Path, required=True)
    parser.add_argument("--output-directory", type=Path, required=True)
    args = parser.parse_args()
    args.output_directory.mkdir(parents=True, exist_ok=False)
    observations, inputs = [], {}
    for role in ["reference", "candidate"]:
        trx = getattr(args, role + "_trx") or args.directory / (args.scenario + "-" + role + ".trx")
        media_path = (args.media_directory or args.directory / (args.scenario + "-media")) / (role + "-media.json")
        media = json.loads(media_path.read_text(encoding="utf-8"))
        report = read_trace(trx)
        require(report["testAssemblySha256"].lower() == digest(args.test_assembly) and
            report["emulatorAssemblySha256"].lower() == digest(args.test_assembly.parent / "CopperMod.Amiga.Emulator.dll"), "Executor identity")
        require(report["fixtureReceiptSha256"].lower() == digest(media_path) and
            media["native_qualification_sha256"] == digest(args.qualification), "Receipt identity")
        binary = Path(media["replacements"][0]["local_file"])
        expected = "163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff" if role == "reference" else one([
            a for a in json.loads(args.qualification.read_text(encoding="utf-8-sig"))["artifacts"] if a["cpu"] == "68000"], "CPU")["sha256"]
        require(digest(binary) == expected, "Command identity")
        observations.append(observe(report, media, role == "candidate", args.scenario))
        path = args.output_directory / (role + "-observations.json")
        path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        inputs[role] = {"trxSha256": digest(trx), "receiptSha256": digest(media_path), "binarySha256": digest(binary), "observationsSha256": digest(path)}
    require(observations[0] == observations[1], "Original and replacement observations differ")
    result = {"status": "passed", "suite": "Relabel-extended-original-DOS-boot", "scenario": args.scenario,
        "inputs": inputs, "observations": observations[0], "verifierSha256": digest(__file__),
        "mediaVerifierSha256": digest(Path(__file__).with_name("verify_relabel_boot.py")), "shippingOrPureApproval": False,
        "limits": "68000 original DOS/Shell under the recorded host Exec/device takeover. Sequential forced resident use, not full PURE or original classification. Pointer and extra allocation differences normalized with ownership checked separately."}
    path = args.output_directory / "comparison.json"
    path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(path)


if __name__ == "__main__": main()
