"""Observe a 65-argument Path command using the original Kickstart Shell."""
import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_keyboard_probe import parent, child
from make_kickstart_shell_probe import build_many_path_script

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--duration-seconds', type=int, default=180)
    args = parser.parse_args()
    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    tree = Adf(original)
    entries = tree.walk()
    generated = {'c/keystart', 'c/keychild', 's/manypath', 'input-sent.txt',
                 'pathmany', 'pathmanydone'}
    if generated.intersection(entries):
        raise ValueError('Input image already contains a probe path')
    disk = Disk(16384)
    directories = {}
    empty_l = not any(key.startswith('l/') and key != 'l/shell-seg' for key in entries)
    for key, entry in sorted(entries.items(), key=lambda item: (item[0].count('/'), item[0])):
        if key == 'l/shell-seg' or (key == 'l' and empty_l):
            continue
        parts = entry['path'].split('/')
        owner = disk.root if len(parts) == 1 else directories['/'.join(parts[:-1]).lower()]
        if entry['secondary_type'] == 2:
            directories[key] = disk.directory(owner, entry['name'])
        elif entry['secondary_type'] == -3:
            contents = b'C:KeyStart\n' if key == 's/startup-sequence' else tree.read_file(entry['block'])
            disk.file(owner, entry['path'], contents)
        else:
            raise ValueError('Unexpected input entry: ' + key)
    disk.file(directories['c'], 'C/KeyStart', parent())
    disk.file(directories['c'], 'C/KeyChild', child('execute s:manypath\n', wait_for_path='SYS:pathmanydone'))
    disk.file(directories['s'], 'S/manypath', build_many_path_script())
    fixture = disk.finish()
    verify(fixture, disk.files)
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    image = output / 'probe.hdf'
    image.write_bytes(fixture)
    rom = args.rom.resolve(strict=True)
    winuae = args.winuae.resolve(strict=True)
    rom_bytes, winuae_bytes = rom.read_bytes(), winuae.read_bytes()
    runner = Path(__file__).with_name('run_kickstart31_candidate.ps1')
    subprocess.run(['pwsh', '-NoProfile', '-File', str(runner), '-Image', str(image),
                    '-KickstartRom', str(rom), '-WinUAE', str(winuae),
                    '-OutputDirectory', str(output / 'run'),
                    '-DurationSeconds', str(args.duration_seconds)], check=True)
    result_bytes = (output / 'run' / 'candidate.hdf').read_bytes()
    result = Adf(result_bytes)
    found = result.walk()
    def read(name):
        entry = found.get(name)
        return result.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    observed = {name: read(name) for name in ('input-sent.txt', 'pathmany', 'pathmanydone')}
    sha = lambda value: hashlib.sha256(value).hexdigest()
    report = {key: value.decode('latin1') if value is not None else None for key, value in observed.items()}
    report.update({'originalShell': True, 'inputImageSha256': sha(original),
                   'fixtureImageSha256': sha(fixture), 'resultImageSha256': sha(result_bytes),
                   'romSha256': sha(rom_bytes), 'winuaeSha256': sha(winuae_bytes),
                   'runtimeQualified': False})
    (output / 'observations.json').write_text(json.dumps(report, indent=2) + '\n')
    if source.read_bytes() != original or rom.read_bytes() != rom_bytes or winuae.read_bytes() != winuae_bytes:
        raise RuntimeError('An input changed during the observation')
    if observed['input-sent.txt'] != b'SENT\n' or observed['pathmanydone'] != b'done\n':
        raise RuntimeError('Original Shell Path observation did not complete; see observations.json')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
