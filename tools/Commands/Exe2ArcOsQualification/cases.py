"""Inert native-DOS inputs and exact Exe2Arc command/output contracts."""
import json
from pathlib import Path
import struct
import sys
import zlib

PREFIX = 'RAM:Exe2ArcQ/'
OUTPUT = PREFIX+'payload.arc'
VALID = 'Valid types are: zip (Zip), ace (Ace), rar (Rar), cab (Cabinet), arj (Arj), lha (LhA), lzh (Amiga-LhA)\n'
FOUND = ' Found.\n'
WARNING = 'Be careful and test the file for correctness.\n'
SOURCES = [('ZIP.IN', 'zip.exe'), ('ACE.IN', 'ace.exe'), ('RAR.IN', 'rar.exe'), ('CAB.IN', 'cab.exe'),
           ('ARJ.IN', 'arj.exe'), ('LHA.IN', 'lha.exe'), ('LZH.IN', 'lzh.exe'), ('RANDOM.IN', 'random.exe'),
           ('EMPTY.IN', 'empty.exe'), ('ZERO.IN', 'zero.exe'), ('BADZIP.IN', 'badzip.exe'), ('SHORTZIP.IN', 'shortzip.exe'),
           ('CROSS.IN', 'cross.exe'), ('BIG.IN', 'big.exe'), ('RAR.IN', 'quoted name.exe'), ('RAR.IN', 'nosuffix')]


def input_mask(cases):
    mask = 0; names = set()
    for i, (name, path) in enumerate(SOURCES):
        if any(PREFIX+path in c['Arguments'] for c in cases): mask |= 1 << i; names.add(name)
    return mask, names


def archive(kind, at=32, length=128):
    data = bytearray([0x55])*length
    if kind == 'rar': data[at:at+7] = b'Rar!\x1a\x07\0'
    elif kind == 'ace': data[at+7:at+14] = b'**ACE**'
    elif kind == 'cab':
        data[at:at+4] = b'MSCF'; struct.pack_into('<I', data, at+8, 36); struct.pack_into('<I', data, at+16, 20)
    elif kind == 'arj':
        data[at:at+4] = b'\x60\xea\x06\0'; data[at+4:at+10] = bytes(range(1, 7))
        struct.pack_into('<I', data, at+10, zlib.crc32(data[at+4:at+10]))
    elif kind == 'lha':
        struct.pack_into('>I', data, 0, 0x3f3); data[44:48] = b'SFX!'; struct.pack_into('>I', data, 52, at)
    elif kind == 'lzh': data[20] = 2; data[at+2:at+7] = b'-lh5-'
    return bytes(data)


def zip_data():
    data = bytearray([0x55])*160
    data[10:14] = b'PK\x03\x04'; struct.pack_into('<I', data, 28, 3)
    data[36:40] = bytes(4); data[40:43] = b'\xa1\xb2\xc3'
    data[43:47] = b'PK\x01\x02'; data[71:77] = bytes(6); struct.pack_into('<I', data, 85, 0)
    data[89:93] = b'PK\x05\x06'; struct.pack_into('<IIH', data, 101, 46, 33, 0)
    output = bytearray(data[10:111]); struct.pack_into('<I', output, 75, 10); struct.pack_into('<I', output, 95, 43)
    return bytes(data), bytes(output)


