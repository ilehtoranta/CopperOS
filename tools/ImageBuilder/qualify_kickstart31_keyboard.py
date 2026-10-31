"""Test a disposable HDF with guest input.device keyboard injection.

The test startup adds an injector before the unchanged BootShell. It uses a
real console input path, but the altered startup is not the shipping image.
"""
import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_keyboard_probe import parent, child
from make_kickstart_shell_probe import build_status_probe

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def sha(data): return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--powershell', default='pwsh')
    parser.add_argument('--without-bootshell', action='store_true', help='Negative control')
    parser.add_argument('--restart', action='store_true', help='Restart the written HDF copy and check a second-boot marker')
    args = parser.parse_args()
    source = args.image.resolve(strict=True); original = source.read_bytes()
    source_disk = Adf(original); tree = source_disk.walk()
    forbidden = {'c/keystart', 'c/keychild', 'c/shellstatus', 'input-sent.txt', 'shell-present.txt',
                 'interactive.txt', 'interactiveprobe', 'eval.txt', 'version.txt', 'dir.txt',
                 'before.txt', 'removeme', 'finish.txt', 'shell-status.txt', 'second-boot.txt'}
    if forbidden.intersection(tree): raise ValueError('Input already contains keyboard probe paths')
    disk = Disk(16384); dirs = {name: disk.directory(disk.root, name) for name in ('C', 'L', 'S')}
    for key, entry in sorted(tree.items()):
        if entry['secondary_type'] == 2 and '/' not in key and entry['path'] in dirs: continue
        if entry['secondary_type'] != -3 or entry['path'].split('/')[0] not in dirs:
            raise ValueError('Unexpected input tree entry: ' + entry['path'])
        path = entry['path']; data = source_disk.read_file(entry['block'])
        if key == 's/startup-sequence':
            data = b'C:KeyStart\n' + (b'' if args.without_bootshell else b'C:BootShell\n')
        disk.file(dirs[path.split('/')[0]], path, data)
    disk.file(dirs['C'], 'C/KeyStart', parent())
    disk.file(dirs['C'], 'C/KeyChild', child())
    disk.file(dirs['C'], 'C/ShellStatus', build_status_probe())
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
                'echo': read('interactive.txt'), 'eval': read('eval.txt'), 'version': read('version.txt'),
                'dirOutput': read('dir.txt'), 'before': read('before.txt'), 'finish': read('finish.txt'),
                'status': read('shell-status.txt'), 'directory': entries.get('interactiveprobe', {}).get('secondary_type') == 2,
                'removed': 'removeme' not in entries}
    (output / 'observations.json').write_text(json.dumps({k: v.decode('latin1') if isinstance(v, bytes) else v
                                                           for k, v in observed.items()}, indent=2) + '\n')
    if observed['inputSent'] != b'SENT\n': raise RuntimeError('Keyboard injector did not finish; see observations.json')
    if args.without_bootshell and observed['shellPresent'] is not None:
        raise RuntimeError('Negative control unexpectedly found a CopperOS Shell process')
    if not args.without_bootshell and (observed['shellPresent'] != b'PRESENT\n' or
        observed['echo'] != b'hello\nagain\n' or observed['eval'] != b'3\n' or
        observed['version'] != b'Eval 1.0\n' or observed['dirOutput'] is None or
        b'removeme' not in (observed['before'] or b'').lower() or not observed['removed'] or
        observed['status'] != b'OK\n' or observed['finish'] != b'done\n' or not observed['directory']):
        raise RuntimeError('Interactive marker mismatch; see observations.json')
    second_sha = None
    if args.restart:
        if args.without_bootshell: raise ValueError('Restart requires BootShell')
        subprocess.run([args.powershell, '-NoProfile', '-File', str(runner),
                        '-Image', str(output / 'run' / 'candidate.hdf'),
                        '-KickstartRom', str(args.rom.resolve(strict=True)), '-WinUAE', str(args.winuae.resolve(strict=True)),
                        '-OutputDirectory', str(output / 'restart'), '-DurationSeconds', '45'], check=True)
        second = (output / 'restart' / 'candidate.hdf').read_bytes()
        second_disk = Adf(second); second_entries = second_disk.walk()
        entry = second_entries.get('second-boot.txt')
        if not entry or second_disk.read_file(entry['block']) != b'AGAIN\n':
            raise RuntimeError('No second-boot marker after restarting the HDF')
        if 'interactiveprobe' not in second_entries:
            raise RuntimeError('First boot directory did not persist')
        second_sha = sha(second)
    if source.read_bytes() != original: raise RuntimeError('Input image changed')
    receipt = {'kind': 'kickstart31-guest-keyboard-probe', 'runtimeQualified': False,
               'withoutBootShell': args.without_bootshell, 'inputImageSha256': sha(original),
               'probeImageSha256': sha(image), 'resultImageSha256': sha(resulting), 'restartImageSha256': second_sha,
               'observed': {k: v.decode('latin1') if isinstance(v, bytes) else v for k, v in observed.items()},
               'limits': 'Test startup adds an input injector; full Shell ABI is not qualified'}
    (output / 'qualification.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__': main()
