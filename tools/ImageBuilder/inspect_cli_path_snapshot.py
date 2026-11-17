"""Inspect the public CLI path lists of two Processes in a Chip RAM capture."""
import argparse
import hashlib
import json
import struct
from pathlib import Path


def inspect(data, task):
    def long(address):
        if address < 0 or address + 4 > len(data) or address & 1:
            raise ValueError(f'Unmapped or unaligned longword at {address:#x}')
        return struct.unpack_from('>I', data, address)[0]

    # Public V40 Process.pr_CLI, CommandLineInterface.cli_CommandDir and
    # PathLock fields; all three links are BPTRs.
    if task == 0:
        raise ValueError('Process address must be nonzero')
    cli = long(task + 172) << 2
    if cli == 0:
        raise ValueError(f'Process {task:#x} has no CLI')
    node = long(cli + 8) << 2
    entries, seen = [], set()
    while node:
        if node in seen:
            raise ValueError(f'Cycle in CLI path at {node:#x}')
        seen.add(node)
        next_node, lock = long(node), long(node + 4)
        entry = {'node': node, 'lockBptr': lock}
        address = lock << 2
        if lock and address + 20 <= len(data):
            # FileLock keys are filesystem-defined; matching values are
            # evidence to inspect, not a substitute for DOS SameLock().
            entry.update({'lockKey': long(address + 4),
                          'handler': long(address + 12),
                          'volumeBptr': long(address + 16)})
        entries.append(entry)
        node = next_node << 2
    return {'task': task, 'cli': cli, 'entries': entries}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--chip', type=Path, required=True)
    parser.add_argument('--parent-task', type=lambda value: int(value, 0), required=True)
    parser.add_argument('--child-task', type=lambda value: int(value, 0), required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    data = args.chip.read_bytes()
    result = {'chipSha256': hashlib.sha256(data).hexdigest(),
              'parent': inspect(data, args.parent_task),
              'child': inspect(data, args.child_task)}
    with args.output.open('x', encoding='utf-8') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
