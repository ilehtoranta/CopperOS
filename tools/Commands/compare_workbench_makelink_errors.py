"""Compare bounded Workbench MakeLink error/recovery traces and resource ownership."""
import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path
from verify_copy_boot_transfer import require, one


def inspect(r, candidate):
    require(r.get('commandUnderTest') == 'MakeLink' and r.get('rootInfoReady') and
            r.get('diskBytesUnchanged') and not any(r.get(k) for k in
            ['failure', 'boundedStop', 'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    rows = r['copyInvocationOwnership']; expected = [0, 20, 20, 20, 0]
    require(len(rows) == 5 and [v['ReturnCode'] for v in rows] == expected, 'Return sequence')
    segment = rows[0]['Segment']
    require(all(v['Segment'] == segment and v['Task'] == rows[0]['Task'] and
                v['Allocations'] == v['Frees'] == (1 if candidate else 0) and v['ImageUnchanged'] for v in rows), 'Resident ownership')
    require(r.get('copyCpuImageWrites') == [] and r.get('copyActiveInvocations') == 0, 'Shared image')
    e = r['observedDosCalls']
    starts = [i for i,v in enumerate(e) if v['Name'] == 'CopyOpenLibrary']
    ends = [i for i,v in enumerate(e) if v['Name'] == 'CopyCloseLibrary']
    require(len(starts) == len(ends) == 5, 'Library leases')
    signatures = []
    for n,(a,b) in enumerate(zip(starts,ends)):
        require(a < b and (n == 4 or b < starts[n+1]), 'Sequential lifetime')
        c = e[a:b+1]
        parser = one([v for v in c if v['Name'] == 'ReadArgs'], 'ReadArgs')
        require(parser['Text'] == 'FROM/A,TO/A,HARD/S,FORCE/S', 'Template')
        parsed = parser.get('ReturnedD0', 0) != 0
        require(parsed == (n != 1), 'Parser result')
        free = [v for v in c if v['Name'] == 'FreeArgs']
        if parsed:
            f = one(free, 'Parser cleanup')
            require(f['D1'] == parser['ReturnedD0'] and 'ReturnedD0' in f, 'Parser release identity')
        else:
            require(not free and parser.get('ReturnedIoErr') == 116 and
                    not any(v['Name'] in ['Lock','Examine','MakeLink','AllocDosObject'] for v in c), 'Parser failure isolation')
        faults = [v for v in c if v['Name'] == 'PrintFault']
        error = {1:116, 2:205, 3:203}.get(n)
        if error is None:
            require(not faults, 'Unexpected success fault')
        else:
            f = one(faults, 'Failure diagnostic')
            require(f['D1'] == error and f['D2'] == 0 and 'ReturnedD0' in f, 'Fault code/header')
        formats = [v['Text'] for v in c if v['Name'] == 'VPrintf']
        require(formats == (["Can't find %s "] if n == 2 else []), 'Diagnostic format')
        locks = [v for v in c if v['Name'] == 'Lock']
        links = [v for v in c if v['Name'] == 'MakeLink']
        if n != 1:
            lock = one(locks, 'Target lock')
            require(lock['Text'] == ('RAM:absent' if n == 2 else 'RAM:link-source'), 'Target name')
            require((lock.get('ReturnedD0',0) != 0) == (n != 2), 'Target lock result')
        if n in [0,3,4]:
            link = one(links, 'Link call')
            require(link['Text'] == ('RAM:second' if n == 4 else 'RAM:first') and
                    link['D2'] == lock['ReturnedD0'] and link['D3'] == 0 and
                    ((link.get('ReturnedD0',0) != 0) == (n != 3)), 'Link result')
            if n == 3: require(link.get('ReturnedIoErr') == 203, 'Duplicate error')
        else: require(not links, 'Link after parse/lock failure')
        if candidate:
            unlocks = [v for v in c if v['Name'] == 'UnLock']
            require(Counter(v['ReturnedD0'] for v in locks if v.get('ReturnedD0',0)) ==
                    Counter(v['D1'] for v in unlocks), 'Candidate target cleanup')
            fibs = [v for v in c if v['Name'] == 'AllocDosObject']
            frees = [v for v in c if v['Name'] == 'FreeDosObject']
            require(len(fibs) == len(frees) == (1 if n in [0,3,4] else 0), 'Candidate FIB count')
            if fibs: require(fibs[0]['ReturnedD0'] == frees[0]['D2'], 'Candidate FIB identity')
        signatures.append({'return':expected[n], 'parsed':parsed, 'fault':error, 'formats':formats,
                           'linkCalled':bool(links), 'linkSucceeded':n in [0,4]})
    for name in ['first','second']:
        opened = one([v for v in e if v['Name']=='Open' and v.get('Text')==name], 'Readback')
        a=e.index(opened); handle=opened.get('ReturnedD0',0)
        require(a>ends[-1] and handle!=0,'Readback handle')
        close=next(v for v in e[a+1:] if v['Name']=='Close' and v['D1']==handle and v['Task']==opened['Task'])
        require([v.get('ReturnedD0') for v in e[a+1:e.index(close)] if v['Name']=='FGetC' and
                 v['D1']==handle and v['Task']==opened['Task']]==list(b'hard-link-payload\n'), 'Readback bytes')
    return signatures


if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('reference',type=Path);p.add_argument('candidate',type=Path)
    p.add_argument('--output',type=Path,required=True)
    args=p.parse_args(); reference=args.reference.read_bytes();candidate=args.candidate.read_bytes()
    expected=inspect(json.loads(reference),False);actual=inspect(json.loads(candidate),True)
    require(expected==actual,'Reference/candidate behavioral difference')
    result={'status':'bounded-workbench-error-parity-verified','cases':actual,'fullProfileParity':False,
            'referenceSha256':hashlib.sha256(reference).hexdigest(),'candidateSha256':hashlib.sha256(candidate).hexdigest(),
            'verifierSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}
    args.output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
