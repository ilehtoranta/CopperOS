"""Verify original-DOS MakeLink success/duplicate/success resident recovery."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, source_deleted=False):
    require(r.get('scope') == 'CC12/disposable-MakeLink-derivative-boot-progress-only'
            and r.get('commandUnderTest') == 'MakeLink', 'Command scope')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged') and not any(
        r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow',
                           'returnObservationCollision']), 'Incomplete trace')
    rows = r['copyInvocationOwnership']
    require(len(rows) == 3 and [v['ReturnCode'] for v in rows] == [0, 20, 0], 'Recovery results')
    segment = rows[0]['Segment']
    require(all(v['Segment'] == segment and v['Task'] == rows[0]['Task']
                and v['Allocations'] == v['Frees'] == 1 and v['ImageUnchanged']
                and not v['ParserFailed'] and not v['OpenFailed'] for v in rows), 'Resident ownership')
    require(r.get('copyCpuImageWrites') == [] and r.get('copyActiveInvocations') == 0
            and r.get('copyMaximumActiveInvocations') == 1, 'Shared image lifetime')
    e = r['observedDosCalls']
    load = one([v for v in e if v['Name'] == 'LoadSeg' and v.get('ReturnedD0') == segment], 'Single load')
    runs = [v for v in e if v['Name'] == 'RunCommand' and v['D1'] == segment]
    require([v.get('ReturnedD0') for v in runs] == [0, 20, 0]
            and all(v['Task'] == rows[0]['Task'] for v in runs), 'Real RunCommand results')
    starts = [i for i, v in enumerate(e) if v['Name'] == 'CopyOpenLibrary']
    ends = [i for i, v in enumerate(e) if v['Name'] == 'CopyCloseLibrary']
    require(len(starts) == len(ends) == 3, 'Library lease count')
    for n, (a, b) in enumerate(zip(starts, ends)):
        require(e.index(load) < e.index(runs[n]) < a < b
                and (n == 2 or b < e.index(runs[n + 1])), 'Sequential lease ordering')
        calls = e[a:b + 1]
        require(all(v['Task'] == rows[0]['Task'] for v in calls), 'Lease task')
        require(e[a].get('ReturnedD0', 0) != 0 and e[b]['A1'] == e[a]['ReturnedD0']
                and 'ReturnedD0' in e[b], 'Library cleanup')
        def call(name):
            return one([v for v in calls if v['Name'] == name], name)
        parser, lock, fib, examined, link = [call(name) for name in
            ['ReadArgs', 'Lock', 'AllocDosObject', 'Examine', 'MakeLink']]
        require(parser['Text'] == 'FROM/A,TO/A,HARD/S,FORCE/S'
                and parser.get('ReturnedD0', 0) != 0, 'Real parser')
        require(lock['Text'] == 'RAM:link-source' and lock['D2'] == 0xfffffffe
                and lock.get('ReturnedD0', 0) != 0, 'Source lock')
        require(fib['D1'] == 2 and fib.get('ReturnedD0', 0) != 0
                and examined['D2'] == fib['ReturnedD0'] and examined['D2'] % 4 == 0
                and examined['D1'] == lock['ReturnedD0']
                and examined.get('ReturnedD0', 0) != 0 and examined['DirectoryEntryType'] < 0, 'File examination')
        require(link['Text'] == ('RAM:link-alias2' if n == 2 else 'RAM:link-alias')
                and link['D2'] == lock['ReturnedD0'] and link['D3'] == 0, 'Hard-link arguments')
        faults = [v for v in calls if v['Name'] == 'PrintFault']
        if n == 1:
            require(link.get('ReturnedD0') == 0 and link.get('ReturnedIoErr') == 203
                    and runs[n].get('ReturnedIoErr') == 203, 'Duplicate-name failure')
            fault = one(faults, 'Duplicate diagnostic')
            require(fault['D1'] == 203 and 'ReturnedD0' in fault, 'Diagnostic error')
        else:
            require(link.get('ReturnedD0', 0) != 0 and not faults, 'Successful link')
        freed, unlock, args_free = [call(name) for name in ['FreeDosObject', 'UnLock', 'FreeArgs']]
        require(freed['D1'] == 2 and freed['D2'] == fib['ReturnedD0']
                and unlock['D1'] == lock['ReturnedD0'] and args_free['D1'] == parser['ReturnedD0']
                and all('ReturnedD0' in v for v in [freed, unlock, args_free]), 'Cleanup identities')
        ordered = [parser, lock, fib, examined, link] + (faults if n == 1 else []) + [freed, unlock, args_free]
        positions = [calls.index(v) for v in ordered]
        require(positions == sorted(positions), 'Cleanup ordering')
    removal = one([v for v in e if v['Name'] == 'RemSegment'
                   and v.get('RemovedSegmentList') == segment], 'Resident removal')
    require(removal.get('ReturnedD0', 0) != 0 and e.index(removal) > ends[-1], 'Removal result/order')
    freed = one([v for v in e if v['Name'] == 'SegmentFreeMem'], 'Segment free')
    require(freed['A1'] == r['copySegmentAllocationBase']
            and freed['D0'] == r['copySegmentAllocationBytes'] and 'ReturnedD0' in freed
            and e.index(freed) > e.index(removal), 'Segment cleanup')
    payload = b'hard-link-payload\n'
    if source_deleted:
        deleted = one([v for v in e if v['Name'] == 'DeleteFile'
                       and v.get('Text') in ['RAM:link-source', 'link-source']], 'Original filename deletion')
        require(deleted.get('ReturnedD0', 0) != 0 and e.index(deleted) > e.index(freed),
                'Source deletion result/order')
    for name in ['link-alias', 'link-alias2']:
        opened = one([v for v in e if v['Name'] == 'Open' and v['Text'] == name], 'Independent readback')
        pos = e.index(opened)
        handle = opened.get('ReturnedD0', 0)
        require(pos > e.index(freed) and handle != 0 and opened['D2'] == 1005, 'Readback open')
        if source_deleted:
            require(e.index(deleted) < pos, 'Alias read before source deletion')
        closed = next(v for v in e[pos + 1:] if v['Name'] == 'Close'
                      and v['D1'] == handle and v['Task'] == opened['Task'])
        chars = [v.get('ReturnedD0') for v in e[pos + 1:e.index(closed)]
                 if v['Name'] == 'FGetC' and v['D1'] == handle and v['Task'] == opened['Task']]
        require(chars == list(payload) and closed.get('ReturnedD0', 0) != 0, 'Alias contents')
        matched = one([v for v in e[e.index(freed):pos] if v['Name'] == 'MatchFirst'
                       and bytes.fromhex(v.get('FileNameBytes', '')).split(b'\0')[0] == name.encode()], 'Alias size')
        require(matched.get('ReturnedD0') == 0 and matched['FileSize'] == len(payload), 'Alias size/result')
    return {'status': 'original-dos-makelink-resident-recovery-verified', 'returns': [0, 20, 0],
            'aliasesSurviveSourceDeletion': source_deleted,
            'duplicateError': 203, 'aliasBytes': [18, 18], 'fullCommandQualified': False,
            'installedFlagsQualified': False, 'concurrencyQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path)
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--source-deleted', action='store_true')
    args = p.parse_args()
    data = args.observations.read_bytes()
    result = verify(json.loads(data), args.source_deleted)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),
                  verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
