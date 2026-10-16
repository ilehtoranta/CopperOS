"""Qualify a compiler coalescing fix against frozen Version images and inputs."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import shutil
import re

from audit_loop_phi_rollout import REPO, read
from measure_c import identity, git_state, parse_map
from qualify_sizes import run, qualify


def save(path, value):
    path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def audit(stage, compiler):
    assert not stage.exists(), 'Use a fresh staging directory.'
    previous = REPO/'artifacts/version-loop-phis-20261017'
    frozen = read(previous/'freeze.json')
    for group in ['compiler', 'runner', 'installed']:
        for item in frozen[group]: assert identity(item['path']) == item
    for item in frozen['shippingInputs']['inputs'] + frozen['privateInputs']:
        assert identity(item['path']) == item
    stage.mkdir(parents=True)
    shutil.copytree(compiler.parent, stage/'compiler')
    shutil.copytree(previous/'runner', stage/'runner')
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    for profile in ['shipping', 'morphos']:
        for old, new in [('baseline', 'baseline'), ('candidate', 'reference')]:
            shutil.copytree(previous/profile/old/'C', stage/profile/new/'C')
    manifest = dict(source=git_state(REPO), previous=identity(previous/'qualification.json'), previousFreeze=frozen,
        compiler=[identity(p) for p in sorted((stage/'compiler').iterdir()) if p.is_file()],
        runner=[identity(p) for p in sorted((stage/'runner').iterdir()) if p.is_file()],
        installed=[identity(p) for p in sorted((stage/'installed/C').iterdir()) if p.is_file()])
    save(stage/'freeze.json', manifest)
    def compile_one(profile):
        dest = stage/profile/'candidate'; (dest/'C').mkdir(parents=True)
        lines = [l for l in (previous/profile/'candidate/compile.rsp').read_text(encoding='utf-8-sig').splitlines() if not l.startswith('output=')]
        lines += ['output='+str(dest/'C/Version')]
        response = dest/'compile.rsp'; response.write_text('\n'.join(lines)+'\n', encoding='utf-8')
        assert run(['dotnet', str(stage/'compiler/CopperSharp.Compiler.Cli.dll'), '@'+str(response)], dest/'compile.log') == 0
        return dict(profile=profile, image=identity(dest/'C/Version'), response=identity(response), **parse_map(dest/'C/Version.map'))
    with ThreadPoolExecutor(max_workers=2) as pool: rows = list(pool.map(compile_one, ['shipping', 'morphos']))
    save(stage/'measurements.json', rows)
    checks = {}
    for profile, suite in [('shipping', 'workbench31-version-native-entry-vector-fixture'), ('morphos', 'morphos320-version-native-entry-vector-fixture')]:
        checks[profile] = {}
        for label in ['baseline', 'reference']:
            checks[profile][label] = qualify(stage/profile/label, stage/profile/'candidate', stage/'runner/CopperOS.Commands.NativeExecution.dll', ['Version'], {'Version': [suite]})
            save(stage/profile/'candidate'/(label+'-qualification.json'), checks[profile][label])
    save(stage/'qualification.json', checks)
    assert all(r['qualified'] for comparisons in checks.values() for rows in comparisons.values() for r in rows)
    assert all(checks[p]['baseline'][0]['accept'] for p in checks)
    assert (stage/'shipping/candidate/C/Version').stat().st_size < (stage/'installed/C/Version').stat().st_size


def validate(stage):
    frozen = read(stage/'freeze.json')
    for group in ['compiler', 'runner', 'installed']:
        for item in frozen[group]: assert identity(item['path']) == item
    for item in frozen['previousFreeze']['shippingInputs']['inputs'] + frozen['previousFreeze']['privateInputs']:
        assert identity(item['path']) == item
    for row in read(stage/'measurements.json'): assert identity(row['image']['path']) == row['image']
    checks = read(stage/'qualification.json')
    assert all(row['qualified'] and row['accept'] for profiles in checks.values() for rows in profiles.values() for row in rows)
    assert (stage/'shipping/candidate/C/Version').stat().st_size < (stage/'installed/C/Version').stat().st_size
    return frozen, checks


def enable(stage):
    validate(stage)
    path = REPO/'src/Commands/CodeSizePolicies.props'
    text = path.read_text(encoding='utf-8')
    pattern = r'(<PropertyGroup Condition="\x27\$\(CopperOSCommandName\)\x27 == \x27Version\x27[^>]*>\s*<CopperOSCodeSizePasses>)([^<]+)(</CopperOSCodeSizePasses>)'
    match = re.search(pattern, text)
    assert match and 'SimplifyLoopPhis' not in match[2]
    before = identity(path)
    path.write_text(text[:match.end(2)]+',SimplifyLoopPhis'+text[match.end(2):], encoding='utf-8')
    save(stage/'selection.json', dict(before=before, after=identity(path)))


def publish(stage):
    frozen, checks = validate(stage)
    assert identity(REPO/'src/Commands/CodeSizePolicies.props') == read(stage/'selection.json')['after']
    fresh = stage/'default-build/C/Version'
    assert fresh.read_bytes() == (stage/'shipping/candidate/C/Version').read_bytes()
    final = stage/'accepted/C'
    assert not final.exists()
    shutil.copytree(stage/'installed/C', final)
    shutil.copy2(fresh, final/'Version')
    names = {Path(p['path']).name for p in frozen['installed']}
    assert len(names) == 61 and {p.name for p in final.iterdir()} == names
    for name in names:
        assert (REPO/'out/C'/name).read_bytes() == (stage/'installed/C'/name).read_bytes(), name
        if name != 'Version': assert (final/name).read_bytes() == (stage/'installed/C'/name).read_bytes()
    shutil.copy2(fresh, REPO/'out/C/Version')
    for name in names: assert (REPO/'out/C'/name).read_bytes() == (final/name).read_bytes()
    save(stage/'publication.json', dict(sourceBuild=identity(fresh), inventory=[identity(p) for p in sorted(final.iterdir())],
        beforeBytes=sum(r['bytes'] for r in frozen['installed']), afterBytes=sum(p.stat().st_size for p in final.iterdir()),
        policy=identity(REPO/'src/Commands/CodeSizePolicies.props')))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode', nargs='?', default='audit', choices=['audit', 'enable', 'publish'])
    parser.add_argument('--stage', required=True, type=Path)
    parser.add_argument('--compiler', type=Path)
    args = parser.parse_args()
    if args.mode == 'audit':
        assert args.compiler
        audit(args.stage.resolve(), args.compiler.resolve())
    else:
        globals()[args.mode](args.stage.resolve())
