"""Challenge idle replacement evidence without changing captured runs or media."""
import argparse
import copy
import json
from pathlib import Path
from verify_relabel_boot import digest, one, require
from verify_relabel_replacement_boot import observe


def event(report, name, predicate=lambda e: True):
    return one([e for e in report["observedDosCalls"] if e["Name"] == name and predicate(e)], name)


def helper(report):
    return event(report, "RunCommand", lambda e: bytes.fromhex(e.get("RunArgumentHex", "")) == b"Ed C:Ed REPLACE PURE\n")


def old_free(report):
    return event(report, "SegmentFreeMem", lambda e: e.get("Generation") == 1)


def post_lookup(report):
    first, last = helper(report)["ReturnRetireCycle"], report["copyInvocationOwnership"][1]["EntryCycle"]
    return event(report, "FindSegment", lambda e: first < e["EntryRetireCycle"] < last)


def first_call_event(report, name):
    row = report["copyInvocationOwnership"][0]
    return event(report, name, lambda e: row["EntryCycle"] < e["EntryRetireCycle"] < row["ReturnCycle"])


def corrupt_payload(report):
    events = report["observedDosCalls"]
    match = event(report, "MatchFirst", lambda e: e.get("Text") == "AfterReplace:relabel-proof")
    start = events.index(match)
    opened = next(e for e in events[start:] if e["Name"] == "Open" and e.get("Text") == "relabel-proof")
    read = next(e for e in events[events.index(opened):] if e["Name"] == "FGetC" and e["D1"] == opened["ReturnedD0"])
    read["ReturnedD0"] ^= 1


def duplicate_call(report):
    row = report["copyInvocationOwnership"][1]
    extra = copy.deepcopy(event(report, "RunCommand", lambda e: e["EntryRetireCycle"] == row["EntryCycle"]))
    extra["EntryRetireCycle"] += 1
    report["observedDosCalls"].append(extra)


def early_free(report):
    cycle = report["copyInvocationOwnership"][0]["EntryCycle"] + 1
    old_free(report)["EntryRetireCycle"] = report["copySegmentGenerations"][0]["FreeEntryCycle"] = cycle


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ["observations-directory", "media-directory", "output"]:
        parser.add_argument("--" + name, type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Use a fresh controls output")
    reports, media, bindings = {}, {}, {}
    for role in ["reference", "candidate"]:
        path, media_path = args.observations_directory / (role + "-observations.json"), args.media_directory / (role + "-media.json")
        reports[role], media[role] = json.loads(path.read_text(encoding="utf-8")), json.loads(media_path.read_text(encoding="utf-8"))
        bindings[role] = {"observationsSha256": digest(path), "mediaSha256": digest(media_path)}
    expected = observe(reports["reference"], media["reference"], False)
    require(observe(reports["candidate"], media["candidate"], True) == expected, "Positive replacement pair")
    controls = [
        ("missing-old-segment-release", lambda r: r["observedDosCalls"].remove(old_free(r))),
        ("missing-old-release-return", lambda r: old_free(r).pop("ReturnRetireCycle")),
        ("old-image-write-before-release", lambda r: r["copyCpuImageWrites"].append({"Generation": 1})),
        ("old-image-changed-at-free", lambda r: r["copySegmentGenerations"][0].update(ImageUnchangedAtFree=False)),
        ("new-image-hash-changed", lambda r: r["copySegmentGenerations"][1].update(ImageSha256="0" * 64)),
        ("old-image-hash-changed", lambda r: r["copySegmentGenerations"][0].update(ImageSha256="0" * 64)),
        ("wrong-loaded-segment", lambda r: event(r, "LoadSeg", lambda e: e.get("Generation") == 2).update(ReturnedD0=1)),
        ("retired-segment-invoked", lambda r: r["copyInvocationOwnership"][1].update(Segment=r["copySegmentGenerations"][0]["Segment"])),
        ("second-generation-reuse-missing", lambda r: r["copyInvocationOwnership"].pop()),
        ("unaccounted-generation-call", duplicate_call),
        ("release-during-active-call", early_free),
        ("release-by-another-task", lambda r: old_free(r).update(Task=0)),
        ("wrong-release-allocation-size", lambda r: old_free(r).update(D0=4)),
        ("registry-node-replaced", lambda r: post_lookup(r).update(ReturnedD0=1)),
        ("registry-keeps-old-segment", lambda r: post_lookup(r).update(FoundSegmentList=r["copySegmentGenerations"][0]["Segment"])),
        ("wrong-registry-use-count", lambda r: post_lookup(r).update(FoundUseCount=2)),
        ("missing-replace-option", lambda r: helper(r).update(RunArgumentHex=b"Ed C:Ed PURE\n".hex())),
        ("refusal-substituted-for-success", lambda r: helper(r).update(ReturnedD0=10)),
        ("replacement-error-not-cleared", lambda r: helper(r).update(ReturnedIoErr=202)),
        ("missing-parser-release", lambda r: r["observedDosCalls"].remove(first_call_event(r, "FreeArgs"))),
        ("wrong-dos-close-owner", lambda r: first_call_event(r, "CopyCloseLibrary").update(Task=0)),
        ("ambient-parser-error-changed", lambda r: first_call_event(r, "FreeArgs").update(ReturnedIoErr=0)),
        ("payload-byte-changed", corrupt_payload),
        ("false-unfinished-pass", lambda r: r.update(copyActiveInvocations=1)),
    ]
    rejected = []
    for name, mutate in controls:
        report = copy.deepcopy(reports["candidate"])
        mutate(report)
        try:
            require(observe(report, media["candidate"], True) == expected, "Original/replacement mismatch")
        except (ValueError, KeyError, StopIteration) as error:
            rejected.append({"control": name, "rejected": True, "reason": str(error)})
        else:
            raise ValueError("Corruption accepted: " + name)
    result = {"status": "passed", "positivePairs": 1, "inputs": bindings, "rejections": rejected,
        "controlsSha256": digest(__file__), "verifierSha256": digest(Path(__file__).with_name("verify_relabel_replacement_boot.py")),
        "mediaVerifierSha256": digest(Path(__file__).with_name("verify_relabel_boot.py"))}
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(args.output, len(rejected), "corruptions rejected")


if __name__ == "__main__":
    main()
