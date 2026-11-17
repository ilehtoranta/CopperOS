"""Stage only the verified files of a native-built CopperOS system image."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf
from build_system_image import verify


def stage(image, receipt, system_root):
    data = image.read_bytes()
    record = json.loads(receipt.read_text(encoding='utf-8-sig'))
    if (record['kind'] != 'copperos-system-image' or record['shipping'] is not False or
            hashlib.sha256(data).hexdigest() != record['sha256'] or len(data) != record['bytes']):
        raise ValueError('System image does not match its native build receipt')
    verify(data, record['files'])
    disk = Adf(data)
    entries = disk.walk()
    pending = []
    try:
        # Validate every payload before replacing any staged file. Resolve each
        # destination below SYS, including any preexisting directory symlinks.
        root = system_root.resolve()
        for item in record['files']:
            relative = Path(item['path'])
            target = (root / relative).resolve()
            if relative.is_absolute() or not target.is_relative_to(root):
                raise ValueError('Staged path escapes SYS: ' + item['path'])
            payload = disk.read_file(entries[item['path'].lower()]['block'])
            if len(payload) != item['bytes'] or hashlib.sha256(payload).hexdigest() != item['sha256']:
                raise ValueError('Image file differs from its receipt: ' + item['path'])
            target.parent.mkdir(parents=True, exist_ok=True)
            fd, temporary = tempfile.mkstemp(prefix='.copperos-', suffix='.tmp', dir=target.parent)
            with os.fdopen(fd, 'wb') as stream:
                stream.write(payload)
            pending.append((Path(temporary), target))
        for temporary, target in pending:
            os.replace(temporary, target)
        return {'kind': 'copperos-system-staging', 'imageSha256': record['sha256'],
                'pairId': record['pairId'], 'files': record['files']}
    finally:
        for temporary, _ in pending:
            temporary.unlink(missing_ok=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', type=Path, required=True)
    parser.add_argument('--receipt', type=Path, required=True)
    parser.add_argument('--system-root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = stage(args.image, args.receipt, args.system_root)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print('Staged ' + str(len(result['files'])) + ' verified system files in ' + str(args.system_root))