def build(stage):
    inputs = stage/'inputs'; inputs.mkdir(exist_ok=True)
    payloads = {}
    for kind in ['zip', 'ace', 'rar', 'cab', 'arj', 'lha', 'lzh']:
        if kind == 'zip': data, payload = zip_data()
        else:
            at, length = (100, 200) if kind == 'lha' else (32, 128)
            data = archive(kind, at, length); payload = data[at:at+36] if kind == 'cab' else data[at:]
        (inputs/(kind.upper()+'.IN')).write_bytes(data); payloads[kind] = payload
    z, zp = zip_data(); bad = bytearray(z); bad[10] = 0x99
    short = bytearray(z); struct.pack_into('<I', short, 28, 102400)
    extras = {'RANDOM': bytes(256), 'EMPTY': b'', 'ZERO': archive('rar', 0), 'BADZIP': bad,
              'SHORTZIP': short, 'CROSS': archive('rar', 102398, 102430), 'BIG': archive('rar', 32, 102445)}
    for name, data in extras.items(): (inputs/(name+'.IN')).write_bytes(data)
    contracts = []
    def add(name, arguments, rc=0, error=0, text='', payload=b'', exists=False, path=OUTPUT, ctrl_c=False):
        contracts.append(dict(Name=name, Arguments=arguments, CtrlC=ctrl_c, OutputPath=path,
                              expectedReturn=rc, expectedIoErr=error, expectedOutput=text,
                              expectedPayload=payload.hex(), expectedExists=exists))
    def success(name, source, kind, to=OUTPUT, explicit=True, path=None):
        payload = payloads[kind]; path = path or to
        args = f'"{PREFIX+source}"'+(f' "{to}"' if to is not None else '')+(f' TYPE {kind}' if explicit else '')
        add(name, args, text=FOUND+f'Saved {len(payload)} byte to {path}\n'+WARNING, payload=payload, exists=True, path=path)
    for kind in payloads: success('typed-'+kind, kind+'.exe', kind)
    for kind in payloads: success('automatic-'+kind, kind+'.exe', kind, explicit=False)
    success('generated-name', 'rar.exe', 'rar', to=None, path=PREFIX+'rar.rar')
    success('quoted-source', 'quoted name.exe', 'rar')
    success('no-suffix', 'nosuffix', 'rar', to=None, path=PREFIX+'nosuffix.rar')
    success('directory-fallback', 'rar.exe', 'rar', to='RAM:Exe2ArcOut', path='RAM:Exe2ArcOut/rar.rar')
    add('required-missing', '', 20, 116, 'required argument missing\n')
    add('unterminated-quote', '"unterminated', 20, 120, 'argument line invalid or too long\n')
    add('unknown-type', f'{PREFIX}rar.exe {OUTPUT} TYPE bogus', 20, 0,
        "\r\x1b[KDid not find archive of type 'bogus'.\n"+VALID)
    add('typed-no-match', f'{PREFIX}random.exe {OUTPUT} TYPE rar', 20, 0,
        "\r\x1b[KDid not find archive of type 'rar'.\nMaybe it is an archive of another type?\n"+VALID)
    add('automatic-no-match', f'{PREFIX}random.exe {OUTPUT}', 20, 0, '\r\x1b[KDid not find archive data.\n')
    add('empty', f'{PREFIX}empty.exe {OUTPUT} TYPE rar', 20, 0,
        "\r\x1b[KDid not find archive of type 'rar'.\nMaybe it is an archive of another type?\n"+VALID)
    add('missing-input', f'{PREFIX}missing.exe {OUTPUT} TYPE rar', 20, 205, 'Failed to open input file.\n')
    add('unsafe-position', f'{PREFIX}zero.exe {OUTPUT} TYPE rar', 20, 0, '\r\x1b[KArchive position is unsafe.\n')
    add('illegal-zip-record', f'{PREFIX}badzip.exe {OUTPUT} TYPE zip', 20, 0, FOUND+'Unknown or illegal data found.\n')
    add('truncated-zip-record', f'{PREFIX}shortzip.exe {OUTPUT} TYPE zip', 20, 0, FOUND+'Unexpected end of data.\n')
    bad_to='RAM:Exe2ArcQ/missing/output.arc'
    add('output-open-failure', f'{PREFIX}rar.exe {bad_to} TYPE rar', 20, 205, FOUND+'\nFailed to open output file.\n', path=bad_to)
    add('zip-ctrl-c', f'{PREFIX}zip.exe {OUTPUT} TYPE zip', 0, 304, FOUND+f'Saved 150 byte to {OUTPUT}\n'+WARNING, exists=True, ctrl_c=True)
    add('cross-window', f'{PREFIX}cross.exe {OUTPUT} TYPE rar', text=FOUND+f'Saved 32 byte to {OUTPUT}\n'+WARNING,
        payload=extras['CROSS'][102398:], exists=True)
    add('multiple-chunks', f'{PREFIX}big.exe {OUTPUT} TYPE rar', text=FOUND+f'Saved 102413 byte to {OUTPUT}\n'+WARNING,
        payload=extras['BIG'][32:], exists=True)
    save(stage/'contracts.json', contracts)
    def rows(names, stack=4096, phase='corpus'):
        return [dict(next(c for c in contracts if c['Name']==name), Image=image, Stack=stack, phase=phase)
                for name in names for image in [0, 1]]
    warm = rows(['typed-rar', 'typed-rar'], phase='warmup')
    batches = {'smoke': warm+rows(['typed-zip', 'typed-rar']),
               'typed': warm+rows([c['Name'] for c in contracts[:7]]),
               'automatic': warm+rows([c['Name'] for c in contracts[7:14]]),
               'names': warm+rows([c['Name'] for c in contracts[14:18]]),
               'errors1': warm+rows([c['Name'] for c in contracts[18:24]]),
               'errors2': warm+rows([c['Name'] for c in contracts[24:30]]),
               'signals': warm+rows(['zip-ctrl-c']),
               'cross-baseline': rows(['cross-window'])[:1],
               'cross-candidate': rows(['cross-window'])[1:],
               'large': warm+rows(['multiple-chunks']),
               'repeat': warm+rows(['typed-rar', 'typed-zip', 'missing-input', 'typed-no-match', 'required-missing'], phase='repeat')+
                         rows(['typed-zip', 'typed-rar', 'required-missing', 'missing-input', 'typed-no-match'], stack=8192, phase='repeat')}
    for name, data in batches.items():
        assert len(data) <= 42; save(stage/('cases-'+name+'.json'), data)
    assert {c['Name'] for c in contracts} == {c['Name'] for name, data in batches.items() if name!='smoke' for c in data}
    return list(batches)


def save(path, value): path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')

if __name__ == '__main__': build(Path(sys.argv[1]))
