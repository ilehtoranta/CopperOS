"""Freeze accepted command inputs and measure independently selected compiler passes.

Never rebuild managed command assemblies during a compiler comparison. Publication
is deliberately separate from compilation and requires successful qualification.
"""
import argparse
import json
import shutil
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

from measure_c import identity, git_state, parse_map
from qualify_sizes import PASSES, run, qualify, compile_stage as compile_peephole_stage

REPO = Path(__file__).resolve().parents[2]
GENERATED_PASSES = dict(A='RemoveRedundantTransport', B='CompactGuestMemory', C='NarrowOperations',
                        D='EliminateRedundantInitialization', E='SizeFirstCosts', F='InlineMemoryHelpers', G='ShareArithmeticCores')


def freeze(stage, compiler_root):
    if stage.exists():
        raise RuntimeError('Freeze destination already exists; preserve the original receipt.')
    accepted = REPO / 'artifacts/command-executable-size-20261004/final'
    execute = REPO / 'artifacts/execute-optimize-20261004/shipping'
    names = sorted(p.name for p in (REPO / 'out/C').iterdir() if p.is_file() and not p.suffix)
    rows = []
    for name in names:
        source = execute if name == 'Execute' else accepted
        image = source / 'C' / name
        if image.read_bytes() != (REPO / 'out/C' / name).read_bytes():
            raise RuntimeError(f'Accepted image mismatch: {name}')
        destination = stage / 'inputs' / name
        shutil.copytree(source / 'inputs' / name, destination)
        lines = []
        for line in (destination / 'compile.rsp').read_text(encoding='utf-8-sig').splitlines():
            if line.startswith(('input=', 'managed-assembly=')):
                key, value = line.split('=', 1)
                line = key + '=' + str((destination / Path(value).name).resolve())
            lines.append(line)
        (destination / 'compile.rsp').write_text('\n'.join(lines) + '\n', encoding='utf-8')
        (stage / 'accepted/C').mkdir(parents=True, exist_ok=True)
        shutil.copy2(image, stage / 'accepted/C' / name)
        shutil.copy2(source / 'maps' / (name + '.map'), stage / 'accepted/C' / (name + '.map'))
        rows.append({'command': name, 'image': identity(image),
                     'inputs': [identity(p) for p in sorted(destination.iterdir()) if p.is_file()],
                     'flags': lines})
    shutil.copytree(accepted / 'compiler', stage / 'baseline-compiler')
    report = {'source': git_state(REPO), 'compiler': git_state(compiler_root),
              'compilerFiles': [identity(p) for p in sorted((stage / 'baseline-compiler').glob('*.dll'))],
              'totalBytes': sum(r['image']['bytes'] for r in rows), 'commands': rows}
    (stage / 'freeze.json').write_text(json.dumps(report, indent=2) + '\n')
    print(f"Frozen {len(rows)} commands: {report['totalBytes']} bytes", flush=True)


def compile_stage(frozen, compiler, stage, names, extra):
    receipt = json.loads((frozen / 'freeze.json').read_text())
    recorded = {r['command']: r for r in receipt['commands']}
    rows = []
    (stage / 'C').mkdir(parents=True, exist_ok=True)
    compiler_files = stage / 'compiler'
    if not compiler_files.exists():
        shutil.copytree(compiler.parent, compiler_files)
    elif identity(compiler_files / compiler.name)['sha256'] != identity(compiler)['sha256']:
        raise RuntimeError('Compiler changed; use a fresh experiment staging directory.')
    compiler_root = compiler.parents[4] if len(compiler.parents) > 4 else None
    compiler_receipt = {'assembly': identity(compiler), 'files': [identity(p) for p in sorted(compiler_files.glob('*.dll'))]}
    if compiler.parent.name == 'net10.0' and compiler_root and (compiler_root / '.git').exists():
        compiler_receipt['source'] = git_state(compiler_root)
    compiler = compiler_files / compiler.name
    for name in names:
        for item in recorded[name]['inputs']:
            if identity(item['path']) != item:
                raise RuntimeError(f'Frozen input changed: {item["path"]}')
        lines = [line for line in recorded[name]['flags']
                 if not line.startswith(('output=', 'compatibility-report='))]
        policy = next((line.split('=', 1)[1] for line in lines if line.startswith('code-size-optimizations=')), 'off')
        selected = next((line.split('=', 1)[1].split(',') for line in lines if line.startswith('code-size-passes=')),
                        list(PASSES) if policy == 'on' else [])
        if extra:
            selected += extra
            lines = [line for line in lines if not line.startswith(('code-size-optimizations=', 'code-size-passes='))]
            lines += ['code-size-optimizations=on', 'code-size-passes=' + ','.join(dict.fromkeys(selected))]
        image = (stage / 'C' / name).resolve()
        lines.append('output=' + str(image))
        response = stage / (name + '.rsp')
        response.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        code = run(['dotnet', str(compiler), '@' + str(response.resolve())], stage / (name + '.compile.log'))
        row = {'command': name, 'compileExit': code, 'passes': selected,
               'response': identity(response)}
        if not code:
            row.update(image=identity(image), **parse_map(Path(str(image) + '.map')))
        rows.append(row)
        (stage / 'compilation.json').write_text(json.dumps({'compiler': compiler_receipt, 'commands': rows}, indent=2) + '\n')
        print(f'{name}: {row.get("image", {}).get("bytes", "FAILED")}', flush=True)
        if code:
            raise RuntimeError(f'{name} failed; see compilation log')
    if not extra:
        mismatches = [name for name in names if (stage / 'C' / name).read_bytes() != (frozen / 'accepted/C' / name).read_bytes()]
        if mismatches:
            raise RuntimeError(f'Baseline reproduction differs: {mismatches}')
        print('Baseline byte identity verified', flush=True)
    return rows


