"""Independently validate the normal-image full runtime replay."""
import sys,json,hashlib,struct,re,subprocess
from pathlib import Path
import argparse
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--evidence-directory',type=Path,required=True)
parser.add_argument('--output',type=Path,required=True)
parser.add_argument('--preflight',action='store_true')
parser.add_argument('--kickstart-only',action='store_true')
options=parser.parse_args()
ROOT=Path(__file__).resolve().parents[2]; A=options.evidence_directory.resolve();P=json.loads((A/'plan.json').read_text());sys.path[:0]=[str(ROOT/'tools/DiskBuilder')];from verify_image import Adf
checks=[];errors=[];receipts=[]
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def read(p):return json.loads(Path(p).read_text(encoding='utf-8-sig'))
def need(ok,msg):
 if not ok:raise AssertionError(msg)
def check(name,f):
 try:f();checks.append(name)
 except Exception as ex:errors.append(name+': '+str(ex))
class Disk:
 def __init__(self,p):self.d=Adf(Path(p).read_bytes());self.tree=self.d.walk()
 def get(self,n):
  e=self.tree.get(n.lower());return self.d.read_file(e['block']) if e and e['secondary_type']==-3 else None
image=Path(P['image']);normal=Disk(image);imagehash=sha(image)
metadata=set('originalShell plainSyntax interactiveRecovery scriptRecovery residentErrors residentArguments globalErrors listFixtureResult builtinCorpus builtinControls builtinSkipLabel executeReferenceSha256 inputImageSha256 fixtureImageSha256 resultImageSha256 romSha256 winuaeSha256 runtimeQualified unexpectedStarPaths executeTemporaryFiles'.split())
def build():
 base=image.parent;b=read(base/'system-build.json');need(b['imageSha256']==imagehash==P['imageSha256'],'Image identity')
 manifest=base/b['nativeManifest'];need(sha(manifest)==b['nativeManifestSha256'],'Native manifest')
 snap=base/b['sourceSnapshot'];need(sha(snap)==b['sourceSnapshotSha256'],'Snapshot receipt')
 for f in read(snap)['files']:need(sha(snap.parent/f['path'])==f['sha256'],'Source snapshot changed: '+f['path'])
 tool=snap.parent/'CopperOS/tools/ImageBuilder/audit_shared_shell.py'
 subprocess.run([sys.executable,str(tool),'--manifest',str(manifest),'--image',str(image),'--output',str(A/'native-audit.json')],check=True,stdout=subprocess.DEVNULL)
 need(read(A/'native-audit.json')['passed'],'Public native gate')
 for f in read(base/'system-staging.json')['files']:
  need(sha(ROOT/'filesystem/SYS'/f['path'])==f['sha256'],'SYS staging changed')
  need(hashlib.sha256(normal.get(f['path'])).hexdigest()==f['sha256'],'Image staging mismatch')
 source=read(A/'shell-source-identity.json');need(source['shellSourceFiles']==128 and not source['changed'],'Shell host/source identity')
 for f in read(A/'tool-snapshot.json')['files']:need(sha(A/'tools-source'/f['path'])==f['sha256'],'Qualification tool changed')
 for key in ['image','copperstartRom','extensionRom','kickstartRom','probe']:need(sha(P[key])==P[key+'Sha256'],'Runtime input changed: '+key)
check('normal-build-source-native-gate-staging-and-input-identities',build)
def hunk():
 data=normal.get('l/shell-seg');u=lambda p:struct.unpack_from('>I',data,p)[0]
 need([u(i*4) for i in range(7)]==[1011,0,1,0,0,u(20),1001],'HUNK header');size=u(28)*4;need(size==u(20)*4,'CODE size');code=data[32:32+size];p=32+size;need(u(p)==1004,'Relocation hunk');p+=4;offsets=[]
 while True:
  count=u(p);p+=4
  if not count:break
  need(u(p)==0,'Relocation target');p+=4
  for i in range(count):offsets.append(u(p));p+=4
 need(u(p)==1010 and len(offsets)==len(set(offsets)),'HUNK end or duplicate relocation')
 for base in [0x20000,0x100000]:
  relocated=bytearray(code)
  for off in offsets:
   need(off%2==0 and off+4<=len(code),'Relocation site');value=struct.unpack_from('>I',code,off)[0];need(value<len(code),'Relocation addend');struct.pack_into('>I',relocated,off,value+base)
  for off in offsets:need(base<=struct.unpack_from('>I',relocated,off)[0]<base+len(code),'Relocated address')
 (A/'hunk-audit.json').write_text(json.dumps({'passed':True,'relocations':len(offsets),'shellSha256':hashlib.sha256(data).hexdigest()},indent=2)+'\n')
