"""Observe Resident SYSTEM against an original or shared Kickstart Shell.

The fixture is disposable. Original mode omits L:Shell-Seg and BootShell so
the original ROM Shell is the command interpreter. Results are observations,
not a complete Shell qualification.
"""
import argparse
import hashlib
import json
import struct
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_keyboard_probe import parent, child
from make_kickstart_shell_probe import build_cli_result_probe, build_resident_system_probe

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--original', action='store_true')
    parser.add_argument('--reference', type=Path,
                        help='Compare command results with a saved original-ROM observation')
    parser.add_argument('--require-order', action='store_true',
                        help='Require selected resident listing order to match the reference')
    parser.add_argument('--duration-seconds', type=int, default=60)
    args = parser.parse_args()
    if args.require_order and args.reference is None:
        parser.error('--require-order requires --reference')
    image_path = args.image.resolve(strict=True)
    original = image_path.read_bytes()
    tree = Adf(original)
    entries = tree.walk()
    generated = {'c/keystart', 'c/keychild', 'c/segstate', 'c/capremove', 'c/capreplace',
                 'c/capsystem', 'c/capsystem2', 'c/capabsent', 's/sysprobe',
                 'input-sent.txt', 'resident-booted', 'resident-reboot', 'segcount.bin',
                 'remove-result', 'replace-result', 'system-result', 'system-result2', 'absent-result',
                 'sysadd', 'sysrun', 'sysremove', 'sysreplace-system', 'sysreplace-system2',
                 'sysreplace', 'sysabsent', 'sysaddnormal', 'sysaddsystem3', 'syslist', 'syslist2',
                 'syslist3', 'sysdone'}
    if generated.intersection(entries):
        raise ValueError('Input image already contains a resident probe path')
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    disk = Disk(16384)
    directories = {}
    empty_original_l = args.original and not any(
        key.startswith('l/') and key != 'l/shell-seg' for key in entries)
    for key, entry in sorted(entries.items(), key=lambda item: (item[0].count('/'), item[0])):
        if args.original and (key == 'l/shell-seg' or (key == 'l' and empty_original_l)):
            continue
        parts = entry['path'].split('/')
        owner = disk.root if len(parts) == 1 else directories['/'.join(parts[:-1]).lower()]
        if entry['secondary_type'] == 2:
            directories[key] = disk.directory(owner, entry['name'])
        elif entry['secondary_type'] == -3:
            # A marker independent of the keyboard injector detects a warm
            # restart even when the original Shell has a different task name.
            startup = (b'IF EXISTS SYS:resident-booted\nEcho >SYS:resident-reboot yes\nEndIf\n'
                       b'Echo >SYS:resident-booted yes\nC:KeyStart\n' +
                       (b'' if args.original else b'C:BootShell\n'))
            contents = startup if key == 's/startup-sequence' else tree.read_file(entry['block'])
            disk.file(owner, entry['path'], contents)
        else:
            raise ValueError('Unexpected input entry: ' + key)
    disk.file(directories['c'], 'C/KeyStart', parent())
    disk.file(directories['c'], 'C/KeyChild', child('execute s:sysprobe\n'))
    disk.file(directories['c'], 'C/SegState', build_resident_system_probe())
    disk.file(directories['c'], 'C/CapRemove', build_cli_result_probe('SYS:remove-result'))
    disk.file(directories['c'], 'C/CapReplace', build_cli_result_probe('SYS:replace-result'))
    disk.file(directories['c'], 'C/CapSystem', build_cli_result_probe('SYS:system-result'))
    disk.file(directories['c'], 'C/CapSystem2', build_cli_result_probe('SYS:system-result2'))
    disk.file(directories['c'], 'C/CapAbsent', build_cli_result_probe('SYS:absent-result'))
    disk.file(directories['s'], 'S/sysprobe',
              b'FailAt 21\n'
              b'Resident Sysprobe SYS:C/Demo ADD PURE SYSTEM >SYS:sysadd\n'
              b'SegState\nSysprobe >SYS:sysrun\n'
              b'Resident Sysprobe REMOVE >SYS:sysremove\nCapRemove\n'
              b'Resident Sysprobe SYS:C/Demo REPLACE PURE SYSTEM >SYS:sysreplace-system\nCapSystem\n'
              b'Resident Sysprobe SYS:C/Demo REPLACE PURE >SYS:sysreplace\nCapReplace\n'
              b'Resident Missing SYS:C/Demo REPLACE PURE >SYS:sysabsent\nCapAbsent\n'
              b'Resident SYSTEM >SYS:syslist\n'
              b'Resident Sysprobe SYS:C/Demo REPLACE PURE SYSTEM >SYS:sysreplace-system2\nCapSystem2\n'
              b'Resident SYSTEM >SYS:syslist2\n'
              b'Resident Sysprobe SYS:C/Demo ADD PURE >SYS:sysaddnormal\n'
              b'Resident Sysprobe SYS:C/Demo ADD PURE SYSTEM >SYS:sysaddsystem3\n'
              b'Resident SYSTEM >SYS:syslist3\nEcho >SYS:sysdone done\n')
    fixture = disk.finish()
    verify(fixture, disk.files)
    (output / 'probe.hdf').write_bytes(fixture)
    runner = Path(__file__).with_name('run_kickstart31_candidate.ps1')
    subprocess.run(['pwsh', '-NoProfile', '-File', str(runner), '-Image', str(output / 'probe.hdf'),
                    '-KickstartRom', str(args.rom.resolve(strict=True)), '-WinUAE', str(args.winuae.resolve(strict=True)),
                    '-OutputDirectory', str(output / 'run'), '-DurationSeconds', str(args.duration_seconds)], check=True)
    result = Adf((output / 'run' / 'candidate.hdf').read_bytes())
    found = result.walk()
    def read(name):
        entry = found.get(name)
        return result.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    values = {name: read(name) for name in ('input-sent.txt', 'resident-booted', 'resident-reboot',
                                             'segcount.bin', 'remove-result', 'replace-result',
                                             'system-result', 'system-result2', 'absent-result', 'sysadd', 'sysrun', 'sysremove',
                                             'sysreplace-system', 'sysreplace-system2', 'sysreplace', 'sysabsent',
                                             'sysaddnormal', 'sysaddsystem3', 'syslist', 'syslist2', 'syslist3', 'sysdone')}
    observations = {name: (list(struct.unpack('>ii', value)) if name in ('remove-result', 'replace-result', 'system-result', 'system-result2', 'absent-result') and value and len(value) == 8 else
                           struct.unpack('>i', value)[0] if name == 'segcount.bin' and value and len(value) == 4 else
                           value.decode('latin1') if value is not None else None)
                    for name, value in values.items()}
    observations['originalShell'] = args.original
    observations['inputImageSha256'] = hashlib.sha256(original).hexdigest()
    (output / 'observations.json').write_text(json.dumps(observations, indent=2) + '\n')
    if (values['input-sent.txt'] != b'SENT\n' or values['resident-booted'] != b'yes\n' or
            values['resident-reboot'] is not None or values['sysdone'] != b'done\n' or
            image_path.read_bytes() != original):
        raise RuntimeError('Resident SYSTEM observation did not complete; see observations.json')
    if args.reference:
        reference_path = args.reference.resolve(strict=True)
        reference = json.loads(reference_path.read_text())
        if reference.get('originalShell') is not True:
            raise ValueError('Reference must be an original ROM Shell observation')
        reference_launch_path = reference_path.parent / 'run' / 'launch.json'
        reference_launch = json.loads(reference_launch_path.read_text())
        candidate_launch = json.loads((output / 'run' / 'launch.json').read_text())
        for key in ('romSha256', 'winuaeSha256'):
            if not reference_launch.get(key) or reference_launch[key] != candidate_launch.get(key):
                raise RuntimeError('Reference and candidate used different ' + key)
        keys = ('segcount.bin', 'remove-result', 'replace-result', 'system-result', 'system-result2',
                'absent-result', 'sysadd', 'sysrun', 'sysremove', 'sysreplace-system',
                'sysreplace-system2', 'sysreplace', 'sysabsent', 'sysaddnormal', 'sysaddsystem3', 'sysdone')
        differences = [key for key in keys if observations[key] != reference.get(key)]
        for key in ('syslist', 'syslist2', 'syslist3'):
            if observations[key] is None or reference.get(key) is None:
                differences.append(key)
                continue
            for entry in ('Sysprobe             0\n', 'Sysprobe          SYSTEM\n'):
                if observations[key].count(entry) != reference[key].count(entry):
                    differences.append(key + ':' + entry.strip())
        if differences:
            raise RuntimeError('Resident SYSTEM differs from original ROM Shell: ' + ', '.join(differences))
        def relative_order(listing):
            return [line.strip() for line in listing.splitlines()
                    if line.startswith('Sysprobe ') or line.startswith('Alias ')]
        def normalized_listing(listing):
            # The proof adds its own Shell module and implements Cls, neither
            # present in this original-ROM reference fixture's listing.
            return [line for line in listing.splitlines() if line.strip() and
                    line not in ('Cls               INTERNAL', 'Shell             SYSTEM')]
        order = {key: relative_order(observations[key]) == relative_order(reference[key])
                 for key in ('syslist', 'syslist2', 'syslist3')}
        normalized = {key: normalized_listing(observations[key]) == normalized_listing(reference[key])
                      for key in ('syslist', 'syslist2', 'syslist3')}
        (output / 'comparison.json').write_text(json.dumps({
            'originalObservationSha256': hashlib.sha256(reference_path.read_bytes()).hexdigest(),
            'originalLaunchSha256': hashlib.sha256(reference_launch_path.read_bytes()).hexdigest(),
            'romSha256': candidate_launch['romSha256'], 'winuaeSha256': candidate_launch['winuaeSha256'],
            'matchingFields': list(keys), 'matchingTargetEntryCounts': True,
            'residentRelativeOrderMatches': order, 'matchingNormalizedListings': normalized,
            'runtimeQualified': False,
        }, indent=2) + '\n')
        if args.require_order and (not all(order.values()) or not all(normalized.values())):
            raise RuntimeError('Resident SYSTEM listing order differs from original ROM Shell; see comparison.json')
    print(json.dumps(observations))


if __name__ == '__main__':
    main()
