"""Recheck the retained images and freeze fresh correctness-checkpoint inputs."""
import argparse
import json
import shutil
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

from measure_c import identity, git_state, parse_map
from qualify_sizes import run, receipt, suites

REPO = Path(__file__).resolve().parents[2]


def freeze_response(response, source_directory, destination):
    destination.mkdir(parents=True)
    lines = []
    for line in response.read_text(encoding='utf-8-sig').splitlines():
        if line.startswith(('output=', 'compatibility-report=')):
            continue
        if line.startswith(('input=', 'managed-assembly=')):
            key, value = line.split('=', 1)
            path = Path(value)
            if not path.is_absolute():
                path = source_directory / path
            target = destination / path.name
            shutil.copy2(path, target)
            line = key + '=' + str(target.resolve())
        lines.append(line)
    target = destination / 'compile.rsp'
    target.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    return [identity(p) for p in sorted(destination.iterdir())]


def counts(path, expected_failures=0):
    values = ET.parse(path).find('.//{*}Counters').attrib
    if int(values['failed']) != expected_failures or int(values['executed']) == 0:
        raise RuntimeError('Unexpected test result: ' + str(path))
    return {'receipt': identity(path), 'counts': values}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--stage', type=Path, required=True)
    parser.add_argument('--compiler-root', type=Path, required=True)
    args = parser.parse_args()
    stage, compiler_root = args.stage.resolve(), args.compiler_root.resolve()
    compiler = compiler_root / 'Compiler.Cli/bin/Release/net10.0'
    shutil.copytree(compiler, stage / 'compiler-corrected')
    cli = stage / 'compiler-corrected/CopperSharp.Compiler.Cli.dll'
    runner_source = REPO / 'tests/Commands.NativeExecution/bin/Release/net10.0'
    shutil.copytree(runner_source, stage / 'runner')
    runner = stage / 'runner/CopperOS.Commands.NativeExecution.dll'
    historical = json.loads((REPO / 'docs/Commands/Workbench31MorphOS320/vararg-format-audit-20261008.json').read_text())
    installed = []
    for row in historical['commands']:
        actual = identity(REPO / 'out/C' / row['command'])
        if actual['sha256'] != row['after']['sha256']:
            raise RuntimeError('Installed image differs: ' + row['command'])
        installed.append(actual)
    commands = []
    supported = suites()
    for name in ('Lock', 'List', 'MakeDir', 'Search'):
        source = REPO / 'src/Commands' / name
        inputs = stage / 'inputs' / name
        frozen = freeze_response(source / 'obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp', source, inputs)
        image = stage / 'fresh/C' / name
        if image.read_bytes() != (REPO / 'out/C' / name).read_bytes():
            raise RuntimeError('Fresh image differs: ' + name)
        shutil.copy2(source / 'bin/Release/net10.0/amiga-m68k/publish' / (name + '.map'), str(image) + '.map')
        cases = [receipt(stage / 'fresh', name, cpu, supported[name][0], runner)
                 for cpu in ('68000', '68020', '68040')]
        if any(r['status'] != 'passed' for r in cases):
            raise RuntimeError('Native fixture failed: ' + name)
        commands.append({'command': name, 'inputs': frozen, 'image': identity(image),
                         'map': parse_map(Path(str(image) + '.map')),
                         'receipts': [identity(stage / 'fresh/execution' / f'{name}.{r["cpu"]}.{r["suite"]}.json') for r in cases]})
    # Both profiles use the same freshly frozen private-root assembly.
    root = REPO / 'tests/Commands.AddBuffersNativeRoot/bin/Release/net10.0'
    private = stage / 'private-inputs'
    private.mkdir()
    for path in root.glob('*.dll'):
        shutil.copy2(path, private / path.name)
    profiles = []
    for name, entry, suite in (
        ('Lock', 'NativeMorphOSLockEntry', 'lock-native-entry-vector-fixture'),
        ('List', 'NativeMorphOSListEntry', 'list-native-entry-vector-fixture'),
        ('MakeDir', 'NativeMorphOSMakeDirEntry', 'makedir-native-entry-vector-fixture')):
        target = stage / 'morphos' / name
        image = target / 'C' / name
        image.parent.mkdir(parents=True)
        response = target / 'compile.rsp'
        lines = ['coppersharp-response-v1', 'input=' + str(private / 'CopperOS.Commands.AddBuffersNativeRoot.dll'),
                 'entry=CopperOS.Commands.AddBuffersNativeRoot.' + entry + '::Main',
                 'output=' + str(image), 'platform=amiga', 'cpu=68000', 'fpu=disabled', 'clr=always',
                 'exceptions=yolo', 'format=hunk', 'runtime=resident', 'memory=none',
                 'peephole=fixed-point', 'symbols=off', 'code-size-optimizations=off']
        lines += ['managed-assembly=' + str(p) for p in sorted(private.glob('*.dll'))
                  if p.name != 'CopperOS.Commands.AddBuffersNativeRoot.dll']
        response.write_text('\n'.join(lines) + '\n')
        if run(['dotnet', str(cli), '@' + str(response)], target / 'compile.log'):
            raise RuntimeError('Profile compilation failed: ' + name)
        # Resolve the exact fixture name from source rather than guessing it.
        import re
        constant = {'Lock': 'LockEntrySuite', 'List': 'ListEntrySuite', 'MakeDir': 'MakeDirEntrySuite'}[name]
        source = (REPO / 'tests/Commands.NativeExecution' / (name + 'EntrySuite.cs')).read_text()
        suite = re.search(r'public const string ' + constant + r'\s*=\s*"([^"]+)"', source)[1]
        reports = [receipt(target, name, cpu, suite, runner) for cpu in ('68000', '68020', '68040')]
        if any(r['status'] != 'passed' for r in reports):
            raise RuntimeError('MorphOS fixture failed: ' + name)
        profiles.append({'command': name, 'image': identity(image),
                         'receipts': [identity(target / 'execution' / f'{name}.{r["cpu"]}.{suite}.json') for r in reports]})
    result = {'schemaVersion': 1, 'source': git_state(REPO), 'compilerSource': git_state(compiler_root),
              'sdk': subprocess.check_output(['dotnet', '--version'], cwd=REPO).decode().strip(),
              'compilerFiles': [identity(p) for p in sorted((stage / 'compiler-corrected').glob('*.dll'))],
              'runnerFiles': [identity(p) for p in sorted((stage / 'runner').glob('*.dll'))],
              'installedImages': installed, 'commands': commands, 'morphosProfiles': profiles,
              'privateInputs': [identity(p) for p in sorted(private.glob('*.dll'))],
              'tests': {name: counts(stage / 'tests' / (name + '.trx'), failures) for name, failures in
                        [('raw-before', 4), ('raw-corrected', 0), ('host-local-sdk', 0), ('compiler-focused', 0)]},
              'limits': ['Historical local receipt files and compiler worktree were absent; this is a fresh focused requalification.',
                         'Host tests use the restored local SDK because the pinned preview package is unavailable.',
                         'Native tests use modeled DOS services; original OS execution and minimum-stack qualification remain open.',
                         'No full compiler repository suite or legacy runtime execution is claimed.']}
    (stage / 'verification.json').write_text(json.dumps(result, indent=2) + '\n')
    output = REPO / 'docs/Commands/Workbench31MorphOS320/correctness-checkpoint-20261011.json'
    output.write_text(json.dumps(result, indent=2) + '\n')
    print('Verified 61 installed hashes, four fresh shipping images, and three MorphOS profiles on all three CPUs.')


if __name__ == '__main__':
    main()
