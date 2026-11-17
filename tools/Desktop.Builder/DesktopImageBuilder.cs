using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;

namespace CopperOS.Desktop.Build;

public static class DesktopImageBuilder
{
    public sealed record Payload(byte[] Image, byte[] Command, uint Entry, string Map, string[] Inputs);
    public static Payload Compile()
    {
        string[] inputs = [typeof(NativeDesktopLauncher).Assembly.Location, typeof(APTR).Assembly.Location,
            typeof(M68kCompiler).Assembly.Location, typeof(AmigaM68kCompiler).Assembly.Location];
        var r = AmigaM68kCompiler.Compile(new M68kCompilationRequest
        {
            AssemblyPath = inputs[0], EntryPoint = "CopperOS.Desktop.NativeDesktopLauncher::Entry",
            Cpu = M68kCpuTarget.M68000, ClrPolicy = M68kClrPolicy.Always, ExceptionMode = M68kExceptionMode.Yolo,
            PeepholeOptimization = M68kPeepholeOptimizationMode.Bounded, OutputFormat = M68kOutputFormat.Hunk,
            Hunk = new HunkOutputOptions { IncludeSymbols = false }, RuntimeProfile = M68kRuntimeProfile.Freestanding,
            MemoryManagement = M68kMemoryManagement.None, ManagedAssemblyPaths = inputs.Skip(1).ToArray(),
            IncludedExportNames = ["copperos.desktop.process", "copperos.desktop.exited"]
        });
        if (!r.FrameworkAnalysis.IsCompatible || r.FrameworkAnalysis.ManagedAllocationSites.Count != 0 ||
            r.NativeCompatibility.RuntimeFeatures.Count != 0 || r.NativeCompatibility.RuntimeHelpers.Count != 0 ||
            r.NativeCompatibility.ExternalNativeTargets.Count != 0 || r.NativeCompatibility.FatalMachineFaultSiteCount != 0)
            throw new InvalidOperationException("Desktop must compile without managed allocation, runtime helpers or unresolved targets.");
        var identity = Convert.ToHexString(SHA256.HashData(r.Image));
        return new(r.Image, Bootstrap(r.EntryPoint, "CopperOS.Home." + identity[..16]), r.EntryPoint, r.Map, inputs);
    }

