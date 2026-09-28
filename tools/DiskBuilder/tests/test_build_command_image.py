"""Serializer integration and release-boundary rejection, using owned test data.

Positive filesystem cases are not command qualification or release admission.
The publication-race test explicitly stubs admission to isolate the later check;
there is no such override in the production CLI.
"""
import hashlib
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

TOOLS = Path(__file__).resolve().parents[1]
REPO = TOOLS.parents[1]
sys.path.insert(0, str(TOOLS))
import build_command_image as builder
import verify_image as reader


class CommandImageTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="copperos-disk-test-")
        self.folder = Path(self.temporary.name).resolve()
        self.assertEqual(self.folder.parent, Path(tempfile.gettempdir()).resolve())
        self.addCleanup(self.temporary.cleanup)
        self.files = []
        for index, bits in enumerate((0, 2, 15, 32, 64, 96, 255)):
            payload = bytes([index]) * (100000 if index == 0 else index)
            source = self.folder / f"payload-{index}.bin"
            source.write_bytes(payload)
            self.files.append({"installed_path": f"Data/Flags{index}", "path": str(source),
                               "sha256": hashlib.sha256(payload).hexdigest(), "bytes": len(payload),
                               "amiga_protection": bits})
        source = self.folder / "empty.bin"
        source.write_bytes(b"")
        self.files.append({"installed_path": "Empty", "path": str(source),
                           "sha256": hashlib.sha256(b"").hexdigest(), "bytes": 0, "amiga_protection": 0})

    def write(self, name, files=None, size=None):
        image = self.folder / name
        builder._write_filesystem(image, files or self.files, "CopperOS", size)
        return image

    def test_adf_metadata_extended_files_and_input_order_are_reproducible(self):
        first = self.write("first.adf")
        second = self.write("second.adf", list(reversed(self.files)))
        self.assertEqual(first.read_bytes(), second.read_bytes())
        report = reader.verify_image(first, self.files, "CopperOS")
        self.assertFalse(report["shipping_qualified"])
        self.assertTrue(report["bitmap"]["exact_reachable_allocation"])
        self.assertTrue(any(item["extension_blocks"] for item in report["files"]))
        self.assertEqual({item["amiga_protection"] for item in report["files"]}, {0, 2, 15, 32, 64, 96, 255})

    def test_raw_hdf_with_bitmap_extension_is_read_independently(self):
        image = self.write("filesystem.hdf", size=52)
        report = reader.verify_image(image, self.files, "CopperOS")
        self.assertEqual(report["image_bytes"], 52 * 1024 * 1024)
        self.assertTrue(report["bitmap"]["extension_blocks"])

    def test_existing_image_and_invalid_paths_never_start_writer(self):
        image = self.folder / "existing.adf"
        image.write_bytes(b"preserve existing image")
        with patch.object(builder.subprocess, "run") as run:
            with self.assertRaises(builder.ImageBuildError):
                builder._write_filesystem(image, self.files, "CopperOS")
            for name in ("../Escape", "/Absolute", "C:Root", "C//Empty", "C/+"):
                files = [{**self.files[0], "installed_path": name}]
                with self.assertRaises(builder.ImageBuildError):
                    self.write("invalid.adf", files)
            with self.assertRaises(builder.ImageBuildError):
                self.write("duplicate.adf", [self.files[0], dict(self.files[0])])
            run.assert_not_called()
        self.assertEqual(image.read_bytes(), b"preserve existing image")

    def test_empty_shipping_manifest_never_falls_back_or_creates_outputs(self):
        inventory = REPO / "docs/Commands/Workbench31MorphOS320/command-inventory.json"
        manifest = REPO / "docs/Commands/Workbench31MorphOS320/build-manifest.json"
        image, receipt = self.folder / "release.adf", self.folder / "release.json"
        with patch.object(builder.subprocess, "run") as run:
            with self.assertRaisesRegex(builder.ImageBuildError, "No admitted release records"):
                builder.build_release_image(manifest, inventory, builder.release.sha256(inventory),
                    image, receipt, "wb31", "68000", "normal-runtime")
            run.assert_not_called()
        self.assertFalse(image.exists())
        self.assertFalse(receipt.exists())

    def test_claimed_shipping_and_pure_flags_cannot_admit_a_candidate(self):
        inventory_path = REPO / "docs/Commands/Workbench31MorphOS320/command-inventory.json"
        inventory = builder.release.load_json(inventory_path)
        record = {"command": "makelink", "profile": "wb31", "cpu": "68000",
                  "distribution": "normal-runtime", "installed_path": "C/MakeLink",
                  "shipping": True, "pure_admission": True, "amiga_protection": 32,
                  "status": "passed", "path": self.files[0]["path"],
                  "sha256": self.files[0]["sha256"], "bytes": self.files[0]["bytes"]}
        manifest = {"schema_version": 1, "qualified_shipping_artifacts": [record]}
        with self.assertRaisesRegex(builder.ImageBuildError, "Release admission failed"):
            builder._select(manifest, inventory, REPO, "wb31", "68000", "normal-runtime")

    def test_publication_rejects_change_after_readback_with_stubbed_admission(self):
        manifest = self.folder / "manifest.json"
        manifest.write_text('{"schema_version":1}', encoding="utf-8")
        inventory = self.folder / "inventory.json"
        inventory.write_text('{"fixture":true}', encoding="utf-8")
        image, receipt = self.folder / "changed.adf", self.folder / "changed.json"
        records = [{**self.files[0], "command": "owned-test-data"}]
        real_verify = reader.verify_image
        def mutate_after_read(path, files, volume):
            report = real_verify(path, files, volume)
            data = bytearray(path.read_bytes())
            data[-1] ^= 1
            path.write_bytes(data)
            return report
        with patch.object(builder, "_select", return_value=(records, [])), \
             patch.object(reader, "verify_image", side_effect=mutate_after_read):
            with self.assertRaisesRegex(builder.ImageBuildError, "changed after verified readback"):
                builder.build_release_image(manifest, inventory, builder.release.sha256(inventory),
                    image, receipt, "wb31", "68000", "normal-runtime")
        self.assertFalse(image.exists())
        self.assertFalse(receipt.exists())


if __name__ == "__main__":
    unittest.main()
