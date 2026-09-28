"""Authored saved-memory fixtures verify fail-closed decoding, not guest execution."""
import copy
import json
from pathlib import Path
import struct
import tempfile
import unittest

import analyze_probe as probe


class Fixture:
    EXEC, PORT, RECORD, PROCESS, OUTPUT = 0x1000, 0xc01000, 0xc02000, 0xc03000, 0xc04000
    TOKEN = 20260926

    def __init__(self):
        self.chip = bytearray(0x80000)
        self.slow = bytearray(0x80000)
        self.state = {"frame": 60, "CompletedFrames": 60, "Cycle": 8526120, "pc": 0xf80000,
                      "RomOverlayEnabled": False, "UnsupportedActiveFeature": None}
        self.put(4, self.EXEC)
        self.put(self.EXEC + 8, 9, 1)
        self.put(self.EXEC + 20, 40, 2)
        self.list_address = self.EXEC + 392
        self.put(self.list_address, self.PORT)
        self.put(self.list_address + 8, self.PORT)
        self.put(self.PORT, self.list_address + 4)
        self.put(self.PORT + 4, self.list_address)
        self.put(self.PORT + 8, 4, 1)
        self.put(self.PORT + 10, self.RECORD)
        self.put(self.PORT + 16, self.PROCESS)
        self.put(self.PROCESS + 8, 13, 1)
        self.put(self.PROCESS + 140, 1)
        self.put(self.PROCESS + 26, 0)
        self.write(self.RECORD, b"CopperOS.CommandProbe.v1\0".ljust(32, b"\0"))
        values = [0x43505242, 1, self.TOKEN, 100, 0, 205, 4, 0,
                  self.PROCESS, self.OUTPUT, 4096, self.PORT, 15, 0, 96, 0]
        self.write(self.RECORD + 32, struct.pack(">16I", *values))
        self.write(self.OUTPUT, b"40.3")

    def write(self, address, data):
        if address >= 0xc00000:
            self.slow[address - 0xc00000:address - 0xc00000 + len(data)] = data
        else:
            self.chip[address:address + len(data)] = data

    def put(self, address, value, size=4):
        self.write(address, value.to_bytes(size, "big"))

    def field(self, index, value):
        self.put(self.RECORD + 32 + index * 4, value)

    def row(self):
        return probe.observe(probe.Memory(bytes(self.chip), bytes(self.slow)), self.state, self.TOKEN)


