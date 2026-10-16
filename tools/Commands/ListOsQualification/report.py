"""Audit the complete native List matrix and generate a finite-qualification report."""
import hashlib
import json
import shutil
import sys
from pathlib import Path

repo=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(repo/'tools/Commands'))
from measure_c import identity
from verify import read, verify

stage=Path(sys.argv[1]).resolve()
output=Path(sys.argv[2]) if len(sys.argv)>2 else repo/'docs/Commands/Workbench31MorphOS320/list-os-qualification-20261016'
frozen=read(stage/'freeze.json')
for files in frozen.values():
 for row in files:
  assert identity(row['path'])==row,('Frozen input changed',row['path'])
for path in (stage/'host').glob('*.dll'):
 origin=stage/'runner'/path.name
 if origin.exists(): assert identity(path)['sha256']==identity(origin)['sha256']
assert (repo/'out/C/List').read_bytes()==(stage/'inputs/List.candidate').read_bytes()
assert (stage/'inputs/List.baseline').read_bytes()==(repo/'artifacts/list-pressure-20261016/baseline/C/List').read_bytes()
contracts=read(stage/'contracts.json'); assert len(contracts)==32
matrix=[]
for cpu in ['68000','68020','68040']:
 for batch in ['part1','part2','part3','repeat']:
  row=verify(stage,batch,cpu)
  assert row['passed'],row['failures']
  directory=stage/'runs'/f'{batch}-{cpu}'
  (directory/'qualification.json').write_text(json.dumps(row,indent=2)+'\n',encoding='utf-8')
  assert row['result']['Frames']<=3000 and row['result']['ExecutionMode']=='Interpreter'
  assert row['result']['RomSha256'].lower()=='8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
  assert row['result']['HunkSha256'].lower()==identity(stage/'guest/ListQualification')['sha256']
  expected_inputs={'LISTB':'List.baseline','LISTC':'List.candidate','TEXT.IN':'TEXT.IN','LONG.IN':'LONG.IN'}
  observed=read(directory/'observations.json')
  for name,file in expected_inputs.items(): assert observed['inputs'][name]==identity(stage/'inputs'/file)['sha256']
  assert observed['inputs']['CASES.FIX']==identity(directory/'CASES.FIX')['sha256']
  row['files']=[identity(p) for p in sorted(directory.iterdir()) if p.is_file()]
  matrix.append(row)
assert sum(row['invocations'] for row in matrix)==300
assert verify(stage,'smoke','68000')['passed']
controls=read(stage/'negative-controls.json'); assert len(controls)==4 and all(r['rejected'] for r in controls)
independent=read(stage/'winuae/results.json'); assert {r['cpu'] for r in independent}=={'68000','68020','68040'}
for row in independent:
 assert row['completed'] and row['passed'] and row['jit'] is False and row['stackBytes']==4096
 for image in ['baseline','candidate']:
  assert row['outputs'][image+'-quick']=='TEXT.IN\n'
  expected='TEXT.IN'.ljust(24)+' '+'19'.rjust(7)+' ----rwed\n'
  assert row['outputs'][image+'-metadata']==expected==row['outputs'][image+'-to']
  assert row['outputs'][image+'-stdout']==''
  assert (stage/'winuae'/row['cpu']/'sys/C'/('ListB' if image=='baseline' else 'ListC')).read_bytes()==(stage/'inputs'/f'List.{image}').read_bytes()
 expected_returns=''.join(f'{image}-{mode} 0 0\n' for image in ['baseline','candidate'] for mode in ['quick','metadata','to'])
 assert row['outputs']['returns']==expected_returns
 assert row['romSha256']==matrix[0]['result']['RomSha256'].lower()
bindings=read(stage/'runner-source-binding.json')
assert all(d['matches'] for module in bindings[:2] for d in module['documents'])
engine_drift=[d for d in bindings[2]['documents'] if not d['matches']]
# Source snapshots accompany the exact executed payload hashes. Snapshotting
# never substitutes for native command execution or for the PDB checks above.
sources=[]
for path in sorted(Path(__file__).parent.iterdir()):
 if path.is_file():
  target=stage/'source'/path.relative_to(repo)
  target.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(path,target)
  sources.append(dict(original=identity(path),snapshot=identity(target)))
