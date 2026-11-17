"""Check overlapping Shell input on a disposable CopperStart proof ADF.

The source image is not modified. A test-only command is added to a copied
image; the emulator injects keyboard events and the guest records the result.
This is a focused regression, not qualification of the complete Shell.
"""
import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_shell_probe import build_run_delayed_probe

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', type=Path, required=True)
    parser.add_argument('--rom', type=Path, required=True)
    parser.add_argument('--extension-rom', type=Path, required=True)
    parser.add_argument('--probe-dll', type=Path, required=True)
    parser.add_argument('--output-directory', type=Path, required=True)
    parser.add_argument('--control', action='store_true', help='Use an independently opened NIL: input')
    args = parser.parse_args()

    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    tree = Adf(original)
    entries = tree.walk()
    generated = {'c/delayprobe', 'delay-started', 'input-identity', 'overlap', 'afterrun', 'delayresult'}
    if generated.intersection(entries):
        raise ValueError('Input image already contains a probe path')
    disk = Disk()
    directories = {}
    for key, entry in sorted(entries.items(), key=lambda item: (item[0].count('/'), item[0])):
        parts = entry['path'].split('/')
        owner = disk.root if len(parts) == 1 else directories['/'.join(parts[:-1]).lower()]
        if entry['secondary_type'] == 2:
            directories[key] = disk.directory(owner, entry['name'])
        elif entry['secondary_type'] == -3:
            disk.file(owner, entry['path'], tree.read_file(entry['block']))
        else:
            raise ValueError('Unexpected input entry: ' + entry['path'])
    if 'c' not in directories:
        raise ValueError('Input image lacks C:')
    disk.file(directories['c'], 'C/DelayProbe', build_run_delayed_probe(
        6000, observe_followup=True, publish_sentinel=True, poll_followup=True))
    fixture = disk.finish()
    verify(fixture, disk.files)

    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    fixture_path = output / 'probe.adf'
    fixture_path.write_bytes(fixture)
    base = output / 'guest'
    rom = args.rom.resolve(strict=True)
    extension = args.extension_rom.resolve(strict=True)
    probe = args.probe_dll.resolve(strict=True)
    subprocess.run(['dotnet', str(probe), str(rom), str(fixture_path), str(extension), str(base),
                    'control' if args.control else 'race'], check=True)
    result = (output / 'guest.result.adf').read_bytes()
    result_disk = Adf(result)
    result_tree = result_disk.walk()
    expected = {'delay-started': b'START\n', 'input-identity': b'DIFF\n',
                'overlap': b'EARLY\n', 'afterrun': b'done\n', 'delayresult': b'nested\n'}
    def read(path):
        entry = result_tree.get(path)
        return result_disk.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    observed = {name: read(name) for name in expected}
    unexpected_star_paths = sorted(name for name in result_tree
                                   if name not in entries and name.split('/')[-1] == '*')
    report = {name: value.decode('latin1') if value is not None else None
              for name, value in observed.items()}
    report['unexpectedStarPaths'] = unexpected_star_paths
    (output / 'observations.json').write_text(json.dumps(report, indent=2) + '\n')
    trace = json.loads((output / 'guest.trace.json').read_text())
    if (observed != expected or unexpected_star_paths or
            any(trace[name] != 0 for name in ('remaining', 'bindingAfter', 'runnerAfter'))
            or (not args.control and (trace['shellHandler'] == 0 or
                                      trace['shellHandler'] != trace['workerHandler']))):
        raise RuntimeError('CopperStart input overlap mismatch; see observations.json and guest.trace.json')
    if source.read_bytes() != original:
        raise RuntimeError('Input proof image changed during qualification')
    receipt = {'kind': 'copperstart-shared-shell-input-overlap', 'runtimeQualified': False,
               'control': args.control, 'inputImageSha256': sha(original),
               'fixtureImageSha256': sha(fixture), 'resultImageSha256': sha(result),
               'romSha256': sha(rom.read_bytes()), 'extensionRomSha256': sha(extension.read_bytes()),
               'probeSha256': sha(probe.read_bytes()),
               'shellInputHandler': trace['shellHandler'], 'workerInputHandler': trace['workerHandler'],
               'limits': 'Focused interactive input and teardown test; complete Shell not qualified'}
    (output / 'qualification.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