def select_stage(frozen, compiler, stage, matrix_root, names, runner):
    experiments = json.loads((matrix_root / 'matrix.json').read_text())
    compiler_files = stage / 'compiler'
    if compiler_files.exists():
        raise RuntimeError('Selection destination already exists; preserve it and use a new destination.')
    shutil.copytree(compiler.parent, compiler_files)
    compiler_receipt = {'source': git_state(compiler.parents[4]),
                        'files': [identity(p) for p in sorted(compiler_files.glob('*.dll'))]}
    (stage / 'compiler-receipt.json').write_text(json.dumps(compiler_receipt, indent=2) + '\n')
    compiler = (compiler_files / compiler.name).resolve()
    selected = []
    all_compilations = []
    for name in names:
        candidates = [(experiment, row) for experiment in experiments for row in experiment['rows']
                      if row['command'] == name and row['accept']]
        candidates.sort(key=lambda pair: (pair[1]['afterBytes'], len(pair[0]['passes'])))
        seen = set()
        accepted = None
        trials = []
        def save_trial(compiled, check=None):
            destination = stage / 'trials' / name / str(len(trials) + 1)
            destination.mkdir(parents=True)
            for source in (stage / 'C').glob(name + '*'):
                if source.name == name or source.name.startswith(name + '.'):
                    shutil.copy2(source, destination / source.name)
            for suffix in ('.rsp', '.compile.log'):
                shutil.copy2(stage / (name + suffix), destination / (name + suffix))
            execution = stage / 'execution'
            if check and execution.exists():
                for source in execution.glob(name + '.*'):
                    shutil.copy2(source, destination / source.name)
            trial = {'measurement': compiled, 'qualification': check, 'directory': str(destination)}
            (destination / 'receipt.json').write_text(json.dumps(trial, indent=2) + '\n')
            trials.append(trial)
        for experiment, row in candidates:
            key = tuple(experiment['passes'])
            if key in seen:
                continue
            seen.add(key)
            compiled = compile_stage(frozen, compiler, stage, [name], list(key))[0]
            if compiled['image']['bytes'] >= row['beforeBytes']:
                save_trial(compiled)
                continue
            check = qualify(frozen / 'baseline', stage, runner, [name])[0]
            save_trial(compiled, check)
            if check['accept']:
                accepted = {**check, 'passes': list(key), 'selectionStage': experiment['stage'],
                            'image': compiled['image'], 'measurement': compiled}
                all_compilations.append(compiled)
                break
        if accepted is None:
            compiled = compile_stage(frozen, compiler, stage, [name], [])[0]
            all_compilations.append(compiled)
            accepted = {'command': name, 'passes': [], 'selectionStage': 'baseline', 'accept': False,
                        'beforeBytes': compiled['image']['bytes'], 'afterBytes': compiled['image']['bytes'],
                        'savedBytes': 0, 'image': compiled['image'], 'measurement': compiled}
        accepted['trials'] = trials
        selected.append(accepted)
        (stage / 'selection.json').write_text(json.dumps(selected, indent=2) + '\n')
    (stage / 'compilation.json').write_text(json.dumps({'compiler': compiler_receipt, 'commands': all_compilations}, indent=2) + '\n')
    (stage / 'qualification.json').write_text(json.dumps(selected, indent=2) + '\n')
    (stage / 'inventory.json').write_text(json.dumps([identity(stage / 'C' / name) for name in names], indent=2) + '\n')


