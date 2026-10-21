using System.Text;

namespace CopperOS.Locale.Tests;

public class LocaleCoreTests
{
    [Fact]
    public void AllLocaleStringsAndAsciiOrdering()
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p);
        for (uint id = 1; id <= 51; id++) Assert.NotEqual(0u, LocaleCore.String(ref p, l, id));
        Assert.Equal(0u, LocaleCore.String(ref p, l, 0)); Assert.Equal(0u, LocaleCore.String(ref p, l, 52));
        Assert.True(CharacterCore.Compare(ref p, l, p.Text("["), p.Text("a"), -1, 0) > 0);
    }
    [Fact]
    public void MalformedPreferencesDoNotPublishOrLeak()
    {
        var p = new TestPlatform(new TestState()); uint d = LocaleCore.Default(ref p), name = p.Text("bad");
        p.State.Files["bad"] = Fixture.Form("PREF", Fixture.Chunk("LCLE", new byte[859]));
        int before = p.State.Allocations.Count; Assert.Equal(0u, LocaleCore.Open(ref p, name, d)); Assert.Equal(before, p.State.Allocations.Count);
    }
    [Fact]
    public void FormatterUsesMultiCharacterAndNonUniformGrouping()
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p), stream = p.Allocate(4), grouping = p.Put([3, 2, 0]);
        p.Write32(l + LocaleLayout.GroupSeparator, p.Text("::")); p.Write32(l + LocaleLayout.Grouping, grouping); p.Write32(stream, 123456789);
        FormatCore.Format(ref p, l, p.Text("%lU"), stream, 1); Assert.Equal("12::34::56::789", p.Output);
    }
    [Fact]
    public void DefaultRecordAndReferenceLifetime()
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p);
        Assert.Equal("united_states.country", p.String(p.Read32(l)));
        Assert.Equal("english.language", p.String(p.Read32(l + 4)));
        Assert.Equal(l, LocaleCore.Open(ref p, 0, l)); Assert.Equal(2u, p.Read32(l + LocaleLayout.References));
        LocaleCore.Close(ref p, 0); LocaleCore.Close(ref p, l); Assert.True(p.State.Allocations.ContainsKey(l));
        LocaleCore.Close(ref p, l); Assert.False(p.State.Allocations.ContainsKey(l));
    }
    [Fact]
    public void PreferenceCountryLayoutAndMissingDriverFallback()
    {
        var p = new TestPlatform(new TestState()); uint d = LocaleCore.Default(ref p);
        p.State.Files["locale.prefs"] = Fixture.Prefs(); uint l = LocaleCore.Open(ref p, p.Text("locale.prefs"), d);
        Assert.NotEqual(0u, l); Assert.Equal(0x46494e00u, p.Read32(l + LocaleLayout.Country)); Assert.Equal(358u, p.Read32(l + LocaleLayout.Telephone));
        Assert.Equal(-120, (int)p.Read32(l + LocaleLayout.GmtOffset)); Assert.Equal("%d.%m.%Y", p.String(p.Read32(l + LocaleLayout.Date)));
        Assert.Equal("finnish", p.String(p.Read32(l + 8))); Assert.Equal("english.language", p.String(p.Read32(l + 4)));
        Assert.Equal("January", p.String(LocaleCore.String(ref p, l, 15))); LocaleCore.Close(ref p, l);
    }
    [Fact]
    public void DriverMaskOverridesOnlySupportedOperations()
    {
        var s = new TestState { Driver = 0x800, DriverMask = 1 | 8 }; var p = new TestPlatform(s);
        uint translated = p.Text("tammikuu"); s.LanguageCall = (fn, value, _, _, _) => fn == 3 ? translated : value + 1;
        uint d = LocaleCore.Default(ref p); s.Files["l"] = Fixture.Prefs(); uint l = LocaleCore.Open(ref p, p.Text("l"), d);
        Assert.Equal(66u, CharacterCore.Operation(ref p, l, 0, 65)); Assert.Equal(65u, CharacterCore.Operation(ref p, l, 1, 97));
        Assert.Equal("tammikuu", p.String(LocaleCore.String(ref p, l, 15)));
        LocaleCore.Close(ref p, l);
    }
    [Fact]
    public void CharacterDomainAndAllClassificationRelations()
    {
        Assert.Equal(0x12345678u, CharacterCore.Lower(0x12345678)); Assert.Equal(0x12345678u, CharacterCore.Upper(0x12345678));
        Assert.Equal(0u, CharacterCore.Classify(0x100));
        for (uint c = 0; c < 256; c++)
        {
            uint f = CharacterCore.Classify(c);
            Assert.Equal((f & CharacterCore.Alpha) != 0, (f & (CharacterCore.LowerCase | CharacterCore.UpperCase)) != 0);
            if (c < 9 || c > 13) Assert.False((f & (CharacterCore.Control | CharacterCore.Printable)) == (CharacterCore.Control | CharacterCore.Printable));
            if ((f & CharacterCore.Hex) != 0) Assert.True((f & (CharacterCore.Alpha | CharacterCore.Digit)) != 0);
        }
        Assert.Equal(0xe4u, CharacterCore.Lower(0xc4)); Assert.Equal(0xc4u, CharacterCore.Upper(0xe4));
        Assert.Equal(0xdfu, CharacterCore.Upper(0xdf));
    }
    [Fact]
    public void ConvertedSortKeysAgreeWithComparisons()
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p), aKey = p.Allocate(1024), bKey = p.Allocate(1024);
        string[] strings = ["", "a", "A", "e", "\u00e9", "role", "r\u00f4le", "r\u00f4lex", "r\u00f4la", "a\x01", "aa", "\xff", "\x01a"];
        for (uint type = 0; type < 3; type++)
        foreach (var a in strings) foreach (var b in strings)
        {
            uint ap = p.Text(a), bp = p.Text(b);
            CharacterCore.Convert(ref p, l, ap, aKey, 1024, type); CharacterCore.Convert(ref p, l, bp, bKey, 1024, type);
            Assert.Equal(Math.Sign(CharacterCore.Compare(ref p, l, ap, bp, -1, type)), Math.Sign(string.CompareOrdinal(p.String(aKey), p.String(bKey))));
        }
        Assert.Equal(0, CharacterCore.Compare(ref p, l, p.Text("abc"), p.Text("abz"), 2, 0));
        Assert.Equal(0u, CharacterCore.Convert(ref p, l, p.Text("abc"), 0, 0, 0));
    }
    [Fact]
    public void GregorianCalendarMatchesEveryDateAcrossCenturyBoundary()
    {
        DateTime epoch = new(1978, 1, 1);
        for (int n = 0; n < 50000; n++)
        {
            DateTime expected = epoch.AddDays(n); var d = DateCore.Decode((uint)n, 1439, 2950);
            Assert.Equal((uint)expected.Year, d.Year); Assert.Equal((uint)expected.Month, d.Month); Assert.Equal((uint)expected.Day, d.Day);
            Assert.Equal((uint)expected.DayOfYear, d.YearDay); Assert.Equal((uint)expected.DayOfWeek, d.Weekday);
            Assert.True(DateCore.Encode(d, out uint result)); Assert.Equal((uint)n, result);
        }
        Assert.False(DateCore.Encode(new LocaleDate { Year = 2100, Month = 2, Day = 29 }, out _));
        Assert.True(DateCore.Encode(new LocaleDate { Year = 2000, Month = 2, Day = 29 }, out _));
    }
    [Fact]
    public void DateFormattingControlsAndHookContract()
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p), stamp = p.Allocate(12);
        DateCore.Format(ref p, l, p.Text("%A %a %B %b %d %e %j %w %U %W %Y %y %% %F %q %Q %n%t"), stamp, 1);
        Assert.Equal("Sunday Sun January Jan 01  1 001 0 01 00 1978 78 % 1978-01-01 0 12 \n\t", p.Output);
        Assert.Equal(0u, p.State.Output[^1]); Assert.All(p.State.Hooks, h => { Assert.Equal(l, h.Locale); Assert.Equal(1u, h.Hook); });
    }
    [Theory]
    [InlineData("29 Feb 2000 12:34:56 pm", true, 2000, 2, 29, 12)]
    [InlineData("29 Feb 2100 12:34:56 am", false, 0, 0, 0, 0)]
    [InlineData("31 Apr 2000 12:34:56 am", false, 0, 0, 0, 0)]
    [InlineData("01 Jan 1978 12:00:00 am", true, 1978, 1, 1, 0)]
    [InlineData("01 Jan 1978 13:00:00 am", false, 0, 0, 0, 0)]
    public void DateParserValidatesAndPublishesOnlyOnSuccess(string text, bool valid, int year, int month, int day, int hour)
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p), stamp = p.Allocate(12); p.Write32(stamp, 123);
        foreach (char c in text) p.State.Input.Enqueue(c);
        Assert.Equal(valid, DateCore.Parse(ref p, l, stamp, p.Text("%d %b %Y %I:%M:%S %p"), 2));
        if (valid) { var d = DateCore.Decode(p.Read32(stamp), p.Read32(stamp + 4), p.Read32(stamp + 8)); Assert.Equal((uint)year, d.Year); Assert.Equal((uint)month, d.Month); Assert.Equal((uint)day, d.Day); Assert.Equal((uint)hour, d.Hour); }
        else Assert.Equal(123u, p.Read32(stamp));
    }
    [Fact]
    public void FormatterWordLongBstrPositionGroupingAnd64Bit()
    {
        var p = new TestPlatform(new TestState()); uint l = LocaleCore.Default(ref p), stream = p.Allocate(32), str = p.Text("abcdef");
        p.Write16(stream, unchecked((ushort)-23)); p.Write32(stream + 2, 0x80000000); p.Write32(stream + 6, str);
        p.Write16(stream + 10, 0xabcd); p.Write16(stream + 12, 0xabcd); p.Write32(stream + 14, 1234567);
        p.Write32(stream + 18, uint.MaxValue); p.Write32(stream + 22, uint.MaxValue);
        uint next = FormatCore.Format(ref p, l, p.Text("%05d|%ld|%-8.3s|%x|%X|%lU|%llu"), stream, 1);
        Assert.Equal("-0023|-2147483648|abc     |ABCD|abcd|1,234,567|18446744073709551615", p.Output); Assert.Equal(stream + 26, next);
        p.State.Output.Clear();
        Assert.Equal(stream + 10, FormatCore.Format(ref p, l, p.Text("%3$s %1$d %2$ld %1$d"), stream, 1)); Assert.Equal("abcdef -23 -2147483648 -23", p.Output);
        p.State.Output.Clear(); Assert.Equal(stream, FormatCore.Format(ref p, l, 0, stream, 1)); Assert.Empty(p.State.Output);
        uint b = p.Allocate(8); p.Write8(b, 3); Encoding.ASCII.GetBytes("xyz").CopyTo(p.State.Memory.AsSpan((int)b + 1)); p.Write32(stream, b >> 2);
        FormatCore.Format(ref p, l, p.Text("%b"), stream, 1); Assert.Equal("xyz", p.Output);
    }
}
