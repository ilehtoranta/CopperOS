"""Add a retained native desktop loader to a private locale Workbench fixture."""
import argparse
import contextlib
import hashlib
import io
import json
from pathlib import Path
import shutil
import sys
import zipfile
from make_probe import Assembler


def loader(entry):
    a = Assembler()
    def call(lvo): a.words(0x4eae, lvo)
    a.words(0x48e7, 0x3f3e, 0x2c78, 4)
    a.d(0, 32768); a.d(1, 0x10001); call(-198)
    a.words(0x2640, 0x4a80); a.branch(0x6700, 'failed-early')
    a.address(0, 'swap'); a.words(0x2080, 0x0680, 0, 32768, 0x2140, 4, 0x2140, 8); call(-732)
    a.address(1, 'dos'); a.d(0, 37); call(-552); a.words(0x2840, 0x4a80); a.branch(0x6700, 'failed')
    a.words(0x2c4c); a.address(0, 'payload'); a.words(0x2208); call(-150)
    a.words(0x4a80); a.branch(0x6700, 'close-failed')
    a.words(0xe580, 0x5880, 0x2440, 0xd5fc, entry >> 16, entry, 0x2c78, 4)
    a.words(0x48e7, 0x3f3e, 0x4e92, 0x4cdf, 0x7cfc, 0x2a40)
    a.branch(0x6000, 'close')
    a.label('close-failed'); a.words(0x7a00)
    a.label('close'); a.words(0x2c78, 4, 0x224c); call(-414)
    a.words(0x4a85); a.branch(0x6700, 'failed'); a.words(0x7a00); a.branch(0x6000, 'restore')
    a.label('failed'); a.words(0x7a14)
    a.label('restore'); a.words(0x2c78, 4); a.address(0, 'swap'); call(-732)
    a.words(0x224b); a.d(0, 32768); call(-210)
    a.words(0x2005, 0x4cdf, 0x7cfc, 0x4e75)
    a.label('failed-early'); a.words(0x4cdf, 0x7cfc, 0x7014, 0x4e75)
    a.text('dos', 'dos.library'); a.text('payload', 'DF1:CopperDesktop')
    a.label('swap'); a.words(0, 0, 0, 0, 0, 0)
    return a.finish()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('fixture', 'desktop', 'manifest', 'output', 'host-tools'):
        p.add_argument('--' + name, type=Path, required=True)
    p.add_argument('--shell', type=Path)
    p.add_argument('--shell-manifest', type=Path)
    p.add_argument('--storage-keymaps', type=Path,
                   help='Restore missing private boot drawers and d/s/usa2 keymaps from a supplied Storage disk ZIP or ADF')
    p.add_argument('--locale-disk', type=Path,
                   help='Copy a supplied Locale ADF and give its volume a distinct label for the LOCALE assign')
    p.add_argument('--locale-library', type=Path, help='Install a fresh locale.library in the private Workbench copy')
    p.add_argument('--locale-manifest', type=Path, help='Producer manifest for --locale-library')
    p.add_argument('--autoload', action='store_true',
                   help='Install as L:CopperDesktop for the production Shell loader; retain original startup commands')
    a = p.parse_args()
    if bool(a.shell) != bool(a.shell_manifest):
        raise ValueError('Supply both --shell and --shell-manifest')
    if bool(a.locale_library) != bool(a.locale_manifest):
        raise ValueError('Supply both --locale-library and --locale-manifest')
    locale_library_manifest = json.loads(a.locale_manifest.read_text()) if a.locale_manifest else None
    if a.output.exists(): raise ValueError('Use a fresh private directory')
    a.output.mkdir(parents=True)
    sys.path.insert(0, str(a.host_tools.resolve()))
    from amitools.tools.xdftool import main as xdf
    def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest().upper()
    manifest = json.loads(a.manifest.read_text())
    if sha(a.desktop) != manifest['sha256']: raise ValueError('Desktop hash mismatch')
    if a.autoload and manifest['entry'] != 0:
        raise ValueError('Production desktop autoload requires entry offset zero')
    shell_manifest = json.loads(a.shell_manifest.read_text()) if a.shell else None
    if a.shell and sha(a.shell) != shell_manifest['sha256']: raise ValueError('Shell hash mismatch')
    disk = a.output / 'Workbench.adf'
    source = a.fixture / 'Workbench.adf'; before = sha(source)
    sources = {name: sha(a.fixture / name) for name in ('Workbench.adf', 'Support.adf')}
    shutil.copyfile(source, disk); shutil.copyfile(a.fixture / 'Support.adf', a.output / 'Support.adf')
    log = io.StringIO()
    def run(*args, image=disk):
        with contextlib.redirect_stdout(log): rc = xdf([str(image), *map(str, args)])
        if rc: raise RuntimeError(log.getvalue())
    if a.locale_library:
        if sha(a.locale_library) != locale_library_manifest['Sha256'] or a.locale_library.stat().st_size != locale_library_manifest['ImageBytes']:
            raise ValueError('Locale library hash/size mismatch')
        if locale_library_manifest['Name'] != 'locale.library': raise ValueError('Unexpected locale library name')
        run('delete', 'Libs/locale.library', '+', 'write', a.locale_library.resolve(), 'Libs/locale.library')
        readback = a.output / 'locale-library-readback'
        run('read', 'Libs/locale.library', readback.resolve())
        if sha(readback) != locale_library_manifest['Sha256']: raise ValueError('Locale library readback mismatch')
    restored_drawers, installed_keymaps, storage_source = [], {}, None
    if a.storage_keymaps:
        storage_hash = sha(a.storage_keymaps)
        storage = a.output / 'Storage-source.adf'
        if zipfile.is_zipfile(a.storage_keymaps):
            with zipfile.ZipFile(a.storage_keymaps) as archive:
                entries = [name for name in archive.namelist() if name.lower().endswith('.adf')]
                if len(entries) != 1: raise ValueError('Storage ZIP must contain one ADF')
                storage.write_bytes(archive.read(entries[0]))
        else:
            shutil.copyfile(a.storage_keymaps, storage)
        storage_source = dict(path=str(a.storage_keymaps.resolve()), sha256=storage_hash,
                              adfSha256=sha(storage))
        # The supplied low-density Workbench image has drawer icons without
        # their directories. Complete a private boot copy; retain every startup
        # command and preserve normal Assign/Path failure semantics.
        for target in ('Devs/Keymaps', 'Rexxc', 'WBStartup', 'Tools', 'Tools/Commodities'):
            with contextlib.redirect_stdout(log): exists = xdf([str(disk), 'list', target]) == 0
            if not exists:
                run('makedir', target)
                restored_drawers.append(target)
        for name in ('d', 's', 'usa2'):
            source_file = a.output / ('keymap-' + name)
            readback = a.output / ('keymap-readback-' + name)
            run('read', 'Keymaps/' + name, source_file.resolve(), image=storage)
            run('write', source_file.resolve(), 'Devs/Keymaps/' + name)
            run('read', 'Devs/Keymaps/' + name, readback.resolve())
            if sha(source_file) != sha(readback): raise ValueError('Keymap readback mismatch')
            installed_keymaps[name] = sha(source_file)
        if sha(a.storage_keymaps) != storage_hash: raise ValueError('Storage source changed')
    locale_media = None
    if a.locale_disk:
        locale_source_hash = sha(a.locale_disk)
        locale_disk = a.output / 'Locale.adf'
        shutil.copyfile(a.locale_disk, locale_disk)
        original = locale_disk.read_bytes()
        root_block = int.from_bytes(original[8:12], 'big') or len(original) // 1024
        root_start = root_block * 512
        old_label = original[root_start + 433:root_start + 433 + original[root_start + 432]].decode('latin-1')
        run('relabel', 'LocaleSupport', image=locale_disk)
        prepared = locale_disk.read_bytes()
        if original[:root_start] != prepared[:root_start] or original[root_start + 512:] != prepared[root_start + 512:]:
            raise ValueError('Locale relabel changed data outside its root block')
        if sha(a.locale_disk) != locale_source_hash: raise ValueError('Locale source changed')
        locale_media = dict(source=str(a.locale_disk.resolve()), sourceSha256=locale_source_hash,
                            preparedSha256=sha(locale_disk), oldLabel=old_label, label='LocaleSupport',
                            rootBlock=root_block, fileDataUnchanged=True)
    sequence = a.output / 'Startup-Sequence'
    run('read', 'S/Startup-Sequence', sequence.resolve())
    startup_source_sha256 = sha(sequence)
    if not a.autoload:
        command = a.output / 'WBInit'; command.write_bytes(loader(manifest['entry']))
        sequence.write_bytes(b'C:WBInit\n' + sequence.read_bytes())
        run('write', command.resolve(), 'C/WBInit', '+',
            'delete', 'S/Startup-Sequence', '+', 'write', sequence.resolve(), 'S/Startup-Sequence')
    if a.shell:
        run('delete', 'L/CopperShell', '+', 'write', a.shell.resolve(), 'L/CopperShell', image=a.output / 'Support.adf')
        run('read', 'L/CopperShell', (a.output / 'shell-readback.hunk').resolve(), image=a.output / 'Support.adf')
        if sha(a.output / 'shell-readback.hunk') != shell_manifest['sha256']: raise ValueError('Shell readback mismatch')
    desktop_path = 'L/CopperDesktop' if a.autoload else 'CopperDesktop'
    run('write', a.desktop.resolve(), desktop_path, image=a.output / 'Support.adf')
    run('read', desktop_path, (a.output / 'desktop-readback.hunk').resolve(), image=a.output / 'Support.adf')
    if sha(a.output / 'desktop-readback.hunk') != manifest['sha256']: raise ValueError('Readback mismatch')
    if any(sha(a.fixture / name) != expected for name, expected in sources.items()): raise ValueError('Source changed')
    if a.locale_library and sha(a.locale_library) != locale_library_manifest['Sha256']: raise ValueError('Locale library source changed')
    (a.output / 'filesystem.log').write_text(log.getvalue())
    (a.output / 'desktop-media.json').write_text(json.dumps(dict(shipping=False, sourceUnchanged=True,
        sourceSha256=before, sources=sources, desktop=manifest, shell=shell_manifest,
        startupSourceSha256=startup_source_sha256, startupPreparedSha256=sha(sequence),
        workbenchSha256=sha(disk), supportSha256=sha(a.output / 'Support.adf'),
        autoload=a.autoload,
        restoredDrawers=restored_drawers, installedKeymaps=installed_keymaps, storageSource=storage_source,
        localeMedia=locale_media, localeLibrary=locale_library_manifest,
        changes=['Retained disk-loaded native desktop before original Startup-Sequence',
                 'Production Shell autoload from L:CopperDesktop' if a.autoload else 'Explicit WBInit diagnostic command']), indent=2))


if __name__ == '__main__': main()
