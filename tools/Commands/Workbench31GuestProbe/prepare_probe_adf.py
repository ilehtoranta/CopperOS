"""Prepare an independently verified disposable Workbench probe ADF copy.

Only the audited Workbench 3.1 disk 2 image is accepted by the public entry
point. Installed amitools 0.8.1 performs pathname writes in a separate process;
this module never formats, creates a filesystem, installs boot code, or edits
the original. A prepared derivative is diagnostic media, not release admission.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.metadata
import importlib.util
import json
from pathlib import Path
import re
import struct
import subprocess
import sys
import tempfile


REPO = Path(__file__).resolve().parents[3]
DIAGNOSTICS_ROOT = Path("D:/TestData/CopperOS-Diagnostics")
SOURCE_SHA256 = "a8f167bad2897e8c7f2cfe73de7a9f8dad10c7a5b1f78ca17f818ab17278b985"
STARTUP_SHA256 = "64cb5972947dba207e852ad69a1a84f0aeb84e3f8b7a1f45ffde91d61de2546c"
VERSION_SHA256 = "dc2f55cd48b37bd1efecdbf07d38757463129dd9076ea6bf1b5d967f2189f224"
IMAGE_BYTES = 901120
MAX_PROBE_BYTES = 128 * 1024
AMITOOLS_VERSION = "0.8.1"
PROBE_PATH = "C/CopperProbe"
STARTUP_PATH = "S/Startup-Sequence"
VERSION_PATH = "C/Version"
INVENTORY_PATH = REPO / "tools/Commands/Inventory/inventory.py"
HUNK_READER_PATH = REPO / "tools/Commands/combine_loadresource_hunks.py"


def _module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


inventory = _module("copperos_probe_adf_inventory", INVENTORY_PATH)
hunk_reader = _module("copperos_probe_hunk_reader", HUNK_READER_PATH)


class ProbePreparationError(ValueError):
    pass


def require(condition, message: str):
    if not condition:
        raise ProbePreparationError(message)


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def identity(path: Path) -> dict:
    resolved = path.resolve(strict=True)
    data = resolved.read_bytes()
    return {"path": str(resolved), "bytes": len(data), "sha256": sha(data)}


def tool_source_identities() -> list[dict]:
    return [identity(path) for path in (Path(__file__), INVENTORY_PATH, HUNK_READER_PATH)]


def verify_tool_sources_unchanged(before: list[dict]) -> list[dict]:
    after = [identity(Path(item['path'])) for item in before]
    require(after == before, 'Preparation tool source changed during preparation')
    return after


def invocation_line(command: str, token: int) -> bytes:
    require(isinstance(command, str) and 1 <= len(command) <= 512 and
            all(32 <= ord(c) <= 126 for c in command),
            "Command must be one printable ASCII line of at most 512 characters")
    require(re.match(r"[Cc]:[A-Za-z0-9_.-]+(?: |$)", command) is not None,
            "The diagnostic command must name a C: command")
    require(type(token) is int and 0 <= token <= 0x7FFFFFFF,
            "Token must fit a nonnegative signed ReadArgs /N LONG")
    # Amiga Shell escapes quoted strings with '*', not a host-shell backslash.
    quoted = command.replace("*", "**").replace('"', '*"')
    return f'C:CopperProbe COMMAND "{quoted}" TOKEN {token}'.encode("ascii")


def insert_invocation(startup: bytes, line: bytes) -> tuple[bytes, dict]:
    require(startup and b"\0" not in startup and b"\r" not in startup.replace(b"\r\n", b""),
            "Startup must contain LF/CRLF text, without NUL or bare CR")
    require(line and not any(c in line for c in (b"\n", b"\r", b"\0")),
            "Probe invocation must occupy exactly one line")
    require(b"copperprobe" not in startup.lower(), "Startup already mentions CopperProbe")
    lines = startup.splitlines(keepends=True)
    matches = [i for i, existing in enumerate(lines)
               if re.match(rb"(?i)^[ \t]*(?:C:)?EndCLI(?:[ \t]|\r?$|\n)", existing)]
    require(len(matches) == 1, "Startup must have exactly one executable EndCLI line")
    index = matches[0]
    target = lines[index]
    require(target.endswith(b"\n"), "EndCLI must have a line terminator")
    newline = b"\r\n" if target.endswith(b"\r\n") else b"\n"
    offset = sum(map(len, lines[:index]))
    addition = line + newline
    result = startup[:offset] + addition + startup[offset:]
    require(result[:offset] + result[offset + len(addition):] == startup,
            "Insertion changed original startup bytes")
    return result, {"original_endcli_line": index + 1, "inserted_line": index + 1,
                    "insertion_offset": offset, "added_bytes": len(addition),
                    "newline_hex": newline.hex(), "original_bytes_preserved": True}


def validate_probe(data: bytes) -> dict:
    require(40 <= len(data) <= MAX_PROBE_BYTES, "Probe HUNK is missing or exceeds 128 KiB")
    try:
        parsed = hunk_reader.parse_single_code(data)
    except hunk_reader.HunkError as error:
        raise ProbePreparationError("Probe HUNK rejected: " + str(error)) from error
    return {"format": "one plain CODE HUNK", "code_bytes": len(parsed.code),
            "relocations": len(parsed.relocation_offsets), "symbols": len(parsed.symbols),
            "runtime_or_shipping_approval": False}


def read_version_replacement(source: Path, probe: Path, output: Path, command: str,
                             candidate: Path | None, expected: str | None) -> tuple[Path, bytes, dict] | None:
    require((candidate is None) == (expected is None),
            'Version replacement path and expected SHA-256 must be supplied together')
    if candidate is None:
        return None
    require(re.match(r'(?i)C:Version(?: |$)', command) is not None,
            'Version replacement is limited to a C:Version diagnostic invocation')
    require(re.fullmatch(r'[0-9a-f]{64}', expected) is not None,
            'Expected Version replacement SHA-256 is invalid')
    candidate = candidate.resolve(strict=True)
    require(candidate.is_file() and not candidate.samefile(source) and not candidate.samefile(probe)
            and not candidate.is_relative_to(output), 'Version replacement aliases another input or output')
    require(candidate.stat().st_size <= MAX_PROBE_BYTES, 'Version replacement exceeds 128 KiB')
    data = candidate.read_bytes()
    require(sha(data) == expected, 'Version replacement SHA-256 mismatch')
    return candidate, data, validate_probe(data)


def replacement_snapshot_name(version_replacement: Path | None,
                              command_replacement: Path | None) -> str:
    require((version_replacement is None) != (command_replacement is None),
            'Exactly one replacement option family must be selected')
    return ('version-replacement.hunk' if version_replacement is not None
            else 'command-replacement.hunk')


def read_command_replacement(source: Path, probe: Path, output: Path,
                             command: str, guest_path: str,
                             candidate: Path | None, expected: str | None,
                             entries: dict | None = None) -> tuple[Path, bytes, dict, str] | None:
    """Bind one existing C command to a replacement CODE HUNK.

    The Workbench disk-2 inventory remains the fixed input. The caller names
    the exact existing ``C/<name>`` path, and the invocation must execute that
    same command. This keeps replacement authority narrow while supporting
    source-backed comparisons beyond Version.
    """
    require((candidate is None) == (expected is None),
            'Command replacement path and expected SHA-256 must be supplied together')
    if candidate is None:
        return None
    require(isinstance(guest_path, str) and
            re.fullmatch(r'C/[A-Za-z0-9_][A-Za-z0-9_.-]{0,63}', guest_path) is not None,
            'Replacement target must be one existing C/<command> path')
    name = guest_path[2:]
    require(re.match(rf'(?i)C:{re.escape(name)}(?: |$)', command) is not None,
            'Command replacement must execute its exact C: command name')
    require(re.fullmatch(r'[0-9a-f]{64}', expected) is not None,
            'Expected command replacement SHA-256 is invalid')
    candidate = candidate.resolve(strict=True)
    require(candidate.is_file() and not candidate.samefile(source) and not candidate.samefile(probe)
            and not candidate.is_relative_to(output), 'Command replacement aliases another input or output')
    require(candidate.stat().st_size <= MAX_PROBE_BYTES, 'Command replacement exceeds 128 KiB')
    data = candidate.read_bytes()
    require(sha(data) == expected, 'Command replacement SHA-256 mismatch')
    if entries is None:
        entries = inspect_adf(source.read_bytes())['entries']
    original = entries.get(guest_path.lower())
    require(original is not None and original['path'] == guest_path and original['kind'] == -3
            and original['metadata']['comment_hex'] == '',
            'Replacement target must be an audited ordinary C file with an empty comment')
    require(original['sha256'] != expected, 'Command replacement is identical to the original member')
    parsed = validate_probe(data)
    return candidate, data, parsed, guest_path


def _name_bucket(name: str) -> int:
    value = len(name)
    for byte in name.encode("latin-1"):
        value = (value * 13 + (byte - 32 if 97 <= byte <= 122 else byte)) & 0x7FF
    return value % 72


def inspect_adf(data: bytes) -> dict:
    """Read-only independent tree, contents, metadata and bounded allocation audit."""
    require(len(data) == IMAGE_BYTES and data[:4] == b"DOS\1", "Requires a DD DOS1 FFS ADF")
    disk = inventory.Adf(data)
    tree = disk.walk()
    root = disk.header(disk.root, (2,), (1,))
    require(root[126] in (0, root[79]) and root[104] == 0 and root[78] == 0xFFFFFFFF,
            "Unsupported root residue/bitmap extension or invalid bitmap")
    require(root[79] != 0 and not any(root[80:104]), "Expected one bitmap block")
    owners: dict[int, str] = {}

    def claim(number: int, owner: str):
        disk.block(number)
        require(number not in owners, f"Shared filesystem block {number}: {owners.get(number)} / {owner}")
        owners[number] = owner

    claim(disk.root, "root")
    claim(root[79], "bitmap")
    entries = {}
    for key, item in tree.items():
        kind = item["secondary_type"]
        require(kind in (-3, 2), "Linked or unsupported filesystem entry: " + key)
        claim(item["block"], "header:" + key)
        words = disk.header(item["block"], (2,), (kind,))
        block = disk.block(item["block"])
        require(words[117] == words[118] == 0, "Unsupported link metadata: " + key)
        require(block[328] <= 79, "Invalid classic comment length: " + key)
        metadata = {"protection": words[80], "datestamp": list(words[105:108]),
                    "comment_hex": block[329:329 + block[328]].hex(),
                    "owner_words": list(words[78:80])}
        facts = {"path": item["path"], "name": item["name"], "kind": kind,
                 "header_block": item["block"], "metadata": metadata}
        if kind == 2:
            require(words[126] == 0, "Directory extension is unsupported")
        else:
            payload = disk.read_file(item["block"])
            facts.update(bytes=len(payload), sha256=sha(payload))
            current = item["block"]
            file_blocks, extensions = [], []
            while current:
                first = current == item["block"]
                words = disk.header(current, (2,) if first else (16,), (-3,))
                if not first:
                    claim(current, "extension:" + key)
                    extensions.append(current)
                count = words[2]
                require(count <= 72 and not any(words[6:78 - count]),
                        "Invalid inactive file data pointers: " + key)
                for number in reversed(words[78 - count:78]):
                    claim(number, "data:" + key)
                    file_blocks.append(number)
                current = words[126]
            facts.update(data_blocks=file_blocks, extension_blocks=extensions)
        entries[key] = facts

    directories = [("", disk.root)] + [(key, facts["header_block"])
                    for key, facts in entries.items() if facts["kind"] == 2]
    for path, number in directories:
        words = disk.header(number, (2,), (1, 2))
        for bucket, head in enumerate(words[6:78]):
            seen = set()
            while head:
                require(head not in seen, "Cyclic directory hash chain")
                seen.add(head)
                require(_name_bucket(disk.name(head)) == bucket, "Incorrect DOS1 name bucket: " + path)
                head = disk.header(head, (2,), (2, -3))[124]

    bitmap = struct.unpack(">128I", disk.block(root[79]))
    require(sum(bitmap) & 0xFFFFFFFF == 0, "Bitmap checksum mismatch")
    count = len(data) // 512
    allocated = {number for number in range(2, count)
                 if not bitmap[1 + (number - 2) // 32] & (1 << ((number - 2) % 32))}
    require(allocated == set(owners), "Bitmap does not match exact reachable filesystem allocation")
    # The audited original has 49 free padding bits beyond the disk. Preserve
    # them; never treat them as allocatable blocks or silently canonicalize them.
    padding = [bool(bitmap[1 + bit // 32] & (1 << (bit % 32))) for bit in range(count - 2, 4064)]
    return {"image_bytes": len(data), "image_sha256": sha(data), "volume": disk.volume,
            "root_block": disk.root, "bootblocks_sha256": sha(data[:1024]),
            "root_word126": root[126],
            "root_metadata": {"mod": list(root[105:108]), "disk": list(root[118:121]),
                              "create": list(root[121:124])},
            "entries": entries, "block_owners": owners,
            "bitmap": {"block": root[79], "allocated_blocks": len(allocated),
                       "free_blocks": count - 2 - len(allocated), "padding_free_bits": sum(padding),
                       "padding_sha256": sha(bytes(padding)), "exact_reachable_allocation": True}}


def verify_derivative(original: bytes, derived: bytes, probe: bytes, startup: bytes,
                      version_replacement: bytes | None = None,
                      command_replacement: tuple[str, bytes] | None = None) -> dict:
    require(not (version_replacement is not None and command_replacement is not None),
            'Only one command replacement can be applied')
    replacements = {}
    if version_replacement is not None:
        replacements[VERSION_PATH.lower()] = version_replacement
    if command_replacement is not None:
        guest_path, replacement_bytes = command_replacement
        require(re.fullmatch(r'C/[A-Za-z0-9_][A-Za-z0-9_.-]{0,63}', guest_path) is not None,
                'Replacement target must be one C command')
        replacements[guest_path.lower()] = replacement_bytes
    before, after = inspect_adf(original), inspect_adf(derived)
    require(derived[:1024] == original[:1024], "Boot blocks changed")
    require(after["volume"] == before["volume"] and after["root_block"] == before["root_block"] and
            after["root_metadata"] == before["root_metadata"], "Volume/root metadata changed")
    require(after["bitmap"]["padding_sha256"] == before["bitmap"]["padding_sha256"],
            "Original out-of-image bitmap padding changed")
    require(set(after["entries"]) == set(before["entries"]) | {PROBE_PATH.lower()},
            "Derivative tree is not exactly original plus C/CopperProbe")
    disk = inventory.Adf(derived)
    replaced_paths = {STARTUP_PATH.lower()}
    replaced_paths.update(replacements)
    for path, payload in replacements.items():
        require(path in before['entries'], 'Command replacement requires an existing C member')
        require(before['entries'][path]['kind'] == -3, 'Command replacement target is not an ordinary file')
        require(disk.read_file(after['entries'][path]['header_block']) == payload,
                'Command replacement content differs')
    unchanged = []
    for key, old in before["entries"].items():
        new = after["entries"][key]
        require(new["path"] == old["path"] and new["kind"] == old["kind"] and
                new["metadata"] == old["metadata"], "Original metadata/spelling changed: " + key)
        if key == STARTUP_PATH.lower():
            require(disk.read_file(new["header_block"]) == startup, "Startup bytes differ from exact insertion")
        elif key in replacements:
            require(new['kind'] == -3 and disk.read_file(new['header_block']) == replacements[key],
                    'Command replacement content differs')
        else:
            require(new["header_block"] == old["header_block"], "Unrelated header moved: " + key)
            if old["kind"] == -3:
                require(new["sha256"] == old["sha256"] and new["bytes"] == old["bytes"] and
                        new["data_blocks"] == old["data_blocks"] and new["extension_blocks"] == old["extension_blocks"],
                        "Unrelated payload or allocation changed: " + key)
                unchanged.append({"path": old["path"], "bytes": old["bytes"], "sha256": old["sha256"]})
    new_probe = after["entries"][PROBE_PATH.lower()]
    require(new_probe["path"] == PROBE_PATH and new_probe["kind"] == -3 and
            new_probe["metadata"]["protection"] == 0 and disk.read_file(new_probe["header_block"]) == probe,
            "Probe content, exact path or ordinary executable protection differs")
    changed_blocks = [n for n in range(1760) if original[n * 512:(n + 1) * 512] != derived[n * 512:(n + 1) * 512]]
    for number in changed_blocks:
        old_owner = before['block_owners'].get(number, 'free')
        new_owner = after['block_owners'].get(number, 'free')
        allowed_bytes = set()
        if number == before['bitmap']['block']:
            continue  # Exact bounded allocation and unchanged padding checked above.
        if any(old_owner.endswith(':' + path) or new_owner.endswith(':' + path) for path in replaced_paths) or new_owner.endswith(':' + PROBE_PATH.lower()):
            continue  # The only intentionally replaced/new file blocks.
        if old_owner in ('header:c', 'header:s'):
            allowed_bytes.update(range(20, 312))  # checksum + directory hash table
        elif old_owner.startswith('header:'):
            old_path = old_owner.removeprefix('header:')
            if old_path.rpartition('/')[0] in ('c', 's'):
                allowed_bytes.update(range(20, 24))  # checksum
                allowed_bytes.update(range(496, 500))  # sibling hash-chain link
        old_raw = original[number * 512:(number + 1) * 512]
        new_raw = derived[number * 512:(number + 1) * 512]
        require(all(a == b or offset in allowed_bytes for offset, (a, b) in enumerate(zip(old_raw, new_raw))),
                f'Unauthorized raw block mutation at {number} ({old_owner} -> {new_owner})')
    return {"status": "passed", "independent_reader": identity(INVENTORY_PATH),
            "original_tree_entries": len(before["entries"]), "derived_tree_entries": len(after["entries"]),
            "bootblocks_identical": True, "original_metadata_restored": True,
            "all_unrelated_payloads_identical": True, "unchanged_files": unchanged,
            "all_unrelated_raw_blocks_and_slack_preserved": True,
            "root_block_identical": True,
            "version_replacement": VERSION_PATH.lower() in replacements,
            "command_replacement": next((path for path in replacements
                                         if path != VERSION_PATH.lower()), None),
            "replaced_file_paths": sorted(replaced_paths),
            "changed_blocks": [{"block": n, "before_owner": before["block_owners"].get(n, "free"),
                                "after_owner": after["block_owners"].get(n, "free"),
                                "before_sha256": sha(original[n * 512:(n + 1) * 512]),
                                "after_sha256": sha(derived[n * 512:(n + 1) * 512])} for n in changed_blocks],
            "source_bitmap": before["bitmap"], "derived_bitmap": after["bitmap"],
            "bootable_or_guest_execution_approved": False}


def writer_identity() -> dict:
    distribution = importlib.metadata.distribution("amitools")
    require(distribution.version == AMITOOLS_VERSION, "Requires installed amitools==0.8.1")
    module_root = distribution.locate_file("").resolve()
    sources = [distribution.locate_file(item).resolve() for item in distribution.files
               if str(item).replace("\\", "/").startswith("amitools/") and str(item).endswith(".py")]
    require(sources, "Installed writer source identity unavailable")
    return {"name": "amitools", "version": AMITOOLS_VERSION, "license": "GPL-2.0",
            "module_root": str(module_root), "python": identity(Path(sys.executable)),
            "sources": [identity(path) for path in sorted(sources)],
            "invocation": "isolated external public ADFSVolume API; no format/create/boot installation"}


# This host-only driver sees the disposable copy and payload snapshots, never
# the original ADF path. All filesystem writes belong to installed amitools.
WRITER_DRIVER = r'''
import json,sys
from pathlib import Path
job=json.load(sys.stdin)
sys.path.insert(0,job['module_root'])
from amitools.fs.blkdev.BlkDevFactory import BlkDevFactory
from amitools.fs.ADFSVolume import ADFSVolume
from amitools.fs.FSString import FSString
from amitools.fs.MetaInfo import MetaInfo
from amitools.fs.RootMetaInfo import RootMetaInfo
from amitools.fs.TimeStamp import TimeStamp
directory=Path(job['directory']).resolve(strict=True)
image=Path(job['image']).resolve(strict=True)
assert image.parent==directory and image.name=='probe.adf'
device=BlkDevFactory().open(str(image),read_only=False)
root_original=device.read_block(job['root_block'])
volume=ADFSVolume(device)
completed=False
operations=['write C/CopperProbe','delete S/Startup-Sequence','write S/Startup-Sequence']
try:
 volume.open()
 assert volume.get_path_name(FSString('C/CopperProbe')) is None
 assert volume.get_path_name(FSString('S/Startup-Sequence')) is not None
 volume.write_file((directory/'probe.hunk').read_bytes(),FSString('C/CopperProbe'))
 volume.delete(FSString('S/Startup-Sequence'))
 volume.write_file((directory/'startup-patched').read_bytes(),FSString('S/Startup-Sequence'))
 if job['replacement_guest_path']:
  guest_path=job['replacement_guest_path']
  assert volume.get_path_name(FSString(guest_path)) is not None
  volume.delete(FSString(guest_path))
  volume.write_file((directory/job['replacement_snapshot']).read_bytes(),FSString(guest_path))
  operations.extend([f'delete {guest_path}',f'write candidate {guest_path}'])
 for path,meta in job['restore'].items():
  node=volume.get_path_name(FSString(path))
  assert node is not None
  assert meta['comment_hex']==''
  node.change_meta_info(MetaInfo(protect=meta['protection'],mod_ts=TimeStamp(*meta['datestamp'])))
 # New probe: ordinary RWED, PURE/SCRIPT clear; reproducible source startup date.
 volume.get_path_name(FSString('C/CopperProbe')).change_meta_info(
  MetaInfo(protect=0,mod_ts=TimeStamp(*job['probe_date'])))
 meta=job['root_metadata']
 assert volume.change_meta_info(RootMetaInfo(create_ts=TimeStamp(*meta['create']),
                        disk_ts=TimeStamp(*meta['disk']),mod_ts=TimeStamp(*meta['mod'])))
 completed=True
finally:
 volume.close()
 # No root entries were added or removed; C/S links and bitmap location must
 # remain identical. Preserve exact original root padding/reserved fields too.
 if completed:
  device.write_block(job['root_block'],root_original)
 device.close()
if job['replacement_guest_path']:
 operations.append('restore exact original '+job['replacement_guest_path']+' metadata/protection')
print(json.dumps({'operations':operations+['restore C/S/startup metadata','restore root dates',
 'restore exact captured root block via installed block-device writer'],
 'filesystem_created':False,'boot_code_installed':False}))
'''


def write_copy(directory: Path, before: dict, writer: dict, version_replacement: bool = False,
               command_replacement_path: str | None = None) -> dict:
    # amitools 0.8.1's nonempty comment mutator calls len(FileName), which
    # fails. The audited affected entries have empty comments; reject other
    # inputs before opening the copy rather than patching the external writer.
    require(not (version_replacement and command_replacement_path is not None),
            'Only one command replacement can be applied')
    replacement_guest_path = VERSION_PATH if version_replacement else command_replacement_path
    replacement_key = replacement_guest_path.lower() if replacement_guest_path else None
    restore_paths = ('c', 's', 's/startup-sequence') + ((replacement_key,) if replacement_key else ())
    require(all(before['entries'][path]['metadata']['comment_hex'] == '' for path in restore_paths),
            'Affected entry comments must be empty for the audited writer')
    job = {"directory": str(directory), "image": str(directory / "probe.adf"),
           "restore": {path: before["entries"][path]["metadata"] for path in restore_paths},
           "replacement_guest_path": replacement_guest_path,
           "replacement_snapshot": "version-replacement.hunk" if replacement_guest_path == VERSION_PATH
               else "command-replacement.hunk",
           "probe_date": before["entries"]["s/startup-sequence"]["metadata"]["datestamp"],
           "root_block": before['root_block'],
           "root_metadata": before["root_metadata"]}
    with tempfile.TemporaryDirectory(prefix="copperos-probe-writer-") as temporary:
        snapshot = Path(temporary).resolve()
        for source in writer["sources"]:
            path = Path(source["path"])
            relative = path.relative_to(writer["module_root"])
            require(relative.parts[0] == "amitools" and path.suffix == ".py", "Writer source escaped package")
            data = path.read_bytes()
            require(sha(data) == source["sha256"], "Writer source changed before snapshot")
            target = snapshot / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
        job["module_root"] = str(snapshot)
        completed = subprocess.run([sys.executable, "-I", "-S", "-B", "-c", WRITER_DRIVER],
                                   input=json.dumps(job), capture_output=True, text=True, timeout=60, check=False)
        require(completed.returncode == 0, "External writer failed: " + (completed.stderr or completed.stdout)[-3000:])
        require(not list(snapshot.rglob("*.pyc")), "Writer unexpectedly loaded cached bytecode")
    return {"stdout": completed.stdout, "stderr": completed.stderr,
            "driver_sha256": sha(WRITER_DRIVER.encode()), "result": json.loads(completed.stdout)}


def check_paths(source: Path, probe: Path, output: Path, diagnostics_root: Path = DIAGNOSTICS_ROOT) -> tuple[Path, Path, Path, Path]:
    source, probe = source.resolve(strict=True), probe.resolve(strict=True)
    output, allowed = output.resolve(), diagnostics_root.resolve()
    require(source.is_file() and probe.is_file() and not source.samefile(probe), "Source/probe must be distinct files")
    require(output.parent == allowed and output != allowed and not output.exists(),
            "Output must be a fresh immediate child of the diagnostic directory")
    require(not output.is_relative_to(REPO.resolve()) and not allowed.is_relative_to(REPO.resolve()),
            "Derivative media must remain outside the repository")
    require(not source.is_relative_to(output) and not probe.is_relative_to(output), "Output contains an input")
    return source, probe, output, allowed


def prepare(source: Path, source_sha256: str, probe: Path, probe_sha256: str,
            output: Path, command: str = "C:Version dos.library", token: int = 20260926,
            version_replacement: Path | None = None, version_replacement_sha256: str | None = None,
            command_replacement: Path | None = None, command_replacement_sha256: str | None = None,
            command_replacement_path: str | None = None) -> dict:
    tool_sources_before = tool_source_identities()
    source, probe, output, allowed = check_paths(source, probe, output)
    require(source_sha256 == SOURCE_SHA256, "Only the audited Workbench 3.1 disk 2 source is supported")
    require(re.fullmatch(r"[0-9a-f]{64}", probe_sha256) is not None, "Expected probe SHA-256 is invalid")
    require(source.stat().st_size == IMAGE_BYTES and probe.stat().st_size <= MAX_PROBE_BYTES, "Input size is outside bounds")
    original, authored = source.read_bytes(), probe.read_bytes()
    require(sha(original) == source_sha256 and sha(authored) == probe_sha256, "Input SHA-256 mismatch")
    hunk = validate_probe(authored)
    require(version_replacement is None or command_replacement is None,
            'Use one replacement option family')
    before = inspect_adf(original)
    require(len(before["entries"]) == 174 and PROBE_PATH.lower() not in before["entries"], "Audited tree differs or probe already exists")
    require(all(before['entries'][path]['metadata']['comment_hex'] == ''
                for path in ('c', 's', 's/startup-sequence')),
            'Audited affected entry comments differ')
    if version_replacement is not None:
        replacement = read_version_replacement(source, probe, output, command,
                                               version_replacement, version_replacement_sha256)
    elif command_replacement is not None:
        require(command_replacement_path is not None,
                'Command replacement requires --command-replacement-path')
        replacement = read_command_replacement(source, probe, output, command,
                                               command_replacement_path, command_replacement,
                                               command_replacement_sha256, before['entries'])
    else:
        require(command_replacement_sha256 is None and command_replacement_path is None,
                'Command replacement path/hash supplied without a HUNK')
        replacement = None
    startup = inventory.Adf(original).read_file(before["entries"][STARTUP_PATH.lower()]["header_block"])
    require(sha(startup) == STARTUP_SHA256, "Audited Startup-Sequence SHA-256 differs")
    line = invocation_line(command, token)
    patched, insertion = insert_invocation(startup, line)
    writer = writer_identity()
    allowed.mkdir(parents=True, exist_ok=True)
    # Resolve once more after parent creation to reject junction/symlink changes.
    require(output.resolve().parent == allowed.resolve() and not output.resolve().is_relative_to(REPO.resolve()),
            "Output path changed during preparation")
    output.mkdir(exist_ok=False)
    try:
        for name, data in (("probe.adf", original), ("probe.hunk", authored),
                           ("startup-original", startup), ("startup-patched", patched)):
            with (output / name).open("xb") as stream:
                stream.write(data)
        if replacement is not None:
            snapshot_name = replacement_snapshot_name(version_replacement,
                                                      command_replacement)
            with (output / snapshot_name).open('xb') as stream:
                stream.write(replacement[1])
        require(sha((output / "probe.adf").read_bytes()) == source_sha256, "Exclusive source copy differs")
        guest_path = replacement[-1] if replacement is not None and len(replacement) == 4 else None
        execution = write_copy(output, before, writer,
                               version_replacement=replacement is not None and guest_path is None,
                               command_replacement_path=guest_path)
        derived = (output / "probe.adf").read_bytes()
        validation = verify_derivative(original, derived, authored, patched,
                                       replacement[1] if replacement is not None and guest_path is None else None,
                                       (guest_path, replacement[1]) if replacement is not None and guest_path else None)
        require(writer_identity() == writer, "Installed writer changed during preparation")
        require(sha(source.read_bytes()) == source_sha256 and sha(probe.read_bytes()) == probe_sha256,
                "Original source or authored probe changed during preparation")
        replacement_record = None
        if replacement is not None:
            if len(replacement) == 4:
                candidate, candidate_data, candidate_hunk, guest_path = replacement
            else:
                candidate, candidate_data, candidate_hunk = replacement
                guest_path = VERSION_PATH
            snapshot = output / ('version-replacement.hunk' if guest_path == VERSION_PATH
                                 else 'command-replacement.hunk')
            require(candidate.read_bytes() == candidate_data and snapshot.read_bytes() == candidate_data,
                    'Command replacement input or snapshot changed during preparation')
            original_command = before['entries'][guest_path.lower()]
            replacement_record = {'guest_path': guest_path,
                                  'original_command': {key: original_command[key] for key in ('path', 'bytes', 'sha256', 'metadata')},
                                  'candidate': identity(candidate), 'snapshot': identity(snapshot),
                                  'hunk_structure': candidate_hunk, 'original_metadata_preserved': True,
                                  'experimental': True, 'pure_admission': False, 'shipping': False}
        tool_sources_after = verify_tool_sources_unchanged(tool_sources_before)
        result = {"schema_version": 1, "kind": "workbench31-diagnostic-probe-adf", "status": "prepared",
                  "source_adf": identity(source), "derived_adf": identity(output / "probe.adf"),
                  "probe": identity(probe), "probe_snapshot": identity(output / "probe.hunk"),
                  "source_before_sha256": source_sha256, "source_after_sha256": sha(source.read_bytes()),
                  "invocation": {"command": command, "token": token, "line": line.decode("ascii"), "guest_path": PROBE_PATH},
                  "startup": {"original": identity(output / "startup-original"),
                              "patched": identity(output / "startup-patched"), "insertion": insertion},
                  "probe_hunk_structure": hunk, "writer": writer, "writer_execution": execution,
                  "replacement": replacement_record,
                  "tool_sources_before": tool_sources_before, "tool_sources_after": tool_sources_after,
                  "tool_sources_unchanged": True,
                  "validation": validation, "builder": tool_sources_before[0],
                  "hunk_reader": tool_sources_before[2], "diagnostic_only": True,
                  "original_reference": False, "guest_execution": False, "shipping": False, "pure_admission": False}
        with (output / "probe-prepare.json").open("x", encoding="utf-8", newline="\n") as stream:
            json.dump(result, stream, indent=2)
            stream.write("\n")
        return result
    except Exception as error:
        with (output / "preparation-failed.json").open("x", encoding="utf-8", newline="\n") as stream:
            json.dump({"status": "failed", "error": str(error), "source_before_sha256": source_sha256,
                       "source_after_sha256": sha(source.read_bytes()), "diagnostic_only": True}, stream, indent=2)
            stream.write("\n")
        raise


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-adf", type=Path, required=True)
    parser.add_argument("--source-sha256", required=True)
    parser.add_argument("--probe", type=Path, required=True)
    parser.add_argument("--probe-sha256", required=True)
    parser.add_argument("--output-directory", type=Path, required=True)
    parser.add_argument("--command", default="C:Version dos.library")
    parser.add_argument("--token", type=int, default=20260926)
    parser.add_argument('--version-replacement', type=Path)
    parser.add_argument('--version-replacement-sha256')
    parser.add_argument('--command-replacement', type=Path,
                        help='Experimental candidate HUNK for one existing C command')
    parser.add_argument('--command-replacement-sha256')
    parser.add_argument('--command-replacement-path',
                        help='Exact guest path such as C/Break; must match --command')
    args = parser.parse_args()
    try:
        result = prepare(args.source_adf, args.source_sha256, args.probe, args.probe_sha256,
                         args.output_directory, args.command, args.token,
                         args.version_replacement, args.version_replacement_sha256,
                         args.command_replacement, args.command_replacement_sha256,
                         args.command_replacement_path)
    except (ProbePreparationError, inventory.EvidenceError, OSError, subprocess.SubprocessError) as error:
        parser.exit(1, "Probe preparation rejected: " + str(error) + "\n")
    print(json.dumps({"status": result["status"], "derived_adf": result["derived_adf"],
                      "receipt": str(Path(result["derived_adf"]["path"]).with_name("probe-prepare.json"))}))


if __name__ == "__main__":
    main()
