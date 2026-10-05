"""Report frozen compiler comparisons and qualified per-command selections."""
import argparse
import hashlib
import json
import re
import struct
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

from measure_c import identity, parse_map
from optimize_codegen_sizes import GENERATED_PASSES, REPO


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def instruction_counts(image, metrics, decoder):
    data = image.read_bytes()
    table, first, last = struct.unpack_from('>III', data, 8)
    position = 20 + 4 * (last - first + 1)
    kind, size = struct.unpack_from('>II', data, position)
    if kind & 0x3fffffff != 1001:
        raise ValueError('Expected code HUNK')
    code = data[position + 8:position + 8 + metrics['rom-code-bytes']]
    instructions = list(decoder.disasm(code, 0))
    branches = {'bra', 'bsr', 'bhi', 'bls', 'bcc', 'bhs', 'bcs', 'blo', 'bne', 'beq',
                'bvc', 'bvs', 'bpl', 'bmi', 'bge', 'blt', 'bgt', 'ble', 'dbra', 'dbf'}
    return {'instructions': len(instructions), 'decodedBytes': sum(i.size for i in instructions),
            'completeDecode': sum(i.size for i in instructions) == len(code),
            'branches': sum(i.mnemonic.split('.')[0] in branches or i.mnemonic.startswith('db') for i in instructions),
            'calls': sum(i.mnemonic.startswith(('bsr', 'jsr')) for i in instructions),
            'returns': sum(i.mnemonic == 'rts' for i in instructions),
            'note': 'Static encoded instruction counts; these are not executed branch counts.'}


