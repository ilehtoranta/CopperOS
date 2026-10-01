"""Build a command distribution volume from admitted release-manifest records.

The production entry point always runs the command release preflight. It has no
development override, P override, or fallback artifact search. The filesystem
writer uses the external GPLv2 amitools tool; no third-party code or reference
operating-system files are copied into CopperOS or emitted command payloads.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.metadata
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile

REPO = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "tools/Commands"))
import verify_command_release as release

AMITOOLS_VERSION = "0.8.1"
EPOCH = "01.01.1978 00:00:00.00"
XDF_DRIVER = ("import json,sys; job=json.load(sys.stdin); sys.path.insert(0,job['module_root']); "
              "from amitools.tools.xdftool import main; sys.exit(main(job['arguments']))")
NAME = re.compile(r"[A-Za-z0-9_.-]{1,30}\Z")


class ImageBuildError(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise ImageBuildError(message)


def bound(path: Path):
    return {"path": str(path.resolve()), "sha256": release.sha256(path)}


def host_tool_identity():
    """Capture the installed external tool's version and Python source files."""
    distribution = importlib.metadata.distribution("amitools")
    require(distribution.version == AMITOOLS_VERSION,
            "Install the pinned host dependency: amitools==" + AMITOOLS_VERSION)
    paths = [distribution.locate_file(item).resolve() for item in distribution.files
             if str(item).startswith("amitools/") and str(item).endswith(".py")]
    require(bool(paths), "Installed amitools source identity is unavailable")
    return {"name": "amitools", "version": distribution.version,
            "invocation": "external xdftool subprocess", "license": "GPL-2.0",
            "module_root": str(distribution.locate_file("").resolve()),
            "python": bound(Path(sys.executable)), "sources": [bound(path) for path in sorted(paths)]}


def _path_parts(name):
    require(isinstance(name, str), "Image path must be text")
    parts = name.split("/")
    require(bool(parts) and all(NAME.fullmatch(part) and part not in (".", "..") for part in parts),
            "Image path must contain plain relative classic Amiga names: " + name)
    return parts


def _protection_text(bits):
    require(type(bits) is int and 0 <= bits <= 255, "Amiga protection must be an explicit byte")
    # HS PA are positive bits; RWED are denial bits. Host attributes are never read.
    flags = "".join(letter for bit, letter in enumerate("dewrapsh")
                    if bool(bits & (1 << bit)) == (bit >= 4))[::-1]
    # xdftool's empty string leaves the default mask (0); explicit dashes
    # select no permissions (15). They are metadata, never process options.
    return flags or "--------"


