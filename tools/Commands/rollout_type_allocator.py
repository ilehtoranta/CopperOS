"""Measure and qualify pending-copy-home allocator ranking on frozen commands."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import re
import shutil
import subprocess
import time
from measure_c import identity, parse_map
from qualify_sizes import qualify, suites, comparable
from rollout_scalar_allocator import freeze, compile_one, read, save
from rollout_receiver_fix import DUAL, PRIVATE_SUITES

REPO = Path(__file__).resolve().parents[2]
PREVIOUS = REPO/'artifacts/scalar-allocator-rollout-20261018'
TYPE = REPO/'artifacts/type-allocator-20261018'

def prepare(stage):
    assert not stage.exists()
    stage.mkdir(parents=True)
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    for label, source in [('baseline', PREVIOUS/'compiler-candidate'), ('candidate', TYPE/'compiler-candidate')]:
        shutil.copytree(source, stage/('compiler-'+label))
    shutil.copytree(PREVIOUS/'runner', stage/'runner')
    names = sorted(p.name for p in (stage/'installed/C').iterdir())
    assert len(names) == 61
    for name in names:
        for profile in (['shipping', 'morphos'] if name in DUAL else ['shipping']):
            folder = 'inputs' if profile == 'shipping' else 'profiles'
            source = PREVIOUS/folder/name/'compile.rsp'
            freeze(source.read_text(encoding='utf-8-sig').splitlines(), stage/folder/name)
    shutil.copy2(TYPE/'compiler-fix.patch', stage/'compiler-fix.patch')
    save(stage/'freeze.json', dict(commands=names, baselineRevision='c536d31', candidateRevision='daab9bc',
        compilerProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/type-allocator-regression-20261018.json'),
        files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))

def vectors(stage):
    results = []
    for name in read(stage/'freeze.json')['commands']:
        if name == 'Type':
            continue
        profiles = ['shipping', 'morphos'] if name in DUAL else ['shipping']
        paths = [stage/p/label/(name+'.measurement.json') for p in profiles for label in ['baseline','candidate']]
        while not all(p.exists() for p in paths):
            time.sleep(2)
        measurements = [read(p) for p in paths]
        assert all(r['exitCode'] == 0 for r in measurements)
        if not any(a['image']['bytes'] > b['image']['bytes'] for a,b in zip(measurements[::2], measurements[1::2])):
            continue
        for profile in profiles:
            overrides = {name: [PRIVATE_SUITES[name]]} if profile == 'morphos' else None
            if name == 'Copy':
                overrides = {name: suites()[name]+['copy-target-dispatch-native-entry-vector-fixture+directory-command']}
            result = qualify(stage/profile/'baseline', stage/profile/'candidate',
                stage/'runner/CopperOS.Commands.NativeExecution.dll', [name], overrides)[0]
            results.append(dict(command=name, profile=profile, result=result))
            save(stage/'vectors.json', results)
    save(stage/'vectors.json', results)

def measure(stage):
    frozen = read(stage/'freeze.json')
    for item in frozen['files']:
        assert identity(item['path']) == item
    jobs = [(n, p, label) for n in frozen['commands'] for p in
        (['shipping','morphos'] if n in DUAL else ['shipping']) for label in ['baseline','candidate']]
    def one(job):
        name, profile, label = job
        root = stage/profile/label
        cached = root/(name+'.measurement.json')
        if cached.exists():
            row = read(cached)
            assert row['exitCode'] == 0
            for key in ['image','response']:
                assert identity(row[key]['path']) == row[key]
            assert row['map'] == parse_map(root/'C'/(name+'.map'))
            expected = (stage/('inputs' if profile == 'shipping' else 'profiles')/name/'compile.rsp').read_text(encoding='utf-8-sig')+'output='+str(root/'C'/name)+'\n'
            assert Path(row['response']['path']).read_text(encoding='utf-8-sig') == expected
            return row
        # An interrupted compiler may have written an image but no receipt.
        image = root/'C'/name
        if image.exists():
            image.unlink()
        return compile_one(stage, *job)
    with ThreadPoolExecutor(max_workers=4) as pool:
        rows = list(pool.map(one, jobs))
    save(stage/'measurements.json', rows)
    summary = []
    for name in frozen['commands']:
        for profile in (['shipping','morphos'] if name in DUAL else ['shipping']):
            pair = {r['compiler']: r for r in rows if r['command'] == name and r['profile'] == profile}
            a,b = pair['baseline'],pair['candidate']
            assert a['exitCode'] == b['exitCode'] == 0
            summary.append(dict(command=name, profile=profile, before=a['image'], after=b['image'],
                reproducedInstalled=profile=='shipping' and a['image']['sha256']==identity(stage/'installed/C'/name)['sha256'],
                savedBytes=a['image']['bytes']-b['image']['bytes']))
    save(stage/'summary.json', summary)
    print(json.dumps([{k: r[k] for k in ['command','profile','savedBytes']} for r in summary if r['savedBytes']], indent=2), flush=True)

def audit(stage):
    frozen = read(stage/'freeze.json')
    for item in frozen['files']:
        assert identity(item['path']) == item
    assert identity(frozen['compilerProof']['path']) == frozen['compilerProof']
    proof = read(Path(frozen['compilerProof']['path']))
    assert proof['passed']
    for item in proof['compilerTests']:
        assert identity(item['receipt']['path']) == item['receipt']
    assert any(int(t['counters']['passed']) == 721 and int(t['counters']['failed']) == 0 for t in proof['compilerTests'])
    for p in (stage/'compiler-candidate').iterdir():
        if p.is_file():
            assert p.read_bytes() == (TYPE/'compiler-candidate'/p.name).read_bytes()
    assert (stage/'compiler-fix.patch').read_bytes() == (TYPE/'compiler-fix.patch').read_bytes()
    summary = read(stage/'summary.json')
    measurements = read(stage/'measurements.json')
    assert len(summary) == 96 and len(measurements) == 192
    for row in measurements:
        assert row['exitCode'] == 0
        for key in ['image', 'response']:
            assert identity(row[key]['path']) == row[key]
        assert row['map'] == parse_map(Path(row['image']['path']+'.map'))
    for row in summary:
        name, profile = row['command'], row['profile']
        assert row['before']['sha256'] == identity(PREVIOUS/profile/'candidate/C'/name)['sha256']
        if profile == 'shipping' and not row['reproducedInstalled']:
            # Earlier size-neutral candidates were measured but never installed.
            assert name in ['List','Type']
            assert (stage/'installed/C'/name).read_bytes() == (PREVIOUS/'shipping/baseline/C'/name).read_bytes()
    tests = read(stage/'vectors.json')
    beneficiaries = {r['command'] for r in summary if r['command'] != 'Type' and r['savedBytes'] > 0}
    assert {(r['command'], r['profile']) for r in tests} == {(n, p) for n in beneficiaries for p in (['shipping','morphos'] if n in DUAL else ['shipping'])}
    receipts = []
    for row in tests:
        name, profile = row['command'], row['profile']
        expected = [PRIVATE_SUITES[name]] if profile == 'morphos' else suites()[name]
        if name == 'Copy':
            expected += ['copy-target-dispatch-native-entry-vector-fixture+directory-command']
        assert {(c['cpu'], c['suite']) for c in row['result']['checks']} == {(cpu, suite) for cpu in ['68000','68020','68040'] for suite in expected}
        for suite in expected:
            group = [c for c in row['result']['checks'] if c['suite'] == suite]
            if all(c['passed'] for c in group):
                for key in ['invocations','instructionsBefore','instructionsAfter','peakStackBefore','peakStackAfter']:
                    assert len({c[key] for c in group}) == 1
        for check in row['result']['checks']:
            pair = []
            for label in ['baseline', 'candidate']:
                root = stage/profile/label
                path = root/'execution'/f'{name}.{check["cpu"]}.{check["suite"]}.json'
                data = read(path)
                assert data['imageSha256'] == identity(root/'C'/name)['sha256']
                assert data['managedExecutorSha256'] == identity(stage/'runner/CopperOS.Commands.NativeExecution.dll')['sha256']
                assert data['instructionCoreSha256'] == identity(stage/'runner/Copper68k.dll')['sha256']
                if data.get('compilerMapSha256'):
                    assert data['compilerMapSha256'] == identity(root/'C'/(name+'.map'))['sha256']
                if check['passed']:
                    assert data['status'] == 'passed' and not data.get('observationOnly', False)
                pair.append(data)
                receipts.append(identity(path))
            if check['passed']:
                a, b = [d.get('cases', d.get('observations')) for d in pair]
                assert len(a) == len(b) > 0
                assert [comparable(c) for c in a] == [comparable(c) for c in b]
                assert all(c.get('peakStackBytes', c['stackBytesWritten']) <= c.get('configuredStackBytes', 8192) for c in b)
    decisions = []
    references = {r['command']: r.get('workbench31') for r in read(PREVIOUS/'audit.json')['decisions']}
    for name in frozen['commands']:
        sizes = [r for r in summary if r['command'] == name]
        shipping = next(r for r in sizes if r['profile'] == 'shipping')
        checks = [r for r in tests if r['command'] == name]
        profiles = ['shipping', 'morphos'] if name in DUAL else ['shipping']
        qualified = all(any(t['profile'] == p and t['result']['qualified'] for t in checks) for p in profiles)
        accepted = shipping['savedBytes'] > 0 and shipping['reproducedInstalled'] and qualified and all(r['savedBytes'] >= 0 for r in sizes)
        decisions.append(dict(command=name, accepted=accepted, sizes=sizes, qualification=checks, workbench31=references.get(name)))
    methods = []
    for row in summary:
        if row['command'] == 'Type' or row['savedBytes'] <= 0:
            continue
        pair = []
        for label in ['baseline','candidate']:
            path = stage/row['profile']/label/'C'/(row['command']+'.map')
            sizes = {m[1]: int(m[0]) for m in re.findall(r'^[0-9A-F]{8}\s+(\d+)\s+(.+::.+)$', path.read_text(encoding='utf-8-sig'), re.M)}
            allocation = {r['method']: r for r in parse_map(path)['records'].get('ALLOCATION', [])}
            pair.append((sizes, allocation))
        a,b = pair
        methods.append(dict(command=row['command'], profile=row['profile'],
            bodyChanges=[dict(method=m, beforeBytes=a[0].get(m), afterBytes=b[0].get(m)) for m in sorted(a[0].keys()|b[0].keys()) if a[0].get(m)!=b[0].get(m)],
            allocationChanges=[dict(method=m, before=a[1].get(m), after=b[1].get(m)) for m in sorted(a[1].keys()|b[1].keys()) if a[1].get(m)!=b[1].get(m)]))
    data = dict(passed=True, freeze=frozen, decisions=decisions, measurements=measurements,
        methodChanges=methods, receipts=receipts, compilerQualification=proof['compilerTests'], tool=identity(Path(__file__)),
        maps=[identity(Path(r['image']['path']+'.map')) for r in measurements],
        runtime=dict(dotnetSdk=subprocess.check_output(['dotnet','--version'],cwd=REPO).decode().strip(), copper68k='1.5.1', core=identity(stage/'runner/Copper68k.dll')))
    save(stage/'audit.json', data)
    return data

def publish(stage):
    data = audit(stage)
    target = stage/'accepted/C'
    assert not target.exists()
    shutil.copytree(stage/'installed/C', target)
    accepted = [r['command'] for r in data['decisions'] if r['accepted']]
    for name in accepted:
        shutil.copy2(stage/'shipping/candidate/C'/name, target/name)
    before = stage/'installed/C'
    assert {p.name for p in target.iterdir()} == {p.name for p in before.iterdir()}
    assert {p.name for p in (REPO/'out/C').iterdir()} == {p.name for p in before.iterdir()}
    assert all((REPO/'out/C'/p.name).read_bytes() == p.read_bytes() for p in before.iterdir())
    for p in target.iterdir():
        source = stage/'shipping/candidate/C'/p.name if p.name in accepted else before/p.name
        assert p.read_bytes() == source.read_bytes()
    for name in accepted:
        shutil.copy2(target/name, REPO/'out/C'/name)
    assert all((REPO/'out/C'/p.name).read_bytes() == p.read_bytes() for p in target.iterdir())
    data['publication'] = dict(accepted=accepted, beforeBytes=sum(p.stat().st_size for p in before.iterdir()),
        afterBytes=sum(p.stat().st_size for p in target.iterdir()), inventory=[identity(p) for p in sorted(target.iterdir())],
        installedInventory=[identity(p) for p in sorted((REPO/'out/C').iterdir())])
    save(stage/'published.json', data)
    save(REPO/'docs/Commands/Workbench31MorphOS320/type-allocator-rollout-20261018.json', data)

def report(stage):
    data = read(stage/'published.json')
    publication = data['publication']
    accepted = publication['accepted']
    same_size = sum(r['before']['bytes'] == r['after']['bytes'] for d in data['decisions'] for r in d['sizes'])
    identical = sum(r['before']['sha256'] == r['after']['sha256'] for d in data['decisions'] for r in d['sizes'])
    lines = ['# Pending copy-home allocator rollout — 2026-10-18', '', '## Result', '',
        f'Compared **61 shipping commands and 35 distinct alternate profiles**, using identical frozen assemblies for each compiler. Accepted shipping updates: **{", ".join(accepted) if accepted else "none"}**.', '',
        f'The complete 61-command inventory is **{publication["beforeBytes"]:,} → {publication["afterBytes"]:,} bytes**, saving **{publication["beforeBytes"]-publication["afterBytes"]:,} bytes**. Only strictly smaller shipping images with passing checks for every applicable profile are eligible for publication. Every unselected installed hash remains unchanged.', '',
        f'Of the 96 profile comparisons, **{same_size} retain their executable size** and **{identical} are byte-identical**. Equal-size images are measured controls and receive no new runtime-adoption claim unless their command also has a smaller profile.', '',
        '## Controlled inputs', '',
        'Baseline compiler: clean `c536d31`. Candidate: that compiler plus the pending-copy-home ranking fix committed as `daab9bc`. The candidate binaries and patch are the exact retained artifacts from Type’s qualified isolated build; the dirty primary compiler checkout was not built. The prior 721 passing compiler regressions and their receipt identities are retained in this report.', '',
        'Command assemblies, all adjacent dependencies, SDK identities and response flags are frozen from the preceding scalar allocator rollout. Every fresh baseline reproduces its preceding compiler image. **59 shipping baselines reproduce the installed executables byte for byte.** List and Type retain their earlier accepted installed images: the previous size-neutral List candidate was not adopted, while Type’s initial repair grew. List remains 4,604 bytes with both compilers. Type changes 4,700 → 4,696 bytes, tying its installed size, and its separate qualification is not counted as a new beneficiary here. Both retained installed identities are checked against the preceding baseline artifacts.', '',
        'Flags remain MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and symbols off, with each command’s accepted pass selection. No command sources, options, stack limits or build-policy settings change. Maps record effective passes, rewrites, code/data metrics, relocations, method sizes and allocation records.', '',
        '## Runtime qualification', '',
        'Each command with a smaller profile is checked in both distinct profiles where implemented. Supplied-vector fixtures run the exact baseline and candidate images on **68000, 68020 and 68040**, comparing output bytes, return codes, IoErr and command-visible resource/ownership observations. Repeated invocation and interleaving coverage is limited to the cases supplied by each fixture. Compiler-owned invocation context is accounted separately. Peak stack must remain within the existing configured limits.', '',
        '| Command/profile | Calls per image/CPU | Instructions before → after | Peak stack before → after |',
        '|---|---:|---:|---:|']
    calls = 0
    for decision in data['decisions']:
        for row in decision['qualification']:
            checks = row['result']['checks']
            # Each fixture suite runs independently on every CPU.
            cpu = [c for c in checks if c['cpu'] == '68000' and c['passed']]
            calls += sum(c.get('invocations', 0)*2 for c in checks)
            lines.append(f'| {row["command"]}/{row["profile"]} | {sum(c["invocations"] for c in cpu)} | {sum(c["instructionsBefore"] for c in cpu):,} → {sum(c["instructionsAfter"] for c in cpu):,} | {max((c["peakStackBefore"] for c in cpu), default=0)} → {max((c["peakStackAfter"] for c in cpu), default=0)} |')
    lines += ['', f'**{calls:,} command invocations** across both images and all three CPUs. Per-CPU and individual-case instruction/stack observations are retained in the raw receipts. These are instruction counts, not execution-time measurements.', '',
        f'Executor: Copper68k **{data["runtime"]["copper68k"]}**, SHA-256 `{data["runtime"]["core"]["sha256"]}`. Actual .NET SDK: `{data["runtime"]["dotnetSdk"]}`.', '',
        '## Smaller profile details', '']
    for row in data['methodChanges']:
        lines += [f'### {row["command"]}/{row["profile"]}', '']
        for m in row['bodyChanges']:
            lines.append(f'- `{m["method"]}`: {m["beforeBytes"]} → {m["afterBytes"]} encoded bytes.')
        for m in row['allocationChanges']:
            a,b = m['before'],m['after']
            if a is not None and b is not None:
                lines.append(f'- `{m["method"]}` allocation: spills {a["spilled"]} → {b["spilled"]}, frame {a["frame-bytes"]} → {b["frame-bytes"]} bytes, saved registers {a["saved-bytes"]} → {b["saved-bytes"]} bytes.')
        lines += ['']
    lines += ['## Complete size sweep', '',
        '| Command | Shipping before → candidate | Saved | Alternate before → candidate | Decision |',
        '|---|---:|---:|---:|---|']
    for row in data['decisions']:
        s = next(r for r in row['sizes'] if r['profile'] == 'shipping')
        alternate = next((r for r in row['sizes'] if r['profile'] == 'morphos'), None)
        alt = f'{alternate["before"]["bytes"]:,} → {alternate["after"]["bytes"]:,}' if alternate else '—'
        decision = 'Updated out/C.' if row['accepted'] else 'Type already qualified; ties installed size.' if row['command'] == 'Type' else 'No shipping size reduction.' if s['savedBytes'] <= 0 else 'Retained: qualification or profile gate did not pass.'
        lines.append(f'| {row["command"]} | {s["before"]["bytes"]:,} → {s["after"]["bytes"]:,} | {s["savedBytes"]} | {alt} | {decision} |')
    lines += ['', '## Qualification limits and evidence', '',
        'The finite vector fixtures use emulated OS calls. They do not establish exhaustive equivalence or original MorphOS/PPC execution. No new original-DOS, WinUAE, original-command-image or CopperScreen checks are claimed by this rollout; earlier native checks do not qualify a newly changed image. MorphOS-only commands receive MC68000 measurements without comparing them to PPC binaries. Unsupported interleavings and minimum-stack guarantees are not inferred.', '',
        'Evidence: `artifacts/type-allocator-rollout-20261018`. Reproduce with `tools/Commands/rollout_type_allocator.py` modes `prepare`, `measure`, `vectors`, `audit`, `publish`, `report`, using a fresh stage. Baselines, complete dependencies, both compilers, patch, responses, maps and runtime receipts are preserved. Publication verifies a complete staged 61-file inventory before replacing only accepted commands. The accompanying JSON contains exact identities and qualification results.', '']
    (REPO/'docs/Commands/Workbench31MorphOS320/type-allocator-rollout-20261018.md').write_text('\n'.join(lines), encoding='utf-8')

if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode', choices=['prepare', 'measure', 'vectors', 'audit', 'publish', 'report'])
    parser.add_argument('--stage', type=Path, required=True)
    args = parser.parse_args()
    globals()[args.mode](args.stage.resolve())
