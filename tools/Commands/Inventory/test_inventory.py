"""Synthetic malformed-media and evidence-integrity regressions; no vendor code."""
from __future__ import annotations

import copy
import json
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest

import inventory as inv


def checksum(block: bytearray) -> None:
    struct.pack_into(">I", block, 20, 0)
    value = (-sum(struct.unpack(">128I", block))) & 0xffffffff
    struct.pack_into(">I", block, 20, value)


def adf_fixture(fast: bool = True) -> tuple[bytes, bytes]:
    image = bytearray(20 * 512)
    image[:4] = b"DOS" + bytes([int(fast)])
    struct.pack_into(">I", image, 8, 10)
    root = bytearray(512)
    for offset, value in ((0, 2), (12, 72), (24, 3), (508, 1)):
        struct.pack_into(">I", root, offset, value)
    root[432:437] = b"\x04Test"
    checksum(root)
    image[10 * 512:11 * 512] = root
    contents = b"\0\0\x03\xf3\0$VER: Probe 40.1 (1.1.1994)\0FILE/A,QUIET/S\0"
    header = bytearray(512)
    for offset, value in ((0, 2), (4, 3), (8, 1), (16, 5), (308, 5),
                          (320, 32), (324, len(contents)), (500, 10), (508, 0xfffffffd)):
        struct.pack_into(">I", header, offset, value)
    header[432:438] = b"\x05Probe"
    checksum(header)
    image[3 * 512:4 * 512] = header
    if fast:
        image[5 * 512:5 * 512 + len(contents)] = contents
    else:
        block = bytearray(512)
        for offset, value in ((0, 8), (4, 3), (8, 1), (12, len(contents))):
            struct.pack_into(">I", block, offset, value)
        block[24:24 + len(contents)] = contents
        checksum(block)
        image[5 * 512:6 * 512] = block
    return bytes(image), contents


def mutate_adf(data: bytes, block_number: int, offset: int, value: int) -> bytes:
    result = bytearray(data)
    block = bytearray(result[block_number * 512:(block_number + 1) * 512])
    struct.pack_into(">I", block, offset, value)
    checksum(block)
    result[block_number * 512:(block_number + 1) * 512] = block
    return bytes(result)


def both32(value: int) -> bytes:
    return struct.pack("<I", value) + struct.pack(">I", value)


def iso_record(name: bytes, extent: int, size: int, directory: bool = False) -> bytes:
    record = bytearray(33 + len(name) + (len(name) % 2 == 0))
    record[0] = len(record)
    record[2:10] = both32(extent)
    record[10:18] = both32(size)
    record[25] = 2 if directory else 0
    record[28:32] = b"\x01\x00\x00\x01"
    record[32] = len(name)
    record[33:33 + len(name)] = name
    return bytes(record)


def iso_fixture() -> bytes:
    image = bytearray(24 * 2048)
    descriptor = bytearray(2048)
    descriptor[:7] = b"\x01CD001\x01"
    descriptor[40:72] = b"TEST".ljust(32, b" ")
    descriptor[80:88] = both32(24)
    descriptor[128:132] = b"\x00\x08\x08\x00"
    root = iso_record(b"\0", 20, 2048, True)
    descriptor[156:156 + len(root)] = root
    image[16 * 2048:17 * 2048] = descriptor
    file = iso_record(b"Probe;1", 21, 4)
    image[20 * 2048:20 * 2048 + len(root) + len(file)] = root + file
    image[21 * 2048:21 * 2048 + 4] = b"\0\0\x03\xf3"
    return bytes(image)