    public static void Write(string providerDirectory, string romDirectory, string outputDirectory)
    {
        var rom = File.ReadAllBytes(Path.Combine(romDirectory, "CopperStart-DiskBoot.rom"));
        using var romManifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(romDirectory, "disk-boot-test-manifest.json")));
        if (rom.Length != 524288 || Convert.ToHexString(SHA256.HashData(rom)) != DesktopProfile.RequiredRomSha256 ||
            romManifest.RootElement.GetProperty("sha256").GetString() != DesktopProfile.RequiredRomSha256)
            throw new InvalidOperationException("The exact desktop ROM with self-task reclamation, IRQ keyboard capture and shared-handle writes is required.");
        var provider = File.ReadAllBytes(Path.Combine(providerDirectory, "CopperIntuition.hunk"));
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(providerDirectory, "intuition.json")));
        if (Convert.ToHexString(SHA256.HashData(provider)) != DesktopProfile.ProviderSha256 ||
            manifest.RootElement.GetProperty("sha256").GetString() != DesktopProfile.ProviderSha256 ||
            manifest.RootElement.GetProperty("textEntry").GetUInt32() != DesktopProfile.TextEntry)
            throw new InvalidOperationException("The Desktop's exact qualified text-provider profile is required.");
        if (Directory.Exists(outputDirectory)) throw new IOException("Use a fresh output directory.");
        var payload = Compile();
        Directory.CreateDirectory(Path.Combine(outputDirectory, "C")); Directory.CreateDirectory(Path.Combine(outputDirectory, "L"));
        File.WriteAllBytes(Path.Combine(outputDirectory, "C", "Desktop"), payload.Command);
        File.WriteAllBytes(Path.Combine(outputDirectory, "L", "CopperHome"), payload.Image);
        File.WriteAllBytes(Path.Combine(outputDirectory, "L", "CopperGUI"), provider);
        File.WriteAllText(Path.Combine(outputDirectory, "Desktop.map"), payload.Map);
        File.WriteAllText(Path.Combine(outputDirectory, "desktop.json"), JsonSerializer.Serialize(new
        {
            protocol = "copperos-native-recovery-v1", shipping = false, romBytes = 524288, requiredRomSha256 = DesktopProfile.RequiredRomSha256,
            chipBytes = 524288, slowBytes = 524288, fastBytes = 0, callbackVeneerBytes = 32,
            stackBytes = DesktopProfile.StackBytes, stateBytes = DesktopProfile.StateBytes, cleanupReserve = DesktopProfile.CleanupReserveBytes,
            filesWorkspaceBytes = DesktopProfile.FilesWorkspaceBytes, filesNavigationPeakBytes = DesktopProfile.FilesWorkspaceBytes * 2,
            filesEntryCapacity = DesktopProfile.FilesEntryCapacity, filesPathBytes = DesktopProfile.FilesPathBytes,
            editorCapacity = DesktopProfile.EditorCapacity, editorDocumentBytes = DesktopProfile.EditorBytes,
            editorOpenOwnedPeakBytes = DesktopProfile.EditorBytes * 2 + DesktopProfile.EditorSnapshotBytes * 4 + DesktopProfile.EditorSaveBytes + 260,
            editorSaveOwnedPeakBytes = DesktopProfile.EditorBytes + DesktopProfile.EditorSnapshotBytes * 4 + DesktopProfile.EditorSaveBytes,
            editorSnapshotBytes = DesktopProfile.EditorSnapshotBytes, editorSaveWorkspaceBytes = DesktopProfile.EditorSaveBytes,
            recoveryWorkspaceBytes = DesktopProfile.RecoveryBytes, recoveryNavigationPeakBytes = DesktopProfile.RecoveryBytes * 2,
            editorFontBytes = DesktopProfile.EditorFontBytes,
            imageBytes = payload.Image.Length, sha256 = Convert.ToHexString(SHA256.HashData(payload.Image)), entry = payload.Entry,
            commandBytes = payload.Command.Length, commandSha256 = Convert.ToHexString(SHA256.HashData(payload.Command)),
            providerBytes = provider.Length, providerSha256 = DesktopProfile.ProviderSha256,
            lifetime = "Home and GUI code are DOS system residents until reset; one copy per identity",
            inputs = payload.Inputs.Select(path => new { file = Path.GetFileName(path), sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) })
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static byte[] Bootstrap(uint entry, string identity)
    {
        var a = new Code();
        void Exec(short lvo) { a.W(0x2C78, 4, 0x4EAE, unchecked((ushort)lvo)); }
        void Dos(short lvo) { a.W(0x2C4D, 0x4EAE, unchecked((ushort)lvo)); }
        void Resident(string prefix, string name, string path)
        {
            void Find() { a.Address(name, 1); a.W(0x2209, 0x7400, 0x76FF); Dos(DosLvo.FindSegment); }
            Exec(ExecLvo.Forbid); Find(); a.W(0x4A80); a.Branch(0x6700, prefix + "load");
            a.W(0x2040, 0x2828, 8); Exec(ExecLvo.Permit); a.Branch(0x6000, prefix + "done");
            a.Label(prefix + "load"); Exec(ExecLvo.Permit); a.Address(path, 1); a.W(0x2209); Dos(DosLvo.LoadSeg);
            a.W(0x2A00, 0x4A80); a.Branch(0x6700, "closefailed");
            Exec(ExecLvo.Forbid); Find(); a.W(0x4A80); a.Branch(0x6700, prefix + "register");
            a.W(0x2040, 0x2828, 8); Exec(ExecLvo.Permit); a.W(0x2205); Dos(DosLvo.UnLoadSeg); a.Branch(0x6000, prefix + "done");
            a.Label(prefix + "register"); a.Address(name, 1); a.W(0x2209, 0x2405, 0x76FF); Dos(DosLvo.AddSegment);
            a.W(0x2805, 0x4A80); a.Branch(0x6600, prefix + "added");
            Exec(ExecLvo.Permit); a.W(0x2205); Dos(DosLvo.UnLoadSeg); a.Branch(0x6000, "closefailed");
            a.Label(prefix + "added"); Exec(ExecLvo.Permit); a.Label(prefix + "done");
        }
        a.W(0x48E7, 0x3F3E); a.Address("dos", 1); a.W(0x7025); Exec(ExecLvo.OpenLibrary);
        a.W(0x4A80); a.Branch(0x6700, "failed"); a.W(0x2A40); // A5 DOS
        // Load the largest HUNK before the growing Desktop module. LoadSeg's
        // transient image plus relocated code must fit the same 1 MiB machine.
        Resident("gui", "guiName", "guiPath"); Resident("home", "name", "path");
        a.Label("invoke"); a.W(0x2004, 0xE580, 0x5880, 0x2640, 0xD7FC, (ushort)(entry >> 16), (ushort)entry, 0x2C78, 4);
        a.W(0x48E7, 0x3F3E, 0x4E93, 0x4CDF, 0x7CFC, 0x2E00); a.Branch(0x6000, "close");
        a.Label("closefailed"); a.W(0x7E14); Dos(DosLvo.Output); a.W(0x2200);
        a.Address("error", 0); a.W(0x2408, 0x762B); Dos(DosLvo.Write);
        a.Label("close"); a.W(0x224D); Exec(ExecLvo.CloseLibrary); a.W(0x2007, 0x4CDF, 0x7CFC, 0x4E75);
        a.Label("failed"); a.W(0x4CDF, 0x7CFC, 0x7014, 0x4E75);
        return a.Hunk(new() { ["dos"] = Encoding.ASCII.GetBytes("dos.library\0"), ["path"] = Encoding.ASCII.GetBytes("L:CopperHome\0"), ["name"] = Encoding.ASCII.GetBytes(identity + "\0"),
            ["guiPath"] = Encoding.ASCII.GetBytes("L:CopperGUI\0"), ["guiName"] = Encoding.ASCII.GetBytes("CopperOS.GUI.Text.v1\0"),
            ["error"] = Encoding.ASCII.GetBytes("Desktop: startup failed; console retained.\n") });
    }

    private sealed class Code
    {
        private readonly List<ushort> words = []; private readonly Dictionary<string, int> labels = [];
        private readonly List<(int index, string label)> branches = [], references = [];
        internal void W(params ushort[] data) => words.AddRange(data);
        internal void Label(string name) => labels.Add(name, words.Count);
        internal void Branch(ushort opcode, string name) { W(opcode, 0); branches.Add((words.Count - 1, name)); }
        internal void Address(string name, int register) { W((ushort)(0x41FA | register << 9), 0); references.Add((words.Count - 1, name)); }
        internal byte[] Hunk(Dictionary<string, byte[]> data)
        {
            foreach (var (i, label) in branches) words[i] = unchecked((ushort)checked((short)((labels[label] - i) * 2)));
            var code = new List<byte>(); foreach (var word in words) { code.Add((byte)(word >> 8)); code.Add((byte)word); }
            foreach (var (name, bytes) in data)
            {
                if ((code.Count & 1) != 0) code.Add(0);
                foreach (var (i, target) in references.Where(r => r.label == name))
                { var displacement = checked((short)(code.Count - i * 2)); code[i * 2] = (byte)(displacement >> 8); code[i * 2 + 1] = (byte)displacement; }
                code.AddRange(bytes);
            }
            while (code.Count % 4 != 0) code.Add(0);
            var hunk = new byte[36 + code.Count]; uint[] header = [0x3F3, 0, 1, 0, 0, (uint)code.Count / 4, 0x3E9, (uint)code.Count / 4];
            for (var i = 0; i < header.Length; i++) BinaryPrimitives.WriteUInt32BigEndian(hunk.AsSpan(i * 4), header[i]);
            code.CopyTo(hunk, 32); BinaryPrimitives.WriteUInt32BigEndian(hunk.AsSpan(32 + code.Count), 0x3F2); return hunk;
        }
    }
}
