"""Controlled Type allocator experiments on unchanged accepted assemblies."""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
from measure_c import identity, parse_map
from qualify_sizes import run, qualify, comparable

REPO=Path(__file__).resolve().parents[2]
PREVIOUS=REPO/'artifacts/scalar-allocator-rollout-20261018'
CHECKOUT=REPO.parent/'CopperSharp68k-type-allocation-20261018'

def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')

def prepare(stage):
    assert not stage.exists();stage.mkdir(parents=True)
    shutil.copytree(REPO/'out/C',stage/'installed/C')
    for label,source in [('original','compiler-baseline'),('current','compiler-candidate')]:
        shutil.copytree(PREVIOUS/source,stage/('compiler-'+label))
    shutil.copytree(PREVIOUS/'runner',stage/'runner')
    for profile,source in [('shipping','inputs'),('morphos','profiles')]:
        target=stage/'inputs'/profile;shutil.copytree(PREVIOUS/source/'Type',target)
        lines=[]
        for line in (target/'compile.rsp').read_text(encoding='utf-8-sig').splitlines():
            if line.startswith(('input=','managed-assembly=')):
                key,value=line.split('=',1);line=key+'='+str(target/Path(value).name)
            lines.append(line)
        (target/'compile.rsp').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    save(stage/'freeze.json',dict(compilerRevision='c536d31',files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))

