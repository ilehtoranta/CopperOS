"""Compare public guest observations from two full-machine IPrefs boots."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zlib


def framebuffer(path):
    """Read BootReplay's RGB PNG (filter zero); compare pixels, not compression."""
    png = path.read_bytes()
    if png[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError('Not a PNG: ' + str(path))
    at, compressed = 8, bytearray()
    width = height = 0
    while at < len(png):
        size = struct.unpack_from('>I', png, at)[0]
        tag, data = png[at + 4:at + 8], png[at + 8:at + 8 + size]
        if tag == b'IHDR':
            width, height, depth, color, compression, filtering, interlace = struct.unpack('>IIBBBBB', data)
            if (depth, color, compression, filtering, interlace) != (8, 2, 0, 0, 0):
                raise ValueError('Unexpected BootReplay PNG format')
        if tag == b'IDAT':
            compressed.extend(data)
        at += size + 12
    raw = zlib.decompress(compressed)
    stride = width * 3 + 1
    if not width or not height or len(raw) != stride * height or any(raw[y * stride] for y in range(height)):
        raise ValueError('Unexpected BootReplay PNG filtering or size')
    return width, height, raw


def verify(candidate, original):
    errors = []
    phases = {'EN': ('english', 'January', 1), 'DE': ('deutsch', 'Januar', 3),
              'SV': ('svenska', 'Januari', 12), 'BACK': ('english', 'January', 1)}
    fields = ('environmentLanguage', 'january', 'fault205', 'fault103', 'languageName', 'utilityLanguage')
    for label, receipt, version in [('original', original, 40), ('candidate', candidate, 53)]:
        for flag in ('workbenchTaskPresent', 'iprefsSeen', 'sourceUnchanged'):
            if receipt.get(flag) is not True:
                errors.append(f'{label}: {flag} was not true')
        if not any('iprefs' in t.get('Name', '').lower() for t in receipt.get('tasks', [])):
            errors.append(f'{label}: IPrefs task did not remain present')
        if receipt.get('halted') or receipt.get('UnsupportedActiveFeature'):
            errors.append(f'{label}: halted or unsupported machine feature')
        if receipt.get('hostOsOverlays') or receipt.get('scalarDiagnostic'):
            errors.append(f'{label}: execution used an overlay or diagnostic trace')
        if not any(l['Name'] == 'locale.library' and l['Version'] == version for l in receipt['libraries']):
            errors.append(f'{label}: expected locale.library version {version}')
    observed = []
    for phase, (language, january, utility) in phases.items():
        name = 'LocaleProof' + phase
        rows = []
        for label, receipt in [('original', original), ('candidate', candidate)]:
            matches = [r for r in receipt.get('observations', []) if r['name'] == name]
            row = matches[0] if len(matches) == 1 else {}
            rows.append(row)
            if row.get('status') != 1:
                errors.append(f'{label}/{phase}: missing or unsuccessful guest probe')
            for field, expected in [('environmentLanguage', language), ('january', january),
                                    ('utilityLanguage', utility), ('languageName', language + '.language')]:
                if row.get(field) != expected:
                    errors.append(f'{label}/{phase}/{field}: {row.get(field)!r} != {expected!r}')
            if not row.get('fault205') or not row.get('fault103'):
                errors.append(f'{label}/{phase}: empty DOS Fault result')
        for field in fields:
            if rows[0].get(field) != rows[1].get(field):
                errors.append(f'{phase}/{field}: original {rows[0].get(field)!r}, candidate {rows[1].get(field)!r}')
        observed.append(dict(phase=phase, **{field: rows[1].get(field) for field in fields}))
    return dict(passed=not errors, scope='Full-machine Workbench/IPrefs public API comparison',
                candidateReplacementRom=candidate.get('replacement'),
                originalInputs=original['inputs'], candidateInputs=candidate['inputs'],
                observations=observed, errors=errors)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('candidate', 'original', 'output'):
        p.add_argument('--' + name, required=True, type=Path)
    a = p.parse_args()
    result = verify(json.loads(a.candidate.read_text()), json.loads(a.original.read_text()))
    expected = framebuffer(a.original.parent / 'screen.png')
    actual = framebuffer(a.candidate.parent / 'screen.png')
    result['desktopPixelsMatchReference'] = actual == expected
    result['desktopPixelsSha256'] = hashlib.sha256(actual[2]).hexdigest().upper()
    if actual != expected:
        result['errors'].append('Final desktop pixels differ from the reference; visual review required')
        result['passed'] = False
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
