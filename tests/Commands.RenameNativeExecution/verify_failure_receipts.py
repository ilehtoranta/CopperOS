#!/usr/bin/env python3
"""Exercise receipt/input protection without copying the licensed original.

This runs the already built package-only host executor. It creates disposable
invalid inputs and receipts only under the explicitly supplied private folder.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import uuid


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executor", type=Path, required=True)
    parser.add_argument("--private-original", type=Path, required=True)
    parser.add_argument("--artifacts", type=Path, required=True)
    options = parser.parse_args()
    lexical_repository = Path(__file__).absolute().parents[2]
    repository = lexical_repository.resolve()
    original = options.private_original.absolute()
    assert original.resolve().is_file() and not original.resolve().is_relative_to(repository)
    assert original.stat().st_size == 1140
    original_hash = digest(original)
    assert original_hash == "ebff9d5f1401ca67fe9542bde304c603d21366f81852716c236c5c7ca1e49579"
    root = options.artifacts.absolute() / ("receipt-negative-" + uuid.uuid4().hex)
    assert not root.resolve().is_relative_to(repository)
    root.mkdir(parents=True, exist_ok=False)
    invalid = root / "invalid-reference.json"
    invalid.write_bytes(b"not a licensed executable\n" + bytes(1114))
    assert invalid.stat().st_size == 1140
    short = root / "short-reference.hunk"
    short.write_bytes(b"invalid\n")
    loaded = [options.executor, options.executor.parent / "Copper68k.dll",
              options.executor.parent / "CopperSharp.Sdk.Amiga.dll"]
    binaries_before = {str(p.resolve()): digest(p) for p in loaded}
    cases = []
    suite = "rename-classic-original-vector-fixture"

    def run(case_id: str, source: Path, report: Path, *, cpu: str = "68000",
            selected_suite: str = suite, refused_write: bool = False,
            expected_text: str | None = None) -> None:
        watched = {p: digest(p) for p in [original, source] if p.is_file()}
        target_before = digest(report) if report.is_file() else None
        completed = subprocess.run(
            ["dotnet", str(options.executor), str(source), cpu, str(report), selected_suite],
            cwd=lexical_repository, capture_output=True, text=True, timeout=60, check=False)
        assert completed.returncode != 0, f"{case_id}: invalid execution passed"
        assert all(digest(p) == value for p, value in watched.items()), f"{case_id}: input changed"
        output = completed.stdout + completed.stderr
        if expected_text is not None:
            assert expected_text in output, f"{case_id}: wrong failure: {output}"
        if refused_write:
            assert (digest(report) if report.is_file() else None) == target_before, case_id
            receipt_hash = None
        else:
            receipt = json.loads(report.read_text(encoding="utf-8"))
            assert receipt["status"] == "failed" and receipt["originalNativeInvocationsStarted"] == 0
            assert receipt["originalReturnedCasesAccepted"] == 0 and receipt["originalExpectedGuardStopsObserved"] == 0
            assert receipt["generatedNativeInvocations"] == 0 and receipt["comparisonsPerformed"] == 0
            receipt_hash = digest(report)
        cases.append({"id": case_id, "passed": True, "exitCode": completed.returncode,
                      "report": str(report), "receiptSha256": receipt_hash,
                      "reportWriteRefused": refused_write,
                      "inputHashesUnchanged": {str(p): value for p, value in watched.items()},
                      "stdout": completed.stdout, "stderr": completed.stderr})

    run("missing-reference", root / "missing.hunk", root / "missing.json", expected_text="missing")
    run("wrong-size", short, root / "wrong-size.json", expected_text="1140-byte")
    run("wrong-hash", invalid, root / "wrong-hash.json", expected_text="hash mismatch")
    run("unknown-suite", original, root / "unknown-suite.json", selected_suite="unrecognised",
        expected_text="Unknown suite")
    run("unsupported-cpu", original, root / "unsupported-cpu.json", cpu="68060",
        expected_text="Unsupported native CPU")
    run("direct-input-report", invalid, invalid, refused_write=True, expected_text="overwrite")
    hardlink = root / "hardlink-report.json"
    os.link(invalid, hardlink)
    run("invalid-input-hardlink-report", invalid, hardlink, refused_write=True,
        expected_text="existing input")
    # Physical resolve traverses this workspace's mapped drive/junctions. This
    # case is still a safe direct-path refusal if a host has no such alias.
    run("physical-input-report", invalid, invalid.resolve(), refused_write=True,
        expected_text="input")
    marker = lexical_repository / "CopperOS.Portable.props"
    run("repository-reference", marker, root / "repository.json", expected_text="outside the repository")
    run("physical-repository-reference", marker.resolve(), root / "physical-repository.json",
        expected_text="outside the repository")
    replaceable = root / "replaceable-failure.json"
    replaceable.write_text('{"previous": true}\n', encoding="utf-8")
    run("replace-safe-existing-receipt", invalid, replaceable, expected_text="hash mismatch")
    run("refuse-non-json-output", invalid, root / "not-a-receipt.hunk", refused_write=True,
        expected_text="JSON path")
    binaries_after = {str(p.resolve()): digest(p) for p in loaded}
    assert binaries_before == binaries_after and digest(original) == original_hash
    result = {"schemaVersion": 1, "status": "passed", "checks": len(cases),
              "scope": "host-only-failure-receipt-and-reference-protection",
              "scriptSha256": digest(Path(__file__)), "originalSha256": original_hash,
              "originalPayloadCopied": False, "nativeInvocations": 0,
              "physicalAliasWasDistinct": str(invalid.absolute()).casefold() != str(invalid.resolve()).casefold(),
              "binariesBefore": binaries_before, "binariesAfter": binaries_after, "cases": cases}
    target = root / "safety-checks.json"
    target.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": result["status"], "checks": len(cases),
                      "receipt": str(target), "sha256": digest(target)}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
