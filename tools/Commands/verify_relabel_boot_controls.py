"""Reject corrupted Relabel boot evidence without modifying media or captures."""
import argparse
import copy
import json
from pathlib import Path
from verify_relabel_boot import digest, require, verify


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--observations-directory", type=Path, required=True)
    parser.add_argument("--media-directory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Use a fresh controls output")
    reports, media = {}, {}
    for role in ["reference", "candidate"]:
        reports[role] = json.loads((args.observations_directory / (role + "-observations.json")).read_text(encoding="utf-8"))
        media[role] = json.loads((args.media_directory / (role + "-media.json")).read_text(encoding="utf-8"))
    reference = verify(reports["reference"], media["reference"], media["reference"]["replacements"][0]["local_file"], False)
    candidate_binary = media["candidate"]["replacements"][0]["local_file"]
    require(verify(reports["candidate"], media["candidate"], candidate_binary, True) == reference, "Positive control differs")

    def event(report, name):
        return next(e for e in report["observedDosCalls"] if e["Name"] == name)

    def command_event(report, name):
        events = report["observedDosCalls"]
        start = events.index(event(report, "CopyOpenLibrary"))
        end = events.index(event(report, "CopyCloseLibrary"))
        return next(e for e in events[start:end] if e["Name"] == name)

    def resident_event(report, name):
        key = "D2" if name == "AddSegment" else "RemovedSegmentList"
        return next(e for e in report["observedDosCalls"] if e["Name"] == name and
            e.get(key) == report["copyInvocationOwnership"][0]["Segment"])

    def mutate_readback(report, filename):
        events = report["observedDosCalls"]
        opened = next(e for e in events if e["Name"] == "Open" and e.get("Text") == filename)
        read = next(e for e in events[events.index(opened)+1:] if e["Name"] == "FGetC" and
            e["D1"] == opened["ReturnedD0"] and e["Task"] == opened["Task"])
        read["ReturnedD0"] ^= 1

    controls = [
        ("ambient-error", lambda r: r["copyInvocationOwnership"][0].update(ReturnIoErr=0)),
        ("missing-parser-release", lambda r: r["observedDosCalls"].remove(command_event(r, "FreeArgs"))),
        ("wrong-list-flags", lambda r: event(r, "FindDosEntry").update(D3=12)),
        ("wrong-volume-name", lambda r: event(r, "Relabel").update(DestinationText="Wrong")),
        ("changed-volume-content", lambda r: mutate_readback(r, "relabel-proof")),
        ("changed-parser-diagnostic", lambda r: mutate_readback(r, "rl-diag3")),
        ("missing-registration", lambda r: r["observedDosCalls"].remove(resident_event(r, "AddSegment"))),
        ("missing-resident-removal", lambda r: r["observedDosCalls"].remove(resident_event(r, "RemSegment"))),
        ("wrong-segment-release", lambda r: event(r, "SegmentFreeMem").update(A1=0)),
        ("wrong-loaded-image", lambda r: r.update(loadedCopyImageSha256="0" * 64)),
        ("shared-image-write", lambda r: r["copyCpuImageWrites"].append({"Address": 1})),
        ("trace-overflow", lambda r: r.update(dosObservationOverflow=True)),
    ]
    results = []
    for name, corrupt in controls:
        modified = copy.deepcopy(reports["candidate"])
        corrupt(modified)
        try:
            result = verify(modified, media["candidate"], candidate_binary, True)
            require(result == reference, "Original/replacement differential mismatch")
        except ValueError as error:
            results.append({"name": name, "rejected": True, "reason": str(error)})
        else:
            raise ValueError("Corrupted evidence accepted: " + name)
    args.output.write_text(json.dumps({"status": "passed", "positivePair": True, "rejections": results,
        "referenceObservationsSha256": digest(args.observations_directory / "reference-observations.json"),
        "candidateObservationsSha256": digest(args.observations_directory / "candidate-observations.json"),
        "controlsSha256": digest(__file__), "verifierSha256": digest(Path(__file__).with_name("verify_relabel_boot.py"))}, indent=2) + "\n", encoding="utf-8")
    print(args.output)


if __name__ == "__main__":
    main()
