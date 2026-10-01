"""Verify bounded recursive file payloads; empty-directory type remains separate."""
import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, directories=False, created_locks=False, traversal_locks=False):
    require(r.get('scope') == 'CC13/disposable-Copy-derivative-boot-progress-only', 'Wrong scope')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged'), 'Boot/media prerequisites')
    require(not any(r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    row = one(r['copyInvocationOwnership'], 'Invocation count')
    require(row['ReturnCode'] == 0 and row['Allocations'] == row['Frees'] == 5 and row['ImageUnchanged'], 'Ownership/results')
    require(r.get('copyCpuImageWrites') == [], 'Code writes')
    e = r['observedDosCalls']
    start = one([i for i,x in enumerate(e) if x['Name'] == 'CopyOpenLibrary'], 'Library lease')
    end = next(i for i in range(start + 1, len(e)) if e[i]['Name'] == 'CopyCloseLibrary')
    calls = e[start:end]
    if traversal_locks:
        live = Counter(); acquired = Counter(); anchors = set()
        original_directory = current_directory = None
        for call in calls:
            name = call['Name']
            if name in ['Lock', 'ParentDir', 'CreateDir', 'DupLock']:
                require('ReturnedD0' in call, 'Lock acquisition did not return')
                handle = call['ReturnedD0']
                if handle:
                    live[handle] += 1; acquired[name] += 1
            elif name == 'UnLock' and call['D1']:
                require(live[call['D1']] > 0 and 'ReturnedD0' in call, 'Unowned/unreturned lock release')
                live[call['D1']] -= 1
            elif name == 'CurrentDir':
                require('ReturnedD0' in call, 'Directory swap did not return')
                if original_directory is None:
                    original_directory = current_directory = call['ReturnedD0']
                require(call['ReturnedD0'] == current_directory, 'Directory swap chain broken')
                current_directory = call['D1']
            elif name == 'MatchFirst':
                require(call['D2'] not in anchors and call.get('ReturnedD0') == 0, 'Matcher start mismatch')
                anchors.add(call['D2'])
            elif name == 'MatchEnd':
                require(call['D1'] in anchors and 'ReturnedD0' in call, 'Matcher cleanup mismatch')
                anchors.remove(call['D1'])
        require(acquired == Counter({'Lock': 11, 'ParentDir': 9, 'CreateDir': 3}), 'Missing traversal acquisition coverage')
        require(not +live and not anchors, 'Traversal ownership remains')
        require(original_directory is not None and current_directory == original_directory, 'Caller directory not restored')
    for path, payload in [('top', b'top-level-payload\n'), ('sub/leaf', b'nested-leaf-payload\n')]:
        source = one([x for x in calls if x['Name'] == 'Open' and x['Text'] == 'RAM:tree/' + path], 'Source open')
        target = one([x for x in calls if x['Name'] == 'Open' and x['Text'] == 'Ram Disk:tree-copy/' + path], 'Nested target open')
        require(source['D2'] == 1005 and target['D2'] == 1006 and calls.index(target) < calls.index(source), 'Normal open modes/order')
        require(source.get('ReturnedD0', 0) and target.get('ReturnedD0', 0), 'Open failed')
        # Handles may be reused later; restrict each transfer to its open/close interval.
        for opened, operation, field in [(source, 'Read', 'ReadPayloadHex'), (target, 'Write', 'PayloadHex')]:
            a = calls.index(opened); handle = opened['ReturnedD0']
            close = next(x for x in calls[a + 1:] if x['Name'] == 'Close' and x['D1'] == handle)
            transfer = one([x for x in calls[a + 1:calls.index(close)] if x['Name'] == operation and x['D1'] == handle], 'Transfer count')
            require(transfer.get('ReturnedD0') == len(payload) and transfer.get(field) == payload.hex().upper() and close.get('ReturnedD0', 0) != 0, 'Transfer/close mismatch')
        name = path.split('/')[-1]
        opened = one([x for x in e[end:] if x['Name'] == 'Open' and x['Text'] == name], 'Independent readback open')
        a = e.index(opened); handle = opened.get('ReturnedD0', 0)
        require(handle != 0, 'Readback open failed')
        close = next(x for x in e[a + 1:] if x['Name'] == 'Close' and x['D1'] == handle and x['Task'] == opened['Task'])
        chars = [x.get('ReturnedD0') for x in e[a + 1:e.index(close)] if x['Name'] == 'FGetC' and x['D1'] == handle]
        require(chars == list(payload) and close.get('ReturnedD0', 0) != 0, 'Readback bytes')
        type_start = max(i for i in range(a) if e[i]['Name'] == 'LoadSeg' and e[i]['Text'] == 'C:Type')
        fib = one([x for x in e[type_start:a] if x['Name'] == 'MatchFirst' and bytes.fromhex(x.get('FileNameBytes', '')).split(b'\0')[0] == name.encode()], 'Readback size record')
        require(fib.get('ReturnedD0') == 0 and fib.get('FileSize') == len(payload), 'Readback size')
    if directories:
        for path in ['RAM:tree-copy', 'Ram Disk:tree-copy/empty', 'Ram Disk:tree-copy/sub']:
            created = one([x for x in calls if x['Name'] == 'CreateDir' and x['Text'] == path], 'Destination directory creation')
            require(created.get('ReturnedD0', 0) != 0, 'CreateDir failed')
            if created_locks:
                handle = created['ReturnedD0']; after = calls[calls.index(created) + 1:]
                release = next(x for x in after if x['Name'] == 'UnLock' and x['D1'] == handle and x['Task'] == created['Task'])
                require('ReturnedD0' in release, 'Created lock release did not return')
                require(not any(x['Name'] in ['Lock', 'CreateDir'] and x.get('ReturnedD0') == handle for x in after[:after.index(release)]), 'Lock reused before release')
        listed = one([x for x in e[end:] if x['Name'] == 'LoadSeg' and x['Text'] == 'C:List'], 'Independent List load')
        run = one([x for x in e[e.index(listed):] if x['Name'] == 'RunCommand' and x['D1'] == listed.get('ReturnedD0')], 'Independent List run')
        require(run.get('ReturnedD0') == 0, 'Independent List failed')
        match = one([x for x in e[e.index(run):] if x['Name'] == 'MatchFirst' and bytes.fromhex(x.get('FileNameBytes', '')).split(b'\0')[0] == b'empty'], 'Empty directory lookup')
        require(match.get('ReturnedD0') == 0 and match.get('DirectoryEntryType') == 2, 'Destination is not a directory')
        require(not any(x['Name'] == 'Open' and 'tree-copy/empty/' in (x.get('Text') or '') for x in calls), 'Unexpected empty-directory file')
    require(not created_locks or directories, 'Created-lock profile requires directories')
    return {'status': 'recursive-two-file-transfer-passed', 'emptyDirectoryPreservationVerified': directories,
            'createdDirectoryLocksReleased': created_locks,
            'explicitTraversalLocksAndDirectoryRestorationVerified': traversal_locks,
            'directoryLockLifecycleQualified': False, 'fullCommandQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path); p.add_argument('--output', type=Path, required=True)
    p.add_argument('--directories', action='store_true')
    p.add_argument('--created-locks', action='store_true')
    p.add_argument('--traversal-locks', action='store_true')
    args = p.parse_args(); data = args.observations.read_bytes()
    result = verify(json.loads(data), args.directories, args.created_locks, args.traversal_locks)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(), verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n')
