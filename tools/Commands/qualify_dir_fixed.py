"""Freeze and qualify Dir with the compiler used for List/Search qualification."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys

from measure_c import identity, git_state, parse_map
from qualify_sizes import run, qualify
from verify_correctness_checkpoint import freeze_response

REPO = Path(__file__).resolve().parents[2]


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def save(path, value): path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def prepare(stage):
    assert not stage.exists(), 'Choose a fresh evidence directory.'
    previous = REPO/'artifacts/list-search-fixed-20261017'
    stage.mkdir(parents=True)
    for folder in ['compiler', 'vector-runner']:
        shutil.copytree(previous/folder, stage/folder)
    for item in read(previous/'freeze.json')['files']:
        if str(previous/'compiler') in item['path'] or str(previous/'vector-runner') in item['path']:
            assert identity(item['path']) == item
    shutil.copytree(previous/'native/Search/runner', stage/'native/runner')
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    for label in ['baseline', 'candidate']:
        (stage/'shipping'/label/'C').mkdir(parents=True)
    shutil.copy2(REPO/'out/C/Dir', stage/'shipping/baseline/C/Dir')
    installed_proof = REPO/'artifacts/loop-phi-rollout-20261016/candidate/C/Dir'
    assert installed_proof.read_bytes() == (REPO/'out/C/Dir').read_bytes()
    shutil.copy2(Path(str(installed_proof)+'.map'), stage/'shipping/baseline/C/Dir.map')
    probe = REPO/'artifacts/version-spills-20261017/other-commands/Dir/candidate'
    freeze_response(probe/'compile.rsp', probe, stage/'shipping-inputs')
    private = REPO/'artifacts/loop-phi-rollout-20261016/profiles/addbuffers-Dir-fixed-point/candidate'
    freeze_response(private/'compile.rsp', private, stage/'morphos-inputs')
    before = stage/'morphos/baseline/C'; before.mkdir(parents=True)
    for suffix in ['', '.map']: shutil.copy2(private/'C'/('Dir'+suffix), before/('Dir'+suffix))
    inputs = stage/'native/inputs'; inputs.mkdir()
    (inputs/'TEXT.IN').write_bytes(b'Alpha\nNeedle\nomega\n')
    (inputs/'LONG.IN').write_bytes(b'x'*1025)
    save(stage/'freeze.json', dict(source=git_state(REPO),
        compilerProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/version-loop-phi-spills-20261017.json'),
        files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


def compile_image(stage, inputs, dest):
    image = dest/'C/Dir'; image.parent.mkdir(parents=True, exist_ok=True)
    response = dest/'compile.rsp'
    response.write_text((inputs/'compile.rsp').read_text(encoding='utf-8')+'output='+str(image)+'\n', encoding='utf-8')
    assert run(['dotnet', str(stage/'compiler/CopperSharp.Compiler.Cli.dll'), '@'+str(response)], dest/'compile.log') == 0
    return identity(image)


def vectors(stage):
    for item in read(stage/'freeze.json')['files']: assert identity(item['path']) == item
    for profile, inputs in [('shipping', 'shipping-inputs'), ('morphos', 'morphos-inputs')]:
        compile_image(stage, stage/inputs, stage/profile/'candidate')
    assert (stage/'default-build/C/Dir').read_bytes() == (stage/'shipping/candidate/C/Dir').read_bytes()
    result = {}
    for profile, suite in [('shipping', 'workbench31-dir-native-entry-vector-fixture'), ('morphos', 'morphos320-dir-native-entry-vector-fixture')]:
        rows = qualify(stage/profile/'baseline', stage/profile/'candidate', stage/'vector-runner/CopperOS.Commands.NativeExecution.dll', ['Dir'], {'Dir': [suite]})
        result[profile] = rows
    save(stage/'vectors.json', result)
    assert all(r['qualified'] for rows in result.values() for r in rows)
    for label in ['baseline', 'candidate']:
        shutil.copy2(stage/'shipping'/label/'C/Dir', stage/'native/inputs'/('Dir.'+label))


def prepare_native(stage):
    root = stage/'native'; tools = REPO/'tools/Commands/DirOsQualification'
    private = stage/'native-morphos'; assert not private.exists()
    for folder in ['host', 'guest', 'runner']: shutil.copytree(root/folder, private/folder)
    (private/'inputs').mkdir()
    for name in ['TEXT.IN', 'LONG.IN']: shutil.copy2(root/'inputs'/name, private/'inputs'/name)
    for label in ['baseline', 'candidate']:
        shutil.copy2(stage/'morphos'/label/'C/Dir', private/'inputs'/('Dir.'+label))
    for profile, target in [('workbench', root), ('morphos', private)]:
        assert run([sys.executable, str(tools/'cases.py'), str(target), profile], target/'cases.log') == 0
    save(stage/'native-freeze.json', [identity(p) for target in [root, private]
         for p in sorted(target.rglob('*')) if p.is_file() and 'runs' not in p.relative_to(target).parts])


def native(stage):
    tools = REPO/'tools/Commands/DirOsQualification'
    for item in read(stage/'freeze.json')['files']+read(stage/'native-freeze.json'): assert identity(item['path']) == item
    rom = Path(os.environ['COPPERSHARP_KICKSTART31_ROM']).resolve()
    assert identity(rom)['sha256'] == '8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
    save(stage/'rom.json', identity(rom))
    for folder in ['native', 'native-morphos']:
        root = stage/folder; host = root/'host/CopperOS.DirQualification.Host.dll'
        assert run(['dotnet', str(host), '--bind', str(root/'runner'), str(root/'runner-source-binding.json')], root/'bind.log') == 0
    def one(job):
        folder, cpu, batch = job; root = stage/folder; log = root/(batch+'-'+cpu+'.log')
        host = root/'host/CopperOS.DirQualification.Host.dll'
        if not (root/'runs'/(batch+'-'+cpu)/'observations.json').exists():
            assert run(['dotnet', str(host), str(root), str(rom), cpu, str(root/('cases-'+batch+'.json')), batch], log) == 0, job
        code = run([sys.executable, str(tools/'verify.py'), str(root), batch, cpu], Path(str(log)+'.verify'))
        row = read(root/'runs'/(batch+'-'+cpu)/'qualification.json')
        assert code == (0 if row['passed'] else 1), job
        return dict(profile=folder, **row)
    jobs = [(folder, cpu, batch) for folder in ['native', 'native-morphos'] for cpu in ['68000', '68020', '68040'] for batch in ['part1', 'part2', 'repeat']]
    with ThreadPoolExecutor(max_workers=2) as pool: rows = list(pool.map(one, jobs))
    save(stage/'native.json', rows)
    assert all(r['passed'] for r in rows), 'Native contract failure; retain installed image.'


def auditor():
    path = REPO/'tools/Commands/DirOsQualification/verify.py'
    spec = importlib.util.spec_from_file_location('DirNativeAuditor', path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module.verify


def check(stage):
    for item in read(stage/'freeze.json')['files']+read(stage/'native-freeze.json'): assert identity(item['path']) == item
    vectors = read(stage/'vectors.json')
    assert all(r['qualified'] for rows in vectors.values() for r in rows)
    assert (stage/'default-build/C/Dir').read_bytes() == (stage/'shipping/candidate/C/Dir').read_bytes()
    audit = auditor(); matrix = []; independent = {}; bindings = {}
    for profile, folder in [('shipping', 'native'), ('morphos', 'native-morphos')]:
        root = stage/folder
        for label in ['baseline', 'candidate']:
            assert (root/'inputs'/('Dir.'+label)).read_bytes() == (stage/profile/label/'C/Dir').read_bytes()
        bindings[profile] = read(root/'runner-source-binding.json')
        for row in bindings[profile][:2]: assert all(d['matches'] for d in row['documents'])
        for path in (root/'host').glob('*.dll'):
            original = root/'runner'/path.name
            if original.exists(): assert path.read_bytes() == original.read_bytes()
        contracts = {r['Name']: r for r in read(root/'contracts.json')}
        assert len(contracts) == 24
        controls = read(root/'negative-controls.json')
        assert len(controls) == 4 and all(r['rejected'] for r in controls)
        for cpu in ['68000', '68020', '68040']:
            names = set(); stacks = set(); count = 0
            for batch in ['part1', 'part2', 'repeat']:
                row = audit(root, batch, cpu)
                assert row['result']['RomSha256'].lower() == read(stage/'rom.json')['sha256']
                observed = read(root/'runs'/(batch+'-'+cpu)/'observations.json')['observations']
                names.update(o['Name'] for o in observed); stacks.update(o['Stack'] for o in observed)
                count += row['invocations']; matrix.append(dict(profile=profile, **row))
            assert names == set(contracts) and stacks == {4096, 8192} and count == 92
        independent[profile] = read(root/'winuae/results.json')
        assert {r['cpu'] for r in independent[profile]} == {'68000', '68020', '68040'}
        for row in independent[profile]:
            assert row['completed'] and not row['jit'] and row['stackBytes'] == 4096
            assert row['romSha256'] == read(stage/'rom.json')['sha256']
            sysroot = root/'winuae'/row['cpu']/'sys'
            for label, suffix in [('baseline', 'B'), ('candidate', 'C')]:
                assert (sysroot/'C'/('Dir'+suffix)).read_bytes() == (root/'inputs'/('Dir.'+label)).read_bytes()
            faults = (root/'runs/part1-68000/FAULTS.FIX').read_bytes()
            import struct
            diagnostics = {struct.unpack_from('>I', faults, i)[0]: faults[i+4:i+128].split(b'\0')[0].decode('latin1')+'\n'
                           for i in range(0, len(faults), 128)}
            returns = ''; keys = set()
            for image in ['B', 'C']:
                for key, name in [('full', 'explicit'), ('files', 'files'), ('dirs', 'dirs'), ('ignored', 'opt-ignored'), ('all', 'all'), ('opt-i', 'opt-i')]:
                    contract = contracts[name]; expected = contract['expectedOutput']
                    if isinstance(expected, dict): expected = expected.get('prefix', '')+diagnostics[expected['fault']]
                    tag = image+'-'+key; keys.add(tag)
                    observed = next(r for r in row['checks'] if r['name'] == tag)
                    actual = (sysroot/(tag+'.txt')).read_bytes().decode('latin1')
                    assert observed['actual'] == actual and observed['expected'] == expected
                    assert observed['passed'] == (actual == expected)
                    returns += tag+' '+str(contract['expectedReturn'])+' '+str(contract['expectedIoErr'])+'\n'
            assert len(row['checks']) == 12 and {r['name'] for r in row['checks']} == keys
            actual_returns = (sysroot/'returns.txt').read_bytes().decode('latin1')
            assert row['returns'] == actual_returns and row['expectedReturns'] == returns
            assert row['passed'] == (actual_returns == returns and all(r['passed'] for r in row['checks']))
    assert audit(stage/'native', 'smoke', '68000')['passed']
    smaller = (stage/'shipping/candidate/C/Dir').stat().st_size < (stage/'installed/C/Dir').stat().st_size
    native_pass = all(r['passed'] for r in matrix)
    winuae_pass = all(r['passed'] for rows in independent.values() for r in rows)
    result = dict(accept=smaller and native_pass and winuae_pass, strictlySmaller=smaller,
        nativePassed=native_pass, winuaePassed=winuae_pass, matrix=matrix, vectors=vectors, sourceBindings=bindings,
        winuae=independent, negativeControls={profile: read(stage/folder/'negative-controls.json') for profile, folder in [('shipping', 'native'), ('morphos', 'native-morphos')]})
    save(stage/'acceptance.json', result)
    return result


def report(stage):
    result = check(stage)
    # A failed qualification must never alter any installed image.
    assert not result['accept'], 'This dated report describes a held experiment.'
    changes = [p.name for p in (stage/'installed/C').iterdir() if p.read_bytes() != (REPO/'out/C'/p.name).read_bytes()]
    assert not changes, changes
    assert {p.name for p in (stage/'installed/C').iterdir()} == {p.name for p in (REPO/'out/C').iterdir()}
    inputs = stage/'installation-inputs'
    if not inputs.exists():
        project = REPO/'src/Commands/Dir'
        freeze_response(project/'obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp', project, inputs)
    compile_image(stage, inputs, stage/'installation-replay')
    assert (stage/'installation-replay/C/Dir').read_bytes() == (stage/'shipping/candidate/C/Dir').read_bytes()
    def compact(value):
        if isinstance(value, dict): return {k: compact(v) for k, v in value.items() if k != 'PayloadOutput'}
        if isinstance(value, list): return [compact(v) for v in value]
        return value
    result = dict(date='2026-10-17', status='held', compilerFixCommit='69d01d1',
        compilerProof=read(REPO/'docs/Commands/Workbench31MorphOS320/version-loop-phi-spills-20261017.json'),
        freeze=read(stage/'freeze.json'), acceptance=compact(result), nativeRuntime=read(stage/'runtime.json'),
        maps={profile: {label: parse_map(stage/profile/label/'C/Dir.map') for label in ['baseline', 'candidate']} for profile in ['shipping', 'morphos']},
        tooling=[identity(p) for p in [Path(__file__), *sorted((REPO/'tools/Commands/DirOsQualification').glob('*')),
            REPO/'tools/Commands/SearchOsQualification/Bind.cs', REPO/'tools/Commands/build-c.ps1',
            REPO/'tools/Commands/measure_c.py', REPO/'tools/Commands/qualify_sizes.py', REPO/'tools/Commands/verify_correctness_checkpoint.py'] if p.is_file()],
        installedUnchanged=True, inventory=[identity(p) for p in sorted((REPO/'out/C').iterdir())],
        evidence=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()])
    save(REPO/'docs/Commands/Workbench31MorphOS320/dir-fixed-compiler-20261017.json', result)
    print('Held: shipping image is larger and original DOS/WinUAE strict output contracts fail. out/C unchanged.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode', choices=['prepare', 'vectors', 'prepare_native', 'native', 'check', 'report'])
    parser.add_argument('--stage', type=Path, required=True)
    args = parser.parse_args()
    globals()[args.mode](args.stage.resolve())
