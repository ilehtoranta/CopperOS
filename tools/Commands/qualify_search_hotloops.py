"""Frozen-image qualification for Search's byte facts and register transport."""
import argparse
from pathlib import Path
import shutil
import subprocess
import json
import xml.etree.ElementTree as ET
from measure_c import identity
from rollout_scalar_allocator import freeze, read, save
from investigate_list_search_spills import compile_all, vectors, native as native_shared
from qualify_sizes import comparable

REPO = Path(__file__).resolve().parents[2]
PREVIOUS = REPO/'artifacts/list-search-spills-20261018'
CHECKOUT = REPO.parent/'CopperSharp68k-search-hotloops-20261018'
FILES = ['Compiler/Backend/M68kValueRangeAnalysis.cs',
    'Compiler/Backend/M68kPeepholeOptimizer.TransportSize.cs',
    'Compiler/Backend/M68kPeepholeOptimizer.ControlFlowCleanup.cs',
    'Compiler/Backend/M68kPeepholeOptimizer.HotLoopTransport.cs',
    'Compiler.Tests/M68kHotLoopTransportTests.cs', 'CodeSizeHotLoops.md']

def prepare(stage):
    assert not (stage/'freeze.json').exists()
    shutil.copytree(PREVIOUS/'compiler-candidate',stage/'compiler-baseline')
    shutil.copytree(PREVIOUS/'runner',stage/'runner')
    shutil.copytree(REPO/'out/C',stage/'installed/C')
    for name in ['List','Search']:
        shutil.copy2(PREVIOUS/'shipping/candidate/C'/(name+'.map'),stage/'installed/C'/(name+'.map'))
        for profile in ['shipping','morphos']:
            source = PREVIOUS/'inputs'/profile/name/'compile.rsp'
            freeze(source.read_text(encoding='utf-8-sig').splitlines(),stage/'inputs'/profile/name)
    save(stage/'freeze.json',dict(compilerBase='daab9bc',baselinePatch=identity(PREVIOUS/'compiler-fix.patch'),
        previousProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/list-search-register-spills-20261018.json'),
        files=[identity(p) for folder in ['compiler-baseline','runner','installed','inputs'] for p in sorted((stage/folder).rglob('*')) if p.is_file()]))

