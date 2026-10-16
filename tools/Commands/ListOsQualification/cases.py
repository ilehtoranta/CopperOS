"""Declare bounded List contracts before native execution; no candidate-derived goldens."""
import json
import sys
from pathlib import Path

stage = Path(sys.argv[1])
# rows contains independent native metadata indices; directory order is a DOS
# handler property, so multi-entry contracts check exact row multisets while
# baseline/candidate output is additionally compared byte for byte.
def case(name, args, rows=(), **kw):
    value = dict(Name=name, Arguments=args, rows=list(rows), expectedReturn=0 if rows else 5,
                 expectedIoErr=0, quick=False, noDates=True, noHead=True)
    value.update(kw)
    return value

def error(name,args,number,rc=20):
    return dict(Name=name,Arguments=args,rows=[],expectedReturn=rc,expectedIoErr=number,diagnostic=number)

cases = [
 case('quick-file','RAM:ListQ/one.txt QUICK',[0],quick=True),
 case('metadata','RAM:ListQ/one.txt NODATES NOHEAD',[0]),
 case('dates','RAM:ListQ/one.txt NOHEAD',[0],noDates=False),
 case('keys','RAM:ListQ/one.txt KEYS NODATES NOHEAD',[0],keys=True),
 case('keys-dates','RAM:ListQ/one.txt KEYS NOHEAD',[0],keys=True,noDates=False),
 case('heading','RAM:ListQ/one.txt NODATES',[0],noHead=False),
 case('heading-dates','RAM:ListQ/one.txt',[0],noHead=False,noDates=False),
 case('heading-keys','RAM:ListQ/one.txt KEYS NODATES',[0],noHead=False,keys=True),
 case('heading-keys-dates','RAM:ListQ/one.txt KEYS',[0],noHead=False,noDates=False,keys=True),
 case('block','RAM:ListQ/long.txt BLOCK NODATES NOHEAD',[1],block=True),
 case('empty-file','RAM:ListQ/empty.txt NODATES NOHEAD',[2]),
 case('quick-dates','RAM:ListQ/one.txt QUICK DATES',[0],quick=True,noDates=False),
 case('quoted-file','"RAM:ListQ/spaced name.txt" QUICK',[3],quick=True),
 case('wildcard','RAM:ListQ/#? QUICK',[0,1,2,3,5],quick=True),
 case('files','RAM:ListQ/#? QUICK FILES',[0,1,2,3],quick=True),
 case('dirs','RAM:ListQ/#? NODATES NOHEAD DIRS',[5]),
 case('pat','RAM:ListQ/#? P #?.txt QUICK',[0,1,2,3],quick=True),
 case('sub','RAM:ListQ/#? SUB space QUICK',[3],quick=True),
 case('sub-literal-meta','RAM:ListQ/#? SUB "spac?d" QUICK',[],quick=True),
 case('all','RAM:ListQ/#? ALL QUICK',[0,1,2,3,4,5],quick=True),
 case('multiple-from','RAM:ListQ/one.txt RAM:ListQ/empty.txt QUICK',[0,2],quick=True),
 case('since-included','RAM:ListQ/one.txt QUICK SINCE 19-May-05 UPTO 19-May-05',[0],quick=True),
 case('since-excluded','RAM:ListQ/one.txt QUICK SINCE 20-May-05',[],quick=True),
 case('upto-excluded','RAM:ListQ/one.txt QUICK UPTO 18-May-05',[],quick=True),
 case('to-quick','RAM:ListQ/one.txt QUICK TO RAM:ListQRedirect',[0],quick=True,Redirect=True),
 case('to-metadata','RAM:ListQ/one.txt NODATES NOHEAD TO RAM:ListQRedirect',[0],Redirect=True),
 error('missing-file','RAM:ListQ/missing QUICK',205),
 error('required-missing','',116),
 error('unterminated-quote','"RAM:ListQ/one.txt',120,10),
 error('unsupported-format','RAM:ListQ/one.txt LFORMAT "%N"',236),
 error('bad-date','RAM:ListQ/one.txt SINCE bad-date',114),
 dict(Name='ctrl-c',Arguments='RAM:ListQ/one.txt QUICK',rows=[],expectedReturn=20,expectedIoErr=304,diagnostic=304,CtrlC=True,allowCompletedQuickRow=True),
]

# Explicitly override defaults without deriving anything from either image.
for c in cases:
 c.setdefault('Stack',4096); c.setdefault('CtrlC',False); c.setdefault('Redirect',False)
def pair(c,phase='corpus',stack=4096,order=(0,1)):
 return [dict(c,Image=i,Stack=stack,phase=phase) for i in order]
warm = pair(cases[0],'warmup') + pair(cases[24],'warmup')
for label, subset in [('part1',cases[:11]),('part2',cases[11:22]),('part3',cases[22:])]:
 rows=warm+[row for c in subset for row in pair(c)]
 (stage/f'cases-{label}.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
repeat=warm+[row for stack in (4096,8192) for i in (0,1,26,18,24) for row in pair(cases[i],'repeat',stack,(1,0))]
(stage/'cases-repeat.json').write_text(json.dumps(repeat,indent=2)+'\n',encoding='utf-8')
(stage/'cases-smoke.json').write_text(json.dumps([row for c in cases[:3] for row in pair(c)],indent=2)+'\n',encoding='utf-8')
(stage/'contracts.json').write_text(json.dumps(cases,indent=2)+'\n',encoding='utf-8')
