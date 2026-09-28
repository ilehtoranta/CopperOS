"""Verify observed same-segment overlapping transfers, not full qualification."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, payload, failure=False, readback=False):
    require(r.get('scope') == 'CC13/disposable-Copy-derivative-boot-progress-only', 'Wrong scope')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged'), 'Boot/media prerequisite')
    require(not any(r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    require(r.get('copyMaximumActiveInvocations') == 2 and r.get('copyActiveInvocations') == 0, 'No completed overlap')
    require(r.get('copyCpuImageWrites') == [], 'Missing/failed image write observation')
    rows = r['copyInvocationOwnership']
    require(len(rows) == 2 and len({x['Task'] for x in rows}) == 2, 'Distinct tasks required')
    require(len({x['Segment'] for x in rows}) == 1, 'Different segments')
    require(sorted(x['ReturnCode'] for x in rows) == ([0, 20] if failure else [0, 0]), 'Invocation results')
    require(all(x['Allocations'] == x['Frees'] == (1 if x['ReturnCode'] else 2) and x['ImageUnchanged'] and not x['ParserFailed'] and x['OpenFailed'] == bool(x['ReturnCode']) for x in rows), 'Invocation ownership/results')
    events = r['observedDosCalls']
    segment = rows[0]['Segment']
    load = one([e for e in events if e['Name'] == 'LoadSeg' and e['Text'] == 'C:Ed'], 'Load count')
    require(load.get('ReturnedD0') == segment, 'Load identity')
    targets = set()
    for row in rows:
        task_events = [e for e in events if e['Task'] == row['Task']]
        run = one([e for e in task_events if e['Name'] == 'RunCommand' and e['D1'] == segment], 'Run identity')
        require(run.get('ReturnedD0') == row['ReturnCode'], 'Run result mismatch')
        start = one([e for e in task_events if e['Name'] == 'CopyOpenLibrary'], 'Library open count')
        end = one([e for e in task_events if e['Name'] == 'CopyCloseLibrary'], 'Library close count')
        a, b = task_events.index(start), task_events.index(end)
        require(task_events.index(run) < a < b and 'ReturnedD0' in end, 'Invocation order')
        calls = task_events[a:b]
        parser = one([e for e in calls if e['Name'] == 'ReadArgs'], 'Parser count')
        free_args = one([e for e in calls if e['Name'] == 'FreeArgs'], 'Parser cleanup count')
        alloc = one([e for e in calls if e['Name'] == 'AllocDosObject'], 'RDArgs allocation count')
        free = one([e for e in calls if e['Name'] == 'FreeDosObject'], 'RDArgs free count')
        require(alloc.get('ReturnedD0', 0) != 0 and alloc['ReturnedD0'] == parser['D3'] == parser.get('ReturnedD0') == free_args['D1'] == free['D2'], 'Parser ownership')
        require(alloc['D1'] == free['D1'] == 5 and 'ReturnedD0' in free_args and 'ReturnedD0' in free, 'Parser cleanup return/type')
        require(calls.index(alloc) < calls.index(parser) < calls.index(free_args) < calls.index(free), 'Parser lifecycle order')
        source = one([e for e in calls if e['Name'] == 'Open' and e['D2'] == 1005], 'Source open count')
        target = one([e for e in calls if e['Name'] == 'Open' and e['D2'] == 1006], 'Target open count')
        require(source['Text'] == 'C:Ed', 'Wrong input')
        targets.add(target['Text'])
        if row['ReturnCode'] != 0:
            require(source.get('ReturnedD0', 0) != 0 and target.get('ReturnedD0') == 0 and target.get('ReturnedIoErr') == 205, 'Wrong open failure')
            require(not any(e['Name'] in ['Read', 'Write', 'ExamineFH'] for e in calls), 'Failed invocation transferred')
            close = one([e for e in calls if e['Name'] == 'Close'], 'Failed invocation source close')
            require(close['D1'] == source['ReturnedD0'] and close.get('ReturnedD0', 0) != 0, 'Failed invocation leaked source')
            require(calls.index(source) < calls.index(target) < calls.index(close) < calls.index(free_args), 'Failed invocation close order')
            continue
        require(source.get('ReturnedD0', 0) != 0 and target.get('ReturnedD0', 0) != 0 and source['ReturnedD0'] != target['ReturnedD0'], 'Invalid handles')
        reads = [e for e in calls if e['Name'] == 'Read']
        writes = [e for e in calls if e['Name'] == 'Write']
        require(reads and len(reads) == len(writes), 'Transfer count')
        for read, write in zip(reads, writes):
            require(read['D1'] == source['ReturnedD0'] and write['D1'] == target['ReturnedD0'], 'Crossed handles')
            require(read.get('ReturnedD0') == write['D3'] == write.get('ReturnedD0'), 'Short/failed transfer')
            require(read.get('ReadPayloadHex') == write.get('PayloadHex'), 'Crossed payload')
            require(calls.index(read) < calls.index(write), 'Transfer ordering')
        require(b''.join(bytes.fromhex(e['PayloadHex']) for e in writes) == payload, 'Wrong complete payload')
        closes = [e for e in calls if e['Name'] == 'Close']
        require(len(closes) == 2, 'Close count')
        for opened in [source, target]:
            close = one([e for e in closes if e['D1'] == opened['ReturnedD0']], 'Handle close identity')
            # DOS BOOL success is nonzero; the original FFS END path may
            # forward FreeMem's rounded span (e.g. 32), not canonical -1.
            require(close.get('ReturnedD0', 0) != 0 and calls.index(close) > calls.index(writes[-1]), 'Close failure/order')
    require(targets == {'RAM:copy-parallel', 'RAM:missing-dir/copy-foreground' if failure else 'RAM:copy-foreground'}, 'Distinct destinations')
    if readback:
        require(not failure, 'Readback profile requires two successes')
        for name in ['copy-parallel', 'copy-foreground']:
            opened = one([e for e in events if e['Name'] == 'Open' and e['Text'] == name], 'Readback open count')
            require(opened['D2'] == 1005 and opened.get('ReturnedD0', 0) != 0, 'Readback open failed')
            a = events.index(opened)
            close = next(e for e in events[a + 1:] if e['Name'] == 'Close' and e['Task'] == opened['Task'] and e['D1'] == opened['ReturnedD0'])
            require(close.get('ReturnedD0', 0) != 0 and close.get('IndependentReadbackBytes') == len(payload) and close.get('IndependentReadbackSha256', '').lower() == hashlib.sha256(payload).hexdigest(), 'Independent readback mismatch')
            fib = one([e for e in events[:a] if e['Name'] == 'MatchFirst' and bytes.fromhex(e.get('FileNameBytes', '')).split(b'\0')[0] == name.encode()], 'Readback file size record')
            require(fib.get('ReturnedD0') == 0 and fib.get('FileSize') == len(payload), 'Readback file size mismatch')
        removal = one([e for e in events if e['Name'] == 'RemSegment' and e.get('RemovedSegmentList') == segment], 'Resident removal')
        freed = one([e for e in events if e['Name'] == 'SegmentFreeMem'], 'Resident segment free')
        require(removal.get('ReturnedD0', 0) != 0 and events.index(removal) < events.index(freed), 'Removal failed/order')
        require(freed['A1'] == r['copySegmentAllocationBase'] and freed['D0'] == r['copySegmentAllocationBytes'] and 'ReturnedD0' in freed, 'Resident allocation release')
    return {'status': 'observed-overlapping-transfer-passed', 'tasks': [x['Task'] for x in rows], 'segment': segment,
            'bytesPerSuccessfulTask': len(payload), 'overlappingTargetFailure': failure, 'independentDestinationReadbackQualified': readback,
            'residentRemovalQualified': readback, 'fullCommandQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path)
    p.add_argument('payload', type=Path)
    p.add_argument('--output', required=True, type=Path)
    p.add_argument('--target-failure', action='store_true')
    p.add_argument('--readback', action='store_true')
    args = p.parse_args()
    data, payload = args.observations.read_bytes(), args.payload.read_bytes()
    result = verify(json.loads(data), payload, args.target_failure, args.readback)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(), payloadSha256=hashlib.sha256(payload).hexdigest(), verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n')
