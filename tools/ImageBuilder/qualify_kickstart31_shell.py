"""Run a test-only file-driven Shell probe on a disposable WinUAE HDF.

The probe runs the supplied Shell-Seg through DOS RunCommand with file I/O.
It does not qualify the normal interactive startup path or complete Shell ABI.
"""
import argparse
import hashlib
import json
import subprocess
import sys
import struct
from pathlib import Path

from build_system_image import Disk, verify
from make_kickstart_shell_probe import (build as build_probe, build_status_probe,
                                       build_system_probe, build_system_failure_probe,
                                       build_system_async_probe, build_system_lifetime_probe)

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'DiskBuilder'))
from verify_image import Adf

SCRIPT = (b'Echo >SYS:shell-echo.txt hello\n'
          b'C:Eval 1 + 2 >SYS:shell-eval.txt\n'
          b'C:Version C:Eval FILE >SYS:shell-version.txt\n'
          b'C:MakeDir SYS:ShellProbeDir\n'
          b'C:Dir SYS:ShellProbeDir >SYS:shell-dir.txt\n'
          b'CopperOSNoSuchCommand\n'
          b'C:ShellStatus\n'
          b'Echo >SYS:shell-done.txt done\n')


def sha(data): return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--image', required=True, type=Path)
    parser.add_argument('--rom', required=True, type=Path)
    parser.add_argument('--winuae', required=True, type=Path)
    parser.add_argument('--output-directory', required=True, type=Path)
    parser.add_argument('--powershell', default='pwsh')
    parser.add_argument('--boot-path', action='store_true',
                        help='Exercise BootShell child creation using a test file as standard input')
    parser.add_argument('--system-probe', action='store_true', help='Exercise DOS SystemTagList through the registered Shell')
    parser.add_argument('--lifetime-probe', action='store_true', help='Measure memory across repeated asynchronous Shell launches')
    args = parser.parse_args()
    source = args.image.resolve(strict=True)
    original = source.read_bytes()
    source_disk = Adf(original)
    tree = source_disk.walk()
    forbidden = {'c/shellprobe', 'c/shellstatus', 'c/shellsystem', 'c/shellfailure', 'c/shellasync', 'c/shelllife', 'c/shellsyseval', 's/shell-probe-input', 'shell-probe-output',
                 'shell-echo.txt', 'shell-eval.txt', 'shell-version.txt',
                 'shell-dir.txt', 'shell-done.txt', 'shell-status.txt', 'shellprobedir', 'systemresult.txt', 'systemchild.txt', 'systemfailure.txt',
                 'asyncresult.txt', 'asyncchild.txt', 'asynclife.bin', 'systemeval.txt', 'systemevalresult.txt'}
    if forbidden.intersection(tree): raise ValueError('Input already contains Shell probe paths')
    disk = Disk(16384)
    dirs = {name: disk.directory(disk.root, name) for name in ('C', 'L', 'S')}
    for key, entry in sorted(tree.items()):
        if entry['secondary_type'] == 2 and '/' not in key and entry['path'] in dirs: continue
        if entry['secondary_type'] != -3 or entry['path'].split('/')[0] not in dirs:
            raise ValueError('Unexpected input tree entry: ' + entry['path'])
        path = entry['path']
        data = source_disk.read_file(entry['block'])
        if key == 's/startup-sequence': data = b'C:ShellProbe\n'
        disk.file(dirs[path.split('/')[0]], path, data)
    disk.file(dirs['C'], 'C/ShellProbe', build_probe(boot_path=args.boot_path))
    disk.file(dirs['C'], 'C/ShellStatus', build_status_probe())
    if args.system_probe:
        disk.file(dirs['C'], 'C/ShellSystem', build_system_probe())
        disk.file(dirs['C'], 'C/ShellFailure', build_system_failure_probe())
        disk.file(dirs['C'], 'C/ShellAsync', build_system_async_probe())
        disk.file(dirs['C'], 'C/ShellSysEval', build_system_probe('C:Eval 1 + 2 >SYS:systemeval.txt\n', 'SYS:systemevalresult.txt'))
    script = SCRIPT.replace(b'Echo >SYS:shell-done.txt done\n', b'C:ShellSystem\nC:ShellFailure\nC:ShellAsync\nC:ShellSysEval\nEcho >SYS:shell-done.txt done\n') if args.system_probe else SCRIPT
    if args.lifetime_probe:
        disk.file(dirs['C'], 'C/ShellLife', build_system_lifetime_probe())
        script = script.replace(b'Echo >SYS:shell-done.txt done\n', b'C:ShellLife\nEcho >SYS:shell-done.txt done\n')
    disk.file(dirs['S'], 'S/shell-probe-input', script)
    image = disk.finish(); verify(image, disk.files)
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=False)
    probe = output / 'probe.hdf'; probe.write_bytes(image)
    runner = Path(__file__).with_name('run_kickstart31_candidate.ps1')
    subprocess.run([args.powershell, '-NoProfile', '-File', str(runner), '-Image', str(probe),
                    '-KickstartRom', str(args.rom.resolve(strict=True)), '-WinUAE', str(args.winuae.resolve(strict=True)),
                    '-OutputDirectory', str(output / 'run'), '-DurationSeconds', '45'], check=True)
    resulting = (output / 'run' / 'candidate.hdf').read_bytes()
    result_disk = Adf(resulting); result_tree = result_disk.walk()
    def read(path):
        entry = result_tree.get(path)
        return result_disk.read_file(entry['block']) if entry and entry['secondary_type'] == -3 else None
    observations = {'echo': read('shell-echo.txt'), 'eval': read('shell-eval.txt'),
                    'version': read('shell-version.txt'), 'shellOutput': read('shell-probe-output'),
                    'done': read('shell-done.txt'), 'status': read('shell-status.txt'),
                    'systemResult': read('systemresult.txt'), 'systemChild': read('systemchild.txt'),
                    'systemFailure': read('systemfailure.txt'),
                    'asyncResult': read('asyncresult.txt'), 'asyncChild': read('asyncchild.txt'),
                    'systemEval': read('systemeval.txt'), 'systemEvalResult': read('systemevalresult.txt'),
                    'directory': 'shellprobedir' in result_tree,
                    'dirOutputPresent': read('shell-dir.txt') is not None}
    lifetime = read('asynclife.bin') if args.lifetime_probe else None
    observations['asyncPublicMemory'] = list(struct.unpack('>III', lifetime)) if lifetime is not None and len(lifetime) == 12 else None
    (output / 'observations.json').write_text(json.dumps({k: v.decode('latin1') if isinstance(v, bytes) else v
                                                           for k, v in observations.items()}, indent=2) + '\n')
    if (observations['echo'] != b'hello\n' or observations['eval'] != b'3\n' or
        observations['version'] != b'Eval 1.0\n' or observations['done'] != b'done\n' or observations['status'] != b'OK\n' or
        not observations['directory'] or not observations['dirOutputPresent'] or
        b'Command not found' not in (observations['shellOutput'] or b'')):
        raise RuntimeError('Shell probe mismatch; see observations.json')
    if args.system_probe and (observations['systemResult'] != b'OK\n' or observations['systemChild'] != b'C:\n' or
                              observations['systemFailure'] != b'OK\n' or observations['asyncResult'] != b'OK\n' or
                              observations['asyncChild'] != b'async\n' or observations['systemEval'] != b'3\n' or
                              observations['systemEvalResult'] != b'OK\n'):
        raise RuntimeError('DOS SystemTagList Shell probe failed; see observations.json')
    if args.lifetime_probe and observations['asyncPublicMemory'] is None:
        raise RuntimeError('No repeated-launch memory measurements; see observations.json')
    if args.lifetime_probe and len(set(observations['asyncPublicMemory'])) != 1:
        raise RuntimeError('Public memory changed across the repeated asynchronous launch batches; see observations.json')
    if source.read_bytes() != original: raise RuntimeError('Input image changed')
    receipt = {'kind': 'kickstart31-file-driven-shell-probe', 'runtimeQualified': False, 'bootPath': args.boot_path,
               'inputImageSha256': sha(original), 'probeImageSha256': sha(image),
               'resultImageSha256': sha(resulting),
               'asyncPublicMemory': observations['asyncPublicMemory'],
               'limits': 'Test startup and file input; normal interactive startup and full Shell ABI are not qualified'}
    (output / 'qualification.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__': main()
