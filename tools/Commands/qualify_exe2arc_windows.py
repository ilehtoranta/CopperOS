"""Audit Exe2Arc's admitted-window kernels and publish a qualified image."""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import xml.etree.ElementTree as ET
from measure_c import identity,parse_map
from qualify_sizes import comparable,run
from qualify_exe2arc_runtime import project
from qualify_exe2arc_os import validate as validate_os

REPO=Path(__file__).resolve().parents[2]
CPUS=['68000','68020','68040']
SUITE='exe2arc-native-entry-vector-fixture'
REFERENCE=REPO/'artifacts/exe2arc-os-v2-20261017'


def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def save(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def helper(name):
    directory=REPO/'tools/Commands/Exe2ArcOsQualification'
    if str(directory) not in sys.path:sys.path.insert(0,str(directory))
    spec=importlib.util.spec_from_file_location('Window'+name,directory/name)
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module


def build(stage,sources,label):
    assert not (stage/label).exists()
    files=[p for p in sources.rglob('*.cs') if not {'bin','obj'}.intersection(p.parts)]
    p=project(stage,label+'-source-build',files,'CopperOS.Commands.Exe2Arc')
    cil=stage/label/'cil'
    assert run(['dotnet','build',str(p),'-c','Release','--nologo','-m:1','-p:UseSharedCompilation=false','-nodeReuse:false','-o',str(cil)],stage/(label+'-build.log'))==0
    image=stage/label/'C/Exe2Arc';image.parent.mkdir()
    lines=[l for l in (stage/'baseline/compile.rsp').read_text(encoding='utf-8-sig').splitlines() if not l.startswith(('input=','managed-assembly=','output='))]
    lines+=['input='+str(cil/'CopperOS.Commands.Exe2Arc.dll')]+['managed-assembly='+str(p) for p in (stage/'dependencies').glob('*.dll')]+['output='+str(image)]
    rsp=stage/label/'compile.rsp';rsp.write_text('\n'.join(lines)+'\n',encoding='utf-8')
    assert run(['dotnet',str(stage/'compiler/CopperSharp.Compiler.Cli.dll'),'@'+str(rsp)],stage/label/'compile.log')==0
    print(label,image.stat().st_size,flush=True)


def audit(stage,os_stage,winuae):
    frozen=read(stage/'freeze.json')
    for f in frozen['files']:assert identity(f['path'])==f
    assert identity(frozen['previous']['path'])==frozen['previous']
    validate_os(os_stage)
    checks=read(stage/'stable-combined/qualification.json')[0];assert checks['accept'] and checks['qualified']
    runner=stage/'runner-stable-exe/CopperOS.Commands.NativeExecution.dll'
    vectors=[]
    for cpu in CPUS:
        before=read(stage/'stable-baseline/execution'/('Exe2Arc.'+cpu+'.'+SUITE+'.json'))
        after=read(stage/'stable-combined/execution'/('Exe2Arc.'+cpu+'.'+SUITE+'.json'))
        for result,label in [(before,'baseline'),(after,'combined')]:
            assert result['status']=='passed' and len(result['cases'])==63
            assert result['imageSha256']==identity(stage/label/'C/Exe2Arc')['sha256']
            assert result['managedExecutorSha256']==identity(runner)['sha256']
            assert result['instructionCoreSha256']=='99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f'
            names=[c['name'] for c in result['cases']]
            assert names.index('interleaved-zip')<names.index('interleaved-rar')
        assert [comparable(c) for c in before['cases']]==[comparable(c) for c in after['cases']]
        vectors.append(dict(cpu=cpu,invocations=63,instructionsBefore=sum(c['instructions'] for c in before['cases']),
            instructionsAfter=sum(c['instructions'] for c in after['cases']),
            crossBefore=next(c['instructions'] for c in before['cases'] if c['name']=='rar-window-crossing'),
            crossAfter=next(c['instructions'] for c in after['cases'] if c['name']=='rar-window-crossing'),
            peakBefore=max(c['peakStackBytes'] for c in before['cases']),peakAfter=max(c['peakStackBytes'] for c in after['cases'])))
    for label in ['baseline','candidate']:
        target=stage/('baseline' if label=='baseline' else 'combined')/'C/Exe2Arc'
        assert (os_stage/'inputs'/('Exe2Arc.'+label)).read_bytes()==target.read_bytes()
    batches=[b for b in read(os_stage/'freeze.json')['batches'] if b!='smoke']
    native=[helper('verify.py').verify(os_stage,b,c) for c in CPUS for b in batches]
    assert all(c['passed'] for c in native) and sum(c['invocations'] for c in native)==342
    for cpu in CPUS:
        for image in [0,1]:
            covered={case['Name'] for b in batches for case in read(os_stage/('cases-'+b+'.json')) if case['Image']==image}
            assert covered=={c['Name'] for c in read(os_stage/'contracts.json')}
    module=helper('qualify-winuae.py')
    independent=[module.verify_run(os_stage,winuae/(b+'-'+c),REFERENCE) for c in CPUS for b in module.LIGHT+module.HEAVY]
    assert len(read(winuae/'results.json'))==27 and sum(c['qualification']['invocations'] for c in independent)==366
    controls=helper('control-auditor.py').controls(os_stage);assert len(controls)==10
    tests={}
    for label in ['trace','legacy-control']:
        path=stage/(label+'-test-results/components.trx');root=ET.parse(path)
        counts=root.find('.//{*}Counters').attrib
        if label=='trace':assert int(counts['passed'])==241 and int(counts['failed'])==0
        else:
            failures=root.findall('.//{*}UnitTestResult[@outcome="Failed"]')
            assert len(failures)==int(counts['failed'])==5
            assert all('Fixed_headers_reuse_full_scratch_proof_across_windows' in f.attrib['testName'] for f in failures)
        tests[label]=dict(counts=counts,receipt=identity(path))
    trials=[]
    for label in ['baseline','bounds','dispatch','combined']:
        report=read(stage/label/'execution'/('Exe2Arc.68000.'+SUITE+'.json'))
        assert report['status']=='passed'
        trials.append(dict(label=label,image=identity(stage/label/'C/Exe2Arc'),
            instructions=sum(c['instructions'] for c in report['cases']),
            crossInstructions=next(c['instructions'] for c in report['cases'] if c['name']=='rar-window-crossing')))
    assert trials[-1]['image']['bytes']<trials[0]['image']['bytes'] and vectors[0]['crossAfter']<vectors[0]['crossBefore']
    maps={}
    for label in ['baseline','combined']:
        parsed=parse_map(stage/label/'C/Exe2Arc.map')
        parsed['records']={k:v for k,v in parsed['records'].items() if k not in ['MACHINE-OP','FRAME-SLOT']};maps[label]=parsed
    data=dict(passed=True,frozen=frozen,trials=trials,maps=maps,vectors=vectors,native=native,winuae=independent,
        componentTests=tests,negativeControls=controls,osFreeze=identity(os_stage/'freeze.json'),
        sourceFiles=[identity(p) for p in sorted((stage/'combined-source').rglob('*')) if p.is_file()],
        stableRunner=[identity(p) for p in sorted((stage/'runner-stable-exe').iterdir()) if p.is_file()],
        fixtureSource=[identity(p) for p in (stage/'runner-stable-order-source/source').glob('*FrontendFixture.cs')],
        disassembly=[identity(p) for label in ['baseline','combined'] for p in sorted((stage/label/'asm').glob('*.asm'))],
        sourceBuildProof=[identity(p) for p in sorted((stage/'source-proof').rglob('*')) if p.is_file()],
        recipe=identity(stage/'experiment.py'),tool=identity(Path(__file__)),
        evidence=[identity(p) for root in [stage/'stable-baseline',stage/'stable-combined',os_stage/'runs',winuae] for p in sorted(root.rglob('*')) if p.is_file()])
    if (stage/'installed-inventory.json').exists():
        inventory=read(stage/'installed-inventory.json')
        assert len(inventory)==61 and len({Path(p['path']).name for p in inventory})==61
        for row in inventory:
            name=Path(row['path']).name;accepted=stage/'accepted/C'/name
            actual=identity(accepted)
            assert (actual['bytes'],actual['sha256'])==(row['bytes'],row['sha256'])
            expected=stage/'combined/C/Exe2Arc' if name=='Exe2Arc' else stage/'installed/C'/name
            assert accepted.read_bytes()==expected.read_bytes()
        data['installedInventory']=inventory
    dest=REPO/'docs/Commands/Workbench31MorphOS320/exe2arc-window-kernels-20261018.json';save(dest,data)
    print(dict(passed=True,bytes=trials[-1]['image']['bytes'],crossInstructions=vectors[0]['crossAfter'],native=342,winuae=366),flush=True)
    return data


def publish(stage,os_stage,winuae):
    data=audit(stage,os_stage,winuae)
    for file in (stage/'combined-source').rglob('*.cs'):
        assert (REPO/'src/Commands/Exe2Arc'/file.relative_to(stage/'combined-source')).read_bytes()==file.read_bytes()
    for file in ['Exe2ArcTestIo.cs','Exe2ArcWindowKernelTests.cs']:
        snapshot=stage/('TraceTestIo.cs' if file=='Exe2ArcTestIo.cs' else file)
        assert (REPO/'tests/Commands'/file).read_bytes()==snapshot.read_bytes()
    assert (stage/'source-proof/C/Exe2Arc').read_bytes()==(stage/'combined/C/Exe2Arc').read_bytes()
    saved=stage/'installed/C';assert not saved.exists();shutil.copytree(REPO/'out/C',saved)
    assert len(list(saved.iterdir()))==61 and (saved/'Exe2Arc').read_bytes()==(stage/'baseline/C/Exe2Arc').read_bytes()
    accepted=stage/'accepted/C';shutil.copytree(saved,accepted);shutil.copy2(stage/'combined/C/Exe2Arc',accepted/'Exe2Arc')
    for p in saved.iterdir():assert (REPO/'out/C'/p.name).read_bytes()==p.read_bytes()
    shutil.copy2(accepted/'Exe2Arc',REPO/'out/C/Exe2Arc')
    assert all((REPO/'out/C'/p.name).read_bytes()==p.read_bytes() for p in accepted.iterdir())
    save(stage/'installed-inventory.json',[identity(p) for p in sorted((REPO/'out/C').iterdir())]);audit(stage,os_stage,winuae)


if __name__=='__main__':
    parser=argparse.ArgumentParser(__doc__);parser.add_argument('mode',choices=['build','audit','publish'])
    parser.add_argument('--stage',type=Path,required=True);parser.add_argument('--os-stage',type=Path);parser.add_argument('--winuae',type=Path)
    parser.add_argument('--sources',type=Path);parser.add_argument('--label');args=parser.parse_args()
    if args.mode=='build':build(args.stage.resolve(),args.sources.resolve(),args.label)
    else:globals()[args.mode](args.stage.resolve(),args.os_stage.resolve(),args.winuae.resolve())
