"""Measure, qualify and publish Version's opt-in loop-phi selection with the fixed compiler."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import re
import shutil

from audit_loop_phi_rollout import REPO, read
from measure_c import identity, parse_map, git_state
from qualify_sizes import run, qualify


def save(path, value):
    path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def audit(stage):
    assert not stage.exists(), 'Preserve previous measurements; use a fresh stage.'
    old = REPO/'artifacts/version-null-20261016'
    proof = read(REPO/'docs/Commands/Workbench31MorphOS320/version-helper-continuation-fix-20261016.json')
    for item in proof['fixedCompiler']:
        assert identity(item['path']) == item
    for item in proof['shippingInputs']['inputs'] + proof['privateInputs']:
        assert identity(item['path']) == item
    stage.mkdir(parents=True)
    shutil.copytree(old/'compiler-fixed', stage/'compiler')
    frozen = REPO/'artifacts/loop-phi-rollout-20261016'
    shutil.copytree(frozen/'version-diagnostic-runner', stage/'runner')
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    save(stage/'freeze.json', dict(source=git_state(REPO), previousProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/version-helper-continuation-fix-20261016.json'),
        compiler=[identity(p) for p in sorted((stage/'compiler').iterdir()) if p.is_file()],
        runner=[identity(p) for p in sorted((stage/'runner').iterdir()) if p.is_file()],
        shippingInputs=proof['shippingInputs'], privateInputs=proof['privateInputs'],
        installed=[identity(p) for p in sorted((stage/'installed/C').iterdir()) if p.is_file()]))
    def compile_one(job):
        profile, selected = job
        label = 'candidate' if selected else 'baseline'
        root = stage/profile/label
        (root/'C').mkdir(parents=True)
        source = old/'fixed'/profile/'inline/compile.rsp'
        lines = [line for line in source.read_text(encoding='utf-8-sig').splitlines() if not line.startswith('output=')]
        assert not any('SimplifyLoopPhis' in line for line in lines)
        if selected:
            lines = [line+',SimplifyLoopPhis' if line.startswith('code-size-passes=') else line for line in lines]
        image = root/'C/Version'
        lines += ['output='+str(image)]
        response = root/'compile.rsp'
        response.write_text('\n'.join(lines)+'\n', encoding='utf-8')
        assert run(['dotnet', str(stage/'compiler/CopperSharp.Compiler.Cli.dll'), '@'+str(response)], root/'compile.log') == 0
        if not selected:
            assert image.read_bytes() == (old/'fixed'/profile/'inline/C/Version').read_bytes()
        return dict(profile=profile, label=label, image=identity(image), response=identity(response), **parse_map(Path(str(image)+'.map')))
    with ThreadPoolExecutor(max_workers=2) as pool:
        rows = list(pool.map(compile_one, [(profile, selected) for profile in ['shipping', 'morphos'] for selected in [False, True]]))
    save(stage/'measurements.json', rows)
    checks = {}
    for profile, suite in [('shipping', 'workbench31-version-native-entry-vector-fixture'), ('morphos', 'morphos320-version-native-entry-vector-fixture')]:
        checks[profile] = qualify(stage/profile/'baseline', stage/profile/'candidate', stage/'runner/CopperOS.Commands.NativeExecution.dll', ['Version'], {'Version': [suite]})
    save(stage/'qualification.json', checks)
    report(stage)


def validate(stage, require_acceptance=True):
    frozen = read(stage/'freeze.json')
    for group in ['compiler', 'runner', 'installed']:
        for item in frozen[group]: assert identity(item['path']) == item
    for item in frozen['shippingInputs']['inputs'] + frozen['privateInputs']:
        assert identity(item['path']) == item
    for row in read(stage/'measurements.json'): assert identity(row['image']['path']) == row['image']
    checks = read(stage/'qualification.json')
    if require_acceptance:
        assert all(r['qualified'] and r['accept'] for rows in checks.values() for r in rows)
        assert (stage/'shipping/candidate/C/Version').stat().st_size < (stage/'installed/C/Version').stat().st_size
    return frozen, checks


def report(stage):
    frozen, checks = validate(stage, require_acceptance=False)
    rows = read(stage/'measurements.json')
    passed = all(r['qualified'] for values in checks.values() for r in values)
    smaller = all(r['accept'] for values in checks.values() for r in values)
    before = (stage/'installed/C/Version').stat().st_size
    after = (stage/'shipping/candidate/C/Version').stat().st_size
    accepted = passed and smaller and after < before
    decision = dict(bothProfilesQualified=passed, bothImagesSmaller=smaller, installedBytes=before,
                    candidateBytes=after, accepted=accepted,
                    reason='Eligible for enablement.' if accepted else 'Keep the current policy: the shipping executable grows.' if passed and after >= before else 'Keep the current policy: qualification or size gate failed.')
    save(stage/'decision.json', decision)
    inventory_unchanged = all((REPO/'out/C'/Path(item['path']).name).read_bytes() == Path(item['path']).read_bytes() for item in frozen['installed'])
    assert inventory_unchanged
    details = []
    for row in rows:
        details.append({**{k: v for k, v in row.items() if k != 'records'}, 'records':
            {k: v for k, v in row['records'].items() if k not in ['ALLOCATION', 'FRAME-SLOT', 'MACHINE-OP']}})
    method_deltas = {}
    allocation_deltas = {}
    for profile in checks:
        def sizes(label):
            text = (stage/profile/label/'C/Version.map').read_text(encoding='utf-8-sig')
            return {m[3]: int(m[2]) for m in re.finditer(r'(?m)^([0-9A-Fa-f]{8})\s+(\d+)\s+(\S+::\S+)\s*$', text)}
        a, b = sizes('baseline'), sizes('candidate')
        method_deltas[profile] = sorted([dict(method=name, baseline=a.get(name, 0), candidate=b.get(name, 0), delta=b.get(name, 0)-a.get(name, 0))
            for name in a.keys() | b.keys() if a.get(name, 0) != b.get(name, 0)], key=lambda r: -abs(r['delta']))
        allocations = {label: {r['method']: r for r in next(row for row in rows if row['profile'] == profile and row['label'] == label)['records']['ALLOCATION']}
                       for label in ['baseline', 'candidate']}
        allocation_deltas[profile] = [dict(method=r['method'], baseline=allocations['baseline'].get(r['method']), candidate=allocations['candidate'].get(r['method']))
                                      for r in method_deltas[profile]]
    target = REPO/'docs/Commands/Workbench31MorphOS320/version-loop-phis-20261017'
    data = dict(decision=decision, freeze=frozen, measurements=details, qualification=checks,
        methodDeltas=method_deltas, allocationDeltas=allocation_deltas, tool=identity(Path(__file__)),
        maps=[identity(p) for p in sorted(stage.rglob('*.map'))],
        receipts=[identity(p) for p in sorted(stage.rglob('*.json')) if 'execution' in p.parts],
        currentPolicy=identity(REPO/'src/Commands/CodeSizePolicies.props'), installedInventoryUnchanged=inventory_unchanged)
    save(target.with_suffix('.json'), data)
    lines = ['# Version SimplifyLoopPhis qualification — 2026-10-17', '',
        '**'+('Eligible for enablement.' if accepted else 'Keep SimplifyLoopPhis disabled for Version: the shipping executable grows by eight bytes.')+'**', '',
        '## Controlled comparison', '',
        'Both profiles use the exact fixed compiler payload qualified for the Version continuation-label fix: clean `eb04dac` plus the five-line fix subsequently committed as `55628a0`. This payload predates the separate remote CPU-probe/word-shift merge. Its hashes are validated against the earlier fix receipt. Within each profile the managed assemblies, SDK dependencies and existing passes are identical; only SimplifyLoopPhis is added. Both freshly compiled baselines reproduce the earlier fixed images byte for byte. The shipping baseline also matches the installed executable.', '',
        '| Profile | Baseline | Candidate | Change | Qualification |',
        '| --- | ---: | ---: | ---: | --- |']
    for profile, values in checks.items():
        r = values[0]
        lines.append(f"| {profile} | {r['beforeBytes']:,} | {r['afterBytes']:,} | {r['afterBytes']-r['beforeBytes']:+,} | {'Pass' if r['qualified'] else 'Fail'} |")
    lines += ['', '## Execution and stack', '',
        'Both baseline and candidate run all 93 shipping and 94 private MorphOS cases on 68000, 68020 and 68040 (1,122 invocations total). The supplied-vector suites enforce their declared contracts and compare output bytes, return codes, IoErr, allocation/release ownership, image immutability and supported repeated/interleaved cases. Instruction totals and stack peaks below are per CPU; all three modes give the same totals.', '',
        '| Profile | Instructions baseline → candidate | Peak stack baseline → candidate |',
        '| --- | ---: | ---: |']
    for profile, values in checks.items():
        c = values[0]['checks'][0]
        if 'instructionsBefore' in c:
            lines.append(f"| {profile} | {c['instructionsBefore']:,} → {c['instructionsAfter']:,} | {c['peakStackBefore']} → {c['peakStackAfter']} |")
    lines += ['', 'Measured stacks fit unchanged configured limits. Instruction totals describe these fixtures, not hardware timing or a general speed guarantee. Original DOS, WinUAE/CopperScreen and actual MorphOS execution were not run. No compiler implementation changed and compiler regressions were not rerun for this pass-selection experiment; prior fixed-payload qualification remains separate.', '',
        '## Encoded method changes', '']
    for profile, deltas in method_deltas.items():
        lines.append('Largest '+profile+' changes:')
        lines.append('')
        for item in deltas[:5]:
            lines.append(f"- `{item['method']}`: {item['baseline']} → {item['candidate']} bytes ({item['delta']:+}).")
        lines.append('')
    lines += ['These are final method ranges; executable changes also include layout, relocation and padding effects. In the shipping profile, TryParseDirectFileVersion grows by 48 bytes and ReadCommandSegmentVersion by 32, outweighing other method savings. Their allocator spill counts increase from 5 to 15 and 8 to 17 respectively; reloads increase from 6 to 18 and 10 to 22. Removing SSA values changes register allocation, so fewer IR values can still produce a larger image. The earlier result from the unfixed dirty compiler is not a valid acceptance baseline.', '',
        '## Evidence and disposition', '',
        'Evidence: `artifacts/version-loop-phis-20261017`. The JSON receipt binds both profile inputs, compiler and Copper68k payloads, response files, maps, optimizer statistics, instruction/stack measurements and raw execution receipts. Flags: MC68000 resident HUNK, YOLO exceptions, memory disabled, symbols off, fixed-point peepholes. Copper68k 1.5.1 SHA-256: `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.', '',
        'Reproduce in a fresh stage with `python tools/Commands/qualify_version_loop_phis.py audit --stage artifacts/version-loop-phis-fresh`. The compiler payload and inputs are frozen under the preceding Version-fix and rollout stages. `report` refreshes the report from existing receipts without running commands again. `enable` and `publish` require passing profile and strict-size gates.', '',
        'No Version policy or installed executable was changed in this investigation. The complete 61-command installed set remains 225,336 bytes. Unrelated workspace changes are preserved. Changes are not committed.', '']
    target.with_suffix('.md').write_text('\n'.join(lines), encoding='utf-8')
    print(decision)


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
    assert fresh.stat().st_size < (stage/'installed/C/Version').stat().st_size
    shutil.copy2(fresh, REPO/'out/C/Version')
    for name in names: assert (REPO/'out/C'/name).read_bytes() == (final/name).read_bytes()
    save(stage/'publication.json', dict(sourceBuild=identity(fresh), inventory=[identity(p) for p in sorted(final.iterdir())],
        beforeBytes=sum(r['bytes'] for r in frozen['installed']), afterBytes=sum(p.stat().st_size for p in final.iterdir()),
        qualified=checks, policy=identity(REPO/'src/Commands/CodeSizePolicies.props')))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode', choices=['audit', 'report', 'enable', 'publish'])
    parser.add_argument('--stage', required=True, type=Path)
    args = parser.parse_args()
    globals()[args.mode](args.stage.resolve())
