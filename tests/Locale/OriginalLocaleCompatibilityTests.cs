/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using Copper68k;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;
using CopperOS.Locale.LibraryBuilder;
namespace CopperOS.Locale.Tests;

public class OriginalLocaleCompatibilityTests
{
    [OriginalLocaleFact]
    public void CompareClassicBinary()
    {
        string root = OriginalLocaleFactAttribute.Root, reference = OriginalLocaleFactAttribute.Reference;
        Assert.True(File.Exists(reference), "Set COPPEROS_ORIGINAL_LOCALE to a locally owned classic locale.library.");
        byte[] referenceBytes = File.ReadAllBytes(reference);
        using var original = new LocaleGuest(referenceBytes);
        string iffPath = OriginalLocaleFactAttribute.Iff;
        Assert.True(File.Exists(iffPath), "Original catalog qualification needs iffparse.library alongside locale.library or COPPEROS_ORIGINAL_IFFPARSE.");
        using (var iff = new LocaleGuest(File.ReadAllBytes(iffPath), original.Bus, 0x80000, NativeBus.Iff))
        { iff.Call(iff.Init, s => { s.D[0] = NativeBus.Iff; s.A[0] = 0x888; s.A[6] = NativeBus.Exec; }); Assert.Equal(NativeBus.Iff, iff.Cpu.State.D[0]); }
        var expected = Observe(original);
        var build = AmigaM68kCompiler.Compile(new M68kCompilationRequest
        {
            AssemblyPath = typeof(LocaleNativeLibrary).Assembly.Location, EntryPoint = "CopperOS.Locale.LocaleNativeLibrary::LibraryImageEntry",
            IncludedExportNames = LocaleLibraryPackager.ExportNames, ManagedAssemblyPaths = Directory.GetFiles(AppContext.BaseDirectory, "CopperSharp.*.dll"),
            Cpu = M68kCpuTarget.M68000, FloatingPoint = M68kFloatingPointMode.Disabled, ClrPolicy = M68kClrPolicy.Always,
            ExceptionMode = M68kExceptionMode.Yolo, OutputFormat = M68kOutputFormat.Hunk,
            RuntimeProfile = M68kRuntimeProfile.Resident, MemoryManagement = M68kMemoryManagement.None,
            PeepholeOptimization = M68kPeepholeOptimizationMode.Disabled,
        });
        byte[] candidateImage = LocaleLibraryPackager.Build(build.Code, build.Relocations, build.Symbols).Image;
        using var candidate = new LocaleGuest(candidateImage);
        var actual = Observe(candidate);
        var differences = expected.Where(e => actual[e.Key] != e.Value).Select(e => new { Case = e.Key, Original = e.Value, CopperOS = actual[e.Key] }).ToArray();
        Directory.CreateDirectory(Path.Combine(root, "artifacts/locale-tests"));
        File.WriteAllText(Path.Combine(root, "artifacts/locale-tests/original-comparison.json"), JsonSerializer.Serialize(new
        { ReferenceVersion = original.P.String(original.P.Read32(NativeBus.Library + 24)), ReferenceSha256 = Convert.ToHexString(SHA256.HashData(referenceBytes)), IffParseSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(iffPath))), CopperOSImageSha256 = Convert.ToHexString(SHA256.HashData(candidateImage)), Cpu = "68000", Cases = expected.Count, Differences = differences, OriginalLookup = original.LookupCosts, CopperOSLookup = candidate.LookupCosts, Original = expected, CopperOS = actual }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(differences.Length == 0, string.Join("\n", differences.Take(30).Select(d => $"{d.Case}: original [{d.Original}], CopperOS [{d.CopperOS}]")));
    }
    private static Dictionary<string, string> Observe(LocaleGuest g)
    {
        var rows = new Dictionary<string, string>(); var p = g.P;
        g.Bus.MaximumRead = int.MaxValue;
        g.Call(g.Init, s => { s.D[0] = NativeBus.Library; s.A[0] = 0x777; s.A[6] = NativeBus.Exec; });
        Assert.Equal(NativeBus.Library, g.Cpu.State.D[0]);
        g.Vector(6, s => s.D[0] = uint.MaxValue);
        rows["Open/AllBitsD0"] = (g.Cpu.State.D[0] == NativeBus.Library).ToString();
        g.Vector(12, _ => { });
        g.Vector(156, s => s.A[0] = 0); uint l = g.Cpu.State.D[0]; Assert.NotEqual(0u, l);
        g.Vector(168, s => s.A[0] = l);
        rows["PrefsUpdate/Language"] = g.Bus.Variables.GetValueOrDefault("Language", "");
        rows["PrefsUpdate/UtilityLanguage"] = p.Read8(NativeBus.Utility + 34).ToString();
        rows["Patches/Targets"] = string.Join(",", g.Bus.PatchTargets.Where(t => t.Library == NativeBus.Exec || t.Library == NativeBus.Utility || t.Library == NativeBus.Dos).Select(t => $"{t.Library:X}/{t.Offset}").Order());
        g.Vector(168, s => s.A[0] = 0); rows["PrefsUpdate/Null"] = (g.Cpu.State.D[0] == l).ToString();
        foreach (int id in new[] { 0, 103, -103, 205, -205, 999 })
        { g.Call(NativeBus.Dos - 978, s => { s.A[6] = NativeBus.Dos; s.D[0] = (uint)id; s.D[1] = (uint)id; }); rows["DosString/" + id] = p.String(g.Cpu.State.D[0]); }
        g.Call(NativeBus.Dos - 978, s => { s.A[6] = NativeBus.Dos; s.D[0] = 103; s.D[1] = 205; });
        rows["DosString/DistinctIndexAndError"] = p.String(g.Cpu.State.D[0]);
        uint tags = p.Allocate(24); p.Write32(tags, CatalogService.TagBase + 4); p.Write32(tags + 4, p.Text("finnish"));
        p.Write32(tags + 8, CatalogService.TagBase + 3); p.Write32(tags + 12, 7);
        g.Bus.Data.Files["RAM:catalog"] = Fixture.Catalog([(0u, "zero"), (7u, "seven"), (999u, "last"), (0xffffffffu, "unsigned")], includeTerminator: false);
        uint catalogName = p.Text("RAM:catalog");
        g.Vector(150, s => { s.A[0] = l; s.A[1] = catalogName; s.A[2] = tags; }); uint catalog = g.Cpu.State.D[0];
        Assert.True(catalog != 0, "OpenCatalog failed: " + g.Bus.Data.Error + " paths " + string.Join(",", g.Bus.Data.FileRequests));
        rows["Catalog/Open"] = (catalog != 0).ToString();
        uint fallback = p.Text("fallback");
        foreach (uint id in new uint[] { 0, 1, 7, 998, 999, 1000, uint.MaxValue })
        { g.Vector(72, s => { s.A[0] = catalog; s.D[0] = id; s.A[1] = fallback; }); rows["Catalog/" + id] = p.String(g.Cpu.State.D[0]); }
        uint builtInCodeSetTags = p.Allocate(32);
        p.Write32(builtInCodeSetTags, CatalogService.TagBase + 4); p.Write32(builtInCodeSetTags + 4, p.Read32(tags + 4));
        p.Write32(builtInCodeSetTags + 8, CatalogService.TagBase + 3); p.Write32(builtInCodeSetTags + 12, 7);
        p.Write32(builtInCodeSetTags + 16, CatalogService.TagBase + 2);
        foreach (uint builtInCodeSet in new uint[] { 1, 2 })
        {
            p.Write32(builtInCodeSetTags + 20, builtInCodeSet);
            g.Vector(150, s => { s.A[0] = l; s.A[1] = catalogName; s.A[2] = builtInCodeSetTags; });
            uint taggedCatalog = g.Cpu.State.D[0]; Assert.NotEqual(0u, taggedCatalog);
            rows[$"Catalog/BuiltInCodeSet/{builtInCodeSet}/Header"] = p.Read32(taggedCatalog + 20).ToString();
            g.Vector(72, s => { s.A[0] = taggedCatalog; s.D[0] = 7; s.A[1] = fallback; });
            rows[$"Catalog/BuiltInCodeSet/{builtInCodeSet}/String"] = p.String(g.Cpu.State.D[0]);
            g.Vector(36, s => s.A[0] = taggedCatalog);
        }
        foreach (bool sparse in new[] { false, true })
        {
            string name = sparse ? "RAM:sparse" : "RAM:dense"; uint stride = sparse ? 100003u : 1u;
            g.Bus.Data.Files[name] = Fixture.Catalog(Enumerable.Range(0, 1024).Select(i => ((uint)i * stride, "string " + i)), includeTerminator: false);
            uint path = p.Text(name); g.Vector(150, s => { s.A[0] = l; s.A[1] = path; s.A[2] = tags; }); uint c = g.Cpu.State.D[0]; Assert.NotEqual(0u, c);
            foreach (uint slot in new uint[] { 0, 512, 1023, 1024 })
            {
                uint id = slot * stride; g.Vector(72, s => { s.A[0] = c; s.D[0] = id; s.A[1] = fallback; });
                string key = $"Catalog/{(sparse ? "Sparse" : "Dense")}/{slot}"; rows[key] = p.String(g.Cpu.State.D[0]);
                g.LookupCosts.Add(new { Case = key, Instructions = g.LastInstructions, Cycles = g.LastCycles });
            }
        }
        for (uint id = 1; id <= 50; id++) { g.Vector(78, s => { s.A[0] = l; s.D[0] = id; }); rows["String/" + id] = p.String(g.Cpu.State.D[0]); }
        for (uint c = 0; c < 256; c++)
        {
            for (uint vector = 48; vector <= 54; vector += 6) { g.Vector(vector, s => { s.A[0] = l; s.D[0] = c; }); rows[$"Character/{vector}/{c}"] = g.Cpu.State.D[0].ToString(); }
            for (uint vector = 84; vector <= 144; vector += 6) { g.Vector(vector, s => { s.A[0] = l; s.D[0] = c; }); rows[$"Character/{vector}/{c}"] = (g.Cpu.State.D[0] != 0).ToString(); }
            foreach (uint vector in new uint[] { 174, 180 }) { g.Call(NativeBus.Utility - vector, s => { s.A[6] = NativeBus.Utility; s.D[0] = c; }); rows[$"Utility/{vector}/{c}"] = g.Cpu.State.D[0].ToString(); }
        }
        foreach (string a in new[] { "", "a", "ABC", "ab", "\u00e9", "\u00e5", "\u00df", "\u00d6", "\u00e6", "\u00f8" })
        foreach (string b in new[] { "", "A", "abc", "ac", "e", "z", "s", "ss", "\u00c9", "\u00e9" })
        for (uint type = 0; type < 3; type++)
        {
            uint ap = p.Text(a), bp = p.Text(b);
            g.Vector(180, s => { s.A[0] = l; s.A[1] = ap; s.A[2] = bp; s.D[0] = uint.MaxValue; s.D[1] = type; });
            rows[$"Compare/{type}/{a}/{b}"] = Math.Sign((int)g.Cpu.State.D[0]).ToString();
            if (type == 0)
            {
                foreach (uint vector in new uint[] { 162, 168 })
                { g.Call(NativeBus.Utility - vector, s => { s.A[6] = NativeBus.Utility; s.A[0] = ap; s.A[1] = bp; s.D[0] = vector == 168 ? 2u : uint.MaxValue; }); rows[$"Utility/{vector}/{a}/{b}"] = Math.Sign((int)g.Cpu.State.D[0]).ToString(); }
            }
        }
        uint dt = p.Allocate(26), day = p.Allocate(128), date = p.Allocate(128), time = p.Allocate(128);
        p.Write32(dt + 14, day); p.Write32(dt + 18, date); p.Write32(dt + 22, time);
        foreach (uint days in new uint[] { 0, 8094, 17000, 17001, 16999, 17002, 16993, 16992 })
        for (byte f = 0; f < 5; f++) for (byte flags = 0; flags < 2; flags++)
        {
            p.Write32(dt, days); p.Write32(dt + 4, 13 * 60 + 24); p.Write32(dt + 8, 1550); p.Write8(dt + 12, f); p.Write8(dt + 13, flags);
            g.Call(NativeBus.Dos - 744, s => { s.A[6] = NativeBus.Dos; s.D[1] = dt; });
            rows[$"DateToStr/{days}/{f}/{flags}"] = $"{g.Cpu.State.D[0] != 0}/{p.String(day)}/{p.String(date)}/{p.String(time)}";
        }
        foreach (string input in new[] { "Today", "tomorrow", "YESTERDAY", "Monday", "Sunday", "01-Jan-78", "29-Feb-00", "31-Feb-00", "2000-02-29", "00-Feb-29", "00-02-29", "02-29-00", "29-02-00", "2/29/00", "junk" })
        for (byte f = 0; f < 5; f++) for (byte flags = 0; flags < 3; flags += 2)
        {
            p.Write32(dt, 123); p.Write32(dt + 4, 456); p.Write32(dt + 8, 789); p.Write8(dt + 12, f); p.Write8(dt + 13, flags);
            p.Write32(dt + 18, p.Text(input)); p.Write32(dt + 22, 0);
            g.Call(NativeBus.Dos - 750, s => { s.A[6] = NativeBus.Dos; s.D[1] = dt; });
            rows[$"StrToDate/{input}/{f}/{flags}"] = $"{g.Cpu.State.D[0] != 0}/{p.Read32(dt)}/{p.Read32(dt + 4)}/{p.Read32(dt + 8)}";
        }
        foreach (string input in new[] { "13:24:31", "13:24", "23:59:59", "24:00:00", "1:24 PM", "12:24 AM", "12:24 PM", "1:24 am", "13:24junk", "13:24:60", "13:60:00", "junk" })
        for (byte f = 0; f < 5; f++)
        {
            p.Write32(dt, 123); p.Write32(dt + 4, 456); p.Write32(dt + 8, 789); p.Write8(dt + 12, f);
            p.Write32(dt + 18, 0); p.Write32(dt + 22, p.Text(input));
            g.Call(NativeBus.Dos - 750, s => { s.A[6] = NativeBus.Dos; s.D[1] = dt; });
            rows[$"StrToTime/{input}/{f}"] = $"{g.Cpu.State.D[0] != 0}/{p.Read32(dt)}/{p.Read32(dt + 4)}/{p.Read32(dt + 8)}";
        }
        uint callback = p.Allocate(4); p.Write32(callback, 0x16c04e75); uint buffer = p.Allocate(256), stream = p.Allocate(16);
        p.Write16(stream, 0xfffe); p.Write32(stream + 2, 1234567);
        foreach (string fmt in new[] { "%d %lD", "%04x %lu", "%2$lu %1$d", "%d %%", "%lD", "%lU", "%D %D", "%lD %d", "%lX", "%ld", "%d %lD %lu", "%d %lU", "%lD %lu", "%lu %lD", "%d %D", "%lD %lD" })
        {
            uint format = p.Text(fmt);
            g.Call(NativeBus.Exec - 522, s => { s.A[6] = NativeBus.Exec; s.A[0] = format; s.A[1] = stream; s.A[2] = callback; s.A[3] = buffer; });
            rows["RawDoFmt/" + fmt] = $"{p.String(buffer)}/{g.Cpu.State.D[0] - stream}";
        }
        return rows;
    }
}