def native_audit(stage, label):
    from qualify_list_search_fixed import verifier
    results=[]; evidence=[]
    rom=read(stage/'rom.json')
    assert rom['sha256']=='8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
    for name in ['List','Search']:
        root=stage/'native'/name
        for version,source in [('baseline',stage/'installed/C'/name),('candidate',stage/'shipping'/label/'C'/name)]:
            assert (root/'inputs'/(name+'.'+version)).read_bytes()==source.read_bytes()
        binding=read(root/'runner-source-binding.json')
        assert all(d['matches'] for r in binding[:2] for d in r['documents'])
        contracts=read(root/('contracts.json' if name=='List' else 'cases.json'))
        for cpu in ['68000','68020','68040']:
            names=set(); stacks=set(); count=0
            for batch in (['part1','part2','part3','repeat'] if name=='List' else ['part1','part2']):
                result=verifier(name)(root,batch,cpu)
                assert result['passed'],result.get('failures')
                raw=result['result']; assert raw['ExecutionMode']=='Interpreter' and raw['Frames']<=3000
                assert raw['RomSha256'].lower()==rom['sha256']
                assert raw['HunkSha256'].lower()==identity(root/'guest'/(name+'Qualification'))['sha256']
                observations=read(root/'runs'/(batch+'-'+cpu)/'observations.json')
                for key,file in [(name.upper()+'B',name+'.baseline'),(name.upper()+'C',name+'.candidate'),('TEXT.IN','TEXT.IN'),('LONG.IN','LONG.IN')]:
                    assert observations['inputs'][key]==identity(root/'inputs'/file)['sha256']
                assert observations['inputs']['CASES.FIX']==identity(root/'runs'/(batch+'-'+cpu)/'CASES.FIX')['sha256']
                names.update(o['Name'] for o in observations['observations']); stacks.update(o['Stack'] for o in observations['observations'])
                count+=result['invocations']; results.append(dict(command=name,**result))
            assert names=={c['Name'] for c in contracts} and stacks=={4096,8192}
            assert count==(100 if name=='List' else 64)
        controls=read(root/'negative-controls.json'); assert len(controls)==4 and all(c['rejected'] for c in controls)
        independent=read(root/'winuae/results.json'); assert {r['cpu'] for r in independent}=={'68000','68020','68040'}
        for r in independent:
            assert r['completed'] and not r['jit'] and r['stackBytes']==4096
            assert r['romSha256']==rom['sha256']
            assert r['winuaeSha256']==identity(Path('C:/Program Files/WinUAE/winuae64.exe'))['sha256']
            assert r['winuaeVersion']=='6.0.3.0'
            sysroot=root/'winuae'/r['cpu']/'sys'
            for version,suffix in [('baseline','B'),('candidate','C')]:
                assert (sysroot/'C'/(name+suffix)).read_bytes()==(root/'inputs'/(name+'.'+version)).read_bytes()
            if name=='Search':
                assert r['outputsAgree'] and r['numberedOutputCorrect']
                assert r['baseline']==r['candidate']=='     2> Needle\n'
                assert r['returns']=='baseline 0 0\ncandidate 0 0\n'
                for key in ['baseline','candidate','returns']:
                    assert (sysroot/(key+'.txt')).read_bytes().decode('latin1')==r[key]
            else:
                assert r['passed']
                expected='TEXT.IN'.ljust(24)+' '+'19'.rjust(7)+' ----rwed\n'
                assert r['outputs']['returns']==''.join(v+'-'+c+' 0 0\n' for v in ['baseline','candidate'] for c in ['quick','metadata','to'])
                for v in ['baseline','candidate']:
                    for case,text in [('quick','TEXT.IN\n'),('metadata',expected),('to',expected),('stdout','')]: assert r['outputs'][v+'-'+case]==text
                for key,text in r['outputs'].items(): assert (sysroot/(key+'.txt')).read_bytes().decode('latin1')==text
        assert identity(root/'runner/Copper68k.dll')['sha256']=='5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2'
        evidence += [identity(p) for p in root.rglob('*') if p.is_file() and p.suffix in ['.json','.ps1','.py']]
    assert verifier('List')(stage/'native/List','smoke','68000')['passed']
    return dict(results=results,rom=rom,evidence=evidence,
        core=identity(stage/'native/Search/runner/Copper68k.dll'),
        coreVersion='1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63',
        winuae=identity(Path('C:/Program Files/WinUAE/winuae64.exe')),winuaeVersion='6.0.3.0')

def native(stage,label):
    # Keep earlier prototype OS receipts intact, including their absolute paths.
    dest=stage/'final-os'; dest.mkdir()
    shutil.copy2(stage/'freeze.json',dest/'freeze.json')
    for name in ['List','Search']:
        for source,target in [(stage/'installed/C'/name,dest/'installed/C'/name),
            (stage/'shipping'/label/'C'/name,dest/'shipping'/label/'C'/name)]:
            target.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(source,target)
    native_shared(dest,label)

def inspect(stage,label):
    import re
    import sys
    from investigate_dir_size import code
    decoder=REPO/'artifacts/dir-size-regression-20261017/decoder'
    sys.path.insert(0,str(decoder))
    import capstone
    rows=[]
    for version in ['baseline',label]:
        for profile in ['shipping','morphos']:
            image=stage/profile/version/'C/Search'; raw=code(image)
            text=Path(str(image)+'.map').read_text(encoding='utf-8-sig')
            for offset,length,name in re.findall(r'^([0-9A-F]{8})[ \t]+(\d+)[ \t]+(.+::(?:FormatLines|LiteralContains))$',text,re.M):
                offset=int(offset,16); length=int(length)
                disassembler=capstone.Cs(capstone.CS_ARCH_M68K,capstone.CS_MODE_M68K_000)
                instructions=list(disassembler.disasm(raw[offset:offset+length],0))
                assert sum(i.size for i in instructions)==length
                dest=stage/(version+'-'+profile+'-'+name.rsplit('::',1)[1]+'.asm')
                dest.write_text('\n'.join(f'{i.address:04x} {i.mnemonic} {i.op_str}' for i in instructions)+'\n',encoding='utf-8')
                rows.append(dict(label=version,profile=profile,method=name,bytes=length,instructions=len(instructions),image=identity(image),disassembly=identity(dest)))
    assert len(rows)==8
    save(stage/'hotloops-final.json',rows)

