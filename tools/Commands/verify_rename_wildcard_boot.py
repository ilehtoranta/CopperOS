"""Verify candidate Rename with original-DOS positional wildcard and AS alias."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, reference=False):
    require(r.get('commandUnderTest')=='Rename' and r.get('rootInfoReady') and r.get('diskBytesUnchanged') and
            not any(r.get(k) for k in ['failure','boundedStop','dosObservationOverflow','returnObservationCollision']),'Incomplete trace')
    rows=r['copyInvocationOwnership']; e=r['observedDosCalls']
    require(len(rows)==2 and all(v['ReturnCode']==0 and v['ImageUnchanged'] and v['Allocations']==v['Frees']==4 and
            v['Segment']==rows[0]['Segment'] for v in rows),'Results/ownership')
    require(r.get('copyActiveInvocations')==0 and r.get('copyCpuImageWrites')==[],'Resident lifetime')
    starts=[i for i,v in enumerate(e) if v['Name']=='CopyOpenLibrary']; ends=[i for i,v in enumerate(e) if v['Name']=='CopyCloseLibrary']
    require(len(starts)==len(ends)==2 and starts[0]<ends[0]<starts[1]<ends[1],'Sequential leases')
    for n,(a,b) in enumerate(zip(starts,ends)):
        c=e[a:b+1]; parser=one([v for v in c if v['Name']=='ReadArgs'],'Parser'); free=one([v for v in c if v['Name']=='FreeArgs'],'Parser release')
        require(parser['Text']=='FROM/A/M,TO=AS/A,QUIET/S' and parser.get('ReturnedD0',0)!=0 and
                free['D1']==parser['ReturnedD0'] and 'ReturnedD0' in free,'Parser identity')
        require(not any(v['Name'] in ['PrintFault','VPrintf'] for v in c),'Unexpected diagnostic')
        renames=[v for v in c if v['Name']=='Rename']; first=[v for v in c if v['Name']=='MatchFirst']; nexts=[v for v in c if v['Name']=='MatchNext']; matches=[v for v in c if v['Name']=='MatchEnd']
        require(all(v.get('ReturnedD0',0)!=0 for v in renames) and all('ReturnedD0' in v for v in matches),'Mutation/search completion')
        if n==0:
            require([v['Text'] for v in first]==['RAM:wild-#?']*2 and len(matches)==2 and
                    [v.get('ReturnedD0') for v in nexts]==[0,232],'Single wildcard multi-match')
            require(sorted((v['Text'],v['DestinationText']) for v in renames)==[(f'RAM:wild-{s}',f'Ram Disk:target/wild-{s}') for s in ['one','two']],'Wildcard destinations')
            require(all(c.index(nexts[k])<c.index(renames[k]) for k in range(2)) and c.index(renames[-1])<c.index(matches[-1]),'Advance/mutation order')
        else:
            rename=one(renames,'Alias rename')
            require(rename['Text']=='RAM:target/wild-one' and rename['DestinationText']=='RAM:renamed' and len(matches)==1 and not nexts,'AS alias effect')
    removal=one([v for v in e if v['Name']=='RemSegment' and v.get('RemovedSegmentList')==rows[0]['Segment']],'Resident removal')
    require(removal.get('ReturnedD0',0)!=0 and e.index(removal)>ends[-1],'Removal result')
    for path,name,payload in [('RAM:renamed','renamed',b'wildcard-one\n'),('RAM:target/wild-two','wild-two',b'wildcard-two\n')]:
        tail=e[e.index(removal)+1:]
        match=one([v for v in tail if v['Name']=='MatchFirst' and v.get('Text')==path],'Readback path')
        opened=one([v for v in tail if v['Name']=='Open' and v.get('Text')==name],'Readback open'); pos=e.index(opened); handle=opened.get('ReturnedD0',0)
        require(handle!=0 and e.index(match)<pos,'Readback result')
        close=next(v for v in e[pos+1:] if v['Name']=='Close' and v['D1']==handle and v['Task']==opened['Task'])
        require([v.get('ReturnedD0') for v in e[pos+1:e.index(close)] if v['Name']=='FGetC' and v['D1']==handle and v['Task']==opened['Task']]==list(payload)
                and close.get('ReturnedD0',0)!=0,'Readback bytes')
    return {'status':('reference-rename-wildcard-as-original-dos-verified' if reference else 'candidate-rename-wildcard-as-original-dos-verified'),'returns':[0,0],'fullContractQualified':False,'shippingQualified':False}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__); p.add_argument('observations',type=Path); p.add_argument('--output',type=Path,required=True)
    p.add_argument('--reference',action='store_true',help='Label original-reference evidence separately from the candidate.')
    args=p.parse_args(); data=args.observations.read_bytes(); result=verify(json.loads(data),reference=args.reference)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
