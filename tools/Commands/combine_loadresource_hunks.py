"""Combine independent LoadResource client/worker CODE hunks into one load file.

This is a structural packer for two separately compiled resident roots, not a
linker or an executable qualifier. Each input must contain exactly one plain
CODE HUNK, optionally followed by RELOC32 and SYMBOL records, then END. The
client remains HUNK 0; the worker becomes HUNK 1. Worker relocation targets
change from 0 to 1. CODE bytes, relocation offsets/addends, symbols, and all
PC-relative instructions remain unchanged. No cross-image references are added.
"""
from __future__ import annotations

import argparse
from dataclasses import dataclass
import hashlib
import json
from pathlib import Path
import re
import struct


HEADER, CODE, RELOC32, SYMBOL, END = 0x3F3, 0x3E9, 0x3EC, 0x3F0, 0x3F2
MAX_INPUT_BYTES = 32 * 1024 * 1024


class HunkError(ValueError):
    pass


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


@dataclass(frozen=True)
class SingleCodeHunk:
    code: bytes
    body: bytes
    relocation_offsets: tuple[int, ...]
    relocation_target_positions: tuple[int, ...]
    symbols: tuple[tuple[str, int], ...]


def parse_single_code(data: bytes) -> SingleCodeHunk:
    """Consume the entire narrow format; refuse layouts this packer cannot prove."""
    if len(data) < 40 or len(data) > MAX_INPUT_BYTES or len(data) % 4:
        raise HunkError("Invalid, truncated, unaligned or excessive input length")
    position = 0

    def word() -> int:
        nonlocal position
        if position > len(data) - 4:
            raise HunkError("Truncated HUNK word")
        value = struct.unpack_from(">I", data, position)[0]
        position += 4
        return value

    if tuple(word() for _ in range(5)) != (HEADER, 0, 1, 0, 0):
        raise HunkError("Requires one plain CODE hunk, no resident names or overlays")
    allocation = word()
    if allocation == 0 or allocation & 0xC0000000:
        raise HunkError("Empty allocation or unsupported allocation flags")
    if word() != CODE or word() != allocation:
        raise HunkError("Requires plain CODE with contents matching allocation size")
    size = allocation * 4
    if size > len(data) - position - 4:
        raise HunkError("Truncated CODE contents")
    code = data[position:position + size]
    position += size
    offsets: list[int] = []
    targets: list[int] = []
    occupied: set[int] = set()
    symbols: list[tuple[str, int]] = []
    symbol_names: set[str] = set()
    seen_relocations = seen_symbols = False
    while True:
        record = word()
        if record == RELOC32 and not seen_relocations and not seen_symbols:
            seen_relocations = True
            while (count := word()) != 0:
                targets.append(position)
                if word() != 0:
                    raise HunkError("Single-input relocation target must be hunk zero")
                if count > (len(data) - position) // 4:
                    raise HunkError("Truncated relocation group")
                for _ in range(count):
                    offset = word()
                    if offset % 2 or offset > size - 4:
                        raise HunkError("Unaligned or out-of-CODE relocation offset")
                    if any(byte in occupied for byte in range(offset, offset + 4)):
                        raise HunkError("Duplicate or overlapping relocation fields")
                    if struct.unpack_from(">I", code, offset)[0] >= size:
                        raise HunkError("Relocation addend points outside its input CODE")
                    occupied.update(range(offset, offset + 4))
                    offsets.append(offset)
        elif record == SYMBOL and not seen_symbols:
            seen_symbols = True
            while (length := word()) != 0:
                if length > (len(data) - position - 4) // 4:
                    raise HunkError("Truncated symbol name/value")
                raw = data[position:position + length * 4]
                position += length * 4
                name_bytes, _, padding = raw.partition(b"\0")
                if not name_bytes or any(c < 32 or c > 126 for c in name_bytes) or any(padding):
                    raise HunkError("Invalid symbol text or padding")
                name, address = name_bytes.decode("ascii"), word()
                if name in symbol_names or address >= size:
                    raise HunkError("Duplicate symbol or symbol outside CODE")
                symbols.append((name, address))
                symbol_names.add(name)
        elif record == END and position == len(data):
            return SingleCodeHunk(code, data[24:], tuple(offsets), tuple(targets), tuple(symbols))
        else:
            raise HunkError("Unsupported, repeated, out-of-order or trailing HUNK record")


