using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcZipRecordExtractorTests
{
    [Fact]
    public void Local_data_central_and_eocd_records_are_streamed_and_rebased()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithZipRecords());
        state.Cursor = 10;
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcZipRecordExtractor.Extract(ref io,
            Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output, state.Scratch,
            102400, 160, 10, 10, out uint outputBytes, out var observation);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(101u, outputBytes);
        Assert.Equal(101, state.OutputBytes.Count);
        Assert.Equal(Exe2ArcIoStage.PayloadWrite, observation.Stage);
        var output = state.OutputBytes.ToArray();
        Assert.Equal([0x50, 0x4b, 3, 4], output[..4]);
        Assert.Equal([0xa1, 0xb2, 0xc3], output[30..33]);
        Assert.Equal([0x50, 0x4b, 1, 2], output[33..37]);
        Assert.Equal(10u, ReadLittleEndian(output, 75));
        Assert.Equal([0x50, 0x4b, 5, 6], output[79..83]);
        Assert.Equal(43u, ReadLittleEndian(output, 95));
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Unknown_record_stops_without_output_write()
    {
        byte[] input = Enumerable.Repeat((byte)0x55, 64).ToArray();
        input[0] = 0x99;
        input[1] = 0x88;
        input[2] = 0x77;
        input[3] = 0x66;
        var state = new Exe2ArcTestIoState(input);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcZipRecordExtractor.Extract(ref io,
            Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output, state.Scratch,
            102400, 64, 0, 0, out uint outputBytes, out _);

        Assert.Equal(Exe2ArcIoStatus.InvalidRecord, status);
        Assert.Equal(0u, outputBytes);
        Assert.Empty(state.OutputBytes);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Ctrl_c_before_zip_record_keeps_source_success_count_and_error()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithZipRecords())
        {
            Cursor = 10,
            BreakPending = true,
        };
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcZipRecordExtractor.Extract(ref io,
            Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output, state.Scratch,
            102400, 160, 10, 10, out uint outputBytes, out var observation);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(150u, outputBytes);
        Assert.Empty(state.OutputBytes);
        Assert.True(observation.BreakObserved);
        Assert.Equal((int)DOS.Error.Break, observation.BreakError);
        Assert.Equal(1, state.SetIoErrCalls);
        Assert.Equal((int)DOS.Error.Break, state.SetIoErrValue);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Ctrl_c_after_eocd_write_keeps_partial_output_and_break_error()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithZipRecords())
        {
            Cursor = 10,
            // Local (5 calls) + central (3) + EOCD read/tail/write (3).
            BreakAfterCallCount = 11,
        };
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcZipRecordExtractor.Extract(ref io,
            Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output, state.Scratch,
            102400, 160, 10, 10, out uint outputBytes, out var observation);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(150u, outputBytes);
        Assert.Equal(101, state.OutputBytes.Count);
        Assert.True(observation.BreakObserved);
        Assert.Equal((int)DOS.Error.Break, observation.BreakError);
        Assert.Equal(1, state.SetIoErrCalls);
        state.AssertUnownedResourcesAndGuards();
    }

    private static uint ReadLittleEndian(byte[] bytes, int offset) =>
        (uint)bytes[offset] |
        ((uint)bytes[offset + 1] << 8) |
        ((uint)bytes[offset + 2] << 16) |
        ((uint)bytes[offset + 3] << 24);
}
