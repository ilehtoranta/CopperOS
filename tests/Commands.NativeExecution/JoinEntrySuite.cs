using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied-vector fixture for the Join frontends.  It deliberately exercises
/// the command boundary and append ownership, while leaving wildcard result
/// population and real filesystem behavior to the reference-guest stage.
/// </summary>
internal sealed record JoinEntryCase(
    string[] Sources,
    int[][] Reads,
    int[][] Writes,
    int Error = 0,
    int DestinationOpenError = 0,
    int SourceOpenFailure = -1,
    int BreakSource = -1,
    int BreakRead = -1,
    int AllocationFailure = -1,
    int ParserError = 0);

internal sealed class JoinNativeLayout(uint control, uint fileVector,
    uint destinationName)
{
    public uint Control { get; } = control;
    public uint FileVector { get; } = fileVector;
    public uint DestinationName { get; } = destinationName;
    public uint ResultArray { get; set; }
    public uint RdArgs { get; set; }
    public uint Workspace { get; set; }
    public List<uint> Buffers { get; } = [];
    public int ReadArgsCalls { get; set; }
    public int FreeArgsCalls { get; set; }
    public int AllocMemCalls { get; set; }
    public int FreeMemCalls { get; set; }
    public int OpenCalls { get; set; }
    public int CloseCalls { get; set; }
    public int ReadCalls { get; set; }
    public int WriteCalls { get; set; }
    public int MatchFirstCalls { get; set; }
    public int MatchEndCalls { get; set; }
    public int DeleteCalls { get; set; }
    public int SignalCalls { get; set; }
    public int CurrentSourceIndex { get; set; } = -1;
    public int[] SourceOpenCalls { get; set; } = [];
    public int[] SourceReadIndices { get; set; } = [];
    public int[] SourceWriteIndices { get; set; } = [];
    public uint DestinationHandle { get; set; }
    public uint[] SourceHandles { get; set; } = [];
}

internal sealed partial class ProbeFixture
{
    public const string MorphOSJoinEntrySuite =
        "morphos320-join-native-entry-vector-fixture";
    public const string Workbench31JoinEntrySuite =
        "workbench31-join-native-entry-vector-fixture";

    public static bool IsJoinEntrySuite(string value) =>
        value is MorphOSJoinEntrySuite or Workbench31JoinEntrySuite;

    private static bool IsWorkbench31JoinEntrySuite(string value) =>
        value == Workbench31JoinEntrySuite;

