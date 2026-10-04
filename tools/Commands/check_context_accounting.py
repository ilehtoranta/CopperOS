"""Check exact heap-context accounting, including deliberately invalid receipts."""
import argparse
import json
import re
import shutil
import subprocess
from pathlib import Path


def check(image, runner, destination):
    destination.mkdir(parents=True, exist_ok=False)
    source_map = Path(str(image) + '.map').read_text(encoding='utf-8-sig')
    match = re.search(r'RESIDENT-CONTEXT bytes=(\d+) placement=heap', source_map)
    if not match:
        raise ValueError('Input must use a heap-backed resident context.')
    size = int(match[1])
    rows = []
    for kind in ('valid', 'wrong-size', 'wrong-flags', 'missing-receipt'):
        candidate = destination / kind
        shutil.copy2(image, candidate)
        map_text = source_map
        if kind == 'wrong-size':
            map_text = map_text.replace(match[0], f'RESIDENT-CONTEXT bytes={size + 4} placement=heap')
        if kind != 'missing-receipt':
            Path(str(candidate) + '.map').write_text(map_text, encoding='utf-8')
        if kind == 'wrong-flags':
            data = bytearray(candidate.read_bytes())
            # HUNK header with one code hunk; locate MOVEQ #0,D1 immediately
            # before the compiler's first Exec.AllocMem sequence in the wrapper.
            if int.from_bytes(data[8:12], 'big') != 1 or data[24:28] != bytes.fromhex('000003e9'):
                raise ValueError('Expected a one-hunk fixture.')
            allocation = data.index(bytes.fromhex('4eaeff3a'), 32)
            flags = data.rfind(bytes.fromhex('7200'), 32, allocation)
            if flags < 0:
                raise ValueError('Cannot locate context allocation flags.')
            data[flags + 1] = 1
            candidate.write_bytes(data)
        for cpu in ('68000', '68020', '68040'):
            report = destination / f'{kind}.{cpu}.json'
            result = subprocess.run(['dotnet', str(runner), str(candidate), cpu, str(report),
                                     'copy-direct-native-entry-vector-fixture+command'], capture_output=True)
            receipt = json.loads(report.read_text(encoding='utf-8-sig'))
            passed = result.returncode == 0 if kind == 'valid' else result.returncode != 0
            if kind == 'valid':
                passed &= all(c['compilerInvocationContext'] == {'bytes': size, 'allocations': 1, 'frees': 1}
                              for c in receipt['cases'])
            rows.append({'variant': kind, 'cpu': cpu, 'passed': passed, 'failure': receipt.get('failure')})
            if not passed:
                raise RuntimeError(f'Context accounting accepted invalid ownership or rejected a valid context: {kind}/{cpu}')
    (destination / 'summary.json').write_text(json.dumps(rows, indent=2) + '\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('image', type=Path)
    parser.add_argument('runner', type=Path)
    parser.add_argument('destination', type=Path)
    args = parser.parse_args()
    check(args.image.resolve(), args.runner.resolve(), args.destination.resolve())
