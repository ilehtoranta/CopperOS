"""Observe command failures and diagnostic streams on the ROM or shared Shell."""
import argparse
import hashlib
import json
import struct
import subprocess
import sys
import importlib.util
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_keyboard_probe import parent, child
from make_kickstart_shell_probe import build_cli_result_probe, module

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf
import shell_builtin_fixture as builtin_fixture


def build_list_fixture(result):
    fixture_path = Path(__file__).resolve().parents[2] / 'tests/Shell.Dos.NativeRoot/make_list_fixture.py'
    spec = importlib.util.spec_from_file_location('diagnostic_list_fixture', fixture_path)
    fixture_module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(fixture_module)
    return fixture_module.build(result, 'SYS:diag-list-arguments')


def system_diagnostic_probe():
    """Capture the ROM/shared System Shell's whole output, including lookup errors."""
    a = module.Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.address(1, 'dos'); a.d(0, 37); call(-552)
    a.words(0x2a40, 0x4a80); a.branch(0x6700, 'fail')
    a.words(0x2c4d); a.address(0, 'output'); a.words(0x2208); a.d(2, 1006); call(-30)
    a.words(0x2c00, 0x4a80); a.branch(0x6700, 'close_dos')
    a.address(0, 'nil'); a.words(0x2208); a.d(2, 1005); call(-30)
    a.words(0x2e00, 0x4a80); a.branch(0x6700, 'close_output')
    a.address(4, 'tags'); a.words(0x2946, 12, 0x2947, 20, 0x240c)
    a.address(0, 'command'); a.words(0x2208); call(-606)
    a.words(0x2207); call(-36)
    a.label('close_output'); a.words(0x2206); call(-36)
    a.label('close_dos'); a.words(0x224d, 0x2c78, 4); call(-414)
    a.words(0x7000); a.branch(0x6000, 'return')
    a.label('fail'); a.words(0x7014)
    a.label('return'); a.words(0x4cdf, 0x7cfc, 0x4e75)
    for name, value in [('dos', 'dos.library'), ('output', 'SYS:diag-system'),
                        ('nil', 'NIL:'), ('command', 'missingcommand\n')]:
        a.text(name, value)
    if len(a.code) & 1: a.code.append(0)
    a.label('tags'); a.code += struct.pack('>8I', 0x80000024, 1, 0x80000022, 0, 0x80000021, 0, 0, 0)
    return a.finish()


def diagnostic_script(plain=False, resident_errors=False, global_errors=False, resident_arguments='Missing'):
    script = (b'FailAt 21\n'
            b'missingcommand *>SYS:diag-missing >SYS:diag-silent\nC:Cap1\n'
            b'missingcommand *>>SYS:diag-missing\nC:Cap2\n'
            b'missingcommand >SYS:diag-merged *<>\nC:Cap3\n'
            b'CD SYS:__missing_directory *>SYS:diag-cd >SYS:diag-cd-out\nC:Cap4\n'
            b'Execute S:__missing_script *>SYS:diag-execute\nC:Cap5\n'
            b'Stack invalid *>SYS:diag-stack >SYS:diag-stack-out\nC:Cap6\n'
            b'Echo >SYS:diag-done done\n')
    if plain:
        for redirect in (b' *>SYS:diag-missing', b' *>>SYS:diag-missing', b' *<>',
                         b' *>SYS:diag-cd', b' *>SYS:diag-execute', b' *>SYS:diag-stack'):
            script = script.replace(redirect, b'')
        script = script.replace(b'Echo >SYS:diag-done', b'C:SysDiag\nEcho >SYS:diag-done')
    if resident_errors:
        command = b'Resident ' + resident_arguments.encode('ascii') + b' >SYS:resident-stdout'
        if not plain:
            command += b' *>SYS:resident-error'
        script = script.replace(b'Echo >SYS:diag-done', command + b'\nC:CapResident\nEcho >SYS:diag-done')
    if global_errors:
        for name in ('SetEnv', 'UnsetEnv'):
            lower = name.lower().encode('ascii')
            command = name.encode('ascii') + b' >SYS:' + lower + b'-stdout'
            if not plain:
                command += b' *>SYS:' + lower + b'-error'
            script = script.replace(b'Echo >SYS:diag-done', command + b'\nC:Cap' +
                                    name.encode('ascii') + b'\nEcho >SYS:diag-done')
    return script


