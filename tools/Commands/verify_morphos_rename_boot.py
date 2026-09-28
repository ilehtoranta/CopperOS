"""Verify the MorphOS Rename candidate's bounded original-Kickstart-DOS fixture.

This exercises our 68k candidate using Workbench 3.1's DOS and RAM handler. It
does not execute, compare with or qualify the original MorphOS executable.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import xml.etree.ElementTree as ET

from verify_copy_boot_transfer import require, one

sys.path.insert(0, str(Path(__file__).resolve().parent / 'Inventory'))
import inventory


STARTUP = (
    b'FailAt 21\nMakeDir RAM:target\n'
    b'Echo >RAM:ordinary "ordinary-payload"\n'
    b'Echo >RAM:wild-one "wildcard-one"\n'
    b'Echo >RAM:wild-two "wildcard-two"\n'
    b'Resident C:Ed PURE\n'
    b'Ed RAM:ordinary TO RAM:ordinary-renamed\n'
    b'Ed RAM:wild-#? TO RAM:target QUIET\n'
    b'Ed RAM:target/wild-one AS RAM:renamed\n'
    b'Resident Ed REMOVE\n'
    b'Type RAM:ordinary-renamed\nType RAM:renamed\n'
    b'Type RAM:target/wild-two\nWait 300\n'
)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def file_digest(path):
    return digest(Path(path).read_bytes())


def relocated_code(hunk, address):
    """Decode only the candidate's admitted one-code-hunk layout and RELOC32."""
    require(len(hunk) >= 36 and len(hunk) % 4 == 0, 'Invalid HUNK size')
    header = struct.unpack_from('>8I', hunk)
    require(header[:5] == (1011, 0, 1, 0, 0) and header[6] == 1001 and
            header[5] == header[7], 'Expected a single code HUNK')
    end = 32 + header[7] * 4
    require(end <= len(hunk), 'Truncated HUNK code')
    code = bytearray(hunk[32:end])
    cursor = end
    ended = False
    relocated = set()

    def word():
        nonlocal cursor
        require(cursor + 4 <= len(hunk), 'Truncated HUNK record')
        value = struct.unpack_from('>I', hunk, cursor)[0]
        cursor += 4
        return value

    while cursor < len(hunk):
        kind = word()
        if kind == 1004:
            while (count := word()) != 0:
                require(word() == 0, 'Relocation targets another HUNK')
                for _ in range(count):
                    offset = word()
                    require(offset % 2 == 0 and offset + 4 <= len(code) and
                            offset not in relocated, 'Invalid/duplicate relocation')
                    relocated.add(offset)
                    value = struct.unpack_from('>I', code, offset)[0]
                    struct.pack_into('>I', code, offset, (value + address) & 0xffffffff)
        elif kind == 1008:
            while (name_words := word()) != 0:
                cursor += name_words * 4
                require(cursor + 4 <= len(hunk), 'Truncated HUNK symbol')
                require(word() < len(code), 'Symbol outside code')
        elif kind == 1010:
            require(cursor == len(hunk), 'Trailing records after HUNK end')
            ended = True
        else:
            raise ValueError(f'Unqualified HUNK record {kind}')
    require(ended, 'Missing HUNK end')
    return bytes(code)


