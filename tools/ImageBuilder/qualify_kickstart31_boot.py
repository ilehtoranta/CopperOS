"""Check Kickstart/WinUAE HDF startup with a disposable marker image.

This probe changes the startup sequence and therefore does not qualify the
original image or interactive Shell. It never writes to the supplied HDF.
"""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path

from build_system_image import Disk, verify
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def sha(data):
    return hashlib.sha256(data).hexdigest()


def marker(data):
    entry = Adf(data).walk().get('kickstartprobe')
    return entry is not None and entry['secondary_type'] == 2


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--powershell', default='pwsh')
    args = parser.parse_args()
    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    if marker(original):
        raise ValueError('Input already contains the probe marker')
    source_disk = Adf(original)
    tree = source_disk.walk()
    if 'c/makedir' not in tree or 'c/bootshell' not in tree or 's/startup-sequence' not in tree:
        raise ValueError('Input lacks the files required for this probe')
    disk = Disk(16384)
    directories = {name: disk.directory(disk.root, name) for name in ('C', 'L', 'S')}
    for key, entry in sorted(tree.items()):
        if entry['secondary_type'] == 2 and '/' not in key and entry['path'] in directories:
            continue
        if entry['secondary_type'] != -3 or entry['path'].split('/')[0] not in directories:
            raise ValueError('Unexpected input tree entry: ' + entry['path'])
        path = entry['path']
        data = source_disk.read_file(entry['block'])
        if key == 's/startup-sequence':
            data = b'C:MakeDir SYS:KickstartProbe\nC:BootShell\n'
        disk.file(directories[path.split('/')[0]], path, data)
    probe = disk.finish()
    verify(probe, disk.files)
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    probe_path = output / 'probe.hdf'
    probe_path.write_bytes(probe)
    runner = Path(__file__).with_name('run_kickstart31_candidate.ps1')

    def run(image, name, seconds):
        target = output / name
        subprocess.run([args.powershell, '-NoProfile', '-File', str(runner), '-Image', str(image),
                        '-KickstartRom', str(args.rom.resolve(strict=True)), '-WinUAE', str(args.winuae.resolve(strict=True)),
                        '-OutputDirectory', str(target), '-DurationSeconds', str(seconds)], check=True)
        return target / 'candidate.hdf'

    first = run(probe_path, 'first-boot', 30)
    first_data = first.read_bytes()
    if not marker(first_data):
        raise RuntimeError('WinUAE did not create the startup marker')
    second = run(first, 'restart', 15)
    second_data = second.read_bytes()
    if not marker(second_data) or source.read_bytes() != original:
        raise RuntimeError('Marker did not persist or input image changed')
    result = {'kind': 'kickstart31-startup-marker', 'runtimeQualified': False,
              'inputImageSha256': sha(original), 'probeImageSha256': sha(probe),
              'firstBootImageSha256': sha(first_data), 'restartImageSha256': sha(second_data),
              'firstBoot': 'Kickstart ran C:MakeDir and created SYS:KickstartProbe',
              'restart': 'Marker remained on the copied HDF after a fresh WinUAE run',
              'limits': 'Probe startup differs from input; no interactive Shell or second-boot execution was observed'}
    (output / 'qualification.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
