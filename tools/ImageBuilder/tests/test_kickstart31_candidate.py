import json
import struct
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

BUILDER = Path(__file__).resolve().parents[1] / 'build_kickstart31_candidate.py'
sys.path.insert(0, str(BUILDER.parent))
from build_kickstart31_candidate import project_names
sys.path.insert(0, str(BUILDER.parents[1] / 'DiskBuilder'))
from verify_image import Adf


class KickstartCandidateTests(unittest.TestCase):
    def test_inventory_and_unqualified_receipt(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            commands = root / 'commands'
            commands.mkdir()
            names = project_names(BUILDER.parents[2])
            for name in names:
                version = '2.7 (1.1.2027)' if name == names[0] else '1.0 (31.10.2026)'
                (commands / name).write_bytes(b'\0\0\x03\xf3fixture\0' +
                                              f'$VER: {name} {version} CopperOS'.encode('ascii') + b'\0')
            shell = root / 'shell'
            def hunk():
                return struct.pack('>8I', 1011, 0, 1, 0, 0, 1, 1001, 1) + b'\x4e\x75\0\0' + struct.pack('>I', 1010)
            shell.write_bytes(hunk())
            bootstrap = root / 'bootstrap'
            bootstrap.write_bytes(hunk())
            output, receipt = root / 'candidate.hdf', root / 'candidate.json'
            command = [sys.executable, str(BUILDER), '--shell', str(shell), '--bootstrap', str(bootstrap),
                       '--commands', str(commands), '--output', str(output), '--receipt', str(receipt)]
            subprocess.run(command, check=True, capture_output=True)
            result = json.loads(receipt.read_text())
            self.assertFalse(result['shipping'])
            self.assertFalse(result['runtimeQualified'])
            self.assertEqual(8 * 1024 * 1024, output.stat().st_size)
            self.assertEqual({f'C/{name}' for name in names} | {'C/BootShell'},
                             {x['path'] for x in result['files'] if x['path'].startswith('C/')})
            self.assertFalse(any(x['path'].lower().endswith('.library') for x in result['files']))
            self.assertTrue(result['verification']['filesystemCheckOnPrivateBootNormalizedCopy']['bitmap']['exact_reachable_allocation'])
            disk = Adf(output.read_bytes())
            tree = disk.walk()
            self.assertIn(f'$VER: {names[0]} 2.7 (1.1.2027) CopperOS'.encode('ascii'),
                          disk.read_file(tree[('c/' + names[0]).lower()]['block']))
            self.assertIn(b'$VER: BootShell 1.0 (31.10.2026) CopperOS\0', disk.read_file(tree['c/bootshell']['block']))
            self.assertIn(b'$VER: Shell-Seg 1.0 (31.10.2026) CopperOS\0', disk.read_file(tree['l/shell-seg']['block']))
            self.assertIn(b'C:BootShell\n', disk.read_file(tree['s/startup-sequence']['block']))
            self.assertNotEqual(0, subprocess.run(command, capture_output=True).returncode)
            (commands / names[0]).write_bytes(b'\0\0\x03\xf3no-version')
            invalid_command = [sys.executable, str(BUILDER), '--shell', str(shell), '--bootstrap', str(bootstrap),
                               '--commands', str(commands), '--output', str(root / 'invalid.hdf'),
                               '--receipt', str(root / 'invalid.json')]
            self.assertNotEqual(0, subprocess.run(invalid_command, capture_output=True).returncode)
            self.assertFalse((root / 'invalid.hdf').exists())

    def test_missing_command_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            commands = root / 'commands'
            commands.mkdir()
            shell = root / 'shell'
            shell.write_bytes(b'\0\0\x03\xf3shell')
            result = subprocess.run([sys.executable, str(BUILDER), '--shell', str(shell),
                                     '--bootstrap', str(shell),
                                     '--commands', str(commands), '--output', str(root / 'x.hdf'),
                                     '--receipt', str(root / 'x.json')], capture_output=True)
            self.assertNotEqual(0, result.returncode)
            self.assertFalse((root / 'x.hdf').exists())


if __name__ == '__main__':
    unittest.main()
