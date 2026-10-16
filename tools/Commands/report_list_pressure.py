"""Report the frozen List loop-phi comparison, qualification, and publication."""
import json
import xml.etree.ElementTree as ET
from pathlib import Path

from measure_c import identity, parse_map
from audit_search_pressure import validate_frozen

REPO = Path(__file__).resolve().parents[2]
STAGE = REPO / 'artifacts/list-pressure-20261016'
OUTPUT = REPO / 'docs/Commands/Workbench31MorphOS320/list-loop-phis-20261016'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    validate_frozen(STAGE)
    labels = ['baseline', 'candidate', 'morphos-baseline', 'morphos-candidate',
              'main-baseline', 'main-candidate', 'main-morphos-candidate']
    compilations = {label: read(STAGE / label / 'compilation.json') for label in labels}
    qualifications = {label: read(STAGE / label / 'qualification.json') for label in
                      ['candidate', 'morphos-candidate', 'main-baseline', 'main-candidate',
                       'main-morphos-candidate', 'default-build']}
    assert all(row['qualified'] and row['accept'] for rows in qualifications.values() for row in rows)
    assert compilations['baseline']['referenceIdentical']
    assert (REPO / 'out/C/List').read_bytes() == (STAGE / 'default-build/C/List').read_bytes()
    assert (STAGE / 'default-build/C/List').read_bytes() == (STAGE / 'main-candidate/C/List').read_bytes()
    test = STAGE / 'tests/compiler-main.trx'
    counters = ET.parse(test).find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters').attrib
    assert int(counters['failed']) == 0
    compiler = STAGE / 'main-candidate/compiler/CopperSharp.Compiler.dll'
    live_compiler = REPO.parent / 'CopperSharp68k/Compiler/bin/Release/net10.0/CopperSharp.Compiler.dll'
    assert identity(compiler)['sha256'] == identity(live_compiler)['sha256']
    publication = read(STAGE / 'publication/receipt.json')
    assert publication['changed'] == ['List']
    assert all(identity(REPO / 'out/C' / row['command'])['sha256'] == row['sha256'] for row in publication['after'])
    first = qualifications['default-build'][0]['checks'][0]
    reference = next(r for r in read(REPO / 'docs/Commands/Workbench31MorphOS320/command-executable-sizes-20261004.json')['commands'] if r['command'] == 'List')
    def allocation(label):
        return next(r for r in compilations[label]['records']['ALLOCATION'] if r['method'] == 'CopperOS.Commands.Native.NativeMorphOSListCommand::Run')
    report = {'frozen': read(STAGE / 'freeze.json'), 'morphosInputs': read(STAGE / 'morphos-freeze.json'),
              'compilations': compilations, 'qualification': qualifications,
              'compilerTests': {'receipt': identity(test), 'counters': counters, 'compiler': identity(compiler)},
              'publication': publication, 'defaultBuild': {'image': identity(STAGE / 'default-build/C/List'),
              'map': parse_map(STAGE / 'default-build/C/List.map'),
              'response': identity(REPO / 'src/Commands/List/obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp')},
              'recordedWorkbench31': {'bytes': reference['workbench31Bytes'], 'sha256': reference['workbench31Sha256']},
              'core': {'version': first['coreVersion'], 'sha256': first['coreSha256']},
              'limits': ['Supplied OS vector fixtures; no new List original-DOS or WinUAE execution.',
                         'No original Workbench functional parity, MorphOS runtime, shipping/PURE admission or absolute minimum stack qualification.',
                         'The main compiler has unrelated working-tree changes; exact payload hashes and source-state receipts define this finite qualification.',
                         'The full compiler suite and unaffected command builds were not rerun.',
                         'Frozen runner implementation and retained source-drift limits are inherited from the correctness checkpoint.']}
    OUTPUT.with_suffix('.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    a, b = allocation('baseline'), allocation('candidate')
    table = '\n'.join(f'| {label} | {compilations[label]["image"]["bytes"]:,} |' for label in labels)
    text = f'''# List loop-phi cleanup — 2026-10-16

**Installed List: 10,232 → 4,684 bytes; 5,548 bytes saved (54.2%).**
The complete preserved 61-command inventory falls from {publication['beforeBytes']:,} to
{publication['afterBytes']:,} bytes. Only List changes.

## Relevance and change

Search's loop-phi work was already qualified and enabled on October 12. List
still used its older pass selection. List now also selects the existing,
default-off `SimplifyLoopPhis` policy. No compiler implementation, command
source behavior, or global default changes. Explicit policy overrides keep
their existing behavior.

The cleanup replaces a phi whose only distinct non-self input is one value.
It removes redundant loop-carried SSA transport before allocation, retaining
the existing type, width, register, GC and byref substitution checks. The
allocator is unchanged. Eligibility remains MC68000 resident HUNK, YOLO
exceptions and memory disabled.

| Main-method allocation measure, isolated compiler | Before | After |
| --- | ---: | ---: |
| SSA values | {a['values']} | {b['values']} |
| Accumulated spill selections | {a['spilled']} | {b['spilled']} |
| Reload operations | {a['reloads']} | {b['reloads']} |
| Spill bytes | {a['spill-bytes']} | {b['spill-bytes']} |
| Local frame bytes | {a['frame-bytes']} | {b['frame-bytes']} |
| Saved-register bytes | {a['saved-bytes']} | {b['saved-bytes']} |
| Allocation rounds | {a['iterations']} | {b['iterations']} |

Spill selections accumulate over rounds; they are not distinct permanent slots.

## Independent measurements

All compiler comparisons use identical frozen managed assemblies, SDK inputs,
flags and runner payload. The isolated baseline reproduces the installed
10,232-byte image exactly.

| Payload/profile | Executable bytes |
| --- | ---: |
{table}

The isolated compiler is clean `02a7fa3` in
`CopperSharp68k-search-pressure-20261011`. Its independent pass saves 5,532 bytes
in the Workbench profile and 5,856 bytes in the private MorphOS profile. The
current main compiler starts at `eb04dac` plus its preserved working-tree edits.
On those same inputs, adding the pass saves 5,516 bytes (10,200 → 4,684).
The additional 32-byte baseline difference belongs to the current toolchain,
not to this pass. The final 4,684-byte source publish reproduces the main
candidate exactly. Current SDK/compiler changes are qualified only within
this recorded List boundary; no blanket correctness claim is made for them.

The recorded genuine Workbench 3.1 List is {reference['workbench31Bytes']:,} bytes.
The installed image is {reference['workbench31Bytes'] - 4684} bytes smaller, with a size ratio
of {4684 / reference['workbench31Bytes']:.3f}×. This does not establish functional parity with
that original. No PPC MorphOS size comparison is made.

## Qualification and execution costs

- **{counters['passed']} focused compiler tests pass**, covering loop-carried values,
  register allocation, resident execution, peepholes and existing generated-size
  execution regressions. No compiler source or assertion was edited.
- Workbench: 32 baseline/candidate invocations per CPU; private MorphOS: 31
  per CPU. Both profiles pass on 68000, 68020 and 68040 with exact output,
  return/IoErr, resource ownership, repeated/interleaved invocation and image
  immutability comparisons. These checks are also repeated against the current
  main payload and the freshly published Workbench image.
- Final Workbench fixture instructions per CPU: {first['instructionsBefore']:,} →
  {first['instructionsAfter']:,}; peak stack: {first['peakStackBefore']} →
  {first['peakStackAfter']} bytes, within unchanged fixture stack limits.
  These are bounded fixture instruction totals, not hardware timing or a
  general speed claim. No added execution cost was observed in these vectors.
- Executor: {first['coreVersion']}, SHA-256 `{first['coreSha256']}`.
  The frozen executor comes from the October 11 correctness checkpoint.

## Build, publication and reproduction

`CodeSizePolicies.props` enables the pass only for List in this change. The
normal List source publish, with no optimization overrides and a frozen copy
of the current main compiler payload, produces the qualified image. Build:

```powershell
pwsh -NoProfile -File tools/Commands/build-c.ps1 -Command List
```

The default main compiler checkout contains unrelated ongoing changes; future
builds from a changed checkout require fresh measurement and qualification.
For the exact experiment use the frozen `main-candidate/compiler` payload.
`audit_search_pressure.py freeze --command List` now supports this same freeze,
compile, compile-morphos and qualify workflow while retaining Search as the
backward-compatible default. Both profile inputs, pass lists, compiler patches,
SDK identities, map details, tests and all CPU receipts are bound by the JSON
report. Evidence is `artifacts/list-pressure-20261016`.

Before refreshing `out/C/List`, all 61 accepted images were staged, preserved
and hash-checked. Only List was replaced; the other 60 images were preserved
byte for byte. No stale files were deleted. Unrelated Shell/ImageBuilder and
compiler changes were preserved. At the measurement checkpoint, these changes
were not yet committed.

## Limits and next work

These are supplied-vector fixtures. New List runs under original DOS/WinUAE,
original Workbench functional parity, actual MorphOS runtime execution,
shipping/PURE admission and absolute minimum stack remain unqualified. The
frozen runner's retained source-drift limits carry forward from the correctness
checkpoint. The full compiler suite and the other 60 command builds were not
rerun; this is a filtered per-command rollout of an existing compiler pass.

Next qualify List under original DOS using the Search-style native OS harness.
After that, measure other large commands with loop-carried SSA transport before
attempting a new allocator heuristic.
'''
    OUTPUT.with_suffix('.md').write_text(text, encoding='utf-8')
    print('List 10232 -> 4684; inventory', publication['beforeBytes'], '->', publication['afterBytes'])


if __name__ == '__main__':
    main()
