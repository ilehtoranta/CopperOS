"""Freeze corrected Search once; compile both profiles with the existing frozen compiler."""
import json
import pathlib
import shutil
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from measure_c import identity, parse_map
from verify_correctness_checkpoint import freeze_response
from qualify_sizes import run

repo = pathlib.Path(__file__).resolve().parents[3]
stage = pathlib.Path(sys.argv[1]).resolve()
previous = repo / "artifacts/search-os-qualification-20261012"
pressure = repo / "artifacts/search-pressure-20261011"
frozen = freeze_response(repo / "src/Commands/Search/obj/Release/net10.0/amiga-m68k/coppersharp.publish.rsp",
                         repo / "src/Commands/Search", stage / "inputs/Search")
manifest = json.loads((pressure / "final/compilation.json").read_text())
for row in manifest["compilerFiles"]:
    assert identity(row["path"]) == row
shutil.copytree(pressure / "final/compiler", stage / "compiler")
shutil.copytree(previous / "runner", stage / "runner")
shutil.copytree(previous / "guest", stage / "guest")
shutil.copytree(previous / "host", stage / "host")
shutil.copy2(previous / "runner-source-binding.json", stage / "runner-source-binding.json")
for filename in ("TEXT.IN", "LONG.IN"):
    shutil.copy2(previous / "inputs" / filename, stage / "inputs" / filename)
(stage / "morphos-inputs").mkdir()
for path in sorted((stage / "morphos-cil").glob("*.dll")):
    shutil.copy2(path, stage / "morphos-inputs" / path.name)

sources = []
for pattern in ("src/Commands/Search/**/*.cs", "src/Commands/Search/*.csproj", "src/Commands/Common/Native*.cs",
                "src/Commands/CodeSizePolicies.props", "src/Commands/Directory.Build.*",
                "tests/Commands.NativeExecution/SearchEntrySuite.cs", "tests/Commands.NativeExecution/Program.cs"):
    for path in sorted(repo.glob(pattern)):
        if any(part in ("bin", "obj") for part in path.relative_to(repo).parts):
            continue
        destination = stage / "source" / path.relative_to(repo)
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, destination)
        sources.append(dict(original=identity(path), snapshot=identity(destination)))
frozen += [identity(p) for directory in ("compiler", "runner", "guest", "host", "vector-runner", "morphos-inputs")
           for p in sorted((stage / directory).rglob("*")) if p.is_file()]
frozen += [identity(stage / "inputs" / name) for name in ("TEXT.IN", "LONG.IN")]
(stage / "freeze.json").write_text(json.dumps(dict(files=frozen, sources=sources), indent=2) + "\n")

lines = (stage / "inputs/Search/compile.rsp").read_text().splitlines()
assert all("SimplifyLoopPhis" not in line for line in lines)
compilations = []
for profile in ("workbench", "morphos"):
    for trial in ("baseline", "candidate"):
        target = stage / (trial if profile == "workbench" else "morphos-" + trial)
        (target / "C").mkdir(parents=True)
        flags = list(lines)
        if trial == "candidate":
            flags = [line + ",SimplifyLoopPhis" if line.startswith("code-size-passes=") else line for line in flags]
        if profile == "morphos":
            flags = [line for line in flags if not line.startswith(("input=", "entry=", "managed-assembly="))]
            flags += ["input=" + str(stage / "morphos-inputs/CopperOS.Commands.AddBuffersNativeRoot.dll"),
                      "entry=CopperOS.Commands.AddBuffersNativeRoot.NativeMorphOSSearchEntry::Main"]
            flags += ["managed-assembly=" + str(path) for path in sorted((stage / "morphos-inputs").glob("*.dll"))
                      if path.name != "CopperOS.Commands.AddBuffersNativeRoot.dll"]
        image = target / "C/Search"
        flags += ["output=" + str(image)]
        response = target / "compile.rsp"
        response.write_text("\n".join(flags) + "\n")
        assert run(["dotnet", str(stage / "compiler/CopperSharp.Compiler.Cli.dll"), "@" + str(response)], target / "compile.log") == 0
        row = dict(profile=profile, trial=trial, image=identity(image), response=identity(response),
                   map=identity(pathlib.Path(str(image) + ".map")), **parse_map(pathlib.Path(str(image) + ".map")))
        (target / "compilation.json").write_text(json.dumps(row, indent=2) + "\n")
        compilations.append(row)
        print(profile, trial, row["image"]["bytes"], row["image"]["sha256"], flush=True)
        if profile == "workbench":
            shutil.copy2(image, stage / "inputs" / ("Search." + trial))
assert (stage / "baseline/C/Search").read_bytes() == (stage / "publish-baseline/C/Search").read_bytes()
for row in frozen:
    assert identity(row["path"]) == row
(stage / "compilations.json").write_text(json.dumps(compilations, indent=2) + "\n")
