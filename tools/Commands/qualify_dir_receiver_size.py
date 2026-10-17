"""Compare identical corrected Dir assemblies with isolated receiver-classification compilers."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

from measure_c import identity, git_state, parse_map
from verify_correctness_checkpoint import freeze_response
from qualify_sizes import run, qualify
from qualify_dir_fixed import native
from qualify_dir_format_fix import validate

REPO = Path(__file__).resolve().parents[2]
OLD = REPO/'artifacts/dir-format-fix-20261017'


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def save(path, value): path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def prepare(stage):
    assert not (stage/'shipping-inputs').exists()
    freeze_response(OLD/'common-sdk-inputs/compile.rsp', OLD, stage/'shipping-inputs')
    freeze_response(OLD/'morphos-inputs/compile.rsp', OLD, stage/'morphos-inputs')
    shutil.copytree(OLD/'vector-runner', stage/'vector-runner')
    # Preserve the baseline before emitting any comparison or qualification.
    save(stage/'compiler-freeze.json', dict(source=git_state(REPO.parent/'CopperSharp68k-dir-size-20261017'),
        files=[identity(p) for folder in ['compiler-baseline', 'compiler-candidate', 'shipping-inputs', 'morphos-inputs', 'vector-runner']
               for p in sorted((stage/folder).rglob('*')) if p.is_file()]))
    result = {}
    for profile, suite in [('shipping', 'workbench31-dir-native-entry-vector-fixture'), ('morphos', 'morphos320-dir-native-entry-vector-fixture')]:
        for label in ['baseline', 'candidate']:
            dest = stage/profile/label; (dest/'C').mkdir(parents=True)
            image = dest/'C/Dir'; response = dest/'compile.rsp'
            response.write_text((stage/(profile+'-inputs')/'compile.rsp').read_text(encoding='utf-8')+'output='+str(image)+'\n', encoding='utf-8')
            assert run(['dotnet', str(stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll'), '@'+str(response)], dest/'compile.log') == 0
            if label == 'baseline': assert image.read_bytes() == (OLD/profile/'candidate/C/Dir').read_bytes()
        result[profile] = qualify(stage/profile/'baseline', stage/profile/'candidate', stage/'vector-runner/CopperOS.Commands.NativeExecution.dll', ['Dir'], {'Dir': [suite]})
    save(stage/'vectors.json', result)
    assert all(r['qualified'] and r['accept'] for rows in result.values() for r in rows)
    assert (stage/'shipping/candidate/C/Dir').stat().st_size < (stage/'installed/C/Dir').stat().st_size
    shutil.copytree(OLD/'controls', stage/'controls')
    for profile, folder in [('shipping', 'native'), ('morphos', 'native-morphos')]:
        root = stage/folder
        for name in ['host', 'guest', 'runner']: shutil.copytree(OLD/folder/name, root/name)
        (root/'inputs').mkdir()
        for name in ['TEXT.IN', 'LONG.IN']: shutil.copy2(OLD/folder/'inputs'/name, root/'inputs'/name)
        for name in ['contracts.json', 'cases-smoke.json', 'cases-part1.json', 'cases-part2.json', 'cases-repeat.json']:
            shutil.copy2(OLD/folder/name, root/name)
        for label in ['baseline', 'candidate']: shutil.copy2(stage/profile/label/'C/Dir', root/'inputs'/('Dir.'+label))
    save(stage/'native-freeze.json', [identity(p) for folder in ['native', 'native-morphos'] for p in sorted((stage/folder).rglob('*')) if p.is_file()])
    save(stage/'freeze-audit.json', read(stage/'freeze.json'))
    save(stage/'freeze.json', dict(files=read(stage/'compiler-freeze.json')['files']))


def negative_controls(stage):
    root = stage/'controls'; original = root/'abi-negative-controls.json'
    inherited = root/'abi-negative-controls-inherited.json'; assert not inherited.exists()
    shutil.move(original, inherited)
    runner = stage/'vector-runner/CopperOS.Commands.NativeExecution.dll'; records = []
    for profile, suite in [('shipping', 'workbench31-dir-native-entry-vector-fixture'), ('morphos', 'morphos320-dir-native-entry-vector-fixture')]:
        directory = root/'images'/profile/'C'; shutil.copytree(OLD/'old'/profile/'C', directory)
        image = directory/'Dir'
        for cpu in ['68000', '68020', '68040']:
            receipt = root/('rechecked-'+profile+'-'+cpu+'.json')
            assert run(['dotnet', str(runner), str(image), cpu, str(receipt), suite], receipt.with_suffix('.log')) != 0
            result = read(receipt); assert result['status'] == 'failed' and 'unknown-option' in result['failure']
            assert result['imageSha256'] == identity(image)['sha256']
            records.append(dict(profile=profile, cpu=cpu, runner=identity(runner), image=identity(image), receipt=identity(receipt)))
    save(original, records)


def integrate(stage):
    isolated = REPO.parent/'CopperSharp68k-dir-size-20261017'; primary = REPO.parent/'CopperSharp68k'
    paths = ['Compiler/Backend/CilStackAnalyzer.cs', 'Compiler/Metadata/CompilationModule.cs']
    before = [identity(primary/p) for p in paths]
    # The primary checkout already contains equivalent changes amid unrelated work.
    assert 'module.IsValueTypeMethod(method) ? CilStackValueKind.ManagedPointer' in (primary/paths[0]).read_text(encoding='utf-8')
    assert 'public bool IsValueTypeMethod(CilMethod method)' in (primary/paths[1]).read_text(encoding='utf-8')
    tests = ['Compiler.Tests/ValueTypeReceiverRootTests.cs', 'Compiler.Tests.MultiModule/ExternalStructReceiverRootProbe.cs']
    for p in tests:
        assert not (primary/p).exists(); shutil.copy2(isolated/p, primary/p)
    assert before == [identity(primary/p) for p in paths]
    patch = subprocess.check_output(['git', 'diff', '--binary', 'HEAD', '--', *paths], cwd=isolated)
    for p in tests:
        lines = (isolated/p).read_text(encoding='utf-8').splitlines()
        patch += (f'diff --git a/{p} b/{p}\nnew file mode 100644\n--- /dev/null\n+++ b/{p}\n@@ -0,0 +1,{len(lines)} @@\n'+
                  ''.join('+'+line+'\n' for line in lines)).encode('utf-8')
    target = REPO/'docs/Commands/Workbench31MorphOS320/dir-value-type-receiver-size-20261017.patch'
    target.write_bytes(patch)
    assert subprocess.run(['git', 'apply', '--cached', '--check', str(target)], cwd=primary).returncode == 0
    save(stage/'integration.json', dict(primaryCompilerSourcesUnchanged=before, addedTests=[identity(primary/p) for p in tests],
         portablePatch=identity(target), isolated=git_state(isolated)))


def test_result(path):
    root = ET.parse(path).getroot()
    counters = root.find('.//{*}Counters').attrib
    rows = [dict(name=r.attrib['testName'], outcome=r.attrib['outcome'],
                 message=r.findtext('.//{*}Message')) for r in root.findall('.//{*}UnitTestResult')]
    return dict(receipt=identity(path), counts=counters, failed=[r for r in rows if r['outcome'] == 'Failed'],
                receiverCases=[r for r in rows if 'ValueTypeReceiverRootTests.' in r['name']])


def check(stage):
    save(stage/'prepare.json', dict(files=read(stage/'freeze-audit.json')['files']+read(stage/'compiler-freeze.json')['files']))
    result = validate(stage)
    assert (stage/'default-build/C/Dir').read_bytes() == (stage/'shipping/candidate/C/Dir').read_bytes()
    assert (stage/'default-build/C/Dir').stat().st_size < (stage/'installed/C/Dir').stat().st_size
    focused = test_result(stage/'tests/focused-receiver-qualified.trx')
    baseline = test_result(stage/'tests/baseline-controls.trx')
    initial = test_result(stage/'tests/focused-receiver-size.trx')
    assert int(focused['counts']['executed']) == 900 and int(focused['counts']['failed']) == 0
    assert len(focused['receiverCases']) == 4 and all(r['outcome'] == 'Passed' for r in focused['receiverCases'])
    assert len(baseline['receiverCases']) == 4 and sum(r['outcome'] == 'Failed' for r in baseline['receiverCases']) == 3
    old_limits = {r['name'] for r in baseline['failed'] if 'ValueTypeReceiverRootTests.' not in r['name']}
    first_limits = {r['name'] for r in initial['failed'] if 'ValueTypeReceiverRootTests.' not in r['name']}
    assert old_limits == first_limits and len(old_limits) == 7
    save(stage/'acceptance.json', dict(passed=True, qualification=result, tests=dict(focused=focused, baseline=baseline, initial=initial)))
    return result, dict(focused=focused, baseline=baseline, initial=initial)


def publish(stage):
    check(stage)
    source = stage/'default-build/C/Dir'; final = stage/'accepted/C'; assert not final.exists()
    names = {p.name for p in (stage/'installed/C').iterdir() if p.is_file() and p.suffix == ''}
    assert len(names) == 61 and names == {p.name for p in (REPO/'out/C').iterdir()}
    for name in names: assert (stage/'installed/C'/name).read_bytes() == (REPO/'out/C'/name).read_bytes(), name
    final.mkdir(parents=True)
    for name in names: shutil.copy2(source if name == 'Dir' else stage/'installed/C'/name, final/name)
    shutil.copy2(final/'Dir', REPO/'out/C/Dir')
    for name in names: assert (final/name).read_bytes() == (REPO/'out/C'/name).read_bytes(), name
    assert {name for name in names if (stage/'installed/C'/name).read_bytes() != (final/name).read_bytes()} == {'Dir'}
    save(stage/'publication.json', dict(images=[identity(p) for p in sorted(final.iterdir())],
        beforeBytes=sum((stage/'installed/C'/name).stat().st_size for name in names),
        afterBytes=sum((final/name).stat().st_size for name in names), image=identity(REPO/'out/C/Dir')))


def report(stage):
    result, tests = check(stage)
    def compact(value):
        if isinstance(value, dict): return {k: compact(v) for k, v in value.items() if k != 'PayloadOutput'}
        if isinstance(value, list): return [compact(v) for v in value]
        return value
    publication = read(stage/'publication.json'); assert identity(REPO/'out/C/Dir') == publication['image']
    runtime = read(OLD/'runtime.json')
    for row in runtime: assert identity(stage/'native/host'/(row['name']+'.dll'))['sha256'] == row['sha256']
    maps = {profile: {label: parse_map(stage/profile/label/'C/Dir.map') for label in ['baseline', 'candidate']}
            for profile in ['shipping', 'morphos']}
    save(REPO/'docs/Commands/Workbench31MorphOS320/dir-value-type-receiver-size-20261017.json',
         dict(date='2026-10-17', status='accepted', qualification=compact(result), tests=tests,
              integration=read(stage/'integration.json'), publication=publication, runtime=runtime, maps=maps,
              audit=read(stage/'method-deltas.json'), freeze=read(stage/'compiler-freeze.json'),
              tooling=[identity(p) for p in [Path(__file__), REPO/'tools/Commands/investigate_dir_size.py',
                         REPO/'tools/Commands/qualify_dir_format_fix.py', REPO/'tools/Commands/qualify_dir_fixed.py']],
              evidence=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__); parser.add_argument('mode', choices=['prepare', 'native', 'negative_controls', 'integrate', 'check', 'publish', 'report'])
    parser.add_argument('--stage', type=Path, required=True); args = parser.parse_args()
    globals()[args.mode](args.stage.resolve())
