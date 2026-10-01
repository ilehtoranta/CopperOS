"""Verify the aligned CLONE and NOPRO COM DATES original-DOS fixture."""
import argparse
import hashlib
import json
from pathlib import Path
from verify_copy_boot_transfer import require, one
from verify_copy_normal_transfer import verify as verify_normal


def cstring(value):
    return bytes.fromhex(value).split(b'\0')[0]


def verify(r, precedence=False):
    result = verify_normal(r, workspace_bytes=4772)
    events = r['observedDosCalls']
    starts = [i for i, e in enumerate(events) if e['Name'] == 'CopyOpenLibrary']
    dates = []
    for start, name, protection in zip(starts, ['copy-target', 'copy-target2'], [97 if precedence else 64, 0]):
        end = next(i for i in range(start + 1, len(events)) if events[i]['Name'] == 'CopyCloseLibrary')
        calls = events[start:end]
        source = one([e for e in calls if e['Name'] == 'Examine' and
                      cstring(e.get('FileNameBytes', '')) == b'copy-source'], 'Source metadata')
        require(source.get('ReturnedD0', 0) != 0 and source['D2'] % 4 == 0, 'FIB alignment/result')
        require(source['FileProtection'] == (113 if precedence else 80) and cstring(source['FileCommentHex']) == b'metadata-note', 'Source setup')
        date = source['FileDateHex']
        require(len(bytes.fromhex(date)) == 12, 'DateStamp size')
        dates.append(date)
        dated = one([e for e in calls if e['Name'] == 'SetFileDate'], 'Date application')
        comment = one([e for e in calls if e['Name'] == 'SetComment'], 'Comment application')
        require(dated['DateArgumentHex'] == date and cstring(comment['CommentArgumentHex']) == b'metadata-note', 'Metadata arguments')
        require(all(e['Text'] == 'Ram Disk:' + name and e.get('ReturnedD0', 0) != 0 for e in [dated, comment]), 'Metadata operation result')
        require(calls.index(dated) < calls.index(comment), 'Date/comment order')
        protections = [e for e in calls if e['Name'] == 'SetProtection']
        if protection:
            applied = one(protections, 'CLONE protection')
            require(applied['D2'] == protection and applied['Text'] == 'Ram Disk:' + name and applied.get('ReturnedD0', 0) != 0, 'Archive-cleared protection')
            require(calls.index(applied) < calls.index(dated), 'Protection/date order')
        else:
            require(not protections, 'NOPRO applied protection')
        reopened = one([e for e in events if e['Name'] == 'Open' and e['Text'] == name], 'Independent open')
        pos = events.index(reopened)
        type_start = max(i for i in range(pos) if events[i]['Name'] == 'LoadSeg' and events[i]['Text'] == 'C:Type' and events[i]['Task'] == reopened['Task'])
        observed = one([e for e in events[type_start:pos] if e['Name'] == 'MatchFirst' and cstring(e.get('FileNameBytes', '')) == name.encode()], 'Independent metadata')
        require(observed.get('ReturnedD0') == 0 and observed['FileProtection'] == protection and observed['FileDateHex'] == date and cstring(observed['FileCommentHex']) == b'metadata-note', 'Independent metadata differs')
    require(dates[0] == dates[1], 'Source date changed between copies')
    result.update(status='copy-clone-nopro-com-dates-evidence-passed', workspaceBytes=4772,
                  metadataProfiles=['CLONE PROX', 'CLONE NOPRO'] if precedence else ['CLONE', 'NOPRO COM DATES'], fullCommandQualified=False)
    return result


def verify_individual(r):
    result = verify_normal(r, workspace_bytes=4772)
    events = r['observedDosCalls']
    starts = [i for i, e in enumerate(events) if e['Name'] == 'CopyOpenLibrary']
    for start, name, selected, forbidden in zip(starts, ['copy-target', 'copy-target2'],
                                               ['SetComment', 'SetFileDate'], ['SetFileDate', 'SetComment']):
        end = next(i for i in range(start + 1, len(events)) if events[i]['Name'] == 'CopyCloseLibrary')
        calls = events[start:end]
        source = one([e for e in calls if e['Name'] == 'Examine' and cstring(e.get('FileNameBytes', '')) == b'copy-source'], 'Source metadata')
        require(source.get('ReturnedD0', 0) != 0 and source['D2'] % 4 == 0 and source['FileProtection'] == 80 and cstring(source['FileCommentHex']) == b'metadata-note', 'Source metadata/setup')
        require(not any(e['Name'] in [forbidden, 'SetFilePosixDate'] for e in calls), 'Unselected metadata operation')
        applied = one([e for e in calls if e['Name'] == selected], 'Selected metadata operation')
        protect = one([e for e in calls if e['Name'] == 'SetProtection'], 'Default protection')
        require(protect['D2'] == 64 and calls.index(protect) < calls.index(applied), 'Default protection/order')
        require(all(e['Text'] == 'Ram Disk:' + name and e.get('ReturnedD0', 0) != 0 for e in [protect, applied]), 'Metadata operation result')
        if selected == 'SetComment':
            require(cstring(applied['CommentArgumentHex']) == b'metadata-note', 'Comment argument')
        else:
            require(applied['DateArgumentHex'] == source['FileDateHex'], 'Date argument')
        reopened = one([e for e in events if e['Name'] == 'Open' and e['Text'] == name], 'Independent open')
        pos = events.index(reopened)
        type_start = max(i for i in range(pos) if events[i]['Name'] == 'LoadSeg' and events[i]['Text'] == 'C:Type' and events[i]['Task'] == reopened['Task'])
        observed = one([e for e in events[type_start:pos] if e['Name'] == 'MatchFirst' and cstring(e.get('FileNameBytes', '')) == name.encode()], 'Independent metadata')
        require(observed.get('ReturnedD0') == 0 and observed['FileProtection'] == 64, 'Independent protection')
        require(cstring(observed['FileCommentHex']) == (b'metadata-note' if selected == 'SetComment' else b''), 'Independent comment')
        if selected == 'SetFileDate':
            require(observed['FileDateHex'] == source['FileDateHex'], 'Independent date')
        # COM has no date setter. Do not infer its creation timestamp from the
        # host clock or require two guest timestamps to differ by chance.
    result.update(status='copy-individual-metadata-evidence-passed', workspaceBytes=4772,
                  metadataProfiles=['COM', 'DATES'], fullCommandQualified=False)
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('observations', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--precedence', action='store_true')
    parser.add_argument('--individual', action='store_true')
    args = parser.parse_args()
    data = args.observations.read_bytes()
    require(not (args.individual and args.precedence), 'Choose one metadata profile')
    result = verify_individual(json.loads(data)) if args.individual else verify(json.loads(data), args.precedence)
    result.update(observationsSha256=hashlib.sha256(data).hexdigest(),
                  verifierSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                  normalVerifierSha256=hashlib.sha256(Path(__file__).with_name('verify_copy_normal_transfer.py').read_bytes()).hexdigest())
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
