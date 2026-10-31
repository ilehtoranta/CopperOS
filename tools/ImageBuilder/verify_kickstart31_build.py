"""Independently check an unmodified development HDF and its build receipt."""
import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

from build_system_image import verify
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def sha(data): return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build-directory', required=True, type=Path)
    parser.add_argument('--report', required=True, type=Path)
    args = parser.parse_args()
    root = args.build_directory.resolve(strict=True)
    image = (root / 'CopperOS.hdf').read_bytes()
    receipt_bytes = (root / 'image.json').read_bytes()
    receipt = json.loads(receipt_bytes.decode('utf-8-sig'))
    build = json.loads((root / 'build.json').read_text(encoding='utf-8-sig'))
    if args.report.exists(): raise ValueError('Choose a fresh verification report path')
    if build['shipping'] is not False or build['runtimeQualified'] is not False:
        raise ValueError('Input is not an unqualified development build')
    if sha(image) != build['imageSha256'] or sha(image) != receipt['imageSha256']:
        raise ValueError('Image hash differs from its receipts')
    if sha(receipt_bytes) != build['imageReceiptSha256']:
        raise ValueError('Image receipt hash differs from build receipt')
    if sha((root / 'shell-build/native-manifest.json').read_bytes()) != build['shellManifestSha256']:
        raise ValueError('Shell manifest hash differs from build receipt')
    for record in build['sourceInputs']:
        path = Path(record['path'])
        if sha(path.read_bytes()) != record['sha256']:
            raise ValueError('Recorded source changed: ' + str(path))
    checked = verify(image, receipt['files'])
    disk = Adf(image); tree = disk.walk()
    if any(key.endswith('.library') for key in tree): raise ValueError('Image contains a library binary')
    commands = receipt['commands']
    expected = {('c/' + name).lower() for name in commands} | {'c/bootshell'}
    actual = {key for key, item in tree.items() if key.startswith('c/') and item['secondary_type'] == -3}
    if len(commands) != 61 or actual != expected: raise ValueError('Command inventory mismatch')
    versions = {}
    for key in sorted(actual | {'l/shell-seg'}):
        item = tree[key]; data = disk.read_file(item['block']); name = item['name']
        pattern = (rb'\$VER: ' + re.escape(name.encode('ascii')) +
                   rb' [0-9]+\.[0-9]+ \([0-9]{1,2}\.[0-9]{1,2}\.[0-9]{4}\)(?: [ -~]+)?\x00')
        matches = list(re.finditer(pattern, data))
        if len(matches) != 1: raise ValueError('Missing or duplicate version ID: ' + item['path'])
        versions[item['path']] = matches[0].group()[:-1].decode('ascii')
        if key != 'c/bootshell' and key != 'l/shell-seg' and sha(data) != commands[name]:
            raise ValueError('Command hash differs from release metadata: ' + name)
    result = {'kind': 'kickstart31-build-readback', 'imageSha256': sha(image),
              'commandCount': len(commands), 'versionedBinaries': len(versions),
              'sourceInputsChecked': len(build['sourceInputs']), 'libraries': 0,
              'versions': versions, 'verification': checked,
              'limits': 'Readback and current input hashes only; runtime qualification is separate'}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    with args.report.open('x', encoding='utf-8', newline='\n') as stream:
        json.dump(result, stream, indent=2); stream.write('\n')
    print(json.dumps({k: result[k] for k in ('imageSha256', 'commandCount', 'versionedBinaries', 'sourceInputsChecked', 'libraries')}))


if __name__ == '__main__': main()
