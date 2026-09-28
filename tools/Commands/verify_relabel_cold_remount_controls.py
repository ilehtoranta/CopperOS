"""Reject stale state, mutated disks, missing reads and hidden Relabel execution."""
import argparse
import copy
import json
from pathlib import Path
from verify_relabel_boot import digest, require
from verify_relabel_cold_remount import observe


def guard_close(report):
    return next(e for e in report["observedDosCalls"] if "IndependentReadbackBytes" in e)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Fresh controls output")
    reports, media, bindings = {}, {}, {}
    for role in ["reference", "candidate"]:
        r, m = args.directory / "qualified" / (role + "-observations.json"), args.directory / "media" / (role + "-media.json")
        reports[role], media[role] = json.loads(r.read_text(encoding="utf-8")), json.loads(m.read_text(encoding="utf-8"))
        bindings[role] = {"observationsSha256": digest(r), "mediaSha256": digest(m)}
    reference = observe(reports["reference"], media["reference"], False)
    require(observe(reports["candidate"], media["candidate"], True) == reference, "Positive cold-remount pair")
    controls = [
        ("not-cold-remount", lambda r: r.update(dataDiskColdRemount=False)),
        ("writable-data-drive", lambda r: r.update(dataDiskWriteProtected=False)),
        ("mutated-disk", lambda r: r.update(dataDiskFinalSha256="0" * 64)),
        ("unbound-disk-input", lambda r: r.update(dataDiskReceiptSha256="0" * 64)),
        ("hidden-relabel-call", lambda r: r["observedDosCalls"].append({"Name": "Relabel"})),
        ("copied-guest-state", lambda r: r.update(cpuStatePatchedAfterReset=True)),
        ("missing-volume-lookup", lambda r: next(e for e in r["observedDosCalls"] if e["Name"] == "MatchFirst" and e.get("Text") == "SavedDisk:proof").update(Text="Wrong:proof")),
        ("wrong-guard-bytes", lambda r: guard_close(r).update(IndependentReadbackSha256="0" * 64)),
        ("short-guard-read", lambda r: guard_close(r).update(IndependentReadbackBytes=16)),
        ("failed-guard-close", lambda r: guard_close(r).update(ReturnedD0=0)),
        ("guard-not-finished", lambda r: guard_close(r).update(ReturnRetireCycle=0xffffffffffffffff)),
        ("unresolved-requester", lambda r: r["observedDosCalls"].append({"Name": "IntuitionEasyRequestArgs"})),
    ]
    results = []
    for name, change in controls:
        report = copy.deepcopy(reports["candidate"])
        change(report)
        try:
            value = observe(report, media["candidate"], True)
            require(value == reference, "Original/replacement remount mismatch")
        except ValueError as error:
            results.append({"control": name, "rejected": True, "reason": str(error)})
        else:
            raise ValueError("Corruption accepted: " + name)
    args.output.write_text(json.dumps({"status": "passed", "positivePairs": 1, "inputs": bindings,
        "rejections": results, "verifierSha256": digest(Path(__file__).with_name("verify_relabel_cold_remount.py")),
        "controlsSha256": digest(__file__), "shippingOrPureApproval": False}, indent=2) + "\n", encoding="utf-8")
    print(args.output)


if __name__ == "__main__":
    main()
