"""Verify first-success/missing-source/later-source effects on original DOS."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, in_use=False):
    require(r.get('scope') == 'CC13/disposable-Copy-derivative-boot-progress-only' and r.get('rootInfoReady') and r.get('diskBytesUnchanged'), 'Scope/media')
    require(not any(r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    row = one(r['copyInvocationOwnership'], 'Invocation')
    require(row['ReturnCode'] == 20 and row['Allocations'] == row['Frees'] == 6 and row['ImageUnchanged'] and not row['ParserFailed'], 'Result/ownership')
    require(r.get('copyCpuImageWrites') == [] and r.get('copyActiveInvocations') == 0, 'Image/live ownership')
    e = r['observedDosCalls']
    load = one([v for v in e if v['Name'] == 'LoadSeg' and v['Text'] == 'C:Ed'], 'Load')
    require(load.get('ReturnedD0') == row['Segment'], 'Segment identity')
    run = one([v for v in e if v['Name'] == 'RunCommand' and v['D1'] == row['Segment']], 'Run')
    require(run.get('ReturnedD0') == 20, 'Native result')
    a = one([i for i,v in enumerate(e) if v['Name'] == 'CopyOpenLibrary'], 'Lease')
    b = next(i for i in range(a + 1, len(e)) if e[i]['Name'] == 'CopyCloseLibrary')
    calls = e[a:b]
    require([v['D0'] for v in calls if v['Name'] == 'CopyAllocMem'] == [4772, 25, 2330, 512, 2330, 2330], 'Allocation sequence')
    opens = [v for v in calls if v['Name'] == 'Open']
    require([v['Text'] for v in opens] == ['Ram Disk:partial/first', 'RAM:first'] and [v['D2'] for v in opens] == [1006, 1005] and all(v.get('ReturnedD0', 0) != 0 for v in opens), 'Only first transferred')
    payload = b'first-payload\n'
    read = one([v for v in calls if v['Name'] == 'Read'], 'Read')
    write = one([v for v in calls if v['Name'] == 'Write'], 'Write')
    require(read['D1'] == opens[1]['ReturnedD0'] and write['D1'] == opens[0]['ReturnedD0'] and read.get('ReturnedD0') == write.get('ReturnedD0') == len(payload) and read['ReadPayloadHex'] == write['PayloadHex'] == payload.hex().upper(), 'First payload')
    for opened in opens:
        closed = one([v for v in calls if v['Name'] == 'Close' and v['D1'] == opened['ReturnedD0']], 'Close')
        require(closed.get('ReturnedD0', 0) != 0 and calls.index(closed) > calls.index(write), 'Close order/result')
    error = 202 if in_use else 205
    failed = [v for v in calls[calls.index(write) + 1:] if v['Name'] == 'MatchFirst' and v.get('ReturnedD0') == error]
    require(len(failed) == 2 and all(v.get('ReturnedIoErr') == error for v in failed) and calls.index(failed[0]) > calls.index(write), 'Failure after success')
    faults = [v for v in calls if v['Name'] == 'PrintFault']
    require([v['D1'] for v in faults] == ([202, 0] if in_use else [205]), 'Diagnostic error sequence')
    fault = faults[0]
    require(fault['D1'] == error and 'ReturnedD0' in fault, 'Missing-source fault')
    later = [v for v in calls[calls.index(fault) + 1:] if v['Name'] == 'MatchFirst']
    require(len(later) == 2 and all(v.get('ReturnedD0') == 0 and bytes.fromhex(v.get('FileNameBytes', '')).split(b'\0')[0] == b'last' for v in later), 'Later source examination')
    types = [i for i in range(b, len(e)) if e[i]['Name'] == 'LoadSeg' and e[i]['Text'] == 'C:Type']
    require(len(types) == 2, 'Independent Type runs')
    first = e[types[0]:types[1]]
    opened = one([v for v in first if v['Name'] == 'Open'], 'Independent first open')
    require(opened['Text'] == 'first' and opened.get('ReturnedD0', 0) != 0, 'First exists')
    handle = opened['ReturnedD0']
    require([v.get('ReturnedD0') for v in first if v['Name'] == 'FGetC' and v['D1'] == handle] == list(payload), 'Independent first bytes')
    fib = one([v for v in first if v['Name'] == 'MatchFirst'], 'First size')
    require(fib.get('ReturnedD0') == 0 and fib['FileSize'] == len(payload), 'First size/result')
    end = next(i for i in range(types[1] + 1, len(e)) if e[i]['Name'] == 'LoadSeg')
    last = e[types[1]:end]
    require(not any(v['Name'] == 'Open' for v in last) and one([v for v in last if v['Name'] == 'MatchFirst'], 'Last absence').get('ReturnedD0') == 205 and one([v for v in last if v['Name'] == 'RunCommand'], 'Last Type result').get('ReturnedD0') == 10, 'Last destination absent')
    return {'status': 'partial-source-failure-effects-verified', 'returnCode': 20, 'sourceError': error,
            'firstOutputPreserved': True, 'laterSourceExamined': True, 'laterOutputAbsent': True,
            'fullCommandQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path); p.add_argument('--output', type=Path, required=True)
    p.add_argument('--in-use', action='store_true')
    args = p.parse_args(); data = args.observations.read_bytes()
    result = verify(json.loads(data), args.in_use)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(), verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