for path in [repo/'tools/Commands/SearchOsQualification/Bind.cs',repo/'src/Commands/List/Native/NativeMorphOSListCommand.cs',repo/'src/Commands/List/Native/NativeWorkbench31ListCommand.cs']:
 target=stage/'source'/path.relative_to(repo); target.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(path,target)
 sources.append(dict(original=identity(path),snapshot=identity(target)))
inventory=[identity(path) for dirname in ['runner','compiler','sdk','inputs','guest','host'] for path in sorted((stage/dirname).rglob('*')) if path.is_file()]
extra=[identity(p) for pattern in ['*.json','winuae/**/*.json','winuae/**/*.uae','winuae/**/Startup-Sequence'] for p in sorted(stage.glob(pattern)) if p.is_file()]
report=dict(date='2026-10-16',status='passed-finite-original-dos-qualification',scope='Original Kickstart DOS LoadSeg/RunCommand with unchanged baseline and installed List candidate; no supplied OS vectors.',
 baseline=identity(stage/'inputs/List.baseline'),candidate=identity(stage/'inputs/List.candidate'),installed=identity(repo/'out/C/List'),
 nativeInvocations=300,matchedPairs=150,smokeInvocations=6,winuaeInvocations=18,
 contracts=contracts,matrix=matrix,winuae=independent,negativeControls=controls,
 runnerSourceBinding=bindings,engineSourceDrift=engine_drift,sources=sources,files=inventory,evidence=extra,
 runtime=read(stage/'runtime.json') if (stage/'runtime.json').exists() else None,
 limits=['Finite original Kickstart DOS integration, not original Workbench List binary parity or full option parity.',
         'Actual MorphOS execution, shipping/PURE admission, absolute minimum stack, overflow canaries and loaded-segment immutability are not established.',
         'No real allocation/read/write failure injection or separate stderr. Pending Ctrl-C uses public SetSignal, not keyboard/mid-read break.',
         'Early buffer/handler memory growth is observed; zero final repeat deltas do not establish complete OS resource reclamation.',
         'Results bind the frozen engine payload; four current engine source documents differ. Boot-inclusive frame/cycle counts are not a List speed benchmark.'])
