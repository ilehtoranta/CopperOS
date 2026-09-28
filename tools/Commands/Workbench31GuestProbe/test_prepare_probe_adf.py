"""Bounded media-preparation checks using authored synthetic FFS bytes only."""
from __future__ import annotations

import importlib.util
from pathlib import Path
import struct
import sys
import tempfile
import unittest


HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location("prepare_probe_adf", HERE / "prepare_probe_adf.py")
prep = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = prep
SPEC.loader.exec_module(prep)


def checksum(block: bytearray, position=5) -> bytes:
    struct.pack_into(">I", block, position * 4, 0)
    words = struct.unpack(">128I", block)
    struct.pack_into(">I", block, position * 4, -sum(words) & 0xFFFFFFFF)
    return bytes(block)


def header(number: int, name: str, parent: int, kind: int) -> bytearray:
    block = bytearray(512)
    struct.pack_into(">2I", block, 0, 2, number)
    struct.pack_into(">3I", block, 420, 100, 200, 300)
    block[432] = len(name)
    block[433:433 + len(name)] = name.encode("ascii")
    struct.pack_into(">I", block, 500, parent)
    struct.pack_into(">i", block, 508, kind)
    return block


def synthetic_adf(with_version=False) -> bytes:
    """Hand-authored DOS1 fixture: no writer filesystem create/format operation."""
    data = bytearray(prep.IMAGE_BYTES)
    data[:4] = b"DOS\1"
    struct.pack_into(">I", data, 8, 880)
    root = header(0, "ProbeFixture", 0, 1)
    struct.pack_into(">I", root, 12, 72)
    struct.pack_into(">2I", root, 312, 0xFFFFFFFF, 881)
    struct.pack_into(">6I", root, 472, 101, 202, 303, 102, 203, 304)
    # DOS1 original-style ignored root residue and stale BSTR padding.
    struct.pack_into('>I', root, 504, 881)
    root[455:463] = b'old text'
    c, s = header(882, "C", 880, 2), header(883, "S", 880, 2)
    for name, number in (("C", 882), ("S", 883)):
        struct.pack_into(">I", root, (6 + prep._name_bucket(name)) * 4, number)
    startup = b"; authored fixture\nC:LoadWB\nEndCLI >NIL:\n"
    unchanged = b"Original unrelated fixture contents\0\xff"
    files = [
        ("Startup-Sequence", 884, 883, startup, 885, s, 0x40),
        ("Untouched", 886, 882, unchanged, 887, c, 0x20),
    ]
    if with_version:
        files.append(('Version', 888, 882, probe_hunk(), 889, c, 0x20))
    for name, number, parent, payload, blocknum, parentblock, protect in files:
        entry = header(number, name, parent, -3)
        struct.pack_into(">I", entry, 8, 1)
        struct.pack_into(">I", entry, 16, blocknum)
        struct.pack_into(">I", entry, 308, blocknum)
        struct.pack_into(">2I", entry, 320, protect, len(payload))
        if name == 'Untouched':
            entry[328:333] = bytes([4]) + b"note"
        struct.pack_into(">I", parentblock, (6 + prep._name_bucket(name)) * 4, number)
        data[number * 512:(number + 1) * 512] = checksum(entry)
        data[blocknum * 512:blocknum * 512 + len(payload)] = payload
    for number, block in ((880, root), (882, c), (883, s)):
        data[number * 512:(number + 1) * 512] = checksum(block)
    bitmap = bytearray(512)
    # Mark only in-range unused blocks free, plus one deliberate padding bit.
    for number in list(range(2, 880)) + list(range(890 if with_version else 888, 1760)) + [1769]:
        bit = number - 2
        offset = (1 + bit // 32) * 4
        value = struct.unpack_from(">I", bitmap, offset)[0] | (1 << (bit % 32))
        struct.pack_into(">I", bitmap, offset, value)
    data[881 * 512:882 * 512] = checksum(bitmap, 0)
    return bytes(data)


def probe_hunk() -> bytes:
    return struct.pack(">10I", 0x3F3, 0, 1, 0, 0, 1, 0x3E9, 1, 0x70004E75, 0x3F2)


class PreparationTests(unittest.TestCase):
    def test_exact_lf_insertion_and_amiga_escaping(self):
        startup = b'; EndCLI ignored\nC:LoadWB\nEndCLI >NIL:\n'
        line = prep.invocation_line('C:Version "a*b"', 20260926)
        self.assertEqual(line, b'C:CopperProbe COMMAND "C:Version *"a**b*"" TOKEN 20260926')
        patched, receipt = prep.insert_invocation(startup, line)
        self.assertEqual(patched, b'; EndCLI ignored\nC:LoadWB\n' + line + b'\nEndCLI >NIL:\n')
        self.assertEqual(receipt['newline_hex'], '0a')

    def test_crlf_preserved(self):
        patched, receipt = prep.insert_invocation(b'C:LoadWB\r\n  C:EndCLI >NIL:\r\n', b'probe')
        self.assertEqual(patched, b'C:LoadWB\r\nprobe\r\n  C:EndCLI >NIL:\r\n')
        self.assertEqual(receipt['newline_hex'], '0d0a')

    def test_ambiguous_or_malformed_startup_rejected(self):
        for startup in (b'EndCLI\nEndCLI\n', b'; EndCLI\n', b'EndCLIX\n', b'EndCLI',
                        b'EndCLI\r', b'CopperProbe\nEndCLI\n', b'\0EndCLI\n'):
            with self.subTest(startup=startup), self.assertRaises(prep.ProbePreparationError):
                prep.insert_invocation(startup, b'probe')

    def test_command_and_token_injection_rejected(self):
        for command in ('C:Version\nEndCLI', 'C:Version\r', 'Version', 'C:Version\targ', 'C:'):
            with self.subTest(command=command), self.assertRaises(prep.ProbePreparationError):
                prep.invocation_line(command, 1)
        for token in (-1, 0x80000000, True):
            with self.subTest(token=token), self.assertRaises(prep.ProbePreparationError):
                prep.invocation_line('C:Version', token)

    def test_narrow_hunk_bounds(self):
        self.assertEqual(prep.validate_probe(probe_hunk())['code_bytes'], 4)
        for payload in (probe_hunk()[:-1], probe_hunk() + b'\0' * 4,
                        probe_hunk().replace(b'\0\0\x03\xe9', b'\0\0\x03\xea')):
            with self.subTest(payload=payload), self.assertRaises(prep.ProbePreparationError):
                prep.validate_probe(payload)

    def test_independent_fixture_inventory(self):
        result = prep.inspect_adf(synthetic_adf())
        self.assertEqual(len(result['entries']), 4)
        self.assertEqual(result['bitmap']['allocated_blocks'], 8)
        self.assertEqual(result['bitmap']['padding_free_bits'], 1)

    def test_invalid_allocation_and_bounds_rejected(self):
        for pointer in (1760, 881, 887):
            changed = bytearray(synthetic_adf())
            entry = bytearray(changed[884 * 512:885 * 512])
            struct.pack_into('>I', entry, 16, pointer)
            struct.pack_into('>I', entry, 308, pointer)
            changed[884 * 512:885 * 512] = checksum(entry)
            with self.subTest(pointer=pointer), self.assertRaises((prep.ProbePreparationError, prep.inventory.EvidenceError)):
                prep.inspect_adf(bytes(changed))

    def test_mismatched_bitmap_rejected(self):
        changed = bytearray(synthetic_adf())
        bitmap = bytearray(changed[881 * 512:882 * 512])
        bitmap[7] ^= 1
        changed[881 * 512:882 * 512] = checksum(bitmap, 0)
        with self.assertRaisesRegex(prep.ProbePreparationError, 'Bitmap does not match'):
            prep.inspect_adf(bytes(changed))

    def test_paths_deny_alias_existing_outside_scope(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source, probe = root / 'original.adf', root / 'probe'
            source.write_bytes(b'original')
            probe.write_bytes(probe_hunk())
            allowed = root / 'diagnostics'
            valid = allowed / 'fresh'
            self.assertEqual(prep.check_paths(source, probe, valid, allowed)[2], valid.resolve())
            for output in (root / 'outside', allowed, allowed / 'deep' / 'child'):
                with self.subTest(output=output), self.assertRaises(prep.ProbePreparationError):
                    prep.check_paths(source, probe, output, allowed)
            with self.assertRaises(prep.ProbePreparationError):
                prep.check_paths(source, source, valid, allowed)
            valid.mkdir(parents=True)
            with self.assertRaises(prep.ProbePreparationError):
                prep.check_paths(source, probe, valid, allowed)

    def test_version_replacement_argument_pair_hash_and_alias(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            source, probe, candidate = (directory / name for name in ('source', 'probe', 'candidate'))
            source.write_bytes(b'original')
            probe.write_bytes(probe_hunk())
            candidate.write_bytes(probe_hunk())
            output = directory / 'fresh'
            expected = prep.sha(candidate.read_bytes())
            self.assertIsNone(prep.read_version_replacement(source, probe, output, 'C:Version', None, None))
            result = prep.read_version_replacement(source, probe, output, 'C:Version dos.library', candidate, expected)
            self.assertEqual(result[0], candidate.resolve())
            self.assertEqual(result[1], probe_hunk())
            for path, digest, command in ((None, expected, 'C:Version'), (candidate, None, 'C:Version'),
                                          (candidate, '0' * 64, 'C:Version'), (candidate, expected, 'C:List'),
                                          (probe, expected, 'C:Version'), (source, expected, 'C:Version'),
                                          (candidate, expected.upper(), 'C:Version')):
                with self.subTest(path=path, digest=digest, command=command), self.assertRaises(prep.ProbePreparationError):
                    prep.read_version_replacement(source, probe, output, command, path, digest)

    def test_replacement_snapshot_name_follows_selected_option_family(self):
        candidate = Path('candidate.hunk')
        self.assertEqual('version-replacement.hunk',
                         prep.replacement_snapshot_name(candidate, None))
        self.assertEqual('command-replacement.hunk',
                         prep.replacement_snapshot_name(None, candidate))
        for version, command in ((None, None), (candidate, candidate)):
            with self.subTest(version=version, command=command), \
                    self.assertRaises(prep.ProbePreparationError):
                prep.replacement_snapshot_name(version, command)

    def test_named_command_replacement_requires_existing_exact_c_member(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            source, probe, candidate = (directory / name for name in ('source', 'probe', 'candidate'))
            source.write_bytes(b'original')
            probe.write_bytes(probe_hunk())
            candidate.write_bytes(probe_hunk())
            output = directory / 'fresh'
            entries = {'c/break': {'path': 'C/Break', 'kind': -3,
                       'sha256': '0' * 64, 'metadata': {'comment_hex': ''}}}
            expected = prep.sha(candidate.read_bytes())
            result = prep.read_command_replacement(source, probe, output,
                'C:Break', 'C/Break', candidate, expected, entries)
            self.assertEqual((candidate.resolve(), probe_hunk(), 'C/Break'),
                             (result[0], result[1], result[3]))
            invalid = (
                ('C:ChangeTaskPri', 'C/Break', candidate, expected, entries),
                ('C:Break', 'C/../S', candidate, expected, entries),
                ('C:Other', 'C/Other', candidate, expected, entries),
                ('C:Break', 'C/Break', candidate, expected.upper(), entries),
                ('C:Break', 'C/Break', candidate, expected,
                 {'c/break': {**entries['c/break'], 'sha256': expected}}),
                ('C:Break', 'C/Break', probe, expected, entries),
            )
            for command, guest_path, path, digest, inventory in invalid:
                with self.subTest(command=command, guest_path=guest_path), \
                        self.assertRaises(prep.ProbePreparationError):
                    prep.read_command_replacement(source, probe, output, command,
                        guest_path, path, digest, inventory)

    def test_tool_source_changes_detected(self):
        with tempfile.TemporaryDirectory() as temporary:
            source = Path(temporary) / 'source.py'
            source.write_bytes(b'original tool source')
            before = [prep.identity(source)]
            self.assertEqual(prep.verify_tool_sources_unchanged(before), before)
            source.write_bytes(b'changed! tool source')
            with self.assertRaisesRegex(prep.ProbePreparationError, 'tool source changed'):
                prep.verify_tool_sources_unchanged(before)

    def test_version_replacement_external_writer_and_exact_metadata(self):
        original = synthetic_adf(with_version=True)
        before = prep.inspect_adf(original)
        startup = prep.inventory.Adf(original).read_file(before['entries']['s/startup-sequence']['header_block'])
        patched, _ = prep.insert_invocation(startup, prep.invocation_line('C:Version dos.library', 20260926))
        authored = probe_hunk()
        # 74 data blocks force an extension block and different allocation/size.
        code = b'\x70\x14\x4e\x75' + bytes(512 * 73)
        candidate = struct.pack('>8I', 0x3F3, 0, 1, 0, 0, len(code) // 4, 0x3E9, len(code) // 4) + code + struct.pack('>I', 0x3F2)
        prep.validate_probe(candidate)
        with tempfile.TemporaryDirectory(prefix='copperos-version-replacement-test-') as temporary:
            directory = Path(temporary)
            for name, data in (('probe.adf', original), ('probe.hunk', authored),
                               ('startup-patched', patched), ('version-replacement.hunk', candidate)):
                (directory / name).write_bytes(data)
            execution = prep.write_copy(directory, before, prep.writer_identity(), version_replacement=True)
            self.assertIn('restore exact original C/Version metadata/protection', execution['result']['operations'])
            derived = (directory / 'probe.adf').read_bytes()
            validation = prep.verify_derivative(original, derived, authored, patched, candidate)
            after = prep.inspect_adf(derived)
            version = after['entries']['c/version']
            self.assertTrue(validation['version_replacement'])
            self.assertEqual(version['metadata'], before['entries']['c/version']['metadata'])
            self.assertEqual(version['metadata']['protection'], 0x20)
            self.assertEqual(len(version['extension_blocks']), 1)
            self.assertEqual(version['sha256'], prep.sha(candidate))
            self.assertNotIn('C/Version', [f['path'] for f in validation['unchanged_files']])
            with self.assertRaises(prep.ProbePreparationError):
                prep.verify_derivative(original, derived, authored, patched)
            with self.assertRaisesRegex(prep.ProbePreparationError, 'replacement content differs'):
                prep.verify_derivative(original, derived, authored, patched, authored)
            for offset in (2 * 512, 887 * 512 + 511):
                changed = bytearray(derived)
                changed[offset] ^= 1
                with self.subTest(offset=offset), self.assertRaises(prep.ProbePreparationError):
                    prep.verify_derivative(original, bytes(changed), authored, patched, candidate)
            for field in (80, 106):
                changed = bytearray(derived)
                number = version['header_block']
                block = bytearray(changed[number * 512:(number + 1) * 512])
                struct.pack_into('>I', block, field * 4, 0)
                changed[number * 512:(number + 1) * 512] = checksum(block)
                with self.subTest(field=field), self.assertRaisesRegex(prep.ProbePreparationError, 'metadata/spelling changed: c/version'):
                    prep.verify_derivative(original, bytes(changed), authored, patched, candidate)

    def test_external_writer_synthetic_roundtrip_and_tamper_rejection(self):
        original = synthetic_adf()
        before = prep.inspect_adf(original)
        startup = prep.inventory.Adf(original).read_file(before['entries']['s/startup-sequence']['header_block'])
        patched, _ = prep.insert_invocation(startup, prep.invocation_line('C:Version dos.library', 20260926))
        authored = probe_hunk()
        with tempfile.TemporaryDirectory(prefix='copperos-probe-test-') as temporary:
            directory = Path(temporary)
            (directory / 'probe.adf').write_bytes(original)
            (directory / 'probe.hunk').write_bytes(authored)
            (directory / 'startup-patched').write_bytes(patched)
            execution = prep.write_copy(directory, before, prep.writer_identity())
            self.assertFalse(execution['result']['filesystem_created'])
            derived = (directory / 'probe.adf').read_bytes()
            result = prep.verify_derivative(original, derived, authored, patched)
            self.assertTrue(result['all_unrelated_payloads_identical'])
            self.assertEqual(result['derived_tree_entries'], 5)
            self.assertEqual(result['derived_bitmap']['padding_free_bits'], 1)
            for offset in (20, 2 * 512, 887 * 512, 887 * 512 + 511):
                changed = bytearray(derived)
                changed[offset] ^= 1
                with self.subTest(offset=offset), self.assertRaises(prep.ProbePreparationError):
                    prep.verify_derivative(original, bytes(changed), authored, patched)
            for number, offset in ((886, 84 * 4), (882, 84 * 4), (880, 455)):
                changed = bytearray(derived)
                block = bytearray(changed[number * 512:(number + 1) * 512])
                block[offset] ^= 1
                changed[number * 512:(number + 1) * 512] = checksum(block)
                with self.subTest(number=number, offset=offset), self.assertRaisesRegex(prep.ProbePreparationError, 'Unauthorized raw block mutation'):
                    prep.verify_derivative(original, bytes(changed), authored, patched)


if __name__ == '__main__':
    unittest.main()
