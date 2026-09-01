using System.Buffers.Binary;

namespace CopperOS.Commands.Exe2ArcIoNativeExecution;

// Expected I/O scripts are test data derived from the recorded source contract,
// not calls into the production scanner. There is no archive execution here.
internal sealed record IoStep(string Operation, int Argument, int Mode, int Result,
    uint Stage, int Error = 0);
internal sealed record ExpectedIo(uint HasResult, uint Stage, int RawResult,
    uint RequestedBytes, uint BytesCompleted, uint Captured, int Error)
{
    public static readonly ExpectedIo None = new(0, 0, 0, 0, 0, 0, 0);
}
internal sealed record IoCase
{
    public required string Id { get; init; }
    public required uint Operation { get; init; }
    public required byte[] Input { get; init; }
    public required uint Length { get; init; }
    public int InitialPosition { get; init; }
    public uint Result { get; init; }
    public uint ScanStatus { get; init; } = uint.MaxValue;
    public uint Offset { get; init; }
    public uint PayloadLength { get; init; }
    public ExpectedIo ScanIo { get; init; } = ExpectedIo.None;
    public uint CopyStatus { get; init; } = uint.MaxValue;
    public ExpectedIo CopyIo { get; init; } = ExpectedIo.None;
    public IoStep[] Script { get; init; } = [];
    public byte[] Output { get; init; } = [];
    public uint? ScratchArgument { get; init; }
    public uint Capacity { get; init; } = 102400;
    public bool NullInput { get; init; }
    public bool NullOutput { get; init; }
    public uint StackBytes { get; init; } = 4096;
}

internal static class IoCases
{
    public const string Suite = "exe2arc-rar4-cab-dos-components";
    public const int ExpectedInvocations = 77;

    public static IEnumerable<IoCase[]> All()
    {
        var singles = Ordinary().ToArray();
        foreach (var test in singles) yield return [test];
        // Pairs actually interleave individual CPU instructions in one shared
        // image. These are extra invocations, not another independent image.
        string[][] pairs = [
            ["EXA-S04.small", "EXA-S05.small"],
            ["EXA-S04.copy-trailer", "EXA-S05.drop-trailer"],
            ["EXA-F08.candidate-seek", "EXA-S05.eof"],
            ["EXA-F08.copy.Write.2.-1", "EXA-COPY.102401"],
        ];
        for (int pair = 0; pair < pairs.Length; pair++)
            yield return pairs[pair].Select((id, slot) => singles.Single(c => c.Id == id) with
            {
                Id = $"EXA-F14.pair{pair}.{slot}.{id}", StackBytes = slot == 0 ? 4096u : 16384u,
            }).ToArray();
    }

