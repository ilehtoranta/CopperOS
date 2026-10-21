"""Check each native image against its own immutable producer cohort.

The DOS client used to compile the Shell is distinct from the DOS provider
used to compile the ROM. No producer DLL is substituted in another role.
"""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for role in ('rom', 'shell', 'desktop'):
        parser.add_argument('--' + role + '-manifest', type=Path, required=True)
        parser.add_argument('--' + role + '-runtime', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    errors, roles, images = [], {}, {}
    for role, image_name in [('rom', 'CopperStart-DiskBoot.rom'),
                             ('shell', 'CopperShell'), ('desktop', 'CopperDesktop.hunk')]:
        manifest_path = getattr(args, role + '_manifest')
        runtime = getattr(args, role + '_runtime')
        manifest = json.loads(manifest_path.read_text())
        image = manifest_path.parent / image_name
        expected, actual = manifest['sha256'].upper(), sha(image)
        images[role] = dict(path=str(image.resolve()), expected=expected, actual=actual)
        if actual != expected:
            errors.append(role + ': native image hash mismatch')
        rows = []
        for item in manifest['inputs']:
            name = item['file']
            path = runtime / name
            if role == 'shell' and name == 'CopperStart.Dos.dll' and 'dosClient' in manifest:
                if manifest['dosClient']['serviceVersion'] != 2:
                    errors.append('shell: unsupported DOS client service ABI')
                path = manifest_path.parent / manifest['dosClient']['path']
            expected, actual = item['sha256'].upper(), sha(path)
            rows.append(dict(file=name, path=str(path.resolve()), expected=expected, actual=actual))
            if actual != expected:
                errors.append(role + ': producer hash mismatch: ' + name)
        roles[role] = rows
    result = dict(scope='Independent native ROM, Shell and desktop producer roles',
                  passed=not errors, errors=errors, images=images, roles=roles)
    args.output.write_text(json.dumps(result, indent=2))
    print(json.dumps(dict(passed=result['passed'], errors=errors,
                          producerChecks=sum(map(len, roles.values())))))
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
