using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcScannerSelectionTests
{
    [Fact]
    public void Unspecified_type_uses_source_order_even_when_cab_precedes_rar()
    {
        var bytes = Enumerable.Repeat((byte)0x55, 120).ToArray();
        Exe2ArcTestIoState.PutMarker(bytes, 12, false);
        Exe2ArcTestIoState.PutMarker(bytes, 40, true, 36, 20);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcScannerSelection.Scan(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, (uint)bytes.Length,
            Exe2ArcArchiveType.Unspecified, out var selected,
            out var offset, out var length, out var correction, out _);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(Exe2ArcArchiveType.Rar, selected);
        Assert.Equal(12u, offset);
        Assert.Equal((uint)bytes.Length - 12, length);
        Assert.Equal(0u, correction);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Explicit_type_bypasses_other_matching_scanners()
    {
        var bytes = Enumerable.Repeat((byte)0x55, 120).ToArray();
        Exe2ArcTestIoState.PutMarker(bytes, 12, false);
        Exe2ArcTestIoState.PutMarker(bytes, 40, true, 36, 20);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcScannerSelection.Scan(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, (uint)bytes.Length,
            Exe2ArcArchiveType.Cabinet, out var selected,
            out var offset, out var length, out _, out _);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(Exe2ArcArchiveType.Cabinet, selected);
        Assert.Equal(40u, offset);
        Assert.Equal(36u, length);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Offset_zero_is_terminal_and_does_not_fall_through()
    {
        var bytes = Enumerable.Repeat((byte)0x55, 120).ToArray();
        Exe2ArcTestIoState.PutMarker(bytes, 0, false);
        Exe2ArcTestIoState.PutMarker(bytes, 40, true, 36, 20);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcScannerSelection.Scan(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, (uint)bytes.Length,
            Exe2ArcArchiveType.Unspecified, out var selected,
            out var offset, out var length, out _, out _);

        Assert.Equal(Exe2ArcIoStatus.OffsetZero, status);
        Assert.Equal(Exe2ArcArchiveType.Rar, selected);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void No_match_clears_selection_after_all_scanners()
    {
        var bytes = Enumerable.Repeat((byte)0x55, 120).ToArray();
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcScannerSelection.Scan(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, (uint)bytes.Length,
            Exe2ArcArchiveType.Unspecified, out var selected,
            out var offset, out var length, out var correction, out _);

        Assert.Equal(Exe2ArcIoStatus.NoMatch, status);
        Assert.Equal(Exe2ArcArchiveType.Unspecified, selected);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        Assert.Equal(0u, correction);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Initial_seek_failure_stops_the_first_table_entry()
    {
        var bytes = Enumerable.Repeat((byte)0x55, 120).ToArray();
        var state = new Exe2ArcTestIoState(bytes);
        state.Faults[("Seek", 1)] = (-1, 205);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcScannerSelection.Scan(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, (uint)bytes.Length,
            Exe2ArcArchiveType.Unspecified, out var selected,
            out var offset, out var length, out _, out var observation);

        Assert.Equal(Exe2ArcIoStatus.IoStopped, status);
        Assert.Equal(Exe2ArcArchiveType.Zip, selected);
        Assert.Equal(205, observation.IoError);
        Assert.True(observation.IoErrCaptured);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        state.AssertUnownedResourcesAndGuards();
    }
}
