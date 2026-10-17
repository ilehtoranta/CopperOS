"""Qualify Dir's character-format ABI correction without rebuilding the compiler."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

from measure_c import identity, git_state, parse_map
from qualify_sizes import run, qualify
from qualify_dir_fixed import compile_image, native, auditor
from verify_correctness_checkpoint import freeze_response

REPO = Path(__file__).resolve().parents[2]
OLD = REPO/'artifacts/dir-fixed-v2-20261017'
CPUS = ['68000', '68020', '68040']


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def save(path, value): path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def snapshot_project(stage, folder, assembly, executable=False):
    original = REPO/'tests'/folder/(assembly+'.csproj')
    sources = list(original.parent.glob('*.cs'))
    sources += [(original.parent/p.attrib['Include'].replace('\\', '/')).resolve()
                for p in ET.parse(original).findall('.//Compile') if 'Include' in p.attrib]
    sources = list(dict.fromkeys(sources))
    root = stage/'projects'/folder; root.mkdir(parents=True)
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
    props = ET.SubElement(project, 'PropertyGroup')
    for key, value in dict(TargetFramework='net10.0', EnableDefaultCompileItems='false', AssemblyName=assembly,
                           ImplicitUsings='enable' if executable else 'disable', Nullable='enable',
                           OutputType='Exe' if executable else 'Library').items():
        ET.SubElement(props, key).text = value
    items = ET.SubElement(project, 'ItemGroup')
    for path in sources:
        target = root/'source'/path.relative_to(REPO); target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
        ET.SubElement(items, 'Compile', Include=str(target))
    for name in ['CopperSharp.Sdk.Amiga', 'CopperSharp.Compiler']:
        ref = ET.SubElement(items, 'Reference', Include=name)
        ET.SubElement(ref, 'HintPath').text = str(stage/'sdk'/(name+'.dll'))
    if executable:
        ET.SubElement(items, 'PackageReference', Include='Copper68k', Version='1.5.1')
        attribute = ET.SubElement(items, 'AssemblyAttribute', Include='System.Reflection.AssemblyMetadataAttribute')
        ET.SubElement(attribute, '_Parameter1').text = 'Copper68kPackageVersion'
        ET.SubElement(attribute, '_Parameter2').text = '1.5.1'
    path = root/(assembly+'.csproj'); ET.indent(project); ET.ElementTree(project).write(path, encoding='utf-8', xml_declaration=True)
    output = stage/('vector-runner' if executable else 'root-cil')
    assert run(['dotnet', 'build', str(path), '-c', 'Release', '--artifacts-path', str(stage/'build'/folder),
                '-o', str(output)], root/'build.log') == 0


def prepare(stage):
    assert not stage.exists(), 'Use a fresh stage; preserve earlier results.'
    stage.mkdir(parents=True)
    shutil.copytree(OLD/'compiler', stage/'compiler')
    for item in read(OLD/'freeze.json')['files']:
        if str(OLD/'compiler') in item['path']: assert identity(item['path']) == item
    shutil.copytree(OLD/'shipping-inputs', stage/'sdk')
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    for profile in ['shipping', 'morphos']:
        shutil.copytree(OLD/profile/'candidate/C', stage/'old'/profile/'C')
    for folder in ['host', 'guest', 'runner']: shutil.copytree(OLD/'native'/folder, stage/'native'/folder)
    for path in sorted((REPO/'src/Commands/Dir').rglob('*')):
        if path.is_file() and not any(p in ['bin', 'obj'] for p in path.relative_to(REPO/'src/Commands/Dir').parts):
            target = stage/'source'/path.relative_to(REPO); target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(path, target)
    snapshot_project(stage, 'Commands.NativeExecution', 'CopperOS.Commands.NativeExecution', True)
    snapshot_project(stage, 'Commands.AddBuffersNativeRoot', 'CopperOS.Commands.AddBuffersNativeRoot')
    save(stage/'prepare.json', dict(source=git_state(REPO),
        files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


def compile_and_vectors(stage):
    for item in read(stage/'prepare.json')['files']: assert identity(item['path']) == item
    inputs = stage/'shipping-inputs'; project = REPO/'src/Commands/Dir'
    freeze_response(project/'obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp', project, inputs)
    private = stage/'morphos-inputs'; private.mkdir()
    old = (OLD/'morphos-inputs/compile.rsp').read_text(encoding='utf-8').splitlines()
    flags = [line for line in old if not line.startswith(('input=', 'managed-assembly='))]
    for name in ['CopperOS.Commands.AddBuffersNativeRoot.dll', 'CopperSharp.Compiler.dll', 'CopperSharp.Sdk.Amiga.dll', 'CopperSharp.Sdk.Amiga.Support.dll']:
        origin = stage/'root-cil'/name if name == 'CopperOS.Commands.AddBuffersNativeRoot.dll' else stage/'sdk'/name
        shutil.copy2(origin, private/name)
        flags.append(('input=' if name.startswith('CopperOS.') else 'managed-assembly=')+str(private/name))
    (private/'compile.rsp').write_text('\n'.join(flags)+'\n', encoding='utf-8')
    checks = {}; controls = []
    runner = stage/'vector-runner/CopperOS.Commands.NativeExecution.dll'
    for profile, source, suite in [('shipping', inputs, 'workbench31-dir-native-entry-vector-fixture'),
                                    ('morphos', private, 'morphos320-dir-native-entry-vector-fixture')]:
        compile_image(stage, source, stage/profile/'candidate')
        control_inputs = stage/(profile+'-no-phi-inputs'); shutil.copytree(source, control_inputs)
        # Keep identical CIL/SDK paths and change only this policy flag.
        text = (source/'compile.rsp').read_text(encoding='utf-8'); assert ',SimplifyLoopPhis' in text
        (control_inputs/'compile.rsp').write_text(text.replace(',SimplifyLoopPhis', ''), encoding='utf-8')
        compile_image(stage, control_inputs, stage/profile/'baseline')
        checks[profile] = qualify(stage/profile/'baseline', stage/profile/'candidate', runner, ['Dir'], {'Dir': [suite]})
        for cpu in CPUS:
            receipt = stage/'controls'/(profile+'-'+cpu+'.json'); receipt.parent.mkdir(exist_ok=True)
            image = stage/'old'/profile/'C/Dir'
            code = run(['dotnet', str(runner), str(image), cpu, str(receipt), suite], receipt.with_suffix('.log'))
            result = read(receipt)
            assert code != 0 and result['status'] == 'failed' and 'unknown-option' in result['failure']
            assert result['imageSha256'] == identity(image)['sha256']
            controls.append(dict(profile=profile, cpu=cpu, runner=identity(runner), image=identity(image), receipt=identity(receipt)))
    assert (stage/'default-build/C/Dir').read_bytes() == (stage/'shipping/candidate/C/Dir').read_bytes()
    save(stage/'vectors.json', checks); save(stage/'controls/abi-negative-controls.json', controls)
    assert all(r['qualified'] for rows in checks.values() for r in rows)
    for profile, folder in [('shipping', 'native'), ('morphos', 'native-morphos')]:
        root = stage/folder
        if profile == 'morphos':
            for child in ['host', 'guest', 'runner']: shutil.copytree(stage/'native'/child, root/child)
        (root/'inputs').mkdir()
        for name in ['TEXT.IN', 'LONG.IN']: shutil.copy2(OLD/'native/inputs'/name, root/'inputs'/name)
        for label in ['baseline', 'candidate']: shutil.copy2(stage/profile/label/'C/Dir', root/'inputs'/('Dir.'+label))
        assert run([sys.executable, str(REPO/'tools/Commands/DirOsQualification/cases.py'), str(root),
                    'workbench' if profile == 'shipping' else 'morphos', '--extended'], root/'cases.log') == 0
    save(stage/'native-freeze.json', [identity(p) for folder in ['native', 'native-morphos'] for p in sorted((stage/folder).rglob('*')) if p.is_file()])
    # native() checks this immutable phase separately from the prepared build inputs.
    save(stage/'freeze.json', dict(files=[identity(p) for folder in ['compiler', 'vector-runner', 'shipping-inputs', 'morphos-inputs']
                                       for p in sorted((stage/folder).rglob('*')) if p.is_file()]))


def validate(stage):
    for item in read(stage/'prepare.json')['files']+read(stage/'freeze.json')['files']+read(stage/'native-freeze.json'):
        assert identity(item['path']) == item
    runner = stage/'vector-runner/CopperOS.Commands.NativeExecution.dll'
    checks = read(stage/'vectors.json')
    for profile, suite, count in [('shipping', 'workbench31-dir-native-entry-vector-fixture', 33),
                                  ('morphos', 'morphos320-dir-native-entry-vector-fixture', 42)]:
        actual = qualify(stage/profile/'baseline', stage/profile/'candidate', runner, ['Dir'], {'Dir': [suite]})
        assert actual == checks[profile] and all(r['qualified'] for r in actual)
        assert all(c['invocations'] == count for r in actual for c in r['checks'])
    audit = auditor(); matrix = []; independent = {}; bindings = {}
    for profile, folder in [('shipping', 'native'), ('morphos', 'native-morphos')]:
        root = stage/folder; contracts = {r['Name']: r for r in read(root/'contracts.json')}
        assert len(contracts) == 27
        for label in ['baseline', 'candidate']:
            assert (root/'inputs'/('Dir.'+label)).read_bytes() == (stage/profile/label/'C/Dir').read_bytes()
        bindings[profile] = read(root/'runner-source-binding.json')
        for row in bindings[profile][:2]: assert all(d['matches'] for d in row['documents'])
        for path in (root/'host').glob('*.dll'):
            original = root/'runner'/path.name
            if original.exists(): assert path.read_bytes() == original.read_bytes()
        for cpu in CPUS:
            names = set(); stacks = set(); count = 0
            for batch in ['part1', 'part2', 'repeat']:
                row = audit(root, batch, cpu); assert row['passed'], row['failures']
                assert row['result']['RomSha256'].lower() == read(stage/'rom.json')['sha256']
                observed = read(root/'runs'/(batch+'-'+cpu)/'observations.json')['observations']
                names.update(r['Name'] for r in observed); stacks.update(r['Stack'] for r in observed)
                count += row['invocations']; matrix.append(dict(profile=profile, **row))
            assert names == set(contracts) and stacks == {4096, 8192} and count == 98
        controls = read(root/'negative-controls.json'); assert len(controls) == 4 and all(r['rejected'] for r in controls)
        independent[profile] = read(root/'winuae/results.json')
        assert {r['cpu'] for r in independent[profile]} == set(CPUS)
        for row in independent[profile]:
            assert row['completed'] and row['passed'] and not row['jit'] and row['stackBytes'] == 4096
            assert row['romSha256'] == read(stage/'rom.json')['sha256']
            sysroot = root/'winuae'/row['cpu']/'sys'
            for label, suffix in [('baseline', 'B'), ('candidate', 'C')]:
                assert (sysroot/'C'/('Dir'+suffix)).read_bytes() == (root/'inputs'/('Dir.'+label)).read_bytes()
            import struct
            faults = (root/'runs/part1-68000/FAULTS.FIX').read_bytes()
            diagnostics = {struct.unpack_from('>I', faults, i)[0]: faults[i+4:i+128].split(b'\0')[0].decode('latin1')+'\n'
                           for i in range(0, len(faults), 128)}
            returns = ''; tags = set()
            for image in ['B', 'C']:
                for key, name in [('full', 'explicit'), ('files', 'files'), ('dirs', 'dirs'), ('ignored', 'opt-ignored'),
                                  ('all', 'all'), ('opt-i', 'opt-i'), ('ignored-lower', 'opt-lower-ignored'),
                                  ('ignored-multiple', 'opt-multiple-ignored')]:
                    contract = contracts[name]; expected = contract['expectedOutput']
                    if isinstance(expected, dict): expected = expected.get('prefix', '')+diagnostics[expected['fault']]
                    tag = image+'-'+key; tags.add(tag)
                    observed = next(c for c in row['checks'] if c['name'] == tag)
                    actual = (sysroot/(tag+'.txt')).read_bytes().decode('latin1')
                    assert observed['actual'] == actual == expected == observed['expected'] and observed['passed']
                    returns += tag+' '+str(contract['expectedReturn'])+' '+str(contract['expectedIoErr'])+'\n'
            assert len(row['checks']) == 16 and {c['name'] for c in row['checks']} == tags
            assert row['returns'] == row['expectedReturns'] == returns == (sysroot/'returns.txt').read_bytes().decode('latin1')
    abi_controls = read(stage/'controls/abi-negative-controls.json'); assert len(abi_controls) == 6
    for row in abi_controls:
        for key in ['runner', 'image', 'receipt']: assert identity(row[key]['path']) == row[key]
        rejected = read(row['receipt']['path'])
        assert row['runner'] == identity(runner) and rejected['status'] == 'failed' and 'unknown-option' in rejected['failure']
    return dict(passed=True, vectors=checks, matrix=matrix, winuae=independent, sourceBindings=bindings, abiNegativeControls=abi_controls,
                auditorControls={profile: read(stage/folder/'negative-controls.json') for profile, folder in [('shipping', 'native'), ('morphos', 'native-morphos')]})


def report(stage):
    qualification = validate(stage)
    common = stage/'common-sdk-inputs'
    if not common.exists():
        shutil.copytree(stage/'shipping-inputs', common)
        for name in ['CopperSharp.Compiler.dll', 'CopperSharp.Sdk.Amiga.dll', 'CopperSharp.Sdk.Amiga.Support.dll']:
            shutil.copy2(stage/'sdk'/name, common/name)
        flags = []
        for line in (common/'compile.rsp').read_text(encoding='utf-8').splitlines():
            if line.startswith(('input=', 'managed-assembly=')):
                key, value = line.split('=', 1); line = key+'='+str(common/Path(value).name)
            flags.append(line)
        (common/'compile.rsp').write_text('\n'.join(flags)+'\n', encoding='utf-8')
    compile_image(stage, common, stage/'common-sdk-replay')
    assert (stage/'common-sdk-replay/C/Dir').read_bytes() == (stage/'shipping/candidate/C/Dir').read_bytes() == (stage/'default-build/C/Dir').read_bytes()
    runtime = read(OLD/'runtime.json')
    for row in runtime: assert identity(stage/'native/host'/(row['name']+'.dll'))['sha256'] == row['sha256']
    save(stage/'runtime.json', runtime)
    assert (REPO/'out/C/Dir').read_bytes() == (stage/'installed/C/Dir').read_bytes()
    changes = [p.name for p in (stage/'installed/C').iterdir() if p.read_bytes() != (REPO/'out/C'/p.name).read_bytes()]
    assert not changes, changes
    def compact(value):
        if isinstance(value, dict): return {k: compact(v) for k, v in value.items() if k != 'PayloadOutput'}
        if isinstance(value, list): return [compact(v) for v in value]
        return value
    tools = [Path(__file__), REPO/'tools/Commands/qualify_dir_fixed.py', REPO/'tools/Commands/qualify_sizes.py',
             REPO/'tools/Commands/measure_c.py', REPO/'tools/Commands/verify_correctness_checkpoint.py',
             *sorted((REPO/'tools/Commands/DirOsQualification').glob('*'))]
    result = dict(date='2026-10-17', status='behavior-qualified-size-held', qualification=compact(qualification),
        compilerProof=read(REPO/'docs/Commands/Workbench31MorphOS320/version-loop-phi-spills-20261017.json'),
        prepare=read(stage/'prepare.json'), sourcePatch= subprocess.check_output(['git', 'diff', '--',
            'src/Commands/Dir/Native/NativeMorphOSDirCommand.cs', 'tests/Commands.NativeExecution/DirEntrySuite.cs'], cwd=REPO).decode('utf-8'),
        sizes={profile: {label: identity(stage/profile/label/'C/Dir') for label in ['baseline', 'candidate']}
               for profile in ['shipping', 'morphos']},
        maps={profile: {label: parse_map(stage/profile/label/'C/Dir.map') for label in ['baseline', 'candidate']}
              for profile in ['shipping', 'morphos']}, nativeRuntime=runtime, installedUnchanged=True,
        installed=identity(REPO/'out/C/Dir'), inventory=[identity(p) for p in sorted((REPO/'out/C').iterdir())],
        tooling=[identity(p) for p in tools if p.is_file()], evidence=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()])
    save(REPO/'docs/Commands/Workbench31MorphOS320/dir-opt-format-fix-20261017.json', result)
    print('Both profiles pass; out/C unchanged because the shipping candidate exceeds the installed size.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode', choices=['prepare', 'compile_and_vectors', 'native', 'report'])
    parser.add_argument('--stage', type=Path, required=True)
    args = parser.parse_args(); globals()[args.mode](args.stage.resolve())
