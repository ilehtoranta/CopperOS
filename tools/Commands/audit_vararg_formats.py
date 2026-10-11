"""Freeze a fresh command build and compare compiler payloads on identical inputs."""
import argparse
import json
import shutil
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

from measure_c import identity, git_state, parse_map
from qualify_sizes import run

REPO = Path(__file__).resolve().parents[2]


def freeze(stage, fresh):
    destination = stage / 'fresh-inputs'
    if destination.exists():
        raise RuntimeError('Preserve the existing freeze; use a new destination.')
    names = sorted(p.name for p in (fresh / 'C').iterdir() if p.is_file() and not p.suffix)
    shipping = sorted(p.name for p in (REPO / 'out/C').iterdir() if p.is_file() and not p.suffix)
    if names != shipping:
        raise RuntimeError('Fresh build and shipping inventories differ.')
    rows = []
    baseline = destination / 'accepted/C'
    baseline.mkdir(parents=True)
    for name in names:
        shutil.copy2(REPO / 'out/C' / name, baseline / name)
        source = REPO / 'src/Commands' / name
        response = source / 'obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp'
        lines = response.read_text(encoding='utf-8-sig').splitlines()
        inputs = destination / 'inputs' / name
        inputs.mkdir(parents=True)
        flags = []
        for line in lines:
            if line.startswith(('input=', 'managed-assembly=')):
                key, value = line.split('=', 1)
                path = Path(value)
                if not path.is_absolute():
                    path = source / path
                target = inputs / path.name
                shutil.copy2(path, target)
                line = key + '=' + str(target.resolve())
            if not line.startswith(('output=', 'compatibility-report=')):
                flags.append(line)
        (inputs / 'compile.rsp').write_text('\n'.join(flags) + '\n', encoding='utf-8')
        rows.append({'command': name, 'flags': flags,
                     'inputs': [identity(p) for p in sorted(inputs.iterdir())],
                     'freshImage': identity(fresh / 'C' / name),
                     'shippingImage': identity(baseline / name)})
    report = {'source': git_state(REPO), 'commands': rows}
    (destination / 'freeze.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    return report


def compare(stage, compiler, label, workers):
    frozen = json.loads((stage / 'fresh-inputs/freeze.json').read_text(encoding='utf-8'))
    target = stage / label
    if target.exists():
        raise RuntimeError('Use a fresh comparison destination.')
    payload = target / 'compiler'
    shutil.copytree(compiler.parent, payload)
    cli = payload / compiler.name
    (target / 'C').mkdir()

    def compile_one(row):
        name = row['command']
        for item in row['inputs']:
            if identity(item['path']) != item:
                raise RuntimeError('Frozen input changed: ' + item['path'])
        response = target / (name + '.rsp')
        image = target / 'C' / name
        response.write_text('\n'.join(row['flags'] + ['output=' + str(image.resolve())]) + '\n', encoding='utf-8')
        code = run(['dotnet', str(cli), '@' + str(response.resolve())], target / (name + '.compile.log'))
        if code:
            raise RuntimeError(name + ' compilation failed; inspect its log.')
        result = {'command': name, 'image': identity(image), 'response': identity(response),
                  'shippingIdentical': image.read_bytes() == Path(row['shippingImage']['path']).read_bytes(),
                  'freshIdentical': image.read_bytes() == Path(row['freshImage']['path']).read_bytes(),
                  **parse_map(Path(str(image) + '.map'))}
        print(name + ': ' + str(result['image']['bytes']), flush=True)
        return result

    with ThreadPoolExecutor(max_workers=workers) as pool:
        rows = list(pool.map(compile_one, frozen['commands']))
    report = {'compiler': {'assembly': identity(cli),
                          'files': [identity(p) for p in sorted(payload.glob('*.dll'))]},
              'commands': rows}
    (target / 'compilation.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['freeze', 'compare'])
    parser.add_argument('--stage', type=Path, required=True)
    parser.add_argument('--fresh', type=Path)
    parser.add_argument('--compiler', type=Path)
    parser.add_argument('--label')
    parser.add_argument('--workers', type=int, default=2)
    args = parser.parse_args()
    if args.mode == 'freeze':
        if not args.fresh:
            parser.error('--fresh is required for freeze')
        freeze(args.stage.resolve(), args.fresh.resolve())
    else:
        if not args.compiler or not args.label:
            parser.error('--compiler and --label are required for compare')
        if args.workers < 1 or args.workers > 4:
            parser.error('--workers must be between 1 and 4')
        compare(args.stage.resolve(), args.compiler.resolve(), args.label, args.workers)


if __name__ == '__main__':
    main()
