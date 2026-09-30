/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Security.Cryptography;
using System.Text.Json;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;

namespace CopperOS.MuiMaster.LibraryBuilder;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 2) throw new ArgumentException("usage: LibraryBuilder [68000|68020|68040] [output-directory]");
            var cpuName = args.Length == 0 ? "68000" : args[0];
            var cpu = cpuName switch
            {
                "68000" => M68kCpuTarget.M68000,
                "68020" => M68kCpuTarget.M68020,
                "68040" => M68kCpuTarget.M68040,
                _ => throw new ArgumentException("CPU must be 68000, 68020, or 68040."),
            };
            var directory = Path.GetFullPath(args.Length == 2 ? args[1] :
                Path.Combine(AppContext.BaseDirectory, "artifacts", cpuName));
            var assembly = typeof(MuiNativeLibraryEntrypoints).Assembly.Location;
            var dependencies = Directory.GetFiles(AppContext.BaseDirectory, "CopperSharp.*.dll")
                .Order(StringComparer.Ordinal).ToArray();
            var inputHashes = dependencies.Append(assembly).Append(typeof(MuiLibraryPackager).Assembly.Location)
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
                .ToDictionary(path => path, HashFile, StringComparer.Ordinal);
            Console.WriteLine($"Compiling production MUI management exports for {cpu}...");
            var result = AmigaM68kCompiler.Compile(new M68kCompilationRequest
            {
                AssemblyPath = assembly,
                EntryPoint = "CopperOS.MuiMaster.MuiNativeLibraryEntrypoints::LibraryImageEntry",
                IncludedExportNames = MuiLibraryPackager.ExportNames,
                ManagedAssemblyPaths = dependencies,
                Cpu = cpu,
                FloatingPoint = M68kFloatingPointMode.Disabled,
                ClrPolicy = M68kClrPolicy.Always,
                ExceptionMode = M68kExceptionMode.Yolo,
                OutputFormat = M68kOutputFormat.Hunk,
                RuntimeProfile = M68kRuntimeProfile.Freestanding,
                MemoryManagement = M68kMemoryManagement.None,
                Hunk = new HunkOutputOptions { IncludeSymbols = false },
            });
            Validate(result);
            var artifact = MuiLibraryPackager.Build(result.Code, result.Relocations, result.Symbols);
            foreach (var input in inputHashes)
                if (HashFile(input.Key) != input.Value)
                    throw new InvalidDataException($"Build input changed during compilation: {input.Key}");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, MuiResidentMetadata.DevelopmentNameText);
            File.WriteAllBytes(path, artifact.Image);
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path + ".framework.json", JsonSerializer.Serialize(result.FrameworkAnalysis, jsonOptions) + "\n");
            File.WriteAllText(path + ".native.json", JsonSerializer.Serialize(result.NativeCompatibility, jsonOptions) + "\n");
            File.WriteAllText(path + ".json", JsonSerializer.Serialize(new
            {
                Format = "single-code-hunk-development-library",
                Cpu = cpuName,
                Name = MuiResidentMetadata.DevelopmentNameText,
                Version = MuiResidentMetadata.DevelopmentVersion,
                Revision = MuiResidentMetadata.DevelopmentRevision,
                PositiveBytes = MuiNativeLibraryEntrypoints.PositiveBytes,
                NegativeBytes = MuiNativeLibraryEntrypoints.NegativeBytes,
                ImageBytes = artifact.Image.Length,
                CodeBytes = artifact.Code.Length,
                Sha256 = Convert.ToHexString(SHA256.HashData(artifact.Image)),
                artifact.Layout,
                artifact.Exports,
                RelocationCount = artifact.Relocations.Count,
                InputSha256 = inputHashes.ToDictionary(input => Path.GetFileName(input.Key)!, input => input.Value),
            }, jsonOptions) + "\n");
            Console.WriteLine($"Wrote {path} ({artifact.Image.Length} bytes, {artifact.Relocations.Count} relocations).");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Validate(M68kCompilationResult result)
    {
        if (result.Code.Length == 0 || !result.FrameworkAnalysis.IsCompatible ||
            result.FrameworkFeatures.Count != 0 || result.FrameworkAnalysis.Members.Count != 0 ||
            result.FrameworkAnalysis.ManagedAllocationSites.Count != 0 ||
            result.NativeCompatibility.ExceptionRegionCount != 0 ||
            result.NativeCompatibility.RuntimeFeatures.Count != 0 ||
            result.NativeCompatibility.RuntimeHelpers.Count != 0 ||
            result.NativeCompatibility.ExternalNativeTargets.Count != 0 ||
            result.Relocations.Any(relocation => relocation.Target.StartsWith("runtime:type-descriptor:", StringComparison.Ordinal)) ||
            result.NativeCompatibility.ReachableAssemblies.Any(assembly =>
                assembly.Name.StartsWith("CopperStart.", StringComparison.Ordinal)))
            throw new InvalidDataException("MUI library violates the freestanding/native-only compilation contract.");
    }
}
