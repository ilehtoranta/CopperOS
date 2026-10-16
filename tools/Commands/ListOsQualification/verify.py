"""Independently check List native requests, binary receipts and declared contracts."""
from collections import Counter
from datetime import date, timedelta
import hashlib
import json
import struct
from pathlib import Path

NAMES = ['one.txt','long.txt','empty.txt','spaced name.txt','two.txt','sub','ListQ']
HEADINGS = {
 (False,True): 'Name                      Size Protection Comment\n',
 (True,True): 'Name                      Size  Key Protection Comment\n',
 (False,False): 'Name                      Size Protection Date        Time     Comment\n',
 (True,False): 'Name                      Size  Key Protection Date        Time     Comment\n',
}


def read(path):
 return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def signed(value):
 return value if value < 0x80000000 else value - 0x100000000


def protection(bits):
 return ''.join(letter if bits & bit else '-' for bit,letter in [(128,'h'),(64,'s'),(32,'p'),(16,'a')]) + ''.join('-' if bits & bit else letter for bit,letter in [(8,'r'),(4,'w'),(2,'e'),(1,'d')])


def render(c, index, metadata):
 name = NAMES[index]
 if c.get('quick'): return name+'\n'
 kind,size,key,bits,days,minutes,ticks,_ = metadata[index]
 size = 'Dir' if signed(kind) >= 0 else (size+511)//512 if c.get('block') else size
 text = f'{name:<24} {size:>7}'
 if c.get('keys'): text += f' {signed(key):>4}'
 text += f' {protection(bits):>8}'
 if not c.get('noDates'):
  value = date(1978,1,1)+timedelta(days=days)
  # Explicit English month names, independent of the host locale.
  month = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'][value.month-1]
  day = f'{value.day:02}-{month}-{value.year%100:02}'
  time = f'{minutes//60:02}:{minutes%60:02}:{ticks//50:02}'
  text += f' {day:>11} {time:>8}'
 if index==0: text += ':native comment'
 return text+'\n'


def verify(stage, batch, cpu):
 stage=Path(stage); directory=stage/'runs'/f'{batch}-{cpu}'
 definitions=read(stage/f'cases-{batch}.json'); result=read(directory/'observations.json')
 assert result['completed'], result.get('result')
 assert result['cpu']==cpu
 request=(directory/'CASES.FIX').read_bytes(); receipt=(directory/'receipt.bin').read_bytes()
 assert struct.unpack_from('>II',request)==(0x53514631,len(definitions))
 assert len(request)==16+1040*len(definitions)
 assert struct.unpack_from('>II',receipt)==(0x53515231,len(definitions))
 meta=(directory/'META.FIX').read_bytes(); assert len(meta)==32*7
 metadata=[struct.unpack_from('>8I',meta,32*i) for i in range(7)]
 assert [m[1] for m in metadata[:5]]==[19,1025,0,19,19]
 assert metadata[0][3]==0x41
 assert all(m[4:7]==(10000,754,2800) for m in metadata)
 faults=(directory/'FAULTS.FIX').read_bytes(); assert len(faults)==7*128
 diagnostics={struct.unpack_from('>I',faults,i)[0]:faults[i+4:i+128].split(b'\0')[0].decode('latin1')+'\n' for i in range(0,len(faults),128)}
 failures=[]; observations=result['observations']; assert len(observations)==len(definitions)
 cursor=8
 for i,(c,o) in enumerate(zip(definitions,observations)):
  # Recheck host JSON against both original guest request and raw receipt.
  req=struct.unpack_from('>5I',request,16+1040*i)
  flags=int(c['CtrlC']) | (2 if c['Redirect'] else 0)
  args=(c['Arguments']+'\n').encode('ascii')
  assert req==(i,c['Image'],c['Stack'],flags,len(args))
  assert request[16+1040*i+20:16+1040*i+20+len(args)]==args
  fields=struct.unpack_from('>28I',receipt,cursor); length=fields[5]
  assert fields[:3]==(i,c['Image'],c['Stack'])
  text=receipt[cursor+112:cursor+112+length].decode('latin1'); cursor+=112+length
  assert text==o['output'] and hashlib.sha256(text.encode('latin1')).hexdigest()==o['outputSha256']
  assert signed(fields[3])==o['returnCode'] and fields[4]==o['ioError']
  assert list(fields[8:18])==o['before'] and list(fields[18:28])==o['after']
  errors=[]
  if o['returnCode']!=c['expectedReturn'] or o['ioError']!=c['expectedIoErr']: errors.append('return/IoErr')
  if fields[8:18]!=fields[18:28]: errors.append('caller restoration')
  if 'diagnostic' in c:
   allowed=[diagnostics[c['diagnostic']]]
   if c.get('allowCompletedQuickRow'): allowed += ['one.txt\n'+diagnostics[c['diagnostic']]]
   if text not in allowed: errors.append('diagnostic bytes')
  else:
   body=text
   if not c.get('quick') and not c.get('noHead'):
    heading=HEADINGS[(c.get('keys',False),c['noDates'])]
    if not body.startswith(heading) or not body.endswith('\n'): errors.append('heading/footer')
    else: body=body[len(heading):-1]
   expected=[render(c,index,metadata) for index in c['rows']]
   if Counter(body.splitlines(keepends=True))!=Counter(expected): errors.append('row bytes')
  if c['phase']=='repeat' and fields[6]!=fields[7]: errors.append('repeat AvailMem delta')
  if errors: failures.append(dict(index=i,name=c['Name'],image=c['Image'],errors=errors,actual=o,contract=c))
  if i%2==1:
   prev=observations[i-1]; assert definitions[i-1]['Name']==c['Name']
   if (prev['output'],prev['returnCode'],prev['ioError'])!=(o['output'],o['returnCode'],o['ioError']):
    failures.append(dict(index=i,name=c['Name'],errors=['baseline/candidate mismatch']))
 assert cursor==len(receipt)
 return dict(batch=batch,cpu=cpu,passed=not failures,invocations=len(observations),pairs=len(observations)//2,
             failureCount=len(failures),failures=failures,
             memoryDeltas=[signed(o['availableAfter']-o['availableBefore']) for o in observations],
             result=result['result'])


if __name__=='__main__':
 import sys
 summary=verify(sys.argv[1],sys.argv[2],sys.argv[3])
 target=Path(sys.argv[1])/'runs'/f'{sys.argv[2]}-{sys.argv[3]}'/'qualification.json'
 target.write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
 print(summary['cpu'],summary['batch'],summary['passed'],summary['invocations'],summary['failureCount'])
 for f in summary['failures']: print(f['name'],f['errors'],repr(f.get('actual',{}).get('output')))
 sys.exit(0 if summary['passed'] else 1)