def audit(stage,label):
    frozen=read(stage/'freeze.json')
    for item in frozen['files']: assert identity(item['path'])==item
    for item in read(stage/'final-os/native-freeze.json'): assert identity(item['path'])==item
    measurements={v:read(stage/(v+'.measurements.json')) for v in ['baseline',label]}
    for version,rows in measurements.items():
        assert len(rows)==4
        for row in rows:
            for key in ['image','response','mapIdentity']: assert identity(row[key]['path'])==row[key]
            baseline=stage/row['profile']/'baseline/C'/row['command']
            if version=='baseline':
                assert baseline.read_bytes()==(PREVIOUS/row['profile']/'candidate/C'/row['command']).read_bytes()
                if row['profile']=='shipping': assert baseline.read_bytes()==(stage/'installed/C'/row['command']).read_bytes()
            else: assert row['image']['bytes']<baseline.stat().st_size
    qualified=read(stage/(label+'.vectors.json')); assert len(qualified)==4
    receipts={}
    for row in qualified:
        name,profile=row['command'],row['profile']; result=row['result'][0]
        assert result['qualified'] and result['accept'] and len(result['checks'])==3
        assert {c['cpu'] for c in result['checks']}=={'68000','68020','68040'}
        for check in result['checks']:
            pair=[]
            for version in ['baseline',label]:
                root=stage/profile/version
                path=root/'execution'/f'{name}.{check["cpu"]}.{check["suite"]}.json'
                receipt=read(path); receipts[str(path)]=identity(path)
                assert receipt['status']=='passed' and not receipt.get('observationOnly',False)
                assert receipt['imageSha256']==identity(root/'C'/name)['sha256']
                assert receipt['managedExecutorSha256']==identity(stage/'runner/CopperOS.Commands.NativeExecution.dll')['sha256']
                assert receipt['instructionCoreSha256']==identity(stage/'runner/Copper68k.dll')['sha256']
                assert receipt['compilerMapSha256']==identity(root/'C'/(name+'.map'))['sha256']
                assert len(receipt['cases'])=={('List','shipping'):36,('List','morphos'):35,('Search','shipping'):30,('Search','morphos'):57}[name,profile]
                assert all(c['peakStackBytes']<=c.get('configuredStackBytes',8192) for c in receipt['cases'])
                pair.append(receipt['cases'])
            assert [comparable(c) for c in pair[0]]==[comparable(c) for c in pair[1]]
        for key in ['invocations','instructionsBefore','instructionsAfter','peakStackBefore','peakStackAfter']:
            assert len({c[key] for c in result['checks']})==1
    compiler=stage/('compiler-'+label)/'CopperSharp.Compiler.dll'
    tests=[]
    for folder,count,runner in [('tests-complete',40,'tests-focused/bin/Release/net10.0'),('tests-final40-regressions',1529,'regression-runner')]:
        paths=list((stage/folder).glob('*.trx')); assert len(paths)==1
        counts=ET.parse(paths[0]).find('.//{*}Counters').attrib
        assert int(counts['passed'])==count and int(counts['failed'])==0
        assert (stage/runner/'CopperSharp.Compiler.dll').read_bytes()==compiler.read_bytes()
        tests.append(dict(receipt=identity(paths[0]),counts=counts,runner=[identity(p) for p in (stage/runner).iterdir() if p.is_file()]))
    baseline_test=stage/'tests-baseline-complete/results/baseline.trx'
    baseline_tree=ET.parse(baseline_test); counts=baseline_tree.find('.//{*}Counters').attrib
    assert int(counts['passed'])==19 and int(counts['failed'])==21
    failures=[r.attrib['testName'] for r in baseline_tree.findall('.//{*}UnitTestResult') if r.attrib['outcome']=='Failed']
    assert all('smaller: True' in n or 'ByteLoadsRetainKnownUpperBytes' in n or
        'ZeroOperandCleanup' in n or 'AnnotatedCallInvalidates' in n for n in failures)
    assert (stage/'tests-baseline-complete/bin/Release/net10.0/CopperSharp.Compiler.dll').read_bytes()==(stage/'compiler-baseline/CopperSharp.Compiler.dll').read_bytes()
    assert (stage/'tests-baseline-complete/M68kHotLoopTransportTests.cs').read_bytes()==(stage/'source/Compiler.Tests/M68kHotLoopTransportTests.cs').read_bytes()
    assert (stage/'tests-focused/M68kHotLoopTransportTests.cs').read_bytes()==(stage/'source/Compiler.Tests/M68kHotLoopTransportTests.cs').read_bytes()
    hotloops=read(stage/'hotloops-final.json')
    for item in hotloops:
        assert identity(item['disassembly']['path'])==item['disassembly']
        assert item['image']==identity(stage/item['profile']/item['label']/'C/Search')
    data=dict(passed=True,freeze=frozen,measurements=measurements,qualification=qualified,
        vectorReceipts=list(receipts.values()),vectorInvocations=sum(len(read(Path(p))['cases']) for p in receipts),
        compilerTests=tests,baselineTests=dict(receipt=identity(baseline_test),counts=counts,failures=failures),
        native=native_audit(stage/'final-os',label),hotloops=hotloops,
        compiler=[identity(p) for p in (stage/('compiler-'+label)).iterdir() if p.is_file()],
        patch=identity(stage/'compiler-hotloops.patch'),source=[identity(stage/'source'/f) for f in FILES],
        tool=identity(Path(__file__)),helper=identity(REPO/'tools/Commands/investigate_list_search_spills.py'),
        dotnetSdk=subprocess.check_output(['dotnet','--version'],cwd=REPO).decode().strip(),
        vectorCore=identity(stage/'runner/Copper68k.dll'),vectorCoreVersion='1.5.1',label=label)
    data['nativeFreeze']=dict(receipt=identity(stage/'final-os/native-freeze.json'),
        files=read(stage/'final-os/native-freeze.json'))
    if (stage/'integration.json').exists():
        data['integration']=read(stage/'integration.json')
        for item in data['integration']['after']: assert identity(item['path'])==item
    save(stage/'audit-final.json',data)
    return data

