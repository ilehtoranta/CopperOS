"""Freeze and measure SimplifyLoopPhis individually on the remaining large commands."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import shutil
import sys

REPO=Path(__file__).resolve().parents[2]
from measure_c import identity,git_state,parse_map
from verify_correctness_checkpoint import freeze_response
from qualify_sizes import run,qualify,suites


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))

def prepare(stage):
 if stage.exists(): raise RuntimeError('Use a fresh stage.')
 names=sorted(p.name for p in (REPO/'out/C').iterdir() if p.is_file() and not p.suffix and p.stat().st_size>=4096 and p.name not in ['List','Search'])
 (stage/'installed/C').mkdir(parents=True)
 for p in (REPO/'out/C').iterdir():
  if p.is_file() and not p.suffix: shutil.copy2(p,stage/'installed/C'/p.name)
 previous=REPO/'artifacts/list-pressure-20261016'
 shutil.copytree(previous/'main-candidate/compiler',stage/'compiler')
 shutil.copytree(previous/'runner',stage/'runner')
 manifest={'source':git_state(REPO),'commands':names,'installed':[identity(p) for p in sorted((stage/'installed/C').iterdir())],
           'compiler':[identity(p) for p in sorted((stage/'compiler').rglob('*')) if p.is_file()],
           'runner':[identity(p) for p in sorted((stage/'runner').rglob('*')) if p.is_file()]}
 (stage/'prepare.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
 print(','.join(names))

def validate(stage):
 m=read(stage/'prepare.json')
 for key in ['installed','compiler','runner']:
  for r in m[key]:
   assert identity(r['path'])==r,('Changed frozen file',r['path'])
 return m

def freeze(stage):
 m=validate(stage); rows=[]
 for name in m['commands']:
  source=REPO/'src/Commands'/name
  response=source/'obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp'
  inputs=freeze_response(response,source,stage/'inputs'/name)
  image=stage/'fresh/C'/name
  assert image.exists(),name
  rows.append(dict(command=name,inputs=inputs,sourceImage=identity(image),installed=identity(stage/'installed/C'/name)))
  for path in source.rglob('*'):
   if path.is_file() and path.suffix in ['.cs','.csproj'] and not {'bin','obj'}.intersection(path.relative_to(source).parts):
    target=stage/'source'/path.relative_to(REPO); target.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(path,target)
 for path in list((REPO/'src/Commands/Common').glob('Native*.cs'))+list((REPO/'src/Commands').glob('*.props'))+list((REPO/'src/Commands').glob('*.targets')):
  target=stage/'source'/path.relative_to(REPO); target.parent.mkdir(parents=True,exist_ok=True); shutil.copy2(path,target)
 (stage/'freeze.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')

def measure(stage):
 m=validate(stage); frozen=read(stage/'freeze.json')
 def one(item):
  row,selected=item; name=row['command']; label='candidate' if selected else 'baseline'
  for r in row['inputs']: assert identity(r['path'])==r
  target=stage/label; (target/'C').mkdir(parents=True,exist_ok=True)
  response=target/(name+'.rsp'); image=target/'C'/name
  if image.exists(): raise RuntimeError('Retain prior comparison: '+str(image))
  lines=(stage/'inputs'/name/'compile.rsp').read_text(encoding='utf-8-sig').splitlines()
  if selected:
   policy=next((line.split('=',1)[1] for line in lines if line.startswith('code-size-passes=')),'')
   lines=[line for line in lines if not line.startswith(('code-size-passes=','code-size-optimizations='))]
   lines+=['code-size-optimizations=on','code-size-passes='+(','.join([p for p in policy.split(',') if p]+['SimplifyLoopPhis']))]
  lines+=['output='+str(image)]
  response.write_text('\n'.join(lines)+'\n',encoding='utf-8')
  if run(['dotnet',str(stage/'compiler/CopperSharp.Compiler.Cli.dll'),'@'+str(response)],target/(name+'.log')):
   raise RuntimeError(name+' '+label+' failed')
  result=dict(command=name,label=label,image=identity(image),response=identity(response),**parse_map(Path(str(image)+'.map')))
  (target/(name+'.json')).write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
  if not selected: assert image.read_bytes()==Path(row['sourceImage']['path']).read_bytes(),name+' source reproduction failed'
  print(name,label,result['image']['bytes'],flush=True)
  return result
 with ThreadPoolExecutor(max_workers=3) as pool:
  values=list(pool.map(one,[(row,selected) for row in frozen for selected in [False,True]]))
 (stage/'measurements.json').write_text(json.dumps(values,indent=2)+'\n',encoding='utf-8')
 validate(stage)

def qualification(stage):
 values=read(stage/'measurements.json'); pairs={name:{r['label']:r for r in values if r['command']==name} for name in validate(stage)['commands']}
 names=[name for name,p in pairs.items() if p['candidate']['image']['bytes']<p['baseline']['image']['bytes'] and name in suites()]
 results=qualify(stage/'baseline',stage/'candidate',stage/'runner/CopperOS.Commands.NativeExecution.dll',names)
 assert all(r['qualified'] for r in results),'Qualification failed; do not enable failing commands.'

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__); p.add_argument('mode',choices=['prepare','freeze','measure','qualify']); p.add_argument('--stage',type=Path,required=True); a=p.parse_args()
 {'prepare':prepare,'freeze':freeze,'measure':measure,'qualify':qualification}[a.mode](a.stage.resolve())