check('packaged-HUNK-independent-relocation-at-two-load-bases',hunk)
if '--preflight' in sys.argv:
 (A/'preflight.json').write_text(json.dumps({'passed':not errors,'checks':checks,'errors':errors},indent=2)+'\n')
 print(json.dumps({'passed':not errors,'checks':checks,'errors':errors}));sys.exit(bool(errors))
def runtime(platform,item):
 name=item['name'];d=A/platform/name;q=read(d/'qualification.json');cu=platform=='copper'
 fixture=d/('probe.adf' if cu else 'probe.hdf');result=d/('guest.result.adf' if cu else 'run/candidate.hdf');before=Disk(fixture);after=Disk(result)
 need(q['inputImageSha256']==imagehash,'Wrong source image');need(q.get('fixtureImageSha256',q.get('probeImageSha256'))==sha(fixture),'Fixture changed');need(q['resultImageSha256']==sha(result),'Result changed')
 for n in ['l/shell-seg','libs/mathtrans.library','c/bootshell']:
  need(before.get(n)==normal.get(n)==after.get(n),'Changed system file: '+n)
 if cu:
  need(before.get('s/startup-sequence')==normal.get('s/startup-sequence'),'Changed normal startup')
  need(q['romSha256']==P['copperstartRomSha256'] and q['extensionRomSha256']==P['extensionRomSha256'],'Wrong CopperStart profile')
  need(q['probeSha256']==P['probeSha256'] and q['emulatorSha256']==sha(Path(P['probe']).with_name('CopperMod.Amiga.Lightweight.dll')),'Observer identity')
  t=read(d/'guest.trace.json');need(t['complete'] and not t.get('followupError'),'Incomplete script');need(all(t.get(k)==0 for k in ['remaining','childRemaining','backgroundRemaining']),'Shell tasks remain')
  if name=='builtins':need('RESET SENTINEL' not in t['scriptScreen'] and 'SCRIPT DONE' in t['scriptScreen'],'Physical console reset')
 else:
  launch=read(d/'run/launch.json');need(launch['romSha256']==P['kickstartRomSha256'],'Wrong Kickstart');need(launch['winuaeSha256']==sha('C:/Program Files/WinUAE/winuae64.exe'),'WinUAE identity');need(launch['sourceImageSha256']==sha(fixture) and launch['imageCopySha256']==sha(result),'Launch images')
  if item.get('diagnostic'):need(q['checksPassed'],'Unverified diagnostic observation')
 obs=read(d/'observations.json');count=0
 for n,v in obs.items():
  if n in metadata:continue
  raw=None if v is None else v.encode('latin1') if isinstance(v,str) else struct.pack('>'+'I'*len(v),*[x&0xffffffff for x in v]) if isinstance(v,list) else struct.pack('>I',v&0xffffffff)
  need(after.get(n)==raw,'Guest observation changed: '+n);count+=1
 need(count>0,'No guest observations')
 if name in ['owner','abi']:
  helper=Path(item['args'][item['args'].index('--runner-hunk')+1]);embedded=Disk(d/'helper.adf') if cu else before
  expected_hash='f6fe18375a8b62689a0ad69be9bc111b115eba64fb0f6a316b268e896ec8170c' if name=='owner' else 'd661ddba3d280113d6741625adde9d6e90ccf742952fdac15bae34cb5e702b67'
  need(sha(helper)==expected_hash,'Native helper identity')
  native_report=read(str(helper)+'.compatibility.json');native=native_report['NativeCompatibility']
  need(native_report['IsCompatible'] and native_report['Cpu']=='m68000' and native_report['RuntimeProfile']=='freestanding' and not native_report['ManagedAllocationSites'] and not native['ExceptionRegionCount'] and not native['FatalMachineFaultSiteCount'] and not native['RuntimeFeatures'] and not native['RuntimeHelpers'] and not native['ExternalNativeTargets'],'Native helper runtime gate')
  build_audit=read(helper.parent/'build-audit.json');need(build_audit['passed'] and build_audit['hunkSha256']==expected_hash,'Native helper build audit')
  need(any(embedded.get(n)==helper.read_bytes() for n in embedded.tree),'Native helper differs');need(after.get('runner-ok')==b'' and after.get('runner-fail') is None,'Native helper failure')
  if name=='owner':
   need(after.get('t/continuation.bin')==struct.pack('>I',0x3fff),'Reduced owner stage mask');oom=struct.unpack('>5I',after.get('t/continuation-oom.bin'));need(oom[0]>0 and oom[2:]==(1,0,103),'Heap failure not exercised')
  else:need(after.get('t/exported-output') is None and after.get('t/exported-script') is None,'ABI scratch leak')
 if name in ['new-cli','new-cli-exit-alias']:
  pre='t/' if cu else '';need(after.get(pre+'afterendshell') is None and after.get(pre+'newdone')==b'done\n' and after.get(pre+'parentdone')==b'done\n','EndShell failed')
 if name in ['run-lifetime','run-owned-lifetime','new-shell-lifetime','system-lifetime']:
  sample=struct.unpack('>3I',after.get('asynclife.bin' if name=='system-lifetime' else 'runlife.bin'));need(sample[0]>0 and len(set(sample))==1,'Retirement memory loss')
 receipts.append({'platform':platform,'case':name,'receiptSha256':sha(d/'qualification.json'),'fixtureSha256':sha(fixture),'resultSha256':sha(result),'guestFiles':count})
