"""Exercise public Shell child processes on a disposable CopperStart proof disk.

The broad case launches several workers in succession; the owned case starts
with the leading-output command. The nested-leading case tests whether a
preceding background script is sufficient to reproduce a failure; its control
gives the second worker an independent NIL: input. These cases do not qualify
the complete Shell. The new-shell and new-cli cases exercise startup and
inherited CLI state in an independent process.
"""
import argparse
import hashlib
import json
import os
import re
import struct
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_shell_probe import (build_cli_result_probe, build_deferred_segment_probe, build_path_signal_probe, build_nonresident_boot_probe, build_prompt_probe,
                                       build_resident_system_probe, build_run_input_probe,
                                       build_system_probe, build_system_async_probe, build_system_input_probe,
                                       build_control_script, build_many_path_script)
from execute_disk_full_fixture import scripts as disk_full_scripts, fill_volume, build_execute_probe
from observe_diagnostics31 import diagnostic_script, expected_diagnostics, build_list_fixture
import shell_builtin_fixture as builtin_fixture

sys.path[:0] = [str(Path(__file__).resolve().parents[1] / 'DiskBuilder'),
                str(Path(__file__).resolve().parent / 'tests')]
from verify_image import Adf
from make_error_probe import build as build_error_probe


def sha(data):
    return hashlib.sha256(data).hexdigest()


