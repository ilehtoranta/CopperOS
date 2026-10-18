"""Measure and qualify Exe2Arc scan code on a frozen compiler and unchanged CIL."""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import sys
import xml.etree.ElementTree as ET

from measure_c import identity, parse_map
from qualify_sizes import qualify, receipt, run
from qualify_exe2arc_os import validate as validate_os, execute as execute_os

REPO=Path(__file__).resolve().parents[2]
RUNTIME=REPO/'artifacts/exe2arc-runtime-v8-20261017'
ORIGINAL=REPO/'artifacts/exe2arc-os-v2-20261017'
CPUS=['68000','68020','68040']
PASSES='SimplifyLoopPhis,RemoveRedundantTransport,CompactPrivateStackCopies,CompactGuestMemory,NarrowOperations,InlineMemoryHelpers,InlineSingleUseMethods,ElideUnusedRegisterArguments,ClusterInternalCalls,ReuseIncomingArgumentHomes'
POLICIES={'reproduce':None,'phis':'SimplifyLoopPhis','transport':'RemoveRedundantTransport,CompactPrivateStackCopies',
          'memory':'CompactGuestMemory,NarrowOperations','inline':'InlineMemoryHelpers,InlineSingleUseMethods','all':PASSES}


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def save(path,value): path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def module(name):
    directory=REPO/'tools/Commands/Exe2ArcOsQualification'
    if str(directory) not in sys.path: sys.path.insert(0,str(directory))
    spec=importlib.util.spec_from_file_location('Exe2ArcScan'+name,directory/name)
    result=importlib.util.module_from_spec(spec);spec.loader.exec_module(result);return result