    private List<object> RunJoinEntryCases()
    {
        static ProbeCase Case(string name, JoinEntryCase definition,
            int result = DOS.RETURN_OK, int error = 0, bool workbench = false,
            bool missingDos = false, int? entryLength = null,
            bool nullArgument = false) => new(name, "ignored\n", result, error, "")
        {
            Join = definition,
            Workbench = workbench,
            MissingDos = missingDos,
            WritesOwnProcessError = missingDos,
            EntryLength = entryLength,
            NullArgumentPointer = nullArgument
        };

        var cases = new List<ProbeCase>
        {
            Case("join-success", new(["one"], [[8, 0]], [[8, 0]])),
            Case("two-sources", new(["one", "two"], [[3, 0], [4, 0]],
                [[3, 0], [4, 0]])),
            Case("short-write", new(["one"], [[8, 0]], [[7]],
                Error: 221), result: DOS.RETURN_FAIL, error: 221),
            Case("read-failure", new(["one"], [[-1]], [[]], Error: 205),
                result: DOS.RETURN_FAIL, error: 205),
            Case("destination-open-failure", new(["one"], [[],], [[]],
                DestinationOpenError: 203), result: DOS.RETURN_FAIL, error: 203),
            Case("source-open-failure", new(["one"], [[],], [[]],
                Error: 205, SourceOpenFailure: 0), result: DOS.RETURN_FAIL,
                error: 205),
            Case("parser-failure", new(["one"], [[]], [[]], ParserError: 116),
                result: DOS.RETURN_ERROR, error: 116),
            Case("workspace-allocation-failure", new(["one"], [[]], [[]],
                Error: (int)DOS.Error.NoFreeStore, AllocationFailure: 0),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.NoFreeStore),
            Case("result-allocation-failure", new(["one"], [[]], [[]],
                Error: (int)DOS.Error.NoFreeStore, AllocationFailure: 1),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.NoFreeStore),
            Case("buffer-allocation-failure", new(["one"], [[]], [[]],
                Error: (int)DOS.Error.NoFreeStore, AllocationFailure: 2),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.NoFreeStore),
            Case("empty-source-vector", new([], [], [], Error: 0),
                result: DOS.RETURN_OK, error: 0),
            Case("break-before-read", new(["one"], [[8, 0]], [[8, 0]],
                Error: (int)DOS.Error.Break, BreakSource: 0, BreakRead: 0),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.Break),
            Case("missing-dos", new(["one"], [[]], [[]],
                Error: (int)DOS.Error.InvalidResidentLibrary),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.InvalidResidentLibrary, missingDos: true),
            Case("workbench-startup", new(["one"], [[]], [[]]),
                result: DOS.RETURN_ERROR, error: (int)DOS.Error.ObjectWrongType,
                workbench: true),
            Case("negative-entry-length", new(["one"], [[]], [[]]),
                result: DOS.RETURN_ERROR, error: (int)DOS.Error.LineTooLong,
                entryLength: -1),
            Case("null-entry-buffer", new(["one"], [[]], [[]]),
                result: DOS.RETURN_ERROR, error: (int)DOS.Error.LineTooLong,
                entryLength: 4, nullArgument: true)
        };

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-left" },
            cases[1] with { Name = "interleaved-right" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void PrepareJoinEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Join!;
        var vector = invocation.Arguments + 0x300;
        var next = vector + (uint)((definition.Sources.Length + 1) * 4);
        var destination = next;
        PutJoinString(destination, "joined");
        next = checked(destination + 7);
        var sourcePointers = new uint[definition.Sources.Length];
        for (var index = 0; index < sourcePointers.Length; index++)
        {
            sourcePointers[index] = next;
            PutJoinString(next, definition.Sources[index]);
            next = checked(next + (uint)definition.Sources[index].Length + 1);
            Bus.Long(vector + (uint)index * 4, sourcePointers[index]);
        }
        if (sourcePointers.Length != 0)
            Bus.Long(vector + (uint)sourcePointers.Length * 4, 0);
        invocation.JoinLayout = new(invocation.Arguments + 0x200, vector,
            destination)
        {
            SourceOpenCalls = new int[sourcePointers.Length],
            SourceReadIndices = new int[sourcePointers.Length],
            SourceWriteIndices = new int[sourcePointers.Length],
            SourceHandles = sourcePointers.Select((_, i) => 0x500u +
                (uint)i).ToArray()
        };
        Bus.Long(invocation.JoinLayout.Control, 0);
    }

    private void RegisterJoinEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state,
            invocation) =>
        {
            var definition = invocation.Definition.Join!;
            var layout = invocation.JoinLayout!;
            Require(state.D[0] == 0 && state.D[1] == 0,
                "Join Ctrl-C query ABI differs.");
            layout.SignalCalls++;
            var source = layout.CurrentSourceIndex;
            return source == definition.BreakSource &&
                layout.SourceReadIndices[source] == definition.BreakRead
                ? 1u << 12 : 0u;
        });
    }

    private void RegisterJoinEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.Join!;
            var layout = invocation.JoinLayout!;
            Require(Bus.CString(state.D[1]) == NativeMorphOSJoinCommand.Template &&
                state.D[3] == 0 && state.D[2] % 4 == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "JoinResults").Size == 8,
                "Join ReadArgs ABI differs.");
            layout.ReadArgsCalls++;
            layout.ResultArray = state.D[2];
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            Bus.Long(state.D[2], definition.Sources.Length == 0 ? 0u :
                layout.FileVector);
            Bus.Long(state.D[2] + 4, layout.DestinationName);
            layout.RdArgs = Bus.Allocate(invocation, 40, "JoinRdArgs", true);
            invocation.IoError = 0;
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            var layout = invocation.JoinLayout!;
            Require(state.D[1] == layout.RdArgs,
                "Join FreeArgs RDArgs ownership differs.");
            Bus.Release(invocation, state.D[1], "JoinRdArgs");
            layout.FreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Open, "Open", (state, invocation) =>
        {
            var definition = invocation.Definition.Join!;
            var layout = invocation.JoinLayout!;
            var path = Bus.CString(state.D[1]);
            layout.OpenCalls++;
            if (path == "joined")
            {
                Require(state.D[2] == unchecked((uint)DOS.FileMode.NewFile),
                    "Join destination Open mode differs.");
                invocation.IoError = definition.DestinationOpenError;
                if (definition.DestinationOpenError != 0) return 0;
                layout.DestinationHandle = 0x400;
                return layout.DestinationHandle;
            }
            var source = Array.IndexOf(definition.Sources, path);
            Require(source >= 0 && state.D[2] == unchecked((uint)DOS.FileMode.OldFile),
                "Join source Open ABI differs.");
            layout.CurrentSourceIndex = source;
            layout.SourceOpenCalls[source]++;
            if (source == definition.SourceOpenFailure)
            {
                invocation.IoError = definition.Error;
                return 0;
            }
            invocation.IoError = 0;
            return layout.SourceHandles[source];
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            var layout = invocation.JoinLayout!;
            var handle = state.D[1];
            Require(handle == layout.DestinationHandle ||
                layout.SourceHandles.Contains(handle),
                "Join Close handle differs.");
            layout.CloseCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.DeleteFile, "DeleteFile", (state,
            invocation) =>
        {
            var layout = invocation.JoinLayout!;
            Require(Bus.CString(state.D[1]) == "joined",
                "Join partial destination cleanup differs.");
            layout.DeleteCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state,
            invocation) =>
        {
            var layout = invocation.JoinLayout!;
            var definition = invocation.Definition.Join!;
            var path = Bus.CString(state.D[1]);
            Require(Array.IndexOf(definition.Sources, path) >= 0 &&
                state.D[2] != 0 &&
                Bus.Word(state.D[2] + (uint)DosLayout.AnchorPath.StringLength) == 0,
                "Join pattern classifier ABI differs.");
            Bus.Memory[state.D[2] + (uint)DosLayout.AnchorPath.Flags] =
                (byte)AnchorPathFlags.DoWild;
            layout.MatchFirstCalls++;
            invocation.IoError = 0;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state,
            invocation) =>
        {
            Require(state.D[1] != 0, "Join MatchEnd anchor differs.");
            invocation.JoinLayout!.MatchEndCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Read, "Read", (state, invocation) =>
        {
            var definition = invocation.Definition.Join!;
            var layout = invocation.JoinLayout!;
            var source = layout.CurrentSourceIndex;
            Require(source >= 0 && state.D[1] == layout.SourceHandles[source] &&
                state.D[2] == layout.Buffers[^1] &&
                state.D[3] == NativeMorphOSJoinCommand.TransferBufferBytes,
                "Join Read ABI differs.");
            var index = layout.SourceReadIndices[source]++;
            Require(index < definition.Reads[source].Length,
                "Join Read stream overrun.");
            var count = definition.Reads[source][index];
            if (count > 0)
                for (var offset = 0; offset < count; offset++)
                    Bus.Memory[state.D[2] + (uint)offset] =
                        (byte)(0x30 + source + offset);
            if (count < 0) invocation.IoError = definition.Error;
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            var definition = invocation.Definition.Join!;
            var layout = invocation.JoinLayout!;
            var source = layout.CurrentSourceIndex;
            var index = layout.SourceWriteIndices[source]++;
            Require(source >= 0 && index < definition.Writes[source].Length &&
                state.D[1] == layout.DestinationHandle &&
                state.D[2] == layout.Buffers[^1],
                "Join Write ABI differs.");
            var expected = index < definition.Reads[source].Length
                ? definition.Reads[source][index] : 0;
            var count = definition.Writes[source][index];
            Require(expected >= 0 && unchecked((int)state.D[3]) == expected,
                "Join Write length differs from Read.");
            if (expected > 0)
                for (var offset = 0; offset < expected; offset++)
                    Require(Bus.Memory[state.D[2] + (uint)offset] ==
                        (byte)(0x30 + source + offset),
                        "Join Write bytes differ from Read bytes.");
            if (count != expected) invocation.IoError = definition.Error;
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyJoinEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Join!;
        var layout = invocation.JoinLayout!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(layout.AllocMemCalls == 0 && layout.ReadArgsCalls == 0 &&
                layout.OpenCalls == 0, "Join crossed an invalid startup boundary.");
            return;
        }
        if (definition.AllocationFailure == 0)
        {
            Require(layout.AllocMemCalls == 1 && layout.FreeMemCalls == 0 &&
                layout.ReadArgsCalls == 0 && layout.FreeArgsCalls == 0,
                "Join workspace allocation failure ownership differs.");
            return;
        }
        if (definition.AllocationFailure == 1)
        {
            Require(layout.AllocMemCalls == 2 && layout.FreeMemCalls == 1 &&
                layout.ReadArgsCalls == 0 && layout.FreeArgsCalls == 0,
                "Join result allocation failure ownership differs.");
            return;
        }
        if (definition.AllocationFailure == 2)
        {
            Require(layout.AllocMemCalls == 3 && layout.FreeMemCalls == 2 &&
                layout.ReadArgsCalls == 1 && layout.FreeArgsCalls == 1 &&
                layout.OpenCalls == 2 && layout.CloseCalls == 2 &&
                layout.MatchFirstCalls == 1 && layout.MatchEndCalls == 1 &&
                layout.DeleteCalls == 1,
                "Join buffer allocation failure ownership differs.");
            return;
        }
        if (definition.ParserError != 0)
        {
            Require(layout.AllocMemCalls == 2 && layout.FreeMemCalls == 2 &&
                layout.ReadArgsCalls == 1 && layout.FreeArgsCalls == 0,
                "Join parser failure ownership differs.");
            return;
        }
        var empty = definition.Sources.Length == 0;
        var destinationFailed = definition.DestinationOpenError != 0;
        var transferFailed = destinationFailed || definition.SourceOpenFailure >= 0 ||
            definition.BreakSource >= 0 || definition.Error != 0;
        var openedSources = empty || destinationFailed ? 0 :
            definition.SourceOpenFailure >= 0 ? definition.SourceOpenFailure + 1 :
            definition.Sources.Length;
        var completedSources = empty || destinationFailed ? 0 :
            definition.SourceOpenFailure >= 0 ? definition.SourceOpenFailure :
            definition.Sources.Length;
        var allocatedBuffers = empty || destinationFailed ? 0 :
            definition.SourceOpenFailure >= 0 ? definition.SourceOpenFailure :
            definition.BreakSource >= 0 ? definition.BreakSource + 1 :
            definition.Sources.Length;
        var expectedAllocations = 2 + allocatedBuffers;
        Require(layout.ReadArgsCalls == 1 && layout.FreeArgsCalls == 1 &&
            layout.AllocMemCalls == expectedAllocations &&
            layout.FreeMemCalls == expectedAllocations &&
            layout.OpenCalls == (empty ? 0 : 1 + openedSources) &&
            layout.MatchFirstCalls == openedSources &&
            layout.MatchEndCalls == openedSources &&
            layout.CloseCalls == (empty || destinationFailed ? 0 :
                1 + completedSources) &&
            layout.DeleteCalls == (transferFailed && !empty && !destinationFailed ? 1 : 0) &&
            layout.SourceOpenCalls.Sum() == openedSources,
            $"Join lifecycle differs for {invocation.Definition.Name}: " +
            $"open={layout.OpenCalls}, close={layout.CloseCalls}, " +
            $"match={layout.MatchFirstCalls}/{layout.MatchEndCalls}, " +
            $"delete={layout.DeleteCalls}.");
    }

    private void PutJoinString(uint address, string value)
    {
        Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)value.Length] = 0;
    }
}
