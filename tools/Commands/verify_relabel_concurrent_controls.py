"""Challenge concurrent Relabel evidence without editing captures or media."""
import argparse
import copy
import json
from pathlib import Path
from verify_relabel_boot import digest, one, require
from verify_relabel_concurrent_boot import observe


def invocation(report, destination):
    mutation = one([e for e in report["observedDosCalls"] if e["Name"] == "Relabel" and e.get("DestinationText") == destination], "Mutation")
    row = one([r for r in report["copyInvocationOwnership"] if r["Task"] == mutation["Task"] and
        r["EntryCycle"] < mutation["EntryRetireCycle"] < r["ReturnCycle"]], "Owner")
    return row, [e for e in report["observedDosCalls"] if e["Task"] == row["Task"] and
        row["EntryCycle"] < e["EntryRetireCycle"] < row["ReturnCycle"]]


def alias_parser(report):
    _, blocked = invocation(report, "MustNotChange")
    _, other = invocation(report, "ConcurrentVolume")
    pointer = next(e for e in blocked if e["Name"] == "ReadArgs")["ReturnedD0"]
    next(e for e in other if e["Name"] == "ReadArgs")["ReturnedD0"] = pointer
    next(e for e in other if e["Name"] == "FreeArgs")["D1"] = pointer


def alias_storage(report):
    _, blocked = invocation(report, "MustNotChange")
    _, other = invocation(report, "ConcurrentVolume")
    pointer = next(e for e in blocked if e["Name"] == "CopyAllocMem")["ReturnedD0"]
    allocation = next(e for e in other if e["Name"] == "CopyAllocMem")
    released = next(e for e in other if e["Name"] == "CopyFreeMem" and e["A1"] == allocation["ReturnedD0"])
    allocation["ReturnedD0"] = released["A1"] = pointer
    next(e for e in other if e["Name"] == "ReadArgs")["D2"] = pointer


def wrong_cleanup_owner(report):
    blocked, _ = invocation(report, "MustNotChange")
    _, other = invocation(report, "ConcurrentVolume")
    next(e for e in other if e["Name"] == "CopyFreeMem")["Task"] = blocked["Task"]


