"""Compile frozen command inputs and compare native execution receipts."""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
from pathlib import Path
from measure_c import parse_map, identity, git_state

REPO = Path(__file__).resolve().parents[2]
PASSES = ('ElideUnusedRegisterArguments', 'ReuseIncomingArgumentHomes',
          'ForwardReadOnlyAggregateLocals', 'InlineSingleUseMethods',
          'ClusterInternalCalls', 'ShareIdenticalMethods', 'ShareReturnSequences')
SUITE_CONSTANTS = dict(pair.split('=') for pair in '''
AddBuffers=AddBuffersEntrySuite AddDataTypes=Workbench31AddDataTypesEntrySuite Assign=Workbench31AssignEntrySuite
Avail=Workbench31AvailEntrySuite Beep=BeepEntrySuite BindDrivers=Workbench31BindDriversEntrySuite
Break=Workbench31BreakEntrySuite ChangeTaskPri=Workbench31ChangeTaskPriEntrySuite
Check2090=Check2090EntrySuite Date=Workbench31DateEntrySuite Delete=WorkbenchDeleteCommandProbeSuite
DevList=DevListEntrySuite Dir=Workbench31DirEntrySuite DiskChange=Workbench31DiskChangeEntrySuite
DiskFree=DiskFreeEntrySuite DosList=DosListEntrySuite Eval=WorkbenchEvalEntrySuite
ExtractKickstart=ExtractKickstartEntrySuite FileNote=WorkbenchFileNoteEntrySuite
FindResident=FindResidentEntrySuite Format=FormatEntrySuite GuessBootDev=GuessBootDevEntrySuite
IconPos=IconPosEntrySuite Info=Workbench31InfoEntrySuite Join=Workbench31JoinEntrySuite
LibList=LibListEntrySuite List=WorkbenchListEntrySuite LoadMonDrvs=LoadMonDrvsEntrySuite
Lock=Workbench31LockEntrySuite MakeDir=Workbench31MakeDirEntrySuite MakeLink=WorkbenchMakeLinkEntrySuite
ModList=ModListEntrySuite Mount=Workbench31MountEntrySuite PathPart=PathPartEntrySuite
PortList=PortListEntrySuite Protect=Workbench31ProtectEntrySuite Quote=QuoteNativeEntrySuite
Reboot=Workbench31RebootEntrySuite Relabel=RelabelEntrySuite Rename=WorkbenchRenameStartupSuite
RequestChoice=Workbench31RequestChoiceEntrySuite RequestFile=Workbench31RequestFileEntrySuite
ResList=ResListEntrySuite Search=WorkbenchSearchEntrySuite SetClock=Workbench31SetClockEntrySuite
SetDate=Workbench31SetDateEntrySuite SetFont=Workbench31SetFontEntrySuite
SetKeyboard=Workbench31SetKeyboardEntrySuite Status=Workbench31StatusEntrySuite TaskList=TaskListEntrySuite
Touch=TouchEntrySuite Type=Workbench31TypeNativeEntrySuite Version=Workbench31VersionEntrySuite
Wait=Workbench31WaitEntrySuite WaitForLib=WaitForLibEntrySuite WaitForNotification=WaitForNotificationEntrySuite
WaitForPort=WaitForPortEntrySuite Which=WorkbenchWhichEntrySuite
'''.split())


def suites():
    constants = {}
    for source in (REPO / 'tests/Commands.NativeExecution').glob('*Suite.cs'):
        constants.update(re.findall(r'public const string (\w+)\s*=\s*"([^"]+)"', source.read_text()))
    result = {name: [constants[key]] for name, key in SUITE_CONSTANTS.items()}
    result['Copy'] = ['copy-direct-native-entry-vector-fixture+command',
                      'copy-parsed-delete-native-entry-vector-fixture+command',
                      'copy-parsed-makedir-native-entry-vector-fixture+command',
                      'copy-single-target-native-entry-vector-fixture+single-command',
                      'copy-target-dispatch-native-entry-vector-fixture+recursive-command']
    result['Execute'] = ['wb31-execute-native-entry-vector-fixture']
    return result


def run(command, log):
    log.parent.mkdir(parents=True, exist_ok=True)
    with log.open('w', encoding='utf-8') as output:
        return subprocess.run(command, cwd=REPO, stdout=output, stderr=subprocess.STDOUT).returncode


def compile_stage(inputs, compiler, stage, names, policy, selected=None, peephole=None):
    (stage / 'C').mkdir(parents=True, exist_ok=True)
    rows = []
    for name in names:
        source = inputs / 'inputs' / name / 'compile.rsp'
        lines = [line for line in source.read_text(encoding='utf-8-sig').splitlines()
                 if not line.startswith(('code-size-optimizations=', 'code-size-passes=',
                                         'output=', 'compatibility-report='))
                 and not (peephole and line.startswith('peephole='))]
        image = (stage / 'C' / name).resolve()
        lines += ['code-size-optimizations=' + policy, 'output=' + str(image)]
        if selected:
            lines.append('code-size-passes=' + selected)
        if peephole:
            lines.append('peephole=' + peephole)
        response = stage / (name + '.rsp')
        response.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        code = run(['dotnet', str(compiler), '@' + str(response.resolve())], stage / (name + '.compile.log'))
        row = {'command': name, 'compileExit': code}
        if not code:
            row.update(image=identity(image), **parse_map(Path(str(image) + '.map')))
        rows.append(row)
        print(f'{name} {policy}/{selected or "all"} {row.get("image", {}).get("bytes", "FAILED")}', flush=True)
    (stage / 'compilation.json').write_text(json.dumps(rows, indent=2) + '\n')
    if any(r['compileExit'] for r in rows):
        raise RuntimeError('Compilation failed; see logs.')


