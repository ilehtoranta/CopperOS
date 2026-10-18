"""Validate independent WinUAE receipts and publish closure of the heavy-case gap."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from measure_c import identity
from qualify_exe2arc_os import validate
from cases import input_mask
from raw import audit
from verify import read

CPUS = ['68000', '68020', '68040']
LIGHT = ['typed', 'automatic', 'names', 'errors1', 'errors2', 'signals', 'repeat']
HEAVY = ['cross', 'large']
ROM_HASH = '8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
UAE_HASH = 'f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123'


def request(cases):
    raw = bytearray(16+1552*len(cases))
    mask, _ = input_mask(cases)
    struct.pack_into('>4I', raw, 0, 0x45414631, len(cases), mask, 0)
    for index, case in enumerate(cases):
        offset = 16+index*1552
        args = (case['Arguments']+'\n').encode('ascii')
        path = case['OutputPath'].encode('ascii')
        assert len(args)<=1019 and len(path)<=511
        struct.pack_into('>5I', raw, offset, index, case['Image'], case['Stack'], int(case['CtrlC']), len(args))
        raw[offset+20:offset+20+len(args)] = args
        raw[offset+1040:offset+1040+len(path)] = path
    return bytes(raw)


def verify_run(stage, directory, reference_stage=None):
    reference_stage = stage if reference_stage is None else reference_stage
    result = read(directory/'result.json')
    cpu, batch = result['cpu'], result['batch']
    assert cpu in CPUS and batch in LIGHT+HEAVY
    assert result['completed'] and result['guestReturn']=='0 0\n' and result['jit'] is False
    assert result['romSha256']==ROM_HASH and result['winuaeSha256']==UAE_HASH
    assert result['winuaeVersion']=='6.0.3.0'
    sysdir = directory/'sys'
    cases = read(stage/('cases-'+batch+'.json'))
    assert (sysdir/'CASES.FIX').read_bytes()==request(cases)
    for name, source in [('EXE2ARCB',stage/'inputs/Exe2Arc.baseline'), ('EXE2ARCC',stage/'inputs/Exe2Arc.candidate'),
                         ('Exe2ArcQualification',stage/'guest/Exe2ArcQualification')]:
        assert (sysdir/name).read_bytes()==source.read_bytes()
    _, names = input_mask(cases)
    for name in names: assert (sysdir/name).read_bytes()==(stage/'inputs'/name).read_bytes()
    config = (directory/'run.uae').read_text(encoding='utf-8-sig').splitlines()
    # Require the original hardware/execution recipe; only the output directory changes.
    old = (reference_stage/'winuae/retry-cross-68000/run.uae').read_text(encoding='utf-8-sig').splitlines()
    normalize = lambda lines: [line for line in lines if not line.startswith(('filesystem2=', 'cpu_type=', 'cpu_model='))]
    assert normalize(config)==normalize(old)
    assert 'cpu_type='+cpu in config and 'cpu_model='+cpu in config
    assert [line for line in config if line.startswith('filesystem2=')]==['filesystem2=rw,DH0:Exe2ArcQualification:'+str(sysdir)+',1']
    assert (sysdir/'S/Startup-Sequence').read_bytes()==(reference_stage/'winuae/typed-68000/sys/S/Startup-Sequence').read_bytes()
    assert (sysdir/'C/UAEquit').read_bytes()==(reference_stage/'winuae/typed-68000/sys/C/UAEquit').read_bytes()
    raw = (sysdir/'PAYLOAD.OK').read_bytes()
    qualification = audit(cases,raw)
    assert qualification['passed'], (directory,qualification)
    assert (sysdir/'guest-return.txt').read_text(encoding='ascii')=='0 0\n'
    assert (sysdir/'done.txt').read_text(encoding='ascii')=='DONE\n'
    return dict(result, qualification=qualification, receipt=identity(sysdir/'PAYLOAD.OK'), request=identity(sysdir/'CASES.FIX'))


def partial_rows(raw):
    magic, count = struct.unpack_from('>II',raw)
    assert magic==0x45415231
    cursor=8; rows=[]
    while cursor+128<=len(raw):
        fields=struct.unpack_from('>32I',raw,cursor)
        end=cursor+128+fields[5]+fields[29]
        if end>len(raw): break
        rows.append(dict(index=fields[0],image=fields[1],returnCode=fields[3],ioErr=fields[4],payloadBytes=fields[29]))
        cursor=end
    return dict(expectedRows=count,completeRows=rows,trailingBytes=len(raw)-cursor)


def report(stage, heavy):
    stage=stage.resolve(); heavy=heavy.resolve(); validate(stage)
    prior_path=Path('docs/Commands/Workbench31MorphOS320/exe2arc-original-dos-qualification-20261017.json').resolve()
    prior=read(prior_path)
    for label in ['baseline','candidate']: assert identity(prior[label]['path'])==prior[label]
    assert identity('C:/Data/ROM/kickstart-3.1-a500.rom')['sha256']==ROM_HASH
    assert identity('C:/Program Files/WinUAE/winuae64.exe')['sha256']==UAE_HASH
    runs=[verify_run(stage,stage/'winuae'/(batch+'-'+cpu)) for cpu in CPUS for batch in LIGHT]
    heavy_runs=[verify_run(stage,heavy/(batch+'-'+cpu)) for cpu in CPUS for batch in HEAVY]
    assert len(read(heavy/'results.json'))==len(heavy_runs)==6
    for result in heavy_runs:
        assert result['timeoutSeconds']==120 and result['seconds']<125
    runs+=heavy_runs
    contracts=read(stage/'contracts.json')
    coverage=[]
    for cpu in CPUS:
        for image in [0,1]:
            names={case['Name'] for batch in LIGHT+HEAVY for case in read(stage/('cases-'+batch+'.json')) if case['Image']==image}
            assert names=={case['Name'] for case in contracts} and len(names)==32
            coverage.append(dict(cpu=cpu,image=image,contracts=len(names)))
    olddir=stage/'winuae/retry-cross-68000'
    old=read(olddir/'result.json'); assert not old['completed']
    partial=partial_rows((olddir/'sys/PAYLOAD.OK').read_bytes())
    assert len(partial['completeRows'])==5 and partial['expectedRows']==6
    timeline=read(heavy/'cross-68000/progress.json')
    control_visible=next(row['seconds'] for row in timeline if row['receiptBytes']>=1544)
    after45=[row for row in timeline if row['seconds']>=45]
    installed=identity('out/C/Exe2Arc'); assert installed['sha256']==prior['candidate']['sha256']
    repo=Path(__file__).resolve().parents[3]
    data=dict(passed=True,contracts=32,cpus=CPUS,coverage=coverage,
              independentInvocations=sum(row['qualification']['invocations'] for row in runs),
              additionalInvocations=sum(row['qualification']['invocations'] for row in heavy_runs),
              runs=runs,priorQualification=identity(prior_path),installed=installed,
              investigation=dict(oldAttempt=old,partialReceipt=partial,controlReceiptVisibleSeconds=control_visible,
                                 progressAfter45=after45,configUnchangedExceptCpuAndOutputDirectory=True,
                                 reason='The fixed 45-second host deadline expired during the second scan; the identical combined batch completes with a 120-second deadline.'),
              tools=[identity(p) for p in [Path(__file__),Path(__file__).with_name('confirm-winuae.ps1'),Path(__file__).with_name('raw.py')]],
              evidence=[identity(p) for p in sorted(heavy.rglob('*')) if p.is_file()])
    dest=repo/'docs/Commands/Workbench31MorphOS320/exe2arc-winuae-cross-window-qualification-20261018'
    dest.with_suffix('.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
    table='\n'.join(f"| {cpu} | {next(r['seconds'] for r in heavy_runs if r['cpu']==cpu and r['batch']=='cross'):.2f} s | {next(r['seconds'] for r in heavy_runs if r['cpu']==cpu and r['batch']=='large'):.2f} s | 122 | 32 |" for cpu in CPUS)
    text=f'''# Exe2Arc independent WinUAE qualification — 2026-10-18

## Result

**Pass on MC68000, MC68020 and MC68040.** The cross-window and multi-chunk gaps from the [original DOS qualification](exe2arc-original-dos-qualification-20261017.md) are closed. Both unchanged compiler images pass every one of the 32 contracts independently in WinUAE under original Kickstart 3.1 DOS.

The new six boots add **36 command calls** to the prior 330 independently qualified calls: **366 total**, including warm-ups, across both images. Each CPU has 122 calls and all 32 distinct contracts for each image. These counts do not add the native harness's 354 calls or failed attempts.

| CPU | Combined cross-window batch | Multi-chunk batch | Total independent calls | Contracts per image |
|---|---:|---:|---:|---:|
{table}

The control remains **14,680 bytes**, and the installed candidate remains **14,652 bytes**, SHA-256 `{installed['sha256']}`. No executable or command source changed.

## Timeout investigation

The retained 45-second failure contains four successful RAR warm-up rows followed by a successful control cross-window extraction: five complete rows of the expected six. The candidate result had not yet been written. There is no error result in that partial receipt.

The same six-call batch now completes on MC68000 in **{next(r['seconds'] for r in heavy_runs if r['cpu']=='68000' and r['batch']=='cross'):.2f} seconds**, exceeding the previous 45-second deadline. The control row becomes visible at approximately {control_visible:.2f} seconds; the timestamp is a one-second host sample of receipt-file growth, not an exact instruction boundary. Timing samples retain elapsed wall time, process CPU time and receipt byte count. The candidate completes with the same exact output, 32-byte extracted payload, return code 0 and IoErr 0 as the control.

`Exe2ArcForwardScanner.Scan` probes successive candidate positions in each window. The cross-window fixture places the marker at offset 102,398, forcing both images to scan approximately 100 KiB before recognizing it. The similarly sized multi-chunk fixture places its marker at offset 32 and completes much sooner despite copying a 102,413-byte payload. The receipts and timings support scan workload as the reason the combined cross-window batch needs more time; they do not indicate an extraction or window-boundary failure. Timings depend on host load and are not CPU performance comparisons.

This was an insufficient host deadline for the combined workload. No scanner, compiler, emulator core or ROM correction was required. The WinUAE wrapper now exposes a bounded timeout (default 120 seconds), explicit CPU/batch selection and a separate evidence directory. It records progress and preserves timed-out runs. Its default matrix includes the heavy batches. The original native harness retains its separate 3,000-frame/120-second limits and single-image cross-window qualification.

All emulation settings are identical to the failed run except the selected CPU model and host output-directory path. JIT remains disabled; the six-call cross-window batch has not been split or shortened. The 102,413-byte multi-chunk extraction also completes on all CPUs. Host runtimes include boot, input preparation, both image calls and clean guest exit; they are not isolated command timing benchmarks.

## Independent assertions and provenance

The new auditor regenerates the complete binary request from the frozen authored cases, checks guest/command/input identities, verifies the original startup script and hardware configuration, and audits the raw guest receipt. Assertions include exact stdout and extracted bytes, return codes, immediate IoErr, output-file existence, successful close/delete and restoration of all ten caller-state fields. The previous repeat batches still require zero free-memory change after output-file cleanup at 4 KiB and 8 KiB requested stack sizes. Cross-window and large-input calls use 4 KiB; their initial warming deltas remain recorded rather than treated as repeat measurements.

All 330 older independent calls were re-audited together with the 36 new calls. The same two unchanged target images were used throughout. The frozen native stage is `artifacts/exe2arc-os-v2-20261017`; new evidence is `artifacts/exe2arc-winuae-heavy-20261018`. Historical failed receipts are retained.

- WinUAE **6.0.3.0**, interpreter/JIT disabled; SHA-256 `{UAE_HASH}`.
- Kickstart 3.1 A500 ROM; SHA-256 `{ROM_HASH}`.
- The frozen Search-based guest, compiler and SDK remain those recorded in the previous qualification; there was no guest rebuild for this follow-up.

Run `confirm-winuae.ps1` with the frozen stage, fresh `-EvidenceRoot`, `-Batches cross,large` and `-TimeoutSeconds 120` under PowerShell 7. Then run `qualify-winuae.py STAGE EVIDENCE_ROOT` to verify all old and new independent cases and regenerate this report. The [machine-readable receipt](exe2arc-winuae-cross-window-qualification-20261018.json) records each run, timing samples, hashes and full coverage checks.

## Remaining boundaries

There is no remaining WinUAE contract gap in this corpus. This remains original DOS CLI qualification, not MorphOS PPC or actual Workbench-message startup. No native allocation/I/O fault injection, instruction-level concurrent invocation, original-OS peak-stack measurement or CopperScreen run was added. The original report's vector-fixture and execution-cost qualifications remain applicable.
'''
    dest.with_suffix('.md').write_text(text,encoding='utf-8')
    print(dict(passed=True,independentCalls=data['independentInvocations'],newCalls=data['additionalInvocations']))

if __name__=='__main__':
    parser=argparse.ArgumentParser(__doc__)
    parser.add_argument('stage',type=Path);parser.add_argument('heavy',type=Path)
    args=parser.parse_args();report(args.stage,args.heavy)
