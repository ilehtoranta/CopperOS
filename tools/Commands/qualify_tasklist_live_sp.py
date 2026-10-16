"""Strict TaskList comparison with independently observed live-SP evidence only."""
import json
from pathlib import Path
import re
import shutil
import struct
import sys
from measure_c import identity
from qualify_sizes import comparable,receipt,suites

stage=Path(sys.argv[1]).resolve(); runner=stage/'tasklist-runner-v2/CopperOS.Commands.NativeExecution.dll'
marker=re.compile(r'(?m)^:M68k  SPReg 0x([0-9a-f]{8})$')
rows=[]
for cpu in ['68000','68020','68040']:
 reports=[receipt(stage/('tasklist-'+label+'-v2'),'TaskList',cpu,suites()['TaskList'][0],runner) for label in ['baseline','candidate']]
 assert all(r['status']=='passed' for r in reports)
 assert len(reports[0]['cases'])==len(reports[1]['cases'])==57
 proofs=[]
 normalized=[]
 for label,report in zip(['baseline','candidate'],reports):
  image=stage/label/'C/TaskList'; data=image.read_bytes()
  assert struct.unpack_from('>6I',data)==(1011,0,1,0,0,struct.unpack_from('>I',data,20)[0])
  assert struct.unpack_from('>I',data,24)[0]==1001
  code=data[32:]
  text=Path(str(image)+'.map').read_text(encoding='utf-8-sig')
  match=re.search(r'(?m)^([0-9A-Fa-f]{8})\s+(\d+)\s+CopperOS.Commands.Native.NativeMorphOSTaskListCommand::WriteVerbose\s*$',text)
  assert match; start=0x100000+int(match[1],16); end=start+int(match[2])
  cases=[]
  for case in report['cases']:
   value=comparable(case); trace=value.pop('taskListStackPointerReads')
   output=bytes.fromhex(case['stdoutHex']).decode('latin1'); fields=marker.findall(output)
   if trace:
    assert case['name']=='verbose-running-stack-pointer' and len(trace)==len(fields)==1
    entry=trace[0]; assert int(fields[0],16)==entry['Value']
    pc=entry['ProgramCounter']; assert start<=pc<end
    opcode=struct.unpack_from('>H',code,pc-0x100000)[0]
    assert opcode & 0xf03f==0x200f and ((opcode>>6)&7) in [0,5]
    proofs.append(dict(image=label,case=case['name'],pc=pc,opcode=opcode,actualSp=entry['Value'],printed=fields[0]))
    value['stdoutHex']=marker.sub(':M68k  SPReg <verified live A7>',output).encode('latin1').hex()
   else: assert not fields or case['name']!='verbose-running-stack-pointer'
   cases.append(value)
  normalized.append(cases)
 assert normalized[0]==normalized[1], 'A difference other than the verified live SP is not acceptable.'
 rows.append(dict(cpu=cpu,qualified=True,invocations=57,proofs=proofs,
  instructionsBefore=sum(c['instructions'] for c in reports[0]['cases']),instructionsAfter=sum(c['instructions'] for c in reports[1]['cases']),
  peakStackBefore=max(c['peakStackBytes'] for c in reports[0]['cases']),peakStackAfter=max(c['peakStackBytes'] for c in reports[1]['cases']),
  baselineReceipt=identity(stage/'tasklist-baseline-v2/execution'/f'TaskList.{cpu}.{suites()["TaskList"][0]}.json'),
  candidateReceipt=identity(stage/'tasklist-candidate-v2/execution'/f'TaskList.{cpu}.{suites()["TaskList"][0]}.json')))
(stage/'tasklist-qualification.json').write_text(json.dumps(dict(runner=identity(runner),checks=rows),indent=2)+'\n',encoding='utf-8')
print('TaskList: all 57 cases per CPU agree; only independently verified live A7 values differ.')
