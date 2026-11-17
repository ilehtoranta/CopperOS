"""Compare the complete shared built-in corpus with a stock V40 Shell capture."""
import argparse
import hashlib
import json
import struct
from pathlib import Path

from observe_diagnostics31 import Adf
import shell_builtin_fixture as fixture


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--original', required=True, type=Path)
    parser.add_argument('--candidate', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    reference = json.loads((args.original / 'observations.json').read_text())
    candidate = json.loads((args.candidate / 'observations.json').read_text())
    qualification = json.loads((args.candidate / 'qualification.json').read_text())
    if not reference['originalShell'] or candidate['originalShell'] or not qualification['checksPassed']:
        raise ValueError('Require a stock-Shell capture and a passing candidate corpus')
    for report in (reference, candidate):
        if not report['builtinCorpus'] or report['builtinControls'] or report['input-sent.txt'] != 'SENT\n':
            raise ValueError('Require completed full-corpus observations')
    if reference['romSha256'] != candidate['romSha256'] or reference['winuaeSha256'] != candidate['winuaeSha256']:
        raise ValueError('Reference and candidate must use the same ROM and emulator')
    if qualification['inputImageSha256'] != candidate['inputImageSha256']:
        raise ValueError('Candidate qualification does not match its observation')
    expected = {name: list(struct.unpack('>ii', value)) if name in fixture.RESULTS
                else value.decode('latin1') if value is not None else None
                for name, value in fixture.expected().items()}
    extensions = {'bi-cls', 'bi-cls-reset', 't/builtin-envarc/bivar'}
    common = sorted(set(expected) - extensions)
    errors = []
    for name, value in expected.items():
        if name not in candidate or candidate[name] != value:
            errors.append('Candidate contract mismatch: ' + name)
    for name in common:
        if name not in reference or reference[name] != expected[name] or candidate[name] != reference[name]:
            errors.append('Original-Shell mismatch: ' + name)
    images = []
    for directory, report in ((args.original, reference), (args.candidate, candidate)):
        path = directory / 'probe.hdf'
        if sha(path) != report['fixtureImageSha256']:
            raise ValueError('Fixture image changed: ' + str(path))
        disk = Adf(path.read_bytes())
        entries = disk.walk()
        def read(name):
            entry = entries.get(name)
            return disk.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
        images.append({name: read(name) for name in fixture.GENERATED if name.startswith(('c/', 's/'))})
        if read('s/diagprobe') != fixture.script(include_extensions=not report['originalShell']):
            errors.append('Unexpected main corpus script: ' + str(directory))
        if report['originalShell']:
            if 'l/shell-seg' in entries or read('s/startup-sequence') != b'C:BIEnv\nC:KeyStart\n':
                errors.append('Original fixture overrides the ROM Shell')
            execute = read('c/execute')
            if execute is None or hashlib.sha256(execute).hexdigest() != report['executeReferenceSha256']:
                errors.append('Original Execute does not match its capture receipt')
    if any(value is None or value != images[1][name] for name, value in images[0].items()):
        errors.append('The reference and candidate helpers/subscripts differ')
    report = {'kind': 'shared-shell-original-builtin-comparison', 'passed': not errors,
              'commonChecks': len(common), 'extensionChecks': sorted(extensions),
              'originalObservationSha256': sha(args.original / 'observations.json'),
              'candidateObservationSha256': sha(args.candidate / 'observations.json'),
              'originalFixtureSha256': reference['fixtureImageSha256'],
              'candidateFixtureSha256': candidate['fixtureImageSha256'],
              'errors': errors,
              'limits': 'Built-in corpus comparison; full Shell acceptance also requires the process/path/script matrices'}
    args.output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report))
    if errors:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
