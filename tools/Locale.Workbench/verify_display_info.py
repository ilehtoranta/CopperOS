"""Compare the public PAL OCS display records used by the original IPrefs.

MonitorInfo/MonitorSpec is outside this native slice. Pointer values are opaque;
their alias relationships, record data and private NAME mutations are checked.
"""
import argparse
import json
from pathlib import Path


def aliases(rows):
    seen = {}
    return [seen.setdefault(row['handle'], i) for i, row in enumerate(rows)]


def verify(candidate, original):
    errors = []
    for label, value in [('candidate', candidate), ('original', original)]:
        if value.get('complete') != 0x44495350 or value.get('count') != 44:
            errors.append(label + ': incomplete PAL display inventory')
        if any(not row['handle'] for row in value['rows']):
            errors.append(label + ': null handle for an enumerated mode')
    if len(candidate['rows']) != len(original['rows']):
        errors.append('Different enumeration lengths')
    if aliases(candidate['rows']) != aliases(original['rows']):
        errors.append('Default/physical monitor handle aliases differ')
    fields = ('id', 'dispLength', 'dimsLength', 'nameLength', 'availability', 'disp', 'dims', 'name')
    for a, b in zip(candidate['rows'], original['rows']):
        for field in fields:
            if a[field] != b[field]:
                errors.append(f'{b["id"]:08X}/{field}: original={b[field]!r}, candidate={a[field]!r}')
    for field in ('mutationResults', 'changedName'):
        if candidate[field] != original[field]:
            errors.append(field + ': display-name mutation differs')
    return dict(passed=not errors, scope='PAL OCS IDs, aliases, DISP/DIMS/NAME and private name mutation',
                unsupported=['MonitorInfo/MonitorSpec'], records=len(candidate['rows']), errors=errors)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('candidate', 'original', 'output'):
        p.add_argument('--' + name, type=Path, required=True)
    a = p.parse_args()
    result = verify(json.loads(a.candidate.read_text()), json.loads(a.original.read_text()))
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
