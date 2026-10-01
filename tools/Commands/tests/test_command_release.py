"""Adversarial CC08 preflight tests; fixture reports never qualify a release."""
from __future__ import annotations

import copy
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

TOOLS = Path(__file__).resolve().parents[1]
REPO = TOOLS.parents[1]
sys.path.insert(0, str(TOOLS))
import verify_command_release as release


class ReleasePreflightTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.version = "$VER: Rename 0.1 (8.9.2026)"
        self.binary = self.file("rename.hunk", b"\0\0\x03\xf3" + self.version.encode() + b"\0")
        self.source = self.file("source.cs", b"synthetic source")
        self.compiler = self.file("compiler.bin", b"synthetic compiler")
        self.sdk = self.file("sdk.bin", b"synthetic SDK")
        self.inputs = self.file("inputs.json", b'{"fixture":"not a real build"}')
        self.executor = self.file("runner.dll", b"synthetic runner")
        self.core = self.file("cpu.dll", b"synthetic core")
        self.expected = {"command": "rename", "profile": "wb31", "cpu": "68000",
                         "distribution": "normal-runtime", "installed_path": "C/Rename"}
        self.inventory = {"schema_version": 1, "commands": [{"id": "rename", "kind": "external",
            "reference_profiles": {"wb31": {"required": True,
                "source_files": [{"original_name": "Rename"}],
                "purity": {"required_by_observed_design_evidence": False,
                           "classification": "unresolved-not-nonpure"}}}}]}
        self.record = {**self.expected, "release_record_version": 1, "shipping": True,
            "path": self.binary["path"], "sha256": self.binary["sha256"],
            "bytes": (self.root / self.binary["path"]).stat().st_size,
            "entry": "Fixture.Rename::Main", "fpu": "disabled", "output_format": "hunk",
            "version_id": self.version, "minimum_stack_bytes": 4096,
            "source": self.source, "compiler": self.compiler, "sdk": self.sdk,
            "input_manifest": self.inputs, "inputs": [self.source, self.compiler, self.sdk],
            "dependencies": [{"id": name, "minimum_version": 36, "abi": self.sdk}
                             for name in ("exec.library", "dos.library")],
            "amiga_protection": 0, "required_pure": False, "pure_admission": False,
            "evidence": {}}

    def file(self, name, data):
        path = self.root / name
        path.write_bytes(data)
        return {"path": name, "sha256": release.sha256(path)}

    def run_record(self, record=None):
        return release.preflight(record if record is not None else self.record,
                                 self.inventory, self.root, self.expected)

    def assert_error(self, code):
        result = self.run_record()
        self.assertFalse(result["eligible_for_staging"])
        self.assertIn(code, {item["code"] for item in result["errors"]}, result)
        return result

    def vector(self):
        cases = [{"name": name, "result": result, "ioErr": 0, "stdoutHex": "",
                  "instructionInterleaved": True, "instructions": 100,
                  "configuredStackBytes": 4096, "stackBytesWritten": 80,
                  "events": ["OpenLibrary", "CloseLibrary"]}
                 for name, result in release.VECTOR_CASES[("rename", "wb31")].items()]
        return {"schemaVersion": 1, "status": "passed",
                "suite": "workbench-rename-startup-vector-fixture", "cpu": "68000",
                "imageSha256": self.record["sha256"], "imageBytes": self.record["bytes"],
                "imageLoads": 1, "sharedImageWrites": 0, "passed": len(cases), "cases": cases,
                "managedExecutorPath": self.executor["path"], "managedExecutorSha256": self.executor["sha256"],
                "instructionCorePath": self.core["path"], "instructionCoreSha256": self.core["sha256"],
                **{key: False for key in ("realKickstartExecution", "realCopperStartExecution",
                   "realDosParser", "realDosIo", "referenceCommandBehavior", "shippingOrPureApproval", "minimumStackQualified")}}

    def attach_vector(self, report):
        reference = self.file("vector.json", json.dumps(report).encode())
        self.record["evidence"]["native-vector"] = {
            "report_type": "copperos-native-vector-v1", "report": reference}

    def test_complete_metadata_still_blocks_every_full_release_gate(self):
        result = self.run_record()
        self.assertTrue(result["metadata_valid"], result)
        self.assertEqual(result["status"], "blocked")
        self.assertEqual(set(release.BASE_GATES), {gate["gate"] for gate in result["blockers"]})
        self.assertFalse(result["eligible_for_staging"])

    def test_missing_release_fields_fail_specifically(self):
        for field, code in (("release_record_version", "record-schema"), ("shipping", "not-release"),
                            ("minimum_stack_bytes", "minimum-stack"), ("version_id", "version"),
                            ("dependencies", "dependencies"), ("source", "file-binding"),
                            ("input_manifest", "file-binding"), ("required_pure", "pure-policy")):
            with self.subTest(field=field):
                record = copy.deepcopy(self.record)
                del record[field]
                result = self.run_record(record)
                self.assertIn(code, {row["code"] for row in result["errors"]})

    def test_each_selection_dimension_is_independent(self):
        for field, replacement in (("command", "makelink"), ("profile", "morphos320"),
                                   ("cpu", "68020"), ("distribution", "installation-media"),
                                   ("installed_path", "C/Other")):
            with self.subTest(field=field):
                record = {**self.record, field: replacement}
                result = self.run_record(record)
                self.assertIn("identity", {row["code"] for row in result["errors"]})

    def test_absent_stale_and_malformed_hashes_rejected(self):
        for value in (None, "", "f" * 64, "G" * 64):
            with self.subTest(value=value):
                self.record["sha256"] = value
                self.assert_error("file-binding")

    def test_changed_dependency_and_compiler_invalidate_binding(self):
        for name in ("sdk.bin", "compiler.bin", "source.cs", "inputs.json"):
            with self.subTest(name=name):
                original = (self.root / name).read_bytes()
                (self.root / name).write_bytes(original + b"changed")
                self.assert_error("file-binding")
                (self.root / name).write_bytes(original)

    def test_version_must_exist_in_artifact_with_nul(self):
        self.record["version_id"] = "$VER: Rename 99.2 (8.9.2026)"
        self.assert_error("version")
        self.record["version_id"] = "$VER: Copy 0.1 (8.9.2026)"
        self.assert_error("version")

    def test_dos_paths_cannot_escape_or_silently_change_original_spelling(self):
        for path in ("C/../Rename", "/C/Rename", "C:\\Rename", "C/rename", "SYS/C/Rename", "C/Rename/extra"):
            with self.subTest(path=path):
                self.record["installed_path"] = self.expected["installed_path"] = path
                self.assert_error("placement")

    def test_profile_membership_not_inferred_from_shared_command_name(self):
        self.record["profile"] = self.expected["profile"] = "morphos320"
        self.assert_error("inventory")

    def test_duplicate_inventory_identity_rejected(self):
        self.inventory["commands"].append(copy.deepcopy(self.inventory["commands"][0]))
        self.assert_error("inventory")

    def test_original_required_pure_cannot_be_downgraded_by_clear_p_bit(self):
        self.inventory["commands"][0]["reference_profiles"]["wb31"]["purity"]["required_by_observed_design_evidence"] = True
        result = self.assert_error("pure-policy")
        self.assertTrue(set(release.PURE_GATES).issubset({item["gate"] for item in result["blockers"]}))

    def test_p_request_needs_purity_even_if_requirement_unknown(self):
        self.record["amiga_protection"] = 32
        self.assert_error("pure-policy")
        self.record["pure_admission"] = True
        result = self.run_record()
        self.assertFalse(result["eligible_for_staging"])
        self.assertTrue(set(release.PURE_GATES).issubset({item["gate"] for item in result["blockers"]}))

    def test_boolean_or_passed_status_cannot_discharge_any_gate(self):
        for gate in release.BASE_GATES:
            self.record["evidence"][gate] = {"status": "passed", "qualified": True}
        result = self.assert_error("gate-evidence")
        self.assertEqual(len(result["blockers"]), len(release.BASE_GATES))

    def test_fabricated_named_schema_cannot_discharge_gate(self):
        reference = self.file("claimed-pure.json", b'{"status":"passed","pure":true}')
        self.record["evidence"]["native-static"] = {"report_type": "release-native-static-v1", "report": reference}
        result = self.run_record()
        self.assertIn({"gate": "native-static", "reason": "unsupported-release-report-schema"}, result["blockers"])

    def test_vector_is_only_bounded_support_never_purity_or_minimum_stack(self):
        self.attach_vector(self.vector())
        result = self.run_record()
        self.assertTrue(result["metadata_valid"], result)
        self.assertEqual(result["supporting_evidence"][0]["satisfies_release_gates"], [])
        self.assertFalse(result["eligible_for_staging"])

    def test_vector_report_negative_controls(self):
        mutations = {
            "unknown schema": lambda x: x.update(schemaVersion=2),
            "boolean schema": lambda x: x.update(schemaVersion=True),
            "wrong command": lambda x: x.update(suite="workbench-makelink-native-entry-vector-fixture"),
            "wrong profile": lambda x: x.update(suite="morphos-rename-native-entry-vector-fixture"),
            "wrong cpu": lambda x: x.update(cpu="68020"),
            "wrong hash": lambda x: x.update(imageSha256="f" * 64),
            "wrong count": lambda x: x.update(passed=10000),
            "no cases": lambda x: x.update(cases=[]),
            "private image copies": lambda x: x.update(imageLoads=2),
            "shared write": lambda x: x.update(sharedImageWrites=1),
            "unsupported purity": lambda x: x.update(shippingOrPureApproval=True),
            "unsupported stack": lambda x: x.update(minimumStackQualified=True),
            "unsupported real parser": lambda x: x.update(realDosParser=True),
            "no distinct calls": lambda x: [c.update(name="same") for c in x["cases"]],
            "required case omitted": lambda x: x["cases"][0].update(name="unknown-case"),
            "required case wrong result": lambda x: x["cases"][0].update(result=0),
            "no interleaving": lambda x: [c.update(instructionInterleaved=False) for c in x["cases"]],
            "stack overflow": lambda x: x["cases"][0].update(stackBytesWritten=5000),
            "missing events": lambda x: x["cases"][0].update(events=[]),
            "runner changed": lambda x: x.update(managedExecutorSha256="0" * 64),
        }
        for name, mutate in mutations.items():
            with self.subTest(name=name):
                report = self.vector()
                mutate(report)
                self.attach_vector(report)
                self.assert_error("supporting-evidence")

    def test_hash_bound_report_must_remain_unchanged(self):
        self.attach_vector(self.vector())
        (self.root / "vector.json").write_text('{"status":"passed"}')
        self.assert_error("file-binding")

    def test_duplicate_json_key_rejected(self):
        path = self.root / "duplicate.json"
        path.write_text('{"shipping":false,"shipping":true}')
        with self.assertRaisesRegex(ValueError, "Duplicate JSON key"):
            release.load_json(path)

    def test_manifest_never_selects_development_as_missing_release_fallback(self):
        manifest = {"schema_version": 1, "qualified_shipping_artifacts": [],
                    "qualification_only_artifacts": [self.record], "candidates": [self.record]}
        result = release.preflight_manifest(manifest, self.inventory, self.root, self.expected)
        self.assertIn("artifact-selection", {row["code"] for row in result["errors"]})

    def test_manifest_duplicate_artifact_or_case_insensitive_destination_is_rejected(self):
        for change, code in (({}, "duplicate-artifact"),
                             ({"command": "other", "installed_path": "C/RENAME"}, "duplicate-destination")):
            with self.subTest(change=change):
                manifest = {"schema_version": 1, "qualified_shipping_artifacts": [self.record, {**self.record, **change}]}
                result = release.preflight_manifest(manifest, self.inventory, self.root, self.expected)
                self.assertIn(code, {row["code"] for row in result["errors"]})

    def test_manifest_keeps_profile_and_cpu_output_namespaces_separate(self):
        manifest = {"schema_version": 1, "qualified_shipping_artifacts": [self.record,
                    {**self.record, "cpu": "68020"}, {**self.record, "profile": "morphos320"}]}
        result = release.preflight_manifest(manifest, self.inventory, self.root, self.expected)
        self.assertTrue(result["metadata_valid"], result)
        self.assertFalse(result["eligible_for_staging"])

    def test_cli_cannot_signal_success_for_metadata_only(self):
        inventory = self.file("inventory.json", json.dumps(self.inventory).encode())
        self.file("record.json", json.dumps(self.record).encode())
        arguments = [sys.executable, str(TOOLS / "verify_command_release.py"), "--root", str(self.root),
                     "--record", str(self.root / "record.json"), "--inventory", str(self.root / "inventory.json"),
                     "--inventory-sha256", inventory["sha256"]]
        for key, value in self.expected.items():
            arguments.extend(["--" + key.replace("_", "-"), value])
        completed = subprocess.run(arguments, capture_output=True, text=True)
        self.assertEqual(completed.returncode, 1, completed.stderr)
        self.assertEqual(json.loads(completed.stdout)["status"], "blocked")

    def test_all_current_unqualified_manifest_and_candidate_records_remain_rejected(self):
        folder = REPO / "docs/Commands/Workbench31MorphOS320"
        inventory = release.load_json(folder / "command-inventory.json")
        manifests = [(folder / "build-manifest.json", "qualification_only_artifacts")]
        manifests.extend((path, "candidates") for path in folder.glob("*-development-candidates.json"))
        count = 0
        for path, key in manifests:
            records = release.load_json(path).get(key, [])
            for record in records:
                with self.subTest(file=path.name, record=record.get("id")):
                    result = release.preflight(record, inventory, REPO, self.expected)
                    self.assertFalse(result["eligible_for_staging"])
                    self.assertIn("not-release", {item["code"] for item in result["errors"]})
                    count += 1
        self.assertGreaterEqual(count, 21)
        self.assertEqual(release.load_json(folder / "build-manifest.json")["qualified_shipping_artifacts"], [])


if __name__ == "__main__":
    unittest.main()
