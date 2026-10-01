"""Verify original-DOS directory rejection followed by FORCE and child readback."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r):
    require(r.get('commandUnderTest') == 'MakeLink' and
            r.get('scope') == 'CC12/disposable-MakeLink-derivative-boot-progress-only', 'Scope')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged') and not any(r.get(k)
            for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    rows = r['copyInvocationOwnership']
    require(len(rows) == 2 and [v['ReturnCode'] for v in rows] == [20, 0], 'Directory results')
    segment = rows[0]['Segment']
    require(all(v['Segment'] == segment and v['Task'] == rows[0]['Task'] and
                v['Allocations'] == v['Frees'] == 1 and v['ImageUnchanged'] for v in rows), 'Resident ownership')
    require(r.get('copyCpuImageWrites') == [] and r.get('copyActiveInvocations') == 0, 'Image/active calls')
    e = r['observedDosCalls']
    load = one([v for v in e if v['Name'] == 'LoadSeg' and v.get('ReturnedD0') == segment], 'Single load')
    runs = [v for v in e if v['Name'] == 'RunCommand' and v['D1'] == segment]
    require([v.get('ReturnedD0') for v in runs] == [20, 0], 'Real invocation results')
    starts = [i for i,v in enumerate(e) if v['Name'] == 'CopyOpenLibrary']
    ends = [i for i,v in enumerate(e) if v['Name'] == 'CopyCloseLibrary']
    require(len(starts) == len(ends) == 2, 'Library leases')
    for n,(a,b) in enumerate(zip(starts, ends)):
        require(e.index(load) < e.index(runs[n]) < a < b and
                (n == 1 or b < e.index(runs[1])), 'Sequential calls')
        c = e[a:b + 1]
        def call(name):
            return one([v for v in c if v['Name'] == name], name)
        require(e[a].get('ReturnedD0', 0) != 0 and e[b]['A1'] == e[a]['ReturnedD0']
                and 'ReturnedD0' in e[b], 'Library release')
        parser, lock, fib, examined = [call(name) for name in ['ReadArgs', 'Lock', 'AllocDosObject', 'Examine']]
        require(parser['Text'] == 'FROM/A,TO/A,HARD/S,FORCE/S' and parser.get('ReturnedD0', 0) != 0, 'Parser')
        require(lock['Text'] == 'RAM:link-dir' and lock['D2'] == 0xfffffffe
                and lock.get('ReturnedD0', 0) != 0, 'Directory lock')
        require(fib['D1'] == 2 and fib.get('ReturnedD0', 0) != 0 and
                examined['D1'] == lock['ReturnedD0'] and examined['D2'] == fib['ReturnedD0']
                and examined['D2'] % 4 == 0 and examined.get('ReturnedD0', 0) != 0
                and examined['DirectoryEntryType'] > 0, 'Directory FIB')
        require(not any(v['Name'] == 'PrintFault' for v in c), 'Unexpected fault')
        if n == 0:
            require(not any(v['Name'] == 'MakeLink' for v in c), 'Unforced directory reached handler')
            action = call('PutStr')
            require(action['Text'] == 'Hard-links to directories require the FORCE keyword\n'
                    and 'ReturnedD0' in action, 'FORCE diagnostic')
        else:
            action = call('MakeLink')
            require(action['Text'] == 'RAM:dir-alias' and action['D2'] == lock['ReturnedD0']
                    and action['D3'] == 0 and action.get('ReturnedD0', 0) != 0, 'Forced hard link')
        freed, unlock, args_free = [call(name) for name in ['FreeDosObject', 'UnLock', 'FreeArgs']]
        require(freed['D1'] == 2 and freed['D2'] == fib['ReturnedD0'] and
                unlock['D1'] == lock['ReturnedD0'] and args_free['D1'] == parser['ReturnedD0']
                and all('ReturnedD0' in v for v in [freed, unlock, args_free]), 'Cleanup identity')
        positions = [c.index(v) for v in [parser, lock, fib, examined, action, freed, unlock, args_free]]
        require(positions == sorted(positions), 'Cleanup ordering')
    removal = one([v for v in e if v['Name'] == 'RemSegment' and
                   v.get('RemovedSegmentList') == segment], 'Resident removal')
    freed = one([v for v in e if v['Name'] == 'SegmentFreeMem'], 'Segment free')
    require(removal.get('ReturnedD0', 0) != 0 and ends[-1] < e.index(removal) < e.index(freed)
            and freed['A1'] == r['copySegmentAllocationBase'] and
            freed['D0'] == r['copySegmentAllocationBytes'] and 'ReturnedD0' in freed, 'Segment cleanup')
    opened = one([v for v in e if v['Name'] == 'Open' and v.get('Text') == 'payload'], 'Child readback')
    pos = e.index(opened); handle = opened.get('ReturnedD0', 0)
    require(pos > e.index(freed) and handle != 0 and opened['D2'] == 1005, 'Child open')
    match = one([v for v in e[e.index(freed):pos] if v['Name'] == 'MatchFirst'], 'Alias traversal')
    require(match.get('Text') == 'RAM:dir-alias/payload', 'Alias traversal path')
    require(match.get('ReturnedD0') == 0 and match.get('FileSize') == 18 and
            bytes.fromhex(match.get('FileNameBytes', '')).split(b'\0')[0] == b'payload', 'Child size')
    closed = next(v for v in e[pos + 1:] if v['Name'] == 'Close' and
                  v['D1'] == handle and v['Task'] == opened['Task'])
    chars = [v.get('ReturnedD0') for v in e[pos + 1:e.index(closed)] if
             v['Name'] == 'FGetC' and v['D1'] == handle and v['Task'] == opened['Task']]
    require(chars == list(b'hard-link-payload\n') and closed.get('ReturnedD0', 0) != 0, 'Child bytes')
    return {'status': 'original-dos-directory-force-verified', 'returns': [20, 0],
            'childBytes': 18, 'fullCommandQualified': False, 'installedFlagsQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path)
    p.add_argument('--output', type=Path, required=True)
    args = p.parse_args(); data = args.observations.read_bytes()
    result = verify(json.loads(data))
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),
                  verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
