"""Verify original Rename nondirectory diagnostics, omitted MatchEnd and recovery."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, candidate=False):
    require(r.get('commandUnderTest')=='Rename' and r.get('rootInfoReady') and r.get('diskBytesUnchanged')
            and not any(r.get(k) for k in ['failure','boundedStop','dosObservationOverflow','returnObservationCollision']),'Incomplete trace')
    rows=r['copyInvocationOwnership']
    require(len(rows)==3 and all(v['ReturnCode']==0 and v['Allocations']==v['Frees']==4 and
            v['ImageUnchanged'] and v['Segment']==rows[0]['Segment'] for v in rows),'Results/command ownership')
    e=r['observedDosCalls'];starts=[i for i,v in enumerate(e) if v['Name']=='CopyOpenLibrary'];ends=[i for i,v in enumerate(e) if v['Name']=='CopyCloseLibrary']
    require(r.get('copyActiveInvocations')==0 and r.get('copyCpuImageWrites')==[],'Resident image lifetime')
    require(len(starts)==len(ends)==3,'Leases')
    for n,(a,b) in enumerate(zip(starts,ends)):
        require(a<b and (n==2 or b<starts[n+1]),'Lease order')
        c=e[a:b+1];match=one([v for v in c if v['Name']=='MatchFirst'],'Preflight')
        allocations=[v for v in c if v['Name']=='CopyAllocVec'];frees=[v for v in c if v['Name']=='CopyFreeMem']
        require([v['D0'] for v in allocations]==[80,538,256,256] and len(frees)==4,'Vector requests')
        require(sorted((v['VectorAllocationBase'],v['VectorAllocationBytes']) for v in allocations)==
                sorted((v['A1'],v['D0']) for v in frees) and all('ReturnedD0' in v for v in frees),'Vector cleanup identity')
        parser=one([v for v in c if v['Name']=='ReadArgs'],'Parser')
        parser_free=one([v for v in c if v['Name']=='FreeArgs'],'Parser cleanup')
        require(parser['Text']=='FROM/A/M,TO=AS/A,QUIET/S' and parser.get('ReturnedD0',0)!=0 and
                parser_free['D1']==parser['ReturnedD0'] and 'ReturnedD0' in parser_free,'Parser ownership')
        require(match['Text']==('RAM:rn-#?' if n==1 else 'RAM:rn-one') and match.get('ReturnedD0')==0,'Preflight result')
        require(not any(v['Name']=='PrintFault' for v in c),'Unexpected fault')
        if n<2:
            require(not any(v['Name'] in ['Rename','MatchNext'] for v in c),'Rejection mutated or advanced search')
            search_ends=[v for v in c if v['Name']=='MatchEnd']
            require(len(search_ends)==(1 if candidate else 0),'Profile search cleanup count')
            diagnostic=one([v for v in c if v['Name']=='VPrintf'],'Destination diagnostic')
            lock=one([v for v in c if v['Name']=='Lock'],'Destination lock')
            examined=one([v for v in c if v['Name']=='Examine'],'Destination examine')
            require(diagnostic['Text']=='Destination "%s" is not a directory.\n' and
                    lock['Text']=='RAM:destination' and lock.get('ReturnedD0',0)!=0 and
                    examined['D1']==lock['ReturnedD0'] and examined.get('ReturnedD0',0)!=0 and
                    examined['DirectoryEntryType']<0,'Nondirectory branch')
            if candidate:
                end=search_ends[0]
                require(end['D1']==match['D2'] and 'ReturnedD0' in end and
                        c.index(diagnostic)<c.index(end),'Candidate search cleanup identity/order')
                fib=one([v for v in c if v['Name']=='AllocDosObject'],'Candidate FIB')
                free=one([v for v in c if v['Name']=='FreeDosObject'],'Candidate FIB cleanup')
                require(fib['D1']==free['D1']==2 and fib.get('ReturnedD0',0)!=0 and
                        free['D2']==fib['ReturnedD0'] and 'ReturnedD0' in free,'Candidate FIB identity')
        else:
            rename=one([v for v in c if v['Name']=='Rename'],'Recovery rename')
            end=one([v for v in c if v['Name']=='MatchEnd'],'Recovery search end')
            require(rename['Text']=='RAM:rn-one' and rename['DestinationText']=='RAM:recovered' and
                    rename.get('ReturnedD0',0)!=0 and c.index(end)<c.index(rename),'Recovery result')
    for name,payload in [('recovered',b'rename-first\n'),('rn-two',b'rename-second\n'),('destination',b'preserved-target\n')]:
        opened=one([v for v in e[ends[-1]:] if v['Name']=='Open' and v.get('Text')==name],'Readback')
        pos=e.index(opened);handle=opened.get('ReturnedD0',0);require(handle!=0,'Readback handle')
        closed=next(v for v in e[pos+1:] if v['Name']=='Close' and v['D1']==handle and v['Task']==opened['Task'])
        require([v.get('ReturnedD0') for v in e[pos+1:e.index(closed)] if v['Name']=='FGetC' and
                v['D1']==handle and v['Task']==opened['Task']]==list(payload) and closed.get('ReturnedD0',0)!=0,'Preserved data')
    removal=one([v for v in e if v['Name']=='RemSegment' and v.get('RemovedSegmentList')==rows[0]['Segment']],'Resident removal')
    require(removal.get('ReturnedD0',0)!=0 and e.index(removal)>ends[-1],'Resident removal result/order')
    return {'status':('candidate-rename-nondirectory-verified' if candidate else 'original-rename-nondirectory-reference-verified'),'returns':[0,0,0],
            'rejectionMatchEndCalls':1 if candidate else 0,'matcherLeakFreedomQualified':False,'replacementQualified':False,'fullContractQualified':False}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('observations',type=Path);p.add_argument('--output',type=Path,required=True)
    p.add_argument('--candidate',action='store_true',help='Require the documented added search cleanup and candidate FIB ownership.')
    args=p.parse_args();data=args.observations.read_bytes();result=verify(json.loads(data),candidate=args.candidate)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
