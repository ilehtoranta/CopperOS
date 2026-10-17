"""Audit Search's strict native DOS contracts against raw requests and receipts.

The contracts and ordering checks are retained from report-corrected.py; this
auditor is reusable without that historical report's policy/image assertions.
"""
import hashlib
import json
from pathlib import Path
import struct


def read(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))
def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def verify(stage, batch, cpu):
    stage = Path(stage)
    directory = stage/'runs'/(batch+'-'+cpu)
    definitions = read(stage/('cases-'+batch+'.json'))
    report = read(directory/'observations.json')
    result = report['result']
    assert report['completed'] and result['StopReason'] == 'ProgramReturned'
    assert result['LaunchStatus'] == result['ReturnValue'] == 0
    assert report['cpu'] == cpu and result['CpuModel'] == 'M'+cpu
    assert result['ExecutionMode'] == 'Interpreter' and result['Frames'] <= 3000
    assert result['DiskSnapshotError'] is None
    assert result['HunkSha256'].lower() == digest(stage/'guest/SearchQualification')
    for name, key in [('Search.baseline', 'SEARCHB'), ('Search.candidate', 'SEARCHC'), ('TEXT.IN', 'TEXT.IN'), ('LONG.IN', 'LONG.IN')]:
        assert report['inputs'][key] == digest(stage/'inputs'/name)
    fixture = (directory/'CASES.FIX').read_bytes()
    raw = (directory/'receipt.bin').read_bytes()
    rows = report['observations']
    assert len(rows) == len(definitions) == (30 if batch == 'part1' else 34)
    assert struct.unpack_from('>II', fixture) == (0x53514631, len(rows))
    assert len(fixture) == 16+1040*len(rows)
    assert struct.unpack_from('>II', raw) == (0x53515231, len(rows))
    assert hashlib.sha256(fixture).hexdigest() == report['inputs']['CASES.FIX']
    long_line = 'prefix '+'x'*8179+' Needle\n'
    strict_outputs = {
        'multiple-from': '   one.txt..\nNeedle\n   two.txt..\nNeedle\n',
        'all-directory': '     SearchQ (dir)\n        spaced name.txt..\n     2> Needle\n'
                         '        empty.txt..\n        long.txt..\n     1> '+long_line+
                         '        one.txt..\n     2> Needle\n          sub (dir)\n'
                         '             two.txt..\n     2> Needle\n',
        'unterminated-quote': 'argument line invalid or too long\n',
        'extra-argument': '   one.txt..\nobject not found\n',
        'ctrl-c': '     2> Needle\n***Break\n',
    }
    failures = []
    cursor = 8
    for index, (case, row) in enumerate(zip(definitions, rows)):
        request = struct.unpack_from('>5I', fixture, 16+index*1040)
        args = (case['Arguments']+'\n').encode('ascii')
        assert request == (index, case['Image'], case['Stack'], int(case['CtrlC']), len(args))
        assert fixture[36+index*1040:36+index*1040+len(args)] == args
        assert all(row[key] == case[key] for key in ['Name', 'Image', 'Stack', 'Arguments', 'CtrlC'])
        fields = struct.unpack_from('>28I', raw, cursor)
        length = fields[5]
        data = raw[cursor+112:cursor+112+length]
        assert len(data) == length
        assert fields[:3] == (index, row['Image'], row['Stack'])
        assert fields[3:5] == (row['returnCode'] & 0xffffffff, row['ioError'])
        assert fields[6:8] == (row['availableBefore'], row['availableAfter'])
        assert list(fields[8:18]) == row['before'] and list(fields[18:28]) == row['after']
        assert data.decode('latin1') == row['output'] and hashlib.sha256(data).hexdigest() == row['outputSha256']
        cursor += 112+length
        errors = []
        if (row['returnCode'], row['ioError']) != (case['expectedReturn'], case['expectedIoErr']): errors.append('return/IoErr')
        if fields[8:18] != fields[18:28] or not row['callerStateRestored']: errors.append('caller restoration')
        expected = strict_outputs.get(row['Name'], case['expectedOutput'])
        if expected is not None:
            if row['output'] != expected: errors.append('exact output')
        else:
            assert row['Name'] == 'wildcard-from'
            if sorted(row['output'].splitlines(keepends=True)) != sorted('Ram Disk:SearchQ/'+name+'\n' for name in ['spaced name.txt', 'long.txt', 'one.txt']): errors.append('wildcard rows')
        if case['phase'] == 'repeat' and fields[6] != fields[7]: errors.append('repeat AvailMem delta')
        if errors: failures.append(dict(index=index, name=row['Name'], image=row['Image'], errors=errors))
        if index % 2:
            a = rows[index-1]
            assert {a['Image'], row['Image']} == {0, 1}
            if any(a[key] != row[key] for key in ['Name', 'Arguments', 'Stack', 'CtrlC', 'returnCode', 'ioError', 'output', 'outputSha256']):
                failures.append(dict(index=index, name=row['Name'], errors=['baseline/candidate mismatch']))
    assert cursor == len(raw)
    return dict(batch=batch, cpu=cpu, passed=not failures, invocations=len(rows), pairs=len(rows)//2,
                failureCount=len(failures), failures=failures,
                memoryDeltas=[row['availableAfter']-row['availableBefore'] for row in rows], result=result)


if __name__ == '__main__':
    import sys
    stage, batch, cpu = Path(sys.argv[1]), sys.argv[2], sys.argv[3]
    summary = verify(stage, batch, cpu)
    (stage/'runs'/(batch+'-'+cpu)/'qualification.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
    print(cpu, batch, summary['passed'], summary['invocations'], summary['failures'])
    sys.exit(0 if summary['passed'] else 1)
