"""Verify original Rename's unstarted/repeated MatchEnd and failure recovery."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, candidate=False):
    end_counts = [0,1,1] if candidate else [1,2,1]
    require(r.get('commandUnderTest')=='Rename' and r.get('rootInfoReady') and r.get('diskBytesUnchanged')
            and not any(r.get(k) for k in ['failure','boundedStop','dosObservationOverflow','returnObservationCollision']), 'Incomplete trace')
    rows=r['copyInvocationOwnership']
    require(len(rows)==3 and [v['ReturnCode'] for v in rows]==[20,20,0] and all(
        v['Segment']==rows[0]['Segment'] and v['Allocations']==v['Frees']==4 and v['ImageUnchanged'] for v in rows),'Results/ownership')
    require(r.get('copyActiveInvocations')==0 and r.get('copyCpuImageWrites')==[], 'Image lifetime')
    e=r['observedDosCalls'];starts=[i for i,v in enumerate(e) if v['Name']=='CopyOpenLibrary'];ends=[i for i,v in enumerate(e) if v['Name']=='CopyCloseLibrary']
    require(len(starts)==len(ends)==3,'Library lease count')
    for n,(a,b) in enumerate(zip(starts,ends)):
        require(a<b and (n==2 or b<starts[n+1]),'Sequential leases')
        c=e[a:b+1];alloc=[v for v in c if v['Name']=='CopyAllocVec'];frees=[v for v in c if v['Name']=='CopyFreeMem']
        require([v['D0'] for v in alloc]==[80,538,256,256] and len(frees)==4,'Vector allocations')
        require(sorted((v['VectorAllocationBase'],v['VectorAllocationBytes']) for v in alloc)==
                sorted((v['A1'],v['D0']) for v in frees) and all('ReturnedD0' in v for v in frees),'Vector cleanup')
        parser=one([v for v in c if v['Name']=='ReadArgs'],'ReadArgs');matches=[v for v in c if v['Name']=='MatchFirst']
        ends_search=[v for v in c if v['Name']=='MatchEnd'];renames=[v for v in c if v['Name']=='Rename']
        require(parser['Text']=='FROM/A/M,TO=AS/A,QUIET/S' and len(ends_search)==end_counts[n]
                and all(v['D1']==alloc[1]['ReturnedD0'] and 'ReturnedD0' in v for v in ends_search),'MatchEnd identity/count/completion')
        parser_free=[v for v in c if v['Name']=='FreeArgs']
        if candidate:
            fibs=[v for v in c if v['Name']=='AllocDosObject']
            released=[v for v in c if v['Name']=='FreeDosObject']
            require(len(fibs)==len(released)==(1 if n==1 else 0),'Candidate FIB count')
            if fibs:
                require(fibs[0]['D1']==released[0]['D1']==2 and fibs[0].get('ReturnedD0',0)!=0 and
                        released[0]['D2']==fibs[0]['ReturnedD0'] and 'ReturnedD0' in released[0], 'Candidate FIB cleanup')
        if n==0:
            require(parser.get('ReturnedD0')==0 and parser.get('ReturnedIoErr')==116 and not matches and
                    not renames and not parser_free and (candidate or c.index(parser)<c.index(ends_search[0])),'Unstarted search case')
        else:
            require(parser.get('ReturnedD0',0)!=0 and len(matches)==len(renames)==1 and
                    matches[0]['Text']=='RAM:old' and matches[0].get('ReturnedD0')==0,'Started search')
            free=one(parser_free,'Parser release');require(free['D1']==parser['ReturnedD0'] and 'ReturnedD0' in free,'Parser identity')
            rename=renames[0];require(rename['Text']=='RAM:old' and rename['DestinationText']==('RAM:existing' if n==1 else 'RAM:recovered'),'Rename paths')
            require(c.index(matches[0])<c.index(ends_search[0])<c.index(rename),'Initial MatchEnd ordering')
            if n==1:
                require(rename.get('ReturnedD0')==0 and rename.get('ReturnedIoErr')==203
                        and (candidate or c.index(rename)<c.index(ends_search[1])),'Duplicate failure/search cleanup')
            else: require(rename.get('ReturnedD0',0)!=0,'Recovery rename')
        faults=[v for v in c if v['Name']=='PrintFault']
        if n<2:
            fault=one(faults,'Fault');require(fault['D1']==[116,203][n] and fault['D2']==0 and
                    (not ends_search or c.index(ends_search[-1])<c.index(fault)) and
                    c.index(parser)<c.index(fault),'Fault after parser/search cleanup')
        else:require(not faults,'Recovery fault')
    for name,payload in [('recovered',b'rename-first\n'),('existing',b'preserved-target\n')]:
        opened=one([v for v in e[ends[-1]:] if v['Name']=='Open' and v.get('Text')==name],'Readback')
        a=e.index(opened);handle=opened.get('ReturnedD0',0);require(handle!=0,'Readback handle')
        close=next(v for v in e[a+1:] if v['Name']=='Close' and v['D1']==handle and v['Task']==opened['Task'])
        require([v.get('ReturnedD0') for v in e[a+1:e.index(close)] if v['Name']=='FGetC' and
                 v['D1']==handle and v['Task']==opened['Task']]==list(payload) and close.get('ReturnedD0',0)!=0,'Preserved/recovered bytes')
    removal=one([v for v in e if v['Name']=='RemSegment' and v.get('RemovedSegmentList')==rows[0]['Segment']],'Resident removal')
    require(removal.get('ReturnedD0',0)!=0 and e.index(removal)>ends[-1],'Resident removal result/order')
    return {'status':('candidate-rename-error-recovery-verified' if candidate else 'original-rename-unstarted-repeated-matchend-reference-verified'),'returns':[20,20,0],
            'matchEndCounts':end_counts,'opaqueMatcherAllocationsQualified':False,'fullContractQualified':False,'replacementQualified':False}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('observations',type=Path);p.add_argument('--output',type=Path,required=True)
    p.add_argument('--candidate',action='store_true',help='Require once-per-attempt cleanup, explicitly distinct from original redundant MatchEnd calls.')
    args=p.parse_args();data=args.observations.read_bytes();result=verify(json.loads(data),candidate=args.candidate)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
