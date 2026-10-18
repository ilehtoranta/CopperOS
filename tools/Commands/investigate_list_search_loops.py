"""Controlled List/Search loop experiments using the retained receiver-fixed compiler."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import shutil
import os
import sys
import xml.etree.ElementTree as ET
from measure_c import identity, git_state, parse_map
from qualify_sizes import run, qualify, comparable
from qualify_exe2arc_runtime import project

REPO = Path(__file__).resolve().parents[2]
OLD = REPO/'artifacts/receiver-rollout-v3-20261017'
NAMES = ['List', 'Search']


def save(path, value):
    path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def prepare(stage):
    assert not stage.exists()
    stage.mkdir(parents=True)
    shutil.copytree(OLD/'compiler-candidate', stage/'compiler')
    shutil.copytree(OLD/'vector-runner', stage/'runner')
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    dependencies = stage/'dependencies'; dependencies.mkdir()
    for file in (OLD/'inputs/List').glob('*.dll'):
        if file.name != 'CopperOS.Commands.List.dll': shutil.copy2(file, dependencies/file.name)
    for name in NAMES:
        shutil.copytree(REPO/'src/Commands'/name, stage/'baseline-source'/name,
                        ignore=shutil.ignore_patterns('bin', 'obj'))
        shutil.copytree(OLD/'inputs'/name, stage/'inputs'/name)
    save(stage/'freeze.json', dict(source=git_state(REPO), compiler='9eaa2aa',
         previous=identity(REPO/'docs/Commands/Workbench31MorphOS320/receiver-classification-rollout-20261017.json'),
         files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


def build(stage, name, label, sources, profile):
    dest = stage/label/profile
    files = [p for p in sources.rglob('*.cs') if not {'bin','obj'}.intersection(p.relative_to(sources).parts)]
    if profile == 'morphos':
        files += [REPO/'tests/Commands.AddBuffersNativeRoot'/('NativeMorphOS'+name+'Entry.cs')]
    assembly = 'CopperOS.Commands.'+name
    path = project(stage, label+'-'+profile+'-source', files, assembly)
    xml = ET.parse(path)
    if profile == 'shipping':
        ET.SubElement(xml.find('PropertyGroup'), 'DefineConstants').text = 'COPPEROS_WORKBENCH31_'+name.upper()
    xml.write(path, encoding='utf-8')
    cil = dest/'cil'
    assert run(['dotnet','build',str(path),'-c','Release','--nologo','-m:1','-p:UseSharedCompilation=false','-nodeReuse:false','-o',str(cil)],stage/(label+'-'+profile+'-build.log')) == 0
    image = dest/'C'/name; image.parent.mkdir(parents=True, exist_ok=True)
    lines = [l for l in (stage/'inputs'/name/'compile.rsp').read_text(encoding='utf-8-sig').splitlines()
             if not l.startswith(('input=','managed-assembly=','output=','entry='))]
    entry = ('CopperOS.Commands.Entries.'+name+'Entry' if profile == 'shipping' else
             'CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOS'+name+'Entry')
    lines += ['input='+str(cil/(assembly+'.dll')), 'entry='+entry+'::Main']
    lines += ['managed-assembly='+str(p) for p in (stage/'dependencies').glob('*.dll')]
    lines += ['output='+str(image)]
    response = dest/(name+'.rsp'); response.write_text('\n'.join(lines)+'\n', encoding='utf-8')
    assert run(['dotnet',str(stage/'compiler/CopperSharp.Compiler.Cli.dll'),'@'+str(response)], dest/(name+'-compile.log')) == 0
    save(dest/(name+'-measurement.json'), dict(image=identity(image),response=identity(response)))
    print(label,profile,name,image.stat().st_size,flush=True)


def vectors(stage, name, label, runner=None):
    for profile in ['shipping','morphos']:
        suites = {name: ['morphos320-'+name.lower()+'-native-entry-vector-fixture']} if profile == 'morphos' else None
        before, after = stage/('baseline-'+name)/profile, stage/label/profile
        if runner:
            for version, source in [('baseline',before),('candidate',after)]:
                target=stage/'expanded'/name/profile/version
                assert not target.exists()
                shutil.copytree(source/'C',target/'C')
            before=stage/'expanded'/name/profile/'baseline'
            after=stage/'expanded'/name/profile/'candidate'
        rows = qualify(before, after,
                       runner or stage/'runner/CopperOS.Commands.NativeExecution.dll', [name], suites)
        save(after/'qualification.json',rows)
        assert rows[0]['qualified']


def native(stage):
    import qualify_list_search_fixed as previous
    for name, label in [('List','list-comment2'),('Search','search-combined')]:
        root = stage/'native'/name; (root/'inputs').mkdir(parents=True)
        source = OLD/'native'/name
        for folder in ['runner','host','guest']: shutil.copytree(source/folder,root/folder)
        for file in ['TEXT.IN','LONG.IN']: shutil.copy2(source/'inputs'/file,root/'inputs'/file)
        for version, path in [('baseline',stage/('baseline-'+name)/'shipping/C'/name),
                              ('candidate',stage/label/'shipping/C'/name)]:
            shutil.copy2(path,root/'inputs'/(name+'.'+version))
    os.environ['COPPERSHARP_KICKSTART31_ROM']='C:/Data/ROM/kickstart-3.1-a500.rom'
    previous.native(stage)
    for name in NAMES:
        root=stage/'native'/name; tools=REPO/'tools/Commands'/(name+'OsQualification')
        assert run([sys.executable,str(tools/'control-auditor.py'),str(root)],root/'controls.log')==0
        args=['pwsh','-NoProfile','-File',str(tools/'confirm-winuae.ps1'),'-Stage',str(root)]
        if name=='Search':args.append('-RequireCorrectOutput')
        assert run(args,root/'winuae.log')==0


def audit(stage):
    import qualify_list_search_fixed as previous
    frozen=json.loads((stage/'freeze.json').read_text(encoding='utf-8-sig'))
    for file in frozen['files']: assert identity(file['path'])==file
    assert identity(frozen['previous']['path'])==frozen['previous']
    prior=json.loads(Path(frozen['previous']['path']).read_text(encoding='utf-8-sig'))
    for item in prior['sourceFreeze']['files']:
        path=Path(item['path'])
        if path.is_relative_to(OLD/'compiler-candidate'):
            current=identity(stage/'compiler'/path.relative_to(OLD/'compiler-candidate'))
            assert (current['bytes'],current['sha256'])==(item['bytes'],item['sha256'])
    rows=[]; trials=[]; native_rows=[]; independent={}
    read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
    for name,label in [('List','list-comment2'),('Search','search-combined')]:
        baseline=stage/('baseline-'+name)/'shipping/C'/name
        assert baseline.read_bytes()==(stage/'installed/C'/name).read_bytes()
        for profile in ['shipping','morphos']:
            root=stage/'expanded'/name/profile
            row=read(root/'candidate/qualification.json')[0]
            assert row['accept'] and row['qualified']
            assert {c['cpu'] for c in row['checks']}=={'68000','68020','68040'}
            observations={}
            for version,source in [('baseline',stage/('baseline-'+name)/profile),('candidate',stage/label/profile)]:
                image=root/version/'C'/name
                assert image.read_bytes()==(source/'C'/name).read_bytes()
                for check in row['checks']:
                    receipt=root/version/'execution'/(name+'.'+check['cpu']+'.'+check['suite']+'.json')
                    data=read(receipt)
                    assert data['status']=='passed' and data['imageSha256']==identity(image)['sha256']
                    assert data['managedExecutorSha256']==identity(stage/'runner-final/CopperOS.Commands.NativeExecution.dll')['sha256']
                    assert data['instructionCoreSha256']=='99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f'
                    expected={('List','shipping'):36,('List','morphos'):35,('Search','shipping'):27,('Search','morphos'):54}
                    assert len(data['cases'])==expected[name,profile]
                    assert all(c['peakStackBytes']<=c.get('configuredStackBytes',8192) for c in data['cases'])
                    observations[version,check['cpu']]=data['cases']
                assert image.stat().st_size==row['beforeBytes' if version=='baseline' else 'afterBytes']
            assert (stage/('source-proof-'+name)/profile/'C'/name).read_bytes()==(root/'candidate/C'/name).read_bytes()
            assert all(c['instructionsAfter']<c['instructionsBefore'] and c['peakStackAfter']<=c['peakStackBefore'] for c in row['checks'])
            deltas=[]
            for cpu in ['68000','68020','68040']:
                before,after=observations['baseline',cpu],observations['candidate',cpu]
                assert [comparable(c) for c in before]==[comparable(c) for c in after]
                deltas+= [dict(cpu=cpu,name=a['name'],instructions=b['instructions']-a['instructions']) for a,b in zip(before,after)]
            rows.append(dict(command=name,profile=profile,qualification=row,caseInstructionDeltas=deltas))
        native_root=stage/'native'/name
        for version,label_ in [('baseline','baseline-'+name),('candidate',label)]:
            assert (native_root/'inputs'/(name+'.'+version)).read_bytes()==(stage/label_/'shipping/C'/name).read_bytes()
        for cpu in ['68000','68020','68040']:
            count=0
            for batch in (['part1','part2','part3','repeat'] if name=='List' else ['part1','part2']):
                result=previous.verifier(name)(native_root,batch,cpu)
                assert result['passed'],result['failures']
                assert result['result']['Frames']<=3000 and result['result']['ExecutionMode']=='Interpreter'
                assert result['result']['RomSha256'].lower()==read(stage/'rom.json')['sha256']
                count+=result['invocations']; native_rows.append(dict(result,command=name,cpu=cpu,batch=batch))
            assert count==(100 if name=='List' else 64)
        controls=read(native_root/'negative-controls.json');assert len(controls)==4 and all(c['rejected'] for c in controls)
        binding=read(native_root/'runner-source-binding.json')
        assert all(d['matches'] for r in binding[:2] for d in r['documents'])
        independent[name]=read(native_root/'winuae/results.json')
        assert {r['cpu'] for r in independent[name]}=={'68000','68020','68040'}
        for result in independent[name]:
            assert result['completed'] and not result['jit'] and result['stackBytes']==4096
            assert result['romSha256']==read(stage/'rom.json')['sha256']
            sysroot=native_root/'winuae'/result['cpu']/'sys'
            for version,suffix in [('baseline','B'),('candidate','C')]:
                assert (sysroot/'C'/(name+suffix)).read_bytes()==(native_root/'inputs'/(name+'.'+version)).read_bytes()
            if name=='List':
                assert result['passed']
                for key,value in result['outputs'].items():assert (sysroot/(key+'.txt')).read_bytes().decode('latin1')==value
            else:
                assert result['outputsAgree'] and result['numberedOutputCorrect']
                assert result['baseline']==result['candidate']=='     2> Needle\n'
                assert result['returns']=='baseline 0 0\ncandidate 0 0\n'
                for key in ['baseline','candidate','returns']:assert (sysroot/(key+'.txt')).read_bytes().decode('latin1')==result[key]
    smoke=previous.verifier('List')(stage/'native/List','smoke','68000');assert smoke['passed']
    native_rows.append(dict(smoke,command='List',cpu='68000',batch='smoke'))
    maps={}
    for name,labels in [('List',['baseline-List','list-formats','list-offsets','list-comment','list-comment2']),
                        ('Search',['baseline-Search','search-chunks','search-bounds','search-combined'])]:
        for label in labels:
            for profile in ['shipping','morphos']:
                image=stage/label/profile/'C'/name
                m=parse_map(Path(str(image)+'.map'))
                maps[label+'/'+profile]=dict(metrics=m['records']['METRICS'],largestMethods=m['largestMethods'],
                    reachableMethods=m['reachableMethods'],effectivePasses=m['records'].get('GENERATED-SIZE'),
                    rewrites=m['records'].get('GENERATED-SIZE-REWRITE'))
                trials.append(dict(command=name,label=label,profile=profile,image=identity(image),
                    response=identity(stage/label/profile/(name+'.rsp'))))
    for name in ['ListEntrySuite.cs','SearchEntrySuite.cs']:
        snapshots=list((stage/'runner-source-final/source').glob('*-'+name))
        assert len(snapshots)==1 and snapshots[0].read_bytes()==(REPO/'tests/Commands.NativeExecution'/name).read_bytes()
    result=dict(passed=True,freeze=frozen,trials=trials,maps=maps,vectors=rows,native=native_rows,
        independent=independent,tool=identity(Path(__file__)),runner=identity(stage/'runner-final/CopperOS.Commands.NativeExecution.dll'),
        tests=[identity(REPO/'tests/Commands.NativeExecution'/file) for file in ['ListEntrySuite.cs','SearchEntrySuite.cs']],
        sourceSnapshots=[identity(p) for folder in ['list-comment2-source','search-combined-source','runner-source-final/source'] for p in sorted((stage/folder).rglob('*.cs'))],
        buildReceipts=[identity(p) for p in sorted(stage.glob('*-build.log'))]+[identity(p) for p in sorted(stage.glob('*-source/Build.csproj'))],
        nativeRuntime=[identity(p) for name in NAMES for folder in ['runner','host','guest'] for p in sorted((stage/'native'/name/folder).rglob('*')) if p.is_file()],
        evidence=[identity(p) for folder in ['expanded','native'] for p in sorted((stage/folder).rglob('*.json'))])
    if (stage/'installed-inventory.json').exists():
        inventory=read(stage/'installed-inventory.json');assert len(inventory)==61
        for item in inventory:
            file=stage/'accepted/C'/Path(item['path']).name
            accepted=identity(file)
            assert (accepted['bytes'],accepted['sha256'])==(item['bytes'],item['sha256'])
        result['installedInventory']=inventory
        result['totalBefore']=sum(p.stat().st_size for p in (stage/'installed/C').iterdir())
        result['totalAfter']=sum(p['bytes'] for p in inventory)
    save(stage/'audit.json',result)
    return result


def publish(stage):
    result=audit(stage)
    assert len(list((stage/'installed/C').iterdir()))==61
    for file in (stage/'installed/C').iterdir():assert (REPO/'out/C'/file.name).read_bytes()==file.read_bytes()
    for name,source,label in [('List','list-comment2-source','list-comment2'),('Search','search-combined-source','search-combined')]:
        for file in (stage/source).rglob('*.cs'):
            assert (REPO/'src/Commands'/name/file.relative_to(stage/source)).read_bytes()==file.read_bytes()
    accepted=stage/'accepted/C';assert not accepted.exists();shutil.copytree(stage/'installed/C',accepted)
    for name,label in [('List','list-comment2'),('Search','search-combined')]:shutil.copy2(stage/label/'shipping/C'/name,accepted/name)
    assert sum(p.stat().st_size for p in accepted.iterdir())<sum(p.stat().st_size for p in (stage/'installed/C').iterdir())
    for name in NAMES:shutil.copy2(accepted/name,REPO/'out/C'/name)
    assert all((REPO/'out/C'/p.name).read_bytes()==p.read_bytes() for p in accepted.iterdir())
    save(stage/'installed-inventory.json',[identity(p) for p in sorted((REPO/'out/C').iterdir())])
    save(REPO/'docs/Commands/Workbench31MorphOS320/list-search-loops-20261018.json',audit(stage))


if __name__ == '__main__':
    parser=argparse.ArgumentParser(__doc__)
    parser.add_argument('mode',choices=['prepare','build','vectors','native','audit','publish'])
    parser.add_argument('--stage',type=Path,required=True)
    parser.add_argument('--name',choices=NAMES)
    parser.add_argument('--label')
    parser.add_argument('--sources',type=Path)
    parser.add_argument('--runner',type=Path)
    args=parser.parse_args(); stage=args.stage.resolve()
    if args.mode=='prepare':prepare(stage)
    elif args.mode=='audit':audit(stage)
    elif args.mode=='publish':publish(stage)
    elif args.mode=='native':native(stage)
    elif args.mode=='build':
        with ThreadPoolExecutor(max_workers=2) as pool:
            jobs=[pool.submit(build,stage,args.name,args.label,args.sources.resolve(),p) for p in ['shipping','morphos']]
            for job in jobs:job.result()
    else:vectors(stage,args.name,args.label,args.runner.resolve() if args.runner else None)
