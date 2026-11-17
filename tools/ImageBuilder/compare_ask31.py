"""Compare original and candidate ASK observations from the same V40 ROM."""
import argparse
import json
from pathlib import Path


def compare(reference, candidate):
    if reference.get('originalShell') is not True or candidate.get('originalShell') is not False:
        raise ValueError('Expected an original Shell observation followed by a candidate')
    if (reference.get('romSha256') != candidate.get('romSha256') or
            reference.get('winuaeSha256') != candidate.get('winuaeSha256')):
        raise ValueError('Original and candidate used different ROM or emulator inputs')
    if not reference.get('romSha256') or not reference.get('winuaeSha256'):
        raise ValueError('Missing ROM or emulator identity')
    keys = ('input-sent.txt', 'askdone', 'ask-yes-result', 'ask-no-result',
            'ask-retry-result', 'ask-yes-text', 'ask-no-text', 'ask-retry-text')
    for observed in (reference, candidate):
        if observed.get('input-sent.txt') != 'SENT\n' or observed.get('askdone') != 'done\n':
            raise ValueError('ASK observation did not complete')
        if any(not isinstance(observed.get(key), str) for key in keys[-3:]):
            raise ValueError('ASK prompt output is missing')
        for key in keys[2:5]:
            value = observed.get(key)
            if not isinstance(value, list) or len(value) != 2 or any(type(item) is not int for item in value):
                raise ValueError('ASK result is missing or malformed: ' + key)
    differences = {key: {'original': reference.get(key), 'candidate': candidate.get(key)}
                   for key in keys if reference.get(key) != candidate.get(key)}
    if differences:
        raise ValueError('ASK observations differ: ' + json.dumps(differences, sort_keys=True))
    return {'matched': True, 'romSha256': reference['romSha256'],
            'winuaeSha256': reference['winuaeSha256'], 'observations': {key: reference[key] for key in keys}}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('reference', type=Path)
    parser.add_argument('candidate', type=Path)
    args = parser.parse_args()
    print(json.dumps(compare(json.loads(args.reference.read_text()), json.loads(args.candidate.read_text())), indent=2))


if __name__ == '__main__':
    main()
