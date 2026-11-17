import hashlib
import importlib.util
import json
import tempfile
import struct
import subprocess
import sys
import unittest
from pathlib import Path

path = Path(__file__).resolve().parents[1] / 'build_system_image.py'
spec = importlib.util.spec_from_file_location('system_image', path)
image = importlib.util.module_from_spec(spec)
spec.loader.exec_module(image)


class OriginalSystemImageTests(unittest.TestCase):
    def test_normal_system_image_stages_exact_payloads_and_preserves_other_files(self):
        stage_spec = importlib.util.spec_from_file_location('stage_system_image', path.parent / 'stage_system_image.py')
        stage = importlib.util.module_from_spec(stage_spec)
        stage_spec.loader.exec_module(stage)
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            manifest = self.fixture(directory)
            record = json.loads(manifest.read_text())
            record['kind'] = 'copperos-system-native'
            manifest.write_text(json.dumps(record))
            data, files = image.build(manifest)
            image.verify(data, files)
            disk_path = directory / 'System.adf'
            disk_path.write_bytes(data)
            receipt = directory / 'system-image.json'
            receipt.write_text(json.dumps(dict(kind='copperos-system-image', shipping=False,
                pairId=image.pair_id(manifest.read_bytes()), sha256=image.digest(data), bytes=len(data), files=files)))
            system = directory / 'SYS'
            system.mkdir()
            (system / 'unrelated').write_bytes(b'keep')
            staged = stage.stage(disk_path, receipt, system)
            self.assertEqual(files, staged['files'])
            self.assertEqual(b'keep', (system / 'unrelated').read_bytes())
            self.assertEqual((directory / 'Shell-Seg.hunk').read_bytes(), (system / 'L/Shell-Seg').read_bytes())
            self.assertEqual(b'; CopperOS system startup\nC:BootShell\n', (system / 'S/Startup-Sequence').read_bytes())
            self.assertTrue(staged['pairId'].startswith('a500-copperos-system-dev.1.'))
            # A failed repeat cannot replace the previously staged Shell.
            original = (system / 'L/Shell-Seg').read_bytes()
            disk_path.write_bytes(data[:-1] + bytes([data[-1] ^ 1]))
            with self.assertRaisesRegex(ValueError, 'does not match'):
                stage.stage(disk_path, receipt, system)
            self.assertEqual(original, (system / 'L/Shell-Seg').read_bytes())

    def test_normal_build_does_not_admit_shipping_or_unknown_manifest(self):
        with tempfile.TemporaryDirectory() as tmp:
            manifest = self.fixture(Path(tmp))
            record = json.loads(manifest.read_text())
            for kind, shipping in [('copperos-system-native', True), ('untrusted', False)]:
                record.update(kind=kind, shipping=shipping)
                manifest.write_text(json.dumps(record))
                with self.assertRaisesRegex(ValueError, 'nonshipping'):
                    image.build(manifest)

    def test_helper_volume_is_checked_by_the_independent_reader(self):
        disk = image.Disk()
        disk.header(disk.root, 0, 'ProbeHelper', 1)
        disk.checksum(disk.root)
        disk.file(disk.root, 'LongPath', b'probe')
        data = disk.finish()
        image.verify(data, disk.files, 'ProbeHelper')
        with self.assertRaises(ValueError):
            image.verify(data, disk.files)

    def test_hdf_multiple_bitmaps_and_payload_beyond_floppy_capacity(self):
        disk = image.Disk(16384)
        directory = disk.directory(disk.root, 'L')
        disk.file(directory, 'L/Large', bytes(range(256)) * 9000)
        data = disk.finish()
        verified = image.verify(data, disk.files)['filesystemCheckOnPrivateBootNormalizedCopy']
        self.assertEqual(8 * 1024 * 1024, len(data))
        self.assertEqual(8192, struct.unpack_from('>I', data, 8)[0])
        self.assertEqual(5, len(verified['bitmap']['blocks']))
        self.assertTrue(verified['bitmap']['exact_reachable_allocation'])
        corrupt = bytearray(data)
        corrupt[disk.bitmaps[-1] * 512 + 4] ^= 1
        with self.assertRaises(ValueError):
            image.verify(bytes(corrupt), disk.files)

    def test_hdf_system_build_is_deterministic_and_independently_verified(self):
        with tempfile.TemporaryDirectory() as tmp:
            manifest = self.fixture(Path(tmp))
            data, files = image.build(manifest, 16384)
            image.verify(data, files)
            self.assertEqual((data, files), image.build(manifest, 16384))
            self.assertEqual(image.build(manifest)[1], files)

    def fixture(self, directory):
        records = []
        for name, size in [('Shell-Seg', 620000), ('BootShell', 5000), ('Demo', 700), ('mathtrans.library', 8000)]:
            # Serialization fixtures only, never native execution evidence.
            data = b'\0\0\x03\xf3' + bytes((i * 17) & 255 for i in range(size - 4))
            file = directory / (name + '.hunk')
            file.write_bytes(data)
            records.append(dict(name=name, path=file.name, bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
        manifest = directory / 'native-manifest.json'
        manifest.write_text(json.dumps(dict(kind='original-system-boot-proof', shipping=False, version='fixture', artifacts=records)))
        return manifest

    def test_large_shell_extensions_and_root_boundary_independently_verify(self):
        with tempfile.TemporaryDirectory() as tmp:
            manifest = self.fixture(Path(tmp))
            data, files = image.build(manifest)
            result = image.verify(data, files)
            self.assertEqual('passed', result['originalBootChecksum'])
            self.assertTrue(result['filesystemCheckOnPrivateBootNormalizedCopy']['bitmap']['exact_reachable_allocation'])
            self.assertEqual((data, files), image.build(manifest))

    def test_mathtrans_is_hash_bound_and_present_in_the_image(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            manifest = self.fixture(directory)
            data, files = image.build(manifest)
            verified = image.verify(data, files)['filesystemCheckOnPrivateBootNormalizedCopy']
            library = next(f for f in verified['files'] if f['installed_path'] == 'Libs/mathtrans.library')
            self.assertEqual(hashlib.sha256((directory / 'mathtrans.library.hunk').read_bytes()).hexdigest(), library['sha256'])
            (directory / 'mathtrans.library.hunk').write_bytes(b'\0\0\x03\xf3changed')
            with self.assertRaisesRegex(ValueError, 'altered'):
                image.build(manifest)

    def test_shell_stack_gate_provenance_is_verified(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            manifest = self.fixture(directory)
            gate_path = path.parent / 'wrap_shell_stack.py'
            gate_spec = importlib.util.spec_from_file_location('test_gate', gate_path)
            gate = importlib.util.module_from_spec(gate_spec)
            gate_spec.loader.exec_module(gate)
            raw = (struct.pack('>8I', 0x3f3, 0, 1, 0, 0, 2, 0x3e9, 2) +
                   struct.pack('>2I', 4, 0x4e754e71) +
                   struct.pack('>6I', 0x3ec, 1, 0, 0, 0, 0x3f2))
            (directory / 'raw.hunk').write_bytes(raw)
            wrapped = gate.wrap(raw)
            (directory / 'Shell-Seg.hunk').write_bytes(wrapped)
            record = json.loads(manifest.read_text())
            shell = next(item for item in record['artifacts'] if item['name'] == 'Shell-Seg')
            shell.update(bytes=len(wrapped), sha256=hashlib.sha256(wrapped).hexdigest(),
                         stackGate=dict(input='raw.hunk', inputSha256=hashlib.sha256(raw).hexdigest(),
                                        packager=gate_path.name, packagerSha256=hashlib.sha256(gate_path.read_bytes()).hexdigest()))
            manifest.write_text(json.dumps(record))
            image.build(manifest)
            (directory / 'raw.hunk').write_bytes(raw + b'bad')
            with self.assertRaisesRegex(ValueError, 'stack-gate input'):
                image.build(manifest)

    def test_missing_mathtrans_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            manifest = self.fixture(Path(tmp))
            record = json.loads(manifest.read_text())
            record['artifacts'] = [item for item in record['artifacts'] if item['name'] != 'mathtrans.library']
            manifest.write_text(json.dumps(record))
            with self.assertRaisesRegex(ValueError, 'mathtrans'):
                image.build(manifest)

    def test_independent_adf_check_binds_image_receipt_and_staged_library(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            manifest = self.fixture(directory)
            data, files = image.build(manifest)
            adf = directory / 'proof.adf'
            adf.write_bytes(data)
            receipt = directory / 'image.json'
            receipt.write_text(json.dumps(dict(kind='original-system-boot-proof', imageFormat='adf', shipping=False,
                                               sha256=hashlib.sha256(data).hexdigest(), bytes=len(data), files=files)))
            staged = directory / 'mathtrans.library.hunk'
            helper = path.parent / 'verify_mathtrans_image.py'
            command = [sys.executable, str(helper), '--image', str(adf), '--receipt', str(receipt), '--staged-library', str(staged)]
            result = subprocess.run(command, capture_output=True, text=True)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual(hashlib.sha256(staged.read_bytes()).hexdigest(), json.loads(result.stdout)['librarySha256'])
            staged.write_bytes(b'\0\0\x03\xf3changed')
            result = subprocess.run(command, capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertIn('differs from the image receipt', result.stderr)

    def test_storage_command_is_included_and_independently_verified(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            manifest = self.fixture(directory)
            payload = b'\0\0\x03\xf3original storage serialization fixture'
            (directory / 'Storage.hunk').write_bytes(payload)
            record = json.loads(manifest.read_text())
            record['artifacts'].append(dict(name='Storage', path='Storage.hunk', bytes=len(payload),
                                           sha256=hashlib.sha256(payload).hexdigest()))
            manifest.write_text(json.dumps(record))
            data, files = image.build(manifest)
            verified = image.verify(data, files)['filesystemCheckOnPrivateBootNormalizedCopy']
            storage = next(f for f in verified['files'] if f['installed_path'] == 'C/Storage')
            self.assertEqual(hashlib.sha256(payload).hexdigest(), storage['sha256'])
            self.assertTrue(verified['bitmap']['exact_reachable_allocation'])

    def test_list_is_hash_bound_and_staged(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            manifest = self.fixture(directory)
            payload = b'\0\0\x03\xf3original list serialization fixture'
            (directory / 'List.hunk').write_bytes(payload)
            record = json.loads(manifest.read_text())
            record['artifacts'].append(dict(name='List', path='List.hunk', bytes=len(payload),
                                            sha256=hashlib.sha256(payload).hexdigest()))
            manifest.write_text(json.dumps(record))
            data, files = image.build(manifest)
            verified = image.verify(data, files)['filesystemCheckOnPrivateBootNormalizedCopy']
            command = next(f for f in verified['files'] if f['installed_path'] == 'C/List')
            self.assertEqual(hashlib.sha256(payload).hexdigest(), command['sha256'])
            (directory / 'List.hunk').write_bytes(b'\0\0\x03\xf3changed')
            with self.assertRaisesRegex(ValueError, 'altered'):
                image.build(manifest)

    def test_execute_and_t_directory_are_hash_bound_and_staged(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            manifest = self.fixture(directory)
            payload = b'\0\0\x03\xf3original execute serialization fixture'
            (directory / 'Execute.hunk').write_bytes(payload)
            record = json.loads(manifest.read_text())
            record['artifacts'].append(dict(name='Execute', path='Execute.hunk', bytes=len(payload),
                                           sha256=hashlib.sha256(payload).hexdigest()))
            manifest.write_text(json.dumps(record))
            data, files = image.build(manifest)
            verified = image.verify(data, files)['filesystemCheckOnPrivateBootNormalizedCopy']
            entries = {f['installed_path']: f for f in verified['files']}
            self.assertEqual(hashlib.sha256(payload).hexdigest(), entries['C/Execute']['sha256'])
            self.assertEqual(0, entries['T/.keep']['bytes'])
            (directory / 'Execute.hunk').write_bytes(b'\0\0\x03\xf3changed')
            with self.assertRaisesRegex(ValueError, 'altered'):
                image.build(manifest)

    def test_changed_native_input_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            manifest = self.fixture(Path(tmp))
            (Path(tmp) / 'Demo.hunk').write_bytes(b'wrong')
            with self.assertRaisesRegex(ValueError, 'altered'):
                image.build(manifest)

    def test_different_native_manifests_have_different_candidate_identities(self):
        with tempfile.TemporaryDirectory() as tmp:
            manifest = self.fixture(Path(tmp))
            original = manifest.read_bytes()
            record = json.loads(original)
            record['version'] = 'different-development-build'
            changed = json.dumps(record).encode()
            self.assertNotEqual(image.pair_id(original), image.pair_id(changed))

    def test_shipping_and_traversal_manifests_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            manifest = self.fixture(Path(tmp))
            record = json.loads(manifest.read_text())
            record['shipping'] = True
            manifest.write_text(json.dumps(record))
            with self.assertRaisesRegex(ValueError, 'nonshipping'):
                image.build(manifest)
            record['shipping'] = False
            record['artifacts'][0]['path'] = '../other.hunk'
            manifest.write_text(json.dumps(record))
            with self.assertRaisesRegex(ValueError, 'sibling'):
                image.build(manifest)

    def test_corrupt_boot_and_allocation_bitmap_fail_readback(self):
        with tempfile.TemporaryDirectory() as tmp:
            data, files = image.build(self.fixture(Path(tmp)))
            corrupt = bytearray(data)
            corrupt[30] ^= 1
            with self.assertRaisesRegex(ValueError, 'boot-block'):
                image.verify(bytes(corrupt), files)
            corrupt = bytearray(data)
            corrupt[881 * 512 + 12] ^= 1
            with self.assertRaises(ValueError):
                image.verify(bytes(corrupt), files)


if __name__ == '__main__':
    unittest.main()
