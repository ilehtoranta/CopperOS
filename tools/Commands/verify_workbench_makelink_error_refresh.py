"""Bind the bounded MakeLink error comparison to a completed current-HUNK boot."""
import argparse
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from compare_workbench_makelink_errors import inspect
from verify_copy_boot_transfer import require


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def bind(observations, media, role, expected_binary):
    trace, receipt = read(observations), read(media)
    require(receipt["command_under_test"] == "MakeLink" and
            receipt["binary_role"] == role, "Wrong command or binary role")
    require(trace["fixtureReceiptSha256"].lower() == digest(media), "Stale media receipt")
    require(trace["fixtureImageSha256"].lower() == receipt["output_adf_sha256"] and
            trace["finalImageSha256"].lower() == receipt["output_adf_sha256"], "Fixture image binding")
    require(trace["imageSha256"].lower() == receipt["reference_adf_sha256"], "Reference disk binding")
    require(len(receipt["replacements"]) == 1, "Unexpected replacement set")
    replacement = receipt["replacements"][0]
    require(replacement["guest_path"] == "c/ed" and
            replacement["sha256"] == expected_binary, "Wrong command artifact")
    binary = Path(replacement["local_file"])
    require(binary.stat().st_size == replacement["bytes"] and digest(binary) == expected_binary,
            "Changed command artifact")
    return trace, receipt


def verify_trx(path, observations):
    tree = ET.parse(path)
    results = [node for node in tree.iter() if node.tag.endswith("}UnitTestResult")]
    require(len(results) == 1 and results[0].get("outcome") == "Passed", "Boot test did not pass")
    require(results[0].get("testName") ==
            "CopperMod.Amiga.Tests.KickstartRomLayersDifferentialTests.CopyDerivativeBootProgressV4063",
            "Wrong test case")
    prefix = "DOS passive boot readiness: "
    captures = [line[len(prefix):] for node in tree.iter() if node.tag.endswith("}StdOut")
                for line in (node.text or "").splitlines() if line.startswith(prefix)]
    require(len(captures) == 1 and json.loads(captures[0]) == observations, "TRX/observation mismatch")


def verify(args):
    reference, reference_media = bind(args.reference, args.reference_media, "reference",
        "c24b0af713e6f3a252d964c4545da29535bf6652394879a7dc4c57fdce7c02de")
    candidate, candidate_media = bind(args.candidate, args.candidate_media, "candidate",
        args.expected_candidate_sha256)
    expected, actual = inspect(reference, False), inspect(candidate, True)
    require(expected == actual, "Reference/candidate behavioral difference")
    require(reference_media["probe_sha256"] == candidate_media["probe_sha256"] and
            reference_media["probe_bytes"] == candidate_media["probe_bytes"], "Changed startup scenario")
    for name in ("archiveSha256", "imageSha256", "romSha256", "emulatorAssemblySha256", "testAssemblySha256"):
        require(reference[name].lower() == candidate[name].lower(), "Changed runtime input: " + name)
    require(candidate["testAssemblySha256"].lower() == digest(args.test_assembly), "Changed observer assembly")
    verify_trx(args.reference_trx, reference)
    verify_trx(args.candidate_trx, candidate)

    paths = {"reference_observations": args.reference, "candidate_observations": args.candidate,
             "reference_media": args.reference_media, "candidate_media": args.candidate_media,
             "reference_trx": args.reference_trx, "candidate_trx": args.candidate_trx, "test_assembly": args.test_assembly,
             "behavior_verifier": Path(__file__).with_name("compare_workbench_makelink_errors.py"),
             "binding_verifier": Path(__file__)}
    return {"status": "current-hunk-workbench-makelink-error-comparison-passed",
            "scope": "Five identical original-DOS startup invocations: success, parser failure, missing target, duplicate link, recovery; primary returns, fault inputs and format strings, link decisions, candidate ownership, unchanged shared image and exact alias readbacks. Not rendered diagnostic bytes, final IoErr, full startup/profile/purity or shipping qualification.",
            "cases": actual, "candidate_binary_sha256": args.expected_candidate_sha256,
            "candidate_binary_bytes": candidate_media["replacements"][0]["bytes"],
            "test_assembly_sha256": candidate["testAssemblySha256"].lower(),
            "emulator_assembly_sha256": candidate["emulatorAssemblySha256"].lower(),
            "test_status": "Passed", "shipping_qualified": False, "full_profile_parity": False,
            "evidence": {name: {"path": str(path), "sha256": digest(path)} for name, path in paths.items()}}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("reference", "candidate", "reference_media", "candidate_media", "reference_trx", "candidate_trx", "test_assembly"):
        parser.add_argument(name, type=Path)
    parser.add_argument("--expected-candidate-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    require(not args.output.exists(), "Refusing to overwrite historical evidence")
    result = verify(args)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
