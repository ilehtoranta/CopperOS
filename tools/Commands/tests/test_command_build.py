"""Integration checks for the real versioned-build release adapter.

Set COPPER_COMMAND_BUILD_REPORT to a fresh build producer's qualification.json.
Without that artifact these checks explicitly skip; ordinary metadata tests run
separately. Mutations use private temporary copies, never retained build files.
"""
from __future__ import annotations

import copy
from contextlib import contextmanager
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

TOOLS = Path(__file__).resolve().parents[1]
REPO = TOOLS.parents[1]
sys.path.insert(0, str(TOOLS))
import verify_command_release as release
from verify_command_build import GATES, REPORT_TYPE, verify_versioned_build


@unittest.skipUnless(os.environ.get("COPPER_COMMAND_BUILD_REPORT"),
                     "Requires an actual fresh versioned build report")
class CommandBuildIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.report_path = Path(os.environ["COPPER_COMMAND_BUILD_REPORT"]).resolve(strict=True)
        self.report = release.load_json(self.report_path)
        self.record = copy.deepcopy(self.report["records"][0])
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.counter = 0

    def bound(self, path):
        return {"path": str(path), "sha256": release.sha256(path)}

    def changed_json(self, reference, change):
        value = release.load_json(Path(reference["path"]))
        change(value)
        self.counter += 1
        path = self.folder / f"mutation-{self.counter}.json"
        path.write_text(json.dumps(value), encoding="utf-8")
        return self.bound(path)

    def verify(self):
        return verify_versioned_build(self.report, self.record, REPO)

    def selected(self, index=0):
        return next(item for item in self.report["build_passes"][index]["records"]
                    if item["cpu"] == self.record["cpu"])

    @contextmanager
    def parsed_mutation(self, reference, change):
        """Exercise semantic validation after IO; not a file/hash tamper test."""
        original = release.load_json
        target = Path(reference["path"]).resolve()
        def altered(path):
            value = original(path)
            if Path(path).resolve() == target:
                change(value)
            return value
        with patch.object(release, "load_json", side_effect=altered):
            yield

    def test_actual_three_cpu_builds_discharge_only_two_gates(self):
        for record in self.report["records"]:
            with self.subTest(cpu=record["cpu"]):
                result = verify_versioned_build(self.report, record, REPO)
                self.assertEqual(set(result["satisfies_release_gates"]), set(GATES))
                self.assertGreater(result["verified_file_count"], 10)
                self.assertEqual(len(set(result["build_directories"])), 2)

    def test_same_build_twice_cannot_establish_reproducibility(self):
        self.report["build_passes"][1] = copy.deepcopy(self.report["build_passes"][0])
        with self.assertRaisesRegex(release.EvidenceError, "independent"):
            self.verify()

    def test_forbidden_static_feature_is_checked_after_read(self):
        item = self.selected()
        with self.parsed_mutation(item["native_static"],
                lambda value: value["NativeCompatibility"]["RuntimeHelpers"].append("unexpected managed helper")):
            with self.assertRaisesRegex(release.EvidenceError, "forbidden native dependency"):
                self.verify()

    def test_native_option_is_checked_after_read(self):
        build = self.report["build_passes"][1]
        def mutate(value):
            command = next(cmd for cmd in value["commands"]
                           if cmd[cmd.index("--cpu") + 1] == self.record["cpu"])
            command[command.index("--fpu") + 1] = "required"
        with self.parsed_mutation(build["input_manifest"], mutate):
            with self.assertRaisesRegex(release.EvidenceError, "generation arguments differ"):
                self.verify()

    def test_required_startup_source_cannot_be_removed(self):
        def mutate(value):
            value["inputs"] = [entry for entry in value["inputs"]
                               if Path(entry["path"]).name != "Workbench31MakeLinkEntry.cs"]
        self.report["source_snapshot"] = self.changed_json(self.report["source_snapshot"], mutate)
        with self.assertRaisesRegex(release.EvidenceError, "startup source not captured"):
            self.verify()

    def test_selected_compiler_and_cpu_must_match(self):
        for field, value in (("compiler", {"path": self.record["compiler"]["path"], "sha256": "f" * 64}),
                             ("cpu", "68020"), ("profile", "morphos320")):
            with self.subTest(field=field):
                original = self.record[field]
                self.record[field] = value
                with self.assertRaises(release.EvidenceError):
                    self.verify()
                self.record[field] = original

    def test_transform_receipt_is_recomputed_not_trusted(self):
        item = self.selected()
        with self.parsed_mutation(item["version_append"],
                lambda value: value.update(original_code_bytes=value["original_code_bytes"] + 4)):
            with self.assertRaisesRegex(release.EvidenceError, "recomputed structure"):
                self.verify()

    def test_second_build_cannot_reuse_first_managed_inputs(self):
        self.selected(1)["sdk"] = self.selected(0)["sdk"]
        with self.assertRaises(release.EvidenceError):
            self.verify()

    def test_source_snapshot_membership_cannot_be_reduced(self):
        def mutate(value):
            value["inputs"] = [entry for entry in value["inputs"]
                               if Path(entry["path"]).name != "NativeCommandArguments.cs"]
        self.report["source_snapshot"] = self.changed_json(self.report["source_snapshot"], mutate)
        with self.assertRaisesRegex(release.EvidenceError, "source snapshot omits"):
            self.verify()

    def test_native_input_membership_is_checked_after_read(self):
        with self.parsed_mutation(self.report["build_passes"][1]["input_manifest"],
                lambda value: value["inputs"].pop()):
            with self.assertRaisesRegex(release.EvidenceError, "closure omits"):
                self.verify()

    def test_managed_command_trailing_override_is_checked(self):
        target = Path(self.report["build_passes"][1]["build_logs"][0]["path"])
        original = Path.read_text
        def altered(path, *args, **kwargs):
            text = original(path, *args, **kwargs)
            if path == target:
                first, rest = text.split("\n", 1)
                text = json.dumps(json.loads(first) + ["-p:UnexpectedOverride=true"]) + "\n" + rest
            return text
        with patch.object(Path, "read_text", altered):
            with self.assertRaisesRegex(release.EvidenceError, "managed build arguments differ"):
                self.verify()

    def test_full_preflight_keeps_every_other_gate_open(self):
        # A proposed release is inspected only; nothing enters the shipping
        # manifest. Unknown original classification is still an open gate.
        record = copy.deepcopy(self.record)
        expected = {"command": "makelink", "profile": "wb31", "cpu": record["cpu"],
                    "distribution": "normal-runtime", "installed_path": "C/MakeLink"}
        record.update(expected, release_record_version=1, status="proposed", shipping=True,
                      minimum_stack_bytes=4096, amiga_protection=0,
                      required_pure=False, pure_admission=False)
        record["inputs"] = release.load_json(Path(record["input_manifest"]["path"]))["inputs"]
        item = {"report_type": REPORT_TYPE, "report": self.bound(self.report_path)}
        record["evidence"] = {gate: item for gate in release.BASE_GATES}
        inventory = release.load_json(REPO / "docs/Commands/Workbench31MorphOS320/command-inventory.json")
        result = release.preflight(record, inventory, REPO, expected)
        self.assertFalse(result["eligible_for_staging"], result)
        self.assertTrue(result["metadata_valid"], result)
        self.assertEqual({row["gate"] for row in result["passed_gates"]}, set(GATES))
        self.assertEqual({row["gate"] for row in result["blockers"]}, set(release.BASE_GATES) - set(GATES))


if __name__ == "__main__":
    unittest.main()
