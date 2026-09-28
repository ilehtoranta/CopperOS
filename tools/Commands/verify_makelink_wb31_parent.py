"""Verify bounded original Workbench MakeLink missing-parent reference behavior."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, receipt_bytes):
    receipt = json.loads(receipt_bytes)
    require(r.get('fixtureReceiptSha256', '').lower() == hashlib.sha256(receipt_bytes).hexdigest(), 'Receipt binding')
    replacement = one(receipt['replacements'], 'Reference replacement')
    require(replacement['sha256'].lower() == 'c24b0af713e6f3a252d964c4545da29535bf6652394879a7dc4c57fdce7c02de'
            and receipt.get('binary_role') == 'original-workbench31-reference', 'Reference identity')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged') and not any(r.get(k) for k in
            ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Trace incomplete')
    rows = r['copyInvocationOwnership']
    require(len(rows) == 2 and [v['ReturnCode'] for v in rows] == [20, 0]
            and all(v['Segment'] == rows[0]['Segment'] and v['Allocations'] == v['Frees'] == 0
                    and v['ImageUnchanged'] for v in rows), 'Reference recovery')
    require(r.get('copyCpuImageWrites') == [] and r.get('copyActiveInvocations') == 0, 'Reference lifetime')
    e = r['observedDosCalls']
    starts = [i for i,v in enumerate(e) if v['Name'] == 'CopyOpenLibrary']
    ends = [i for i,v in enumerate(e) if v['Name'] == 'CopyCloseLibrary']
    require(len(starts) == len(ends) == 2 and starts[0] < ends[0] < starts[1] < ends[1], 'Leases')
    c = e[starts[0]:ends[0]]
    parser = one([v for v in c if v['Name'] == 'ReadArgs'], 'Parser')
    require(parser.get('ReturnedD0', 0) != 0 and parser['Text'] == 'FROM/A,TO/A,HARD/S,FORCE/S', 'Real arguments')
    locks = [v for v in c if v['Name'] == 'Lock']
    require(len(locks) == 2 and locks[0]['Text'] == 'RAM:link-dir' and locks[0].get('ReturnedD0', 0) != 0
            and locks[1]['Text'] == 'RAM:missing' and locks[1].get('ReturnedD0') == 0
            and locks[1].get('ReturnedIoErr') == 205, 'Parent failure')
    require(not any(v['Name'] == 'MakeLink' for v in c), 'Link attempted after parent failure')
    fault = one([v for v in c if v['Name'] == 'PrintFault'], 'Fault')
    freed = one([v for v in c if v['Name'] == 'FreeArgs'], 'Parser cleanup')
    require(fault['D1'] == 205 and 'ReturnedD0' in fault and freed['D1'] == parser['ReturnedD0']
            and 'ReturnedD0' in freed and c.index(locks[1]) < c.index(fault) < c.index(freed), 'Failure cleanup/report')
    c = e[starts[1]:ends[1]]
    link = one([v for v in c if v['Name'] == 'MakeLink'], 'Recovery link')
    require(link['Text'] == 'RAM:dir-alias' and link['D3'] == 0 and link.get('ReturnedD0', 0) != 0, 'Recovery result')
    return {'status': 'workbench31-missing-parent-reference-verified', 'returns': [20, 0],
            'parentError': 205, 'fullProfileParity': False, 'candidateQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path); p.add_argument('receipt', type=Path)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args(); data = args.observations.read_bytes()
    result = verify(json.loads(data), args.receipt.read_bytes())
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),
                  verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
