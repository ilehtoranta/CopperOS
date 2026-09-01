using System.Buffers.Binary;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record NativeIoCase(uint Operation, int RawResult, int AmbientError)
{
    public int Length { get; init; } = 8;
    public int InitialError { get; init; } = Invocation.InitialIoError;
    public uint Signals { get; init; }
    public bool NullQueriedHandle { get; init; }
    public bool RequireRetainedTask { get; init; }
}

// This object identifies one guest task across separate command invocations.
// On reuse its Process bytes are read and checked, never seeded again.
internal sealed class NativeIoTaskState(int initialError, uint initialSignals)
{
    public int InitialError { get; } = initialError;
    public uint InitialSignals { get; } = initialSignals;
    public uint? Process { get; set; }
    public bool Active { get; set; }
    public int CompletedInvocations { get; set; }
    public byte[] LastProcessSnapshot { get; set; } = [];
}

internal sealed class NativeIoInvocation(NativeIoCase definition, NativeIoTaskState task)
{
    public const int ControlBytes = 40;
    public const int InputBytes = 20;
    public const int OutputBytes = 20;
    public const int PayloadCapacity = 8;
    public const byte ControlGuard = 0xc7;
    public const byte PayloadGuard = 0xd3;

    public NativeIoCase Definition { get; } = definition;
    public NativeIoTaskState Task { get; } = task;
    public uint Control { get; set; }
    public uint Payload { get; set; }
    public uint ExplicitHandle { get; set; }
    public uint InputHandle { get; set; }
    public uint OutputHandle { get; set; }
    public int StartingError { get; set; }
    public uint StartingSignals { get; set; }
    public bool RetainedTaskState { get; set; }
    public byte[] ProcessBefore { get; set; } = [];
    public byte[] ControlPrefix { get; set; } = [];
    public byte[] PayloadBefore { get; set; } = [];
    public byte[] TransferredBytes { get; set; } = [];
    public byte[] ReadPattern { get; } = [0xe1, 0x00, 0xff, 0x23, 0x7f, 0x80, 0x0a, 0x55];
    public bool[] OutputWritten { get; } = new bool[OutputBytes];
    public int OutputStoreCalls { get; set; }
    public int ReadCalls { get; set; }
    public int WriteCalls { get; set; }
    public int InputCalls { get; set; }
    public int OutputCalls { get; set; }
    public int SignalCalls { get; set; }
    public int SetIoErrCalls { get; set; }
    public List<int> IoErrValues { get; } = [];
    public List<int> PreviousIoErrValues { get; } = [];
    public List<string> ExpectedEvents { get; } = [];
    public object? Report { get; set; }

    public bool IsTransfer => Definition.Operation <= 3;
    public bool IsRead => Definition.Operation is 0 or 2;
    public bool CapturesError => IsTransfer && Definition.RawResult == -1;
    public uint TransferHandle => Definition.Operation switch
    {
        2 => InputHandle,
        3 => OutputHandle,
        _ => ExplicitHandle
    };

    public bool ContainsOutput(uint address, int size) =>
        Contains(Control + InputBytes, OutputBytes, address, size);

    public bool ContainsReadableControl(uint address, int size) =>
        Contains(Control, ControlBytes, address, size);

    public bool ContainsReadablePayload(uint address, int size) =>
        Contains(Payload, Definition.Length, address, size);

    public void RecordOutputWrite(uint address, int size)
    {
        Require(ContainsOutput(address, size), "I/O protocol write crosses the owned output extent.");
        Require(SetIoErrCalls == 1, "I/O protocol output was written outside the deliberate post-SetIoErr observation phase.");
        var first = checked((int)(address - Control - InputBytes));
        for (var index = first; index < first + size; index++) OutputWritten[index] = true;
        OutputStoreCalls++;
    }

    private static bool Contains(uint start, int length, uint address, int size) =>
        length >= 0 && size > 0 && address >= start &&
        (ulong)address + (uint)size <= (ulong)start + (uint)length;
}

internal sealed partial class ProbeFixture
{
    public const string NativeIoSuite = "command-io-vector-fixture";
    public const int NativeIoInvocationCount = 32;
    private const uint IoProtocolMagic = 0x4343494f;
    private const int IoProbePoison = 0x5a17;
    // NDK 3.1 dos/dos.h: SIGBREAKF_CTRL_C. This is the fixture's expected bit.
    private const uint IoCtrlCMask = 1u << 12;

