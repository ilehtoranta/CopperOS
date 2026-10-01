"""Bind normal caller-owned storage to actual frees before allocator reuse."""
from verify_relabel_boot import one, require
from verify_relabel_caller_retirement import observe as observe_retirement
from verify_relabel_replacement_boot import qualify


def free_ranges(headers):
    require(0 < len(headers) <= 32, "Bounded memory headers")
    ranges, regions = [], []
    for header in headers:
        lower, upper = header["Lower"], header["Upper"]
        require(0 < lower < upper <= 0x100000000 and header["Address"] != 0, "Memory header bounds")
        require(all(upper <= a or lower >= b for a, b in regions), "Memory headers do not overlap")
        regions.append((lower, upper))
        end, total = lower, 0
        require(len(header["Chunks"]) <= 4096, "Bounded free chunks")
        for chunk in header["Chunks"]:
            start, size = chunk["Address"], chunk["Bytes"]
            require(start >= end and size >= 8 and start % 8 == size % 8 == 0 and start + size <= upper,
                "Ordered valid free chunks")
            end = start + size
            total += size
            ranges.append((start, end))
        require(total == header["FreeBytes"] <= upper - lower, "Free bytes match complete chunk list")
    return regions, ranges


def observe(report, media, candidate):
    result = observe_retirement(report, media, candidate)
    retired = one(report["copyCallerRetirements"], "Retired caller")
    owner = retired["OwnedMemoryAtRemoval"]
    spans = owner["Spans"]
    require(owner["Task"] == retired["TargetTask"] and owner["StackLower"] < owner["StackUpper"] and
        len(spans) == 4 and [s["Kind"] for s in spans] == ["MemList", "MemEntry", "MemList", "MemEntry"],
        "Original caller MemList ownership shape")
    require(retired.get("RemTaskHostGateway") is False, "Original RemTask vector retained")
    require(report["callerDeferredTasksAtBound"] == [] and report["callerMemoryReapingQualified"] is False,
        "Read-only observer does not self-qualify or leave a deferred host owner")
    regions, initial_free = free_ranges(retired["FreeMemoryAtRemoval"])
    owned = []
    for span in spans:
        start, size, rounded = span["Address"], span["RequestedBytes"], span["RoundedBytes"]
        end = start + rounded
        require(start != 0 and start % 8 == 0 and 0 < size <= 3 * 1024 * 1024 and rounded == (size + 7) & ~7 and
            end <= 0x100000000 and any(a <= start < end <= b for a, b in regions), "Owned release geometry")
        require(all(end <= a or start >= b for a, b in owned), "Owned spans do not overlap")
        require(all(end <= a or start >= b for a, b in initial_free), "Owned allocation is not initially free")
        owned.append((start, end))
    require(any(a <= owner["Task"] < owner["StackLower"] < owner["StackUpper"] <= b for a, b in owned),
        "Owned allocation includes actual Process and complete process stack")
    frees = retired["OwnedMemoryFreeCalls"]
    global_frees = [e for e in report["observedDosCalls"] if e["Name"] == "CallerOwnedFreeMem"]
    require(len(frees) == len(global_frees) == len(spans) and frees == global_frees, "Exactly one observed release per owned span")
    normalized = []
    for span in spans:
        released = one([e for e in frees if e["A1"] == span["Address"]], "Owned span release")
        require(released["D0"] == span["RequestedBytes"] and released["OwnedTask"] == owner["Task"] and
            retired["EntryCycle"] < released["EntryRetireCycle"] < released["ReturnRetireCycle"] < retired["FirstOtherTaskCycle"],
            "Exact requested free completes within terminal removal")
        require(released["FreeMemHostGateway"] is True and released["ExecBase"] != 0 and
            released["StatusRegister"] & 0x2000 == 0 and released["ActiveSp"] == released["UserSp"] and
            owner["StackLower"] <= released["ActiveSp"] <= owner["StackUpper"],
            "Observed original RemTask cleanup uses retiring user stack before terminal switch")
        _, after_free = free_ranges(released["FreeMemoryAfterReturn"])
        require(any(a <= span["Address"] and span["Address"] + span["RoundedBytes"] <= b for a, b in after_free),
            "Whole rounded owned allocation actually free immediately after return")
        normalized.append({"kind": span["Kind"], "requestedBytes": span["RequestedBytes"], "roundedBytes": span["RoundedBytes"]})
    # Later allocations may legitimately reuse these addresses. The final free
    # list is checked for integrity but is not a substitute for release evidence.
    free_ranges(report["callerFreeMemoryAtBound"])
    result["callerMemory"] = {"ownedSpans": normalized, "roundedBytesReclaimed": sum(s["RoundedBytes"] for s in spans),
        "originalRemTaskWithHostFreeMem": True, "allOwnedSpansReclaimedBeforeSwitch": True,
        "originalFreesBeforeLeavingUserStack": True, "hostDeferredQueueEmptyAtBound": True,
        "hostRetirementStackSafetyQualified": False,
        "scope": "Captured tc_MemEntry spans and Process/stack only; no arbitrary resources or abrupt death."}
    return result


if __name__ == "__main__":
    qualify(observe, "Relabel-caller-owned-memory-original-DOS-boot", __file__,
        "Four captured caller-owned tc_MemEntry spans are actually freed by original RemTask via host FreeMem before another task runs; original cleanup still uses the retiring user stack and normal command return precedes retirement. This does not authorize host cleanup before a safe handoff. No general process resource ledger, forced death, pending packet/callback termination, CopperOS host RemTask native-switch proof, other platform/launch, original PURE classification or shipping admission.",
        dependencies=("verify_relabel_caller_retirement.py", "verify_relabel_concurrent_boot.py"))
