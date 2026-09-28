"""Verify bounded Copy transfer/readback observations, not full qualification."""
import argparse
import hashlib
import json
from pathlib import Path


def require(condition, message):
    if not condition:
        raise ValueError(message)


def one(items, message):
    require(len(items) == 1, message)
    return items[0]


def verify(report):
    require(report.get('scope') == 'CC13/disposable-Copy-derivative-boot-progress-only', 'Wrong scope')
    require(report.get('rootInfoReady') and report.get('diskBytesUnchanged'), 'Boot/media prerequisite failed')
    require(not report.get('failure') and not report.get('boundedStop'), 'Boot observation failed')
    require(not report.get('dosObservationOverflow') and not report.get('returnObservationCollision'), 'Incomplete trace')
    events = report['observedDosCalls']
    expected = b'copy-native-boot-payload\n'
    load = one([e for e in events if e['Name'] == 'LoadSeg' and e['Text'] == 'C:Ed'], 'Missing/duplicate Copy load')
    require(load.get('ReturnedD0', 0) != 0, 'Copy load failed')
    run = one([e for e in events if e['Name'] == 'RunCommand' and e['D1'] == load['ReturnedD0']], 'Missing/ambiguous Copy run')
    require(run.get('ReturnedD0') == 0 and run['Task'] == load['Task'], 'Copy did not return OK in loader task')
    start = events.index(run)
    type_load = one([e for e in events if e['Name'] == 'LoadSeg' and e['Text'] == 'C:Type'], 'Missing/duplicate Type load')
    finish = events.index(type_load)
    require(start < finish, 'Readback precedes Copy')
    transfer = events[start:finish]
    library_open = one([e for e in transfer if e['Name'] == 'CopyOpenLibrary'], 'Missing library open')
    library_close = one([e for e in transfer if e['Name'] == 'CopyCloseLibrary'], 'Missing library close')
    require(library_open.get('ReturnedD0', 0) != 0 and library_close['A1'] == library_open['ReturnedD0'] and
            'ReturnedD0' in library_close, 'Library lease mismatch')
    owned = events[events.index(library_open):events.index(library_close)]
    allocation = one([e for e in owned if e['Name'] == 'AllocDosObject'], 'Missing RDArgs allocation')
    parser = one([e for e in owned if e['Name'] == 'ReadArgs'], 'Missing Copy parser call')
    free_args = one([e for e in owned if e['Name'] == 'FreeArgs'], 'Missing FreeArgs')
    free_object = one([e for e in owned if e['Name'] == 'FreeDosObject'], 'Missing FreeDosObject')
    pointer = allocation.get('ReturnedD0', 0)
    require(pointer != 0 and allocation['D1'] == free_object['D1'] == 5, 'Invalid RDArgs object')
    require(pointer == parser['D3'] == parser.get('ReturnedD0') == free_args['D1'] == free_object['D2'], 'RDArgs ownership mismatch')
    require(events.index(allocation) < events.index(parser) < events.index(free_args) < events.index(free_object), 'RDArgs cleanup order')
    require(all('ReturnedD0' in e and e['Task'] == run['Task'] for e in [allocation, parser, free_args, free_object, library_open, library_close]), 'Incomplete ownership calls')
    source = one([e for e in transfer if e['Name'] == 'Open' and e['Text'] == 'RAM:copy-source'], 'Missing source open')
    target = one([e for e in transfer if e['Name'] == 'Open' and e['Text'] == 'RAM:copy-target'], 'Missing target open')
    require(source['D2'] == 1005 and target['D2'] == 1006, 'Wrong open modes')
    handles = [source.get('ReturnedD0', 0), target.get('ReturnedD0', 0)]
    require(all(handles) and handles[0] != handles[1], 'Invalid transfer handles')
    read = one([e for e in transfer if e['Name'] == 'Read' and e['D1'] == handles[0]], 'Missing source read')
    write = one([e for e in transfer if e['Name'] == 'Write' and e['D1'] == handles[1]], 'Missing target write')
    require(read.get('ReturnedD0') == write.get('ReturnedD0') == write['D3'] == len(expected), 'Transfer count mismatch')
    require(read.get('ReadPayloadHex') == write.get('PayloadHex') == expected.hex().upper(), 'Transfer bytes mismatch')
    for handle in handles:
        close = one([e for e in transfer if e['Name'] == 'Close' and e['D1'] == handle], 'Missing/duplicate transfer close')
        require(close.get('ReturnedD0') == 0xffffffff and events.index(close) > events.index(write), 'Transfer close failed/out of order')
    require(all(e['Task'] == run['Task'] for e in [source, target, read, write]), 'Transfer task mismatch')
    opened = one([e for e in events[finish:] if e['Name'] == 'Open' and e['Text'] == 'copy-target'], 'Missing Type target open')
    size = one([e for e in events[finish:events.index(opened)] if e['Name'] == 'MatchFirst' and
                bytes.fromhex(e.get('FileNameBytes', '')).split(b'\0', 1)[0] == b'copy-target'], 'Missing destination size')
    require(size.get('ReturnedD0') == 0 and size.get('FileSize') == len(expected), 'Destination length mismatch')
    require(size['Task'] == opened['Task'], 'Size/readback task mismatch')
    handle = opened.get('ReturnedD0', 0)
    require(handle != 0 and opened['D2'] == 1005, 'Readback open failed')
    after = events.index(opened) + 1
    close = next((e for e in events[after:] if e['Name'] == 'Close' and e['D1'] == handle), None)
    require(close is not None and close.get('ReturnedD0') == 0xffffffff, 'Readback close failed')
    reads = [e for e in events[after:events.index(close)] if e['Name'] == 'FGetC' and e['D1'] == handle]
    require(all(e['Task'] == opened['Task'] and 'ReturnedD0' in e for e in reads), 'Incomplete readback')
    require([e['ReturnedD0'] for e in reads] == list(expected), 'Destination bytes mismatch')
    return {'status': 'bounded-transfer-and-25-byte-readback-passed', 'bytes': len(expected),
            'payloadHex': expected.hex().upper(), 'copyReturn': 0,
            'destinationLengthQualified': True, 'allocationLifecycleQualified': False,
            'successfulInvocationParserLibraryOwnershipVerified': True,
            'pureResidentQualified': False, 'fullCommandQualified': False}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('observations', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    data = args.observations.read_bytes()
    result = verify(json.loads(data))
    result['observationsSha256'] = hashlib.sha256(data).hexdigest()
    result['verifierSha256'] = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
    args.output.write_text(json.dumps(result, indent=2) + '\n')
