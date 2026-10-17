"""Verify Dir contracts against raw native requests, receipts and original DOS Fault."""
import hashlib
import json
from pathlib import Path
import struct
import sys


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def verify(stage, batch, cpu):
    stage = Path(stage); directory = stage/'runs'/(batch+'-'+cpu)
    definitions = read(stage/('cases-'+batch+'.json'))
    report = read(directory/'observations.json'); result = report['result']
    assert report['completed'] and result['StopReason'] == 'ProgramReturned'
    assert result['LaunchStatus'] == result['ReturnValue'] == 0 and result['DiskSnapshotError'] is None
    assert report['cpu'] == cpu and result['CpuModel'] == 'M'+cpu
    assert result['ExecutionMode'] == 'Interpreter' and result['Frames'] <= 3000
    assert result['HunkSha256'].lower() == digest(stage/'guest/DirQualification')
    for name, key in [('Dir.baseline', 'DIRB'), ('Dir.candidate', 'DIRC'), ('TEXT.IN', 'TEXT.IN'), ('LONG.IN', 'LONG.IN')]:
        assert report['inputs'][key] == digest(stage/'inputs'/name)
    fixture = (directory/'CASES.FIX').read_bytes(); raw = (directory/'receipt.bin').read_bytes()
    faults = (directory/'FAULTS.FIX').read_bytes(); assert len(faults) == 8*128
    diagnostics = {struct.unpack_from('>I', faults, i)[0]: faults[i+4:i+128].split(b'\0')[0].decode('latin1')+'\n'
                   for i in range(0, len(faults), 128)}
    assert set(diagnostics) == {117, 118, 120, 204, 205, 212, 236, 304}
    rows = report['observations']; assert len(rows) == len(definitions)
    assert struct.unpack_from('>II', fixture) == (0x53514631, len(rows)) and len(fixture) == 16+1040*len(rows)
    assert struct.unpack_from('>II', raw) == (0x53515231, len(rows))
    assert digest(directory/'CASES.FIX') == report['inputs']['CASES.FIX']
    cursor = 8; failures = []
    for index, (case, row) in enumerate(zip(definitions, rows)):
        args = (case['Arguments']+'\n').encode('ascii')
        assert struct.unpack_from('>5I', fixture, 16+index*1040) == (index, case['Image'], case['Stack'], int(case['CtrlC']), len(args))
        assert fixture[36+index*1040:36+index*1040+len(args)] == args
        assert all(row[k] == case[k] for k in ['Name', 'Image', 'Stack', 'Arguments', 'CtrlC'])
        fields = struct.unpack_from('>28I', raw, cursor); length = fields[5]
        data = raw[cursor+112:cursor+112+length]; cursor += 112+length
        assert len(data) == length and fields[:3] == (index, row['Image'], row['Stack'])
        assert fields[3:5] == (row['returnCode'] & 0xffffffff, row['ioError'])
        assert fields[6:8] == (row['availableBefore'], row['availableAfter'])
        assert list(fields[8:18]) == row['before'] and list(fields[18:28]) == row['after']
        assert data.decode('latin1') == row['output'] and hashlib.sha256(data).hexdigest() == row['outputSha256']
        errors = []
        if (row['returnCode'], row['ioError']) != (case['expectedReturn'], case['expectedIoErr']): errors.append('return/IoErr')
        if fields[8:18] != fields[18:28] or not row['callerStateRestored']: errors.append('caller restoration')
        expected = case['expectedOutput']
        if isinstance(expected, dict): expected = expected.get('prefix', '')+diagnostics[expected['fault']]
        if row['output'] != expected: errors.append('exact output')
        if case['phase'] == 'repeat' and fields[6] != fields[7]: errors.append('repeat AvailMem delta')
        if errors: failures.append(dict(index=index, name=row['Name'], image=row['Image'], errors=errors,
                                        expectedReturn=case['expectedReturn'], actualReturn=row['returnCode'],
                                        expectedIoErr=case['expectedIoErr'], actualIoErr=row['ioError'], expected=expected, actual=row['output']))
        if index % 2:
            a = rows[index-1]; assert {a['Image'], row['Image']} == {0, 1}
            if any(a[k] != row[k] for k in ['Name', 'Arguments', 'Stack', 'CtrlC', 'returnCode', 'ioError', 'output', 'outputSha256']):
                failures.append(dict(index=index, name=row['Name'], errors=['baseline/candidate mismatch']))
    assert cursor == len(raw)
    return dict(batch=batch, cpu=cpu, passed=not failures, invocations=len(rows), pairs=len(rows)//2,
                failureCount=len(failures), failures=failures,
                memoryDeltas=[r['availableAfter']-r['availableBefore'] for r in rows], result=result)


if __name__ == '__main__':
    stage, batch, cpu = Path(sys.argv[1]), sys.argv[2], sys.argv[3]
    summary = verify(stage, batch, cpu)
    (stage/'runs'/(batch+'-'+cpu)/'qualification.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
    print(cpu, batch, summary['passed'], summary['invocations'], summary['failures'])
    sys.exit(0 if summary['passed'] else 1)