def compile_all(stage,label):
    for profile in ['shipping','morphos']:
        root=stage/label/profile;image=root/'C/Type';image.parent.mkdir(parents=True)
        response=root/'compile.rsp'
        response.write_text((stage/'inputs'/profile/'compile.rsp').read_text(encoding='utf-8-sig')+'output='+str(image)+'\n',encoding='utf-8')
        assert run(['dotnet',str(stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll'),'@'+str(response)],root/'compile.log')==0
        save(root/'measurement.json',dict(image=identity(image),response=identity(response),**parse_map(Path(str(image)+'.map'))))
        print(label,profile,image.stat().st_size,flush=True)

def vectors(stage,label):
    results=[]
    for profile,suite in [('shipping','workbench31-type-native-entry-vector-fixture'),('morphos','type-native-entry-vector-fixture')]:
        result=qualify(stage/'original'/profile,stage/label/profile,stage/'runner/CopperOS.Commands.NativeExecution.dll',['Type'],{'Type':[suite]})
        results.append(dict(profile=profile,result=result))
    save(stage/label/'vectors.json',results)

def controls(stage,label):
    records=[]
    for name in ['AddDataTypes','Copy','Delete','Dir','Mount','Search','Info','Version','List']:
        for profile in (['shipping'] if name=='Copy' else ['shipping','morphos']):
            source=PREVIOUS/('inputs' if profile=='shipping' else 'profiles')/name/'compile.rsp'
            root=stage/'controls'/label/profile;image=root/'C'/name
            image.parent.mkdir(parents=True,exist_ok=True);assert not image.exists()
            response=root/(name+'.rsp')
            response.write_text(source.read_text(encoding='utf-8-sig')+'output='+str(image)+'\n',encoding='utf-8')
            assert run(['dotnet',str(stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll'),'@'+str(response)],root/(name+'.log'))==0
            baseline=PREVIOUS/profile/'candidate/C'/name
            row=dict(command=name,profile=profile,before=identity(baseline),after=identity(image),response=identity(response),map=parse_map(Path(str(image)+'.map')))
            records.append(row);save(stage/'controls'/label/'measurements.json',records)
            print(name,profile,row['before']['bytes'],row['after']['bytes'],flush=True)

def text_io(stage):
    source=stage/'inputs/morphos/compile.rsp'
    results=[]
    for label in ['original','current','candidate']:
        root=stage/'text-io'/label;image=root/'C/Type';image.parent.mkdir(parents=True)
        lines=[l for l in source.read_text(encoding='utf-8-sig').splitlines() if not l.startswith('entry=')]
        lines+=['entry=CopperOS.Commands.TypeNativeRoot.TypeTextIoProbe::Main','output='+str(image)]
        response=root/'compile.rsp';response.write_text('\n'.join(lines)+'\n',encoding='utf-8')
        assert run(['dotnet',str(stage/('compiler-'+label)/'CopperSharp.Compiler.Cli.dll'),'@'+str(response)],root/'compile.log')==0
        save(root/'measurement.json',dict(image=identity(image),response=identity(response),**parse_map(Path(str(image)+'.map'))))
    for label in ['current','candidate']:
        result=qualify(stage/'text-io/original',stage/'text-io'/label,stage/'runner/CopperOS.Commands.NativeExecution.dll',
            ['Type'],{'Type':['type-text-io-probe-fixture']})
        assert result[0]['qualified'];results.append(dict(compiler=label,result=result))
    save(stage/'text-io/vectors.json',results)

def audit(stage):
    frozen=read(stage/'freeze.json')
    for item in frozen['files']:assert identity(item['path'])==item
    measurements=[];receipts=[]
    for label in ['original','current','candidate']:
        for profile in ['shipping','morphos']:
            root=stage/label/profile;m=read(root/'measurement.json')
            assert identity(m['image']['path'])==m['image']
            reference=PREVIOUS/profile/('baseline' if label=='original' else 'candidate')/'C/Type'
            if label!='candidate':assert (root/'C/Type').read_bytes()==reference.read_bytes()
            measurements.append(dict(compiler=label,profile=profile,**m))
            suite='workbench31-type-native-entry-vector-fixture' if profile=='shipping' else 'type-native-entry-vector-fixture'
            for cpu in ['68000','68020','68040']:
                p=root/'execution'/f'Type.{cpu}.{suite}.json';data=read(p)
                assert data['status']=='passed' and not data['observationOnly']
                assert len(data['cases'])==(17 if profile=='shipping' else 16)
                assert data['imageSha256']==m['image']['sha256']
                assert data['managedExecutorSha256']==identity(stage/'runner/CopperOS.Commands.NativeExecution.dll')['sha256']
                assert data['instructionCoreSha256']=='99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f'
                assert all(c['peakStackBytes']<=c.get('configuredStackBytes',8192) for c in data['cases'])
                if label!='original':
                    before=read(stage/'original'/profile/'execution'/p.name)
                    assert [comparable(c) for c in before['cases']]==[comparable(c) for c in data['cases']]
                receipts.append(identity(p))
    helper=[]
    for label in ['original','current','candidate']:
        root=stage/'text-io'/label;m=read(root/'measurement.json')
        assert identity(m['image']['path'])==m['image']
        for cpu in ['68000','68020','68040']:
            p=root/'execution'/f'Type.{cpu}.type-text-io-probe-fixture.json';data=read(p)
            assert data['status']=='passed' and len(data['cases'])==6
            assert data['imageSha256']==m['image']['sha256']
            assert data['managedExecutorSha256']==identity(stage/'runner/CopperOS.Commands.NativeExecution.dll')['sha256']
            assert data['instructionCoreSha256']==identity(stage/'runner/Copper68k.dll')['sha256']
            assert all(c['peakStackBytes']<=c.get('configuredStackBytes',8192) for c in data['cases'])
            before=read(stage/'text-io/original/execution'/p.name)
            assert [comparable(c) for c in before['cases']]==[comparable(c) for c in data['cases']]
            helper.append(identity(p))
    tests=[]
    for folder,passed,failed in [('tests-before',19,2),('tests-candidate',721,0)]:
        paths=list((stage/folder).rglob('*.trx'));assert len(paths)==1
        tree=ET.parse(paths[0]);counts=tree.find('.//{*}Counters').attrib
        assert int(counts['passed'])==passed and int(counts['failed'])==failed
        failures=[r.attrib['testName'] for r in tree.findall('.//{*}UnitTestResult') if r.attrib['outcome']=='Failed']
        if failed:assert len(failures)==2 and all('RepairLeavesThePreferredAddressColorForPendingNeighbor' in n and 'forbiddenPreference: False' in n for n in failures)
        tests.append(dict(receipt=identity(paths[0]),counters=counts,failedCases=failures))
    assert (stage/'compiler-candidate/CopperSharp.Compiler.dll').read_bytes()==(CHECKOUT/'Compiler.Tests/bin/Release/net10.0/CopperSharp.Compiler.dll').read_bytes()
    integration=read(stage/'integration.json')
    for field in ['before','after','source']:
        for item in integration[field]:assert identity(item['path'])==item
    assert identity(integration['qualifiedPatch']['path'])==integration['qualifiedPatch']
    controls=read(stage/'controls/candidate/measurements.json');assert len(controls)==17
    for row in controls:
        for field in ['before','after','response']:assert identity(row[field]['path'])==row[field]
        assert row['after']['bytes']<=row['before']['bytes']
    for results in [read(stage/l/'vectors.json') for l in ['current','candidate']]+[read(stage/'text-io/vectors.json')]:
        for row in results:
            checks=row['result'][0]['checks']
            assert len(checks)==3 and {c['cpu'] for c in checks}=={'68000','68020','68040'}
            for field in ['instructionsBefore','instructionsAfter','peakStackBefore','peakStackAfter','invocations']:
                assert len({c[field] for c in checks})==1
    saved=stage/'installed/C';installed=REPO/'out/C'
    assert len(list(installed.iterdir()))==len(list(saved.iterdir()))==61
    assert all((installed/p.name).read_bytes()==p.read_bytes() for p in saved.iterdir())
    assert (installed/'Type').read_bytes()==(stage/'original/shipping/C/Type').read_bytes()
    assert (stage/'candidate/shipping/C/Type').stat().st_size==(installed/'Type').stat().st_size==4696
    data=dict(passed=True,freeze=frozen,measurements=measurements,qualification={l:read(stage/l/'vectors.json') for l in ['current','candidate']},
        textIo=read(stage/'text-io/vectors.json'),compilerTests=tests,integration=integration,controls=controls,
        receipts=receipts+helper,tool=identity(Path(__file__)),
        finalCompiler=[identity(p) for p in sorted((stage/'compiler-candidate').iterdir()) if p.is_file()],
        retainedInstalled=[identity(p) for p in sorted(installed.iterdir())],totalBytes=sum(p.stat().st_size for p in installed.iterdir()),
        disassembly=[identity(stage/(label+'-Copy.asm')) for label in ['original','current','candidate']],
        runtime=dict(dotnetSdk=subprocess.check_output(['dotnet','--version'],cwd=REPO).decode().strip(),copper68k='1.5.1',core=identity(stage/'runner/Copper68k.dll')))
    save(stage/'audit.json',data)
    save(REPO/'docs/Commands/Workbench31MorphOS320/type-allocator-regression-20261018.json',data)
    return data

if __name__=='__main__':
    p=argparse.ArgumentParser(__doc__);p.add_argument('mode',choices=['prepare','compile','vectors','controls','text_io','audit']);p.add_argument('--stage',type=Path,required=True);p.add_argument('--label')
    a=p.parse_args();stage=a.stage.resolve()
    if a.mode=='prepare':prepare(stage)
    elif a.mode=='compile':compile_all(stage,a.label)
    elif a.mode=='vectors':vectors(stage,a.label)
    elif a.mode=='controls':controls(stage,a.label)
    elif a.mode=='text_io':text_io(stage)
    else:audit(stage)
