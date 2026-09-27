#!/usr/bin/env python3
"""Create a checksum-verified disposable WB 3.1 Execute probe ADF.

The original archive and image are read-only inputs.  This tool only rewrites
the already allocated FFS data blocks of S/Startup-Sequence and explicitly
requested replacement files in an output copy. It does not change directory
allocation. Keep derivative media outside the repository; the JSON receipt
records identities and hashes, not media.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parent))
import inventory  # noqa: E402


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def checksum(block: bytearray) -> None:
    struct.pack_into(">I", block, 20, 0)
    value = (-sum(struct.unpack(">128I", block))) & 0xFFFFFFFF
    struct.pack_into(">I", block, 20, value)


def data_blocks(adf: inventory.Adf, header_block: int) -> list[int]:
    pointers: list[int] = []
    current = header_block
    visited: set[int] = set()
    while current:
        if current in visited:
            raise ValueError("Startup-Sequence extension loop")
        visited.add(current)
        words = adf.header(current, (2,) if current == header_block else (16,), (-3,))
        count = words[2]
        pointers.extend(list(reversed(words[6:78]))[:count])
        current = words[126]
    if not pointers or len(pointers) != len(set(pointers)):
        raise ValueError("Startup-Sequence data blocks are invalid")
    return pointers


def load_image(archive: Path) -> tuple[str, bytes]:
    with zipfile.ZipFile(archive) as bundle:
        candidates = [entry for entry in bundle.infolist()
                      if entry.filename.lower().endswith(".adf")]
        if len(candidates) != 1:
            raise ValueError("Archive must contain exactly one ADF")
        return candidates[0].filename, bundle.read(candidates[0])


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", type=Path, required=True)
    parser.add_argument("--startup", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    parser.add_argument("--replace-file", nargs=2, action="append", default=[],
                        metavar=("GUEST_PATH", "LOCAL_FILE"),
                        help="Replace an existing file within its allocated capacity in the disposable image only")
    args = parser.parse_args()

    member, original = load_image(args.archive)
    adf = inventory.Adf(original)
    if not adf.fast:
        raise ValueError("Fixture writer only admits FFS images")
    startup = args.startup.read_bytes()
    if b"\0" in startup:
        raise ValueError("Startup probe must be plain text")
    tree = adf.walk()
    entry = tree.get("s/startup-sequence")
    if entry is None:
        raise ValueError("S/Startup-Sequence is missing")
    blocks = data_blocks(adf, entry["block"])
    capacity = len(blocks) * 512
    if len(startup) > capacity:
        raise ValueError(f"Startup probe is {len(startup)} bytes; capacity is {capacity}")

    image = bytearray(original)
    for index, block_number in enumerate(blocks):
        start = block_number * 512
        image[start:start + 512] = b"\0" * 512
        payload_start = index * 512
        payload = startup[payload_start:payload_start + 512]
        image[start:start + len(payload)] = payload

    header_start = entry["block"] * 512
    header = bytearray(image[header_start:header_start + 512])
    struct.pack_into(">I", header, 8, (len(startup) + 511) // 512)
    struct.pack_into(">I", header, 324, len(startup))
    checksum(header)
    image[header_start:header_start + 512] = header
    replacements = []
    touched = {entry["block"], *blocks}
    for guest_path, local_file in args.replace_file:
        key = guest_path.replace("\\", "/").lower()
        target = tree.get(key)
        if target is None or key == "s/startup-sequence":
            raise ValueError(f"Invalid replacement target: {guest_path}")
        target_blocks = data_blocks(adf, target["block"])
        if touched.intersection({target["block"], *target_blocks}):
            raise ValueError("Replacement allocations overlap")
        local_file = str(Path(local_file).resolve(strict=True))
        payload = Path(local_file).read_bytes()
        if not payload or len(payload) > len(target_blocks) * 512:
            raise ValueError(f"Replacement exceeds allocated capacity: {guest_path}")
        touched.update({target["block"], *target_blocks})
        for index, block_number in enumerate(target_blocks):
            chunk = payload[index * 512:(index + 1) * 512]
            image[block_number * 512:(block_number + 1) * 512] = chunk.ljust(512, b"\0")
        start = target["block"] * 512
        target_header = bytearray(image[start:start + 512])
        struct.pack_into(">I", target_header, 8, (len(payload) + 511) // 512)
        struct.pack_into(">I", target_header, 324, len(payload))
        checksum(target_header)
        image[start:start + 512] = target_header
        replacements.append({"guest_path": key, "local_file": local_file,
                             "sha256": sha256(payload), "bytes": len(payload)})
    verified = inventory.Adf(bytes(image))
    verified_entry = verified.walk().get("s/startup-sequence")
    if verified_entry is None or verified.read_file(verified_entry["block"]) != startup:
        raise ValueError("Output image did not retain exact startup probe")
    for replacement in replacements:
        target = verified.walk()[replacement["guest_path"]]
        if sha256(verified.read_file(target["block"])) != replacement["sha256"]:
            raise ValueError("Replacement failed exact read-back")

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(image)
    receipt = {
        "schema_version": 1,
        "kind": "disposable-wb31-execute-startup-probe",
        "reference_archive": str(args.archive),
        "archive_member": member,
        "reference_adf_sha256": sha256(original),
        "reference_file": "S/Startup-Sequence",
        "reference_header_block": entry["block"],
        "allocated_data_blocks": blocks,
        "original_capacity": capacity,
        "probe_sha256": sha256(startup),
        "probe_bytes": len(startup),
        "output_adf_sha256": sha256(bytes(image)),
        "output_path": str(args.output),
        "replacements": replacements,
        "limits": [
            "The output ADF is a disposable derivative, not reference evidence.",
            "Only startup and explicitly listed replacement file data/length/checksums change; this does not qualify installation paths or protection flags.",
        ],
    }
    args.receipt.parent.mkdir(parents=True, exist_ok=True)
    args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
