"""Preserve Dir's installed/fixed images and decode changed method bodies."""
import argparse
import json
from pathlib import Path
import re
import shutil
import struct
import sys

from measure_c import identity, parse_map, git_state

REPO = Path(__file__).resolve().parents[2]


def save(path, value): path.write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')


def methods(path):
    return {m[3]: dict(offset=int(m[1], 16), bytes=int(m[2])) for m in
            re.finditer(r'^([0-9A-F]{8})\s+(\d+)\s+(.+::.+)$', path.read_text(encoding='utf-8-sig'), re.M)}


def code(path):
    data = path.read_bytes(); table, first, last = struct.unpack_from('>III', data, 8)
    position = 20+4*(last-first+1); kind, size = struct.unpack_from('>II', data, position)
    assert kind & 0x3fffffff == 1001
    return data[position+8:position+8+size*4]


def audit(stage):
    assert not (stage/'installed').exists()
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    old = REPO/'artifacts/loop-phi-rollout-20261016/candidate/C'
    assert (old/'Dir').read_bytes() == (stage/'installed/C/Dir').read_bytes()
    shutil.copy2(old/'Dir.map', stage/'installed/C/Dir.map')
    shutil.copytree(REPO/'artifacts/dir-format-fix-20261017/shipping/candidate', stage/'fixed')
    save(stage/'freeze.json', dict(source=git_state(REPO), files=[identity(p) for folder in ['installed', 'fixed', 'compiler-baseline']
             for p in sorted((stage/folder).rglob('*')) if p.is_file()]))
    sys.path.insert(0, str(stage/'decoder')); import capstone
    decoder = capstone.Cs(capstone.CS_ARCH_M68K, capstone.CS_MODE_BIG_ENDIAN | capstone.CS_MODE_M68K_000)
    before = methods(stage/'installed/C/Dir.map'); after = methods(stage/'fixed/C/Dir.map')
    delta = [dict(method=k, before=before.get(k), after=after.get(k), delta=after.get(k, {}).get('bytes', 0)-before.get(k, {}).get('bytes', 0))
             for k in sorted(before.keys() | after.keys()) if before.get(k, {}).get('bytes') != after.get(k, {}).get('bytes')]
    for label, records in [('installed', before), ('fixed', after)]:
        data = code(stage/label/'C/Dir'); directory = stage/'disassembly'/label; directory.mkdir(parents=True)
        for row in delta:
            name = row['method']; item = records[name]; start = item['offset']
            decoded = list(decoder.disasm(data[start:start+item['bytes']], start))
            assert sum(i.size for i in decoded) == item['bytes']
            lines = [f'{i.address-start:04x}: {i.bytes.hex():16} {i.mnemonic:10} {i.op_str}' for i in decoded]
            (directory/(name.split('::')[1]+'.asm')).write_text('\n'.join(lines)+'\n', encoding='utf-8')
    save(stage/'method-deltas.json', delta)
    for row in delta: print(row['delta'], row['method'])
    for name in ['Release', 'TryGetResult']:
        print('\n'+name)
        for label in ['installed', 'fixed']:
            print(label, (stage/'disassembly'/label/(name+'.asm')).read_text(encoding='utf-8'))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__); parser.add_argument('--stage', type=Path, required=True)
    args = parser.parse_args(); audit(args.stage.resolve())
