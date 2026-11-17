"""Exercise the shared proof Shell on Kickstart 3.1 with guest input.device keys.

The input image is never modified. A disposable HDF carries the same Shell and
BootShell, plus an injector in a replacement Startup-Sequence. This checks the
real interactive child, but not the unmodified image's startup or all Shell
features.
"""
import argparse
import hashlib
import json
import re
import struct
import subprocess
import sys
from pathlib import Path

from build_system_image import Disk, verify
from execute_disk_full_fixture import scripts as disk_full_scripts, fill_volume, build_execute_probe
from make_kickstart_keyboard_probe import parent, child, exit_observer
from make_kickstart_shell_probe import (build_nonresident_boot_probe, build_prompt_probe, build_system_probe, build_system_failure_probe,
                                       build_system_async_probe, build_system_input_probe, build_system_lifetime_probe, build_run_lifetime_probe,
                                       build_temp_assign_probe, build_cli_result_probe,
                                       build_resident_order_probe, build_run_notice_probe, build_run_input_probe,
                                       build_run_delayed_probe, build_nonresident_memory_probe,
                                       build_nonresident_swap_probe, build_segment_layout_probe,
                                       build_control_script, build_many_path_script, build_path_signal_probe, build_task_userdata_probe,
                                       build_append_file_copy_probe)

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf
sys.path.insert(0, str(Path(__file__).with_name('tests')))
from make_error_probe import build as build_error_probe


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--powershell', default='pwsh')
    parser.add_argument('--duration-seconds', type=int, default=60,
                        help='WinUAE observation window in seconds (1 to 300)')
    parser.add_argument('--line-delay-ticks', type=int, default=0,
                        help='Pause after each injected Return (0 to 500 DOS ticks)')
    parser.add_argument('--run-diagnostics', action='store_true',
                        help='Record the return code and IoErr after each Run in the broad probe')
    parser.add_argument('--diagnostic-log', action='store_true',
                        help='Run a portable WinUAE copy with diagnostic logging enabled')
    parser.add_argument('--nonresident-shell', action='store_true',
                        help='Start the proof Shell without registering a permanent segment')
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument('--execute-disk-full', choices=('source', 'tail'),
                      help='Fail Execute source or caller-tail copying on a near-full FFS volume')
    mode.add_argument('--nonresident-retire', action='store_true',
                      help='Check a copied background worker after its parent Shell exits')
    mode.add_argument('--nonresident-memory', action='store_true',
                      help='Measure memory after sequential copied workers exit')
    mode.add_argument('--nonresident-mismatch', action='store_true',
                      help='Reject an installed Shell with a different build fingerprint')
    mode.add_argument('--nonresident-missing', action='store_true',
                      help='Handle a missing installed Shell without starting a worker')
    mode.add_argument('--segment-layout', action='store_true',
                      help='Record the live Process segment array')
    mode.add_argument('--system', action='store_true',
                      help='Check packet-started synchronous and asynchronous System() children')
    mode.add_argument('--control-flow', action='store_true',
                      help='Check script arguments, conditions, labels, variables, aliases and redirection')
    parser.add_argument('--control-padding-lines', type=int, default=0,
                        help='Insert comment lines into the control-flow script to cross buffered-copy boundaries')
    mode.add_argument('--runner-hunk', type=Path,
                      help='Run the native Shell-owned runner and public file-I/O probe')
    mode.add_argument('--append', action='store_true',
                      help='Check consecutive interactive output and append redirections')
    mode.add_argument('--append-trace', action='store_true',
                      help='Capture the task user-data marker from an instrumented diagnostic Shell')
    mode.add_argument('--lifetime', action='store_true',
                      help='Measure public memory after repeated asynchronous System() children')
    mode.add_argument('--failat', action='store_true',
                      help='Check FailAt across an ordinary command and a nested Execute script')
    mode.add_argument('--path', action='store_true',
                      help='Check CLI path add, remove, reset, display, and the implicit C: search')
    mode.add_argument('--path-many', action='store_true',
                      help='Check more than 64 Path arguments in one command')
    mode.add_argument('--path-isolation', action='store_true',
                      help='Check inherited CLI path ownership across a background child')
    mode.add_argument('--cd', action='store_true',
                      help='Check current-directory changes, parent, and failed lookup')
    mode.add_argument('--substitution', action='store_true',
                      help='Check command substitution in a script without a T: assign')
    mode.add_argument('--substitution-assigned', action='store_true',
                      help='Check command substitution with T: assigned to SYS:T')
    mode.add_argument('--resident', action='store_true',
                      help='Check resident listing, single filenames and internal toggles')
    mode.add_argument('--resident-extensions', action='store_true',
                      help='Check unsupported resident extensions return safely on V40 DOS')
    mode.add_argument('--new-shell', action='store_true',
                      help='Check an independent NewShell startup and inherited CLI state')
    mode.add_argument('--new-cli', action='store_true',
                      help='Check an independent NewCLI startup and inherited CLI state')
    mode.add_argument('--new-shell-default', action='store_true',
                      help='Check NewShell with the default startup file')
    mode.add_argument('--new-shell-window', action='store_true',
                      help='Check NewShell with an explicit console window')
    mode.add_argument('--new-shell-prompt', action='store_true',
                      help='Check that NewShell inherits a changed prompt')
    mode.add_argument('--new-shell-missing', action='store_true',
                      help='Check interactive startup when FROM is missing')
    mode.add_argument('--new-shell-lifetime', action='store_true',
                      help='Measure repeated NewShell startup and teardown')
    mode.add_argument('--child-launch', action='store_true',
                      help='Check public child process path and output ownership')
    mode.add_argument('--run', action='store_true',
                      help='Check background commands, inherited path, and redirection')
    mode.add_argument('--run-leading', action='store_true',
                      help='Check redirections immediately following Run against V40 behavior')
    mode.add_argument('--run-leading-split', action='store_true',
                      help='Check launcher and child output redirections separately')
    mode.add_argument('--run-lifetime', action='store_true',
                      help='Measure public memory after repeated Run workers exit')
    mode.add_argument('--run-owned-lifetime', action='store_true',
                      help='Measure workers launched with an owned leading output stream')
    mode.add_argument('--run-console-lifetime', action='store_true',
                      help='Measure repeated workers launched with console input')
    mode.add_argument('--run-nested-lifetime', action='store_true',
                      help='Measure nested workers after a leading output redirection')
    mode.add_argument('--run-nested-owned', action='store_true',
                      help='Check a nested background worker after its launcher redirects output')
    mode.add_argument('--run-nested-delayed', action='store_true',
                      help='Delay the nested command to exercise its launcher retiring first')
    mode.add_argument('--run-nested-control', action='store_true',
                      help='Run the delayed stream check while its owning worker remains alive')
    mode.add_argument('--run-multi-owned', action='store_true',
                      help='Check multiple delayed descendants sharing an owned stream')
    mode.add_argument('--run-input-race', action='store_true',
                      help='Type into the Shell while a background command borrows its input')
    mode.add_argument('--run-input-control', action='store_true',
                      help='Repeat the input race with an independent NIL: child input')
    parser.add_argument('--child-hunk', type=Path, help='Native child-launch probe HUNK')
    parser.add_argument('--runner-fixture', type=Path,
                        help='Place a foreground-command fixture at DH0:fixture for --runner-hunk')
    parser.add_argument('--child-keep-shell', action='store_true',
                        help='Leave the Shell running after the child probe to isolate exit behavior')
    parser.add_argument('--run-keep-shell', action='store_true',
                        help='Check a follow-up command instead of ending the Shell after a Run probe')
    parser.add_argument('--split-case', choices=('all', 'none', 'empty', 'child', 'error', 'input', 'shared'),
                        default='all', help='Isolate one leading-redirection case in the split probe')
    parser.add_argument('--isolate-split-input', action='store_true',
                        help='Give split-probe workers independent NIL: input where not already specified')
    parser.add_argument('--input-poll-followup', action='store_true',
                        help='Hold the input-race command until the follow-up appears')
    parser.add_argument('--exit-diagnostics', action='store_true',
                        help='Record injector progress and whether the Shell remains after EndCLI')
    parser.add_argument('--suppress-exit-state', action='store_true',
                        help='Omit extra injector exit diagnostics for a control run')
    parser.add_argument('--independent-exit-observer', action='store_true',
                        help='Observe EndCLI from a separate guest process')
    parser.add_argument('--observer-control', action='store_true',
                        help='Leave the baseline Shell running for the independent observer')
    args = parser.parse_args()
    if args.run_diagnostics and not args.run:
        parser.error('--run-diagnostics requires --run')
    if (args.nonresident_retire or args.nonresident_memory or args.nonresident_mismatch or args.nonresident_missing) and not args.nonresident_shell:
        parser.error('Nonresident worker probes require --nonresident-shell')
    new_shell = (args.new_shell or args.new_cli or args.new_shell_default or
                 args.new_shell_window or args.new_shell_prompt)
    if not 1 <= args.duration_seconds <= 300:
        parser.error('--duration-seconds must be between 1 and 300')
    if not 0 <= args.control_padding_lines <= 1024 or (args.control_padding_lines and not args.control_flow):
        parser.error('--control-padding-lines requires --control-flow and a value from 0 to 1024')
    if not 0 <= args.line_delay_ticks <= 500:
        parser.error('--line-delay-ticks must be between 0 and 500')
    if args.child_launch != (args.child_hunk is not None):
        parser.error('--child-launch and --child-hunk must be supplied together')
    if args.runner_fixture is not None and args.runner_hunk is None:
        parser.error('--runner-fixture requires --runner-hunk')
    if args.child_keep_shell and not args.child_launch:
        parser.error('--child-keep-shell requires --child-launch')
    if args.run_keep_shell and not (args.run or args.run_leading_split):
        parser.error('--run-keep-shell requires --run or --run-leading-split')
    if args.split_case != 'all' and not args.run_leading_split:
        parser.error('--split-case requires --run-leading-split')
    if args.isolate_split_input and not args.run_leading_split:
        parser.error('--isolate-split-input requires --run-leading-split')
    if args.observer_control and (not args.independent_exit_observer or any((
            args.system, args.control_flow, args.execute_disk_full, args.runner_hunk, args.append, args.append_trace, args.lifetime, args.failat, args.path, args.path_many, args.path_isolation, args.cd, args.substitution,
            args.substitution_assigned, args.resident, args.resident_extensions, new_shell, args.new_shell_missing, args.new_shell_lifetime, args.child_launch, args.run,
            args.run_leading, args.run_leading_split, args.run_lifetime, args.run_owned_lifetime, args.run_console_lifetime,
            args.run_nested_lifetime, args.run_nested_owned, args.run_nested_delayed,
            args.run_nested_control, args.run_multi_owned, args.run_input_race,
            args.run_input_control, args.nonresident_retire, args.nonresident_memory, args.segment_layout,
            args.nonresident_mismatch, args.nonresident_missing, args.exit_diagnostics))):
        parser.error('--observer-control requires an independent observer and the baseline probe')
    child_hunk = args.child_hunk.resolve(strict=True).read_bytes() if args.child_launch else None
    if child_hunk is not None and not child_hunk.startswith(b'\x00\x00\x03\xf3'):
        parser.error('--child-hunk must be a loadable HUNK')
    runner_hunk = args.runner_hunk.resolve(strict=True).read_bytes() if args.runner_hunk else None
    runner_fixture = args.runner_fixture.resolve(strict=True).read_bytes() if args.runner_fixture else None
    if runner_hunk is not None and not runner_hunk.startswith(b'\x00\x00\x03\xf3'):
        parser.error('--runner-hunk must be a loadable HUNK')
    if runner_fixture is not None and not runner_fixture.startswith(b'\x00\x00\x03\xf3'):
        parser.error('--runner-fixture must be a loadable HUNK')
    substitution = args.substitution or args.substitution_assigned
    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    tree = Adf(original)
    entries = tree.walk()
    generated = {'c/keystart', 'c/keychild', 'input-sent.txt', 'shell-present.txt', 'second-boot.txt'}
    owned_lifetime = args.run_owned_lifetime or args.run_nested_lifetime
    run_lifetime = args.run_lifetime or args.run_console_lifetime or owned_lifetime or args.new_shell_lifetime
    run_leading = args.run_leading or args.run_leading_split
    run_nested = args.run_nested_owned or args.run_nested_delayed or args.run_nested_control or args.run_multi_owned
    run_input_probe = args.run_input_race or args.run_input_control
    if args.input_poll_followup and not run_input_probe:
        parser.error('--input-poll-followup requires an input race or control probe')
    nested_keep_shell = args.run_nested_delayed or args.run_nested_control or args.run_multi_owned
    if args.segment_layout:
        generated.update({'c/segprobe', 'segments.bin', 'shell-ended.txt'})
    elif args.nonresident_missing:
        generated.update({'c/swap', 'c/restore', 'c/capresult', 'l/savedshell',
                          's/mismatchprobe', 'shell-ended.txt', 'unexpected', 'rdiagnostics.bin', 'runfinish'})
    elif args.nonresident_mismatch:
        generated.update({'c/swap', 'c/restore', 'c/capresult', 'l/altshell', 'l/savedshell',
                          's/mismatchprobe', 'shell-ended.txt', 'unexpected', 'rdiagnostics.bin', 'runfinish'})
    elif args.nonresident_memory:
        generated.update({'c/mem1', 'c/mem2', 'c/mem3', 's/memoryprobe', 'shell-ended.txt',
                          'memory1', 'memory2', 'memory3', 'runfinish'})
    elif args.nonresident_retire:
        generated.update({'c/delayprobe', 's/retireprobe', 'shell-ended.txt',
                          'parent-state', 'delayed', 'runfinish'})
    elif run_input_probe:
        generated.update({'c/delayprobe', 'delayresult', 'afterrun', 'delay-started', 'overlap', 'input-identity'})
    elif run_lifetime:
        generated.update({'c/runlife', 'runlife.bin', 'runshared'})
        if not args.new_shell_lifetime:
            generated.add('finish')
        if args.new_shell_lifetime:
            generated.add('s/lifestart')
        if owned_lifetime:
            generated.add('runowned')
    elif run_nested:
        generated.update({'s/runnestedowned', 'shell-ended.txt', 'nestednotice', 'nestedout', 'runfinish'})
        if args.run_nested_delayed or args.run_nested_control or args.run_multi_owned:
            generated.add('c/delayprobe')
        if args.run_multi_owned:
            generated.update({'s/multichild', 'nestedout2'})
    elif run_leading:
        generated.update({'s/runleading', 'shell-ended.txt', 'prefix', 'prefix2', 'childout', 'runfinish'})
        if args.run_leading_split:
            generated.update({'c/errprobe', 'c/readprobe', 's/runinput', 'runerr', 'runstdout', 'runread', 'runboth'})
    elif args.run:
        generated.update({'c/capresult', 'c/errprobe', 'c/runnotice', 's/runprobe', 's/runchild', 'shell-ended.txt',
                          'runout', 'rundemo', 'runpath', 'runquoted', 'runescaped',
                          'runstdout', 'runstderr', 'runnested', 'runnotice', 'runalias', 'runvar',
                          'rdiagnostics.bin', 'runfinish'})
        if args.run_diagnostics:
            generated.update({f'c/caprun{i}' for i in range(1, 11)})
            generated.update({f'run-diag-{i}' for i in range(1, 11)})
    elif args.child_launch:
        generated.update({'c/childprobe', 't/child-ok', 't/child-fail', 't/child-output',
                          'shell-ended.txt', 'childpath', 'childafter'})
        if args.child_keep_shell:
            generated.add('childextra')
    elif args.lifetime:
        generated.update({'c/shelllife', 'asynclife.bin', 'finish'})
    elif args.system:
        generated.update({'c/shellsystem', 'c/shellfailure', 'c/shellasync', 'c/shellnull', 'c/shellinput',
                          'c/readprobe', 's/sysinput', 's/sysnull', 'sysread', 'sysinputresult',
                          'systemresult.txt', 'systemchild.txt', 'systemfailure.txt',
                          'asyncresult.txt', 'asyncchild.txt', 'nullresult.txt', 'nullchild.txt', 'finish'})
    elif args.execute_disk_full:
        generated.update({'s/regress', 's/execfull', 'c/execfull', 'c/capfull', 'execute-fill',
                          'shell-ended.txt', 'execute-full-result', 'execute-full-done', 'execute-full-signal'})
    elif args.control_flow:
        generated.update({'s/controlprobe', 'shell-ended.txt', 'ctl-arguments', 'ctl-true',
                          'ctl-else', 'ctl-wrong', 'ctl-skipped', 'ctl-variable',
                          'ctl-alias', 'ctl-redirection', 'ctl-childdone', 'ctlparentdone'})
    elif args.runner_hunk:
        generated.update({'c/runnerprobe', 'shell-ended.txt', 'runner-ok', 'runner-fail', 'io-test',
                          'append-one', 'append-zero', 'append-test'})
        if args.runner_fixture is not None:
            generated.add('fixture')
    elif args.append or args.append_trace:
        generated.update({'shell-ended.txt', 'appendfile', 'appenddone'})
        if args.append_trace:
            generated.update({'c/traceprobe', 'c/copyprobe', 'appendtrace', 'firstcopy'})
    elif args.failat:
        generated.update({'s/failatprobe', 'shell-ended.txt', 'before', 'nestedvalue', 'after'})
    elif args.cd:
        generated.update({'c/capresult', 's/cdprobe', 'shell-ended.txt',
                          'cdfirst', 'cdparent', 'cdsecond', 'cdafter', 'rdiagnostics.bin'})
    elif args.path_isolation:
        generated.update({'c/sigsetup', 'c/sigwait', 'c/sigsend', 's/pathisolation', 's/pathchild', 'shell-ended.txt',
                          'path-parent-before', 'path-parent-after', 'path-child-before',
                          'path-child-after', 'path-child-done', 'path-setup-ok', 'path-wait-ok'})
    elif args.path_many:
        generated.update({'s/manypath', 'shell-ended.txt', 'pathmany', 'pathmanydone'})
    elif args.path:
        generated.update({'s/pathprobe', 'shell-ended.txt', 'pathinitial', 'pathadded', 'pathhit',
                          'pathremoved', 'pathmiss', 'pathcleared', 'pathdemo'})
    elif substitution:
        generated.update({'c/tassigncheck', 's/subprobe', 'shell-ended.txt', 'subinternal', 'subafter', 'tassign'})
    elif args.resident_extensions:
        generated.update({'c/capdefer', 'c/capalias', 's/resextensions', 'shell-ended.txt',
                          'defer-result', 'alias-result', 'extensions-done'})
    elif args.resident:
        generated.update({'c/capresult', 'c/rorderprobe', 's/resprobe', 'shell-ended.txt', 'rmissing', 'rnormal', 'rsystem',
                          'rmissing2', 'rreplace', 'rdisabled', 'rdiagnostics.bin', 'rrestored', 'rorder'})
    elif new_shell:
        generated.update({'s/shell-startup' if args.new_shell_default else 's/newstart',
                          'shell-ended.txt', 'newalias', 'newvar',
                          'newpath', 'newdir', 'newstack', 'newdone', 'parentdone'})
        if args.new_cli:
            generated.add('afterendshell')
        if args.new_shell_prompt:
            generated.update({'c/promptprobe', 'newprompt'})
    elif args.new_shell_missing:
        generated.update({'shell-ended.txt', 'childdone', 'parentdone'})
    else:
        generated.update({'shell-ended.txt', 'bootresult', 'aliasresult', 'pathresult', 'demoresult'})
    if args.run_keep_shell:
        generated.add('afterrun')
    if args.observer_control:
        generated.add('keepfinish')
    if args.exit_diagnostics and (args.system or args.lifetime or run_lifetime or args.child_keep_shell or
                                  nested_keep_shell or args.run_keep_shell or run_input_probe):
        parser.error('--exit-diagnostics requires a probe that ends the Shell')
    if args.independent_exit_observer and (args.system or args.lifetime or run_lifetime or
                                           args.child_keep_shell or nested_keep_shell):
        parser.error('--independent-exit-observer is not supported for this probe')
    if args.independent_exit_observer:
        generated.update({'c/observestart', 'c/observechild', 'observer-started.txt',
                          'observer-tick.txt', 'observer-result.txt'})
    if args.diagnostic_log:
        generated.add('second-alert.bin')
    record_exit_state = not args.suppress_exit_state and (
        (args.run_leading_split and not args.run_keep_shell) or args.exit_diagnostics)
    if record_exit_state:
        generated.update({'shell-still.txt', 'before-wait.txt', 'after-wait.txt'})
    if generated.intersection(entries):
        raise ValueError('Input image already contains a probe path')
    disk = Disk(16384)
    directories = {}
    for key, entry in sorted(entries.items(), key=lambda item: (item[0].count('/'), item[0])):
        path = entry['path']
        parts = path.split('/')
        owner = disk.root if len(parts) == 1 else directories['/'.join(parts[:-1]).lower()]
        if entry['secondary_type'] == 2:
            directories[key] = disk.directory(owner, entry['name'])
        elif entry['secondary_type'] == -3:
            startup = (b'C:ObserveStart\n' if args.independent_exit_observer else b'') + b'C:KeyStart\nC:BootShell\n'
            content = (startup if key == 's/startup-sequence' else
                       build_nonresident_boot_probe() if key == 'c/bootshell' and args.nonresident_shell else
                       tree.read_file(entry['block']))
            disk.file(owner, path, content)
        else:
            raise ValueError('Unexpected input entry: ' + path)
    if 'c' not in directories or 's/startup-sequence' not in entries:
        raise ValueError('Input image lacks the proof startup or command directory')
    commands = ('segprobe\nendcli\n' if args.segment_layout else
                'execute s:mismatchprobe\nendcli\n' if args.nonresident_mismatch or args.nonresident_missing else
                'execute s:memoryprobe\nendcli\n' if args.nonresident_memory else
                'execute s:retireprobe\nendcli\n' if args.nonresident_retire else
                ('run <nil: c:delayprobe >delayresult\n' if args.run_input_control else
                 'run c:delayprobe >delayresult\n') + 'echo >afterrun done\n' if run_input_probe else
                ('runlife\n' if args.new_shell_lifetime else 'runlife\necho >finish done\n') if run_lifetime else
                ('execute s:runnestedowned\n' if nested_keep_shell else
                 'execute s:runnestedowned\nendcli\n') if run_nested else
                ('execute s:runleading\necho >afterrun done\n' if args.run_keep_shell else
                 'execute s:runleading\nendcli\n') if run_leading else
                ('execute s:runprobe\necho >afterrun done\n' if args.run_keep_shell else
                 'execute s:runprobe\nendcli\n') if args.run else
                'path reset\npath sys:s add\nchildprobe\npath >childpath\n'
                'demo >childafter\n' + ('echo >childextra done\n' if args.child_keep_shell else 'endcli\n') if args.child_launch else
                'shelllife\necho >finish done\nendcli\n' if args.lifetime else
                'shellsystem\nshellfailure\nshellinput\nshellasync\nshellnull\necho >finish done\nendcli\n' if args.system else
                'runnerprobe\nendcli\n' if args.runner_hunk else
                'echo alpha >appendfile\ncopyprobe\necho beta >>appendfile\ntraceprobe\necho >appenddone done\nendcli\n' if args.append_trace else
                'echo alpha >appendfile\necho beta >>appendfile\necho >appenddone done\nendcli\n' if args.append else
                'execute s:regress\nendcli\n' if args.execute_disk_full else
                'failat 21\nexecute s:controlprobe world\necho >ctlparentdone done\nendcli\n' if args.control_flow else
                'failat 21\nfailat >before\nexecute s:failatprobe\nfailat >after\nendcli\n' if args.failat else
                'execute s:cdprobe\nendcli\n' if args.cd else
                'execute s:pathisolation\nendcli\n' if args.path_isolation else
                'execute s:manypath\nendcli\n' if args.path_many else
                'path reset\npath sys:c add\npath >pathinitial\npath sys:s add\npath >pathadded\n'
                'pathprobe >pathhit\npath sys:s remove\npath >pathremoved\npathprobe >pathmiss\n'
                'path reset\npath >pathcleared\ndemo >pathdemo\nendcli\n' if args.path else
                'tassigncheck\nexecute s:subprobe\nendcli\n' if substitution else
                'execute s:resextensions\nendcli\n' if args.resident_extensions else
                'execute s:resprobe\nendcli\n' if args.resident else
                'newshell from s:missing\necho >sys:childdone child\nendcli\n'
                'echo >sys:parentdone parent\nendcli\n' if args.new_shell_missing else
                'path reset\npath sys:s add\nalias mark echo inherited\nset word value\n'
                'stack 8192\ncd sys:s\n' + ('prompt special\n' if args.new_shell_prompt else '') +
                ('newcli' if args.new_cli else 'newshell') +
                (' con:0/0/640/200/test/close' if args.new_shell_window else '') +
                ('' if args.new_shell_default else ' from s:newstart') +
                '\necho >sys:parentdone done\nendcli\n' if new_shell else
                'echo >bootresult hello\nalias proof echo shared\n'
                'proof >aliasresult\npath >pathresult\ndemo >demoresult\n' +
                ('echo >keepfinish done\n' if args.observer_control else 'endcli\n'))
    disk.file(directories['c'], 'C/KeyStart', parent())
    if args.independent_exit_observer:
        disk.file(directories['c'], 'C/ObserveStart', parent('C:ObserveChild'))
        disk.file(directories['c'], 'C/ObserveChild',
                  exit_observer(1800 if new_shell or args.new_shell_missing else 950))
    disk.file(directories['c'], 'C/KeyChild', child(commands,
              check_shell_exit=not (args.system or args.lifetime or run_lifetime or args.child_keep_shell or nested_keep_shell or args.run_keep_shell or args.observer_control or run_input_probe),
              record_shell_alive=record_exit_state,
              line_delay_ticks=args.line_delay_ticks or (250 if args.new_shell_missing else 100 if new_shell else 0),
              wait_after_first_line=run_input_probe, capture_second_alert=args.diagnostic_log,
              wait_for_path='SYS:runner-ok' if args.runner_hunk else
                  'SYS:execute-full-signal' if args.execute_disk_full else
                  'SYS:ctl-childdone' if args.control_flow else
                  'SYS:runfinish' if args.run else
                  'SYS:pathmanydone' if args.path_many else
                  'SYS:path-child-done' if args.path_isolation else None,
              wait_after_line=2 if args.control_flow else 1))
    if new_shell:
        if args.new_shell_prompt:
            disk.file(directories['c'], 'C/PromptProbe', build_prompt_probe())
        disk.file(directories['s'], 'S/Shell-Startup' if args.new_shell_default else 'S/newstart',
                  b'failat 21\nmark >sys:newalias\nget word >sys:newvar\npath >sys:newpath\n'
                  b'cd >sys:newdir\nstack >sys:newstack\n' +
                  (b'C:PromptProbe\n' if args.new_shell_prompt else b'') +
                  b'echo >sys:newdone done\n' +
                  (b'endshell\necho >sys:afterendshell wrong\n' if args.new_cli else b'endcli\n'))
    if args.segment_layout:
        disk.file(directories['c'], 'C/SegProbe', build_segment_layout_probe())
    if args.nonresident_mismatch or args.nonresident_missing:
        original_shell = bytearray(tree.read_file(entries['l/shell-seg']['block']))
        if original_shell[32:44] != bytes.fromhex('6000002a4353484600000001'):
            raise ValueError('Input Shell lacks the expected fingerprint header')
        if args.nonresident_mismatch:
            original_shell[44] ^= 1
            disk.file(directories['l'], 'L/AltShell', bytes(original_shell))
        disk.file(directories['c'], 'C/Swap', build_nonresident_swap_probe(missing=args.nonresident_missing))
        disk.file(directories['c'], 'C/Restore', build_nonresident_swap_probe(True, missing=args.nonresident_missing))
        disk.file(directories['c'], 'C/CapResult', build_cli_result_probe())
        disk.file(directories['s'], 'S/mismatchprobe',
                  b'FailAt 21\nC:Swap\nRun Echo >SYS:unexpected child\nC:CapResult\n'
                  b'C:Restore\nEcho >SYS:runfinish done\n')
    if args.nonresident_memory:
        for number in range(1, 4):
            disk.file(directories['c'], f'C/Mem{number}',
                      build_nonresident_memory_probe(f'SYS:memory{number}'))
        disk.file(directories['s'], 'S/memoryprobe',
                  b'FailAt 21\nRun <NIL: Echo >NIL: warmup\nC:Mem1\n'
                  b'Run <NIL: Echo >NIL: one\nC:Mem2\n'
                  b'Run <NIL: Echo >NIL: two\nC:Mem3\nEcho >SYS:runfinish done\n')
    if args.nonresident_retire:
        disk.file(directories['c'], 'C/DelayProbe', build_run_delayed_probe(500, verify_parent_retired=True))
        disk.file(directories['s'], 'S/retireprobe',
                  b'FailAt 21\nRun C:DelayProbe >SYS:delayed\nEcho >SYS:runfinish done\n')
    if run_input_probe:
        disk.file(directories['c'], 'C/DelayProbe', build_run_delayed_probe(
            500, observe_followup=True, poll_followup=args.input_poll_followup))
    if run_lifetime:
        disk.file(directories['c'], 'C/RunLife',
                  build_run_lifetime_probe(owned_lifetime, args.run_nested_lifetime,
                                           console_input=args.run_console_lifetime,
                                           new_shell=args.new_shell_lifetime))
        if args.new_shell_lifetime:
            disk.file(directories['s'], 'S/lifestart', b'EndCLI\n')
    if run_nested:
        nested_command = b'C:DelayProbe' if args.run_nested_delayed or args.run_nested_control else b'Echo nested'
        nested_prefix = b'' if args.run_nested_control else b'Run '
        if args.run_multi_owned:
            nested_prefix, nested_command = b'', b'Execute S:multichild'
        disk.file(directories['s'], 'S/runnestedowned',
                  b'FailAt 21\nRun >SYS:nestednotice ' + nested_prefix + nested_command +
                  (b'\n' if args.run_multi_owned else b' >SYS:nestedout\n') + b'Echo >SYS:runfinish done\n')
        if args.run_multi_owned:
            disk.file(directories['s'], 'S/multichild',
                      b'Run <NIL: C:DelayProbe >SYS:nestedout\nRun <NIL: C:DelayProbe >SYS:nestedout2\n')
        if args.run_nested_delayed or args.run_nested_control or args.run_multi_owned:
            disk.file(directories['c'], 'C/DelayProbe', build_run_delayed_probe(500 if args.run_multi_owned else 150))
    if run_leading:
        def enabled(case):
            return args.split_case in ('all', case)
        input_prefix = b'<NIL: ' if args.isolate_split_input else b''
        first_run = (b'Run ' + input_prefix + b'>SYS:prefix\n' if args.run_leading_split else
                     b'Run >SYS:prefix Echo child\n') if enabled('empty') else b''
        second_run = b'Run ' + input_prefix + b'>SYS:prefix2 Echo nested >SYS:childout\n' if enabled('child') else b''
        additional = ((b'Run ' + input_prefix + b'*>SYS:runerr C:ErrProbe >SYS:runstdout\n' if enabled('error') else b'') +
                      (b'Run <SYS:S/runinput C:ReadProbe >SYS:runread\n' if enabled('input') else b'') +
                      (b'Run ' + input_prefix + b'>SYS:runboth *<> C:ErrProbe\n' if enabled('shared') else b'')) if args.run_leading_split else b''
        disk.file(directories['s'], 'S/runleading',
                  b'FailAt 21\n' + first_run +
                  second_run + additional + b'Echo >SYS:runfinish done\n')
        if args.run_leading_split:
            disk.file(directories['c'], 'C/ErrProbe', build_error_probe())
            disk.file(directories['c'], 'C/ReadProbe', build_run_input_probe())
            disk.file(directories['s'], 'S/runinput', b'input-data\n')
    if args.run:
        disk.file(directories['c'], 'C/CapResult', build_cli_result_probe())
        disk.file(directories['c'], 'C/ErrProbe', build_error_probe())
        disk.file(directories['c'], 'C/RunNotice', build_run_notice_probe())
        run_script = (b'FailAt 21\nRun Echo child >runout\nRun C:Demo >rundemo\n'
                  b'Run Echo "FIRST" >runquoted\nRun Echo "hello*Nworld" >runescaped\n'
                  b'Run C:ErrProbe >runstdout *>runstderr\nRun Execute S:runchild\n'
                  b'Alias RunAlias Echo inherited\nRun RunAlias >runalias\n'
                  b'Set RunVariable value\nRun Get RunVariable >runvar\n'
                  b'Path RESET\nPath SYS:S ADD\nRun Path >runpath\nPath RESET\n'
                  b'Run\nCapResult\nRunNotice\nEcho >runfinish done\n')
        if args.run_diagnostics:
            lines = []
            number = 0
            for line in run_script.splitlines(keepends=True):
                lines.append(line)
                if line.startswith(b'Run ') or line == b'Run\n':
                    number += 1
                    disk.file(directories['c'], f'C/CapRun{number}',
                              build_cli_result_probe(f'SYS:run-diag-{number}'))
                    lines.append(f'C:CapRun{number}\n'.encode('ascii'))
            if number != 10:
                raise ValueError('Run diagnostic fixture has an unexpected command count')
            run_script = b''.join(lines)
        disk.file(directories['s'], 'S/runprobe', run_script)
        disk.file(directories['s'], 'S/runchild', b'Echo >runnested nested\n')
    if args.cd:
        disk.file(directories['c'], 'C/CapResult', build_cli_result_probe())
        disk.file(directories['s'], 'S/cdprobe',
                  b'FailAt 21\nCD SYS:S\nCD >SYS:cdfirst\nCD /\nCD >SYS:cdparent\n'
                  b'CD SYS:C\nCD >SYS:cdsecond\nCD SYS:NoSuchDirectory\nCapResult\nCD >SYS:cdafter\n')
    if args.child_launch:
        disk.file(directories['c'], 'C/ChildProbe', child_hunk)
    if args.lifetime:
        disk.file(directories['c'], 'C/ShellLife', build_system_lifetime_probe())
    if args.system:
        disk.file(directories['c'], 'C/ShellSystem', build_system_probe())
        disk.file(directories['c'], 'C/ShellFailure', build_system_failure_probe())
        disk.file(directories['c'], 'C/ShellAsync', build_system_async_probe())
        disk.file(directories['c'], 'C/ShellNull', build_system_async_probe(null_command=True))
        disk.file(directories['c'], 'C/ShellInput', build_system_input_probe())
        disk.file(directories['c'], 'C/ReadProbe', build_run_input_probe())
        disk.file(directories['s'], 'S/sysinput', b'INPUT DATA\n')
        disk.file(directories['s'], 'S/sysnull', b'Echo >SYS:nullchild.txt null\n')
    if args.failat:
        disk.file(directories['s'], 'S/failatprobe', b'FailAt 7\nFailAt >nestedvalue\n')
    if args.control_flow:
        disk.file(directories['s'], 'S/controlprobe', build_control_script(args.control_padding_lines))
    if args.execute_disk_full:
        full_source, full_caller = disk_full_scripts(args.execute_disk_full)
        disk.file(directories['s'], 'S/execfull', full_source)
        disk.file(directories['s'], 'S/regress', full_caller)
        disk.file(directories['c'], 'C/CapFull', build_cli_result_probe('SYS:execute-full-result'))
        disk.file(directories['c'], 'C/ExecFull', build_execute_probe())
        disk.file(disk.root, 'execute-full-result', bytes([0xa5]) * 8)
        disk.file(disk.root, 'execute-full-done', b'PENDING!\n')
    if args.runner_hunk:
        disk.file(directories['c'], 'C/RunnerProbe', runner_hunk)
        if runner_fixture is not None:
            disk.file(disk.root, 'fixture', runner_fixture)
    if args.append_trace:
        disk.file(directories['c'], 'C/TraceProbe', build_task_userdata_probe())
        disk.file(directories['c'], 'C/CopyProbe', build_append_file_copy_probe())
    if substitution:
        disk.file(directories['c'], 'C/TAssignCheck', build_temp_assign_probe(args.substitution_assigned))
        disk.file(directories['s'], 'S/subprobe',
                  b'FailAt 21\nEcho >subinternal before `Echo inner` after\n'
                  b'Echo >subafter done\n')
    if args.resident:
        disk.file(directories['c'], 'C/CapResult', build_cli_result_probe())
        disk.file(directories['c'], 'C/RorderProbe', build_resident_order_probe())
        disk.file(directories['s'], 'S/resprobe',
                  b'FailAt 21\nResident Missing >rmissing\n'
                  b'Resident Probe SYS:C/Demo ADD PURE\nResident >rnormal\n'
                  b'Resident SYSTEM >rsystem\nResident Probe REMOVE\n'
                  b'Resident Probe >rmissing2\nResident Echo REPLACE >rreplace\n'
                  b'Resident Echo REMOVE\nEcho >rdisabled yes\nCapResult\n'
                  b'Resident Echo REMOVE\nResident Echo REPLACE\n'
                  b'Echo >rrestored yes\nRorderProbe\n')
    if args.resident_extensions:
        disk.file(directories['c'], 'C/CapDefer', build_cli_result_probe('SYS:defer-result'))
        disk.file(directories['c'], 'C/CapAlias', build_cli_result_probe('SYS:alias-result'))
        disk.file(directories['s'], 'S/resextensions',
                  b'FailAt 21\nResident Dprobe SYS:C/Demo ADD FORCE DEFER\nCapDefer\n'
                  b'Resident Dalias ALIAS Dprobe\nCapAlias\nEcho >SYS:extensions-done done\n')
    if args.path:
        demo = entries.get('c/demo')
        if demo is None or demo['secondary_type'] != -3:
            raise ValueError('Input image lacks the demonstration command')
        disk.file(directories['s'], 'S/pathprobe', tree.read_file(demo['block']))
    if args.path_many:
        disk.file(directories['s'], 'S/manypath', build_many_path_script())
    if args.path_isolation:
        for command, kind in (('SigSetup', 'setup'), ('SigWait', 'wait'), ('SigSend', 'send')):
            disk.file(directories['c'], 'C/' + command, build_path_signal_probe(kind))
        disk.file(directories['s'], 'S/pathchild',
                  b'Path >SYS:path-child-before\nPath RESET\nPath SYS:C ADD\n'
                  b'Path >SYS:path-child-after\nEcho >SYS:path-child-done done\nC:SigSend\n')
        disk.file(directories['s'], 'S/pathisolation',
                  b'FailAt 21\nPath RESET\nPath SYS:S ADD\nPath >SYS:path-parent-before\n'
                  b'C:SigSetup\nRun Execute S:pathchild\nC:SigWait\nPath >SYS:path-parent-after\n')
    free_blocks = fill_volume(disk, full_caller) if args.execute_disk_full else None
    image = disk.finish()
    verify(image, disk.files)
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    probe = output / 'probe.hdf'
    probe.write_bytes(image)
    runner = Path(__file__).with_name('run_kickstart31_candidate.ps1')
    launch = [args.powershell, '-NoProfile', '-File', str(runner), '-Image', str(probe),
              '-KickstartRom', str(args.rom.resolve(strict=True)),
              '-WinUAE', str(args.winuae.resolve(strict=True)),
              '-OutputDirectory', str(output / 'run'), '-DurationSeconds', str(args.duration_seconds)]
    if args.diagnostic_log:
        launch.append('-PortableLog')
    subprocess.run(launch, check=True)
    result = (output / 'run' / 'candidate.hdf').read_bytes()
    result_disk = Adf(result)
    result_tree = result_disk.walk()
    def read(path):
        entry = result_tree.get(path)
        return result_disk.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    observed = {name: read(name) for name in generated if not name.startswith(('c/', 's/', 'l/')) and name != 'execute-fill'}
    execute_temps = sorted(name for name in result_tree if name.startswith('t/command-')) if args.execute_disk_full else []
    measurement_name = 'runlife.bin' if run_lifetime else 'asynclife.bin'
    measurements = observed.get(measurement_name)
    memory = list(struct.unpack('>III', measurements)) if measurements is not None and len(measurements) == 12 else None
    (output / 'observations.json').write_text(json.dumps(
        {name: memory if name == measurement_name else
         list(struct.unpack('>ii', value)) if (name in ('rdiagnostics.bin', 'defer-result', 'alias-result', 'execute-full-result') or name.startswith('run-diag-')) and value is not None and len(value) == 8 else
         struct.unpack('>I', value)[0] if name in ('memory1', 'memory2', 'memory3') and value is not None and len(value) == 4 else
         list(struct.unpack('>IIIII', value)) if name == 'segments.bin' and value is not None and len(value) == 20 else
         list(struct.unpack('>IIII', value)) if name == 'second-alert.bin' and value is not None and len(value) == 16 else
         value.decode('latin1') if value is not None else None
         for name, value in sorted(observed.items())}, indent=2) + '\n')
    expected = {'input-sent.txt': b'SENT\n', 'shell-present.txt': b'PRESENT\n', 'second-boot.txt': None}
    if args.diagnostic_log:
        expected['second-alert.bin'] = None
    if args.segment_layout:
        segment_words = observed['segments.bin']
        if segment_words is None or len(segment_words) != 20:
            raise RuntimeError('Process segment array missing; see observations.json')
        words = struct.unpack('>IIIII', segment_words)
        if words[0] != 2 or not all(words[1:4]) or words[4] != 0:
            raise RuntimeError('Unexpected Kickstart segment-array layout; see observations.json')
        expected.update({'shell-ended.txt': b'ENDED\n', 'segments.bin': segment_words})
    elif args.nonresident_mismatch or args.nonresident_missing:
        expected.update({'shell-ended.txt': b'ENDED\n', 'unexpected': None,
                         'rdiagnostics.bin': struct.pack('>ii', 20, 122 if args.nonresident_mismatch else 205),
                         'runfinish': b'done\n'})
        if result_tree.get('l/savedshell') is not None or (args.nonresident_mismatch and not result_tree.get('l/altshell')):
            raise RuntimeError('Nonresident mismatch fixture did not restore the installed Shell')
        if read('l/shell-seg') != tree.read_file(entries['l/shell-seg']['block']):
            raise RuntimeError('Nonresident mismatch fixture restored the wrong Shell')
    elif args.nonresident_memory:
        samples = [observed[f'memory{i}'] for i in range(1, 4)]
        if any(value is None or len(value) != 4 for value in samples) or len(set(samples)) != 1:
            raise RuntimeError('Nonresident worker memory mismatch; see observations.json')
        expected.update({'shell-ended.txt': b'ENDED\n', 'runfinish': b'done\n',
                         **{f'memory{i}': samples[i - 1] for i in range(1, 4)}})
    elif args.nonresident_retire:
        expected.update({'shell-ended.txt': b'ENDED\n', 'parent-state': b'ENDED\n',
                         'delayed': b'nested\n', 'runfinish': b'done\n'})
    elif run_input_probe:
        expected.update({'delayresult': b'nested\n', 'afterrun': b'done\n',
                         'delay-started': b'START\n', 'overlap': b'EARLY\n', 'input-identity': b'DIFF\n'})
    elif run_lifetime:
        shared = observed['runshared']
        inherited_output = observed['runowned'] if owned_lifetime else shared
        output_pattern = (rb'' if args.new_shell_lifetime else
                          rb'\[CLI [1-9][0-9]*\]\n')
        if inherited_output is None or re.fullmatch(output_pattern, inherited_output) is None:
            raise RuntimeError('Run inherited-output mismatch; see observations.json')
        expected.update({'runlife.bin': measurements, 'runshared': shared})
        if not args.new_shell_lifetime:
            expected['finish'] = b'done\n'
        if owned_lifetime:
            expected.update({'runshared': b'', 'runowned': inherited_output})
    elif run_nested:
        notice = observed['nestednotice']
        # Only the launching RUN writes to the leading file. Descendant
        # notices and the delayed probe's standard output use their own
        # console/NIL: handles; its redirected output remains independently tested.
        valid_notice = notice is not None and re.fullmatch(rb'\[CLI [1-9][0-9]*\]\n', notice) is not None
        if not valid_notice:
            raise RuntimeError('Nested Run notices mismatch; see observations.json')
        expected.update({'shell-ended.txt': None if nested_keep_shell else b'ENDED\n', 'nestednotice': notice,
                         'nestedout': b'nested\n', 'runfinish': b'done\n'})
        if args.run_multi_owned:
            expected['nestedout2'] = b'nested\n'
    elif run_leading:
        first, second = observed['prefix'], observed['prefix2']
        notice = rb'\[CLI [1-9][0-9]*\]\n'
        if ((enabled('empty') and (first is None or re.fullmatch(notice, first) is None)) or
                (enabled('child') and (second is None or re.fullmatch(notice, second) is None))):
            raise RuntimeError('Run leading-redirection notice mismatch; see observations.json')
        expected.update({'shell-ended.txt': None if args.run_keep_shell else b'ENDED\n',
                         'prefix': first if enabled('empty') else None,
                         'prefix2': second if enabled('child') else None,
                         'childout': b'nested\n' if enabled('child') else None, 'runfinish': b'done\n'})
        if args.run_leading_split:
            both = observed['runboth']
            # The child uses its CLI output, while *<> transfers the
            # launcher-opened file as its error stream. This checks our
            # handle lifetime; the original shared-error case is unresolved.
            if enabled('shared') and (both is None or re.fullmatch(rb'\[CLI [1-9][0-9]*\]\nERR\n', both) is None):
                raise RuntimeError('Run shared output/error mismatch; see observations.json')
            expected.update({'runerr': b'ERR\n' if enabled('error') else None,
                             'runstdout': b'OUT\n' if enabled('error') else None,
                             'runread': b'input-data\n' if enabled('input') else None,
                             'runboth': both if enabled('shared') else None})
    elif args.run:
        notice = observed['runnotice']
        if notice is None or re.fullmatch(rb'\[CLI [1-9][0-9]*\]\nAFTER\n', notice) is None:
            raise RuntimeError('Run notice mismatch; see observations.json')
        expected.update({'shell-ended.txt': None if args.run_keep_shell else b'ENDED\n', 'runout': b'child\n',
                         'rundemo': b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n',
                         'runpath': b'Current_directory\nCopperOS:S\nC:\n',
                         'runquoted': b'FIRST\n', 'runescaped': b'hello\nworld\n',
                         'runstdout': b'OUT\n', 'runstderr': b'ERR\n', 'runnested': b'nested\n', 'runnotice': notice,
                         'runalias': b'inherited\n', 'runvar': b'value\n',
                         'rdiagnostics.bin': struct.pack('>ii', 0, 0), 'runfinish': b'done\n'})
        if args.run_diagnostics:
            expected.update({f'run-diag-{i}': struct.pack('>ii', 0, 0) for i in range(1, 11)})
    elif args.child_launch:
        expected.update({'shell-ended.txt': None if args.child_keep_shell else b'ENDED\n',
                         't/child-ok': b'', 't/child-fail': None,
                         't/child-output': b'before\nchild\nafter\n',
                         'childpath': b'Current_directory\nCopperOS:S\nC:\n',
                         'childafter': b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n'})
        if args.child_keep_shell:
            expected['childextra'] = b'done\n'
    elif args.lifetime:
        expected.update({'asynclife.bin': measurements, 'finish': b'done\n'})
    elif args.system:
        expected.update({'systemresult.txt': b'OK\n', 'systemchild.txt': b'Current_directory\nCopperOS:C\nC:\n',
                         'systemfailure.txt': b'OK\n', 'asyncresult.txt': b'OK\n',
                         'asyncchild.txt': b'async\n', 'sysread': b'INPUT DATA\n',
                         'sysinputresult': b'OK\n', 'nullresult.txt': b'OK\n',
                         'nullchild.txt': b'null\n', 'finish': b'done\n'})
    elif args.runner_hunk:
        expected.update({'shell-ended.txt': b'ENDED\n', 'runner-ok': b'',
                         'runner-fail': None, 'io-test': None,
                         'append-one': observed['append-one'], 'append-zero': observed['append-zero'],
                         'append-test': None})
        if runner_fixture is not None:
            expected['fixture'] = runner_fixture
    elif args.append_trace:
        if observed['appendfile'] is None or observed['firstcopy'] is None or observed['appendtrace'] is None or len(observed['appendtrace']) != 4:
            raise RuntimeError('Append diagnostic did not produce its trace; see observations.json')
        expected.update({'shell-ended.txt': b'ENDED\n', 'appendfile': observed['appendfile'],
                         'firstcopy': observed['firstcopy'], 'appendtrace': observed['appendtrace'],
                         'appenddone': b'done\n'})
    elif args.append:
        expected.update({'shell-ended.txt': b'ENDED\n', 'appendfile': b'alpha\nbeta\n',
                         'appenddone': b'done\n'})
    elif args.execute_disk_full:
        expected.update({'shell-ended.txt': b'ENDED\n', 'execute-full-result': struct.pack('>ii', 10, 221),
                         'execute-full-done': b'survived\n', 'execute-full-signal': b'done\n'})
    elif args.control_flow:
        expected.update({'shell-ended.txt': b'ENDED\n', 'ctl-arguments': b'world fallback\n',
                         'ctl-true': b'yes\n', 'ctl-else': b'yes\n',
                         'ctl-wrong': None, 'ctl-skipped': None,
                         'ctl-variable': b'two words\n', 'ctl-alias': b'alias\n',
                         'ctl-redirection': b'alpha\nbeta\n', 'ctl-childdone': b'done\n',
                         'ctlparentdone': b'done\n'})
    elif args.failat:
        expected.update({'shell-ended.txt': b'ENDED\n', 'before': b'Fail limit: 21\n',
                         'nestedvalue': b'Fail limit: 7\n', 'after': b'Fail limit: 7\n'})
    elif args.cd:
        expected.update({'shell-ended.txt': b'ENDED\n', 'cdfirst': b'CopperOS:S\n',
                         'cdparent': b'CopperOS:\n', 'cdsecond': b'CopperOS:C\n',
                         'cdafter': b'CopperOS:C\n', 'rdiagnostics.bin': struct.pack('>ii', 20, 205)})
    elif args.path_isolation:
        parent_path = b'Current_directory\nCopperOS:S\nC:\n'
        expected.update({'shell-ended.txt': b'ENDED\n', 'path-parent-before': parent_path,
                         'path-parent-after': parent_path, 'path-child-before': parent_path,
                         'path-child-after': b'Current_directory\nCopperOS:C\nC:\n',
                         'path-child-done': b'done\n', 'path-setup-ok': b'READY\n', 'path-wait-ok': b'WAITED\n'})
    elif args.path_many:
        expected.update({'shell-ended.txt': b'ENDED\n',
                         'pathmany': b'Current_directory\nCopperOS:S\nC:\n',
                         'pathmanydone': b'done\n'})
    elif args.path:
        initial = b'Current_directory\nCopperOS:C\nC:\n'
        expected.update({'shell-ended.txt': b'ENDED\n', 'pathinitial': initial,
                         'pathadded': b'Current_directory\nCopperOS:C\nCopperOS:S\nC:\n',
                         'pathhit': b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n',
                         'pathremoved': initial, 'pathmiss': None, 'pathcleared': b'Current_directory\nC:\n',
                         'pathdemo': b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n'})
    elif substitution:
        expected.update({'shell-ended.txt': b'ENDED\n',
                         'tassign': b'PRESENT\n' if args.substitution_assigned else b'MISSING\n',
                         'subinternal': b'before inner after\n',
                         'subafter': b'done\n'})
    elif args.resident_extensions:
        expected.update({'shell-ended.txt': b'ENDED\n', 'defer-result': struct.pack('>ii', 5, 236),
                         'alias-result': struct.pack('>ii', 5, 236), 'extensions-done': b'done\n'})
    elif args.resident:
        normal, system, order = observed['rnormal'], observed['rsystem'], observed['rorder']
        header = b'NAME              USE COUNT\n\n'
        resident = b'Probe                0\n'
        echo = b'Echo              INTERNAL\n'
        unsetenv = b'Unsetenv          INTERNAL\n'
        if (normal is None or system is None or not normal.startswith(header) or not system.startswith(header)
                or normal.count(resident) != 1 or system.count(resident) != 1
                or normal.count(echo) != 1 or system.count(echo) != 1
                or normal.count(unsetenv) != 1 or system.count(unsetenv) != 1
                or b'SYSTEM\n' in normal or b'SYSTEM\n' not in system
                or order is None or not order.startswith(b'BEFORE\n' + header)
                or not order.endswith(b'AFTER\n') or order.count(echo) != 1):
            raise RuntimeError('Resident listing mismatch; see observations.json')
        expected.update({'shell-ended.txt': b'ENDED\n', 'rmissing': b'object not found\n',
                         'rnormal': normal, 'rsystem': system, 'rmissing2': b'object not found\n',
                         'rreplace': b'object not found\n', 'rdisabled': None,
                         'rdiagnostics.bin': struct.pack('>ii', 10, 205), 'rrestored': b'yes\n', 'rorder': order})
    elif new_shell:
        expected.update({'shell-ended.txt': b'ENDED\n', 'newalias': b'inherited\n',
                         'newvar': b'value\n', 'newpath': b'Current_directory\nCopperOS:S\nC:\n',
                         'newdir': b'CopperOS:S\n', 'newstack': b'Current stack size is 8192 bytes\n',
                         'newdone': b'done\n', 'parentdone': b'done\n'})
        if args.new_cli:
            expected['afterendshell'] = None
        if args.new_shell_prompt:
            expected['newprompt'] = b'special'
    elif args.new_shell_missing:
        expected.update({'shell-ended.txt': b'ENDED\n', 'childdone': b'child\n',
                         'parentdone': b'parent\n'})
    else:
        expected.update({'shell-ended.txt': None if args.observer_control else b'ENDED\n', 'bootresult': b'hello\n',
                         'aliasresult': b'shared\n', 'pathresult': b'Current_directory\nCopperOS:C\nC:\n',
                         'demoresult': b'Original CopperOS native demonstration\nCommand returned to CopperOS Shell\n'})
        if args.observer_control:
            expected['keepfinish'] = b'done\n'
    if args.run_keep_shell:
        expected['afterrun'] = b'done\n'
    if record_exit_state:
        expected.update({'shell-still.txt': None, 'before-wait.txt': b'WAIT\n', 'after-wait.txt': b'WAIT\n'})
    if args.independent_exit_observer:
        expected.update({'observer-started.txt': b'BEGIN\n', 'observer-tick.txt': b'TICK\n',
                         'observer-result.txt': b'ALIVE\n' if args.run_keep_shell or args.observer_control or run_input_probe else b'ENDED\n'})
    if observed != expected or execute_temps or ((args.lifetime or run_lifetime) and (memory is None or len(set(memory)) != 1)):
        raise RuntimeError('Interactive Shell mismatch; see observations.json')
    if substitution and any(name.startswith('t/prompt.') for name in result_tree):
        raise RuntimeError('Shell left a prompt capture file behind')
    if source.read_bytes() != original:
        raise RuntimeError('Input proof image changed during qualification')
    receipt = {'kind': 'kickstart31-shared-shell-keyboard-probe', 'runtimeQualified': False,
               'nonresidentShell': args.nonresident_shell,
               'nonresidentRetireProbe': args.nonresident_retire,
               'nonresidentMemoryProbe': args.nonresident_memory,
               'nonresidentMismatchProbe': args.nonresident_mismatch,
               'nonresidentMissingProbe': args.nonresident_missing,
               'segmentLayoutProbe': args.segment_layout,
               'systemProbe': args.system, 'controlFlowProbe': args.control_flow,
               'controlPaddingLines': args.control_padding_lines,
               'executeFailureStage': args.execute_disk_full, 'fixtureFreeBlocks': free_blocks,
               'executeTemporaryFiles': execute_temps if args.execute_disk_full else None,
               'runnerProbeSha256': sha(runner_hunk) if runner_hunk is not None else None,
               'runnerFixtureSha256': sha(runner_fixture) if runner_fixture is not None else None,
               'appendProbe': args.append,
               'appendTraceProbe': args.append_trace,
               'failatProbe': args.failat, 'pathProbe': args.path, 'pathManyProbe': args.path_many,
               'pathIsolationProbe': args.path_isolation,
               'cdProbe': args.cd, 'asyncPublicMemory': memory,
               'substitutionProbe': substitution, 'tempAssigned': args.substitution_assigned if substitution else None,
               'residentProbe': args.resident, 'residentExtensionsProbe': args.resident_extensions,
               'newShellProbe': args.new_shell,
               'newCliProbe': args.new_cli,
               'newShellDefaultProbe': args.new_shell_default,
               'newShellWindowProbe': args.new_shell_window,
               'newShellPromptProbe': args.new_shell_prompt,
               'newShellMissingProbe': args.new_shell_missing,
               'newShellLifetimeProbe': args.new_shell_lifetime,
               'runProbe': args.run,
               'runLeadingProbe': args.run_leading,
               'runLeadingSplitProbe': args.run_leading_split,
               'splitCase': args.split_case if args.run_leading_split else None,
               'isolatedSplitInput': args.isolate_split_input,
               'runKeepShell': args.run_keep_shell,
               'independentExitObserver': args.independent_exit_observer,
               'observerControl': args.observer_control,
               'recordExitState': record_exit_state,
               'durationSeconds': args.duration_seconds,
               'lineDelayTicks': args.line_delay_ticks or (250 if args.new_shell_missing else 100 if new_shell else 0),
               'diagnosticLog': args.diagnostic_log,
               'runLifetimeProbe': args.run_lifetime,
               'runOwnedLifetimeProbe': args.run_owned_lifetime,
               'runConsoleLifetimeProbe': args.run_console_lifetime,
               'runNestedLifetimeProbe': args.run_nested_lifetime,
               'runNestedOwnedProbe': args.run_nested_owned,
               'runNestedDelayedProbe': args.run_nested_delayed,
               'runNestedControlProbe': args.run_nested_control,
               'runMultiOwnedProbe': args.run_multi_owned,
               'runInputRaceProbe': args.run_input_race,
               'runInputControlProbe': args.run_input_control,
               'inputPollFollowup': args.input_poll_followup,
               'childLaunchProbe': args.child_launch,
               'childKeepShell': args.child_keep_shell,
               'childProbeSha256': sha(child_hunk) if child_hunk is not None else None,
               'inputImageSha256': sha(original), 'probeImageSha256': sha(image),
               'resultImageSha256': sha(result),
               'limits': 'Test startup adds guest keyboard injection; the complete Shell is not qualified'}
    (output / 'qualification.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
