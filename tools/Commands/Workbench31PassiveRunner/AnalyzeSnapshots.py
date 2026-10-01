"""Decode only saved RAM/ROM bytes; never access a live emulated machine.

Offsets: CopperSharp68k/Sdk.Amiga/{Exec/ExecLayout.cs,DOS/DosLayout.cs}.
Public-structure presence is evidence of progress, not command readiness.
"""
import argparse
import hashlib
import json
from pathlib import Path


class Memory:
    def __init__(self, stem, rom):
        self.regions = [(0, stem.with_suffix('.chipram').read_bytes()),
                        (0xc00000, stem.with_suffix('.slowram').read_bytes()),
                        (0xf80000, rom)]

    def read(self, address, size):
        for base, data in self.regions:
            offset = address - base
            if 0 <= offset and offset + size <= len(data):
                return data[offset:offset + size]
        raise ValueError(f'unmapped snapshot span {address:08x}+{size}')

    def n(self, address, size=4):
        return int.from_bytes(self.read(address, size), 'big')

    def cstr(self, address):
        if not address:
            return None
        out = bytearray()
        for index in range(256):
            value = self.n(address + index, 1)
            if not value:
                return out.decode('latin-1')
            out.append(value)
        raise ValueError(f'unterminated CString {address:08x}')

    def bstr(self, bptr):
        if not bptr:
            return None
        address = bptr << 2
        return self.read(address + 1, self.n(address, 1)).decode('latin-1')

    def node(self, address):
        return dict(address=address, type=self.n(address + 8, 1), name=self.cstr(self.n(address + 10)))

    def list(self, address):
        rows, seen = [], set()
        current = self.n(address)
        while current != address + 4:
            if not current or current in seen or len(rows) >= 256:
                raise ValueError(f'incomplete/cyclic list at {address:08x}, next={current:08x}')
            seen.add(current)
            rows.append(self.node(current))
            current = self.n(current)
        return rows

    def task(self, address):
        node = self.node(address)
        node.update(state=self.n(address + 15, 1), signalWait=self.n(address + 22),
                    signalReceived=self.n(address + 26), stackPointer=self.n(address + 54))
        if node['type'] != 13:  # NT_PROCESS, SDK Exec.NodeType.Process
            return node
        process = dict(taskNumber=self.n(address + 140), result2=self.n(address + 148),
                       currentDirectoryBptr=self.n(address + 152), inputBptr=self.n(address + 156),
                       outputBptr=self.n(address + 160), cliBptr=self.n(address + 172),
                       segmentListBptr=self.n(address + 128), arguments=self.cstr(self.n(address + 204)))
        if process['cliBptr']:
            cli = process['cliBptr'] << 2
            process['cli'] = dict(address=cli, result2=self.n(cli), returnCode=self.n(cli + 12),
                                 commandName=self.bstr(self.n(cli + 16)), commandFile=self.bstr(self.n(cli + 36)),
                                 directoryName=self.bstr(self.n(cli + 4)), interactive=self.n(cli + 40),
                                 background=self.n(cli + 44), moduleBptr=self.n(cli + 60),
                                 standardInputBptr=self.n(cli + 28), currentInputBptr=self.n(cli + 32),
                                 currentOutputBptr=self.n(cli + 48), standardOutputBptr=self.n(cli + 56))
        node['process'] = process
        return node


def analyze(stem, rom):
    state = json.loads(stem.with_suffix('.json').read_text(encoding='utf-8-sig'))
    row = dict(frame=state['frame'], cycle=state['Cycle'], pc=state['pc'], issues=[])
    memory = Memory(stem, rom)
    def inspect(key, function):
        try:
            row[key] = function()
        except ValueError as error:
            row['issues'].append(dict(field=key, error=str(error)))
    row['execBase'] = memory.n(4)
    base = row['execBase']
    if not base or state['RomOverlayEnabled']:
        row['issues'].append(dict(field='execBase', error='not yet an established RAM ExecBase'))
        return row
    inspect('execVersion', lambda: [memory.n(base + 20, 2), memory.n(base + 22, 2)])
    inspect('libraries', lambda: [dict(node, version=memory.n(node['address'] + 20, 2), revision=memory.n(node['address'] + 22, 2)) for node in memory.list(base + 378)])
    inspect('thisTask', lambda: memory.task(memory.n(base + 276)))
    inspect('readyTasks', lambda: [memory.task(node['address']) for node in memory.list(base + 406)])
    inspect('waitingTasks', lambda: [memory.task(node['address']) for node in memory.list(base + 420)])
    dos = next((node for node in row.get('libraries', []) if node['name'] == 'dos.library'), None)
    if dos:
        def dos_root():
            root = memory.n(dos['address'] + 34)
            info_bptr = memory.n(root + 24)
            return dict(dosBase=dos['address'], root=root, infoBptr=info_bptr,
                        bootProcess=memory.n(root + 44), shellSegmentBptr=memory.n(root + 48),
                        residentListBptr=memory.n((info_bptr << 2)) if info_bptr else None)
        inspect('dos', dos_root)
    return row


parser = argparse.ArgumentParser()
parser.add_argument('snapshots', type=Path)
parser.add_argument('rom', type=Path)
parser.add_argument('output', type=Path)
args = parser.parse_args()
rom = args.rom.read_bytes()
observations = [analyze(path.with_suffix(''), rom) for path in sorted(args.snapshots.glob('frame-*.json'))]
process_observations = []
for row in observations:
    tasks = ([row['thisTask']] if 'thisTask' in row else []) + row.get('readyTasks', []) + row.get('waitingTasks', [])
    for task in tasks:
        if 'process' in task:
            process_observations.append(dict(frame=row['frame'], task=task))
result = dict(schemaVersion=1, status='passive-structure-observation-not-functional-readiness',
              authority='CopperSharp68k public SDK ExecLayout/DosLayout and Exec NodeType.Process=13',
              romSha256=hashlib.sha256(rom).hexdigest(),
              scriptSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
              snapshotCount=len(observations), dosRootObservationCount=sum('dos' in row for row in observations),
              processObservationCount=len(process_observations),
              cliObservationCount=sum('cli' in row['task']['process'] for row in process_observations),
              observations=observations)
args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps({key: value for key, value in result.items() if key != 'observations'}, indent=2))
print(json.dumps(observations[-1], indent=2))
