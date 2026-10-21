"""Inspect public Exec memory lists in a passive BootReplay RAM snapshot."""
import argparse
import json
import struct
from pathlib import Path


def inspect(directory):
    boot = json.loads((directory / 'boot.json').read_text())
    banks = [(0, (directory / 'chip.bin').read_bytes()),
             (0xC00000, (directory / 'slow.bin').read_bytes())]
    fast = (directory / 'fast.bin').read_bytes()
    if fast:
        banks.append((0x200000, fast))

    def read(address):
        for start, data in banks:
            if start <= address and address - start <= len(data) - 4:
                return struct.unpack_from('>I', data, address - start)[0]
        raise ValueError(f'Unmapped snapshot address {address:#x}')

    control = read(0x880)
    process = read(control + 16)
    cli = read(process + 172) << 2
    head = read(4) + 322
    current = read(head)
    headers, seen = [], set()
    while current != head + 4:
        if not current or current in seen:
            raise ValueError('Invalid public MemHeader list')
        seen.add(current)
        lower, upper = read(current + 20), read(current + 24)
        chunk = read(current + 16)
        chunks, chunk_seen = [], set()
        while chunk:
            if chunk in chunk_seen:
                raise ValueError('Cyclic public MemChunk list')
            chunk_seen.add(chunk)
            size = read(chunk + 4)
            if not lower <= chunk < upper or size > upper - chunk:
                raise ValueError('MemChunk outside its MemHeader')
            chunks.append(dict(address=chunk, bytes=size))
            chunk = read(chunk)
        total = sum(c['bytes'] for c in chunks)
        headers.append(dict(header=current, lower=lower, upper=upper,
                            free=read(current + 28),
                            largest=max((c['bytes'] for c in chunks), default=0),
                            chunks=chunks, freeChunkBytes=total,
                            publicFreeMatchesChunks=total == read(current + 28)))
        current = read(current)
    return dict(frame=boot['frame'], machine=boot['memory'],
                phase=boot['phase'], bootError=boot['bootError'],
                cliReturnCode=read(cli + 12), cliResult2=read(cli),
                memory=headers)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--boot', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    args.output.write_text(json.dumps(inspect(args.boot), indent=2))