def integrate(stage,label):
    audit(stage,label)
    main=REPO.parent/'CopperSharp68k'; patch=stage/'compiler-hotloops.patch'
    before=[]
    for f in FILES:
        if (main/f).exists():
            dest=stage/'main-before'/f; dest.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(main/f,dest); before.append(identity(dest))
    subprocess.run(['git','-C',str(main),'apply','--check',str(patch)],check=True)
    subprocess.run(['git','-C',str(main),'apply',str(patch)],check=True)
    after=[identity(main/f) for f in FILES]
    save(stage/'integration.json',dict(before=before,after=after,patch=identity(patch)))

def publish(stage,label):
    data=audit(stage,label); assert 'integration' in data
    names=sorted(p.name for p in (stage/'installed/C').iterdir() if p.is_file() and not p.suffix)
    assert len(names)==61 and {p.name for p in (REPO/'out/C').iterdir()}==set(names)
    dest=stage/'accepted/C'; dest.mkdir(parents=True)
    for name in names:
        before=stage/'installed/C'/name; assert (REPO/'out/C'/name).read_bytes()==before.read_bytes()
        source=stage/'shipping'/label/'C'/name if name=='Search' else before
        if name=='Search': assert source.stat().st_size<before.stat().st_size
        shutil.copy2(source,dest/name)
    assert {p.name for p in dest.iterdir()}==set(names)
    shutil.copy2(dest/'Search',REPO/'out/C/Search')
    assert all((REPO/'out/C'/n).read_bytes()==(dest/n).read_bytes() for n in names)
    data['publication']=dict(beforeBytes=sum((stage/'installed/C'/n).stat().st_size for n in names),
        afterBytes=sum((dest/n).stat().st_size for n in names),installed=[identity(REPO/'out/C'/n) for n in names])
    save(stage/'published.json',data)
    save(REPO/'docs/Commands/Workbench31MorphOS320/search-hot-loop-transport-20261018.json',data)

