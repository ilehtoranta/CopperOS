"""Capture the qualified List formatting-source comparison and full staging inventory."""
import argparse
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from measure_c import identity, git_state, parse_map


def generate(frozen, stage, compiler_root, output):
    freeze = json.loads((frozen / 'freeze.json').read_text())
    wb = json.loads((stage / 'qualification.json').read_text())
    morph = json.loads((frozen / 'morphos/after-final/qualification.json').read_text())
    if not all(row['accept'] for row in wb + morph):
        raise RuntimeError('List qualification failed.')
    rows = []
    for item in freeze['commands']:
        name = item['command']
        before, after = frozen / 'accepted/C' / name, stage / 'C' / name
        if name != 'List' and before.read_bytes() != after.read_bytes():
            raise RuntimeError(f'Unqualified change: {name}')
        rows.append({'command': name, 'before': identity(before), 'after': identity(after),
                     'map': parse_map(Path(str(after) + '.map'))})
    root = Path(__file__).resolve().parents[2]
    before_inputs = {Path(r['path']).name: r for r in next(r for r in freeze['commands'] if r['command'] == 'List')['inputs']}
    candidate = json.loads((frozen / 'candidate-frozen/freeze.json').read_text())
    after_inputs = {Path(r['path']).name: r for r in next(r for r in candidate['commands'] if r['command'] == 'List')['inputs']}
    for name in before_inputs:
        if name != 'CopperOS.Commands.List.dll' and before_inputs[name]['sha256'] != after_inputs[name]['sha256']:
            raise RuntimeError(f'List dependency differs: {name}')
    tests = {}
    for name, filename in [('compiler', 'compiler-qualified-final.trx'), ('host', 'host-profiles.trx')]:
        path = frozen / 'tests' / filename
        tests[name] = {'identity': identity(path), 'counts': ET.parse(path).find('.//{*}Counters').attrib}
    control_map = parse_map(frozen / 'control-final/C/List.map')
    data = {'baseline': freeze, 'candidateInputs': candidate, 'compiler': git_state(compiler_root),
            'sourceBefore': identity(frozen / 'NativeMorphOSListCommand.before.cs'),
            'sourceAfter': identity(root / 'src/Commands/List/Native/NativeMorphOSListCommand.cs'),
            'commands': rows, 'workbench': wb, 'morphos': morph, 'tests': tests,
            'controlMap': control_map,
            'compilation': json.loads((stage / 'compilation.json').read_text()),
            'morphosCompilation': json.loads((frozen / 'morphos/compilation.json').read_text()),
            'morphosFinalCompilation': json.loads((frozen / 'morphos/final-compilation.json').read_text()),
            'publication': json.loads((stage / 'publication.json').read_text()) if (stage / 'publication.json').exists() else None,
            'beforeBytes': sum(r['before']['bytes'] for r in rows),
            'afterBytes': sum(r['after']['bytes'] for r in rows)}
    for path in frozen.glob('*/C/List'):
        data.setdefault('experimentalImages', []).append(identity(path))
    output.with_suffix('.json').write_text(json.dumps(data, indent=2) + '\n')
    old = next(r for r in control_map['records']['ALLOCATION'] if r['method'].endswith('::Run'))
    new = next(r for r in next(r for r in rows if r['command'] == 'List')['map']['records']['ALLOCATION'] if r['method'].endswith('::Run'))
    lines = ['# List formatting record reduction — 2026-10-05', '',
             f"**61 shipping commands: {data['beforeBytes']:,} → {data['afterBytes']:,} bytes.**", '',
             '| Profile | Before | After | Saved |', '| --- | ---: | ---: | ---: |',
             '| Workbench shipping List | 10,620 | 10,232 | 388 |',
             '| MorphOS profile, MC68000 HUNK | 11,064 | 10,676 | 388 |', '',
             'The previous 24-byte stack-copy reduction is already in this baseline and is not counted again. The other 60 shipping images are byte-identical. Workbench List remains 2.00× the admitted genuine Workbench 3.1 size of 5,108 bytes; no PPC-size comparison is made for the MorphOS profile.', '',
             '## Implementation', '',
             'List previously constructed four metadata records (88 bytes) and a quick record (4 bytes) before choosing one. It now packs the selected format prefix into one 28-byte stack record and selects the format before one VPrintf call. Name, size, key, protection, dates, time and comment retain their selected positions. Maximum write offset is 24, ending at byte 28. Existing file/key reads, formatting calls, options, errors, allocation ownership, startup and cleanup are preserved.', '',
             'Each format literal is converted to CString inside its branch before values merge. The public compiler control ValidateLiteralOperands is opt-in and defaults to false. CLI/response-file code-size-passes selects it; only shipping List enables it. It checks input legality and has no executable-byte savings attributed to it.', '',
             '## Independent measurements', '',
             '| Experiment | List bytes | Decision |', '| --- | ---: | --- |',
             '| Rearranging typed fields in one record | 11,080 | Reject: grows 460 bytes |',
             '| Sequential packing with separate calls | 10,668 | Reject: grows 48 bytes; native comparisons pass |',
             '| One call retaining all typed records | 10,640 | Reject: grows 20 bytes |',
             '| Packing plus one call, conversion after string merge | 10,156 | Reject: wrong output on all CPUs |',
             '| Packing plus one call, literals converted in each branch | 10,232 | Accept |', '',
             'The accepted reduction depends on the combined implementation. Individual source sketches and rejected image sizes are not additive savings.', '',
             '## Frame and register traffic', '',
             '| Final allocated IR metric, before peepholes | Before | After |', '| --- | ---: | ---: |']
    for label, key in [('Frame bytes', 'frame-bytes'), ('Saved-register bytes', 'saved-bytes'),
                       ('Spill area bytes', 'spill-bytes'), ('Spill reloads', 'reloads'),
                       ('Stack instructions', 'pre-peephole-stack-instructions')]:
        lines.append(f"| {label} | {old[key]} | {new[key]} |")
    lines += ['', 'The spill area remains unchanged; this source change removes 64 bytes of formatting homes and reduces reloads. Spilled-value counts accumulate across allocation rounds and are not permanent slot counts. Remaining register pressure requires a separate compiler investigation.', '',
              '## Qualification', '',
              '- Workbench shipping entry: 32 fixture invocations per CPU on 68000, 68020 and 68040; MorphOS profile: 31 per CPU. All 189 baseline/candidate comparisons pass with matching output, return codes, IoErr and resource/allocation events.',
              '- Workbench fixture instruction totals per CPU: 64,306 → 64,063. MorphOS: 69,430 → 69,187. Peak stack in both profiles: 356 → 292 bytes. No additional execution overhead is measured in those totals.',
              f"- {tests['compiler']['counts']['passed']} focused compiler tests passed, including the new conditional-literal regression, converted-branch execution across three CPUs and two relocation bases, and existing branch-liveness, register/CCR, alias and stack regressions.",
              f"- {tests['host']['counts']['passed']} host command tests passed, including applicable profile fixtures.",
              '- A fresh source publish matches the qualified frozen candidate exactly. All 61 accepted commands rebuild into staging; completeness and hashes are checked before refreshing out/C.',
              f"- Compiler starts at f7986bf and finishes at {data['compiler']['commit']} in the isolated codex/list-search-stack-size checkout. Comparisons use identical compiler binaries, flags and SDK dependencies; only List’s command assembly differs. The JSON records SDK, input, compiler, map, receipt and image identities.",
              '- MC68000 resident HUNK, YOLO exceptions, memory disabled, fixed-point peepholes, symbols disabled and existing stack limits.',
              '- Copper68k package 1.5.1; actual product 1.5.1+43a3a84f79657cb4c2b32bb58d8e510e3aec9c15; core SHA256 99173918a4da1cd5446d8f6e115e6b5c4d0a8f1c7f9173261c754988d3cbcb8f.', '',
              '## Compiler discovery and limits', '',
              'The literal-address emitter identifies a literal using the physically preceding ldstr. A branch entering the conversion may instead supply another literal, causing silent wrong-code selection. Opt-in validation now rejects that merge; supported source converts each literal before merging CString values. This is validation of the existing literal-only API contract, not general support for managed string operands.', '',
              'Unconditional validation exposed an existing merged-string expression in ModList and prevented its rebuild. The validator therefore remains disabled by default and ModList keeps its exact accepted image. Its native fixture accepts both format strings but always renders revision and priority as if the revision form were selected; it cannot qualify real format selection. Correct ModList’s conversion and fixture before enabling strict validation there. Other legacy callers may contain the same unsupported pattern.', '',
              'Original Workbench execution and CopperScreen checks remain unavailable. The previously documented broad compiler-suite failures remain outside this focused qualification. Native MorphOS qualification executes our MC68000 profile, not a PPC original. These limitations are not treated as passing checks.']
    output.with_suffix('.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    return data


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('frozen', 'stage', 'compiler-root', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    generate(args.frozen.resolve(), args.stage.resolve(), args.compiler_root.resolve(), args.output.resolve())
