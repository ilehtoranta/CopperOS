using System.Text.Json;
using Amiga;
using Copper68k;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;
using CopperOS.Locale.LibraryBuilder;

namespace CopperOS.Locale.Tests;

public class NativeExportTests
{
    [Theory]
    [InlineData(M68kCpuTarget.M68000, M68kCpuModel.M68000)]
    [InlineData(M68kCpuTarget.M68020, M68kCpuModel.M68020)]
    [InlineData(M68kCpuTarget.M68040, M68kCpuModel.M68040)]
    public void LoadableLibraryExecutesRealRegisterAdapters(M68kCpuTarget target, M68kCpuModel model)
    {
        string directory = AppContext.BaseDirectory;
        var result = AmigaM68kCompiler.Compile(new M68kCompilationRequest
        {
            AssemblyPath = typeof(LocaleNativeLibrary).Assembly.Location,
            EntryPoint = "CopperOS.Locale.LocaleNativeLibrary::LibraryImageEntry",
            IncludedExportNames = LocaleLibraryPackager.ExportNames,
            ManagedAssemblyPaths = Directory.GetFiles(directory, "CopperSharp.*.dll"),
            Cpu = target, FloatingPoint = M68kFloatingPointMode.Disabled, ClrPolicy = M68kClrPolicy.Always,
            ExceptionMode = M68kExceptionMode.Yolo, OutputFormat = M68kOutputFormat.Hunk,
            RuntimeProfile = M68kRuntimeProfile.Resident, MemoryManagement = M68kMemoryManagement.None,
            PeepholeOptimization = M68kPeepholeOptimizationMode.Disabled,
        });
        Assert.True(result.FrameworkAnalysis.IsCompatible); Assert.Empty(result.FrameworkFeatures);
        Assert.Empty(result.FrameworkAnalysis.ManagedAllocationSites); Assert.Empty(result.NativeCompatibility.RuntimeHelpers);
        Assert.Empty(result.NativeCompatibility.RuntimeFeatures); Assert.Empty(result.NativeCompatibility.ExternalNativeTargets);
        Assert.Equal(0, result.NativeCompatibility.ExceptionRegionCount);
        // The fault-site count includes the compiler's deliberate division
        // guards. Keep them and report the count; Call below verifies that all
        // exercised native APIs return without taking a fault.
        var artifact = LocaleLibraryPackager.Build(result.Code, result.Relocations, result.Symbols);
        var bus = new NativeBus(); var p = new TestPlatform(bus.Data); var costs = new List<object>();
        string lookupShape = "";
        foreach (int relocation in artifact.Relocations)
        {
            uint v = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(artifact.Code.AsSpan(relocation, 4));
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(artifact.Code.AsSpan(relocation, 4), v + NativeBus.CodeBase);
        }
        artifact.Code.CopyTo(bus.Data.Memory.AsSpan((int)NativeBus.CodeBase));
        byte[] snapshot = artifact.Code.ToArray();
        var entries = artifact.Exports.ToDictionary(e => e.Key, e => NativeBus.CodeBase + e.Value);
        foreach (var e in new[] { ("RawDoFmt", NativeBus.Exec - 522), ("Strnicmp", NativeBus.Utility - 168), ("Stricmp", NativeBus.Utility - 162),
            ("ToLower", NativeBus.Utility - 180), ("ToUpper", NativeBus.Utility - 174), ("DateToStr", NativeBus.Dos - 744), ("StrToDate", NativeBus.Dos - 750), ("DosString", NativeBus.Dos - 978) })
            entries.Add("copperos.locale.System." + e.Item1, e.Item2);
        for (int i = 0; i < LocaleLibraryPackager.FunctionTableExports.Count; i++)
        {
            string name = LocaleLibraryPackager.FunctionTableExports[i];
            uint vector = NativeBus.Library - (uint)((i + 1) * 6);
            p.Write16(vector, 0x4ef9); p.Write32(vector + 2, NativeBus.CodeBase + artifact.Exports[name]);
            entries[name] = vector;
        }
        p.Write16(NativeBus.Library + 16, (ushort)LocaleNativeLibrary.NegativeBytes);
        p.Write16(NativeBus.Library + 18, (ushort)LocaleNativeLibrary.PositiveBytes);
        uint Address(string name) => entries["copperos.locale." + name];
        using var cpu = M68kCoreFactory.Default.Create(model, bus);
        int Call(string name, Action<M68kCpuState>? args = null)
        {
            cpu.State.StatusRegister = 0;
            cpu.BeginSubroutine(Address(name), 0xf00000, 0xff0000);
            for (int i = 0; i < 8; i++) cpu.State.D[i] = 0xd0000000u + (uint)i;
            for (int i = 0; i < 7; i++) cpu.State.A[i] = 0xa0000000u + (uint)i;
            cpu.State.A[6] = NativeBus.Library; args?.Invoke(cpu.State);
            var incomingD = Enumerable.Range(0, 8).Select(i => cpu.State.D[i]).ToArray();
            var incomingA = Enumerable.Range(0, 7).Select(i => cpu.State.A[i]).ToArray();
            long cycles = cpu.State.Cycles; int n = 0;
            while (cpu.State.ProgramCounter != 0xff0000 && !cpu.State.Halted && n++ < 4_000_000) cpu.ExecuteInstruction();
            Assert.Equal(0xff0000u, cpu.State.ProgramCounter);
            Assert.Equal(0xf00000u, cpu.State.A[7]);
            for (int i = 2; i < 8; i++) Assert.Equal(incomingD[i], cpu.State.D[i]);
            for (int i = 2; i < 7; i++) Assert.Equal(incomingA[i], cpu.State.A[i]);
            var changes = Enumerable.Range(0, snapshot.Length).Where(i => bus.Data.Memory[NativeBus.CodeBase + i] != snapshot[i]).Take(16).ToArray();
            Assert.True(changes.Length == 0, "shared library code/constants changed by " + name + ": " + string.Join(", ", changes.Select(i => $"{i:x} {snapshot[i]:x2}->{bus.Data.Memory[NativeBus.CodeBase + i]:x2} [{string.Join(';', result.Symbols.Where(s => s.Address == i - artifact.Layout.CompiledCode).Select(s => s.Name))}]")));
            if (name == "GetCatalogStr") costs.Add(new { Shape = lookupShape, Instructions = n, Cycles = cpu.State.Cycles - cycles });
            return n;
        }
        Call("Init", s => { s.D[0] = NativeBus.Library; s.A[0] = 0x777; s.A[6] = NativeBus.Exec; });
        Assert.Equal(NativeBus.Library, cpu.State.D[0]); Assert.Equal(1, bus.RegisteredMemoryHandlers);
        Assert.Equal("locale.library 53.9 (CopperOS)", p.String(p.Read32(NativeBus.Library + 24)));
        Assert.Contains("$VER: locale.library 53.9 (CopperOS)", System.Text.Encoding.Latin1.GetString(snapshot));
        Call("Open", s => s.D[0] = 38); Assert.Equal(NativeBus.Library, cpu.State.D[0]);
        Call("System.ToLower", s => { s.A[6] = NativeBus.Utility; s.D[0] = 215; }); Assert.Equal(247u, cpu.State.D[0]);
        uint utilityA = p.Text("AbCd"), utilityB = p.Text("abce");
        Call("System.Stricmp", s => { s.A[6] = NativeBus.Utility; s.A[0] = utilityA; s.A[1] = utilityB; }); Assert.True((int)cpu.State.D[0] < 0);
        Call("System.Strnicmp", s => { s.A[6] = NativeBus.Utility; s.A[0] = utilityA; s.A[1] = utilityB; s.D[0] = 3; }); Assert.Equal(0u, cpu.State.D[0]);
        Call("System.DosString", s => { s.A[6] = NativeBus.Dos; s.D[0] = 17; s.D[1] = 205; }); Assert.Equal("Object not found", p.String(cpu.State.D[0]));
        Call("OpenLocale", s => s.A[0] = 0); uint locale = cpu.State.D[0]; Assert.NotEqual(0u, locale);
        Assert.Equal("\u00a2", p.String(p.Read32(locale + LocaleLayout.SmallCurrency)));
        Call("ConvToUpper", s => { s.A[0] = 0; s.D[0] = 0xe4; }); Assert.Equal(0xc4u, cpu.State.D[0]);
        Call("GetLocaleStr", s => { s.A[0] = 0; s.D[0] = 15; }); Assert.Equal("January", p.String(cpu.State.D[0]));
        Call("GetLocaleStr", s => { s.A[0] = 0; s.D[0] = 51; }); Assert.Equal("English", p.String(cpu.State.D[0]));
        foreach (bool sparse in new[] { false, true })
        {
            lookupShape = sparse ? "sparse-1024" : "dense-1024";
            byte[] image = Fixture.Catalog(Enumerable.Range(0, 1024).Reverse().Select(i => ((uint)i * (sparse ? 65537u : 1u) + 100, $"text {i}")));
            string name = sparse ? "RAM:sparse.catalog" : "RAM:dense.catalog"; bus.Data.Files[name] = image; uint namePtr = p.Text(name);
            Call("OpenCatalogA", s => { s.A[0] = 0; s.A[1] = namePtr; s.A[2] = 0; });
            uint catalog = cpu.State.D[0]; Assert.NotEqual(0u, catalog);
            foreach (int id in new[] { 0, 512, 1023 })
            {
                int n = Call("GetCatalogStr", s => { s.A[0] = catalog; s.D[0] = 100 + (uint)id * (sparse ? 65537u : 1u); s.A[1] = 0; });
                Assert.Equal($"text {id}", p.String(cpu.State.D[0])); Assert.InRange(n, 1, sparse ? 280 : 24);
            }
            uint fallback = p.Text("fallback");
            foreach (uint id in new[] { 99u, uint.MaxValue })
            { Call("GetCatalogStr", s => { s.A[0] = catalog; s.D[0] = id; s.A[1] = fallback; }); Assert.Equal(fallback, cpu.State.D[0]); }
            Call("CloseCatalog", s => s.A[0] = catalog);
        }
        lookupShape = "encoding-adapters";
        // Cover conversion through OpenCatalogA's actual register/tag adapter,
        // rather than constructing only host-side catalog records.
        foreach (uint source in new uint[] { 0, 1, 2 }) foreach (uint targetSet in new uint[] { 0, 1, 2 })
        {
            if (source == targetSet) continue;
            string catalogName = $"RAM:encoding-{source}-{targetSet}.catalog";
            bus.Data.Files[catalogName] = Fixture.Catalog([(1u, "hämärä")], codeSet: source);
            uint namePointer = p.Text(catalogName), conversionTags = p.Allocate(32);
            p.Write32(conversionTags, CatalogService.TagBase + 5); p.Write32(conversionTags + 4, targetSet); // OC_CodeSet, MorphOS V51
            p.Write32(conversionTags + 16, CatalogService.TagBase + 2); p.Write32(conversionTags + 20, 1);
            foreach (bool convert in new[] { true, false })
            {
                p.Write32(conversionTags + 8, CatalogService.TagBase + 7); p.Write32(conversionTags + 12, convert ? 1u : 0u);
                Call("OpenCatalogA", s => { s.A[0] = 0; s.A[1] = namePointer; s.A[2] = conversionTags; });
                uint converted = cpu.State.D[0]; Assert.NotEqual(0u, converted);
                uint actualSet = convert ? targetSet : source; Assert.Equal(actualSet, p.Read32(converted + 20));
                int lookupAllocations = bus.Data.AllocationCalls;
                Call("GetCatalogStr", s => { s.A[0] = converted; s.D[0] = 1; s.A[1] = 0; });
                Assert.NotEqual(0u, cpu.State.D[0]); Assert.Equal(lookupAllocations, bus.Data.AllocationCalls);
                var encoding = actualSet == 0 ? System.Text.Encoding.Latin1 : actualSet == 1 ?
                    System.Text.Encoding.UTF8 : new System.Text.UTF32Encoding(true, false);
                var expectedBytes = encoding.GetBytes("hämärä").Concat(new byte[actualSet == 2 ? 4 : 1]).ToArray();
                Assert.Equal(expectedBytes, bus.Data.Memory.AsSpan((int)cpu.State.D[0], expectedBytes.Length).ToArray());
                Call("CloseCatalog", s => s.A[0] = converted);
            }
        }
        lookupShape = "builtin-tag-adapters";
        foreach (uint builtInType in new uint[] { 0, 1 })
        {
            string fileName = $"RAM:native-builtins-{builtInType}.catalog";
            bus.Data.Files[fileName] = Fixture.Catalog([(1u, "translated"), (3u, "three")]);
            uint data = p.Allocate(20);
            if (builtInType == 0)
            {
                p.Write32(data, 1); p.Write32(data + 4, p.Text("one"));
                p.Write32(data + 8, 2); p.Write32(data + 12, p.Text("two"));
            }
            else
            {
                p.Write32(data, 1); p.Write16(data + 4, 4); p.Write32(data + 6, 0x6f6e6500);
                p.Write32(data + 10, 2); p.Write16(data + 14, 4); p.Write32(data + 16, 0x74776f00);
            }
            uint builtDescriptor = p.Allocate(12), builtTags = p.Allocate(16), builtName = p.Text(fileName);
            p.Write32(builtDescriptor, builtInType); p.Write32(builtDescriptor + 4, builtInType == 0 ? 16u : 20u);
            p.Write32(builtDescriptor + 8, data); p.Write32(builtTags, CatalogService.TagBase + 6);
            p.Write32(builtTags + 4, builtDescriptor);
            Call("OpenCatalogA", s => { s.A[0] = 0; s.A[1] = builtName; s.A[2] = builtTags; });
            uint builtCatalog = cpu.State.D[0]; Assert.NotEqual(0u, builtCatalog);
            foreach (var entry in new[] { (1u, "translated"), (2u, "two"), (3u, "three") })
            {
                Call("GetCatalogStr", s => { s.A[0] = builtCatalog; s.D[0] = entry.Item1; s.A[1] = 0; });
                Assert.Equal(entry.Item2, p.String(cpu.State.D[0]));
            }
            // A simultaneous open can supply different built-ins without
            // changing strings borrowed from the first per-open wrapper.
            uint otherData = p.Allocate(8), otherDescriptor = p.Allocate(12);
            p.Write32(otherData, 2); p.Write32(otherData + 4, p.Text("other"));
            p.Write32(otherDescriptor + 4, 8); p.Write32(otherDescriptor + 8, otherData);
            p.Write32(builtTags + 4, otherDescriptor);
            Call("OpenCatalogA", s => { s.A[0] = 0; s.A[1] = builtName; s.A[2] = builtTags; });
            uint otherCatalog = cpu.State.D[0]; Assert.NotEqual(0u, otherCatalog); Assert.NotEqual(builtCatalog, otherCatalog);
            Call("GetCatalogStr", s => { s.A[0] = otherCatalog; s.D[0] = 2; s.A[1] = 0; });
            Assert.Equal("other", p.String(cpu.State.D[0]));
            Call("GetCatalogStr", s => { s.A[0] = builtCatalog; s.D[0] = 2; s.A[1] = 0; });
            Assert.Equal("two", p.String(cpu.State.D[0]));
            Call("CloseCatalog", s => s.A[0] = otherCatalog); Call("CloseCatalog", s => s.A[0] = builtCatalog);
        }
        lookupShape = "unsigned-and-builtins";
        byte[] highImage = Fixture.Catalog([(0x80000000u, "high"), (uint.MaxValue, "last")]);
        uint highCatalog = CatalogCore.Read(ref p, p.Put(highImage), (uint)highImage.Length, p.Text("n"), 0, 0, 0);
        foreach (uint id in new[] { 0x80000000u, uint.MaxValue })
        { Call("GetCatalogStr", s => { s.A[0] = highCatalog; s.D[0] = id; s.A[1] = 0; }); Assert.Equal(id == uint.MaxValue ? "last" : "high", p.String(cpu.State.D[0])); }
        uint array = p.Allocate(8); p.Write32(array, 42); p.Write32(array + 4, p.Text("built-in"));
        uint descriptor = p.Allocate(12); p.Write32(descriptor + 4, 8); p.Write32(descriptor + 8, array);
        uint wrapper = CatalogCore.BuiltIn(ref p, descriptor, highCatalog);
        Call("GetCatalogStr", s => { s.A[0] = wrapper; s.D[0] = 42; s.A[1] = 0; }); Assert.Equal("built-in", p.String(cpu.State.D[0]));
        Call("GetCatalogStr", s => { s.A[0] = wrapper; s.D[0] = uint.MaxValue; s.A[1] = 0; }); Assert.Equal("last", p.String(cpu.State.D[0]));
        Call("GetCatalogStr", s => { s.A[0] = 0; s.D[0] = 99; s.A[1] = array; }); Assert.Equal(array, cpu.State.D[0]);
        byte[] holes = Fixture.Catalog([(100u, "first"), (102u, "last")]);
        uint holeCatalog = CatalogCore.Read(ref p, p.Put(holes), (uint)holes.Length, p.Text("holes"), 0, 0, 0);
        Call("GetCatalogStr", s => { s.A[0] = holeCatalog; s.D[0] = 101; s.A[1] = array; }); Assert.Equal(array, cpu.State.D[0]);
        uint stamp = p.Allocate(12), format = p.Text("%F %T"), hook = p.Allocate(20);
        p.Write32(hook + 8, NativeBus.OutputHook); bus.OutputLocale = locale; bus.Output.Clear();
        Call("FormatDate", s => { s.A[0] = locale; s.A[1] = format; s.A[2] = stamp; s.A[3] = hook; }); Assert.Equal("1978-01-01 00:00:00", bus.OutputText);
        uint stream = p.Allocate(8); p.Write16(stream, unchecked((ushort)-2)); p.Write32(stream + 2, 1234567); format = p.Text("%d %lU"); bus.Output.Clear();
        Call("FormatString", s => { s.A[0] = locale; s.A[1] = format; s.A[2] = stream; s.A[3] = hook; });
        Assert.Equal("-2 1,234,567", bus.OutputText); Assert.Equal(stream + 6, cpu.State.D[0]);
        // Exercise the 64-bit M68k FormatString stream, including signed minimum,
        // both extension spellings, the returned next pointer and allocation-free output.
        uint wideStream = p.Allocate(24), wideFormat = p.Text("%Ld|%llu|%Lx");
        p.Write32(wideStream, 0x80000000); p.Write32(wideStream + 4, 0);
        p.Write32(wideStream + 8, uint.MaxValue); p.Write32(wideStream + 12, uint.MaxValue);
        p.Write32(wideStream + 16, 0xfedcba98); p.Write32(wideStream + 20, 0x76543210);
        int wideAllocations = bus.Data.AllocationCalls; bus.Output.Clear();
        Call("FormatString", s => { s.A[0] = locale; s.A[1] = wideFormat; s.A[2] = wideStream; s.A[3] = hook; });
        Assert.Equal("-9223372036854775808|18446744073709551615|FEDCBA9876543210", bus.OutputText);
        Assert.Equal(wideStream + 24, cpu.State.D[0]); Assert.Equal(wideAllocations, bus.Data.AllocationCalls);
        uint put = p.Allocate(4); p.Write32(put, 0x16c04e75); uint rawOutput = p.Allocate(64);
        int allocations = bus.Data.AllocationCalls;
        Call("System.RawDoFmt", s => { s.A[6] = NativeBus.Exec; s.A[0] = format; s.A[1] = stream; s.A[2] = put; s.A[3] = rawOutput; });
        Assert.Equal("-2 1,234,567", p.String(rawOutput)); Assert.Equal(stream + 2, cpu.State.D[0]); Assert.Equal(allocations, bus.Data.AllocationCalls);
        Call("System.RawDoFmt", s => { s.A[6] = NativeBus.Exec; s.A[0] = format; s.A[1] = stream; s.A[2] = 0; s.A[3] = rawOutput; }); Assert.Equal("-2 1,234,567", p.String(rawOutput));
        uint rawCount = p.Allocate(4);
        Call("System.RawDoFmt", s => { s.A[6] = NativeBus.Exec; s.A[0] = format; s.A[1] = stream; s.A[2] = 2; s.A[3] = rawCount; }); Assert.Equal(13u, p.Read32(rawCount));
        bus.Serial.Clear();
        Call("System.RawDoFmt", s => { s.A[6] = NativeBus.Exec; s.A[0] = format; s.A[1] = stream; s.A[2] = 1; s.A[3] = 0; }); Assert.Equal("-2 1,234,567", new string(bus.Serial.Select(c => (char)c).ToArray()));
        uint dt = p.Allocate(26), dayText = p.Allocate(32), dateText = p.Allocate(32), timeText = p.Allocate(32);
        p.Write32(dt, 8094); p.Write32(dt + 4, 804); p.Write32(dt + 8, 1550); p.Write8(dt + 12, 1);
        p.Write32(dt + 14, dayText); p.Write32(dt + 18, dateText); p.Write32(dt + 22, timeText);
        allocations = bus.Data.AllocationCalls;
        Call("System.DateToStr", s => { s.A[6] = NativeBus.Dos; s.D[1] = dt; }); Assert.Equal(1u, cpu.State.D[0]);
        Assert.Equal("Tuesday", p.String(dayText)); Assert.Equal("00-Feb-29", p.String(dateText)); Assert.Equal("13:24:31", p.String(timeText));
        p.Write32(dt + 18, p.Text("00-02-29")); p.Write32(dt + 22, p.Text("13:24")); allocations += 2;
        Call("System.StrToDate", s => { s.A[6] = NativeBus.Dos; s.D[1] = dt; }); Assert.Equal(1u, cpu.State.D[0]);
        Assert.Equal(8094u, p.Read32(dt)); Assert.Equal(804u, p.Read32(dt + 4)); Assert.Equal(0u, p.Read32(dt + 8)); Assert.Equal(allocations, bus.Data.AllocationCalls);
        format = p.Text("%d %b %Y"); p.Write32(hook + 8, NativeBus.InputHook); bus.Input = new Queue<uint>("29 Feb 2000".Select(c => (uint)c));
        Call("ParseDate", s => { s.A[0] = locale; s.A[1] = stamp; s.A[2] = format; s.A[3] = hook; }); Assert.Equal(1u, cpu.State.D[0]);
        Assert.Equal((uint)(new DateTime(2000, 2, 29) - new DateTime(1978, 1, 1)).Days, p.Read32(stamp));
        uint msg = p.Allocate(128); p.Write32(msg + 28, 0x02000001); p.Write32(msg + 40, p.Text("CONVTOUPPER")); p.Write32(msg + 44, p.Text("a"));
        Call("RexxHost", s => s.A[0] = msg); Assert.Equal(0u, cpu.State.D[0]); Assert.Equal("A", p.String(cpu.State.A[0]));
        foreach (var rexxCase in new[] { ("ISALPHA", "a", "1"), ("ISALPHA", "3", "0"),
            ("ISXDIGIT", "f", "1"), ("CONVTOLOWER", "A", "a"), ("GETLOCALESTR", "15", "January") })
        {
            p.Write32(msg + 40, p.Text(rexxCase.Item1)); p.Write32(msg + 44, p.Text(rexxCase.Item2));
            Call("RexxHost", s => s.A[0] = msg);
            Assert.Equal(0u, cpu.State.D[0]); Assert.Equal(rexxCase.Item3, p.String(cpu.State.A[0]));
        }
        foreach (var rexxError in new[] { ("UNKNOWN", 1u, 1u), ("ISALPHA", 2u, 17u), ("ISALPHA", 1u, 18u) })
        {
            p.Write32(msg + 28, 0x02000000 | rexxError.Item2); p.Write32(msg + 40, p.Text(rexxError.Item1));
            p.Write32(msg + 44, 0);
            Call("RexxHost", s => s.A[0] = msg);
            Assert.Equal(rexxError.Item3, cpu.State.D[0]); Assert.Equal(0u, cpu.State.A[0]);
        }
        Call("MemoryHandler", s => s.A[1] = NativeBus.Library); Assert.Equal(1u, cpu.State.D[0]);
        bus.DriverAvailable = true; bus.Data.Files["RAM:locale.prefs"] = Fixture.Prefs();
        uint prefsName = p.Text("RAM:locale.prefs");
        Call("OpenLocale", s => s.A[0] = prefsName); uint configured = cpu.State.D[0]; Assert.NotEqual(0u, configured);
        Assert.Equal(-120, (int)p.Read32(configured + LocaleLayout.GmtOffset));
        Call("GetLocaleStr", s => { s.A[0] = configured; s.D[0] = 15; }); Assert.Equal("tammikuu", p.String(cpu.State.D[0]));
        bus.Data.Files["LOCALE:Catalogs/finnish/sys/dos.catalog"] = Fixture.Catalog([(205u, "translated DOS error")]); bus.Data.Error = 77;
        Call("LocalePrefsUpdate", s => s.A[0] = configured); uint previous = cpu.State.D[0]; Assert.Equal(locale, previous); Assert.Equal(77, bus.Data.Error);
        Assert.Equal("finnish", bus.Variables["Language"]);
        Call("System.DosString", s => { s.A[6] = NativeBus.Dos; s.D[0] = 17; s.D[1] = 205; }); uint translatedError = cpu.State.D[0]; Assert.Equal("translated DOS error", p.String(translatedError));
        Call("System.DosString", s => { s.A[6] = NativeBus.Dos; s.D[0] = 17; s.D[1] = unchecked((uint)-205); }); Assert.Equal(0u, cpu.State.D[0]);
        Call("System.DosString", s => { s.A[6] = NativeBus.Dos; s.D[0] = 103; s.D[1] = 103; }); Assert.Equal("Not enough memory", p.String(cpu.State.D[0]));
        Call("CloseLocale", s => s.A[0] = previous);
        Call("GetLocaleStr", s => { s.A[0] = 0; s.D[0] = 15; }); Assert.Equal("tammikuu", p.String(cpu.State.D[0]));
        Call("CloseLocale", s => s.A[0] = configured);
        Call("CloseLocale", s => s.A[0] = locale);
        Call("Expunge"); Assert.Equal(0u, cpu.State.D[0]);
        Call("Close"); Assert.Equal(0u, cpu.State.D[0]); Assert.Equal(1, bus.RegisteredMemoryHandlers); Assert.False(bus.FreedLibrary);
        Assert.Equal(1u, p.Read32(NativeBus.Library + 34));
        Call("System.ToUpper", s => { s.A[6] = NativeBus.Utility; s.D[0] = 97; }); Assert.Equal(65u, cpu.State.D[0]);
        Assert.Equal("translated DOS error", p.String(translatedError));
        string reports = Path.Combine(OriginalLocaleFactAttribute.Root, "artifacts/locale-tests"); Directory.CreateDirectory(reports);
        File.WriteAllText(Path.Combine(reports, target + ".json"), JsonSerializer.Serialize(new { Cpu = target.ToString(), ImageBytes = artifact.Image.Length,
            ImageSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(artifact.Image)),
            ExceptionRegions = result.NativeCompatibility.ExceptionRegionCount,
            FatalMachineFaultSites = result.NativeCompatibility.FatalMachineFaultSiteCount,
            InputSha256 = new[] { typeof(LocaleNativeLibrary).Assembly.Location, typeof(LocaleLibraryPackager).Assembly.Location,
                typeof(M68kCompiler).Assembly.Location, typeof(AmigaM68kCompiler).Assembly.Location,
                typeof(APTR).Assembly.Location, typeof(IAmigaGuestMemory).Assembly.Location, typeof(M68kCoreFactory).Assembly.Location }
                .Distinct().ToDictionary(path => Path.GetFileName(path), path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)))),
            NativeExtensionCoverage = new[] { "LANG_NAME", "%L/%ll signed minimum and unsigned maximum", "OC_CodeSet/OC_ConvertStrings six encoding pairs", "CatComp array/block tag adapters", "simultaneous per-open built-ins", "ARexx D0/A0 successes and errors" },
            Lookup = costs }, new JsonSerializerOptions { WriteIndented = true }));
    }
}

