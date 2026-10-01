"""Verify five Workbench MakeLink candidate cases on original DOS."""
import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r):
    require(r.get('commandUnderTest') == 'MakeLink' and r.get('rootInfoReady') and
            r.get('diskBytesUnchanged') and not any(r.get(k) for k in
            ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    rows = r['copyInvocationOwnership']; expected = [20, 0, 20, 20, 0]
    require(len(rows) == 5 and [v['ReturnCode'] for v in rows] == expected, 'Profile results')
    segment = rows[0]['Segment']
    require(all(v['Segment'] == segment and v['Task'] == rows[0]['Task'] and
                v['Allocations'] == v['Frees'] == 1 and v['ImageUnchanged'] for v in rows), 'Resident ownership')
    require(r.get('copyCpuImageWrites') == [] and r.get('copyActiveInvocations') == 0, 'Image lifetime')
    e = r['observedDosCalls']
    starts = [i for i,v in enumerate(e) if v['Name'] == 'CopyOpenLibrary']
    ends = [i for i,v in enumerate(e) if v['Name'] == 'CopyCloseLibrary']
    require(len(starts) == len(ends) == 5, 'Library lease count')
    runs = [v for v in e if v['Name'] == 'RunCommand' and v['D1'] == segment]
    require([v.get('ReturnedD0') for v in runs] == expected, 'Real command returns')
    for n,(a,b) in enumerate(zip(starts, ends)):
        require(e.index(runs[n]) < a < b and (n == 4 or b < e.index(runs[n+1])), 'Lease order')
        c = e[a:b+1]
        require(e[a].get('ReturnedD0', 0) != 0 and e[b]['A1'] == e[a]['ReturnedD0'] and
                'ReturnedD0' in e[b], 'DOS release')
        parser = one([v for v in c if v['Name'] == 'ReadArgs'], 'Parser')
        free = one([v for v in c if v['Name'] == 'FreeArgs'], 'Parser free')
        require(parser['Text'] == 'FROM/A,TO/A,HARD/S,FORCE/S' and parser.get('ReturnedD0', 0) != 0
                and free['D1'] == parser['ReturnedD0'] and 'ReturnedD0' in free, 'Parser identity')
        fib = one([v for v in c if v['Name'] == 'AllocDosObject'], 'FIB')
        fib_free = one([v for v in c if v['Name'] == 'FreeDosObject'], 'FIB free')
        examined = one([v for v in c if v['Name'] == 'Examine'], 'Examine')
        require(fib['D1'] == fib_free['D1'] == 2 and fib.get('ReturnedD0', 0) != 0 and
                fib_free['D2'] == examined['D2'] == fib['ReturnedD0'] and examined['D2'] % 4 == 0
                and examined.get('ReturnedD0', 0) != 0, 'FIB identity')
        locks = [v for v in c if v['Name'] in ['Lock', 'ParentDir'] and v.get('ReturnedD0', 0) != 0]
        unlocks = [v for v in c if v['Name'] == 'UnLock']
        require(Counter(v['ReturnedD0'] for v in locks) == Counter(v['D1'] for v in unlocks)
                and all('ReturnedD0' in v and c.index(v) < c.index(free) for v in unlocks), 'Lock cleanup')
        require(locks[0]['Text'] == ('RAM:link-dir/payload' if n == 4 else 'RAM:link-dir')
                and examined['D1'] == locks[0]['ReturnedD0'], 'Target identity')
        links = [v for v in c if v['Name'] == 'MakeLink']
        faults = [v for v in c if v['Name'] == 'PrintFault']
        if n in [1, 4]:
            link = one(links, 'Hard link')
            require(link['Text'] == ('RAM:dir-alias' if n == 1 else 'RAM:default-alias') and
                    link['D2'] == locks[0]['ReturnedD0'] and link['D3'] == 0 and
                    link.get('ReturnedD0', 0) != 0 and not faults, 'Hard-link result')
        else:
            require(not links, 'Rejected case reached MakeLink')
            fault = one(faults, 'Failure report')
            require(fault['D1'] == (205 if n == 3 else 0) and 'ReturnedD0' in fault, 'Failure error')
            if n in [0, 2]:
                diagnostic = one([v for v in c if v['Name'] == 'VPrintf'], 'Diagnostic')
                require(diagnostic['Text'] == ('Links to directories require use of the FORCE keyword\n'
                        if n == 0 else 'Link loop from %s to %s not allowed\n'), 'Workbench diagnostic')
            else:
                failed = one([v for v in c if v['Name'] == 'Lock' and v.get('ReturnedD0') == 0], 'Parent rejection')
                require(failed['Text'] == 'RAM:missing' and failed.get('ReturnedIoErr') == 205, 'Parent error')
    removal = one([v for v in e if v['Name'] == 'RemSegment' and v.get('RemovedSegmentList') == segment], 'Removal')
    freed = one([v for v in e if v['Name'] == 'SegmentFreeMem'], 'Segment free')
    require(removal.get('ReturnedD0', 0) != 0 and ends[-1] < e.index(removal) < e.index(freed) and
            freed['A1'] == r['copySegmentAllocationBase'] and freed['D0'] == r['copySegmentAllocationBytes']
            and 'ReturnedD0' in freed, 'Segment lifetime')
    for path,name in [('RAM:dir-alias/payload','payload'), ('RAM:default-alias','default-alias')]:
        match = one([v for v in e if v['Name'] == 'MatchFirst' and v.get('Text') == path], 'Readback path')
        opened = one([v for v in e if v['Name'] == 'Open' and v.get('Text') == name], 'Readback open')
        pos = e.index(opened); handle = opened.get('ReturnedD0', 0)
        require(e.index(freed) < e.index(match) < pos and match.get('ReturnedD0') == 0 and
                match.get('FileSize') == 18 and handle != 0, 'Readback result')
        close = next(v for v in e[pos+1:] if v['Name'] == 'Close' and v['D1'] == handle and v['Task'] == opened['Task'])
        chars = [v.get('ReturnedD0') for v in e[pos+1:e.index(close)] if v['Name'] == 'FGetC' and
                 v['D1'] == handle and v['Task'] == opened['Task']]
        require(chars == list(b'hard-link-payload\n') and close.get('ReturnedD0', 0) != 0, 'Readback bytes')
    return {'status': 'workbench-candidate-five-case-original-dos-verified', 'returns': expected,
            'fullCommandQualified': False, 'installedFlagsQualified': False, 'concurrencyQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path); p.add_argument('--output', type=Path, required=True)
    args = p.parse_args(); data = args.observations.read_bytes()
    result = verify(json.loads(data))
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(), verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