internal sealed class OriginalLocaleFactAttribute : FactAttribute
{
    internal static string Root => Environment.GetEnvironmentVariable("COPPEROS_LOCALE_TEST_ROOT") ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    internal static string Reference => Environment.GetEnvironmentVariable("COPPEROS_ORIGINAL_LOCALE") ?? Path.Combine(Root, "../CopperStart/artifacts/workbench-startup/wb31-files/Libs/locale.library");
    internal static string Iff => Environment.GetEnvironmentVariable("COPPEROS_ORIGINAL_IFFPARSE") ?? Path.Combine(Path.GetDirectoryName(Reference)!, "iffparse.library");
    public OriginalLocaleFactAttribute()
    {
        if (!File.Exists(Reference) || !File.Exists(Iff))
            Skip = "Provide locally owned locale.library and iffparse.library using COPPEROS_ORIGINAL_LOCALE / COPPEROS_ORIGINAL_IFFPARSE.";
    }
}

// Load the original HUNK image without rewriting its code. Exec's documented
// AutoInit table and InitStruct stream create the base; OS dependencies alone
// are host gateways. All locale operations execute their actual 68000 code.
internal sealed class LocaleGuest : IDisposable
{
    internal readonly NativeBus Bus;
    internal TestPlatform P;
    internal IM68kCore Cpu;
    internal uint Init;
    internal int LastInstructions;
    internal long LastCycles;
    internal List<object> LookupCosts = new();
    private readonly uint library;
    internal LocaleGuest(byte[] file, NativeBus? bus = null, uint codeBase = NativeBus.CodeBase, uint library = NativeBus.Library)
    {
        Bus = bus ?? new NativeBus(); this.library = library;
        P = new TestPlatform(Bus.Data);
        int at = 0;
        uint Long() { uint v = BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(at, 4)); at += 4; return v; }
        Assert.Equal(1011u, Long()); Assert.Equal(0u, Long()); uint count = Long(), first = Long(), last = Long();
        var bases = new uint[count]; var sizes = new uint[count]; uint next = codeBase;
        for (uint i = first; i <= last; i++) { sizes[i] = (Long() & 0x3fffffff) * 4; bases[i] = next; next += (sizes[i] + 3) & ~3u; }
        uint hunk = first;
        while (at < file.Length)
        {
            uint type = Long() & 0x3fffffff;
            if (type == 1001 || type == 1002) { uint bytes = Long() * 4; file.AsSpan(at, (int)bytes).CopyTo(Bus.Data.Memory.AsSpan((int)bases[hunk])); at += (int)bytes; }
            else if (type == 1003) { Assert.Equal(sizes[hunk], Long() * 4); }
            else if (type == 1004)
            { uint n; while ((n = Long()) != 0) { uint target = Long(); while (n-- != 0) { uint site = bases[hunk] + Long(); P.Write32(site, P.Read32(site) + bases[target]); } } }
            else if (type == 1010) hunk++;
            else throw new InvalidDataException("Unsupported reference HUNK " + type);
        }
        uint resident = bases[0] + 4; Assert.Equal((ushort)0x4afc, P.Read16(resident));
        uint auto = P.Read32(resident + 22), table = P.Read32(auto + 4), initStruct = P.Read32(auto + 8);
        Init = P.Read32(auto + 12);
        uint vector = library - 6, cursor = table;
        if (P.Read16(cursor) == 0xffff)
        {
            cursor += 2;
            while (P.Read16(cursor) != 0xffff) { P.Write16(vector, 0x4ef9); P.Write32(vector + 2, table + (uint)(short)P.Read16(cursor)); vector -= 6; cursor += 2; }
        }
        else while (P.Read32(cursor) != uint.MaxValue) { P.Write16(vector, 0x4ef9); P.Write32(vector + 2, P.Read32(cursor)); vector -= 6; cursor += 4; }
        P.Write16(library + 16, (ushort)(library - vector - 6)); P.Write16(library + 18, (ushort)P.Read32(auto));
        if (initStruct != 0) Initialize(initStruct);
        Bus.Register(NativeBus.Exec, -198, s => P.Allocate(s.D[0]));
        Bus.Register(NativeBus.Exec, -624, s => { uint a = s.A[0], b = s.A[1]; for (uint i = 0; i < s.D[0]; i++) P.Write8(b + i, P.Read8(a + i)); return 0; });
        Cpu = M68kCoreFactory.Default.Create(M68kCpuModel.M68000, Bus);
    }
    private void Initialize(uint at)
    {
        uint offset = 0;
        while (true)
        {
            at = (at + 1) & ~1u; uint command = P.Read8(at++); if (command == 0) return;
            uint mode = command >> 6, kind = (command >> 4) & 3, n = (command & 15) + 1;
            if (mode == 2) offset = P.Read8(at++);
            if (mode == 3) { offset = ((uint)P.Read8(at) << 16) | ((uint)P.Read8(at + 1) << 8) | P.Read8(at + 2); at += 3; }
            uint width = kind == 0 ? 4u : kind == 1 ? 2u : 1u, value = 0;
            for (uint i = 0; i < n; i++)
            {
                if (i == 0 || mode != 1)
                { if (width > 1) at = (at + 1) & ~1u; value = width == 4 ? P.Read32(at) : width == 2 ? P.Read16(at) : P.Read8(at); at += width; }
                uint target = library + offset;
                if (width == 4) P.Write32(target, value); else if (width == 2) P.Write16(target, (ushort)value); else P.Write8(target, (byte)value);
                offset += width;
            }
        }
    }
    internal void Vector(uint offset, Action<M68kCpuState>? args = null) => Call(library - offset, args);
    internal void Call(uint address, Action<M68kCpuState>? args = null)
    {
        Cpu.State.StatusRegister = 0; Cpu.BeginSubroutine(address, 0xf00000, 0xff0000);
        for (int i = 0; i < 8; i++) Cpu.State.D[i] = 0;
        for (int i = 0; i < 7; i++) Cpu.State.A[i] = 0;
        Cpu.State.A[6] = library; args?.Invoke(Cpu.State);
        uint[] incomingD = Enumerable.Range(0, 8).Select(i => Cpu.State.D[i]).ToArray(), incomingA = Enumerable.Range(0, 7).Select(i => Cpu.State.A[i]).ToArray();
        long cycles = Cpu.State.Cycles;
        var trace = new Queue<uint>(); int n = 0;
        try
        {
            while (Cpu.State.ProgramCounter != 0xff0000 && !Cpu.State.Halted && n++ < 16_000_000)
            { trace.Enqueue(Cpu.State.ProgramCounter); if (trace.Count > 20) trace.Dequeue(); if (P.Read16(Cpu.State.ProgramCounter) == 0 && Cpu.State.ProgramCounter < NativeBus.CodeBase) throw new InvalidOperationException("Unimplemented OS entry"); Cpu.ExecuteInstruction(); }
            Assert.Equal(0xff0000u, Cpu.State.ProgramCounter);
            Assert.Equal(0xf00000u, Cpu.State.A[7]);
            for (int i = 2; i < 8; i++) Assert.Equal(incomingD[i], Cpu.State.D[i]);
            for (int i = 2; i < 7; i++) Assert.Equal(incomingA[i], Cpu.State.A[i]);
            LastInstructions = n; LastCycles = Cpu.State.Cycles - cycles;
        }
        catch (Exception e) { throw new InvalidOperationException("Guest PC " + Cpu.State.ProgramCounter.ToString("x") + " trace " + string.Join(",", trace.Select(v => v.ToString("x"))) + " A " + string.Join(",", Enumerable.Range(0, 8).Select(i => Cpu.State.A[i].ToString("x"))) + " D " + string.Join(",", Enumerable.Range(0, 8).Select(i => Cpu.State.D[i].ToString("x"))), e); }
    }
    public void Dispose() => Cpu.Dispose();
}
