using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcZipScannerTests
{
    [Fact]
    public void Backward_eocd_scan_returns_rebased_local_header_start()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithZipEocd(160, 120));
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcZipScanner.Scan(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 160, out uint offset, out uint length,
            out uint correction, out var observed);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(30u, offset);
        Assert.Equal(130u, length);
        Assert.Equal(30u, correction);
        Assert.Equal(30, state.Cursor);
        Assert.Equal(Exe2ArcIoStage.PayloadSeek, observed.Stage);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Malformed_latest_eocd_stops_before_an_earlier_candidate()
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithZipEocd(230, 120,
            declaredCentralOffset: 74, localOffset: 30);
        Exe2ArcTestIoState.PutZipEocd(bytes, 200, declaredCentralOffset: 0,
            centralSize: 0);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcZipScanner.Scan(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 230, out uint offset, out uint length,
            out _, out _);

        Assert.Equal(Exe2ArcIoStatus.NoMatch, status);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Final_seek_failure_retains_zip_match_but_blocks_copy()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithZipEocd(160, 120));
        state.Faults.Add(("Seek", 4), (-1, 219));
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcZipScanner.Scan(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 160, out uint offset, out uint length,
            out uint correction, out var observed);

        Assert.Equal(Exe2ArcIoStatus.MatchedNotPositioned, status);
        Assert.Equal(30u, offset);
        Assert.Equal(130u, length);
        Assert.Equal(30u, correction);
        Assert.True(observed.IoErrCaptured);
        Assert.Equal(219, observed.IoError);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Wrapped_central_offset_is_rejected_without_followup_read()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithZipEocd(160, 120,
                declaredCentralOffset: 100));
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcZipScanner.Scan(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 160, out _, out _, out _, out _);

        Assert.Equal(Exe2ArcIoStatus.NoMatch, status);
        Assert.Equal(2, state.Calls.Count(call => call.Operation == "Seek"));
        Assert.Equal(1, state.Calls.Count(call => call.Operation == "Read"));
        state.AssertUnownedResourcesAndGuards();
    }
}
