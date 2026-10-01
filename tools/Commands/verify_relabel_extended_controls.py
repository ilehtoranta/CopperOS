"""Reject incomplete or corrupted extended Relabel boot evidence."""
import argparse
import copy
import json
from pathlib import Path
from verify_relabel_boot import digest, read_trace, require
from verify_relabel_extended_boot import observe


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--cancel-directory", type=Path, help="Qualified protected-disk observations to challenge")
    args = parser.parse_args()
    require(not args.output.exists(), "Fresh controls output required")
    results, bindings = [], {}
    for scenario in ["arguments", "help"]:
        reports, media = {}, {}
        for role in ["reference", "candidate"]:
            report_path = args.directory / ("final-" + scenario) / (role + "-observations.json")
            reports[role] = json.loads(report_path.read_text(encoding="utf-8"))
            media[role] = json.loads((args.directory / (scenario + "-media") / (role + "-media.json")).read_text(encoding="utf-8"))
            bindings[scenario + "-" + role] = digest(report_path)
        expected = observe(reports["reference"], media["reference"], False, scenario)
        require(observe(reports["candidate"], media["candidate"], True, scenario) == expected, "Positive pair")

        def command_event(report, name):
            events = report["observedDosCalls"]
            start = next(i for i, e in enumerate(events) if e["Name"] == "CopyOpenLibrary")
            return next(e for e in events[start:] if e["Name"] == name)

        def remove_payload_open(report):
            events = report["observedDosCalls"]
            item = next(e for e in events if e["Name"] == "Open" and e.get("Text") == "relabel-proof")
            events.remove(item)

        def corrupt_readback(report):
            events = report["observedDosCalls"]
            prefix = "ra01" if scenario == "arguments" else "rh03"
            opened = next(e for e in events if e["Name"] == "Open" and e.get("Text") == prefix)
            read = next(e for e in events[events.index(opened)+1:] if e["Name"] == "FGetC" and
                e["D1"] == opened["ReturnedD0"] and e["Task"] == opened["Task"])
            read["ReturnedD0"] = 0

        controls = [
            ("wrong-command-result", lambda r: r["copyInvocationOwnership"][0].update(ReturnCode=10)),
            ("wrong-ambient-error", lambda r: r["copyInvocationOwnership"][0].update(ReturnIoErr=999)),
            ("wrong-handler-name", lambda r: command_event(r, "Relabel").update(DestinationText="Changed")),
            ("missing-payload-open", remove_payload_open),
            ("changed-empty-or-prompt-output", corrupt_readback),
        ]
        for name, corrupt in controls:
            modified = copy.deepcopy(reports["candidate"])
            corrupt(modified)
            try:
                require(observe(modified, media["candidate"], True, scenario) == expected, "Differential mismatch")
            except ValueError as error:
                results.append({"scenario": scenario, "control": name, "rejected": True, "reason": str(error)})
            else:
                raise ValueError("Corruption accepted: " + name)
    for role in ["reference", "candidate"]:
        report_path = args.directory / ("help-handler-" + role + ".trx")
        report = read_trace(report_path)
        media = json.loads((args.directory / "help-handler-media" / (role + "-media.json")).read_text(encoding="utf-8"))
        try:
            observe(report, media, role == "candidate", "help-handler")
        except ValueError as error:
            require(str(error) == "No completed command invocation", "Unexpected incomplete-case rejection")
            results.append({"scenario": "help-handler", "control": role + "-unfinished-DOS-call",
                "rejected": True, "reason": str(error), "trxSha256": digest(report_path)})
        else:
            raise ValueError("Incomplete disk call accepted")
    if args.cancel_directory:
        cancel_reports = {}
        cancel_media = {}
        for role in ["reference", "candidate"]:
            path = args.cancel_directory / (role + "-observations.json")
            cancel_reports[role] = json.loads(path.read_text(encoding="utf-8"))
            cancel_media[role] = json.loads((args.directory / "help-handler-media" / (role + "-media.json")).read_text(encoding="utf-8"))
            bindings["cancel-" + role] = digest(path)
        expected = observe(cancel_reports["reference"], cancel_media["reference"], False, "help-handler")
        require(observe(cancel_reports["candidate"], cancel_media["candidate"], True, "help-handler") == expected, "Positive cancel pair")
        controls = [
            ("rejected-key", lambda r: r["scriptedKeyboardEvents"][1].update(Accepted=False)),
            ("wrong-key-modifier", lambda r: r["scriptedInputDeviceEvents"][1].update(Qualifier=0)),
            ("missing-native-delivery", lambda r: r["observedDosCalls"].remove(next(e for e in r["observedDosCalls"] if e["Name"] == "NativeInputHandler"))),
            ("wrong-requester-result", lambda r: next(e for e in r["observedDosCalls"] if e["Name"] == "IntuitionEasyRequestArgs").update(ReturnedD0=1)),
            ("requester-still-pending", lambda r: r.update(requesterStillPending=True)),
            ("wrong-protected-packet-error", lambda r: r["copyClosePacketEvents"][1].update(Result2=0)),
            ("wrong-protected-packet-identity", lambda r: r["copyClosePacketEvents"][1].update(Message=0)),
            ("wrong-command-error", lambda r: r["copyInvocationOwnership"][0].update(ReturnIoErr=0)),
        ]
        for name, corrupt in controls:
            modified = copy.deepcopy(cancel_reports["candidate"])
            corrupt(modified)
            try:
                require(observe(modified, cancel_media["candidate"], True, "help-handler") == expected, "Differential mismatch")
            except ValueError as error:
                results.append({"scenario": "help-handler", "control": name, "rejected": True, "reason": str(error)})
            else:
                raise ValueError("Cancellation corruption accepted: " + name)
    result = {"status": "passed", "positivePairs": 3 if args.cancel_directory else 2, "rejections": results, "inputs": bindings,
        "controlsSha256": digest(__file__), "verifierSha256": digest(Path(__file__).with_name("verify_relabel_extended_boot.py"))}
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(args.output)


if __name__ == "__main__": main()
