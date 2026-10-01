"""Verify two original-DOS processes executing the same candidate Rename image."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r):
    require(r.get('commandUnderTest')=='Rename' and r.get('rootInfoReady') and r.get('diskBytesUnchanged') and
            not any(r.get(k) for k in ['failure','boundedStop','dosObservationOverflow','returnObservationCollision']), 'Incomplete trace')
    require(r.get('copyMaximumActiveInvocations')==2 and r.get('copyActiveInvocations')==0 and r.get('copyCpuImageWrites')==[], 'Completed overlap')
    rows=r['copyInvocationOwnership']; e=r['observedDosCalls']; intervals=[]; prefixes=[]
    require(len(rows)==2 and len({v['Task'] for v in rows})==2 and len({v['Segment'] for v in rows})==1 and
            all(v['ReturnCode']==0 and v['ImageUnchanged'] and v['Allocations']==v['Frees']==4 for v in rows), 'Task ownership/results')
    for row in rows:
        task=[v for v in e if v['Task']==row['Task']]
        start=one([v for v in task if v['Name']=='CopyOpenLibrary'],'DOS open')
        end=one([v for v in task if v['Name']=='CopyCloseLibrary'],'DOS close')
        intervals.append((e.index(start),e.index(end)))
        require(start.get('ReturnedD0',0)!=0 and end['A1']==start['ReturnedD0'] and 'ReturnedD0' in end,'DOS lease')
        c=task[task.index(start):task.index(end)+1]
        alloc=[v for v in c if v['Name']=='CopyAllocVec']; frees=[v for v in c if v['Name']=='CopyFreeMem']
        require([v['D0'] for v in alloc]==[80,538,256,256] and len(frees)==4 and
                sorted((v['VectorAllocationBase'],v['VectorAllocationBytes']) for v in alloc)==sorted((v['A1'],v['D0']) for v in frees),'Vector ownership')
        parser=one([v for v in c if v['Name']=='ReadArgs'],'Parser'); free=one([v for v in c if v['Name']=='FreeArgs'],'Parser release')
        require(parser.get('ReturnedD0',0)!=0 and free['D1']==parser['ReturnedD0'] and 'ReturnedD0' in free,'Parser identity')
        fib=one([v for v in c if v['Name']=='AllocDosObject'],'FIB'); fib_free=one([v for v in c if v['Name']=='FreeDosObject'],'FIB release')
        require(fib.get('ReturnedD0',0)!=0 and fib_free['D2']==fib['ReturnedD0'] and 'ReturnedD0' in fib_free,'FIB identity')
        lock=one([v for v in c if v['Name']=='Lock'],'Destination lock'); unlock=one([v for v in c if v['Name']=='UnLock'],'Destination release')
        require(lock.get('ReturnedD0',0)!=0 and unlock['D1']==lock['ReturnedD0'] and 'ReturnedD0' in unlock,'Lock identity')
        prefix='bg' if lock['Text']=='RAM:bg-target' else 'fg'
        require(lock['Text']==f'RAM:{prefix}-target','Destination'); prefixes.append(prefix)
        renames=[v for v in c if v['Name']=='Rename']; nexts=[v for v in c if v['Name']=='MatchNext']; ends=[v for v in c if v['Name']=='MatchEnd']
        require(len(renames)==len(nexts)==2 and len(ends)==3 and all('ReturnedD0' in v for v in ends),'Traversal count')
        for n,suffix in enumerate(['one','two']):
            v=renames[n]
            require(v['Text']==f'RAM:{prefix}-{suffix}' and v['DestinationText']==f'Ram Disk:{prefix}-target/{prefix}-{suffix}' and
                    v.get('ReturnedD0',0)!=0 and nexts[n].get('ReturnedD0')==232 and c.index(nexts[n])<c.index(v)<c.index(ends[n+1]),'Mutation paths/order')
        require(not any(v['Name'] in ['VPrintf','PrintFault'] for v in c),'Unexpected output')
    require(sorted(prefixes)==['bg','fg'] and max(a for a,b in intervals)<min(b for a,b in intervals),'Overlapping DOS leases')
    removal=one([v for v in e if v['Name']=='RemSegment' and v.get('RemovedSegmentList')==rows[0]['Segment']],'Resident removal')
    require(removal.get('ReturnedD0',0)!=0 and e.index(removal)>max(b for a,b in intervals),'Removal order/result')
    tail=e[e.index(removal)+1:]
    for prefix,word in [('bg','background'),('fg','foreground')]:
        for suffix in ['one','two']:
            name=f'{prefix}-{suffix}'; payload=f'{word}-{suffix}\n'.encode()
            match=one([v for v in tail if v['Name']=='MatchFirst' and v.get('Text')==f'RAM:{prefix}-target/{name}'],'Readback path')
            opened=one([v for v in tail if v['Name']=='Open' and v.get('Text')==name],'Readback open'); handle=opened.get('ReturnedD0',0); pos=e.index(opened)
            require(handle!=0 and e.index(match)<pos,'Readback order')
            close=next(v for v in e[pos+1:] if v['Name']=='Close' and v['D1']==handle and v['Task']==opened['Task'])
            require([v.get('ReturnedD0') for v in e[pos+1:e.index(close)] if v['Name']=='FGetC' and v['D1']==handle and v['Task']==opened['Task']]==list(payload) and close.get('ReturnedD0',0)!=0,'Readback bytes')
    return {'status':'candidate-rename-original-dos-concurrent-verified','tasks':[v['Task'] for v in rows],'renames':4,'fullContractQualified':False,'pureAdmission':False}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__); p.add_argument('observations',type=Path); p.add_argument('--output',type=Path,required=True)
    args=p.parse_args(); data=args.observations.read_bytes(); result=verify(json.loads(data))
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