class ProbeAnalyzerTests(unittest.TestCase):
    def test_complete_records_bytes_and_parent_ioerr_without_parity_claim(self):
        row = Fixture().row()
        self.assertEqual("complete", row["status"])
        self.assertEqual("34302e33", row["record"]["outputHex"])
        self.assertEqual(205, row["record"]["postSystemIoErr"])
        self.assertEqual(1, row["record"]["ownerTaskNumber"])
        self.assertEqual(0, row["record"]["ownerTaskPriority"])
        self.assertEqual(0, row["record"]["ownerTaskSignalsReceived"])
        self.assertEqual([], probe.evaluate([row]))

    def test_owner_priority_is_read_as_signed_exec_node_byte(self):
        fixture = Fixture()
        fixture.put(fixture.PROCESS + 9, 0xd6, 1)
        row = fixture.row()
        self.assertEqual(1, row["record"]["ownerTaskNumber"])
        self.assertEqual(-42, row["record"]["ownerTaskPriority"])

    def test_owner_received_signal_mask_is_read_from_saved_task(self):
        fixture = Fixture()
        fixture.put(fixture.PROCESS + 26, 1 << 12)
        self.assertEqual(1 << 12,
                         fixture.row()["record"]["ownerTaskSignalsReceived"])

    def test_completed_owner_state_must_remain_stable(self):
        fixture = Fixture()
        before = fixture.row()
        fixture.put(fixture.PROCESS + 9, 42, 1)
        after = fixture.row()
        self.assertTrue(probe.evaluate([before, after]))

    def test_nonzero_command_return_is_a_complete_capture(self):
        fixture = Fixture()
        fixture.field(4, 5)
        self.assertEqual("complete", fixture.row()["status"])

    def test_published_before_readargs_can_have_zero_token(self):
        fixture = Fixture()
        fixture.field(2, 0)
        fixture.field(3, 1)
        fixture.field(6, 0)
        fixture.field(9, 0)
        fixture.field(12, 0)
        row = fixture.row()
        self.assertEqual("in-progress", row["status"])
        self.assertFalse(row["record"]["tokenVerified"])
        self.assertTrue(probe.evaluate([row]))
        self.assertEqual([], probe.evaluate([row, Fixture().row()]))

    def test_failure_before_readargs_reports_unknown_token(self):
        fixture = Fixture()
        for index, value in ((2, 0), (3, 200), (6, 0), (7, 2), (9, 0), (12, 0), (13, 114)):
            fixture.field(index, value)
        row = fixture.row()
        self.assertEqual("failed", row["status"])
        self.assertEqual("ReadArgsFailed", row["record"]["probeErrorName"])
        self.assertFalse(row["record"]["tokenVerified"])
        self.assertTrue(probe.evaluate([row]))

    def test_missing_port_is_not_success(self):
        fixture = Fixture()
        fixture.put(fixture.list_address, fixture.list_address + 4)
        fixture.put(fixture.list_address + 8, fixture.list_address)
        row = fixture.row()
        self.assertEqual("missing", row["status"])
        self.assertTrue(probe.evaluate([row]))

    def test_duplicate_ports_are_rejected_even_before_later_complete(self):
        fixture = Fixture()
        other = 0xc01100
        fixture.put(fixture.PORT, other)
        fixture.put(other, fixture.list_address + 4)
        fixture.put(other + 4, fixture.PORT)
        fixture.put(other + 10, fixture.RECORD)
        fixture.put(fixture.list_address + 8, other)
        row = fixture.row()
        self.assertEqual("invalid", row["status"])
        self.assertTrue(row["candidateFound"])
        self.assertTrue(probe.evaluate([row, Fixture().row()]))

    def test_bad_header_fields_reject(self):
        for field, value in ((0, 0), (1, 2), (2, 99), (3, 101), (6, 4097), (7, 999),
                             (9, 0xc7fff0), (10, 4095), (11, 0), (12, 31), (13, 103), (14, 95), (15, 1)):
            with self.subTest(field=field, value=value):
                fixture = Fixture()
                fixture.field(field, value)
                row = fixture.row()
                self.assertEqual("invalid", row["status"])
                self.assertTrue(probe.evaluate([row]))

    def test_terminal_failure_enums_include_input_and_flush(self):
        for value in (10, 11, 12):
            fixture = Fixture()
            fixture.field(3, 200)
            fixture.field(7, value)
            self.assertEqual("failed", fixture.row()["status"])

    def test_public_list_cycle_is_bounded(self):
        fixture = Fixture()
        fixture.put(fixture.PORT, fixture.PORT)
        self.assertEqual("invalid", fixture.row()["status"])

    def test_owner_mismatch_rejects(self):
        fixture = Fixture()
        fixture.put(fixture.PORT + 16, fixture.PROCESS + 4)
        self.assertEqual("invalid", fixture.row()["status"])

    def test_only_saved_ram_can_hold_record_and_output(self):
        fixture = Fixture()
        fixture.field(9, 0xf80000)
        self.assertEqual("invalid", fixture.row()["status"])

    def test_complete_cannot_change_or_disappear(self):
        good = Fixture().row()
        changed = copy.deepcopy(good)
        changed["record"]["outputHex"] = "00"
        self.assertTrue(probe.evaluate([good, changed]))
        self.assertTrue(probe.evaluate([good, {"frame": 120, "status": "missing"}]))

    def test_unsupported_feature_prevents_final_acceptance(self):
        fixture = Fixture()
        fixture.state["UnsupportedActiveFeature"] = "disk write"
        self.assertTrue(probe.evaluate([fixture.row()]))

    def test_saved_receipt_hash_and_frame_binding(self):
        fixture = Fixture()
        with tempfile.TemporaryDirectory(prefix="copperprobe-analyzer-") as folder:
            directory = Path(folder)
            snapshots = directory / "snapshots"
            snapshots.mkdir()
            state = snapshots / "frame-000060.json"
            state.write_text(json.dumps(fixture.state))
            state.with_suffix(".chipram").write_bytes(fixture.chip)
            state.with_suffix(".slowram").write_bytes(fixture.slow)
            receipt = dict(schemaVersion=1, kind="workbench31-guest-probe-passive-capture",
                           status="capture-complete-not-probe-success", errors=[], process={"exitCode": 0},
                           inputsUnchanged=True, inputsBefore=[{"fixture": "authored"}], inputsAfter=[{"fixture": "authored"}],
                           qualifiedRuntimeBinding={"fixture": "not-a-real-runtime-qualification"},
                           invocation={"token": fixture.TOKEN, "command": "authored fixture"},
                           expectedFrames=[60], snapshotDirectory=str(snapshots),
                           snapshotIdentities=[probe.identity(path) for path in sorted(snapshots.iterdir())])
            capture = directory / "capture-receipt.json"
            capture.write_text(json.dumps(receipt))
            report = probe.analyze(capture, fixture.TOKEN)
            self.assertEqual("complete-probe-record-observed", report["status"])
            self.assertFalse(report["commandParityClaim"])
            state.with_suffix(".slowram").write_bytes(b"changed")
            with self.assertRaisesRegex(probe.InvalidSnapshot, "hash/length changed"):
                probe.analyze(capture, fixture.TOKEN)


if __name__ == "__main__":
    unittest.main()
