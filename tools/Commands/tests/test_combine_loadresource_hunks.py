"""Independent scatter-loading and rejection checks for the two-root packer."""
from __future__ import annotations

import importlib.util
import json
import os
from pathlib import Path
import struct
import sys
import tempfile
import unittest


TOOL = Path(__file__).resolve().parents[1] / "combine_loadresource_hunks.py"
SPEC = importlib.util.spec_from_file_location("combine_loadresource_hunks", TOOL)
packer = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = packer
SPEC.loader.exec_module(packer)


def longs(*values: int) -> bytes:
    return struct.pack(">" + "I" * len(values), *values)


def fixture(value: int, relocations: tuple[int, ...] = (6, 12),
            target: int = 0, symbols: bool = True) -> bytes:
    # BRA.S over NOP; LEA value,A0; JSR helper; RTS; helper MOVE.L (A0),D0;
    # RTS; NOP; data. Relocations target the constant and machine-code helper.
    code = (bytes.fromhex("60024e7141f9") + longs(24) +
            bytes.fromhex("4eb9") + longs(18) +
            bytes.fromhex("4e7520104e754e71") + longs(value))
    body = longs(0x3E9, len(code) // 4) + code
    if relocations:
        body += longs(0x3EC, len(relocations), target, *relocations, 0)
    if symbols:
        body += longs(0x3F0)
        for name, address in ((b"entry\0\0\0", 0), (b"helper\0\0", 18), (b"value\0\0\0", 24)):
            body += longs(len(name) // 4) + name + longs(address)
        body += longs(0)
    return longs(0x3F3, 0, 1, 0, 0, len(code) // 4) + body + longs(0x3F2)


def replace_word(data: bytes, offset: int, value: int) -> bytes:
    result = bytearray(data)
    struct.pack_into(">I", result, offset, value)
    return bytes(result)


def scatter_load(data: bytes, bases: tuple[int, int]) -> list[bytearray]:
    """Test-only loader independent of the packer's parser and relocation model."""
    position = 0

    def take() -> int:
        nonlocal position
        value = struct.unpack_from(">I", data, position)[0]
        position += 4
        return value

    assert [take() for _ in range(5)] == [0x3F3, 0, 2, 0, 1]
    sizes = [take() * 4 for _ in bases]
    loaded: list[bytearray] = []
    pending: list[tuple[int, int, int]] = []
    for source, size in enumerate(sizes):
        assert take() == 0x3E9 and take() * 4 == size
        loaded.append(bytearray(data[position:position + size]))
        position += size
        while (record := take()) != 0x3F2:
            if record == 0x3EC:
                while (count := take()) != 0:
                    target = take()
                    for _ in range(count):
                        pending.append((source, target, take()))
            elif record == 0x3F0:
                while (count := take()) != 0:
                    position += count * 4
                    take()
            else:
                raise AssertionError(f"Unexpected packed record {record:x}")
    assert position == len(data)
    for source, target, offset in pending:
        old = struct.unpack_from(">I", loaded[source], offset)[0]
        struct.pack_into(">I", loaded[source], offset, bases[target] + old)
    return loaded


def execute_fixture(images: list[bytearray], bases: tuple[int, int], entry: int) -> tuple[int, int, list[int]]:
    """Execute only the six fixture opcodes, not the production compiler/CPU."""
    def read(address: int, size: int) -> int:
        for base, image in zip(bases, images):
            if base <= address <= base + len(image) - size:
                return int.from_bytes(image[address - base:address - base + size], "big")
        raise AssertionError(f"Instruction/data pointer escaped both images: {address:x}")

    pc, a0, d0 = entry, 0, 0
    returns: list[int] = []
    calls: list[int] = []
    for _ in range(20):
        op = read(pc, 2)
        if op == 0x6002:
            pc += 4
        elif op == 0x4E71:
            pc += 2
        elif op == 0x41F9:
            a0 = read(pc + 2, 4)
            pc += 6
        elif op == 0x4EB9:
            returns.append(pc + 6)
            pc = read(pc + 2, 4)
            calls.append(pc)
        elif op == 0x2010:
            d0 = read(a0, 4)
            pc += 2
        elif op == 0x4E75:
            if not returns:
                return d0, a0, calls
            pc = returns.pop()
        else:
            raise AssertionError(f"Unexpected instruction {op:x} at {pc:x}")
    raise AssertionError("Fixture failed to return")


class CombineHunksTests(unittest.TestCase):
    def test_independent_machine_entries_and_constants_survive_scatter_loading(self):
        client, worker = fixture(0x12345678), fixture(0x87654321)
        packed, receipt = packer.combine_hunks(client, worker)
        bases = (0x180000, 0x920000)
        images = scatter_load(packed, bases)
        self.assertEqual(execute_fixture(images, bases, bases[0]),
                         (0x12345678, bases[0] + 24, [bases[0] + 18]))
        self.assertEqual(execute_fixture(images, bases, bases[1]),
                         (0x87654321, bases[1] + 24, [bases[1] + 18]))
        self.assertEqual(images[0][:6], images[1][:6])  # PC-relative branch preserved.
        self.assertEqual(packed[28:28 + len(client) - 24], client[24:])
        self.assertEqual(receipt["worker_relocation_groups_remapped"], 1)
        self.assertFalse(receipt["cross_image_relocations_added"])
        self.assertFalse(receipt["shipping"])

    def test_no_relocations_or_symbols_remains_valid(self):
        data = fixture(7, (), symbols=False)
        packed, receipt = packer.combine_hunks(data, data)
        self.assertEqual(scatter_load(packed, (0x10000, 0x90000)),
                         [bytearray(data[32:60]), bytearray(data[32:60])])
        self.assertEqual(receipt["worker_relocation_groups_remapped"], 0)

    def test_every_truncation_is_rejected(self):
        valid = fixture(1)
        for end in range(len(valid)):
            with self.subTest(length=end), self.assertRaises(packer.HunkError):
                packer.parse_single_code(valid[:end])

    def test_unsupported_header_record_and_length_forms(self):
        valid = fixture(1)
        changes = [(0, 0x3E7), (4, 1), (8, 2), (12, 1), (16, 1),
                   (20, 0), (20, 0x40000007), (20, 8),
                   (24, 0x3EA), (24, 0x3EB), (24, 0x400003E9), (28, 6),
                   (60, 0x3ED), (60, 0x3F1), (64, 0xFFFFFFFF),
                   (68, 1), (68, 0xFFFFFFFF)]
        for offset, value in changes:
            with self.subTest(offset=offset, value=value), self.assertRaises(packer.HunkError):
                packer.parse_single_code(replace_word(valid, offset, value))
        with self.assertRaises(packer.HunkError):
            packer.parse_single_code(valid + longs(0))

    def test_illegal_duplicate_and_overlapping_relocations(self):
        for offsets in ((5,), (25,), (28,), (0xFFFFFFFE,), (6, 6), (6, 8)):
            with self.subTest(offsets=offsets), self.assertRaises(packer.HunkError):
                packer.parse_single_code(fixture(1, offsets))
        for value in (28, 0xFFFFFFFF):
            with self.subTest(addend=value), self.assertRaises(packer.HunkError):
                packer.parse_single_code(replace_word(fixture(1), 38, value))

    def test_repeated_and_out_of_order_records(self):
        valid = fixture(1)
        relocation_record = valid[60:84]
        for data in (valid[:-4] + relocation_record + valid[-4:],
                     valid[:84] + relocation_record + valid[84:],
                     valid[:-4] + longs(0x3F0, 0, 0x3F2)):
            with self.subTest(data=data.hex()), self.assertRaises(packer.HunkError):
                packer.parse_single_code(data)

    def test_invalid_symbol_names_addresses_and_duplicates(self):
        base = fixture(1, symbols=False)[:-4] + longs(0x3F0)
        names = [longs(1) + b"\0\0\0\0" + longs(0),
                 longs(1) + b"a\0x\0" + longs(0),
                 longs(1) + b"a\xff\0\0" + longs(0),
                 longs(1) + b"okay" + longs(28),
                 (longs(1) + b"okay" + longs(0)) * 2,
                 longs(0xFFFFFFFF)]
        for symbol in names:
            with self.subTest(symbol=symbol.hex()), self.assertRaises(packer.HunkError):
                packer.parse_single_code(base + symbol + longs(0, 0x3F2))

    def test_file_hash_receipt_and_exclusive_creation(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            client, worker, output, receipt = [root / name for name in
                                              ("client", "worker", "output", "receipt.json")]
            client.write_bytes(fixture(1))
            worker.write_bytes(fixture(2))
            hashes = (packer.digest(client.read_bytes()), packer.digest(worker.read_bytes()))
            result = packer.combine_files(client, worker, output, receipt, *hashes)
            self.assertEqual(json.loads(receipt.read_text()), result)
            self.assertEqual(result["output_sha256"], packer.digest(output.read_bytes()))
            before = output.read_bytes()
            with self.assertRaises(packer.HunkError):
                packer.combine_files(client, worker, output, root / "newreceipt", *hashes)
            self.assertEqual(output.read_bytes(), before)

    def test_alias_paths_existing_receipt_and_wrong_hash_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            client, worker = root / "client", root / "worker"
            client.write_bytes(fixture(1)); worker.write_bytes(fixture(2))
            hashes = (packer.digest(client.read_bytes()), packer.digest(worker.read_bytes()))
            combinations = [(client, worker, client, root / "r"),
                            (client, client, root / "o", root / "r"),
                            (client, worker, root / "o", root / "o"),
                            (client, worker, root / "o", worker)]
            for paths in combinations:
                with self.subTest(paths=paths), self.assertRaises(packer.HunkError):
                    packer.combine_files(*paths, *hashes)
            for bad in ("0" * 64, "BAD", hashes[0].upper()):
                with self.subTest(hash=bad), self.assertRaises(packer.HunkError):
                    packer.combine_files(client, worker, root / "o", root / "r", bad, hashes[1])
            existing = root / "r"
            existing.write_text("historical")
            with self.assertRaises(packer.HunkError):
                packer.combine_files(client, worker, root / "o", existing, *hashes)
            self.assertFalse((root / "o").exists())
            self.assertEqual(existing.read_text(), "historical")

    def test_hardlinked_inputs_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            client, alias = root / "client", root / "alias"
            client.write_bytes(fixture(1))
            try:
                os.link(client, alias)
            except OSError as error:
                self.skipTest(f"Hard links unavailable: {error}")
            expected = packer.digest(client.read_bytes())
            with self.assertRaises(packer.HunkError):
                packer.combine_files(client, alias, root / "o", root / "r", expected, expected)


if __name__ == "__main__":
    unittest.main()
