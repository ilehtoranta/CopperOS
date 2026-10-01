"""Verify the disposable two-process, one-SegList MakeLink purity probe.

This verifier intentionally does not require resident removal: the input trace
is a disposable resident-use probe whose image is released by the surrounding
boot fixture.  Resident add/remove and startup policy use separate reports.
"""
import argparse
import hashlib
import json
from pathlib import Path


def require(condition, message):
    if not condition:
        raise ValueError(message)


def verify(report):
    require(report.get("commandUnderTest") == "MakeLink", "wrong command")
    require(report.get("rootInfoReady") and report.get("diskBytesUnchanged") and
            report.get("privateDosStructuresWritten") is False,
            "trace is not the required disposable probe")
    require(report.get("copyMaximumActiveInvocations") == 2 and
            report.get("copyActiveInvocations") == 0,
            "overlap did not start and finish")
    require(report.get("copyCpuImageWrites") == [] and
            report.get("copyImageUnchangedAtReturn") is True,
            "shared image was written or changed")
    rows = report.get("copyInvocationOwnership")
    require(isinstance(rows, list) and len(rows) == 2, "invocation rows")
    require(len({row["Task"] for row in rows}) == 2, "distinct process owners")
    require(len({row["Segment"] for row in rows}) == 1, "not one shared SegList")
    require(sorted(row["ReturnCode"] for row in rows) == [0, 20],
            "success/failure cases")
    require(all(row["Allocations"] == row["Frees"] == 1 and
                row["ImageUnchanged"] and not row["ParserFailed"] and
                not row["OpenFailed"] for row in rows),
            "invocation-owned state")
    calls = report.get("observedDosCalls")
    require(isinstance(calls, list) and calls, "DOS call trace")
    tasks = {row["Task"] for row in rows}
    for task in tasks:
        owned = [call for call in calls if call.get("Task") == task]
        opens = [call for call in owned if call.get("Name") == "CopyOpenLibrary"]
        closes = [call for call in owned if call.get("Name") == "CopyCloseLibrary"]
        require(len(opens) == len(closes) == 1, "per-process DOS lease")
        require(opens[0].get("ReturnedD0", 0) != 0 and
                closes[0].get("A1") == opens[0].get("ReturnedD0") and
                "ReturnedD0" in closes[0], "library ownership")
        start, end = calls.index(opens[0]), calls.index(closes[0])
        require(start < end, "lease order")
        body = owned
        parsers = [call for call in body if call.get("Name") == "ReadArgs" and
                   call.get("Text") == "FROM/A,TO/A,HARD/S,FORCE/S"]
        require(len(parsers) == 1 and parsers[0].get("ReturnedD0", 0) != 0,
                "MakeLink parser")
        parser_result = parsers[0].get("ReturnedD0")
        frees = [call for call in body if call.get("Name") == "FreeArgs" and
                 call.get("D1") == parser_result]
        require(len(frees) >= 1 and all("ReturnedD0" in free for free in frees),
                "MakeLink parser ownership")
    require(report.get("copyDirectAllocationsBalanced") is True and
            report.get("copyDirectFrees") == report.get("copyDirectAllocations"),
            "direct allocation balance")
    return {
        "status": "same-seglist-two-process-makelink-purity-probe-passed",
        "tasks": sorted(tasks),
        "sharedSegments": [rows[0]["Segment"]],
        "returns": sorted(row["ReturnCode"] for row in rows),
        "residentRemovalQualified": False,
        "fullCommandQualified": False,
        "pureAdmission": False,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("observations", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    data = args.observations.read_bytes()
    result = verify(json.loads(data))
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),
                  verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
