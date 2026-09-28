"""Build a fresh native DOS image for the isolated production boot test.

This records compiler inputs and output bytes; it does not qualify runtime
behavior, reproducibility, command release, or PURE/resident admission.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "tools/Commands/CopperStartNativeDosBuilder/CopperStartNativeDosBuilder.csproj"


def bound(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "bytes": path.stat().st_size}


def sources():
    roots = [ROOT.parent / "CopperStart/src", ROOT.parent / "CopperSharp68k/Compiler",
             ROOT.parent / "CopperSharp68k/Targets.Amiga", ROOT.parent / "CopperSharp68k/Sdk.Amiga",
             ROOT.parent / "CopperSharp68k/Sdk.Amiga.Support", PROJECT.parent]
    paths = {path for parent in roots for path in parent.rglob("*")
             if path.is_file() and path.suffix in (".cs", ".csproj", ".props", ".targets")
             and not {"bin", "obj"}.intersection(path.parts)}
    for parent in (ROOT.parent / "CopperStart", ROOT.parent / "CopperSharp68k", ROOT):
        paths.update(path for path in parent.glob("*") if path.is_file() and
                     (path.suffix in (".props", ".targets") or path.name in ("global.json", "NuGet.Config", "nuget.config")))
    return [bound(path) for path in sorted(paths)]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    before = sources()
    (out / "sources-before.json").write_text(json.dumps(before, indent=2) + "\n", encoding="utf-8")
    inputs = [bound(__file__), bound(args.dotnet)]
    commands = []

    def run(argv, log_name):
        commands.append(argv)
        with (out / log_name).open("x", encoding="utf-8") as log:
            log.write(json.dumps(argv) + "\n")
            log.flush()
            return subprocess.run(argv, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT).returncode

    build_exit = run([str(args.dotnet.resolve()), "build", str(PROJECT), "-c", "Release", "--no-incremental",
                      "--artifacts-path", str(out / "build"), "--nologo"], "build.log")
    compile_exit = None
    if build_exit == 0:
        producer = out / "build/bin/CopperStartNativeDosBuilder/release/CopperStartNativeDosBuilder.dll"
        compile_exit = run([str(args.dotnet.resolve()), str(producer), str(out / "native")], "compile.log")
    unchanged = sources() == before and all(bound(item["path"]) == item for item in inputs)
    result = {"schema_version": 1, "build_exit": build_exit, "compile_exit": compile_exit,
              "sources_unchanged": unchanged, "inputs": inputs, "source_snapshot": bound(out / "sources-before.json"),
              "commands": commands, "outputs": [bound(path) for path in sorted(out.rglob("*")) if path.is_file()
                  and (path.suffix in (".dll", ".json", ".hunk", ".map", ".log") and "ref" not in path.parts and "refint" not in path.parts)],
              "shipping": False, "pure_admission": False, "runtime_qualified": False,
              "source_snapshot_scope": "Listed source roots and root build configuration only; not a hermetic build or two-build reproducibility claim."}
    (out / "runner-receipt.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: result[key] for key in ("build_exit", "compile_exit", "sources_unchanged")}))
    return 0 if build_exit == 0 and compile_exit == 0 and unchanged else 1


if __name__ == "__main__":
    sys.exit(main())
