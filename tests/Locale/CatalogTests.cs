namespace CopperOS.Locale.Tests;

public class CatalogTests
{
    [Fact]
    public void BuiltInCodeSetDoesNotSelectOrOverrideTheTranslationCodeSet()
    {
        var state = new TestState(); var p = new TestPlatform(state);
        uint locale = LocaleCore.Default(ref p), head = 0;
        state.Files["RAM:codeset.catalog"] = Fixture.Catalog([(1u, "hämärä")]);
        uint name = p.Text("RAM:codeset.catalog"), tags = p.Allocate(24);
        p.Write32(tags, CatalogService.TagBase + 2); p.Write32(tags + 4, 1); // OC_BuiltInCodeSet
        uint catalog = CatalogService.Open(ref p, locale, name, tags, ref head);
        Assert.NotEqual(0u, catalog); Assert.Equal(0u, p.Read32(catalog + 20));
        Assert.Equal("hämärä", p.String(CatalogCore.GetString(ref p, catalog, 1, 0)));
        CatalogService.Close(ref p, catalog); CatalogService.Flush(ref p, ref head);
        // The explicit OC_CodeSet must remain authoritative even when the
        // built-in code-set hint occurs later in the taglist.
        p.Write32(tags, CatalogService.TagBase + 5); p.Write32(tags + 4, 2);
        p.Write32(tags + 8, CatalogService.TagBase + 2); p.Write32(tags + 12, 1);
        catalog = CatalogService.Open(ref p, locale, name, tags, ref head);
        Assert.NotEqual(0u, catalog); Assert.Equal(2u, p.Read32(catalog + 20));
        uint text = CatalogCore.GetString(ref p, catalog, 1, 0);
        Assert.Equal((uint)'h', p.Read32(text)); Assert.Equal(0xe4u, p.Read32(text + 4));
        CatalogService.Close(ref p, catalog); CatalogService.Flush(ref p, ref head);
        Assert.Equal(0u, head);
    }

