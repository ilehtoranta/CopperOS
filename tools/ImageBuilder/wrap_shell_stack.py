"""Prefix a single-hunk Shell command with an Exec V37 StackSwap entry.

The compiled entry is reached unchanged after the wrapper; all original
relocation sites and their addends move by the wrapper's aligned length.
"""
import argparse
import hashlib
import struct
from pathlib import Path


def u32(data, offset):
    if offset < 0 or offset + 4 > len(data):
        raise ValueError('Truncated HUNK')
    return struct.unpack_from('>I', data, offset)[0]


def put32(data, offset, value):
    struct.pack_into('>I', data, offset, value)


def wrap(hunk):
    if [u32(hunk, i) for i in (0, 4, 8, 12, 16, 24)] != [0x3f3, 0, 1, 0, 0, 0x3e9]:
        raise ValueError('Expected one HUNK_CODE segment')
    size = u32(hunk, 28) * 4
    if u32(hunk, 20) * 4 != size or size > len(hunk) - 32:
        raise ValueError('Invalid HUNK_CODE size')
    original = bytearray(hunk[32:32 + size])
    pos = 32 + size
    if u32(hunk, pos) != 0x3ec:
        raise ValueError('Expected HUNK_RELOC32')
    pos += 4
    offsets = []
    while (count := u32(hunk, pos)):
        if u32(hunk, pos + 4) != 0 or count > (len(hunk) - pos - 8) // 4:
            raise ValueError('Invalid relocation group')
        pos += 8
        offsets.extend(u32(hunk, pos + 4 * i) for i in range(count))
        pos += 4 * count
    if u32(hunk, pos + 4) != 0x3f2 or pos + 8 != len(hunk):
        raise ValueError('Expected HUNK_END')
    ordered = sorted(offsets)
    if any(off & 1 or off + 4 > size or u32(original, off) >= size or
           (i and off < ordered[i - 1] + 4) for i, off in enumerate(ordered)):
        raise ValueError('Invalid, overlapping or out-of-range relocation')

    # This is the same register-preserving 68000 StackSwap sequence used by
    # MuiNativeStackGate. The saved-register frame and swap descriptor remain
    # on the original task stack until the compiled entry returns.
    gate = bytearray()
    labels = {}
    branches = []
    targets = []

    def w(*xs):
        for value in xs:
            gate.extend(struct.pack('>H', value & 0xffff))

    def long(value):
        gate.extend(struct.pack('>I', value))

    def branch(opcode, label):
        w(opcode)
        branches.append((len(gate), label))
        w(0)

    def compiled_entry(opcode):
        w(opcode)
        targets.append(len(gate))
        long(0)

    # A loaded copy can identify the exact packaged image before executing it
    # as a child. The metadata is skipped by the entry branch and is not a
    # relocation target. Hash the compiler output, including its relocations.
    branch(0x6000, 'start')
    long(0x43534846)  # CSHF
    long(1)           # header version
    gate.extend(hashlib.sha256(hunk).digest())
    labels['start'] = len(gate)

    # Save all registers, reserve a StackSwapStruct plus allocation pointer,
    # and use the canonical ExecBase at address 4.
    w(0x48e7, 0xfffe, 0x4fef, 0xfff0, 0x2c78, 4)
    w(0x0c6e, 37, 20); branch(0x6500, 'fail')  # Exec V37
    w(0x7000, 0x7200, 0x4eae, -144, 0x0240, 0x2700); branch(0x6600, 'fail')
    w(0x206e, 276, 0x2008); branch(0x6700, 'fail')  # ThisTask
    w(0x200f, 0xb0a8, 58); branch(0x6500, 'fail')  # stack lower bound
    w(0x90a8, 58, 0x0c80); long(32768); branch(0x6400, 'fast')
    w(0x203c); long(131072)  # 128 KiB of public temporary stack
    w(0x7201, 0x4eae, -198, 0x2f40, 12, 0x4a80); branch(0x6700, 'fail')
    w(0x2a4f, 0x2a80, 0x0680); long(131072)
    w(0x2b40, 4, 0x0480); long(16)
    w(0x2b40, 8, 0x2040, 0x208d, 0x204d, 0x4eae, -732)  # StackSwap
    w(0x4ced, 0x7fff, 16); compiled_entry(0x4eb9)  # restore arguments, JSR
    w(0x2a57, 0x2b40, 16, 0x204d, 0x2c78, 4, 0x4eae, -732)  # restore stack
    w(0x226f, 12, 0x203c); long(131072)
    w(0x4eae, -210, 0x4fef, 16, 0x4cdf, 0x7fff, 0x4e75)  # FreeMem and return
    labels['fast'] = len(gate)
    w(0x4fef, 16, 0x4cdf, 0x7fff); compiled_entry(0x4ef9)  # tail JMP
    labels['fail'] = len(gate)
    w(0x4fef, 16, 0x4cdf, 0x7fff, 0x7014, 0x4e75)  # return 20
    while len(gate) % 4:
        w(0x4e71)
    for off, label in branches:
        struct.pack_into('>h', gate, off, labels[label] - off)
    for off in targets:
        put32(gate, off, len(gate))
    for off in offsets:
        put32(original, off, u32(original, off) + len(gate))
    code = gate + original
    relocs = targets + [off + len(gate) for off in offsets]
    header = struct.pack('>8I', 0x3f3, 0, 1, 0, 0, len(code) // 4, 0x3e9, len(code) // 4)
    return (header + code + struct.pack('>3I', 0x3ec, len(relocs), 0) +
            b''.join(struct.pack('>I', off) for off in relocs) +
            struct.pack('>2I', 0, 0x3f2))

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    args.output.write_bytes(wrap(args.input.read_bytes()))
