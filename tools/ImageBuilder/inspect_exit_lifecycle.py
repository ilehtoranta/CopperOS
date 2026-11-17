"""Inspect the guest-written exit-callback observation in an ADF or HDF."""
import argparse
import json
import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def decode(data):
    """Validate the synchronized test and return its independent observations."""
    if data is None or len(data) != 36:
        raise ValueError('Missing or malformed lifecycle observation')
    flags, number, _, _, _, result, mask, task, _ = struct.unpack('>9I', data)
    if flags & ~15 or (flags & 13) != 13 or not number or result != 7 or not task:
        raise ValueError('Exit callback or process retirement did not satisfy the probe')
    if not mask or mask & (mask - 1):
        raise ValueError('Exit callback did not allocate a single signal')
    return {'cliNumber': number, 'callbackResult': result,
            'callbackSignalMask': mask, 'callbackTask': task,
            'taskVisibleDuringCallback': bool(flags & 1),
            'cliVisibleDuringCallback': bool(flags & 2),
            'callbackInChild': bool(flags & 4),
            'cliAbsentAfterTaskRetirement': bool(flags & 8)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('image', type=Path, help='Guest result ADF or HDF')
    args = parser.parse_args()
    disk = Adf(args.image.read_bytes())
    entry = disk.walk().get('t/lifecycle.bin')
    data = disk.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    print(json.dumps(decode(data), indent=2))


if __name__ == '__main__':
    main()
