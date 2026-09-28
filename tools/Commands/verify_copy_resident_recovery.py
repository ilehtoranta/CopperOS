"""Check captured forced-PURE sequential recovery; not full resident qualification."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, failure_kind='parser'):
    require(failure_kind in ['parser', 'missing-source', 'missing-target-directory'], 'Unknown failure kind')
    require(r.get('scope') == 'CC13/disposable-Copy-derivative-boot-progress-only', 'Wrong scope')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged'), 'Prerequisites failed')
    require(not any(r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete observation')
    require(r.get('copyCpuImageWrites') == [], 'Missing/failed CPU write observation')
    rows = r['copyInvocationOwnership']
    require([x['ReturnCode'] for x in rows] == [0, 20, 0], 'Wrong recovery results')
    require([x['ParserFailed'] for x in rows] == [False, failure_kind == 'parser', False], 'Wrong parser outcomes')
    if failure_kind != 'parser':
        require([x.get('OpenFailed') for x in rows] == [False, True, False], 'Wrong open outcomes')
    require([x['Allocations'] for x in rows] == [2, 1, 2], 'Wrong allocations')
    require(all(x['Allocations'] == x['Frees'] and x['ImageUnchanged'] for x in rows), 'Ownership/image failure')
    require(len({x['Segment'] for x in rows}) == 1, 'Segment was not reused')
    e = r['observedDosCalls']; segment = rows[0]['Segment']
    load = one([x for x in e if x['Name'] == 'LoadSeg' and x['Text'] == 'C:Ed'], 'Copy load count')
    require(load.get('ReturnedD0') == segment, 'Loaded segment mismatch')
    runs = [x for x in e if x['Name'] == 'RunCommand' and x['D1'] == segment]
    require([x.get('ReturnedD0') for x in runs] == [0, 20, 0], 'RunCommand evidence mismatch')
    starts = [i for i, x in enumerate(e) if x['Name'] == 'CopyOpenLibrary']
    require(len(starts) == 3, 'Library lease count')
    a = starts[1]
    b = next(i for i in range(a + 1, len(e)) if e[i]['Name'] == 'CopyCloseLibrary')
    failure = e[a:b]
    parser = one([x for x in failure if x['Name'] == 'ReadArgs'], 'Failed parser count')
    if failure_kind == 'parser':
        require(parser.get('ReturnedD0') == 0 and parser.get('ReturnedIoErr') == 115, 'Wrong parse failure')
        require(not any(x['Name'] in ['FreeArgs', 'Open', 'Read', 'Write'] for x in failure), 'Failure did file work or freed failed parse')
    else:
        require(parser.get('ReturnedD0', 0) != 0, 'Expected successful parsing')
        opens = [x for x in failure if x['Name'] == 'Open']
        if failure_kind == 'missing-source':
            opened = one(opens, 'Failed invocation open count')
            require(opened['Text'] == 'RAM:copy-missing' and opened['D2'] == 1005 and opened.get('ReturnedD0') == 0 and opened.get('ReturnedIoErr') == 205, 'Wrong source failure')
            require(not any(x['Name'] == 'Close' for x in failure), 'Close after failed source open')
        else:
            require(len(opens) == 2, 'Destination failure open count')
            source, target = opens
            require(source['Text'] == 'RAM:copy-source' and source['D2'] == 1005 and source.get('ReturnedD0', 0) != 0, 'Source open failed')
            require(target['Text'] == 'RAM:missing-dir/copy-unused' and target['D2'] == 1006 and target.get('ReturnedD0') == 0 and target.get('ReturnedIoErr') == 205, 'Wrong target failure')
            close = one([x for x in failure if x['Name'] == 'Close'], 'Source close count')
            require(close['D1'] == source['ReturnedD0'] and close.get('ReturnedD0') == 0xffffffff, 'Source handle cleanup failed')
            require(failure.index(target) < failure.index(close), 'Premature source close')
        require(not any(x['Name'] in ['Read', 'Write', 'ExamineFH'] for x in failure), 'Transfer after failed open')
        free_args = one([x for x in failure if x['Name'] == 'FreeArgs'], 'Missing successful parse cleanup')
        require(free_args['D1'] == parser['ReturnedD0'] and 'ReturnedD0' in free_args, 'Wrong parser cleanup')
    alloc = one([x for x in failure if x['Name'] == 'AllocDosObject'], 'Failed invocation object count')
    free = one([x for x in failure if x['Name'] == 'FreeDosObject'], 'Failed invocation cleanup count')
    require(alloc.get('ReturnedD0', 0) != 0 and free['D2'] == alloc['ReturnedD0'] and 'ReturnedD0' in free, 'Failed invocation object leak')
    require(alloc['D1'] == free['D1'] == 5 and parser['D3'] == alloc['ReturnedD0'], 'RDArgs object identity/type')
    if failure_kind != 'parser':
        require(failure.index(parser) < failure.index(free_args) < failure.index(free), 'Wrong parser cleanup order')
        if failure_kind == 'missing-target-directory':
            require(failure.index(close) < failure.index(free_args), 'Late source cleanup')
    for name in ['copy-target', 'copy-target2']:
        opened = one([x for x in e if x['Name'] == 'Open' and x['Text'] == name], 'Readback open count')
        a = e.index(opened); handle = opened.get('ReturnedD0', 0)
        require(handle != 0, 'Readback open failed')
        close = next(x for x in e[a + 1:] if x['Name'] == 'Close' and x['D1'] == handle)
        chars = [x.get('ReturnedD0') for x in e[a + 1:e.index(close)] if x['Name'] == 'FGetC' and x['D1'] == handle]
        require(chars == list(b'copy-native-boot-payload\n') and close.get('ReturnedD0') == 0xffffffff, 'Wrong readback')
        fib = one([x for x in e[:a] if x['Name'] == 'MatchFirst' and bytes.fromhex(x.get('FileNameBytes', '')).split(b'\0')[0] == name.encode()], 'Missing size')
        require(fib.get('ReturnedD0') == 0 and fib.get('FileSize') == 25, 'Wrong destination size')
    removal = one([x for x in e if x['Name'] == 'RemSegment' and x.get('RemovedSegmentList') == segment], 'Removal mismatch')
    require(removal.get('ReturnedD0') == 0xffffffff, 'Removal failed')
    freed = one([x for x in e if x['Name'] == 'SegmentFreeMem'], 'Segment free count')
    require(freed['A1'] == r['copySegmentAllocationBase'] and freed['D0'] == r['copySegmentAllocationBytes'] and 'ReturnedD0' in freed, 'Segment free mismatch')
    require(e.index(removal) < e.index(freed), 'Premature free')
    return {'status': 'forced-pure-sequential-recovery-evidence-passed', 'returns': [0, 20, 0], 'failureKind': failure_kind,
            'concurrencyQualified': False, 'installedFlagsQualified': False, 'fullCommandQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path); p.add_argument('--output', type=Path, required=True)
    p.add_argument('--failure-kind', choices=['parser', 'missing-source', 'missing-target-directory'], default='parser')
    args = p.parse_args(); data = args.observations.read_bytes()
    result = verify(json.loads(data), args.failure_kind)
    result['observationsSha256'] = hashlib.sha256(data).hexdigest()
    result['verifierSha256'] = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
    args.output.write_text(json.dumps(result, indent=2) + '\n')
