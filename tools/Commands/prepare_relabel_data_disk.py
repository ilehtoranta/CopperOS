"""Create owned-data-only writable media for the Relabel persistence probe."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/DiskBuilder"))
from build_command_image import _write_filesystem
from verify_image import verify_image


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    args = parser.parse_args()
    folder = args.directory.resolve()
    if folder.exists():
        raise ValueError("Use a fresh data directory.")
    folder.mkdir(parents=True)
    files = []
    for name, data, protection in [("proof", b"relabel-payload\n", 0),
                                   ("nested/guard", bytes(range(256)) * 7, 2)]:
        source = folder / (name.replace("/", "-") + ".bin")
        source.write_bytes(data)
        files.append({"installed_path": name, "path": str(source), "bytes": len(data),
                      "sha256": hashlib.sha256(data).hexdigest(), "amiga_protection": protection})
    disk = folder / "initial.adf"
    writer = _write_filesystem(disk, files, "FixtureDisk")
    readback = verify_image(disk, files, "FixtureDisk")
    (folder / "creation.json").write_text(json.dumps({"writer": writer, "readback": readback}, indent=2) + "\n")
    for role in ["reference", "candidate"]:
        receipt = {"scope": "Owned data only; disposable writable DF1, no shipping commands.",
                   "inputPath": str(disk), "inputSha256": readback["image_sha256"],
                   "outputPath": str(folder / (role + "-after.adf")), "initialVolume": "FixtureDisk",
                   "finalVolume": "SavedDisk", "files": files,
                   "creationSha256": hashlib.sha256((folder / "creation.json").read_bytes()).hexdigest(),
                   "producerSha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}
        (folder / (role + "-data.json")).write_text(json.dumps(receipt, indent=2) + "\n")
    print(folder)


if __name__ == "__main__":
    main()
