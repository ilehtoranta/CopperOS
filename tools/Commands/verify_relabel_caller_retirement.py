"""Verify normal caller self-removal without losing a reusable resident command."""
from verify_relabel_boot import one, require
from verify_relabel_concurrent_boot import observe as observe_concurrent
from verify_relabel_replacement_boot import qualify


def task_addresses(topology):
    require(topology.get("terminatedAtTail") is True and topology.get("predecessorsConsistent") is True,
        "Valid task-list traversal")
    return [row["address"] for row in topology["tasks"]]


def observe(report, media, candidate):
    result = observe_concurrent(report, media, candidate)
    require(report.get("requireCallerRetirement") is True and media["scenario"] == "active-replace",
        "Explicit caller retirement workload")
    rows = report["copyInvocationOwnership"]
    background = one([r for r in rows if r["ReturnCode"] == 20], "Completed background caller")
    parent = [r for r in rows if r["ReturnCode"] == 0]
    require(len(parent) == 2 and parent[0]["Task"] == parent[1]["Task"] != background["Task"], "Parent recovery owner")
    retired = one(report["copyCallerRetirements"], "One command caller retires")
    require(retired["Task"] == retired["TargetTask"] == background["Task"] and retired["ArgumentTask"] == 0 and
        retired["CallerPc"] != 0 and retired["ReturnSp"] != 0 and retired.get("ReturnedToCaller") is False,
        "Terminal RemTask(NULL) from the background caller")
    require(retired["ActiveCommandCalls"] == 0 and retired["ImageLive"] is True and
        retired["Segment"] == retired["RegistrySegment"] == background["Segment"] and retired["RegistryUseCount"] == 1,
        "Command ownership released before process self-removal; resident stays live")
    require(background["ReturnCycle"] < retired["EntryCycle"] < retired["FirstOtherTaskCycle"] and
        retired["FirstOtherTask"] != 0 and retired["FirstOtherTask"] != background["Task"], "Actual post-removal task switch")
    for key in ["ReadyTasksAfterSwitch", "WaitTasksAfterSwitch"]:
        require(background["Task"] not in task_addresses(retired[key]), "Retired task absent after switch")
    # Require later successful use, not just disappearance from one list sample.
    recovery = max(parent, key=lambda row: row["EntryCycle"])
    require(retired["FirstOtherTaskCycle"] < recovery["EntryCycle"], "Parent reuses resident after worker retires")
    events = report["observedDosCalls"]
    removal = one([e for e in events if e["Name"] == "RemSegment" and
        e.get("RemovedSegmentList") == background["Segment"]], "Final resident removal")
    require(removal["D1"] == retired["RegistryNode"] and removal["RemovedUseCount"] == 1 and
        recovery["ReturnCycle"] < removal["EntryRetireCycle"], "Same idle registry node after task retirement")
    require(not any(e["Name"] == "RunCommand" and e["Task"] == background["Task"] and
        e["EntryRetireCycle"] > retired["EntryCycle"] for e in events), "No command execution by retired caller")
    snapshots = [s for s in report["snapshots"] if s["cycles"] > retired["FirstOtherTaskCycle"]]
    require(snapshots and snapshots[-1]["cycles"] > removal["ReturnRetireCycle"], "Observation continues through final cleanup")
    for snapshot in snapshots:
        require(snapshot["currentTask"] != background["Task"], "Retired caller never runs in later snapshots")
        require(background["Task"] not in task_addresses(snapshot["readyTasks"]) + task_addresses(snapshot["waitTasks"]),
            "Retired caller remains unlinked")
    result["callerRetirement"] = {"normalSelfRemoval": True, "activeCommandCallsAtRemoval": 0,
        "residentUseCountAtRemoval": 1, "returnedToCaller": False, "absentAfterSwitch": True,
        "parentReusedResidentAfterRemoval": True, "sameRegistryNodeAtFinalCleanup": True,
        "taskStorageReapingQualified": False, "forcedTaskDeathQualified": False}
    return result


if __name__ == "__main__":
    qualify(observe, "Relabel-caller-retirement-original-DOS-boot", __file__,
        "Normal background caller RemTask(NULL), unlink/switch and subsequent resident reuse on 68000; original DOS/Shell with host Exec/device takeover. Command allocations/parser/DOS leases balance before retirement. No complete process-storage reaping, abrupt task death, outstanding-resource termination, other platform/launch, original PURE classification or shipping admission.",
        dependencies=("verify_relabel_concurrent_boot.py",))
