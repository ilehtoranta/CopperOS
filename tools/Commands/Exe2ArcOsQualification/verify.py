"""Audit original-DOS Exe2Arc results against raw requests and disk receipts."""
import hashlib
import json
from pathlib import Path
import struct


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def verify(stage, batch, cpu):
    stage = Path(stage); directory = stage/'runs'/(batch+'-'+cpu)
    cases = read(stage/('cases-'+batch+'.json')); observed = read(directory/'observations.json')
    result = observed['result']; rows = observed['observations']
    assert observed['completed'] and result['StopReason']=='ProgramReturned'
    assert result['LaunchStatus']==result['ReturnValue']==0 and result['DiskSnapshotError'] is None
    assert observed['cpu']==cpu and result['CpuModel']=='M'+cpu and result['ExecutionMode']=='Interpreter' and result['Frames']<=3000
    assert result['HunkSha256'].lower()==digest(stage/'guest/Exe2ArcQualification')
    assert result['RomSha256'].lower()==read(stage/'freeze.json')['rom']['sha256']
    for label, key in [('baseline', 'EXE2ARCB'), ('candidate', 'EXE2ARCC')]: assert observed['inputs'][key]==digest(stage/'inputs'/('Exe2Arc.'+label))
    # This descriptor is part of the request, not an inferred success result.
    import importlib.util
    spec = importlib.util.spec_from_file_location('Exe2ArcCaseAssets', Path(__file__).with_name('cases.py'))
    assets = importlib.util.module_from_spec(spec); spec.loader.exec_module(assets)
    mask, names = assets.input_mask(cases)
    for name in names: assert observed['inputs'][name]==digest(stage/'inputs'/name)
    assert set(observed['inputs']) == names | {'EXE2ARCB', 'EXE2ARCC', 'CASES.FIX'}
    fixture = (directory/'CASES.FIX').read_bytes(); raw = (directory/'receipt.bin').read_bytes()
    assert hashlib.sha256(fixture).hexdigest()==observed['inputs']['CASES.FIX']
    assert len(rows)==len(cases) and len(fixture)==16+1552*len(cases)
    assert struct.unpack_from('>II', fixture)==(0x45414631, len(cases))
    assert struct.unpack_from('>II', fixture, 8)==(mask, 0)
    assert struct.unpack_from('>II', raw)==(0x45415231, len(cases))
    failures = []; cursor = 8
    for index, (case, row) in enumerate(zip(cases, rows)):
        fields = struct.unpack_from('>32I', raw, cursor)
        args = (case['Arguments']+'\n').encode('ascii'); start = 16+index*1552
        assert struct.unpack_from('>5I', fixture, start)==(index, case['Image'], case['Stack'], int(case['CtrlC']), len(args))
        assert fixture[start+20:start+20+len(args)]==args
        assert fixture[start+1040:start+1552].split(b'\0', 1)[0].decode('ascii')==case['OutputPath']
        assert all(row[key]==case[key] for key in ['Name', 'Image', 'Stack', 'Arguments', 'CtrlC', 'OutputPath'])
        assert fields[:3]==(index, row['Image'], row['Stack'])
        assert fields[3:5]==(row['returnCode']&0xffffffff, row['ioError'])
        assert fields[6:8]==(row['availableBefore'], row['availableAfter'])
        assert list(fields[8:18])==row['before'] and list(fields[18:28])==row['after']
        stdout = raw[cursor+128:cursor+128+fields[5]]
        payload = raw[cursor+128+fields[5]:cursor+128+fields[5]+fields[29]]
        assert len(stdout)==fields[5] and len(payload)==fields[29]
        assert row['output']==stdout.decode('latin1') and row['outputSha256']==hashlib.sha256(stdout).hexdigest()
        assert row['payloadHex']==payload.hex() and row['payloadSha256']==hashlib.sha256(payload).hexdigest()
        assert fields[28] in [0, 1] and bool(fields[28])==row['fileExists']
        assert fields[29:32]==(row['payloadLength'], row['payloadClosed'], row['payloadDeleted'])
        cursor += 128+len(stdout)+len(payload)
        errors = []
        if (row['returnCode'], row['ioError'])!=(case['expectedReturn'], case['expectedIoErr']): errors.append('return/IoErr')
        if stdout!=case['expectedOutput'].encode('latin1'): errors.append('stdout')
        if payload!=bytes.fromhex(case['expectedPayload']): errors.append('extracted bytes')
        if bool(fields[28])!=case['expectedExists']: errors.append('output-file existence')
        if fields[28] and (not fields[30] or fields[31]!=1): errors.append('output close/delete cleanup')
        if not fields[28] and (fields[29] or fields[30] or fields[31]): errors.append('missing-file receipt')
        if fields[8:18]!=fields[18:28] or not row['callerStateRestored']: errors.append('caller restoration')
        if case['phase']=='repeat' and fields[6]!=fields[7]: errors.append('repeat memory delta')
        if errors: failures.append(dict(index=index, name=case['Name'], image=case['Image'], errors=errors))
        if index%2:
            other = rows[index-1]
            assert {row['Image'], other['Image']}=={0, 1}
            keys = ['Name', 'Stack', 'Arguments', 'CtrlC', 'OutputPath', 'returnCode', 'ioError', 'output', 'fileExists', 'payloadHex', 'payloadClosed', 'payloadDeleted']
            if any(row[key]!=other[key] for key in keys): failures.append(dict(index=index, name=case['Name'], errors=['compiler-image mismatch']))
    assert cursor==len(raw)
    return dict(batch=batch, cpu=cpu, passed=not failures, invocations=len(rows), failures=failures,
                memoryDeltas=[r['availableAfter']-r['availableBefore'] for r in rows],
                frames=result['Frames'], cycles=result['Cycles'])


if __name__=='__main__':
    import sys
    stage, batch, cpu = Path(sys.argv[1]), sys.argv[2], sys.argv[3]
    result = verify(stage, batch, cpu)
    (stage/'runs'/(batch+'-'+cpu)/'qualification.json').write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print(result); sys.exit(0 if result['passed'] else 1)
