"""Qualify alternate profiles and Eval peephole modes for the large-command rollout."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import shutil
import sys

from audit_loop_phi_rollout import REPO,read,validate
from qualify_sizes import run,qualify
from measure_c import identity,parse_map

# Default shipping tests already cover MorphOS-only commands. These entries
# exercise distinct source profiles of Workbench commands, without PPC claims.
PROFILES={
 'AddDataTypes':('addbuffers','CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSAddDataTypesListEntry::Main','morphos320-adddatatypes-files-iff-dtcd-native-entry-vector-fixture'),
 'Dir':('addbuffers','CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSDirEntry::Main','morphos320-dir-native-entry-vector-fixture'),
 'Mount':('addbuffers','CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSMountEntry::Main','mount-morphos-native-entry-boundary-fixture'),
 'Delete':('addbuffers','CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSDeleteEntry::Main','delete-command-native-entry-vector-fixture'),
 'Version':('addbuffers','CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSVersionEntry::Main','morphos320-version-native-entry-vector-fixture'),
 'Info':('addbuffers','CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSInfoEntry::Main','morphos320-info-native-entry-vector-fixture'),
 'Type':('type','CopperOS.Commands.TypeNativeRoot.NativeMorphOSTypeEntry::Main','type-native-entry-vector-fixture'),
 'Eval':('eval','CopperOS.Commands.NativeRoot.NativeEvalEntry::Main','eval-native-entry-vector-fixture'),
}
ROOTS={'addbuffers':'Commands.AddBuffersNativeRoot','type':'Commands.TypeNativeRoot','eval':'Commands.NativeRoot'}


def freeze(stage):
 validate(stage)
 for key,folder in ROOTS.items():
  source=REPO/'tests'/folder/'bin/Release/net10.0'; target=stage/'private-inputs'/key
  if target.exists(): raise RuntimeError('Preserve private input freeze.')
  target.mkdir(parents=True)
  for p in source.glob('*.dll'): shutil.copy2(p,target/p.name)
  assert any(target.glob('CopperOS.Commands.*.dll')),folder
 records=[identity(p) for p in sorted((stage/'private-inputs').rglob('*.dll'))]
 (stage/'private-freeze.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')


def profiles(stage, retry=False):
 validate(stage)
 for r in read(stage/'private-freeze.json'): assert identity(r['path'])==r
 selected=[r['command'] for r in read(stage/'candidate/qualification.json') if r['accept']]
 jobs=[]
 for name in selected:
  if name not in PROFILES: continue
  key,entry,suite=PROFILES[name]
  for mode in (['disabled','bounded','fixed-point'] if name=='Eval' else ['fixed-point']):
   jobs.append((name,key,entry,suite,mode))
 # Independently compare both shipping Eval images under each peephole mode.
 if 'Eval' in selected:
  for mode in ['disabled','bounded','fixed-point']:
   jobs.append(('Eval',None,None,'eval-wb31-native-entry-vector-fixture',mode))
 previous=read(stage/'profiles.json') if retry else []
 if retry:
  failed={(r['name'],r['profile'],r['mode']) for r in previous if r.get('error')}
  jobs=[j for j in jobs if (j[0],j[1] or 'shipping',j[4]) in failed]
 def one(job):
  name,key,entry,suite,mode=job; stem=(key or 'shipping')+'-'+name+'-'+mode; root=stage/'profiles'/(stem+('-retry'+str(len(list((stage/'profiles').glob(stem+'*')))) if retry else ''))
  for label in ['baseline','candidate']:
   lines=(stage/label/(name+'.rsp')).read_text(encoding='utf-8-sig').splitlines()
   lines=[line for line in lines if not line.startswith(('output=','peephole='))]
   if key:
    inputs=stage/'private-inputs'/key
    primary=inputs/('CopperOS.'+ROOTS[key]+'.dll')
    lines=[line for line in lines if not line.startswith(('input=','managed-assembly=','entry=','exports='))]
    exports={'AddDataTypes':'copperos.morphos.adddatatypes.load.read,copperos.morphos.adddatatypes.load.alloc,copperos.morphos.adddatatypes.load.free','Info':'copperos.morphos.info.formatdate.putchar'}.get(name,'none')
    lines+=['input='+str(primary),'entry='+entry,'exports='+('none' if exports=='none' else 'all')]
    if exports!='none': lines+=['include-export='+name for name in exports.split(',')]
    lines+=['managed-assembly='+str(p) for p in sorted(inputs.glob('*.dll')) if p!=primary]
   dest=root/label; (dest/'C').mkdir(parents=True)
   image=dest/'C'/name; response=dest/'compile.rsp'; lines+=['peephole='+mode,'output='+str(image)]
   response.write_text('\n'.join(lines)+'\n',encoding='utf-8')
   if run(['dotnet',str(stage/'compiler/CopperSharp.Compiler.Cli.dll'),'@'+str(response)],dest/'compile.log'):
    return dict(name=name,profile=key or 'shipping',mode=mode,error='compile failure',path=str(dest))
   (dest/'compilation.json').write_text(json.dumps(dict(image=identity(image),response=identity(response),**parse_map(Path(str(image)+'.map'))),indent=2)+'\n',encoding='utf-8')
  checks=qualify(root/'baseline',root/'candidate',stage/'runner/CopperOS.Commands.NativeExecution.dll',[name],{name:[suite]})
  return dict(name=name,profile=key or 'shipping',mode=mode,checks=checks,path=str(root))
 with ThreadPoolExecutor(max_workers=2) as pool: results=list(pool.map(one,jobs))
 if retry:
  shutil.copy2(stage/'profiles.json',stage/('profiles-previous-'+str(len(list(stage.glob('profiles-previous-*.json'))))+'.json'))
  updated={(r['name'],r['profile'],r['mode']):r for r in results}
  results=[updated.get((r['name'],r['profile'],r['mode']),r) for r in previous]
 (stage/'profiles.json').write_text(json.dumps(results,indent=2)+'\n',encoding='utf-8')
 assert all('checks' in r and all(c['qualified'] for c in r['checks']) for r in results),'Private profile failure; hold affected command.'

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('mode',choices=['freeze','qualify','retry']);p.add_argument('--stage',required=True,type=Path);a=p.parse_args()
 freeze(a.stage.resolve()) if a.mode=='freeze' else profiles(a.stage.resolve(),a.mode=='retry')
