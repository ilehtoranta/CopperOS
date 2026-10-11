"""Freeze Search once, compile isolated payloads, and compare three-CPU receipts."""
import argparse
import json
import shutil
import subprocess
from pathlib import Path

from measure_c import identity, git_state, parse_map
from qualify_sizes import run, qualify
from verify_correctness_checkpoint import freeze_response

REPO = Path(__file__).resolve().parents[2]


def freeze(stage, checkpoint):
    if stage.exists():
        raise RuntimeError('Use a new freeze destination.')
    inputs = stage / 'inputs/Search'
    source = checkpoint / 'inputs/Search'
    frozen = freeze_response(source / 'compile.rsp', source, inputs)
    shutil.copytree(checkpoint / 'runner', stage / 'runner')
    shutil.copytree(checkpoint / 'private-inputs', stage / 'morphos-inputs')
    (stage / 'morphos-freeze.json').write_text(json.dumps(
        [identity(p) for p in sorted((stage / 'morphos-inputs').glob('*.dll'))], indent=2) + '\n')
    reference = stage / 'reference/C/Search'
    reference.parent.mkdir(parents=True)
    shutil.copy2(checkpoint / 'fresh/C/Search', reference)
    shutil.copy2(checkpoint / 'fresh/C/Search.map', str(reference) + '.map')
    manifest = {'source': git_state(REPO), 'inputs': frozen,
                'reference': identity(reference),
                'runnerFiles': [identity(p) for p in sorted((stage / 'runner').rglob('*')) if p.is_file()]}
    (stage / 'freeze.json').write_text(json.dumps(manifest, indent=2) + '\n')


def validate_frozen(stage):
    manifest = json.loads((stage / 'freeze.json').read_text())
    for row in manifest['inputs'] + manifest['runnerFiles'] + [manifest['reference']]:
        if identity(row['path']) != row:
            raise RuntimeError('Frozen file changed: ' + row['path'])


def add_pass(lines, selected):
    if not selected:
        return lines
    if not any(line.startswith('code-size-passes=') for line in lines):
        raise RuntimeError('An explicit baseline pass selection is required.')
    return [line + ',' + selected if line.startswith('code-size-passes=') else line for line in lines]


def compile_trial(stage, label, compiler, compiler_root, selected=None):
    validate_frozen(stage)
    target = stage / label
    if target.exists():
        raise RuntimeError('Use a new trial label.')
    payload = target / 'compiler'
    shutil.copytree(compiler.parent, payload)
    (target / 'compiler.patch').write_bytes(subprocess.check_output(
        ['git', '-C', str(compiler_root), 'diff', '--binary', 'HEAD']))
    image = target / 'C/Search'
    image.parent.mkdir()
    response = target / 'compile.rsp'
    lines = add_pass((stage / 'inputs/Search/compile.rsp').read_text().splitlines(), selected)
    lines += ['output=' + str(image.resolve())]
    response.write_text('\n'.join(lines) + '\n')
    if run(['dotnet', str(payload / compiler.name), '@' + str(response.resolve())], target / 'compile.log'):
        raise RuntimeError('Compilation failed; inspect ' + str(target / 'compile.log'))
    report = {'compilerSource': git_state(compiler_root),
              'compilerFiles': [identity(p) for p in sorted(payload.rglob('*')) if p.is_file()],
              'image': identity(image), 'response': identity(response),
              'referenceIdentical': image.read_bytes() == (stage / 'reference/C/Search').read_bytes(),
              **parse_map(Path(str(image) + '.map'))}
    (target / 'compilation.json').write_text(json.dumps(report, indent=2) + '\n')
    validate_frozen(stage)
    main = next(r for r in report['records']['ALLOCATION']
                if r['method'] == 'CopperOS.Commands.Native.NativeMorphOSSearchCommand::Run')
    print(label, report['image']['bytes'], main, flush=True)


def compile_morphos(stage, label, compiler, selected=None):
    validate_frozen(stage)
    destination = stage / 'morphos-inputs'
    if not destination.exists():
        raise RuntimeError('Frozen MorphOS inputs are missing.')
    for row in json.loads((stage / 'morphos-freeze.json').read_text()):
        if identity(row['path']) != row:
            raise RuntimeError('MorphOS frozen input changed.')
    target = stage / label
    if target.exists():
        raise RuntimeError('Use a new trial label.')
    payload = target / 'compiler'
    shutil.copytree(compiler.parent, payload)
    image = target / 'C/Search'
    image.parent.mkdir()
    flags = add_pass((stage / 'inputs/Search/compile.rsp').read_text().splitlines(), selected)
    flags = [line for line in flags if not line.startswith(('input=', 'managed-assembly=', 'entry='))]
    flags += ['input=' + str(destination / 'CopperOS.Commands.AddBuffersNativeRoot.dll'),
              'entry=CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSSearchEntry::Main',
              'output=' + str(image.resolve())]
    flags += ['managed-assembly=' + str(p) for p in sorted(destination.glob('*.dll'))
              if p.name != 'CopperOS.Commands.AddBuffersNativeRoot.dll']
    response = target / 'compile.rsp'
    response.write_text('\n'.join(flags) + '\n')
    if run(['dotnet', str(payload / compiler.name), '@' + str(response.resolve())], target / 'compile.log'):
        raise RuntimeError('MorphOS compilation failed.')
    report = {'image': identity(image), 'response': identity(response),
              'compilerFiles': [identity(p) for p in sorted(payload.rglob('*')) if p.is_file()],
              **parse_map(Path(str(image) + '.map'))}
    (target / 'compilation.json').write_text(json.dumps(report, indent=2) + '\n')
    print(label, report['image']['bytes'], flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['freeze', 'compile', 'compile-morphos', 'qualify'])
    parser.add_argument('--stage', required=True, type=Path)
    parser.add_argument('--checkpoint', type=Path)
    parser.add_argument('--label')
    parser.add_argument('--compiler', type=Path)
    parser.add_argument('--compiler-root', type=Path)
    parser.add_argument('--baseline', default='baseline')
    parser.add_argument('--morphos', action='store_true')
    parser.add_argument('--pass', dest='selected', choices=['SimplifyLoopPhis'])
    args = parser.parse_args()
    stage = args.stage.resolve()
    if args.mode == 'freeze':
        if not args.checkpoint:
            parser.error('--checkpoint is required')
        freeze(stage, args.checkpoint.resolve())
    elif args.mode == 'compile':
        if not args.label or not args.compiler or not args.compiler_root:
            parser.error('--label, --compiler, and --compiler-root are required')
        compile_trial(stage, args.label, args.compiler.resolve(), args.compiler_root.resolve(), args.selected)
    elif args.mode == 'compile-morphos':
        if not args.label or not args.compiler:
            parser.error('--label and --compiler are required')
        compile_morphos(stage, args.label, args.compiler.resolve(), args.selected)
    else:
        if not args.label:
            parser.error('--label is required')
        validate_frozen(stage)
        qualify(stage / args.baseline, stage / args.label,
                stage / 'runner/CopperOS.Commands.NativeExecution.dll', ['Search'],
                {'Search': ['morphos320-search-native-entry-vector-fixture']} if args.morphos else None)


if __name__ == '__main__':
    main()
