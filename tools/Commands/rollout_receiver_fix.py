"""Measure and qualify the receiver classification correction on installed commands."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import re
import shutil
import sys
import xml.etree.ElementTree as ET
import time
import os
import copy

from measure_c import identity, git_state, parse_map
from verify_correctness_checkpoint import freeze_response
from qualify_sizes import run, qualify, suites, comparable

REPO = Path(__file__).resolve().parents[2]
PREVIOUS = REPO/'artifacts/dir-size-regression-20261017'


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def save(path, value): path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def prepare(stage):
    assert not stage.exists(), 'Choose a fresh stage.'
    stage.mkdir(parents=True)
    proof = read(REPO/'docs/Commands/Workbench31MorphOS320/dir-value-type-receiver-size-20261017.json')
    for item in proof['freeze']['files']:
        if 'compiler-' in item['path']: assert identity(item['path']) == item
    for folder in ['compiler-baseline', 'compiler-candidate', 'vector-runner']:
        shutil.copytree(PREVIOUS/folder, stage/folder)
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    names = sorted(p.name for p in (stage/'installed/C').iterdir() if p.is_file())
    assert len(names) == 61
    records = []
    for name in names:
        project = REPO/'src/Commands'/name
        # Execute has unrelated source edits in progress. Use its accepted CIL.
        source = (REPO/'artifacts/loop-phi-rollout-20261016/inputs/Execute/compile.rsp' if name == 'Execute'
                  else project/'obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp')
        # Resolve retired compiler-checkout references from the retained accepted SDK.
        lines = source.read_text(encoding='utf-8-sig').splitlines()
        recovered = []
        for i, line in enumerate(lines):
            if line.startswith(('input=', 'managed-assembly=')):
                key, value = line.split('=', 1); path = Path(value)
                if not path.is_absolute(): path = project/path
                if not path.exists():
                    replacement = PREVIOUS/'shipping-inputs'/path.name
                    assert replacement.exists(), str(path)
                    recovered.append(dict(original=str(path), replacement=identity(replacement)))
                    lines[i] = key+'='+str(replacement)
        resolved = stage/(name+'.resolved.rsp')
        resolved.write_text('\n'.join(lines)+'\n', encoding='utf-8')
        files = freeze_response(resolved, project, stage/'inputs'/name)
        records.append(dict(command=name, response=identity(source), recovered=recovered, inputs=files, installed=identity(stage/'installed/C'/name)))
    # Full private aggregate already includes the accepted Version and Dir fixes.
    shutil.copytree(REPO/'artifacts/dir-format-fix-20261017/root-cil', stage/'private-inputs/addbuffers')
    support = REPO/'artifacts/dir-format-fix-20261017/sdk/CopperSharp.Sdk.Amiga.Support.dll'
    shutil.copy2(support, stage/'private-inputs/addbuffers'/support.name)
    roots = {'type': 'Commands.TypeNativeRoot', 'eval': 'Commands.NativeRoot',
             'date': 'Commands.MorphOSDateNativeRoot', 'setdate': 'Commands.MorphOSSetDateNativeRoot',
             'wait': 'Commands.MorphOSWaitNativeRoot', 'which': 'Commands.WhichNativeRoot'}
    for key, folder in roots.items():
        source = (REPO/'artifacts/loop-phi-rollout-20261016/private-inputs'/key if key in ['type', 'eval']
                  else REPO/'tests'/folder/'bin/Release/net10.0')
        if not (source/('CopperOS.'+folder+'.dll')).exists(): continue
        target = stage/'private-inputs'/key; target.mkdir()
        for path in source.glob('*.dll'): shutil.copy2(path, target/path.name)
        assert (target/('CopperOS.'+folder+'.dll')).exists(), folder
        if not (target/support.name).exists(): shutil.copy2(support, target/support.name)
    source_files = []
    for name in names:
        for path in (REPO/'src/Commands'/name).rglob('*'):
            if path.is_file() and path.suffix in ['.cs', '.csproj'] and not {'bin', 'obj'}.intersection(path.relative_to(REPO/'src/Commands'/name).parts):
                target = stage/'source'/path.relative_to(REPO); target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(path, target); source_files.append(identity(target))
    save(stage/'freeze.json', dict(source=git_state(REPO), commands=names, inputs=records,
        compilerFix='9eaa2aa', compilerProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/dir-value-type-receiver-size-20261017.json'),
        files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


def validate(stage):
    frozen = read(stage/'freeze.json')
    for item in frozen['files']: assert identity(item['path']) == item
    return frozen


def compile_image(stage, name, source, dest, compiler, flags=None):
    image = dest/'C'/name; image.parent.mkdir(parents=True, exist_ok=True)
    response = dest/(name+'.rsp')
    lines = flags if flags is not None else source.read_text(encoding='utf-8-sig').splitlines()
    lines = [l for l in lines if not l.startswith(('output=', 'compatibility-report='))]
    response.write_text('\n'.join(lines+['output='+str(image)])+'\n', encoding='utf-8')
    code = run(['dotnet', str(stage/('compiler-'+compiler)/'CopperSharp.Compiler.Cli.dll'), '@'+str(response)], dest/(name+'.log'))
    return dict(command=name, compiler=compiler, exitCode=code,
                image=identity(image) if code == 0 else None, response=identity(response),
                map=parse_map(Path(str(image)+'.map')) if code == 0 else None)


def measure(stage):
    frozen = validate(stage)
    jobs = [(name, compiler) for name in frozen['commands'] for compiler in ['baseline', 'candidate']]
    def one(job):
        name, compiler = job
        row = compile_image(stage, name, stage/'inputs'/name/'compile.rsp', stage/compiler, compiler)
        save(stage/compiler/(name+'.measurement.json'), row)
        print(name, compiler, row['image']['bytes'] if row['image'] else 'FAILED', flush=True)
        return row
    with ThreadPoolExecutor(max_workers=2) as pool: rows = list(pool.map(one, jobs))
    save(stage/'measurements.json', rows)
    summary = []
    for name in frozen['commands']:
        pair = {r['compiler']: r for r in rows if r['command'] == name}
        base, candidate = pair['baseline']['image'], pair['candidate']['image']
        installed = (stage/'installed/C'/name).stat().st_size
        summary.append(dict(command=name, baselineBytes=base['bytes'] if base else None,
                            candidateBytes=candidate['bytes'] if candidate else None, installedBytes=installed,
                            shrinks=bool(base and candidate and candidate['bytes'] < base['bytes']),
                            installSmaller=bool(candidate and candidate['bytes'] < installed)))
    save(stage/'summary.json', summary)
    print('Smaller than baseline and installed:', ','.join(r['command'] for r in summary if r['shrinks'] and r['installSmaller']))


# Only commands with distinct implemented Workbench/MorphOS profiles belong here.
DUAL = '''AddBuffers AddDataTypes Assign Avail BindDrivers Break ChangeTaskPri Date Delete Dir
FileNote Info Join List Lock MakeDir MakeLink Mount Protect Reboot Relabel Rename RequestChoice
RequestFile Search SetClock SetDate SetKeyboard Status Type Version Wait Which Eval DiskChange'''.split()
PRIVATE_SUITES = dict(pair.split('=') for pair in '''
AddBuffers=addbuffers-native-entry-vector-fixture
AddDataTypes=morphos320-adddatatypes-files-iff-dtcd-native-entry-vector-fixture
Assign=morphos320-assign-native-entry-vector-fixture Avail=morphos-avail-native-entry-vector-fixture
BindDrivers=binddrivers-morphos-native-entry-vector-fixture Break=break-native-entry-vector-fixture
ChangeTaskPri=changetaskpri-native-entry-vector-fixture Delete=delete-command-native-entry-vector-fixture
FileNote=filenote-native-entry-vector-fixture Info=morphos320-info-native-entry-vector-fixture
Join=morphos320-join-native-entry-vector-fixture List=morphos320-list-native-entry-vector-fixture
Lock=morphos320-lock-native-entry-vector-fixture MakeDir=morphos320-makedir-native-entry-vector-fixture
MakeLink=makelink-native-entry-vector-fixture Mount=mount-morphos-native-entry-boundary-fixture
Protect=protect-morphos-native-entry-vector-fixture Reboot=morphos320-reboot-native-entry-vector-fixture
Relabel=relabel-native-entry-vector-fixture Rename=morphos-rename-native-entry-vector-fixture
RequestChoice=morphos320-requestchoice-native-entry-vector-fixture
RequestFile=morphos320-requestfile-native-entry-vector-fixture Search=morphos320-search-native-entry-vector-fixture
SetClock=setclock-morphos-native-entry-vector-fixture SetKeyboard=morphos320-setkeyboard-native-entry-vector-fixture
Status=morphos320-status-native-entry-vector-fixture Version=morphos320-version-native-entry-vector-fixture
DiskChange=morphos320-diskchange-native-entry-vector-fixture Dir=morphos320-dir-native-entry-vector-fixture
Type=type-native-entry-vector-fixture Eval=eval-native-entry-vector-fixture
Which=which-morphos-native-entry-vector-fixture Date=morphos320-date-native-entry-vector-fixture
SetDate=morphos320-setdate-native-entry-vector-fixture Wait=morphos320-wait-native-entry-vector-fixture
'''.split())


def private_flags(stage, name):
    folder, assembly, entry = 'addbuffers', 'AddBuffersNativeRoot', 'NativeMorphOS'+name+'Entry'
    if name == 'AddDataTypes': entry = 'NativeMorphOSAddDataTypesListEntry'
    if name in ['Date', 'SetDate', 'Wait']:
        folder, assembly, entry = name.lower(), 'MorphOS'+name+'NativeRoot', 'MorphOS'+name+'Entry'
    if name == 'Which': folder, assembly, entry = 'which', 'WhichNativeRoot', 'MorphOS320WhichEntry'
    if name == 'Type': folder, assembly, entry = 'type', 'TypeNativeRoot', 'NativeMorphOSTypeEntry'
    if name == 'Eval': folder, assembly, entry = 'eval', 'NativeRoot', 'NativeEvalEntry'
    root = stage/'private-inputs'/folder
    if (stage/'private-root-builds'/folder).exists(): root = stage/'private-root-builds'/folder
    image = root/('CopperOS.Commands.'+assembly+'.dll')
    assert image.exists(), str(image)
    lines = [l for l in (stage/'inputs'/name/'compile.rsp').read_text(encoding='utf-8-sig').splitlines()
             if not l.startswith(('input=', 'managed-assembly=', 'entry=', 'exports=', 'include-export='))]
    lines += ['input='+str(image), 'entry=CopperOS.Commands.'+assembly+'.'+entry+'::Main', 'exports=none']
    lines += ['managed-assembly='+str(p) for p in sorted(root.glob('*.dll')) if p != image]
    exports = {'AddDataTypes': ['copperos.morphos.adddatatypes.load.'+s for s in ['read', 'alloc', 'free']],
               'Info': ['copperos.morphos.info.formatdate.putchar'],
               'Date': ['copperos.morphos.date.putch']}.get(name, [])
    if exports:
        lines.remove('exports=none'); lines += ['exports=all']+['include-export='+s for s in exports]
    return lines


def roots(stage):
    """Build missing private CIL roots against retained dependencies only."""
    records = []
    for name, folder in [('Date', 'MorphOSDateNativeRoot'), ('SetDate', 'MorphOSSetDateNativeRoot'),
                         ('Wait', 'MorphOSWaitNativeRoot'), ('Which', 'WhichNativeRoot')]:
        source = REPO/'tests'/('Commands.'+folder)
        original = source/('CopperOS.Commands.'+folder+'.csproj')
        target = stage/'private-root-builds'/name.lower(); target.mkdir(parents=True, exist_ok=True)
        build = stage/'private-source'/name; build.mkdir(parents=True, exist_ok=True)
        project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
        properties = ET.SubElement(project, 'PropertyGroup')
        for key, value in dict(TargetFramework='net10.0', AssemblyName='CopperOS.Commands.'+folder,
                               EnableDefaultCompileItems='false', ImplicitUsings='disable').items():
            ET.SubElement(properties, key).text = value
        items = ET.SubElement(project, 'ItemGroup')
        sources = [p for p in source.glob('*.cs')]
        sources += [source/entry.attrib['Include'].replace('\\', '/') for entry in ET.parse(original).findall('.//Compile')]
        for i, path in enumerate(sources):
            copy = build/(str(i)+'-'+path.name); shutil.copy2(path, copy)
            ET.SubElement(items, 'Compile', Include=str(copy))
        for name_dll in ['CopperSharp.Compiler.dll', 'CopperSharp.Sdk.Amiga.dll', 'CopperSharp.Sdk.Amiga.Support.dll']:
            ref = ET.SubElement(items, 'Reference', Include=Path(name_dll).stem)
            ET.SubElement(ref, 'HintPath').text = str(PREVIOUS/'shipping-inputs'/name_dll)
        csproj = build/'Root.csproj'; ET.ElementTree(project).write(csproj, encoding='utf-8')
        assert run(['dotnet', 'build', str(csproj), '-c', 'Release', '-o', str(target), '--nologo'], build/'build.log') == 0, name
        records.append(dict(command=name, original=identity(original),
                            files=[identity(p) for p in sorted(build.rglob('*')) if p.is_file() and 'obj' not in p.parts],
                            output=[identity(p) for p in sorted(target.glob('*.dll'))]))
    save(stage/'private-roots.json', records)


def qualify_one(stage, name):
    runner = stage/'vector-runner/CopperOS.Commands.NativeExecution.dll'
    shipping = qualify(stage/'baseline', stage/'candidate', runner, [name])[0]
    row = dict(command=name, shipping=shipping, morphos=None)
    if name in DUAL:
        try:
            flags = private_flags(stage, name)
            compiles = [compile_image(stage, name, None, stage/'morphos'/label, label, flags)
                        for label in ['baseline', 'candidate']]
            row['privateCompiles'] = compiles
            if all(c['exitCode'] == 0 for c in compiles):
                row['morphos'] = qualify(stage/'morphos/baseline', stage/'morphos/candidate', runner,
                                         [name], {name: [PRIVATE_SUITES[name]]})[0]
        except AssertionError as error: row['profileGap'] = str(error)
    row['eligible'] = shipping['qualified'] and (name not in DUAL or bool(row['morphos'] and row['morphos']['qualified']))
    return row


def vectors(stage):
    frozen = validate(stage)
    folder = stage/'vector-results'; folder.mkdir(exist_ok=True)
    # Migrate receipts from the initial serial run, preserving their bindings.
    for row in read(stage/'vectors.json') if (stage/'vectors.json').exists() else []:
        save(folder/(row['command']+'.json'), row)
    def one(name):
        target = folder/(name+'.json')
        if target.exists(): return read(target)
        before, after = [stage/label/(name+'.measurement.json') for label in ['baseline', 'candidate']]
        while not (before.exists() and after.exists()): time.sleep(2)
        a, b = read(before)['image'], read(after)['image']
        if not (a and b and b['bytes'] < min(a['bytes'], (stage/'installed/C'/name).stat().st_size)): return None
        row = qualify_one(stage, name); save(target, row); return row
    with ThreadPoolExecutor(max_workers=3) as pool:
        rows = [r for r in pool.map(one, frozen['commands']) if r]
    save(stage/'vectors.json', rows)


def native(stage):
    # Retain the original-DOS and independent-emulator coverage already used
    # to admit List and Search; compare the installed binaries directly.
    import qualify_list_search_fixed as original
    prior = REPO/'artifacts/list-search-fixed-20261017/native'
    for name in ['List', 'Search']:
        root = stage/'native'/name
        (root/'inputs').mkdir(parents=True)
        for folder in ['host', 'guest', 'runner']: shutil.copytree(prior/name/folder, root/folder)
        for filename in ['TEXT.IN', 'LONG.IN']: shutil.copy2(prior/name/'inputs'/filename, root/'inputs'/filename)
        shutil.copy2(stage/'installed/C'/name, root/'inputs'/(name+'.baseline'))
        shutil.copy2(stage/'candidate/C'/name, root/'inputs'/(name+'.candidate'))
    os.environ['COPPERSHARP_KICKSTART31_ROM'] = 'C:/Data/ROM/kickstart-3.1-a500.rom'
    original.native(stage)
    for name in ['List', 'Search']:
        root = stage/'native'/name; tools = REPO/'tools/Commands'/(name+'OsQualification')
        assert run([sys.executable, str(tools/'control-auditor.py'), str(root)], root/'auditor-controls.log') == 0
        args = ['pwsh', '-NoProfile', '-File', str(tools/'confirm-winuae.ps1'), '-Stage', str(root)]
        if name == 'Search': args.append('-RequireCorrectOutput')
        assert run(args, root/'winuae.log') == 0, name
    save(stage/'native-evidence.json', [identity(p) for p in sorted((stage/'native').rglob('*'))
                                       if p.is_file() and p.suffix in ['.dll', '.json', '.py', '.ps1']])


def independent(stage):
    os.environ['COPPERSHARP_KICKSTART31_ROM'] = 'C:/Data/ROM/kickstart-3.1-a500.rom'
    for name in ['List', 'Search']:
        original = stage/'native'/name
        root = stage/'native'/(name+'-independent'); root.mkdir()
        shutil.copytree(original/'inputs', root/'inputs')
        tools = REPO/'tools/Commands'/(name+'OsQualification')
        assert run([sys.executable, str(tools/'control-auditor.py'), str(original)], original/'auditor-controls.log') == 0
        args = ['pwsh', '-NoProfile', '-File', str(tools/'confirm-winuae.ps1'), '-Stage', str(root)]
        if name == 'Search': args.append('-RequireCorrectOutput')
        assert run(args, root/'winuae.log') == 0, name
    save(stage/'native-evidence.json', [identity(p) for p in sorted((stage/'native').rglob('*'))
                                       if p.is_file() and p.suffix in ['.dll', '.json', '.py', '.ps1']])


def eval_modes(stage):
    result = []
    for profile in ['shipping', 'morphos']:
        source = (stage/'inputs/Eval/compile.rsp').read_text(encoding='utf-8-sig').splitlines() if profile == 'shipping' else private_flags(stage, 'Eval')
        suite = suites()['Eval'] if profile == 'shipping' else [PRIVATE_SUITES['Eval']]
        for mode in ['disabled', 'bounded', 'fixed-point']:
            flags = [l for l in source if not l.startswith('peephole=')]+['peephole='+mode]
            dest = stage/'eval-modes'/profile/mode
            compiled = [compile_image(stage, 'Eval', None, dest/label, label, flags) for label in ['baseline', 'candidate']]
            checks = qualify(dest/'baseline', dest/'candidate', stage/'vector-runner/CopperOS.Commands.NativeExecution.dll', ['Eval'], {'Eval': suite})
            result.append(dict(profile=profile, mode=mode, compiles=compiled, checks=checks))
            save(stage/'eval-modes.json', result)


def execute_policy(stage):
    # The retained pre-rollout Execute CIL is accepted, but its old response
    # predates the accepted SimplifyLoopPhis selection. Preserve the first
    # comparison and replay the accepted policy without touching dirty sources.
    prior = stage/'superseded-execute-policy'; prior.mkdir()
    for label in ['baseline', 'candidate']:
        target = prior/label; target.mkdir()
        for file in [stage/label/'C/Execute', stage/label/'C/Execute.map', stage/label/'Execute.rsp', stage/label/'Execute.measurement.json']:
            shutil.copy2(file, target/file.name)
    current = REPO/'src/Commands/Execute/obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp'
    selection = [l for l in current.read_text(encoding='utf-8-sig').splitlines() if l.startswith(('code-size-passes=', 'code-size-optimizations='))]
    flags = [l for l in (stage/'inputs/Execute/compile.rsp').read_text(encoding='utf-8-sig').splitlines() if not l.startswith(('code-size-passes=', 'code-size-optimizations='))]+selection
    save(stage/'execute-policy.json', dict(source=identity(current), selection=selection, originalPolicy=identity(prior/'baseline/Execute.rsp')))
    for label in ['baseline', 'candidate']:
        row = compile_image(stage, 'Execute', None, stage/label, label, flags)
        save(stage/label/'Execute.measurement.json', row)
    rows = [read(stage/r['compiler']/(r['command']+'.measurement.json')) for r in read(stage/'measurements.json')]
    save(stage/'measurements.json', rows)
    summary = read(stage/'summary.json')
    for row in summary:
        if row['command'] != 'Execute': continue
        a, b = [read(stage/label/'Execute.measurement.json')['image']['bytes'] for label in ['baseline', 'candidate']]
        row.update(baselineBytes=a, candidateBytes=b, shrinks=b < a, installSmaller=b < row['installedBytes'])
    save(stage/'summary.json', summary)


def decision(stage):
    validate(stage)
    for group in read(stage/'private-roots.json'):
        for item in group['output']: assert identity(item['path']) == item
    summary = read(stage/'summary.json')
    assert len(summary) == 61 and all(r['baselineBytes'] and r['candidateBytes'] for r in summary)
    checks = {r['command']: r for r in read(stage/'vectors.json')}
    assert set(checks) == {r['command'] for r in summary if r['shrinks'] and r['installSmaller']}
    result = []
    for row in summary:
        name = row['command']; vector = checks.get(name)
        accepted = bool(row['shrinks'] and row['installSmaller'] and vector and vector['eligible'])
        reason = ('Smaller and qualified.' if accepted else 'Already installed at this size.' if row['candidateBytes'] == row['installedBytes']
                  else 'No receiver-fix reduction.' if not row['shrinks'] else 'Not smaller than installed.' if not row['installSmaller']
                  else 'Qualification unavailable or failed; retain installed image.')
        if accepted and name in ['List', 'Search']:
            native_rows = [r for r in read(stage/'native.json') if r['command'] == name]
            controls = read(stage/'native'/name/'negative-controls.json')
            independent_root = stage/'native'/(name+'-independent')
            if not independent_root.exists(): independent_root = stage/'native'/name
            independent = read(independent_root/'winuae/results.json')
            assert len(native_rows) == (13 if name == 'List' else 6)
            assert all(r['qualification']['passed'] for r in native_rows)
            assert len(controls) == 4 and all(r['rejected'] for r in controls)
            assert len(independent) == 3 and all(r['completed'] and r.get('passed', r.get('numberedOutputCorrect')) for r in independent)
            bindings = read(stage/'native'/name/'runner-source-binding.json')
            assert all(d['matches'] for r in bindings[:2] for d in r['documents'])
            for r in independent:
                assert r['romSha256'] == read(stage/'rom.json')['sha256'] and not r['jit'] and r['stackBytes'] == 4096
                if name == 'Search':
                    assert r['outputsAgree'] and r['returns'] == 'baseline 0 0\ncandidate 0 0\n'
        if accepted and name == 'Eval':
            modes = read(stage/'eval-modes.json')
            assert len(modes) == 6 and all(r['qualified'] for m in modes for r in m['checks'])
        if accepted and name == 'TaskList':
            controls = read(stage/'tasklist-comparator-controls.json')
            assert len(controls) == 21 and all(c['equivalent'] == (c['mutation'] == 'instruction-address') for c in controls)
        result.append(dict(**row, accepted=accepted, reason=reason,
                           receiverSavedBytes=row['baselineBytes']-row['candidateBytes'],
                           installedSavedBytes=row['installedBytes']-row['candidateBytes'] if accepted else 0))
    return result


def repair_profiles(stage):
    old = stage/'superseded-profile-setup'; old.mkdir()
    rows = {r['command']: r for r in [read(p) for p in sorted((stage/'vector-results').glob('*.json'))]}
    names = ['Date', 'Lock', 'MakeDir', 'TaskList', 'Execute']
    for name in names:
        save(old/(name+'.json'), rows.get(name))
        for label in ['baseline', 'candidate']:
            target = old/label; target.mkdir(exist_ok=True)
            for suffix in ['.log', '.rsp']:
                source = stage/'morphos'/label/(name+suffix)
                if source.exists(): shutil.copy2(source, target/(name+suffix))
    with ThreadPoolExecutor(max_workers=3) as pool:
        for row in pool.map(lambda n: qualify_one(stage, n), names):
            rows[row['command']] = row
            save(stage/'vector-results'/(row['command']+'.json'), row)
            save(stage/'vectors.json', [rows[n] for n in sorted(rows)])
    controls = []
    for cpu in ['68000', '68020', '68040']:
        path = stage/'candidate/execution'/('TaskList.'+cpu+'.morphos320-tasklist-native-entry-vector-fixture.json')
        source = next(c for c in read(path)['cases'] if c['name'] == 'verbose-running-stack-pointer')
        for label in ['instruction-address', 'stack-value', 'read-count', 'output', 'return', 'ioerr', 'cleanup']:
            changed = copy.deepcopy(source)
            if label == 'instruction-address': changed['taskListStackPointerReads'][0]['ProgramCounter'] += 2
            if label == 'stack-value': changed['taskListStackPointerReads'][0]['Value'] += 4
            if label == 'read-count': changed['taskListStackPointerReads'].append(copy.deepcopy(changed['taskListStackPointerReads'][0]))
            if label == 'output': changed['stdoutHex'] += '00'
            if label == 'return': changed['result'] += 1
            if label == 'ioerr': changed['ioErr'] += 1
            if label == 'cleanup': changed['freeMemCalls'] += 1
            equal = comparable(source) == comparable(changed)
            assert equal == (label == 'instruction-address'), label
            controls.append(dict(cpu=cpu, mutation=label, equivalent=equal))
    save(stage/'tasklist-comparator-controls.json', controls)


def report(stage):
    save(stage/'vectors.json', [read(p) for p in sorted((stage/'vector-results').glob('*.json'))])
    decisions = decision(stage)
    inventory = read(REPO/'docs/Commands/Workbench31MorphOS320/command-inventory.json')
    references = {c['name']: next((f for f in c.get('reference_profiles', {}).get('wb31', {}).get('source_files', [])
                                 if f.get('file_format') == 'amiga-hunk'), None) for c in inventory['commands']}
    for row in decisions:
        reference = references.get(row['command'])
        row['workbench31'] = reference
        row['ratioToWorkbench31'] = (row['candidateBytes'] if row['accepted'] else row['installedBytes'])/reference['bytes'] if reference else None
    measurements = []
    for r in read(stage/'measurements.json'):
        metrics = {k: v for k, v in r['map']['records'].items() if k not in ['MACHINE-OP', 'ALLOCATION', 'FRAME-SLOT']}
        measurements.append(dict(command=r['command'], compiler=r['compiler'], image=r['image'],
                                 response=r['response'], metrics=metrics, reachableMethods=r['map']['reachableMethods'], largestMethods=r['map']['largestMethods']))
    data = dict(compilerFix='9eaa2aa', sourceFreeze=read(stage/'freeze.json'), privateRootBuilds=read(stage/'private-roots.json'),
                executePolicy=read(stage/'execute-policy.json'), decisions=decisions, measurements=measurements,
                vectors=read(stage/'vectors.json'), evalModes=read(stage/'eval-modes.json'),
                native=read(stage/'native.json'), nativeEvidence=read(stage/'native-evidence.json'),
                taskListComparatorControls=read(stage/'tasklist-comparator-controls.json'),
                tools=[identity(Path(__file__)), identity(REPO/'tools/Commands/qualify_sizes.py')],
                receipts=[identity(p) for p in sorted(stage.rglob('*.json')) if 'execution' in p.parts])
    def concise(value):
        if isinstance(value, dict): return {k: concise(v) for k, v in value.items() if k not in ['MACHINE-OP', 'ALLOCATION', 'FRAME-SLOT']}
        if isinstance(value, list): return [concise(v) for v in value]
        return value
    data['vectors'] = concise(data['vectors']); data['evalModes'] = concise(data['evalModes'])
    data['rawMeasurements'] = [identity(p) for p in sorted(stage.glob('*/*.measurement.json'))]
    data['rawQualification'] = [identity(stage/name) for name in ['vectors.json', 'eval-modes.json', 'native.json']]
    if (stage/'accepted-inventory.json').exists(): data['acceptedInventory'] = read(stage/'accepted-inventory.json')
    if (stage/'installed-inventory.json').exists(): data['installedInventory'] = read(stage/'installed-inventory.json')
    target = REPO/'docs/Commands/Workbench31MorphOS320/receiver-classification-rollout-20261017'
    save(target.with_suffix('.json'), data)
    accepted = [r for r in decisions if r['accepted']]
    saved = sum(r['installedSavedBytes'] for r in accepted)
    total = sum(r['installedBytes'] for r in decisions)
    checks = [c for row in data['vectors'] for profile in ['shipping', 'morphos'] if row.get(profile) for c in row[profile]['checks'] if c['passed']]
    lines = ['# Receiver classification rollout — 17 October 2026', '',
             f"Measured all 61 installed commands with clean `69d01d1` and the isolated receiver-classification correction committed as `9eaa2aa`. {sum(r['shrinks'] for r in decisions)} commands shrink in that controlled comparison. {len(accepted)} pass the installation gates, saving **{saved:,} bytes** against installed images: **{total:,} → {total-saved:,} bytes**.", '',
             '## Inputs and interpretation', '',
             'Each compiler pair uses identical frozen CIL, dependencies and response flags. MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and no symbols. Per-command size policies remain unchanged. Removed checkout references are resolved from the retained SDK; every substitution and hash is recorded. Execute uses accepted frozen CIL because unrelated source edits are in progress; its initial old-policy probe is preserved separately and the final comparison uses the accepted SimplifyLoopPhis selection.', '',
             'The receiver correction classifies ordinary unboxed struct receivers as managed pointers rather than GC references. This removes false object roots and can unlock private-storage initialization elimination. Installed savings can also include earlier qualified compiler improvements: the controlled baseline column isolates the receiver effect. Dir already contains this fix and is retained.', '',
             '## Per-command results', '',
             '| Command | Installed | Control | Fixed | Receiver saving | Decision | WB 3.1 ratio after |',
             '|---|---:|---:|---:|---:|---|---:|']
    for r in decisions:
        ratio = f"{r['ratioToWorkbench31']:.2f}×" if r['ratioToWorkbench31'] else '—'
        lines.append(f"| {r['command']} | {r['installedBytes']:,} | {r['baselineBytes']:,} | {r['candidateBytes']:,} | {r['receiverSavedBytes']:+,} | {'Install' if r['accepted'] else 'Retain'} | {ratio} |")
    lines += ['', '## Qualification and execution cost', '',
              'Candidates are compared on 68000, 68020 and 68040 using command-visible output bytes, returns, IoErr, ownership and release observations, startup cases and supported repeated/interleaved calls. Distinct implemented MorphOS profiles are compiled and checked independently. MorphOS-only implementations receive no PPC size comparison. Each command keeps its existing options, checks and stack limit.', '',
              '| Command/profile | Invocations per image | Instructions control → fixed | Peak stack bytes control → fixed |',
              '|---|---:|---:|---:|']
    for row in data['vectors']:
        for profile in ['shipping', 'morphos']:
            q = row.get(profile)
            if not q: continue
            passed = [c for c in q['checks'] if c['passed']]
            if passed:
                lines.append(f"| {row['command']}/{profile} | {sum(c['invocations'] for c in passed):,} | {sum(c['instructionsBefore'] for c in passed):,} → {sum(c['instructionsAfter'] for c in passed):,} | {max(c['peakStackBefore'] for c in passed)} → {max(c['peakStackAfter'] for c in passed)} |")
    lines += ['', f"The main vector comparisons account for {sum(c['invocations'] for c in checks):,} invocations per image. Eval additionally runs both profiles with disabled, bounded and fixed-point peepholes, on all three CPUs. Fixture stack limits are checked on every candidate case. Instruction counts include fixture calls and are not wall-clock speed claims.", '',
              f"Across the main vector matrix, instructions fall from {sum(c['instructionsBefore'] for c in checks):,} to {sum(c['instructionsAfter'] for c in checks):,}. No qualified profile/CPU total increases its instruction count or peak stack use.", '',
              'List and Search additionally compare installed and candidate images under original Kickstart 3.1 DOS: List 306 and Search 192 total calls across both images and all three CPUs, including 4 KiB and 8 KiB stacks, plus eight auditor negative controls. Independent WinUAE confirmations cover both images on all three CPUs. These are CLI original-DOS checks; private MorphOS profiles use supplied-vector fixtures.', '',
              'TaskList initially differed only in the diagnostic program counter of its live-A7 read, not in any output byte or SP value. The comparator now excludes that instruction address while retaining read count, observed SP, output and all resource observations. Eighteen mutation controls reject SP/read-count/output/return/IoErr/cleanup changes; three controls allow changed instruction addresses. The runtime fixture independently validates live A7 and the printed stack value. Initial receipts and setup corrections are preserved.', '',
              f"Vector runtime: Copper68k 1.5.1, SHA-256 `{checks[0]['coreSha256']}`. Original-DOS engine and WinUAE identities, source bindings, ROM identity and raw receipts are retained in the JSON evidence. Original-DOS/CopperScreen coverage is unavailable for the other commands in this rollout; no such coverage is claimed. Exe2Arc lacks a shipping runtime suite and is retained even if it shrinks.", '',
              'Original-DOS runtime: Copper68k `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. Independent emulator: WinUAE `6.0.3.0`, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`.', '',
              '## Retained candidates', '']
    for r in decisions:
        if r['shrinks'] and not r['accepted']: lines.append(f"- **{r['command']}**: {r['reason']}")
    lines += ['', '## Reproduction and delivery', '',
              'Tool: `tools/Commands/rollout_receiver_fix.py`. The JSON report binds compiler payloads, SDK and command assemblies, effective flags, maps and optimizer metrics, largest methods, profile receipts and execution costs. Raw artifacts: `artifacts/receiver-rollout-v3-20261017`; the two earlier incomplete preparation stages are preserved. All 61 command pairs are built before selection. A complete accepted staging inventory is hash-checked before updating selected `out/C` files; unselected files are preserved. No command source or compiler policy change is required.', '',
              'With the retained inputs available, select a fresh stage and run the tool modes in this order: `prepare`, `roots`, `measure`, `execute_policy`, `vectors`, `eval_modes`, `native`, `repair_profiles`, `publish`, `report`, each with `--stage <absolute-stage-path>`. Native confirmation requires PowerShell 7, the recorded ROM and WinUAE. `independent` recovers only the independent-emulator phase into fresh subdirectories after a failed confirmation attempt. Reports retain all initial failures; fixture names/callback exports were corrected without changing command behavior.', '']
    target.with_suffix('.md').write_text('\n'.join(lines), encoding='utf-8')
    save(stage/'decisions.json', decisions)


def publish(stage):
    report(stage)
    decisions = read(stage/'decisions.json')
    target = stage/'accepted/C'; target.mkdir(parents=True)
    # Check the entire installed inventory before modifying any executable.
    assert sorted(p.name for p in (REPO/'out/C').iterdir() if p.is_file()) == sorted(r['command'] for r in decisions)
    for row in decisions:
        name = row['command']
        assert (REPO/'out/C'/name).read_bytes() == (stage/'installed/C'/name).read_bytes(), name
        source = stage/('candidate' if row['accepted'] else 'installed')/'C'/name
        shutil.copy2(source, target/name)
        assert (target/name).read_bytes() == source.read_bytes()
    save(stage/'accepted-inventory.json', [identity(p) for p in sorted(target.iterdir())])
    for row in decisions:
        if row['accepted']: shutil.copy2(target/row['command'], REPO/'out/C'/row['command'])
    assert all((REPO/'out/C'/r['command']).read_bytes() == (target/r['command']).read_bytes() for r in decisions)
    save(stage/'installed-inventory.json', [identity(p) for p in sorted((REPO/'out/C').iterdir()) if p.is_file()])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__); parser.add_argument('mode', choices=['prepare', 'measure', 'roots', 'vectors', 'native', 'independent', 'eval_modes', 'execute_policy', 'repair_profiles', 'report', 'publish'])
    parser.add_argument('--stage', type=Path, required=True); args = parser.parse_args()
    globals()[args.mode](args.stage.resolve())
