"""Compare the bounded wildcard/AS scenarios with hash-bound original media."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_rename_wildcard_boot import verify
from verify_copy_boot_transfer import require


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def compare(reference, candidate, reference_media, candidate_media):
    rows=[]
    for obs,media,is_reference in [(reference,reference_media,True),(candidate,candidate_media,False)]:
        r=json.loads(obs.read_text()); m=json.loads(media.read_text())
        verify(r,reference=is_reference)
        require(r['fixtureReceiptSha256'].lower()==digest(media) and r['fixtureImageSha256'].lower()==m['output_adf_sha256'],'Fixture binding')
        require(m['binary_role']==('workbench31-reference' if is_reference else 'workbench31-candidate'),'Binary role')
        binary=m['replacements'][0]
        require(binary['guest_path']=='c/ed','Replacement path')
        if is_reference:
            require(binary['sha256']=='ebff9d5f1401ca67fe9542bde304c603d21366f81852716c236c5c7ca1e49579','Original identity')
        else:
            require(digest(Path(binary['local_file']))==binary['sha256'],'Candidate identity')
        rows.append((r,m))
    a,b=rows
    require(a[1]['probe_sha256']==b[1]['probe_sha256'] and a[1]['reference_adf_sha256']==b[1]['reference_adf_sha256'] and
            a[0]['romSha256']==b[0]['romSha256'] and a[0]['testAssemblySha256']==b[0]['testAssemblySha256'],'Different scenario/runtime')
    return {'status':'rename-wildcard-as-bounded-comparison-passed',
            'scope':'Same startup, original DOS/handler: parser success, two wildcard effects, AS effect, primary returns, silent calls and exact destination readback. Not complete call-trace, final IoErr or full-profile parity.',
            'referenceObservationsSha256':digest(reference),'candidateObservationsSha256':digest(candidate),
            'referenceReceiptSha256':digest(reference_media),'candidateReceiptSha256':digest(candidate_media),
            'fullContractQualified':False,'shippingQualified':False}


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['reference','candidate','reference_media','candidate_media']:
        p.add_argument(name,type=Path)
    p.add_argument('--output',type=Path,required=True); a=p.parse_args()
    result=compare(a.reference,a.candidate,a.reference_media,a.candidate_media)
    result['verifierSha256']=digest(Path(__file__))
    a.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
