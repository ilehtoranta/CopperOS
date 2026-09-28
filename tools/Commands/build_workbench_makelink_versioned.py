"""Build two isolated MakeLink development images per CPU, add $VER, and execute.

The observed reproducibility claim is byte equality of the raw and versioned
HUNKs from two fresh managed/native build directories on this host. It is not a
hermetic cross-host source build, shipping gate, minimum-stack or PURE approval.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import platform
import subprocess
import sys

from append_hunk_version import append_file, digest


VERSION = "$VER: MakeLink 0.1 (8.9.2026) CopperOS wb31"
ENTRY = "CopperOS.Commands.AddBuffersNativeRoot.Workbench31MakeLinkEntry::Main"
SUITE = "workbench-makelink-native-entry-vector-fixture"
CPUS = ("68000", "68020", "68040")
OPTIONS = ("--entry", ENTRY, "--platform", "amiga", "--clr", "always",
           "--exceptions", "yolo", "--format", "hunk", "--runtime", "resident",
           "--memory", "none", "--peephole", "disabled", "--fpu", "disabled")


def bound(path: Path) -> dict:
    path = path.resolve(strict=True)
    return {"path": str(path), "sha256": digest(path.read_bytes())}


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, value) -> None:
    with path.open("x", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, indent=2)
        stream.write("\n")


def run(command: list, cwd: Path, log: Path) -> None:
    with log.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(json.dumps([str(item) for item in command]) + "\n")
        stream.flush()
        completed = subprocess.run([str(item) for item in command], cwd=cwd, stdout=stream, stderr=subprocess.STDOUT)
    if completed.returncode:
        raise ValueError(f"Command failed ({completed.returncode}); retained log: {log}")


def source_snapshot(repo: Path, sharp: Path) -> list[dict]:
    """Bind observed relevant source/project trees, not a hermetic MSBuild claim."""
    groups = [repo / "src/Commands/Native", repo / "tests/Commands.AddBuffersNativeRoot"]
    groups += [sharp / name for name in ("Compiler", "Compiler.Cli", "Sdk.Amiga", "Targets.Amiga", "Runtime.Managed")]
    files = set()
    for group in groups:
        for path in group.rglob("*"):
            if any(part.casefold() in ("bin", "obj", ".git", "artifacts", ".artifacts") for part in path.relative_to(group).parts):
                continue
            if path.is_file() and path.suffix.casefold() in (".cs", ".csproj", ".props", ".targets", ".json"):
                files.add(path.resolve())
    for root in (repo, sharp):
        for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json", "NuGet.Config", "nuget.config", "CopperOS.Portable.props"):
            path = root / name
            if path.is_file():
                files.add(path.resolve())
    files.update((Path(__file__).resolve(), Path(__file__).with_name("append_hunk_version.py").resolve()))
    return [bound(path) for path in sorted(files, key=lambda p: str(p).casefold())]


def support_from_restore(assets: Path) -> Path:
    data = read_json(assets)
    libraries = [(name, item) for name, item in data["libraries"].items()
                 if name.split("/")[0].casefold() == "coppersharp.sdk.amiga.support"]
    if len(libraries) != 1:
        raise ValueError("Restore must resolve exactly one Amiga support package")
    name, library = libraries[0]
    selected = set()
    for target in data["targets"].values():
        for file in target.get(name, {}).get("runtime", {}):
            if Path(file).name == "CopperSharp.Sdk.Amiga.Support.dll":
                for folder in data["packageFolders"]:
                    path = Path(folder) / library["path"] / file
                    if path.is_file():
                        selected.add(path.resolve())
    if len(selected) != 1:
        raise ValueError("Resolved support runtime assembly is missing or ambiguous")
    return selected.pop()


def build_pass(dotnet: Path, repo: Path, sharp: Path, output: Path) -> dict:
    output.mkdir()
    build = output / "build"
    common = [dotnet, "build", "-c", "Release", "--nologo", "--no-incremental", "--artifacts-path", build]
    run(common + [sharp / "Compiler.Cli/CopperSharp.Compiler.Cli.csproj"], repo, output / "compiler-build.log")
    run(common + [repo / "tests/Commands.AddBuffersNativeRoot/CopperOS.Commands.AddBuffersNativeRoot.csproj",
                  f"-p:CopperSharp68kRoot={sharp}"], repo, output / "root-build.log")
    compiler_dir = build / "bin/CopperSharp.Compiler.Cli/release"
    root_dir = build / "bin/CopperOS.Commands.AddBuffersNativeRoot/release"
    compiler = compiler_dir / "CopperSharp.Compiler.Cli.dll"
    root = root_dir / "CopperOS.Commands.AddBuffersNativeRoot.dll"
    sdk = root_dir / "CopperSharp.Sdk.Amiga.dll"
    assets = build / "obj/CopperOS.Commands.AddBuffersNativeRoot/project.assets.json"
    support = support_from_restore(assets)
    closure = sorted({file.resolve() for folder in (compiler_dir, root_dir) for file in folder.iterdir()
                      if file.is_file() and file.suffix in (".dll", ".json")} | {support}, key=str)
    closure_before = [bound(path) for path in closure]
    commands, records = [], []
    for cpu in CPUS:
        raw, versioned = output / f"makelink-{cpu}.raw.hunk", output / f"makelink-{cpu}.hunk"
        static = output / f"makelink-{cpu}.compatibility.json"
        command = [dotnet, compiler, root, *OPTIONS, "--cpu", cpu,
                   "--managed-assembly", sdk, "--managed-assembly", support,
                   "--managed-assembly", root_dir / "CopperSharp.Compiler.dll",
                   "--output", raw, "--compatibility-report", static]
        run(command, repo, output / f"makelink-{cpu}.compile.log")
        commands.append([str(item) for item in command])
        report = read_json(static)
        native = report["NativeCompatibility"]
        zeroes = ("RuntimeFeatureCount", "RuntimeHelperCount", "ExternalNativeTargetCount", "ExceptionRegionCount", "FatalMachineFaultSiteCount")
        if (report.get("IsCompatible") is not True or report.get("Cpu") != "m" + cpu or
                report.get("RuntimeProfile") != "resident" or report.get("ReachableMethodCount") != 11 or
                any(native.get(field) != 0 for field in zeroes)):
            raise ValueError(f"Static native gate failed for {cpu}")
        for assembly in native["ReachableAssemblies"]:
            actual = root_dir / (assembly["Name"] + ".dll")
            if bound(actual)["sha256"] != assembly["Sha256"]:
                raise ValueError(f"Static assembly identity changed: {actual}")
        tag_receipt = output / f"makelink-{cpu}.version.json"
        transform = append_file(raw, versioned, tag_receipt, VERSION, bound(raw)["sha256"])
        records.append({"command": "makelink", "profile": "wb31", "cpu": cpu,
                        "status": "development-only", "shipping": False, "pure_admission": False,
                        "installed_path": None, "amiga_protection": None,
                        "path": str(versioned.resolve()), "sha256": transform["output_sha256"],
                        "bytes": transform["output_bytes"], "raw": bound(raw), "version_id": VERSION,
                        "output_format": "hunk", "entry": ENTRY, "fpu": "disabled",
                        "runtime_profile": "resident", "declared_stack_bytes": 4096,
                        "minimum_stack_qualified": False, "source": bound(repo / "src/Commands/Native/Workbench31MakeLinkCommand.cs"),
                        "compiler": bound(compiler), "sdk": bound(sdk),
                        "native_static": bound(static), "version_append": bound(tag_receipt)})
    if closure_before != [bound(path) for path in closure]:
        raise ValueError("Native compiler/managed input files changed during generation")
    manifest = output / "native-build-inputs.json"
    write_json(manifest, {"schema_version": 1, "scope": "Native compiler executable directory, root managed assemblies, restored support assembly and exact CLI arguments; not a hermetic host/runtime closure.",
                          "inputs": closure_before, "restored_packages": bound(assets), "commands": commands})
    for record in records:
        record["input_manifest"] = bound(manifest)
    return {"records": records, "build_logs": [bound(output / name) for name in ("compiler-build.log", "root-build.log")],
            "input_manifest": bound(manifest)}


def execute(dotnet: Path, runner: Path, record: dict, repo: Path, directory: Path) -> dict:
    cpu, hunk = record["cpu"], Path(record["path"])
    report_path = directory / f"makelink-{cpu}.runtime.json"
    identities = [bound(path) for path in (runner, runner.with_name("Copper68k.dll"))]
    run([dotnet, runner, hunk, cpu, report_path, SUITE], repo, directory / f"makelink-{cpu}.runtime.log")
    report = read_json(report_path)
    if (report.get("status") != "passed" or report.get("suite") != SUITE or report.get("cpu") != cpu or
            report.get("imageSha256") != record["sha256"] or report.get("imageBytes") != record["bytes"] or
            report.get("passed") != 20 or len(report.get("cases", [])) != 20 or report.get("sharedImageWrites") != 0 or
            report.get("shippingOrPureApproval") is not False or report.get("minimumStackQualified") is not False or
            bound(hunk)["sha256"] != record["sha256"]):
        raise ValueError(f"Mismatched or failed native execution for {cpu}")
    for prefix, expected in zip(("managedExecutor", "instructionCore"), identities):
        if Path(report[prefix + "Path"]).resolve() != Path(expected["path"]) or report[prefix + "Sha256"] != expected["sha256"] or bound(Path(expected["path"])) != expected:
            raise ValueError(f"Runtime identity changed for {cpu}/{prefix}")
    return {"report": bound(report_path), "invocations": 20, "suite": SUITE,
            "observed_configured_stack_bytes": sorted({case["configuredStackBytes"] for case in report["cases"]}),
            "maximum_observed_stack_bytes_written": max(case["stackBytesWritten"] for case in report["cases"]),
            "scope": "Existing supplied-vector suite, including interleaving; not real DOS, declared-stack qualification or full parity."}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--coppersharp-root", type=Path, default=Path(__file__).resolve().parents[3] / "CopperSharp68k")
    parser.add_argument("--runner", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[2]
    dotnet, sharp, runner = [path.resolve(strict=True) for path in (args.dotnet, args.coppersharp_root, args.runner)]
    output = args.output.resolve()
    if output.exists():
        parser.error("Use a fresh output directory; historical evidence must remain unchanged")
    output.mkdir(parents=True)
    try:
        sources = source_snapshot(repo, sharp)
        source_manifest = output / "source-snapshot.json"
        write_json(source_manifest, {"schema_version": 1, "inputs": sources,
                                   "scope": "Observed relevant source/project/configuration files; not a complete hermetic MSBuild input closure."})
        run([dotnet, "--info"], repo, output / "dotnet-info.log")
        passes = []
        for name in ("pass-a", "pass-b"):
            print(f"Building {name} in fresh managed/native output directories", flush=True)
            passes.append(build_pass(dotnet, repo, sharp, output / name))
            if source_snapshot(repo, sharp) != sources:
                raise ValueError("Observed source/configuration snapshot changed during builds")
        for left, right in zip(passes[0]["records"], passes[1]["records"]):
            if left["raw"]["sha256"] != right["raw"]["sha256"] or left["sha256"] != right["sha256"]:
                raise ValueError(f"Raw/versioned HUNK reproducibility failed for {left['cpu']}")
            print(f"Executing versioned {left['cpu']} MakeLink", flush=True)
            left["native_execution"] = execute(dotnet, runner, left, repo, output)
            left["source_snapshot"] = bound(source_manifest)
            left["dependencies"] = [
                {"id": "exec.library", "minimum_version": 36, "abi": bound(sharp / "Sdk.Amiga/Exec/Exec.cs")},
                {"id": "dos.library", "minimum_version": 36, "abi": bound(sharp / "Sdk.Amiga/DOS/DOS.cs")},
            ]
            left["dependency_policy"] = "Exec36 is the declared 2.0+ platform floor, obtained at absolute4 without an OpenLibrary version check; DOS36 is explicitly required by the entry. Kickstart3.1 is the compatibility target. No version below these floors is claimed. ABI layout/LVO sources are in source-snapshot.json."
        write_json(output / "qualification.json", {
            "schema_version": 1, "status": "passed", "command": "makelink", "profile": "wb31",
            "version_id": VERSION, "shipping": False, "pure_admission": False,
            "declared_stack_bytes": 4096, "minimum_stack_qualified": False,
            "reproducible_raw_hunks": True, "reproducible_versioned_hunks": True,
            "reproducibility_scope": "Two fresh managed/native build directories on the same host; no cross-host hermetic reproducibility claim.",
            "runtime_invocations": 60, "runtime_scope": "Existing 20-case Workbench MakeLink supplied-vector suite on each CPU; no original-OS result transfer from older hashes.",
            "source_snapshot": bound(source_manifest), "build_passes": passes,
            "environment": {"python": platform.python_version(), "platform": platform.platform(),
                            "dotnet": bound(dotnet), "dotnet_info": bound(output / "dotnet-info.log")},
            "records": passes[0]["records"],
        })
        print(f"Passed: three reproducible development HUNKs and 60 native invocations. Evidence: {output / 'qualification.json'}")
    except (ValueError, OSError, KeyError) as error:
        write_json(output / "failure.json", {"status": "failed", "error": str(error), "shipping": False, "pure_admission": False})
        parser.exit(1, f"Versioned build failed: {error}\n")


if __name__ == "__main__":
    main()
