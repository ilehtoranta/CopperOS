"""Compare ASK responses in a disposable original or CopperOS Shell boot."""
import argparse
import hashlib
import json
import struct
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_keyboard_probe import parent, child
from make_kickstart_shell_probe import build_cli_result_probe

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--duration-seconds', type=int, default=180)
    parser.add_argument('--original', action='store_true')
    args = parser.parse_args()

    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    tree = Adf(original)
    entries = tree.walk()
    generated = {'c/keystart', 'c/keychild', 'c/capyes', 'c/capno', 'c/capretry',
                 's/askprobe', 's/askyes', 's/askno', 's/askretry', 'input-sent.txt',
                 'askdone', 'ask-yes-result', 'ask-no-result', 'ask-retry-result',
                 'ask-yes-text', 'ask-no-text', 'ask-retry-text'}
    if generated.intersection(entries):
        raise ValueError('Input image already contains an ASK probe path')
    disk = Disk(16384)
    directories = {}
    empty_l = args.original and not any(key.startswith('l/') and key != 'l/shell-seg' for key in entries)
    for key, entry in sorted(entries.items(), key=lambda item: (item[0].count('/'), item[0])):
        if args.original and (key == 'l/shell-seg' or (key == 'l' and empty_l)):
            continue
        parts = entry['path'].split('/')
        owner = disk.root if len(parts) == 1 else directories['/'.join(parts[:-1]).lower()]
        if entry['secondary_type'] == 2:
            directories[key] = disk.directory(owner, entry['name'])
        elif entry['secondary_type'] == -3:
            startup = b'C:KeyStart\n' + (b'' if args.original else b'C:BootShell\n')
            disk.file(owner, entry['path'], startup if key == 's/startup-sequence' else tree.read_file(entry['block']))
        else:
            raise ValueError('Unexpected input entry: ' + key)
    disk.file(directories['c'], 'C/KeyStart', parent())
    disk.file(directories['c'], 'C/KeyChild', child('execute s:askprobe\n', wait_for_path='SYS:askdone'))
    disk.file(directories['c'], 'C/CapYes', build_cli_result_probe('SYS:ask-yes-result'))
    disk.file(directories['c'], 'C/CapNo', build_cli_result_probe('SYS:ask-no-result'))
    disk.file(directories['c'], 'C/CapRetry', build_cli_result_probe('SYS:ask-retry-result'))
    disk.file(directories['s'], 'S/askyes', b'Y\n')
    disk.file(directories['s'], 'S/askno', b'N\n')
    disk.file(directories['s'], 'S/askretry', b'X\nY\n')
    disk.file(directories['s'], 'S/askprobe',
              b'FailAt 21\nAsk <SYS:S/askyes >SYS:ask-yes-text "Continue?"\nC:CapYes\n'
              b'Ask <SYS:S/askno >SYS:ask-no-text "Continue?"\nC:CapNo\n'
              b'Ask <SYS:S/askretry >SYS:ask-retry-text "Continue?"\nC:CapRetry\nEcho >SYS:askdone done\n')
    fixture = disk.finish()
    verify(fixture, disk.files)
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    image = output / 'probe.hdf'
    image.write_bytes(fixture)
    rom, winuae = args.rom.resolve(strict=True), args.winuae.resolve(strict=True)
    rom_bytes, winuae_bytes = rom.read_bytes(), winuae.read_bytes()
    subprocess.run(['pwsh', '-NoProfile', '-File', str(Path(__file__).with_name('run_kickstart31_candidate.ps1')),
                    '-Image', str(image), '-KickstartRom', str(rom), '-WinUAE', str(winuae),
                    '-OutputDirectory', str(output / 'run'), '-DurationSeconds', str(args.duration_seconds)], check=True)
    result_bytes = (output / 'run' / 'candidate.hdf').read_bytes()
    result = Adf(result_bytes)
    found = result.walk()
    def read(name):
        entry = found.get(name)
        return result.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    values = {name: read(name) for name in ('input-sent.txt', 'askdone', 'ask-yes-result',
                                             'ask-no-result', 'ask-retry-result', 'ask-yes-text',
                                             'ask-no-text', 'ask-retry-text')}
    report = {name: (list(struct.unpack('>ii', value)) if name.endswith('-result') and value is not None and len(value) == 8
                     else value.decode('latin1') if value is not None else None)
              for name, value in values.items()}
    sha = lambda value: hashlib.sha256(value).hexdigest()
    report.update({'originalShell': args.original, 'inputImageSha256': sha(original),
                   'fixtureImageSha256': sha(fixture), 'resultImageSha256': sha(result_bytes),
                   'romSha256': sha(rom_bytes), 'winuaeSha256': sha(winuae_bytes),
                   'runtimeQualified': False})
    (output / 'observations.json').write_text(json.dumps(report, indent=2) + '\n')
    if source.read_bytes() != original or rom.read_bytes() != rom_bytes or winuae.read_bytes() != winuae_bytes:
        raise RuntimeError('An input changed during the observation')
    if (values['input-sent.txt'] != b'SENT\n' or values['askdone'] != b'done\n' or
            values['ask-yes-result'] is None or len(values['ask-yes-result']) != 8 or
            values['ask-no-result'] is None or len(values['ask-no-result']) != 8 or
            values['ask-retry-result'] is None or len(values['ask-retry-result']) != 8):
        raise RuntimeError('ASK observation did not complete; see observations.json')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