def matrix(frozen, compiler, stage, names, runner, jobs):
    compiler_files = stage / 'compiler'
    if compiler_files.exists():
        raise RuntimeError('Matrix destination already exists; preserve its compiler and receipts.')
    shutil.copytree(compiler.parent, compiler_files)
    provenance = {'source': git_state(compiler.parents[4]),
                  'files': [identity(p) for p in sorted(compiler_files.glob('*.dll'))]}
    (stage / 'compiler-receipt.json').write_text(json.dumps(provenance, indent=2) + '\n')
    compiler = (compiler_files / compiler.name).resolve()
    # Populate the common control receipts before concurrent comparisons.
    qualify(frozen / 'baseline', frozen / 'baseline', runner, names)
    def candidate(item):
        label, extra = item
        destination = stage / label
        compile_stage(frozen, compiler, destination, names, extra)
        changed = [name for name in names if (destination / 'C' / name).read_bytes() != (frozen / 'baseline/C' / name).read_bytes()]
        rows = qualify(frozen / 'baseline', destination, runner, changed)
        return {'stage': label, 'passes': extra, 'rows': rows}
    jobs_list = [('control', [])] + [(key, [value]) for key, value in GENERATED_PASSES.items()] + [
        ('AB', list(GENERATED_PASSES.values())[:2]), ('ABC', list(GENERATED_PASSES.values())[:3]),
        ('ABCD', list(GENERATED_PASSES.values())[:4]), ('ABCDE', list(GENERATED_PASSES.values())[:5]),
        ('ABCDEF', list(GENERATED_PASSES.values())[:6]), ('combined', list(GENERATED_PASSES.values()))]
    results = []
    with ThreadPoolExecutor(max_workers=jobs) as executor:
        for row in executor.map(candidate, jobs_list):
            results.append(row)
            (stage / 'matrix.json').write_text(json.dumps(results, indent=2) + '\n')


