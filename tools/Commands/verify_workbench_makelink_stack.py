"""Validate measured command/DOS stack samples; never admit a minimum stack."""
import argparse
import json
from pathlib import Path

import verify_workbench_makelink_error_refresh as binding
from verify_workbench_makelink_secondary import secondary_results
from verify_copy_boot_transfer import require


def inspect_stack(report):
    scope = report["commandStackObservationScope"]
    require(scope["SchemaVersion"] == 1 and scope["MinimumStackQualified"] is False and
            scope["ShippingOrPureApproval"] is False, "Unknown or overclaimed stack observation scope")
    stacks = report["commandStackObservations"]
    rows = report["copyInvocationOwnership"]
    require(len(stacks) == len(rows) == 5, "Incomplete five-invocation stack observation")
    result = []
    for stack, row in zip(stacks, rows):
        require(stack["Task"] == row["Task"] and all(stack[k] is True for k in
            ("HunkEntryObserved", "HunkReturnObserved", "RunCommandReturned",
             "EntryTaskBoundsMapped", "EntryReturnSlotWithinTaskBounds")), "Incomplete or foreign stack window")
        keys = ("RequestedStackBytes", "EntrySp", "ReturnSp", "EntryTaskStackLower", "EntryTaskStackUpper",
                "EntryStatusRegister", "EntryCycle", "ReturnCycle", "MetadataBoundedMinimumSp",
                "MetadataBoundedMaximumSp", "MetadataBoundedObservedBytesBelowEntrySp",
                "MetadataBoundedObservedBytesBelowStackUpper", "MetadataBoundedObservedSpHeadroom",
                "OwnerUserWindowSamples", "MetadataBoundedUserSamples", "MetadataBoundedHunkPcSamples",
                "MetadataBoundedCalleePcSamples", "EntryBoundsUnprovenUserSamples",
                "ChangedTaskBoundsUserSamples", "OutsideEntryTaskBoundsUserSamples")
        require(all(type(stack[k]) is int and stack[k] >= 0 for k in keys), "Invalid stack metric")
        lower, upper = stack["EntryTaskStackLower"], stack["EntryTaskStackUpper"]
        entry, minimum, maximum = stack["EntrySp"], stack["MetadataBoundedMinimumSp"], stack["MetadataBoundedMaximumSp"]
        require(lower <= minimum <= entry <= maximum <= upper and
                stack["ReturnSp"] == entry+4 <= upper and
                upper-lower == stack["RequestedStackBytes"] == 4096 and
                stack["EntryStatusRegister"] & 0x2000 == 0 and
                stack["ReturnCycle"] > stack["EntryCycle"], "Stack bounds/entry/return or requested budget mismatch")
        require(all(stack[k] == 0 for k in ("EntryBoundsUnprovenUserSamples", "ChangedTaskBoundsUserSamples",
                                          "OutsideEntryTaskBoundsUserSamples")), "Unknown or changed stack bounds")
        require(stack["OwnerUserWindowSamples"] == stack["MetadataBoundedUserSamples"] ==
                stack["MetadataBoundedHunkPcSamples"] + stack["MetadataBoundedCalleePcSamples"] and
                stack["MetadataBoundedHunkPcSamples"] > 0 and stack["MetadataBoundedCalleePcSamples"] > 0,
                "Missing HUNK/callee samples or inconsistent sample counts")
        require(minimum == min(stack["MetadataBoundedHunkMinimumSp"], stack["MetadataBoundedCalleeMinimumSp"]) ==
                stack["OwnerUserMinimumSp"] and maximum == stack["OwnerUserMaximumSp"], "Inconsistent SP extrema")
        require(stack["MetadataBoundedObservedBytesBelowEntrySp"] == entry-minimum and
                stack["MetadataBoundedObservedBytesBelowStackUpper"] == upper-minimum and
                stack["MetadataBoundedObservedSpHeadroom"] == minimum-lower, "Incorrect stack arithmetic")
        result.append({"primary": row["ReturnCode"], "requestedBytes": 4096, "publishedBytes": upper-lower,
                       "observedBytesBelowStackUpper": upper-minimum,
                       "observedBytesBelowEntrySp": entry-minimum, "observedSpHeadroom": minimum-lower,
                       "hunkOnlyBytesBelowEntrySp": entry-stack["MetadataBoundedHunkMinimumSp"],
                       "userSamples": stack["MetadataBoundedUserSamples"],
                       "excludedSupervisorSamples": stack["OwnerSupervisorWindowSamples"],
                       "excludedOtherTaskSamples": stack["OtherTaskWindowSamples"]})
    return result


def verify(args):
    report, media = binding.bind(args.observations, args.media, "candidate", args.expected_candidate_sha256)
    binding.inspect(report, True)
    binding.verify_trx(args.trx, report)
    require(binding.digest(args.test_assembly) == report["testAssemblySha256"].lower(), "Changed stack observer assembly")
    stacks = inspect_stack(report)
    results = secondary_results(report)
    paths = {"observations": args.observations, "media": args.media, "testResult": args.trx,
             "observerAssembly": args.test_assembly, "stackVerifier": Path(__file__),
             "bindingVerifier": Path(binding.__file__),
             "behaviorVerifier": Path(__file__).with_name("compare_workbench_makelink_errors.py"),
             "secondaryVerifier": Path(__file__).with_name("verify_workbench_makelink_secondary.py")}
    return {"status": "workbench-makelink-stack-observation-verified",
            "scope": report["commandStackObservationScope"], "stacks": stacks, "results": results,
            "candidateBinarySha256": args.expected_candidate_sha256,
            "minimumStackQualified": False, "shippingOrPureApproval": False,
            "evidence": {name: {"path": str(path), "sha256": binding.digest(path)} for name, path in paths.items()}}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("observations", "media", "trx", "test_assembly"):
        parser.add_argument(name, type=Path)
    parser.add_argument("--expected-candidate-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Refusing to overwrite stack evidence")
    result = verify(args)
    args.output.write_text(json.dumps(result, indent=2) + "\n")
    print("PASS: five metadata-bounded command/DOS SP observations; minimum-stack admission remains false")