def _write_filesystem(image: Path, files: list[dict], volume: str, hdf_mib: int | None = None):
    """Internal serialization primitive; callers own admission, snapshots and readback.

    Exposed for data-only filesystem tests. Do not call this primitive to bypass
    release admission; build_release_image is the command packaging entry point.
    """
    require(not image.exists(), "Filesystem writer requires a new private image")
    require(NAME.fullmatch(volume) and volume not in (".", "..") and not volume.startswith("-"),
            "Volume must be a plain classic Amiga name")
    require(hdf_mib is None or type(hdf_mib) is int and 1 <= hdf_mib <= 512,
            "Raw HDF size must be 1..512 MiB; larger volumes require separate qualification")
    require(image.suffix.lower() == (".adf" if hdf_mib is None else ".hdf"), "Image extension/geometry mismatch")
    require(isinstance(files, list) and bool(files), "Cannot build an empty command volume")
    directories, paths = {}, set()
    ordered = sorted(files, key=lambda item: item["installed_path"].casefold())
    for item in ordered:
        parts = _path_parts(item["installed_path"])
        folded = item["installed_path"].casefold()
        require(folded not in paths, "Duplicate image file: " + item["installed_path"])
        paths.add(folded)
        for count in range(1, len(parts)):
            directory = "/".join(parts[:count])
            previous = directories.setdefault(directory.casefold(), directory)
            require(previous == directory, "Inconsistent directory spelling")
        _protection_text(item["amiga_protection"])
        require(Path(item["path"]).is_file(), "Missing snapshotted payload")
    require(not paths.intersection(directories), "Image file/directory collision")
    commands = [["create"] + ([] if hdf_mib is None else [f"size={hdf_mib}Mi", "h=1", "s=32"]),
                ["format", volume, "DOS1"]]
    for name in sorted(directories.values(), key=lambda value: (value.count("/"), value.casefold())):
        commands.append(["makedir", name])
    for item in ordered:
        commands.extend([["write", str(Path(item["path"]).resolve()), item["installed_path"]],
                         ["protect", item["installed_path"], _protection_text(item["amiga_protection"])],
                         ["time", item["installed_path"], EPOCH]])
    # Directory mutation updates directory dates. Freeze them after all writes,
    # then freeze all three root dates last so output is independent of wall time.
    for name in sorted(directories.values(), reverse=True):
        commands.extend([["protect", name, "rwed"], ["time", name, EPOCH]])
    root_commands = [["root", operation, EPOCH] for operation in ("create_time", "disk_time", "time")]
    executions = []
    # Reopen before changing root dates. In amitools 0.8.1 a just-created root
    # does not have its read-validation flag set, and RootCmd rejects edits until
    # it is read back. Closing also flushes the allocation bitmap before that read.
    tool = host_tool_identity()
    # Run exactly the captured package sources. Exclude installed bytecode,
    # site initialization, working-directory modules and PYTHONPATH. The host
    # Python/standard library remain a prerequisite, not a hermetic toolchain.
    with tempfile.TemporaryDirectory(prefix="copperos-xdftool-") as temporary:
        module_root = Path(temporary).resolve()
        require(module_root.parent == Path(tempfile.gettempdir()).resolve() and
                module_root.name.startswith("copperos-xdftool-"), "Unexpected host-tool workspace")
        for source in tool["sources"]:
            path = Path(source["path"])
            relative = path.relative_to(tool["module_root"])
            require(relative.parts[0] == "amitools" and path.suffix == ".py",
                    "Unexpected host-tool source path")
            data = path.read_bytes()
            require(hashlib.sha256(data).hexdigest() == source["sha256"], "Host-tool source changed while snapshotting")
            target = module_root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
        for phase in (commands, root_commands):
            arguments = ["--", str(image.resolve())]
            for index, command in enumerate(phase):
                if index:
                    arguments.append("+")
                arguments.extend(command)
            # JSON is data sent over stdin, never shell text; full argument lists
            # can exceed Windows' process-command-line limit for a distribution.
            completed = subprocess.run([sys.executable, "-I", "-S", "-B", "-c", XDF_DRIVER],
                                       input=json.dumps({"module_root": str(module_root), "arguments": arguments}),
                                       capture_output=True, text=True, check=False)
            require(completed.returncode == 0,
                    "xdftool failed; no final image published: " + (completed.stderr or completed.stdout)[-3000:])
            executions.append({"commands": phase, "stdout": completed.stdout, "stderr": completed.stderr})
        require(not list(module_root.rglob("*.pyc")), "Unexpected host-tool bytecode cache")
    require(image.stat().st_size == (901120 if hdf_mib is None else hdf_mib * 1024 * 1024),
            "Image size differs from selected geometry")
    from canonicalize_bitmap import canonicalize_bitmap_padding
    canonical = canonicalize_bitmap_padding(image)
    return {"host_tool": tool, "phases": executions, "bitmap_canonicalization": canonical}


def _select(manifest, inventory, root, profile, cpu, distribution):
    require(profile in release.PROFILES and cpu in release.CPUS and distribution in release.DISTRIBUTIONS,
            "Unsupported profile/CPU/distribution selection")
    require(isinstance(manifest, dict) and manifest.get("schema_version") == 1 and
            isinstance(manifest.get("qualified_shipping_artifacts"), list), "Unsupported release manifest")
    records = [item for item in manifest["qualified_shipping_artifacts"] if isinstance(item, dict) and
               (item.get("profile"), item.get("cpu"), item.get("distribution")) == (profile, cpu, distribution)]
    require(bool(records), "No admitted release records for selection; no development fallback")
    decisions = []
    for record in records:
        expected = {key: record.get(key) for key in ("command", "installed_path")}
        expected.update(profile=profile, cpu=cpu, distribution=distribution)
        decision = release.preflight_manifest(manifest, inventory, root, expected)
        require(decision["eligible_for_staging"] is True,
                "Release admission failed for " + str(record.get("command")) + ": " +
                json.dumps({"errors": decision["errors"], "blockers": decision["blockers"]}))
        decisions.append(decision)
    return records, decisions