def freeze(stage):
    assert not stage.exists();stage.mkdir(parents=True)
    for src,dst in [('compiler-candidate','compiler'),('dependencies','dependencies'),('cil','cil'),('runner','runner')]:
        shutil.copytree(RUNTIME/src,stage/dst)
    (stage/'baseline/C').mkdir(parents=True)
    for suffix in ['', '.map']:shutil.copy2(RUNTIME/('repaired/candidate/C/Exe2Arc'+suffix),stage/('baseline/C/Exe2Arc'+suffix))
    lines=[]
    for line in (RUNTIME/'repaired/candidate/compile.rsp').read_text(encoding='utf-8-sig').splitlines():
        if line.startswith('output='):continue
        if line.startswith('input='):line='input='+str(stage/'cil/CopperOS.Commands.Exe2Arc.dll')
        if line.startswith('managed-assembly='):line='managed-assembly='+str(stage/'dependencies'/Path(line.split('=',1)[1]).name)
        lines.append(line)
    (stage/'base.rsp').write_text('\n'.join(lines)+'\n',encoding='utf-8')
    save(stage/'freeze.json',dict(files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


def validate(stage):
    for row in read(stage/'freeze.json')['files']: assert identity(row['path'])==row
    assert (stage/'baseline/C/Exe2Arc').read_bytes()==(RUNTIME/'repaired/candidate/C/Exe2Arc').read_bytes()


def trials(stage):
    validate(stage);lines=(stage/'base.rsp').read_text(encoding='utf-8').splitlines()
    for label,policy in POLICIES.items():
        folder=stage/label/'C';folder.mkdir(parents=True);image=folder/'Exe2Arc'
        opts=lines if policy is None else [l for l in lines if not l.startswith('code-size-optimizations=')]+['code-size-optimizations=on','code-size-passes='+policy]
        response=folder.parent/'compile.rsp';response.write_text('\n'.join(opts+['output='+str(image)])+'\n',encoding='utf-8')
        assert run(['dotnet',str(stage/'compiler/CopperSharp.Compiler.Cli.dll'),'@'+str(response)],folder.parent/'compile.log')==0
        if policy is None: assert image.read_bytes()==(stage/'baseline/C/Exe2Arc').read_bytes()
        print(label,image.stat().st_size,flush=True)
    for label in ['baseline','phis','transport','memory','inline','all']:
        result=receipt(stage/label,'Exe2Arc','68000','exe2arc-native-entry-vector-fixture',stage/'runner/CopperOS.Commands.NativeExecution.dll')
        assert result['status']=='passed'
    result=qualify(stage/'baseline',stage/'all',stage/'runner/CopperOS.Commands.NativeExecution.dll',['Exe2Arc'])
    assert result[0]['accept'];save(stage/'qualification.json',result)


def native(stage,os_stage):
    validate(stage);validate_os(ORIGINAL);assert not os_stage.exists();os_stage.mkdir(parents=True)
    for name in ['runner','host','guest','source','inputs']:shutil.copytree(ORIGINAL/name,os_stage/name)
    for p in ORIGINAL.glob('cases-*.json'):shutil.copy2(p,os_stage/p.name)
    for p in (REPO/'artifacts/exe2arc-os-cross-split-20261017').glob('cases-cross-*.json'):shutil.copy2(p,os_stage/p.name)
    for name in ['contracts.json','runner-source-binding.json']:shutil.copy2(ORIGINAL/name,os_stage/name)
    for label,src in [('baseline',stage/'baseline/C/Exe2Arc'),('candidate',stage/'all/C/Exe2Arc')]:shutil.copy2(src,os_stage/'inputs'/('Exe2Arc.'+label))
    large=read(os_stage/'cases-large.json')
    for label,image in [('baseline',0),('candidate',1)]:
        save(os_stage/('cases-large-'+label+'.json'),[c for c in large if c['Name']=='multiple-chunks' and c['Image']==image])
    batches=['smoke','typed','automatic','names','errors1','errors2','signals','cross-baseline','cross-candidate','large-baseline','large-candidate','repeat']
    save(os_stage/'freeze.json',dict(rom=read(ORIGINAL/'freeze.json')['rom'],batches=batches,
         sourceQualification=identity(ORIGINAL/'freeze.json'),targetQualification=identity(stage/'freeze.json'),
         files=[identity(p) for p in sorted(os_stage.rglob('*')) if p.is_file()]))
    # WinUAE's combined request is independent of native batching/frame limits.
    for cpu in CPUS:
        for batch in ['cross','large']:
            folder=os_stage/'runs'/(batch+'-'+cpu);folder.mkdir(parents=True)
            (folder/'CASES.FIX').write_bytes(module('qualify-winuae.py').request(read(os_stage/('cases-'+batch+'.json'))))
    execute_os(os_stage)


def audit(stage,os_stage,uae,large_stage=None):
    validate(stage);validate_os(os_stage)
    checks=read(stage/'qualification.json')[0];assert checks['accept'] and checks['qualified']
    for label in ['baseline','all']:
        for cpu in CPUS:
            execution=read(stage/label/'execution'/('Exe2Arc.'+cpu+'.exe2arc-native-entry-vector-fixture.json'))
            assert execution['status']=='passed' and execution['imageSha256']==identity(stage/label/'C/Exe2Arc')['sha256']
            assert execution['managedExecutorSha256']==identity(stage/'runner/CopperOS.Commands.NativeExecution.dll')['sha256']
    for label,target in [('baseline','baseline'),('candidate','all')]:assert (os_stage/'inputs'/('Exe2Arc.'+label)).read_bytes()==(stage/target/'C/Exe2Arc').read_bytes()
    batches=[b for b in read(os_stage/'freeze.json')['batches'] if b!='smoke' and not (large_stage and b=='large')]
    native_rows=[module('verify.py').verify(os_stage,b,c) for c in CPUS for b in batches]
    if large_stage:
        validate_os(large_stage)
        for label in ['baseline','candidate']:assert (large_stage/'inputs'/('Exe2Arc.'+label)).read_bytes()==(os_stage/'inputs'/('Exe2Arc.'+label)).read_bytes()
        native_rows += [module('verify.py').verify(large_stage,b,c) for c in CPUS for b in ['large-baseline','large-candidate']]
    assert all(r['passed'] for r in native_rows)
    for cpu in CPUS:
        for image in [0,1]:
            covered={case['Name'] for b in batches for case in read(os_stage/('cases-'+b+'.json')) if case['Image']==image}
            if large_stage: covered|={case['Name'] for b in ['large-baseline','large-candidate'] for case in read(large_stage/('cases-'+b+'.json')) if case['Image']==image}
            assert covered=={c['Name'] for c in read(os_stage/'contracts.json')}
    winuae=[module('qualify-winuae.py').verify_run(os_stage,uae/(b+'-'+c),ORIGINAL) for c in CPUS for b in module('qualify-winuae.py').LIGHT+module('qualify-winuae.py').HEAVY]
    assert sum(r['qualification']['invocations'] for r in winuae)==366
    negative=module('control-auditor.py').controls(os_stage);assert len(negative)==10
    trials=[]
    for label in ['baseline','phis','transport','memory','inline','all']:
        result=read(next((stage/label/'execution').glob('Exe2Arc.68000.*.json')));assert result['status']=='passed'
        cross=next(c for c in result['cases'] if c['name']=='rar-window-crossing')
        trials.append(dict(label=label,image=identity(stage/label/'C/Exe2Arc'),instructions=sum(c['instructions'] for c in result['cases']),crossInstructions=cross['instructions']))
    hint_trials=[]
    for label in ['source-reproduce','inline-rar','inline-validation','inline-both']:
        if (stage/label/'C/Exe2Arc').exists():
            assert (stage/label/'C/Exe2Arc').read_bytes()==(stage/'all/C/Exe2Arc').read_bytes()
            hint_trials.append(dict(label=label,image=identity(stage/label/'C/Exe2Arc')))
    maps={}
    for label in ['baseline','all']:
        parsed=parse_map(stage/label/'C/Exe2Arc.map')
        parsed['records']={k:v for k,v in parsed['records'].items() if k not in ['MACHINE-OP','FRAME-SLOT']}
        maps[label]=parsed
    before=read(stage/'baseline/execution/Exe2Arc.68000.exe2arc-native-entry-vector-fixture.json')['cases']
    after=read(stage/'all/execution/Exe2Arc.68000.exe2arc-native-entry-vector-fixture.json')['cases']
    costs=[dict(name=a['name'],before=a['instructions'],after=b['instructions']) for a,b in zip(before,after) if b['instructions']>a['instructions']]
    result=dict(passed=True,passes=PASSES,trials=trials,hintTrials=hint_trials,vectorQualification=checks,native=native_rows,winuae=winuae,
                maps=maps,smallPathInstructionIncreases=costs,negativeControls=negative,
                nativeInvocations=sum(r['invocations'] for r in native_rows),winuaeInvocations=366,
                frozenFiles=read(stage/'freeze.json')['files'],osFreeze=identity(os_stage/'freeze.json'),
                sourceBuildProof=identity(REPO/'docs/Commands/Workbench31MorphOS320/exe2arc-runtime-qualification-20261017.json'),
                buildSettings=[identity(REPO/p) for p in ['src/Commands/Exe2Arc/CopperOS.Commands.Exe2Arc.csproj','src/Commands/CodeSizePolicies.props']],
                retainedLimits=[dict(identity=identity(p),result=read(p)) for p in (os_stage/'runs').glob('large-*/result.json') if read(p)['StopReason']!='ProgramReturned'],
                disassembly=[identity(p) for label in ['baseline','phis','all'] for p in sorted((stage/label/'asm').glob('*.asm'))],
                largeStageFreeze=identity(large_stage/'freeze.json') if large_stage else None,
                evidence=[identity(p) for root in [stage/'all',stage/'baseline',os_stage/'runs',uae]+([large_stage/'runs'] if large_stage else []) for p in sorted(root.rglob('*')) if p.is_file()],
                tool=identity(Path(__file__)))
    if (stage/'installed-inventory.json').exists():result['installedInventory']=read(stage/'installed-inventory.json')
    dest=REPO/'docs/Commands/Workbench31MorphOS320/exe2arc-scan-optimization-20261018.json';save(dest,result)
    print(dict(passed=True,before=trials[0]['image']['bytes'],after=trials[-1]['image']['bytes'],native=result['nativeInvocations'],winuae=366))
    return result


def publish(stage,os_stage,uae,large_stage=None):
    result=audit(stage,os_stage,uae,large_stage)
    settings=ET.parse(REPO/'src/Commands/CodeSizePolicies.props').getroot()
    selection=next(g.findtext('CopperOSCodeSizePasses') for g in settings.findall('PropertyGroup') if "== 'Exe2Arc'" in g.attrib.get('Condition',''))
    assert selection==PASSES
    assert ET.parse(REPO/'src/Commands/Exe2Arc/CopperOS.Commands.Exe2Arc.csproj').findtext('.//CopperOSCodeSizeOptimizations')=='on'
    # Source must still match the unchanged, previously qualified CIL.
    for file in (REPO/'src/Commands/Exe2Arc').rglob('*.cs'):
        if {'bin','obj'}.intersection(file.parts):continue
        snapshot=list((RUNTIME/'command-source/source').glob('*-'+file.name));assert len(snapshot)==1 and snapshot[0].read_bytes()==file.read_bytes()
    installed=stage/'installed/C';assert not installed.exists();shutil.copytree(REPO/'out/C',installed)
    assert (installed/'Exe2Arc').read_bytes()==(stage/'baseline/C/Exe2Arc').read_bytes()
    assert len(list(installed.iterdir()))==61 and result['trials'][-1]['image']['bytes']<result['trials'][0]['image']['bytes']
    accepted=stage/'accepted/C';shutil.copytree(installed,accepted);shutil.copy2(stage/'all/C/Exe2Arc',accepted/'Exe2Arc')
    for file in installed.iterdir():assert (REPO/'out/C'/file.name).read_bytes()==file.read_bytes()
    shutil.copy2(accepted/'Exe2Arc',REPO/'out/C/Exe2Arc')
    assert all((REPO/'out/C'/file.name).read_bytes()==file.read_bytes() for file in accepted.iterdir())
    save(stage/'installed-inventory.json',[identity(p) for p in sorted((REPO/'out/C').iterdir())]);audit(stage,os_stage,uae,large_stage)


if __name__=='__main__':
    parser=argparse.ArgumentParser(__doc__);parser.add_argument('mode',choices=['freeze','trials','native','audit','publish'])
    parser.add_argument('--stage',type=Path,required=True);parser.add_argument('--os-stage',type=Path);parser.add_argument('--winuae',type=Path)
    parser.add_argument('--large-stage',type=Path)
    args=parser.parse_args();stage=args.stage.resolve()
    if args.mode in ['freeze','trials']:globals()[args.mode](stage)
    elif args.mode=='native':native(stage,args.os_stage.resolve())
    else:globals()[args.mode](stage,args.os_stage.resolve(),args.winuae.resolve(),args.large_stage.resolve() if args.large_stage else None)
