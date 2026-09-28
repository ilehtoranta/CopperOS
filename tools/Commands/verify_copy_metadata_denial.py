"""Record denied metadata writes and resident recovery, not shipping parity."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one
from verify_copy_metadata_transfer import cstring


def verify(r, visible_skip=False, diagnostics=False, diagnostic_readback=False, open_denied=False):
    require(not open_denied or (visible_skip and diagnostics), 'Open denial requires visible diagnostics')
    require(r.get('scope') == 'CC13/disposable-Copy-derivative-boot-progress-only', 'Scope')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged'), 'Media/boot')
    require(not any(r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Trace incomplete')
    require(r.get('copyCpuImageWrites') == [] and r.get('copyActiveInvocations') == 0, 'Image/active ownership')
    rows = r['copyInvocationOwnership']; e = r['observedDosCalls']
    expected_returns = [10 if open_denied else 0, 0]
    expected_error = 214 if open_denied else 203
    require(len(rows) == 2 and len({v['Segment'] for v in rows}) == 1, 'Resident reuse')
    require([v['Allocations'] for v in rows] == [3, 4] and [v['ReturnCode'] for v in rows] == expected_returns and all(v['Frees'] == v['Allocations'] and v['ImageUnchanged'] for v in rows), 'Results/ownership')
    require([v['ParserFailed'] for v in rows] == [False, False] and [v['OpenFailed'] for v in rows] == [open_denied, False], 'Parser/open outcomes')
    segment = rows[0]['Segment']
    require(one([v for v in e if v['Name'] == 'LoadSeg' and v['Text'] == 'C:Ed'], 'Load').get('ReturnedD0') == segment, 'Loaded segment')
    require([v.get('ReturnedD0') for v in e if v['Name'] == 'RunCommand' and v['D1'] == segment] == expected_returns, 'Command returns')
    starts = [i for i, v in enumerate(e) if v['Name'] == 'CopyOpenLibrary']
    require(len(starts) == 2, 'Library leases')
    calls_by_run = []
    for index, start in enumerate(starts):
        end = next(i for i in range(start + 1, len(e)) if e[i]['Name'] == 'CopyCloseLibrary')
        calls = e[start:end]; calls_by_run.append(calls)
        require([v['D0'] for v in calls if v['Name'] == 'CopyAllocMem'] == ([4772, 27 if open_denied else 23, 33] if index == 0 else [4772, 33, 33, 512]), 'Allocation sizes')
        metadata = [v for v in calls if v['Name'] in ['SetProtection', 'SetFileDate', 'SetComment']]
        require([v['Name'] for v in metadata] == ([] if visible_skip and index == 0 else ['SetProtection', 'SetFileDate', 'SetComment']), 'Metadata order/count')
        target = 'Workbench3.1:C/Type' if index == 0 else 'Ram Disk:copy-target'
        require(all(v['Text'] == target for v in metadata), 'Metadata target')
        if index == 0:
            if visible_skip:
                require(e[end].get('ReturnedIoErr') == expected_error, 'Visible failure error')
                if diagnostics:
                    prefix = one([v for v in calls if v['Name'] == 'VPrintf' and v['Text'] == ' not %s: '], 'Failure prefix')
                    fault = one([v for v in calls if v['Name'] == 'PrintFault'], 'Failure diagnostic count')
                    require(fault['D1'] == expected_error and fault['D2'] == 0 and 'ReturnedD0' in fault and 'ReturnedD0' in prefix and calls.index(prefix) < calls.index(fault), 'Diagnostic error/order')
            else:
                require(all(v.get('ReturnedD0') == 0 and v.get('ReturnedIoErr') == 214 for v in metadata), 'Write protection not observed')
            if open_denied:
                failed = one([v for v in calls if v['Name'] == 'Open'], 'Failed destination open')
                require(failed['Text'] == 'Workbench3.1:copy-new' and failed['D2'] == 1006 and failed.get('ReturnedD0') == 0 and failed.get('ReturnedIoErr') == 214, 'Destination denial')
            require(not any(v['Name'] in (['Read', 'Write', 'Close', 'DeleteFile'] if open_denied else ['Open', 'Read', 'Write', 'Close', 'DeleteFile']) for v in calls), 'Failed/skipped file changed')
        else:
            require(all(v.get('ReturnedD0', 0) != 0 for v in metadata), 'Recovery metadata failed')
    calls = calls_by_run[1]
    payload = b'copy-native-boot-payload\n'
    opens = [v for v in calls if v['Name'] == 'Open']
    require(len(opens) == 2 and [v['Text'] for v in opens] == ['Ram Disk:copy-target', 'RAM:copy-source'] and all(v.get('ReturnedD0', 0) != 0 for v in opens), 'Recovery opens')
    read = one([v for v in calls if v['Name'] == 'Read'], 'Read')
    write = one([v for v in calls if v['Name'] == 'Write'], 'Write')
    require(read['D1'] == opens[1]['ReturnedD0'] and write['D1'] == opens[0]['ReturnedD0'] and read.get('ReturnedD0') == write.get('ReturnedD0') == len(payload) and read['ReadPayloadHex'] == write['PayloadHex'] == payload.hex().upper(), 'Recovery transfer')
    for opened in opens:
        closed = one([v for v in calls if v['Name'] == 'Close' and v['D1'] == opened['ReturnedD0']], 'Close')
        require(closed.get('ReturnedD0', 0) != 0 and calls.index(closed) > calls.index(write), 'Close result/order')
    reopened = one([v for v in e if v['Name'] == 'Open' and v['Text'] == 'copy-target'], 'Independent open')
    pos = e.index(reopened); handle = reopened.get('ReturnedD0', 0)
    require(handle != 0, 'Readback handle')
    closed = next(v for v in e[pos + 1:] if v['Name'] == 'Close' and v['D1'] == handle and v['Task'] == reopened['Task'])
    require([v.get('ReturnedD0') for v in e[pos + 1:e.index(closed)] if v['Name'] == 'FGetC' and v['D1'] == handle] == list(payload) and closed.get('ReturnedD0', 0) != 0, 'Independent bytes')
    removed = one([v for v in e if v['Name'] == 'RemSegment' and v.get('RemovedSegmentList') == segment], 'Removal')
    # The allocator may reuse the address after removal; match the loaded
    # segment's size as well as its pointer, not later unrelated frees.
    freed = one([v for v in e if v['Name'] == 'SegmentFreeMem' and v['A1'] == r['copySegmentAllocationBase'] and v['D0'] == r['copySegmentAllocationBytes']], 'Segment free')
    require(removed.get('ReturnedD0', 0) != 0 and e.index(removed) < e.index(freed) and freed['A1'] == r['copySegmentAllocationBase'] and freed['D0'] == r['copySegmentAllocationBytes'] and 'ReturnedD0' in freed, 'Resident removal/free')
    if diagnostic_readback:
        require(visible_skip and diagnostics, 'Readback requires diagnostic profile')
        expected = b' not opened for output: disk is write-protected\n' if open_denied else b' not opened for output: object already exists\n'
        opened = one([v for v in e if v['Name'] == 'Open' and v['Text'] == 'copy-diagnostic'], 'Diagnostic readback open')
        pos = e.index(opened); handle = opened.get('ReturnedD0', 0)
        require(handle != 0, 'Diagnostic readback handle')
        closed = next(v for v in e[pos + 1:] if v['Name'] == 'Close' and v['D1'] == handle and v['Task'] == opened['Task'])
        require([v.get('ReturnedD0') for v in e[pos + 1:e.index(closed)] if v['Name'] == 'FGetC' and v['D1'] == handle and v['Task'] == opened['Task']] == list(expected) and closed.get('ReturnedD0', 0) != 0, 'Diagnostic bytes')
        type_start = max(i for i in range(pos) if e[i]['Name'] == 'LoadSeg' and e[i]['Text'] == 'C:Type' and e[i]['Task'] == opened['Task'])
        fib = one([v for v in e[type_start:pos] if v['Name'] == 'MatchFirst' and cstring(v.get('FileNameBytes', '')) == b'copy-diagnostic'], 'Diagnostic size record')
        require(fib.get('ReturnedD0') == 0 and fib['FileSize'] == len(expected), 'Diagnostic size')
    return {'status': 'destination-open-denial-and-recovery-observed' if open_denied else 'visible-overwrite-skip-and-recovery-observed' if visible_skip else 'denied-metadata-and-resident-recovery-observed', 'metadataError': None if visible_skip else 214, 'visibleSkipProfile': visible_skip and not open_denied, 'diagnosticTextQualified': diagnostic_readback, 'diagnosticCallsQualified': diagnostics,
            'returns': expected_returns, 'destinationOpenDenied': open_denied, 'deniedMetadataApplied': False, 'fullCommandQualified': False,
            'limit': 'This bounded failure/recovery case does not qualify full original-command parity or multi-object partial-copy reporting.'}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path)
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--visible-skip', action='store_true')
    p.add_argument('--diagnostics', action='store_true')
    p.add_argument('--diagnostic-readback', action='store_true')
    p.add_argument('--open-denied', action='store_true')
    args = p.parse_args(); data = args.observations.read_bytes()
    require(not args.diagnostics or args.visible_skip, 'Diagnostics require visible skip')
    result = verify(json.loads(data), args.visible_skip, args.diagnostics, args.diagnostic_readback, args.open_denied)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(), verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
