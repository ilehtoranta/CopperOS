using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcForwardScannerTests
{
    [Theory]
    [InlineData("EXA-ACE.small", 100, 8, true, 1)]
    [InlineData("EXA-ACE.eof-last-first-window", 102400, 102386, true, 1)]
    [InlineData("EXA-ACE.eof-at-next-window", 102401, 102387, false, 1)]
    [InlineData("EXA-ACE.eof-inside-next-window", 102402, 102388, true, 2)]
    public void Ace_source_window_matrix(string fixture, int fileLength, int offset,
        bool expectedMatch, int expectedWindows)
    {
        Assert.NotEmpty(fixture);
        var state = new Exe2ArcTestIoState(Exe2ArcTestIoState.InputWithAceMarker(
            fileLength, offset));
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcForwardScanner.ScanAce(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, (uint)fileLength, out uint archiveOffset,
            out uint payloadLength, out var observed);

        Assert.Equal(expectedMatch ? Exe2ArcIoStatus.Completed : Exe2ArcIoStatus.NoMatch,
            status);
        Assert.Equal(expectedMatch ? (uint)offset : 0u, archiveOffset);
        Assert.Equal(expectedMatch ? (uint)(fileLength - offset) : 0u, payloadLength);
        Assert.Equal(expectedWindows, state.Calls.Count(call => call.Operation == "Read"));
        Assert.Equal(expectedWindows != 0, observed.HasResult);
        if (expectedMatch)
        {
            int stride = 102387;
            int windowStart = (expectedWindows - 1) * stride;
            int candidateIndex = offset - windowStart;
            var seeks = state.Calls.Where(call => call.Operation == "Seek").ToArray();
            Assert.Equal(-candidateIndex, seeks[^2].Argument);
            Assert.Equal(0, seeks[^2].Mode);
            Assert.Equal(offset, seeks[^1].Argument);
            Assert.Equal(-1, seeks[^1].Mode);
            Assert.Equal(offset, state.Cursor);
            Assert.Equal(Exe2ArcIoStage.PayloadSeek, observed.Stage);
        }
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Ace_zero_offset_stops_before_a_later_marker()
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithAceMarker(100, 0);
        Exe2ArcTestIoState.PutAceMarker(bytes, 40);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcForwardScanner.ScanAce(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, 100, out uint offset, out uint length, out var observed);

        Assert.Equal(Exe2ArcIoStatus.OffsetZero, status);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        Assert.Equal(["Seek", "Read", "Seek"], state.Calls.Select(x => x.Operation));
        Assert.Equal(Exe2ArcIoStage.CandidateSeek, observed.Stage);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Arj_valid_crc_returns_payload_to_eof()
    {
        const int fileLength = 100;
        const int marker = 8;
        var state = new Exe2ArcTestIoState(
            Exe2ArcTestIoState.InputWithArjMarker(fileLength, marker));
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcForwardScanner.ScanArj(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, fileLength,
            out uint offset, out uint length, out var observed);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal((uint)marker, offset);
        Assert.Equal((uint)(fileLength - marker), length);
        Assert.Equal(marker, state.Cursor);
        Assert.Equal(Exe2ArcIoStage.PayloadSeek, observed.Stage);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Arj_bad_crc_is_skipped_before_a_later_valid_candidate()
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithArjMarker(140, 8,
            validCrc: false);
        Exe2ArcTestIoState.PutArjMarker(bytes, 60);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcForwardScanner.ScanArj(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, 140,
            out uint offset, out uint length, out _);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(60u, offset);
        Assert.Equal(80u, length);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Arj_truncated_crc_tail_is_rejected_without_reading_past_window()
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithArjMarker(56, 1,
            headerLength: 44);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcForwardScanner.ScanArj(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, 52,
            out uint offset, out uint length, out _);

        Assert.Equal(Exe2ArcIoStatus.NoMatch, status);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Lzh_uses_the_source_scan_window_level_byte()
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithLzhMarker(100, 8);
        bytes[28] = 3; // candidate byte +20 is ignored by the source predicate.
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcForwardScanner.ScanLzh(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, 100,
            out uint offset, out uint length, out _);

        Assert.Equal(Exe2ArcIoStatus.Completed, status);
        Assert.Equal(8u, offset);
        Assert.Equal(92u, length);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Lzh_rejects_a_high_level_window_and_does_not_skip_to_later_marker()
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithLzhMarker(100, 8, level: 3);
        Exe2ArcTestIoState.PutLzhMarker(bytes, 40);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var status = Exe2ArcForwardScanner.ScanLzh(ref io,
            Exe2ArcTestIoState.Input, state.Scratch, 102400, 100,
            out uint offset, out uint length, out _);

        Assert.Equal(Exe2ArcIoStatus.NoMatch, status);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData("EXA-S04.small", false, 100, 32, true, 1)]
    [InlineData("EXA-S05.small", true, 100, 32, true, 1)]
    [InlineData("EXA-S04.eof", false, 8, 1, true, 1)]
    [InlineData("EXA-S05.eof", true, 21, 1, true, 1)]
    [InlineData("EXA-S13.rar.eof-last-first-window", false, 102400, 102393, true, 1)]
    [InlineData("EXA-S13.rar.eof-at-next-window", false, 102401, 102394, false, 1)]
    [InlineData("EXA-S13.rar.eof-inside-next-window", false, 102402, 102395, true, 2)]
    [InlineData("EXA-S13.cab.eof-last-first-window", true, 102400, 102380, true, 1)]
    [InlineData("EXA-S13.cab.eof-at-next-window", true, 102401, 102381, false, 1)]
    [InlineData("EXA-S13.cab.eof-inside-next-window", true, 102402, 102382, true, 2)]
    [InlineData("EXA-S13.rar.crossing", false, 102430, 102398, true, 2)]
    [InlineData("EXA-S13.cab.crossing", true, 102430, 102390, true, 2)]
    [InlineData("EXA-S13.rar.third-window", false, 205100, 205000, true, 3)]
    [InlineData("EXA-S13.cab.third-window", true, 205100, 205000, true, 3)]
    [InlineData("EXA-F04.rar.empty", false, 0, -1, false, 0)]
    [InlineData("EXA-F04.rar.six", false, 6, -1, false, 0)]
    [InlineData("EXA-F04.rar.seven", false, 7, -1, false, 0)]
    [InlineData("EXA-F04.rar.eight", false, 8, -1, false, 1)]
    [InlineData("EXA-F04.cab.empty", true, 0, -1, false, 0)]
    [InlineData("EXA-F04.cab.nineteen", true, 19, -1, false, 0)]
    [InlineData("EXA-F04.cab.twenty", true, 20, -1, false, 0)]
    [InlineData("EXA-F04.cab.twenty-one", true, 21, -1, false, 1)]
    [InlineData("EXA-F04.rar.multiple-windows", false, 205100, -1, false, 3)]
    [InlineData("EXA-F04.cab.multiple-windows", true, 205100, -1, false, 3)]
    public void Source_window_matrix(string fixture, bool cabinet, int fileLength,
        int marker, bool expectedMatch, int expectedWindows)
    {
        Assert.NotEmpty(fixture);
        // CAB length1/table0 deliberately isolates source predicate acceptance
        // from archive integrity, including the exact20-byte EOF cases.
        var state = new Exe2ArcTestIoState(Exe2ArcTestIoState.InputWithMarker(
            cabinet, fileLength, marker, 1, 0));
        var io = new Exe2ArcTestIo(state);
        Exe2ArcIoStatus status = Scan(cabinet, ref io, state, (uint)fileLength,
            out uint offset, out uint length, out var observed);

        Assert.Equal(expectedMatch ? Exe2ArcIoStatus.Completed : Exe2ArcIoStatus.NoMatch, status);
        Assert.Equal(expectedMatch ? (uint)marker : 0u, offset);
        Assert.Equal(expectedMatch ? cabinet ? 1u : (uint)(fileLength - marker) : 0u, length);
        Assert.Equal(expectedWindows, state.Calls.Count(call => call.Operation == "Read"));
        int stride = cabinet ? 102381 : 102394;
        var readCalls = state.Calls.Where(call => call.Operation == "Read").ToArray();
        for (int index = 0; index < expectedWindows; index++)
            Assert.Equal(Math.Min(102400, fileLength - index * stride), readCalls[index].Argument);
        var seeks = state.Calls.Where(call => call.Operation == "Seek").ToArray();
        for (int index = 0; index < expectedWindows; index++)
        {
            Assert.Equal(index * stride, seeks[index].Argument);
            Assert.Equal(-1, seeks[index].Mode);
        }
        if (expectedMatch)
        {
            int windowStart = (expectedWindows - 1) * stride;
            int candidateIndex = marker - windowStart;
            Assert.Equal(-candidateIndex, seeks[^2].Argument);
            Assert.Equal(0, seeks[^2].Mode);
            Assert.Equal(marker, seeks[^1].Argument);
            Assert.Equal(-1, seeks[^1].Mode);
            Assert.Equal(marker, state.Cursor);
            Assert.Equal(Exe2ArcIoStage.PayloadSeek, observed.Stage);
            Assert.Equal(seeks[^1].Result, observed.RawResult); // Seek returns OLD position.
        }
        else if (expectedWindows != 0)
            Assert.Equal(Exe2ArcIoStage.WindowRead, observed.Stage);
        Assert.Equal(expectedWindows != 0, observed.HasResult);
        Assert.Equal(0u, observed.BytesCompleted);
        Assert.False(observed.IoErrCaptured);
        Assert.Equal(0, state.IoErrCalls);
        Assert.Empty(state.OutputBytes);
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EXA_F05_recognized_offset_zero_stops_before_a_later_valid_marker(bool cabinet)
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithMarker(cabinet, 100, 0, 24, 20);
        Exe2ArcTestIoState.PutMarker(bytes, 40, cabinet, 24, 20);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        var result = Scan(cabinet, ref io, state, 100, out uint offset,
            out uint length, out var observed);

        Assert.Equal(Exe2ArcIoStatus.OffsetZero, result);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        Assert.Equal(["Seek", "Read", "Seek"], state.Calls.Select(x => x.Operation));
        Assert.Equal(0, state.Calls[^1].Argument);
        Assert.Equal(0, state.Calls[^1].Mode);
        Assert.Equal(100, state.Cursor);
        Assert.Equal(Exe2ArcIoStage.CandidateSeek, observed.Stage);
        Assert.Equal(cabinet ? 12 : 7, state.ByteReads);
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData(200u, 0u)]
    [InlineData(24u, 24u)]
    [InlineData(24u, uint.MaxValue)]
    [InlineData(0u, 0u)]
    public void EXA_F10_invalid_CAB_candidate_does_not_hide_later_valid_candidate(uint size, uint table)
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithMarker(true, 100, 1, size, table);
        Exe2ArcTestIoState.PutMarker(bytes, 40, true, 24, 20);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);

        Assert.Equal(Exe2ArcIoStatus.Completed, Scan(true, ref io, state, 100,
            out uint offset, out uint length, out _));
        Assert.Equal(40u, offset);
        Assert.Equal(24u, length);
        Assert.Equal(3, state.Calls.Count(c => c.Operation == "Seek"));
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void EXA_F10_RAR5_is_skipped_without_hiding_a_later_RAR4_marker()
    {
        byte[] bytes = Exe2ArcTestIoState.InputWithMarker(false, 100, 1);
        bytes[7] = 1;
        Exe2ArcTestIoState.PutMarker(bytes, 40, false);
        var state = new Exe2ArcTestIoState(bytes);
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.Completed, Scan(false, ref io, state, 100,
            out uint offset, out uint length, out _));
        Assert.Equal(40u, offset);
        Assert.Equal(60u, length);
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData("initial-seek", "Seek", 1, -1, 0, 100, 32, Exe2ArcIoStage.WindowSeek)]
    [InlineData("read-error", "Read", 1, -1, 205, 100, 32, Exe2ArcIoStage.WindowRead)]
    [InlineData("read-zero", "Read", 1, 0, 0, 100, 32, Exe2ArcIoStage.WindowRead)]
    [InlineData("read-short", "Read", 1, 99, 0, 100, 32, Exe2ArcIoStage.WindowRead)]
    [InlineData("candidate-seek", "Seek", 2, -1, 219, 100, 32, Exe2ArcIoStage.CandidateSeek)]
    [InlineData("final-seek", "Seek", 3, -1, 219, 100, 32, Exe2ArcIoStage.PayloadSeek)]
    [InlineData("second-window-seek", "Seek", 2, -1, 219, 102430, 102398, Exe2ArcIoStage.WindowSeek)]
    [InlineData("second-window-read", "Read", 2, 35, 0, 102430, 102398, Exe2ArcIoStage.WindowRead)]
    public void EXA_F08_preserves_raw_failure_and_stops_without_payload_copy(string fixture,
        string operation, int occurrence, int raw, int error, int fileLength,
        int marker, Exe2ArcIoStage stage)
    {
        Assert.NotEmpty(fixture);
        var state = new Exe2ArcTestIoState(Exe2ArcTestIoState.InputWithMarker(false, fileLength, marker));
        state.Faults.Add((operation, occurrence), (raw, error));
        var io = new Exe2ArcTestIo(state);
        var result = Scan(false, ref io, state, (uint)fileLength,
            out uint offset, out uint length, out var observed);

        bool finalSeek = stage == Exe2ArcIoStage.PayloadSeek;
        Assert.Equal(finalSeek ? Exe2ArcIoStatus.MatchedNotPositioned : Exe2ArcIoStatus.IoStopped, result);
        Assert.Equal(finalSeek ? (uint)marker : 0u, offset);
        Assert.Equal(finalSeek ? (uint)(fileLength - marker) : 0u, length);
        Assert.True(observed.HasResult);
        Assert.Equal(stage, observed.Stage);
        Assert.Equal(raw, observed.RawResult);
        Assert.Equal(raw == -1, observed.IoErrCaptured);
        Assert.Equal(raw == -1 ? error : 0, observed.IoError);
        Assert.Equal(raw == -1 ? 1 : 0, state.IoErrCalls);
        Assert.Equal(0u, observed.BytesCompleted);
        Assert.Equal(operation == "Read" ? (uint)(fileLength > 102400 ? fileLength - 102394 : fileLength) : 0u,
            observed.RequestedBytes);
        Assert.Empty(state.OutputBytes);
        Assert.DoesNotContain(state.Calls, c => c.Operation == "Write");
        if (operation == "Read")
            Assert.Equal(occurrence, state.Calls.Count(c => c.Operation == "Read"));
        else
            Assert.Equal(occurrence, state.Calls.Count(c => c.Operation == "Seek"));
        state.AssertUnownedResourcesAndGuards();
    }

    [Theory]
    [InlineData("null", 0u, 102400u, false)]
    [InlineData("capacity", 0x1001u, 102399u, false)]
    [InlineData("wrap", 0xffff0000u, 102400u, false)]
    [InlineData("unmapped", 0x1001u, 102400u, true)]
    public void Invalid_scratch_rejects_before_IO_or_memory_read(string fixture,
        uint address, uint capacity, bool unmapped)
    {
        Assert.NotEmpty(fixture);
        var state = new Exe2ArcTestIoState(new byte[100], address) { RejectMapping = unmapped };
        var io = new Exe2ArcTestIo(state);
        var result = Exe2ArcForwardScanner.ScanRar4(ref io, Exe2ArcTestIoState.Input,
            new APTR(address), capacity, 100, out uint offset, out uint length, out var observed);
        Assert.Equal(Exe2ArcIoStatus.InvalidBuffer, result);
        Assert.Equal(0u, offset);
        Assert.Equal(0u, length);
        Assert.False(observed.HasResult);
        Assert.Equal(0, state.ByteReads);
        Assert.Empty(state.Calls);
        state.AssertUnownedResourcesAndGuards();
    }

    [Fact]
    public void Signed_seek_domain_and_null_handle_are_explicit_admission_failures()
    {
        var state = new Exe2ArcTestIoState(new byte[100]);
        var io = new Exe2ArcTestIo(state);
        Assert.Equal(Exe2ArcIoStatus.UnsupportedRange, Scan(false, ref io, state,
            0x80000000, out _, out _, out var unsupported));
        Assert.Equal(Exe2ArcIoStatus.InvalidHandle, Exe2ArcForwardScanner.ScanRar4(ref io,
            BPTR.Null, state.Scratch, 102400, 100, out _, out _, out var invalid));
        Assert.False(unsupported.HasResult);
        Assert.False(invalid.HasResult);
        Assert.Empty(state.Calls);
        Assert.Equal(0, state.MappingCalls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EXA_F14_repeated_interleaved_borrowers_keep_positions_and_output_separate(bool firstCab, bool secondCab)
    {
        var first = new Exe2ArcTestIoState(Exe2ArcTestIoState.InputWithMarker(firstCab, 100, 8, 36, 20));
        var second = new Exe2ArcTestIoState(Exe2ArcTestIoState.InputWithMarker(secondCab, 200, 16, 48, 20));
        var firstIo = new Exe2ArcTestIo(first);
        var secondIo = new Exe2ArcTestIo(second);
        for (int repeat = 0; repeat < 3; repeat++)
        {
            Assert.Equal(Exe2ArcIoStatus.Completed, Scan(firstCab, ref firstIo, first, 100, out uint firstOffset, out uint firstLength, out _));
            Assert.Equal(Exe2ArcIoStatus.Completed, Scan(secondCab, ref secondIo, second, 200, out uint secondOffset, out uint secondLength, out _));
            Assert.Equal(8u, firstOffset);
            Assert.Equal(16u, secondOffset);
            Assert.Equal(Exe2ArcIoStatus.Completed, Exe2ArcPayloadCopy.Copy(ref firstIo,
                Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output, first.Scratch, 102400, firstLength, out var firstCopy));
            Assert.Equal(Exe2ArcIoStatus.Completed, Exe2ArcPayloadCopy.Copy(ref secondIo,
                Exe2ArcTestIoState.Input, Exe2ArcTestIoState.Output, second.Scratch, 102400, secondLength, out var secondCopy));
            Assert.Equal(firstLength, firstCopy.BytesCompleted);
            Assert.Equal(secondLength, secondCopy.BytesCompleted);
            Assert.Equal(first.InputBytes.AsSpan(8, (int)firstLength).ToArray(), first.OutputBytes.TakeLast((int)firstLength));
            Assert.Equal(second.InputBytes.AsSpan(16, (int)secondLength).ToArray(), second.OutputBytes.TakeLast((int)secondLength));
        }
        first.AssertUnownedResourcesAndGuards();
        second.AssertUnownedResourcesAndGuards();
    }

    private static Exe2ArcIoStatus Scan(bool cabinet, ref Exe2ArcTestIo io,
        Exe2ArcTestIoState state, uint fileLength, out uint offset, out uint length,
        out Exe2ArcIoObservation observation) => cabinet
        ? Exe2ArcForwardScanner.ScanCabinet(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, fileLength, out offset, out length, out observation)
        : Exe2ArcForwardScanner.ScanRar4(ref io, Exe2ArcTestIoState.Input,
            state.Scratch, 102400, fileLength, out offset, out length, out observation);
}