class MediaTests(unittest.TestCase):
    def test_workbench_mount_leading_comma_template_candidate(self):
        data = (b"\0\0\x03\xf3\0$VER: mount 40.4 (27.9.93)\0"
                b"DEVICE/M,FROM/K\0f,SECTORSIZE=BLOCKSIZE,,SURFACES,"
                b"FORCELOAD\0")
        facts = inv.binary_facts(data, "Mount")
        candidates = facts["template_candidates"]
        self.assertEqual(candidates[0]["text"], "DEVICE/M,FROM/K")
        self.assertEqual(candidates[1]["text"],
                         ",SECTORSIZE=BLOCKSIZE,,SURFACES,FORCELOAD")
        self.assertEqual(candidates[1]["byte_offset"], data.index(b",SECTORSIZE"))
        self.assertIn("leading-comma", candidates[1]["confidence"])

    def test_ffs_and_ofs_return_exact_payload_and_native_p_flag(self):
        for fast in (True, False):
            image, contents = adf_fixture(fast)
            adf = inv.Adf(image)
            entry = adf.walk()["probe"]
            facts = adf.file_facts(entry, "synthetic")
            self.assertEqual(adf.read_file(entry["block"]), contents)
            self.assertTrue(facts["protection"]["pure"])
            self.assertEqual(facts["version"]["version"], 40)
            self.assertFalse(facts["contract_complete"])

    def test_corrupt_header_is_not_accepted_as_metadata(self):
        image, _ = adf_fixture()
        damaged = bytearray(image)
        damaged[10 * 512 + 100] ^= 1
        with self.assertRaisesRegex(inv.EvidenceError, "checksum"):
            inv.Adf(bytes(damaged))

    def test_directory_hash_cycle_is_rejected_even_with_valid_checksum(self):
        image, _ = adf_fixture()
        image = mutate_adf(image, 3, 496, 3)
        with self.assertRaisesRegex(inv.EvidenceError, "cyclic"):
            inv.Adf(image).walk()

    def test_file_extension_cycle_is_rejected(self):
        image, _ = adf_fixture()
        image = mutate_adf(image, 3, 504, 3)
        with self.assertRaisesRegex(inv.EvidenceError, "extension cycle"):
            inv.Adf(image).read_file(3)

    def test_file_length_cannot_claim_unavailable_blocks(self):
        image, _ = adf_fixture()
        image = mutate_adf(image, 3, 324, 513)
        with self.assertRaisesRegex(inv.EvidenceError, "block count"):
            inv.Adf(image).read_file(3)

    def test_iso_file_and_mismatched_endian_extent(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "synthetic.iso"
            image = iso_fixture()
            path.write_bytes(image)
            with inv.Iso(path) as iso:
                facts = iso.file_facts(iso.walk()["probe"], "synthetic")
                self.assertIsNone(facts["protection"]["pure"])
                self.assertEqual(facts["file_format"], "amiga-hunk")
            damaged = bytearray(image)
            damaged[16 * 2048 + 156 + 6] ^= 1
            path.write_bytes(damaged)
            with self.assertRaisesRegex(inv.EvidenceError, "both-endian"):
                inv.Iso(path)

    def test_morphos_capture_audit_rejects_hash_drift(self):
        image = iso_fixture()
        with tempfile.TemporaryDirectory() as folder:
            iso_path = Path(folder) / "synthetic.iso"
            audit_path = Path(folder) / "probe-audit.json"
            iso_path.write_bytes(image)
            with inv.Iso(iso_path) as iso:
                entry = iso.walk()["probe"]
                entry["path"] = "MorphOS/C/Probe"
                audit = {
                    "schema_version": inv.SCHEMA_VERSION,
                    "profile": "morphos320",
                    "source_id": "synthetic",
                    "command": "Probe",
                    "path": "MorphOS/C/Probe",
                    "iso_extent": 21,
                    "bytes": 4,
                    "sha256": inv.digest(b"wrong"),
                }
                audit_path.write_text(json.dumps(audit), encoding="utf-8")
                with self.assertRaisesRegex(inv.EvidenceError, "hash differs"):
                    inv.validate_morphos_capture_audit(
                        audit_path, "probe", entry, iso, "synthetic")

    def test_conclip_private_console_audit_freezes_vectors_and_media_hashes(self):
        audit_path = (inv.DEFAULT_OUTPUT / "reference-captures" /
                      "conclip-console-private-api-audit-20260923.json")
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
        source = audit["source_media"]
        self.assertEqual(source["bytes"], 287)
        self.assertRegex(source["sha256"], r"^[0-9a-f]{64}$")
        self.assertRegex(source["sha256_file"], r"^[0-9a-f]{64}$")
        self.assertEqual(
            [(item["name"], item["lvo"]) for item in audit["fd"]["private"]],
            [("GetConSnip", -54), ("SetConSnip", -60),
             ("AddConSnipHook", -66), ("RemConSnipHook", -72)])
        self.assertEqual(audit["fd"]["private"][1]["registers"],
                         {"A0": "APTR data"})
        self.assertEqual(audit["fd"]["private"][2]["registers"],
                         {"A0": "struct Hook *hook"})
        conclip = json.loads((inv.DEFAULT_OUTPUT / "reference-captures" /
                              "conclip-reference-audit-20260923.json")
                             .read_text(encoding="utf-8"))
        self.assertEqual(conclip["profiles"]["morphos320"]
                         ["startup_contract"]["worker_code_type"],
                         "MACHINE_PPC")

    def test_reboot_capture_audit_freezes_media_and_source_identity(self):
        audit_path = (inv.DEFAULT_OUTPUT / "reference-captures" /
                      "reboot-morphos-binary-audit-20260923.json")
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
        self.assertEqual(audit["command"], "Reboot")
        self.assertEqual(audit["path"], "MorphOS/C/Reboot")
        self.assertEqual(audit["iso_extent"], 178088)
        self.assertEqual(audit["bytes"], 1206)
        self.assertRegex(audit["sha256"], r"^[0-9a-f]{64}$")
        self.assertEqual(audit["source"]["bytes"], 955)
        self.assertEqual(
            audit["source"]["sha256"],
            "9e3cc8dd4f9fba123fb3507f86333c09164cf05e26b64ec237125d1c10990fc5")
        self.assertEqual(audit["startup_contract"]["return_if_reboot_returns"], 666)

    def test_avail_capture_audit_freezes_media_and_source_identity(self):
        audit_path = (inv.DEFAULT_OUTPUT / "reference-captures" /
                      "avail-morphos-binary-audit-20260923.json")
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
        self.assertEqual(audit["command"], "Avail")
        self.assertEqual(audit["path"], "MorphOS/C/Avail")
        self.assertEqual(audit["iso_extent"], 175208)
        self.assertEqual(audit["bytes"], 3999)
        self.assertRegex(audit["sha256"], r"^[0-9a-f]{64}$")
        self.assertEqual(audit["template"]["text"],
                         "CHIP/S,FAST/S,TOTAL/S,FLUSH/S,H=HUMAN/S")
        self.assertEqual(audit["source"]["bytes"], 9191)
        self.assertEqual(
            audit["source"]["sha256"],
            "90fd07e3edff26f5cc80fec358c7f28ec06ab422d68a75c903d1c25d3ca857c")

    def test_date_capture_audit_freezes_media_and_source_identity(self):
        audit_path = (inv.DEFAULT_OUTPUT / "reference-captures" /
                      "date-morphos-binary-audit-20260923.json")
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
        self.assertEqual(audit["command"], "Date")
        self.assertEqual(audit["path"], "MorphOS/C/Date")
        self.assertEqual(audit["iso_extent"], 175288)
        self.assertEqual(audit["bytes"], 3748)
        self.assertRegex(audit["sha256"], r"^[0-9a-f]{64}$")
        self.assertEqual(audit["template"]["text"],
                         "DAY,DATE,TIME,TO=VER/K,LFORMAT/K")
        self.assertEqual(audit["source"]["bytes"], 9453)
        self.assertEqual(
            audit["source"]["sha256"],
            "c63217b0383ad0861bdd167838dc03c22dab74555d96819fdc0f229b148d86ee")

    def test_status_capture_audit_freezes_media_and_source_identity(self):
        audit_path = (inv.DEFAULT_OUTPUT / "reference-captures" /
                      "status-morphos-binary-audit-20260923.json")
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
        self.assertEqual(audit["command"], "Status")
        self.assertEqual(audit["path"], "MorphOS/C/Status")
        self.assertEqual(audit["iso_extent"], 178472)
        self.assertEqual(audit["bytes"], 3679)
        self.assertRegex(audit["sha256"], r"^[0-9a-f]{64}$")
        self.assertEqual(
            audit["template"]["text"],
            "PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K")
        self.assertEqual(audit["source"]["bytes"], 5709)
        self.assertEqual(
            audit["source"]["sha256"],
            "bb92acf52c915f3549c57cb8253a58d60ee6912e751aa509495ac5d04b12dc25")

    def test_workbench_status_capture_freezes_disk_member_identity(self):
        audit_path = (inv.DEFAULT_OUTPUT / "reference-captures" /
                      "wb31-disk2-c-command-captures-20260923.json")
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
        entry = next(item for item in audit["commands"]
                     if item["name"].lower() == "status")
        self.assertEqual(entry["path"], "C:/Status")
        self.assertEqual(entry["bytes"], 828)
        self.assertEqual(
            entry["sha256"],
            "fd7b386f103bafba80add97523991389880f9b6987f4534280abb61b8193580c")

    def test_workbench_status_binary_audit_binds_candidate_contract(self):
        audit_path = (inv.DEFAULT_OUTPUT / "reference-captures" /
                      "status-wb31-binary-audit-20260923.json")
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
        self.assertEqual(audit["command"], "Status")
        self.assertEqual(audit["path"], "C/Status")
        self.assertEqual(audit["bytes"], 828)
        self.assertEqual(audit["version"]["byte_offset"], 615)
        self.assertEqual(audit["template"]["byte_offset"], 568)
        self.assertEqual(audit["template"]["text"],
                         "PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K")

    def test_redirected_resident_remove_and_installer_pure_are_captured(self):
        script = (b'Resident >NIL: MOSSYS:C/Assign PURE\nResident >NIL: Assign REMOVE\n'
                  b'(run (cat "Resident " (tackon installPath "C/IconPos PURE")) (safe))\n'
                  b'(run "FindResident A3000")\n')
        facts = inv.resident_events(script, "synthetic", "script")
        self.assertEqual([(item["command_id"], item["operation"]) for item in facts],
                         [("assign", "resident-add"), ("assign", "resident-remove"), ("iconpos", "resident-add")])


class LedgerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.inventory = json.loads((inv.DEFAULT_OUTPUT / "command-inventory.json").read_text(encoding="utf-8"))
        cls.evidence = json.loads((inv.DEFAULT_OUTPUT / "media-evidence.json").read_text(encoding="utf-8"))

    def test_generated_ledger_validates(self):
        inv.validate(self.inventory, self.evidence, inv.DEFAULT_PLAN)

    def test_deleting_a_required_command_is_detected(self):
        changed = copy.deepcopy(self.inventory)
        changed["commands"].pop()
        with self.assertRaisesRegex(inv.EvidenceError, "missing command"):
            inv.validate(changed, self.evidence, inv.DEFAULT_PLAN)

    def test_omitting_original_pure_evidence_is_detected(self):
        changed = copy.deepcopy(self.inventory)
        command = next(item for item in changed["commands"] if item["id"] == "assign")
        purity = command["reference_profiles"]["morphos320"]["purity"]
        purity["event_ids"] = []
        purity["required_by_observed_design_evidence"] = False
        with self.assertRaisesRegex(inv.EvidenceError, "omitted"):
            inv.validate(changed, self.evidence, inv.DEFAULT_PLAN)

    def test_iso_posix_mode_cannot_be_promoted_to_amigados_pure(self):
        changed = copy.deepcopy(self.inventory)
        command = next(item for item in changed["commands"] if "morphos320" in item["reference_profiles"])
        command["reference_profiles"]["morphos320"]["source_files"][0]["protection"]["pure"] = True
        with self.assertRaisesRegex(inv.EvidenceError, "Do not infer"):
            inv.validate(changed, self.evidence, inv.DEFAULT_PLAN)

    def test_matching_but_fabricated_complete_coverage_is_detected(self):
        inventory, evidence = copy.deepcopy(self.inventory), copy.deepcopy(self.evidence)
        inventory["coverage"]["installed_system_observed"] = True
        evidence["coverage"]["installed_system_observed"] = True
        with self.assertRaisesRegex(inv.EvidenceError, "presented as complete"):
            inv.validate(inventory, evidence, inv.DEFAULT_PLAN)

    def test_strict_completion_gate_returns_incomplete_exit_code(self):
        result = subprocess.run([sys.executable, str(Path(inv.__file__)), "verify", "--strict-complete"],
                                cwd=inv.ROOT, capture_output=True, text=True, check=False)
        self.assertEqual(result.returncode, 2, result.stdout + result.stderr)
        self.assertEqual(json.loads(result.stdout)["validation"], "passed")


if __name__ == "__main__":
    unittest.main()
