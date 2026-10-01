"""Independent readback of newly created, unbootable DOS1 FFS image files.

Uses the existing read-only inventory.Adf parser, never the image writer's
objects. ADF and raw filesystem HDF use 512-byte blocks, two reserved boot
blocks, ordinary files/directories and ASCII names. RDB/partitions, links, other
DOS variants and images above 512 MiB are rejected rather than approximated.

Format facts cross-checked against primary Linux AFFS sources (no code copied):
https://github.com/torvalds/linux/blob/master/fs/affs/amigaffs.h
https://github.com/torvalds/linux/blob/master/fs/affs/bitmap.c
https://github.com/torvalds/linux/blob/master/fs/affs/namei.c
"""
from __future__ import annotations

import hashlib
import importlib.util
from pathlib import Path
import re
import struct


INVENTORY_PATH = Path(__file__).resolve().parents[1] / "Commands/Inventory/inventory.py"
_spec = importlib.util.spec_from_file_location("copperos_disk_inventory", INVENTORY_PATH)
_inventory = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(_inventory)
Adf = _inventory.Adf

BLOCK_BYTES = 512
MAX_IMAGE_BYTES = 512 * 1024 * 1024
BITMAP_BITS = 127 * 32
SHA256 = re.compile(r"[0-9a-f]{64}\Z")


class ImageVerificationError(ValueError):
    """Image does not exactly satisfy the declared deterministic filesystem."""


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ImageVerificationError(message)


def component(name: str) -> None:
    require(isinstance(name, str) and 1 <= len(name) <= 30 and name not in (".", "..") and
            all(32 <= ord(c) <= 126 and c not in "/:\\" for c in name),
            "Expected a printable ASCII Amiga name of 1..30 bytes: " + repr(name))


def expected_tree(files: list[dict]) -> dict[str, dict]:
    require(isinstance(files, list), "Expected a list of file records")
    tree, folded = {}, {}

    def add(path, record):
        key = path.lower()
        require(key not in folded or folded[key] == path, "Case-colliding image path: " + path)
        require(path not in tree or tree[path]["kind"] == record["kind"] == "directory",
                "Duplicate file or file/directory collision: " + path)
        folded[key], tree[path] = path, record

    for record in files:
        require(isinstance(record, dict), "File record must be an object")
        path = record.get("installed_path")
        require(isinstance(path, str), "Missing installed_path")
        parts = path.split("/")
        for part in parts:
            component(part)
        require(type(record.get("bytes")) is int and 0 <= record["bytes"] <= MAX_IMAGE_BYTES,
                "Invalid file byte count: " + path)
        require(isinstance(record.get("sha256"), str) and SHA256.fullmatch(record["sha256"]),
                "Invalid file SHA-256: " + path)
        require(type(record.get("amiga_protection")) is int and 0 <= record["amiga_protection"] <= 255,
                "Invalid Amiga protection byte: " + path)
        for n in range(1, len(parts)):
            add("/".join(parts[:n]), {"kind": "directory"})
        add(path, {**record, "kind": "file"})
    return tree


def name_bucket(name: str) -> int:
    # DOS1 uppercases ASCII a-z only; DOS3 international case folding differs.
    value = len(name)
    for byte in name.encode("ascii"):
        upper = byte - 32 if 97 <= byte <= 122 else byte
        value = (value * 13 + upper) & 0x7ff
    return value % 72


