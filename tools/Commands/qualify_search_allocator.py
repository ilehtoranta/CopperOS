"""Controlled allocator comparison on retained Search command assemblies."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET
from measure_c import identity, parse_map
from qualify_sizes import run, qualify, comparable

REPO=Path(__file__).resolve().parents[2]


def save(path,value):path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')


def compile_one(stage,label,variant,profile):
    root=stage/label/variant/profile;image=root/'C/Search';image.parent.mkdir(parents=True)
    source=stage/'inputs'/variant/profile
    lines=[line for line in (source/'original.rsp').read_text(encoding='utf-8-sig').splitlines()
        if not line.startswith(('input=','managed-assembly=','output='))]
    lines+=['input='+str(source/'CopperOS.Commands.Search.dll')]
    lines+=['managed-assembly='+str(p) for p in source.glob('*.dll') if p.name!='CopperOS.Commands.Search.dll']
    lines+=['output='+str(image)]
    response=root/'compile.rsp';response.write_text('\n'.join(lines)+'\n',encoding='utf-8')
    assert run(['dotnet',str(stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll'),'@'+str(response)],root/'compile.log')==0
    m=parse_map(Path(str(image)+'.map'))
    save(root/'measurement.json',dict(image=identity(image),response=identity(response),input=identity(source/'CopperOS.Commands.Search.dll'),**m))
    print(label,variant,profile,image.stat().st_size,flush=True)


def compile_all(stage, labels):
    for label in labels:assert (stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll').is_file()
    jobs=[(label,variant,profile) for label in labels for variant in ['original','cached-flag','shipping'] for profile in ['shipping','morphos']]
    with ThreadPoolExecutor(max_workers=2) as pool:
        futures=[pool.submit(compile_one,stage,*job) for job in jobs]
        for future in futures:future.result()


def vectors(stage, label):
    for variant in ['original','cached-flag','shipping']:
        for profile in ['shipping','morphos']:
            suite='wb31-search-native-entry-vector-fixture' if profile=='shipping' else 'morphos320-search-native-entry-vector-fixture'
            result=qualify(stage/'baseline'/variant/profile,stage/label/variant/profile,
                stage/'runner/CopperOS.Commands.NativeExecution.dll',['Search'],{'Search':[suite]})
            assert result[0]['qualified']


def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))


def audit(stage):
    from qualify_list_search_fixed import verifier
    initial=read(stage/'initial.json')
    for item in initial['files']:assert identity(item['path'])==item
    assert identity(initial['previous']['path'])==initial['previous']
    previous=REPO/'artifacts/search-literal-length-20261018'
    rows=[];measurements=[]
    for variant,old in [('original','baseline'),('cached-flag','accepted'),('shipping','cached-index')]:
        for profile in ['shipping','morphos']:
            before=stage/'baseline'/variant/profile;after=stage/'narrow'/variant/profile
            assert (before/'C/Search').read_bytes()==(previous/old/profile/'C/Search').read_bytes()
            checks=read(after/'qualification.json')[0]
            assert checks['qualified'] and checks['accept']
            assert {c['cpu'] for c in checks['checks']}=={'68000','68020','68040'}
            changes=[]
            for cpu in ['68000','68020','68040']:
                cases={};suite='wb31-search-native-entry-vector-fixture' if profile=='shipping' else 'morphos320-search-native-entry-vector-fixture'
                for version,root in [('baseline',before),('candidate',after)]:
                    receipt=read(root/'execution'/('Search.'+cpu+'.'+suite+'.json'))
                    assert receipt['status']=='passed' and len(receipt['cases'])==(30 if profile=='shipping' else 57)
                    assert receipt['imageSha256']==identity(root/'C/Search')['sha256']
                    assert receipt['managedExecutorSha256']==identity(stage/'runner/CopperOS.Commands.NativeExecution.dll')['sha256']
                    assert receipt['instructionCoreSha256']=='99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f'
                    assert all(c['peakStackBytes']<=c.get('configuredStackBytes',8192) for c in receipt['cases'])
                    cases[version]=receipt['cases']
                assert [comparable(c) for c in cases['baseline']]==[comparable(c) for c in cases['candidate']]
                changes += [dict(cpu=cpu,name=a['name'],instructions=b['instructions']-a['instructions'],stack=b['peakStackBytes']-a['peakStackBytes']) for a,b in zip(cases['baseline'],cases['candidate'])]
            rows.append(dict(variant=variant,profile=profile,qualification=checks,deltas=changes))
            for label in ['baseline','candidate','weighted','narrow']:
                root=stage/label/variant/profile;m=read(root/'measurement.json')
                assert identity(m['image']['path'])==m['image']
                records=m['records'];measurements.append(dict(variant=variant,profile=profile,compiler=label,image=m['image'],response=m['response'],
                    metrics=records['METRICS'],effectivePasses=records.get('GENERATED-SIZE'),rewrites=records.get('GENERATED-SIZE-REWRITE'),
                    allocation=records['ALLOCATION'],largestMethods=m['largestMethods']))
    root=stage/'qualification/native-candidate/Search'
    for version,label in [('baseline','baseline'),('candidate','narrow')]:
        assert (root/'inputs'/('Search.'+version)).read_bytes()==(stage/label/'shipping/shipping/C/Search').read_bytes()
    native=[verifier('Search')(root,b,c) for c in ['68000','68020','68040'] for b in ['part1','part2']]
    assert all(r['passed'] for r in native) and sum(r['invocations'] for r in native)==192
    rom=read(stage/'qualification/rom.json')
    assert rom['sha256']=='8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
    assert all(r['result']['RomSha256'].lower()==rom['sha256'] for r in native)
    binding=read(root/'runner-source-binding.json');assert all(d['matches'] for r in binding[:2] for d in r['documents'])
    controls=read(root/'negative-controls.json');assert len(controls)==4 and all(c['rejected'] for c in controls)
    win=read(root/'winuae/results.json');assert {r['cpu'] for r in win}=={'68000','68020','68040'}
    for row in win:
        assert row['completed'] and row['outputsAgree'] and row['numberedOutputCorrect'] and not row['jit'] and row['stackBytes']==4096
        assert row['romSha256']==rom['sha256']
        assert row['baseline']==row['candidate']=='     2> Needle\n' and row['returns']=='baseline 0 0\ncandidate 0 0\n'
        folder=root/'winuae'/row['cpu']/'sys'
        for version,suffix in [('baseline','B'),('candidate','C')]:assert (folder/'C'/('Search'+suffix)).read_bytes()==(root/'inputs'/('Search.'+version)).read_bytes()
        for key in ['baseline','candidate','returns']:assert (folder/(key+'.txt')).read_bytes().decode('latin1')==row[key]
    trx=stage/'tests-narrow/allocator.trx';tree=ET.parse(trx);counts=tree.find('.//{*}Counters').attrib
    assert int(counts['passed'])==int(counts['total'])==717 and int(counts['failed'])==0
    integration=read(stage/'main-integration.json');assert identity(integration['after']['path'])==integration['after']
    for field in ['before','patch','test','qualifiedPatch']:assert identity(integration[field]['path'])==integration[field]
    data=dict(passed=True,initial=initial,measurements=measurements,vectors=rows,native=native,winuae=win,negativeControls=controls,
        compilerTests=dict(counters=counts,receipt=identity(trx)),integration=integration,
        source=[identity(p) for p in sorted((stage/'source').rglob('*')) if p.is_file()],
        methodDetails=read(stage/'method-details.json'),
        probes=[identity(p) for p in sorted((stage/'probe-missing-adjacent-dependencies').rglob('measurement.json'))],
        completeInputs=[identity(p) for p in sorted((stage/'inputs').rglob('*')) if p.is_file()],
        compiler=[identity(p) for label in ['compiler-baseline','compiler-narrow'] for p in sorted((stage/label).iterdir()) if p.is_file()],
        runtime=[identity(p) for folder in ['host','guest','runner'] for p in sorted((root/folder).rglob('*')) if p.is_file()],
        disassembly=[identity(p) for p in sorted(stage.glob('*.asm'))],
        tool=identity(Path(__file__)),evidence=[identity(p) for folder in ['baseline','narrow','qualification'] for p in sorted((stage/folder).rglob('*.json'))])
    if (stage/'installed-inventory.json').exists():
        inventory=read(stage/'installed-inventory.json');assert len(inventory)==61
        for item in inventory:
            accepted=identity(stage/'accepted/C'/Path(item['path']).name)
            assert (accepted['bytes'],accepted['sha256'])==(item['bytes'],item['sha256'])
        data.update(installedInventory=inventory,totalBefore=sum(p.stat().st_size for p in (stage/'installed/C').iterdir()),totalAfter=sum(p['bytes'] for p in inventory))
    save(stage/'audit.json',data);return data


def publish(stage):
    audit(stage)
    saved=stage/'installed/C';assert len(list(saved.iterdir()))==61
    assert all((REPO/'out/C'/p.name).read_bytes()==p.read_bytes() for p in saved.iterdir())
    accepted=stage/'accepted/C';assert not accepted.exists();shutil.copytree(saved,accepted)
    shutil.copy2(stage/'narrow/shipping/shipping/C/Search',accepted/'Search')
    assert (accepted/'Search').stat().st_size<(saved/'Search').stat().st_size
    shutil.copy2(accepted/'Search',REPO/'out/C/Search')
    assert all((REPO/'out/C'/p.name).read_bytes()==p.read_bytes() for p in accepted.iterdir())
    save(stage/'installed-inventory.json',[identity(p) for p in sorted((REPO/'out/C').iterdir())])
    save(REPO/'docs/Commands/Workbench31MorphOS320/search-register-allocation-20261018.json',audit(stage))


if __name__=='__main__':
    p=argparse.ArgumentParser(__doc__);p.add_argument('mode',choices=['compile','vectors','audit','publish']);p.add_argument('--stage',type=Path,required=True);p.add_argument('--label')
    a=p.parse_args()
    if a.mode=='compile':compile_all(a.stage.resolve(),a.label.split(',') if a.label else ['baseline','candidate'])
    elif a.mode=='vectors':vectors(a.stage.resolve(),a.label or 'candidate')
    else:globals()[a.mode](a.stage.resolve())