def expected_diagnostics(plain=False, resident_errors=False, global_errors=False, list_result=None,
                         resident_arguments='Missing'):
    unknown = b'missingcommand: Unknown command\n'
    expected = {'diag-done': b'done\n', 'diag-missing': None if plain else unknown * 2,
                'diag-silent': None, 'diag-merged': None if plain else unknown,
                'diag-cd': None if plain else b'object not found\n',
                'diag-cd-out': b'object not found\n' if plain else b'',
                'diag-execute': None if plain else b'',
                'diag-stack': None if plain else b'bad number\n',
                'diag-stack-out': b'bad number\n' if plain else b''}
    expected.update({f'diag-result-{n}': struct.pack('>ii', *value) for n, value in
                     enumerate(((10, 205), (10, 205), (10, 205), (20, 205), (20, 205), (10, 115)), 1)})
    if plain:
        expected['diag-system'] = unknown + b'missingcommand failed returncode 10\n'
    if resident_errors:
        message, result = {
            'Missing': (b'object not found\n', (5, 205)),
            'one two three': (b'wrong number of arguments\n', (10, 118)),
        }[resident_arguments]
        expected.update({'resident-error': None if plain else message,
                         'resident-stdout': message if plain else b'',
                         'resident-result': struct.pack('>ii', *result)})
    if global_errors:
        normal = b'' if list_result is None else b'MOCK LIST\n'
        message = b'LIST: Unknown command\nLIST failed returncode 10\n' if list_result is None else (
            b'LIST failed returncode 10\n' if list_result == 10 else b'')
        for name in ('setenv', 'unsetenv'):
            expected.update({name + '-error': None if plain else message,
                             name + '-stdout': normal + message if plain else normal,
                             name + '-result': struct.pack('>ii', 0, 0)})
        if list_result is not None:
            expected['diag-list-arguments'] = b'env: QUICK NOHEAD FILES\n'
    return expected


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--execute-reference', type=Path, help='Replace C:Execute with a locally owned original command in the disposable image')
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--duration-seconds', type=int, default=120)
    parser.add_argument('--original', action='store_true')
    parser.add_argument('--builtins', action='store_true', help='Replay the complementary built-in command corpus')
    parser.add_argument('--builtin-controls', action='store_true', help='Isolate WHY outcomes and standalone ENDSKIP in the built-in corpus')
    parser.add_argument('--builtin-skip-label', choices=('', 'later', 'missing', 'eof', 'back', 'back-named', 'lab', 'lab-empty', 'lab-command'), default='', help='Check SKIP termination and backward search')
    parser.add_argument('--plain', action='store_true', help='Use V40 stdout syntax and capture a System command diagnostic')
    parser.add_argument('--recovery', action='store_true', help='Observe interactive recovery after malformed redirection and quoting')
    parser.add_argument('--script-recovery', action='store_true', help='Observe console recovery after malformed and FailAt-aborted Execute scripts')
    parser.add_argument('--resident-errors', action='store_true', help='Include Resident-owned failure diagnostics')
    parser.add_argument('--resident-arguments', default='Missing', help='Arguments for an observational Resident failure case')
    parser.add_argument('--global-errors', action='store_true', help='Omit LIST and observe SetEnv/UnsetEnv listing failures')
    parser.add_argument('--list-result', type=int, choices=(0, 5, 10), help='Install a deterministic LIST fixture for --global-errors')
    parser.add_argument('--verify-shared', action='store_true', help='Require the shared Shell diagnostic or recovery results')
    args = parser.parse_args()
    if args.builtin_skip_label and not args.builtin_controls:
        parser.error('--builtin-skip-label requires --builtin-controls')
    if args.builtin_controls and not args.builtins:
        parser.error('--builtin-controls requires --builtins')
    if args.builtins and (args.plain or args.recovery or args.script_recovery or args.resident_errors or args.global_errors):
        parser.error('--builtins is a separate script corpus')
    if args.resident_arguments != 'Missing' and not args.resident_errors:
        parser.error('Custom --resident-arguments requires --resident-errors')
    if args.verify_shared and args.resident_arguments not in ('Missing', 'one two three'):
        parser.error('This Resident argument tail has no original-Shell verification contract')
    if '\n' in args.resident_arguments or '\r' in args.resident_arguments:
        parser.error('--resident-arguments must be one command tail')
    if args.list_result is not None and not args.global_errors:
        parser.error('--list-result requires --global-errors')
    if args.script_recovery:
        args.recovery = True
    if args.plain and args.recovery:
        parser.error('Choose plain diagnostics or interactive recovery')
    if args.verify_shared and args.original:
        parser.error('--verify-shared requires the shared Shell')
    source, rom, winuae = (p.resolve(strict=True) for p in (args.image, args.rom, args.winuae))
    original, rom_bytes, engine_bytes = (p.read_bytes() for p in (source, rom, winuae))
    execute_reference = args.execute_reference.resolve(strict=True) if args.execute_reference else None
    execute_bytes = execute_reference.read_bytes() if execute_reference else None
    if execute_bytes is not None and not execute_bytes.startswith(b'\x00\x00\x03\xf3'):
        parser.error('--execute-reference must be a loadable HUNK')
    tree = Adf(original)
    entries = tree.walk()
    outputs = ['input-sent.txt', 'diag-done', 'diag-missing', 'diag-silent', 'diag-merged',
               'diag-cd', 'diag-cd-out', 'diag-execute', 'diag-stack', 'diag-stack-out']
    if args.plain:
        outputs.append('diag-system')
    results = [f'diag-result-{n}' for n in range(1, 7)]
    if args.resident_errors:
        outputs.extend(['resident-error', 'resident-stdout'])
        results.append('resident-result')
    if args.global_errors:
        for name in ('setenv', 'unsetenv'):
            outputs.extend([name + '-error', name + '-stdout'])
            results.append(name + '-result')
        if args.list_result is not None:
            outputs.append('diag-list-arguments')
    if args.recovery:
        outputs = ['input-sent.txt', 'recovery-redir', 'recovery-quote']
        results = []
        if args.script_recovery:
            outputs = ['input-sent.txt', 'recovery-script', 'recovery-failat', 'recovery-unexpected', 'recovery-if-error']
        if not args.original:
            outputs.append('shell-ended.txt')
    if args.builtins:
        builtin_expected = builtin_fixture.control_expected(args.builtin_skip_label) if args.builtin_controls else builtin_fixture.expected()
        results = [name for name in builtin_fixture.RESULTS if name in builtin_expected]
        outputs = ['input-sent.txt', *(name for name in builtin_expected if name not in results)]
        if args.builtin_skip_label in ('back', 'back-named'):
            outputs.append('bi-back-transcript')
            if 'bi-endskip-result' not in results:
                results.append('bi-endskip-result')
        if not args.original:
            outputs.append('shell-ended.txt')
    generated = set(outputs + results + ['c/keystart', 'c/keychild', 's/diagprobe'] +
                    [f'c/cap{n}' for n in range(1, 7)])
    if args.plain:
        generated.add('c/sysdiag')
    if args.script_recovery:
        generated.update({'s/badsyntax', 's/badfailat'})
    if args.resident_errors:
        generated.add('c/capresident')
    if args.global_errors:
        generated.update({'c/capsetenv', 'c/capunsetenv'})
    if args.builtins:
        generated.update(builtin_fixture.GENERATED)
    if generated.intersection(entries):
        raise ValueError('Input image already contains a diagnostic probe path')
    disk = Disk(16384)
    directories = {}
    empty_l = args.original and not any(key.startswith('l/') and key != 'l/shell-seg' for key in entries)
    for key, entry in sorted(entries.items(), key=lambda item: (item[0].count('/'), item[0])):
        if args.global_errors and key == 'c/list':
            continue
        if args.original and (key == 'l/shell-seg' or (key == 'l' and empty_l)):
            continue
        parts = entry['path'].split('/')
        owner = disk.root if len(parts) == 1 else directories['/'.join(parts[:-1]).lower()]
        if entry['secondary_type'] == 2:
            directories[key] = disk.directory(owner, entry['name'])
        elif entry['secondary_type'] == -3:
            startup = b'C:KeyStart\n' + (b'' if args.original else b'C:BootShell\n')
            if args.builtins:
                # Stock Execute requires T:, and undefined variables can cause
                # an ENV: volume requester when no environment is assigned.
                startup = b'C:BIEnv\n' + startup
            payload = startup if key == 's/startup-sequence' else tree.read_file(entry['block'])
            if key == 'c/execute' and execute_bytes is not None:
                payload = execute_bytes
            disk.file(owner, entry['path'], payload)
        else:
            raise ValueError('Unexpected input entry: ' + key)
    disk.file(directories['c'], 'C/KeyStart', parent())
    if args.list_result is not None:
        disk.file(directories['c'], 'C/List', build_list_fixture(args.list_result))
    if args.recovery:
        commands = 'echo >\necho >sys:recovery-redir survived\necho "unterminated\necho >sys:recovery-quote survived\nendcli\n'
        recovery_marker = 'SYS:recovery-quote'
        if args.script_recovery:
            commands = 'execute s:badsyntax\necho >sys:recovery-script survived\nexecute s:badfailat\nif error\necho >sys:recovery-if-error yes\nendif\necho >sys:recovery-failat survived\nendcli\n'
            recovery_marker = 'SYS:recovery-failat'
            disk.file(directories['s'], 'S/badsyntax', b'Echo >\nEcho >SYS:recovery-unexpected badsyntax\n')
            disk.file(directories['s'], 'S/badfailat', b'FailAt 10\nStack invalid\nEcho >SYS:recovery-unexpected failat\n')
        injector = child(commands, wait_for_path=recovery_marker, wait_after_line=7 if args.script_recovery else 4,
                         check_shell_exit=not args.original)
    elif args.builtins:
        if args.builtin_skip_label in ('missing', 'eof', 'lab', 'back'):
            injector = child('execute s:diagprobe\nc:bicap6\necho >sys:bi-done done\n' +
                             builtin_fixture.followup(complete_corpus=not args.builtin_controls) + '\nendcli\n',
                             line_delay_ticks=75, check_shell_exit=not args.original)
        else:
            execute = 'execute s:diagprobe'
            if args.builtin_skip_label in ('back', 'back-named'):
                execute += ' >sys:bi-back-transcript'
            capture = 'c:bicap6\n' if args.builtin_skip_label in ('back', 'back-named') else ''
            injector = child(execute + '\n' + capture + builtin_fixture.followup(complete_corpus=not args.builtin_controls) + '\nendcli\n',
                             wait_for_path='SYS:bi-done', wait_after_line=1,
                             check_shell_exit=not args.original)
    else:
        injector = child('execute s:diagprobe\n', wait_for_path='SYS:diag-done')
    disk.file(directories['c'], 'C/KeyChild', injector)
    for n in range(1, 7):
        disk.file(directories['c'], f'C/Cap{n}', build_cli_result_probe(f'SYS:diag-result-{n}'))
    if args.resident_errors:
        disk.file(directories['c'], 'C/CapResident', build_cli_result_probe('SYS:resident-result'))
    if args.global_errors:
        for name in ('SetEnv', 'UnsetEnv'):
            disk.file(directories['c'], 'C/Cap' + name,
                      build_cli_result_probe('SYS:' + name.lower() + '-result'))
    if args.plain:
        disk.file(directories['c'], 'C/SysDiag', system_diagnostic_probe())
    if args.builtins:
        builtin_fixture.install(disk, directories)
    script = builtin_fixture.script(include_extensions=not args.original) if args.builtins else diagnostic_script(args.plain, args.resident_errors, args.global_errors, args.resident_arguments)
    if args.builtin_controls:
        script = builtin_fixture.control_script(args.builtin_skip_label)
    disk.file(directories['s'], 'S/diagprobe', script)
    fixture = disk.finish()
    verify(fixture, disk.files)
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    image = output / 'probe.hdf'
    image.write_bytes(fixture)
    subprocess.run(['pwsh', '-NoProfile', '-File', str(Path(__file__).with_name('run_kickstart31_candidate.ps1')),
                    '-Image', str(image), '-KickstartRom', str(rom), '-WinUAE', str(winuae),
                    '-OutputDirectory', str(output / 'run'), '-DurationSeconds', str(args.duration_seconds)], check=True)
    result_bytes = (output / 'run' / 'candidate.hdf').read_bytes()
    result = Adf(result_bytes)
    found = result.walk()
    def read(name):
        entry = found.get(name)
        return result.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    values = {name: read(name) for name in outputs + results}
    report = {name: list(struct.unpack('>ii', value)) if name in results and value is not None and len(value) == 8
              else value.decode('latin1') if value is not None else None for name, value in values.items()}
    sha = lambda value: hashlib.sha256(value).hexdigest()
    report.update({'originalShell': args.original, 'plainSyntax': args.plain, 'interactiveRecovery': args.recovery,
                   'scriptRecovery': args.script_recovery,
                   'residentErrors': args.resident_errors,
                   'residentArguments': args.resident_arguments,
                   'globalErrors': args.global_errors,
                   'listFixtureResult': args.list_result,
                   'builtinCorpus': args.builtins,
                   'builtinControls': args.builtin_controls,
                   'builtinSkipLabel': args.builtin_skip_label,
                   'executeReferenceSha256': sha(execute_bytes) if execute_bytes is not None else None,
                   'inputImageSha256': sha(original),
                   'fixtureImageSha256': sha(fixture), 'resultImageSha256': sha(result_bytes),
                   'romSha256': sha(rom_bytes), 'winuaeSha256': sha(engine_bytes), 'runtimeQualified': False})
    (output / 'observations.json').write_text(json.dumps(report, indent=2) + '\n')
    if source.read_bytes() != original or rom.read_bytes() != rom_bytes or winuae.read_bytes() != engine_bytes:
        raise RuntimeError('An input changed during the observation')
    if execute_reference and execute_reference.read_bytes() != execute_bytes:
        raise RuntimeError('The original Execute command changed during the observation')
    completion = 'recovery-quote' if args.recovery else 'diag-done'
    if args.script_recovery:
        completion = 'recovery-failat'
    if args.builtins:
        completion = 'bi-after-quit'
    completed_bytes = b'survived\n' if args.recovery else b'done\n'
    required_results = [name for name in results if not (args.builtin_skip_label == 'later' and name == 'bi-endskip-result')]
    if (values['input-sent.txt'] != b'SENT\n' or values[completion] != completed_bytes or
            any(values[name] is None or len(values[name]) != 8 for name in required_results)):
        raise RuntimeError('Diagnostic observation did not complete; see observations.json')
    if args.verify_shared:
        expected = {'recovery-redir': b'survived\n', 'recovery-quote': b'survived\n'} if args.recovery else expected_diagnostics(args.plain, args.resident_errors, args.global_errors, args.list_result, args.resident_arguments)
        if args.builtins:
            expected = builtin_fixture.control_expected(args.builtin_skip_label) if args.builtin_controls else builtin_fixture.expected()
            expected['shell-ended.txt'] = b'ENDED\n'
        if args.script_recovery:
            expected = {'recovery-script': b'survived\n', 'recovery-failat': b'survived\n', 'recovery-unexpected': b'badsyntax\n',
                        'recovery-if-error': b'yes\n'}
        if args.recovery:
            expected['shell-ended.txt'] = b'ENDED\n'
        if any(values[name] != value for name, value in expected.items()):
            raise RuntimeError('Shared Shell diagnostic mismatch; see observations.json')
        receipt = {'kind': 'shared-shell-diagnostics-check', 'checksPassed': True,
                   'inputImageSha256': sha(original), 'fixtureImageSha256': sha(fixture),
                   'resultImageSha256': sha(result_bytes), 'romSha256': sha(rom_bytes),
                   'winuaeSha256': sha(engine_bytes), 'checkedPaths': sorted(expected),
                   'limits': 'Focused diagnostic/recovery checks; complete Shell acceptance remains separate'}
        (output / 'qualification.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
