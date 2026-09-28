"""Verify bounded Rename E02/E04/R03 policy receipts for both profiles."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any


CPUS = ("68000", "68020", "68040")
PREFIX = bytes.fromhex("43616e27742072656e616d65206f6c64206173206e6577206265636175736520")


def fail(message: str) -> None:
    raise SystemExit(message)


def require(condition: bool, message: str) -> None:
    if not condition:
        fail(message)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_case(path: Path, name: str) -> dict[str, Any]:
    report = json.loads(path.read_text(encoding="utf-8"))
    cases = [case for case in report.get("cases", [])
             if case.get("name") == name and not case.get("instructionInterleaved", False)]
    require(len(cases) == 1, f"{path}: expected one sequential {name} case")
    case = cases[0]
    require(case.get("configuredStackBytes") == 4096, f"{path}: {name} stack budget")
    require(case.get("stackBytesWritten", 0) <= 4096, f"{path}: {name} stack overflow")
    return case


def event_order(events: list[str], before: str, after: str) -> None:
    require(before in events and after in events and events.index(before) < events.index(after),
            f"event order {before} -> {after}")


def check_workbench_case(case: dict[str, Any], name: str) -> None:
    events = case["events"]
    if name == "direct-false-zero":
        require(case["result"] == 0 and case["ioErr"] == 0, "Workbench E02 direct result")
        require(bytes.fromhex(case["stdoutHex"]) == PREFIX, "Workbench E02 direct prefix")
        require("Rename" in events and "PrintFault" not in events, "Workbench E02 direct calls")
        event_order(events, "VPrintf", "SetIoErr")
    elif name == "directory-compose-second-zero":
        require(case["result"] == 0 and case["ioErr"] == 0, "Workbench E02 directory result")
        require(bytes.fromhex(case["stdoutHex"]) == bytes.fromhex(
            "43616e27742072656e616d65205352433a66696c65206173204f55543a66696c65206265636175736520"),
            "Workbench E02 directory prefix")
        require(events.count("Rename") == 2 and "PrintFault" not in events,
                "Workbench E02 directory calls")
        event_order(events, "VPrintf", "SetIoErr")
    elif name == "parser-fault-fails":
        require(case["result"] == 20 and case["ioErr"] == 999, "Workbench E04 parser result")
        require(events.count("PrintFault") == 1 and "FreeArgs" not in events,
                "Workbench E04 parser calls")
    elif name == "parser-cleanup-error":
        require(case["result"] == 20 and case["ioErr"] == 777, "Workbench R03 failure result")
        require(events.count("PrintFault") == 1, "Workbench R03 failure fault")
    elif name == "direct-cleanup-error":
        require(case["result"] == 0 and case["ioErr"] == 777, "Workbench R03 success result")
        require("PrintFault" not in events, "Workbench R03 success fault")


def check_morphos_case(case: dict[str, Any], name: str) -> None:
    events = case["events"]
    if name == "selected-error-survives-output":
        require(case["result"] == 20 and case["ioErr"] == 203, "MorphOS E04 selected result")
        require(bytes.fromhex(case["stdoutHex"]) == PREFIX, "MorphOS E04 selected prefix")
        require(events.count("PrintFault") == 1, "MorphOS E04 selected fault")
        event_order(events, "VPrintf", "PrintFault")
    elif name == "zero-selected-error-keeps-output-error":
        require(case["result"] == 20 and case["ioErr"] == 999, "MorphOS E02 zero result")
        require(bytes.fromhex(case["stdoutHex"]) == PREFIX, "MorphOS E02 zero prefix")
        require("PrintFault" not in events, "MorphOS E02 zero fault")
    elif name == "parser-fault-ambient-error":
        require(case["result"] == 10 and case["ioErr"] == 777, "MorphOS E04 parser result")
        require(events.count("PrintFault") == 1, "MorphOS E04 parser fault")
    elif name == "successful-cleanup-ambient-error":
        require(case["result"] == 0 and case["ioErr"] == 777, "MorphOS R03 success result")
        require(case["stdoutHex"] == "" and "PrintFault" not in events, "MorphOS R03 success output")


def verify_profile(root: Path, profile: str, expected_total: int, expected_per_cpu: int,
                   names: tuple[str, ...], checker: Any) -> dict[str, Any]:
    qualification_path = root / "qualification.json"
    qualification = json.loads(qualification_path.read_text(encoding="utf-8"))
    require(qualification.get("status") == "passed", f"{profile}: qualification status")
    require(qualification.get("shippingOrPureApproval") is False,
            f"{profile}: receipt must remain development-only")
    require(qualification.get("invocations") == expected_total,
            f"{profile}: invocation count")
    rows = []
    for cpu in CPUS:
        report_path = root / f"rename-{cpu}.runtime.json"
        report = json.loads(report_path.read_text(encoding="utf-8"))
        require(report.get("status") == "passed", f"{profile}/{cpu}: runtime status")
        require(report.get("cases") and len(report["cases"]) == expected_per_cpu,
                f"{profile}/{cpu}: runtime count")
        for name in names:
            checker(load_case(report_path, name), name)
        rows.append({"cpu": cpu, "runtime": str(report_path),
                     "runtimeSha256": digest(report_path),
                     "cases": len(report["cases"])})
    return {"qualification": str(qualification_path),
            "qualificationSha256": digest(qualification_path),
            "invocationsTotal": expected_total, "invocationsPerCpu": expected_per_cpu,
            "cpus": rows}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--workbench", type=Path,
                        default=Path("artifacts/workbench-rename-examine-qualified"))
    parser.add_argument("--morphos", type=Path,
                        default=Path("artifacts/morphos-rename-body-qualified"))
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result: dict[str, Any] = {
        "status": "passed",
        "suite": "rename-error-policy-supplied-vector-fixture",
        "scope": "E02 Rename(FALSE)/IoErr0, E04 output/fault error poisoning, and R03 cleanup-time IoErr for Workbench and MorphOS development receipts on 68000/020/040. Synthetic provider combinations remain separate from real-DOS differential and shipping admission.",
        "profiles": {
            "wb31": verify_profile(
                args.workbench, "wb31", 309, 103,
                ("direct-false-zero", "directory-compose-second-zero",
                 "parser-fault-fails", "parser-cleanup-error", "direct-cleanup-error"),
                check_workbench_case),
            "morphos320": verify_profile(
                args.morphos, "morphos320", 336, 112,
                ("selected-error-survives-output", "zero-selected-error-keeps-output-error",
                 "parser-fault-ambient-error", "successful-cleanup-ambient-error"),
                check_morphos_case),
        },
    }
    result["verifierSha256"] = digest(Path(__file__))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
