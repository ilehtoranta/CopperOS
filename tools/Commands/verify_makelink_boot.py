"""Verify bounded original-DOS hard-link creation and alias readback."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, soft=False, unsupported=False):
    require(not unsupported or soft, "Unsupported profile requires soft mode")
    require(r.get('scope') == 'CC12/disposable-MakeLink-derivative-boot-progress-only' and r.get('commandUnderTest') == 'MakeLink', 'Command scope')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged') and not any(r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete boot/trace')
    row = one(r['copyInvocationOwnership'], 'Invocation')
    require(row['ReturnCode'] == (20 if unsupported else 0) and row['Allocations'] == row['Frees'] == 1 and row['ImageUnchanged'] and r.get('copyCpuImageWrites') == [] and r.get('copyActiveInvocations') == 0, 'Command ownership/result')
    e = r['observedDosCalls']
    a = one([i for i,v in enumerate(e) if v['Name'] == 'CopyOpenLibrary'], 'Library lease')
    b = next(i for i in range(a + 1,len(e)) if e[i]['Name'] == 'CopyCloseLibrary')
    calls = e[a:b]
    parser = one([v for v in calls if v['Name'] == 'ReadArgs'], 'Parser')
    require(parser['Text'] == 'FROM/A,TO/A,HARD/S,FORCE/S' and parser.get('ReturnedD0', 0) != 0, 'Real parser')
    if soft:
        require(not any(v['Name'] in ['Lock', 'UnLock', 'Examine', 'AllocDosObject', 'FreeDosObject'] for v in calls), 'Soft link acquired target/FIB ownership')
        link = one([v for v in calls if v['Name'] == 'MakeLink'], 'Soft-link call')
        require(link['Text'] == 'RAM:link-alias' and link['D2'] != 0 and link['D3'] == 1 and (link.get('ReturnedD0') == 0 and link.get('ReturnedIoErr') == 236 if unsupported else link.get('ReturnedD0', 0) != 0), 'Soft-link ABI/result')
        args_free = one([v for v in calls if v['Name'] == 'FreeArgs'], 'Parser free')
        require(args_free['D1'] == parser['ReturnedD0'] and calls.index(link) < calls.index(args_free), 'Soft-link parser lifetime')
    else:
        lock = one([v for v in calls if v['Name'] == 'Lock'], 'Target lock')
        require(lock['Text'] == 'RAM:link-source' and lock.get('ReturnedD0', 0) != 0, 'Target lock identity')
        examined = one([v for v in calls if v['Name'] == 'Examine'], 'Target examine')
        require(examined['D1'] == lock['ReturnedD0'] and examined['D2'] % 4 == 0 and examined.get('ReturnedD0', 0) != 0 and examined['DirectoryEntryType'] < 0, 'Regular-file FIB')
        link = one([v for v in calls if v['Name'] == 'MakeLink'], 'Link call')
        require(link['Text'] == 'RAM:link-alias' and link['D2'] == lock['ReturnedD0'] and link['D3'] == 0 and link.get('ReturnedD0', 0) != 0, 'Hard-link ABI/result')
        freed = one([v for v in calls if v['Name'] == 'FreeDosObject'], 'FIB free')
        unlock = one([v for v in calls if v['Name'] == 'UnLock'], 'Unlock')
        args_free = one([v for v in calls if v['Name'] == 'FreeArgs'], 'Parser free')
        require(freed['D2'] == examined['D2'] and unlock['D1'] == lock['ReturnedD0'] and args_free['D1'] == parser['ReturnedD0'] and calls.index(link) < calls.index(freed) < calls.index(unlock) < calls.index(args_free), 'Cleanup identities/order')
    if unsupported:
        fault = one([v for v in calls if v['Name'] == 'PrintFault'], 'Unsupported diagnostic')
        require(fault['D1'] == 236 and 'ReturnedD0' in fault and calls.index(link) < calls.index(fault) < calls.index(args_free), 'Unsupported failure ordering')
        require(not any(v['Name'] == 'Open' and v['Text'] == 'link-alias' for v in e[b:]), 'Unexpected alias open')
        match = one([v for v in e[b:] if v['Name'] == 'MatchFirst'], 'Absent alias match')
        require(match.get('ReturnedD0') == 205 and match.get('ReturnedIoErr') == 205, 'Alias unexpectedly exists')
        return {'status': 'original-dos-soft-link-unsupported-verified', 'error': 236, 'returnCode': 20, 'softLinkCreationQualified': False, 'fullCommandQualified': False}
    opened = one([v for v in e[b:] if v['Name'] == 'Open' and v['Text'] == 'link-alias'], 'Alias readback')
    handle = opened.get('ReturnedD0', 0); require(handle != 0, 'Alias missing')
    pos = e.index(opened)
    closed = next(v for v in e[pos + 1:] if v['Name'] == 'Close' and v['D1'] == handle and v['Task'] == opened['Task'])
    payload = b'hard-link-payload\n'
    require([v.get('ReturnedD0') for v in e[pos + 1:e.index(closed)] if v['Name'] == 'FGetC' and v['D1'] == handle] == list(payload) and closed.get('ReturnedD0', 0) != 0, 'Alias bytes')
    fib = one([v for v in e[b:pos] if v['Name'] == 'MatchFirst' and bytes.fromhex(v.get('FileNameBytes', '')).split(b'\0')[0] == b'link-alias'], 'Alias size')
    require(fib.get('ReturnedD0') == 0 and fib['FileSize'] == len(payload), 'Alias size/result')
    return {'status': 'original-dos-soft-link-readback-verified' if soft else 'original-dos-hard-link-readback-verified', 'aliasBytes': len(payload), 'fullCommandQualified': False, 'installedFlagsQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path); p.add_argument('--output', type=Path, required=True)
    p.add_argument('--soft', action='store_true')
    p.add_argument('--unsupported', action='store_true')
    args = p.parse_args(); data = args.observations.read_bytes()
    result = verify(json.loads(data), args.soft, args.unsupported)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(), verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
