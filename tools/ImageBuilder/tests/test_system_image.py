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