def combine_hunks(client_data: bytes, worker_data: bytes) -> tuple[bytes, dict]:
    client, worker = parse_single_code(client_data), parse_single_code(worker_data)
    worker_body = bytearray(worker.body)
    for input_position in worker.relocation_target_positions:
        struct.pack_into(">I", worker_body, input_position - 24, 1)
    header = struct.pack(">7I", HEADER, 0, 2, 0, 1,
                         len(client.code) // 4, len(worker.code) // 4)
    combined = header + client.body + worker_body

    # Recover both original files from the output and reverse ONLY the target
    # renumbering. This guards accidental changes to instructions or metadata.
    restored_client = client_data[:24] + combined[28:28 + len(client.body)]
    restored_worker_body = bytearray(combined[28 + len(client.body):])
    for input_position in worker.relocation_target_positions:
        struct.pack_into(">I", restored_worker_body, input_position - 24, 0)
    restored_worker = worker_data[:24] + restored_worker_body
    if restored_client != client_data or restored_worker != worker_data:
        raise HunkError("Internal byte-preservation check failed")

    return combined, {
        "schema_version": 1,
        "status": "combined",
        "layout": "client CODE hunk 0; independent worker CODE hunk 1",
        "output_sha256": digest(combined),
        "output_bytes": len(combined),
        "inputs": [
            {"role": role, "sha256": digest(data), "bytes": len(data),
             "output_hunk": number, "code_bytes": len(hunk.code),
             "code_sha256": digest(hunk.code), "relocations": len(hunk.relocation_offsets),
             "symbols": len(hunk.symbols)}
            for role, number, data, hunk in
            (("client", 0, client_data, client), ("worker", 1, worker_data, worker))
        ],
        "worker_relocation_groups_remapped": len(worker.relocation_target_positions),
        "code_bytes_preserved": True,
        "relocation_offsets_and_addends_preserved": True,
        "symbols_preserved": True,
        "cross_image_relocations_added": False,
        "shipping": False,
        "pure_admission": False,
        "scope": "Structural packaging only. Does not verify CPU compatibility, process startup, detached SegList ownership, callback quiescence, guest behavior or worker image lifetime.",
    }


def combine_files(client: Path, worker: Path, output: Path, receipt: Path,
                  client_sha256: str, worker_sha256: str) -> dict:
    paths = [path.resolve() for path in (client, worker, output, receipt)]
    if len(set(paths)) != 4 or output.exists() or receipt.exists():
        raise HunkError("Inputs, new output and new receipt must be distinct; no overwrites")
    if client.samefile(worker):
        raise HunkError("Client and worker input files must not be aliases")
    data: list[bytes] = []
    for source, expected in ((client, client_sha256), (worker, worker_sha256)):
        if not re.fullmatch(r"[0-9a-f]{64}", expected):
            raise HunkError("Expected input SHA-256 must be 64 lowercase hex characters")
        if source.stat().st_size > MAX_INPUT_BYTES:
            raise HunkError("Input exceeds the bounded HUNK size")
        payload = source.read_bytes()
        if digest(payload) != expected:
            raise HunkError("Input hash changed or does not match selected compiler artifact")
        data.append(payload)
    combined, result = combine_hunks(*data)
    for info, source in zip(result["inputs"], paths[:2]):
        info["path"] = str(source)
    result.update(output=str(paths[2]), tool={
        "path": str(Path(__file__).resolve()), "sha256": digest(Path(__file__).read_bytes())})
    # Exclusive creation prevents an output race from overwriting evidence.
    # If receipt creation fails, the output alone remains unqualified.
    with output.open("xb") as stream:
        stream.write(combined)
    with receipt.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--client", type=Path, required=True)
    parser.add_argument("--client-sha256", required=True)
    parser.add_argument("--worker", type=Path, required=True)
    parser.add_argument("--worker-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    try:
        result = combine_files(args.client, args.worker, args.output, args.receipt,
                               args.client_sha256, args.worker_sha256)
    except (HunkError, OSError) as error:
        parser.exit(1, f"LoadResource packing rejected: {error}\n")
    print(json.dumps({key: result[key] for key in ("status", "output_sha256", "output_bytes")}))


if __name__ == "__main__":
    main()
