"""Reject a plausible in-stack pointer that is not the actual captured A7."""
from pathlib import Path
import json,re,shutil,struct,sys
from qualify_sizes import receipt,suites
from measure_c import identity

stage=Path(sys.argv[1]).resolve(); root=stage/'tasklist-negative-control'
(root/'C').mkdir(parents=True)
source=stage/'candidate/C/TaskList'; image=root/'C/TaskList'; data=bytearray(source.read_bytes())
match=re.search(r'(?m)^([0-9A-Fa-f]{8})\s+(\d+)\s+CopperOS.Commands.Native.NativeMorphOSTaskListCommand::WriteVerbose\s*$',Path(str(source)+'.map').read_text())
start=int(match[1],16); end=start+int(match[2]); positions=[p for p in range(start,end,2) if struct.unpack_from('>H',data,32+p)[0]&0xf03f==0x200f]
assert len(positions)==1
where=32+positions[0]; old=struct.unpack_from('>H',data,where)[0]
# Change only the source An from SP/A7 to reserved invocation-context A5.
# A5 is a valid in-stack address here; the old range-only check accepts it.
struct.pack_into('>H',data,where,old-2); image.write_bytes(data)
shutil.copy2(Path(str(source)+'.map'),str(image)+'.map')
original=receipt(root,'TaskList','68000',suites()['TaskList'][0],stage/'runner/CopperOS.Commands.NativeExecution.dll')
assert original['status']=='passed',original.get('failure')
old_report=root/'execution'/f'TaskList.68000.{suites()["TaskList"][0]}.json'
shutil.copy2(old_report,root/'old-range-only-receipt.json')
strong=receipt(root,'TaskList','68000',suites()['TaskList'][0],stage/'tasklist-runner-v2/CopperOS.Commands.NativeExecution.dll')
assert strong['status']=='failed' and 'verbose-running-stack-pointer' in strong.get('failure','')
result=dict(image=identity(image),original=identity(source),offset=where,oldOpcode=old,mutatedOpcode=old-2,
 oldRangeOnlyStatus=original['status'],strongStatus=strong['status'],strongFailure=strong['failure'],
 oldReceipt=identity(root/'old-range-only-receipt.json'),strongReceipt=identity(old_report))
(root/'control.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('Invalid but in-stack A5 value: old range-only fixture passed; exact live-SP fixture rejected it.')