def publish(frozen, stage):
    names = [r['command'] for r in json.loads((frozen / 'freeze.json').read_text())['commands']]
    selected = json.loads((stage / 'selection.json').read_text())
    inventory = json.loads((stage / 'inventory.json').read_text())
    if len(selected) != len(names) or {r['command'] for r in selected} != set(names):
        raise RuntimeError('Incomplete selection inventory.')
    if len(inventory) != len(names) or {Path(r['path']).name for r in inventory} != set(names):
        raise RuntimeError('Incomplete executable inventory.')
    recorded = {Path(r['path']).name: r for r in inventory}
    for row in selected:
        name = row['command']
        image = stage / 'C' / name
        if identity(image) != recorded[name] or identity(image) != row['image']:
            raise RuntimeError(f'Staged executable changed: {name}')
        baseline = frozen / 'baseline/C' / name
        if image.read_bytes() != baseline.read_bytes():
            if not row.get('accept') or not row.get('qualified') or not all(c['passed'] for c in row['checks']):
                raise RuntimeError(f'Unqualified executable: {name}')
            if image.stat().st_size >= baseline.stat().st_size:
                raise RuntimeError(f'Executable did not shrink: {name}')
    if sum(r['bytes'] for r in inventory) >= sum((frozen / 'baseline/C' / n).stat().st_size for n in names):
        raise RuntimeError('The complete selected set did not shrink.')
    target = (REPO / 'out/C').resolve()
    if target.parent != (REPO / 'out').resolve():
        raise RuntimeError('Unexpected publication directory.')
    backup = stage / 'previous-C'
    if backup.exists():
        raise RuntimeError('Publication backup already exists; preserve the receipt.')
    target.mkdir(parents=True, exist_ok=True)
    shutil.copytree(target, backup)
    for name in names:
        shutil.copy2(stage / 'C' / name, target / name)
        if identity(target / name)['sha256'] != recorded[name]['sha256']:
            raise RuntimeError(f'Published hash mismatch: {name}; previous files are preserved in {backup}')
    stale = [p for p in target.iterdir() if p.is_file() and p.name not in names]
    for path in stale:
        path.unlink()
    receipt = {'directory': str(target), 'backup': str(backup),
               'removedStaleFiles': [p.name for p in stale],
               'files': [identity(target / name) for name in names]}
    (stage / 'publication.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(f'Published and verified {len(names)} commands: {sum(r["bytes"] for r in inventory)} bytes', flush=True)


def eval_modes(frozen, final, runner, profile='workbench'):
    selection = next(r for r in json.loads((final / 'selection.json').read_text()) if r['command'] == 'Eval')
    compiler = final / 'compiler/CopperSharp.Compiler.Cli.dll'
    baseline_compiler = frozen / 'baseline-compiler/CopperSharp.Compiler.Cli.dll'
    inputs = frozen
    prefix = 'eval-modes'
    overrides = None
    input_identities = None
    if profile == 'morphos':
        inputs = frozen / 'morphos-profile-inputs'
        destination = inputs / 'inputs/Eval'
        manifest = destination / 'compile.rsp'
        input_receipt = inputs / 'freeze.json'
        if not manifest.exists():
            destination.mkdir(parents=True)
            source = REPO / 'tests/Commands.NativeRoot/bin/Release/net10.0'
            for path in source.glob('*.dll'):
                shutil.copy2(path, destination / path.name)
            lines = [line for line in (frozen / 'inputs/Eval/compile.rsp').read_text().splitlines()
                     if not line.startswith(('input=', 'entry=', 'managed-assembly='))]
            lines += ['input=' + str(destination / 'CopperOS.Commands.NativeRoot.dll'),
                      'entry=CopperOS.Commands.NativeRoot.NativeEvalEntry::Main']
            lines += ['managed-assembly=' + str(p) for p in destination.glob('*.dll')
                      if p.name != 'CopperOS.Commands.NativeRoot.dll']
            manifest.write_text('\n'.join(lines) + '\n')
        if not input_receipt.exists():
            input_receipt.write_text(json.dumps([identity(p) for p in sorted(destination.iterdir())], indent=2) + '\n')
        input_identities = json.loads(input_receipt.read_text())
        if any(identity(item['path']) != item for item in input_identities):
            raise RuntimeError('Frozen MorphOS fixture inputs changed.')
        prefix = 'morphos-eval-modes'
        overrides = {'Eval': ['eval-native-entry-vector-fixture']}
    results = []
    for mode in ('disabled', 'bounded', 'fixed-point'):
        before = frozen / prefix / mode / 'baseline'
        after = frozen / prefix / mode / 'candidate'
        compile_peephole_stage(inputs, baseline_compiler, before, ['Eval'], 'on', ','.join(PASSES), mode)
        compile_peephole_stage(inputs, compiler, after, ['Eval'], 'on', ','.join((*PASSES, *selection['passes'])), mode)
        checks = qualify(before, after, runner, ['Eval'], overrides)
        if not all(r['qualified'] for r in checks):
            raise RuntimeError(f'Eval vectors failed with {mode} peepholes.')
        results.append({'mode': mode, 'checks': checks, 'inputs': input_identities,
                        'baseline': json.loads((before / 'compilation.json').read_text()),
                        'candidate': json.loads((after / 'compilation.json').read_text())})
        (frozen / (prefix + '.json')).write_text(json.dumps(results, indent=2) + '\n')


def enable(stage, output):
    selected = json.loads((stage / 'selection.json').read_text())
    lines = ['<Project>', '  <!-- Qualified frozen-assembly selections; see the generated-code size report. -->']
    for row in selected:
        if not row['accept']:
            continue
        passes = ','.join(row['measurement']['passes'])
        lines += [f'  <PropertyGroup Condition="\'$(CopperOSCommandName)\' == \'{row["command"]}\' and \'$(CopperOSCodeSizePasses)\' == \'\'">',
                  f'    <CopperOSCodeSizePasses>{passes}</CopperOSCodeSizePasses>', '  </PropertyGroup>']
    lines.append('</Project>')
    output.write_text('\n'.join(lines) + '\n', encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=('freeze', 'compile', 'qualify', 'matrix', 'select', 'publish', 'eval-modes', 'enable'))
    parser.add_argument('--frozen', type=Path, required=True)
    parser.add_argument('--compiler-root', type=Path)
    parser.add_argument('--compiler', type=Path)
    parser.add_argument('--stage', type=Path)
    parser.add_argument('--commands')
    parser.add_argument('--passes', default='')
    parser.add_argument('--runner', type=Path)
    parser.add_argument('--jobs', type=int, default=3)
    parser.add_argument('--matrix', type=Path)
    parser.add_argument('--profile', choices=('workbench', 'morphos'), default='workbench')
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    frozen = args.frozen.resolve()
    if args.operation == 'freeze':
        freeze(frozen, args.compiler_root.resolve())
    else:
        names = args.commands.split(',') if args.commands else [r['command'] for r in json.loads((frozen / 'freeze.json').read_text())['commands']]
        if args.operation == 'compile':
            compile_stage(frozen, args.compiler.resolve(), args.stage.resolve(), names,
                          [p for p in args.passes.split(',') if p])
        elif args.operation == 'matrix':
            matrix(frozen, args.compiler.resolve(), args.stage.resolve(), names, args.runner.resolve(), args.jobs)
        elif args.operation == 'select':
            select_stage(frozen, args.compiler.resolve(), args.stage.resolve(), args.matrix.resolve(), names, args.runner.resolve())
        elif args.operation == 'publish':
            if args.commands:
                raise RuntimeError('Publication requires the complete frozen command set.')
            publish(frozen, args.stage.resolve())
        elif args.operation == 'eval-modes':
            eval_modes(frozen, args.stage.resolve(), args.runner.resolve(), args.profile)
        elif args.operation == 'enable':
            enable(args.stage.resolve(), args.output.resolve())
        else:
            qualify(frozen / 'baseline', args.stage.resolve(), args.runner.resolve(), names)
