"""Compose a disposable diagnostic disk by replacing only L:Shell-Seg.

This is not a reproducible proof build: its unchanged files come from the
supplied image. It is useful for testing a separately compiled Shell while the
full image producer is running.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

from build_system_image import Disk, verify

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', type=Path, required=True)
    parser.add_argument('--shell', type=Path, required=True)
    parser.add_argument('--output-directory', type=Path, required=True)
    args = parser.parse_args()
    original = args.image.read_bytes()
    shell = args.shell.read_bytes()
    source = Adf(original)
    entries = source.walk()
    if 'l/shell-seg' not in entries:
        raise ValueError('Source has no L:Shell-Seg')
    disk = Disk()
    dirs = {}
    for key, entry in sorted(entries.items(), key=lambda item: (item[0].count('/'), item[0])):
        parts = entry['path'].split('/')
        owner = disk.root if len(parts) == 1 else dirs['/'.join(parts[:-1]).lower()]
        if entry['secondary_type'] == 2:
            dirs[key] = disk.directory(owner, entry['name'])
        elif entry['secondary_type'] == -3:
            contents = shell if key == 'l/shell-seg' else source.read_file(entry['block'])
            disk.file(owner, entry['path'], contents)
        else:
            raise ValueError('Unexpected entry: ' + key)
    image = disk.finish()
    verify(image, disk.files)
    args.output_directory.mkdir(parents=True, exist_ok=False)
    (args.output_directory / 'System.adf').write_bytes(image)
    sha = lambda value: hashlib.sha256(value).hexdigest()
    receipt = {'diagnosticOnly': True, 'inputImageSha256': sha(original),
               'shellSha256': sha(shell), 'outputImageSha256': sha(image)}
    (args.output_directory / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
