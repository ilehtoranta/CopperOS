"""Compare frozen accepted commands with the bounded scalar allocator repair."""
import argparse
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import json
import shutil
import time
import re
import os
import sys
from qualify_sizes import comparable
from measure_c import identity, parse_map
from qualify_sizes import run, qualify, suites
from rollout_receiver_fix import DUAL, PRIVATE_SUITES, private_flags

REPO = Path(__file__).resolve().parents[2]
OLD = REPO/'artifacts/receiver-rollout-v3-20261017'
SEARCH = REPO/'artifacts/search-allocator-20261018'

def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p, v): p.parent.mkdir(parents=True, exist_ok=True); p.write_text(json.dumps(v, indent=2)+'\n', encoding='utf-8')

def freeze(lines, target):
    target.mkdir(parents=True)
    pairs = [l.split('=', 1) for l in lines if l.startswith(('input=', 'managed-assembly='))]
    primary = Path(next(v for k,v in pairs if k=='input'))
    # Struct-layout discovery also reads adjacent assemblies. Preserve that layout.
    for p in primary.parent.glob('*.dll'): shutil.copy2(p, target/p.name)
    for k,v in pairs: shutil.copy2(v, target/Path(v).name)
    result=[]
    for line in lines:
        if line.startswith(('output=', 'compatibility-report=')): continue
        if line.startswith(('input=', 'managed-assembly=')):
            k,v=line.split('=',1); line=k+'='+str(target/Path(v).name)
        result.append(line)
    (target/'compile.rsp').write_text('\n'.join(result)+'\n',encoding='utf-8')

def prepare(stage):
    assert not stage.exists()
    stage.mkdir(parents=True)
    shutil.copytree(REPO/'out/C',stage/'installed/C')
    for label,folder in [('baseline','compiler-baseline'),('candidate','compiler-narrow')]:
        shutil.copytree(SEARCH/folder,stage/('compiler-'+label))
    shutil.copytree(SEARCH/'runner',stage/'runner')
    shutil.copytree(OLD/'private-inputs',stage/'private-inputs')
    shutil.copytree(OLD/'private-root-builds',stage/'private-root-builds')
    names=sorted(p.name for p in (stage/'installed/C').iterdir())
    assert len(names)==61
    for name in names:
        source=OLD/'candidate'/(name+'.rsp')
        if name=='List': source=REPO/'artifacts/list-search-loops-20261018/list-comment2/shipping/List.rsp'
        if name=='Exe2Arc': source=REPO/'artifacts/exe2arc-window-kernel-20261018/combined/compile.rsp'
        if name=='Search': source=SEARCH/'narrow/shipping/shipping/compile.rsp'
        freeze(source.read_text(encoding='utf-8-sig').splitlines(),stage/'inputs'/name)
    for name in DUAL:
        lines=private_flags(stage,name)
        if name=='List': lines=(REPO/'artifacts/list-search-loops-20261018/list-comment2/morphos/List.rsp').read_text(encoding='utf-8-sig').splitlines()
        if name=='Search': lines=(SEARCH/'narrow/shipping/morphos/compile.rsp').read_text(encoding='utf-8-sig').splitlines()
        freeze(lines,stage/'profiles'/name)
    save(stage/'freeze.json',dict(commands=names,compilerBase='9eaa2aa',compilerFix='c536d31',
        files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))

