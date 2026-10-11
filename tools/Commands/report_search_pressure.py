"""Bind frozen Search measurements to native and compiler-test comparisons."""
import argparse
import collections
import json
import xml.etree.ElementTree as ET
from pathlib import Path

from audit_search_pressure import validate_frozen
from measure_c import identity, git_state


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def tests(path):
    root = ET.parse(path)
    results = {item.attrib['testName']: item.attrib['outcome']
               for item in root.findall('.//{*}UnitTestResult')}
    failures = {item.attrib['testName']: item.findtext('.//{*}Message')
                for item in root.findall('.//{*}UnitTestResult') if item.attrib['outcome'] == 'Failed'}
    return {'receipt': identity(path), 'counts': root.find('.//{*}Counters').attrib,
            'failures': failures}, results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--stage', type=Path, required=True)
    parser.add_argument('--compiler-root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    stage, output = args.stage.resolve(), args.output.resolve()
    validate_frozen(stage)
    trials = {name: read(stage / name / 'compilation.json') for name in
              ('baseline', 'traced-baseline', 'constrained-first', 'loop-trivial-phis',
               'baseline-morphos', 'loop-trivial-phis-morphos', 'opt-in', 'opt-in-off', 'opt-in-morphos', 'final')}
    if not trials['baseline']['referenceIdentical'] or not trials['traced-baseline']['referenceIdentical']:
        raise RuntimeError('Baseline was not reproduced.')
    if trials['final']['image']['sha256'] != trials['loop-trivial-phis']['image']['sha256']:
        raise RuntimeError('Final image differs from tested trial.')
    if not trials['opt-in-off']['referenceIdentical']:
        raise RuntimeError('Compiler defaults changed the baseline image.')
    native = {name: read(stage / name / 'qualification.json') for name in
              ('loop-trivial-phis', 'loop-trivial-phis-morphos', 'opt-in', 'opt-in-morphos', 'final')}
    if any(not row['qualified'] for rows in native.values() for row in rows):
        raise RuntimeError('Native comparison is incomplete.')
    control, control_results = tests(stage / 'tests/compiler-expanded-control.trx')
    candidate, candidate_results = tests(stage / 'tests/compiler-expanded-opt-in.trx')
    new_cases = candidate_results.keys() - control_results.keys()
    allowed_new = {'CopperSharp.Compiler.Tests.GeneratedCodeSizeExecutionTests.LoopPhiPolicyIsOptInAndRestrictedToItsQualifiedProfile'}
    if control_results.keys() - candidate_results.keys() or new_cases != allowed_new:
        raise RuntimeError('Compiler test membership differs.')
    if any(candidate_results[name] != 'Passed' for name in new_cases):
        raise RuntimeError('New eligibility test failed.')
    additional = sorted(name for name, outcome in candidate_results.items()
                        if outcome == 'Failed' and control_results.get(name) != 'Failed')
    fixed = sorted(name for name, outcome in candidate_results.items()
                   if outcome == 'Passed' and control_results.get(name) == 'Failed')
    exploratory, _ = tests(stage / 'tests/compiler-expanded.trx')
    regression, _ = tests(stage / 'tests/loop-opt-in-regressions-final.trx')
    focused, _ = tests(stage / 'tests/compiler-final-opt-in-focused.trx')
    if additional or int(regression['counts']['failed']) or int(focused['counts']['failed']):
        raise RuntimeError('Candidate has unresolved compiler regressions: ' + repr(additional))
    trace = read(stage / 'baseline-pressure.json')
    pressure = [{'iteration': row['Iteration'], 'spilledValues': len(row['Spilled']),
                 'definitionKinds': dict(collections.Counter(v['Definition'] for v in row['Spilled'])),
                 'calls': len(row['Calls']),
                 'maximumLiveThroughCall': max(len(c['LiveThrough']) for c in row['Calls'])}
                for row in trace]
    evidence = [identity(p) for pattern in ('*/execution/*.json', '*/qualification.json',
                                          '*/compiler.patch', '*/C/Search.map')
                for p in sorted(stage.glob(pattern))]
    result = {'schemaVersion': 1, 'source': git_state(Path(__file__).resolve().parents[2]),
              'compilerSource': git_state(args.compiler_root.resolve()),
              'freeze': read(stage / 'freeze.json'), 'freezeReceipt': identity(stage / 'freeze.json'),
              'morphosInputs': read(stage / 'morphos-freeze.json'),
              'trials': trials, 'pressureTrace': identity(stage / 'baseline-pressure.json'),
              'traceSource': identity(stage / 'traced-baseline/Allocator.cs'),
              'pressureRounds': pressure, 'native': native, 'evidence': evidence,
              'tests': {'control': control, 'candidate': candidate, 'exploratoryUnconditional': exploratory, 'focused': focused,
                        'regressions': regression, 'additionalFailedCases': additional,
                        'newlyPassingCases': fixed, 'newCases': sorted(new_cases)},
              'publication': 'Candidate is retained in the isolated compiler worktree and staged artifacts; out/C is unchanged.',
              'limits': ['The expanded compiler execution selection contains baseline failures; it is not an all-pass suite.',
                         'Only Search Workbench and private MorphOS profiles were differentially qualified with this compiler change.',
                         'Fixtures model DOS services and do not establish original OS parity, minimum-stack execution or hardware timing.',
                         'No global compiler integration or complete command-set publication is claimed.']}
    output.with_suffix('.json').write_text(json.dumps(result, indent=2) + '\n')
    check = native['final'][0]['checks'][0]
    before = next(r for r in trials['baseline']['records']['ALLOCATION'] if r['method'].endswith('NativeMorphOSSearchCommand::Run'))
    after = next(r for r in trials['final']['records']['ALLOCATION'] if r['method'].endswith('NativeMorphOSSearchCommand::Run'))
    markdown = f'''# Search register-pressure investigation — 2026-10-12

**Search: 10,664 → 5,364 bytes (5,300 bytes, 49.7% saved).** The private
MorphOS profile falls from 20,744 to 10,464 bytes with the same optimizer change.
Installed `out/C` images remain unchanged; this is a qualified Search candidate.

## Cause and change

An instrumented compiler reproduces the exact baseline image. Its trace records
812 values selected for spilling in the first allocation round, including
774 phi definitions. The main method has 88 static calls and up to 26 values
live through a call before spilling. These are static SSA/allocation counts,
not runtime call or distinct local counts. Spill totals accumulate across rounds.

The machine optimizer skips copy/phi simplification when a function has loops.
The candidate enables only trivial-phi cleanup in those functions. A phi with
one distinct non-self input carries the same value around the loop and can be
replaced by that input. Existing type, width, register-set, precolor, GC, and
managed-byref checks remain in `CanSubstitute`. Loop copy substitution and loop
memory optimization retain their existing restrictions. The allocator itself
is unchanged. `SimplifyLoopPhis` is an explicit, default-off API/CLI size pass,
restricted to MC68000 resident HUNKs with YOLO exceptions and no managed memory.
The final default-off image is byte-identical to the baseline. This reduces
redundant SSA transport before coloring rather than
changing spill weights or register assignments.

| Main-method measure | Baseline | Candidate |
| --- | ---: | ---: |
| Virtual values before allocation | {before['values']} | {after['values']} |
| Accumulated spilled values | {before['spilled']} | {after['spilled']} |
| Spill reload operations | {before['reloads']} | {after['reloads']} |
| Spill storage bytes | {before['spill-bytes']} | {after['spill-bytes']} |
| Local frame bytes | {before['frame-bytes']} | {after['frame-bytes']} |
| Saved-register bytes | {before['saved-bytes']} | {after['saved-bytes']} |
| Allocation rounds | {before['iterations']} | {after['iterations']} |

The recorded original Workbench Search is 2,476 bytes: the size ratio improves
from 4.31× to 2.17×. No comparison with a PPC MorphOS original is made.

## Rejected experiment

Coloring constrained groups first grew Search to 10,748 bytes, increased reloads
to 544 and the frame to 180 bytes, and needed six rounds. Its isolated payload,
patch, map, and trace remain in the experiment; that allocator change was reverted.
The diagnostic instrumentation was also removed from the retained compiler.

An unconditional version introduced two failures in the expanded selection:
the FileStats instruction-pattern assertion and ConsoleIo pinned costs
(30,132 expected cycles versus 29,848 observed). The retained pass is opt-in;
those assertions and compiler defaults were preserved. That exploratory run
and its 66 failures remain in JSON alongside the final comparison.

## Verification

- Frozen command assemblies, dependencies, response flags, and runner files are
  hash-checked before and after compilation. The baseline is byte-identical to
  the correctness checkpoint and installed Search. Final compiler source
  reproduces the exact qualified candidate image.
- Workbench Search: 19 supplied vectors per CPU on 68000, 68020, and 68040.
  Output, return levels, IoErr, resource events, repeated invocation, image
  writes, and supported interleaving agree. Per-CPU instruction totals:
  {check['instructionsBefore']:,} → {check['instructionsAfter']:,}; peak stack:
  {check['peakStackBefore']} → {check['peakStackAfter']} bytes.
- Private MorphOS Search passes the same baseline/candidate comparison on all
  three CPUs. Per-case receipts retain its distinct locale and traversal inputs.
- Four new regression cases pass, covering trivial versus changing loop phis, incompatible
  register constraints, retained loop copies, zero-iteration loops, and values
  live across calls. Execution spans 68000/020/040 compilation targets, peephole modes,
  three CPU models, two relocation bases, boundary inputs, and repeated calls.
  The API eligibility case checks default-off behavior and byte
  identity for unqualified CPU targets and the application runtime.
- Final focused compiler selection: {focused['counts']['passed']} passing,
  {focused['counts']['failed']} failing tests. The expanded selection has
  {candidate['counts']['passed']} passing and {candidate['counts']['failed']}
  failing tests; its control has {control['counts']['passed']} passing and
  {control['counts']['failed']} failing tests. No additional failed case is
  introduced across the common test membership; the added policy test passes.
  Exact baseline failures and newly passing cases remain in JSON;
  no assertions or budgets were weakened.

## Reproduction and scope

Use `audit_search_pressure.py freeze` with a fresh correctness-checkpoint stage,
then `compile` with isolated baseline/candidate payloads and distinct labels,
adding `--pass SimplifyLoopPhis` only for the candidate, and `qualify` against
the baseline. `compile-morphos` uses the private frozen root;
`qualify --morphos` selects its fixture. `report_search_pressure.py` binds the
measurements and receipts. Local evidence: `artifacts/search-pressure-20261011`.
The [JSON report]({output.name}.json) pins inputs, compiler payloads, patches,
maps, executor hashes, native observations, and compiler-test comparisons.

The retained source is in `../CopperSharp68k-search-pressure-20261011`, based on
the separately committed correctness fix. The primary compiler checkout remains
untouched. The pass remains disabled by default;
only the two Search profiles above were differentially qualified. Broader
command-set rollout remains future work. Real DOS parsing/I/O, original OS execution, minimum-stack execution,
and exact 020/040 hardware timing remain outside these fixtures.
'''
    output.with_suffix('.md').write_text(markdown, encoding='utf-8')
    print('Search saved 5300 bytes; no additional failing compiler case.')


if __name__ == '__main__':
    main()
