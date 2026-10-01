"""Compare one admitted original/candidate Workbench command pair without normalization.

Re-decodes saved RAM and verifies bound media/probe/runtime evidence. Equality is
limited to this exact invocation and its captured bytes/return/caller IoErr; no
general command, child Result2, resident/PURE or shipping qualification follows.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import struct
import sys

import analyze_probe
from capture_probe import bind_command_replacement, bind_qualified_runtime, identity

INVENTORY_PATH = Path(__file__).resolve().parents[1] / "Inventory/inventory.py"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def bytes_identity(data):
    return {"bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}


def same_bytes(first, second):
    return first["bytes"] == second["bytes"] and first["sha256"] == second["sha256"]


def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def bound_json(record):
    path = Path(record["path"]).resolve(strict=True)
    require(identity(path) == record, f"Bound JSON identity changed: {path}")
    return read_json(path)


def file_fact(data, guest_path):
    spec = importlib.util.spec_from_file_location("probe_compare_adf_inventory", INVENTORY_PATH)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    disk = module.Adf(data)
    entry = disk.walk().get(guest_path.lower())
    require(entry is not None and entry["secondary_type"] == -3, f"Missing ordinary guest file {guest_path}")
    header = disk.block(entry["block"])
    words = struct.unpack(">128I", header)
    return {"path": entry["path"], **bytes_identity(disk.read_file(entry["block"])),
            "metadata": {"protection": words[80], "datestamp": list(words[105:108]),
                         "comment_hex": header[329:329 + header[328]].hex(), "owner_words": list(words[78:80])}}


def invocation_line(invocation):
    command = invocation["command"]
    quoted = command.replace("*", "**").replace('"', '*"')
    return f'C:CopperProbe COMMAND "{quoted}" TOKEN {invocation["token"]}'


def command_guest_path(command):
    match = re.match(r'(?i)^C:([A-Za-z0-9_][A-Za-z0-9_.-]{0,63})(?: |$)', command)
    require(match is not None, 'Invocation does not name one C command')
    return 'C/' + match.group(1)


def load_side(path, candidate):
    side = {"admitted": False, "errors": [], "analysisPath": str(Path(path).resolve()),
            "result": None, "lastObservedRecord": None}
    try:
        side["analysis"] = identity(path)
        report = read_json(path)
        # Preserve raw reported fields even if later admission rejects them.
        side["reportedStatus"] = report.get("status")
        side["result"] = report.get("result")
        side["invocation"] = report.get("invocation")
        rows = report.get("observations", [])
        side["lastObservedRecord"] = rows[-1].get("record") if rows else None
        require(report.get("schemaVersion") == 1 and report.get("kind") == "workbench31-guest-probe-passive-analysis",
                "Unsupported analysis schema/kind")
        capture = bound_json(report["capture"])
        side["capture"] = report["capture"]
        token = report.get("expectedToken")
        require(type(token) is int and 0 <= token <= 0xffffffff, "Invalid expected token")
        fresh = analyze_probe.analyze(Path(report["capture"]["path"]), token)
        side["redecodedStatus"] = fresh["status"]
        side["redecodedResult"] = fresh["result"]
        side["ownerTaskStates"] = owner_task_states(fresh["observations"])
        require(report.get("status") == fresh["status"] == "complete-probe-record-observed"
                and not report.get("errors") and not fresh["errors"], "Probe analysis is not complete")
        for key in ("result", "observations", "expectedToken", "invocation"):
            require(report.get(key) == fresh.get(key), f"Saved analysis {key} differs from re-decoded snapshots")
        recorded = {item["path"]: item for item in capture["inputsAfter"]}
        require(len(recorded) == len(capture["inputsAfter"]), "Duplicate capture input identities")
        roles = capture["roles"]
        role_ids = {}
        for name in ("rom", "originalAdf", "derivedAdf", "probe", "derivativeReceipt"):
            current = identity(Path(roles[name]))
            require(recorded.get(current["path"]) == current, f"Capture role bytes changed/not bound: {name}")
            role_ids[name] = current
        side["roles"] = role_ids
        preparation = read_json(role_ids["derivativeReceipt"]["path"])
        side["preparation"] = role_ids["derivativeReceipt"]
        require(preparation.get("schema_version") == 1 and preparation.get("kind") == "workbench31-diagnostic-probe-adf"
                and preparation.get("status") == "prepared", "Preparation receipt was not successful")
        for pre_key, role in (("source_adf", "originalAdf"), ("derived_adf", "derivedAdf"), ("probe", "probe")):
            require(same_bytes(preparation[pre_key], role_ids[role]), f"Preparation {pre_key} bytes differ from capture")
        require(preparation.get("source_before_sha256") == preparation.get("source_after_sha256")
                == role_ids["originalAdf"]["sha256"], "Preparation changed original media")
        invocation = capture["invocation"]
        require(preparation["invocation"] == invocation == report["invocation"], "Invocation differs across receipts")
        require(invocation.get("guest_path") == "C/CopperProbe" and invocation.get("line") == invocation_line(invocation),
                "Invocation line does not exactly encode command/token")
        command = invocation["command"]
        replacement = preparation.get("replacement")
        side["replacement"] = replacement
        require((replacement is not None) == candidate, "Original/candidate role does not match replacement receipt")
        require(capture.get("replacement") == replacement, "Replacement metadata differs between capture/preparation")
        if candidate:
            require(report.get("replacement") == replacement, "Candidate analysis lacks exact replacement binding")
            require(command_guest_path(command).lower() == replacement.get("guest_path", "").lower(),
                    "Candidate invocation does not execute the replaced C command")
            _, paths = bind_command_replacement(preparation, Path(replacement["candidate"]["path"]),
                                                replacement.get("guest_path"))
            for item in paths:
                actual = identity(item)
                require(recorded.get(actual["path"]) == actual, "Candidate source/snapshot is not capture-bound")
        runtime_proof = capture["qualifiedRuntimeBinding"]
        require(recorded.get(runtime_proof["qualification"]["path"]) == runtime_proof["qualification"],
                "Runner qualification was not capture-bound")
        require(identity(Path(runtime_proof["qualification"]["path"])) == runtime_proof["qualification"],
                "Runner qualification changed")
        runtime_path = Path(capture["process"]["arguments"][1]).resolve(strict=True)
        runtime_files = sorted(Path(item["path"]) for item in recorded.values()
                               if Path(item["path"]).parent == runtime_path.parent
                               and Path(item["path"]).suffix.lower() in {".dll", ".json", ".pdb"})
        require(len(runtime_files) == 10 and runtime_path in runtime_files, "Incomplete frozen runtime set")
        _, verified_proof = bind_qualified_runtime(Path(runtime_proof["qualification"]["path"]), runtime_files)
        require(verified_proof == runtime_proof, "Frozen runtime/source qualification differs")
        side["runtime"] = [identity(item) for item in runtime_files]
        side["runtimeQualification"] = runtime_proof["qualification"]
        side["requestedFrames"] = capture["requestedFrames"]
        require(capture["process"]["arguments"][2:5] == [roles["rom"], roles["derivedAdf"], str(capture["requestedFrames"])],
                "Runner command arguments differ from bound roles/frame count")
        source_data = Path(role_ids["originalAdf"]["path"]).read_bytes()
        derived_data = Path(role_ids["derivedAdf"]["path"]).read_bytes()
        guest_path = replacement.get("guest_path") if candidate else command_guest_path(command)
        require(command_guest_path(command).lower() == guest_path.lower(),
                "Invocation target differs from the reference/candidate C member")
        original_command = file_fact(source_data, guest_path)
        command_fact = file_fact(derived_data, guest_path)
        side["originalCommand"] = original_command
        side["executedCommand"] = command_fact
        require(same_bytes(file_fact(derived_data, "C/CopperProbe"), role_ids["probe"]), "Derived guest probe differs from bound HUNK")
        if candidate:
            require(replacement["original_command"] == original_command, "Substitution original-command facts differ from base image")
            require(same_bytes(command_fact, replacement["candidate"]),
                    "Derived command is not the bound candidate")
            require(command_fact["metadata"] == original_command["metadata"], "Candidate command metadata was not preserved")
        else:
            require(command_fact == original_command, "Reference derivative changed original C/Version")
        side["admitted"] = True
    except (OSError, ValueError, KeyError, TypeError, AttributeError) as error:
        side["errors"].append(str(error))
    return side


def owner_task_states(observations):
    """Extract the pre-System stage-3 and terminal owner task observations."""
    before = next((row for row in observations
                   if (row.get("record") or {}).get("stage") == 3), None)
    after = next((row for row in reversed(observations)
                  if (row.get("record") or {}).get("stage") == 100), None)

    def state(row):
        if row is None:
            return None
        record = row["record"]
        return {"frame": row.get("frame"), "stage": record.get("stage"),
                "taskNumber": record.get("ownerTaskNumber"),
                "priority": record.get("ownerTaskPriority"),
                "signalsReceived": record.get("ownerTaskSignalsReceived")}

    return {"before": state(before), "after": state(after)}


def compare_sides(original, candidate, expected_owner_task_number=None,
                  expected_owner_priority_before=None,
                  expected_owner_priority_after=None,
                  expected_owner_task_signal_bit=None,
                  expected_owner_task_signal_mask=None):
    errors = []
    admitted = original["admitted"] and candidate["admitted"]
    if admitted:
        for role in ("rom", "originalAdf", "probe"):
            if not same_bytes(original["roles"][role], candidate["roles"][role]):
                errors.append(f"Paired {role} identities differ")
        if original["runtime"] != candidate["runtime"] or original["runtimeQualification"] != candidate["runtimeQualification"]:
            errors.append("Paired frozen runtime/qualification identities differ")
        if original["requestedFrames"] != candidate["requestedFrames"]:
            errors.append("Paired frame counts differ")
        if original["originalCommand"] != candidate["originalCommand"]:
                errors.append("Paired original command facts differ")
        if original["roles"]["derivedAdf"]["path"] == candidate["roles"]["derivedAdf"]["path"]:
            errors.append("Original/candidate derivatives are the same file")
    else:
        errors.append("Both capture sides must be admitted before equality can be established")
    admitted = admitted and not errors
    def result_field(side, name):
        return (side.get("result") or {}).get(name)
    comparison = {}
    for name, getter in (
        ("command", lambda side: (side.get("invocation") or {}).get("command")),
        ("outputHex", lambda side: result_field(side, "outputHex")),
        ("outputLength", lambda side: result_field(side, "outputLength")),
        ("commandReturn", lambda side: result_field(side, "commandReturn")),
        ("postSystemIoErr", lambda side: result_field(side, "postSystemIoErr")),
    ):
        first, second = getter(original), getter(candidate)
        raw_equal = first is not None and second is not None and first == second
        comparison[name] = {"original": first, "candidate": second,
                            "rawValuesEqual": raw_equal, "equal": admitted and raw_equal}
    command_equal = admitted and all(comparison[name]["equal"] for name in ("command", "outputHex", "outputLength", "commandReturn"))
    equal = command_equal and comparison["postSystemIoErr"]["equal"]
    effect_expectations = (expected_owner_task_number,
                           expected_owner_priority_before,
                           expected_owner_priority_after,
                           expected_owner_task_signal_bit,
                           expected_owner_task_signal_mask)
    effect_requested = any(value is not None for value in effect_expectations)
    if effect_requested:
        require(expected_owner_task_number is not None,
                "Owner task number is required for an owner task effect")
        require((expected_owner_priority_before is None) ==
                (expected_owner_priority_after is None),
                "Both expected owner priorities must be supplied together")
        require(expected_owner_priority_before is not None or
                expected_owner_task_signal_bit is not None or
                expected_owner_task_signal_mask is not None,
                "Specify an expected priority transition or signal effect")
        require(expected_owner_task_signal_mask is None or
                1 <= expected_owner_task_signal_mask <= 0xFFFFFFFF,
                "Expected owner signal mask must be a nonzero 32-bit value")

        def signature(side):
            states = side.get("ownerTaskStates") or {}
            before, after = states.get("before"), states.get("after")
            if before is None or after is None:
                return None
            return {"before": {"stage": before.get("stage"),
                                "taskNumber": before.get("taskNumber"),
                                "priority": before.get("priority"),
                                "signalsReceived": before.get("signalsReceived")},
                    "after": {"stage": after.get("stage"),
                              "taskNumber": after.get("taskNumber"),
                              "priority": after.get("priority"),
                              "signalsReceived": after.get("signalsReceived")}}

        def projection(states):
            if states is None:
                return None
            selected = {}
            for phase in ("before", "after"):
                state = states[phase]
                item = {"stage": state.get("stage"),
                        "taskNumber": state.get("taskNumber")}
                if expected_owner_priority_before is not None:
                    item["priority"] = state.get("priority")
                if expected_owner_task_signal_bit is not None:
                    signals = state.get("signalsReceived")
                    item["signalBitSet"] = (None if signals is None else
                                             bool(signals & (1 << expected_owner_task_signal_bit)))
                if expected_owner_task_signal_mask is not None:
                    signals = state.get("signalsReceived")
                    item["signalMaskBitsSet"] = (None if signals is None else
                                                  signals & expected_owner_task_signal_mask)
                selected[phase] = item
            return selected

        expected = {"before": {"stage": 3,
                               "taskNumber": expected_owner_task_number},
                    "after": {"stage": 100,
                              "taskNumber": expected_owner_task_number}}
        if expected_owner_priority_before is not None:
            expected["before"]["priority"] = expected_owner_priority_before
            expected["after"]["priority"] = expected_owner_priority_after
        if expected_owner_task_signal_bit is not None:
            expected["before"]["signalBitSet"] = False
            expected["after"]["signalBitSet"] = True
        if expected_owner_task_signal_mask is not None:
            expected["before"]["signalMaskBitsSet"] = 0
            expected["after"]["signalMaskBitsSet"] = expected_owner_task_signal_mask

        original_effect, candidate_effect = signature(original), signature(candidate)
        original_projection = projection(original_effect)
        candidate_projection = projection(candidate_effect)
        raw_equal = (original_projection is not None and
                     original_projection == candidate_projection)
        expected_equal = (original_projection == expected and
                          candidate_projection == expected)
        effect_equal = admitted and raw_equal and expected_equal
        comparison["ownerTaskEffect"] = {
            "original": original_effect,
            "candidate": candidate_effect,
            "originalProjection": original_projection,
            "candidateProjection": candidate_projection,
            "expected": expected,
            "rawValuesEqual": raw_equal,
            "expectedValuesMatch": expected_equal,
            "equal": effect_equal,
        }
        if original_effect is None or candidate_effect is None:
            errors.append("Owner task effect lacks a sampled pre-System or terminal state")
        elif not expected_equal:
            errors.append("Owner task before/after state differs from the expected target effect")
        equal = equal and effect_equal
    return {"schemaVersion": 1, "kind": "workbench31-single-case-original-candidate-comparison",
            "status": "captured-case-equal" if equal else "captured-case-mismatch" if admitted else "pair-rejected",
            "pairedEvidenceAdmitted": admitted, "commandResultEqual": command_equal, "allObservedFieldsEqual": equal,
            "errors": errors, "comparison": comparison, "original": original, "candidate": candidate,
            "tokenPolicy": "Each token is checked against its own invocation; diagnostic tokens may differ. Command text is compared exactly.",
            "postSystemIoErrMeaning": "Compared raw as caller post-System IoErr only; not proof of child pr_Result2 equality.",
            "scope": "Equality, if established, covers only this exact invocation/output/return/caller-error observation in this bounded guest profile. No full command, resident/PURE, child secondary-error or shipping qualification.",
            "normalization": None, "fullCommandParityClaim": False, "pureAdmission": False, "shipping": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--original-analysis", type=Path, required=True)
    parser.add_argument("--candidate-analysis", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--expect-owner-task-number", type=int)
    parser.add_argument("--expect-owner-priority-before", type=int)
    parser.add_argument("--expect-owner-priority-after", type=int)
    parser.add_argument("--expect-owner-task-signal-bit", type=int)
    parser.add_argument("--expect-owner-task-signal-mask",
                        type=lambda value: int(value, 0))
    args = parser.parse_args()
    if args.output.exists() or not args.output.parent.is_dir():
        parser.error("Comparison output must be a fresh file under an existing directory")
    effects = (args.expect_owner_task_number, args.expect_owner_priority_before,
               args.expect_owner_priority_after,
               args.expect_owner_task_signal_bit,
               args.expect_owner_task_signal_mask)
    priorities = effects[1:3]
    if any(value is not None for value in effects):
        if args.expect_owner_task_number is None:
            parser.error("Owner task number is required for an owner task effect")
        if (priorities[0] is None) != (priorities[1] is None):
            parser.error("Both expected owner priorities must be supplied together")
        if (priorities[0] is None and args.expect_owner_task_signal_bit is None
                and args.expect_owner_task_signal_mask is None):
            parser.error("Specify an expected priority transition or signal effect")
    if (args.expect_owner_task_number is not None and args.expect_owner_task_number < 1
            or any(value is not None and not -128 <= value <= 127
                   for value in priorities)
            or args.expect_owner_task_signal_bit is not None and
            not 0 <= args.expect_owner_task_signal_bit <= 31
            or args.expect_owner_task_signal_mask is not None and
            not 1 <= args.expect_owner_task_signal_mask <= 0xFFFFFFFF):
        parser.error("Task number must be positive, priorities -128..127, signal bit 0..31, and signal mask 1..0xFFFFFFFF")
    report = compare_sides(load_side(args.original_analysis, False),
                           load_side(args.candidate_analysis, True), *effects)
    report["comparator"] = identity(Path(__file__))
    report["decoder"] = identity(Path(analyze_probe.__file__))
    report["captureValidator"] = identity(Path(__file__).with_name("capture_probe.py"))
    report["inventoryReader"] = identity(INVENTORY_PATH)
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": report["status"], "output": str(args.output), "comparison": report["comparison"],
                      "errors": report["errors"], "originalErrors": report["original"]["errors"],
                      "candidateErrors": report["candidate"]["errors"]}, indent=2))
    return 0 if report["status"] == "captured-case-equal" else 1


if __name__ == "__main__":
    sys.exit(main())
