"""Run the isolated friend test against the complete production emulator.

Every invocation requires a new artifact directory. An install-stage success
proves only native-library readiness, never command or complete boot behavior.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "tools/Commands/CopperStartBootIntegration/CopperStartBootIntegration.csproj"


def bound(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "bytes": path.stat().st_size}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--stage", choices=("install", "command"), required=True)
    parser.add_argument("--native-dos", type=Path, default=ROOT / "artifacts/copperstart-makelink-host-packet/final/dos.hunk")
    parser.add_argument("--command", type=Path, default=ROOT / "artifacts/workbench-makelink-filehandle-abi-20260908/pass-a/makelink-68000.hunk")
    parser.add_argument("--launcher", type=Path)
    parser.add_argument("--launcher-map", type=Path)
    parser.add_argument("--sessions", type=int, choices=range(1, 9), default=1)
    args = parser.parse_args()
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    inputs = [bound(args.native_dos), bound(args.native_dos.with_suffix(".map")), bound(args.command), bound(PROJECT),
              bound(PROJECT.parent / "NativeDosBootIntegrationTests.cs"), bound(__file__)]
    environment = os.environ.copy()
    environment.update(COPPER_BOOT_INTEGRATION_OUTPUT=str(out), COPPER_BOOT_INTEGRATION_STAGE=args.stage,
                       COPPER_BOOT_SESSION_COUNT=str(args.sessions),
                       COPPER_BOOT_NATIVE_DOS=str(args.native_dos.resolve()), COPPER_BOOT_MAKELINK=str(args.command.resolve()))
    for path, name in ((args.launcher, "COPPER_BOOT_LAUNCHER"), (args.launcher_map, "COPPER_BOOT_LAUNCHER_MAP")):
        if path:
            inputs.append(bound(path))
            environment[name] = str(path.resolve())
    managed_sources = [path for parent in (ROOT.parent / "MedPlayer/CopperMod.Amiga.Emulator",
                                          ROOT.parent / "CopperStart/src/CopperStart.Dos")
                       for path in parent.rglob("*.cs") if not {"bin", "obj"}.intersection(path.parts)]
    sources_before = [bound(p) for p in sorted(managed_sources)]
    (out / "sources-before.json").write_text(json.dumps(sources_before, indent=2) + "\n", encoding="utf-8")
    commands = []

    def run(argv, name):
        commands.append(argv)
        with (out / name).open("x", encoding="utf-8", newline="\n") as log:
            log.write(json.dumps(argv) + "\n")
            log.flush()
            result = subprocess.run(argv, cwd=ROOT, env=environment, stdout=log, stderr=subprocess.STDOUT)
        return result.returncode

    build_exit = run([str(args.dotnet.resolve()), "build", str(PROJECT), "-c", "Release", "--no-incremental",
                      "--artifacts-path", str(out / "build"), "--nologo"], "build.log")
    test_exit = None
    if build_exit == 0:
        assembly = out / "build/bin/CopperStartBootIntegration/release/CopperMod.Amiga.Tests.dll"
        assert assembly.is_file(), assembly
        test_exit = run([str(args.dotnet.resolve()), "vstest", str(assembly),
                         f"/Logger:trx;LogFileName={out / 'test.trx'}"], "test.log")
    unchanged = all(bound(item["path"]) == item for item in inputs + sources_before)
    receipt = {"schema_version": 1, "stage": args.stage, "build_exit": build_exit, "test_exit": test_exit,
               "sessions": args.sessions,
               "sources_unchanged": unchanged, "inputs": inputs, "commands": commands,
               "source_snapshot": bound(out / "sources-before.json"),
               "outputs": [bound(out / name) for name in ("build.log", "test.log", "test.trx", "result.json") if (out / name).is_file()],
               "shipping": False, "pure_admission": False, "full_system_boot_qualified": False}
    (out / "runner-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: receipt[key] for key in ("stage", "build_exit", "test_exit", "sources_unchanged")}))
    return 0 if build_exit == 0 and test_exit == 0 and unchanged else 1


if __name__ == "__main__":
    sys.exit(main())
