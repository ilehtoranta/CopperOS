"""Qualify Exe2Arc through Search's frozen original-DOS execution harness."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import importlib.util
import json
import os
from pathlib import Path
import shutil
import sys

from measure_c import identity, git_state
from qualify_sizes import run

REPO = Path(__file__).resolve().parents[2]
TOOLS = REPO/'tools/Commands/Exe2ArcOsQualification'
RUNTIME = REPO/'artifacts/exe2arc-runtime-v8-20261017'
SEARCH = REPO/'artifacts/receiver-rollout-v3-20261017/native/Search'


def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))
def save(path, data): path.write_text(json.dumps(data, indent=2)+'\n', encoding='utf-8')
def module(name):
    spec = importlib.util.spec_from_file_location('Exe2ArcOS'+name, TOOLS/(name+'.py'))
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result); return result


def prepare(stage):
    assert not stage.exists(), 'Use a fresh stage and preserve earlier evidence.'
    stage.mkdir(parents=True)
    proof_path = REPO/'docs/Commands/Workbench31MorphOS320/exe2arc-runtime-qualification-20261017.json'
    proof = read(proof_path)
    for item in proof['freeze']['files']:
        if 'compiler-candidate' in item['path'] or 'dependencies' in item['path']: assert identity(item['path']) == item
    assert identity(proof['candidate']['path']) == proof['candidate']
    assert (REPO/'out/C/Exe2Arc').read_bytes() == Path(proof['candidate']['path']).read_bytes()
    shutil.copytree(SEARCH/'runner', stage/'runner')
    shutil.copytree(RUNTIME/'compiler-candidate', stage/'compiler')
    shutil.copytree(RUNTIME/'dependencies', stage/'dependencies')
    shutil.copytree(REPO/'out/C', stage/'installed/C')
    inputs = stage/'inputs'; inputs.mkdir()
    for label in ['baseline', 'candidate']: shutil.copy2(RUNTIME/'repaired'/label/'C/Exe2Arc', inputs/('Exe2Arc.'+label))
    for name in ['Guest.cs', 'Guest.csproj', 'Host.cs', 'Host.csproj', 'Bind.cs', 'cases.py', 'verify.py', 'raw.py', 'control-auditor.py', 'confirm-winuae.ps1', 'report.py']:
        target = stage/'source'/name; target.parent.mkdir(exist_ok=True); shutil.copy2(TOOLS/name, target)
    guest = stage/'guest'; host = stage/'host'
    assert run(['dotnet', 'build', str(stage/'source/Guest.csproj'), '-c', 'Release', '--nologo',
                '-p:FrozenInputs='+str(stage/'dependencies'), '--artifacts-path', str(stage/'build/guest'), '-o', str(guest/'cil')], stage/'guest-build.log') == 0
    response = guest/'compile.rsp'
    lines = ['coppersharp-response-v1', 'input='+str(guest/'cil/CopperOS.Exe2ArcQualification.Guest.dll'),
             'entry=CopperOS.Exe2ArcQualification.Guest::Main', 'output='+str(guest/'Exe2ArcQualification'),
             'platform=amiga', 'cpu=68000', 'exceptions=yolo', 'format=hunk', 'runtime=resident',
             'memory=none', 'peephole=fixed-point', 'symbols=off', 'exports=none', 'code-size-optimizations=off']
    lines += ['managed-assembly='+str(p) for p in (stage/'dependencies').glob('CopperSharp.*.dll')]
    response.write_text('\n'.join(lines)+'\n', encoding='utf-8')
    assert run(['dotnet', str(stage/'compiler/CopperSharp.Compiler.Cli.dll'), '@'+str(response)], guest/'compile.log') == 0
    assert run(['dotnet', 'build', str(stage/'source/Host.csproj'), '-c', 'Release', '--nologo',
                '-p:FrozenRunner='+str(stage/'runner'), '--artifacts-path', str(stage/'build/host'), '-o', str(host)], stage/'host-build.log') == 0
    assert run(['dotnet', str(host/'CopperOS.Exe2ArcQualification.Host.dll'), '--bind', str(stage/'runner'), str(stage/'runner-source-binding.json')], stage/'bind.log') == 0
    batches = module('cases').build(stage)
    rom = Path(os.environ.get('COPPERSHARP_KICKSTART31_ROM', 'C:/Data/ROM/kickstart-3.1-a500.rom'))
    assert identity(rom)['sha256'] == '8c8a0cf04f91b88eaf0c4f1126041987067e2286a8ee590bdbae447a8000c5ee'
    save(stage/'freeze.json', dict(source=git_state(REPO), runtimeQualification=identity(proof_path), rom=identity(rom), batches=batches,
         files=[identity(p) for p in sorted(stage.rglob('*')) if p.is_file()]))


def validate(stage):
    frozen = read(stage/'freeze.json')
    for item in frozen['files']: assert identity(item['path']) == item, item['path']
    assert identity(frozen['rom']['path']) == frozen['rom']
    return frozen


def execute(stage, smoke=False):
    frozen = validate(stage)
    jobs = [('smoke', '68000')] if smoke else [(batch, cpu) for cpu in ['68000', '68020', '68040'] for batch in frozen['batches'] if batch!='smoke']
    def one(job):
        batch, cpu = job; log = stage/(batch+'-'+cpu+'.log')
        code = run(['dotnet', str(stage/'host/CopperOS.Exe2ArcQualification.Host.dll'), str(stage), frozen['rom']['path'], cpu,
                    str(stage/('cases-'+batch+'.json')), batch], log)
        print(batch, cpu, 'host exit', code, flush=True)
        qualification = module('verify').verify(stage, batch, cpu) if code == 0 else None
        if qualification is not None: save(stage/'runs'/(batch+'-'+cpu)/'qualification.json', qualification)
        return dict(batch=batch, cpu=cpu, exitCode=code, qualification=qualification)
    with ThreadPoolExecutor(max_workers=2) as pool: rows = list(pool.map(one, jobs))
    save(stage/('smoke.json' if smoke else 'execution.json'), rows)
    assert all(row['exitCode']==0 and row['qualification']['passed'] for row in rows)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(__doc__); parser.add_argument('mode', choices=['prepare', 'smoke', 'execute'])
    parser.add_argument('--stage', type=Path, required=True); args=parser.parse_args()
    execute(args.stage.resolve(), smoke=True) if args.mode=='smoke' else globals()[args.mode](args.stage.resolve())
