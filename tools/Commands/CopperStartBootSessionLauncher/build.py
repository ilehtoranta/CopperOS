"""Compile the guest integration launcher with an already identified compiler."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools/Commands"))
from build_workbench_makelink_versioned import support_from_restore

def bound(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--dotnet", type=Path, required=True)
parser.add_argument("--compiler", type=Path, required=True)
parser.add_argument("--managed-build", type=Path, required=True)
parser.add_argument("--output", type=Path, required=True)
args = parser.parse_args()
output = args.output.resolve()
output.mkdir(exist_ok=False)
managed = args.managed_build.resolve() / "bin/CopperStartBootSessionLauncher/release"
assembly = managed / "CopperOS.Commands.BootSessionLauncher.dll"
assets = args.managed_build.resolve() / "obj/CopperStartBootSessionLauncher/project.assets.json"
support = support_from_restore(assets)
inputs = [bound(path) for folder in (args.compiler.resolve().parent, managed)
          for path in sorted(folder.iterdir()) if path.suffix in (".dll", ".json")]
inputs.append(bound(support))
hunk = output / "session-launcher.hunk"
command = [args.dotnet, args.compiler, assembly, "--entry",
    "CopperOS.Commands.BootSessionLauncher.SessionLauncher::ImageEntry", "--platform", "amiga",
    "--cpu", "68000", "--clr", "always", "--exceptions", "yolo", "--format", "hunk",
    "--runtime", "resident", "--memory", "none", "--peephole", "disabled", "--fpu", "disabled",
    "--include-export", "copperos.boot-session.start", "--include-export", "copperos.boot-session.child",
    "--include-export", "copperos.boot-session.exit",
    "--managed-assembly", managed / "CopperSharp.Sdk.Amiga.dll", "--managed-assembly", support,
    "--managed-assembly", managed / "CopperSharp.Compiler.dll", "--output", hunk,
    "--compatibility-report", output / "native-compatibility.json"]
with (output / "compile.log").open("x", encoding="utf-8") as log:
    log.write(json.dumps([str(item) for item in command]) + "\n")
    log.flush()
    completed = subprocess.run([str(item) for item in command], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
if completed.returncode:
    raise SystemExit("Native compiler failed; see " + str(output / "compile.log"))
for item in inputs:
    assert bound(item["path"]) == item, item
report = {"status": "built-not-executed", "shipping": False, "pure_admission": False,
    "scope": "Compiled public guest process/command integration launcher, not an external command or runtime qualification.",
    "inputs": inputs, "managed_assets": bound(assets), "command": [str(item) for item in command],
    "sources": [bound(Path(__file__)), bound(Path(__file__).with_name("SessionLauncher.cs")),
                bound(Path(__file__).with_name("CopperStartBootSessionLauncher.csproj"))],
    "hunk": bound(hunk), "map": bound(Path(str(hunk) + ".map")),
    "native_compatibility": bound(output / "native-compatibility.json")}
(output / "build.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"status": report["status"], "hunk": report["hunk"]}))
