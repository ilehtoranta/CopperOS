"""Require contract and raw-consistency auditors to reject damaged receipts."""
import json
from pathlib import Path
import struct
import sys
from raw import audit
from verify import verify,read


def controls(stage):
    stage=Path(stage); cases=read(stage/'cases-repeat.json')
    data=(stage/'runs/repeat-68000/receipt.bin').read_bytes()
    records=[]
    # Corrupt a repeat row, after warm-up. Leave all other evidence intact.
    cursor=8
    for _ in range(4):
        f=struct.unpack_from('>32I',data,cursor); cursor+=128+f[5]+f[29]
    fields=struct.unpack_from('>32I',data,cursor)
    for name,offset,value in [('return',12,99),('ioerr',16,999),('file-existence',112,0),
                              ('close',120,0),('delete',124,0),('caller-state',72,0),
                              ('repeat-memory',28,fields[6]-8)]:
        mutated=bytearray(data); struct.pack_into('>I',mutated,cursor+offset,value)
        r=audit(cases,mutated); assert not r['passed'],name
        records.append(dict(control=name,rejected=True,failures=r['failures']))
    for name,offset in [('stdout',cursor+128),('payload',cursor+128+fields[5])]:
        mutated=bytearray(data);mutated[offset]^=1
        r=audit(cases,mutated);assert not r['passed'],name
        records.append(dict(control=name,rejected=True,failures=r['failures']))
    path=stage/'runs/repeat-68000/observations.json'; original=path.read_bytes()
    try:
        forged=json.loads(original);forged['observations'][4]['payloadHex']='00'
        path.write_text(json.dumps(forged),encoding='utf-8')
        rejected=False
        try: verify(stage,'repeat','68000')
        except AssertionError: rejected=True
        assert rejected
        records.append(dict(control='forged-json-vs-raw',rejected=True))
    finally: path.write_bytes(original)
    assert verify(stage,'repeat','68000')['passed'] and audit(cases,data)['passed']
    (stage/'negative-controls.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
    return records

if __name__=='__main__': print(len(controls(Path(sys.argv[1]))),'negative controls rejected')