def build_release_image(manifest_path: Path, inventory_path: Path, inventory_sha256: str,
                        output: Path, receipt: Path, profile: str, cpu: str,
                        distribution: str, volume: str = "CopperOS", hdf_mib: int | None = None,
                        root: Path = REPO):
    from verify_image import verify_image

    root = root.resolve(strict=True)
    output, receipt = output.resolve(), receipt.resolve()
    manifest_path, inventory_path = manifest_path.resolve(strict=True), inventory_path.resolve(strict=True)
    require(output != receipt and not output.exists() and not receipt.exists(), "Output/receipt must be distinct new files")
    require(output.suffix.lower() == (".adf" if hdf_mib is None else ".hdf"), "Output extension/geometry mismatch")
    require(not output.is_relative_to((root / "filesystem").resolve()),
            "Build a new distribution image outside the live filesystem tree")
    inventory_path = release.bound_file({"path": str(inventory_path), "sha256": inventory_sha256}, root, "inventory")
    manifest_identity, inventory_identity = bound(manifest_path), bound(inventory_path)
    manifest, inventory = release.load_json(manifest_path), release.load_json(inventory_path)
    records, decisions = _select(manifest, inventory, root, profile, cpu, distribution)
    tool = host_tool_identity()
    with tempfile.TemporaryDirectory(prefix="copperos-image-") as temporary:
        private = Path(temporary)
        require(private.resolve().parent == Path(tempfile.gettempdir()).resolve() and
                private.name.startswith("copperos-image-"), "Unexpected temporary workspace")
        snapshots = []
        for index, record in enumerate(records):
            source = release.bound_file({"path": record["path"], "sha256": record["sha256"]}, root, "release payload")
            data = source.read_bytes()
            require(hashlib.sha256(data).hexdigest() == record["sha256"] and len(data) == record["bytes"],
                    "Release payload changed while snapshotting")
            snapshot = private / f"payload-{index:04d}.bin"
            snapshot.write_bytes(data)
            snapshots.append({**record, "path": str(snapshot)})
        staged = private / ("distribution.adf" if hdf_mib is None else "distribution.hdf")
        execution = _write_filesystem(staged, snapshots, volume, hdf_mib)
        readback = verify_image(staged, records, volume)
        # Revalidate admission/source freshness after image generation too.
        require(bound(manifest_path) == manifest_identity and bound(inventory_path) == inventory_identity,
                "Manifest/inventory changed during image build")
        _, refreshed = _select(manifest, inventory, root, profile, cpu, distribution)
        require(refreshed == decisions and host_tool_identity() == tool and execution["host_tool"] == tool,
                "Admission or host tool changed during image build")
        image_bytes = staged.read_bytes()
        require(hashlib.sha256(image_bytes).hexdigest() == readback["image_sha256"] and
                len(image_bytes) == readback["image_bytes"], "Staged image changed after verified readback")
        output.parent.mkdir(parents=True, exist_ok=True)
        receipt.parent.mkdir(parents=True, exist_ok=True)
        # Exclusive creates never replace a historical image or receipt. A late
        # write failure cannot produce an accepted pair: the receipt is last.
        with output.open("xb") as stream:
            stream.write(image_bytes)
        require(output.read_bytes() == image_bytes, "Final image write/readback differs")
        readback = verify_image(output, records, volume)
        result = {"schema_version": 1, "status": "command-volume-built", "profile": profile,
                  "cpu": cpu, "distribution": distribution, "volume": volume,
                  "image": bound(output), "image_bytes": len(image_bytes),
                  "format": "DOS1 FFS ADF" if hdf_mib is None else "DOS1 FFS raw HDF",
                  "bootable_system_qualified": False, "manifest": manifest_identity,
                  "inventory": inventory_identity, "host_tool": tool,
                  "builder": bound(Path(__file__)), "reader": bound(Path(__file__).with_name("verify_image.py")),
                  "admission": decisions, "image_readback": readback,
                  "file_count": len(records), "files": [{key: record[key] for key in
                      ("command", "installed_path", "sha256", "bytes", "amiga_protection")} for record in records],
                  "external_tool_log": execution,
                  "scope": "Admitted command distribution files and exact Amiga filesystem metadata; no boot code, RDB partitions, full SYS boot or additional command qualification is supplied by packaging."}
        with receipt.open("x", encoding="utf-8", newline="\n") as stream:
            json.dump(result, stream, indent=2)
            stream.write("\n")
        return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("manifest", "inventory", "output", "receipt"):
        parser.add_argument("--" + name, type=Path, required=True)
    for name in ("inventory-sha256", "profile", "cpu", "distribution"):
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--volume", default="CopperOS")
    parser.add_argument("--hdf-mib", type=int)
    args = parser.parse_args()
    try:
        result = build_release_image(args.manifest, args.inventory, args.inventory_sha256,
                                     args.output, args.receipt, args.profile, args.cpu,
                                     args.distribution, args.volume, args.hdf_mib)
    except (ValueError, OSError, KeyError, TypeError, importlib.metadata.PackageNotFoundError) as error:
        print(json.dumps({"status": "rejected", "image_admitted": False, "error": str(error)}))
        return 1
    print(json.dumps({"status": result["status"], "image": result["image"], "files": result["file_count"]}))
    return 0


if __name__ == "__main__":
    sys.exit(main())
