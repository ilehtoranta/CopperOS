using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcPayloadCopyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(102399)]
    [InlineData(102400)]
    [InlineData(102401)]
    [InlineData(204813)]
    public void Exact_chunks_copy_only_selected_length_without_retry_or_seek(int length)
    {
        byte[] input = Enumerable.Range(0, length + 17).Select(index => (byte)(index * 29)).ToArray();
        var state = new Exe2ArcTestIoState(input) { Cursor = 7 };
        var io = new Exe2ArcTestIo(state);
        var result = Exe2ArcPayloadCopy.Copy(ref io, Exe2ArcTestIoState.Input,
            Exe2ArcTestIoState.Output, state.Scratch, 102400, (uint)length, out var observed);
        Assert.Equal(Exe2ArcIoStatus.Completed, result);
        Assert.Equal(input.AsSpan(7, length).ToArray(), state.OutputBytes);
        Assert.Equal(7 + length, state.Cursor);
        Assert.Equal((uint)length, observed.BytesCompleted);
        Assert.Equal(length != 0, observed.HasResult);
        Assert.False(observed.IoErrCaptured);
        Assert.Equal(0, state.IoErrCalls);
        var counts = state.Calls.Where(c => c.Operation == "Read").Select(c => c.Argument).ToArray();
        Assert.Equal((length + 102399) / 102400, counts.Length);
        Assert.Equal(length, counts.Sum());
        Assert.All(counts.SkipLast(1), count => Assert.Equal(102400, count));
        Assert.Equal(counts, state.Calls.Where(c => c.Operation == "Write").Select(c => c.Argument));
        Assert.Equal(Enumerable.Range(0, counts.Length).SelectMany(_ => new[] { "Read", "Write" }),
            state.Calls.Select(c => c.Operation));
        if (length != 0)
        {
            Assert.Equal(Exe2ArcIoStage.PayloadWrite, observed.Stage);
            Assert.Equal(counts[^1], observed.RawResult);
            Assert.Equal((uint)counts[^1], observed.RequestedBytes);
        }
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData("Read", 1, -1, 205)]
    [InlineData("Read", 1, 0, 0)]
    [InlineData("Read", 1, 3, 0)]
    [InlineData("Write", 1, -1, 221)]
    [InlineData("Write", 1, 0, 0)]
    [InlineData("Write", 1, 3, 0)]
    [InlineData("Read", 2, -1, 0)]
    [InlineData("Read", 2, 0, 0)]
    [InlineData("Read", 2, 3, 0)]
    [InlineData("Write", 2, -1, 221)]
    [InlineData("Write", 2, 0, 0)]
    [InlineData("Write", 2, 3, 0)]
    public void EXA_F08_short_or_negative_IO_stops_with_confirmed_output_count(
        string operation, int occurrence, int raw, int error)
    {
        byte[] bytes = Enumerable.Range(0, 102413).Select(index => (byte)(index * 37)).ToArray();
        var state = new Exe2ArcTestIoState(bytes);
        state.Faults.Add((operation, occurrence), (raw, error));
        var io = new Exe2ArcTestIo(state);
        var result = Exe2ArcPayloadCopy.Copy(ref io, Exe2ArcTestIoState.Input,
            Exe2ArcTestIoState.Output, state.Scratch, 102400, (uint)bytes.Length, out var observed);

        uint full = occurrence == 1 ? 0u : 102400u;
        uint confirmed = full + (operation == "Write" && raw > 0 ? (uint)raw : 0u);
        Assert.Equal(Exe2ArcIoStatus.IoStopped, result);
        Assert.Equal(confirmed, observed.BytesCompleted);
        Assert.Equal(bytes.Take((int)confirmed), state.OutputBytes);
        Assert.Equal(operation == "Read" ? Exe2ArcIoStage.PayloadRead : Exe2ArcIoStage.PayloadWrite, observed.Stage);
        Assert.Equal(raw, observed.RawResult);
        Assert.Equal(occurrence == 1 ? 102400u : 13u, observed.RequestedBytes);
        Assert.Equal(raw == -1, observed.IoErrCaptured);
        Assert.Equal(raw == -1 ? error : 0, observed.IoError);
        Assert.Equal(raw == -1 ? 1 : 0, state.IoErrCalls);
        Assert.Equal(occurrence, state.Calls.Count(c => c.Operation == "Read"));
        Assert.Equal(operation == "Read" ? occurrence - 1 : occurrence,
            state.Calls.Count(c => c.Operation == "Write"));
        Assert.DoesNotContain(state.Calls, c => c.Operation == "Seek");
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Zero_length_does_not_require_or_touch_unused_handles_and_storage()
    {
        var state = new Exe2ArcTestIoState([]) { RejectMapping = true };
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.Completed, Exe2ArcPayloadCopy.Copy(ref io,
            BPTR.Null, BPTR.Null, APTR.Null, 0, 0, out var observed));
        Assert.False(observed.HasResult);
        Assert.Equal(0u, observed.BytesCompleted);
        Assert.Empty(state.Calls);
        Assert.Equal(0, state.MappingCalls);
    }

    [Theory]
    [InlineData("Read", 5)]
    [InlineData("Read", -2)]
    [InlineData("Write", 5)]
    [InlineData("Write", -2)]
    public void Defensive_invalid_provider_counts_stop_without_inventing_DOS_error_or_completed_bytes(
        string operation, int raw)
    {
        // These are deliberately invalid provider returns, not ordinary DOS
        // behavior or new claims about an original executable.
        var state = new Exe2ArcTestIoState(new byte[100]);
        state.Faults.Add((operation, 1), (raw, 12345));
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.IoStopped, Exe2ArcPayloadCopy.Copy(ref io,
            Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output, state.Scratch,
            102400, 4, out var observed));
        Assert.Equal(raw, observed.RawResult);
        Assert.Equal(4u, observed.RequestedBytes);
        Assert.Equal(0u, observed.BytesCompleted);
        Assert.False(observed.IoErrCaptured);
        Assert.Equal(0, state.IoErrCalls);
        Assert.Empty(state.OutputBytes);
        Assert.Equal(operation == "Read" ? 1 : 2, state.Calls.Count);
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData("null", 0u, 102400u, false)]
    [InlineData("capacity", 0x1001u, 102399u, false)]
    [InlineData("wrap", 0xffff0000u, 102400u, false)]
    [InlineData("unmapped", 0x1001u, 102400u, true)]
    public void Invalid_scratch_stops_before_any_transfer(string fixture,
        uint address, uint capacity, bool unmapped)
    {
        Assert.NotEmpty(fixture);
        var state = new Exe2ArcTestIoState(new byte[100], address) { RejectMapping = unmapped };
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.InvalidBuffer, Exe2ArcPayloadCopy.Copy(ref io,
            Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output,
            state.Scratch, capacity, 100, out var observed));
        Assert.False(observed.HasResult);
        Assert.Empty(state.Calls);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Unsupported_length_and_null_handles_are_admission_failures_not_command_return_policies()
    {
        var state = new Exe2ArcTestIoState(new byte[100]);
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.UnsupportedRange, Exe2ArcPayloadCopy.Copy(ref io,
            Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output, state.Scratch,
            102400, 0x80000000, out _));
        Assert.Equal(Exe2ArcIoStatus.InvalidHandle, Exe2ArcPayloadCopy.Copy(ref io,
            BPTR.Null, Exe2ArcTestIoState.Output, state.Scratch, 102400, 100, out _));
        Assert.Equal(Exe2ArcIoStatus.InvalidHandle, Exe2ArcPayloadCopy.Copy(ref io,
            Exe2ArcTestIoState.Input, BPTR.Null, state.Scratch, 102400, 100, out _));
        Assert.Empty(state.Calls);
        Assert.Equal(0, state.MappingCalls);
    }
}
