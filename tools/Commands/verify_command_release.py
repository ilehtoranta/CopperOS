"""Read-only CC08 release preflight. This is not an image builder or P authority.

The versioned MakeLink producer supports native-static and reproducible-build
gates. Remaining release gates still lack complete qualifying producers/adapters.
Bounded vector reports cannot discharge those gates. See release-preflight.md.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import sys
from typing import Any


PROFILES = {"wb31", "morphos320"}
CPUS = {"68000", "68020", "68040"}
DISTRIBUTIONS = {"normal-runtime", "installation-media"}
BASE_GATES = (
    "original-metadata-placement", "full-option-semantics", "native-static",
    "reproducible-build", "native-startup", "kickstart31-execution",
    "copperstart-execution", "reference-differential", "resource-lifetime",
    "minimum-stack", "dependency-integration",
)
PURE_GATES = ("shared-image-purity", "resident-lifecycle")
SHA256 = re.compile(r"[0-9a-f]{64}\Z")
VERSION = re.compile(r"\$VER: ([^\s]+) [0-9]+\.[0-9]+(?: [^\r\n\x00]+)?\Z")
# These are existing, explicitly bounded reports, not invented release schemas.
VECTOR_SUITES = {
    "workbench-rename-startup-vector-fixture": ("rename", "wb31"),
    "morphos-rename-native-entry-vector-fixture": ("rename", "morphos320"),
    "workbench-makelink-native-entry-vector-fixture": ("makelink", "wb31"),
    "makelink-native-entry-vector-fixture": ("makelink", "morphos320"),
}
VECTOR_CASES = {
    ("rename", "wb31"): {"missing-dos": 20, "workbench": 10, "direct-success": 0, "direct-pattern-failure": 20},
    ("rename", "morphos320"): {"missing-dos": 20, "workbench": 10, "direct-success": 0, "parser-error": 10},
    ("makelink", "wb31"): {"missing-dos": 20, "workbench": 10, "default-hard-file": 0, "parser-failure": 20},
    ("makelink", "morphos320"): {"missing-dos": 20, "workbench": 10, "soft-success": 0, "parser-failure": 20},
}


def sha256(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def load_json(path: Path) -> Any:
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError(f"Duplicate JSON key: {key}")
            result[key] = value
        return result
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=unique)


def integer(value: Any, minimum: int = 0) -> bool:
    return type(value) is int and value >= minimum


class EvidenceError(ValueError):
    pass


def bound_file(reference: Any, root: Path, label: str) -> Path:
    """Resolve a local evidence file and verify its supplied digest, never fetch it."""
    if not isinstance(reference, dict) or set(reference) != {"path", "sha256"}:
        raise EvidenceError(f"{label}: expected exactly path and sha256")
    name, digest = reference["path"], reference["sha256"]
    if not isinstance(name, str) or not name.strip() or "\x00" in name:
        raise EvidenceError(f"{label}: missing local path")
    if not isinstance(digest, str) or not SHA256.fullmatch(digest):
        raise EvidenceError(f"{label}: missing or malformed SHA-256")
    path = Path(name)
    if not path.is_absolute():
        path = root / path
    try:
        path = path.resolve(strict=True)
        if not path.is_file() or sha256(path) != digest:
            raise EvidenceError(f"{label}: stale hash or not a file: {name}")
    except OSError as error:
        raise EvidenceError(f"{label}: unavailable file: {name}") from error
    return path


def verify_native_vector(report: dict, record: dict, root: Path) -> dict:
    """Validate existing report identity/shape and its limited coverage honestly.

    This checks a captured report and runner identities; it does not re-execute
    the runner or establish the truth/completeness of a fabricated capture.
    """
    def require(condition, message):
        if not condition:
            raise EvidenceError(message)

    require(isinstance(report, dict), "native-vector report is not an object")
    require(type(report.get("schemaVersion")) is int and report["schemaVersion"] == 1,
            "unsupported native-vector schemaVersion")
    identity = VECTOR_SUITES.get(report.get("suite"))
    require(identity == (record.get("command"), record.get("profile")),
            "native-vector suite does not bind this command/profile")
    require(report.get("cpu") == record.get("cpu"), "native-vector CPU mismatch")
    require(report.get("imageSha256") == record.get("sha256") and
            report.get("imageBytes") == record.get("bytes"), "native-vector binary mismatch")
    require(report.get("status") == "passed", "native-vector execution failed")
    require(type(report.get("imageLoads")) is int and report["imageLoads"] == 1,
            "native-vector evidence is not one loaded image")
    require(type(report.get("sharedImageWrites")) is int and report["sharedImageWrites"] == 0,
            "native-vector shared image modified or count absent")
    for flag in ("realKickstartExecution", "realCopperStartExecution", "realDosParser",
                 "realDosIo", "referenceCommandBehavior", "shippingOrPureApproval",
                 "minimumStackQualified"):
        require(report.get(flag) is False, f"native-vector {flag} exceeds known schema scope")
    for prefix in ("managedExecutor", "instructionCore"):
        bound_file({"path": report.get(prefix + "Path"),
                    "sha256": report.get(prefix + "Sha256")}, root, prefix)
    cases = report.get("cases")
    require(isinstance(cases, list) and len(cases) >= 2, "native-vector cases absent")
    require(integer(report.get("passed")) and report["passed"] == len(cases),
            "native-vector passed count does not equal case coverage")
    names, interleaved = set(), set()
    for case in cases:
        require(isinstance(case, dict), "native-vector case is not an object")
        name = case.get("name")
        require(isinstance(name, str) and bool(name), "native-vector case identity absent")
        names.add(name)
        require(type(case.get("instructionInterleaved")) is bool,
                "native-vector interleaving evidence absent")
        if case["instructionInterleaved"]:
            interleaved.add(name)
        require(integer(case.get("instructions"), 1), "native-vector case did not execute")
        require(type(case.get("result")) is int and type(case.get("ioErr")) is int,
                "native-vector case result/IoErr absent")
        require(integer(case.get("configuredStackBytes"), 1) and
                integer(case.get("stackBytesWritten")) and
                case["stackBytesWritten"] <= case["configuredStackBytes"],
                "native-vector case stack bounds invalid")
        require(isinstance(case.get("events"), list) and bool(case["events"]),
                "native-vector case has no public-boundary observations")
        output = case.get("stdoutHex")
        require(isinstance(output, str) and re.fullmatch(r"(?:[0-9a-fA-F]{2})*", output) is not None,
                "native-vector output bytes malformed")
    require(len(names) >= 2 and len(interleaved) >= 2,
            "native-vector coverage needs distinct interleaved callers")
    for name, result in VECTOR_CASES[identity].items():
        found = [case for case in cases if case["name"] == name]
        require(bool(found) and all(case["result"] == result for case in found),
                f"native-vector missing or inconsistent required case: {name}")
    return {"report_type": "copperos-native-vector-v1", "suite": report["suite"],
            "cases": len(cases), "distinct_cases": len(names),
            "coverage": ["supplied-vector-execution", "reported-single-image-interleaving"],
            "satisfies_release_gates": []}


def preflight(record: Any, inventory: Any, root: Path, expected: dict) -> dict:
    """Check one proposed record against independent caller selection and inventory.

    Retain manifest v1 field names; release_record_version distinguishes the new
    metadata requirements from historical qualification-only artifact records.
    No filename, status string, or P boolean is a qualification certificate.
    """
    errors, blockers, supporting, passed_gates = [], [], [], []
    build_evidence_cache = {}

    def error(code, detail):
        errors.append({"code": code, "detail": detail})

    def check_file(reference, label):
        try:
            return bound_file(reference, root, label)
        except (EvidenceError, ValueError, TypeError) as failure:
            error("file-binding", str(failure))
            return None

    if not isinstance(record, dict):
        return {"schema_version": 1, "status": "rejected", "eligible_for_staging": False,
                "metadata_valid": False, "errors": [{"code": "record-schema", "detail": "Record is not an object"}],
                "blockers": [], "supporting_evidence": []}
    if type(record.get("release_record_version")) is not int or record["release_record_version"] != 1:
        error("record-schema", "Missing or unsupported release_record_version; historical records are not releases")
    if record.get("shipping") is not True or record.get("status") in ("development-only", "qualification-only"):
        error("not-release", "Development and qualification-only artifacts cannot be staged")
    for field in ("command", "profile", "cpu", "distribution", "installed_path"):
        if not isinstance(expected.get(field), str) or not expected[field]:
            error("selection", f"Independent expected {field} is required")
        elif record.get(field) != expected[field]:
            error("identity", f"Record {field} does not match requested selection")
    if record.get("profile") not in PROFILES or record.get("cpu") not in CPUS:
        error("identity", "Unsupported command profile or CPU")
    if record.get("distribution") not in DISTRIBUTIONS:
        error("placement", "Unknown distribution role")

    command = record.get("command")
    matches = []
    if isinstance(inventory, dict) and type(inventory.get("schema_version")) is int and inventory["schema_version"] == 1:
        matches = [row for row in inventory.get("commands", []) if isinstance(row, dict) and row.get("id") == command]
    else:
        error("inventory", "Unsupported inventory schema")
    profile = None
    if len(matches) != 1 or matches[0].get("kind") != "external":
        error("inventory", "Command must identify exactly one external inventory row")
    else:
        profile = matches[0].get("reference_profiles", {}).get(record.get("profile"))
        if not isinstance(profile, dict) or profile.get("required") is not True:
            error("inventory", "Command does not belong to this reference profile")
            profile = None
        names = {entry.get("original_name") for entry in (profile or {}).get("source_files", [])}
        install = record.get("installed_path")
        if not isinstance(install, str) or not re.fullmatch(r"C/[A-Za-z0-9_.-]+", install):
            error("placement", "Installed path must be a relative image path C/<original name>")
        elif PurePosixPath(install).name not in names:
            error("placement", "Installed basename/spelling is absent from profile source metadata")
    # Unknown original purity is an open gate, never equivalent to non-pure.
    original_pure = (profile or {}).get("purity", {}).get("required_by_observed_design_evidence") is True
    if type(record.get("required_pure")) is not bool:
        error("pure-policy", "Explicit required_pure metadata is missing")
    if original_pure and record.get("required_pure") is not True:
        error("pure-policy", "Record attempts to downgrade an observed pure/resident requirement")
    bits = record.get("amiga_protection")
    if not integer(bits) or bits > 255:
        error("protection", "Explicit Amiga protection byte is missing or invalid")
    needs_purity = original_pure or record.get("required_pure") is True or (integer(bits) and bool(bits & 32))
    if needs_purity and record.get("pure_admission") is not True:
        error("pure-policy", "Required or requested P has no qualified purity declaration; reports still required")

    binary = check_file({"path": record.get("path"), "sha256": record.get("sha256")}, "artifact")
    if not integer(record.get("bytes"), 1):
        error("binary-metadata", "Positive artifact byte count required")
    if record.get("fpu") != "disabled" or record.get("output_format") != "hunk":
        error("binary-metadata", "This verifier supports freestanding no-FPU HUNK release records only")
    if not isinstance(record.get("entry"), str) or "::" not in record["entry"]:
        error("binary-metadata", "Native entry identity missing")
    stack = record.get("minimum_stack_bytes")
    if not integer(stack, 4) or stack % 4:
        error("minimum-stack", "Positive four-byte-aligned minimum stack metadata is required")
    version = record.get("version_id")
    match = VERSION.fullmatch(version) if isinstance(version, str) else None
    if not match or match[1].casefold() != command:
        error("version", "A command-specific $VER: identifier is required")
    if binary:
        contents = binary.read_bytes()
        if len(contents) != record.get("bytes") or contents[:4] != b"\0\0\x03\xf3":
            error("binary-metadata", "Artifact size or HUNK header mismatch (not a complete HUNK validation)")
        try:
            if not match or version.encode("latin-1") + b"\0" not in contents:
                error("version", "Exact NUL-terminated version ID absent from artifact")
        except UnicodeEncodeError:
            error("version", "Version ID is not Latin-1")

    for field in ("source", "compiler", "sdk", "input_manifest"):
        check_file(record.get(field), field)
    inputs = record.get("inputs")
    if not isinstance(inputs, list) or not inputs:
        error("input-closure", "Hash-bound build inputs are required; closure still needs a reproducibility gate")
    else:
        paths = []
        for index, reference in enumerate(inputs):
            path = check_file(reference, f"inputs[{index}]")
            if path:
                paths.append(str(path).casefold())
        if len(paths) != len(set(paths)):
            error("input-closure", "Duplicate build input path")
    dependencies = record.get("dependencies")
    if not isinstance(dependencies, list) or not dependencies:
        error("dependencies", "Public library/provider dependency metadata is required")
    else:
        ids = []
        for dependency in dependencies:
            if not isinstance(dependency, dict) or set(dependency) != {"id", "minimum_version", "abi"}:
                error("dependencies", "Dependency requires exactly id, minimum_version and ABI file binding")
                continue
            if not isinstance(dependency["id"], str) or not dependency["id"] or not integer(dependency["minimum_version"]):
                error("dependencies", "Invalid dependency identity or minimum version")
            else:
                ids.append(dependency["id"].casefold())
            check_file(dependency["abi"], "dependency ABI")
        if len(ids) != len(set(ids)) or not {"exec.library", "dos.library"}.issubset(ids):
            error("dependencies", "Duplicate dependencies or missing public Exec/DOS dependencies")

    evidence = record.get("evidence")
    if not isinstance(evidence, dict):
        error("gate-evidence", "Explicit gate evidence map missing")
        evidence = {}
    required = BASE_GATES + (PURE_GATES if needs_purity else ())
    from verify_command_build import GATES as BUILD_GATES, REPORT_TYPE, verify_versioned_build
    for gate in required:
        item = evidence.get(gate)
        if item is None:
            blockers.append({"gate": gate, "reason": "missing-report"})
            continue
        if not isinstance(item, dict) or set(item) != {"report_type", "report"}:
            error("gate-evidence", f"{gate}: report_type and hash-bound report are mandatory")
        else:
            report_path = check_file(item["report"], gate)
            if report_path and gate in BUILD_GATES and item["report_type"] == REPORT_TYPE:
                try:
                    if report_path not in build_evidence_cache:
                        build_evidence_cache[report_path] = verify_versioned_build(load_json(report_path), record, root)
                    result = build_evidence_cache[report_path]
                    if gate not in result["satisfies_release_gates"]:
                        raise EvidenceError("Build adapter does not establish " + gate)
                    passed_gates.append({"gate": gate, "report": item["report"],
                                         "validation": result})
                    continue
                except (EvidenceError, ValueError, TypeError, KeyError, IndexError, OSError) as failure:
                    error("gate-evidence", f"{gate}: {failure}")
                    blockers.append({"gate": gate, "reason": "invalid-release-report"})
                    continue
        # Unimplemented gate schemas remain blocked. A valid report from the
        # build producer may discharge only its two explicitly supported gates.
        blockers.append({"gate": gate, "reason": "unsupported-release-report-schema"})
    for gate in evidence.keys() - set(required) - {"native-vector"}:
        error("gate-evidence", f"Unknown or inapplicable evidence gate: {gate}")
    item = evidence.get("native-vector")
    if item is not None:
        if not isinstance(item, dict) or set(item) != {"report_type", "report"} or item["report_type"] != "copperos-native-vector-v1":
            error("supporting-evidence", "Unknown native-vector report type")
        else:
            report_path = check_file(item["report"], "native-vector")
            if report_path:
                try:
                    supporting.append(verify_native_vector(load_json(report_path), record, root))
                except (EvidenceError, ValueError, TypeError, KeyError) as failure:
                    error("supporting-evidence", str(failure))
    eligible = not errors and not blockers
    return {"schema_version": 1, "status": "rejected" if errors else "eligible" if eligible else "blocked",
            "eligible_for_staging": eligible, "metadata_valid": not errors,
            "selection": expected, "errors": errors, "blockers": blockers,
            "supporting_evidence": supporting, "passed_gates": passed_gates,
            "scope": "Read-only preflight with native-static/reproducible-build adapters; remaining complete runtime/reference/purity/placement gates and image-builder integration are required"}


def preflight_manifest(manifest: Any, inventory: Any, root: Path, expected: dict) -> dict:
    """Select exactly one artifact; never fall back to qualification-only entries.

    Duplicate identities and case-insensitive image-path collisions anywhere in
    the submitted release set invalidate the set, even outside the selection.
    """
    errors = []
    records = []
    if not isinstance(manifest, dict) or type(manifest.get("schema_version")) is not int or manifest["schema_version"] != 1:
        errors.append({"code": "manifest-schema", "detail": "Unsupported build-manifest schema"})
    elif not isinstance(manifest.get("qualified_shipping_artifacts"), list):
        errors.append({"code": "manifest-schema", "detail": "qualified_shipping_artifacts must be an explicit list"})
    else:
        records = manifest["qualified_shipping_artifacts"]
    identities, destinations, selected = set(), set(), []
    for record in records:
        fields = ("command", "profile", "cpu", "distribution", "installed_path")
        if not isinstance(record, dict) or not all(isinstance(record.get(key), str) and record[key] for key in fields):
            errors.append({"code": "manifest-record", "detail": "Release record identity/placement fields missing"})
            continue
        identity = tuple(record[key].casefold() for key in fields[:4])
        destination = tuple(record[key].casefold() for key in ("profile", "cpu", "distribution", "installed_path"))
        if identity in identities:
            errors.append({"code": "duplicate-artifact", "detail": "Duplicate command/profile/CPU/distribution identity"})
        if destination in destinations:
            errors.append({"code": "duplicate-destination", "detail": "Case-insensitive image path collision"})
        identities.add(identity)
        destinations.add(destination)
        if all(record[key] == expected.get(key) for key in fields[:4]):
            selected.append(record)
    if len(selected) != 1:
        errors.append({"code": "artifact-selection", "detail": f"Expected exactly one release artifact, found {len(selected)}; no development fallback"})
    if errors:
        return {"schema_version": 1, "status": "rejected", "eligible_for_staging": False,
                "metadata_valid": False, "selection": expected, "errors": errors,
                "blockers": [], "supporting_evidence": []}
    return preflight(selected[0], inventory, root, expected)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--record", type=Path, help="One proposed release record JSON object")
    source.add_argument("--manifest", type=Path, help="Build-manifest v1; select only qualified_shipping_artifacts")
    parser.add_argument("--inventory", type=Path, required=True)
    parser.add_argument("--inventory-sha256", required=True, help="Pin caller-selected inventory; prevents silent replacement")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    for name in ("command", "profile", "cpu", "distribution", "installed-path"):
        parser.add_argument("--" + name, required=True)
    args = parser.parse_args(argv)
    try:
        inventory_path = bound_file({"path": str(args.inventory.resolve()), "sha256": args.inventory_sha256}, args.root, "inventory")
        verify = preflight_manifest if args.manifest else preflight
        result = verify(load_json(args.manifest or args.record), load_json(inventory_path), args.root,
                        {key: getattr(args, key) for key in ("command", "profile", "cpu", "distribution", "installed_path")})
    except (ValueError, OSError, TypeError) as failure:
        result = {"schema_version": 1, "status": "rejected", "eligible_for_staging": False,
                  "metadata_valid": False, "errors": [{"code": "input", "detail": str(failure)}]}
    print(json.dumps(result, indent=2))
    return 0 if result["eligible_for_staging"] else 1


if __name__ == "__main__":
    sys.exit(main())
