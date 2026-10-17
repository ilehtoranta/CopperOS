"""Independently check the guest's raw receipt, including WinUAE executions."""
import struct


def audit(cases, raw):
    assert struct.unpack_from('>II', raw)==(0x45415231, len(cases))
    cursor=8; failures=[]; deltas=[]
    for index, case in enumerate(cases):
        fields=struct.unpack_from('>32I', raw, cursor)
        assert fields[:3]==(index, case['Image'], case['Stack'])
        stdout=raw[cursor+128:cursor+128+fields[5]]
        payload=raw[cursor+128+fields[5]:cursor+128+fields[5]+fields[29]]
        assert len(stdout)==fields[5] and len(payload)==fields[29]
        cursor+=128+len(stdout)+len(payload)
        errors=[]
        if fields[3:5]!=(case['expectedReturn'] & 0xffffffff, case['expectedIoErr']): errors.append('return/IoErr')
        if stdout!=case['expectedOutput'].encode('latin1'): errors.append('stdout')
        if payload!=bytes.fromhex(case['expectedPayload']): errors.append('payload')
        if fields[28]!=int(case['expectedExists']): errors.append('file existence')
        if fields[28] and (not fields[30] or fields[31]!=1): errors.append('file cleanup')
        if not fields[28] and any(fields[29:32]): errors.append('absent file')
        if fields[8:18]!=fields[18:28]: errors.append('caller restoration')
        if case['phase']=='repeat' and fields[6]!=fields[7]: errors.append('repeat memory')
        deltas.append(fields[7]-fields[6])
        if errors: failures.append(dict(index=index, name=case['Name'], image=case['Image'], errors=errors))
    assert cursor==len(raw)
    return dict(passed=not failures, invocations=len(cases), failures=failures, memoryDeltas=deltas)
