"""Reject missing releases, false allocator evidence and wrong owner boundaries."""
import argparse
import copy
import json
from pathlib import Path
from verify_relabel_boot import digest, require
from verify_relabel_caller_memory import observe


def retired(r):
    return r["copyCallerRetirements"][0]


def change_free(r, key, value):
    item = retired(r)["OwnedMemoryFreeCalls"][0]
    event = next(e for e in r["observedDosCalls"] if e["Name"] == "CallerOwnedFreeMem" and e["EntryRetireCycle"] == item["EntryRetireCycle"])
    item[key] = event[key] = value


def remove_free(r):
    item = retired(r)["OwnedMemoryFreeCalls"].pop(0)
    r["observedDosCalls"].remove(next(e for e in r["observedDosCalls"] if e["Name"] == "CallerOwnedFreeMem" and e["EntryRetireCycle"] == item["EntryRetireCycle"]))


def hide_free_chunk(r):
    item = retired(r)["OwnedMemoryFreeCalls"][0]
    headers = copy.deepcopy(item["FreeMemoryAfterReturn"])
    header = next(h for h in headers if h["Lower"] <= item["A1"] < h["Upper"])
    chunk = next(c for c in header["Chunks"] if c["Address"] <= item["A1"] < c["Address"] + c["Bytes"])
    header["Chunks"].remove(chunk)
    header["FreeBytes"] -= chunk["Bytes"]
    change_free(r, "FreeMemoryAfterReturn", headers)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--directory", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    args = p.parse_args()
    require(not args.output.exists(), "Fresh controls output")
    reports, media, bindings = {}, {}, {}
    for role in ["reference", "candidate"]:
        path, m = args.directory / "qualified" / (role + "-observations.json"), args.directory / "media" / (role + "-media.json")
        reports[role], media[role] = json.loads(path.read_text(encoding="utf-8")), json.loads(m.read_text(encoding="utf-8"))
        bindings[role] = {"observationsSha256": digest(path), "mediaSha256": digest(m)}
    expected = observe(reports["reference"], media["reference"], False)
    require(observe(reports["candidate"], media["candidate"], True) == expected, "Positive memory pair")
    controls = [
        ("missing-owned-span", lambda r: retired(r)["OwnedMemoryAtRemoval"]["Spans"].pop()),
        ("wrong-memory-owner", lambda r: retired(r)["OwnedMemoryAtRemoval"].update(Task=1)),
        ("incorrect-rounded-span", lambda r: retired(r)["OwnedMemoryAtRemoval"]["Spans"][0].update(RoundedBytes=32)),
        ("process-stack-outside-owned-span", lambda r: retired(r)["OwnedMemoryAtRemoval"].update(StackUpper=0xffffffff)),
        ("missing-owned-free", remove_free),
        ("wrong-free-owner", lambda r: change_free(r, "OwnedTask", 1)),
        ("wrong-free-request-size", lambda r: change_free(r, "D0", 1)),
        ("free-before-removal", lambda r: change_free(r, "EntryRetireCycle", 1)),
        ("free-not-complete-before-switch", lambda r: change_free(r, "ReturnRetireCycle", retired(r)["FirstOtherTaskCycle"])),
        ("no-actual-free-chunk", hide_free_chunk),
        ("final-snapshot-substituted-for-free-return", lambda r: change_free(r, "FreeMemoryAfterReturn", copy.deepcopy(r["callerFreeMemoryAtBound"]))),
        ("allocator-free-total-corrupted", lambda r: change_free(r, "FreeMemoryAfterReturn", [dict(h, FreeBytes=h["FreeBytes"]+8) for h in retired(r)["OwnedMemoryFreeCalls"][0]["FreeMemoryAfterReturn"]])),
        ("host-remtask-substituted-for-original", lambda r: retired(r).update(RemTaskHostGateway=True)),
        ("wrong-freemem-provider", lambda r: change_free(r, "FreeMemHostGateway", False)),
        ("invented-supervisor-frame", lambda r: change_free(r, "StatusRegister", 0x2000)),
        ("host-owner-still-deferred", lambda r: r["callerDeferredTasksAtBound"].append(retired(r)["TargetTask"])),
        ("observer-self-qualification", lambda r: r.update(callerMemoryReapingQualified=True)),
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
        "controlsSha256": digest(__file__), "verifierSha256": digest(Path(__file__).with_name("verify_relabel_caller_memory.py"))}
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(args.output, len(rejections), "corruptions rejected")


if __name__ == "__main__":
    main()