def _verify(data: bytes, files: list[dict], volume: str) -> dict:
    require(1024 <= len(data) <= MAX_IMAGE_BYTES and len(data) % BLOCK_BYTES == 0,
            "Image must contain 512-byte blocks and be at most 512 MiB")
    require(data[:4] == b"DOS\1", "Only a raw DOS1 FFS filesystem is supported; no RDB or partition wrapper")
    # Newly built command/media containers are intentionally unbootable. A
    # standard boot checksum applies to boot code, which this builder excludes.
    require(data[4:8] == bytes(4) and not any(data[12:1024]), "Unexpected boot code or boot checksum")
    component(volume)
    wanted = expected_tree(files)
    disk = Adf(data)
    require(disk.volume == volume, "Volume spelling differs")
    require(struct.unpack_from(">I", data, 8)[0] == disk.root,
            "Explicit root-block pointer required")
    root_words = disk.header(disk.root, (2,), (1,))
    require(root_words[126] == 0, "Directory-cache extension is unsupported")
    for offset, label in ((105, "root modification"), (118, "volume modification"), (121, "volume creation")):
        require(root_words[offset:offset + 3] == (0, 0, 0), label + " date is not the Amiga epoch")

    walked = disk.walk()
    actual = {entry["path"]: entry for entry in walked.values()}
    require(set(actual) == set(wanted), "Exact file/directory membership or spelling differs")
    owners = {}

    def claim(number, owner):
        disk.block(number)  # Bounds checked independently of the bitmap.
        require(number not in owners, f"Block {number} is shared by {owners.get(number)} and {owner}")
        owners[number] = owner

    claim(disk.root, "root")
    for path, entry in actual.items():
        component(entry["name"])
        kind = wanted[path]["kind"]
        require(entry["secondary_type"] == (-3 if kind == "file" else 2), "Wrong entry type: " + path)
        claim(entry["block"], kind + ":" + path)
        words = disk.header(entry["block"], (2,), (entry["secondary_type"],))
        require(words[105:108] == (0, 0, 0), "Entry date is not the Amiga epoch: " + path)
        require(words[117] == words[118] == 0, "Unexpected link metadata: " + path)
        if kind == "directory":
            require(words[126] == 0 and words[80] == 0, "Directory extension or protection differs: " + path)

    # inventory validates chain ownership/cycles. Also check each chain is in
    # the name's actual DOS1 bucket; merely walking a misplaced entry is weaker
    # than proving that an ordinary filesystem lookup can find it.
    directories = [("", disk.root)] + [(p, e["block"]) for p, e in actual.items()
                                       if wanted[p]["kind"] == "directory"]
    for path, number in directories:
        words = disk.header(number, (2,), (1, 2))
        seen = set()
        for bucket, head in enumerate(words[6:78]):
            while head:
                require(head not in seen, "Repeated directory chain while checking hash buckets")
                seen.add(head)
                name = disk.name(head)
                component(name)
                require(name_bucket(name) == bucket, "Wrong directory hash bucket: " + path + "/" + name)
                head = disk.header(head, (2,), (2, -3))[124]

    verified_files = []
    for path in sorted(actual):
        if wanted[path]["kind"] != "file":
            continue
        entry, expected = actual[path], wanted[path]
        payload = disk.read_file(entry["block"])
        digest = hashlib.sha256(payload).hexdigest()
        words = disk.header(entry["block"], (2,), (-3,))
        require(len(payload) == expected["bytes"] and digest == expected["sha256"], "File bytes/hash differ: " + path)
        require(words[80] == expected["amiga_protection"], "Amiga protection differs: " + path)
        protection = words[80]
        current, file_data, extension_blocks = entry["block"], [], []
        while current:
            first = current == entry["block"]
            words = disk.header(current, (2,) if first else (16,), (-3,))
            if not first:
                claim(current, "file-extension:" + path)
                extension_blocks.append(current)
            count = words[2]
            require(count <= 72, "Excessive data table length")
            require(not any(words[6:78 - count]), "Inactive data table pointers: " + path)
            for number in reversed(words[78 - count:78]):
                claim(number, "file-data:" + path)
                file_data.append(number)
            current = words[126]
        verified_files.append({"installed_path": path, "sha256": digest, "bytes": len(payload),
                               "amiga_protection": protection,
                               "header_block": entry["block"], "data_blocks": len(file_data),
                               "extension_blocks": extension_blocks, "datestamp": [0, 0, 0]})

    blocks = len(data) // BLOCK_BYTES
    bitmap_count = (blocks - 2 + BITMAP_BITS - 1) // BITMAP_BITS
    require(root_words[78] == 0xffffffff, "Bitmap is not marked valid")
    bitmap_numbers, bitmap_extensions = [], []
    remaining = bitmap_count
    pointers, next_extension = root_words[79:104], root_words[104]
    while True:
        take = min(remaining, len(pointers))
        require(all(pointers[:take]) and not any(pointers[take:]), "Missing or surplus bitmap pointers")
        bitmap_numbers.extend(pointers[:take])
        remaining -= take
        if remaining == 0:
            require(next_extension == 0, "Surplus bitmap extension")
            break
        require(next_extension != 0, "Missing bitmap extension")
        claim(next_extension, "bitmap-extension")
        bitmap_extensions.append(next_extension)
        extension = struct.unpack(">128I", disk.block(next_extension))
        pointers, next_extension = extension[:127], extension[127]

    maps = []
    for number in bitmap_numbers:
        claim(number, "bitmap")
        words = struct.unpack(">128I", disk.block(number))
        require(sum(words) & 0xffffffff == 0, "Bitmap checksum mismatch")
        maps.append(words[1:])
    allocated = set()
    for index, words in enumerate(maps):
        for word_index, word in enumerate(words):
            for bit in range(32):
                number = 2 + index * BITMAP_BITS + word_index * 32 + bit
                free = bool(word & (1 << bit))
                if number >= blocks:
                    require(not free, "Bitmap marks an out-of-image block free")
                elif not free:
                    allocated.add(number)
    require(allocated == set(owners), "Bitmap allocation differs from exact reachable filesystem blocks")
    return {"schema_version": 1, "status": "passed", "filesystem": "DOS1", "block_bytes": BLOCK_BYTES,
            "volume": disk.volume, "root_block": disk.root, "image_bytes": len(data),
            "image_sha256": hashlib.sha256(data).hexdigest(), "files": verified_files,
            "directories": sorted(p for p in actual if wanted[p]["kind"] == "directory"),
            "epoch_datestamp": [0, 0, 0], "checked_headers": len(disk.checked_headers),
            "bitmap": {"blocks": bitmap_numbers, "extension_blocks": bitmap_extensions,
                       "allocated_blocks": len(allocated), "free_blocks": blocks - 2 - len(allocated),
                       "exact_reachable_allocation": True},
            "shipping_qualified": False, "pure_admitted": False, "bootable": False,
            "scope": "Independent exact tree/content/protection, header checksums, DOS1 lookup buckets, epoch dates, disjoint block ownership and complete bitmap allocation readback. Not OS execution, bootability, full runtime, release admission or PURE safety; free-space bytes and unused metadata padding are not certified.",
            "reader": {"path": str(Path(__file__).resolve()), "sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest()},
            "inventory_reader": {"path": str(INVENTORY_PATH), "sha256": hashlib.sha256(INVENTORY_PATH.read_bytes()).hexdigest()}}


def verify_image(path: Path, files: list[dict], volume: str) -> dict:
    """Read one complete new raw image; fail closed without modifying it."""
    path = Path(path).resolve(strict=True)
    require(path.is_file() and path.stat().st_size <= MAX_IMAGE_BYTES, "Missing image or image exceeds 512 MiB")
    try:
        result = _verify(path.read_bytes(), files, volume)
    except _inventory.EvidenceError as error:
        raise ImageVerificationError(str(error)) from error
    result["image_path"] = str(path)
    return result