    private static IEnumerable<IoCase> Ordinary()
    {
        // Explicit source-window starts, including the six EOF triplets.
        yield return Scan("EXA-S04.small", false, 100, 32, [0], true);
        yield return Scan("EXA-S05.small", true, 100, 32, [0], true);
        yield return Scan("EXA-S04.eof", false, 8, 1, [0], true);
        yield return Scan("EXA-S05.eof", true, 21, 1, [0], true);
        yield return Scan("EXA-S13.rar.last-first", false, 102400, 102393, [0], true);
        yield return Scan("EXA-S13.rar.at-next", false, 102401, 102394, [0], false);
        yield return Scan("EXA-S13.rar.inside-next", false, 102402, 102395, [0, 102394], true);
        yield return Scan("EXA-S13.cab.last-first", true, 102400, 102380, [0], true);
        yield return Scan("EXA-S13.cab.at-next", true, 102401, 102381, [0], false);
        yield return Scan("EXA-S13.cab.inside-next", true, 102402, 102382, [0, 102381], true);
        yield return Scan("EXA-S13.rar.crossing", false, 102430, 102398, [0, 102394], true);
        yield return Scan("EXA-S13.cab.crossing", true, 102430, 102390, [0, 102381], true);
        yield return Scan("EXA-S13.rar.third-window", false, 205100, 205000, [0, 102394, 204788], true);
        yield return Scan("EXA-S13.cab.third-window", true, 205100, 205000, [0, 102381, 204762], true);
        yield return Scan("EXA-F04.rar.empty", false, 0, -1, [], false);
        yield return Scan("EXA-F04.rar.six", false, 6, -1, [], false);
        yield return Scan("EXA-F04.rar.seven", false, 7, -1, [], false);
        yield return Scan("EXA-F04.rar.eight", false, 8, -1, [0], false);
        yield return Scan("EXA-F04.cab.empty", true, 0, -1, [], false);
        yield return Scan("EXA-F04.cab.nineteen", true, 19, -1, [], false);
        yield return Scan("EXA-F04.cab.twenty", true, 20, -1, [], false);
        yield return Scan("EXA-F04.cab.twenty-one", true, 21, -1, [0], false);
        yield return Scan("EXA-F04.rar.multiple", false, 205100, -1, [0, 102394, 204788], false);
        yield return Scan("EXA-F04.cab.multiple", true, 205100, -1, [0, 102381, 204762], false);

        foreach (bool cab in new[] { false, true })
        {
            var atZero = Scan("EXA-F05.zero." + cab, cab, 100, 0, [0], true);
            PutMarker(atZero.Input, 40, cab, 1, 0);
            yield return atZero;
        }
        (uint length, uint table)[] badCab = [(200, 0), (24, 24), (24, uint.MaxValue), (0, 0)];
        for (int index = 0; index < badCab.Length; index++)
        {
            var test = Scan("EXA-F10.cab-invalid." + index, true, 100, 40, [0], true);
            PutMarker(test.Input, 1, true, badCab[index].length, badCab[index].table);
            yield return test;
        }
        var rar5 = Scan("EXA-F10.rar5-then-rar4", false, 100, 40, [0], true);
        PutMarker(rar5.Input, 1, false, 0, 0);
        rar5.Input[7] = 1;
        yield return rar5;
        foreach (bool cab in new[] { false, true })
        {
            var first = Scan("EXA-S04.first-marker." + cab, cab, 100, 20, [0], true);
            PutMarker(first.Input, 60, cab, 1, 0);
            yield return first;
        }

        var basic = Scan("base", false, 100, 32, [0], true);
        yield return FailScan(basic, "EXA-F08.initial-seek", 0, -1, 0, 3);
        yield return FailScan(basic, "EXA-F08.read-error", 1, -1, 205, 3);
        yield return FailScan(basic, "EXA-F08.read-zero", 1, 0, 0, 3);
        yield return FailScan(basic, "EXA-F08.read-short", 1, 99, 0, 3);
        yield return FailScan(basic, "EXA-F08.candidate-seek", 2, -1, 219, 3);
        // Pipeline request makes the no-copy consequence of final-seek failure
        // executable, rather than only inspecting an enum in isolation.
        yield return FailScan(basic, "EXA-F08.final-seek-no-copy", 3, -1, 219, 4) with { Operation = 3 };
        var next = Scan("next", false, 102430, 102398, [0, 102394], true);
        yield return FailScan(next, "EXA-F08.second-window-seek", 2, -1, 219, 3);
        yield return FailScan(next, "EXA-F08.second-window-read", 3, 35, 0, 3);

        yield return Copy("EXA-COPY.0", 0, []);
        yield return Copy("EXA-COPY.1", 1, [1]);
        yield return Copy("EXA-COPY.102399", 102399, [102399]);
        yield return Copy("EXA-COPY.102400", 102400, [102400]);
        yield return Copy("EXA-COPY.102401", 102401, [102400, 1]);
        yield return Copy("EXA-COPY.204813", 204813, [102400, 102400, 13]);
        foreach (string operation in new[] { "Read", "Write" })
        foreach (int occurrence in new[] { 1, 2 })
        foreach (int raw in new[] { -1, 0, 3 })
        {
            var copy = Copy("copy", 102413, [102400, 13]);
            int index = (occurrence - 1) * 2 + (operation == "Read" ? 0 : 1);
            int completed = (occurrence - 1) * 102400 + (operation == "Write" && raw > 0 ? raw : 0);
            var script = StopAt(copy.Script, index, raw, raw == -1 ? occurrence == 1 ? 221 : 0 : 0);
            yield return copy with
            {
                Id = $"EXA-F08.copy.{operation}.{occurrence}.{raw}", Result = 3, CopyStatus = 3,
                Script = script, Output = copy.Input.AsSpan(7, completed).ToArray(),
                CopyIo = Observation(script, (uint)completed),
            };
        }

        yield return Pipeline(false);
        yield return Pipeline(true);
        var invalidScan = Scan("invalid", false, 100, 32, [0], true) with
        {
            Result = 5, ScanStatus = 5, Offset = 0, PayloadLength = 0,
            Script = [], ScanIo = ExpectedIo.None,
        };
        yield return invalidScan with { Id = "EXA-BOUND.scan.null-scratch", ScratchArgument = 0 };
        yield return invalidScan with { Id = "EXA-BOUND.scan.capacity", Capacity = 102399 };
        var invalidCopy = Copy("invalid-copy", 100, [100]) with
        {
            Result = 5, CopyStatus = 5, Script = [], CopyIo = ExpectedIo.None, Output = [],
        };
        yield return invalidCopy with { Id = "EXA-BOUND.copy.null-scratch", ScratchArgument = 0 };
        yield return invalidCopy with { Id = "EXA-BOUND.copy.uint-wrap", ScratchArgument = 0xffff0000 };
        yield return invalidScan with { Id = "EXA-BOUND.scan.signed-range", Length = 0x80000000, Result = 6, ScanStatus = 6 };
        yield return invalidCopy with { Id = "EXA-BOUND.copy.signed-range", Length = 0x80000000, Result = 6, CopyStatus = 6 };
        yield return invalidScan with { Id = "EXA-BOUND.scan.null-input", NullInput = true, Result = 7, ScanStatus = 7 };
        yield return invalidCopy with { Id = "EXA-BOUND.copy.null-output", NullOutput = true, Result = 7, CopyStatus = 7 };
    }