def requires_run_notice(case):
    return case in ('broad', 'owned', 'nested-leading', 'nested-leading-control', 'broad-prefix')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', type=Path, required=True)
    parser.add_argument('--rom', type=Path, required=True)
    parser.add_argument('--extension-rom', type=Path, required=True)
    parser.add_argument('--probe-dll', type=Path, required=True)
    parser.add_argument('--output-directory', type=Path, required=True)
    parser.add_argument('--case', choices=('broad', 'owned', 'nested-leading',
                                          'nested-leading-control', 'broad-prefix',
                                          'new-shell', 'new-cli', 'new-shell-prompt', 'console-clear', 'console-reset', 'resident-system',
                                          'resident-system-short', 'resident-defer', 'nonresident-run', 'system-api', 'control-flow', 'execute-full', 'foreground-smoke', 'longpath', 'path-many', 'path-isolation', 'path', 'cd', 'diagnostics', 'recovery', 'script-recovery', 'ask', 'builtins'), default='broad')
    parser.add_argument('--runner-hunk', type=Path,
                        help='68000 long-path probe HUNK, required for --case longpath')
    parser.add_argument('--runner-df1', action='store_true',
                        help='Place the long-path probe on a read-only DF1 helper disk')
    parser.add_argument('--runner-fixture', type=Path,
                        help='Place an additional command HUNK on the DF1 helper disk')
    parser.add_argument('--nonresident-shell', action='store_true',
                        help='Start the proof Shell without permanent segment registration')
    parser.add_argument('--control-padding-lines', type=int, default=0,
                        help='Insert comment lines into the control-flow script to cross buffered-copy boundaries')
    parser.add_argument('--execute-failure-stage', choices=('source', 'tail'), default='source')
    parser.add_argument('--resident-errors', action='store_true', help='Include Resident diagnostic routing in the diagnostics case')
    parser.add_argument('--resident-arguments', default='Missing', choices=('Missing', 'one two three'), help='Resident failure case established by the original Shell')
    parser.add_argument('--global-errors', action='store_true', help='Omit LIST and include global-listing diagnostics')
    parser.add_argument('--list-result', type=int, choices=(0, 5, 10), help='Install a deterministic LIST fixture for --global-errors')
    parser.add_argument('--observe-only', action='store_true', help='Collect the built-in corpus before its reference comparison')
    parser.add_argument('--builtin-controls', action='store_true', help='Use the focused WHY/SKIP control corpus')
    parser.add_argument('--builtin-skip-label', choices=('', 'later', 'missing', 'eof', 'back', 'back-named', 'lab', 'lab-empty', 'lab-command'), default='')
    parser.add_argument('--prelude-count', type=int, choices=range(6), default=5,
                        help='Number of broad-case workers before the leading-output command')
    args = parser.parse_args()
    if args.builtin_controls and args.case != 'builtins':
        parser.error('--builtin-controls requires --case builtins')
    if args.builtin_skip_label and not args.builtin_controls:
        parser.error('--builtin-skip-label requires --builtin-controls')
    builtin_expected = builtin_fixture.control_expected(args.builtin_skip_label) if args.builtin_controls else builtin_fixture.expected()
    if args.observe_only and args.case != 'builtins':
        parser.error('--observe-only requires --case builtins')
    if args.resident_arguments != 'Missing' and not args.resident_errors:
        parser.error('--resident-arguments requires --resident-errors')
    if args.list_result is not None and not args.global_errors:
        parser.error('--list-result requires --global-errors')
    if (args.resident_errors or args.global_errors) and args.case != 'diagnostics':
        parser.error('--resident-errors and --global-errors require --case diagnostics')
    if not 0 <= args.control_padding_lines <= 1024 or (args.control_padding_lines and args.case != 'control-flow'):
        parser.error('--control-padding-lines requires control-flow and a value from 0 to 1024')
    if (args.case == 'longpath') != (args.runner_hunk is not None):
        parser.error('--runner-hunk is required only for --case longpath')
    if args.runner_df1 and args.case != 'longpath':
        parser.error('--runner-df1 requires --case longpath')
    if args.runner_fixture and not args.runner_df1:
        parser.error('--runner-fixture requires --runner-df1')
    runner = args.runner_hunk.resolve(strict=True) if args.runner_hunk else None
    runner_bytes = runner.read_bytes() if runner else None
    runner_fixture = args.runner_fixture.resolve(strict=True) if args.runner_fixture else None
    runner_fixture_bytes = runner_fixture.read_bytes() if runner_fixture else None
    if runner_fixture_bytes is not None and not runner_fixture_bytes.startswith(b'\x00\x00\x03\xf3'):
        parser.error('--runner-fixture must be a loadable HUNK')

    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    tree = Adf(original)
    entries = tree.walk()
    generated = {'c/capresult', 'c/errprobe', 'c/readprobe', 'c/promptprobe', 's/regress',
                 's/runchild', 's/runinput', 's/newstart', 'rdiagnostics.bin', 'newprompt',
                 *(f't/{name}' for name in ('runout', 'rundemo', 'runescaped',
                   'runstdout', 'runstderr', 'runnested', 'runprefix',
                   'runleading-child', 'runalias', 'runvar', 'runpath',
                   'runfinish', 'runerr', 'runread', 'runboth', 'newalias',
                   'newvar', 'newpath', 'newdir', 'newstack', 'newdone', 'parentdone'))}
    if args.case == 'resident-system':
        generated.update({'c/segstate', 'c/capremove', 'c/capreplace', 'c/capsystem', 'c/capsystem2', 'c/capabsent',
                          'segcount.bin', 'remove-result', 'replace-result', 'system-result', 'system-result2',
                          'absent-result', 'sysadd', 'sysrun', 'sysremove', 'sysreplace-system', 'sysreplace-system2',
                          'sysreplace', 'sysabsent', 'sysaddnormal', 'sysaddsystem3', 'syslist', 'syslist2', 'syslist3', 'sysdone'})
    if args.case == 'resident-system-short':
        generated.update({'syslist3', 'sysdone'})
    if args.case == 'system-api':
        generated.update({'c/shellsystem', 'c/shellasync', 'c/shellnull', 'c/shellinput', 'c/readprobe',
                          's/sysinput', 's/sysnull', 'nullresult.txt', 'nullchild.txt',
                          'systemresult.txt', 'systemchild.txt', 'asyncresult.txt', 'asyncchild.txt',
                          'sysread', 'sysinputresult', 'systembefore', 'systemdone'})
    if args.case == 'control-flow':
        generated.update({'s/controlprobe', 'ctl-arguments', 'ctl-true', 'ctl-else',
                          'ctl-wrong', 'ctl-skipped', 'ctl-variable', 'ctl-alias',
                          'ctl-redirection', 'ctl-childdone', 'ctlparentdone'})
    if args.case == 'execute-full':
        generated.update({'c/execfull', 'c/capfull', 's/execfull', 'execute-fill',
                          'execute-full-result', 'execute-full-done', 'execute-full-signal'})
    if args.case == 'cd':
        generated.update({'c/capresult', 'cdfirst', 'cdparent', 'cdsecond', 'cdafter', 'rdiagnostics.bin'})
    if args.case == 'diagnostics':
        generated.update(expected_diagnostics(False, args.resident_errors, args.global_errors, args.list_result, args.resident_arguments))
        generated.update({f'c/cap{n}' for n in range(1, 7)})
        if args.resident_errors:
            generated.add('c/capresident')
        if args.global_errors:
            generated.update({'c/capsetenv', 'c/capunsetenv'})
    if args.case == 'builtins':
        generated.update(builtin_expected)
        generated.update(builtin_fixture.GENERATED)
    if args.case == 'recovery':
        generated.update({'recovery-redir', 'recovery-quote'})
    if args.case == 'script-recovery':
        generated.update({'s/badsyntax', 's/badfailat', 'recovery-script', 'recovery-failat', 'recovery-unexpected', 'recovery-if-error'})
    if args.case == 'path':
        generated.update({'s/pathprobe', 'pathinitial', 'pathadded', 'pathhit', 'pathremoved',
                          'pathmiss', 'pathcleared', 'pathdemo'})
    if args.case == 'foreground-smoke':
        generated.update({'foreground-result', 'foreground-done'})
    if args.case == 'ask':
        generated.update({'c/capyes', 'c/capno', 'c/capretry', 's/askyes', 's/askno', 's/askretry',
                          'ask-yes-result', 'ask-no-result', 'ask-retry-result', 'ask-yes-text',
                          'ask-no-text', 'ask-retry-text', 'askdone'})
    if args.case == 'longpath':
        generated.update({'c/longpath', 'runner-ok', 'runner-fail'})
    if args.case == 'path-many':
        generated.update({'pathmany', 'pathmanydone'})
    if args.case == 'path-isolation':
        generated.update({'c/sigsetup', 'c/sigwait', 'c/sigsend', 's/pathchild', 'path-parent-before', 'path-parent-after',
                          'path-child-before', 'path-child-after', 'path-child-done', 'path-setup-ok', 'path-wait-ok'})
    if args.case == 'resident-defer':
        generated.update({'c/deferbefore', 'c/deferafter', 'c/deferremoved', 'c/deferreject', 'c/capreject',
                          'defer-before', 'defer-after', 'defer-removed',
                          'defer-reject', 'defer-reject-state', 'defer-add', 'defer-output', 'defer-remove', 'defer-done'})
    if generated.intersection(entries):
        raise ValueError('Input image already contains a probe path')
    disk = Disk()
    directories = {}
    for key, entry in sorted(entries.items(), key=lambda item: (item[0].count('/'), item[0])):
        if args.global_errors and key == 'c/list':
            continue
        parts = entry['path'].split('/')
        owner = disk.root if len(parts) == 1 else directories['/'.join(parts[:-1]).lower()]
        if entry['secondary_type'] == 2:
            directories[key] = disk.directory(owner, entry['name'])
        elif entry['secondary_type'] == -3:
            content = (build_nonresident_boot_probe() if key == 'c/bootshell' and args.nonresident_shell
                       else tree.read_file(entry['block']))
            disk.file(owner, entry['path'], content)
        else:
            raise ValueError('Unexpected input entry: ' + entry['path'])
    if not {'c', 's', 't'}.issubset(directories):
        raise ValueError('Input image must contain C:, S: and T:')
    disk.file(directories['c'], 'C/ErrProbe', build_error_probe())
    if args.case == 'nonresident-run':
        if not args.nonresident_shell:
            parser.error('nonresident-run requires --nonresident-shell')
        disk.file(directories['c'], 'C/CapResult', build_cli_result_probe())
        script = (b'FailAt 21\nRun Echo >SYS:T/runout child\nC:CapResult\n'
                  b'Echo >SYS:T/runfinish done\nEcho SCRIPT DONE\n')
        expected = {'t/runout': b'child\n', 't/runfinish': b'done\n',
                    'rdiagnostics.bin': struct.pack('>ii', 0, 0)}
    elif args.case in ('console-clear', 'console-reset'):
        command = b'Cls RESET' if args.case == 'console-reset' else b'Cls'
        script = (b'Echo RESET SENTINEL\n' + command +
                  b'\nEcho >SYS:console-done done\nEcho SCRIPT DONE\n')
        expected = {'console-done': b'done\n'}
    elif args.case == 'ask':
        for command, target in (('CapYes', 'ask-yes-result'), ('CapNo', 'ask-no-result'),
                                ('CapRetry', 'ask-retry-result')):
            disk.file(directories['c'], 'C/' + command, build_cli_result_probe('SYS:' + target))
        for filename, content in (('askyes', b'Y\n'), ('askno', b'N\n'), ('askretry', b'X\nY\n')):
            disk.file(directories['s'], 'S/' + filename, content)
        script = (b'FailAt 21\nAsk <SYS:S/askyes >SYS:ask-yes-text "Continue?"\nC:CapYes\n'
                  b'Ask <SYS:S/askno >SYS:ask-no-text "Continue?"\nC:CapNo\n'
                  b'Ask <SYS:S/askretry >SYS:ask-retry-text "Continue?"\nC:CapRetry\n'
                  b'Echo >SYS:askdone done\nEcho SCRIPT DONE\n')
        expected = {'ask-yes-result': struct.pack('>ii', 5, 0),
                    'ask-no-result': struct.pack('>ii', 0, 0),
                    'ask-retry-result': struct.pack('>ii', 5, 118),
                    'ask-yes-text': b'Continue? ', 'ask-no-text': b'Continue? ',
                    'ask-retry-text': b'Continue? Continue? ', 'askdone': b'done\n'}
    elif args.case == 'foreground-smoke':
        script = (b'FailAt 21\nC:Demo >SYS:foreground-result\n'
                  b'Echo >SYS:foreground-done done\nEcho SCRIPT DONE\n')
        expected = {'foreground-result': b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n',
                    'foreground-done': b'done\n'}
    elif args.case == 'script-recovery':
        disk.file(directories['s'], 'S/badsyntax', b'Echo >\nEcho >SYS:recovery-unexpected badsyntax\n')
        disk.file(directories['s'], 'S/badfailat', b'FailAt 10\nStack invalid\nEcho >SYS:recovery-unexpected failat\n')
        script = b''
        expected = {'recovery-script': b'survived\n', 'recovery-failat': b'survived\n', 'recovery-unexpected': b'badsyntax\n',
                    'recovery-if-error': b'yes\n'}
    elif args.case == 'recovery':
        script = b''
        expected = {'recovery-redir': b'survived\n', 'recovery-quote': b'survived\n'}
    elif args.case == 'diagnostics':
        for n in range(1, 7):
            disk.file(directories['c'], f'C/Cap{n}', build_cli_result_probe(f'SYS:diag-result-{n}'))
        if args.resident_errors:
            disk.file(directories['c'], 'C/CapResident', build_cli_result_probe('SYS:resident-result'))
        if args.global_errors:
            for name in ('SetEnv', 'UnsetEnv'):
                disk.file(directories['c'], 'C/Cap' + name,
                          build_cli_result_probe('SYS:' + name.lower() + '-result'))
            if args.list_result is not None:
                disk.file(directories['c'], 'C/List', build_list_fixture(args.list_result))
        script = diagnostic_script(False, args.resident_errors, args.global_errors, args.resident_arguments) + b'Echo SCRIPT DONE\n'
        expected = expected_diagnostics(False, args.resident_errors, args.global_errors, args.list_result, args.resident_arguments)
    elif args.case == 'builtins':
        builtin_fixture.install(disk, directories)
        script = (b'C:BIEnv\n' + builtin_fixture.control_script(args.builtin_skip_label)
                  if args.builtin_controls else builtin_fixture.script())
        expected = builtin_expected
    elif args.case == 'cd':
        disk.file(directories['c'], 'C/CapResult', build_cli_result_probe())
        script = (b'FailAt 21\nCD SYS:S\nCD >SYS:cdfirst\nCD /\nCD >SYS:cdparent\n'
                  b'CD SYS:C\nCD >SYS:cdsecond\nCD SYS:missing-directory\nC:CapResult\n'
                  b'CD >SYS:cdafter\nEcho SCRIPT DONE\n')
        expected = {'cdfirst': b'CopperOS:S\n', 'cdparent': b'CopperOS:\n',
                    'cdsecond': b'CopperOS:C\n', 'cdafter': b'CopperOS:C\n',
                    'rdiagnostics.bin': struct.pack('>ii', 20, 205)}
    elif args.case == 'path':
        demo = entries.get('c/demo')
        if demo is None or demo['secondary_type'] != -3:
            raise ValueError('Input image lacks the demonstration command')
        disk.file(directories['s'], 'S/pathprobe', tree.read_file(demo['block']))
        script = (b'FailAt 21\nPath RESET\nPath SYS:C ADD\nPath >SYS:pathinitial\n'
                  b'Path SYS:S ADD\nPath >SYS:pathadded\npathprobe >SYS:pathhit\n'
                  b'Path SYS:S REMOVE\nPath >SYS:pathremoved\npathprobe >SYS:pathmiss\n'
                  b'Path RESET\nPath >SYS:pathcleared\nDemo >SYS:pathdemo\nEcho SCRIPT DONE\n')
        initial = b'Current_directory\nCopperOS:C\nC:\n'
        output = b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n'
        expected = {'pathinitial': initial, 'pathadded': b'Current_directory\nCopperOS:C\nCopperOS:S\nC:\n',
                    'pathhit': output, 'pathremoved': initial, 'pathmiss': None,
                    'pathcleared': b'Current_directory\nC:\n', 'pathdemo': output}
    elif args.case == 'path-isolation':
        for command, kind in (('SigSetup', 'setup'), ('SigWait', 'wait'), ('SigSend', 'send')):
            disk.file(directories['c'], 'C/' + command, build_path_signal_probe(kind))
        disk.file(directories['s'], 'S/pathchild',
                  b'Path >SYS:path-child-before\nPath RESET\nPath SYS:C ADD\n'
                  b'Path >SYS:path-child-after\nEcho >SYS:path-child-done done\nC:SigSend\n')
        script = (b'FailAt 21\nPath RESET\nPath SYS:S ADD\nPath >SYS:path-parent-before\n'
                  b'C:SigSetup\nRun Execute S:pathchild\nC:SigWait\nPath >SYS:path-parent-after\nEcho SCRIPT DONE\n')
        parent_path = b'Current_directory\nCopperOS:S\nC:\n'
        expected = {'path-parent-before': parent_path, 'path-parent-after': parent_path,
                    'path-child-before': parent_path,
                    'path-child-after': b'Current_directory\nCopperOS:C\nC:\n',
                    'path-child-done': b'done\n', 'path-setup-ok': b'READY\n', 'path-wait-ok': b'WAITED\n'}
    elif args.case == 'path-many':
        script = build_many_path_script() + b'Echo SCRIPT DONE\n'
        expected = {'pathmany': b'Current_directory\nCopperOS:S\nC:\n',
                    'pathmanydone': b'done\n'}
    elif args.case == 'longpath':
        if not args.runner_df1:
            disk.file(directories['c'], 'C/LongPath', runner_bytes)
        script = (b'FailAt 21\nDF1:LongPath\nEcho SCRIPT DONE\n' if args.runner_df1 else
                  b'FailAt 21\nC:LongPath\nEcho SCRIPT DONE\n')
        expected = {'runner-ok': b'', 'runner-fail': None}
    elif args.case == 'execute-full':
        source_script, script = disk_full_scripts(args.execute_failure_stage)
        disk.file(directories['s'], 'S/execfull', source_script)
        disk.file(directories['c'], 'C/CapFull', build_cli_result_probe('SYS:execute-full-result'))
        disk.file(directories['c'], 'C/ExecFull', build_execute_probe())
        # Existing allocated result blocks let the guest report a failed copy
        # even when the faulty implementation leaves the volume completely full.
        disk.file(disk.root, 'execute-full-result', bytes([0xa5]) * 8)
        disk.file(disk.root, 'execute-full-done', b'PENDING!\n')
        expected = {'execute-full-result': struct.pack('>ii', 10, 221),
                    'execute-full-done': b'survived\n', 'execute-full-signal': b'done\n'}
    elif args.case == 'control-flow':
        disk.file(directories['s'], 'S/controlprobe', build_control_script(args.control_padding_lines))
        script = (b'FailAt 21\nExecute S:controlprobe world\n'
                  b'Echo >SYS:ctlparentdone done\nEcho SCRIPT DONE\n')
        expected = {'ctl-arguments': b'world fallback\n', 'ctl-true': b'yes\n',
                    'ctl-else': b'yes\n', 'ctl-wrong': None, 'ctl-skipped': None,
                    'ctl-variable': b'two words\n', 'ctl-alias': b'alias\n',
                    'ctl-redirection': b'alpha\nbeta\n', 'ctl-childdone': b'done\n',
                    'ctlparentdone': b'done\n'}
    elif args.case == 'system-api':
        disk.file(directories['c'], 'C/ShellSystem',
                  build_system_probe('Echo >SYS:systemchild.txt sync\n'))
        disk.file(directories['c'], 'C/ShellAsync', build_system_async_probe())
        disk.file(directories['c'], 'C/ShellNull', build_system_async_probe(null_command=True))
        disk.file(directories['c'], 'C/ShellInput', build_system_input_probe())
        disk.file(directories['c'], 'C/ReadProbe', build_run_input_probe())
        disk.file(directories['s'], 'S/sysinput', b'INPUT DATA\n')
        disk.file(directories['s'], 'S/sysnull', b'Echo >SYS:nullchild.txt null\n')
        script = (b'FailAt 21\nEcho >SYS:systembefore start\nC:ShellSystem\nC:ShellInput\nC:ShellAsync\nC:ShellNull\n'
                  b'Echo >SYS:systemdone done\nEcho SCRIPT DONE\n')
        expected = {'systemresult.txt': b'OK\n', 'systemchild.txt': b'sync\n',
                    'asyncresult.txt': b'OK\n', 'asyncchild.txt': b'async\n',
                    'nullresult.txt': b'OK\n', 'nullchild.txt': b'null\n',
                    'sysread': b'INPUT DATA\n', 'sysinputresult': b'OK\n',
                    'systembefore': b'start\n', 'systemdone': b'done\n'}
    elif args.case == 'broad':
        disk.file(directories['c'], 'C/CapResult', build_cli_result_probe())
        disk.file(directories['s'], 'S/runchild', b'Echo >SYS:T/runnested nested\n')
        script = (b'FailAt 21\nRun Echo child >SYS:T/runout\nRun C:Demo >SYS:T/rundemo\n'
                  b'Run Echo "hello*Nworld" >SYS:T/runescaped\n'
                  b'Run C:ErrProbe >SYS:T/runstdout *>SYS:T/runstderr\nRun Execute S:runchild\n'
                  b'Run >SYS:T/runprefix Echo child >SYS:T/runleading-child\n'
                  b'Alias RunAlias Echo inherited\nRun RunAlias >SYS:T/runalias\n'
                  b'Set RunVariable value\nRun Get RunVariable >SYS:T/runvar\n'
                  b'Path RESET\nPath SYS:S ADD\nRun Path >SYS:T/runpath\nPath RESET\n'
                  b'Run\nCapResult\nEcho >SYS:T/runfinish done\nEcho SCRIPT DONE\n')
        expected = {'t/runout': b'child\n', 't/rundemo':
                    b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n',
                    't/runescaped': b'hello\nworld\n', 't/runstdout': b'OUT\n',
                    't/runstderr': b'ERR\n', 't/runnested': b'nested\n',
                    't/runleading-child': b'child\n', 't/runalias': b'inherited\n',
                    't/runvar': b'value\n', 't/runpath': b'Current_directory\nCopperOS:S\nC:\n',
                    't/runfinish': b'done\n', 'rdiagnostics.bin': struct.pack('>ii', 0, 0)}
    elif args.case == 'resident-defer':
        for command, target in (('DeferBefore', 'defer-before'), ('DeferAfter', 'defer-after'),
                                ('DeferRemoved', 'defer-removed'), ('DeferReject', 'defer-reject-state')):
            disk.file(directories['c'], 'C/' + command, build_deferred_segment_probe('SYS:' + target))
        disk.file(directories['c'], 'C/CapReject', build_cli_result_probe('SYS:defer-reject'))
        script = (b'FailAt 21\nResident Dprobe SYS:C/Demo ADD DEFER >NIL:\nCapReject\nDeferReject\n'
                  b'Resident Dprobe SYS:C/Demo ADD FORCE DEFER >SYS:defer-add\n'
                  b'DeferBefore\nDprobe >SYS:defer-output\nDeferAfter\n'
                  b'Resident Dprobe REMOVE >SYS:defer-remove\nDeferRemoved\n'
                  b'Echo >SYS:defer-done done\nEcho SCRIPT DONE\n')
        expected = {'defer-reject': struct.pack('>ii', 5, 122), 'defer-reject-state': struct.pack('>i', -1),
                    'defer-before': struct.pack('>i', 0), 'defer-after': struct.pack('>i', 1),
                    'defer-removed': struct.pack('>i', -1),
                    'defer-add': b'Warning: forced resident is unsafe.\n',
                    'defer-output': b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n',
                    'defer-remove': b'', 'defer-done': b'done\n'}
    elif args.case == 'resident-system-short':
        script = (b'FailAt 21\nResident Sysprobe SYS:C/Demo ADD PURE SYSTEM >NIL:\n'
                  b'Resident Sysprobe SYS:C/Demo REPLACE PURE >NIL:\n'
                  b'Resident Sysprobe SYS:C/Demo ADD PURE SYSTEM >NIL:\n'
                  b'Resident SYSTEM >SYS:syslist3\nEcho >SYS:sysdone done\nEcho SCRIPT DONE\n')
        expected = {'sysdone': b'done\n'}
    elif args.case == 'resident-system':
        disk.file(directories['c'], 'C/SegState', build_resident_system_probe())
        for command, target in (('CapRemove', 'remove-result'), ('CapReplace', 'replace-result'),
                                ('CapSystem', 'system-result'), ('CapSystem2', 'system-result2'),
                                ('CapAbsent', 'absent-result')):
            disk.file(directories['c'], 'C/' + command, build_cli_result_probe('SYS:' + target))
        script = (b'FailAt 21\nResident Sysprobe SYS:C/Demo ADD PURE SYSTEM >SYS:sysadd\n'
                  b'SegState\nSysprobe >SYS:sysrun\nResident Sysprobe REMOVE >SYS:sysremove\nCapRemove\n'
                  b'Resident Sysprobe SYS:C/Demo REPLACE PURE SYSTEM >SYS:sysreplace-system\nCapSystem\n'
                  b'Resident Sysprobe SYS:C/Demo REPLACE PURE >SYS:sysreplace\nCapReplace\n'
                  b'Resident Missing SYS:C/Demo REPLACE PURE >SYS:sysabsent\nCapAbsent\n'
                  b'Resident SYSTEM >SYS:syslist\n'
                  b'Resident Sysprobe SYS:C/Demo REPLACE PURE SYSTEM >SYS:sysreplace-system2\nCapSystem2\n'
                  b'Resident SYSTEM >SYS:syslist2\n'
                  b'Resident Sysprobe SYS:C/Demo ADD PURE >SYS:sysaddnormal\n'
                  b'Resident Sysprobe SYS:C/Demo ADD PURE SYSTEM >SYS:sysaddsystem3\n'
                  b'Resident SYSTEM >SYS:syslist3\nEcho >SYS:sysdone done\nEcho SCRIPT DONE\n')
        expected = {'segcount.bin': struct.pack('>i', -1), 'remove-result': struct.pack('>ii', 5, 202),
                    'replace-result': struct.pack('>ii', 0, 0), 'system-result': struct.pack('>ii', 0, 0),
                    'system-result2': struct.pack('>ii', 0, 0), 'absent-result': struct.pack('>ii', 5, 205),
                    'sysadd': b'', 'sysrun': None, 'sysremove': b'object is in use\n',
                    'sysreplace-system': b'', 'sysreplace-system2': b'', 'sysreplace': b'',
                    'sysabsent': b'object not found\n', 'sysaddnormal': b'', 'sysaddsystem3': b'', 'sysdone': b'done\n'}
    elif args.case in ('new-shell', 'new-cli', 'new-shell-prompt'):
        prompt_probe = args.case == 'new-shell-prompt'
        if prompt_probe:
            disk.file(directories['c'], 'C/PromptProbe', build_prompt_probe())
        disk.file(directories['s'], 'S/newstart',
                  b'FailAt 21\nMark >SYS:T/newalias\nGet Word >SYS:T/newvar\nPath >SYS:T/newpath\n'
                  b'CD >SYS:T/newdir\nStack >SYS:T/newstack\n' +
                  (b'C:PromptProbe\n' if prompt_probe else b'') +
                  b'Echo >SYS:T/newdone done\n' +
                  (b'EndShell\nEcho >SYS:T/afterendshell wrong\n' if args.case == 'new-cli' else b'EndCLI\n'))
        launcher = b'NewCLI' if args.case == 'new-cli' else b'NewShell'
        script = (b'FailAt 21\nPath RESET\nPath SYS:S ADD\nAlias Mark Echo inherited\n'
                  b'Set Word value\nStack 8192\nCD SYS:S\n' +
                  (b'Prompt special\n' if prompt_probe else b'') + launcher +
                  b' FROM S:newstart\n' + (b'Prompt\n' if prompt_probe else b'') +
                  b'Echo >SYS:T/parentdone done\nEcho SCRIPT DONE\n')
        expected = {'t/newalias': b'inherited\n', 't/newvar': b'value\n',
                    't/newpath': b'Current_directory\nCopperOS:S\nC:\n',
                    't/newdir': b'CopperOS:S\n', 't/newstack': b'Current stack size is 8192 bytes\n',
                    't/newdone': b'done\n', 't/parentdone': b'done\n'}
        if prompt_probe:
            expected['newprompt'] = b'special'
        if args.case == 'new-cli':
            expected['t/afterendshell'] = None
    elif args.case in ('nested-leading', 'nested-leading-control', 'broad-prefix'):
        disk.file(directories['c'], 'C/CapResult', build_cli_result_probe())
        disk.file(directories['s'], 'S/runchild', b'Echo >SYS:T/runnested nested\n')
        if args.case == 'broad-prefix':
            prelude = (b'Run Echo child >SYS:T/runout\n', b'Run C:Demo >SYS:T/rundemo\n',
                       b'Run Echo "hello*Nworld" >SYS:T/runescaped\n',
                       b'Run C:ErrProbe >SYS:T/runstdout *>SYS:T/runstderr\n',
                       b'Run Execute S:runchild\n')
            prefix = b''.join(prelude[:args.prelude_count])
        else:
            prefix = b'Run Execute S:runchild\n'
        second_input = b'<NIL: ' if args.case == 'nested-leading-control' else b''
        script = (b'FailAt 21\n' + prefix +
                  b'Run ' + second_input + b'>SYS:T/runprefix Echo child >SYS:T/runleading-child\n' +
                  b'CapResult\nEcho >SYS:T/runfinish done\nEcho SCRIPT DONE\n')
        expected = {'t/runleading-child': b'child\n',
                    't/runfinish': b'done\n', 'rdiagnostics.bin': struct.pack('>ii', 0, 0)}
        if args.case != 'broad-prefix' or args.prelude_count == 5:
            expected['t/runnested'] = b'nested\n'
    else:
        disk.file(directories['c'], 'C/ReadProbe', build_run_input_probe())
        disk.file(directories['s'], 'S/runinput', b'input-data\n')
        script = (b'FailAt 21\nRun >SYS:T/runprefix Echo child >SYS:T/runleading-child\n'
                  b'Run *>SYS:T/runerr C:ErrProbe >SYS:T/runstdout\n'
                  b'Run <SYS:S/runinput C:ReadProbe >SYS:T/runread\n'
                  b'Run >SYS:T/runboth *<> C:ErrProbe\n'
                  b'Echo >SYS:T/runfinish done\nEcho SCRIPT DONE\n')
        expected = {'t/runleading-child': b'child\n', 't/runerr': b'ERR\n',
                    't/runstdout': b'OUT\n', 't/runread': b'input-data\n',
                    't/runfinish': b'done\n'}
    disk.file(directories['s'], 'S/regress', script)
    free_blocks = fill_volume(disk, script) if args.case == 'execute-full' else None
    fixture = disk.finish()
    verify(fixture, disk.files)

    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    fixture_path = output / 'probe.adf'
    fixture_path.write_bytes(fixture)
    helper_bytes = None
    environment = os.environ.copy()
    if args.case == 'builtins':
        environment['COPPEROS_PROBE_AFTER_SCRIPT'] = builtin_fixture.followup(complete_corpus=not args.builtin_controls)
        if args.builtin_skip_label in ('missing', 'eof', 'lab', 'back'):
            environment['COPPEROS_PROBE_BEFORE_SCRIPT_WAIT'] = 'c:bicap6\necho >sys:bi-done done\necho script done'
    if args.runner_df1:
        helper = Disk()
        helper.header(helper.root, 0, 'ProbeHelper', 1)
        helper.checksum(helper.root)
        helper.file(helper.root, 'LongPath', runner_bytes)
        if runner_fixture_bytes is not None:
            helper.file(helper.root, 'fixture', runner_fixture_bytes)
        helper_bytes = helper.finish()
        verify(helper_bytes, helper.files, 'ProbeHelper')
        helper_path = output / 'helper.adf'
        helper_path.write_bytes(helper_bytes)
        environment['COPPEROS_PROBE_DF1'] = str(helper_path)
    else:
        environment.pop('COPPEROS_PROBE_DF1', None)
    rom = args.rom.resolve(strict=True)
    extension = args.extension_rom.resolve(strict=True)
    probe = args.probe_dll.resolve(strict=True)
    emulator = probe.with_name('CopperMod.Amiga.Lightweight.dll').resolve(strict=True)
    rom_bytes = rom.read_bytes()
    extension_bytes = extension.read_bytes()
    probe_bytes = probe.read_bytes()
    emulator_bytes = emulator.read_bytes()
    base = output / 'guest'
    mode = (('nonresident-' if args.nonresident_shell else '') + args.case) if args.case in ('recovery', 'script-recovery') else (
        'nonresident-script' if args.nonresident_shell else 'run-script')
    subprocess.run(['dotnet', str(probe), str(rom), str(fixture_path), str(extension), str(base), mode], check=True,
                   env=environment)
    result = (output / 'guest.result.adf').read_bytes()
    result_disk = Adf(result)
    result_tree = result_disk.walk()
    def read(path):
        entry = result_tree.get(path)
        return result_disk.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    observed = {name: read(name) for name in expected}
    observed['t/runprefix'] = read('t/runprefix')
    if args.case in ('resident-system', 'resident-system-short'):
        if args.case == 'resident-system':
            observed['syslist'] = read('syslist')
            observed['syslist2'] = read('syslist2')
        observed['syslist3'] = read('syslist3')
    if args.case == 'owned':
        observed['t/runboth'] = read('t/runboth')
    unexpected_star_paths = sorted(name for name in result_tree
                                   if name not in entries and name.split('/')[-1] == '*')
    execute_temps = sorted(name for name in result_tree if name.startswith('t/command-')) if args.case == 'execute-full' else []
    report = {name: (list(struct.unpack('>ii', value))
                     if (name.startswith('diag-result-') or name in builtin_fixture.RESULTS or name in ('resident-result', 'setenv-result', 'unsetenv-result', 'rdiagnostics.bin', 'remove-result', 'replace-result',
                                 'system-result', 'system-result2', 'absent-result', 'defer-reject',
                                 'ask-yes-result', 'ask-no-result', 'ask-retry-result', 'execute-full-result')) and value is not None and len(value) == 8
                     else struct.unpack('>i', value)[0]
                     if name in ('segcount.bin', 'defer-before', 'defer-after', 'defer-removed', 'defer-reject-state') and value is not None and len(value) == 4
                     else value.decode('latin1') if value is not None else None)
              for name, value in observed.items()}
    report['unexpectedStarPaths'] = unexpected_star_paths
    report['executeTemporaryFiles'] = execute_temps
    (output / 'observations.json').write_text(json.dumps(report, indent=2) + '\n')
    trace = json.loads((output / 'guest.trace.json').read_text())
    if (args.case == 'builtins' and not args.builtin_controls) or args.case in ('console-clear', 'console-reset'):
        reset_screen = trace.get('scriptScreen')
        if reset_screen is None or 'RESET SENTINEL' in reset_screen or 'SCRIPT DONE' not in reset_screen:
            raise RuntimeError('Console reset was not observed; use the current input probe and inspect guest.trace.json')
    notice = not requires_run_notice(args.case) or re.fullmatch(
        rb'\[CLI [1-9][0-9]*\]\n', observed['t/runprefix'] or b'')
    shared = args.case != 'owned' or re.fullmatch(
        rb'\[CLI [1-9][0-9]*\]\nERR\n', observed['t/runboth'] or b'')
    def selected_order(value):
        if value is None:
            return None
        return [line.strip() for line in value.splitlines()
                if line.startswith(b'Sysprobe ') or line.startswith(b'Alias ')]
    system_entries = args.case not in ('resident-system', 'resident-system-short') or (
        args.case == 'resident-system-short' and selected_order(observed['syslist3']) ==
        [b'Sysprobe          SYSTEM', b'Sysprobe             0', b'Sysprobe          SYSTEM', b'Alias             INTERNAL']) or (
        args.case == 'resident-system' and
        selected_order(observed['syslist']) == [b'Sysprobe             0', b'Sysprobe          SYSTEM',
                                               b'Sysprobe          SYSTEM', b'Alias             INTERNAL'] and
        selected_order(observed['syslist2']) == [b'Sysprobe          SYSTEM'] * 3 + [b'Alias             INTERNAL'] and
        selected_order(observed['syslist3']) == [b'Sysprobe          SYSTEM', b'Sysprobe             0'] +
                                                [b'Sysprobe          SYSTEM'] * 3 + [b'Alias             INTERNAL'])
    if (not args.observe_only and any(observed[name] != value for name, value in expected.items())) or not notice or not shared or not system_entries or unexpected_star_paths or execute_temps \
            or not trace['complete'] or trace['remaining'] != 0 or trace.get('childRemaining', 0) != 0 \
            or trace.get('backgroundRemaining', 0) != 0:
        raise RuntimeError('CopperStart Run mismatch; see observations.json and guest.trace.json')
    if source.read_bytes() != original:
        raise RuntimeError('Input proof image changed during qualification')
    if (rom.read_bytes() != rom_bytes or extension.read_bytes() != extension_bytes or
            probe.read_bytes() != probe_bytes or emulator.read_bytes() != emulator_bytes or
            (runner is not None and runner.read_bytes() != runner_bytes) or
            (runner_fixture is not None and runner_fixture.read_bytes() != runner_fixture_bytes) or
            (helper_bytes is not None and helper_path.read_bytes() != helper_bytes)):
        raise RuntimeError('Emulator or ROM inputs changed during qualification')
    if args.observe_only:
        print('Built-in corpus observed; output contracts have not been verified.')
        print(json.dumps(report))
        return
    receipt = {'kind': 'copperstart-shared-shell-run', 'runtimeQualified': False,
               'case': args.case, 'inputImageSha256': sha(original),
               'nonresidentShell': args.nonresident_shell,
               'residentErrors': args.resident_errors, 'globalErrors': args.global_errors,
               'residentArguments': args.resident_arguments,
               'builtinControls': args.builtin_controls, 'builtinSkipLabel': args.builtin_skip_label,
               'listFixtureResult': args.list_result,
               'preludeCount': args.prelude_count if args.case == 'broad-prefix' else None,
               'fixtureImageSha256': sha(fixture), 'resultImageSha256': sha(result),
               'romSha256': sha(rom_bytes), 'extensionRomSha256': sha(extension_bytes),
               'probeSha256': sha(probe_bytes), 'emulatorSha256': sha(emulator_bytes),
               'runnerProbeSha256': sha(runner_bytes) if runner_bytes is not None else None,
               'controlPaddingLines': args.control_padding_lines,
               'executeFailureStage': args.execute_failure_stage if args.case == 'execute-full' else None,
               'fixtureFreeBlocks': free_blocks,
               'runnerFixtureSha256': sha(runner_fixture_bytes) if runner_fixture_bytes is not None else None,
               'helperImageSha256': sha(helper_bytes) if helper_bytes is not None else None,
               'limits': 'Focused child-process and Shell exit test; complete Shell not qualified'}
    (output / 'qualification.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
