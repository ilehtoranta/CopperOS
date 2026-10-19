"""Controlled List/Search allocator experiments with frozen accepted CIL."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import re
import xml.etree.ElementTree as ET
from measure_c import identity, parse_map
from rollout_scalar_allocator import freeze, read, save
from qualify_sizes import qualify, run, comparable

REPO = Path(__file__).resolve().parents[2]
PREVIOUS = REPO/'artifacts/type-allocator-rollout-20261018'
CHECKOUT = REPO.parent/'CopperSharp68k-list-search-spills-20261018'

def prepare(stage):
    assert not (stage/'freeze.json').exists()
    shutil.copytree(PREVIOUS/'compiler-candidate', stage/'compiler-baseline')
    shutil.copytree(PREVIOUS/'runner', stage/'runner')
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    for name in ['List','Search']:
        for profile in ['shipping','morphos']:
            folder = 'inputs' if profile == 'shipping' else 'profiles'
            freeze((PREVIOUS/folder/name/'compile.rsp').read_text(encoding='utf-8-sig').splitlines(), stage/'inputs'/profile/name)
            dest = stage/profile/'baseline/C'; dest.mkdir(parents=True, exist_ok=True)
            for suffix in ['', '.map']:
                shutil.copy2(PREVIOUS/profile/'candidate/C'/(name+suffix), dest/(name+suffix))
    # The installed List predates the size-neutral scalar-bank candidate.
    for name in ['List','Search']:
        source = (REPO/'artifacts/scalar-allocator-rollout-20261018/shipping/baseline/C/List.map' if name == 'List'
                  else PREVIOUS/'shipping/candidate/C/Search.map')
        shutil.copy2(source, stage/'installed/C'/(name+'.map'))
    save(stage/'freeze.json', dict(compilerRevision='daab9bc',
        files=[identity(p) for folder in ['inputs','installed','runner','compiler-baseline'] for p in sorted((stage/folder).rglob('*')) if p.is_file()]))

def compile_all(stage, label):
    def one(job):
        name, profile = job
        root = stage/profile/label; root.mkdir(parents=True, exist_ok=True)
        image = root/'C'/name; image.parent.mkdir(exist_ok=True)
        if label != 'baseline':
            assert not image.exists()
        response = root/(name+'.rsp')
        response.write_text((stage/'inputs'/profile/name/'compile.rsp').read_text(encoding='utf-8-sig')+'output='+str(image)+'\n', encoding='utf-8')
        env = os.environ.copy()
        if label == 'trace': env['COPPERSHARP_SPILL_TRACE'] = '1'
        with (root/(name+'.log')).open('w', encoding='utf-8') as log:
            code = subprocess.run(['dotnet', str(stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll'), '@'+str(response)], cwd=REPO, stdout=log, stderr=subprocess.STDOUT, env=env).returncode
        assert code == 0, (name, profile, label)
        row = dict(command=name, profile=profile, compiler=label, image=identity(image), response=identity(response), mapIdentity=identity(Path(str(image)+'.map')), **parse_map(Path(str(image)+'.map')))
        save(root/(name+'.measurement.json'), row)
        print(label, name, profile, image.stat().st_size, flush=True)
        return row
    with ThreadPoolExecutor(max_workers=4) as pool:
        rows = list(pool.map(one, [(n,p) for n in ['List','Search'] for p in ['shipping','morphos']]))
    save(stage/(label+'.measurements.json'), rows)

def vectors(stage, label):
    rows = []
    for name in ['List','Search']:
        for profile in ['shipping','morphos']:
            override = {name: ['morphos320-'+name.lower()+'-native-entry-vector-fixture']} if profile == 'morphos' else None
            result = qualify(stage/profile/'baseline', stage/profile/label, stage/'runner/CopperOS.Commands.NativeExecution.dll', [name], override)
            rows.append(dict(command=name, profile=profile, comparison='compiler', result=result))
            assert result[0]['qualified']
            if profile == 'shipping' and (stage/'installed/C'/name).read_bytes() != (stage/profile/'baseline/C'/name).read_bytes():
                result = qualify(stage/'installed', stage/profile/label, stage/'runner/CopperOS.Commands.NativeExecution.dll', [name], override)
                rows.append(dict(command=name, profile=profile, comparison='installed', result=result))
                assert result[0]['qualified']
            save(stage/(label+'.vectors.json'), rows)

def native(stage, label):
    import qualify_list_search_fixed as harness
    old = REPO/'artifacts/list-search-loops-20261018/native'
    for name in ['List','Search']:
        root = stage/'native'/name; (root/'inputs').mkdir(parents=True)
        for folder in ['host','guest','runner']:
            shutil.copytree(old/name/folder, root/folder)
        for filename in ['TEXT.IN','LONG.IN']:
            shutil.copy2(old/name/'inputs'/filename, root/'inputs'/filename)
        shutil.copy2(stage/'installed/C'/name, root/'inputs'/(name+'.baseline'))
        shutil.copy2(stage/'shipping'/label/'C'/name, root/'inputs'/(name+'.candidate'))
    save(stage/'native-freeze.json', [identity(p) for p in sorted((stage/'native').rglob('*')) if p.is_file()])
    os.environ['COPPERSHARP_KICKSTART31_ROM'] = 'C:/Data/ROM/kickstart-3.1-a500.rom'
    harness.native(stage)
    for name in ['List','Search']:
        root = stage/'native'/name; tools = REPO/'tools/Commands'/(name+'OsQualification')
        assert run([sys.executable, str(tools/'control-auditor.py'), str(root)], root/'controls.log') == 0
        args = ['pwsh','-NoProfile','-File',str(tools/'confirm-winuae.ps1'),'-Stage',str(root)]
        if name == 'Search': args.append('-RequireCorrectOutput')
        assert run(args, root/'winuae.log') == 0

def audit(stage):
    from qualify_list_search_fixed import verifier
    frozen = read(stage/'freeze.json')
    for item in frozen['files']:
        assert identity(item['path']) == item
    results = read(stage/'candidate.vectors.json')
    assert len(results) == 6
    receipts = {}
    for row in results:
        name, profile = row['command'], row['profile']
        result = row['result'][0]
        assert result['qualified'] and result['accept'] and len(result['checks']) == 3
        assert {c['cpu'] for c in result['checks']} == {'68000','68020','68040'}
        before = stage/'installed' if row['comparison'] == 'installed' else stage/profile/'baseline'
        after = stage/profile/'candidate'
        for check in result['checks']:
            pair = []
            for root in [before, after]:
                path = root/'execution'/f'{name}.{check["cpu"]}.{check["suite"]}.json'
                data = read(path)
                assert data['status'] == 'passed' and not data.get('observationOnly', False)
                assert data['imageSha256'] == identity(root/'C'/name)['sha256']
                assert data['managedExecutorSha256'] == identity(stage/'runner/CopperOS.Commands.NativeExecution.dll')['sha256']
                assert data['instructionCoreSha256'] == identity(stage/'runner/Copper68k.dll')['sha256']
                if data.get('compilerMapSha256'):
                    assert data['compilerMapSha256'] == identity(root/'C'/(name+'.map'))['sha256']
                assert len(data['cases']) == {('List','shipping'):36,('List','morphos'):35,('Search','shipping'):30,('Search','morphos'):57}[name,profile]
                pair.append(data['cases'])
                receipts[str(path)] = identity(path)
            a,b = pair
            assert [comparable(c) for c in a] == [comparable(c) for c in b]
            assert all(c['peakStackBytes'] <= c.get('configuredStackBytes',8192) for c in b)
        for key in ['invocations','instructionsBefore','instructionsAfter','peakStackBefore','peakStackAfter']:
            assert len({c[key] for c in result['checks']}) == 1
    measurements = {label: read(stage/(label+'.measurements.json')) for label in ['baseline','trace','long','generic','domains','candidate']}
    for label, rows in measurements.items():
        assert len(rows) == 4
        for row in rows:
            for key in ['image','response','mapIdentity']:
                assert identity(row[key]['path']) == row[key]
            if label in ['baseline','trace']:
                assert row['image']['sha256'] == identity(PREVIOUS/row['profile']/'candidate/C'/row['command'])['sha256']
            if label == 'candidate':
                path = stage/row['profile']/'domains/C'/row['command']
                assert Path(row['image']['path']).read_bytes() == path.read_bytes()
                assert row['image']['bytes'] < (stage/row['profile']/'baseline/C'/row['command']).stat().st_size
    tested = CHECKOUT/'Compiler.Tests/bin/Release/net10.0/CopperSharp.Compiler.dll'
    assert tested.read_bytes() == (stage/'compiler-candidate/CopperSharp.Compiler.dll').read_bytes()
    tests = []
    for folder, passed, failed in [('tests-before/results',4,4),('tests-candidate',729,0)]:
        paths = list((stage/folder).glob('*.trx')); assert len(paths) == 1
        tree = ET.parse(paths[0]); counts = tree.find('.//{*}Counters').attrib
        assert int(counts['passed']) == passed and int(counts['failed']) == failed
        failures = [r.attrib['testName'] for r in tree.findall('.//{*}UnitTestResult') if r.attrib['outcome'] == 'Failed']
        if failed:
            assert len(failures) == 4 and all('enabled: True' in f for f in failures)
        tests.append(dict(receipt=identity(paths[0]), counters=counts, expectedFailures=failures))
    for item in read(stage/'native-freeze.json'):
        assert identity(item['path']) == item
    native_results = []; independent = {}; controls = {}; native_runtime = []
    assert read(stage/'rom.json')['sha256'] == '8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
    for name in ['List','Search']:
        root = stage/'native'/name
        assert (root/'inputs'/(name+'.baseline')).read_bytes() == (stage/'installed/C'/name).read_bytes()
        assert (root/'inputs'/(name+'.candidate')).read_bytes() == (stage/'shipping/candidate/C'/name).read_bytes()
        binding = read(root/'runner-source-binding.json')
        assert all(d['matches'] for r in binding[:2] for d in r['documents'])
        contracts = read(root/('contracts.json' if name == 'List' else 'cases.json'))
        for cpu in ['68000','68020','68040']:
            names = set(); stacks = set(); count = 0
            for batch in (['part1','part2','part3','repeat'] if name == 'List' else ['part1','part2']):
                result = verifier(name)(root,batch,cpu)
                assert result['passed'], result.get('failures')
                raw = result['result']
                assert raw['ExecutionMode'] == 'Interpreter' and raw['Frames'] <= 3000
                assert raw['RomSha256'].lower() == read(stage/'rom.json')['sha256']
                assert raw['HunkSha256'].lower() == identity(root/'guest'/(name+'Qualification'))['sha256']
                observed = read(root/'runs'/(batch+'-'+cpu)/'observations.json')
                for key, file in [(name.upper()+'B',name+'.baseline'),(name.upper()+'C',name+'.candidate'),('TEXT.IN','TEXT.IN'),('LONG.IN','LONG.IN')]:
                    assert observed['inputs'][key] == identity(root/'inputs'/file)['sha256']
                assert observed['inputs']['CASES.FIX'] == identity(root/'runs'/(batch+'-'+cpu)/'CASES.FIX')['sha256']
                names.update(o['Name'] for o in observed['observations']); stacks.update(o['Stack'] for o in observed['observations'])
                count += result['invocations']; native_results.append(dict(command=name, **result))
            assert names == {c['Name'] for c in contracts} and stacks == {4096,8192}
            assert count == (100 if name == 'List' else 64)
        controls[name] = read(root/'negative-controls.json')
        assert len(controls[name]) == 4 and all(c['rejected'] for c in controls[name])
        independent[name] = read(root/'winuae/results.json')
        assert {r['cpu'] for r in independent[name]} == {'68000','68020','68040'}
        for row in independent[name]:
            assert row['completed'] and not row['jit'] and row['stackBytes'] == 4096
            assert row['romSha256'] == read(stage/'rom.json')['sha256']
            assert row['winuaeSha256'] == identity(Path('C:/Program Files/WinUAE/winuae64.exe'))['sha256']
            assert row['winuaeVersion'] == '6.0.3.0'
            sysroot = root/'winuae'/row['cpu']/'sys'
            for version,suffix in [('baseline','B'),('candidate','C')]:
                assert (sysroot/'C'/(name+suffix)).read_bytes() == (root/'inputs'/(name+'.'+version)).read_bytes()
            if name == 'List':
                assert row['passed']
                expected = 'TEXT.IN'.ljust(24)+' '+'19'.rjust(7)+' ----rwed\n'
                assert row['outputs']['returns'] == ''.join(label+'-'+case+' 0 0\n' for label in ['baseline','candidate'] for case in ['quick','metadata','to'])
                for label in ['baseline','candidate']:
                    for case,text in [('quick','TEXT.IN\n'),('metadata',expected),('to',expected),('stdout','')]:
                        assert row['outputs'][label+'-'+case] == text
                for key,text in row['outputs'].items():
                    assert (sysroot/(key+'.txt')).read_bytes().decode('latin1') == text
            else:
                assert row['outputsAgree'] and row['numberedOutputCorrect']
                assert row['baseline'] == row['candidate'] == '     2> Needle\n'
                assert row['returns'] == 'baseline 0 0\ncandidate 0 0\n'
                for key in ['baseline','candidate','returns']:
                    assert (sysroot/(key+'.txt')).read_bytes().decode('latin1') == row[key]
        native_runtime += [identity(p) for folder in ['host','guest','runner'] for p in sorted((root/folder).rglob('*')) if p.is_file()]
        assert identity(root/'runner/Copper68k.dll')['sha256'] == '5780f5084fd44e2d4732969bc0f9ce7ae7e3beffc1135ded92bc46d9b21399f2'
    assert verifier('List')(stage/'native/List','smoke','68000')['passed']
    methods = []
    for before,after in zip(measurements['baseline'],measurements['candidate']):
        assert (before['command'],before['profile']) == (after['command'],after['profile'])
        def occurrences(entries):
            # DisplayName omits overload signatures. Retain each occurrence;
            # collapsing by name loses the main Run body to a tiny overload.
            counts = {}; result = {}
            for name,value in entries:
                ordinal = counts.get(name,0); counts[name] = ordinal+1
                result[name,ordinal] = value
            return result
        def sizes(row):
            return occurrences((m[1],int(m[0])) for m in re.findall(r'^[0-9A-F]{8}[ \t]+(\d+)[ \t]+(.+::.+)$',Path(row['mapIdentity']['path']).read_text(encoding='utf-8-sig'),re.M))
        a,b = sizes(before),sizes(after)
        aa,bb = [occurrences((m['method'],m) for m in r['records']['ALLOCATION']) for r in [before,after]]
        assert a.keys() == b.keys() and aa.keys() == bb.keys()
        methods.append(dict(command=before['command'],profile=before['profile'],
            bodyChanges=[dict(method=m[0],occurrence=m[1],before=a[m],after=b[m]) for m in sorted(a) if a[m]!=b[m]],
            allocationChanges=[dict(method=m[0],occurrence=m[1],before=aa[m],after=bb[m]) for m in sorted(aa) if aa[m]!=bb[m]]))
    unique = list(receipts.values())
    reference_path = REPO/'artifacts/scalar-allocator-rollout-20261018/audit.json'
    references = {r['command']:r['workbench31'] for r in read(reference_path)['decisions'] if r['command'] in ['List','Search']}
    data = dict(passed=True,freeze=frozen,measurements=measurements,qualification=results,vectorReceipts=unique,
        vectorInvocations=sum(len(read(Path(r['path']))['cases']) for r in unique),compilerTests=tests,
        native=native_results,winuae=independent,negativeControls=controls,nativeRuntime=native_runtime,
        methods=methods,compiler=[identity(p) for p in sorted((stage/'compiler-candidate').iterdir()) if p.is_file()],
        patch=identity(stage/'compiler-fix.patch'),tool=identity(Path(__file__)),
        workbench31=references,workbenchReferenceProof=identity(reference_path),
        runtime=dict(dotnetSdk=subprocess.check_output(['dotnet','--version'],cwd=REPO).decode().strip(),core=identity(stage/'runner/Copper68k.dll'),copper68k='1.5.1',
            nativeCoreVersion='1.5.4-detroit-dev.2+ddc753000b492a15048ba6c16a06e03c0c160a63',nativeCore=identity(stage/'native/List/runner/Copper68k.dll'),
            winuae=identity(Path('C:/Program Files/WinUAE/winuae64.exe')),winuaeVersion='6.0.3.0'),
        evidence=[identity(p) for p in sorted((stage/'native').rglob('*')) if p.is_file() and p.suffix in ['.json','.ps1','.py']])
    if (stage/'integration.json').exists():
        data['integration'] = read(stage/'integration.json')
        for key in ['before','after','source']:
            for row in data['integration'][key]:
                assert identity(row['path']) == row
        assert identity(data['integration']['patch']['path']) == data['integration']['patch']
    save(stage/'audit.json',data)
    return data

def integrate(stage):
    audit(stage)
    main = REPO.parent/'CopperSharp68k'
    patch = stage/'compiler-fix.patch'
    files = ['Compiler/Backend/M68kRegisterAllocation.cs','Compiler.Tests/M68kFeasibleColorDegreeTests.cs','RegisterAllocation.md']
    before = []; source = []
    assert not (stage/'integration.json').exists()
    assert not (main/files[1]).exists()
    old = (main/files[0]).read_bytes()
    for relative in files:
        target = stage/'source'/relative; target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(CHECKOUT/relative,target); source.append(identity(target))
        if (main/relative).exists():
            target = stage/'main-before'/relative; target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(main/relative,target); before.append(identity(target))
    subprocess.run(['git','-C',str(main),'apply','--check',str(patch)],check=True)
    subprocess.run(['git','-C',str(main),'apply',str(patch)],check=True)
    new = (main/files[0]).read_bytes()
    assert old.count(b'pairAliases') == new.count(b'pairAliases')
    assert (main/files[1]).read_bytes() == (CHECKOUT/files[1]).read_bytes()
    save(stage/'integration.json',dict(before=before,source=source,after=[identity(main/f) for f in files],
        patch=identity(patch),pairAliasDraftPreserved=True))

def publish(stage):
    data = audit(stage)
    assert 'integration' in data
    names = sorted(p.name for p in (stage/'installed/C').iterdir() if p.is_file() and not p.suffix)
    assert len(names) == 61
    target = stage/'accepted/C'; target.mkdir(parents=True)
    assert {p.name for p in (REPO/'out/C').iterdir()} == set(names)
    for name in names:
        saved = stage/'installed/C'/name
        assert (REPO/'out/C'/name).read_bytes() == saved.read_bytes()
        source = stage/'shipping/candidate/C'/name if name in ['List','Search'] else saved
        if name in ['List','Search']: assert source.stat().st_size < saved.stat().st_size
        shutil.copy2(source,target/name)
    assert {p.name for p in target.iterdir()} == set(names)
    for name in ['List','Search']: shutil.copy2(target/name,REPO/'out/C'/name)
    assert all((REPO/'out/C'/n).read_bytes() == (target/n).read_bytes() for n in names)
    data['publication'] = dict(beforeBytes=sum((stage/'installed/C'/n).stat().st_size for n in names),
        afterBytes=sum((target/n).stat().st_size for n in names),installed=[identity(REPO/'out/C'/n) for n in names])
    save(stage/'published.json',data)
    save(REPO/'docs/Commands/Workbench31MorphOS320/list-search-register-spills-20261018.json',data)

def report(stage):
    publication = read(stage/'published.json')['publication']
    for item in publication['installed']:
        assert identity(item['path']) == item
        assert Path(item['path']).read_bytes() == (stage/'accepted/C'/Path(item['path']).name).read_bytes()
    data = audit(stage)
    data['publication'] = publication
    save(stage/'published.json',data)
    save(REPO/'docs/Commands/Workbench31MorphOS320/list-search-register-spills-20261018.json',data)
    before = data['measurements']['baseline']; after = data['measurements']['candidate']
    publication = data['publication']
    lines = ['# List and Search remaining register spills — 2026-10-18', '', '## Accepted images', '',
        '| Command | Shipping before → accepted | Saved | MorphOS before → accepted | Saved |',
        '|---|---:|---:|---:|---:|']
    for name in ['List','Search']:
        pairs = {p: (next(r['image']['bytes'] for r in before if r['command']==name and r['profile']==p),next(r['image']['bytes'] for r in after if r['command']==name and r['profile']==p)) for p in ['shipping','morphos']}
        a,b = pairs['shipping']; c,d = pairs['morphos']
        lines.append(f'| {name} | {a:,} → {b:,} | {a-b} | {c:,} → {d:,} | {c-d} |')
    lines += ['', f'Updated only `out/C/List` and `out/C/Search`. The complete 61-command inventory is **{publication["beforeBytes"]:,} → {publication["afterBytes"]:,} bytes**, saving **{publication["beforeBytes"]-publication["afterBytes"]} bytes**. All other installed hashes are unchanged. No command-source or build-policy changes were made.', '',
        'Retained genuine Workbench 3.1 media size references: List **5,108 bytes** (accepted image **0.889×**); Search **2,476 bytes** (accepted image **2.078×**). The original media identities and versions are retained in the JSON. These are size references; no new original-command executions are claimed.', '',
        '## Cause and compiler fix', '',
        'The allocator compared each group’s available-color count, which excludes forbidden registers, with a degree estimate that still counted conflicts on those forbidden colors. Call-clobbered, fixed and reserved registers could inflate the estimated pressure even when the two values had no usable register in common. This could change the simplification order and choose an avoidable spill.', '',
        'Under the existing opt-in `SimplifyLoopPhis` policy, scalar degree estimates now intersect each group’s allowed registers with the complement of its forbidden registers before counting a conflict. The interference graph is retained in full for allocation and verification. Register pairs retain the previous approximation; policy-off clients retain their previous behavior. No register, clobber, alias, resource, resident-context or frame constraint is relaxed.', '',
        'The new four-value reproductions cover byte, word, long data and address-register domains. All four enabled cases fail on clean `daab9bc` and pass with the fix; their four policy-off controls retain the prior allocation. **729 compiler regressions pass**, including scalar repair, allocation, instruction/CCR dataflow, branch relaxation, resident execution and frame preservation.', '',
        '## What remains spilled', '',
        'List’s main routine and Search’s main/formatting routines remain the pressure centers. Many traced live-through groups have forbidden mask `0x630f`, leaving D4–D7 and A2–A4; byte flags can use only the data registers. Long-lived options and loop/ownership state still compete with short-lived values and call conventions. These finite traces do not establish a minimum possible spill count.', '',
        '| Main routine/profile | Spilled SSA values | Reloads | Frame bytes | Encoded body bytes |',
        '|---|---:|---:|---:|---:|']
    for row in data['methods']:
        allocation = next(m for m in row['allocationChanges'] if m['method'].endswith('::Run') and m['occurrence']==0)
        body = next(m for m in row['bodyChanges'] if m['method'].endswith('::Run') and m['occurrence']==0)
        a,b = allocation['before'],allocation['after']
        lines.append(f'| {row["command"]}/{row["profile"]} | {a["spilled"]} → {b["spilled"]} | {a["reloads"]} → {b["reloads"]} | {a["frame-bytes"]} → {b["frame-bytes"]} | {body["before"]} → {body["after"]} |')
    lines += ['', 'Spilled SSA-value counts are not stack-access counts. Some counts increase while reload traffic and complete image size fall. Search’s shipping formatter grows 450 → 490 bytes, and some other small helpers grow; the complete executable still wins. Display names omit overload signatures, so the report retains occurrence indices instead of collapsing the main `Run` routine into a tiny overload. All method records and code/data/relocation metrics remain in the JSON and maps.', '',
        '## Rejected experiments', '',
        '| Compiler experiment | List shipping/MorphOS | Search shipping/MorphOS |',
        '|---|---:|---:|']
    for label,title in [('baseline','Clean daab9bc'),('long','Full-width data-to-address repair'),('generic','Full-width repair in either direction'),('candidate','Feasible scalar degree (accepted)')]:
        rows = data['measurements'][label]
        pair = lambda n: '/'.join(f'{next(r["image"]["bytes"] for r in rows if r["command"]==n and r["profile"]==p):,}' for p in ['shipping','morphos'])
        lines.append(f'| {title} | {pair("List")} | {pair("Search")} |')
    lines += ['', 'Both broader repair candidates grew Search and were rejected without adoption. Their patches, compilers, maps and images are preserved. The accepted compiler contains only the feasible-degree change, its regression tests and documentation.', '',
        '## Qualification and execution costs', '',
        'The exact shipping and MorphOS-profile executables pass supplied-vector fixtures on **68000, 68020 and 68040**. Output bytes, return codes, IoErr, command-visible ownership/releases and supported repeated/interleaved calls match. Workbench startup, parser/I/O/allocation failures and Ctrl-C paths are checked where supplied by the existing fixtures. Every peak stack remains within its existing limit.', '',
        f'**{data["vectorInvocations"]:,} unique vector invocations** include both compiler baselines and the actual installed shipping images, which differ in bytes from the size-neutral `daab9bc` baselines. Candidate executions reused between these comparisons are counted once.', '',
        '| Command/profile | Calls per image/CPU | Instructions before → after | Change | Peak stack before → after |',
        '|---|---:|---:|---:|---:|']
    for name in ['List','Search']:
        for profile in ['shipping','morphos']:
            row = next(r for r in data['qualification'] if r['command']==name and r['profile']==profile and r['comparison']==('installed' if profile=='shipping' else 'compiler'))
            check = row['result'][0]['checks'][0]
            a,b = check['instructionsBefore'],check['instructionsAfter']
            lines.append(f'| {name}/{profile} | {check["invocations"]} | {a:,} → {b:,} | {(b-a)/a:+.2%} | {check["peakStackBefore"]} → {check["peakStackAfter"]} |')
    lines += ['', 'The displayed per-CPU totals agree on all three CPUs. Shipping “before” is the actual installed image; alternate-profile “before” is the frozen compiler baseline. Size takes priority over execution speed: Search’s additional instructions are retained explicitly. These are instruction counts, not cycle or wall-clock timing measurements.', '',
        f'Original Kickstart 3.1 DOS: **{sum(r["invocations"] for r in data["native"])} invocations** across the shipping List/Search images, all three CPUs, the declared finite corpora and 4/8 KiB stacks; the extra six-invocation List smoke run also passes. Eight auditor mutation controls are rejected. Independent WinUAE checks pass **24 invocations** across all CPUs with JIT disabled and 4 KiB stacks. Output files and return/IoErr records are independently rechecked against the contracts.', '',
        '## Frozen inputs and limits', '',
        'Compiler work starts from clean `daab9bc` in `CopperSharp68k-list-search-spills-20261018`. Each comparison uses the same accepted CIL assemblies and complete adjacent dependencies from the preceding rollout. All four fresh baselines reproduce the retained `daab9bc` images, and trace instrumentation is verified byte-neutral. The final candidate compiler is byte-identical to the compiler used by the regression suite; its images reproduce the qualified prototype exactly. The qualified patch is integrated into the main CopperSharp workspace while preserving its unrelated allocator and runtime drafts; that dirty checkout was not built.', '',
        'Flags: MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols off, unchanged per-command size passes and stack limits. SDK/assembly identities, compiler patch hashes, effective passes, rewrite counts, largest methods, frame records and relocation counts are preserved.', '',
        f'Actual .NET SDK: `{data["runtime"]["dotnetSdk"]}`. Vector core: Copper68k `1.5.1`, SHA-256 `{data["runtime"]["core"]["sha256"]}`. Native OS core: `{data["runtime"]["nativeCoreVersion"]}`, SHA-256 `{data["runtime"]["nativeCore"]["sha256"]}`. WinUAE `{data["runtime"]["winuaeVersion"]}`, SHA-256 `{data["runtime"]["winuae"]["sha256"]}`. ROM SHA-256: `8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee`.', '',
        'MorphOS profiles are MC68000 qualification images, not comparisons with PPC executables. No original MorphOS/PPC, original-command-image or CopperScreen checks were available. Original DOS/WinUAE checks cover the shipping Workbench entries; alternate profiles receive the vector checks. No exhaustive equivalence, new minimum stack or unsupported-interleaving guarantee is inferred. Other commands have not been adopted with this compiler change.', '',
        'Evidence: `artifacts/list-search-spills-20261018`. Tool: `tools/Commands/investigate_list_search_spills.py` (`prepare`, `compile`, `vectors`, `native`, `audit`, `integrate`, `publish`, `report`). Compiler probe build logs and regression receipts are retained. The complete accepted 61-command set is staged and hash-checked before refreshing only the two accepted executables.', '']
    (REPO/'docs/Commands/Workbench31MorphOS320/list-search-register-spills-20261018.md').write_text('\n'.join(lines),encoding='utf-8')

if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__)
    parser.add_argument('mode', choices=['prepare','compile','vectors','native','audit','integrate','publish','report'])
    parser.add_argument('--stage', type=Path, required=True)
    parser.add_argument('--label', default='candidate')
    args = parser.parse_args()
    stage = args.stage.resolve()
    if args.mode == 'prepare': prepare(stage)
    elif args.mode == 'compile': compile_all(stage, args.label)
    elif args.mode == 'vectors': vectors(stage, args.label)
    elif args.mode == 'native': native(stage, args.label)
    else: globals()[args.mode](stage)
