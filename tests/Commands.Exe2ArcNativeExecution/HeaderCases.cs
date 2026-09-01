using System.Buffers.Binary;
using Copper68k;

namespace CopperOS.Commands.Exe2ArcNativeExecution;

internal sealed record HeaderCase(string Id, uint Format, byte[] HeaderBytes,
    uint WindowBytes, uint FileOffset, uint FileLength,
    bool Found, uint PayloadLength, uint MappingCalls, uint ReadCalls, uint ReadMask)
{
    public uint? ArgumentAddress { get; init; }
    public uint? MappedBytes { get; init; }
    public bool MappingEnabled { get; init; } = true;
    public bool AtPhysicalAddressLimit { get; init; }
    public uint StackBytes { get; init; } = 4096;
}

internal static class HeaderCases
{
    internal const string Suite = "exe2arc-header-components";
    // Each single is an actual public-method invocation; interleaved batches
    // count both callers. Expected counts are checked before and after runs.
    internal const int ExpectedInvocations = 76;

    public static List<HeaderCase[]> All(M68kCpuModel model)
    {
        var batches = new List<HeaderCase[]>();
        void One(HeaderCase test) => batches.Add([test]);
        var rar = Rar("EXA-HR01.minimum", 1, 9, 8);
        var cab = Cab("EXA-HC01.minimum", 21, 0, 21, true);
        One(rar);
        One(Rar("EXA-HR02.length", 123, 4096, 3973));
        One(Rar("EXA-HR03.window-location-only", 102397, 204811, 102414));
        One(Rar("EXA-HR04.high-offset", 0xfffffff0, uint.MaxValue, 15));
        One(Rar("EXA-HR05.high-remaining", 1, uint.MaxValue, 0xfffffffe));
        for (int index = 0; index < 7; index++)
        {
            byte[] marker = RarMarker();
            marker[index] ^= 0x80;
            One(rar with { Id = $"EXA-HR06.marker-{index}", HeaderBytes = marker,
                Found = false, PayloadLength = 0, ReadCalls = (uint)index + 1,
                ReadMask = (1u << (index + 1)) - 1 });
        }
        One(rar with { Id = "EXA-HR07.rar5", HeaderBytes = [0x52, 0x61, 0x72,
            0x21, 0x1a, 0x07, 0x01, 0x00], WindowBytes = 8, FileLength = 100,
            Found = false, PayloadLength = 0 });
        One(cab);
        One(Cab("EXA-HC02.trailing-bytes", 21, 20, 30, true));
        One(Cab("EXA-HC03.table-equals-length", 21, 21, 30, false));
        One(Cab("EXA-HC04.length-past-eof", 22, 0, 21, false));
        One(Cab("EXA-HC05.zero-length", 0, 0, 30, false));
        One(Cab("EXA-HC06.predicate-not-integrity", 1, 0, 30, true));
        One(Cab("EXA-HC07.little-endian", 0x12345678, 0x01234567, 0x20000000, true));
        One(Cab("EXA-HC08.high-length", 0x80000000, 0x7fffffff, 0x80000001, true));
        One(Cab("EXA-HC09.max-length-too-large", uint.MaxValue, 0, 0xfffffffe, false));
        One(Cab("EXA-HC10.high-table", 100, uint.MaxValue, 1000, false));
        One(Cab("EXA-HC11.max-fitting-length", 0xfffffffe, 0xfffffffd, 0xfffffffe, true));
        for (int index = 0; index < 4; index++)
        {
            byte[] marker = Cabinet(24, 20);
            marker[index] ^= 0x20;
            One(cab with { Id = $"EXA-HC12.marker-{index}", HeaderBytes = marker,
                FileLength = 100, Found = false, PayloadLength = 0,
                ReadCalls = (uint)index + 1, ReadMask = (1u << (index + 1)) - 1 });
        }
        foreach (var valid in new[] { rar, cab })
        {
            var prefix = valid.Format == 0 ? "EXA-HB.RAR" : "EXA-HB.CAB";
            var rejected = valid with { Found = false, PayloadLength = 0,
                MappingCalls = 0, ReadCalls = 0, ReadMask = 0 };
            One(rejected with { Id = prefix + ".threshold-minus-one", FileLength = valid.WindowBytes });
            One(rejected with { Id = prefix + ".threshold-equal", FileLength = valid.WindowBytes + 1 });
            One(rejected with { Id = prefix + ".empty-window", WindowBytes = 0 });
            One(rejected with { Id = prefix + ".short-window", WindowBytes = valid.WindowBytes - 1 });
            One(rejected with { Id = prefix + ".zero-offset", FileOffset = 0 });
            One(rejected with { Id = prefix + ".offset-equals-file", FileOffset = valid.FileLength });
            One(rejected with { Id = prefix + ".offset-past-file", FileOffset = valid.FileLength + 1 });
            One(rejected with { Id = prefix + ".empty-file", FileLength = 0 });
            One(rejected with { Id = prefix + ".null-header", ArgumentAddress = 0 });
            One(rejected with { Id = prefix + ".uint-wrap-one-byte",
                ArgumentAddress = uint.MaxValue - valid.WindowBytes + 2 });
            One(rejected with { Id = prefix + ".huge-window", WindowBytes = uint.MaxValue,
                FileLength = uint.MaxValue });
            One(rejected with { Id = prefix + ".window-past-eof", WindowBytes = 101, FileLength = 100 });
            One(rejected with { Id = prefix + ".mapping-denied", MappingEnabled = false, MappingCalls = 1 });
            One(rejected with { Id = prefix + ".mapping-short", MappedBytes = valid.WindowBytes - 1, MappingCalls = 1 });
            One(rejected with { Id = prefix + ".before-borrowed-header", ArgumentAddress = 0x30000, MappingCalls = 1 });
            One(rejected with { Id = prefix + ".inside-short-borrowed-tail", ArgumentAddress = 0x30002, MappingCalls = 1 });
            One(valid with { Id = prefix + ".final-physical-byte", AtPhysicalAddressLimit = true,
                StackBytes = 16384 });
        }
        // Independent header storage: extra borrowed bytes must remain unread.
        One(rar with { Id = "EXA-HR08.extra-window", HeaderBytes =
            [0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x00, 0xd3], WindowBytes = 8 });
        One(cab with { Id = "EXA-HC13.extra-window", HeaderBytes = [.. cab.HeaderBytes, 0xd3],
            WindowBytes = 21 });
        for (int round = 0; round < 3; round++)
        {
            batches.Add([
                Rar($"EXA-HI.{round}.rar", 8, 100, 92),
                Cab($"EXA-HI.{round}.cab", 36, 30, 100, true) with { StackBytes = 16384 }
            ]);
            batches.Add([
                rar with { Id = $"EXA-HI.{round}.rejected-rar", ArgumentAddress = 0,
                    Found = false, PayloadLength = 0, MappingCalls = 0, ReadCalls = 0, ReadMask = 0 },
                Cab($"EXA-HI.{round}.small-cab-predicate", 1, 0, 30, true) with { StackBytes = 16384 }
            ]);
        }
        HeaderTestBus.Require(batches.Sum(b => b.Length) == ExpectedInvocations,
            "Declared header case count changed; update the finite protocol explicitly.");
        HeaderTestBus.Require(batches.SelectMany(b => b).Select(t => t.Id).Distinct().Count() == ExpectedInvocations,
            "Repeated header fixture ID.");
        _ = model; // Address selection is explicit in HeaderInvocation, below.
        return batches;
    }

    private static HeaderCase Rar(string id, uint offset, uint length, uint expected) =>
        new(id, 0, RarMarker(), 7, offset, length, true, expected, 1, 7, 0x7f);

    private static HeaderCase Cab(string id, uint length, uint table, uint remaining, bool found) =>
        new(id, 1, Cabinet(length, table), 20, 1, checked(remaining + 1),
            found, found ? length : 0, 1, 12, 0x000f0f0f);

    private static byte[] RarMarker() => [0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x00];
    private static byte[] Cabinet(uint length, uint table)
    {
        // Reserved bytes intentionally are not all zero: this is the recorded
        // Exe2Arc predicate, not a standards-conforming complete CAB archive.
        byte[] bytes = Enumerable.Repeat((byte)0xa5, 20).ToArray();
        "MSCF"u8.CopyTo(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), table);
        return bytes;
    }
}
