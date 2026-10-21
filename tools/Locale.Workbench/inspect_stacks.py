"""Observe public task stack bounds and written spans in passive replay dumps.

The first nonzero byte is an observation, not a worst-case stack proof: zero
writes and uninitialized allocations can hide or exaggerate historical use.
"""
import argparse
import json
import struct
from pathlib import Path


def inspect(directory):
    boot = json.loads((directory / 'boot.json').read_text())
    banks = [(base, (directory / name).read_bytes()) for name, base in
             [('chip.bin', 0), ('slow.bin', 0xC00000), ('fast.bin', 0x200000)]]

    def span(address, length):
        for base, data in banks:
            if base <= address and address - base + length <= len(data):
                return data[address - base:address - base + length]
        raise ValueError(f'Unmapped stack span {address:#x}+{length:#x}')

    def read(address):
        return struct.unpack('>I', span(address, 4))[0]

    rows = []
    for task in boot['tasks']:
        address = task['Address']
        lower, upper = read(address + 58), read(address + 62)
        row = dict(address=address, name=task['Name'], lower=lower, upper=upper,
                   savedSP=read(address + 54))
        if lower and upper > lower:
            data = span(lower, upper - lower)
            first = next((i for i, byte in enumerate(data) if byte), len(data))
            row.update(capacity=len(data), firstNonzeroOffset=first,
                       nonzeroSpan=len(data) - first)
        rows.append(row)
    return dict(frame=boot['frame'], memory=boot['memory'], tasks=rows,
                scope='Passive observations, not a worst-case stack bound')


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--boot', required=True, type=Path)
    p.add_argument('--output', required=True, type=Path)
    args = p.parse_args()
    args.output.write_text(json.dumps(inspect(args.boot), indent=2))
