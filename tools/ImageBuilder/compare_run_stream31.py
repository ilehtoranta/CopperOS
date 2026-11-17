"""Compare focused RUN stream observations made with the same V40 ROM."""
import argparse
import json
import re
from pathlib import Path


def compare(reference, candidate):
    if not reference.get('originalShell') or candidate.get('originalShell'):
        raise ValueError('Expected an original Shell reference and a candidate')
    for key in ('romSha256', 'winuaeSha256'):
        if not reference.get(key) or reference[key] != candidate.get(key):
            raise ValueError('Reference and candidate differ in ' + key)
    for key in ('systemOutput', 'nilRedirection', 'noLeadingRedirection', 'outerRedirection', 'nested', 'consoleInput'):
        if bool(reference.get(key)) != bool(candidate.get(key)):
            raise ValueError('Reference and candidate use different ' + key)
    system = bool(reference.get('systemOutput'))
    nil = bool(reference.get('nilRedirection'))
    if reference.get('outerRedirection'):
        raise ValueError('Outer redirection is not covered by this comparison')
    for observation in (reference, candidate):
        stream = observation.get('streamreport')
        if not isinstance(stream, list) or len(stream) != 7:
            raise ValueError('Missing RUN child stream report')
        if stream[:3] != [6, 0, 0] or stream[6] != 0 or observation.get('streamname') != '':
            raise ValueError('Child write or stream identity differs from reference')
        if (system and not observation.get('consoleInput')) or nil:
            if stream[4:6] != [0, 0]:
                raise ValueError('Expected a noninteractive NIL-style child stream')
        elif stream[4] != -1 or stream[5] == 0:
            raise ValueError('Expected an interactive child stream')
        if observation.get('input-sent.txt') != 'SENT\n':
            raise ValueError('Input injector did not finish')
        if system:
            shared_pattern = (r'\[CLI [1-9][0-9]*\]\nPARENT\n'
                              if observation.get('noLeadingRedirection') else r'PARENT\n')
            if (observation.get('systemdone') != 'OK\n' or
                    not re.fullmatch(shared_pattern, observation.get('runshared') or '')):
                raise ValueError('Caller output ownership differs from reference')
        elif observation.get('parentdone') != 'done\n':
            raise ValueError('Launching Shell did not finish')
        if not nil and not observation.get('noLeadingRedirection'):
            if not re.fullmatch(r'\[CLI [1-9][0-9]*\]\n', observation.get('runprefix') or ''):
                raise ValueError('Leading output contains more than the launcher notice')
    return {'kind': 'kickstart31-run-stream-comparison', 'matched': True,
            'systemOutput': system, 'nilRedirection': nil, 'nested': bool(reference.get('nested')),
            'consoleInput': bool(reference.get('consoleInput')),
            'romSha256': reference['romSha256'], 'winuaeSha256': reference['winuaeSha256'],
            'limits': 'Focused stream behavior; full Shell compatibility is not established'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reference', type=Path, required=True)
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = compare(json.loads(args.reference.read_text()), json.loads(args.candidate.read_text()))
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
