"""Freeze, qualify and install the repaired smaller Exe2Arc shipping entry."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import shutil
import sys
import xml.etree.ElementTree as ET

from measure_c import identity, git_state, parse_map
from qualify_sizes import run, qualify

REPO = Path(__file__).resolve().parents[2]
OLD = REPO/'artifacts/receiver-rollout-v3-20261017'
SUITE = 'exe2arc-native-entry-vector-fixture'
PROBE = 'exe2arc-type-diagnostic-native-probe'


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def save(path, data): path.write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')


def project(stage, name, sources, assembly, packages=None, attributes=False, tests=False, references=None):
    folder = stage/name; folder.mkdir(parents=True)
    root = ET.Element('Project', Sdk='Microsoft.NET.Sdk'); props = ET.SubElement(root, 'PropertyGroup')
    values = dict(TargetFramework='net10.0', ImplicitUsings='enable', Nullable='enable',
                  EnableDefaultCompileItems='false', AssemblyName=assembly)
    if name == 'runner-source': values['OutputType'] = 'Exe'
    if tests: values.update(IsTestProject='true', IsPackable='false')
    for key, value in values.items(): ET.SubElement(props, key).text = value
    items = ET.SubElement(root, 'ItemGroup')
    for index, source in enumerate(sorted(set(p.resolve() for p in sources))):
        target = folder/'source'/(str(index)+'-'+source.name); target.parent.mkdir(exist_ok=True)
        shutil.copy2(source, target); ET.SubElement(items, 'Compile', Include=str(target))
    for source in references or list((stage/'dependencies').glob('*.dll')):
        item = ET.SubElement(items, 'Reference', Include=source.stem)
        ET.SubElement(item, 'HintPath').text = str(source)
    for package, version in (packages or {}).items(): ET.SubElement(items, 'PackageReference', Include=package, Version=version)
    if attributes:
        item = ET.SubElement(items, 'AssemblyAttribute', Include='System.Reflection.AssemblyMetadataAttribute')
        ET.SubElement(item, '_Parameter1').text = 'Copper68kPackageVersion'
        ET.SubElement(item, '_Parameter2').text = '1.5.1'
    if tests: ET.SubElement(items, 'Using', Include='Xunit')
    path = folder/'Build.csproj'; ET.ElementTree(root).write(path, encoding='utf-8'); return path


def prepare(stage):
    assert not stage.exists(), 'Preserve earlier evidence; use a fresh stage.'
    stage.mkdir(parents=True)
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    for label in ['baseline', 'candidate']:
        shutil.copytree(OLD/('compiler-'+label), stage/('compiler-'+label))
        target = stage/'original'/label/'C'; target.mkdir(parents=True)
        for suffix in ['', '.map']: shutil.copy2(OLD/label/'C'/('Exe2Arc'+suffix), target/('Exe2Arc'+suffix))
    deps = stage/'dependencies'; deps.mkdir()
    for source in (OLD/'inputs/Exe2Arc').glob('*.dll'):
        if source.name.startswith('CopperSharp.') or source.name == 'CopperOS.Commands.Common.dll': shutil.copy2(source, deps/source.name)
    command_sources = [p for p in (REPO/'src/Commands/Exe2Arc').rglob('*.cs')
                       if not {'bin', 'obj'}.intersection(p.relative_to(REPO/'src/Commands/Exe2Arc').parts)]
    command = project(stage, 'command-source', command_sources,
                      'CopperOS.Commands.Exe2Arc')
    assert run(['dotnet', 'build', str(command), '-c', 'Release', '-o', str(stage/'cil'), '--nologo'], stage/'command-build.log') == 0
    root = REPO/'tests/Commands.NativeExecution'
    original = root/'CopperOS.Commands.NativeExecution.csproj'
    sources = list(root.glob('*.cs'))
    sources += [root/item.attrib['Include'].replace('\\', '/') for item in ET.parse(original).findall('.//Compile')]
    sdk_references = list(deps.glob('CopperSharp.*.dll'))
    runner = project(stage, 'runner-source', sources, 'CopperOS.Commands.NativeExecution', {'Copper68k': '1.5.1'}, attributes=True, references=sdk_references)
    assert run(['dotnet', 'build', str(runner), '-c', 'Release', '-o', str(stage/'runner'), '--nologo'], stage/'runner-build.log') == 0
    sources = list((REPO/'src/Commands/Exe2Arc').glob('*.cs'))+list((REPO/'tests/Commands').glob('Exe2Arc*.cs'))
    tests = project(stage, 'unit-source', sources, 'CopperOS.Exe2Arc.Qualification.Tests',
                    {'Microsoft.NET.Test.Sdk': '17.14.1', 'xunit': '2.9.3', 'xunit.runner.visualstudio': '3.1.4'}, tests=True, references=sdk_references)
    assert run(['dotnet', 'test', str(tests), '-c', 'Release', '--nologo', '--logger', 'trx;LogFileName=components.trx', '--results-directory', str(stage/'unit-results')], stage/'unit-tests.log') == 0
    lines = (OLD/'inputs/Exe2Arc/compile.rsp').read_text(encoding='utf-8-sig').splitlines()
    output = []
    for line in lines:
        if line.startswith('input='): line = 'input='+str(stage/'cil/CopperOS.Commands.Exe2Arc.dll')
        if line.startswith('managed-assembly='): line = 'managed-assembly='+str(deps/Path(line.split('=', 1)[1]).name)
        output.append(line)
    (stage/'compile.rsp').write_text('\n'.join(output)+'\n', encoding='utf-8')
    save(stage/'freeze.json', dict(source=git_state(REPO), compilerFix='9eaa2aa',
         previousProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/receiver-classification-rollout-20261017.json'),
         files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


def validate(stage):
    frozen = read(stage/'freeze.json')
    for item in frozen['files']: assert identity(item['path']) == item, item['path']
    previous = frozen['previousProof']; assert identity(previous['path']) == previous
    proof = read(Path(previous['path']))
    for item in proof['sourceFreeze']['files']:
        path = Path(item['path'])
        relative = path.relative_to(OLD)
        if relative.parts[0] in ['compiler-baseline', 'compiler-candidate']:
            actual = identity(stage/relative)
            assert (actual['bytes'], actual['sha256']) == (item['bytes'], item['sha256'])


def compile(stage):
    validate(stage)
    for label in ['baseline', 'candidate']:
        root = stage/'repaired'/label; (root/'C').mkdir(parents=True)
        image = root/'C/Exe2Arc'; response = root/'compile.rsp'
        lines = (stage/'compile.rsp').read_text(encoding='utf-8').splitlines()+['output='+str(image)]
        response.write_text('\n'.join(lines)+'\n', encoding='utf-8')
        assert run(['dotnet', str(stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll'), '@'+str(response)], root/'compile.log') == 0
        save(root/'measurement.json', dict(image=identity(image), response=identity(response), **parse_map(Path(str(image)+'.map'))))


def vectors(stage):
    validate(stage)
    runner = stage/'runner/CopperOS.Commands.NativeExecution.dll'
    rows = qualify(stage/'repaired/baseline', stage/'repaired/candidate', runner, ['Exe2Arc'])
    save(stage/'vectors.json', rows)
    assert rows[0]['qualified']
    # Reintroduce the embedded-NUL defect in literal data only. Keep all HUNK
    # sizes, code and relocations intact so it is an independent output check.
    corrected = stage/'repaired/candidate/C/Exe2Arc'
    raw = corrected.read_bytes(); marker = b'\r\x1b[K'
    assert 3 <= raw.count(marker) <= 4
    mutant = stage/'nul-literal-control'; mutant.write_bytes(raw.replace(marker, b'\r\0[K'))
    shutil.copy2(str(corrected)+'.map', str(mutant)+'.map')
    save(stage/'nul-literal-control.json', dict(source=identity(corrected), mutant=identity(mutant), replacements=raw.count(marker)))
    controls = []
    for cpu in ['68000', '68020', '68040']:
        for suite, expected in [(SUITE, 'extract-zip'), (PROBE, 'unknown-type')]:
            report = stage/f'original-{cpu}-{suite}.json'
            code = run(['dotnet', str(runner), str(stage/'original/candidate/C/Exe2Arc'), cpu, str(report), suite], Path(str(report)+'.log'))
            receipt = read(report)
            assert code != 0 and receipt['status'] == 'failed' and receipt['lastFailure']['id'] == expected
            controls.append(dict(cpu=cpu, suite=suite, expectedCase=expected, rejected=True, receipt=identity(report)))
        report = stage/f'nul-literal-{cpu}.json'
        code = run(['dotnet', str(runner), str(mutant), cpu, str(report), PROBE], Path(str(report)+'.log'))
        receipt = read(report)
        assert code != 0 and receipt['status'] == 'failed' and receipt['lastFailure']['id'] == 'unknown-type' and 'stdout differs' in receipt['failure']
        controls.append(dict(cpu=cpu, suite=PROBE, expectedCase='embedded-nul-diagnostic', rejected=True, receipt=identity(report)))
    save(stage/'negative-controls.json', controls)


def report(stage):
    validate(stage)
    checks = read(stage/'vectors.json')[0]; assert checks['qualified'] and checks['accept']
    controls = read(stage/'negative-controls.json'); assert len(controls) == 9 and all(c['rejected'] for c in controls)
    before = identity(stage/'installed/C/Exe2Arc'); after = identity(stage/'repaired/candidate/C/Exe2Arc')
    assert after['bytes'] < before['bytes']
    trx = next((stage/'unit-results').glob('*.trx'))
    counts = ET.parse(trx).find('.//{*}Counters').attrib
    assert int(counts['executed']) > 0 and int(counts['failed']) == 0 and int(counts['passed']) == int(counts['executed'])
    before_total = sum(p.stat().st_size for p in (stage/'installed/C').iterdir())
    data = dict(freeze=read(stage/'freeze.json'), installedBefore=before, candidate=after,
                compilerComparisons={label: read(stage/'repaired'/label/'measurement.json') for label in ['baseline', 'candidate']},
                qualification=checks, negativeControls=controls,
                receipts=[identity(p) for p in sorted(stage.rglob('*.json')) if 'execution' in p.parts or p.name.startswith('original-')],
                componentTests=[identity(p) for p in (stage/'unit-results').glob('*.trx')],
                componentTestCounts=counts, totalBefore=before_total,
                totalAfter=before_total-before['bytes']+after['bytes'],
                nulLiteralControl=read(stage/'nul-literal-control.json'),
                tools=[identity(Path(__file__)), identity(REPO/'tools/Commands/qualify_sizes.py')])
    for value in data['compilerComparisons'].values():
        value['records'] = {k: v for k, v in value['records'].items() if k not in ['MACHINE-OP', 'ALLOCATION', 'FRAME-SLOT']}
    if (stage/'installed-inventory.json').exists(): data['installedInventory'] = read(stage/'installed-inventory.json')
    target = REPO/'docs/Commands/Workbench31MorphOS320/exe2arc-runtime-qualification-20261017'
    save(target.with_suffix('.json'), data)
    lines = ['# Exe2Arc runtime qualification — 17 October 2026', '',
             f"Shipping executable: **{before['bytes']:,} → {after['bytes']:,} bytes**, saving **{before['bytes']-after['bytes']:,} bytes**. Both repaired-source compiler images pass the supplied-vector suite on 68000, 68020 and 68040; the receiver-fixed compiler saves a further {checks['savedBytes']} bytes on identical repaired CIL.", '',
             '## Defects exposed and corrected', '',
             '- ZIP scanner used seek mode 2 for end-of-file. DOS `OffsetEnd` is 1; use the SDK constant. The host test adapter had also accepted the incorrect mode and now rejects it.',
             '- Unknown-type and typed-no-match diagnostics passed the type string as the `VPrintf` LONG argument vector. They now use `PutStr` directly. The strict fixture rejects the old binary for reading its first string bytes as a guest pointer.', '',
             '- C-style `\\033` escapes in C# diagnostic literals introduced embedded NULs, truncating the messages. They now use `\\u001b`. A literal-only mutant independently reintroduces this defect and is rejected on all three CPUs.', '',
             'The previously proposed 14,680-byte image is rejected. The replacement is built from repaired sources. Six negative controls reject the old image on the seek/argument-vector defects; three additional controls reject embedded-NUL diagnostics. Full original-image output equivalence cannot be claimed: these paths were broken before this repair.', '',
             '## Runtime matrix', '', '| CPU | Invocations per image | Instructions control → fixed | Peak stack bytes control → fixed |', '|---|---:|---:|---:|']
    for c in checks['checks']: lines.append(f"| {c['cpu']} | {c['invocations']} | {c['instructionsBefore']:,} → {c['instructionsAfter']:,} | {c['peakStackBefore']} → {c['peakStackAfter']} |")
    lines += ['', 'The suite executes the actual shipping entry through one protected, relocated resident HUNK image. It checks inert ZIP/ACE/RAR/CAB/ARJ/LhA/LZH payloads and auto-detection, output bytes and naming, exact parser/workspace/FIB allocation ownership and release, close-before-delete order, output/return/IoErr, deliberately poisoned cleanup errors, register/CCR clobbers at public calls, 4 KiB stack bounds and guards, Workbench message ownership, repeated calls and instruction-level interleaving. An 8 KiB peer stack is also exercised. ZIP Ctrl-C cases preserve the existing partial-output/count contract; other extractors retain their existing cancellation behavior.', '',
              f"All {counts['passed']} portable component tests pass. The 61-command installed total changes from {before_total:,} to {before_total-before['bytes']+after['bytes']:,} bytes. Instruction totals fall by {sum(c['instructionsBefore']-c['instructionsAfter'] for c in checks['checks']):,} across the measured matrix, with no peak-stack increase.", '',
              'Exe2Arc has a MorphOS-derived implementation only; no PPC executable-size comparison or separate Workbench command profile is claimed. Workbench launch handling is included. Supplied DOS parsing and in-memory file vectors are used: this does not qualify original DOS/MorphOS image parity, filesystem handlers, original diagnostic text from PrintFault, or CopperScreen execution.', '',
              f"Runtime: {checks['checks'][0]['coreVersion']}, SHA-256 `{checks['checks'][0]['coreSha256']}`. Compiler payloads are the retained clean `69d01d1` control and the receiver-classification fix committed as `9eaa2aa`. Flags and frozen SDK/CIL/source hashes are in the JSON evidence. Exe2Arc size-policy settings are unchanged.", '',
              '## Reproduction', '',
              f'Run `tools/Commands/qualify_exe2arc_runtime.py` modes `prepare`, `compile`, `vectors`, `publish`, `report`, with `--stage <fresh-absolute-directory>`. The complete installed inventory is preserved and hash-checked; only Exe2Arc is updated. Raw evidence for this run is under `{stage.relative_to(REPO).as_posix()}`. Earlier fixture/build iterations are preserved under the preceding Exe2Arc runtime stages. The initial 68040 fixture failure was a longword fetch starting at a half-line boundary before a library stub; the fixture now maps that bounded instruction-fetch span, retaining data-read, write and executable-PC restrictions.', '']
    target.with_suffix('.md').write_text('\n'.join(lines), encoding='utf-8')


def publish(stage):
    report(stage)
    before = stage/'installed/C'; destination = stage/'accepted/C'; shutil.copytree(before, destination)
    assert sorted(p.name for p in before.iterdir()) == sorted(p.name for p in (REPO/'out/C').iterdir())
    for source in (REPO/'src/Commands/Exe2Arc').rglob('*.cs'):
        if {'bin', 'obj'}.intersection(source.relative_to(REPO/'src/Commands/Exe2Arc').parts): continue
        snapshot = list((stage/'command-source/source').glob('*-'+source.name))
        assert len(snapshot) == 1 and snapshot[0].read_bytes() == source.read_bytes(), source
    for file in before.iterdir(): assert (REPO/'out/C'/file.name).read_bytes() == file.read_bytes()
    shutil.copy2(stage/'repaired/candidate/C/Exe2Arc', destination/'Exe2Arc')
    assert len(list(destination.iterdir())) == len(list(before.iterdir())) == 61
    save(stage/'accepted-inventory.json', [identity(p) for p in sorted(destination.iterdir())])
    shutil.copy2(destination/'Exe2Arc', REPO/'out/C/Exe2Arc')
    assert all((REPO/'out/C'/p.name).read_bytes() == p.read_bytes() for p in destination.iterdir())
    save(stage/'installed-inventory.json', [identity(p) for p in sorted((REPO/'out/C').iterdir())])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode', choices=['prepare', 'compile', 'vectors', 'report', 'publish'])
    parser.add_argument('--stage', type=Path, required=True); args = parser.parse_args()
    globals()[args.mode](args.stage.resolve())