    [Theory]
    [InlineData(0u)] [InlineData(1u)] [InlineData(2u)]
    public void CatCompLengthsNeedNotIncludeTerminatingNull(uint encoding)
    {
        var p = new TestPlatform(new TestState()); byte[] file = Fixture.Catalog([(0u, ""), (1u, "1234"), (2u, "\u00e4bc")], codeSet: encoding, includeTerminator: false);
        uint input = p.Put(file), catalog = CatalogCore.Read(ref p, input, (uint)file.Length, p.Text("test"), p.Text("finnish"), 7, 0);
        Assert.NotEqual(0u, catalog);
        Assert.Equal("", p.String(CatalogCore.GetString(ref p, catalog, 0, 0)));
        Assert.Equal("1234", p.String(CatalogCore.GetString(ref p, catalog, 1, 0)));
        Assert.Equal("\u00e4bc", p.String(CatalogCore.GetString(ref p, catalog, 2, 0)));
    }
    [Fact]
    public void AllocationPressureFlushesClosedCatalogsAndRetries()
    {
        var s = new TestState(); var p = new TestPlatform(s); uint l = LocaleCore.Default(ref p), head = 0;
        s.Files["RAM:first"] = Fixture.Catalog([(1u, "first")]); s.Files["RAM:second"] = Fixture.Catalog([(2u, "second")]);
        uint first = CatalogService.Open(ref p, l, p.Text("RAM:first"), 0, ref head); CatalogService.Close(ref p, first);
        uint secondName = p.Text("RAM:second"); s.FailAllocation = s.AllocationCalls;
        uint second = CatalogService.Open(ref p, l, secondName, 0, ref head);
        Assert.NotEqual(0u, second); Assert.False(s.Allocations.ContainsKey(first)); Assert.Equal(second, head);
        Assert.Equal("second", p.String(CatalogCore.GetString(ref p, second, 2, 0)));
    }
    [Fact]
    public void MountedLocalePrecedesUnavailableProgramVolume()
    {
        var s = new TestState { AvailableLocations = 2 }; var p = new TestPlatform(s);
        uint l = LocaleCore.Default(ref p); p.Write32(l + 8, p.Text("finnish"));
        s.Files["LOCALE:Catalogs/finnish/x"] = Fixture.Catalog([(7u, "seven")]);
        uint head = 0, c = CatalogService.Open(ref p, l, p.Text("x"), 0, ref head);
        Assert.NotEqual(0u, c); Assert.Single(s.FileRequests); Assert.StartsWith("LOCALE:", s.FileRequests[0]);
    }
    [Fact]
    public void TagTraversalForcedLanguageAndExactVersionFallback()
    {
        var s = new TestState(); var p = new TestPlatform(s); uint l = LocaleCore.Default(ref p), head = 0;
        p.Write32(l + 8, p.Text("finnish"));
        s.Files["PROGDIR:Catalogs/german/x"] = Fixture.Catalog([(1u, "wrong version")], "german", version: 6);
        s.Files["PROGDIR:Catalogs/finnish/x"] = Fixture.Catalog([(1u, "correct")]);
        uint more = p.Allocate(32); p.Write32(more, CatalogService.TagBase + 4); p.Write32(more + 4, p.Text("german"));
        p.Write32(more + 8, CatalogService.TagBase + 3); p.Write32(more + 12, 7);
        uint tags = p.Allocate(40); p.Write32(tags, 3); p.Write32(tags + 4, 1); // TAG_SKIP skips an invalid version
        p.Write32(tags + 8, CatalogService.TagBase + 3); p.Write32(tags + 12, 99);
        p.Write32(tags + 16, 1); p.Write32(tags + 24, 2); p.Write32(tags + 28, more);
        uint c = CatalogService.Open(ref p, l, p.Text("x"), tags, ref head);
        Assert.NotEqual(0u, c); Assert.Equal("correct", p.String(CatalogCore.GetString(ref p, c, 1, 0)));
    }
    [Fact]
    public void AllocationFailuresRollBackOwnedIndexAndConversionBuffers()
    {
        for (int failure = 0; failure < 4; failure++)
        {
            var s = new TestState(); var p = new TestPlatform(s); byte[] b = Fixture.Catalog([(1u, "\u00e4"), (2u, "two")], codeSet: 1);
            uint f = p.Put(b), name = p.Text("n"); int before = s.Allocations.Count;
            s.FailAllocation = s.AllocationCalls + failure;
            uint c = CatalogCore.Read(ref p, f, (uint)b.Length, name, 0, 0, 0);
            if (c == 0) Assert.Equal(before, s.Allocations.Count);
            else { CatalogCore.Release(ref p, c); Assert.Equal(before - 1, s.Allocations.Count); }
        }
    }
    [Fact]
    public void CatalogConversionCanBeExplicitlyDisabled()
    {
        var p = new TestPlatform(new TestState()); byte[] b = Fixture.Catalog([(1u, "\u00e4")], codeSet: 1);
        uint c = CatalogCore.Read(ref p, p.Put(b), (uint)b.Length, p.Text("n"), 0, 0, 0, false);
        Assert.Equal(1u, p.Read32(c + 20)); uint text = CatalogCore.GetString(ref p, c, 1, 0);
        Assert.Equal(0xc3, p.Read8(text)); Assert.Equal(0xa4, p.Read8(text + 1));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DenseAndSparseUnsortedIdsResolveWithoutScanning(bool sparse)
    {
        var state = new TestState(); var p = new TestPlatform(state);
        var entries = Enumerable.Range(0, 4096).Select(i => ((uint)i * (sparse ? 65537u : 1u) + 100, $"translated {i}")).Reverse().ToArray();
        byte[] image = Fixture.Catalog(entries); uint file = p.Put(image);
        uint c = CatalogCore.Read(ref p, file, (uint)image.Length, p.Text("test.catalog"), p.Text("finnish"), 7, 0);
        Assert.NotEqual(0u, c); Assert.Equal(sparse ? 0u : 1u, p.Read32(c + CatalogCore.Dense));
        foreach (int i in new[] { 0, 1, 123, 2048, 4095 })
        {
            state.Reads = 0; int allocations = state.AllocationCalls;
            uint text = CatalogCore.GetString(ref p, c, 100u + (uint)i * (sparse ? 65537u : 1u), 0);
            Assert.InRange(state.Reads, 1, sparse ? 22 : 8); Assert.Equal(allocations, state.AllocationCalls);
            Assert.Equal($"translated {i}", p.String(text));
        }
        Assert.Equal(0x1234u, CatalogCore.GetString(ref p, c, uint.MaxValue, 0x1234));
        CatalogCore.Release(ref p, c);
    }
    [Fact]
    public void FullUnsignedIdsHolesDuplicatesAndEmptyStrings()
    {
        var p = new TestPlatform(new TestState());
        byte[] image = Fixture.Catalog([(uint.MaxValue, "last"), (0x80000000, "high"), (0, ""), (0x80000000, "duplicate")]);
        uint f = p.Put(image), c = CatalogCore.Read(ref p, f, (uint)image.Length, p.Text("n"), 0, 0, 0);
        Assert.NotEqual(0u, c); Assert.Equal("high", p.String(CatalogCore.GetString(ref p, c, 0x80000000, 0)));
        Assert.Equal("last", p.String(CatalogCore.GetString(ref p, c, uint.MaxValue, 0)));
        Assert.NotEqual(0u, CatalogCore.GetString(ref p, c, 0, 42));
        Assert.Equal(42u, CatalogCore.GetString(ref p, 0, 0, 42));
    }
    [Theory]
    [InlineData(0, 1)] [InlineData(0, 2)] [InlineData(1, 0)] [InlineData(1, 2)] [InlineData(2, 0)] [InlineData(2, 1)]
    public void CatalogCodeSetsTranscode(int source, int target)
    {
        var p = new TestPlatform(new TestState()); byte[] image = Fixture.Catalog([(1u, "h\u00e4m\u00e4r\u00e4")], codeSet: (uint)source);
        uint c = CatalogCore.Read(ref p, p.Put(image), (uint)image.Length, p.Text("n"), 0, 0, (uint)target);
        Assert.NotEqual(0u, c); Assert.Equal((uint)target, p.Read32(c + 20));
        uint text = CatalogCore.GetString(ref p, c, 1, 0), at = text;
        var chars = new List<char>(); uint code;
        do { Assert.True(CharsetCore.Read(ref p, ref at, at + 4, (uint)target, out code)); if (code != 0) chars.Add((char)code); } while (code != 0);
        Assert.Equal("h\u00e4m\u00e4r\u00e4", new string(chars.ToArray()));
    }
    [Fact]
    public void FailedDenseAllocationRetainsSparseIndex()
    {
        var s = new TestState(); var p = new TestPlatform(s); byte[] image = Fixture.Catalog([(1u, "one"), (2u, "two")]);
        uint f = p.Put(image), name = p.Text("n"); s.FailAllocation = s.AllocationCalls + 2;
        uint c = CatalogCore.Read(ref p, f, (uint)image.Length, name, 0, 0, 0);
        Assert.NotEqual(0u, c); Assert.Equal(0u, p.Read32(c + CatalogCore.Dense)); Assert.Equal("two", p.String(CatalogCore.GetString(ref p, c, 2, 0)));
    }
    [Fact]
    public void EveryTruncationIsRejectedAndDoesNotLeak()
    {
        byte[] image = Fixture.Catalog([(1u, "one"), (2u, "two")]);
        for (int n = 0; n < image.Length; n++)
        {
            var s = new TestState(); var p = new TestPlatform(s); uint f = p.Put(image), name = p.Text("n"); int before = s.Allocations.Count;
            Assert.Equal(0u, CatalogCore.Read(ref p, f, (uint)n, name, 0, 0, 0)); Assert.Equal(before, s.Allocations.Count);
        }
    }
    [Fact]
    public void CatalogVersionIsExactAndLanguageMustMatch()
    {
        var p = new TestPlatform(new TestState()); byte[] b = Fixture.Catalog([(1u, "one")]); uint f = p.Put(b), n = p.Text("n");
        Assert.Equal(0u, CatalogCore.Read(ref p, f, (uint)b.Length, n, 0, 6, 0));
        Assert.Equal(0u, CatalogCore.Read(ref p, f, (uint)b.Length, n, p.Text("german"), 7, 0));
    }
    [Fact]
    public void ServiceSelectsPreferredLanguageAndCachesUntilFlushed()
    {
        var s = new TestState(); var p = new TestPlatform(s); uint l = LocaleCore.Default(ref p); p.Write32(l + 8, p.Text("finnish"));
        s.Files["LOCALE:Catalogs/finnish/sys/c.catalog"] = Fixture.Catalog([(1u, "translated")]);
        uint head = 0, n = p.Text("sys/c.catalog"), c = CatalogService.Open(ref p, l, n, 0, ref head); Assert.NotEqual(0u, c);
        int loads = s.Loads; CatalogService.Close(ref p, c);
        Assert.Equal(c, CatalogService.Open(ref p, l, n, 0, ref head)); Assert.Equal(loads, s.Loads);
        CatalogService.Flush(ref p, ref head); Assert.NotEqual(0u, head);
        CatalogService.Close(ref p, c); CatalogService.Flush(ref p, ref head); Assert.Equal(0u, head);
    }
    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void MorphOsBuiltInsAreIndexedAndTranslationsWin(int type)
    {
        var s = new TestState(); var p = new TestPlatform(s); uint l = LocaleCore.Default(ref p), head = 0;
        uint data;
        if (type == 0) { data = p.Allocate(16); p.Write32(data, 1); p.Write32(data + 4, p.Text("one")); p.Write32(data + 8, 2); p.Write32(data + 12, p.Text("two")); }
        else { data = p.Allocate(20); p.Write32(data, 1); p.Write16(data + 4, 4); p.Write32(data + 6, 0x6f6e6500); p.Write32(data + 10, 2); p.Write16(data + 14, 4); p.Write32(data + 16, 0x74776f00); }
        uint descriptor = p.Allocate(12); p.Write32(descriptor, (uint)type); p.Write32(descriptor + 4, type == 0 ? 16u : 20u); p.Write32(descriptor + 8, data);
        uint tags = p.Allocate(16); p.Write32(tags, CatalogService.TagBase + 6); p.Write32(tags + 4, descriptor);
        uint c = CatalogService.Open(ref p, l, p.Text("x.catalog"), tags, ref head); Assert.NotEqual(0u, c);
        Assert.Equal("one", p.String(CatalogCore.GetString(ref p, c, 1, 0))); CatalogService.Close(ref p, c);
        p.Write32(l + 8, p.Text("finnish")); s.Files["PROGDIR:Catalogs/finnish/x.catalog"] = Fixture.Catalog([(1u, "translated"), (3u, "three")]);
        c = CatalogService.Open(ref p, l, p.Text("x.catalog"), tags, ref head);
        Assert.Equal("translated", p.String(CatalogCore.GetString(ref p, c, 1, 0)));
        Assert.Equal("two", p.String(CatalogCore.GetString(ref p, c, 2, 0)));
        Assert.Equal("three", p.String(CatalogCore.GetString(ref p, c, 3, 0)));
        CatalogService.Close(ref p, c); CatalogService.Flush(ref p, ref head); Assert.Equal(0u, head);
    }
}
