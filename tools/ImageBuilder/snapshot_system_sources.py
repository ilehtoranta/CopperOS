"""Capture a coherent source tree for the normal native system build."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

from snapshot_shell_toolchain import inventory as compiler_inventory, ROOT_FILES


def inventory(repo, toolchain):
    roots = {'CopperOS': repo, 'CopperStart': repo.parent/'CopperStart',
             'CopperFS': repo.parent/'CopperFS', 'CopperSharp68k': toolchain}
    trees = {
        'CopperOS': ['src/System/Shell', 'src/Commands/Common', 'src/Commands/Execute',
                     'src/Commands/List', 'src/Libraries/mathtrans.library',
                     'tools/SystemImage.NativeRoot', 'tools/MathTrans.LibraryBuilder', 'tools/ImageBuilder'],
        'CopperStart': ['src/CopperStart.Exec', 'src/CopperStart.Dos', 'external/CopperSFS/src/CopperSFS.Core'],
        'CopperFS': ['src/CopperSFS.Core', 'src/CopperFFS.Core', 'src/CopperFS.Amiga']}
    singles = {
        'CopperOS': ['CopperOS.Portable.props', 'src/Commands/Directory.Build.props',
                     'src/Commands/Directory.Build.targets', 'src/Commands/CodeSizePolicies.props',
                     'src/Commands/CommandVersions.props', 'tools/build_system.ps1', 'tools/Commands/append_hunk_version.py',
                     'tools/DiskBuilder/verify_image.py', 'tools/Commands/Inventory/inventory.py'],
        'CopperStart': ['src/CopperStart.Portable.props', 'external/CopperSFS/Directory.Build.props'],
        'CopperFS': []}
    files = {}
    for name, root in roots.items():
        if name == 'CopperSharp68k':
            selected = compiler_inventory(root)
        else:
            selected = [root/p for p in singles[name]]
            selected += [root/p for p in ROOT_FILES if (root/p).is_file()]
            for subtree in trees[name]:
                if not (root/subtree).is_dir():
                    raise ValueError('Missing source directory: '+str(root/subtree))
                for directory, children, names in os.walk(root/subtree):
                    children[:] = [n for n in children if n.lower() not in ('.git','bin','obj','artifacts','__pycache__')]
                    selected += [Path(directory)/n for n in names if n != '.git']
        for file in sorted(set(selected)):
            files[Path(name)/file.relative_to(root)] = file
    return files


def capture(repo, toolchain, output):
    if output.exists():
        raise ValueError('Use a fresh source snapshot directory')
    files = inventory(repo, toolchain)
    data = {relative: path.read_bytes() for relative,path in files.items()}
    if files != inventory(repo, toolchain) or any(path.read_bytes() != data[relative] for relative,path in files.items()):
        raise RuntimeError('Sources changed during snapshot capture; no snapshot written')
    records = []
    for relative, payload in data.items():
        target = output/relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(payload)
        if target.read_bytes() != payload:
            raise RuntimeError('Snapshot write mismatch: '+str(relative))
        records.append({'path':relative.as_posix(), 'source':str(files[relative]),
                        'bytes':len(payload), 'sha256':hashlib.sha256(payload).hexdigest()})
    result = {'kind':'copperos-system-source-snapshot', 'capturedUtc':datetime.now(timezone.utc).isoformat(), 'files':records}
    (output/'snapshot.json').write_text(json.dumps(result,indent=2)+'\n')
    # Check the frozen tool dependency graph before spending time compiling.
    subprocess.run([sys.executable, '-c',
        "import sys; sys.path.insert(0, sys.argv[1]); import stage_system_image, audit_shared_shell",
        str(output/'CopperOS/tools/ImageBuilder')], check=True, cwd=output/'CopperOS')
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, required=True)
    parser.add_argument('--toolchain', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = capture(args.repo.resolve(strict=True), args.toolchain.resolve(strict=True), args.output.resolve())
    print('Captured '+str(len(result['files']))+' double-read-checked system source files')
