"""Build an explicitly unqualified Kickstart 3.1 HDF candidate.

This tool packages inputs for boot investigation. It does not establish that
the supplied Shell-Seg implements the Kickstart shell entry contract.
"""
import argparse
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

from build_system_image import Disk, verify


def sha(data):
    return hashlib.sha256(data).hexdigest()


def project_names(repo):
    names = []
    for project in sorted((repo / 'src/Commands').glob('*/*.csproj')):
        root = ET.parse(project).getroot()
        names.extend(e.text.strip() for e in root.iter('CopperOSCommandName') if e.text and e.text.strip())
    if len(names) != 61 or len({n.casefold() for n in names}) != len(names):
        raise ValueError('Expected 61 distinct command project names')
    return sorted(names, key=str.casefold)


def read_hunk(path):
    data = path.read_bytes()
    if data[:4] != b'\0\0\x03\xf3':
        raise ValueError(f'Not a HUNK executable: {path}')
    return data


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--shell', required=True, type=Path)
    parser.add_argument('--bootstrap', required=True, type=Path)
    parser.add_argument('--commands', required=True, type=Path)
    parser.add_argument('--binary-version', default='1.0', help='Shell and bootstrap version')
    parser.add_argument('--binary-date', default='31.10.2026', help='Shell and bootstrap version date')
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--receipt', required=True, type=Path)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    names = project_names(repo)
    if any(name.casefold() == 'bootshell' for name in names):
        raise ValueError('Command inventory collides with C/BootShell')
    directory = args.commands.resolve(strict=True)
    files = [p for p in directory.iterdir() if p.is_file()]
    if {p.name for p in files} != set(names):
        raise ValueError('Command directory does not exactly match the project inventory')
    shell_path = args.shell.resolve(strict=True)
    shell_raw = read_hunk(shell_path)
    bootstrap_path = args.bootstrap.resolve(strict=True)
    bootstrap_raw = read_hunk(bootstrap_path)
    commands = {name: read_hunk(directory / name) for name in names}
    for name, data in commands.items():
        version = rb'\$VER: ' + re.escape(name.encode('ascii')) + rb' ([0-9]+\.[0-9]+) \(([0-9]{1,2}\.[0-9]{1,2}\.[0-9]{4})\)(?: [ -~]+)?\x00'
        matches = list(re.finditer(version, data))
        if len(matches) != 1:
            raise ValueError(f'Command lacks one matching version ID: {name}')
    version = args.binary_version
    date = args.binary_date
    sys.path.insert(0, str(repo / 'tools/Commands'))
    from append_hunk_version import append_version
    shell, shell_version = append_version(shell_raw, f'$VER: Shell-Seg {version} ({date}) CopperOS')
    bootstrap, bootstrap_version = append_version(bootstrap_raw, f'$VER: BootShell {version} ({date}) CopperOS')
    if args.output.resolve() == args.receipt.resolve() or args.output.exists() or args.receipt.exists():
        raise ValueError('Image and receipt must be distinct new files')
    disk = Disk(16384)
    dirs = {name: disk.directory(disk.root, name) for name in ('C', 'L', 'S')}
    disk.file(dirs['L'], 'L/Shell-Seg', shell)
    disk.file(dirs['C'], 'C/BootShell', bootstrap)
    for name, data in commands.items():
        disk.file(dirs['C'], 'C/' + name, data)
    disk.file(dirs['S'], 'S/Startup-Sequence', b'; CopperOS Kickstart development candidate\nC:BootShell\n')
    identity = {'kind': 'kickstart31-command-hdf-candidate', 'shipping': False,
                'runtimeQualified': False, 'shellSha256': sha(shell), 'bootstrapSha256': sha(bootstrap),
                'shellVersion': shell_version, 'bootstrapVersion': bootstrap_version,
                'commands': {name: sha(data) for name, data in commands.items()}}
    disk.file(dirs['S'], 'S/CopperOS-Release.json', (json.dumps(identity, sort_keys=True, indent=2) + '\n').encode('ascii'))
    image = disk.finish()
    verification = verify(image, disk.files)
    if shell_path.read_bytes() != shell_raw or bootstrap_path.read_bytes() != bootstrap_raw or any((directory / n).read_bytes() != data for n, data in commands.items()):
        raise ValueError('Input changed during image composition')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.receipt.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('xb') as stream:
        stream.write(image)
    receipt = dict(identity, imageSha256=sha(image), imageBytes=len(image), filesystem='DOS1',
                   volume='CopperOS', files=disk.files, verification=verification,
                   mount={'mode': 'partition', 'blockBytes': 512, 'surfaces': 2,
                          'sectorsPerTrack': 32, 'reservedBlocks': 2, 'bootPriority': 0},
                   limitation='Shell entry and Kickstart boot are not qualified by image construction')
    with args.receipt.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(receipt, stream, indent=2)
        stream.write('\n')
    print(f'Built unqualified candidate {args.output} SHA-256 {sha(image)}')


if __name__ == '__main__':
    main()