    private static IoCase Scan(string id, bool cabinet, int fileLength, int marker,
        int[] windows, bool recognized)
    {
        byte[] input = Enumerable.Repeat((byte)0x55, fileLength).ToArray();
        if (marker >= 0) PutMarker(input, marker, cabinet, 1, 0);
        var script = new List<IoStep>();
        int cursor = 0;
        foreach (int start in windows)
        {
            script.Add(new IoStep("Seek", start, -1, cursor, 1));
            int bytes = Math.Min(102400, fileLength - start);
            script.Add(new IoStep("Read", bytes, 0, bytes, 2));
            cursor = start + bytes;
        }
        if (recognized)
        {
            int index = marker - windows[^1];
            script.Add(new IoStep("Seek", -index, 0, cursor, 3));
            cursor -= index;
            if (marker != 0)
                script.Add(new IoStep("Seek", marker, -1, cursor, 4));
        }
        uint status = !recognized ? 1u : marker == 0 ? 2u : 0u;
        return new IoCase
        {
            Id = id, Operation = cabinet ? 1u : 0u, Input = input,
            Length = (uint)fileLength, Result = status, ScanStatus = status,
            Offset = recognized && marker != 0 ? (uint)marker : 0,
            PayloadLength = recognized && marker != 0 ? cabinet ? 1u : (uint)(fileLength - marker) : 0,
            Script = script.ToArray(), ScanIo = Observation(script, 0),
        };
    }

    private static IoCase FailScan(IoCase source, string id, int scriptIndex,
        int raw, int error, uint status)
    {
        var steps = StopAt(source.Script, scriptIndex, raw, error);
        return source with
        {
            Id = id, Script = steps, Result = status, ScanStatus = status,
            Offset = status == 4 ? source.Offset : 0,
            PayloadLength = status == 4 ? source.PayloadLength : 0,
            ScanIo = Observation(steps, 0),
        };
    }

    private static IoCase Copy(string id, int length, int[] chunks)
    {
        if (chunks.Sum() != length) throw new InvalidOperationException("Bad finite copy script.");
        byte[] bytes = Enumerable.Range(0, length + 17).Select(i => (byte)(i * 29)).ToArray();
        var script = new List<IoStep>();
        foreach (int chunk in chunks)
        {
            script.Add(new IoStep("Read", chunk, 0, chunk, 5));
            script.Add(new IoStep("Write", chunk, 0, chunk, 6));
        }
        return new IoCase
        {
            Id = id, Operation = 2, Input = bytes, Length = (uint)length,
            InitialPosition = 7, CopyStatus = 0, Script = script.ToArray(),
            Output = bytes.AsSpan(7, length).ToArray(), CopyIo = Observation(script, (uint)length),
        };
    }

    private static IoCase Pipeline(bool cabinet)
    {
        IoCase scan = Scan(cabinet ? "EXA-S05.drop-trailer" : "EXA-S04.copy-trailer",
            cabinet, 100, 32, [0], true);
        int length = cabinet ? 36 : 68;
        if (cabinet) PutMarker(scan.Input, 32, true, 36, 20);
        IoStep[] copy = [new("Read", length, 0, length, 5), new("Write", length, 0, length, 6)];
        return scan with
        {
            Operation = cabinet ? 4u : 3u, PayloadLength = (uint)length,
            CopyStatus = 0, CopyIo = Observation(copy, (uint)length),
            Script = [..scan.Script, ..copy], Output = scan.Input.AsSpan(32, length).ToArray(),
        };
    }

    private static IoStep[] StopAt(IoStep[] source, int index, int raw, int error)
    {
        var steps = source.Take(index).Append(source[index] with { Result = raw, Error = error }).ToList();
        if (raw == -1) steps.Add(new IoStep("IoErr", 0, 0, error, 0));
        return steps.ToArray();
    }

    private static ExpectedIo Observation(IEnumerable<IoStep> script, uint completed)
    {
        IoStep? last = script.LastOrDefault(s => s.Operation != "IoErr");
        return last is null ? ExpectedIo.None : new ExpectedIo(1, last.Stage, last.Result,
            last.Operation == "Seek" ? 0u : (uint)last.Argument, completed,
            last.Result == -1 ? 1u : 0u, last.Result == -1 ? last.Error : 0);
    }

    private static void PutMarker(byte[] bytes, int offset, bool cabinet, uint length, uint table)
    {
        if (!cabinet)
            new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0 }.CopyTo(bytes, offset);
        else
        {
            "MSCF"u8.CopyTo(bytes.AsSpan(offset));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 8), length);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 16), table);
        }
    }
}
