"""Compile unchanged Version inputs with/without helper inlining and qualify both profiles."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path

from audit_loop_phi_rollout import read
from measure_c import identity, parse_map
from qualify_sizes import run, qualify


def main():
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('--stage', required=True, type=Path)
    parser.add_argument('--inputs', required=True, type=Path)
    parser.add_argument('--compiler', required=True, type=Path)
    args = parser.parse_args()
    stage, inputs, compiler = args.stage.resolve(), args.inputs.resolve(), args.compiler.resolve()
    frozen = read(inputs/'freeze.json')
    private = read(inputs/'private-freeze.json')
    for record in [item for command in frozen for item in command['inputs']] + private:
        assert identity(record['path']) == record
    runner = inputs/'version-diagnostic-runner/CopperOS.Commands.NativeExecution.dll'
    suites = {'shipping': 'workbench31-version-native-entry-vector-fixture',
              'morphos': 'morphos320-version-native-entry-vector-fixture'}
    jobs = [(profile, inline) for profile in suites for inline in [False, True]]
    def compile_one(job):
        profile, inline = job
        source = inputs/('baseline/Version.rsp' if profile == 'shipping' else
            'profiles/addbuffers-Version-fixed-point/baseline/compile.rsp')
        root = stage/profile/('inline' if inline else 'no-inline')
        (root/'C').mkdir(parents=True, exist_ok=True)
        image = root/'C/Version'
        assert not image.exists(), 'Preserve previous compilation receipts.'
        lines = source.read_text(encoding='utf-8-sig').splitlines()
        assert sum('InlineMemoryHelpers' in line for line in lines) == 1
        if not inline:
            lines = [line.replace(',InlineMemoryHelpers', '') for line in lines]
        lines = [line for line in lines if not line.startswith('output=')]
        lines += ['output='+str(image)]
        response = root/'compile.rsp'
        response.write_text('\n'.join(lines)+'\n', encoding='utf-8')
        assert run(['dotnet', str(compiler), '@'+str(response)], root/'compile.log') == 0
        return dict(profile=profile, inline=inline, image=identity(image), response=identity(response),
                    **parse_map(Path(str(image)+'.map')))
    with ThreadPoolExecutor(max_workers=2) as pool:
        compiled = list(pool.map(compile_one, jobs))
    (stage/'compiled.json').write_text(json.dumps(compiled, indent=2)+'\n', encoding='utf-8')
    checks = {}
    for profile, suite in suites.items():
        checks[profile] = qualify(stage/profile/'no-inline', stage/profile/'inline', runner, ['Version'], {'Version': [suite]})
    report = dict(compiler=identity(compiler), compilerFiles=[identity(p) for p in sorted(compiler.parent.iterdir()) if p.is_file()],
                  frozenInputs=frozen, privateInputs=private,
                  runner=identity(runner), compiled=compiled, checks=checks)
    (stage/'qualification.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    assert all(row['qualified'] for rows in checks.values() for row in rows)
    print({profile: [(c['cpu'], c['invocations']) for c in rows[0]['checks']] for profile, rows in checks.items()})


if __name__ == '__main__':
    main()
