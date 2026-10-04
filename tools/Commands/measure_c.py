"""Capture reproducible command images, inputs, maps and raw Workbench sizes."""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
from pathlib import Path


def identity(path):
    path = Path(path)
    return {"path": str(path.resolve()), "bytes": path.stat().st_size,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def git_state(root):
    def git(*args):
        return subprocess.check_output(["git", "-C", str(root), *args])
    diff = git("diff", "--binary", "HEAD")
    return {"root": str(root.resolve()), "commit": git("rev-parse", "HEAD").decode().strip(),
            "status": git("status", "--short").decode().splitlines(),
            "trackedPatchSha256": hashlib.sha256(diff).hexdigest(),
            "untrackedFiles": [identity(root / name) for name in
                               git("ls-files", "--others", "--exclude-standard").decode().splitlines()
                               if (root / name).is_file()]}


def parse_map(path):
    text = path.read_text(encoding="utf-8-sig")
    records = {}
    for line in text.splitlines():
        if line.startswith(("METRICS ", "PEEPHOLE ", "MACHINE ", "AGGREGATE-FORWARDING ",
                            "BULK-COPY ", "CODE-SIZE ", "RESIDENT-CONTEXT ")):
            name, _, values = line.partition(" ")
            records[name] = {k: int(v) if v.isdecimal() else v for k, v in
                             re.findall(r"([\w-]+)=([^ ]+)", values)}
    methods = [{"offset": int(m[1], 16), "bytes": int(m[2]), "method": m[3]}
               for m in re.finditer(r"^([0-9A-F]{8})\s+(\d+)\s+(.+::.+)$", text, re.M)]
    return {"records": records, "reachableMethods": len(methods),
            "largestMethods": sorted(methods, key=lambda x: x["bytes"], reverse=True)[:10]}


def capture(repo, compiler, stage, compiler_assembly=None):
    image_dir = stage / "C"
    inventory = json.loads((repo / "docs/Commands/Workbench31MorphOS320/command-inventory.json").read_text())
    references = {c["name"].casefold(): c.get("reference_profiles", {}).get("wb31", {}).get("source_files", [])
                  for c in inventory["commands"]}
    rows = []
    for image in sorted(image_dir.iterdir()):
        if not image.is_file() or image.suffix in (".map", ".json", ".rsp", ".log"):
            continue
        name = image.name
        project = repo / "src/Commands" / name
        publish = project / "bin/Release/net10.0/amiga-m68k/publish" / name
        if image.read_bytes() != publish.read_bytes():
            raise RuntimeError(f"Staged image differs from publish: {name}")
        destination = stage / "inputs" / name
        destination.mkdir(parents=True, exist_ok=True)
        manifest = project / "obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp"
        source_lines = manifest.read_text(encoding="utf-8-sig").splitlines()
        inputs, lines = [], []
        for line in source_lines:
            if line.startswith(("input=", "managed-assembly=")):
                key, value = line.split("=", 1)
                source = Path(value)
                copied = destination / source.name
                shutil.copy2(source, copied)
                inputs.append(identity(copied))
                lines.append(f"{key}={copied.resolve()}")
            elif not line.startswith(("output=", "compatibility-report=")):
                lines.append(line)
        (destination / "compile.rsp").write_text("\n".join(lines) + "\n", encoding="utf-8")
        map_path = stage / "maps" / (name + ".map")
        map_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(str(publish) + ".map", map_path)
        report = Path(str(publish) + ".framework.json")
        if report.exists():
            shutil.copy2(report, destination / "framework.json")
        originals = [r for r in references.get(name.casefold(), []) if r.get("file_format") == "amiga-hunk"]
        reference = originals[0] if originals else None
        rows.append({"command": name, "image": identity(image), "inputs": inputs,
                     "flags": dict(line.split('=', 1) for line in lines
                                   if '=' in line and not line.startswith(('input=', 'managed-assembly='))),
                     **parse_map(map_path), "workbench31": reference,
                     "ratioToWorkbench31": image.stat().st_size / reference["bytes"] if reference else None})
    compiler_files = stage / "compiler"
    if not compiler_files.exists():
        shutil.copytree(compiler_assembly.parent if compiler_assembly else
                        compiler / "Compiler.Cli/bin/Release/net10.0", compiler_files)
    report = {"schemaVersion": 1, "source": git_state(repo), "compiler": git_state(compiler),
              "dotnetSdk": subprocess.check_output(["dotnet", "--version"], cwd=repo).decode().strip(),
              "compilerFiles": [identity(p) for p in compiler_files.glob("*.dll")],
              "totalBytes": sum(r["image"]["bytes"] for r in rows), "commands": rows}
    (stage / "measurement.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Captured {len(rows)} commands, {report['totalBytes']} bytes at {stage}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", type=Path)
    parser.add_argument("--compiler", type=Path, required=True)
    parser.add_argument("--compiler-assembly", type=Path)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    capture(args.repo.resolve(), args.compiler.resolve(), args.stage.resolve(),
            args.compiler_assembly.resolve() if args.compiler_assembly else None)
