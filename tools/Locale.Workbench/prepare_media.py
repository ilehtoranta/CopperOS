"""Create private Workbench locale verification disks; never a release image.

Uses the separately installed amitools 0.8.1 filesystem tool. Licensed inputs
are read-only; derived media and receipts belong in ignored artifacts.
"""
import argparse
import contextlib
import hashlib
import io
import json
from pathlib import Path
import shutil
import sys
import zipfile


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def adf(path):
    path = Path(path)
    if path.suffix.lower() == '.zip':
        with zipfile.ZipFile(path) as z:
            members = [n for n in z.namelist() if n.lower().endswith('.adf')]
            if len(members) != 1:
                raise ValueError('Expected exactly one ADF in ' + str(path))
            data = z.read(members[0])
    else:
        data = path.read_bytes()
    if len(data) != 901120 or data[:4] != b'DOS\x01':
        raise ValueError('Expected a standard DD FFS disk: ' + str(path))
    return data


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('workbench', 'locale', 'library', 'shell', 'output', 'host-tools'):
        p.add_argument('--' + name, required=True, type=Path)
    a = p.parse_args()
    if a.output.exists():
        raise ValueError('Use a fresh private output directory')
    sys.path.insert(0, str(a.host_tools.resolve()))
    from amitools.tools.xdftool import main as xdf
    a.output.mkdir(parents=True)
    inputs = {name: {'path': str(path.resolve()), 'sha256': sha(path)} for name, path in
              [('workbench', a.workbench), ('locale', a.locale), ('library', a.library), ('shell', a.shell)]}
    log = io.StringIO()

    def run(image, *args):
        with contextlib.redirect_stdout(log):
            rc = xdf([str(image), *map(str, args)])
        if rc:
            raise RuntimeError('xdftool failed: ' + log.getvalue())

    original = a.output / 'Workbench-original.adf'
    original.write_bytes(adf(a.workbench))
    candidate = a.output / 'Workbench-CopperOS.adf'
    shutil.copyfile(original, candidate)
    run(candidate, 'delete', 'Libs/locale.library', '+', 'write', a.library.resolve(), 'Libs/locale.library')
    run(candidate, 'read', 'Libs/locale.library', (a.output / 'readback.library').resolve())
    if sha(a.output / 'readback.library') != inputs['library']['sha256']:
        raise ValueError('Candidate library filesystem readback mismatch')
    (a.output / 'Locale-original.adf').write_bytes(adf(a.locale))
    run(a.output / 'Locale-original.adf', 'list')
    run(a.output / 'ShellSupport.adf', 'create', '+', 'format', 'ShellSupport', 'DOS1', '+',
        'makedir', 'L', '+', 'write', a.shell.resolve(), 'L/CopperShell')
    run(candidate, 'info')
    for item in inputs.values():
        if sha(item['path']) != item['sha256']:
            raise ValueError('Source media changed')
    (a.output / 'filesystem.log').write_text(log.getvalue(), encoding='utf-8')
    receipt = dict(kind='private-workbench-locale-verification', shipping=False, inputs=inputs,
                   sourceUnchanged=True, outputs=[{'file': f.name, 'sha256': sha(f)} for f in a.output.glob('*.adf')])
    (a.output / 'media.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')
    print(json.dumps(receipt, indent=2))


if __name__ == '__main__':
    main()
