"""Require Search's native auditor to reject false contracts and forged JSON."""
import hashlib
import json
from pathlib import Path
import sys
from verify import verify

stage = Path(sys.argv[1]).resolve()
cases = stage/'cases-part1.json'
observations = stage/'runs/part1-68000/observations.json'
original_cases, original_observations = cases.read_bytes(), observations.read_bytes()
records = []
try:
    for name, key, value in [('return-code', 'expectedReturn', 99), ('ioerr', 'expectedIoErr', 999), ('output-contract', 'expectedOutput', 'wrong\n')]:
        definitions = json.loads(original_cases)
        definitions[0][key] = value
        cases.write_text(json.dumps(definitions), encoding='utf-8')
        result = verify(stage, 'part1', '68000')
        assert not result['passed'] and result['failureCount'] > 0
        records.append(dict(control=name, rejected=True, failures=result['failures']))
        cases.write_bytes(original_cases)
    report = json.loads(original_observations)
    report['observations'][0]['output'] = 'forged\n'
    report['observations'][0]['outputSha256'] = hashlib.sha256(b'forged\n').hexdigest()
    observations.write_text(json.dumps(report), encoding='utf-8')
    rejected = False
    try:
        verify(stage, 'part1', '68000')
    except AssertionError:
        rejected = True
    assert rejected
    records.append(dict(control='json-output-does-not-match-binary', rejected=True))
finally:
    cases.write_bytes(original_cases)
    observations.write_bytes(original_observations)
assert verify(stage, 'part1', '68000')['passed']
(stage/'negative-controls.json').write_text(json.dumps(records, indent=2)+'\n', encoding='utf-8')
print('Four Search auditor controls rejected; original evidence restored byte for byte.')
