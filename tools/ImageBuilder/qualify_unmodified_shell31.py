"""Boot an unmodified system ADF under Kickstart or CopperStart.

The input image is mounted as DF0 without modifying its startup. An optional
read-only DF1 helper contains only the mathtrans probe. Guest writes are
exported to a separate ADF for inspection. This is a focused boot check, not
full Shell qualification.
"""
import argparse
import hashlib
import importlib.util
import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--extension-rom', type=Path, help='CopperStart extension ROM; omit for original Kickstart')
    parser.add_argument('--probe-dll', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--mathtrans', action='store_true',
                        help='Mount a read-only helper in DF1 and probe SYS:Libs/mathtrans.library')
    parser.add_argument('--mathtrans-full', action='store_true',
                        help='Check all 17 vectors, capture 20 numerical/CCR samples, and test overflow traps')
    args = parser.parse_args()
    args.mathtrans = args.mathtrans or args.mathtrans_full
    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    original_disk = Adf(original)
    original_entries = original_disk.walk()
    if 't/exact' in original_entries or (args.mathtrans and 't/mathresult' in original_entries):
        raise ValueError('Input image already contains the command result path')
    library_bytes = None
    if args.mathtrans:
        library = original_entries.get('libs/mathtrans.library')
        if library is None or library['secondary_type'] != -3:
            raise ValueError('Input image has no SYS:Libs/mathtrans.library')
        library_bytes = original_disk.read_file(library['block'])
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    result = output / 'result.adf'
    probe = args.probe_dll.resolve(strict=True)
    emulator = probe.with_name('CopperMod.Amiga.Lightweight.dll')
    rom = args.rom.resolve(strict=True)
    rom_bytes = rom.read_bytes()
    extension = args.extension_rom.resolve(strict=True) if args.extension_rom else None
    extension_bytes = extension.read_bytes() if extension else None
    emulator_bytes = emulator.read_bytes()
    probe_bytes = probe.read_bytes()
    command = ['dotnet', str(probe), str(rom), str(source), str(result)]
    helper_bytes = None
    if args.mathtrans:
        from build_system_image import Disk
        module_path = Path(__file__).resolve().parents[1] / 'MathTrans.Workbench' / ('make_edge_probe.py' if args.mathtrans_full else 'make_probe.py')
        spec = importlib.util.spec_from_file_location('mathtrans_probe', module_path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        helper = Disk()
        helper.header(helper.root, 0, 'MathProbe', 1)
        helper.file(helper.root, 'p', module.build(40, 'SYS:Libs/mathtrans.library', 'SYS:T/mathresult', full=True) if args.mathtrans_full else module.build(40, 'SYS:Libs/mathtrans.library', 'SYS:T/mathresult'))
        helper_bytes = helper.finish()
        helper_disk = Adf(helper_bytes)
        if helper_disk.volume == original_disk.volume or set(helper_disk.walk()) != {'p'}:
            raise ValueError('Helper must contain only the probe on a distinct volume')
        helper_path = output / 'helper.adf'
        helper_path.write_bytes(helper_bytes)
        command.append(str(helper_path))
    if extension:
        if not args.mathtrans:
            command.append('')
        command.append(str(extension))
    subprocess.run(command, check=True)
    if source.read_bytes() != original:
        raise RuntimeError('Input proof image changed during the run')
    if probe.read_bytes() != probe_bytes or emulator.read_bytes() != emulator_bytes:
        raise RuntimeError('Emulator inputs changed during the run')
    if rom.read_bytes() != rom_bytes:
        raise RuntimeError('Kickstart ROM changed during the run')
    if extension and extension.read_bytes() != extension_bytes:
        raise RuntimeError('CopperStart extension ROM changed during the run')
    result_bytes = result.read_bytes()
    disk = Adf(result_bytes)
    entry = disk.walk().get('t/exact')
    if entry is None or entry['secondary_type'] != -3 or disk.read_file(entry['block']) != b'ok\n':
        raise RuntimeError('Shell command did not produce the expected SYS:T/exact file')
    if args.mathtrans:
        entries = disk.walk()
        report = entries.get('t/mathresult')
        report_bytes = disk.read_file(report['block']) if report else b''
        if args.mathtrans_full:
            if len(report_bytes) != 124 or report_bytes[:4] != bytes(4):
                raise RuntimeError(f'Full staged mathtrans probe failed: {report_bytes.hex()}')
        elif report_bytes != b'RESULT 0\n':
            raise RuntimeError('The probe did not load SYS:Libs/mathtrans.library successfully')
        staged = entries.get('libs/mathtrans.library')
        if staged is None or disk.read_file(staged['block']) != library_bytes:
            raise RuntimeError('The staged library changed during the run')
        if helper_path.read_bytes() != helper_bytes:
            raise RuntimeError('Read-only helper image changed during the run')
    receipt = {'kind': 'copperstart-unmodified-shell-boot-probe' if extension else 'kickstart31-unmodified-shell-boot-probe', 'runtimeQualified': False,
               'inputImageSha256': sha(original), 'resultImageSha256': sha(result_bytes),
               'romSha256': sha(rom_bytes), 'probeSha256': sha(probe_bytes),
               'extensionRomSha256': sha(extension_bytes) if extension_bytes is not None else None,
               'emulatorSha256': sha(emulator_bytes),
               'mathtransResult': 'PASS: 17 vectors, 20 samples, 6 overflow/trap stages' if args.mathtrans_full else 'RESULT 0' if args.mathtrans else None,
               'mathtransSamplesHex': report_bytes[4:].hex() if args.mathtrans_full else None,
               'librarySha256': sha(library_bytes) if library_bytes is not None else None,
               'helperImageSha256': sha(helper_bytes) if helper_bytes is not None else None,
               'limits': 'Interactive boot test on a 2 MiB Chip RAM emulator; complete Shell not qualified'}
    (output / 'qualification.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