def report(stage,label):
    publication=read(stage/'published.json')['publication']
    for item in publication['installed']: assert identity(item['path'])==item
    data=audit(stage,label); data['publication']=publication
    previous=read(REPO/'docs/Commands/Workbench31MorphOS320/list-search-register-spills-20261018.json')
    comparisons=[]
    for profile in ['shipping','morphos']:
        a=next(r for r in previous['qualification'] if r['command']=='Search' and r['profile']==profile and r['comparison']==('installed' if profile=='shipping' else 'compiler'))['result'][0]['checks'][0]
        b=next(r for r in data['qualification'] if r['command']=='Search' and r['profile']==profile)['result'][0]['checks'][0]
        assert a['instructionsAfter']==b['instructionsBefore']
        comparisons.append(dict(profile=profile,preAllocator=a['instructionsBefore'],installed=b['instructionsBefore'],accepted=b['instructionsAfter']))
    data['previousAllocatorInstructionComparison']=comparisons
    save(stage/'published.json',data)
    destination=REPO/'docs/Commands/Workbench31MorphOS320/search-hot-loop-transport-20261018'
    save(destination.with_suffix('.json'),data)
    lines=['# Search hot-loop normalization and copies — 2026-10-18','','## Accepted images','',
        '| Profile | Installed baseline | Accepted | Saved |','|---|---:|---:|---:|']
    for profile in ['shipping','morphos']:
        sizes=[next(r['image']['bytes'] for r in data['measurements'][v] if r['command']=='Search' and r['profile']==profile) for v in ['baseline',label]]
        lines.append(f'| {profile} | {sizes[0]:,} | {sizes[1]:,} | {sizes[0]-sizes[1]} |')
    lines += ['',f'Updated only `out/C/Search`. The complete 61-command inventory is **{publication["beforeBytes"]:,} → {publication["afterBytes"]:,} bytes**. The other 60 installed hashes remain unchanged. List was compiled and qualified as a control; its additional 16/8-byte reductions are retained as measurements without adopting another command in this task.', '',
        'The retained genuine Workbench 3.1 Search reference is **2,476 bytes**: the accepted shipping image is **2.060×** that size. MorphOS-profile sizes are independent MC68000 measurements, without comparing against native PPC code.', '',
        '## Cause and fix','',
        'The allocator improvement exposed two separate emitter/optimizer gaps. Value-range transfer forgot the known upper 24 bits after memory byte loads. That prevented the existing boolean narrowing from replacing `SEQ; EXT.W; EXT.L; NEG.L` with `SEQ; NEG.B`. Zero staging also survived around an OR of a proven zero operand. Retaining the upper bits, removing redundant zero transport, and preserving live flags with a test eliminates five added instructions per ordinary shipping scanner byte.', '',
        'A copied register was retained even when its sole use was a comparison or arithmetic source. The bounded forwarding proof substitutes the original register when it remains unchanged and no flags or alternate entry are lost. The last matcher copy required tracking which copied bits remain observable through later byte/word writes. Its internal `Fold` helper preserves the register without reading it; the helper’s return ABI annotation had kept it conservatively live. A bounded leaf check plus the caller’s actual consumers permits forwarding while rejecting real reads, saves, clobbers, opaque instructions and nested calls.', '',
        'All four added shipping matcher copies are eliminated. `LiteralContains` returns to its pre-allocator 110-byte body. The new proof does not change register assignments, spill constraints, command source, options, startup, ownership, stack limits or build policy. Rewrites remain under the existing generated-code policy and eligibility gates; the more precise byte facts and explicit call-clobber invalidation are shared analysis corrections.', '',
        '| Hot method | Baseline body | Accepted body |','|---|---:|---:|']
    for profile in ['shipping','morphos']:
        for method in ['FormatLines','LiteralContains']:
            pair=[next(r['bytes'] for r in data['hotloops'] if r['profile']==profile and r['label']==v and r['method'].endswith('::'+method)) for v in ['baseline',label]]
            lines.append(f'| {profile}/{method} | {pair[0]} | {pair[1]} |')
    lines += ['', 'Body savings overlap with image layout, branch sizing and HUNK padding. The final two-byte matcher improvement changes the image hash while leaving its padded byte count unchanged from the first prototype; only the final exact images are accepted.', '',
        '## Execution costs and stack','', '| Profile | Calls per image/CPU | Instructions before → accepted | Change | Peak stack bytes |','|---|---:|---:|---:|---:|']
    for row in data['qualification']:
        if row['command']!='Search': continue
        c=row['result'][0]['checks'][0]; a,b=c['instructionsBefore'],c['instructionsAfter']
        lines.append(f'| {row["profile"]} | {c["invocations"]} | {a:,} → {b:,} | {(b-a)/a:+.2%} | {c["peakStackBefore"]} → {c["peakStackAfter"]} |')
    lines += ['', 'Instruction totals and stack peaks agree on 68000, 68020 and 68040. These are finite-fixture instruction counts, not cycle or wall-clock timing claims. No stack limit changed.', '',
        '| Profile | Before the previous allocator change | Current accepted | Change |','|---|---:|---:|---:|']
    for r in comparisons: lines.append(f'| {r["profile"]} | {r["preAllocator"]:,} | {r["accepted"]:,} | {(r["accepted"]-r["preAllocator"])/r["preAllocator"]:+.2%} |')
    lines += ['', 'Shipping Search now executes fewer fixture instructions than before the allocator size reduction. The MorphOS total still includes a small residual increase against that earlier compiler; this work removes the identified transport/normalization sequences and does not claim to eliminate every cost of its different allocation.', '',
        '## Qualification','',
        '**40 new compiler cases pass**: 39 execution cases and one direct call-clobber analysis case. The execution cases compare before/after images on all three CPUs with all 32 initial CCR combinations including X, varied data bytes/words, all registers, final flags, stack restoration, and guest access counts/width/order. Direct range assertions cover memory and immediate byte loads. The same cases against the frozen baseline have **21 expected failures and 19 passes**; negative safety controls pass. **1,529 existing compiler regressions pass** against the exact accepted compiler, covering dataflow/CCR, allocation, generated/resident size transformations, branch relaxation, relocation and frame preservation.', '',
        f'The final shipping and MorphOS Search entries pass **30 and 57 vector invocations per image/CPU**, respectively, on 68000/68020/68040: **522 Search invocations** across baseline and accepted images. Including List controls, **{data["vectorInvocations"]:,} vector invocations** pass. Output bytes, return codes, IoErr, releases and supported repeated/interleaved invocations match; supplied startup, parser, allocation/I/O failure and Ctrl-C cases remain covered.', '',
        'The final exact shipping image passes original Kickstart 3.1 DOS checks on all three CPUs: **192 Search invocations** at 4/8 KiB stacks, plus **6 independent WinUAE Search invocations**, JIT disabled and 4 KiB stacks. The four Search auditor mutation controls are rejected. List controls add 300 original DOS invocations, six smoke invocations, 18 WinUAE invocations and four auditor controls. The receipts independently bind input images, ROM, harness and outputs; the final image was rerun after the last matcher refinement.', '',
        '## Reproducibility and limits','',
        'The isolated compiler checkout starts at `daab9bc` plus the already qualified feasible-color patch. All comparisons use identical accepted CIL assemblies and adjacent dependencies. Fresh baselines reproduce the installed Search and List images and retained MorphOS qualification images. Command sources are unchanged. Flags remain MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off, existing size passes and stack limits.', '',
        'The accepted compiler DLL is byte-identical across CLI compilation, focused regressions and the frozen full regression runner. Compiler/source patch hashes, SDK/CIL identities, flags, rewrite counts, method and relocation metrics, maps, disassembly and receipts are preserved in the JSON and staging directory. Earlier prototypes, trace compilers and failed test-observer setups are retained separately and are not qualification evidence. Test observations now declare exact SR-read effects and make every final register/CCR observable.', '',
        f'.NET SDK `{data["dotnetSdk"]}`; vector Copper68k `1.5.1`, SHA-256 `{data["vectorCore"]["sha256"]}`. Native core `{data["native"]["coreVersion"]}`, SHA-256 `{data["native"]["core"]["sha256"]}`. WinUAE `{data["native"]["winuaeVersion"]}`, SHA-256 `{data["native"]["winuae"]["sha256"]}`. Kickstart SHA-256 `{data["native"]["rom"]["sha256"]}`.', '',
        'Original MorphOS/PPC, original-command-image and CopperScreen checks were unavailable. Original DOS/WinUAE checks cover shipping Workbench entries; the alternate profile receives the vector checks. These finite corpora do not establish exhaustive equivalence, a new minimum stack, or unsupported interleaving guarantees. Other commands are not adopted with this compiler change.', '',
        'The qualified incremental compiler patch is integrated into the main CopperSharp workspace while preserving unrelated drafts; that dirty workspace was not used for qualification. The complete 61-command accepted set was staged, inventory-checked and hash-checked before updating only Search.', '',
        'Evidence: `artifacts/search-hotloops-20261018`. Tool: `tools/Commands/qualify_search_hotloops.py` (`prepare`, `compile`, `vectors`, `native`, `inspect`, `audit`, `integrate`, `publish`, `report`).', '']
    destination.with_suffix('.md').write_text('\n'.join(lines),encoding='utf-8')

if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode',choices=['prepare','compile','vectors','native','inspect','audit','integrate','publish','report'])
    parser.add_argument('--stage',type=Path,required=True)
    parser.add_argument('--label',default='candidate')
    args=parser.parse_args(); stage=args.stage.resolve()
    if args.mode == 'prepare': prepare(stage)
    elif args.mode == 'compile': compile_all(stage,args.label)
    elif args.mode == 'vectors': vectors(stage,args.label)
    elif args.mode=='native': native(stage,args.label)
    else: globals()[args.mode](stage,args.label)
