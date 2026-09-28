"""Record a protected-disk wait as incomplete evidence, never command success."""
import argparse
import json
from pathlib import Path
from verify_relabel_boot import digest, one, read_trace, require


def inspect(report):
    require(report.get("commandUnderTest") == "Relabel" and report.get("rootInfoReady") and
        report.get("diskBytesUnchanged") and report.get("diskWriteProtected"), "Boot/media prerequisite")
    require(not any(report.get(k) for k in ["failure", "boundedStop", "dosObservationOverflow",
        "returnObservationCollision", "copyClosePacketOverflow"]), "Incomplete observation transport")
    require(report.get("copyInvocationOwnership") == [] and report.get("copyActiveInvocations") == 1,
        "Expected one pending command, with no completed invocation")
    command = one([e for e in report["observedDosCalls"] if e["Name"] == "Relabel"], "Relabel call")
    require(command["Text"] == "DF0:" and command["DestinationText"] == "MustNotChange" and
        "ReturnedD0" not in command, "Protected Relabel call completed or changed")
    packets = [e for e in report["copyClosePacketEvents"] if e["PacketType"] == 9]
    require(len(packets) == 2, "Expected request and reply only")
    request, reply = packets
    require(request["Name"] == reply["Name"] == "PutMsg" and request["Task"] == command["Task"] and
        request["Packet"] == reply["Packet"] and request["Message"] == reply["Message"] and
        request["Destination"] == reply["Port"] and request["Port"] == reply["Destination"] and
        request["Task"] != reply["Task"] and reply["Result1"] == 0 and reply["Result2"] == 214,
        "RenameDisk reply identity/write-protection error")
    dialogs = [e for e in report["observedDosCalls"] if e["Name"] in ["IntuitionAutoRequest", "IntuitionEasyRequestArgs"]]
    pending_dialogs = [e for e in dialogs if "ReturnedD0" not in e]
    return {"status": "incomplete", "shippingOrPureApproval": False, "completedCommands": 0,
        "pendingCommandsAtBound": 1, "handlerReply": {"result": 0, "error": 214},
        "pendingRequesterCalls": [e["Name"] for e in pending_dialogs],
        "publicErrorReportCalls": sum(e["Name"] == "ErrorReport" for e in report["observedDosCalls"]),
        "limits": "The bounded test has terminated and disposed the guest; this is not a live wait handle. Handler error 214 is observed, but the command's final result, diagnostics and cleanup are not qualified."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ["trx", "media", "test-assembly", "output"]:
        parser.add_argument("--" + name, type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Use a fresh observation path")
    report = read_trace(args.trx)
    require(report["testAssemblySha256"].lower() == digest(args.test_assembly) and
        report["emulatorAssemblySha256"].lower() == digest(args.test_assembly.parent / "CopperMod.Amiga.Emulator.dll") and
        report["fixtureReceiptSha256"].lower() == digest(args.media), "Bound inputs changed")
    result = inspect(report)
    result.update(trxSha256=digest(args.trx), mediaReceiptSha256=digest(args.media),
        testAssemblySha256=digest(args.test_assembly), recorderSha256=digest(__file__))
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(args.output)


if __name__ == "__main__": main()