def comparable(case):
    # Instruction count, stack footprint and compiler-owned context are measured
    # separately. Every command-visible observation remains in the comparison.
    return {key: value for key, value in case.items()
            if key not in ('instructions', 'stackBytesWritten', 'peakStackBytes', 'compilerInvocationContext')}


def receipt(stage, name, cpu, suite, runner):
    image = stage / 'C' / name
    map_path = Path(str(image) + '.map')
    recorded_map = stage / 'maps' / (name + '.map')
    if not map_path.exists() and recorded_map.exists():
        shutil.copy2(recorded_map, map_path)
    report = stage / 'execution' / f'{name}.{cpu}.{suite}.json'
    cached = json.loads(report.read_text(encoding='utf-8-sig')) if report.exists() else {}
    valid = cached.get('status') in ('passed', 'failed') and cached.get('managedExecutorSha256') == identity(runner)['sha256'] and cached.get('imageSha256') == identity(image)['sha256']
    if cached.get('compilerMapSha256') is not None:
        valid &= cached['compilerMapSha256'] == identity(map_path)['sha256']
    if not valid:
        run(['dotnet', str(runner), str(image.resolve()), cpu, str(report.resolve()), suite],
            Path(str(report) + '.log'))
    return json.loads(report.read_text(encoding='utf-8-sig'))


def qualify(before, after, runner, names, suite_overrides=None):
    supported = suites()
    if suite_overrides:
        supported.update(suite_overrides)
    rows = []
    for name in names:
        base_bytes = (before / 'C' / name).stat().st_size
        new_bytes = (after / 'C' / name).stat().st_size
        row = {'command': name, 'beforeBytes': base_bytes, 'afterBytes': new_bytes,
               'savedBytes': base_bytes - new_bytes, 'checks': [], 'passed': True}
        for suite in supported.get(name, []):
            for cpu in ('68000', '68020', '68040'):
                control = receipt(before, name, cpu, suite, runner)
                candidate = receipt(after, name, cpu, suite, runner)
                passed = control['status'] == candidate['status'] == 'passed'
                differences = []
                if passed:
                    a, b = control.get('cases', control.get('observations')), candidate.get('cases', candidate.get('observations'))
                    passed = [comparable(c) for c in a] == [comparable(c) for c in b]
                    differences = [x.get('name', x.get('id')) for x, y in zip(a, b) if comparable(x) != comparable(y)]
                    passed &= len(a) == len(b) and all(c.get('peakStackBytes', c['stackBytesWritten']) <= c.get('configuredStackBytes', 8192) for c in b)
                check = {'cpu': cpu, 'suite': suite, 'passed': passed,
                         'baselineFailure': control.get('failure'), 'candidateFailure': candidate.get('failure'),
                         'differentCases': differences}
                if passed:
                    check.update(invocations=len(b),
                                 instructionsBefore=sum(c['instructions'] for c in a),
                                 instructionsAfter=sum(c['instructions'] for c in b),
                                 peakStackBefore=max(c.get('peakStackBytes', c['stackBytesWritten']) for c in a),
                                 peakStackAfter=max(c.get('peakStackBytes', c['stackBytesWritten']) for c in b),
                                 coreVersion=candidate.get('instructionExecutor', 'Copper68k 1.5.1'), coreSha256=candidate['instructionCoreSha256'])
                row['checks'].append(check)
                row['passed'] &= passed
        row['qualified'] = row['passed'] and bool(row['checks'])
        row['accept'] = row['qualified'] and new_bytes < base_bytes
        rows.append(row)
        (after / 'qualification.json').write_text(json.dumps(rows, indent=2) + '\n')
        print(f'{name}: {base_bytes} -> {new_bytes}, qualified={row["qualified"]}, accept={row["accept"]}', flush=True)
    return rows


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--inputs', type=Path)
    parser.add_argument('--compiler', type=Path)
    parser.add_argument('--stage', type=Path, required=True)
    parser.add_argument('--compare', type=Path)
    parser.add_argument('--runner', type=Path)
    parser.add_argument('--commands', help='comma-separated command names')
    parser.add_argument('--policy', choices=('on', 'off'), default='off')
    parser.add_argument('--pass', dest='selected', choices=PASSES + ('RemoveRedundantTransport',
                        'CompactGuestMemory', 'NarrowOperations', 'EliminateRedundantInitialization',
                        'SizeFirstCosts', 'InlineMemoryHelpers', 'ShareArithmeticCores'))
    parser.add_argument('--peephole', choices=('off', 'bounded', 'fixed-point'))
    args = parser.parse_args()
    names = args.commands.split(',') if args.commands else sorted(p.name for p in
             ((args.inputs or args.stage) / ('inputs' if args.inputs else 'C')).iterdir()
             if (p.is_dir() if args.inputs else not p.suffix))
    if args.inputs:
        compile_stage(args.inputs.resolve(), args.compiler.resolve(), args.stage.resolve(), names,
                      args.policy, args.selected, args.peephole)
    if args.compare:
        qualify(args.compare.resolve(), args.stage.resolve(), args.runner.resolve(), names)
