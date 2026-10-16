"""Verify default builds, assemble the complete inventory, and publish qualified images."""
import argparse
import json
from pathlib import Path
import re
import shutil

from audit_loop_phi_rollout import REPO, read, validate
from measure_c import identity


def methods(path):
    text = path.read_text(encoding='utf-8-sig')
    return {m[3]: int(m[2]) for m in re.finditer(r'(?m)^([0-9A-Fa-f]{8})\s+(\d+)\s+(\S+::\S+)\s*$', text)}


def main():
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('stage', type=Path)
    parser.add_argument('--publish', action='store_true')
    args = parser.parse_args()
    stage = args.stage.resolve()
    manifest = validate(stage)
    selection = read(stage/'selection.json')
    shipping = {r['command']: r for r in read(stage/'candidate/qualification.json')}
    task = read(stage/'tasklist-qualification.json')
    profiles = read(stage/'profiles.json')
    measurements = read(stage/'measurements.json')
    frozen_inputs = read(stage/'freeze.json')
    for command in frozen_inputs:
        for item in command['inputs']:
            assert identity(item['path']) == item
    control = read(stage/'tasklist-negative-control/control.json')
    assert all(r['qualified'] for r in task['checks'])
    assert control['strongStatus'] == 'failed' and control['oldRangeOnlyStatus'] == 'passed'
    for label in ['baseline', 'candidate']:
        for path in (stage/('tasklist-'+label+'-v2')/'execution').glob('*.json'):
            raw = read(path)
            assert raw['status'] == 'passed'
            assert all(c['peakStackBytes'] <= c.get('configuredStackBytes', 8192) for c in raw['cases'])
    selected = selection['selected']
    assert identity(REPO/'src/Commands/CodeSizePolicies.props') == selection['policyAfter']
    assert {p.name for p in (stage/'default-build/C').iterdir() if p.is_file()} == set(selected)
    defaults = []
    for name in selected:
        image = stage/'default-build/C'/name
        assert image.read_bytes() == (stage/'candidate/C'/name).read_bytes(), name
        defaults.append(identity(image))
        if name != 'TaskList':
            assert shipping[name]['qualified']
        for row in profiles:
            if row['name'] == name:
                assert all(r['qualified'] for r in row['checks'])
    rows = []
    for decision in selection['decisions']:
        name = decision['command']
        pair = {r['label']: r for r in measurements if r['command'] == name}
        before, after = [methods(stage/label/'C'/(name+'.map')) for label in ['baseline', 'candidate']]
        deltas = sorted([dict(method=m, before=b, after=after.get(m, 0), saved=b-after.get(m, 0))
                         for m, b in before.items() if b > after.get(m, 0)], key=lambda r: -r['saved'])
        row = dict(decision, largestMethodReductions=deltas[:5], measurements={})
        for label, item in pair.items():
            row['measurements'][label] = {k: item[k] for k in ['image', 'response', 'reachableMethods', 'largestMethods']}
            row['measurements'][label]['records'] = {k: v for k, v in item['records'].items()
                if k not in ['ALLOCATION', 'FRAME-SLOT', 'MACHINE-OP']}
        row['checks'] = task['checks'] if name == 'TaskList' else shipping.get(name, {}).get('checks', [])
        rows.append(row)
    final = stage/'accepted/C'
    final.mkdir(parents=True, exist_ok=True)
    for original in manifest['installed']:
        source = stage/'installed/C'/Path(original['path']).name
        replacement = stage/'default-build/C'/source.name
        shutil.copy2(replacement if source.name in selected else source, final/source.name)
    names = {Path(r['path']).name for r in manifest['installed']}
    assert len(names) == 61 and {p.name for p in final.iterdir()} == names
    changes = [name for name in sorted(names) if (final/name).read_bytes() != (stage/'installed/C'/name).read_bytes()]
    assert changes == sorted(selected)
    before_total = sum(r['bytes'] for r in manifest['installed'])
    after_total = sum(p.stat().st_size for p in final.iterdir())
    assert after_total < before_total
    # Refuse to overwrite concurrent changes to any installed executable.
    for name in names:
        expected = (stage/'installed/C'/name).read_bytes()
        current = (REPO/'out/C'/name).read_bytes()
        assert current == expected or current == (final/name).read_bytes(), name
    if args.publish:
        for name in selected:
            shutil.copy2(final/name, REPO/'out/C'/name)
        for name in names:
            assert (REPO/'out/C'/name).read_bytes() == (final/name).read_bytes(), name
    report = dict(stage=str(stage), source=manifest['source'],
                  compilerProvenance='Frozen List main-candidate payload: eb04dac plus preserved patches; no compiler changes in this rollout.',
                  compiler=manifest['compiler'], inputs=frozen_inputs,
                  runner=manifest['runner'], sourceBuilds=defaults, decisions=rows,
                  profiles=profiles, taskList=task, negativeControl=control,
                  provenance=identity(REPO/'docs/Commands/Workbench31MorphOS320/list-loop-phis-20261016.json'),
                  evidence=[identity(p) for p in sorted(stage.rglob('*.json'))
                            if 'execution' in p.parts or p.name in ['profiles.json', 'measurements.json', 'selection.json', 'control.json']],
                  taskListRunner=[identity(p) for p in sorted((stage/'tasklist-runner-v2').rglob('*')) if p.is_file()],
                  fixtureSources=[identity(REPO/p) for p in ['tests/Commands.NativeExecution/Program.cs',
                      'tests/Commands.NativeExecution/TaskListEntrySuite.cs', 'tests/Commands.NativeExecution/VersionEntrySuite.cs']],
                  beforeBytes=before_total, afterBytes=after_total, savedBytes=before_total-after_total,
                  sameAssemblyPassSavings=sum(r['passSavings'] for r in rows if r['enable']),
                  inventory=[identity(p) for p in sorted(final.iterdir())],
                  published=args.publish, policy=identity(REPO/'src/Commands/CodeSizePolicies.props'))
    target = REPO/'docs/Commands/Workbench31MorphOS320/loop-phi-rollout-20261016'
    target.with_suffix('.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    lines = ['# SimplifyLoopPhis command rollout — 2026-10-16', '',
        f'Enabled the existing opt-in pass for **{len(selected)} commands** after qualification. The complete 61-command inventory falls from **{before_total:,} to {after_total:,} bytes**, saving **{before_total-after_total:,} bytes**.', '',
        '## Controlled size comparison', '',
        'All remaining installed commands of at least 4,096 bytes were screened; previously qualified List and Search were excluded. Each off/on pair uses identical frozen managed assemblies and compiler payload. Only SimplifyLoopPhis differs. Fresh baseline sizes differ from installed sizes because the earlier installed toolchain/source inputs differ; those differences are not credited to this pass. Accepted same-assembly pass savings total '+str(report['sameAssemblyPassSavings'])+' bytes.', '',
        '| Command | Installed | Fresh off | Pass on | Pass saving | Decision |',
        '| --- | ---: | ---: | ---: | ---: | --- |']
    for r in rows:
        lines.append(f"| {r['command']} | {r['installedBytes']:,} | {r['baselineBytes']:,} | {r['candidateBytes']:,} | {r['passSavings']:,} | {'Enabled' if r['enable'] else 'Held'} |")
    lines += ['', 'Exe2Arc lacks a shipping-entry execution fixture. Quote merely matches its installed size. Version passes its shipping profile but both off/on private MorphOS profiles fail the same null-buffer read: handle 0x530 is correct, buffer is zero, count is 16,385 (`default-name-mossys-libs-elf-preference`). These three policies and installed images remain unchanged.', '',
        '## Where the savings come from', '',
        'SimplifyLoopPhis reduces redundant loop-carried SSA transport before register allocation. The resulting frame/spill and peephole changes can amplify the local saving. These are encoded method-body reductions, with downstream layout effects included separately in the executable totals:', '']
    for r in sorted([r for r in rows if r['enable']], key=lambda r: -r['passSavings'])[:5]:
        lines.append('- **'+r['command']+'**: '+ '; '.join(f"`{m['method'].split('::')[-1]}` {m['before']} → {m['after']} bytes" for m in r['largestMethodReductions'][:3])+'.')
    lines += ['', '## Shipping execution qualification', '',
        'Baseline and candidate fixtures execute on 68000, 68020 and 68040. Checks cover the available declared output, return/IoErr, resource ownership, repeated/interleaved invocation, image immutability and configured stack limits. Execute runs all 151 cases per CPU. The following totals are per CPU; instruction and peak-stack observations agree across these CPU modes.', '',
        '| Command | Cases/CPU | Instructions off → on | Peak stack off → on |',
        '| --- | ---: | ---: | ---: |']
    for r in rows:
        if not r['enable']: continue
        checks = [c for c in r['checks'] if c['cpu'] == '68000']
        lines.append(f"| {r['command']} | {sum(c['invocations'] for c in checks)} | {sum(c['instructionsBefore'] for c in checks):,} → {sum(c['instructionsAfter'] for c in checks):,} | {max(c['peakStackBefore'] for c in checks)} → {max(c['peakStackAfter'] for c in checks)} |")
    lines += ['', 'Copy adds 48,853 instructions across its 96 cases per CPU (0.71%); Delete adds 2,080 (0.78%). Copy, DosList and Mount each add four bytes to measured peak stack. All remain within unchanged configured stack limits. These finite instruction totals are not hardware timing or a general performance guarantee. Per-case raw receipts retain execution costs and stack observations.', '',
        '### TaskList live stack pointer', '',
        'The original strict comparison detected one legitimate output difference: verbose self-reporting prints the live A7 value, which changes with the generated frame. The strengthened fixture records actual A7 at the mapped WriteVerbose instruction, verifies its opcode, and requires the printed pointer to match that observation. Only this verified line is normalized for comparison; every other command-visible field still matches. All 57 cases per CPU pass. A negative control changes the store source from A7 to A5: the old range-only fixture passes, while the strengthened fixture rejects it. The original strict failure and initial unsuccessful probe attempts are preserved in staging.', '',
        '## Alternate profiles and peepholes', '',
        'Workbench/MorphOS private fixtures pass for AddDataTypes, Delete, Dir, Info, Mount and Type. Eval runs all 37 shipping and 20 MorphOS vectors per CPU with disabled, bounded and fixed-point peepholes. Version is held as described above. Initial AddDataTypes/Info compilation attempts omitted callback exports; corrected explicit export allowlists pass. Failed attempts remain preserved.', '',
        '## Reproduction and publication', '',
        'Evidence: `artifacts/loop-phi-rollout-20261016`. The JSON report binds compiler payload, SDK/input identities, response files, maps, pass counts, method sizes, CPU receipts and complete accepted inventory hashes. The compiler payload is the previously frozen List main candidate (`eb04dac` plus its recorded patches); no compiler implementation or global default changed. Detailed compiler source/patch provenance is retained in `list-loop-phis-20261016.json` and its frozen artifacts.', '',
        'Flags: MC68000 HUNK, resident runtime, YOLO exceptions, memory disabled, symbols off, fixed-point peepholes. Existing per-command passes remain selected; only SimplifyLoopPhis is appended. Copper68k 1.5.1 SHA-256: `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`.', '',
        'The fresh default source build of all 12 accepted commands, with no pass override and the frozen compiler CLI, reproduces every qualified candidate byte for byte. The other 49 installed executables are preserved. The complete 61-command inventory is staged and hash-checked before refreshing the selected files in `out/C`; no stale files are removed. Publication status: **'+('completed' if args.publish else 'staged only')+'**.', '',
        'Tools: `audit_loop_phi_rollout.py`, `qualify_loop_phi_profiles.py`, `qualify_tasklist_live_sp.py`, `control_tasklist_live_sp.py`, `enable_loop_phi_rollout.py`, and this report/publication tool. Run preparation in a fresh staging directory; enabling is a one-time policy edit after successful qualification. Unrelated Shell/ImageBuilder/compiler work is preserved.', '',
        '## Qualification limits', '',
        'This rollout uses supplied-vector fixtures. Original DOS, WinUAE/CopperScreen and original-image checks were not rerun for these commands; prior List/Search native qualification remains separate. Actual MorphOS execution, original Workbench functional parity, PURE admission and absolute minimum stack are not established. No PPC binary-size comparison is made. Existing compiler-pass regressions were qualified in the earlier List work; the compiler suite was not rerun for this policy-only rollout. Version’s baseline failure and Exe2Arc shipping-entry coverage remain follow-up work.', '']
    target.with_suffix('.md').write_text('\n'.join(lines), encoding='utf-8')
    print(f'{len(selected)} enabled; {before_total} -> {after_total}; published={args.publish}')


if __name__ == '__main__':
    main()