internal sealed class NativeBus : IM68kBus
{
    internal readonly List<(uint Library, int Offset)> PatchTargets = new();
    internal const uint CodeBase = 0x40000, Exec = 0x10000, Dos = 0x12000, Rexx = 0x14000, Language = 0x16000, Library = 0x18000,
        OutputHook = 0x20000, InputHook = 0x20100, Utility = 0x1a000, Iff = 0x1c000, Intuition = 0x1e000;
    internal readonly TestState Data = new() { Next = 0x100000 };
    internal readonly Dictionary<uint, Action<M68kCpuState>> Gateways = new();
    private readonly Dictionary<uint, (byte[] Bytes, int Position)> files = new();
    internal int RegisteredMemoryHandlers;
    internal bool FreedLibrary;
    internal bool DriverAvailable;
    internal uint OutputLocale;
    internal readonly List<uint> Output = new();
    internal readonly List<uint> Serial = new();
    internal Queue<uint> Input = new();
    internal string OutputText => new(Output.Where(c => c != 0).Select(c => (char)c).ToArray());
    internal uint Today = 17000;
    internal int MaximumRead = 17;
    internal readonly Dictionary<int, uint> ErrorStrings = new();
    internal readonly Dictionary<string, string> Variables = new();
    private uint nextGateway = 0x21000;
    internal void Register(uint b, int lvo, Func<M68kCpuState, uint> fn, bool dual = false, bool pure = false)
    {
        uint address = nextGateway; nextGateway += 8; var p = new TestPlatform(Data);
        Gateway(address, s => { if (!pure) Assert.Equal(b, s.A[6]); uint result = fn(s); s.D[0] = result; if (!dual) s.D[1] = 0xd1d1d1d1; if (!pure) { s.A[0] = 0xa0a0a0a0; s.A[1] = 0xa1a1a1a1; } });
        p.Write16((uint)(b + lvo), 0x4ef9); p.Write32((uint)(b + lvo + 2), address);
    }
    internal NativeBus()
    {
        var p = new TestPlatform(Data); p.Write32(4, Exec); p.Write16(Exec + 20, 40);
        p.Write32(Exec + 276, 0x26000); p.Write8(0x26000 + 8, 13);
        uint errors = p.Allocate(8), ranges = p.Allocate(24), rangeIndex = 0; var messages = new List<byte>();
        foreach (var e in new[] { (103, "Not enough memory"), (205, "Object not found") })
        {
            uint i = rangeIndex++; p.Write32(ranges + i * 8, (uint)e.Item1); p.Write32(ranges + i * 8 + 4, (uint)e.Item1);
            messages.Add((byte)(e.Item2.Length + 1)); messages.AddRange(System.Text.Encoding.Latin1.GetBytes(e.Item2)); messages.Add(0);
        }
        uint textBlob = p.Put(messages.ToArray()); p.Write32(errors, ranges); p.Write32(errors + 4, textBlob); p.Write32(Dos + 54, errors);
        ErrorStrings.Add(103, textBlob + 1); ErrorStrings.Add(205, textBlob + 1 + (uint)"Not enough memory".Length + 2);
        Register(Exec, -294, _ => 0x26000);
        Register(Exec, -552, s => { string name = p.String(s.A[1]); return name == "dos.library" ? Dos : name == "utility.library" ? Utility : name == "intuition.library" ? Intuition : name == "iffparse.library" ? Iff : name == "rexxsyslib.library" ? Rexx : DriverAvailable && name.StartsWith("LOCALE:Languages/") ? Language : 0; });
        Register(Exec, -636, _ => 0);
        Register(Exec, -420, s => { PatchTargets.Add((s.A[1], (int)s.A[0])); uint vector = s.A[1] + s.A[0], old = p.Read32(vector + 2); p.Write16(vector, 0x4ef9); p.Write32(vector + 2, s.D[0]); return old; });
        Register(Exec, -522, _ => 0);
        Register(Exec, -516, s => { Serial.Add(s.D[0]); return 0; });
        foreach (int lvo in new[] { -162, -168, -174, -180 }) Register(Utility, lvo, _ => 0);
        Register(Utility, -36, s =>
        {
            uint at = s.A[0]; while (at != 0) { uint tag = p.Read32(at), value = p.Read32(at + 4); at += 8;
                if (tag == 0) break; if (tag == 1) continue; if (tag == 2) { at = value; continue; } if (tag == 3) { at += value * 8; continue; } if (tag == s.D[0]) return value; }
            return s.D[1];
        });
        p.Write32(Utility - 102, 0x2f280008); p.Write16(Utility - 98, 0x4e75); // CallHookPkt: tail-call h_Entry
        Register(Utility, -138, s => s.D[0] * s.D[1], true, true); Register(Utility, -144, s => s.D[0] * s.D[1], true, true);
        Register(Utility, -150, s => { int a = (int)s.D[0], b = (int)s.D[1]; s.D[1] = (uint)(a % b); return (uint)(a / b); }, true, true);
        Register(Utility, -156, s => { uint a = s.D[0], b = s.D[1]; s.D[1] = a % b; return a / b; }, true, true);
        Register(Utility, -120, s =>
        {
            var d = new DateTime(1978, 1, 1).AddSeconds(s.D[0]); uint a = s.A[0];
            ushort[] values = { (ushort)d.Second, (ushort)d.Minute, (ushort)d.Hour, (ushort)d.Day, (ushort)d.Month, (ushort)d.Year, (ushort)d.DayOfWeek };
            for (uint i = 0; i < values.Length; i++) p.Write16(a + i * 2, values[i]); return 0;
        });
        foreach (int lvo in new[] { -126, -132 }) Register(Utility, lvo, s =>
        { uint a = s.A[0]; try { return (uint)(new DateTime(p.Read16(a + 10), p.Read16(a + 8), p.Read16(a + 6), p.Read16(a + 4), p.Read16(a + 2), p.Read16(a)) - new DateTime(1978, 1, 1)).TotalSeconds; } catch (ArgumentOutOfRangeException) { return 0; } });
        Register(Dos, -84, _ => 1); Register(Dos, -90, _ => 0);
        Register(Dos, -600, _ => 0); Register(Dos, -126, _ => 0);
        Register(Dos, -744, _ => 0); Register(Dos, -750, _ => 0);
        Register(Dos, -924, s => { int n = (int)s.D[1]; return ErrorStrings.GetValueOrDefault(n < 0 ? -n : n); });
        Register(Dos, -978, s => ErrorStrings.GetValueOrDefault((int)s.D[1]));
        Register(Dos, -900, s => { Variables[p.String(s.D[1])] = (int)s.D[3] < 0 ? p.String(s.D[2]) : System.Text.Encoding.Latin1.GetString(Data.Memory, (int)s.D[2], (int)s.D[3]); return uint.MaxValue; });
        Register(Dos, -882, s =>
        {
            string path = p.String(s.D[1]), part = p.String(s.D[2]);
            string joined = part.Contains(':') ? part : path + (path.Length == 0 || path.EndsWith(':') || path.EndsWith('/') ? "" : "/") + part;
            if (joined.Length >= s.D[3]) return 0;
            for (uint i = 0; i < joined.Length; i++) p.Write8(s.D[1] + i, (byte)joined[(int)i]); p.Write8(s.D[1] + (uint)joined.Length, 0); return uint.MaxValue;
        });
        Register(Dos, -816, s =>
        {
            string text = p.String(s.D[1]); int i = 0; while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i < text.Length && (text[i] == '-' || text[i] == '+')) i++;
            int digits = i; while (i < text.Length && text[i] >= '0' && text[i] <= '9') i++;
            if (i == digits || !int.TryParse(text.AsSpan(0, i), out int value)) return uint.MaxValue;
            p.Write32(s.D[2], (uint)value); return (uint)i;
        });
        Register(Dos, -192, s => { p.Write32(s.D[1], Today); p.Write32(s.D[1] + 4, 0); p.Write32(s.D[1] + 8, 0); return s.D[1]; });
        Register(Exec, -414, _ => 0);
        Register(Exec, -684, s => p.Allocate(s.D[0])); Register(Exec, -690, s => { p.Free(s.A[1]); return 0; });
        Register(Exec, -210, s => { if (s.A[1] == Library - LocaleNativeLibrary.NegativeBytes) FreedLibrary = true; else p.Free(s.A[1]); return 0; });
        foreach (int lvo in new[] { -132, -138, -558, -564, -570 }) Register(Exec, lvo, _ => 0);
        Register(Exec, -240, s => { uint list = s.A[0], node = s.A[1], next = p.Read32(list); p.Write32(node, next); p.Write32(node + 4, list); p.Write32(next + 4, node); p.Write32(list, node); return 0; });
        Register(Exec, -246, s => { uint list = s.A[0], node = s.A[1], prev = p.Read32(list + 8); p.Write32(node, list + 4); p.Write32(node + 4, prev); p.Write32(prev, node); p.Write32(list + 8, node); return 0; });
        Register(Exec, -252, s => { uint next = p.Read32(s.A[1]), prev = p.Read32(s.A[1] + 4); if (next != 0 && prev != 0) { p.Write32(prev, next); p.Write32(next + 4, prev); } return 0; });
        Register(Exec, -258, s => { uint list = s.A[0], node = p.Read32(list), next = p.Read32(node); if (next == 0) return 0; p.Write32(list, next); p.Write32(next + 4, list); return node; });
        Register(Exec, -264, s => { uint list = s.A[0], node = p.Read32(list + 8), prev = p.Read32(node + 4); if (prev == 0) return 0; p.Write32(list + 8, prev); p.Write32(prev, list + 4); return node; });
        Register(Exec, -576, _ => uint.MaxValue);
        Register(Exec, -774, _ => { RegisteredMemoryHandlers++; return 0; }); Register(Exec, -780, _ => { RegisteredMemoryHandlers--; return 0; });
        Register(Dos, -30, s =>
        {
            string name = p.String(s.D[1]); Data.FileRequests.Add(name);
            if (!Data.Files.TryGetValue(name, out var bytes)) { Data.Error = 205; return 0; }
            uint h = (uint)files.Count + 1; files.Add(h, (bytes, 0)); return h;
        });
        Register(Dos, -36, s => { files.Remove(s.D[1]); return 1; });
        Register(Dos, -66, s =>
        { var f = files[s.D[1]]; int mode = (int)s.D[3], next = (mode == 1 ? f.Bytes.Length : mode == 0 ? f.Position : 0) + (int)s.D[2]; if (next < 0 || next > f.Bytes.Length) { Data.Error = 219; return uint.MaxValue; } files[s.D[1]] = (f.Bytes, next); return (uint)f.Position; });
        Register(Dos, -42, s =>
        { var f = files[s.D[1]]; int n = Math.Min((int)s.D[3], f.Bytes.Length - f.Position); n = Math.Min(n, MaximumRead); f.Bytes.AsSpan(f.Position, n).CopyTo(Data.Memory.AsSpan((int)s.D[2])); files[s.D[1]] = (f.Bytes, f.Position + n); return (uint)n; });
        Register(Dos, -132, _ => (uint)Data.Error); Register(Dos, -462, s => { int old = Data.Error; Data.Error = (int)s.D[1]; return (uint)old; });
        Register(Rexx, -168, _ => 1); Register(Rexx, -126, s => p.Put(Data.Memory.AsSpan((int)s.A[0], (int)s.D[0]).ToArray().Concat(new byte[1]).ToArray()));
        Register(Language, -30, _ => 8); Register(Language, -54, s => { Assert.Equal(15u, s.D[0]); return p.Text("tammikuu"); });
        Gateway(OutputHook, s => { Assert.Equal(OutputLocale, s.A[2]); Output.Add(s.A[1]); });
        Gateway(InputHook, s => { Assert.Equal(OutputLocale, s.A[2]); Assert.Equal(0u, s.A[1]); s.D[0] = Input.Count != 0 ? Input.Dequeue() : 0; });
    }
    private void Gateway(uint address, Action<M68kCpuState> fn)
    { Gateways.Add(address, fn); var p = new TestPlatform(Data); p.Write16(address, 0xff00); p.Write32(address + 2, 1); }
    public bool HasHostGateway(uint a) => Gateways.ContainsKey(a);
    public bool TryInvokeHostGateway(uint pc, uint token, M68kCpuState s) { if (!Gateways.TryGetValue(pc, out var f)) return false; f(s); return true; }
    public byte ReadByte(uint a, ref long c, M68kBusAccessKind k) => Data.Memory[a];
    public ushort ReadWord(uint a, ref long c, M68kBusAccessKind k) => new TestPlatform(Data).Read16(a);
    public uint ReadLong(uint a, ref long c, M68kBusAccessKind k) => new TestPlatform(Data).Read32(a);
    public void WriteByte(uint a, byte v, ref long c, M68kBusAccessKind k) => Data.Memory[a] = v;
    public void WriteWord(uint a, ushort v, ref long c, M68kBusAccessKind k) => new TestPlatform(Data).Write16(a, v);
    public void WriteLong(uint a, uint v, ref long c, M68kBusAccessKind k) => new TestPlatform(Data).Write32(a, v);
    public void ResetExternalDevices(long c) => throw new InvalidOperationException("unexpected RESET");
}
