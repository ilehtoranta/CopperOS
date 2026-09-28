"""Reject corrupted evidence for both resident replacement failure paths."""
import argparse
import copy
import json
from pathlib import Path
from verify_relabel_boot import digest, require
from verify_relabel_failed_replacement import observe
from verify_relabel_replacement_controls import event, first_call_event, corrupt_payload
from verify_relabel_concurrent_controls import corrupt_output


def attempt(report):
    return event(report, "RunCommand", lambda e: bytes.fromhex(e.get("RunArgumentHex", "")).endswith(b" REPLACE PURE\n"))


def during(report, name):
    run = attempt(report)
    return event(report, name, lambda e: run["EntryRetireCycle"] < e["EntryRetireCycle"] < run["ReturnRetireCycle"])


def retained_lookup(report):
    first, last = attempt(report)["ReturnRetireCycle"], report["copyInvocationOwnership"][1]["EntryCycle"]
    return event(report, "FindSegment", lambda e: first < e["EntryRetireCycle"] < last)


def illegal_unload(report):
    run = attempt(report)
    report["observedDosCalls"].append({"Name": "UnLoadSeg", "Task": run["Task"],
        "EntryRetireCycle": run["EntryRetireCycle"] + 1, "D1": report["copySegmentGenerations"][0]["Segment"]})


def early_release(report):
    cycle = report["copyInvocationOwnership"][1]["EntryCycle"] + 1
    event(report, "SegmentFreeMem")["EntryRetireCycle"] = report["copySegmentGenerations"][0]["FreeEntryCycle"] = cycle


def extra_call(report):
    row = report["copyInvocationOwnership"][1]
    call = copy.deepcopy(event(report, "RunCommand", lambda e: e["EntryRetireCycle"] == row["EntryCycle"]))
    call["EntryRetireCycle"] += 1
    report["observedDosCalls"].append(call)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Use fresh controls output")
    rejections, bindings = [], {}
    for scenario in ["missing", "invalid"]:
        reports, media = {}, {}
        directory = args.directory / scenario
        for role in ["reference", "candidate"]:
            path, media_path = directory / "qualified" / (role + "-observations.json"), directory / "media" / (role + "-media.json")
            reports[role], media[role] = json.loads(path.read_text(encoding="utf-8")), json.loads(media_path.read_text(encoding="utf-8"))
            bindings[scenario + "-" + role] = {"observationsSha256": digest(path), "mediaSha256": digest(media_path)}
        expected = observe(reports["reference"], media["reference"], False)
        require(observe(reports["candidate"], media["candidate"], True) == expected, "Positive failed replacement pair")
        controls = [
            ("wrong-scenario-mode", lambda r: r.update(requireFailedResidentReplacement=False)),
            ("replacement-succeeds", lambda r: attempt(r).update(ReturnedD0=0)),
            ("active-refusal-level-substituted", lambda r: attempt(r).update(ReturnedD0=10)),
            ("raw-loader-error-propagated", lambda r: attempt(r).update(ReturnedIoErr=212)),
            ("wrong-fault-error", lambda r: during(r, "PrintFault").update(D1=212)),
            ("missing-helper-parser-release", lambda r: r["observedDosCalls"].remove(during(r, "FreeArgs"))),
            ("missing-retained-node-lookup", lambda r: r["observedDosCalls"].remove(retained_lookup(r))),
            ("registry-node-changed", lambda r: retained_lookup(r).update(ReturnedD0=1)),
            ("registry-segment-changed", lambda r: retained_lookup(r).update(FoundSegmentList=1)),
            ("wrong-idle-count", lambda r: retained_lookup(r).update(FoundUseCount=2)),
            ("old-segment-unloaded-during-failure", illegal_unload),
            ("unexpected-second-generation", lambda r: r["copySegmentGenerations"].append(copy.deepcopy(r["copySegmentGenerations"][0]))),
            ("later-call-uses-different-segment", lambda r: r["copyInvocationOwnership"][1].update(Segment=1)),
            ("recovery-call-missing", lambda r: r["copyInvocationOwnership"].pop()),
            ("extra-segment-call", extra_call),
            ("final-segment-free-missing", lambda r: r["observedDosCalls"].remove(event(r, "SegmentFreeMem"))),
            ("segment-free-by-other-task", lambda r: event(r, "SegmentFreeMem").update(Task=0)),
            ("segment-free-before-recovery", early_release),
            ("retained-code-write", lambda r: r["copyCpuImageWrites"].append({"Generation": 1})),
            ("image-changed-at-final-free", lambda r: r["copySegmentGenerations"][0].update(ImageUnchangedAtFree=False)),
            ("diagnostic-byte-changed", lambda r: corrupt_output(r, "rr-replace")),
            ("retained-command-payload-changed", corrupt_payload),
            ("command-parser-error-changed", lambda r: first_call_event(r, "FreeArgs").update(ReturnedIoErr=0)),
            ("command-dos-lease-owner-changed", lambda r: first_call_event(r, "CopyCloseLibrary").update(Task=0)),
        ]
        if scenario == "missing":
            controls += [
                ("missing-source-lock-succeeds", lambda r: during(r, "Lock").update(ReturnedD0=1)),
                ("missing-source-error-cleared", lambda r: during(r, "Lock").update(ReturnedIoErr=0)),
            ]
        else:
            controls += [
                ("invalid-source-load-succeeds", lambda r: during(r, "LoadSeg").update(ReturnedD0=1)),
                ("invalid-source-error-changed", lambda r: during(r, "LoadSeg").update(ReturnedIoErr=205)),
                ("source-lock-release-missing", lambda r: r["observedDosCalls"].remove(during(r, "UnLock"))),
                ("wrong-source-lock-released", lambda r: during(r, "UnLock").update(D1=1)),
                ("invalid-source-metadata-changed", lambda r: during(r, "Examine").update(FileSize=0)),
                ("parser-cleanup-error-changed", lambda r: during(r, "FreeArgs").update(ReturnedIoErr=205)),
            ]
        for name, mutate in controls:
            report = copy.deepcopy(reports["candidate"])
            mutate(report)
            try:
                require(observe(report, media["candidate"], True) == expected, "Differential mismatch")
            except (ValueError, KeyError, StopIteration) as error:
                rejections.append({"scenario": scenario, "control": name, "rejected": True, "reason": str(error)})
            else:
                raise ValueError("Corruption accepted: " + scenario + "/" + name)
    result = {"status": "passed", "positivePairs": 2, "inputs": bindings, "rejections": rejections,
        "controlsSha256": digest(__file__), "verifierSha256": digest(Path(__file__).with_name("verify_relabel_failed_replacement.py")),
        "sharedVerifierSha256": digest(Path(__file__).with_name("verify_relabel_replacement_boot.py")),
        "mediaVerifierSha256": digest(Path(__file__).with_name("verify_relabel_boot.py"))}
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(args.output, len(rejections), "corruptions rejected")


if __name__ == "__main__":
    main()
