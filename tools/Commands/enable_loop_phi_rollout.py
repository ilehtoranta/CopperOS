"""Select strictly smaller, fully qualified command policies and enable only those."""
import json
from pathlib import Path
import re
import sys
from audit_loop_phi_rollout import REPO,validate,read
from measure_c import identity

stage=Path(sys.argv[1]).resolve(); manifest=validate(stage)
rows=read(stage/'candidate/qualification.json'); profiles=read(stage/'profiles.json')
task=read(stage/'tasklist-qualification.json'); control=read(stage/'tasklist-negative-control/control.json')
assert all(r['qualified'] for r in task['checks']) and control['strongStatus']=='failed' and control['oldRangeOnlyStatus']=='passed'
qualified={r['command']:r['qualified'] for r in rows}; qualified['TaskList']=True
measurements=read(stage/'measurements.json'); chosen=[]; decisions=[]
for name in manifest['commands']:
 pair={r['label']:r for r in measurements if r['command']==name}
 base=pair['baseline']['image']['bytes']; after=pair['candidate']['image']['bytes']; before=(stage/'installed/C'/name).stat().st_size
 private=[r for r in profiles if r['name']==name]
 reason=None
 if not qualified.get(name): reason='No qualified shipping-entry execution fixture.'
 elif after>=before: reason='Not strictly smaller than the installed executable.'
 elif any('checks' not in r or not all(c['qualified'] for c in r['checks']) for r in private): reason='An alternate profile remains unqualified; baseline and candidate fail the same private Version read.'
 if reason is None: chosen.append(name)
 decisions.append(dict(command=name,installedBytes=before,baselineBytes=base,candidateBytes=after,passSavings=base-after,installedSavings=before-after,enable=reason is None,reason=reason,
                       taskListLiveSpProof=name=='TaskList'))
assert 'Exe2Arc' not in chosen and 'Quote' not in chosen and 'Version' not in chosen
path=REPO/'src/Commands/CodeSizePolicies.props'; text=path.read_text(encoding='utf-8'); original=identity(path)
for name in chosen:
 match=re.search(r'(<PropertyGroup Condition="\x27\$\(CopperOSCommandName\)\x27 == \x27'+re.escape(name)+r'\x27[^>]*>\s*<CopperOSCodeSizePasses>)([^<]+)(</CopperOSCodeSizePasses>)',text)
 assert match,name
 assert 'SimplifyLoopPhis' not in match[2]
 text=text[:match.start(2)]+match[2]+',SimplifyLoopPhis'+text[match.end(2):]
path.write_text(text,encoding='utf-8')
(stage/'selection.json').write_text(json.dumps(dict(selected=chosen,decisions=decisions,policyBefore=original,policyAfter=identity(path)),indent=2)+'\n',encoding='utf-8')
print('Enable:',','.join(chosen)); print('Net installed savings:',sum(r['installedSavings'] for r in decisions if r['enable']))