def corrupt_output(report, basename="rc01"):
    events = report["observedDosCalls"]
    opened = one([e for e in events if e["Name"] == "Open" and e.get("Text") == basename], "Diagnostic open")
    read = next(e for e in events[events.index(opened)+1:] if e["Name"] == "FGetC" and
        e["Task"] == opened["Task"] and e["D1"] == opened["ReturnedD0"])
    read["ReturnedD0"] ^= 1


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ["observations-directory", "media-directory", "output"]:
        parser.add_argument("--" + name, type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Use a fresh controls output")
    reports, media, bindings = {}, {}, {}
    for role in ["reference", "candidate"]:
        path = args.observations_directory / (role + "-observations.json")
        reports[role] = json.loads(path.read_text(encoding="utf-8"))
        media_path = args.media_directory / (role + "-media.json")
        media[role] = json.loads(media_path.read_text(encoding="utf-8"))
        bindings[role] = {"observationsSha256": digest(path), "mediaSha256": digest(media_path)}
    expected = observe(reports["reference"], media["reference"], False)
    require(observe(reports["candidate"], media["candidate"], True) == expected, "Positive concurrent pair")
    controls = [
        ("only-one-active-caller", lambda r: r.update(copyMaximumActiveInvocations=1)),
        ("second-loaded-segment", lambda r: r["copyInvocationOwnership"][1].update(Segment=r["copyInvocationOwnership"][0]["Segment"]+1)),
        ("unbound-invocation-time", lambda r: r["copyInvocationOwnership"][0].update(EntryCycle=r["copyInvocationOwnership"][0]["EntryCycle"]+1)),
        ("shared-rdargs", alias_parser),
        ("overlapping-live-storage", alias_storage),
        ("other-task-frees-storage", wrong_cleanup_owner),
        ("missing-segment-release", lambda r: r["observedDosCalls"].remove(next(e for e in r["observedDosCalls"] if e["Name"] == "SegmentFreeMem"))),
        ("early-segment-release", lambda r: next(e for e in r["observedDosCalls"] if e["Name"] == "SegmentFreeMem").update(EntryRetireCycle=0)),
        ("shared-image-write", lambda r: r["copyCpuImageWrites"].append({"control": "write"})),
        ("cancel-before-overlap", lambda r: r["scriptedKeyboardEvents"][0].update(OverlapObserved=False)),
        ("wrong-requester-result", lambda r: next(e for e in r["observedDosCalls"] if e["Name"] == "IntuitionEasyRequestArgs").update(ReturnedD0=1)),
        ("changed-diagnostic-byte", corrupt_output),
    ]
    if media["candidate"]["scenario"] == "active-remove":
        def refused(report):
            return next(e for e in report["observedDosCalls"] if e["Name"] == "RemSegment" and e.get("ReturnedD0") == 0)

        def resident_runs(report):
            return [e for e in report["observedDosCalls"] if e["Name"] == "RunCommand" and
                bytes.fromhex(e.get("RunArgumentHex", "")) == b"Ed REMOVE\n"]

        def missing_retained_lookup(report):
            attempt = resident_runs(report)[0]
            overlap, _ = invocation(report, "ConcurrentVolume")
            entry = next(e for e in report["observedDosCalls"] if e["Name"] == "FindSegment" and
                e.get("FoundSegmentList") == overlap["Segment"] and
                attempt["ReturnRetireCycle"] < e["EntryRetireCycle"] < overlap["EntryCycle"])
            report["observedDosCalls"].remove(entry)

        controls += [
            ("missing-active-removal-attempt", lambda r: r["observedDosCalls"].remove(refused(r))),
            ("active-removal-accepted", lambda r: refused(r).update(ReturnedD0=0xffffffff)),
            ("wrong-active-use-count", lambda r: refused(r).update(RemovedUseCount=0)),
            ("different-registry-node", lambda r: refused(r).update(D1=0)),
            ("missing-retained-registry-lookup", missing_retained_lookup),
            ("wrong-resident-warning-level", lambda r: resident_runs(r)[0].update(ReturnedD0=10)),
            ("cleared-retry-ambient-error", lambda r: resident_runs(r)[1].update(ReturnedIoErr=0)),
            ("changed-in-use-diagnostic", lambda r: corrupt_output(r, "rc-remove")),
        ]
    if media["candidate"]["scenario"] == "active-replace":
        def replacement_run(report):
            return next(e for e in report["observedDosCalls"] if e["Name"] == "RunCommand" and
                bytes.fromhex(e.get("RunArgumentHex", "")) == b"Ed C:Ed REPLACE PURE\n")

        def replacement_lookup(report, retained=False):
            attempt = replacement_run(report)
            overlap, _ = invocation(report, "ConcurrentVolume")
            first = attempt["ReturnRetireCycle"] if retained else attempt["EntryRetireCycle"]
            last = overlap["EntryCycle"] if retained else attempt["ReturnRetireCycle"]
            return next(e for e in report["observedDosCalls"] if e["Name"] == "FindSegment" and
                e.get("FoundSegmentList") == overlap["Segment"] and first < e["EntryRetireCycle"] < last)

        def unexpected_load(report):
            attempt = replacement_run(report)
            event = copy.deepcopy(next(e for e in report["observedDosCalls"] if e["Name"] == "LoadSeg"))
            event.update(Text="OtherImage", Task=attempt["Task"], EntryRetireCycle=attempt["EntryRetireCycle"]+1,
                ReturnRetireCycle=attempt["EntryRetireCycle"]+2)
            report["observedDosCalls"].append(event)

        def unexpected_removal(report):
            attempt = replacement_run(report)
            event = copy.deepcopy(next(e for e in report["observedDosCalls"] if e["Name"] == "RemSegment"))
            event.update(ReturnedD0=0, EntryRetireCycle=attempt["EntryRetireCycle"]+1,
                ReturnRetireCycle=attempt["EntryRetireCycle"]+2)
            report["observedDosCalls"].append(event)

        controls += [
            ("missing-replace-option", lambda r: replacement_run(r).update(RunArgumentHex=b"Ed C:Ed PURE\n".hex())),
            ("removal-warning-substituted-for-replace-error", lambda r: replacement_run(r).update(ReturnedD0=5)),
            ("replacement-load-before-refusal", unexpected_load),
            ("registry-removal-before-replace-refusal", unexpected_removal),
            ("wrong-replacement-use-count", lambda r: replacement_lookup(r).update(FoundUseCount=1)),
            ("different-replacement-registry-node", lambda r: replacement_lookup(r).update(ReturnedD0=0)),
            ("missing-post-replace-lookup", lambda r: r["observedDosCalls"].remove(replacement_lookup(r, True))),
            ("cleared-replacement-error", lambda r: replacement_run(r).update(ReturnedIoErr=0)),
            ("changed-replacement-diagnostic", lambda r: corrupt_output(r, "rc-replace")),
        ]
    results = []
    for name, corrupt in controls:
        modified = copy.deepcopy(reports["candidate"])
        corrupt(modified)
        try:
            require(observe(modified, media["candidate"], True) == expected, "Differential mismatch")
        except ValueError as error:
            results.append({"control": name, "rejected": True, "reason": str(error)})
        else:
            raise ValueError("Corruption accepted: " + name)
    result = {"status": "passed", "positivePairs": 1, "inputs": bindings, "rejections": results,
        "controlsSha256": digest(__file__), "verifierSha256": digest(Path(__file__).with_name("verify_relabel_concurrent_boot.py"))}
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(args.output)


if __name__ == "__main__":
    main()
