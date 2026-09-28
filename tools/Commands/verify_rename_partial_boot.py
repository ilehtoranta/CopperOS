"""Check candidate Rename partial failure and persisted recovery on original DOS."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r):
    require(r.get('commandUnderTest') == 'Rename' and r.get('rootInfoReady') and r.get('diskBytesUnchanged')
            and not any(r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    rows = r['copyInvocationOwnership']
    require(len(rows) == 2 and [v['ReturnCode'] for v in rows] == [20, 0] and all(
        v['Segment'] == rows[0]['Segment'] and v['ImageUnchanged'] and v['Allocations'] == v['Frees'] == 4 for v in rows), 'Results/ownership')
    require(r.get('copyActiveInvocations') == 0 and r.get('copyCpuImageWrites') == [], 'Resident image lifetime')
    e = r['observedDosCalls']
    starts = [i for i,v in enumerate(e) if v['Name'] == 'CopyOpenLibrary']
    ends = [i for i,v in enumerate(e) if v['Name'] == 'CopyCloseLibrary']
    require(len(starts) == len(ends) == 2 and starts[0] < ends[0] < starts[1] < ends[1], 'Leases')
    for n,(a,b) in enumerate(zip(starts, ends)):
        c = e[a:b+1]
        renames = [v for v in c if v['Name'] == 'Rename']
        expected = [('RAM:first', 'Ram Disk:target/first'), ('RAM:second', 'Ram Disk:target/second')] if n == 0 else [('RAM:second', 'RAM:recovered')]
        require([(v['Text'],v['DestinationText']) for v in renames] == expected, 'Mutation paths/count')
        require(renames[0].get('ReturnedD0',0) != 0, 'First/recovery result')
        matches = [v for v in c if v['Name'] == 'MatchEnd']
        require(len(matches) == (3 if n == 0 else 1) and all('ReturnedD0' in v for v in matches), 'Search cleanup')
        parser = one([v for v in c if v['Name'] == 'ReadArgs'], 'Parser')
        free = one([v for v in c if v['Name'] == 'FreeArgs'], 'Parser release')
        require(parser.get('ReturnedD0',0) != 0 and free['D1'] == parser['ReturnedD0'] and 'ReturnedD0' in free, 'Parser ownership')
        faults = [v for v in c if v['Name'] == 'PrintFault']
        if n == 0:
            require(renames[1].get('ReturnedD0') == 0 and renames[1].get('ReturnedIoErr') == 203, 'Second mutation failure')
            diagnostic = one([v for v in c if v['Name'] == 'VPrintf'], 'Failure prefix')
            fault = one(faults, 'Failure fault')
            require(diagnostic['Text'] == "Can't rename %s as %s because " and fault['D1'] == 203 and fault['D2'] == 0 and
                    c.index(renames[1]) < c.index(diagnostic) < c.index(matches[-1]) < c.index(fault), 'Failure reporting order')
        else:
            require(not faults, 'Recovery fault')
    removal = one([v for v in e if v['Name'] == 'RemSegment' and v.get('RemovedSegmentList') == rows[0]['Segment']], 'Resident removal')
    require(removal.get('ReturnedD0',0) != 0 and e.index(removal) > ends[-1], 'Resident removal result')
    for path,name,payload in [('RAM:target/first','first',b'first-payload\n'), ('RAM:target/second','second',b'existing-payload\n'), ('RAM:recovered','recovered',b'second-payload\n')]:
        match = one([v for v in e[ends[-1]:] if v['Name'] == 'MatchFirst' and v.get('Text') == path], 'Readback path')
        opened = one([v for v in e[ends[-1]:] if v['Name'] == 'Open' and v.get('Text') == name], 'Readback open')
        a = e.index(opened); handle = opened.get('ReturnedD0',0)
        require(handle != 0 and e.index(removal) < e.index(match) < a, 'Readback ownership/order')
        close = next(v for v in e[a+1:] if v['Name'] == 'Close' and v['D1'] == handle and v['Task'] == opened['Task'])
        require([v.get('ReturnedD0') for v in e[a+1:e.index(close)] if v['Name'] == 'FGetC' and v['D1'] == handle and v['Task'] == opened['Task']] == list(payload)
                and close.get('ReturnedD0',0) != 0, 'Persisted payload')
    return {'status':'candidate-rename-partial-failure-readback-verified', 'returns':[20,0], 'fullContractQualified':False, 'shippingQualified':False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path); p.add_argument('--output', type=Path, required=True)
    args = p.parse_args(); data = args.observations.read_bytes(); result = verify(json.loads(data))
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(), verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
