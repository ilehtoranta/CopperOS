"""Run negative controls against a real captured Copy boot observation."""
import copy
import json
import sys
from pathlib import Path
from verify_copy_boot_transfer import verify


def controls(original):
    mutations = {
        'wrong-write-count': lambda r: next(e for e in r['observedDosCalls'] if e['Name'] == 'Write').update(ReturnedD0=24),
        'wrong-copy-result': lambda r: next(e for e in r['observedDosCalls'] if e['Name'] == 'RunCommand' and e['D1'] == next(x for x in r['observedDosCalls'] if x['Name'] == 'LoadSeg' and x['Text'] == 'C:Ed')['ReturnedD0']).update(ReturnedD0=20),
        'trace-overflow': lambda r: r.update(dosObservationOverflow=True),
        'return-collision': lambda r: r.update(returnObservationCollision=True),
        'missing-character': lambda r: r['observedDosCalls'].remove(next(e for e in reversed(r['observedDosCalls']) if e['Name'] == 'FGetC')),
        'wrong-transfer-payload': lambda r: next(e for e in r['observedDosCalls'] if e['Name'] == 'Write').update(PayloadHex='00'),
        'trailing-data': lambda r: next(e for e in r['observedDosCalls'] if e['Name'] == 'MatchFirst').update(FileSize=26),
    }
    verify(original)
    passed = []
    for name, mutate in mutations.items():
        candidate = copy.deepcopy(original)
        if name == 'missing-character':
            events = candidate['observedDosCalls']
            start = next(i for i, e in enumerate(events) if e['Name'] == 'LoadSeg' and e['Text'] == 'C:Type')
            opened = next(e for e in events[start:] if e['Name'] == 'Open' and e['Text'] == 'copy-target')
            events.remove(next(e for e in events[events.index(opened) + 1:] if e['Name'] == 'FGetC' and e['D1'] == opened['ReturnedD0']))
        else:
            mutate(candidate)
        try:
            verify(candidate)
        except ValueError:
            passed.append(name)
        else:
            raise AssertionError(f'Accepted corrupted evidence: {name}')
    return passed


if __name__ == '__main__':
    print(json.dumps({'rejectedCorruptions': controls(json.loads(Path(sys.argv[1]).read_text()))}, indent=2))
