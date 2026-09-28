"""Challenge terminal caller removal and retained resident reuse evidence."""
import argparse
import copy
import json
from pathlib import Path
from verify_relabel_boot import digest, require
from verify_relabel_caller_retirement import observe


def retired(report):
    return report["copyCallerRetirements"][0]


def reappear(report):
    report["snapshots"][-1]["waitTasks"]["tasks"].append({"address": retired(report)["TargetTask"]})


def post_exit_command(report):
    item = retired(report)
    report["observedDosCalls"].append({"Name": "RunCommand", "Task": item["TargetTask"], "D1": 1,
        "EntryRetireCycle": item["FirstOtherTaskCycle"] + 1})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Use fresh controls output")
    reports, media, bindings = {}, {}, {}
    for role in ["reference", "candidate"]:
        path, media_path = args.directory / "qualified" / (role + "-observations.json"), args.directory / "media" / (role + "-media.json")
        reports[role], media[role] = json.loads(path.read_text(encoding="utf-8")), json.loads(media_path.read_text(encoding="utf-8"))
        bindings[role] = {"observationsSha256": digest(path), "mediaSha256": digest(media_path)}
    expected = observe(reports["reference"], media["reference"], False)
    require(observe(reports["candidate"], media["candidate"], True) == expected, "Positive retirement pair")
    controls = [
        ("missing-retirement", lambda r: r.update(copyCallerRetirements=[])),
        ("wrong-retired-owner", lambda r: retired(r).update(TargetTask=1)),
        ("nonself-task-removal", lambda r: retired(r).update(ArgumentTask=1)),
        ("returned-to-retired-frame", lambda r: retired(r).update(ReturnedToCaller=True)),
        ("unbound-terminal-frame", lambda r: retired(r).update(ReturnSp=0)),
        ("active-command-at-removal", lambda r: retired(r).update(ActiveCommandCalls=1)),
        ("resident-image-already-freed", lambda r: retired(r).update(ImageLive=False)),
        ("use-count-not-released", lambda r: retired(r).update(RegistryUseCount=2)),
        ("registry-segment-changed", lambda r: retired(r).update(RegistrySegment=1)),
        ("different-registry-node", lambda r: retired(r).update(RegistryNode=1)),
        ("missing-dispatch", lambda r: retired(r).pop("FirstOtherTaskCycle")),
        ("dispatch-to-retired-task", lambda r: retired(r).update(FirstOtherTask=retired(r)["TargetTask"])),
        ("retirement-before-command-return", lambda r: retired(r).update(EntryCycle=1)),
        ("recovery-before-switch", lambda r: retired(r).update(FirstOtherTaskCycle=max(x["EntryCycle"] for x in r["copyInvocationOwnership"])+1)),
        ("still-ready-after-switch", lambda r: retired(r)["ReadyTasksAfterSwitch"]["tasks"].append({"address": retired(r)["TargetTask"]})),
        ("invalid-wait-list", lambda r: retired(r)["WaitTasksAfterSwitch"].update(terminatedAtTail=False)),
        ("retired-task-reappears", reappear),
        ("command-after-retirement", post_exit_command),
        ("missing-later-snapshots", lambda r: r.update(snapshots=[])),
        ("retired-task-current-in-last-snapshot", lambda r: r["snapshots"][-1].update(currentTask=retired(r)["TargetTask"])),
    ]
    rejections = []
    for name, mutate in controls:
        report = copy.deepcopy(reports["candidate"])
        mutate(report)
        try:
            require(observe(report, media["candidate"], True) == expected, "Differential mismatch")
        except (ValueError, KeyError, StopIteration) as error:
            rejections.append({"control": name, "rejected": True, "reason": str(error)})
        else:
            raise ValueError("Corruption accepted: " + name)
    result = {"status": "passed", "positivePairs": 1, "inputs": bindings, "rejections": rejections,
        "controlsSha256": digest(__file__), "verifierSha256": digest(Path(__file__).with_name("verify_relabel_caller_retirement.py")),
        "concurrentVerifierSha256": digest(Path(__file__).with_name("verify_relabel_concurrent_boot.py"))}
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(args.output, len(rejections), "corruptions rejected")


if __name__ == "__main__":
    main()
