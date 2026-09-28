"""Canonicalize unused bitmap padding in a newly generated private DOS1 image.

amitools 0.8.1 can leave nonexistent-block bits set in the last bitmap block.
Linux AFFS clears this padding before counting free blocks/allocating:
https://github.com/torvalds/linux/blob/master/fs/affs/bitmap.c (affs_init_bitmap).
This is an explicit deterministic image policy, not a claim that all filesystems
with nonzero padding are unmountable. Only padding and its bitmap checksum change.
Callers must supply their new private image; this is not an existing-media repair
tool or release/PURE admission path. The independent reader runs afterwards.
"""
from __future__ import annotations

import hashlib
from pathlib import Path
import struct

from verify_image import Adf, BLOCK_BYTES, BITMAP_BITS, MAX_IMAGE_BYTES


class BitmapCanonicalizationError(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise BitmapCanonicalizationError(message)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def canonicalize_bitmap_padding(image: Path) -> dict:
    """Modify only off-image padding bits and checksum; verify every changed byte."""
    image = Path(image).resolve(strict=True)
    require(image.is_file() and 1024 <= image.stat().st_size <= MAX_IMAGE_BYTES,
            "Expected a new private image no larger than 512 MiB")
    original = image.read_bytes()
    require(1024 <= len(original) <= MAX_IMAGE_BYTES and len(original) % BLOCK_BYTES == 0 and
            original[:4] == b"DOS\1", "Expected a raw 512-byte-block DOS1 filesystem")
    try:
        disk = Adf(original)
        root = disk.header(disk.root, (2,), (1,))
        require(root[78] == 0xffffffff, "Invalid bitmap validity flag")
        blocks = len(original) // BLOCK_BYTES
        count = (blocks - 2 + BITMAP_BITS - 1) // BITMAP_BITS
        remaining, bitmap_blocks, extensions = count, [], []
        seen = {disk.root}

        def claim(number):
            disk.block(number)
            require(number not in seen, "Repeated or overlapping bitmap/root block")
            seen.add(number)

        pointers, next_extension = root[79:104], root[104]
        while True:
            take = min(remaining, len(pointers))
            require(all(pointers[:take]) and not any(pointers[take:]), "Missing or surplus bitmap pointer")
            for number in pointers[:take]:
                claim(number)
                bitmap_blocks.append(number)
            remaining -= take
            if remaining == 0:
                require(next_extension == 0, "Surplus bitmap extension")
                break
            require(next_extension != 0, "Missing bitmap extension")
            claim(next_extension)
            extensions.append(next_extension)
            words = struct.unpack(">128I", disk.block(next_extension))
            pointers, next_extension = words[:127], words[127]
        maps = []
        for number in bitmap_blocks:
            words = struct.unpack(">128I", disk.block(number))
            require(sum(words) & 0xffffffff == 0, "Bitmap checksum mismatch")
            maps.append(words)
    except ValueError as error:
        raise BitmapCanonicalizationError(str(error)) from error

    # Everything before the final map describes actual disk blocks. Only its
    # padding can change; bitmap extension blocks contain pointers, not bitmaps.
    valid_bits = blocks - 2 - (count - 1) * BITMAP_BITS
    last = list(maps[-1])
    cleared = 0
    permitted_word_fields = {0}
    for index in range(127):
        in_range = max(0, min(32, valid_bits - 32 * index))
        mask = (1 << in_range) - 1
        before = last[index + 1]
        after = before & mask
        require((before & mask) == (after & mask), "In-range allocation changed")
        cleared += (before ^ after).bit_count()
        if before != after:
            permitted_word_fields.add(index + 1)
        last[index + 1] = after
    require(0 < valid_bits <= BITMAP_BITS, "Invalid final bitmap coverage")
    changed_blocks = []
    expected = bytearray(original)
    if cleared:
        last[0] = 0
        last[0] = -sum(last) & 0xffffffff
        require(sum(last) & 0xffffffff == 0, "Canonical bitmap checksum failed")
        number = bitmap_blocks[-1]
        start = number * BLOCK_BYTES
        updated = struct.pack(">128I", *last)
        expected[start:start + BLOCK_BYTES] = updated
        changed_blocks.append(number)
        # Bound the mutation before writing. A checksum word may change, while
        # data bits belonging to the image must remain identical, even when a
        # bitmap word mixes the last real block and padding.
        require(all(maps[-1][field] == last[field] for field in range(128)
                    if field not in permitted_word_fields), "Unexpected bitmap word change")
        require(expected[:start] == original[:start] and expected[start+BLOCK_BYTES:] == original[start+BLOCK_BYTES:],
                "Unexpected change outside final bitmap block")
        with image.open("r+b") as stream:
            require(stream.read() == original, "Private image changed during canonicalization")
            stream.seek(start)
            require(stream.write(updated) == BLOCK_BYTES, "Short bitmap write")
            stream.flush()
    actual = image.read_bytes()
    require(actual == bytes(expected), "Canonical image readback differs")
    return {"schema_version": 1, "status": "canonicalized" if cleared else "already-canonical",
            "image_path": str(image), "image_bytes": len(original), "image_blocks": blocks,
            "before_sha256": sha(original), "after_sha256": sha(actual),
            "bitmap_blocks": bitmap_blocks, "bitmap_extension_blocks": extensions,
            "changed_blocks": changed_blocks, "cleared_padding_bits": cleared,
            "in_range_bits_preserved": True, "only_padding_and_bitmap_checksum_changed": True,
            "shipping_qualified": False, "pure_admitted": False,
            "tool": {"path": str(Path(__file__).resolve()), "sha256": sha(Path(__file__).read_bytes())},
            "scope": "Deterministic padding policy for a newly generated private DOS1 image. Valid-block allocation bits, pointers and all other image bytes are preserved; no file/tree/runtime/PURE or release admission."}
