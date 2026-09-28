"""Verify final process secondary results for the five-case MakeLink fixture.

The passive observer reads pr_Result2 from the owning task when the public
RunCommand vector returns. This is the observable DOS boundary, after command
cleanup and DOS RunCommand cleanup; it is not an inferred PrintFault argument.
"""
import argparse
import json
from pathlib import Path

import verify_workbench_makelink_error_refresh as refresh
from verify_copy_boot_transfer import one, require


def secondary_results(report):
    rows = report["copyInvocationOwnership"]
    require(len(rows) == 5, "Expected five completed command invocations")
    segment = rows[0]["Segment"]
    task = rows[0]["Task"]
    events = report["observedDosCalls"]
    starts = [n for n, event in enumerate(events) if event["Name"] == "RunCommand"
              and event["D1"] == segment and event["Task"] == task]
    require(len(starts) == len(rows), "RunCommand/ownership cardinality")
    result = []
    names = ["success", "missing-required-argument", "missing-target", "duplicate-link", "recovery"]
    for index, start in enumerate(starts):
        run = events[start]
        row = rows[index]
        require(row["Segment"] == segment and row["Task"] == task and
                run.get("ReturnedD0") == row["ReturnCode"], "RunCommand/ownership identity")
        require("ReturnedIoErr" in run and isinstance(run["ReturnedIoErr"], int) and
                not isinstance(run["ReturnedIoErr"], bool) and 0 <= run["ReturnedIoErr"] <= 0xffffffff,
                "Missing or invalid final process secondary result")
        stop = starts[index + 1] if index + 1 < len(starts) else len(events)
        calls = [event for event in events[start + 1:stop] if event["Task"] == task]
        opened = one([e for e in calls if e["Name"] == "CopyOpenLibrary"], "Command DOS open")
        closed = one([e for e in calls if e["Name"] == "CopyCloseLibrary"], "Command DOS close")
        require(opened.get("ReturnedD0", 0) != 0 and closed["A1"] == opened["ReturnedD0"] and
                calls.index(opened) < calls.index(closed) and "ReturnedD0" in closed and
                "ReturnedIoErr" in closed, "Completed DOS library lifetime")
        require(run["D2"] == 4096, "Changed requested RunCommand stack size")
        result.append({"case": names[index], "primary": run["ReturnedD0"],
                       "secondaryAtRunCommandReturn": run["ReturnedIoErr"],
                       "secondaryAtCloseLibraryReturn": closed["ReturnedIoErr"],
                       "requestedStackBytes": run["D2"]})
    return result


def verify(args):
    bound = refresh.verify(args)
    expected = secondary_results(refresh.read(args.reference))
    actual = secondary_results(refresh.read(args.candidate))
    # Library-close values are recorded for investigation. The public command
    # completion values, rather than an internal cleanup checkpoint, define this
    # comparison's contract.
    signature = lambda rows: [(r["case"], r["primary"], r["secondaryAtRunCommandReturn"])
                              for r in rows]
    require(signature(expected) == signature(actual), "Final primary/secondary result mismatch")
    bound["evidence"]["secondary_verifier"] = {
        "path": str(Path(__file__).resolve()), "sha256": refresh.digest(Path(__file__))}
    return {"status": "workbench-makelink-secondary-results-verified",
            "scope": "Five current-HUNK versus original Workbench MakeLink calls through the same original DOS. Final owning-process pr_Result2 at public RunCommand return is compared after cleanup. Other options, handlers, startup, cleanup-error injections, rendered output and full-profile/purity/shipping remain separate.",
            "reference": expected, "candidate": actual,
            "candidateBinarySha256": args.expected_candidate_sha256,
            "shippingQualified": False, "fullProfileParity": False,
            "evidence": bound["evidence"]}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("reference", "candidate", "reference_media", "candidate_media",
                 "reference_trx", "candidate_trx", "test_assembly"):
        parser.add_argument(name, type=Path)
    parser.add_argument("--expected-candidate-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Refusing to overwrite historical evidence")
    result = verify(args)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print("PASS: five final MakeLink primary/secondary results match original Workbench")
