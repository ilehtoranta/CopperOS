"""Observe the lifetime of a RUN launcher's redirected output on V40."""
import argparse
import hashlib
import json
import struct
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_keyboard_probe import parent, child
from make_kickstart_shell_probe import (build_run_stream_probe, build_system_run_output_probe,
                                       build_run_parent_state_probe, build_run_shared_error_probe)

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--original', action='store_true')
    parser.add_argument('--outer', action='store_true',
                        help='Redirect the enclosing Execute command as well')
    parser.add_argument('--nil', action='store_true',
                        help='Redirect the RUN launcher to NIL:')
    parser.add_argument('--no-leading', action='store_true',
                        help='Launch without a leading RUN redirection')
    parser.add_argument('--system-output', action='store_true',
                        help='Launch RUN with a caller-owned System() output file')
    parser.add_argument('--nested', action='store_true',
                        help='Launch an additional RUN in the background child')
    parser.add_argument('--console-input', action='store_true',
                        help='Give System() a separate interactive input handle')
    parser.add_argument('--parent-state', action='store_true',
                        help='Record the public Process and CLI fields before RUN')
    parser.add_argument('--shared-error', action='store_true',
                        help='Observe a leading output followed by shared error redirection')
    parser.add_argument('--shared-pre-only', action='store_true',
                        help='Stop the shared-error child before writing to its error handle')
    parser.add_argument('--shared-command', choices=('run', 'direct', 'run-no-shared', 'direct-no-shared',
                                                      'plain', 'run-plain'),
                        default='run', help='Select a control for the shared-error observation')
    parser.add_argument('--duration-seconds', type=int, default=180)
    args = parser.parse_args()
    if args.system_output and (args.outer or args.nil):
        parser.error('--system-output cannot be combined with --outer or --nil')
    if args.shared_error and (args.system_output or args.outer or args.nil or args.no_leading or args.parent_state):
        parser.error('--shared-error cannot be combined with other observation modes')
    if args.shared_pre_only and not args.shared_error:
        parser.error('--shared-pre-only requires --shared-error')
    if args.shared_command != 'run' and not args.shared_error:
        parser.error('--shared-command requires --shared-error')
    if args.nested and not args.system_output:
        parser.error('--nested requires --system-output')
    if args.console_input and not args.system_output:
        parser.error('--console-input requires --system-output')
    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    tree = Adf(original)
    entries = tree.walk()
    generated = {'c/keystart', 'c/keychild', 'c/streamprobe', 's/streamrun',
                 'c/sysrunprobe', 'input-sent.txt', 'streamreport', 'streamname', 'runprefix',
                 'c/parentprobe', 'c/sharedprobe', 'parentdone', 'outer', 'runshared', 'systemdone', 'parentstate', 'sharedreport', 'sharedpre', 'sharedentry'}
    if generated.intersection(entries):
        raise ValueError('Input image already contains a probe path')
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
    commands = ('sysrunprobe' if args.system_output else
                'execute s:streamrun' + (' >sys:outer' if args.outer else '')) + '\n'
    wait_path = ('SYS:sharedpre' if args.shared_pre_only else
                 'SYS:sharedreport' if args.shared_error else
                 'SYS:systemdone' if args.system_output else 'SYS:streamreport')
    disk.file(directories['c'], 'C/KeyChild', child(commands, wait_for_path=wait_path))
    disk.file(directories['c'], 'C/StreamProbe', build_run_stream_probe())
    if args.parent_state:
        disk.file(directories['c'], 'C/ParentProbe', build_run_parent_state_probe())
    if args.shared_error:
        disk.file(directories['c'], 'C/SharedProbe', build_run_shared_error_probe(args.shared_pre_only))
    if args.system_output:
        disk.file(directories['c'], 'C/SysRunProbe', build_system_run_output_probe(not args.no_leading, args.parent_state, args.nested, args.console_input))
    if args.nil and args.no_leading:
        parser.error('--nil and --no-leading cannot be combined')
    target = b'NIL:' if args.nil else b'SYS:runprefix'
    redirect = b'' if args.no_leading else b'>' + target + b' '
    shared_commands = {
        'run': b'Run >SYS:runprefix *<> C:SharedProbe\n',
        'direct': b'>SYS:runprefix *<> C:SharedProbe\n',
        'run-no-shared': b'Run >SYS:runprefix C:SharedProbe\n',
        'direct-no-shared': b'>SYS:runprefix C:SharedProbe\n',
        'plain': b'C:SharedProbe\n',
        'run-plain': b'Run C:SharedProbe\n',
    }
    command = shared_commands[args.shared_command] if args.shared_error else b'Run ' + redirect + b'C:StreamProbe\n'
    disk.file(directories['s'], 'S/streamrun',
              b'FailAt 21\n' + (b'C:ParentProbe\n' if args.parent_state else b'') +
              command + b'Echo >SYS:parentdone done\n')
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
    values = {name: read(name) for name in ('input-sent.txt', 'parentdone', 'runprefix', 'streamreport', 'streamname', 'outer', 'runshared', 'systemdone', 'parentstate', 'sharedreport', 'sharedpre', 'sharedentry')}
    report = {name: (list(struct.unpack('>iiiiiii', value)) if name == 'streamreport' and value is not None and len(value) == 28
                     else list(struct.unpack('>IIIIIIIII', value)) if name == 'parentstate' and value is not None and len(value) == 36
                     else list(struct.unpack('>ii', value)) if name == 'sharedreport' and value is not None and len(value) == 8
                     else list(struct.unpack('>iI', value)) if name == 'sharedpre' and value is not None and len(value) == 8
                     else value.split(b'\0', 1)[0].decode('latin1') if name == 'streamname' and value is not None
                     else value.decode('latin1') if value is not None else None) for name, value in values.items()}
    sha = lambda value: hashlib.sha256(value).hexdigest()
    report.update({'originalShell': args.original, 'outerRedirection': args.outer,
                   'nilRedirection': args.nil, 'noLeadingRedirection': args.no_leading,
                   'systemOutput': args.system_output,
                   'nested': args.nested,
                   'consoleInput': args.console_input,
                   'sharedError': args.shared_error,
                   'sharedPreOnly': args.shared_pre_only,
                   'sharedCommand': args.shared_command if args.shared_error else None,
                   'inputImageSha256': sha(original),
                   'fixtureImageSha256': sha(fixture), 'resultImageSha256': sha(result_bytes),
                   'romSha256': sha(rom_bytes), 'winuaeSha256': sha(winuae_bytes), 'runtimeQualified': False})
    (output / 'observations.json').write_text(json.dumps(report, indent=2) + '\n')
    if source.read_bytes() != original or rom.read_bytes() != rom_bytes or winuae.read_bytes() != winuae_bytes:
        raise RuntimeError('An input changed during the observation')
    if (values['input-sent.txt'] != b'SENT\n' or
            (values['systemdone'] != b'OK\n' if args.system_output else values['parentdone'] != b'done\n') or
            (values['sharedpre'] is None or len(values['sharedpre']) != 8 if args.shared_pre_only else
             values['sharedreport'] is None or len(values['sharedreport']) != 8 if args.shared_error else
             values['streamreport'] is None or len(values['streamreport']) != 28 or values['streamname'] is None) or
            (args.parent_state and (values['parentstate'] is None or len(values['parentstate']) != 36)) or
            (not args.nil and not args.no_leading and
             (not args.shared_error or args.shared_command not in ('plain', 'run-plain')) and
             values['runprefix'] is None)):
        raise RuntimeError('RUN stream observation did not complete; see observations.json')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
