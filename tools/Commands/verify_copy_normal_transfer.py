"""Check two sequential normal-mode RAM copies; not full Copy qualification."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one


def verify(r, overwrite=False, protected=False, workspace_bytes=4770):
    require(r.get('scope') == 'CC13/disposable-Copy-derivative-boot-progress-only', 'Wrong scope')
    require(r.get('rootInfoReady') and r.get('diskBytesUnchanged'), 'Boot/media prerequisites')
    require(not any(r.get(k) for k in ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    rows = r['copyInvocationOwnership']; e = r['observedDosCalls']
    require(len(rows) == 2 and len({x['Segment'] for x in rows}) == 1, 'Resident reuse')
    require([x['Allocations'] for x in rows] == ([3, 4] if overwrite else [4, 4]), 'Mode allocation counts')
    require(all(x['ReturnCode'] == 0 and x['Allocations'] == x['Frees'] and x['ImageUnchanged'] for x in rows), 'Ownership/results')
    require(r.get('copyCpuImageWrites') == [], 'Image writes')
    segment = rows[0]['Segment']
    load = one([x for x in e if x['Name'] == 'LoadSeg' and x['Text'] == 'C:Ed'], 'Load count')
    require(load.get('ReturnedD0') == segment, 'Segment identity')
    runs = [x for x in e if x['Name'] == 'RunCommand' and x['D1'] == segment]
    require(len(runs) == 2 and all(x.get('ReturnedD0') == 0 for x in runs), 'Run results')
    starts = [i for i, x in enumerate(e) if x['Name'] == 'CopyOpenLibrary']
    require(len(starts) == 2, 'Invocation leases')
    if protected:
        require(overwrite, 'Protected profile requires overwrite')
        setup = one([x for x in e[:starts[0]] if x['Name'] == 'SetProtection' and x['Text'] == 'copy-target2'], 'Protection setup')
        require(setup['D2'] == 1 and setup.get('ReturnedD0', 0) != 0, 'Delete protection not established')
    payload = b'copy-native-boot-payload\n'
    for a, name in zip(starts, ['copy-target', 'copy-target2']):
        b = next(i for i in range(a + 1, len(e)) if e[i]['Name'] == 'CopyCloseLibrary')
        calls = e[a:b]
        if overwrite and name == 'copy-target':
            require(not any(x['Name'] in ['Open', 'Read', 'Write', 'Close'] for x in calls), 'DONTOVERWRITE did file transfer')
            reopened = one([x for x in e if x['Name'] == 'Open' and x['Text'] == name], 'Preserved file open')
            i = e.index(reopened); handle = reopened.get('ReturnedD0', 0)
            require(handle != 0, 'Preserved file missing')
            close = next(x for x in e[i + 1:] if x['Name'] == 'Close' and x['D1'] == handle and x['Task'] == reopened['Task'])
            chars = [x.get('ReturnedD0') for x in e[i + 1:e.index(close)] if x['Name'] == 'FGetC' and x['D1'] == handle]
            require(chars == list(b'preserve-existing\n') and close.get('ReturnedD0', 0) != 0, 'DONTOVERWRITE changed bytes')
            type_start = max(j for j in range(i) if e[j]['Name'] == 'LoadSeg' and e[j]['Text'] == 'C:Type' and e[j]['Task'] == reopened['Task'])
            fib = one([x for x in e[type_start:i] if x['Name'] == 'MatchFirst' and bytes.fromhex(x.get('FileNameBytes', '')).split(b'\0')[0] == name.encode()], 'Preserved file size record')
            require(fib.get('ReturnedD0') == 0 and fib.get('FileSize') == 18, 'Preserved file size')
            continue
        require([x['D0'] for x in calls if x['Name'] == 'CopyAllocMem'] == [workspace_bytes, 33 if name == 'copy-target' else 35, 33, 512], 'Normal allocation sequence')
        opens = [x for x in calls if x['Name'] == 'Open']
        require(len(opens) == 2, 'Open count')
        target, source = opens
        if protected:
            clear = one([x for x in calls if x['Name'] == 'SetProtection' and x['D2'] == 0], 'Force clear count')
            delete = one([x for x in calls if x['Name'] == 'DeleteFile'], 'Force delete count')
            require(clear['Text'] == delete['Text'] == target['Text'] and clear.get('ReturnedD0', 0) != 0 and delete.get('ReturnedD0', 0) != 0, 'Force clear/delete failed')
            require(calls.index(clear) < calls.index(delete) < calls.index(target), 'Force clear/delete/open order')
        require(target['Text'] == 'Ram Disk:' + name and target['D2'] == 1006 and source['Text'] == 'RAM:copy-source' and source['D2'] == 1005, 'Normal destination-first path')
        require(target.get('ReturnedD0', 0) and source.get('ReturnedD0', 0) and target['ReturnedD0'] != source['ReturnedD0'], 'Handle identities')
        read = one([x for x in calls if x['Name'] == 'Read'], 'Read count')
        write = one([x for x in calls if x['Name'] == 'Write'], 'Write count')
        require(read['D1'] == source['ReturnedD0'] and write['D1'] == target['ReturnedD0'], 'Transfer handles')
        require(read.get('ReturnedD0') == write.get('ReturnedD0') == 25 and read.get('ReadPayloadHex') == write.get('PayloadHex') == payload.hex().upper(), 'Transfer payload')
        for opened in opens:
            close = one([x for x in calls if x['Name'] == 'Close' and x['D1'] == opened['ReturnedD0']], 'Close count')
            require(close.get('ReturnedD0', 0) != 0 and calls.index(close) > calls.index(write), 'Close result/order')
        reopened = one([x for x in e if x['Name'] == 'Open' and x['Text'] == name], 'Independent open')
        i = e.index(reopened); handle = reopened.get('ReturnedD0', 0)
        require(handle != 0, 'Independent open failed')
        close = next(x for x in e[i + 1:] if x['Name'] == 'Close' and x['D1'] == handle and x['Task'] == reopened['Task'])
        chars = [x.get('ReturnedD0') for x in e[i + 1:e.index(close)] if x['Name'] == 'FGetC' and x['D1'] == handle]
        require(chars == list(payload) and close.get('ReturnedD0', 0) != 0, 'Independent payload')
        type_start = max(j for j in range(i) if e[j]['Name'] == 'LoadSeg' and e[j]['Text'] == 'C:Type' and e[j]['Task'] == reopened['Task'])
        fib = one([x for x in e[type_start:i] if x['Name'] == 'MatchFirst' and bytes.fromhex(x.get('FileNameBytes', '')).split(b'\0')[0] == name.encode()], 'Size record')
        require(fib.get('ReturnedD0') == 0 and fib.get('FileSize') == 25, 'Independent size')
    return {'status': 'normal-copy-overwrite-evidence-passed' if overwrite else 'normal-copy-two-transfer-evidence-passed',
            'successfulTransferBytes': 25, 'overwriteProfile': overwrite, 'protectedTargetProfile': protected, 'fullCommandQualified': False}


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('observations', type=Path); p.add_argument('--output', type=Path, required=True)
    p.add_argument('--overwrite', action='store_true')
    p.add_argument('--protected', action='store_true')
    args = p.parse_args(); data = args.observations.read_bytes()
    result = verify(json.loads(data), args.overwrite, args.protected)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(), verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n')
