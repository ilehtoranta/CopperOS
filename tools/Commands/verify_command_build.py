"""Validate the existing versioned MakeLink build producer for two release gates.

This adapter establishes native-static checks and reproducible bytes from two
fresh builds on one host. It does not establish behavior, runtime service
integration, stack, purity, residency, reference parity, or image placement.
Reports are local build evidence, not signed attestations. No build is executed
and no report's shipping/P booleans are used to admit a command.
"""
from __future__ import annotations

import json
from pathlib import Path

from append_hunk_version import append_version, parse_hunk


REPORT_TYPE = "copperos-versioned-makelink-build-v1"
GATES = ("native-static", "reproducible-build")
ENTRY = "CopperOS.Commands.AddBuffersNativeRoot.Workbench31MakeLinkEntry::Main"
CPUS = {"68000", "68020", "68040"}


def verify_versioned_build(report: dict, record: dict, root: Path) -> dict:
    # Import at the boundary so the release preflight can load this adapter
    # without creating a module initialization cycle.
    from verify_command_release import EvidenceError, bound_file, load_json, sha256

    verified_files = {}

    def require(condition, message):
        if not condition:
            raise EvidenceError("versioned-build: " + message)

    def file(reference, label):
        path = bound_file(reference, root, label)
        verified_files[str(path)] = reference["sha256"]
        return path

    def document(reference, label):
        return load_json(file(reference, label))

    def bindings(items, label):
        require(isinstance(items, list) and bool(items), label + " is empty")
        paths = [file(item, label) for item in items]
        require(len(set(paths)) == len(paths), label + " has duplicate paths")
        return dict(zip(paths, items))

    require(isinstance(report, dict) and type(report.get("schema_version")) is int and
            report["schema_version"] == 1 and report.get("status") == "passed",
            "unsupported or failed build report")
    require((record.get("command"), record.get("profile"), record.get("entry")) ==
            ("makelink", "wb31", ENTRY), "unsupported command/profile/entry")
    require(record.get("cpu") in CPUS, "unsupported CPU")
    require(report.get("command") == "makelink" and report.get("profile") == "wb31" and
            report.get("version_id") == record.get("version_id"), "report identity mismatch")
    require(report.get("reproducible_raw_hunks") is True and
            report.get("reproducible_versioned_hunks") is True, "reproducibility not observed")

    snapshot = document(report["source_snapshot"], "build source snapshot")
    require(snapshot.get("schema_version") == 1, "unknown source snapshot")
    sources = bindings(snapshot["inputs"], "source snapshot input")
    # Pin the known producer and transformer, rather than recognizing a report
    # merely because it has the expected status string and field names.
    for name in ("build_workbench_makelink_versioned.py", "append_hunk_version.py"):
        require((root / "tools/Commands" / name).resolve() in sources,
                "known producer/tool missing from current source snapshot")
    source = file(record["source"], "selected source")
    require(source == (root / "src/Commands/Native/Workbench31MakeLinkCommand.cs").resolve()
            and source in sources, "selected source not captured by producer")
    require((root / "tests/Commands.AddBuffersNativeRoot/Workbench31MakeLinkEntry.cs").resolve()
            in sources, "startup source not captured")
    from build_workbench_makelink_versioned import OPTIONS, source_snapshot, support_from_restore
    sharp = compiler_project(sources).parent.parent
    expected_sources = {Path(item["path"]).resolve(): item for item in source_snapshot(root, sharp)}
    require(sources == expected_sources, "source snapshot omits or changes known producer inputs")
    environment = report["environment"]
    dotnet = file(environment["dotnet"], "build dotnet")
    file(environment["dotnet_info"], "build runtime identity")

    passes = report.get("build_passes")
    require(isinstance(passes, list) and len(passes) == 2, "requires two build passes")
    outputs, build_directories, static_reports = [], [], []
    for index, build in enumerate(passes):
        records = build.get("records")
        require(isinstance(records, list) and len(records) == 3 and
                {item.get("cpu") for item in records} == CPUS,
                "each build must contain each target CPU exactly once")
        selected = next(item for item in records if item["cpu"] == record["cpu"])
        for field in ("command", "profile", "cpu", "entry", "version_id", "output_format", "fpu"):
            require(selected.get(field) == record.get(field), "build selection differs: " + field)
        require(selected.get("runtime_profile") == "resident", "native runtime must be resident")
        require(selected.get("source") == record.get("source"), "source identity differs")
        if index == 0:
            for field in ("compiler", "sdk", "input_manifest"):
                require(selected.get(field) == record.get(field), "selected " + field + " differs")
            for field in ("sha256", "bytes"):
                require(selected.get(field) == record.get(field), "selected artifact differs: " + field)
        native_path = file(build["input_manifest"], "native build inputs")
        pass_directory = native_path.parent
        require(native_path.name == "native-build-inputs.json", "unexpected native manifest path")
        require(selected["input_manifest"] == build["input_manifest"], "native input manifest differs")
        native = load_json(native_path)
        require(native.get("schema_version") == 1, "unknown native input schema")
        inputs = bindings(native["inputs"], "native input")
        assets = file(native["restored_packages"], "restored package assets")
        compiler = file(selected["compiler"], "compiler")
        sdk = file(selected["sdk"], "SDK")
        require(compiler in inputs and sdk in inputs, "compiler/SDK outside native inputs")
        root_assembly = sdk.with_name("CopperOS.Commands.AddBuffersNativeRoot.dll")
        require(root_assembly in inputs, "native managed root missing")
        build_directory = pass_directory / "build"
        compiler_directory = build_directory / "bin/CopperSharp.Compiler.Cli/release"
        root_directory = build_directory / "bin/CopperOS.Commands.AddBuffersNativeRoot/release"
        require(compiler == compiler_directory / "CopperSharp.Compiler.Cli.dll" and
                sdk == root_directory / "CopperSharp.Sdk.Amiga.dll" and
                assets == build_directory / "obj/CopperOS.Commands.AddBuffersNativeRoot/project.assets.json",
                "compiler/SDK/restore paths do not belong to this isolated build")
        support = support_from_restore(assets)
        closure = {path.resolve() for folder in (compiler_directory, root_directory)
                   for path in folder.iterdir() if path.is_file() and path.suffix in (".dll", ".json")}
        closure.add(support)
        require(set(inputs) == closure, "native input closure omits or adds producer inputs")
        build_directories.append(build_directory)

        logs = bindings(build["build_logs"], "managed build log")
        require(len(logs) == 2 and {path.name for path in logs} ==
                {"compiler-build.log", "root-build.log"}, "managed build logs missing")
        for log in logs:
            require(log.parent == pass_directory, "managed log belongs to another build")
            lines = log.read_text(encoding="utf-8-sig").splitlines()
            command = json.loads(lines[0])
            require("Build succeeded." in lines, "managed build did not succeed")
            expected_project = (root / "tests/Commands.AddBuffersNativeRoot/CopperOS.Commands.AddBuffersNativeRoot.csproj"
                                if log.name == "root-build.log" else
                                compiler_project(sources))
            expected_build = [str(dotnet), "build", "-c", "Release", "--nologo", "--no-incremental",
                              "--artifacts-path", str(build_directory), str(expected_project)]
            if log.name == "root-build.log":
                expected_build.append("-p:CopperSharp68kRoot=" + str(sharp))
            require(command == expected_build and expected_project.resolve() in sources,
                    "managed build arguments differ from known isolated producer")

        raw = file(selected["raw"], "raw HUNK")
        output = file({"path": selected["path"], "sha256": selected["sha256"]}, "versioned HUNK")
        require(output.stat().st_size == selected["bytes"], "versioned HUNK size differs")
        static_path = file(selected["native_static"], "compiler native report")
        prefix = "makelink-" + record["cpu"]
        require(raw == pass_directory / (prefix + ".raw.hunk") and
                output == pass_directory / (prefix + ".hunk") and
                static_path == pass_directory / (prefix + ".compatibility.json"),
                "native output/report belongs to another build")
        static = load_json(static_path)
        require(type(static.get("SchemaVersion")) is int and static["SchemaVersion"] == 2 and
                static.get("IsCompatible") is True and static.get("Cpu") == "m" + record["cpu"] and
                static.get("RuntimeProfile") == "resident" and static.get("OutputFormat") == "hunk" and
                static.get("RootMethodCount") == 1 and static.get("ReachableMethodCount") == 11,
                "compiler static identity/reachability mismatch")
        require(static.get("ManagedAllocationSites") == [] and static.get("Members") == [],
                "managed allocation or unsupported framework members")
        native_static = static["NativeCompatibility"]
        require(native_static.get("ExceptionMode") == "yolo" and
                native_static.get("MemoryManagement") == "none", "managed runtime mode")
        for count, values in (("RuntimeFeatureCount", "RuntimeFeatures"),
                              ("RuntimeHelperCount", "RuntimeHelpers"),
                              ("ExternalNativeTargetCount", "ExternalNativeTargets")):
            require(type(native_static.get(count)) is int and native_static[count] == 0 and
                    native_static.get(values) == [], "forbidden native dependency: " + values)
        for count in ("ExceptionRegionCount", "FatalMachineFaultSiteCount"):
            require(type(native_static.get(count)) is int and native_static[count] == 0,
                    "forbidden exception/fault site")
        assemblies = native_static["ReachableAssemblies"]
        require(native_static.get("ReachableAssemblyCount") == 2 and len(assemblies) == 2 and
                {item.get("Name") for item in assemblies} ==
                {"CopperOS.Commands.AddBuffersNativeRoot", "CopperSharp.Sdk.Amiga"},
                "reachable dependency closure contains unexpected assemblies")
        for assembly in assemblies:
            path = sdk.with_name(assembly["Name"] + ".dll")
            require(path in inputs and inputs[path]["sha256"] == assembly["Sha256"],
                    "reachable assembly identity differs")

        commands = native.get("commands")
        require(isinstance(commands, list) and len(commands) == 3, "native build commands missing")
        selected_commands = [cmd for cmd in commands if "--cpu" in cmd and
                             cmd[cmd.index("--cpu") + 1] == record["cpu"]]
        require(len(selected_commands) == 1, "ambiguous native CPU command")
        command = selected_commands[0]
        # The known producer supplies exactly these options, in this order.
        require(support in inputs, "restored support not a captured native input")
        expected_command = [str(dotnet), str(compiler), str(root_assembly), *OPTIONS,
                            "--cpu", record["cpu"], "--managed-assembly", str(sdk),
                            "--managed-assembly", str(support), "--managed-assembly",
                            str(sdk.with_name("CopperSharp.Compiler.dll")), "--output", str(raw),
                            "--compatibility-report", str(static_path)]
        require(command == expected_command, "native generation arguments differ")
        require(sdk.with_name("CopperSharp.Compiler.dll") in inputs, "compiler intrinsic assembly missing")
        compile_log = native_path.parent / ("makelink-" + record["cpu"] + ".compile.log")
        lines = compile_log.read_text(encoding="utf-8-sig").splitlines()
        require(json.loads(lines[0]) == command and
                f"Wrote {raw.stat().st_size} bytes for M{record['cpu']} to '{raw}' (entry $00000000)." in lines,
                "native compiler output/log mismatch")
        verified_files[str(compile_log)] = sha256(compile_log)

        receipt_path = file(selected["version_append"], "version transform receipt")
        require(receipt_path == pass_directory / (prefix + ".version.json"),
                "version transform receipt belongs to another build")
        receipt = load_json(receipt_path)
        transformed, expected_receipt = append_version(raw.read_bytes(), record["version_id"])
        require(output.read_bytes() == transformed, "versioned bytes are not the exact supported transform")
        require(all(receipt.get(key) == value for key, value in expected_receipt.items()),
                "version transform receipt differs from recomputed structure")
        require(file(receipt["tool"], "version transformer") ==
                (root / "tools/Commands/append_hunk_version.py").resolve(), "unknown version transformer")
        require(Path(receipt["input"]).resolve() == raw and Path(receipt["output"]).resolve() == output,
                "version transform paths differ")
        require(parse_hunk(raw.read_bytes()).symbols[0][0] == ENTRY,
                "native HUNK entry symbol differs")
        outputs.append((raw.read_bytes(), transformed))
        static_reports.append({"path": str(static_path), "sha256": selected["native_static"]["sha256"]})

    require(build_directories[0] != build_directories[1] and
            not build_directories[0].is_relative_to(build_directories[1]) and
            not build_directories[1].is_relative_to(build_directories[0]),
            "build directories are not independent")
    require(outputs[0] == outputs[1], "fresh builds produced different raw/versioned bytes")
    require(file({"path": record["path"], "sha256": record["sha256"]}, "selected artifact").read_bytes()
            == outputs[0][1], "selected artifact differs from reproduced bytes")
    return {"report_type": REPORT_TYPE, "cpu": record["cpu"],
            "satisfies_release_gates": list(GATES), "static_reports": static_reports,
            "build_directories": [str(path) for path in build_directories],
            "verified_file_count": len(verified_files),
            "source_snapshot": report["source_snapshot"],
            "scope": "Compiler native-static dependency/reachability checks plus exact raw/versioned byte equality in two fresh builds on one host; not hermetic/cross-host reproducibility, runtime behavior, minimum stack, purity, residency or shipping admission."}


def compiler_project(sources: dict) -> Path:
    matches = [path for path in sources if path.name == "CopperSharp.Compiler.Cli.csproj"
               and path.parent.name == "Compiler.Cli"]
    if len(matches) != 1:
        raise ValueError("Compiler project is missing or ambiguous in source snapshot")
    return matches[0]
