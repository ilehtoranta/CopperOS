"""Freeze the source dependencies needed by build_system_proof.ps1.

Use --CopperSharpRoot on the proof builder to select the snapshot. This keeps
independent compiler development from changing an in-progress Shell build.
"""
import argparse
import hashlib
import json
import os
from datetime import datetime, timezone
from pathlib import Path


DIRECTORIES = ('Compiler', 'Compiler.Cli', 'Targets.Amiga', 'Sdk', 'Sdk.Amiga',
               'Sdk.Amiga.Support', 'Runtime.Managed', 'Runtime.AmigaPal')
ROOT_FILES = ('Directory.Build.props', 'Directory.Build.targets',
              'Directory.Packages.props', 'NuGet.Config', 'nuget.config', 'global.json')


def inventory(root):
    files = {root / name for name in ROOT_FILES if (root / name).is_file()}
    for name in DIRECTORIES:
        if not (root / name).is_dir():
            raise ValueError('Missing dependency directory: ' + name)
        for directory, children, names in os.walk(root / name):
            children[:] = [name for name in children if name.lower() not in
                           ('.git', 'bin', 'obj', 'artifacts')]
            files.update(Path(directory) / name for name in names)
    return sorted(files)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    source = args.source.resolve(strict=True)
    output = args.output.resolve()
    if output.exists() or output == source or source in output.parents:
        raise ValueError('Use a fresh snapshot directory outside the source tree')
    files = inventory(source)
    data = {path.relative_to(source): path.read_bytes() for path in files}
    if files != inventory(source) or any(path.read_bytes() != data[path.relative_to(source)] for path in files):
        raise RuntimeError('Toolchain sources changed while being captured; no snapshot written')
    output.mkdir(parents=True)
    records = []
    for relative, value in data.items():
        target = output / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(value)
        if target.read_bytes() != value:
            raise RuntimeError('Snapshot write mismatch: ' + str(relative))
        records.append({'path': relative.as_posix(), 'bytes': len(value),
                        'sha256': hashlib.sha256(value).hexdigest()})
    receipt = {'kind': 'shell-toolchain-source-snapshot', 'source': str(source),
               'capturedUtc': datetime.now(timezone.utc).isoformat(), 'files': records}
    (output / 'snapshot.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({'snapshot': str(output), 'files': len(records),
                      'receiptSha256': hashlib.sha256((output / 'snapshot.json').read_bytes()).hexdigest()}))


if __name__ == '__main__':
    main()