for platform in (['kickstart'] if '--kickstart-only' in sys.argv else ['copper','kickstart']):
 plan=P['copperCases'] if platform=='copper' else [{'name':n,'args':args} for n,args in P['kickstartPlan']['regular']]+[{'name':n,'args':args,'diagnostic':True} for n,args in P['kickstartPlan']['diagnostics']]+P['kickstartPlan']['additionalRequired']
 def matrix(platform=platform,plan=plan):
  run=read(A/platform/'run.json');need(run['passed'] and len(run['cases'])==len(plan),'Runtime plan incomplete')
 check(platform+'-complete-plan',matrix)
 for item in plan:check(platform+':'+item['name'],lambda platform=platform,item=item:runtime(platform,item))
def comparisons():
 c=read(A/'kickstart/builtins/original-comparison.json');need(c['passed'] and c['commonChecks']==57 and not c['errors'],'Original builtin comparison')
 reference_dir=ROOT/'artifacts/shell-builtins-original-ks-v19';reference=read(reference_dir/'observations.json')
 need(sha(reference_dir/'observations.json')==c['originalObservationSha256'] and sha(reference_dir/'probe.hdf')==c['originalFixtureSha256'],'Original builtin capture changed')
 reference_disk=Disk(reference_dir/'run/candidate.hdf');need(sha(reference_dir/'run/candidate.hdf')==reference['resultImageSha256'],'Original builtin result changed')
 from_expected=read(A/'kickstart/builtins/observations.json');cu=read(A/'copper/builtins/observations.json')
 sys.path.insert(0,str(A/'tools-source/tools/ImageBuilder'));import shell_builtin_fixture as fixture
 for n,value in fixture.expected().items():
  need(cu[n]==from_expected[n],'Cross-system builtin mismatch: '+n)
  if n not in ['bi-cls','bi-cls-reset','t/builtin-envarc/bivar']:need(reference_disk.get(n)==value,'Original builtin guest file changed: '+n)
check('all-60-builtins-and-57-original-V40-contracts',comparisons)
def ask():
 d=A/'kickstart/ask';q=read(d/'observations.json');before=Disk(d/'probe.hdf');after=Disk(d/'run/candidate.hdf')
 need(q['inputImageSha256']==imagehash and q['fixtureImageSha256']==sha(d/'probe.hdf') and q['resultImageSha256']==sha(d/'run/candidate.hdf'),'ASK image identities')
 for n in ['l/shell-seg','libs/mathtrans.library']:need(before.get(n)==normal.get(n)==after.get(n),'ASK system files')
 comparison=read(d/'original-comparison.json');need(comparison['matched'] and comparison['romSha256']==P['kickstartRomSha256'],'Original ASK mismatch')
 reference_dir=ROOT/'artifacts/shell-ask-reference-ks-v2';reference=read(reference_dir/'observations.json');reference_disk=Disk(reference_dir/'run/candidate.hdf')
 need(sha(reference_dir/'run/candidate.hdf')==reference['resultImageSha256'] and sha(reference_dir/'probe.hdf')==reference['fixtureImageSha256'],'Original ASK capture changed')
 for n,v in comparison['observations'].items():
  raw=v.encode('latin1') if isinstance(v,str) else struct.pack('>2i',*v);need(after.get(n)==raw and reference_disk.get(n)==raw,'ASK guest observation: '+n)
 receipts.append({'platform':'kickstart','case':'ask','receiptSha256':sha(d/'observations.json'),'fixtureSha256':sha(d/'probe.hdf'),'resultSha256':sha(d/'run/candidate.hdf'),'guestFiles':8})
check('kickstart:ask-and-original-reference',ask)
def inventory():
 source=(image.parent/read(image.parent/'system-build.json')['sourceSnapshot']).parent/'CopperOS/src/System/Shell/ShellInternalCommand.cs'
 body=source.read_text().split('public enum ShellInternalCommand : int')[1].split('{',1)[1].split('}',1)[0]
 names=re.findall(r'\b[A-Za-z]+\b',body.replace('Unknown = 0',''));coverage=read(ROOT/'artifacts/shell-v22-builtin-coverage-plan.json');covered=[n for g in coverage['groups'] for n in g['commands']]
 need(len(names)==len(covered)==len(set(covered))==31 and set(names)==set(covered),'Built-in inventory')
