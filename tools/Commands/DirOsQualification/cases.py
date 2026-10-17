"""Declare finite Dir contracts before executing either image on original DOS."""
import json
from pathlib import Path
import sys

root = Path(sys.argv[1]); morphos = len(sys.argv) > 2 and sys.argv[2] == 'morphos'


def pairs(names, depth=0):
    return ''.join(' '*depth*5+'  '+name.ljust(32)+' '+(names[i+1] if i+1 < len(names) else '')+'\n'
                   for i, name in enumerate(names) if i % 2 == 0)


files = pairs(['empty.txt', 'long.txt', 'one.txt', 'spaced name.txt'])
listing = '     sub (dir)\n'+files
recursive = '     sub (dir)\n'+pairs(['two.txt'], 1)+files
definitions = [
    ('default', '', 0, 0, listing),
    ('explicit', 'RAM:DirQ', 0, 0, listing),
    ('dirs', 'RAM:DirQ DIRS', 0, 0, '     sub (dir)\n'),
    ('files', 'RAM:DirQ FILES', 0, 0, files),
    ('opt-d', 'RAM:DirQ OPT D', 0, 0, '     sub (dir)\n'),
    ('opt-f', 'RAM:DirQ OPT F', 0, 0, files),
    ('opt-df', 'RAM:DirQ OPT DF', 0, 0, listing),
    ('opt-lower', 'RAM:DirQ OPT d', 0, 0, '     sub (dir)\n'),
    ('opt-ignored', 'RAM:DirQ OPT Z', 0, 0, 'Z option ignored\n'+listing),
    ('empty', 'RAM:DirEmptyQ', 0, 0, ''),
    ('sub-odd', 'RAM:DirQ/sub', 0, 0, pairs(['two.txt'])),
    ('missing', 'RAM:DirQ/missing', 20, 205, dict(prefix='Could not get information for RAM:DirQ/missing\n', fault=205)),
    ('wrong-type', 'RAM:DirQ/one.txt', 10, 212, dict(prefix='RAM:DirQ/one.txt is not a directory\n', fault=204)),
    ('quoted-file', '"RAM:DirQ/spaced name.txt"', 10, 212, dict(prefix='RAM:DirQ/spaced name.txt is not a directory\n', fault=204)),
    ('unterminated-quote', '"RAM:DirQ', 10, 120, dict(fault=120)),
    ('extra-argument', 'RAM:DirQ UNKNOWN', 10, 118, dict(fault=118)),
    ('opt-missing', 'RAM:DirQ OPT', 10, 117, dict(fault=117)),
    ('all', 'RAM:DirQ ALL', 0 if morphos else 20, 0 if morphos else 236, recursive if morphos else dict(fault=236)),
    ('opt-a', 'RAM:DirQ OPT A', 0 if morphos else 20, 0 if morphos else 236, recursive if morphos else dict(fault=236)),
    ('inter', 'RAM:DirQ INTER', 10 if morphos else 20, 236, dict(fault=236)),
    ('opt-i', 'RAM:DirQ OPT I', 0 if morphos else 20, 0 if morphos else 236, listing.replace('\n', '') if morphos else dict(fault=236)),
    ('wildcard-files', 'RAM:DirQ/#?.txt FILES', 0, 0, files),
    ('ctrl-c', 'RAM:DirQ', 5, 304, dict(fault=304)),
    ('relative', 'sub', 0, 0, pairs(['two.txt'])),
]
if '--extended' in sys.argv:
    definitions += [
        ('opt-lower-ignored', 'RAM:DirQ OPT z', 0, 0, 'z option ignored\n'+listing),
        ('opt-multiple-ignored', 'RAM:DirQ OPT Z?', 0, 0, 'Z option ignored\n? option ignored\n'+listing),
        ('opt-x-ignored', 'RAM:DirQ OPT X', 0, 0, 'X option ignored\n'+listing),
    ]


def rows(selected, stack=4096, phase='corpus', images=(0, 1)):
    return [dict(Name=name, Arguments=args, Image=image, Stack=stack, CtrlC=name == 'ctrl-c',
                 expectedReturn=rc, expectedIoErr=error, expectedOutput=output, phase=phase)
            for name, args, rc, error, output in selected for image in images]


warmup = rows(definitions[:1]*2, phase='warmup')
batches = dict(smoke=rows(definitions[:3]), part1=warmup+rows(definitions[:12]), part2=warmup+rows(definitions[12:]),
               repeat=rows([definitions[i] for i in [0, 3, 9, 11, 17, 22]], phase='warmup')+sum((rows([definitions[i] for i in [0, 3, 9, 11, 17, 22]], stack, 'repeat', (1, 0))
                                 for stack in [4096, 8192]), []))
root.mkdir(parents=True, exist_ok=True)
(root/'contracts.json').write_text(json.dumps([dict(Name=n, Arguments=a, expectedReturn=r, expectedIoErr=e, expectedOutput=o)
                                            for n, a, r, e, o in definitions], indent=2)+'\n', encoding='utf-8')
for label, cases in batches.items():
    (root/('cases-'+label+'.json')).write_text(json.dumps(cases, indent=2)+'\n', encoding='utf-8')
print('MorphOS' if morphos else 'Workbench', {k: len(v) for k, v in batches.items()})
