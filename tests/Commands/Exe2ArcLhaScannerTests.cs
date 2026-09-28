using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcLhaScannerTests
{
    [Fact]
    public void Valid_hunk_sfx_start_is_repositioned_twice_as_in_source()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithLhaSfx(200, 100));
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcLhaScanner.Scan(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 200, out uint offset, out uint length,
            out var observed);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(100u, offset);
        Assert.Equal(100u, length);
        Assert.Equal(100, state.Cursor);
        Assert.Equal(["Read", "Seek", "Seek"],
            state.Calls.Select(call => call.Operation));
        Assert.Equal(Exe2ArcIoStage.PayloadSeek, observed.Stage);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Positive_short_probe_is_rejected_before_signature_reads()
    {
        var state = new Exe2ArcTestIoState(new byte[99]);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcLhaScanner.Scan(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 99, out uint offset, out uint length,
            out var observed);

        Assert.Equal(Exe2ArcIoStatus.IoStopped, status);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        Assert.Equal(0, state.ByteReads);
        Assert.Equal(Exe2ArcIoStage.WindowRead, observed.Stage);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Out_of_file_start_is_rejected_without_seeking()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithLhaSfx(200, 200));
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcLhaScanner.Scan(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 200, out uint offset, out uint length,
            out _);

        Assert.Equal(Exe2ArcIoStatus.NoMatch, status);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        Assert.Single(state.Calls);
        Assert.Equal("Read", state.Calls[0].Operation);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Final_seek_failure_retains_match_but_blocks_copy()
    {
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithLhaSfx(200, 100));
        state.Faults.Add(("Seek", 2), (-1, 219));
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcLhaScanner.Scan(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 200, out uint offset, out uint length,
            out var observed);

        Assert.Equal(Exe2ArcIoStatus.MatchedNotPositioned, status);
        Assert.Equal(100u, offset);
        Assert.Equal(100u, length);
        Assert.True(observed.IoErrCaptured);
        Assert.Equal(219, observed.IoError);
        state.AssertUnownedResourcesAndGuards();
    }
}
