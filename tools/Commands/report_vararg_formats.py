"""Summarize Raw accessor correctness and the command formatting fixture audit."""
import json
import xml.etree.ElementTree as ET
from pathlib import Path

from measure_c import identity, git_state, parse_map

REPO = Path(__file__).resolve().parents[2]
STAGE = REPO / 'artifacts/vararg-format-audit-20261007'
OUTPUT = REPO / 'docs/Commands/Workbench31MorphOS320/vararg-format-audit-20261008'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def counters(name, passing=True):
    path = STAGE / 'tests' / name
    result = ET.parse(path).find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters').attrib
    if passing:
        assert int(result['failed']) == 0
    return {'receipt': identity(path), 'counters': result}


def main():
    frozen = read(STAGE / 'fresh-inputs/freeze.json')
    old = read(STAGE / 'old-control/compilation.json')
    new = read(STAGE / 'new-control/compilation.json')
    publication = read(STAGE / 'publication.json')
    old_rows = {r['command']: r for r in old['commands']}
    new_rows = {r['command']: r for r in new['commands']}
    commands = []
    for row in frozen['commands']:
        name = row['command']
        candidate = new_rows[name]
        before = identity(STAGE / 'shipping/C' / name)
        assert before['sha256'] == row['shippingImage']['sha256']
        commands.append({'command': name, 'before': before, 'after': candidate['image'],
                         'changed': before['sha256'] != candidate['image']['sha256'],
                         'compilerComparisonIdentical': old_rows[name]['image']['sha256'] == candidate['image']['sha256'],
                         'metrics': candidate['records']['METRICS'],
                         'optimizer': candidate['records'].get('PEEPHOLE'),
                         'reachableMethods': candidate['reachableMethods']})
    assert len(commands) == 61
    assert all(r['compilerComparisonIdentical'] for r in commands)
    assert {r['command'] for r in commands if r['changed']} == {'Lock'}
    native = []
    for directory in ['shipping', 'lock-source', 'morphos', 'new-control']:
        for path in sorted((STAGE / directory / 'execution').glob('*.json')):
            receipt = read(path)
            cases = receipt.get('cases', [])
            native.append({'stage': directory, 'receipt': identity(path),
                           'suite': receipt['suite'], 'cpu': receipt['cpu'], 'status': receipt['status'],
                           'failure': receipt.get('failure'), 'invocations': len(cases),
                           'instructions': sum(c.get('instructions', 0) for c in cases),
                           'peakStackBytes': max((c.get('peakStackBytes', 0) for c in cases), default=0)})
    assert all(r['status'] == 'passed' for r in native if not (r['stage'] == 'shipping' and 'lock-' in r['suite']))
    assert all(row['oldMatchesPrior'] for row in read(STAGE / 'raw-controls.json'))
    core = read(STAGE / 'runtime.json')
    references = read(REPO / 'docs/Commands/Workbench31MorphOS320/command-executable-sizes-20261004.json')
    lock_reference = next(r for r in references['commands'] if r['command'] == 'Lock')
    report = {
        'source': git_state(REPO), 'compilerSource': git_state(Path('C:/Users/ilkle/Koodit/GIT/CopperSharp68k-wt-stack-size')),
        'beforeBytes': sum(r['before']['bytes'] for r in commands),
        'afterBytes': sum(r['after']['bytes'] for r in commands),
        'inputs': identity(STAGE / 'fresh-inputs/freeze.json'),
        'oldCompiler': old['compiler'], 'newCompiler': new['compiler'],
        'runtime': core, 'rawControls': read(STAGE / 'raw-controls.json'),
        'compilerTests': counters('compiler-qualified-final.trx'),
        'hostTests': counters('host-profiles.trx'),
        'oldRawExecution': counters('old-symbols-off.trx', False),
        'native': native, 'commands': commands, 'publication': publication,
        'lockWorkbench31Bytes': lock_reference['workbench31Bytes'],
        'lockWorkbench31Sha256': lock_reference['workbench31Sha256'],
        'limits': [
            'Historical referenced managed assemblies were missing. Fresh assemblies are frozen once and shared by both compiler comparisons; unchanged shipping images are reproduced.',
            'The initial symbol-enabled Raw test runs failed in the loader and are excluded from correctness evidence.',
            'The full compiler suite was not run. Qualification covers the reported focused tests and resident HUNK configurations.',
            'Legacy classifier shortcuts were corrected by the same intrinsic-only rule; legacy runtime profiles were not independently executed.',
            'Formatting fixtures cover the listed command conversions, not complete RawDoFmt semantics; Dir string precision is incomplete.',
            'LoadResource message fixtures were reviewed statically; the separate worker runtime suite was not rerun.',
            'Original OS images, real DOS parsing and I/O, CopperScreen, and minimum-stack execution were unavailable.',
            'The old Lock fails before completion under the corrected ABI fixture, so no matched instruction delta is claimed.'
        ]}
    OUTPUT.with_suffix('.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    success = [r for r in native if r['status'] == 'passed']
    lock = [r for r in native if r['stage'] == 'new-control' and 'lock-' in r['suite']]
    total_native = sum(r['invocations'] for r in success)
    markdown = f'''# Raw accessor and formatting audit — 2026-10-08

## Findings and fixes

**The `AmigaVarArg.Raw` bug predates the CString correction.** On identical frozen assemblies, compiler `9e61ea7` and the accepted CString compiler payload produce byte-identical failing scalar and literal reproductions. The valid symbol-free execution comparison fails with a stack-restoration mismatch for the scalar case and an out-of-range guest access for the literal case. Earlier symbol-enabled runs failed before execution and are excluded from evidence.

The compiler treated a parameterless `uint` getter named `get_Raw` as an intrinsic accessor. An ordinary instance getter requires the address of its storage; the shortcut instead supplied the contained scalar bits. Both local promotion and legacy argument/frame shortcuts now require an actual `intrinsic:*-raw` import. Getter names do not grant intrinsic semantics. A custom nonidentity Raw getter guards against reinstating that assumption.

**Lock had a separate `VPrintf` ABI bug.** It supplied the drive string itself as the argument vector. The old fixture also read that direct string and masked the error. Lock now supplies a private four-byte stack cell containing the string pointer. The fixture dereferences the vector, verifies the selected lock/unlock format, and includes unlock packet failure. All three CPUs reject the old shipping image with the unmapped pointer `$576F726B` (the bytes of `Work`).

List now explicitly verifies QUICK, KEYS, NODATES and file/directory format selection. MakeDir reads no argument cell for its argument-free `No name given` message. Both profile fixtures pass. The reviewed DosList, DiskFree, Dir, Rename, Copy, ModList and TaskList helpers consume the actual selected conversions; their applicable shipping suites pass. LoadResource's message/catalog handling was reviewed statically and its separate worker suite was not rerun.

## Measurements

| Image | Before bytes | Corrected bytes | Change |
| --- | ---: | ---: | ---: |
| Scalar Raw reproduction | 52 | 64 | +12 |
| Literal Raw reproduction | 84 | 92 | +8 |
| Lock | 1,568 | 1,592 | +24 |
| Complete 61-command set | {report['beforeBytes']:,} | {report['afterBytes']:,} | +{report['afterBytes'] - report['beforeBytes']} |

These are correctness costs. **The other 60 commands are byte-identical.** The compiler fix changes none of the 61 shipping command images when comparing identical freshly frozen assemblies; Lock's change comes from the source correction. Execute remains 6,168 bytes, List 10,232, Search 10,664, Reboot 516. The recorded genuine Workbench 3.1 Lock is {lock_reference['workbench31Bytes']} bytes, giving a corrected ratio of {1592 / lock_reference['workbench31Bytes']:.3f}×. No comparison is made with PPC originals.

## Qualification

- **1,665 focused compiler tests pass**, retaining branch-liveness and size-pass regressions. Raw execution covers scalar, literal, conditional, reference, by-value, embedded-field and custom nonidentity getters, all available compilation targets, disabled/bounded/fixed-point peepholes, 68000/68020/68040 execution, two relocation bases and repeated invocation. Guest checks include stack restoration and reserved registers.
- **843 host profile tests pass.**
- Workbench and MorphOS Lock, List and MakeDir suites pass on all three CPUs. Applicable shipping formatting suites also pass. Receipts contain **{total_native:,} successful native invocations** across the recorded runs; repeated qualification of the same image is included in that count.
- Corrected shipping Lock has 12 invocations per CPU and a peak stack of {max(r['peakStackBytes'] for r in lock)} bytes, within the unchanged 16 KiB fixture stack. Its local frame grows from 44 to 52 bytes, while saved-register storage falls from 32 to 28 bytes: four additional bytes in total. Per-case instruction counts, allocations/releases, output bytes, return levels and IoErr are retained in receipts. The old image cannot complete the corrected ABI fixture, so no matched execution-overhead claim is made.
- Actual emulator: Copper68k `{core['productVersion']}`, SHA-256 `{core['coreSha256']}`.

## Inputs and publication

The earlier receipt's managed-input paths were missing. A complete fresh source build was staged, its assemblies/dependencies frozen once, and both compiler payloads run against those exact inputs with unchanged per-command policies. The accepted compiler reproduces all 60 unchanged shipping images; its corrected-source Lock image matches the candidate. Both payloads produce identical complete inventories. MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes and symbols disabled are retained. Exact compiler payload hashes, SDK identities, response flags, map metrics and input hashes are recorded in the JSON report and local experiment receipts.

All 61 final images and hashes were verified before `out/C` was refreshed. Previous output remains in the experiment's shipping snapshot. No stale command files required removal. The compiler's primary dirty checkout was preserved; work remains in `CopperSharp68k-wt-stack-size`.

## Limits and next work

This is focused resident-HUNK qualification. Other runtime profiles and legacy emission were not independently executed. Fixtures do not establish full RawDoFmt compatibility (Dir's string precision remains incomplete). Minimum-stack execution, original OS images, real DOS parsing/I/O and CopperScreen were unavailable. The full compiler repository suite was not rerun.

With these correctness issues addressed, the next size investigation is List/Search register pressure and aggregate transport. Establish sizes and stack costs before selecting further allocator changes.
'''
    OUTPUT.with_suffix('.md').write_text(markdown, encoding='utf-8')
    print(f'{report["beforeBytes"]} -> {report["afterBytes"]}; {total_native} successful native invocations')


if __name__ == '__main__':
    main()