    private List<object> RunNativeIoCases()
    {
        ProbeCase[] cases =
        [
            IoCase("R01-read-full", 0, 8, 205),
            IoCase("R02-read-short", 0, 3, 212),
            IoCase("R03-read-zero", 0, 0, 242),
            IoCase("R04-read-zero-length", 0, 0, 221, length: 0),
            IoCase("R05-read-failure", 0, -1, 205),
            IoCase("R06-read-failure-zero-error", 0, -1, 0),
            IoCase("R07-read-failure-signed-error", 0, -1, -101),
            IoCase("W01-write-full", 1, 8, 212),
            IoCase("W02-write-short", 1, 3, 205),
            IoCase("W03-write-zero", 1, 0, 303),
            IoCase("W04-write-zero-length", 1, 0, 242, length: 0),
            IoCase("W05-write-failure", 1, -1, 221),
            IoCase("W06-write-failure-zero-error", 1, -1, 0),
            IoCase("W07-write-failure-signed-error", 1, -1, -101),
            IoCase("B01-input-short-read", 2, 3, 117),
            IoCase("B02-output-short-write", 3, 3, 118),
            IoCase("B03-input-query", 5, 0, 205),
            IoCase("B04-output-query", 6, 0, 212),
            IoCase("B05-input-query-zero", 5, 0, 205, nullHandle: true),
            IoCase("B06-output-query-zero", 6, 0, 212, nullHandle: true),
            IoCase("S01-poll-none", 4, 0, 205),
            IoCase("S02-poll-unrelated", 4, 0, 205, signals: 0x8000e110),
            IoCase("S03-poll-ctrl-c", 4, 0, 205, signals: 0x00001000),
            IoCase("S04-poll-ctrl-c-and-unrelated", 4, 0, 205, signals: 0x8000f110)
        ];
        Require(cases.Length == 24 && cases.Select(test => test.Name).Distinct().Count() == 24,
            "I/O base-case inventory differs from its 24-case contract.");
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));

        var ioTask = new NativeIoTaskState(Invocation.InitialIoError, 0);
        reports.AddRange(Execute([IoCase("sequential-write-failure-zero-error", 1, -1, 0)], false, [ioTask]));
        reports.AddRange(Execute([IoCase("sequential-read-success-no-stale-error", 0, 8, 212,
            retainedTask: true)], false, [ioTask]));
        Require(ioTask.CompletedInvocations == 2, "Sequential I/O did not reuse one completed guest task twice.");

        var pollTask = new NativeIoTaskState(205, 0x8000f110);
        reports.AddRange(Execute([IoCase("repeated-poll-first", 4, 0, 205,
            signals: 0x8000f110)], false, [pollTask]));
        reports.AddRange(Execute([IoCase("repeated-poll-retained-task", 4, 0, IoProbePoison,
            signals: 0x8000f110, retainedTask: true)], false, [pollTask]));
        Require(pollTask.CompletedInvocations == 2, "Repeated polling did not reuse the same guest task twice.");

        // Execute alternates individual ExecuteInstruction calls, not just
        // library gateways; both contexts use the same already loaded image.
        reports.AddRange(Execute([
            IoCase("interleaved-read-error-4k", 0, -1, -101) with { StackBytes = 4096 },
            IoCase("interleaved-short-write-16k", 1, 3, 242) with { StackBytes = 16384 }
        ], true));
        reports.AddRange(Execute([
            IoCase("interleaved-poll-ctrl-c-4k", 4, 0, -101, signals: 0x8000f110) with { StackBytes = 4096 },
            IoCase("interleaved-poll-unrelated-16k", 4, 0, 221, signals: 0x8000e110) with { StackBytes = 16384 }
        ], true));

        Require(reports.Count == NativeIoInvocationCount, "I/O invocation inventory changed without updating its contract.");
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase IoCase(string name, uint operation, int result, int ambientError,
        int length = 8, uint signals = 0, bool nullHandle = false, bool retainedTask = false) =>
        new($"cc04.io/{name}", "", DOS.RETURN_OK, IoProbePoison, "")
        {
            EntryLength = NativeIoInvocation.ControlBytes,
            NativeIo = new NativeIoCase(operation, result, ambientError)
            {
                Length = operation <= 3 ? length : 0,
                InitialError = operation <= 3 ? Invocation.InitialIoError : ambientError,
                Signals = signals,
                NullQueriedHandle = nullHandle,
                RequireRetainedTask = retainedTask
            }
        };

    private void PrepareNativeIoInvocation(Invocation invocation)
    {
        var io = invocation.NativeIo ?? throw new InvalidOperationException("Missing native I/O fixture state.");
        var definition = io.Definition;
        var task = io.Task;
        Require(suite == NativeIoSuite && definition.Operation <= 6 && definition.Length is >= 0 and <= NativeIoInvocation.PayloadCapacity,
            "Unqualified native I/O request definition.");
        Require(!definition.NullQueriedHandle || definition.Operation is 5 or 6,
            "Null borrowed handles must be queried without a transfer.");
        Require(!io.IsTransfer || definition.RawResult >= -1 && definition.RawResult <= definition.Length,
            "I/O vector result exceeds its configured request.");
        Require(!task.Active, "Two command invocations attempted to own the same task simultaneously.");
        Require(definition.RequireRetainedTask == task.Process.HasValue,
            "I/O task reuse differs from the case's explicit continuation contract.");
        io.RetainedTaskState = task.Process.HasValue;
        if (task.Process is uint process)
        {
            Require(process == invocation.Process && task.CompletedInvocations > 0 &&
                task.LastProcessSnapshot.Length == 0x400 &&
                Bus.Memory.AsSpan((int)process, 0x400).SequenceEqual(task.LastProcessSnapshot),
                "A reused task was relocated, reset, changed after completion, or never completed.");
        }
        else
        {
            task.Process = invocation.Process;
            Bus.Memory.AsSpan((int)invocation.Process, 0x400).Clear();
            Bus.Long(invocation.Process + (uint)DosLayout.Process.CommandLineInterface, 0x100);
            Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, unchecked((uint)task.InitialError));
            Bus.Long(invocation.Process + (uint)ExecLayout.Task.SignalReceived, task.InitialSignals);
        }
        task.Active = true;
        // Read the actual prior guest state. In particular, no expected error
        // or signal value is written on the retained-task path above.
        invocation.IoError = unchecked((int)Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2));
        io.StartingError = invocation.IoError;
        io.StartingSignals = Bus.Long(invocation.Process + (uint)ExecLayout.Task.SignalReceived);
        Require(io.StartingError == (io.RetainedTaskState ? IoProbePoison : definition.InitialError) &&
            io.StartingSignals == definition.Signals, "Wrong actual task state at I/O command entry.");
        io.ProcessBefore = Bus.Memory.AsSpan((int)invocation.Process, 0x400).ToArray();

        io.Control = invocation.Arguments;
        io.Payload = invocation.Arguments + 0x101; // Byte buffer deliberately has an odd address.
        var taskIndex = invocation.Process >> 12;
        io.ExplicitHandle = 0x801 + taskIndex * 0x1d;
        io.InputHandle = definition.NullQueriedHandle ? 0 : 0x201 + taskIndex * 0x17;
        io.OutputHandle = definition.NullQueriedHandle ? 0 : 0x401 + taskIndex * 0x19;
        Bus.Memory.AsSpan((int)io.Control - 16, NativeIoInvocation.ControlBytes + 32).Fill(NativeIoInvocation.ControlGuard);
        Bus.Memory.AsSpan((int)io.Control + NativeIoInvocation.InputBytes, NativeIoInvocation.OutputBytes).Fill(0xe5);
        Bus.Long(io.Control, IoProtocolMagic);
        Bus.Long(io.Control + 4, definition.Operation);
        Bus.Long(io.Control + 8, io.ExplicitHandle);
        Bus.Long(io.Control + 12, io.Payload);
        Bus.Long(io.Control + 16, unchecked((uint)definition.Length));
        io.ControlPrefix = Bus.Memory.AsSpan((int)io.Control, NativeIoInvocation.InputBytes).ToArray();

        Bus.Memory.AsSpan((int)io.Payload - 16, NativeIoInvocation.PayloadCapacity + 32).Fill(NativeIoInvocation.PayloadGuard);
        io.PayloadBefore = [0x00, 0x7f, 0x80, 0xff, 0x0a, 0x20, 0x41, 0x00];
        io.PayloadBefore.CopyTo(Bus.Memory.AsSpan((int)io.Payload, NativeIoInvocation.PayloadCapacity));

        io.ExpectedEvents.AddRange(["FindTask", "OpenLibrary"]);
        if (definition.Operation is 2 or 5) io.ExpectedEvents.Add("Input");
        if (definition.Operation is 3 or 6) io.ExpectedEvents.Add("Output");
        if (io.IsTransfer) io.ExpectedEvents.Add(io.IsRead ? "Read" : "Write");
        if (definition.Operation == 4) io.ExpectedEvents.Add("SetSignal");
        if (io.CapturesError) io.ExpectedEvents.Add("IoErr");
        io.ExpectedEvents.AddRange(["IoErr", "SetIoErr", "SetIoErr", "CloseLibrary"]);
    }

    private static void RequireNativeIoGateway(Invocation invocation, string name)
    {
        var io = invocation.NativeIo ?? throw new InvalidOperationException("Missing I/O gateway owner.");
        var index = invocation.Events.Count;
        Require(index < io.ExpectedEvents.Count && io.ExpectedEvents[index] == name,
            $"{invocation.Definition.Name}: unexpected I/O vector {name} at call {index}; " +
            "retries, error resets, borrowed-stream cleanup, and allocation are forbidden.");
    }

    private void RegisterIoExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
        {
            var io = invocation.NativeIo ?? throw new InvalidOperationException("SetSignal without an I/O owner.");
            Require(state.D[0] == 0 && state.D[1] == 0, "I/O polling must use SetSignal(newSignals=0, mask=0).");
            var field = invocation.Process + (uint)ExecLayout.Task.SignalReceived;
            var previous = Bus.Long(field);
            var updated = (previous & ~state.D[1]) | (state.D[0] & state.D[1]);
            Bus.Long(field, updated);
            io.SignalCalls++;
            return previous;
        });
    }

    private void RegisterIoDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Read, "Read", (state, invocation) => IoTransfer(state, invocation, true));
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) => IoTransfer(state, invocation, false));
        Register(baseAddress, DosLvo.Input, "Input", (_, invocation) =>
        {
            var io = invocation.NativeIo!;
            io.InputCalls++;
            return io.InputHandle;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
        {
            var io = invocation.NativeIo!;
            io.OutputCalls++;
            return io.OutputHandle;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
        {
            var io = invocation.NativeIo!;
            var error = unchecked((int)Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2));
            Require(error == invocation.IoError, "Task's actual result field and current IoErr disagree.");
            io.IoErrValues.Add(error);
            return unchecked((uint)error);
        });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            var io = invocation.NativeIo!;
            Require(unchecked((int)state.D[1]) == IoProbePoison, "I/O probe unexpectedly reset or substituted the current error.");
            Require(io.SetIoErrCalls == 0 ? !io.OutputWritten.Any(written => written) :
                io.SetIoErrCalls == 1 && io.OutputWritten.All(written => written),
                "I/O protocol stores were omitted or moved across a SetIoErr checkpoint.");
            var previous = unchecked((int)Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2));
            Require(previous == invocation.IoError, "SetIoErr was applied to a different process error.");
            io.PreviousIoErrValues.Add(previous);
            io.SetIoErrCalls++;
            SetIoProcessError(invocation, unchecked((int)state.D[1]));
            return unchecked((uint)previous);
        });

        // Explicit mappings provide a deterministic rejection before any
        // borrowed handle or provider state could be modified.
        foreach (var forbidden in new (short Offset, string Name)[]
        {
            (DosLvo.Close, "Close"), (DosLvo.Flush, "Flush"), (DosLvo.PrintFault, "PrintFault"),
            (DosLvo.SelectInput, "SelectInput"), (DosLvo.SelectOutput, "SelectOutput"),
            (DosLvo.ReadArgs, "ReadArgs"), (DosLvo.FreeArgs, "FreeArgs")
        })
            Register(baseAddress, forbidden.Offset, forbidden.Name, (_, _) =>
                throw new InvalidOperationException("I/O fixture attempted an unowned stream or parser operation."));
    }

    private uint IoTransfer(M68kCpuState state, Invocation invocation, bool read)
    {
        var io = invocation.NativeIo ?? throw new InvalidOperationException("Transfer without an I/O owner.");
        var definition = io.Definition;
        Require(io.IsTransfer && io.IsRead == read && state.D[1] == io.TransferHandle &&
            state.D[2] == io.Payload && unchecked((int)state.D[3]) == definition.Length,
            "I/O D1 raw BPTR, D2 byte pointer, D3 signed count, or vector does not match the invocation.");
        Require(io.TransferHandle != 0 && io.ReadCalls == 0 && io.WriteCalls == 0,
            "I/O fixture made a transfer through a null handle or retried a transfer.");
        Require(Bus.Memory.AsSpan((int)io.Payload, NativeIoInvocation.PayloadCapacity).SequenceEqual(io.PayloadBefore),
            "The native caller modified the borrowed payload before the DOS vector.");
        if (read) io.ReadCalls++;
        else io.WriteCalls++;
        if (definition.RawResult > 0)
        {
            if (read)
            {
                // This is a supplied DOS-vector result, not a real file read.
                io.ReadPattern.AsSpan(0, definition.RawResult).CopyTo(
                    Bus.Memory.AsSpan((int)io.Payload, definition.RawResult));
            }
            io.TransferredBytes = Bus.Memory.AsSpan((int)io.Payload, definition.RawResult).ToArray();
        }
        // Success intentionally may leave a nonzero ambient error. The helper
        // must not turn that value into a captured failure or reset it.
        SetIoProcessError(invocation, definition.AmbientError);
        return unchecked((uint)definition.RawResult);
    }

    private void SetIoProcessError(Invocation invocation, int error)
    {
        invocation.IoError = error;
        Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, unchecked((uint)error));
    }

    private void VerifyNativeIo(Invocation invocation)
    {
        var io = invocation.NativeIo ?? throw new InvalidOperationException("Missing I/O observation.");
        var definition = io.Definition;
        Require(invocation.Events.SequenceEqual(io.ExpectedEvents), "I/O vector observations do not match the exact call contract.");
        Require(invocation.Allocations == 0 && invocation.Reads == 0 && invocation.FreeArgs == 0 &&
            invocation.FreeMem == 0 && invocation.AllocationRequests.Count == 0 && !invocation.Forbidden,
            "I/O fixture acquired memory, parser ownership, or unexpected Exec state.");
        Require(io.ReadCalls == (io.IsTransfer && io.IsRead ? 1 : 0) &&
            io.WriteCalls == (io.IsTransfer && !io.IsRead ? 1 : 0) &&
            io.InputCalls == (definition.Operation is 2 or 5 ? 1 : 0) &&
            io.OutputCalls == (definition.Operation is 3 or 6 ? 1 : 0) &&
            io.SignalCalls == (definition.Operation == 4 ? 1 : 0) && io.SetIoErrCalls == 2,
            "I/O vector call counts differ from the selected operation.");
        Require(io.IoErrValues.SequenceEqual(Enumerable.Repeat(definition.AmbientError, io.CapturesError ? 2 : 1)) &&
            io.PreviousIoErrValues.SequenceEqual(new[] { definition.AmbientError, IoProbePoison }),
            "Immediate/ambient IoErr or SetIoErr's prior value was lost.");

        uint expectedResult = definition.Operation switch
        {
            <= 3 => unchecked((uint)definition.RawResult),
            4 => (definition.Signals & IoCtrlCMask) == 0 ? 0u : 1u,
            5 => io.InputHandle,
            6 => io.OutputHandle,
            _ => throw new InvalidOperationException("Unknown I/O operation.")
        };
        uint[] expected =
        [
            expectedResult, io.CapturesError ? 1u : 0u,
            io.CapturesError ? unchecked((uint)definition.AmbientError) : 0u,
            unchecked((uint)definition.AmbientError), unchecked((uint)definition.AmbientError)
        ];
        Require(io.OutputWritten.All(written => written), "Native code did not write every I/O observation byte.");
        var observed = new uint[expected.Length];
        for (var index = 0; index < expected.Length; index++)
        {
            var actual = Bus.Long(io.Control + NativeIoInvocation.InputBytes + (uint)index * 4);
            observed[index] = actual;
            Require(actual == expected[index],
                $"{invocation.Definition.Name}: I/O field {index} is ${actual:X8}, expected ${expected[index]:X8}.");
        }
        Require(Bus.Memory.AsSpan((int)io.Control, NativeIoInvocation.InputBytes).SequenceEqual(io.ControlPrefix),
            "Native I/O probe changed its input protocol fields.");
        Require(GuardUnchanged(io.Control - 16, NativeIoInvocation.ControlGuard) &&
            GuardUnchanged(io.Control + NativeIoInvocation.ControlBytes, NativeIoInvocation.ControlGuard),
            "I/O control guard changed.");
        Require(GuardUnchanged(io.Payload - 16, NativeIoInvocation.PayloadGuard) &&
            GuardUnchanged(io.Payload + NativeIoInvocation.PayloadCapacity, NativeIoInvocation.PayloadGuard),
            "I/O payload guard changed.");
        var expectedPayload = (byte[])io.PayloadBefore.Clone();
        if (io.IsTransfer && io.IsRead && definition.RawResult > 0)
            io.ReadPattern.AsSpan(0, definition.RawResult).CopyTo(expectedPayload);
        Require(Bus.Memory.AsSpan((int)io.Payload, NativeIoInvocation.PayloadCapacity).SequenceEqual(expectedPayload),
            "I/O payload was changed beyond the supplied Read result or during Write/query/poll.");
        var expectedTransfer = definition.RawResult > 0 && io.IsTransfer
            ? expectedPayload.AsSpan(0, definition.RawResult).ToArray() : [];
        Require(io.TransferredBytes.SequenceEqual(expectedTransfer), "I/O vector did not preserve the transferred byte prefix.");

        var signals = Bus.Long(invocation.Process + (uint)ExecLayout.Task.SignalReceived);
        Require(signals == io.StartingSignals && signals == definition.Signals, "Polling consumed or changed a received signal.");
        var expectedProcess = (byte[])io.ProcessBefore.Clone();
        BinaryPrimitives.WriteInt32BigEndian(expectedProcess.AsSpan(DosLayout.Process.Result2, 4), IoProbePoison);
        Require(Bus.Memory.AsSpan((int)invocation.Process, 0x400).SequenceEqual(expectedProcess),
            "I/O invocation modified task fields other than its explicit current error.");
        Require(io.Task.Active && io.Task.Process == invocation.Process, "I/O completion lost its task owner.");
        io.Task.Active = false;
        io.Task.CompletedInvocations++;
        io.Task.LastProcessSnapshot = Bus.Memory.AsSpan((int)invocation.Process, 0x400).ToArray();
        io.Report = new
        {
            operation = definition.Operation,
            rawResult = unchecked((int)observed[0]),
            errorCaptured = observed[1] != 0,
            capturedIoErr = unchecked((int)observed[2]),
            observedAmbientIoErr = unchecked((int)observed[3]),
            oldIoErr = unchecked((int)observed[4]),
            initialIoErr = io.StartingError,
            signalsBefore = io.StartingSignals,
            signalsAfter = signals,
            process = invocation.Process,
            taskInvocation = io.Task.CompletedInvocations,
            retainedTaskState = io.RetainedTaskState,
            explicitHandle = io.ExplicitHandle,
            inputHandle = io.InputHandle,
            outputHandle = io.OutputHandle,
            requestHandle = io.IsTransfer ? io.TransferHandle : 0u,
            requestPointer = io.Payload,
            requestLength = definition.Length,
            readCalls = io.ReadCalls,
            writeCalls = io.WriteCalls,
            inputCalls = io.InputCalls,
            outputCalls = io.OutputCalls,
            setSignalCalls = io.SignalCalls,
            ioErrCalls = io.IoErrValues.Count,
            setIoErrCalls = io.SetIoErrCalls,
            nativeOutputBytesWritten = io.OutputWritten.Count(written => written),
            nativeOutputStoreCalls = io.OutputStoreCalls,
            transferredHex = Convert.ToHexStringLower(io.TransferredBytes)
        };
    }

    private bool GuardUnchanged(uint address, byte value) =>
        Bus.Memory.AsSpan((int)address, 16).IndexOfAnyExcept(value) < 0;
}
