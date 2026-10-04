"""Publish the accepted executable measurements and qualification limits."""
import argparse
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from measure_c import identity


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def report(audit, destination):
    baseline = read(audit / 'baseline/measurement.json')
    final = read(audit / 'final/measurement.json')
    qualified = {r['command']: r for r in read(audit / 'final/qualification.json')}
    before = {r['command']: r for r in baseline['commands']}
    if set(before) != set(qualified) or set(before) != {r['command'] for r in final['commands']}:
        raise ValueError('Incomplete measurements or qualification.')
    rows = []
    for item in final['commands']:
        name = item['command']
        checks = qualified[name]
        old = before[name]
        if item['image']['bytes'] != old['image']['bytes'] and not checks['accept']:
            raise ValueError(f'Unqualified change: {name}')
        rows.append({'command': name, 'beforeBytes': old['image']['bytes'],
                     'afterBytes': item['image']['bytes'], 'savedBytes': checks['savedBytes'],
                     'sha256': item['image']['sha256'], 'qualified': checks['qualified'],
                     'accepted': checks['accept'], 'policy': item['flags'].get('code-size-optimizations', 'off'),
                     'workbench31Bytes': old['workbench31']['bytes'] if old['workbench31'] else None,
                     'workbench31Sha256': old['workbench31']['sha256'] if old['workbench31'] else None,
                     'ratioToWorkbench31': item['ratioToWorkbench31'],
                     'before': {k: old[k] for k in ('records', 'reachableMethods', 'largestMethods')},
                     'after': {k: item[k] for k in ('records', 'reachableMethods', 'largestMethods')},
                     'execution': checks['checks']})
    passes = {}
    for folder in sorted((audit / 'passes').iterdir()):
        checks = read(folder / 'qualification.json')
        if not all(r['qualified'] for r in checks):
            raise ValueError(f'Unqualified independent pass: {folder.name}')
        passes[folder.name] = checks
    profiles = read(audit / 'profiles/qualification.json')
    if any(r['compileExit'] or any(c['exit'] for c in r['checks']) for r in profiles):
        raise ValueError('A shared source profile failed.')
    results = ET.parse(audit / 'compiler-tests/resident-size-final.trx').getroot()
    tests = results.findall('.//{*}UnitTestResult')
    if any(t.attrib['outcome'] != 'Passed' for t in tests):
        raise ValueError('Compiler regression failure.')
    source_rows = read(audit / 'source-ref/qualification.json')
    specialization = {name: read(audit / f'source-profile/{name}/qualification.json')[0]
                      for name in ('Dir', 'Mount')}
    argument_rows = []
    for row in source_rows:
        if not row['accept']:
            continue
        start = specialization[row['command']]['afterBytes'] if row['command'] in specialization else row['beforeBytes']
        argument_rows.append({'command': row['command'], 'beforeBytes': start,
                              'afterBytes': row['afterBytes'], 'savedBytes': start - row['afterBytes']})
    execution = [c for r in rows for c in r['execution'] if c['passed']]
    cpu_summary = {}
    for cpu in ('68000', '68020', '68040'):
        checks = [c for c in execution if c['cpu'] == cpu]
        cpu_summary[cpu] = {'invocations': sum(c['invocations'] for c in checks),
                            'instructionsBefore': sum(c['instructionsBefore'] for c in checks),
                            'instructionsAfter': sum(c['instructionsAfter'] for c in checks),
                            'peakStackBefore': max(c['peakStackBefore'] for c in checks),
                            'peakStackAfter': max(c['peakStackAfter'] for c in checks)}
    report = {'schemaVersion': 2, 'beforeBytes': baseline['totalBytes'], 'afterBytes': final['totalBytes'],
              'savedBytes': baseline['totalBytes'] - final['totalBytes'],
              'savedPercent': (baseline['totalBytes'] - final['totalBytes']) * 100 / baseline['totalBytes'],
              'commandCount': len(rows), 'acceptedCommands': sum(r['accepted'] for r in rows),
              'previousReductionsIncludedInBaseline': ['Delete', 'List', 'Search'],
              'baselineProvenance': {k: baseline[k] for k in ('source', 'compiler', 'dotnetSdk', 'compilerFiles')},
              'finalProvenance': {k: final[k] for k in ('source', 'compiler', 'dotnetSdk', 'compilerFiles')},
              'sdkVersion': '0.1.0-preview.1; exact matching SDK assembly hashes are in each input receipt',
              'flags': {'cpu': '68000', 'format': 'hunk', 'runtime': 'resident', 'exceptions': 'yolo',
                        'memory': 'none', 'peephole': 'fixed-point', 'symbols': 'off'},
              'core': {'packageVersion': '1.5.1', 'assemblyVersion': '1.5.1.0',
                       'productVersion': '1.5.1+43a3a84f79657cb4c2b32bb58d8e510e3aec9c15',
                       'sha256': next(c['coreSha256'] for r in rows for c in r['execution'] if c['passed'])},
              'compilerRegressions': {'passed': len(tests), 'branchLivenessPassed': sum(
                  'M68kDataRegisterBranchPeepholeTests' in t.attrib['testName'] for t in tests)},
              'executionSummary': cpu_summary,
              'evalModes': {mode: read(audit / mode / 'qualification.json') for mode in ('eval-disabled', 'eval-bounded')},
              'fixedPointEval': qualified['Eval'], 'specialization': specialization,
              'argumentPassing': argument_rows, 'independentPasses': passes,
              'sharedProfileFixtures': profiles, 'contextAccounting': read(audit / 'context-accounting/summary.json'),
              'filteredBuild': read(audit / 'filtered/receipt.json'),
              'smallCommandReview': {'Beep': 'No unused incoming arguments; no further encoded gain.',
                                     'Check2090': 'Startup checks and local ownership retained; return sharing saves four bytes.',
                                     'Reboot': 'Empty-template parsing, Ctrl-C and Workbench lifecycle retained; return sharing saves four bytes.'},
              'limits': ['Exec/DOS vector fixtures model parser and I/O responses; no real Kickstart or CopperStart execution.',
                         'CopperScreen.Headless.Cli is absent from ../MedPlayer on this host.',
                         'Pinned AddBuffers and Relabel originals are absent from D:/TestData/CopperOSCommands/Workbench31.',
                         'Exe2Arc lacks a shipping-entry execution fixture; it retains its baseline policy and executable.',
                         'Mount profile fixtures cover entry and startup boundaries; they are not complete real-parser or filesystem replay.',
                         'Peak stack is the minimum observed A7 at instruction boundaries; existing configured stack limits are retained.',
                         'Workbench ratios use inventoried original MC68000 HUNK sizes; MorphOS-only commands have no PPC size ratio.',
                         'The main compiler workspace has pre-existing changes; reproduction uses the clean codex/command-executable-size checkout and captured compiler.'],
              'receipts': str(audit.resolve()), 'commands': rows}
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(f'{len(rows)} commands: {report["beforeBytes"]} -> {report["afterBytes"]}, saved {report["savedBytes"]} bytes.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('audit', type=Path)
    parser.add_argument('destination', type=Path)
    args = parser.parse_args()
    report(args.audit.resolve(), args.destination.resolve())
