"""Authored provenance/comparison checks; no guest execution or reference media."""
import copy
import json
from pathlib import Path
import tempfile
import unittest

from capture_probe import bind_command_replacement, bind_version_replacement, identity
from compare_captures import compare_sides, invocation_line, load_side


def admitted_side(candidate=False):
    side = {"admitted": True, "errors": [], "invocation": {"command": "C:Version dos.library RES", "token": 2 if candidate else 1},
            "result": {"commandReturn": 0, "postSystemIoErr": 0, "outputLength": 17,
                       "outputHex": b"dos.library 40.3\n".hex()},
            "roles": {}, "runtime": [{"path": "frozen", "bytes": 100, "sha256": "1" * 64}],
            "runtimeQualification": {"path": "qualification", "bytes": 20, "sha256": "2" * 64},
            "requestedFrames": 2400, "originalCommand": {"bytes": 4764, "sha256": "3" * 64}}
    for role in ("rom", "originalAdf", "probe"):
        side["roles"][role] = {"path": role, "bytes": 80, "sha256": "4" * 64}
    side["roles"]["derivedAdf"] = {"path": "candidate" if candidate else "original", "bytes": 901120, "sha256": "5" * 64}
    return side


class ReplacementBindingTests(unittest.TestCase):
    def preparation(self, folder, guest_path="C/Version"):
        candidate = folder / "candidate.hunk"
        candidate.write_bytes(b"authored candidate fixture")
        derived = folder / "derivative"
        derived.mkdir()
        snapshot = derived / ("version-replacement.hunk" if guest_path == "C/Version"
                              else "command-replacement.hunk")
        snapshot.write_bytes(candidate.read_bytes())
        replacement = {"guest_path": guest_path, "candidate": identity(candidate), "snapshot": identity(snapshot),
                       "original_command": {"path": guest_path, "bytes": 4, "sha256": "0" * 64, "metadata": {}},
                       "experimental": True, "original_metadata_preserved": True, "pure_admission": False, "shipping": False}
        return {"replacement": replacement, "derived_adf": {"path": str(derived / "probe.adf")}}, candidate, snapshot

    def test_old_original_receipts_and_explicit_null_remain_valid(self):
        self.assertEqual((None, []), bind_version_replacement({}, None))
        self.assertEqual((None, []), bind_version_replacement({"replacement": None}, None))

    def test_candidate_requires_explicit_input_and_snapshot_match(self):
        with tempfile.TemporaryDirectory() as directory:
            receipt, candidate, snapshot = self.preparation(Path(directory))
            self.assertEqual(receipt["replacement"], bind_version_replacement(receipt, candidate)[0])
            with self.assertRaisesRegex(ValueError, "explicit"):
                bind_version_replacement(receipt, None)
            snapshot.write_bytes(b"tampered")
            with self.assertRaisesRegex(ValueError, "snapshot identity changed"):
                bind_version_replacement(receipt, candidate)

    def test_candidate_without_receipt_is_rejected(self):
        with self.assertRaisesRegex(ValueError, "without a replacement"):
            bind_version_replacement({}, Path("unused"))

    def test_wrong_candidate_and_classification_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            receipt, candidate, _ = self.preparation(Path(directory))
            wrong = Path(directory) / "wrong.hunk"
            wrong.write_bytes(b"different")
            with self.assertRaisesRegex(ValueError, "candidate identity"):
                bind_version_replacement(receipt, wrong)
            for key, value in (("pure_admission", True), ("shipping", True), ("experimental", False),
                               ("original_metadata_preserved", False), ("guest_path", "C/Other")):
                changed = copy.deepcopy(receipt)
                changed["replacement"][key] = value
                with self.subTest(key=key), self.assertRaisesRegex(ValueError, "misclassified"):
                    bind_version_replacement(changed, candidate)

    def test_generic_command_candidate_binding_accepts_exact_break_target(self):
        with tempfile.TemporaryDirectory() as directory:
            receipt, candidate, _ = self.preparation(Path(directory), "C/Break")
            replacement, paths = bind_command_replacement(receipt, candidate, "C/Break")
            self.assertEqual("C/Break", replacement["guest_path"])
            self.assertEqual(2, len(paths))
            with self.assertRaisesRegex(ValueError, "misclassified"):
                bind_command_replacement(receipt, candidate, "C/ChangeTaskPri")