output.with_suffix('.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
table='\n'.join(f'| {cpu} | {sum(r["invocations"] for r in matrix if r["cpu"]==cpu)} | 50 | 4096 / 8192 | pass | pass |' for cpu in ['68000','68020','68040'])
text=f'''# List original-DOS qualification — 2026-10-16

**The installed 4,684-byte List passes the finite original-DOS gate.**
All 300 full-corpus native invocations pass on 68000, 68020 and 68040.
Baseline and candidate agree byte for byte and independently meet the declared
row, diagnostic, return and IoErr contracts. WinUAE independently passes six
calls per CPU. No command, compiler or emulator implementation change was needed.
The existing List optimization and installed image remain unchanged.

## Native boundary

This adapts the Search native OS harness. A frozen NativeOsRunner boots a
RAM-backed OFS floppy with the user-provided Kickstart 3.1 A500 40.63 ROM,
SHA-256 `{matrix[0]['result']['RomSha256']}`.
Original DOS LoadSeg and RunCommand load and invoke the unchanged images.
The authored diagnostic uses native RAM files and writes binary receipts
through OFS. There are no supplied DOS vectors, host HUNK loading, guest
result injection or copied OS implementation in this execution route.

| CPU interpreter | Full-corpus calls | Matched pairs | Requested stack bytes | Caller restoration | Final repeat memory |
| --- | ---: | ---: | --- | --- | --- |
{table}

Four batches per CPU complete under the unchanged 3000-frame/120-second caps.
The largest completes at {max(r['result']['Frames'] for r in matrix)} frames.
The preliminary six-call 68000 smoke run is recorded separately and is not
counted in the 300-call matrix. Boot-inclusive frames and cycles are diagnostic,
not a payload speed result or exact accelerator timing qualification.

## Contracts and cases

The 32 declared cases cover QUICK, QUICK with DATES, full metadata, KEYS,
DATES/NODATES and all four headings, BLOCK rounding, empty files, quoted names,
wildcards, FILES/DIRS, PAT, literal SUB filtering including a metacharacter,
recursive ALL, multiple DIR inputs, SINCE/UPTO inclusion and exclusion, TO
redirection, missing paths, missing arguments, unterminated quotes, unsupported
LFORMAT, invalid dates and pending Ctrl-C.

Native SetFileDate/SetComment/SetProtection create known fixture metadata.
Separate Lock/Examine queries record sizes, keys, protection and timestamps;
known lengths, the fixed date stamp and protection bits are additionally
checked. Separate original-DOS Fault calls supply diagnostic text. Expected
rows and return/error contracts are declared before execution, rather than
learned from either command image. Multi-entry checks require exact row
multisets because directory order belongs to the native handler; every
baseline/candidate pair additionally has identical complete output bytes,
including ordering. This does not claim sorting or original Workbench parity.

The auditor independently checks raw requests and binary receipts against
host JSON. Four negative controls reject wrong return, IoErr and row contracts,
and JSON output that differs from the raw receipt; original evidence is
restored byte for byte afterward. The host's completion flag alone cannot pass
this gate.

## Cleanup and repeated invocation

Both LoadSeg instances stay loaded while calls alternate between them and
between successful, filtered, error and redirected-output cases. The final
20 calls per CPU alternate both images at 4096- and 8192-byte RunCommand stacks.
Every call restores selected input/output, current directory, CLI, parent stack
size/base/bounds, allocated signals and DOS library open count. Every final
repeat has zero AvailMem delta. Earlier per-call reductions of 48, 144 and
208 bytes are retained as observations of initial handler/buffer growth, not
credited as proof of either a command leak or complete OS resource reclamation.

TO cases require the selected-output capture to remain empty and read the
closed redirected file through original DOS. Pending Ctrl-C uses public
SetSignal, with the previous break signal restored after each invocation;
keyboard and mid-read interruption are not exercised.

## Independent WinUAE check

WinUAE {independent[0]['winuaeVersion']}, JIT disabled, repeats QUICK, NODATES metadata
and TO redirection for both images on all three CPUs. The original Kickstart
Shell runs with a 4096-byte stack and a disposable directory filesystem.
Exact outputs and all six Shell return/Result2 records per CPU pass at 0/0.
This independently changes the CPU/engine and filesystem provider boundary;
it is narrower than the complete OFS/RAM corpus.

## Images and evidence

| Image | Bytes | SHA-256 |
| --- | ---: | --- |
| Preserved baseline | {report['baseline']['bytes']:,} | `{report['baseline']['sha256']}` |
| Installed candidate | {report['candidate']['bytes']:,} | `{report['candidate']['sha256']}` |

The candidate is byte-identical to `out/C/List` and the previously qualified
normal-build image. No image was rebuilt or refreshed for this OS qualification.
[The size checkpoint](list-loop-phis-20261016.md) records the default policy,
independent compiler comparisons and supplied-vector qualification.

[The JSON report](list-os-qualification-20261016.json) binds the frozen runner,
SDK/compiler inputs, authored guest/host, request corpus, binary receipts,
final disk hashes, native observations, independent WinUAE results and auditor
controls. Raw evidence is `artifacts/list-os-qualification-20261016`.
Reproduction instructions are in
[ListOsQualification](../../../tools/Commands/ListOsQualification/README.md).

The runner and launcher PDB IDs match their frozen modules, and all ten of
their source documents match. Of 50 engine documents, 46 match and four differ:
LightweightA500Machine, LightweightCopper, LightweightDiskDma and generated
AssemblyInfo. Therefore this qualifies the exact frozen engine binary, not
the current dirty CopperScreen source checkout. Runtime versions and binary
hashes are retained in JSON. ROM bytes remain an external user-owned input.
No primary compiler or CopperScreen checkout was modified.

## Remaining limits

This closes the recorded finite List original-DOS gate. It does not establish
original Workbench command/option parity, actual MorphOS runtime behavior,
shipping/PURE admission, loaded-segment immutability, absolute minimum stack,
overflow canaries, a general leak proof or a speed improvement. Real
allocation/read/write failure injection and separate stderr remain outside
this corpus. No production behavior changes, assertion weakening or golden
output replacements were needed. This report records qualification before the changes were committed.
'''
output.with_suffix('.md').write_text(text,encoding='utf-8')
print('Passed: 300 native calls, 150 matched pairs, 18 independent WinUAE calls.')
