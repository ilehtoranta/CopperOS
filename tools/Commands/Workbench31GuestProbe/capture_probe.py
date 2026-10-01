"""Run the existing public-only passive runner against a disposable probe ADF.

This wrapper changes no media, engine, guest memory, program counter or vectors.
It records capture completion only; analyze_probe.py owns probe-record validation.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
from datetime import datetime, timezone


def identity(path: Path) -> dict:
    path = path.resolve(strict=True)
    if not path.is_file():
        raise ValueError(f"Expected input file: {path}")
    data = path.read_bytes()
    return {"path": str(path), "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}


def source_files(paths: list[Path]) -> list[Path]:
    files = set()
    for source in paths:
        source = source.resolve(strict=True)
        if source.is_file():
            files.add(source)
        else:
            selected = [path for path in source.rglob("*") if path.is_file()
                        and path.suffix.lower() in {".cs", ".csproj", ".py", ".ps1", ".json", ".md"}
                        and not {"bin", "obj", ".git", "__pycache__"}.intersection(path.relative_to(source).parts)]
            if not selected:
                raise ValueError(f"Empty source selection: {source}")
            files.update(selected)
    if not files:
        raise ValueError("At least one explicit source input is required")
    return sorted(files)


def bind_command_replacement(preparation, candidate_path, requested_guest_path=None):
    """Validate one explicitly named diagnostic C command substitution."""
    replacement = preparation.get("replacement")
    if replacement is None:
        if candidate_path is not None:
            raise ValueError("Candidate input supplied without a replacement preparation receipt")
        if requested_guest_path is not None:
            raise ValueError("Replacement target supplied without a replacement preparation receipt")
        return None, []
    if candidate_path is None:
        raise ValueError("Replacement preparation requires its explicit candidate HUNK input")
    guest_path = replacement.get("guest_path")
    if (not isinstance(guest_path, str) or
            re.fullmatch(r'C/[A-Za-z0-9_][A-Za-z0-9_.-]{0,63}', guest_path) is None
            or (requested_guest_path is not None and requested_guest_path != guest_path)
            or replacement.get("experimental") is not True
            or replacement.get("pure_admission") is not False or replacement.get("shipping") is not False
            or replacement.get("original_metadata_preserved") is not True):
        raise ValueError("Unsupported or misclassified candidate substitution")
    candidate_path = Path(candidate_path).resolve(strict=True)
    candidate = identity(candidate_path)
    if replacement.get("candidate") != candidate:
        raise ValueError("Explicit candidate input differs from preparation candidate identity")
    snapshot = replacement.get("snapshot", {})
    snapshot_path = Path(snapshot["path"]).resolve(strict=True)
    if snapshot != identity(snapshot_path):
        raise ValueError("Candidate snapshot identity changed")
    if (snapshot["bytes"], snapshot["sha256"]) != (candidate["bytes"], candidate["sha256"]):
        raise ValueError("Candidate snapshot differs from explicit candidate input")
    if snapshot_path.parent != Path(preparation["derived_adf"]["path"]).resolve().parent:
        raise ValueError("Candidate snapshot is outside its derivative directory")
    original = replacement.get("original_command", {})
    if (original.get("path") != guest_path or type(original.get("bytes")) is not int
            or original["bytes"] <= 0 or not isinstance(original.get("sha256"), str)
            or len(original["sha256"]) != 64 or not isinstance(original.get("metadata"), dict)):
        raise ValueError("Missing original command identity/metadata")
    if (original["bytes"], original["sha256"]) == (candidate["bytes"], candidate["sha256"]):
        raise ValueError("Candidate substitution is identical to the original command")
    return replacement, [candidate_path, snapshot_path]


def bind_version_replacement(preparation, candidate_path):
    """Backward-compatible Version-specific candidate admission."""
    replacement = preparation.get("replacement")
    if replacement is not None and replacement.get("guest_path") != "C/Version":
        raise ValueError("Unsupported or misclassified Version replacement")
    return bind_command_replacement(preparation, candidate_path,
                                    "C/Version" if replacement is not None else None)


def bind_qualified_runtime(qualification_path, runtime_files):
    """Cross-bind retained replay/source evidence; never treat fresh hashes as build provenance."""
    qualification_path = qualification_path.resolve(strict=True)
    qualification = json.loads(qualification_path.read_text(encoding="utf-8-sig"))
    if (qualification.get("schemaVersion") != 1 or qualification.get("status")
            != "passive-slowram-property-regression-and-bounded-replay-passed"):
        raise ValueError("Expected the retained passive-runner qualification")
    evidence = qualification.get("evidenceIdentities", [])
    capture_ids = [item for item in evidence if Path(item["path"]).name == "capture-receipt.json"]
    source_ids = [item for item in evidence if Path(item["path"]).name == "capture-source-binding.json"]
    if len(capture_ids) != 1 or len(source_ids) != 1:
        raise ValueError("Qualification does not uniquely bind capture/source-PDB receipts")
    proof_paths = [qualification_path]
    for item in capture_ids + source_ids:
        if identity(Path(item["path"])) != item:
            raise ValueError("Retained runtime evidence identity changed")
        proof_paths.append(Path(item["path"]))
    capture = json.loads(Path(capture_ids[0]["path"]).read_text(encoding="utf-8-sig"))
    if not (capture.get("inputsUnchanged") is True and capture.get("process", {}).get("exitCode") == 0
            and capture.get("baseline", {}).get("fingerprintsEqual") is True
            and capture.get("baseline", {}).get("allComparedSnapshotsEqual") is True):
        raise ValueError("Retained runner capture lacks its successful unchanged-baseline evidence")
    previous = {item["path"]: item for item in capture.get("inputsAfter", [])}
    for path in runtime_files:
        actual = identity(path)
        if previous.get(actual["path"]) != actual:
            raise ValueError(f"Runtime does not match qualified bytes/path: {path}")
    source_rows = json.loads(Path(source_ids[0]["path"]).read_text(encoding="utf-8-sig"))
    checked_sources = []
    for row in source_rows:
        if row.get("actual") is None:
            continue  # The original receipt explicitly excludes generated sources.
        path = Path(row["localPath"])
        if not row.get("matches") or identity(path)["sha256"] != row["expected"]:
            raise ValueError("Engine source bytes no longer match retained PDB document identity")
        proof_paths.append(path)
        checked_sources.append(identity(path))
    if len(checked_sources) != 21:
        raise ValueError("Expected all 21 bound engine source documents")
    return proof_paths, {"qualification": identity(qualification_path), "priorCapture": capture_ids[0],
                         "engineSourcePdbReceipt": source_ids[0], "checkedEngineSources": checked_sources,
                         "scope": "Runtime bytes match prior qualified paths/bytes; all 21 engine source PDB document hashes match. Additional --source inputs are current provenance only, not proof of compilation."}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("runner", "runner-qualification", "rom", "original-adf", "derived-adf", "probe", "derivative-receipt", "output"):
        parser.add_argument("--" + name, type=Path, required=True)
    parser.add_argument("--source", type=Path, action="append", required=True,
                        help="Explicit source file or source-only tree; repeat for engine, runner, probe and builder")
    parser.add_argument("--frames", type=int, default=2400)
    parser.add_argument("--timeout-seconds", type=int, default=60)
    parser.add_argument("--expected-token", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--version-replacement", type=Path,
                        help="Explicit candidate HUNK, required only for prepared C/Version substitution")
    parser.add_argument("--command-replacement", type=Path,
                        help="Explicit candidate HUNK, required for another prepared C command substitution")
    parser.add_argument("--command-replacement-path",
                        help="Exact command target path such as C/Break")
    args = parser.parse_args()
    if not 1 <= args.frames <= 100000 or not 1 <= args.timeout_seconds <= 60:
        parser.error("Use 1..100000 frames and a bounded timeout of 1..60 seconds")
    if not 0 <= args.expected_token <= 0xffffffff:
        parser.error("Expected token must be unsigned 32-bit")
    original = args.original_adf.resolve(strict=True)
    derived = args.derived_adf.resolve(strict=True)
    repo = Path(__file__).resolve().parents[3]
    output = args.output.resolve()
    if original == derived or original.samefile(derived):
        parser.error("Original and derivative media must be separate files")
    if derived.is_relative_to(repo):
        parser.error("Licensed derivative media must remain outside the repository")
    if output.exists() or not output.parent.is_dir():
        parser.error("Output must be a fresh path under an existing diagnostic parent directory")
    if output in (original.parent, derived.parent):
        parser.error("Output must not be a media input directory")
    if original.stat().st_size != 901120 or derived.stat().st_size != 901120:
        parser.error("This runner profile requires standard 880 KiB ADF images")
    if identity(original)["sha256"] == identity(derived)["sha256"]:
        parser.error("Derivative is identical to original; no probe-image change is bound")
    preparation = json.loads(args.derivative_receipt.read_text(encoding="utf-8-sig"))
    if (preparation.get("schema_version") != 1 or preparation.get("kind") != "workbench31-diagnostic-probe-adf"
            or preparation.get("status") != "prepared"):
        parser.error("Expected a successful diagnostic probe-image preparation receipt")
    for role, path in (("source_adf", original), ("derived_adf", derived), ("probe", args.probe)):
        actual = identity(path)
        bound = preparation.get(role, {})
        if actual["sha256"] != bound.get("sha256") or actual["bytes"] != bound.get("bytes"):
            parser.error(f"Preparation receipt does not bind {role} bytes")
        if role != "probe" and Path(bound.get("path", "")).resolve() != path:
            parser.error(f"Preparation receipt {role} path differs from capture input")
    if not (preparation.get("source_before_sha256") == preparation.get("source_after_sha256")
            == preparation["source_adf"]["sha256"]):
        parser.error("Preparation receipt does not retain unchanged original media")
    invocation = preparation.get("invocation", {})
    if (type(invocation.get("token")) is not int or invocation["token"] & 0xffffffff != args.expected_token
            or not isinstance(invocation.get("command"), str) or not invocation["command"]
            or invocation.get("guest_path") != "C/CopperProbe"):
        parser.error("Preparation receipt invocation/token does not match the requested probe")
    if args.version_replacement is not None and args.command_replacement is not None:
        parser.error('Supply only one candidate replacement input')
    if args.version_replacement is not None:
        replacement, replacement_paths = bind_version_replacement(preparation, args.version_replacement)
    else:
        replacement, replacement_paths = bind_command_replacement(
            preparation, args.command_replacement, args.command_replacement_path)
    runner = args.runner.resolve(strict=True)
    dotnet_path = shutil.which("dotnet")
    if dotnet_path is None:
        parser.error("dotnet is unavailable")
    dotnet = Path(dotnet_path).resolve(strict=True)
    required_runtime = [runner, runner.with_suffix(".deps.json"), runner.with_suffix(".runtimeconfig.json"),
                        runner.parent / "CopperMod.Amiga.Lightweight.dll", runner.parent / "Copper68k.dll",
                        runner.parent / "CopperDisk.dll", runner.parent / "CopperFloat.dll"]
    for runtime in required_runtime:
        identity(runtime)
    runtime_files = sorted(path for path in runner.parent.iterdir()
                           if path.is_file() and path.suffix.lower() in {".dll", ".json", ".pdb"})
    proof_paths, runtime_binding = bind_qualified_runtime(args.runner_qualification, runtime_files)
    fixed = [args.rom, original, derived, args.probe, args.derivative_receipt, dotnet,
             Path(sys.executable), Path(__file__), *runtime_files, *proof_paths, *replacement_paths]
    def inputs():
        return [identity(path) for path in sorted({path.resolve(strict=True) for path in fixed + source_files(args.source)})]
    before = inputs()
    output.mkdir(exist_ok=False)
    arguments = [str(dotnet), str(runner), str(args.rom.resolve(strict=True)), str(derived), str(args.frames), str(output / "snapshots")]
    started = datetime.now(timezone.utc).isoformat()
    exit_code, process_error = None, None
    with (output / "stdout.txt").open("wb") as stdout, (output / "stderr.txt").open("wb") as stderr:
        try:
            result = subprocess.run(arguments, stdout=stdout, stderr=stderr, timeout=args.timeout_seconds,
                                    check=False, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
            exit_code = result.returncode
        except (OSError, subprocess.TimeoutExpired) as error:
            process_error = str(error)
    errors = []
    try:
        after = inputs()
    except (OSError, ValueError) as error:
        after = []
        errors.append(f"Cannot rebind inputs: {error}")
    unchanged = before == after
    if exit_code != 0:
        errors.append(f"Runner exit was not zero: {exit_code}; {process_error}")
    if not unchanged:
        errors.append("Input/runtime/source identities changed")
    snapshots = output / "snapshots"
    expected_frames = sorted({1, args.frames, *range(60, args.frames + 1, 60)})
    actual_frames = []
    summary = None
    try:
        for path in sorted(snapshots.glob("frame-*.json")):
            state = json.loads(path.read_text(encoding="utf-8-sig"))
            frame = state["frame"]
            if path.stem != f"frame-{frame:06d}" or state["CompletedFrames"] != frame:
                errors.append(f"Snapshot frame identity mismatch: {path.name}")
            actual_frames.append(frame)
            for suffix in (".chipram", ".slowram"):
                if path.with_suffix(suffix).stat().st_size != 512 * 1024:
                    errors.append(f"Invalid memory extent: {path.with_suffix(suffix).name}")
            if state.get("UnsupportedActiveFeature") is not None:
                errors.append(f"Unsupported active feature at frame {frame}: {state['UnsupportedActiveFeature']}")
        summary = json.loads((snapshots / "summary.json").read_text(encoding="utf-8-sig"))
        if summary["frames"] != args.frames or summary["CompletedFrames"] != args.frames:
            errors.append("Runner summary did not complete requested frames")
        if summary.get("UnsupportedActiveFeature") is not None:
            errors.append("Runner final summary reports an unsupported feature")
    except (OSError, ValueError, KeyError) as error:
        errors.append(f"Incomplete/invalid capture: {error}")
    if actual_frames != expected_frames:
        errors.append("Snapshot frame set differs from the requested runner schedule")
    snapshot_identities = [identity(path) for path in sorted(snapshots.glob("*")) if path.is_file()]
    receipt = {
        "schemaVersion": 1, "kind": "workbench31-guest-probe-passive-capture",
        "status": "capture-complete-not-probe-success" if not errors else "capture-rejected",
        "startedUtc": started, "finishedUtc": datetime.now(timezone.utc).isoformat(),
        "process": {"arguments": arguments, "exitCode": exit_code, "error": process_error},
        "requestedFrames": args.frames, "snapshotDirectory": str(snapshots),
        "roles": {"rom": str(args.rom.resolve()), "originalAdf": str(original), "derivedAdf": str(derived),
                  "probe": str(args.probe.resolve()), "derivativeReceipt": str(args.derivative_receipt.resolve())},
        "invocation": preparation["invocation"],
        "replacement": replacement,
        "sourceSelections": [str(path.resolve()) for path in args.source],
        "qualifiedRuntimeBinding": runtime_binding,
        "inputsBefore": before, "inputsAfter": after, "inputsUnchanged": unchanged,
        "expectedFrames": expected_frames, "summary": summary,
        "snapshotIdentities": snapshot_identities, "errors": errors,
        "scope": "Normal ROM/CLI guest execution using copied write-protected media and saved public chip/slow RAM; no host DOS services, guest bus reads, reflection, hooks or PC/vector writes.",
        "commandParityClaim": False,
    }
    receipt_path = output / "capture-receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": receipt["status"], "receipt": str(receipt_path), "errors": errors}, indent=2))
    return 0 if not errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
