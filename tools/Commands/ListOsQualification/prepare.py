"""Prepare immutable List command, runner, SDK and compiler inputs in a fresh stage."""
import json
import shutil
import sys
from pathlib import Path

repo = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / 'tools/Commands'))
from measure_c import identity

stage = Path(sys.argv[1]).resolve()
if stage.exists():
    raise RuntimeError('Preserve prior evidence; choose a fresh stage.')
pressure = repo / 'artifacts/list-pressure-20261016'
previous = repo / 'artifacts/search-main-toolchain-20261012'
(stage / 'inputs').mkdir(parents=True)
shutil.copytree(previous / 'runner', stage / 'runner')
shutil.copytree(pressure / 'main-candidate/compiler', stage / 'compiler')
shutil.copytree(pressure / 'inputs/List', stage / 'sdk')
shutil.copy2(pressure / 'baseline/C/List', stage / 'inputs/List.baseline')
shutil.copy2(repo / 'out/C/List', stage / 'inputs/List.candidate')
assert identity(stage / 'inputs/List.candidate')['sha256'] == identity(pressure / 'default-build/C/List')['sha256']
(stage / 'inputs/TEXT.IN').write_bytes(b'Alpha\nNeedle\nomega\n')
(stage / 'inputs/LONG.IN').write_bytes(b'x' * 1025)
manifest = {directory: [identity(p) for p in sorted((stage / directory).rglob('*')) if p.is_file()]
            for directory in ['runner', 'compiler', 'sdk', 'inputs']}
(stage / 'freeze.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
print(stage)