def compile_one(stage, name, profile, label):
    source=stage/('inputs' if profile=='shipping' else 'profiles')/name/'compile.rsp'
    root=stage/profile/label; image=root/'C'/name
    image.parent.mkdir(parents=True,exist_ok=True)
    assert not image.exists()
    response=root/(name+'.rsp')
    response.write_text(source.read_text(encoding='utf-8-sig')+'output='+str(image)+'\n',encoding='utf-8')
    code=run(['dotnet',str(stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll'),'@'+str(response)],root/(name+'.log'))
    result=dict(command=name,profile=profile,compiler=label,exitCode=code,response=identity(response))
    if code==0: result.update(image=identity(image),map=parse_map(Path(str(image)+'.map')))
    save(root/(name+'.measurement.json'),result)
    print(name,profile,label,result.get('image',{}).get('bytes','FAILED'),flush=True)
    return result

def measure(stage):
    frozen=read(stage/'freeze.json')
    for item in frozen['files']: assert identity(item['path'])==item
    jobs=[(n,p,l) for n in frozen['commands'] for p in (['shipping','morphos'] if n in DUAL else ['shipping']) for l in ['baseline','candidate']]
    with ThreadPoolExecutor(max_workers=2) as pool: rows=list(pool.map(lambda j:compile_one(stage,*j),jobs))
    save(stage/'measurements.json',rows)
    summary=[]
    for name in frozen['commands']:
        for profile in (['shipping','morphos'] if name in DUAL else ['shipping']):
            pair={r['compiler']:r for r in rows if r['command']==name and r['profile']==profile}
            a,b=pair['baseline'],pair['candidate']; installed=stage/'installed/C'/name
            summary.append(dict(command=name,profile=profile,before=a.get('image'),after=b.get('image'),
                reproducedInstalled=profile=='shipping' and a.get('image',{}).get('sha256')==identity(installed)['sha256'],
                savedBytes=a['image']['bytes']-b['image']['bytes'] if 'image' in a and 'image' in b else None))
    save(stage/'summary.json',summary)
    print(json.dumps([{k:r[k] for k in ['command','profile','savedBytes']} for r in summary if r['savedBytes']],indent=2),flush=True)

def vectors(stage):
    results=[]
    for name in read(stage/'freeze.json')['commands']:
        profiles=['shipping','morphos'] if name in DUAL else ['shipping']
        paths=[stage/p/l/(name+'.measurement.json') for p in profiles for l in ['baseline','candidate']]
        while not all(p.exists() for p in paths): time.sleep(2)
        measures=[read(p) for p in paths]
        if name=='Search' or not any('image' in a and 'image' in b and a['image']['bytes']>b['image']['bytes'] for a,b in zip(measures[::2],measures[1::2])): continue
        for profile in (['shipping','morphos'] if name in DUAL else ['shipping']):
            overrides={name:[PRIVATE_SUITES[name]]} if profile=='morphos' else None
            if name=='Copy': overrides={name:suites()[name]+['copy-target-dispatch-native-entry-vector-fixture+directory-command']}
            result=qualify(stage/profile/'baseline',stage/profile/'candidate',stage/'runner/CopperOS.Commands.NativeExecution.dll',[name],overrides)[0]
            results.append(dict(command=name,profile=profile,result=result))
            save(stage/'vectors.json',results)

def audit(stage):
    frozen=read(stage/'freeze.json')
    for item in frozen['files']: assert identity(item['path'])==item
    previous=read(REPO/'docs/Commands/Workbench31MorphOS320/search-register-allocation-20261018.json')
    assert previous['passed'] and int(previous['compilerTests']['counters']['passed'])==717
    assert identity(previous['compilerTests']['receipt']['path'])==previous['compilerTests']['receipt']
    for label,old in [('baseline','compiler-baseline'),('candidate','compiler-narrow')]:
        for path in (stage/('compiler-'+label)).iterdir():
            if path.is_file(): assert path.read_bytes()==(SEARCH/old/path.name).read_bytes()
    summary=read(stage/'summary.json'); vectors=read(stage/'vectors.json')
    for row in summary:
        for key in ['before','after']:
            if row[key]: assert identity(row[key]['path'])==row[key]
        if row['profile']=='shipping' and row['command']!='Search':assert row['reproducedInstalled']
    checked=[]
    for row in vectors:
        name,profile=row['command'],row['profile']; result=row['result']
        expected_suites=[PRIVATE_SUITES[name]] if profile=='morphos' else suites()[name]
        if name=='Copy':expected_suites+=['copy-target-dispatch-native-entry-vector-fixture+directory-command']
        assert {(c['cpu'],c['suite']) for c in result['checks']}=={(cpu,suite) for cpu in ['68000','68020','68040'] for suite in expected_suites}
        for check in result['checks']:
            raw=[]
            for label in ['baseline','candidate']:
                root=stage/profile/label
                receipt=root/'execution'/f'{name}.{check["cpu"]}.{check["suite"]}.json'
                data=read(receipt)
                assert data['imageSha256']==identity(root/'C'/name)['sha256']
                assert data['managedExecutorSha256']==identity(stage/'runner/CopperOS.Commands.NativeExecution.dll')['sha256']
                assert data['instructionCoreSha256']=='99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f'
                if check['passed']: assert data['status']=='passed' and not data.get('observationOnly',False)
                raw.append(data)
            if check['passed']:
                a,b=[r.get('cases',r.get('observations')) for r in raw]
                assert len(a)==len(b)>0
                assert [comparable(c) for c in a]==[comparable(c) for c in b]
                assert all(c.get('peakStackBytes',c['stackBytesWritten'])<=c.get('configuredStackBytes',8192) for c in b)
        checked.append(row)
    decisions=[]
    references={r['command']:r.get('workbench31') for r in read(OLD/'decisions.json')}
    for name in frozen['commands']:
        sizes=[r for r in summary if r['command']==name]
        shipping=next(r for r in sizes if r['profile']=='shipping')
        tests=[r for r in checked if r['command']==name]
        smaller=bool(shipping['savedBytes'] and shipping['savedBytes']>0)
        profiles=['shipping','morphos'] if name in DUAL else ['shipping']
        qualified=all(any(t['profile']==p and t['result']['qualified'] for t in tests) for p in profiles)
        no_growth=all(r['savedBytes'] is not None and r['savedBytes']>=0 for r in sizes)
        eligible=smaller and qualified and no_growth and shipping['reproducedInstalled']
        if name=='Search': eligible=False
        decisions.append(dict(command=name,accepted=eligible,sizes=sizes,qualification=tests,workbench31=references.get(name),
            reason=('Smaller shipping image; both available profiles pass.' if eligible else
                'Already qualified and installed in the Search allocator investigation.' if name=='Search' else
                'No shipping size reduction.' if not smaller else
                'Hold: baseline does not reproduce installed image.' if not shipping['reproducedInstalled'] else
                'Hold: profile grows.' if not no_growth else 'Hold: qualification incomplete or failed.')))
    native={}
    for name in ['Dir']:
        if any(r['command']==name and r['accepted'] for r in decisions):
            native[name]=audit_dir(stage)
    methods=[]
    for row in summary:
        if not row['savedBytes']: continue
        name,profile=row['command'],row['profile'];pair=[]
        for label in ['baseline','candidate']:
            path=stage/profile/label/'C'/(name+'.map')
            records=parse_map(path)['records']
            sizes={m[1]:int(m[0]) for m in re.findall(r'^[0-9A-F]{8}\s+(\d+)\s+(.+::.+)$',path.read_text(encoding='utf-8-sig'),re.M)}
            pair.append((records,{r['method']:r for r in records.get('ALLOCATION',[])},sizes))
        a,b=pair
        changes=[dict(method=m,before=a[1].get(m),after=b[1].get(m)) for m in sorted(a[1].keys()|b[1].keys()) if a[1].get(m)!=b[1].get(m)]
        bodies=[dict(method=m,beforeBytes=a[2].get(m),afterBytes=b[2].get(m)) for m in sorted(a[2].keys()|b[2].keys()) if a[2].get(m)!=b[2].get(m)]
        methods.append(dict(command=name,profile=profile,allocationChanges=changes,bodyChanges=bodies))
    helper=audit_softlink(stage)
    data=dict(freeze=frozen,decisions=decisions,methodChanges=methods,measurements=read(stage/'measurements.json'),tool=identity(Path(__file__)),native=native,softLinkHelper=helper,
        compilerQualification=previous['compilerTests'],compilerPatch=identity(SEARCH/'allocator-fix.patch'),
        compilerProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/search-register-allocation-20261018.json'),
        receipts=[identity(p) for p in sorted(stage.rglob('*.json')) if 'execution' in p.parts])
    save(stage/'audit.json',data)
    return data

def audit_softlink(stage):
    from append_hunk_version import parse_hunk
    from investigate_dir_size import methods
    import struct
    frozen=read(stage/'helper-runner-build/freeze.json')
    for item in frozen: assert identity(item['path'])==item
    runner=stage/'helper-runner-build/vector-runner/CopperOS.Commands.NativeExecution.dll'
    result=read(stage/'helper/qualification.json')[0];assert result['qualified'] and len(result['checks'])==3
    assert {c['cpu'] for c in result['checks']}=={'68000','68020','68040'}
    for check in result['checks']:
        cases=[]
        for label in ['baseline','candidate']:
            root=stage/'helper'/label
            receipt=read(root/'execution'/f'SoftLink.{check["cpu"]}.copy-softlink-native-entry-vector-fixture.json')
            assert receipt['status']=='passed' and len(receipt['cases'])==9
            assert receipt['imageSha256']==identity(root/'C/SoftLink')['sha256']
            assert receipt['managedExecutorSha256']==identity(runner)['sha256']
            assert receipt['instructionCoreSha256']==identity(runner.parent/'Copper68k.dll')['sha256']
            assert receipt['instructionCoreSha256']=='99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f'
            assert all(c.get('peakStackBytes',c['stackBytesWritten'])<=c.get('configuredStackBytes',8192) for c in receipt['cases'])
            cases.append(receipt['cases'])
        assert [comparable(c) for c in cases[0]]==[comparable(c) for c in cases[1]]
    name='CopperOS.Commands.Native.NativeMorphOSCopySoftLinkCheck::CanEnter'
    bindings=[]
    def normalized(path):
        hunk=parse_hunk(path.read_bytes());method=methods(Path(str(path)+'.map'))[name]
        start=method['offset'];body=bytearray(hunk.code[start:start+method['bytes']]);literals=[]
        for offset in hunk.relocations:
            if start<=offset<start+len(body):
                assert offset+4<=start+len(body)
                target=struct.unpack_from('>I',hunk.code,offset)[0]
                literal=hunk.code[target:hunk.code.index(b'\0',target)]
                assert literal in [b'',b'Warning: Skipping dangling softlink %s -> %s\n']
                literals.append(dict(offset=offset-start,literal=literal.decode('latin1')))
                body[offset-start:offset-start+4]=b'\0'*4
        assert len(literals)==2
        return bytes(body),literals
    for label in ['baseline','candidate']:
        probe=normalized(stage/'helper'/label/'C/SoftLink')
        for profile in ['shipping','morphos']:
            path=stage/profile/label/'C/Delete'
            actual=normalized(path);assert actual==probe
            bindings.append(dict(compiler=label,profile=profile,method=name,bytes=len(actual[0]),literals=actual[1],image=identity(path)))
    return dict(passed=True,qualification=result,bindings=bindings,freeze=frozen)

def audit_dir(stage):
    import qualify_dir_fixed as original
    import struct
    root=stage/'original-dos/Dir'
    evidence=read(root/'evidence.json')
    for item in read(root/'native-freeze.json')+evidence['files']: assert identity(item['path'])==item
    matrix=[]
    for profile,folder in [('shipping','native'),('morphos','native-morphos')]:
        target=root/folder
        for label in ['baseline','candidate']:
            assert (target/'inputs'/('Dir.'+label)).read_bytes()==(stage/profile/label/'C/Dir').read_bytes()
        bindings=read(target/'runner-source-binding.json')
        assert all(d['matches'] for row in bindings[:2] for d in row['documents'])
        contracts={r['Name']:r for r in read(target/'contracts.json')};assert len(contracts)==27
        controls=read(target/'negative-controls.json');assert len(controls)==4 and all(r['rejected'] for r in controls)
        for cpu in ['68000','68020','68040']:
            names=set();stacks=set();count=0
            for batch in ['part1','part2','repeat']:
                result=original.auditor()(target,batch,cpu);assert result['passed']
                assert result['result']['RomSha256'].lower()=='8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
                observed=read(target/'runs'/(batch+'-'+cpu)/'observations.json')['observations']
                names.update(r['Name'] for r in observed);stacks.update(r['Stack'] for r in observed)
                count+=result['invocations'];matrix.append(dict(profile=profile,**result))
            assert count==98 and names==set(contracts) and stacks=={4096,8192}
        win=read(target/'winuae/results.json');assert {r['cpu'] for r in win}=={'68000','68020','68040'}
        faults=(target/'runs/part1-68000/FAULTS.FIX').read_bytes()
        diagnostics={struct.unpack_from('>I',faults,i)[0]:faults[i+4:i+128].split(b'\0')[0].decode('latin1')+'\n' for i in range(0,len(faults),128)}
        for row in win:
            assert row['passed'] and row['completed'] and not row['jit'] and row['stackBytes']==4096
            assert row['romSha256']==read(root/'rom.json')['sha256']
            sysroot=target/'winuae'/row['cpu']/'sys';returns='';tags=set()
            for label,suffix in [('baseline','B'),('candidate','C')]:
                assert (sysroot/'C'/('Dir'+suffix)).read_bytes()==(target/'inputs'/('Dir.'+label)).read_bytes()
                for key,name in [('full','explicit'),('files','files'),('dirs','dirs'),('ignored','opt-ignored'),('all','all'),('opt-i','opt-i'),('ignored-lower','opt-lower-ignored'),('ignored-multiple','opt-multiple-ignored')]:
                    contract=contracts[name];expected=contract['expectedOutput']
                    if isinstance(expected,dict):expected=expected.get('prefix','')+diagnostics[expected['fault']]
                    tag=suffix+'-'+key;tags.add(tag);check=next(c for c in row['checks'] if c['name']==tag)
                    assert check['passed'] and check['actual']==check['expected']==expected==(sysroot/(tag+'.txt')).read_bytes().decode('latin1')
                    returns+=tag+' '+str(contract['expectedReturn'])+' '+str(contract['expectedIoErr'])+'\n'
            assert len(row['checks'])==16 and {c['name'] for c in row['checks']}==tags
            assert row['returns']==row['expectedReturns']==returns==(sysroot/'returns.txt').read_bytes().decode('latin1')
    return dict(passed=True,matrix=matrix,evidence=evidence)

def publish(stage):
    data=audit(stage)
    installed=stage/'installed/C'
    assert all((REPO/'out/C'/p.name).read_bytes()==p.read_bytes() for p in installed.iterdir())
    accepted=stage/'accepted/C'; assert not accepted.exists()
    shutil.copytree(installed,accepted)
    selected=[r['command'] for r in data['decisions'] if r['accepted']]
    for name in selected: shutil.copy2(stage/'shipping/candidate/C'/name,accepted/name)
    assert len(list(accepted.iterdir()))==61
    assert sum(p.stat().st_size for p in accepted.iterdir())<sum(p.stat().st_size for p in installed.iterdir())
    for name in selected: shutil.copy2(accepted/name,REPO/'out/C'/name)
    assert all((REPO/'out/C'/p.name).read_bytes()==p.read_bytes() for p in accepted.iterdir())
    finalize(stage)

def finalize(stage):
    data=audit(stage)
    installed=stage/'installed/C';accepted=stage/'accepted/C'
    selected=[r['command'] for r in data['decisions'] if r['accepted']]
    assert {p.name for p in accepted.iterdir()}=={p.name for p in installed.iterdir()}=={p.name for p in (REPO/'out/C').iterdir()}
    assert len(list(accepted.iterdir()))==61
    for p in accepted.iterdir():
        expected=stage/'shipping/candidate/C'/p.name if p.name in selected else installed/p.name
        assert p.read_bytes()==expected.read_bytes()==(REPO/'out/C'/p.name).read_bytes()
    data.update(installedInventory=[identity(p) for p in sorted(accepted.iterdir())],accepted=selected,
        totalBefore=sum(p.stat().st_size for p in installed.iterdir()),totalAfter=sum(p.stat().st_size for p in accepted.iterdir()))
    save(stage/'published.json',data)
    save(REPO/'docs/Commands/Workbench31MorphOS320/scalar-allocator-rollout-20261018.json',data)
    report(stage)

def report(stage):
    data=read(stage/'published.json');accepted=[r for r in data['decisions'] if r['accepted']]
    lines=['# Scalar register allocator rollout — 2026-10-18','','## Accepted images','',
        '| Command | Installed before | Accepted | Saved | MorphOS before → after | Workbench 3.1 ratio |','|---|---:|---:|---:|---:|---:|']
    for row in accepted:
        shipping=next(r for r in row['sizes'] if r['profile']=='shipping');private=next((r for r in row['sizes'] if r['profile']=='morphos'),None)
        reference=row['workbench31'];ratio=f"{shipping['after']['bytes']/reference['bytes']:.3f}×" if reference else '—'
        lines.append(f"| {row['command']} | {shipping['before']['bytes']:,} | {shipping['after']['bytes']:,} | {shipping['savedBytes']} | "+(f"{private['before']['bytes']:,} → {private['after']['bytes']:,}" if private else 'same implemented body')+f' | {ratio} |')
    lines+=['',f"Refreshed only these {len(accepted)} executables in `out/C`. All 61 images remain present: **{data['totalBefore']:,} → {data['totalAfter']:,} bytes**, saving **{data['totalBefore']-data['totalAfter']} bytes**. Every other installed hash is unchanged. Search’s previously accepted 4-byte saving is excluded.",'',
        '### Other findings','',
        '- Info’s MorphOS profile saves **88 bytes** (9,588 → 9,500); its 3,420-byte shipping image is unchanged. Both profiles pass.',
        '- Version’s MorphOS profile saves **32 bytes** (19,768 → 19,736); its 8,548-byte shipping image is unchanged. Both profiles pass. Its MorphOS fixture gains 819 instructions per CPU, with unchanged peak stack.',
        '- Type grows **4 bytes** in both profiles (4,696 → 4,700), so its installed image is retained. Its `Copy` helper still has four spilled values while saved-register storage increases from 36 to 40 bytes. It receives no adoption claim or new runtime qualification.',
        '- List, Execute and Exe2Arc do not shrink with this fix. Their installed images are retained.', '',
        '## Controlled comparison','',
        'Compared all 61 accepted shipping command assemblies and 35 distinct alternate profiles using frozen complete adjacent dependencies and unchanged response flags. Both compilers are retained from the Search allocator investigation: clean `9eaa2aa` and that exact compiler plus the bounded narrow-scalar reassignment committed as `c536d31`. Compiler binaries, source patch, SDK identities and the 717 passing compiler regressions remain bound by the preceding Search qualification report. Unrelated current CopperSharp and CopperOS edits were excluded.',
        '', 'The policy remains opt-in through the existing `SimplifyLoopPhis` setting. There are no command source, option, stack-limit or build-policy changes. Images use MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and symbols off. Effective passes, code/data sizes, reachable methods, relocations, allocation records and largest methods are recorded in the JSON and retained maps.',
        '', 'Shipping baselines must reproduce the installed bytes before adoption. Search intentionally reproduces its pre-fix 5,192-byte baseline and its already installed 5,188-byte candidate. Complete CIL/dependency directories are frozen to retain the compiler’s adjacent-assembly metadata discovery.',
        '', '## Qualification and measured costs','',
        'For every size beneficiary, both available distinct profiles were exercised with the same exact baseline and candidate binaries on 68000, 68020 and 68040. The supplied-vector fixtures compare output bytes, return codes, IoErr and command-visible resource/ownership observations, repeated invocation and their supported interleavings. Candidate peak stack remains within the fixtures’ existing configured limits. Compiler-owned invocation context is accounted separately by the existing fixtures.',
        '', '| Command/profile | Calls per image/CPU | Instructions before → after | Peak stack before → after |','|---|---:|---:|---:|']
    calls=0
    for row in data['decisions']:
        for profile in row['qualification']:
            if not profile['result']['qualified']:continue
            checks=profile['result']['checks'];first=[c for c in checks if c['cpu']=='68000'];calls+=sum(c['invocations']*2 for c in checks)
            lines.append(f"| {row['command']}/{profile['profile']} | {sum(c['invocations'] for c in first)} | {sum(c['instructionsBefore'] for c in first):,} → {sum(c['instructionsAfter'] for c in first):,} | {max(c['peakStackBefore'] for c in first)} → {max(c['peakStackAfter'] for c in first)} |")
    lines+=['',f'Size-beneficiary vector coverage: **{calls:,} invocations** across both images and all three CPUs. These are executed instruction counts, not wall-clock timing claims. Per-CPU values and any individual-case stack changes remain in the raw receipts. Size takes priority over execution speed.', '',
        'Vector executor: Copper68k **1.5.1**, SHA-256 `99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f`. The complete executor and core hashes are frozen.', '']
    if 'Dir' in data['native']:
        lines+=['### Dir under original DOS','',
            '**588 original Kickstart 3.1 DOS invocations** pass across both profiles and all three CPUs, covering 27 finite contracts at the existing 4 KiB/8 KiB stacks. Eight auditor mutation controls are rejected. The independent WinUAE runs pass **96 command invocations** (eight cases, both images, both profiles and all three CPUs), with JIT disabled and 4 KiB stacks. Output files and return/IoErr records are rechecked against the declared contracts, including OPT Z, lowercase ignored options and multiple ignored options.', '',
            'The retained native executor uses Copper68k **1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63**, SHA-256 `5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2`. WinUAE is **6.0.3.0**, SHA-256 `f3e55700fd811cd543fdfebf6c3221cfaa3d385828f110c6d3eb534c64503123`. ROM SHA-256: `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`. Source bindings, complete native runtime identities, guest inputs and raw results are retained. Original DOS runs exercise the MC68000 MorphOS-profile binary on Kickstart DOS; they are not original MorphOS execution.', '']
    lines+=['### Focused Delete soft-link qualification','',
        'Delete’s eight command cases do not descend through the changed `CanEnter` helper. Added **54 supplementary invocations**: nine helper cases, both compilers and all three CPUs. These cover lock success, other lock errors, missing device, non-link, dangling link, quiet/warning output, allocation failure and interleaved callers. The helper is **314 → 306 bytes**. Its complete emitted body is identical to the helper in both Delete profiles after normalizing exactly two HUNK relocations, whose targets are verified as the same empty string and warning text. All remaining instructions, branches, register saves, stack offsets and call operands must match byte for byte.', '',
        'The first supplementary run exposed an existing fixture bug: the generic allocation fallback expected eight bytes for the 512-byte soft-link buffer, failing identically with both compilers. Corrected the fixture to require exactly one 512-byte Public allocation, preserve failure injection, and verify exact ownership/release. The isolated rebuilt runner and sources are frozen separately; the complete command matrix retains its original executor. No compiler or command source change was needed. Supplementary instructions fall from 1,493 to 1,490 per CPU, while peak stack rises from 96 to 100 bytes within the existing limit.', '']
    lines+=['## Where allocation improves','']
    for row in data['methodChanges']:
        lines+=[f"### {row['command']}/{row['profile']}",'']
        for change in row['allocationChanges']:
            a,b=change['before'],change['after']
            if a and b:lines.append(f"- `{change['method']}`: spills {a['spilled']} → {b['spilled']}, frame {a['frame-bytes']} → {b['frame-bytes']} bytes, saved registers {a['saved-bytes']} → {b['saved-bytes']} bytes.")
        lines+=['']
    lines+=['## Complete size sweep','', '| Command | Shipping before → candidate | Saved | Alternate before → candidate | Decision |','|---|---:|---:|---:|---|']
    for row in data['decisions']:
        a=next(r for r in row['sizes'] if r['profile']=='shipping');b=next((r for r in row['sizes'] if r['profile']=='morphos'),None)
        lines.append(f"| {row['command']} | {a['before']['bytes']:,} → {a['after']['bytes']:,} | {a['savedBytes']} | "+(f"{b['before']['bytes']:,} → {b['after']['bytes']:,}" if b else '—')+f" | {'accepted' if row['accepted'] else row['reason']} |")
    lines+=['','## Qualification limits','',
        'Copy and Beep ship their sole implemented MorphOS-derived body; they do not have a second independent Workbench body. MorphOS-only commands likewise receive MC68000 measurements without PPC size comparisons. Workbench ratios use the genuine media identities in the receiver-classification report.',
        '', 'Supplied-vector fixtures are finite checks with emulated OS calls. They do not establish exhaustive equivalence to original commands or original MorphOS/PPC execution. Original-image and CopperScreen checks are unavailable for commands without a retained native OS harness. No new minimum-stack or unsupported interleaving guarantee is inferred.',
        '', 'Evidence: `artifacts/scalar-allocator-rollout-20261018`. Reproduce with `tools/Commands/rollout_scalar_allocator.py prepare`, `measure`, `vectors`, relevant native modes, `audit` and `publish` using a fresh staging directory. Baseline executables, responses, complete dependencies, compilers, maps and execution receipts are preserved. Publication stages the complete 61-file inventory before replacing only accepted commands.']
    (REPO/'docs/Commands/Workbench31MorphOS320/scalar-allocator-rollout-20261018.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')

def native_dir(stage):
    import qualify_dir_fixed as original
    root=stage/'original-dos/Dir'; root.mkdir(parents=True)
    previous=REPO/'artifacts/dir-format-fix-20261017/native'
    tools=REPO/'tools/Commands/DirOsQualification'
    for profile,folder in [('shipping','native'),('morphos','native-morphos')]:
        target=root/folder;target.mkdir()
        for child in ['host','guest','runner']: shutil.copytree(previous/child,target/child)
        (target/'inputs').mkdir()
        for filename in ['TEXT.IN','LONG.IN']: shutil.copy2(previous/'inputs'/filename,target/'inputs'/filename)
        for label in ['baseline','candidate']:
            shutil.copy2(stage/profile/label/'C/Dir',target/'inputs'/('Dir.'+label))
        assert run([sys.executable,str(tools/'cases.py'),str(target),'workbench' if profile=='shipping' else 'morphos','--extended'],target/'cases.log')==0
    save(root/'freeze.json',dict(files=[]))
    save(root/'native-freeze.json',[identity(p) for p in sorted(root.rglob('*')) if p.is_file() and p.name not in ['freeze.json','native-freeze.json']])
    os.environ['COPPERSHARP_KICKSTART31_ROM']='C:/Data/ROM/kickstart-3.1-a500.rom'
    original.native(root)
    for profile,folder in [('shipping','native'),('morphos','native-morphos')]:
        target=root/folder
        assert run([sys.executable,str(tools/'control-auditor.py'),str(target)],target/'control-auditor.log')==0
        args=['pwsh','-NoProfile','-File',str(tools/'confirm-winuae.ps1'),'-Stage',str(target),'-Extended']
        if profile=='morphos': args+=['-MorphOS']
        assert run(args,target/'winuae.log')==0
    save(root/'evidence.json',dict(native=read(root/'native.json'),
        winuae={p:read(root/f/'winuae/results.json') for p,f in [('shipping','native'),('morphos','native-morphos')]},
        controls={p:read(root/f/'negative-controls.json') for p,f in [('shipping','native'),('morphos','native-morphos')]},
        files=[identity(p) for p in sorted(root.rglob('*')) if p.is_file() and p.suffix in ['.dll','.json','.ps1','.py']]))

def softlink(stage):
    lines=(stage/'profiles/Delete/compile.rsp').read_text(encoding='utf-8-sig').splitlines()
    lines=[l for l in lines if not l.startswith('entry=')]
    lines+=['entry=CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSCopySoftLinkCheckEntry::Main']
    freeze(lines,stage/'profiles/SoftLink')
    for label in ['baseline','candidate']:
        assert compile_one(stage,'SoftLink','helper',label)['exitCode']==0
    # helper_runner builds the corrected strict-allocation fixture separately.

def helper_runner(stage):
    from qualify_dir_format_fix import snapshot_project
    build=stage/'helper-runner-build';build.mkdir()
    shutil.copytree(stage/'inputs/Delete',build/'sdk')
    snapshot_project(build,'Commands.NativeExecution','CopperOS.Commands.NativeExecution',True)
    result=qualify(stage/'helper/baseline',stage/'helper/candidate',build/'vector-runner/CopperOS.Commands.NativeExecution.dll',
        ['SoftLink'],{'SoftLink':['copy-softlink-native-entry-vector-fixture']})
    assert result[0]['qualified']
    save(stage/'helper/qualification.json',result)
    save(build/'freeze.json',[identity(p) for p in sorted(build.rglob('*')) if p.is_file() and 'obj' not in p.parts])

if __name__=='__main__':
    p=argparse.ArgumentParser(__doc__);p.add_argument('mode',choices=['prepare','measure','vectors','audit','publish','finalize','native_dir','softlink','helper_runner']);p.add_argument('--stage',type=Path,required=True)
    a=p.parse_args();globals()[a.mode](a.stage.resolve())
