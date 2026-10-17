"""Publish original-DOS qualification using retained bounded batches and split heavy cases."""
import importlib.util
import json
from pathlib import Path
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from measure_c import identity
from qualify_exe2arc_os import validate
from verify import verify,read
from raw import audit


def report(stage,cross):
    stage=Path(stage).resolve();cross=Path(cross).resolve()
    validate(stage);validate(cross)
    for label in ['baseline','candidate']:
        assert (stage/'inputs'/('Exe2Arc.'+label)).read_bytes()==(cross/'inputs'/('Exe2Arc.'+label)).read_bytes()
    matrix=[]
    batches=['typed','automatic','names','errors1','errors2','signals','large','repeat']
    for cpu in ['68000','68020','68040']:
        for batch in batches:
            q=verify(stage,batch,cpu);assert q['passed'];matrix.append(q)
        for batch in ['cross-baseline','cross-candidate']:
            q=verify(cross,batch,cpu);assert q['passed'];matrix.append(q)
    contracts=read(stage/'contracts.json')
    assert len(contracts)==32
    winuae=[]
    for row in read(stage/'winuae/results.json'):
        root=stage/'winuae'/(row['batch']+'-'+row['cpu'])
        assert row['completed'] and row['guestReturn']=='0 0\n' and not row['jit']
        assert (root/'sys/CASES.FIX').read_bytes()==(stage/'runs'/root.name/'CASES.FIX').read_bytes()
        assert (root/'sys/Exe2ArcQualification').read_bytes()==(stage/'guest/Exe2ArcQualification').read_bytes()
        for label,key in [('baseline','EXE2ARCB'),('candidate','EXE2ARCC')]:
            assert (root/'sys'/key).read_bytes()==(stage/'inputs'/('Exe2Arc.'+label)).read_bytes()
        for source in (stage/'inputs').glob('*.IN'):
            target=root/'sys'/source.name
            if target.exists(): assert target.read_bytes()==source.read_bytes()
        cases=read(stage/('cases-'+row['batch']+'.json'))
        q=audit(cases,(root/'sys/PAYLOAD.OK').read_bytes());assert q['passed'],(row,q)
        winuae.append(dict(row,qualification=q))
    assert {(r['cpu'],r['batch']) for r in winuae}=={(c,b) for c in ['68000','68020','68040'] for b in ['typed','automatic','names','errors1','errors2','signals','repeat']}
    controls=read(stage/'negative-controls.json');assert len(controls)==10 and all(c['rejected'] for c in controls)
    repo=Path(__file__).resolve().parents[3]
    inventory=[]
    for frozen in sorted((stage/'installed/C').iterdir()):
        current=repo/'out/C'/frozen.name
        assert current.read_bytes()==frozen.read_bytes()
        inventory.append(identity(current))
    assert len(inventory)==61
    failed=[stage/'runs/cross-68000/result.json',stage/'runs/cross-68040/result.json',cross.parent/'exe2arc-os-cross-20261017/runs/cross-68000/result.json',cross.parent/'exe2arc-os-cross-20261017/runs/cross-68040/result.json',stage/'winuae/retry-cross-68000/result.json']
    for label in ['baseline','candidate']:
        expected_names={r['Name'] for r in contracts}
        image=0 if label=='baseline' else 1
        for cpu in ['68000','68020','68040']:
            covered=set()
            for root,batch in [(stage,b) for b in batches]+[(cross,'cross-'+label)]:
                covered.update(r['Name'] for r in read(root/('cases-'+batch+'.json')) if r['Image']==image)
            assert covered==expected_names
    evidence=[]
    for root in [stage,cross]:
        for folder in ['runs','winuae']:
            evidence.extend(identity(p) for p in sorted((root/folder).rglob('*')) if p.is_file())
    result=dict(passed=True,contracts=32,nativeInvocations=sum(r['invocations'] for r in matrix),matrix=matrix,
                winuaeInvocations=sum(r['qualification']['invocations'] for r in winuae),winuae=winuae,
                baseline=identity(stage/'inputs/Exe2Arc.baseline'),candidate=identity(stage/'inputs/Exe2Arc.candidate'),
                freeze=[identity(stage/'freeze.json'),identity(cross/'freeze.json')],rom=read(stage/'freeze.json')['rom'],
                runnerSourceBinding=read(stage/'runner-source-binding.json'),negativeControls=controls,
                installed=inventory,installedTotalBytes=sum(r['bytes'] for r in inventory) if 'bytes' in inventory[0] else sum(Path(r['path']).stat().st_size for r in inventory),
                retainedLimits=[dict(file=identity(p),result=read(p)) for p in failed if p.exists()],
                tooling=[identity(p) for p in sorted(Path(__file__).parent.iterdir()) if p.is_file()]+[identity(repo/'tools/Commands/qualify_exe2arc_os.py')],evidence=evidence)
    dest=repo/'docs/Commands/Workbench31MorphOS320/exe2arc-original-dos-qualification-20261017'
    dest.with_suffix('.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    table='\n'.join(f"| {cpu} | {sum(r['invocations'] for r in matrix if r['cpu']==cpu)} | {max(r['frames'] for r in matrix if r['cpu']==cpu)} | {sum(r['qualification']['invocations'] for r in winuae if r['cpu']==cpu)} |" for cpu in ['68000','68020','68040'])
    text=f'''# Exe2Arc original DOS qualification — 2026-10-17

## Result

**Pass.** The installed receiver-fixed executable (14,652 bytes) and the repaired-source compiler control (14,680 bytes) pass 32 original-DOS contracts on MC68000, MC68020 and MC68040. This adds native OS qualification to the [runtime qualification](exe2arc-runtime-qualification-20261017.md); the compiler saving remains **28 bytes**, already counted there. Exe2Arc is MorphOS-only, so there is no Workbench 3.1 command size ratio.

The installed executable SHA-256 is `{result['candidate']['sha256']}`. All 61 installed command files remain byte-identical to the frozen inventory ({result['installedTotalBytes']:,} bytes). No command source or installed executable changed for this qualification.

| CPU | Accepted native calls, both images | Maximum frames per accepted boot | Independent WinUAE calls |
|---|---:|---:|---:|
{table}

Native total: **{result['nativeInvocations']} calls**. Independent WinUAE total: **{result['winuaeInvocations']} calls**. Warm-ups are included in these totals. A separate 8-call MC68000 smoke and earlier successful experiments are retained but excluded from these counts.

## Harness and assertions

The normal CLI guest adapts Search's frozen NativeOsSession harness. All command loading, RunCommand execution, argument parsing, file access and cleanup use real Kickstart 3.1 Exec/DOS vectors. Guest code implements no DOS service substitutes. Both segments are loaded once per boot and alternate synchronously in the ordinary batches. Cross-window baseline and candidate each run in a separate boot to respect the fixed execution budget.

The independent corpus covers explicit and automatic ZIP, ACE, RAR, CAB, ARJ, LhA and LZH recognition; ZIP offset correction; generated and quoted names; no suffix; directory fallback; missing/invalid arguments; unknown types and absent signatures; empty/missing inputs; unsafe archive offsets; illegal/truncated ZIP records; output-open failure; pending Ctrl-C; a signature straddling the scanner window; and a 102,413-byte multi-chunk extraction. Fixture payloads are inert and are never executed.

Raw binary requests and receipts are checked against exact stdout bytes, return codes, immediate IoErr, output-file presence and complete extracted bytes. Successful output handles must close and their files must delete. Ten caller-state fields must be restored, including input/output, current directory, CLI, stack fields, allocated signal mask and dos.library open count. Repeated calls exercise 4 KiB and 8 KiB requested stacks and require exactly unchanged free memory after closing/deleting extracted RAM files. Initial filesystem/library warming is recorded separately; arbitrary extra command allocations are not permitted by repeat checks. This is finite free-memory and state qualification, not an allocation-by-allocation ownership tracer.

The verifier compares raw disk bytes with decoded JSON. Ten auditor controls reject altered return code, IoErr, stdout, extracted payload, file presence, close/delete results, caller state, repeat memory and forged JSON. Original evidence is restored byte-for-byte after the forged-JSON control.

## Original system and frozen tools

- Kickstart 3.1 A500 ROM SHA-256: `{result['rom']['sha256']}`.
- Search runner snapshot: `artifacts/receiver-rollout-v3-20261017/native/Search/runner`, copied without rebuilding its emulator/OS launcher.
- Copper68k: `1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63`; SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`.
- CopperMod.Amiga.Lightweight: `1.0.0+a4e80b68a7b58e6c12b940b9d786b2ac73757965`; SHA-256 `04b17fd0e6d2e3928ff161dbcf28c402d1d7dc93b70d9160c811a839ea91cc7a`.
- WinUAE 6.0.3.0, JIT disabled; SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`.

The JSON receipt includes compiler/SDK freeze manifests, command identities, source/PDB binding details, generated guest identity, per-run cycles/frames, raw receipt hashes and installed inventory. Native runner/launcher source bindings match; the frozen lightweight binary is authoritative where local source has drifted. Driver compilation uses MC68000 resident HUNK, YOLO, memory disabled, fixed-point peepholes and code-size policy off. Target command images are the unchanged prior qualified images.

## Execution limits and qualification boundaries

Native execution retains Search's 3,000-frame and 120-second limits. Combined cross-window runs exhausted the frame budget on MC68000/MC68040, even after removing warm-ups; those incomplete receipts are retained as failed attempts, never counted as passes. Separating the two target images yields completed exact-contract checks within the same limits. No command behavior or expected output was relaxed.

WinUAE independently confirms all seven formats, automatic recognition, naming cases, parser/scanner errors, Ctrl-C and repeated invocation on all three CPUs. Its combined MC68000 cross-window attempt hit the 45-second host timeout and is retained as an independent-emulator gap. Cross-window and multi-chunk acceptance relies on completed original-DOS native harness runs; no WinUAE claim is made for those two contracts.

This is original DOS CLI execution, not MorphOS PPC execution or actual Workbench-message launch. Existing vector qualification covers Workbench startup and injected allocation/I/O failures; this run adds real missing-file/open failure paths but no native fault injection. Alternation is synchronous, not simultaneous invocation. Requested stack sizes and restoration are checked; original-OS peak stack/instruction counts are not instrumented. The prior runtime measurement remains 940-byte peak stack and 13,843,861 candidate instructions versus 13,844,949 control instructions per complete vector suite/CPU. No CopperScreen or original MorphOS executable comparison was performed.

## Reproduction and evidence

Harness sources: `tools/Commands/Exe2ArcOsQualification/`; orchestration: `tools/Commands/qualify_exe2arc_os.py`. Prepare a fresh stage, then execute; preparation freezes all target images, compiler/SDK/runtime binaries, inputs and generated guest. Current batching splits the heavy cross-window images automatically. The WinUAE confirmation script defaults to its independently qualified subset. Run `control-auditor.py` after the native repeat batch. The report builder combines this run's main stage and separate cross-window stage; new complete stages can supply both arguments identically.

Accepted evidence: `artifacts/exe2arc-os-v2-20261017` and `artifacts/exe2arc-os-cross-split-20261017`. Earlier `exe2arc-os-20261017` and `exe2arc-os-cross-20261017` experiments remain available. Machine-readable report: [qualification receipt](exe2arc-original-dos-qualification-20261017.json).
'''
    dest.with_suffix('.md').write_text(text,encoding='utf-8')
    print(dict(native=result['nativeInvocations'],winuae=result['winuaeInvocations'],passed=True))

if __name__=='__main__': report(Path(sys.argv[1]),Path(sys.argv[2]))
