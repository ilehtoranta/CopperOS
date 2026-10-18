using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcWindowKernelTests
{
    [Theory]
    [InlineData("rar", 7, 1)]
    [InlineData("cab", 20, 1)]
    [InlineData("ace", 14, 1)]
    [InlineData("arj", 50, 2)]
    [InlineData("lzh", 21, 1)]
    public void Fixed_headers_reuse_full_scratch_proof_across_windows(
        string type, int headerBytes, int mappingCalls)
    {
        int stride = 102401 - headerBytes;
        int marker = stride + 8;
        byte[] input = Input(type, 102500, marker);
        if (type == "lzh") input[stride + 20] = 0;
        var state = new Exe2ArcTestIoState(input) { ReadOffsets = [] };
        var io = new Exe2ArcTestIo(state);
        var status = Scan(type, ref io, state, out uint offset, out uint length);
        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal((uint)marker, offset);
        Assert.Equal(type == "cab" ? 36u : (uint)(input.Length - marker), length);
        Assert.Equal(2, state.Calls.Count(c => c.Operation == "Read"));
        var seeks = state.Calls.Where(c => c.Operation == "Seek").ToArray();
        Assert.Equal(-8, seeks[^2].Argument);
        Assert.Equal(0, seeks[^2].Mode);
        Assert.Equal(marker, seeks[^1].Argument);
        Assert.Equal(-1, seeks[^1].Mode);
        Assert.Equal(mappingCalls, state.MappingCalls);
        int firstByte = type == "ace" ? 7 : type == "lzh" ? 2 : 0;
        var expectedReads = Enumerable.Range(0, 102401 - headerBytes).Select(i => i + firstByte)
            .Concat(Enumerable.Range(0, 8).Select(i => i + firstByte)).ToList();
        expectedReads.AddRange(type switch
        {
            "cab" => new[] { 0, 1, 2, 3, 8, 9, 10, 11, 16, 17, 18, 19 }.Select(i => i + 8),
            "ace" => Enumerable.Range(7, 7).Select(i => i + 8),
            "arj" => Enumerable.Range(0, 14).Select(i => i + 8),
            "lzh" => new[] { 10, 11, 12, 14, 20 },
            _ => Enumerable.Range(0, 7).Select(i => i + 8)
        });
        Assert.Equal(expectedReads, state.ReadOffsets);
        Assert.Equal(expectedReads.Count, state.ByteReads);
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData("rar")]
    [InlineData("cab")]
    [InlineData("ace")]
    [InlineData("arj")]
    [InlineData("lzh")]
    public void Last_nonwrapping_scratch_span_admits_fixed_header_reads(string type)
    {
        var state = new Exe2ArcTestIoState(Input(type, 100, 8), uint.MaxValue - 102399);
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.Completed, Scan(type, ref io, state, out uint offset, out _));
        Assert.Equal(8u, offset);
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData("rar")]
    [InlineData("cab")]
    [InlineData("ace")]
    [InlineData("arj")]
    [InlineData("lzh")]
    public void Mapping_rejection_precedes_any_scan_read(string type)
    {
        var state = new Exe2ArcTestIoState(Input(type, 100, 8)) { RejectMapping = true };
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.InvalidBuffer, Scan(type, ref io, state, out uint offset, out uint length));
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        Assert.Equal(0, state.ByteReads);
        Assert.Empty(state.Calls);
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData("rar")]
    [InlineData("cab")]
    [InlineData("ace")]
    [InlineData("arj")]
    [InlineData("lzh")]
    public void Zero_offset_stops_before_a_later_valid_marker(string type)
    {
        byte[] input = Input(type, 100, 0);
        switch (type)
        {
            case "cab": Exe2ArcTestIoState.PutMarker(input, 40, true); break;
            case "ace": Exe2ArcTestIoState.PutAceMarker(input, 40); break;
            case "arj": Exe2ArcTestIoState.PutArjMarker(input, 40); break;
            case "lzh": Exe2ArcTestIoState.PutLzhMarker(input, 40); break;
            default: Exe2ArcTestIoState.PutMarker(input, 40, false); break;
        }
        var state = new Exe2ArcTestIoState(input);
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.OffsetZero, Scan(type, ref io, state, out uint offset, out uint length));
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        Assert.Equal(new[] { "Seek", "Read", "Seek" }, state.Calls.Select(c => c.Operation));
        state.AssertUnownedResourcesAndGuards();
    }

    private static byte[] Input(string type, int length, int offset) => type switch
    {
        "cab" => Exe2ArcTestIoState.InputWithMarker(true, length, offset),
        "ace" => Exe2ArcTestIoState.InputWithAceMarker(length, offset),
        "arj" => Exe2ArcTestIoState.InputWithArjMarker(length, offset),
        "lzh" => Exe2ArcTestIoState.InputWithLzhMarker(length, offset),
        _ => Exe2ArcTestIoState.InputWithMarker(false, length, offset)
    };

    private static Exe2ArcIoStatus Scan(string type, ref Exe2ArcTestIo io,
        Exe2ArcTestIoState state, out uint offset, out uint length)
    {
        uint size = (uint)state.InputBytes.Length;
        switch (type)
        {
            case "cab": return Exe2ArcForwardScanner.ScanCabinet(ref io, Exe2ArcTestIoState.Input, state.Scratch, 102400, size, out offset, out length, out _);
            case "ace": return Exe2ArcForwardScanner.ScanAce(ref io, Exe2ArcTestIoState.Input, state.Scratch, 102400, size, out offset, out length, out _);
            case "arj": return Exe2ArcForwardScanner.ScanArj(ref io, Exe2ArcTestIoState.Input, state.Scratch, 102400, size, out offset, out length, out _);
            case "lzh": return Exe2ArcForwardScanner.ScanLzh(ref io, Exe2ArcTestIoState.Input, state.Scratch, 102400, size, out offset, out length, out _);
            default: return Exe2ArcForwardScanner.ScanRar4(ref io, Exe2ArcTestIoState.Input, state.Scratch, 102400, size, out offset, out length, out _);
        }
    }
}
