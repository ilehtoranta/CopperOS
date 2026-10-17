"""Freeze and qualify List/Search with the already qualified coalescing compiler."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import importlib.util
import json
from pathlib import Path
import os
import shutil
import subprocess

from measure_c import identity, git_state, parse_map
from qualify_sizes import run, qualify
from verify_correctness_checkpoint import freeze_response

REPO = Path(__file__).resolve().parents[2]
COMMANDS = ['List', 'Search']
OLD = {'List': REPO/'artifacts/list-os-qualification-20261016',
       'Search': REPO/'artifacts/search-main-toolchain-20261012'}
PRIVATE = {'List': REPO/'artifacts/list-pressure-20261016/main-morphos-candidate',
           'Search': REPO/'artifacts/search-output-ordering-final-20261012/morphos-candidate'}


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def save(path, value): path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def prepare(stage):
    assert not stage.exists(), 'Preserve earlier evidence; choose a fresh stage.'
    source = REPO/'artifacts/version-spills-20261017'
    proof = read(REPO/'docs/Commands/Workbench31MorphOS320/version-loop-phi-spills-20261017.json')
    for item in proof['freeze']['compiler']: assert identity(item['path']) == item
    stage.mkdir(parents=True)
    shutil.copytree(source/'qualification/compiler', stage/'compiler')
    shutil.copytree(source/'qualification/runner', stage/'vector-runner')
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    for name in COMMANDS:
        root = stage/'native'/name; (root/'inputs').mkdir(parents=True)
        for folder in ['runner', 'host', 'guest']: shutil.copytree(OLD[name]/folder, root/folder)
        shutil.copy2(REPO/'out/C'/name, root/'inputs'/(name+'.baseline'))
        shutil.copy2(source/'other-commands'/name/'candidate/C'/name, root/'inputs'/(name+'.candidate'))
        for filename in ['TEXT.IN', 'LONG.IN']: shutil.copy2(OLD[name]/'inputs'/filename, root/'inputs'/filename)
        for label, image in [('baseline', root/'inputs'/(name+'.baseline')), ('candidate', root/'inputs'/(name+'.candidate'))]:
            dest = stage/'shipping'/label/'C'; dest.mkdir(parents=True, exist_ok=True); shutil.copy2(image, dest/name)
        # Preserve the installed image's previously accepted map, not the
        # clean-compiler probe map: the toolchain inputs differ.
        baseline_map = REPO/('artifacts/list-pressure-20261016/main-candidate/C/List.map' if name == 'List' else 'artifacts/search-main-toolchain-20261012/normal/C/Search.map')
        shutil.copy2(baseline_map, stage/'shipping/baseline/C'/(name+'.map'))
        shutil.copy2(source/'other-commands'/name/'candidate/C'/(name+'.map'), stage/'shipping/candidate/C'/(name+'.map'))
        before = stage/'morphos'/name/'baseline/C'; before.mkdir(parents=True)
        for suffix in ['', '.map']: shutil.copy2(PRIVATE[name]/'C'/(name+suffix), before/(name+suffix))
        freeze_response(PRIVATE[name]/'compile.rsp', PRIVATE[name], stage/'morphos-inputs'/name)
        assert (root/'inputs'/(name+'.candidate')).stat().st_size < (root/'inputs'/(name+'.baseline')).stat().st_size
    manifest = dict(source=git_state(REPO), compilerProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/version-loop-phi-spills-20261017.json'),
        files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()])
    save(stage/'freeze.json', manifest)


def vectors(stage):
    for item in read(stage/'freeze.json')['files']: assert identity(item['path']) == item
    for name in COMMANDS:
        assert (stage/'default-build/C'/name).read_bytes() == (stage/'shipping/candidate/C'/name).read_bytes()
    shipping = qualify(stage/'shipping/baseline', stage/'shipping/candidate', stage/'vector-runner/CopperOS.Commands.NativeExecution.dll', COMMANDS)
    def one(name):
        dest = stage/'morphos'/name/'candidate'; (dest/'C').mkdir(parents=True)
        lines = [l for l in (stage/'morphos-inputs'/name/'compile.rsp').read_text(encoding='utf-8-sig').splitlines() if not l.startswith('output=')]
        assert any('SimplifyLoopPhis' in l for l in lines)
        image = dest/'C'/name; lines += ['output='+str(image)]
        response = dest/'compile.rsp'; response.write_text('\n'.join(lines)+'\n', encoding='utf-8')
        assert run(['dotnet', str(stage/'compiler/CopperSharp.Compiler.Cli.dll'), '@'+str(response)], dest/'compile.log') == 0
        suite = 'morphos320-'+name.lower()+'-native-entry-vector-fixture'
        checks = qualify(stage/'morphos'/name/'baseline', dest, stage/'vector-runner/CopperOS.Commands.NativeExecution.dll', [name], {name: [suite]})
        return dict(command=name, image=identity(image), response=identity(response), checks=checks, **parse_map(Path(str(image)+'.map')))
    with ThreadPoolExecutor(max_workers=2) as pool: private = list(pool.map(one, COMMANDS))
    save(stage/'vectors.json', dict(shipping=shipping, morphos=private))
    assert all(r['qualified'] for r in shipping)
    assert all(r['qualified'] for row in private for r in row['checks'])


def native(stage):
    for item in read(stage/'freeze.json')['files']: assert identity(item['path']) == item
    rom = Path(os.environ['COPPERSHARP_KICKSTART31_ROM']).resolve()
    assert identity(rom)['sha256'] == '8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbAe447A8000c5ee'.lower()
    save(stage/'rom.json', identity(rom))
    for name in COMMANDS:
        root = stage/'native'/name; tools = REPO/'tools/Commands'/(name+'OsQualification')
        host = root/'host'/('CopperOS.'+name+'Qualification.Host.dll')
        assert run(['dotnet', str(host), '--bind', str(root/'runner'), str(root/'runner-source-binding.json')], root/'bind.log') == 0
        assert run([sys_python(), str(tools/'cases.py'), str(root)], root/'cases.log') == 0
        if name == 'Search':
            rows = read(root/'cases.json'); assert len(rows) == 60
            save(root/'cases-part1.json', rows[:30]); save(root/'cases-part2.json', rows[:4]+rows[30:])
    jobs = [('List', cpu, batch) for cpu in ['68000', '68020', '68040'] for batch in ['part1', 'part2', 'part3', 'repeat']]
    jobs += [('Search', cpu, batch) for cpu in ['68000', '68020', '68040'] for batch in ['part1', 'part2']]
    jobs.insert(0, ('List', '68000', 'smoke'))
    def one(job):
        name, cpu, batch = job; root = stage/'native'/name; tools = REPO/'tools/Commands'/(name+'OsQualification')
        log = root/(batch+'-'+cpu+'.log')
        host = root/'host'/('CopperOS.'+name+'Qualification.Host.dll')
        assert run(['dotnet', str(host), str(root), str(rom), cpu, str(root/('cases-'+batch+'.json')), batch], log) == 0, job
        assert run([sys_python(), str(tools/'verify.py'), str(root), batch, cpu], Path(str(log)+'.verify')) == 0, job
        return dict(command=name, cpu=cpu, batch=batch, qualification=read(root/'runs'/(batch+'-'+cpu)/'qualification.json'))
    with ThreadPoolExecutor(max_workers=2) as pool: rows = list(pool.map(one, jobs))
    save(stage/'native.json', rows)


def sys_python():
    import sys
    return sys.executable


def verifier(name):
    path = REPO/'tools/Commands'/(name+'OsQualification')/'verify.py'
    spec = importlib.util.spec_from_file_location(name+'NativeAuditor', path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module.verify


def check(stage):
    frozen = read(stage/'freeze.json')
    for item in frozen['files']: assert identity(item['path']) == item
    vectors = read(stage/'vectors.json')
    assert all(r['qualified'] for r in vectors['shipping'])
    assert all(r['qualified'] for row in vectors['morphos'] for r in row['checks'])
    matrix = []
    bindings = {}
    independent = {}
    for name in COMMANDS:
        root = stage/'native'/name; audit = verifier(name)
        bindings[name] = read(root/'runner-source-binding.json')
        for row in bindings[name][:2]: assert all(d['matches'] for d in row['documents'])
        for path in (root/'host').glob('*.dll'):
            original = root/'runner'/path.name
            if original.exists(): assert path.read_bytes() == original.read_bytes()
        for cpu in ['68000', '68020', '68040']:
            batches = ['part1', 'part2', 'part3', 'repeat'] if name == 'List' else ['part1', 'part2']
            contracts = read(root/'contracts.json') if name == 'List' else read(root/'cases.json')
            names = set(); stacks = set(); count = 0
            for batch in batches:
                row = audit(root, batch, cpu); assert row['passed'], row['failures']
                result = row['result']; assert result['Frames'] <= 3000 and result['ExecutionMode'] == 'Interpreter'
                assert result['LaunchStatus'] == result['ReturnValue'] == 0 and result['StopReason'] == 'ProgramReturned'
                assert result['RomSha256'].lower() == read(stage/'rom.json')['sha256']
                assert result['HunkSha256'].lower() == identity(root/'guest'/(name+'Qualification'))['sha256']
                directory = root/'runs'/(batch+'-'+cpu); observed = read(directory/'observations.json')
                for key, file in [(name.upper()+'B', name+'.baseline'), (name.upper()+'C', name+'.candidate'), ('TEXT.IN', 'TEXT.IN'), ('LONG.IN', 'LONG.IN')]:
                    assert observed['inputs'][key] == identity(root/'inputs'/file)['sha256']
                assert observed['inputs']['CASES.FIX'] == identity(directory/'CASES.FIX')['sha256']
                names.update(o['Name'] for o in observed['observations']); stacks.update(o['Stack'] for o in observed['observations'])
                count += row['invocations']; matrix.append(dict(command=name, **row))
            assert names == {c['Name'] for c in contracts} and stacks == {4096, 8192}
            assert count == (100 if name == 'List' else 64)
        controls = read(root/'negative-controls.json'); assert len(controls) == 4 and all(r['rejected'] for r in controls)
        independent[name] = read(root/'winuae/results.json')
        assert {r['cpu'] for r in independent[name]} == {'68000', '68020', '68040'}
        for row in independent[name]:
            assert row['completed'] and not row['jit'] and row['stackBytes'] == 4096
            assert row['romSha256'] == read(stage/'rom.json')['sha256']
            sysroot = root/'winuae'/row['cpu']/'sys'
            for label, suffix in [('baseline', 'B'), ('candidate', 'C')]:
                assert (sysroot/'C'/(name+suffix)).read_bytes() == (root/'inputs'/(name+'.'+label)).read_bytes()
            if name == 'List':
                assert row['passed']
                expected = 'TEXT.IN'.ljust(24)+' '+'19'.rjust(7)+' ----rwed\n'
                returns = ''.join(label+'-'+case+' 0 0\n' for label in ['baseline', 'candidate'] for case in ['quick', 'metadata', 'to'])
                assert row['outputs']['returns'] == returns
                for label in ['baseline', 'candidate']:
                    for case, text in [('quick', 'TEXT.IN\n'), ('metadata', expected), ('to', expected), ('stdout', '')]:
                        assert row['outputs'][label+'-'+case] == text
                for key, text in row['outputs'].items():
                    assert (sysroot/(key+'.txt')).read_bytes().decode('latin1') == text
            else:
                assert row['outputsAgree'] and row['numberedOutputCorrect']
                assert row['baseline'] == row['candidate'] == '     2> Needle\n'
                assert row['returns'] == 'baseline 0 0\ncandidate 0 0\n'
                for key in ['baseline', 'candidate', 'returns']:
                    assert (sysroot/(key+'.txt')).read_bytes().decode('latin1') == row[key]
    assert verifier('List')(stage/'native/List', 'smoke', '68000')['passed']
    buffering = read(stage/'controls/buffering-control.json')
    for key in ['runner', 'image', 'receipt']: assert identity(buffering[key]['path']) == buffering[key]
    assert buffering['runner'] == identity(stage/'vector-runner/CopperOS.Commands.NativeExecution.dll')
    rejected = read(buffering['receipt']['path'])
    assert rejected['status'] == 'failed' and 'without flushing' in rejected['failure']
    for name in COMMANDS:
        candidate = stage/'shipping/candidate/C'/name
        assert (stage/'default-build/C'/name).read_bytes() == candidate.read_bytes()
        assert candidate.stat().st_size < (stage/'installed/C'/name).stat().st_size
    result = dict(passed=True, matrix=matrix, vectors=vectors, sourceBindings=bindings, winuae=independent,
        negativeControls={name: read(stage/'native'/name/'negative-controls.json') for name in COMMANDS}, bufferingControl=buffering)
    save(stage/'acceptance.json', result)
    return result


def publish(stage):
    check(stage)
    final = stage/'accepted/C'; assert not final.exists()
    shutil.copytree(stage/'installed/C', final)
    for name in COMMANDS: shutil.copy2(stage/'default-build/C'/name, final/name)
    names = {p.name for p in final.iterdir()}
    assert len(names) == 61 and names == {p.name for p in (stage/'installed/C').iterdir()}
    for name in names: assert (REPO/'out/C'/name).read_bytes() == (stage/'installed/C'/name).read_bytes(), name
    for name in COMMANDS: shutil.copy2(final/name, REPO/'out/C'/name)
    for name in names: assert (REPO/'out/C'/name).read_bytes() == (final/name).read_bytes(), name
    assert {name for name in names if (final/name).read_bytes() != (stage/'installed/C'/name).read_bytes()} == set(COMMANDS)
    save(stage/'publication.json', dict(inventory=[identity(p) for p in sorted(final.iterdir())],
        beforeBytes=sum(p.stat().st_size for p in (stage/'installed/C').iterdir()), afterBytes=sum(p.stat().st_size for p in final.iterdir()),
        images={name: identity(REPO/'out/C'/name) for name in COMMANDS}))


def report(stage):
    accepted = check(stage)
    publication = read(stage/'publication.json')
    for name in COMMANDS:
        assert identity(REPO/'out/C'/name) == publication['images'][name]
        destination = stage/'installation-inputs'/name
        project = REPO/'src/Commands'/name
        if not destination.exists():
            freeze_response(project/'obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp', project, destination)
            shutil.copy2(project/'bin/Release/net10.0/amiga-m68k/publish'/(name+'.map'), destination/(name+'.map'))
        replay = stage/'installation-replay/C'/name
        replay.parent.mkdir(parents=True, exist_ok=True)
        response = replay.with_suffix('.rsp')
        response.write_text((destination/'compile.rsp').read_text(encoding='utf-8')+'output='+str(replay)+'\n', encoding='utf-8')
        assert run(['dotnet', str(stage/'compiler/CopperSharp.Compiler.Cli.dll'), '@'+str(response)], replay.with_suffix('.log')) == 0
        assert replay.read_bytes() == (REPO/'out/C'/name).read_bytes()
    def compact(value):
        if isinstance(value, dict): return {k: compact(v) for k, v in value.items() if k != 'PayloadOutput'}
        if isinstance(value, list): return [compact(v) for v in value]
        return value
    result = dict(date='2026-10-17', stage=str(stage), compilerFixCommit='69d01d1',
        compilerProof=read(REPO/'docs/Commands/Workbench31MorphOS320/version-loop-phi-spills-20261017.json'),
        freeze=read(stage/'freeze.json'), acceptance=compact(accepted), publication=publication,
        nativeRuntime=read(stage/'runtime.json'), rom=read(stage/'rom.json'),
        maps={name: {label: parse_map(stage/'shipping'/label/'C'/(name+'.map')) for label in ['baseline', 'candidate']} for name in COMMANDS},
        tooling=[identity(p) for p in [Path(__file__), REPO/'tools/Commands/SearchOsQualification/verify.py',
            REPO/'tools/Commands/SearchOsQualification/control-auditor.py', REPO/'tools/Commands/SearchOsQualification/control-buffering.py']],
        evidence=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()])
    save(REPO/'docs/Commands/Workbench31MorphOS320/list-search-fixed-compiler-20261017.json', result)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode', choices=['prepare', 'vectors', 'native', 'check', 'publish', 'report'])
    parser.add_argument('--stage', type=Path, required=True)
    args = parser.parse_args()
    globals()[args.mode](args.stage.resolve())
