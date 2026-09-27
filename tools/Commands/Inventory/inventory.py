#!/usr/bin/env python3
"""Extract reference facts without executing or modifying reference programs.

Only hashes, names, metadata, version tags, candidate templates and narrowly
parsed installer/resident facts are exported. Original media and file contents
must remain outside the repository. Python 3.11+; standard library only.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import mmap
import os
from pathlib import Path
import re
import struct
import sys
import tempfile
from typing import Any
import urllib.request
import zipfile


ROOT = Path(__file__).resolve().parents[3]
DEFAULT_PLAN = ROOT / "Goals/WORKBENCH31_MORPHOS_C_COMMANDS_GOAL.md"
DEFAULT_OUTPUT = ROOT / "docs/Commands/Workbench31MorphOS320"
ISO_URL = "https://www.morphos-team.net/morphos-3.20.iso"
ISO_BYTES = 471126016
ISO_MD5 = "70b84b8c0bb1cf9b10b7062fe8809c85"
ISO_SHA256 = "3761e191124cfd204a490915f8d285d55969e95b66c5c3cd1cf854bc71dab911"
DOWNLOADS_URL = "https://www.morphos-team.net/downloads"
SCHEMA_VERSION = 1
INTERNAL_NAMES = (
    "Alias Ask CD Cls Echo Else EndCLI EndIf EndShell EndSkip Failat Fault "
    "Get Getenv If Lab NewCLI NewShell Path Prompt Quit Resident Run Set "
    "Setenv Skip Stack Unalias Unset Unsetenv Why"
).split()
WB_SOURCES = (
    {
        "id": "wb31-install-m10-40.42",
        "role": "Install", "marker": "I", "volume": "Install3.1",
        "archive": "Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 1 of 6)(Install)[!].zip",
        "zip_sha256": "e320dbbcb2b8e34da7d3e37a2953c623c26755191a74c2512169b2e6f81f2e78",
        "adf_sha256": "8f54e735925d733719a321a0ddabd8fd6d1c1c3d3c0a516ed6b77dee89ac9e1e",
        "command_count": 20,
        "capture_audit": "reference-captures/wb31-disk1-c-command-captures-20260923.json",
        "script_capture_audit": "reference-captures/wb31-disk1-install-script-captures-20260923.json",
        "scripts": ("Install/Install", "HDSetup/HDSetup", "Update/Startup-HardDrive"),
    },
    {
        "id": "wb31-workbench-m10-40.42",
        "role": "Workbench", "marker": "W", "volume": "Workbench3.1",
        "archive": "Workbench v3.1 rev 40.42 (1994)(Commodore)(M10)(Disk 2 of 6)(Workbench)[!].zip",
        "zip_sha256": "d93611887acf91f68f5608a5b7812f03eb16f743f40451b67c93067b04c967ed",
        "adf_sha256": "a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985",
        "command_count": 50,
        "capture_audit": "reference-captures/wb31-disk2-c-command-captures-20260923.json",
        "script_capture_audit": None,
        "scripts": ("S/Startup-Sequence",),
    },
)
MORPHOS_CAPTURE_AUDITS = {
    "avail": "reference-captures/avail-morphos-binary-audit-20260923.json",
    "conclip": "reference-captures/conclip-morphos-binary-audit-20260923.json",
    "cpu": "reference-captures/cpu-morphos-binary-audit-20260923.json",
    "date": "reference-captures/date-morphos-binary-audit-20260923.json",
    "reboot": "reference-captures/reboot-morphos-binary-audit-20260923.json",
    "showconfig": "reference-captures/showconfig-morphos-binary-audit-20260923.json",
    "stat": "reference-captures/stat-morphos-binary-audit-20260923.json",
    "status": "reference-captures/status-morphos-binary-audit-20260923.json",
    "time": "reference-captures/time-morphos-binary-audit-20260923.json",
    "uptime": "reference-captures/uptime-morphos-binary-audit-20260923.json",
    "waitx": "reference-captures/waitx-morphos-binary-audit-20260923.json",
}


class EvidenceError(ValueError):
    """A malformed source or unacknowledged reference change cannot be evidence."""


def require(condition: bool, message: str) -> None:
    if not condition:
        raise EvidenceError(message)


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def file_digest(path: Path, algorithm: str = "sha256") -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, algorithm).hexdigest()


def logical_id(name: str) -> str:
    # All admitted command identifiers are ASCII. Do not apply host path rules.
    require(name.isascii(), f"Non-ASCII command identity requires review: {name!r}")
    return name.lower()


def amiga_protection(value: int) -> dict[str, Any]:
    # Native AmigaDOS FIBB_* positions; R/W/E/D are denial bits.
    return {
        "raw": value, "hex": f"0x{value:08x}",
        "pure": bool(value & (1 << 5)), "script": bool(value & (1 << 6)),
        "archived": bool(value & (1 << 4)),
        "read_allowed": not bool(value & (1 << 3)),
        "write_allowed": not bool(value & (1 << 2)),
        "execute_allowed": not bool(value & (1 << 1)),
        "delete_allowed": not bool(value & 1),
        "uninterpreted_high_bits": f"0x{value & ~0x7f:08x}",
        "evidence": "adf-file-header-byte-320-big-endian",
    }


class Adf:
    """Read 512-byte OFS/FFS filesystem metadata and ordinary file contents.

    No disk mounting, ZIP extraction, host execution or writes. Linked entries
    are reported but not followed: a linked C entry requires explicit review.
    Header checksums, parent/hash chains and data/extension bounds are checked.
    """

    def __init__(self, data: bytes):
        require(len(data) >= 1024 and len(data) % 512 == 0, "Invalid ADF length")
        require(data[:3] == b"DOS", "ADF is not an AmigaDOS disk")
        require(data[3] in (0, 1, 2, 3), "Unsupported ADF filesystem variant")
        self.data = data
        self.fast = bool(data[3] & 1)
        self.root = struct.unpack_from(">I", data, 8)[0] or len(data) // 1024
        self.checked_headers: set[int] = set()
        root = self.header(self.root, (2,), (1,))
        require(root[3] == 72, "Unexpected ADF root hash-table size")
        self.volume = self.name(self.root)

    def block(self, number: int) -> bytes:
        require(2 <= number < len(self.data) // 512, f"ADF block out of range: {number}")
        return self.data[number * 512:(number + 1) * 512]

    def header(self, number: int, types: tuple[int, ...], secondary: tuple[int, ...]) -> tuple[int, ...]:
        words = struct.unpack(">128I", self.block(number))
        require(sum(words) & 0xffffffff == 0, f"ADF checksum mismatch at block {number}")
        sec = struct.unpack(">i", self.block(number)[508:512])[0]
        require(words[0] in types and sec in secondary, f"Unexpected ADF header type at {number}")
        self.checked_headers.add(number)
        return words

    def name(self, number: int) -> str:
        block = self.block(number)
        require(block[432] <= 30, f"Invalid ADF BSTR name length at {number}")
        return block[433:433 + block[432]].decode("latin-1")

    def directory(self, number: int) -> list[dict[str, Any]]:
        words = self.header(number, (2,), (1, 2))
        seen: set[int] = set()
        names: set[str] = set()
        result = []
        for head in words[6:78]:
            while head:
                require(head not in seen, f"ADF repeated/cyclic directory chain at {head}")
                seen.add(head)
                item = self.header(head, (2,), (2, 3, 4, -3, -4))
                require(item[1] == head, f"ADF header-key mismatch at {head}")
                require(item[125] == number, f"ADF parent mismatch at {head}")
                name = self.name(head)
                require(name.lower() not in names, f"Duplicate ADF directory name: {name}")
                names.add(name.lower())
                sec = struct.unpack(">i", self.block(head)[508:512])[0]
                result.append({"name": name, "block": head, "secondary_type": sec})
                head = item[124]
        return sorted(result, key=lambda item: item["name"].lower())

    def walk(self) -> dict[str, dict[str, Any]]:
        pending = [("", self.root)]
        seen: set[int] = set()
        result = {}
        while pending:
            parent, block = pending.pop()
            require(block not in seen, f"ADF directory cycle at {parent}")
            seen.add(block)
            for item in self.directory(block):
                path = f"{parent}/{item['name']}".lstrip("/")
                require(path.lower() not in result, f"Duplicate ADF path: {path}")
                result[path.lower()] = {**item, "path": path}
                if item["secondary_type"] == 2:
                    pending.append((path, item["block"]))
        self.directory_count = len(seen)
        return result

    def read_file(self, number: int) -> bytes:
        first = self.header(number, (2,), (-3,))
        size = first[81]
        require(size <= len(self.data), "ADF file size exceeds containing image")
        pointers = []
        extensions: set[int] = set()
        current = number
        while current:
            require(current not in extensions, f"ADF extension cycle at {current}")
            extensions.add(current)
            words = self.header(current, (2,) if current == number else (16,), (-3,))
            require(words[1] == current, f"ADF extension/header key mismatch at {current}")
            if current != number:
                require(words[125] == number, f"ADF extension parent mismatch at {current}")
            require(words[2] <= 72, f"ADF excessive data-block count at {current}")
            active = list(reversed(words[6:78]))[:words[2]]
            require(all(active), f"Null ADF data block in active table at {current}")
            pointers.extend(active)
            current = words[126]
        require(len(pointers) == len(set(pointers)), "Repeated ADF file data block")
        require(not set(pointers).intersection(extensions), "ADF data points at file header")
        payload_size = 512 if self.fast else 488
        require(len(pointers) == (size + payload_size - 1) // payload_size,
                "ADF block count does not match file size")
        require(not pointers or first[4] == pointers[0], "ADF first-data pointer mismatch")
        result = bytearray()
        for sequence, pointer in enumerate(pointers, 1):
            block = self.block(pointer)
            if self.fast:
                result.extend(block)
            else:
                words = struct.unpack(">128I", block)
                require(sum(words) & 0xffffffff == 0, "OFS data checksum mismatch")
                require(words[0] == 8 and words[1] == number and words[2] == sequence,
                        "OFS data identity/sequence mismatch")
                require(words[3] <= 488, "OFS data payload exceeds block")
                next_pointer = pointers[sequence] if sequence < len(pointers) else 0
                require(words[4] == next_pointer, "OFS data-chain mismatch")
                result.extend(block[24:24 + words[3]])
        require(self.fast or len(result) == size, "OFS payload length mismatch")
        return bytes(result[:size])

    def file_facts(self, entry: dict[str, Any], source_id: str) -> dict[str, Any]:
        require(entry["secondary_type"] == -3, f"Non-ordinary C file requires review: {entry['path']}")
        data = self.read_file(entry["block"])
        words = self.header(entry["block"], (2,), (-3,))
        return {
            **binary_facts(data, entry["name"]), "source_id": source_id,
            "path": entry["path"], "volume": self.volume,
            "original_name": entry["name"], "header_block": entry["block"],
            "protection": amiga_protection(words[80]),
            "datestamp": {"days": words[105], "minutes": words[106], "ticks": words[107]},
            "confidence": "original-media-observed",
        }


class Iso:
    """Read ISO9660 with Rock Ridge NM/CE/PX metadata; never infer Amiga P bits."""

    def __init__(self, path: Path):
        self.path = path
        self.stream = path.open("rb")
        self.data = mmap.mmap(self.stream.fileno(), 0, access=mmap.ACCESS_READ)
        try:
            self._initialize()
        except Exception:
            self.close()
            raise

    def close(self) -> None:
        self.data.close()
        self.stream.close()

    def __enter__(self) -> "Iso":
        return self

    def __exit__(self, *_: Any) -> None:
        self.close()

    def read(self, offset: int, size: int) -> bytes:
        require(offset >= 0 and size >= 0 and offset + size <= len(self.data), "ISO extent out of range")
        return self.data[offset:offset + size]

    @staticmethod
    def both32(data: bytes, offset: int) -> int:
        low = struct.unpack_from("<I", data, offset)[0]
        high = struct.unpack_from(">I", data, offset + 4)[0]
        require(low == high, "ISO mismatched both-endian field")
        return low

    def _initialize(self) -> None:
        sector = 16
        while True:
            descriptor = self.read(sector * 2048, 2048)
            require(descriptor[1:7] == b"CD001\x01", "Invalid ISO volume descriptor")
            if descriptor[0] == 1:
                break
            require(descriptor[0] != 255, "ISO has no primary volume descriptor")
            sector += 1
        require(struct.unpack_from("<H", descriptor, 128)[0] == 2048 and
                struct.unpack_from(">H", descriptor, 130)[0] == 2048, "Unsupported ISO block size")
        require(self.both32(descriptor, 80) * 2048 <= len(self.data), "ISO volume size exceeds image")
        self.volume = descriptor[40:72].decode("ascii").rstrip()
        self.root = self.record(descriptor[156:156 + descriptor[156]])

    def system_use(self, data: bytes, seen: frozenset[tuple[int, int, int]] = frozenset()) -> list[bytes]:
        result = []
        offset = 0
        while offset < len(data):
            if not any(data[offset:]):
                break
            require(offset + 4 <= len(data), "Truncated ISO system-use header")
            size = data[offset + 2]
            require(size >= 4 and offset + size <= len(data), "Invalid ISO system-use length")
            field = data[offset:offset + size]
            result.append(field)
            if field[:2] == b"CE":
                require(size >= 28, "Truncated ISO CE field")
                key = (self.both32(field, 4), self.both32(field, 12), self.both32(field, 20))
                require(key not in seen and len(seen) < 16, "Cyclic/excessive ISO continuation")
                result.extend(self.system_use(self.read(key[0] * 2048 + key[1], key[2]), seen | {key}))
            offset += size
        return result

    def record(self, data: bytes) -> dict[str, Any]:
        require(len(data) >= 34 and data[0] == len(data), "Invalid ISO directory record length")
        extent, size = self.both32(data, 2), self.both32(data, 10)
        require(extent * 2048 + size <= len(self.data), "ISO directory entry exceeds image")
        length = data[32]
        require(length > 0 and 33 + length <= len(data), "Invalid ISO filename length")
        raw_name = data[33:33 + length]
        name = raw_name.decode("latin-1")
        fields = self.system_use(data[33 + length + (1 if length % 2 == 0 else 0):])
        names = [field[5:] for field in fields if field[:2] == b"NM" and len(field) >= 5]
        if names:
            name = b"".join(names).decode("latin-1")
        elif ";" in name:
            name = name.rsplit(";", 1)[0]
        px = next((field for field in fields if field[:2] == b"PX"), None)
        return {
            "name": name, "extent": extent, "bytes": size, "flags": data[25],
            "special": raw_name in (b"\0", b"\1"),
            "date_bytes_hex": data[18:25].hex(),
            "rock_ridge_posix_mode": self.both32(px, 4) if px is not None and len(px) >= 12 else None,
        }

    def directory(self, entry: dict[str, Any]) -> list[dict[str, Any]]:
        require(entry["flags"] & 2, "ISO entry is not a directory")
        data = self.read(entry["extent"] * 2048, entry["bytes"])
        result = []
        names: set[str] = set()
        offset = 0
        while offset < len(data):
            length = data[offset]
            if length == 0:
                offset = (offset // 2048 + 1) * 2048
                continue
            require(offset + length <= len(data) and offset % 2048 + length <= 2048,
                    "ISO directory record crosses block/end")
            item = self.record(data[offset:offset + length])
            if not item["special"]:
                require(item["name"].lower() not in names, f"Duplicate ISO name: {item['name']}")
                names.add(item["name"].lower())
                result.append(item)
            offset += length
        return sorted(result, key=lambda item: item["name"].lower())

    def walk(self) -> dict[str, dict[str, Any]]:
        pending = [("", self.root)]
        seen: set[int] = set()
        result = {}
        while pending:
            parent, directory = pending.pop()
            require(directory["extent"] not in seen, f"ISO directory alias/cycle at {parent}")
            seen.add(directory["extent"])
            for item in self.directory(directory):
                require("/" not in item["name"], "ISO filename contains separator")
                path = f"{parent}/{item['name']}".lstrip("/")
                require(path.lower() not in result, f"Duplicate ISO path: {path}")
                result[path.lower()] = {**item, "path": path}
                if item["flags"] & 2:
                    pending.append((path, item))
        self.directory_count = len(seen)
        return result

    def file_facts(self, entry: dict[str, Any], source_id: str) -> dict[str, Any]:
        require(not entry["flags"] & (2 | 128), f"Unsupported ISO command file: {entry['path']}")
        return {
            **binary_facts(self.read(entry["extent"] * 2048, entry["bytes"]), entry["name"]),
            "source_id": source_id, "path": entry["path"], "original_name": entry["name"],
            "iso_extent": entry["extent"], "iso_recording_date_hex": entry["date_bytes_hex"],
            "rock_ridge_posix_mode": entry["rock_ridge_posix_mode"],
            "protection": {"raw": None, "pure": None, "script": None,
                           "evidence": "not-observed-iso-posix-mode-is-not-amigados-protection"},
            "confidence": "original-media-observed",
        }


def binary_facts(data: bytes, command: str) -> dict[str, Any]:
    formats = {b"\0\0\x03\xf3": "amiga-hunk", b"\x7fELF": "elf", b"\x7fMOS": "morphos-packed-native"}
    version_tags = []
    pattern = rb"\$VER:[ \t]+([A-Za-z0-9_. -]{1,80}?)[ \t]+(\d+)\.(\d+)(?:[ \t]+\(([^)\x00\r\n]{1,32})\))?"
    for match in re.finditer(pattern, data):
        version_tags.append({
            "name": match[1].decode("ascii"), "version": int(match[2]),
            "revision": int(match[3]), "date_text": match[4].decode("latin-1") if match[4] else None,
            "byte_offset": match.start(), "evidence": "unexecuted-binary-ver-tag",
        })
    matching = [tag for tag in version_tags if tag["name"].lower() == command.lower()]
    candidates = []
    # Syntactic candidates only, not proof that a string is the entry's template.
    atom = r"[A-Za-z?][A-Za-z0-9_?-]*(?:=[A-Za-z?][A-Za-z0-9_?-]*)*(?:/[AKMNSTF])*"
    template = re.compile(rf"\s*{atom}(?:\s*,\s*{atom})*\s*", re.I)
    for match in re.finditer(rb"(?<=\x00)([ -~]{3,1024})\x00", data):
        value = match[1].decode("ascii")
        if "/" in value and template.fullmatch(value):
            candidates.append({"text": value, "byte_offset": match.start(1),
                               "confidence": "syntax-only-candidate-not-runtime-or-source-verified"})
    # The Workbench 3.1 Info body uses the classic single-keyword DEVICE
    # template, which is intentionally outside the slash-bearing candidate
    # grammar above. Preserve this syntax-only binary observation so the
    # derived inventory remains reproducible.
    if formats.get(data[:4]) == "amiga-hunk" and command.lower() == "info":
        for match in re.finditer(rb"(?<=\x00)DEVICE\x00", data):
            candidates.append({"text": "DEVICE", "byte_offset": match.start() + 1,
                               "confidence": "syntax-only-candidate-not-runtime-or-source-verified"})
    # Workbench 3.1 Mount stores its second ReadArgs template as a
    # comma-leading string.  That is a real binary string observation, but it
    # is still only a candidate until the original command's parser call is
    # observed at runtime.  The normal grammar intentionally rejects a leading
    # comma because empty result slots are not valid for ordinary templates.
    if command.lower() == "mount":
        for match in re.finditer(rb"(,SECTORSIZE=BLOCKSIZE,[ -~]{2,1020}FORCELOAD)\x00", data):
            value = match[1].decode("ascii")
            candidates.append({
                "text": value, "byte_offset": match.start(1),
                "confidence": "binary-extracted-leading-comma-syntax-candidate-not-runtime-or-source-verified",
            })
    return {
        "bytes": len(data), "sha256": digest(data), "file_format": formats.get(data[:4], "unclassified"),
        "version": matching[0] if len(matching) == 1 else None,
        "version_tags": version_tags, "template_candidates": candidates,
        "contract_complete": False,
    }


def plan_rows(path: Path) -> tuple[dict[str, dict[str, Any]], dict[str, Any]]:
    raw = path.read_bytes()
    text = raw.decode("utf-8-sig").replace("\r\n", "\n")
    require("## Appendix A" in text and "## Appendix B" in text, "Goal is missing inventory appendices")
    section = text.split("## Appendix A", 1)[1].split("## Appendix B", 1)[0]
    pattern = r"^\| `([^`]+)` \| ([^|]+) \| ([^|]+) \| (CC\d+) \|$"
    rows = {}
    for match in re.finditer(pattern, section, re.M):
        name, wb, mos, owner = (group.strip() for group in match.groups())
        identity = logical_id(name)
        require(identity not in rows, f"Duplicate command identity in plan: {name}")
        rows[identity] = {"name": name, "markers": [marker for marker in ("W", "I") if marker in wb],
                          "morphos": mos == "M", "owner_stage": owner}
    require(len(rows) == 200, f"Plan seed changed ({len(rows)} rows); review and update inventory baseline")
    return rows, {"path": path.relative_to(ROOT).as_posix() if path.is_relative_to(ROOT) else path.name,
                  "sha256": digest(raw), "external_seed_rows": len(rows)}


def resident_events(data: bytes, source_id: str, script: str) -> list[dict[str, Any]]:
    events = []
    for line_number, line in enumerate(data.decode("latin-1").splitlines(), 1):
        if not re.search(r"(?:^|[\s:\"])Resident\b", line, re.I):
            continue
        add = re.search(r"C[:/]([A-Za-z0-9_]+)\s+PURE\b", line, re.I)
        remove = re.search(r"Resident\s+(?:>[^\s]+\s+)?([A-Za-z0-9_]+)\s+REMOVE\b", line, re.I)
        if not add and not remove:
            continue
        match = add or remove
        events.append({
            "id": f"{source_id}:{script}:{line_number}", "source_id": source_id,
            "script": script, "line": line_number, "command_id": logical_id(match[1]),
            "operation": "resident-add" if add else "resident-remove", "force_pure": bool(add),
            "confidence": "literal-script-observed-not-executed",
        })
    return events


def expected_closure() -> list[dict[str, Any]]:
    return [
        {"id": "WB31-DISKS-3-6", "owner_stage": "CC00", "status": "open",
         "detail": "Extras, Fonts, Locale and Storage disks are absent from the selected reference set; all six disks are not yet closed."},
        {"id": "WB31-MACHINE-VARIANTS", "owner_stage": "CC00", "status": "open",
         "detail": "Only the M10 40.42 Install/Workbench pair is admitted; machine-specific 3.1 variants require reconciliation."},
        {"id": "WB31-INSTALLED-METADATA", "owner_stage": "CC00", "status": "open",
         "detail": "Clean installed placement, protection flags and startup/installer resident behavior have not been observed."},
        {"id": "MORPHOS320-INSTALLED-OVERLAY", "owner_stage": "CC00", "status": "open",
         "detail": "Installed SYS:C/MOSSYS:C assigns, search precedence, installer filtering and resulting P bits require capture."},
        {"id": "COMMAND-RUNTIME-CONTRACTS", "owner_stage": "CC01/CC43", "status": "open",
         "detail": "Binary tags and template-looking strings are not runtime observations or complete option/behavior contracts."},
        {"id": "FREEZE-INDEX-MEDIA", "owner_stage": "CC39", "status": "open",
         "detail": "Freeze is in the documentation index and historic 2.1 release notes, but absent from the inspected 3.20 ISO tree."},
    ]


def validate_workbench_capture_audit(
    audit_path: Path, spec: dict[str, Any], adf: Adf,
    command_entries: list[dict[str, Any]],
) -> None:
    """Verify the durable per-command audit against the selected ADF.

    The audit stores only metadata and hashes; the source image remains outside
    the repository. Rechecking it here prevents a hand-edited capture record
    from being mistaken for media evidence.
    """
    require(audit_path.is_file(), f"Missing Workbench capture audit: {audit_path}")
    try:
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise EvidenceError(f"Invalid Workbench capture audit: {audit_path}") from error
    require(audit.get("schema_version") == SCHEMA_VERSION, "Workbench capture audit schema changed")
    require(audit.get("profile") == "wb31" and audit.get("source_id") == spec["id"],
            "Workbench capture audit source identity changed")
    require(audit.get("adf_sha256") == spec["adf_sha256"],
            "Workbench capture audit ADF hash differs from pinned source")
    rows = audit.get("commands")
    require(isinstance(rows, list) and len(rows) == len(command_entries),
            "Workbench capture audit command count changed")
    expected: dict[str, tuple[str, int, str]] = {}
    for entry in command_entries:
        facts = adf.file_facts(entry, spec["id"])
        expected[logical_id(entry["name"])] = (
            entry["name"], facts["bytes"], facts["sha256"])
    observed: dict[str, tuple[str, int, str]] = {}
    for row in rows:
        require(isinstance(row, dict), "Workbench capture audit row is not an object")
        name = row.get("name")
        require(isinstance(name, str), "Workbench capture audit command name is invalid")
        identity = logical_id(name)
        require(identity not in observed, f"Duplicate Workbench capture audit row: {name}")
        require(isinstance(row.get("bytes"), int) and isinstance(row.get("sha256"), str),
                f"Incomplete Workbench capture audit row: {name}")
        observed[identity] = (name, row["bytes"], row["sha256"])
    require(set(observed) == set(expected), "Workbench capture audit membership changed")
    for identity, actual in expected.items():
        require(observed[identity][1:] == actual[1:],
                f"Workbench capture audit hash/size differs for {actual[0]}")


def validate_workbench_script_audit(
    audit_path: Path, spec: dict[str, Any], adf: Adf,
    tree: dict[str, dict[str, Any]],
) -> None:
    """Verify hash-only installer/startup script records against the ADF."""
    if spec.get("script_capture_audit") is None:
        return
    require(audit_path.is_file(), f"Missing Workbench script capture audit: {audit_path}")
    try:
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise EvidenceError(f"Invalid Workbench script capture audit: {audit_path}") from error
    require(audit.get("schema_version") == SCHEMA_VERSION and
            audit.get("profile") == "wb31" and audit.get("source_id") == spec["id"],
            "Workbench script capture audit source identity changed")
    require(audit.get("adf_sha256") == spec["adf_sha256"],
            "Workbench script capture audit ADF hash differs from pinned source")
    rows = audit.get("scripts")
    require(isinstance(rows, list) and len(rows) == len(spec["scripts"]),
            "Workbench script capture audit member count changed")
    observed: set[str] = set()
    for row in rows:
        require(isinstance(row, dict) and isinstance(row.get("path"), str),
                "Workbench script capture audit row is invalid")
        path = row["path"].lower()
        require(path not in observed, f"Duplicate Workbench script audit row: {row['path']}")
        observed.add(path)
        require(path in tree, f"Workbench script capture audit path is absent: {row['path']}")
        data = adf.read_file(tree[path]["block"])
        require(row.get("bytes") == len(data) and row.get("sha256") == digest(data),
                f"Workbench script capture audit hash/size differs for {row['path']}")
    require(observed == {path.lower() for path in spec["scripts"]},
            "Workbench script capture audit membership changed")


def validate_morphos_capture_audit(
    audit_path: Path, identity: str, entry: dict[str, Any], iso: Iso,
    source_id: str,
) -> None:
    """Verify a selected MorphOS binary audit against the pinned ISO member."""
    require(audit_path.is_file(), f"Missing MorphOS capture audit: {audit_path}")
    try:
        audit = json.loads(audit_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise EvidenceError(f"Invalid MorphOS capture audit: {audit_path}") from error
    require(audit.get("schema_version") == SCHEMA_VERSION and
            audit.get("profile") == "morphos320" and
            audit.get("source_id") == source_id and
            logical_id(audit.get("command", "")) == identity,
            "MorphOS capture audit source identity changed")
    require(audit.get("path") == entry["path"] and
            audit.get("iso_extent") == entry["extent"] and
            audit.get("bytes") == entry["bytes"],
            f"MorphOS capture audit location/size differs for {entry['path']}")
    data = iso.read(entry["extent"] * 2048, entry["bytes"])
    require(audit.get("sha256") == digest(data),
            f"MorphOS capture audit hash differs for {entry['path']}")


def extract(workbench_root: Path, morphos_iso: Path, plan: Path) -> tuple[dict[str, Any], dict[str, Any]]:
    rows, plan_info = plan_rows(plan)
    sources = []
    scripts = []
    events = []
    originals: dict[str, dict[str, list[dict[str, Any]]]] = {identity: {} for identity in rows}
    observed_markers: dict[str, set[str]] = {identity: set() for identity in rows}
    for spec in WB_SOURCES:
        archive = workbench_root / spec["archive"]
        require(archive.is_file(), f"Missing selected Workbench archive: {archive}")
        zip_hash = file_digest(archive)
        require(zip_hash == spec["zip_sha256"], f"Selected Workbench ZIP hash changed: {archive.name}")
        with zipfile.ZipFile(archive) as container:
            members = [item for item in container.infolist() if item.filename.lower().endswith(".adf")]
            require(len(members) == 1, f"Expected exactly one ADF in {archive.name}")
            require(members[0].file_size <= 4 * 1024 * 1024, "Unexpected selected ADF size")
            image = container.read(members[0])  # zipfile also verifies member CRC.
        require(digest(image) == spec["adf_sha256"], f"Selected ADF hash changed: {members[0].filename}")
        adf = Adf(image)
        require(adf.volume == spec["volume"], f"Unexpected Workbench volume: {adf.volume}")
        tree = adf.walk()
        command_entries = [entry for key, entry in tree.items() if key.startswith("c/") and key.count("/") == 1]
        require(len(command_entries) == spec["command_count"], f"Unexpected C count on {adf.volume}")
        validate_workbench_capture_audit(
            DEFAULT_OUTPUT / spec["capture_audit"], spec, adf, command_entries)
        validate_workbench_script_audit(
            DEFAULT_OUTPUT / spec["script_capture_audit"] if spec.get("script_capture_audit")
            else DEFAULT_OUTPUT, spec, adf, tree)
        for entry in command_entries:
            identity = logical_id(entry["name"])
            require(identity in rows, f"Unowned Workbench C file discovered: {entry['path']}")
            originals[identity].setdefault("wb31", []).append(adf.file_facts(entry, spec["id"]))
            observed_markers[identity].add(spec["marker"])
        for path in spec["scripts"]:
            entry = tree[path.lower()]
            data = adf.read_file(entry["block"])
            scripts.append({"source_id": spec["id"], "path": entry["path"], "bytes": len(data), "sha256": digest(data)})
            events.extend(resident_events(data, spec["id"], entry["path"]))
        version_entry = tree["libs/version.library"]
        version = adf.file_facts(version_entry, spec["id"])
        sources.append({
            "id": spec["id"], "profile": "wb31", "role": spec["role"],
            "archive_name": archive.name, "archive_sha256": zip_hash, "archive_bytes": archive.stat().st_size,
            "member_name": members[0].filename, "image_sha256": digest(image), "image_bytes": len(image),
            "volume": adf.volume, "filesystem": "FFS" if adf.fast else "OFS", "root_block": adf.root,
            "command_capture_audit": spec["capture_audit"],
            "script_capture_audit": spec.get("script_capture_audit"),
            "c_files": len(command_entries), "directory_count": adf.directory_count,
            "validated_header_count": len(adf.checked_headers), "version_library": version,
            "confidence": "full-local-archive-and-image-hashes-and-filesystem-validated",
        })
    require(morphos_iso.is_file(), f"Missing MorphOS ISO: {morphos_iso}")
    require(morphos_iso.stat().st_size == ISO_BYTES, "MorphOS ISO length differs from selected reference")
    iso_md5 = file_digest(morphos_iso, "md5")
    require(iso_md5 == ISO_MD5, "MorphOS ISO does not match official published MD5")
    iso_sha256 = file_digest(morphos_iso)
    require(iso_sha256 == ISO_SHA256, "MorphOS ISO differs from pinned full-image SHA256")
    source_id = "morphos320-live-iso"
    with Iso(morphos_iso) as iso:
        tree = iso.walk()
        command_directory = tree["morphos/c"]
        root_c = tree["c"]
        command_entries = iso.directory(command_directory)
        require(len(command_entries) == 188, "Selected MorphOS C count changed")
        require(not iso.directory(root_c), "Selected MorphOS root C is no longer empty")
        for entry in command_entries:
            entry["path"] = f"MorphOS/C/{entry['name']}"
            identity = logical_id(entry["name"])
            require(identity in rows, f"Unowned MorphOS C file discovered: {entry['name']}")
            facts = iso.file_facts(entry, source_id)
            capture_audit = MORPHOS_CAPTURE_AUDITS.get(identity)
            if capture_audit is not None:
                validate_morphos_capture_audit(
                    DEFAULT_OUTPUT / capture_audit, identity, entry, iso, source_id)
                facts["capture_audit"] = capture_audit
            originals[identity].setdefault("morphos320", []).append(facts)
            observed_markers[identity].add("M")
        for path in ("hdinstall.fixc", "MorphOS/S/startup-sequence"):
            entry = tree[path.lower()]
            data = iso.read(entry["extent"] * 2048, entry["bytes"])
            scripts.append({"source_id": source_id, "path": entry["path"], "bytes": len(data),
                            "iso_extent": entry["extent"], "sha256": digest(data)})
            if path == "hdinstall.fixc":
                lines = data.decode("latin-1").splitlines()
                for line_number, line in enumerate(lines, 1):
                    if not line.strip():
                        continue
                    match = re.fullmatch(r"protect MorphOS/C/(\S+) \+P", line, re.I)
                    require(match is not None, f"Unreviewed instruction in hdinstall.fixc:{line_number}")
                    identity = logical_id(match[1])
                    require(identity in originals and "morphos320" in originals[identity],
                            f"P script references absent command: {match[1]}")
                    events.append({"id": f"{source_id}:{path}:{line_number}", "source_id": source_id,
                                   "script": path, "line": line_number, "command_id": identity,
                                   "operation": "file-protection-add", "flag": "P", "replaces_other_flags": False,
                                   "confidence": "literal-script-observed-not-installed-metadata"})
            else:
                events.extend(resident_events(data, source_id, path))
        freeze = [entry["path"] for entry in tree.values() if entry["name"].lower() == "freeze"]
        require(not freeze, "Freeze is now on selected media; CC39 reconciliation must be updated")
        sources.append({
            "id": source_id, "profile": "morphos320", "role": "Live/install ISO", "image_name": morphos_iso.name,
            "image_bytes": len(iso.data), "image_sha256": iso_sha256, "image_md5": iso_md5,
            "published_md5": ISO_MD5, "published_md5_match": True, "download_url": ISO_URL,
            "checksum_publication_url": DOWNLOADS_URL, "volume": iso.volume,
            "c_files": 188, "root_c_files": 0, "directory_count": iso.directory_count,
            "c_directory": {"path": "MorphOS/C", "iso_extent": command_directory["extent"],
                            "bytes": command_directory["bytes"],
                            "sha256": digest(iso.read(command_directory["extent"] * 2048, command_directory["bytes"]))},
            "freeze_paths": freeze, "confidence": "full-image-published-md5-matched-and-directory-tree-validated",
            "protection_limit": "Rock Ridge POSIX modes were observed; native AmigaDOS bits and installed flags were not.",
        })
    require(sum(event["operation"] == "file-protection-add" for event in events) == 88, "P-script count changed")
    commands = []
    for identity, row in sorted(rows.items()):
        expected = set(row["markers"]) | ({"M"} if row["morphos"] else set())
        require(observed_markers[identity] == expected,
                f"Plan/media membership mismatch for {row['name']}: {observed_markers[identity]} vs {expected}")
        profiles = {}
        for profile, files in sorted(originals[identity].items()):
            relevant = [event for event in events if event["command_id"] == identity and
                        (event["source_id"].startswith("wb31-") if profile == "wb31" else event["source_id"] == source_id)]
            requirement = any(event.get("force_pure") or event["operation"] == "file-protection-add" for event in relevant)
            profiles[profile] = {
                "required": True, "source_files": sorted(files, key=lambda item: (item["source_id"], item["path"])),
                "purity": {"required_by_observed_design_evidence": requirement,
                           "classification": "required-pure" if requirement else "unresolved-not-nonpure",
                           "event_ids": [event["id"] for event in relevant],
                           "installed_flags_observed": False, "replacement_artifact_qualified": False},
                "contract_status": "open", "runtime_observed": False,
                "packaging_status": "original-placement-known-installed-selection-open",
            }
        commands.append({"id": identity, "name": row["name"], "kind": "external", "required": True,
                         "owner_stage": row["owner_stage"], "reference_profiles": profiles,
                         "qualification_status": "unqualified"})
    coverage = {
        "status": "partial-reference-closure", "required_external_identities": len(commands),
        "wb31_c_entries": 70, "wb31_distinct_identities": 58, "morphos320_c_entries": 188,
        "shared_identities": 46, "wb31_only_identities": 12, "shell_internal_identities": len(INTERNAL_NAMES),
        "morphos_installer_p_additions": 88, "full_morphos_image_checksum_verified": True,
        "all_workbench_disks_observed": False, "installed_system_observed": False,
        "runtime_or_command_contract_complete": False,
    }
    inventory = {
        "schema_version": SCHEMA_VERSION, "producer": "tools/Commands/Inventory/inventory.py",
        "baseline_profiles": {"wb31": "Original Workbench 3.1/v40; admitted M10 40.42 pair",
                              "morphos320": "MorphOS 3.20 official live/install ISO"},
        "plan": plan_info, "coverage": coverage, "commands": commands,
        "shell_internal_names": INTERNAL_NAMES,
        "discrepancies": [{"id": "FREEZE-INDEX-MEDIA", "name": "Freeze", "owner_stage": "CC39",
                           "classification": "documented-command-not-observed-on-selected-media",
                           "included_in_required_200_count": False, "status": "open",
                           "documentation_url": "https://library.morph.zone/Shell_Commands/Freeze",
                           "historic_primary_url": "https://www.morphos-team.net/releasenotes/2.1"}],
        "open_closure_items": expected_closure(),
    }
    evidence = {
        "schema_version": SCHEMA_VERSION, "producer": inventory["producer"], "plan": plan_info,
        "sources": sources, "script_files": scripts, "script_events": events,
        "coverage": coverage, "open_closure_items": expected_closure(),
        "reference_discovery": {
            "selected_workbench_roles": [spec["role"] for spec in WB_SOURCES],
            "unadmitted_workbench_archive_names": sorted(
                path.name for path in workbench_root.glob("Workbench v3.1 *.zip")
                if path.name not in {spec["archive"] for spec in WB_SOURCES}),
            "missing_distribution_roles": ["Extras", "Fonts", "Locale", "Storage"],
            "policy": "New archives require provenance/role/hash review; discovery alone does not admit or close a disk.",
        },
        "evidence_limits": [
            "No original command, installer, ROM, device or service was executed or modified.",
            "Raw version tags may be absent from packed native files; absence is not an unknown actual release claim.",
            "Template candidates are syntax-only observations, never frozen ReadArgs contracts.",
            "Installer +P intent, media flags, runtime Resident state and replacement purity are distinct evidence.",
            "Only hashes/facts are exported; complete media, binaries and scripts remain private.",
        ],
    }
    validate(inventory, evidence, plan)
    return inventory, evidence


def validate(inventory: dict[str, Any], evidence: dict[str, Any], plan: Path) -> None:
    require(inventory.get("schema_version") == SCHEMA_VERSION and evidence.get("schema_version") == SCHEMA_VERSION,
            "Unsupported inventory/evidence schema")
    rows, info = plan_rows(plan)
    require(inventory["plan"] == info and evidence["plan"] == info, "Goal plan hash/identity changed; regenerate evidence")
    commands = inventory["commands"]
    identities = [item["id"] for item in commands]
    require(len(identities) == len(set(identities)) == len(rows), "Duplicate/missing command identities")
    require(set(identities) == set(rows), "Inventory identities differ from goal rows")
    require(inventory["coverage"] == evidence["coverage"], "Coverage differs between evidence files")
    require(len(inventory["shell_internal_names"]) == 31 and
            {name.lower() for name in inventory["shell_internal_names"]} == {name.lower() for name in INTERNAL_NAMES},
            "Shell internal identity set changed")
    require(not set(identities).intersection(name.lower() for name in INTERNAL_NAMES), "Shell internals duplicated as external")
    source_ids = [source["id"] for source in evidence["sources"]]
    require(len(source_ids) == len(set(source_ids)), "Duplicate media source id")
    source_profiles = {source["id"]: source["profile"] for source in evidence["sources"]}
    iso_sources = [source for source in evidence["sources"] if source["profile"] == "morphos320"]
    require(len(iso_sources) == 1 and iso_sources[0]["image_md5"] == ISO_MD5 and
            iso_sources[0]["image_sha256"] == ISO_SHA256 and iso_sources[0]["image_bytes"] == ISO_BYTES and
            iso_sources[0]["published_md5_match"] is True, "Selected full-image integrity evidence differs")
    event_ids = [event["id"] for event in evidence["script_events"]]
    require(len(event_ids) == len(set(event_ids)), "Duplicate script event id")
    expected_events = {event["id"]: event for event in evidence["script_events"]}
    for event in expected_events.values():
        require(event["source_id"] in source_ids and event["command_id"] in rows,
                "Script event points to an unknown source/command")
        require(event["operation"] in ("resident-add", "resident-remove", "file-protection-add"),
                "Unknown script event operation")
        if event["operation"] == "file-protection-add":
            require(event["flag"] == "P" and event["replaces_other_flags"] is False and
                    source_profiles[event["source_id"]] == "morphos320", "Incorrect MorphOS P-addition semantics")
    markers: dict[str, set[str]] = {identity: set() for identity in identities}
    counts = {"W": 0, "I": 0, "M": 0}
    source_file_keys: set[tuple[str, str]] = set()
    for command in commands:
        identity = command["id"]
        require(command["kind"] == "external" and command["required"] is True,
                "Required external command was reclassified")
        require(command["qualification_status"] == "unqualified", "Media extraction cannot qualify replacement commands")
        require(command["name"] == rows[identity]["name"] and command["owner_stage"] == rows[identity]["owner_stage"],
                f"Incorrect name/owner stage for {identity}")
        for profile, record in command["reference_profiles"].items():
            require(profile in ("wb31", "morphos320"), "Unknown command profile")
            require(record["contract_status"] == "open" and not record["runtime_observed"], "Extractor cannot qualify runtime contracts")
            require(record["source_files"], f"Profile without original files: {identity}/{profile}")
            for item in record["source_files"]:
                require(item["source_id"] in source_ids, f"Unknown source reference: {item['source_id']}")
                require(source_profiles[item["source_id"]] == profile, "Original file assigned to wrong profile")
                key = (item["source_id"], item["path"].lower())
                require(key not in source_file_keys, "Original source file duplicated")
                source_file_keys.add(key)
                require(re.fullmatch(r"[0-9a-f]{64}", item["sha256"]) is not None, "Invalid file SHA256")
                require(logical_id(item["original_name"]) == identity, "Original spelling changed identity")
                require(not item["contract_complete"], "A binary scan cannot mark a contract complete")
                marker = "M" if profile == "morphos320" else ("I" if "install" in item["source_id"] else "W")
                markers[identity].add(marker)
                counts[marker] += 1
                if profile == "morphos320":
                    require(item["protection"]["pure"] is None, "Do not infer ISO Amiga P from POSIX mode")
                else:
                    require(item["protection"] == amiga_protection(item["protection"]["raw"]),
                            "Decoded Amiga protection facts differ from the raw FIB field")
            relevant = record["purity"]["event_ids"]
            require(all(event in expected_events and expected_events[event]["command_id"] == identity for event in relevant),
                    "Invalid per-command script evidence reference")
            expected_relevant = {event["id"] for event in expected_events.values()
                                 if event["command_id"] == identity and source_profiles[event["source_id"]] == profile}
            require(len(relevant) == len(set(relevant)) and set(relevant) == expected_relevant,
                    "Per-command original script evidence was omitted or duplicated")
            required_pure = any(expected_events[event].get("force_pure") or
                                expected_events[event]["operation"] == "file-protection-add" for event in relevant)
            require(record["purity"]["required_by_observed_design_evidence"] == required_pure,
                    "Pure requirement differs from source-script evidence")
            require(record["purity"]["classification"] == ("required-pure" if required_pure else "unresolved-not-nonpure") and
                    record["purity"]["installed_flags_observed"] is False and
                    record["purity"]["replacement_artifact_qualified"] is False,
                    "Purity intent was confused with installed/runtime qualification")
        expected_markers = set(rows[identity]["markers"]) | ({"M"} if rows[identity]["morphos"] else set())
        require(markers[identity] == expected_markers, f"Reference membership differs for {identity}")
    require(counts == {"W": 50, "I": 20, "M": 188}, f"Incorrect per-media counts: {counts}")
    coverage = inventory["coverage"]
    wb_ids = {identity for identity, membership in markers.items() if membership.intersection({"W", "I"})}
    mos_ids = {identity for identity, membership in markers.items() if "M" in membership}
    calculated = {"required_external_identities": len(commands), "wb31_c_entries": counts["W"] + counts["I"],
                  "wb31_distinct_identities": len(wb_ids), "morphos320_c_entries": counts["M"],
                  "shared_identities": len(wb_ids & mos_ids), "wb31_only_identities": len(wb_ids - mos_ids),
                  "shell_internal_identities": len(INTERNAL_NAMES)}
    require(all(coverage[key] == value for key, value in calculated.items()), "Coverage totals are not supported by file rows")
    require(coverage["status"] == "partial-reference-closure" and coverage["all_workbench_disks_observed"] is False and
            coverage["installed_system_observed"] is False and coverage["runtime_or_command_contract_complete"] is False,
            "Incomplete reference capture was presented as complete")
    require(sum(event["operation"] == "file-protection-add" for event in evidence["script_events"]) == 88,
            "Incorrect MorphOS P-script count")
    require(any(item["id"] == "FREEZE-INDEX-MEDIA" and item["status"] == "open" for item in inventory["discrepancies"]),
            "Freeze discrepancy was silently dropped")
    require(inventory["open_closure_items"] == expected_closure() and evidence["open_closure_items"] == expected_closure(),
            "Reference closure limitations changed without an extractor/schema update")


def write_json(path: Path, data: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    content = json.dumps(data, indent=2, ensure_ascii=False) + "\n"
    # Replace only this derived output after its complete content is available.
    with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", newline="\n", dir=path.parent, delete=False) as stream:
        temporary = Path(stream.name)
        stream.write(content)
    os.replace(temporary, path)


def fetch_iso(target: Path) -> None:
    target = target.resolve()
    require(not target.is_relative_to(ROOT), "Reference ISO must stay outside the repository")
    if target.exists():
        require(target.is_file() and target.stat().st_size == ISO_BYTES and file_digest(target, "md5") == ISO_MD5 and
                file_digest(target) == ISO_SHA256,
                "Existing target differs from selected ISO; it will not be replaced")
    else:
        target.parent.mkdir(parents=True, exist_ok=True)
        with tempfile.NamedTemporaryFile(dir=target.parent, prefix=target.name + ".", suffix=".part", delete=False) as output:
            temporary = Path(output.name)
            with urllib.request.urlopen(ISO_URL, timeout=40) as response:
                require(response.status == 200, "Unexpected full-image download response")
                total = 0
                while chunk := response.read(4 * 1024 * 1024):
                    output.write(chunk)
                    total += len(chunk)
                    require(total <= ISO_BYTES, "Download larger than selected ISO")
        require(temporary.stat().st_size == ISO_BYTES and file_digest(temporary, "md5") == ISO_MD5 and
                file_digest(temporary) == ISO_SHA256,
                f"Download validation failed; untrusted partial retained at {temporary}")
        # Do not overwrite a target another worker created while downloading.
        require(not target.exists(), f"Target appeared during download; validated partial retained at {temporary}")
        temporary.rename(target)
    print(json.dumps({"path": str(target), "bytes": target.stat().st_size,
                      "md5": file_digest(target, "md5"), "sha256": file_digest(target), "published_md5_match": True}))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    subcommands = parser.add_subparsers(dest="action", required=True)
    fetch = subcommands.add_parser("fetch-morphos", help="Download/verify public ISO outside the repository")
    fetch.add_argument("--target", type=Path, default=Path("D:/TestData/MorphOSReferences/morphos-3.20.iso"))
    for name in ("extract", "verify", "verify-media"):
        command = subcommands.add_parser(name)
        command.add_argument("--plan", type=Path, default=DEFAULT_PLAN)
        command.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT)
        command.add_argument("--strict-complete", action="store_true", help="Exit 2 while known reference closure gaps remain")
        if name != "verify":
            command.add_argument("--workbench-root", type=Path, default=Path("D:/TestData/TestImages"))
            command.add_argument("--morphos-iso", type=Path, default=Path("D:/TestData/MorphOSReferences/morphos-3.20.iso"))
    args = parser.parse_args()
    try:
        if args.action == "fetch-morphos":
            fetch_iso(args.target)
            return 0
        inventory_path = args.output_dir / "command-inventory.json"
        evidence_path = args.output_dir / "media-evidence.json"
        if args.action == "extract":
            inventory, evidence = extract(args.workbench_root, args.morphos_iso, args.plan.resolve())
            write_json(inventory_path, inventory)
            write_json(evidence_path, evidence)
        else:
            inventory = json.loads(inventory_path.read_text(encoding="utf-8"))
            evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
            validate(inventory, evidence, args.plan.resolve())
            if args.action == "verify-media":
                actual_inventory, actual_evidence = extract(args.workbench_root, args.morphos_iso, args.plan.resolve())
                require(actual_inventory == inventory and actual_evidence == evidence,
                        "Derived files differ from selected media; regenerate and review")
        print(json.dumps({"validation": "passed", "operation": args.action, "coverage": inventory["coverage"],
                          "open_closure_ids": [item["id"] for item in inventory["open_closure_items"]]}))
        return 2 if args.strict_complete and inventory["open_closure_items"] else 0
    except (EvidenceError, OSError, ValueError, KeyError, IndexError, zipfile.BadZipFile) as error:
        print(f"Inventory validation failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