def verify(report, media, *, hunk, image):
    require(report.get('scope') == 'CC12/disposable-Rename-derivative-boot-progress-only'
            and report.get('commandUnderTest') == 'Rename', 'Wrong observer scope')
    require(report.get('rootInfoReady') and report.get('diskBytesUnchanged') and
            report.get('referenceMediaUnmodified') and report.get('diskWriteProtected'),
            'Boot/media prerequisite')
    require(not any(report.get(k) for k in ['failure', 'boundedStop',
                'dosObservationOverflow', 'returnObservationCollision']), 'Incomplete trace')
    require(media.get('command_under_test') == 'Rename' and
            media.get('binary_role') == 'morphos320-candidate', 'Wrong candidate role')
    replacement = one(media['replacements'], 'Replacement count')
    require(replacement['guest_path'] == 'c/ed' and len(hunk) == replacement['bytes']
            and digest(hunk) == replacement['sha256'], 'Candidate HUNK binding')
    require(digest(image) == media['output_adf_sha256'] ==
            report['fixtureImageSha256'].lower() == report['finalImageSha256'].lower(),
            'Derivative image binding')
    require(media['reference_adf_sha256'] == report['imageSha256'].lower(),
            'Reference image binding')
    adf = inventory.Adf(image)
    tree = adf.walk()
    require(adf.read_file(tree['c/ed']['block']) == hunk, 'Media executable differs')
    startup = adf.read_file(tree['s/startup-sequence']['block'])
    require(startup == STARTUP and len(startup) == media['probe_bytes'] and
            digest(startup) == media['probe_sha256'], 'Startup scenario binding')

    events = report['observedDosCalls']
    rows = report['copyInvocationOwnership']
    require(len(rows) == 3 and [r['Allocations'] for r in rows] == [1, 2, 1] and
            all(r['ReturnCode'] == 0 and r['ImageUnchanged'] and
                r['Allocations'] == r['Frees'] and not r['ParserFailed'] and
                not r['OpenFailed'] and r['Segment'] == rows[0]['Segment'] for r in rows),
            'Candidate result/ownership rows')
    require(report.get('copyActiveInvocations') == 0 and
            report.get('copyMaximumActiveInvocations') == 1 and
            report.get('copyCpuImageWrites') == [] and
            report.get('copyImageUnchangedAtReturn'), 'Shared image/lifetime')
    load = one([e for e in events if e['Name'] == 'LoadSeg' and e.get('Text') == 'C:Ed'],
               'Resident executable load')
    segment = load.get('ReturnedD0', 0)
    require(segment != 0 and segment == rows[0]['Segment'], 'Loaded segment identity')
    code_address = segment * 4 + 4
    code = relocated_code(hunk, code_address)
    require(len(code) == report['loadedCopyCodeBytes'] and
            digest(code) == report['loadedCopyImageSha256'].lower() and
            report['copySegmentAllocationBase'] == code_address - 8 and
            report['copySegmentAllocationBytes'] >= len(code) + 8, 'Loaded HUNK code binding')
    runs = [i for i, e in enumerate(events) if e['Name'] == 'RunCommand' and e['D1'] == segment]
    require(len(runs) == 3 and all(events[i].get('ReturnedD0') == 0 for i in runs),
            'RunCommand outcomes')
    closes = []
    for number, run_index in enumerate(runs):
        limit = runs[number + 1] if number + 1 < len(runs) else len(events)
        all_calls = events[run_index:limit]
        opened = one([e for e in all_calls if e['Name'] == 'CopyOpenLibrary'], 'DOS open')
        closed = one([e for e in all_calls if e['Name'] == 'CopyCloseLibrary'], 'DOS close')
        start, finish = events.index(opened), events.index(closed)
        require(run_index < start < finish and opened['D0'] == 37 and
                opened.get('ReturnedD0', 0) != 0 and closed['A1'] == opened['ReturnedD0']
                and 'ReturnedD0' in closed, 'DOS lease')
        closes.append(finish)
        calls = events[start:finish + 1]
        require(all(e['Task'] == rows[number]['Task'] == events[run_index]['Task'] for e in calls),
                'Invocation task ownership')
        parser = one([e for e in calls if e['Name'] == 'ReadArgs'], 'Parser')
        released = one([e for e in calls if e['Name'] == 'FreeArgs'], 'Parser release')
        require(parser['Text'] == 'FROM/A/M,TO=AS/A,QUIET/S' and parser['D3'] == 0 and
                parser.get('ReturnedD0', 0) != 0 and released['D1'] == parser['ReturnedD0']
                and 'ReturnedD0' in released, 'ReadArgs identity/template')
        require(not any(e['Name'] in ['PrintFault', 'VPrintf'] for e in calls),
                'Unexpected command diagnostics')
        allocations = [e for e in calls if e['Name'] == 'CopyAllocVec']
        frees = [e for e in calls if e['Name'] == 'CopyFreeMem']
        expected_allocations = [(4378, 65536), (2048, 0)] if number == 1 else [(4378, 65536)]
        require([(e['D0'], e['D1']) for e in allocations] == expected_allocations and
                len(frees) == len(allocations) and
                not any(e['Name'] == 'CopyAllocMem' for e in calls), 'Direct allocation policy')
        for allocation, free in zip(reversed(allocations), frees):
            require(allocation.get('ReturnedD0', 0) != 0 and
                    allocation['VectorAllocationBase'] == allocation['ReturnedD0'] - 4 == free['A1'] and
                    allocation['VectorAllocationBytes'] == free['D0'] and
                    'ReturnedD0' in free and calls.index(parser) < calls.index(allocation) <
                    calls.index(free) < calls.index(released), 'Allocation identity/order')
        workspace = allocations[0]['ReturnedD0']
        first = [e for e in calls if e['Name'] == 'MatchFirst']
        nexts = [e for e in calls if e['Name'] == 'MatchNext']
        ends = [e for e in calls if e['Name'] == 'MatchEnd']
        renames = [e for e in calls if e['Name'] == 'Rename']
        require(len(first) == len(ends) and all(e['D2'] == workspace and 'ReturnedD0' in e for e in first)
                and all(e['D1'] == workspace and 'ReturnedD0' in e for e in ends + nexts)
                and all(e.get('ReturnedD0', 0) != 0 for e in renames), 'Search/mutation completion')
        locks = [e for e in calls if e['Name'] == 'Lock']
        destination_path = ['RAM:ordinary-renamed', 'RAM:target', 'RAM:renamed'][number]
        destination_lock = one([e for e in locks if e.get('Text') == destination_path], 'Destination lock')
        require(all(e['D2'] == 0xfffffffe for e in locks), 'Shared destination/source lock mode')
        if number == 1:
            source_lock = one([e for e in locks if e.get('Text') == 'RAM:wild-#?'], 'Wildcard source lock')
            require(len(locks) == 2 and source_lock.get('ReturnedD0') == 0 and
                    calls.index(destination_lock) < calls.index(source_lock), 'Single-pattern source lookup')
        else:
            require(len(locks) == 1 and destination_lock.get('ReturnedD0') == 0, 'Missing direct destination')
        require(calls.index(first[0]) < calls.index(ends[0]) < calls.index(destination_lock),
                'Preflight ends before destination acquisition')
        unlock = one([e for e in calls if e['Name'] == 'UnLock'], 'Destination unlock')
        require(unlock['D1'] == destination_lock.get('ReturnedD0', 0) and
                'ReturnedD0' in unlock and calls.index(unlock) < calls.index(frees[0]),
                'Destination lock ownership')
        if number == 1:
            require([e['Text'] for e in first] == ['RAM:wild-#?'] * 2 and
                    len(ends) == 2 and [e.get('ReturnedD0') for e in nexts] == [0, 232],
                    'One wildcard pattern/two matches')
            require(sorted((e['Text'], e['DestinationText']) for e in renames) ==
                    [(f'RAM:wild-{s}', f'Ram Disk:target/wild-{s}') for s in ['one', 'two']],
                    'Exact wildcard mutation paths')
            require(all(calls.index(renames[k]) < calls.index(nexts[k]) for k in range(2)) and
                    calls.index(nexts[0]) < calls.index(renames[1]) and
                    calls.index(nexts[-1]) < calls.index(ends[-1]), 'MorphOS mutation-before-next order')
            allocated_fib = one([e for e in calls if e['Name'] == 'AllocDosObject'], 'FIB allocation')
            freed_fib = one([e for e in calls if e['Name'] == 'FreeDosObject'], 'FIB release')
            examine = one([e for e in calls if e['Name'] == 'Examine'], 'FIB examination')
            require(allocated_fib['D1'] == freed_fib['D1'] == 2 and
                    allocated_fib.get('ReturnedD0', 0) != 0 and
                    allocated_fib['ReturnedD0'] == examine['D2'] == freed_fib['D2'] and
                    examine.get('ReturnedD0', 0) != 0 and 'ReturnedD0' in freed_fib and
                    calls.index(allocated_fib) < calls.index(examine) < calls.index(freed_fib),
                    'FIB ownership')
        else:
            rename = one(renames, 'Direct mutation')
            source, target = (('RAM:ordinary', 'RAM:ordinary-renamed') if number == 0 else
                              ('RAM:target/wild-one', 'RAM:renamed'))
            require(rename['Text'] == source and rename['DestinationText'] == target and
                    [e['Text'] for e in first] == [source] and len(ends) == 1 and not nexts,
                    'Direct/AS mutation paths')
            require(not any(e['Name'] in ['AllocDosObject', 'FreeDosObject', 'Examine'] for e in calls),
                    'Unexpected direct-mode FIB')

    removal = one([e for e in events if e['Name'] == 'RemSegment' and
                   e.get('RemovedSegmentList') == segment], 'Resident removal')
    require(removal.get('ReturnedD0', 0) != 0 and events.index(removal) > closes[-1],
            'Resident removal before completion')
    tail = events[events.index(removal) + 1:]
    readbacks = []
    for path, name, payload in [
        ('RAM:ordinary-renamed', 'ordinary-renamed', b'ordinary-payload\n'),
        ('RAM:renamed', 'renamed', b'wildcard-one\n'),
        ('RAM:target/wild-two', 'wild-two', b'wildcard-two\n'),
    ]:
        match = one([e for e in tail if e['Name'] == 'MatchFirst' and e.get('Text') == path], 'Type path')
        opened = one([e for e in tail if e['Name'] == 'Open' and e.get('Text') == name], 'Type open')
        handle = opened.get('ReturnedD0', 0)
        require(handle != 0 and opened['D2'] == 1005 and match.get('ReturnedD0') == 0 and
                match.get('FileSize') == len(payload) and events.index(match) < events.index(opened),
                'Readback open/length')
        position = events.index(opened)
        close = next((e for e in events[position + 1:] if e['Name'] == 'Close' and
                      e['D1'] == handle and e['Task'] == opened['Task']), None)
        require(close is not None and close.get('ReturnedD0', 0) != 0, 'Type close')
        reads = [e.get('ReturnedD0') for e in events[position + 1:events.index(close)]
                 if e['Name'] == 'FGetC' and e['D1'] == handle and e['Task'] == opened['Task']]
        require(reads == list(payload), 'Independent exact readback bytes')
        readbacks.append({'path': path, 'bytes': len(payload), 'sha256': digest(payload)})
    return {'status': 'morphos-rename-candidate-original-dos-verified',
            'scope': 'Original Kickstart DOS/RAM handler: ordinary direct rename, wildcard two-match move, AS alias, tracked ownership, unchanged resident image and exact post-removal readback. No original MorphOS executable comparison.',
            'returns': [0, 0, 0], 'mutations': 4, 'readbacks': readbacks,
            'candidateHunkSha256': digest(hunk), 'loadedCodeSha256': digest(code),
            'morphosOriginalCompared': False, 'fullContractQualified': False,
            'pureResidentQualified': False, 'shippingQualified': False}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('observations', type=Path)
    parser.add_argument('media', type=Path)
    parser.add_argument('--test-assembly', type=Path, required=True)
    parser.add_argument('--test-result', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    data = args.observations.read_bytes()
    report = json.loads(data)
    receipt_bytes = args.media.read_bytes()
    media = json.loads(receipt_bytes)
    trx_bytes = args.test_result.read_bytes()
    trx = ET.fromstring(trx_bytes)
    namespaces = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    test = one(trx.findall('.//t:UnitTestResult', namespaces), 'Single passive boot test')
    require(test.get('outcome') == 'Passed' and test.get('testName') ==
            'CopperMod.Amiga.Tests.KickstartRomLayersDifferentialTests.CopyDerivativeBootProgressV4063',
            'Passing original-DOS observation test')
    recorded = []
    for output in trx.findall('.//t:StdOut', namespaces):
        for line in (output.text or '').splitlines():
            if line.startswith('DOS passive boot readiness: '):
                recorded.append(json.loads(line[len('DOS passive boot readiness: '):]))
    require(one(recorded, 'Single observer report in test result') == report, 'TRX/observation content binding')
    require(digest(receipt_bytes) == report['fixtureReceiptSha256'].lower(), 'Receipt/report hash binding')
    require(file_digest(args.test_assembly) == report['testAssemblySha256'].lower(), 'Observer assembly binding')
    result = verify(report, media, hunk=Path(media['replacements'][0]['local_file']).read_bytes(),
                    image=Path(media['output_path']).read_bytes())
    result.update(observationsSha256=digest(data), mediaReceiptSha256=digest(receipt_bytes),
                  testAssemblySha256=file_digest(args.test_assembly), testResultSha256=digest(trx_bytes),
                  verifierSha256=file_digest(__file__),
                  verificationDependenciesSha256={
                      'verify_copy_boot_transfer.py': file_digest(Path(__file__).with_name('verify_copy_boot_transfer.py')),
                      'Inventory/inventory.py': file_digest(Path(inventory.__file__))})
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
