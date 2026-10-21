"""Compare a settled native desktop raster with its passive bitmap snapshot.

Sprite zero has its own 16x16 low-resolution image; all other visible pixels
must come from the screen bitmap. This catches stale unused sprite channels
without requiring native CopperBench to look like vendor Workbench.
"""
import argparse
import json
import struct
from pathlib import Path
from verify_switch import framebuffer


def verify(directory):
    boot = json.loads((directory / 'boot.json').read_text())
    banks = [(start, (directory / name).read_bytes()) for start, name in
             [(0, 'chip.bin'), (0xc00000, 'slow.bin'), (0x200000, 'fast.bin')]]

    def span(address, length):
        for start, data in banks:
            offset = address - start
            if 0 <= offset <= len(data) - length:
                return data[offset:offset + length]
        raise ValueError(f'Unmapped snapshot span {address:#x}+{length}')

    def word(address): return struct.unpack('>H', span(address, 2))[0]
    def long(address): return struct.unpack('>I', span(address, 4))[0]
    library = next(x['Address'] for x in boot['libraries'] if x['Name'] == 'intuition.library')
    screen = long(library + 104)
    if not boot.get('nativeDesktopReady') or not screen:
        raise ValueError('Native desktop initialization did not complete')
    backdrop_flags = long(boot['nativeDesktopWindow'] + 24)
    width, height = word(screen + 12), word(screen + 14)
    bitmap = screen + 184
    stride, rows = word(bitmap), word(bitmap + 2)
    depth = span(bitmap + 5, 1)[0]
    planes = [span(long(bitmap + 8 + p * 4), stride * rows) for p in range(depth)]
    cpr = long(screen + 356)
    cursor = long(cpr + 4)
    copper_start = cursor
    palette, sprites = {}, {}
    diw = cop2 = 0
    for _ in range(512):
        register, value = word(cursor), word(cursor + 2)
        cursor += 4
        if register == 0xffff: break
        if register & 1: continue
        if register == 0x8e: diw = value
        if 0x180 <= register <= 0x1be: palette[(register - 0x180) // 2] = value
        if 0x120 <= register <= 0x13e: sprites[register] = value
        if register == 0x84: cop2 = cop2 & 0xffff | value << 16
        if register == 0x86: cop2 = cop2 & 0xffff0000 | value
        if register == 0x8a: cursor = cop2
    else:
        raise ValueError('Unterminated display Copper list')
    owner = long(library + 244)
    sprite = long(owner + 64) if owner else long(library + 224)
    position, control = word(sprite), word(sprite + 2)
    pointer_x = (((position & 255) << 1) | (control & 1)) * 2 + 2
    pointer_y = (position >> 8) | ((control & 4) << 6)
    origin_x, origin_y = (diw & 255) * 2, diw >> 8
    fw, fh, pixels = framebuffer(directory / 'screen.png')
    scale = 2 if width == 320 else 1
    mismatches, examples, pointer_changed = 0, [], 0
    for y in range(height):
        for x in range(width):
            pen = sum(((plane[y * stride + x // 8] >> (7 - x % 8)) & 1) << p
                      for p, plane in enumerate(planes))
            color = palette[pen]
            expected = bytes([((color >> shift) & 15) * 17 for shift in (8, 4, 0)])
            for sub in range(scale):
                px, py = origin_x + x * scale + sub, origin_y + y
                if not 0 <= px < fw or not 0 <= py < fh:
                    raise ValueError('Display extends beyond captured framebuffer')
                at = py * (fw * 3 + 1) + 1 + px * 3
                actual = pixels[at:at + 3]
                if pointer_x <= px < pointer_x + 32 and pointer_y <= py < pointer_y + 16:
                    pointer_changed += actual != expected
                elif actual != expected:
                    mismatches += 1
                    if len(examples) < 12:
                        examples.append(dict(x=x, y=y, actual=actual.hex(), expected=expected.hex()))
    empty = sprite + 68
    unused = [((sprites.get(0x120 + n * 4, 0) << 16) |
               sprites.get(0x122 + n * 4, 0)) == empty for n in range(1, 8)]
    empty_zero = span(empty, 4) == bytes(4)
    return dict(passed=mismatches == 0 and pointer_changed > 0 and all(unused) and empty_zero,
                frame=boot['frame'], screen=screen, width=width, height=height,
                copperStart=copper_start, unusedChannelsTerminated=unused,
                backdropFlags=backdrop_flags, backdropSimpleRefresh=bool(backdrop_flags & 0x40),
                emptyTerminatorZero=empty_zero,
                pointerVisiblePixels=pointer_changed, unexpectedRasterPixels=mismatches,
                examples=examples, scope='Settled bitmap raster, visible sprite zero and terminated unused sprite channels')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--boot', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = verify(args.boot)
    args.output.write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))
    raise SystemExit(0 if result['passed'] else 1)