class CaptureComparisonTests(unittest.TestCase):
    def test_only_diagnostic_tokens_may_differ(self):
        report = compare_sides(admitted_side(), admitted_side(True))
        self.assertEqual("captured-case-equal", report["status"])
        self.assertTrue(report["commandResultEqual"])
        self.assertFalse(report["fullCommandParityClaim"])

    def test_caller_ioerr_mismatch_is_preserved_and_fails_overall_equality(self):
        candidate = admitted_side(True)
        candidate["result"]["postSystemIoErr"] = 115
        report = compare_sides(admitted_side(), candidate)
        self.assertEqual("captured-case-mismatch", report["status"])
        self.assertTrue(report["commandResultEqual"])
        self.assertFalse(report["allObservedFieldsEqual"])
        self.assertEqual(115, report["comparison"]["postSystemIoErr"]["candidate"])

    def test_raw_newlines_command_spelling_and_return_are_not_normalized(self):
        for changed in ("output", "command", "return"):
            candidate = admitted_side(True)
            if changed == "output":
                candidate["result"]["outputHex"] = b"dos.library 40.3\r\n".hex()
                candidate["result"]["outputLength"] = 18
            elif changed == "command":
                candidate["invocation"]["command"] = "C:Version DOS.LIBRARY RES"
            else:
                candidate["result"]["commandReturn"] = 5
            with self.subTest(changed=changed):
                report = compare_sides(admitted_side(), candidate)
                self.assertEqual("captured-case-mismatch", report["status"])
                self.assertFalse(report["commandResultEqual"])

    def test_provenance_mismatch_blocks_even_equal_outputs(self):
        for role in ("rom", "originalAdf", "probe"):
            candidate = admitted_side(True)
            candidate["roles"][role]["sha256"] = "9" * 64
            with self.subTest(role=role):
                report = compare_sides(admitted_side(), candidate)
                self.assertEqual("pair-rejected", report["status"])
                self.assertFalse(report["commandResultEqual"])
                self.assertFalse(report["comparison"]["commandReturn"]["equal"])

    def test_runtime_and_frame_counts_must_match(self):
        for key, value in (("runtime", []), ("requestedFrames", 2000)):
            candidate = admitted_side(True)
            candidate[key] = value
            with self.subTest(key=key):
                self.assertEqual("pair-rejected", compare_sides(admitted_side(), candidate)["status"])

    def owner_effect_side(self, candidate=False, after_priority=42):
        side = admitted_side(candidate)
        side["ownerTaskStates"] = {
            "before": {"stage": 3, "taskNumber": 1, "priority": 0,
                       "signalsReceived": 0},
            "after": {"stage": 100, "taskNumber": 1,
                      "priority": after_priority,
                      "signalsReceived": 1 << 12},
        }
        return side

    def test_expected_owner_priority_effect_is_admitted_with_both_guest_states(self):
        report = compare_sides(self.owner_effect_side(),
                               self.owner_effect_side(True), 1, 0, 42)
        self.assertEqual("captured-case-equal", report["status"])
        self.assertTrue(report["comparison"]["ownerTaskEffect"]["equal"])
        self.assertTrue(report["allObservedFieldsEqual"])

    def test_owner_effect_mismatch_or_missing_pre_state_fails_closed(self):
        wrong = compare_sides(self.owner_effect_side(),
                              self.owner_effect_side(True, 41), 1, 0, 42)
        self.assertEqual("captured-case-mismatch", wrong["status"])
        self.assertFalse(wrong["comparison"]["ownerTaskEffect"]["equal"])
        missing = self.owner_effect_side(True)
        missing["ownerTaskStates"]["before"] = None
        report = compare_sides(self.owner_effect_side(), missing, 1, 0, 42)
        self.assertEqual("captured-case-mismatch", report["status"])
        self.assertIn("pre-System or terminal", report["errors"][0])

    def test_expected_owner_signal_bit_must_be_received_by_the_same_cli(self):
        original = self.owner_effect_side()
        candidate = self.owner_effect_side(True)
        report = compare_sides(original, candidate, 1, None, None, 12)
        self.assertEqual("captured-case-equal", report["status"])
        self.assertTrue(report["comparison"]["ownerTaskEffect"]["equal"])
        self.assertTrue(report["comparison"]["ownerTaskEffect"]["expectedValuesMatch"])
        candidate["ownerTaskStates"]["after"]["signalsReceived"] = 0
        mismatch = compare_sides(original, candidate, 1, None, None, 12)
        self.assertEqual("captured-case-mismatch", mismatch["status"])

    def test_expected_owner_signal_mask_requires_each_requested_bit(self):
        original = self.owner_effect_side()
        candidate = self.owner_effect_side(True)
        # The source Break command sets Ctrl-C; this unrelated bit may also
        # arrive during the command and must not weaken the requested mask.
        original["ownerTaskStates"]["after"]["signalsReceived"] |= 1 << 8
        candidate["ownerTaskStates"]["after"]["signalsReceived"] |= 1 << 8
        report = compare_sides(original, candidate, 1, None, None, None,
                               1 << 12)
        self.assertEqual("captured-case-equal", report["status"])
        self.assertEqual(0, report["comparison"]["ownerTaskEffect"]["expected"]["before"]["signalMaskBitsSet"])
        self.assertEqual(1 << 12, report["comparison"]["ownerTaskEffect"]["expected"]["after"]["signalMaskBitsSet"])

        candidate["ownerTaskStates"]["after"]["signalsReceived"] &= ~(1 << 12)
        mismatch = compare_sides(original, candidate, 1, None, None, None,
                                 1 << 12)
        self.assertEqual("captured-case-mismatch", mismatch["status"])

    def test_expected_owner_signal_mask_must_start_clear(self):
        original = self.owner_effect_side()
        candidate = self.owner_effect_side(True)
        original["ownerTaskStates"]["before"]["signalsReceived"] = 1 << 14
        candidate["ownerTaskStates"]["before"]["signalsReceived"] = 1 << 14
        mismatch = compare_sides(original, candidate, 1, None, None, None,
                                 1 << 14)
        self.assertEqual("captured-case-mismatch", mismatch["status"])

    def test_nonadmitted_side_retains_raw_results(self):
        candidate = admitted_side(True)
        candidate["admitted"] = False
        candidate["errors"] = ["bad snapshot"]
        candidate["result"]["commandReturn"] = 20
        report = compare_sides(admitted_side(), candidate)
        self.assertEqual("pair-rejected", report["status"])
        self.assertEqual(20, report["comparison"]["commandReturn"]["candidate"])
        self.assertEqual(["bad snapshot"], report["candidate"]["errors"])

    def test_invalid_analysis_admission_retains_reported_record(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "invalid-analysis.json"
            path.write_text(json.dumps({"schemaVersion": 999, "status": "failed", "result": {"commandReturn": 20}}))
            side = load_side(path, True)
            self.assertFalse(side["admitted"])
            self.assertEqual(20, side["result"]["commandReturn"])
            self.assertIn("Unsupported analysis schema", side["errors"][0])

    def test_invocation_encoding_is_exact_shell_quoting(self):
        self.assertEqual('C:CopperProbe COMMAND "C:Version *"a**b*"" TOKEN 7',
                         invocation_line({"command": 'C:Version "a*b"', "token": 7}))


if __name__ == "__main__":
    unittest.main()