check('all-31-command-identities-covered',inventory)
def math():
 m=read(A/'math-acceptance.json');need(m['passed'] and m['imageSha256']==imagehash and m['mathtransVectors']==17 and m['identicalNumericalAndCcrSamples']==20,'Math acceptance')
check('unchanged-normal-image-two-boots-and-complete-math-probe',math)
def exact_corpus():
 p=A/'exact-corpus'
 for f in read(p/'inputs.json')['files']:need(sha(p/f['path'])==f['sha256'],'Exact-corpus input changed')
 need(sha(p/'helper.adf')==read(p/'fixture.json')['helperSha256'],'Read-only helper changed')
 sys.path.insert(0,str(A/'tools-source/tools/ImageBuilder'));import shell_builtin_fixture as fixture
 if '--kickstart-only' not in sys.argv:
  ready=p/'ready-observer'
  for f in read(ready/'inputs.json')['files']:need(sha(ready/f['path'])==f['sha256'],'Prompt-observer input changed')
  helper=Disk(ready/'helper.adf');need(helper.get('c/execute')==normal.get('c/execute'),'Fixture Execute differs from normal image')
 expected=fixture.expected();samples=[]
 for platform in (['kickstart'] if '--kickstart-only' in sys.argv else ['copper','kickstart']):
  prefix=p/('copper-ready' if platform=='copper' else 'kickstart-grouped');t=read(str(prefix)+'.json');d=Disk(str(prefix)+'.adf')
  need(t['complete'] and all(t[k]==0 for k in ['remaining','childRemaining','backgroundRemaining']),'Exact-startup tasks remain: '+platform)
  need(t['counter']==1+len(read(p/'commands-grouped.json')),'Exact command sequence incomplete')
  for name in ['s/startup-sequence','l/shell-seg','libs/mathtrans.library','c/bootshell']:need(d.get(name)==normal.get(name),'Normal startup or executable changed')
  for name,value in expected.items():need(d.get(name)==value,'Exact-startup corpus mismatch: '+platform+':'+name)
  data=d.get('t/mathresult');need(data and len(data)==124 and data[:4]==bytes(4),'Exact-startup math failed')
  samples.append(data)
  receipts.append({'platform':platform,'case':'unchanged-ADF-startup-full-builtins-and-math','resultSha256':sha(str(prefix)+'.adf'),'traceSha256':sha(str(prefix)+'.json'),'contracts':len(expected)})
 if len(samples)==2:need(samples[0]==samples[1],'Exact-startup math values/CCR differ')
check('normal-ADF-startup-full-builtins-and-math-on-both-ROMs',exact_corpus)
def host():
 for name,minimum in [('host-commands.log',1078),('host-ownership-corrected.log',99)]:
  t=(A/name).read_text(encoding='utf-8-sig');m=re.search(r'Failed:\s+0, Passed:\s+(\d+), Skipped:\s+0',t);need(m and int(m[1])>=minimum,'Host tests: '+name)
check('host-command-and-ownership-regressions',host)
report={'kind':'normal-system-full-shell-and-math-runtime-acceptance','platformSelection':'kickstart-only' if '--kickstart-only' in sys.argv else 'both','auditSha256':sha(__file__),'planSha256':sha(A/'plan.json'),'passed':not errors,'imageSha256':imagehash,'shellSha256':hashlib.sha256(normal.get('l/shell-seg')).hexdigest(),'mathtransSha256':hashlib.sha256(normal.get('libs/mathtrans.library')).hexdigest(),'checks':checks,'receipts':receipts,'errors':errors,'scope':('Complete 31-case Kickstart shared-Shell replay, exact normal-ADF built-in corpus/math, and the fresh two-ROM math check. Full CopperStart Shell replay remains separate and is not accepted by this scoped report.' if '--kickstart-only' in sys.argv else 'Fresh replay of the complete accepted shared-Shell matrix with the normal image binaries: 20 CopperStart cases, 31 Kickstart cases, and exact unchanged-image boots/math on both. CopperStart command fixtures add test files but preserve normal startup. Kickstart command fixtures use guest keyboard injection on disposable HDFs. The complete built-in corpus and math also run after booting the untouched normal ADF on both profiles, with test assets supplied through public guest DOS operations from read-only DF1; the CopperStart exact-ADF corpus uses temporary C:/S: test-data assigns while SYS: remains the boot image. No exhaustive language or physical-hardware claim.')}
options.output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'passed':report['passed'],'checks':len(checks),'receipts':len(receipts),'errors':errors}));sys.exit(bool(errors))
