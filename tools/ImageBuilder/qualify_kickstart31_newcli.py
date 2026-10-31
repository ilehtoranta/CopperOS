"""Qualify NewCLI startup on a disposable HDF using a supplied CLI binary.

The supplied original-system executable is a test input only and is never
copied into the development release image.
"""
import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_keyboard_probe import parent, child
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def sha(data): return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--reference-cli', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--powershell', default='pwsh')
    args = parser.parse_args()
    source = args.image.resolve(strict=True); original = source.read_bytes()
    reference = args.reference_cli.resolve(strict=True); cli = reference.read_bytes()
    if cli[:4] != b'\0\0\x03\xf3': raise ValueError('Reference CLI is not a HUNK')
    source_disk = Adf(original); tree = source_disk.walk()
    forbidden = {'c/keystart', 'c/keychild', 'c/cli', 's/shell-startup', 'input-sent.txt',
                 'shell-present.txt', 'newclipath.txt', 'newcli.txt'}
    if forbidden.intersection(tree): raise ValueError('Input already contains NewCLI probe paths')
    disk = Disk(16384); dirs = {name: disk.directory(disk.root, name) for name in ('C', 'L', 'S')}
    for key, entry in sorted(tree.items()):
        if entry['secondary_type'] == 2 and '/' not in key and entry['path'] in dirs: continue
        if entry['secondary_type'] != -3 or entry['path'].split('/')[0] not in dirs:
            raise ValueError('Unexpected input tree entry: ' + entry['path'])
        path = entry['path']; data = source_disk.read_file(entry['block'])
        if key == 's/startup-sequence': data = b'C:KeyStart\nC:BootShell\n'
        disk.file(dirs[path.split('/')[0]], path, data)
    disk.file(dirs['C'], 'C/KeyStart', parent())
    disk.file(dirs['C'], 'C/KeyChild', child('cli\n'))
    disk.file(dirs['C'], 'C/CLI', cli)
    disk.file(dirs['S'], 'S/Shell-Startup', b'Path >SYS:newclipath.txt\nEcho >SYS:newcli.txt child\n')
    image = disk.finish(); verify(image, disk.files)
    output = args.output_directory.resolve(); output.mkdir(parents=True, exist_ok=False)
    probe = output / 'probe.hdf'; probe.write_bytes(image)
    runner = Path(__file__).with_name('run_kickstart31_candidate.ps1')
    subprocess.run([args.powershell, '-NoProfile', '-File', str(runner), '-Image', str(probe),
                    '-KickstartRom', str(args.rom.resolve(strict=True)), '-WinUAE', str(args.winuae.resolve(strict=True)),
                    '-OutputDirectory', str(output / 'run'), '-DurationSeconds', '45'], check=True)
    resulting = (output / 'run' / 'candidate.hdf').read_bytes()
    result_disk = Adf(resulting); entries = result_disk.walk()
    def read(name):
        entry = entries.get(name)
        return result_disk.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    observed = {'inputSent': read('input-sent.txt'), 'shellPresent': read('shell-present.txt'),
                'newCliPath': read('newclipath.txt'), 'newCliOutput': read('newcli.txt')}
    (output / 'observations.json').write_text(json.dumps({k: v.decode('latin1') if isinstance(v, bytes) else v
                                                           for k, v in observed.items()}, indent=2) + '\n')
    if (observed['inputSent'] != b'SENT\n' or observed['shellPresent'] != b'PRESENT\n' or
        observed['newCliPath'] != b'C:\n' or observed['newCliOutput'] != b'child\n'):
        raise RuntimeError('NewCLI startup mismatch; see observations.json')
    if source.read_bytes() != original or reference.read_bytes() != cli: raise RuntimeError('An input changed')
    receipt = {'kind': 'kickstart31-newcli-probe', 'runtimeQualified': False,
               'inputImageSha256': sha(original), 'referenceCliSha256': sha(cli),
               'probeImageSha256': sha(image), 'resultImageSha256': sha(resulting),
               'limits': 'Test-only reference CLI and startup; full Shell ABI is not qualified'}
    (output / 'qualification.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__': main()