def report(frozen, final, matrix, compiler_root, output, decoder_path):
    if decoder_path:
        sys.path.insert(0, str(decoder_path))
    import capstone
    decoder = capstone.Cs(capstone.CS_ARCH_M68K, capstone.CS_MODE_BIG_ENDIAN | capstone.CS_MODE_M68K_000)
    selections = read(final / 'selection.json')
    freeze = read(frozen / 'freeze.json')
    references = {c['name'].casefold(): c.get('reference_profiles', {}).get('wb31', {}).get('source_files', [])
                  for c in read(REPO / 'docs/Commands/Workbench31MorphOS320/command-inventory.json')['commands']}
    commands = []
    for selected in selections:
        name = selected['command']
        baseline = parse_map(frozen / 'baseline/C' / (name + '.map'))
        candidate = parse_map(final / 'C' / (name + '.map'))
        originals = [r for r in references.get(name.casefold(), []) if r.get('file_format') == 'amiga-hunk']
        reference = originals[0] if originals else None
        row = {**selected, 'baseline': baseline, 'candidate': candidate,
               'baselineImage': identity(frozen / 'baseline/C' / name), 'workbench31': reference,
               'ratioToWorkbench31': selected['afterBytes'] / reference['bytes'] if reference else None,
               'staticBefore': instruction_counts(frozen / 'baseline/C' / name, baseline['records']['METRICS'], decoder),
               'staticAfter': instruction_counts(final / 'C' / name, candidate['records']['METRICS'], decoder)}
        row['executionReceipts'] = [identity(p) for p in sorted((final / 'execution').glob(name + '.*.json'))
                                   if read(p).get('imageSha256') == row['image']['sha256']]
        row.pop('measurement', None)  # The same map measurements are in candidate.
        row['trials'] = [{'passes': trial['measurement']['passes'],
                          'image': trial['measurement']['image'], 'qualification': trial['qualification'],
                          'receipt': identity(Path(trial['directory']) / 'receipt.json')}
                         for trial in row['trials']]
        commands.append(row)
    experiments = []
    for experiment in read(matrix / 'matrix.json'):
        measurements = read(matrix / experiment['stage'] / 'compilation.json')['commands']
        experiments.append({**experiment,
                            'measurements': [{k: row[k] for k in ('command', 'image', 'records', 'reachableMethods')}
                                             for row in measurements],
                            'totalBytes': sum(r['image']['bytes'] for r in measurements),
                            'compiler': (read(matrix / experiment['stage'] / 'compilation.json')['compiler']
                                         if experiment['stage'].startswith('../') else read(matrix / 'compiler-receipt.json'))})
    patch = subprocess.check_output(['git', '-C', str(compiler_root), 'diff', '--binary', 'b73f379', 'HEAD'])
    (final / 'compiler.patch').write_bytes(patch)
    commits = []
    for commit in subprocess.check_output(['git', '-C', str(compiler_root), 'rev-list', '--reverse', 'b73f379..HEAD']).decode().splitlines():
        commits.append({'commit': commit,
                        'subject': subprocess.check_output(['git', '-C', str(compiler_root), 'show', '-s', '--format=%s', commit]).decode().strip(),
                        'patchSha256': hashlib.sha256(subprocess.check_output(['git', '-C', str(compiler_root), 'show', '--format=', '--binary', commit])).hexdigest()})
    tests = []
    for path in (frozen / 'compiler-tests/final-compiler-regressions.trx', frozen / 'command-tests/command-profiles.trx'):
        root = ET.parse(path).getroot()
        counters = root.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters')
        tests.append({'receipt': identity(path), 'counters': counters.attrib})
    core = REPO / 'tests/Commands.NativeExecution/bin/Release/net10.0/Copper68k.dll'
    quoted_core = str(core).replace("'", "''")
    product_version = subprocess.check_output(['powershell', '-NoProfile', '-NonInteractive', '-Command',
        f"[Diagnostics.FileVersionInfo]::GetVersionInfo('{quoted_core}').ProductVersion"]).decode().strip()
    result = {'schemaVersion': 1, 'baselineTotalBytes': freeze['totalBytes'],
              'selectedTotalBytes': sum(r['afterBytes'] for r in commands),
              'acceptedCommands': sum(r['accept'] for r in commands), 'freeze': freeze,
              'compiler': read(final / 'compiler-receipt.json'), 'compilerPatch': identity(final / 'compiler.patch'),
              'compilerCommits': commits, 'dotnetSdk': subprocess.check_output(['dotnet', '--version'], cwd=REPO).decode().strip(),
              'decoder': 'Capstone ' + capstone.__version__, 'tests': tests,
              'instructionCore': {**identity(core), 'productVersion': product_version},
              'buildToolChecks': read(frozen / 'build-tool-check/qualification.json'),
              'evalPeepholeModes': read(frozen / 'eval-modes.json'),
              'morphosEvalPeepholeModes': read(frozen / 'morphos-eval-modes.json'),
              'baselineQualification': read(frozen / 'baseline/qualification.json'), 'experiments': experiments,
              'commands': commands, 'publication': read(final / 'publication.json') if (final / 'publication.json').exists() else None,
              'limits': ['Original Workbench images and CopperScreen are unavailable on this host; verified reference size/hash receipts only.',
                         'AddBuffers and Relabel entry fixtures fail on unchanged shipping baselines (vector ABI and allocation size respectively). Exe2Arc has no native entry fixture. All three retain byte-identical baselines.',
                         'Copper68k 1.5.1 lacks exact 68020/68040 timing for BCHG #0,D5 in the unchanged negative-divisor prefix; those signed compiler boundary cases run on 68000 only. The unsigned shared core and shipping command fixtures run on all three CPUs.',
                         'Host fixtures cover Workbench and MorphOS profiles. Native qualification covers selected shipping profiles, not execution of PPC MorphOS originals.',
                         'Memory arithmetic remains restricted to existing proven private-memory rewrites. No arbitrary APTR read/modify/write fold is introduced.',
                         'Raw branched helper inlining is limited to fixed-point peepholes and private leaf bodies with internal branches, no stack manipulation, and no address/data fixups.',
                         'No dynamic branch counter or wall-clock performance claim is made. Static branch counts and executed instruction counts are recorded separately.']}
    output.with_suffix('.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    saved = result['baselineTotalBytes'] - result['selectedTotalBytes']
    by_name = {r['command']: r for r in commands}
    lines = ['# Generated MC68000 size optimization — 2026-10-05', '',
             f"**61 commands: {result['baselineTotalBytes']:,} → {result['selectedTotalBytes']:,} bytes; {saved:,} bytes saved ({saved / result['baselineTotalBytes']:.2%}).**",
             f"**Execute: 6,376 → {by_name['Execute']['afterBytes']:,} bytes.** {result['acceptedCommands']} commands use qualified new selections; others retain their baseline policy and bytes.", '',
             'The baseline already contains accepted command-source reductions. Every comparison uses the same frozen managed assemblies and dependencies. No command source was changed by this compiler work.', '',
             '## Toolchain and controls', '',
             '- Compiler starts at clean `b73f3794246be849706492df0bab2e2ba24636ec`; final source is `' + result['compiler']['source']['commit'] + '` on `codex/code-generator-size` in the isolated compiler checkout.',
             '- MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols disabled, existing stack limits. Baseline reproduction is byte-identical for all 61 commands.',
             '- New `M68kCodeSizeOptions` switches default to false. CLI/response-file `code-size-passes` and CopperOS `CopperOSCodeSizePasses` select them independently. Legacy ROM options and mutual exclusion are retained.',
             '- The JSON records frozen SDK/input/compiler identities, source and patch hashes, flags, maps, rewrite counts, relocations, largest methods, selection trials and execution receipt hashes.',
             '- `GENERATED-SIZE-REWRITE local-byte-delta` is a local pre-layout measurement, not additive final-image savings. Removed initialization records private storage bytes separately.', '',
             '## Implemented passes', '',
             '| Pass | Independent control | Qualified implementation |', '| --- | --- | --- |',
             '| A | RemoveRedundantTransport | Full register overwrites, private stack writebacks and self-moves; live NZVC uses a shorter test preserving X. |',
             '| B | CompactGuestMemory | Liveness-proven indexed guest accesses, direct memory tests, retained APTR offsets for indexed long reads and byte/word/long writes. |',
             '| C | NarrowOperations | Remove widening masks for proven low-only stores; retain full-width, address, call and merge consumers. Existing narrow comparisons and private-memory updates remain active. |',
             '| D | EliminateRedundantInitialization | Per-home byte analysis through control flow and calls, with escape/alias/observation checks; implicit initialization is removed only when unobserved or overwritten. |',
             '| E | SizeFirstCosts | Byte-first frame-clear planning and arithmetic/helper costs. A one-store DBRA loop can replace speed-oriented unrolling. |',
             '| F | InlineMemoryHelpers | Restricted private single-use memory helpers and encoded-byte decisions for private leaf helpers with internal branches; keep function-address identity and NoInlining exclusions. |',
             '| G | ShareArithmeticCores | Shared full-width unsigned division cores, existing quotient/remainder fusion and proven bounded unsigned register DIVU. |', '',
             'EH, GC, dynamic stack and unsupported alias cases stay excluded. More aggressive early byte/word indexed emission grew images and was rejected; late liveness folding retains those opportunities safely.', '',
             '## Independent and cumulative measurements', '',
             'These are complete experimental images before per-command rejection. The matrix used a frozen compiler snapshot; the final selection was rebuilt and qualified with the final source. B-final also measures the later indexed emitter.', '',
             '| Selection | All 61 bytes | Execute | Eval | List | Search | Accepted smaller commands |',
             '| --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for experiment in experiments:
        sizes = {r['command']: r['image']['bytes'] for r in experiment['measurements']}
        label = experiment['stage'].replace('../', '')
        lines.append(f"| {label} | {experiment['totalBytes']:,} | {sizes['Execute']:,} | {sizes['Eval']:,} | {sizes['List']:,} | {sizes['Search']:,} | {sum(r['accept'] for r in experiment['rows'])} |")
    lines += ['', '## Accepted images and remaining Workbench ratios', '',
              'A–G identify the new switches above. “Baseline” keeps the previous selection. Ratios use genuine admitted Workbench 3.1 HUNK sizes; MorphOS-only commands have no PPC comparison.', '',
              '| Command | Before | After | Saved | New passes | WB 3.1 | Ratio |', '| --- | ---: | ---: | ---: | --- | ---: | ---: |']
    letters = {value: key for key, value in GENERATED_PASSES.items()}
    for row in commands:
        selected = ''.join(letters[p] for p in row['passes']) or 'Baseline'
        wb = f"{row['workbench31']['bytes']:,}" if row['workbench31'] else '—'
        ratio = f"{row['ratioToWorkbench31']:.2f}×" if row['ratioToWorkbench31'] else '—'
        lines.append(f"| {row['command']} | {row['beforeBytes']:,} | {row['afterBytes']:,} | {row['savedBytes']:,} | {selected} | {wb} | {ratio} |")
    lines += ['', '## Execution costs and stack', '',
              'Executed counts below sum each selected command’s complete 68000 fixture set; the JSON contains separate 68020/68040 results. Peak stack is the maximum observed SP displacement, including compiler invocation state. All accepted cases remain within their existing limits.', '',
              '| Command | Instructions before | After | Delta | Peak stack before/after | Static branches before/after |',
              '| --- | ---: | ---: | ---: | ---: | ---: |']
    for row in commands:
        checks = [c for c in row.get('checks', []) if c['cpu'] == '68000' and c['passed']]
        if not row['accept'] or not checks:
            continue
        a, b = sum(c['instructionsBefore'] for c in checks), sum(c['instructionsAfter'] for c in checks)
        stack = f"{max(c['peakStackBefore'] for c in checks)}/{max(c['peakStackAfter'] for c in checks)}"
        branches = f"{row['staticBefore']['branches']}/{row['staticAfter']['branches']}"
        lines.append(f"| {row['command']} | {a:,} | {b:,} | {b-a:+,} | {stack} | {branches} |")
    lines += ['', 'Compact clears trade bytes for loop executions: Execute’s audited 22-byte clear has a 12-byte loop alternative with eleven additional DBRA executions. Removing a clear and compacting that same clear are alternative savings and are counted once. Shared division adds a call/return and a transient four-byte return address per shared-core invocation; measured instruction and stack effects above include these costs.', '',
              '## Qualification', '',
              '- 1,516 relevant compiler tests passed, including the six branch-liveness regressions, all 32 CCR combinations, partial writes, flags including X, memory width/count/order, aliases, function-pointer identity, stack restoration, HUNK relocation and branch-range tests.',
              '- 843 host command tests passed, including applicable Workbench/MorphOS profile fixtures.',
              '- Execute: complete 151-case suite on each CPU, 453 runs. Eval: all 37 Workbench and 20 MorphOS entry vectors under disabled, bounded and fixed-point peepholes on each CPU (333 and 180 runs); receipts in the JSON.',
              '- Changed accepted shipping entries passed baseline/candidate comparisons on 68000, 68020 and 68040. Output bytes, return codes, IoErr, resource events, allocation ownership/releases, repeated invocation and supported interleaving are compared. Copy keeps compiler-owned invocation context separate from command allocations.',
              '- Fresh source publishes of Execute, List, Search and Reboot match their qualified frozen images exactly. Filtered builds preserve unselected output, and a failed build leaves previous executables unchanged.',
              '- Copper68k package 1.5.1; actual product version `' + product_version + '`; core SHA256 `' + identity(core)['sha256'] + '`. Actual receipt identities are retained.', '',
              '## Limits and remaining opportunities', '']
    lines += ['- ' + limit for limit in result['limits']]
    lines += ['', 'The audit’s helper sketches and 42 byte-read candidates are investigation bounds. They are not all removable: liveness, canonical wider consumers and alias effects keep some transport. Whole-image helper choices still grow some commands; their final policies reject that growth. Large Workbench gaps remain, especially aggregate-heavy List/Search and parser/formatter implementations.', '',
              '## Reproduction and delivery', '',
              'Use `tools/Commands/optimize_codegen_sizes.py` for frozen-input compile, qualify, matrix, select and publish operations. `tools/Commands/report_codegen_sizes.py` regenerates this report. `tools/Commands/build-c.ps1` stages selected commands before refreshing its output and preserves unselected commands for filtered builds.', '',
              'Build the compiler branch with .NET SDK 10.0.401, then publish with `tools/Commands/build-c.ps1 -CopperSharpRoot C:/Users/ilkle/Koodit/GIT/CopperSharp68k-wt-codegen-size`. The unrelated dirty primary compiler checkout was preserved and does not contain these commits yet. An older compiler cannot interpret the new selected-pass names.', '',
              f"Local immutable inputs, all baseline executables, trials, maps, compiler payloads and native receipts: `{frozen}`. Final staging: `{final}`.", '',
              'Separate compiler commits and patch hashes are listed in the JSON. CopperOS tooling, per-command enablement and reporting are separate changes. Publication requires all 61 images, matching hashes, a smaller aggregate and qualification for every changed executable; previous output is retained before stale files are removed.', '',
              '[Machine-readable report](code-generator-size-optimization-20261005.json)']
    if result['publication']:
        lines += ['', f"Published to `{result['publication']['directory']}`: all 61 command hashes match staging. Previous output is retained in `{result['publication']['backup']}`."]
    output.with_suffix('.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print(f"Reported {len(commands)} commands: {result['selectedTotalBytes']} bytes, saved {saved}")


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--frozen', type=Path, required=True)
    parser.add_argument('--final', type=Path, required=True)
    parser.add_argument('--matrix', type=Path, required=True)
    parser.add_argument('--compiler-root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--decoder-path', type=Path)
    args = parser.parse_args()
    report(args.frozen.resolve(), args.final.resolve(), args.matrix.resolve(), args.compiler_root.resolve(),
           args.output.resolve(), args.decoder_path.resolve() if args.decoder_path else None)
