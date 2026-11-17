"""Check a proof Shell's source provenance, native gate and linked service owners.

This is an architecture/artifact audit, not a substitute for guest execution.
"""
import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def audit(manifest_path, image_path):
    root = Path(__file__).resolve().parents[2]
    directory = manifest_path.parent
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    errors = []

    def check_file(path, expected):
        if not path.is_file() or sha(path) != expected:
            errors.append('Missing or changed input: ' + str(path))

    for source in manifest['sources']:
        check_file(root / source['path'], source['sha256'])
    for item in manifest['inputs']:
        check_file(directory / 'inputs' / item['file'], item['sha256'])
    for item in manifest['artifacts']:
        check_file(directory / item['path'], item['sha256'])
        check_file(directory / item['report'], item['reportSha256'])
    shell = next(item for item in manifest['artifacts'] if item['name'] == 'Shell-Seg')
    gate = shell['stackGate']
    check_file(directory / gate['input'], gate['inputSha256'])
    check_file(Path(__file__).with_name(gate['packager']), gate['packagerSha256'])
    report = json.loads((directory / shell['report']).read_text(encoding='utf-8-sig'))
    native = report['NativeCompatibility']
    if (not report['IsCompatible'] or report['RuntimeProfile'] != 'freestanding' or
            report['Cpu'] != 'm68000' or report['ManagedAllocationSites'] or
            native['ExceptionRegionCount'] or native['FatalMachineFaultSiteCount'] or
            native['RuntimeFeatures'] or native['RuntimeHelpers'] or native['ExternalNativeTargets']):
        errors.append('Strict native runtime gate failed')
    expected_exports = {'copperos.system.shell-entry', 'copperos.shell.entry',
                        'copperos.shell.child', 'copperos.shell.execute-begin',
                        'copperos.shell.execute-poll', 'copperos.shell.execute-park',
                        'copperos.shell.background', 'copperos.shell.new-process'}
    if set(report['IncludedExportNames']) != expected_exports:
        errors.append('Unexpected native export roots')
    map_path = directory / (shell['path'] + '.map')
    symbol_map = map_path.read_text()
    symbols = symbol_map.partition('\nSYMBOLS\n')[2]
    if not symbols.strip():
        errors.append('Native symbol inventory is missing')
    methods = sorted(set(re.findall(r'^\w+\s+\d+\s+(CopperStart\.[^\s<>]+)', symbols, re.M)))
    classes = sorted(set(method.split('::')[0] for method in methods))
    # These reviewed methods contain pointer memory operations, public Exec
    # allocation calls, or the public ExecBase.ThisTask read. The broad DOS
    # adapter also contains private services, which must never become reachable.
    allowed = {'CopperStart.Dos.CopperSharpNativeDosPlatform::' + name for name in
               ('FreeGuest', 'Copy', 'IsMapped', 'AllocateGuest', 'Clear', 'get_CurrentDosTask')}
    allowed.add('CopperStart.Exec.CopperSharpRomMemoryPlatform::Clear')
    unexpected = sorted(set(methods) - allowed)
    if unexpected:
        errors.append('Unexpected linked CopperStart services: ' + repr(unexpected))
    if not re.search(r'^ENTRY 00000000$', symbol_map, re.M):
        errors.append('Native HUNK entry is not zero')
    disk = Adf(image_path.read_bytes())
    entries = disk.walk()
    for name, item in [('l/shell-seg', shell), ('libs/mathtrans.library', next(
            item for item in manifest['artifacts'] if item['name'] == 'mathtrans.library'))]:
        entry = entries.get(name)
        if entry is None or hashlib.sha256(disk.read_file(entry['block'])).hexdigest() != item['sha256']:
            errors.append('Staged image file does not match native artifact: ' + name)
    return {'kind': 'shared-shell-public-interface-audit', 'passed': not errors,
            'sourceHashesMatchCurrent': not any('input:' in error for error in errors),
            'manifestSha256': sha(manifest_path), 'mapSha256': sha(map_path),
            'shellBytes': shell['bytes'], 'shellSha256': shell['sha256'],
            'imageSha256': sha(image_path), 'linkedCopperStartClasses': classes,
            'linkedCopperStartMethods': methods,
            'errors': errors,
            'limits': 'Source/artifact/link audit; guest runtime acceptance must be verified separately'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', required=True, type=Path)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    result = audit(args.manifest.resolve(strict=True), args.image.resolve(strict=True))
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))
    if not result['passed']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
