using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Supplied-vector cases for the bounded Type stream transfer.</summary>
internal sealed record TypeTextIoProbeCase(string Source, bool Number,
    bool NoLine, string Output, int ReadChunk = 3, int WriteChunk = 2,
    bool BreakOnPoll = false);

internal sealed class TypeTextIoNativeLayout(uint control, uint source,
    uint inputBuffer, uint outputBuffer)
{
    public uint Control { get; } = control;
    public uint Source { get; } = source;
    public uint InputBuffer { get; } = inputBuffer;
    public uint OutputBuffer { get; } = outputBuffer;
    public int ReadOffset { get; set; }
    public int ReadCalls { get; set; }
    public int WriteCalls { get; set; }
    public int SignalCalls { get; set; }
    public bool BreakDelivered { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string TypeTextIoProbeSuite = "type-text-io-probe-fixture";
    private const uint TypeIoControlBytes = 48;
    private const uint TypeIoBufferBytes = 16;
    private const uint CtrlCMask = 1u << 12;
    private const uint TypeIoInputHandle = 0x123;
    private const uint TypeIoOutputHandle = 0x456;

    private List<object> RunTypeTextIoProbeCases()
    {
        var breakSource = new string('x', 256);
        ProbeCase[] cases =
        [
            TypeIo("stream-numbered-short-writes", "a\nb", true, false,
                "    1 a\n    2 b\n", 1, 2),
            TypeIo("stream-noline", "alpha", false, true, "alpha", 2, 1),
            TypeIo("stream-empty-numbered", "", true, false, "    1 \n", 3, 16),
            TypeIo("stream-break", breakSource, false, false,
                new string('x', 240), 16, 16, true)
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-stream-numbered", StackBytes = 4096 },
            cases[1] with { Name = "interleaved-stream-noline" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase TypeIo(string name, string source, bool number,
        bool noLine, string output, int readChunk, int writeChunk,
        bool breakOnPoll = false) => new(name, "", breakOnPoll ? DOS.RETURN_ERROR :
            DOS.RETURN_OK, breakOnPoll ? (int)DOS.Error.Break : 0, output)
        {
            EntryLength = (int)TypeIoControlBytes,
            TypeTextIo = new(source, number, noLine, output, readChunk,
                writeChunk, breakOnPoll)
        };

    private void PrepareTypeTextIoProbe(Invocation invocation)
    {
        var definition = invocation.Definition.TypeTextIo ??
            throw new InvalidOperationException("Missing Type text I/O definition.");
        var source = Encoding.Latin1.GetBytes(definition.Source);
        Require(definition.ReadChunk is > 0 and <= (int)TypeIoBufferBytes &&
            definition.WriteChunk is > 0 and <= (int)TypeIoBufferBytes,
            "Type text I/O fixture chunk size is outside its bounded buffers.");
        var layout = new TypeTextIoNativeLayout(
            Bus.Allocate(invocation, TypeIoControlBytes, "TypeTextIoFixture", true),
            Bus.Allocate(invocation, Math.Max(1u, (uint)source.Length), "TypeTextIoFixture", false),
            Bus.Allocate(invocation, TypeIoBufferBytes, "TypeTextIoFixture", false),
            Bus.Allocate(invocation, TypeIoBufferBytes, "TypeTextIoFixture", false));
        invocation.TypeTextIoLayout = layout;
        Bus.Memory.AsSpan((int)layout.Source, Math.Max(1, source.Length)).Fill(0xa5);
        Bus.Memory.AsSpan((int)layout.InputBuffer, (int)TypeIoBufferBytes).Fill(0xa5);
        Bus.Memory.AsSpan((int)layout.OutputBuffer, (int)TypeIoBufferBytes).Fill(0xa5);
        source.CopyTo(Bus.Memory.AsSpan((int)layout.Source));
        Bus.Long(layout.Control, definition.Number ? 1u : 0u);
        Bus.Long(layout.Control + 4, definition.NoLine ? 1u : 0u);
        Bus.Long(layout.Control + 8, layout.InputBuffer);
        Bus.Long(layout.Control + 12, TypeIoBufferBytes);
        Bus.Long(layout.Control + 16, layout.OutputBuffer);
        Bus.Long(layout.Control + 20, TypeIoBufferBytes);
    }

    private void VerifyTypeTextIoProbe(Invocation invocation)
    {
        var definition = invocation.Definition.TypeTextIo ??
            throw new InvalidOperationException("Missing Type text I/O definition.");
        var layout = invocation.TypeTextIoLayout ??
            throw new InvalidOperationException("Missing Type text I/O storage.");
        Require(invocation.Opens == 1 && invocation.Closes == 1 &&
            invocation.Reads == 0 && invocation.FreeArgs == 0 &&
            invocation.Allocations == 0 && invocation.FreeMem == 0,
            "Type text I/O probe must retain only its supplied DOS stream operations.");
        Require(Bus.Long(layout.Control + 24) == (uint)invocation.Definition.Result &&
            Bus.Long(layout.Control + 28) == unchecked((uint)invocation.Definition.Error) &&
            Bus.Long(layout.Control + 44) == 0x5454494f,
            "Type text I/O probe result/control publication differs from the supplied case.");
        Require(layout.ReadCalls >= 1 && layout.WriteCalls >= 1 &&
            layout.SignalCalls == (definition.BreakOnPoll ? 1 : 0) &&
            invocation.Events.Take(2).SequenceEqual(new[] { "FindTask", "OpenLibrary" }) &&
            invocation.Events.TakeLast(2).SequenceEqual(new[] { "SetIoErr", "CloseLibrary" }),
            "Type text I/O vector order or break polling differs from the bounded contract.");
        ReleaseTypeTextIoStorage(invocation, layout);
    }

    private void ReleaseTypeTextIoStorage(Invocation invocation,
        TypeTextIoNativeLayout layout)
    {
        var sourceSize = Math.Max(1u, (uint)invocation.Definition.TypeTextIo!.Source.Length);
        Bus.Release(invocation, layout.OutputBuffer, "TypeTextIoFixture", TypeIoBufferBytes);
        Bus.Release(invocation, layout.InputBuffer, "TypeTextIoFixture", TypeIoBufferBytes);
        Bus.Release(invocation, layout.Source, "TypeTextIoFixture", sourceSize);
        Bus.Release(invocation, layout.Control, "TypeTextIoFixture", TypeIoControlBytes);
        invocation.TypeTextIoLayout = null;
    }

    private void RegisterTypeTextIoExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
        {
            var layout = invocation.TypeTextIoLayout ??
                throw new InvalidOperationException("Type text I/O SetSignal without storage.");
            Require(state.D[0] == 0 && state.D[1] == CtrlCMask,
                "Type text I/O must clear and observe only Ctrl-C.");
            layout.SignalCalls++;
            if (!invocation.Definition.TypeTextIo!.BreakOnPoll || layout.BreakDelivered)
                return 0;
            layout.BreakDelivered = true;
            return CtrlCMask;
        });
    }

    private void RegisterTypeTextIoDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Read, "Read", (state, invocation) =>
        {
            var layout = invocation.TypeTextIoLayout ??
                throw new InvalidOperationException("Type text I/O Read without storage.");
            var definition = invocation.Definition.TypeTextIo!;
            Require(state.D[1] == TypeIoInputHandle && state.D[2] == layout.InputBuffer &&
                unchecked((int)state.D[3]) == TypeIoBufferBytes,
                "Type text I/O Read ABI differs from the supplied buffer contract.");
            var remaining = definition.Source.Length - layout.ReadOffset;
            var count = Math.Min(remaining, definition.ReadChunk);
            if (count > 0)
            {
                Encoding.Latin1.GetBytes(definition.Source.AsSpan(layout.ReadOffset, count),
                    Bus.Memory.AsSpan((int)layout.InputBuffer, count));
                layout.ReadOffset += count;
            }
            layout.ReadCalls++;
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            var layout = invocation.TypeTextIoLayout ??
                throw new InvalidOperationException("Type text I/O Write without storage.");
            var definition = invocation.Definition.TypeTextIo!;
            var count = unchecked((int)state.D[3]);
            Require(state.D[1] == TypeIoOutputHandle && count > 0 &&
                state.D[2] >= layout.OutputBuffer &&
                (ulong)state.D[2] + (uint)count <= layout.OutputBuffer + TypeIoBufferBytes,
                "Type text I/O Write ABI differs from the supplied buffer contract.");
            var written = Math.Min(count, definition.WriteChunk);
            invocation.Output.Write(Bus.Memory, (int)state.D[2], written);
            layout.WriteCalls++;
            return unchecked((uint)written);
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, ("SetIoErr"), (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, state.D[1]);
            return 0;
        });
    }
}
