"""Verify bounded original Workbench Rename direct and directory-mode evidence."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, candidate=False):
    require(r.get('scope')=='CC12/disposable-Rename-derivative-boot-progress-only' and
            r.get('commandUnderTest')=='Rename' and r.get('rootInfoReady') and r.get('diskBytesUnchanged')
            and not any(r.get(k) for k in ['failure','boundedStop','dosObservationOverflow','returnObservationCollision']), 'Incomplete trace')
    rows=r['copyInvocationOwnership']
    require(len(rows)==2 and all(v['ReturnCode']==0 and v['Allocations']==v['Frees']==4 and
            v['ImageUnchanged'] and v['Segment']==rows[0]['Segment'] for v in rows), 'Reference results/ownership')
    require(r.get('copyActiveInvocations')==0 and r.get('copyCpuImageWrites')==[], 'Resident image')
    e=r['observedDosCalls'];starts=[i for i,v in enumerate(e) if v['Name']=='CopyOpenLibrary'];ends=[i for i,v in enumerate(e) if v['Name']=='CopyCloseLibrary']
    require(len(starts)==len(ends)==2 and starts[0]<ends[0]<starts[1]<ends[1], 'Library leases')
    for n,(a,b) in enumerate(zip(starts,ends)):
        c=e[a:b+1]
        allocations=[v for v in c if v['Name']=='CopyAllocVec'];frees=[v for v in c if v['Name']=='CopyFreeMem']
        require([v['D0'] for v in allocations]==[80,538,256,256] and len(frees)==4,'AllocVec requests')
        require(sorted((v['VectorAllocationBase'],v['VectorAllocationBytes']) for v in allocations)==
                sorted((v['A1'],v['D0']) for v in frees) and all('ReturnedD0' in v for v in frees),'Vector storage cleanup')
        parser=one([v for v in c if v['Name']=='ReadArgs'],'Parser')
        free=one([v for v in c if v['Name']=='FreeArgs'],'Parser cleanup')
        require(parser['Text']=='FROM/A/M,TO=AS/A,QUIET/S' and parser.get('ReturnedD0',0)!=0 and
                free['D1']==parser['ReturnedD0'] and 'ReturnedD0' in free,'ReadArgs identity')
        require(not any(v['Name'] in ['VPrintf','PrintFault'] for v in c),'Unexpected progress/fault')
        if candidate:
            fibs=[v for v in c if v['Name']=='AllocDosObject']
            released=[v for v in c if v['Name']=='FreeDosObject']
            require(len(fibs)==len(released)==n,'Candidate FIB ownership count')
            if n:
                require(fibs[0]['D1']==released[0]['D1']==2 and
                        fibs[0].get('ReturnedD0',0)!=0 and
                        released[0]['D2']==fibs[0]['ReturnedD0'] and
                        'ReturnedD0' in released[0], 'Candidate FIB cleanup')
        first=[v for v in c if v['Name']=='MatchFirst'];nexts=[v for v in c if v['Name']=='MatchNext']
        matches=[v for v in c if v['Name']=='MatchEnd'];renames=[v for v in c if v['Name']=='Rename']
        require(all(v.get('ReturnedD0')==0 for v in first) and all('ReturnedD0' in v for v in matches),'Matcher completion')
        if n==0:
            pattern=one([v for v in c if v['Name']=='ParsePattern'],'Direct pattern')
            require([v['Text'] for v in first]==['RAM:old'] and len(matches)==1 and not nexts and
                    pattern['Text']=='RAM:old' and pattern.get('ReturnedD0')==0 and
                    c.index(matches[0])<c.index(pattern)<c.index(renames[0]),'Direct dispatch')
            expected=[('RAM:old','RAM:new')]
        else:
            require([v['Text'] for v in first]==['RAM:new','RAM:new','RAM:other'] and len(matches)==3
                    and len(nexts)==2 and all(v.get('ReturnedD0')==232 for v in nexts),'Directory matching')
            require(len(renames)==2 and all(c.index(first[k+1])<c.index(nexts[k])<c.index(renames[k])<c.index(matches[k+1]) for k in range(2)), 'Advance before rename')
            expected=[('RAM:new','Ram Disk:target/new'),('RAM:other','Ram Disk:target/other')]
        require([(v['Text'],v['DestinationText']) for v in renames]==expected and all(v.get('ReturnedD0',0)!=0 for v in renames),'Rename paths/results')
    removal=one([v for v in e if v['Name']=='RemSegment' and v.get('RemovedSegmentList')==rows[0]['Segment']],'Resident removal')
    require(removal.get('ReturnedD0',0)!=0 and e.index(removal)>ends[-1],'Removal result')
    for name,payload in [('new',b'rename-first\n'),('other',b'rename-second\n')]:
        match=one([v for v in e[ends[-1]:] if v['Name']=='MatchFirst' and v.get('Text')=='RAM:target/'+name],'Readback path')
        opened=one([v for v in e[ends[-1]:] if v['Name']=='Open' and v.get('Text')==name],'Readback open')
        a=e.index(opened);handle=opened.get('ReturnedD0',0)
        require(e.index(removal)<e.index(match)<a and handle!=0 and match.get('FileSize')==len(payload),'Readback size')
        close=next(v for v in e[a+1:] if v['Name']=='Close' and v['D1']==handle and v['Task']==opened['Task'])
        require([v.get('ReturnedD0') for v in e[a+1:e.index(close)] if v['Name']=='FGetC' and v['D1']==handle and
                 v['Task']==opened['Task']]==list(payload) and close.get('ReturnedD0',0)!=0,'Readback bytes')
    return {'status':('candidate-rename-direct-directory-verified' if candidate else 'original-rename-direct-directory-reference-verified'),'returns':[0,0],'renamedObjects':3,
            'replacementQualified':False,'fullContractQualified':False,'installedFlagsQualified':False}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('observations',type=Path);p.add_argument('--output',type=Path,required=True)
    p.add_argument('--candidate',action='store_true',help='Verify candidate behavior and its additional directory FIB allocation; not original-reference evidence.')
    args=p.parse_args();data=args.observations.read_bytes();result=verify(json.loads(data),candidate=args.candidate)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
