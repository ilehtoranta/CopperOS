"""Malformed executable controls and independent preservation checks."""
import hashlib
from pathlib import Path
import struct
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from append_hunk_version import HunkError, append_file, append_version, parse_hunk


VERSION = "$VER: MakeLink 0.1 (8.9.2026) CopperOS wb31"


def words(*values):
    return struct.pack(">" + "I" * len(values), *values)


def fixture(relocations=True, symbols=True):
    # RTS/NOP, one absolute pointer at +4 to +12, then RTS/NOP. Code is data
    # for this structural test, never executed on the host.
    image = words(0x3F3, 0, 1, 0, 0, 4, 0x3E9, 4, 0x4E754E71, 12, 0x4E714E71, 0x4E754E71)
    if relocations:
        image += words(0x3EC, 1, 0, 4, 0)
    if symbols:
        image += words(0x3F0, 1) + b"Main" + words(0, 0)
    return image + words(0x3F2)


def changed(image, offset, value):
    output = bytearray(image)
    struct.pack_into(">I", output, offset, value)
    return bytes(output)


class HunkVersionTests(unittest.TestCase):
    def test_exact_bytes_and_loaded_relocation_are_preserved(self):
        image = fixture()
        output, report = append_version(image, VERSION)
        metadata = b"\0" + VERSION.encode() + b"\0"
        metadata += bytes(-len(metadata) % 4)
        expected_header = words(0x3F3, 0, 1, 0, 0, (16 + len(metadata)) // 4, 0x3E9, (16 + len(metadata)) // 4)
        self.assertEqual(output, expected_header + image[32:48] + metadata + image[48:])
        self.assertEqual(report["version_file_offset"], 49)
        # A loader adding either base sees the same pointer, independent of the
        # new allocation length. This checks actual relocation offset/addend.
        for base in (0x1000, 0x20000):
            for data in (image, output):
                pointer = struct.unpack_from(">I", data, 36)[0] + base
                self.assertEqual(pointer, base + 12)
        self.assertFalse(report["shipping"])
        self.assertFalse(report["pure_admission"])

    def test_optional_records(self):
        for relocations in (False, True):
            for symbols in (False, True):
                with self.subTest(relocations=relocations, symbols=symbols):
                    original = fixture(relocations, symbols)
                    output, report = append_version(original, VERSION)
                    self.assertEqual(parse_hunk(output).tail, parse_hunk(original).tail)
                    self.assertEqual(report["relocations"], int(relocations))
                    self.assertEqual(report["symbols"], int(symbols))

    def test_all_truncated_prefixes_rejected(self):
        data = fixture()
        for end in range(len(data)):
            with self.subTest(end=end), self.assertRaises(HunkError):
                append_version(data[:end], VERSION)

    def test_header_corruptions_rejected(self):
        for offset, value in ((0, 0), (4, 1), (8, 2), (12, 1), (16, 1),
                              (20, 0), (20, 0x80000004), (20, 5), (24, 0x3EA), (28, 3), (28, 0xFFFFFFFF)):
            with self.subTest(offset=offset, value=value), self.assertRaises(HunkError):
                append_version(changed(fixture(), offset, value), VERSION)

    def test_relocation_bounds_and_group_corruptions_rejected(self):
        for offset, value in ((52, 0xFFFFFFFF), (56, 1), (60, 3), (60, 14), (60, 16), (36, 16), (36, 0xFFFFFFFF)):
            with self.subTest(offset=offset, value=value), self.assertRaises(HunkError):
                append_version(changed(fixture(), offset, value), VERSION)

    def test_duplicate_and_overlapping_relocations_rejected(self):
        for second in (4, 6):
            image = fixture(False, False)[:-4] + words(0x3EC, 2, 0, 4, second, 0, 0x3F2)
            with self.subTest(second=second), self.assertRaises(HunkError):
                append_version(image, VERSION)

    def test_duplicate_and_out_of_order_records_rejected(self):
        base = fixture(False, False)[:-4]
        relocation, symbol = words(0x3EC, 0), words(0x3F0, 0)
        for records in (relocation + relocation, symbol + symbol, symbol + relocation, words(0x3F1, 0)):
            with self.subTest(records=records.hex()), self.assertRaises(HunkError):
                append_version(base + records + words(0x3F2), VERSION)

    def test_bad_symbol_bounds_text_and_duplicate_rejected(self):
        base = fixture(False, False)[:-4]
        for symbol in (words(0xFFFFFFFF), words(1) + b"A\0B\0" + words(0),
                       words(1) + b"\0\0\0\0" + words(0), words(1) + b"Main" + words(16),
                       (words(1) + b"Main" + words(0)) * 2):
            with self.subTest(symbol=symbol.hex()), self.assertRaises(HunkError):
                append_version(base + words(0x3F0) + symbol + words(0, 0x3F2), VERSION)

    def test_extra_bytes_after_end_rejected(self):
        for extra in (b"\0", words(0), words(0x3F2), fixture()):
            with self.subTest(extra=extra.hex()), self.assertRaises(HunkError):
                append_version(fixture() + extra, VERSION)

    def test_duplicate_tag_and_invalid_version_rejected(self):
        output, _ = append_version(fixture(), VERSION)
        with self.assertRaises(HunkError):
            append_version(output, VERSION)
        for version in ("MakeLink 0.1", VERSION + "\n", VERSION + "\0", VERSION + " $VER: Other", VERSION + "x" * 256, VERSION + "\u0100"):
            with self.subTest(version=version), self.assertRaises(HunkError):
                append_version(fixture(), version)

    def test_deterministic_with_repeated_unchanged_input(self):
        self.assertEqual(append_version(fixture(), VERSION), append_version(fixture(), VERSION))

    def test_input_hash_and_no_overwrite_fail_closed(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, output, receipt = [root / name for name in ("raw.hunk", "versioned.hunk", "receipt.json")]
            source.write_bytes(fixture())
            before = source.read_bytes()
            with self.assertRaises(HunkError):
                append_file(source, output, receipt, VERSION, "0" * 64)
            self.assertFalse(output.exists())
            result = append_file(source, output, receipt, VERSION, hashlib.sha256(before).hexdigest())
            self.assertEqual(source.read_bytes(), before)
            self.assertEqual(hashlib.sha256(output.read_bytes()).hexdigest(), result["output_sha256"])
            for destination, proof in ((output, receipt), (source, root / "new.json"), (root / "new.hunk", receipt)):
                with self.subTest(destination=destination, proof=proof), self.assertRaises(HunkError):
                    append_file(source, destination, proof, VERSION, hashlib.sha256(before).hexdigest())
            self.assertEqual(source.read_bytes(), before)


if __name__ == "__main__":
    unittest.main()
