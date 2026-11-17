"""Compare a shared-Shell Path observation with the original ROM Shell."""
import argparse
import hashlib
import json
from pathlib import Path


def compare(reference, candidate, launch):
    if reference.get('originalShell') is not True:
        raise ValueError('Reference is not an original-ROM Shell observation')
    if (reference.get('romSha256') != launch.get('romSha256') or
            reference.get('winuaeSha256') != launch.get('winuaeSha256')):
        raise ValueError('Reference and candidate used different ROM or emulator inputs')
    if (reference.get('input-sent.txt') != 'SENT\n' or
            reference.get('pathmanydone') != 'done\n' or not reference.get('pathmany')):
        raise ValueError('Original Shell observation did not complete')
    for name in ('input-sent.txt', 'pathmany', 'pathmanydone'):
        if candidate.get(name) != reference.get(name):
            raise ValueError('Path observation differs: ' + name)
    if candidate.get('shell-ended.txt') != 'ENDED\n':
        raise ValueError('Candidate Shell exit was not observed')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reference', required=True, type=Path)
    parser.add_argument('--candidate-directory', required=True, type=Path)
    args = parser.parse_args()
    reference_bytes = args.reference.resolve(strict=True).read_bytes()
    directory = args.candidate_directory.resolve(strict=True)
    candidate_bytes = (directory / 'observations.json').read_bytes()
    launch_bytes = (directory / 'run' / 'launch.json').read_bytes()
    reference, candidate, launch = (json.loads(value) for value in
                                     (reference_bytes, candidate_bytes, launch_bytes))
    compare(reference, candidate, launch)
    sha = lambda value: hashlib.sha256(value).hexdigest()
    report = {'referenceSha256': sha(reference_bytes), 'candidateSha256': sha(candidate_bytes),
              'launchSha256': sha(launch_bytes), 'romSha256': launch['romSha256'],
              'winuaeSha256': launch['winuaeSha256'], 'matchingFields':
              ['input-sent.txt', 'pathmany', 'pathmanydone'], 'runtimeQualified': False}
    (directory / 'comparison.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
