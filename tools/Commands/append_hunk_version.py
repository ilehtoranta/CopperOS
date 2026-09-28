"""Append an immutable version ID to the single-CODE HUNK subset we compile.

This is deliberately not a general executable editor. Only a plain one-segment
load file with optional RELOC32 then SYMBOL records is accepted. Every original
CODE byte, relocation record and symbol record survives unchanged. Only the two
length words change; new NUL-delimited metadata and zero alignment padding are
inserted after the original CODE bytes. No instruction or relocation is added.
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
MAX_FILE_BYTES = 32 * 1024 * 1024
VERSION = re.compile(r"\$VER: [A-Za-z0-9_.-]+ [0-9]+\.[0-9]+ \([0-9]{1,2}\.[0-9]{1,2}\.[0-9]{4}\)(?: [ -~]+)?\Z")


class HunkError(ValueError):
    pass


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


@dataclass(frozen=True)
class Hunk:
    code: bytes
    tail: bytes
    relocations: tuple[int, ...]
    symbols: tuple[tuple[str, int], ...]


def parse_hunk(data: bytes) -> Hunk:
    """Fully consume the supported subset; reject ambiguous or malformed input."""
    if len(data) < 40 or len(data) > MAX_FILE_BYTES or len(data) % 4:
        raise HunkError("Invalid, truncated or excessive load-file length")
    position = 0

    def word() -> int:
        nonlocal position
        if position + 4 > len(data):
            raise HunkError("Truncated HUNK word")
        value = struct.unpack_from(">I", data, position)[0]
        position += 4
        return value

    if tuple(word() for _ in range(5)) != (HEADER, 0, 1, 0, 0):
        raise HunkError("Requires one plain CODE hunk, no resident names or overlays")
    allocation = word()
    if allocation == 0 or allocation & 0xC0000000:
        raise HunkError("Invalid allocation length or unsupported memory flags")
    if word() != CODE or word() != allocation:
        raise HunkError("CODE length must equal the single allocation length")
    size = allocation * 4
    if size > len(data) - position - 4:
        raise HunkError("Truncated CODE payload")
    code = data[position:position + size]
    position += size
    tail = data[position:]
    relocations: list[int] = []
    occupied: set[int] = set()
    symbols: list[tuple[str, int]] = []
    names: set[str] = set()
    seen_relocations = seen_symbols = False
    while True:
        record = word()
        if record == RELOC32 and not seen_relocations and not seen_symbols:
            seen_relocations = True
            while (count := word()) != 0:
                if word() != 0 or count > (len(data) - position) // 4:
                    raise HunkError("Invalid relocation target or truncated group")
                for _ in range(count):
                    offset = word()
                    if offset % 2 or offset > size - 4:
                        raise HunkError("Unaligned or out-of-CODE relocation")
                    if any(byte in occupied for byte in range(offset, offset + 4)):
                        raise HunkError("Duplicate or overlapping relocation")
                    # This tool does not support relocation addends aimed outside
                    # the original CODE allocation (including appended metadata).
                    if struct.unpack_from(">I", code, offset)[0] >= size:
                        raise HunkError("Relocation addend outside original CODE")
                    occupied.update(range(offset, offset + 4))
                    relocations.append(offset)
        elif record == SYMBOL and not seen_symbols:
            seen_symbols = True
            while (length := word()) != 0:
                if length > (len(data) - position - 4) // 4:
                    raise HunkError("Truncated symbol name or value")
                raw_name = data[position:position + length * 4]
                position += length * 4
                text, separator, padding = raw_name.partition(b"\0")
                if not text or any(c < 32 or c > 126 for c in text) or any(padding):
                    raise HunkError("Invalid symbol text or padding")
                name, address = text.decode("ascii"), word()
                if name in names or address >= size:
                    raise HunkError("Duplicate symbol or address outside CODE")
                names.add(name)
                symbols.append((name, address))
        elif record == END and position == len(data):
            return Hunk(code, tail, tuple(relocations), tuple(symbols))
        else:
            raise HunkError("Unsupported, repeated, out-of-order or trailing HUNK record")


def append_version(data: bytes, version: str) -> tuple[bytes, dict]:
    original = parse_hunk(data)
    if not VERSION.fullmatch(version) or version.count("$VER:") != 1 or len(version) > 255:
        raise HunkError("Expected one printable ASCII command version ID, at most 255 characters")
    if b"$VER:" in data:
        raise HunkError("Refusing an input that already contains a version ID")
    metadata = b"\0" + version.encode("ascii") + b"\0"
    metadata += bytes((-len(metadata)) % 4)
    length = (len(original.code) + len(metadata)) // 4
    output = bytearray(data[:32])
    struct.pack_into(">I", output, 20, length)
    struct.pack_into(">I", output, 28, length)
    output += original.code + metadata + original.tail
    output = bytes(output)
    result = parse_hunk(output)
    if (result.code != original.code + metadata or result.tail != original.tail or
            result.relocations != original.relocations or result.symbols != original.symbols):
        raise HunkError("Internal preservation check failed")
    return output, {
        "schema_version": 1, "status": "version-appended",
        "input_sha256": digest(data), "input_bytes": len(data),
        "output_sha256": digest(output), "output_bytes": len(output),
        "version_id": version, "version_file_offset": 32 + len(original.code) + 1,
        "original_code_bytes": len(original.code), "appended_code_bytes": len(metadata),
        "original_code_sha256": digest(original.code), "preserved_tail_sha256": digest(original.tail),
        "relocations": len(result.relocations), "symbols": len(result.symbols),
        "original_code_preserved": True, "original_tail_preserved": True,
        "modified_original_file_ranges": [{"offset": 20, "bytes": 4}, {"offset": 28, "bytes": 4}],
        "shipping": False, "pure_admission": False,
        "scope": "Structural append/preservation only; no runtime, original-OS, stack or resident qualification.",
    }


def append_file(source: Path, output: Path, receipt: Path, version: str, expected_sha256: str) -> dict:
    paths = [path.resolve() for path in (source, output, receipt)]
    if len(set(paths)) != 3 or output.exists() or receipt.exists():
        raise HunkError("Input, new output and new receipt must be distinct; no overwrites")
    if not re.fullmatch(r"[0-9a-f]{64}", expected_sha256):
        raise HunkError("Expected input SHA-256 must be 64 lowercase hex characters")
    if source.stat().st_size > MAX_FILE_BYTES:
        raise HunkError("Input exceeds the bounded HUNK size")
    data = source.read_bytes()
    if digest(data) != expected_sha256:
        raise HunkError("Input hash changed or does not match selected compiler artifact")
    transformed, result = append_version(data, version)
    result.update(input=str(paths[0]), output=str(paths[1]),
                  tool={"path": str(Path(__file__).resolve()), "sha256": digest(Path(__file__).read_bytes())})
    # Exclusive creation protects historical artifacts even if a second caller
    # races the preflight. A partial operation remains visibly unqualified.
    with output.open("xb") as stream:
        stream.write(transformed)
    with receipt.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    return result


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--input-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    parser.add_argument("--version", required=True)
    args = parser.parse_args()
    try:
        result = append_file(args.input, args.output, args.receipt, args.version, args.input_sha256)
    except (HunkError, OSError) as error:
        parser.exit(1, f"Version append rejected: {error}\n")
    print(json.dumps({key: result[key] for key in ("status", "output_sha256", "output_bytes")}))


if __name__ == "__main__":
    main()
