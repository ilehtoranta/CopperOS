"""Private Workbench configuration for real IPrefs notifications and probes."""
import argparse
import contextlib
import hashlib
import io
import json
from pathlib import Path
import shutil
import struct
import sys
from make_probe import probe


def chunk(name, data):
    return name.encode('ascii') + struct.pack('>I', len(data)) + data + bytes(len(data) & 1)


def preferences(country, country_name, language):
    at = country.index(b'CTRY')
    size = struct.unpack_from('>I', country, at + 4)[0]
    if size != 504:
        raise ValueError('Unexpected CountryPrefs size')
    data = bytearray(860)
    for offset, text in [(16, country_name), (48, language), (78, 'english')]:
        data[offset:offset + len(text)] = text.encode('ascii')
    struct.pack_into('>i', data, 348, 0 if language == 'english' else -60)
    data[356:] = country[at + 8:at + 8 + size]
    body = b'PREF' + chunk('PRHD', bytes(6)) + chunk('LCLE', data)
    return b'FORM' + struct.pack('>I', len(body)) + body


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('media', 'output', 'host-tools'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--direct', action='store_true', help='Diagnostic only: apply through locale API when IPrefs cannot start')
    p.add_argument('--original', action='store_true', help='Keep the original Workbench locale.library for reference comparison')
    p.add_argument('--locale-on-support', action='store_true', help='Put the two selected language drivers and DOS catalogs on DF1')
    a = p.parse_args()
    if a.output.exists():
        raise ValueError('Use a fresh output directory')
    a.output.mkdir(parents=True)
    sys.path.insert(0, str(a.host_tools.resolve()))
    from amitools.tools.xdftool import main as xdf
    log = io.StringIO()
    def run(image, *args):
        with contextlib.redirect_stdout(log):
            result = xdf([str(image), *map(str, args)])
        if result:
            raise RuntimeError(log.getvalue())
    workbench, support = a.output / 'Workbench.adf', a.output / 'Support.adf'
    shutil.copyfile(a.media / ('Workbench-original.adf' if a.original else 'Workbench-CopperOS.adf'), workbench)
    shutil.copyfile(a.media / 'ShellSupport.adf', support)
    sequence = a.output / 'Startup-Sequence'
    run(workbench, 'read', 'S/Startup-Sequence', sequence.resolve())
    locale_drive = 'DF1:' if a.locale_on_support else 'DF2:'
    sequence.write_bytes(sequence.read_bytes().replace(b'LOCALE: SYS:Locale', ('LOCALE: ' + locale_drive).encode('ascii')))
    run(workbench, 'delete', 'S/Startup-Sequence', '+', 'write', sequence.resolve(), 'S/Startup-Sequence')
    if a.locale_on_support:
        for directory in ['Languages', 'Catalogs', 'Catalogs/deutsch', 'Catalogs/deutsch/sys', 'Catalogs/svenska', 'Catalogs/svenska/sys']:
            run(support, 'makedir', directory)
        for language in ['deutsch', 'svenska']:
            for resource in ['Languages/' + language + '.language', 'Catalogs/' + language + '/sys/dos.catalog']:
                local = a.output / (language + '-' + Path(resource).name)
                run(a.media / 'Locale-original.adf', 'read', resource, local.resolve())
                run(support, 'write', local.resolve(), resource)
    user = []
    for phase, language, country in [('EN', 'english', 'united_states'), ('DE', 'deutsch', 'deutschland'), ('SV', 'svenska', 'sverige'), ('BACK', 'english', 'united_states')]:
        country_path = a.output / (country + '.country')
        if not country_path.exists():
            run(a.media / 'Locale-original.adf', 'read', 'Countries/' + country + '.country', country_path.resolve())
        prefs = a.output / (language + '.prefs')
        prefs.write_bytes(preferences(country_path.read_bytes(), country + '.country', language))
        if phase != 'BACK':
            run(support, 'write', prefs.resolve(), language + '.prefs')
        command = a.output / ('Probe' + phase)
        command.write_bytes(probe('LocaleProof' + phase, delay=600 if phase != 'EN' else 0,
                                  apply=('DF1:' + language + '.prefs') if a.direct else None))
        run(support, 'write', command.resolve(), command.name)
        if phase != 'EN' and not a.direct:
            user.append('C:Copy DF1:' + language + '.prefs ENV:sys/locale.prefs CLONE')
        user.append('DF1:' + command.name)
    user_path = a.output / 'User-Startup'; user_path.write_bytes(('\n'.join(user) + '\n').encode('ascii'))
    run(workbench, 'write', user_path.resolve(), 'S/User-Startup')
    (a.output / 'filesystem.log').write_text(log.getvalue(), encoding='utf-8')
    receipt = dict(kind='direct-locale-diagnostic' if a.direct else 'full-workbench-iprefs-switch', shipping=False, originalLibrary=a.original,
                   inputMedia=json.loads((a.media / 'media.json').read_text()), startupChanges=['LOCALE assigned to ' + locale_drive, 'S/User-Startup with public DOS copy and guest probes'],
                   userStartup=user, outputs={f.name: hashlib.sha256(f.read_bytes()).hexdigest().upper() for f in (workbench, support)})
    (a.output / 'media.json').write_text(json.dumps(receipt, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
