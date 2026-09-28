"""Verify original-DOS same-image MakeLink loop rejection concurrent with success."""
import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r):
    require(r.get('commandUnderTest') == 'MakeLink' and r.get('rootInfoReady') and r.get('diskBytesUnchanged')
            and not any(r.get(k) for k in ['failure','boundedStop','dosObservationOverflow','returnObservationCollision']), 'Incomplete trace')
    require(r.get('copyMaximumActiveInvocations') == 2 and r.get('copyActiveInvocations') == 0
            and r.get('copyCpuImageWrites') == [], 'Completed overlap')
    rows = r['copyInvocationOwnership']
    require(len(rows) == 2 and len({v['Task'] for v in rows}) == 2 and len({v['Segment'] for v in rows}) == 1
            and sorted(v['ReturnCode'] for v in rows) == [0,20], 'Tasks/segment/results')
    require(all(v['Allocations'] == v['Frees'] == 1 and v['ImageUnchanged'] for v in rows), 'Invocation ownership')
    e = r['observedDosCalls']; segment = rows[0]['Segment']; intervals=[]
    for row in rows:
        task = [v for v in e if v['Task'] == row['Task']]
        start = one([v for v in task if v['Name']=='CopyOpenLibrary'], 'Library start')
        end = one([v for v in task if v['Name']=='CopyCloseLibrary'], 'Library end')
        intervals.append((e.index(start),e.index(end)))
        require(start.get('ReturnedD0',0) != 0 and end['A1'] == start['ReturnedD0'] and 'ReturnedD0' in end, 'Library cleanup')
        c = task[task.index(start):task.index(end)+1]
        parser = one([v for v in c if v['Name']=='ReadArgs'], 'Parser')
        free = one([v for v in c if v['Name']=='FreeArgs'], 'Parser free')
        require(parser['Text']=='FROM/A,TO/A,HARD/S,FORCE/S' and parser.get('ReturnedD0',0)!=0
                and free['D1']==parser['ReturnedD0'] and 'ReturnedD0' in free, 'Parser ownership')
        fib = one([v for v in c if v['Name']=='AllocDosObject'], 'FIB')
        examined = one([v for v in c if v['Name']=='Examine'], 'Examine')
        freed = one([v for v in c if v['Name']=='FreeDosObject'], 'FIB free')
        require(fib['D1']==freed['D1']==2 and fib.get('ReturnedD0',0)!=0 and
                fib['ReturnedD0']==examined['D2']==freed['D2'] and examined['D2']%4==0
                and examined.get('ReturnedD0',0)!=0, 'FIB ownership')
        acquired = [v for v in c if v['Name'] in ['Lock','ParentDir'] and v.get('ReturnedD0',0)!=0]
        released = [v for v in c if v['Name']=='UnLock']
        require(Counter(v['ReturnedD0'] for v in acquired)==Counter(v['D1'] for v in released)
                and all('ReturnedD0' in v and c.index(v)<c.index(free) for v in released), 'Task lock ownership')
        locks = [v for v in c if v['Name']=='Lock']; links=[v for v in c if v['Name']=='MakeLink']
        require(examined['D1']==locks[0]['ReturnedD0'], 'Examined target')
        if row['ReturnCode']==20:
            require([v['Text'] for v in locks]==['RAM:link-dir','RAM:link-dir/a/b/c'] and
                    len([v for v in c if v['Name']=='ParentDir'])==3 and examined['DirectoryEntryType']>0
                    and not links, 'Deep loop check')
            diagnostic=one([v for v in c if v['Name']=='VPrintf'],'Loop diagnostic')
            fault=one([v for v in c if v['Name']=='PrintFault'],'Loop fault')
            require(diagnostic['Text']=='Link loop from %s to %s not allowed\n' and fault['D1']==0
                    and c.index(diagnostic)<c.index(fault)<c.index(free), 'Loop reporting')
        else:
            link=one(links,'Successful link')
            require(len(locks)==1 and locks[0]['Text']=='RAM:link-dir/payload' and
                    examined['DirectoryEntryType']<0 and link['Text']=='RAM:foreground' and
                    link['D2']==locks[0]['ReturnedD0'] and link['D3']==0 and link.get('ReturnedD0',0)!=0
                    and not any(v['Name']=='PrintFault' for v in c), 'Foreground success')
    require(max(a for a,b in intervals)<min(b for a,b in intervals), 'No overlapping DOS leases')
    removal=one([v for v in e if v['Name']=='RemSegment' and v.get('RemovedSegmentList')==segment],'Removal')
    freed=one([v for v in e if v['Name']=='SegmentFreeMem'],'Segment free')
    require(removal.get('ReturnedD0',0)!=0 and max(b for a,b in intervals)<e.index(removal)<e.index(freed)
            and freed['A1']==r['copySegmentAllocationBase'] and freed['D0']==r['copySegmentAllocationBytes']
            and 'ReturnedD0' in freed,'Resident release')
    match=one([v for v in e if v['Name']=='MatchFirst' and v.get('Text')=='RAM:foreground'],'Readback path')
    opened=one([v for v in e if v['Name']=='Open' and v.get('Text')=='foreground'],'Readback open')
    pos=e.index(opened);handle=opened.get('ReturnedD0',0)
    require(e.index(freed)<e.index(match)<pos and match.get('ReturnedD0')==0 and match.get('FileSize')==18 and handle!=0,'Readback result')
    closed=next(v for v in e[pos+1:] if v['Name']=='Close' and v['D1']==handle and v['Task']==opened['Task'])
    require([v.get('ReturnedD0') for v in e[pos+1:e.index(closed)] if v['Name']=='FGetC' and v['D1']==handle
             and v['Task']==opened['Task']]==list(b'hard-link-payload\n') and closed.get('ReturnedD0',0)!=0,'Readback bytes')
    return {'status':'original-dos-workbench-makelink-concurrent-loop-success-verified','tasks':[v['Task'] for v in rows],
            'deepAncestorSteps':3,'foregroundBytes':18,'fullCommandQualified':False,'installedFlagsQualified':False}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('observations',type=Path);p.add_argument('--output',type=Path,required=True)
    args=p.parse_args();data=args.observations.read_bytes();result=verify(json.loads(data))
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
