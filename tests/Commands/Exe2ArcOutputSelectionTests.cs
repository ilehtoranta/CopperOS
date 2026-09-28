using System.Text;
using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcOutputSelectionTests
{
    private static readonly APTR To = new(0x1100);
    private static readonly APTR Generated = new(0x1200);
    private static readonly APTR Fallback = new(0x1300);
    private static readonly APTR Basename = new(0x1400);

    [Fact]
    public void Missing_to_opens_generated_name_directly()
    {
        var state = new SelectionState(to: "RAM:in/demo.ace",
            generated: "RAM:in/demo.ace", basename: "demo.ace");
        var io = new SelectionIo(state);

        var status = Exe2ArcOutputSelection.TryOpen(ref io, APTR.Null,
            Generated, Fallback, Basename, 512, out var output,
            out var selected);

        Assert.Equal(Exe2ArcOutputStatus.OpenedGenerated, status);
        Assert.Equal(0x5001u, output.Raw);
        Assert.Equal(Generated, selected);
        Assert.Equal(["RAM:in/demo.ace"], state.Opened);
        Assert.Empty(state.Added);
    }

    [Fact]
    public void Literal_to_success_precedes_directory_fallback()
    {
        var state = new SelectionState(to: "RAM:out", generated: "RAM:in/demo.ace",
            basename: "demo.ace");
        var io = new SelectionIo(state);

        var status = Exe2ArcOutputSelection.TryOpen(ref io, To, Generated,
            Fallback, Basename, 512, out var output, out var selected);

        Assert.Equal(Exe2ArcOutputStatus.OpenedLiteralTo, status);
        Assert.Equal(To, selected);
        Assert.NotEqual(0u, output.Raw);
        Assert.Equal(["RAM:out"], state.Opened);
        Assert.Empty(state.Added);
    }

    [Fact]
    public void Failed_literal_to_uses_two_AddPart_calls_then_opens_fallback()
    {
        var state = new SelectionState(to: "RAM:out", generated: "RAM:in/demo.ace",
            basename: "demo.ace") { FailLiteralTo = true };
        var io = new SelectionIo(state);

        var status = Exe2ArcOutputSelection.TryOpen(ref io, To, Generated,
            Fallback, Basename, 512, out var output, out var selected);

        Assert.Equal(Exe2ArcOutputStatus.OpenedDirectoryFallback, status);
        Assert.Equal(Fallback, selected);
        Assert.NotEqual(0u, output.Raw);
        Assert.Equal(["RAM:out", "RAM:out/demo.ace"], state.Added);
        Assert.Equal(["RAM:out", "RAM:out/demo.ace"], state.Opened);
        Assert.Equal("RAM:out/demo.ace", state.Read(Fallback));
    }

    [Fact]
    public void First_AddPart_failure_stops_without_second_call_or_open()
    {
        var state = new SelectionState(to: "RAM:out", generated: "RAM:in/demo.ace",
            basename: "demo.ace") { FailAddPartCall = 1, FailLiteralTo = true };
        var io = new SelectionIo(state);

        var status = Exe2ArcOutputSelection.TryOpen(ref io, To, Generated,
            Fallback, Basename, 512, out var output, out var selected);

        Assert.Equal(Exe2ArcOutputStatus.NotOpened, status);
        Assert.True(output.IsNull);
        Assert.True(selected.IsNull);
        Assert.Single(state.Added);
        Assert.Equal(["RAM:out"], state.Opened);
    }

    [Fact]
    public void Second_AddPart_failure_stops_before_fallback_open()
    {
        var state = new SelectionState(to: "RAM:out", generated: "RAM:in/demo.ace",
            basename: "demo.ace") { FailAddPartCall = 2, FailLiteralTo = true };
        var io = new SelectionIo(state);

        var status = Exe2ArcOutputSelection.TryOpen(ref io, To, Generated,
            Fallback, Basename, 512, out var output, out var selected);

        Assert.Equal(Exe2ArcOutputStatus.NotOpened, status);
        Assert.True(output.IsNull);
        Assert.True(selected.IsNull);
        Assert.Equal(["RAM:out", "RAM:out/demo.ace"], state.Added);
        Assert.Equal(["RAM:out"], state.Opened);
    }

    [Fact]
    public void Invalid_fallback_buffer_is_reported_after_literal_failure()
    {
        var state = new SelectionState(to: "RAM:out", generated: "RAM:in/demo.ace",
            basename: "demo.ace") { FailLiteralTo = true };
        var io = new SelectionIo(state);

        var status = Exe2ArcOutputSelection.TryOpen(ref io, To, Generated,
            APTR.Null, Basename, 512, out var output, out var selected);

        Assert.Equal(Exe2ArcOutputStatus.InvalidBuffer, status);
        Assert.True(output.IsNull);
        Assert.True(selected.IsNull);
        Assert.Equal(["RAM:out"], state.Opened);
        Assert.Empty(state.Added);
    }

    private sealed class SelectionState
    {
        internal readonly byte[] Bytes = Enumerable.Repeat((byte)0xcc, 0x1000).ToArray();
        internal readonly List<string> Opened = [];
        internal readonly List<string> Added = [];
        internal bool FailLiteralTo;
        internal int FailAddPartCall;
        internal int AddPartCalls;
        internal SelectionState(string to, string generated, string basename)
        {
            ToPath = to;
            GeneratedPath = generated;
            BasenamePath = basename;
            Put(To, to);
            Put(Generated, generated);
            Put(Basename, basename);
        }

        internal string Read(APTR address)
        {
            var offset = checked((int)(address.Raw - 0x1000));
            var length = 0;
            while (offset + length < Bytes.Length && Bytes[offset + length] != 0)
                length++;
            return Encoding.ASCII.GetString(Bytes, offset, length);
        }

        internal void Put(APTR address, string text)
        {
            var offset = checked((int)(address.Raw - 0x1000));
            for (var index = 0; index < text.Length; index++)
                Bytes[offset + index] = (byte)text[index];
            Bytes[offset + text.Length] = 0;
        }

        internal string ToPath { get; }
        internal string GeneratedPath { get; }
        internal string BasenamePath { get; }
    }

    private readonly struct SelectionIo : IExe2ArcOutputIo
    {
        private readonly SelectionState state;
        public SelectionIo(SelectionState state) => this.state = state;

        public BPTR OpenNewFile(APTR path)
        {
            var value = state.Read(path);
            state.Opened.Add(value);
            if (state.FailLiteralTo && path == To)
                return BPTR.Null;
            return new BPTR(0x5000u + (uint)state.Opened.Count);
        }

        public int AddPart(APTR buffer, APTR part, uint capacity)
        {
            state.AddPartCalls++;
            var left = state.Read(buffer);
            var right = state.Read(part);
            var combined = left.Length == 0 ? right : left + "/" + right;
            state.Added.Add(combined);
            if (state.FailAddPartCall == state.AddPartCalls)
                return 0;
            if (combined.Length + 1 > capacity)
                return 0;
            state.Put(buffer, combined);
            return 1;
        }

        public bool IsMapped(APTR address, uint byteSize) =>
            !address.IsNull && address.Raw >= 0x1000 &&
            address.Raw - 0x1000 + byteSize <= state.Bytes.Length;
        public byte ReadUInt8(APTR address, int offset = 0) =>
            state.Bytes[checked((int)(address.Raw - 0x1000) + offset)];
        public ushort ReadUInt16(APTR address, int offset = 0) => throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) => throw new NotSupportedException();
        public void WriteUInt8(APTR address, int offset, byte value) =>
            state.Bytes[checked((int)(address.Raw - 0x1000) + offset)] = value;
        public void WriteUInt16(APTR address, int offset, ushort value) => throw new NotSupportedException();
        public void WriteUInt32(APTR address, int offset, uint value) => throw new NotSupportedException();
        public void Clear(APTR address, uint byteCount) => throw new NotSupportedException();
        public void Copy(APTR source, APTR destination, uint byteCount) => throw new NotSupportedException();
    }
}
