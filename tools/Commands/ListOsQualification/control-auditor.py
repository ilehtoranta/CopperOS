"""Confirm that the native report auditor rejects wrong contracts and altered JSON."""
import hashlib
import json
import sys
from pathlib import Path
from verify import verify

stage = Path(sys.argv[1])
cases = stage / 'cases-smoke.json'
observations = stage / 'runs/smoke-68000/observations.json'
original_cases = cases.read_bytes()
original_observations = observations.read_bytes()
records = []
try:
    for label,key,value in [('return-code','expectedReturn',99),('ioerr','expectedIoErr',999),('row-contract','rows',[5])]:
        definitions = json.loads(original_cases)
        definitions[0][key] = value
        cases.write_text(json.dumps(definitions),encoding='utf-8')
        result = verify(stage,'smoke','68000')
        rejected = not result['passed'] and result['failureCount'] > 0
        assert rejected
        records.append(dict(control=label,rejected=rejected,failures=result['failures']))
        cases.write_bytes(original_cases)
    report = json.loads(original_observations)
    report['observations'][0]['output'] = 'forged\n'
    report['observations'][0]['outputSha256'] = hashlib.sha256(b'forged\n').hexdigest()
    observations.write_text(json.dumps(report),encoding='utf-8')
    rejected = False
    try:
        verify(stage,'smoke','68000')
    except AssertionError:
        rejected = True
    assert rejected
    records.append(dict(control='json-output-does-not-match-binary',rejected=True))
finally:
    cases.write_bytes(original_cases)
    observations.write_bytes(original_observations)
assert verify(stage,'smoke','68000')['passed']
(stage / 'negative-controls.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
print('Four auditor negative controls rejected; original evidence restored.')
