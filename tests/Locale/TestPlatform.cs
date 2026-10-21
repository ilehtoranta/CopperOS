using System.Buffers.Binary;
using System.Text;
using CopperOS.Locale;

namespace CopperOS.Locale.Tests;

public sealed class TestState
{
    public readonly byte[] Memory = new byte[16 * 1024 * 1024];
    public uint Next = 0x1000;
    public readonly Dictionary<uint, uint> Allocations = new();
    public readonly Dictionary<uint, uint> Literals = new();
    public readonly Dictionary<string, byte[]> Files = new(StringComparer.OrdinalIgnoreCase);
    public readonly List<string> FileRequests = new();
    public readonly List<uint> Output = new();
    public readonly Queue<uint> Input = new();
    public int Error, Reads, Loads, AllocationCalls;
    public uint AvailableLocations = 3;
    public int FailAllocation = -1;
    public uint Driver, DriverMask;
    public Func<uint, uint, uint, uint, uint, uint>? LanguageCall;
    public readonly List<(uint Hook, uint Locale, uint Character)> Hooks = new();
}

public struct TestPlatform(TestState state) : ILocalePlatform
{
    public readonly TestState State = state;
    public byte Read8(uint a) { State.Reads++; return State.Memory[a]; }
    public ushort Read16(uint a) { Assert.Equal(0u, a & 1); State.Reads++; return BinaryPrimitives.ReadUInt16BigEndian(State.Memory.AsSpan((int)a, 2)); }
    public uint Read32(uint a) { Assert.Equal(0u, a & 1); State.Reads++; return BinaryPrimitives.ReadUInt32BigEndian(State.Memory.AsSpan((int)a, 4)); }
    public void Write8(uint a, byte value) => State.Memory[a] = value;
    public void Write16(uint a, ushort v) { Assert.Equal(0u, a & 1); BinaryPrimitives.WriteUInt16BigEndian(State.Memory.AsSpan((int)a, 2), v); }
    public void Write32(uint a, uint v) { Assert.Equal(0u, a & 1); BinaryPrimitives.WriteUInt32BigEndian(State.Memory.AsSpan((int)a, 4), v); }
    public uint Allocate(uint bytes)
    {
        if (State.AllocationCalls++ == State.FailAllocation) return 0;
        uint a = State.Next; State.Next += (bytes + 3) & ~3u;
        State.Allocations.Add(a, bytes); Array.Clear(State.Memory, (int)a, (int)bytes); return a;
    }
    public void Free(uint a) { if (a != 0) Assert.True(State.Allocations.Remove(a), $"double/invalid free {a:x}"); }
    public uint Put(byte[] bytes) { uint a = Allocate((uint)bytes.Length); bytes.CopyTo(State.Memory.AsSpan((int)a)); return a; }
    public uint Text(string text) => Put(Encoding.Latin1.GetBytes(text + '\0'));
    public string String(uint a) => a == 0 ? "" : Encoding.Latin1.GetString(State.Memory.AsSpan((int)a, (int)LocaleMemory.Length(ref this, a)));
    public uint Literal(uint id)
    { if (!State.Literals.TryGetValue(id, out uint a)) { a = Text(LocaleText.Value(id)); State.Literals.Add(id, a); } return a; }
    public uint LoadFile(uint name, out uint bytes)
    {
        State.Loads++; string path = String(name); State.FileRequests.Add(path);
        if (!State.Files.TryGetValue(path, out var contents)) { bytes = 0; Error(205); return 0; }
        bytes = (uint)contents.Length; return Put(contents);
    }
    public void Error(int error) => State.Error = error;
    public int LastError() => State.Error;
    public uint CatalogLocations() => State.AvailableLocations;
    public uint OpenLanguage(uint name) => State.Driver;
    public void CloseLanguage(uint language) => Assert.Equal(State.Driver, language);
    public uint Language(uint language, uint function, uint d0, uint d1, uint a1, uint a2) =>
        function == uint.MaxValue ? State.DriverMask : State.LanguageCall!(function, d0, d1, a1, a2);
    public uint Hook(uint hook, uint locale, uint character)
    {
        State.Hooks.Add((hook, locale, character));
        if (hook == 2) return State.Input.Count != 0 ? State.Input.Dequeue() : 0;
        State.Output.Add(character); return 0;
    }
    public string Output => new(State.Output.Where(c => c != 0).Select(c => (char)c).ToArray());
}

internal static class Fixture
{
    public static byte[] Long(uint n) { var b = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(b, n); return b; }
    public static byte[] Chunk(string id, byte[] data) => Encoding.ASCII.GetBytes(id).Concat(Long((uint)data.Length)).Concat(data).Concat((data.Length & 1) != 0 ? new byte[1] : []).ToArray();
    public static byte[] Form(string type, params byte[][] chunks)
    { var data = Encoding.ASCII.GetBytes(type).Concat(chunks.SelectMany(c => c)).ToArray(); return Encoding.ASCII.GetBytes("FORM").Concat(Long((uint)data.Length)).Concat(data).ToArray(); }
    public static byte[] Catalog(IEnumerable<(uint Id, string Text)> entries, string language = "finnish", uint codeSet = 0, ushort version = 7, bool includeTerminator = true)
    {
        var data = new List<byte>();
        foreach (var e in entries)
        {
            byte[] text = (codeSet == 0 ? Encoding.Latin1 : codeSet == 1 ? Encoding.UTF8 : new UTF32Encoding(true, false)).GetBytes(e.Text + (includeTerminator ? "\0" : ""));
            // A classic catalog still needs a NUL in its padded record. Add
            // it to the declared length when there would be no spare padding.
            if (!includeTerminator && (text.Length & 3) == 0) text = text.Concat(new byte[codeSet == 2 ? 4 : 1]).ToArray();
            data.AddRange(Long(e.Id)); data.AddRange(Long((uint)text.Length)); data.AddRange(text);
            while ((data.Count & 3) != 0) data.Add(0);
        }
        return Form("CTLG", Chunk("FVER", Encoding.ASCII.GetBytes($"$VER: test.catalog {version}.2 (18.10.2026)\0")), Chunk("LANG", Encoding.ASCII.GetBytes(language + '\0')),
            Chunk("CSET", Long(codeSet).Concat(new byte[28]).ToArray()), Chunk("STRS", data.ToArray()));
    }
    public static byte[] Prefs(string language = "finnish", int gmt = -120)
    {
        var data = new byte[860];
        Encoding.ASCII.GetBytes("finland.country\0").CopyTo(data, 16); Encoding.ASCII.GetBytes(language + '\0').CopyTo(data, 48);
        Encoding.ASCII.GetBytes("english\0").CopyTo(data, 78); BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(348), gmt);
        Long(0x46494e00).CopyTo(data, 356 + 16); Long(358).CopyTo(data, 356 + 20);
        Encoding.ASCII.GetBytes("%d.%m.%Y\0").CopyTo(data, 356 + 105);
        Encoding.ASCII.GetBytes(",\0").CopyTo(data, 356 + 345); data[356 + 375] = 3;
        return Form("PREF", Chunk("LCLE", data));
    }
}
