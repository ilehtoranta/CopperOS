using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal enum MakeDirDirectoryOperation
{
    Lock,
    CreateDir,
    Examine,
    ChangeMode,
    CurrentDir,
    UnLock
}

internal sealed record MakeDirDirectoryStep(
    MakeDirDirectoryOperation Operation,
    string Name,
    uint Argument,
    uint Result,
    int Error = 0,
    int EntryType = 2);

internal sealed record MakeDirEntryCase(
    string[]? Names,
    bool All,
    int Result,
    int Error,
    string Output,
    MakeDirDirectoryStep[] Operations)
{
    public int ParserError { get; init; }
    public bool AllocationFailure { get; init; }
    public bool FibAllocationFailure { get; init; }
    public bool PrintFaultFailure { get; init; }
    public int[] FaultCodes { get; init; } = [];
}

internal sealed class MakeDirEntryNativeLayout(uint control, uint namesVector)
{
    public uint Control { get; } = control;
    public uint NamesVector { get; } = namesVector;
    public uint[] NamePointers { get; set; } = [];
    public uint ResultArray { get; set; }
    public uint RdArgs { get; set; }
    public uint Fib { get; set; }
    public int ReadArgsCalls { get; set; }
    public int FreeArgsCalls { get; set; }
    public int AllocMemCalls { get; set; }
    public int FreeMemCalls { get; set; }
    public int PrintFaultCalls { get; set; }
    public int DirectoryStepsConsumed { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string MakeDirEntrySuite =
        "morphos320-makedir-native-entry-vector-fixture";

    public const string Workbench31MakeDirEntrySuite =
        "workbench31-makedir-native-entry-vector-fixture";

    public static bool IsMakeDirEntrySuite(string value) =>
        value == MakeDirEntrySuite || value == Workbench31MakeDirEntrySuite;

    public static bool IsWorkbench31MakeDirEntrySuite(string value) =>
        value == Workbench31MakeDirEntrySuite;

    private List<object> RunMakeDirEntryCases()
    {
        if (IsWorkbench31MakeDirEntrySuite(suite))
            return RunWorkbench31MakeDirEntryCases();

        static MakeDirDirectoryStep Create(string name, uint result,
            int error = 0) => new(MakeDirDirectoryOperation.CreateDir, name,
            0, result, error);
        static MakeDirDirectoryStep Lock(string name, uint result,
            int error = 205) => new(MakeDirDirectoryOperation.Lock, name,
            unchecked((uint)DOS.LockMode.Read), result, error);
        static MakeDirDirectoryStep Current(uint argument, uint result,
            int error = 0) => new(MakeDirDirectoryOperation.CurrentDir, "",
            argument, result, error);
        static MakeDirDirectoryStep ChangeMode(uint argument, uint result = 1,
            int error = 0) => new(MakeDirDirectoryOperation.ChangeMode, "",
            argument, result, error);
        static MakeDirDirectoryStep Unlock(uint argument) =>
            new(MakeDirDirectoryOperation.UnLock, "", argument, 0);

        static ProbeCase Case(string name, MakeDirEntryCase definition,
            bool workbench = false, bool missingDos = false,
            int? entryLength = null, bool nullArgument = false) =>
            new(name, "ignored\n", definition.Result, definition.Error,
                definition.Output)
            {
                MakeDir = definition,
                Workbench = workbench,
                MissingDos = missingDos,
                EntryLength = entryLength,
                NullArgumentPointer = nullArgument
            };

        var cases = new List<ProbeCase>
        {
            Case("no-name", new(null, false, 20, 0, "No name given\n", []), false, false),
            Case("empty-vector", new([], false, 20, 0, "", []), false, false),
            Case("create-success", new(["new"], false, 0, 0, "",
                [Create("new", 0x151), Unlock(0x151)])),
            Case("create-failure", new(["bad"], false, 10, 221,
                "Cannot create directory bad\n",
                [Create("bad", 0, 221)]) { FaultCodes = [221] }),
            Case("failure-then-success", new(["bad", "good"], false, 0, 0,
                "Cannot create directory bad\n",
                [Create("bad", 0, 221), Create("good", 0x152), Unlock(0x152)])),
            Case("parser-failure", new(null, false, 20, 119, "", [])
                { ParserError = 119, FaultCodes = [119] }),
            Case("result-allocation-failure", new(null, false, 20, 103, "", [])
                { AllocationFailure = true, FaultCodes = [103] }),
            Case("fib-allocation-failure", new(["new"], true, 20, 103, "", [])
                { FibAllocationFailure = true, FaultCodes = [103] }),
            Case("missing-dos", new(null, false, 20, Invocation.InitialIoError,
                "", []), missingDos: true),
            Case("workbench-startup", new(null, false, 10,
                (int)DOS.Error.ObjectWrongType, "", []), workbench: true),
            Case("negative-entry-length", new(null, false, 10,
                (int)DOS.Error.LineTooLong, "", []), entryLength: -1),
            Case("null-entry-buffer", new(null, false, 10,
                (int)DOS.Error.LineTooLong, "", []), entryLength: 4,
                nullArgument: true),
            // One complete ALL walk through a volume and a new leaf. The
            // sequence is deliberately explicit: it is a supplied public-DOS
            // vector, not a filesystem adapter or a claim about packed output.
            Case("all-volume-new-leaf", new(["RAM:new"], true, 0, 0, "",
                [
                    Current(0, 0x501),
                    Lock("RAM:", 0x502),
                    Current(0x502, 0x501),
                    Lock("new", 0),
                    Create("new", 0x503),
                    ChangeMode(0x503),
                    Current(0x503, 0x502),
                    Unlock(0x502),
                    Current(0x501, 0x503),
                    Unlock(0x503)
                ]))
        };

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            Case("interleaved-left", new(["left"], false, 0, 0, "",
                [Create("left", 0x161), Unlock(0x161)])),
            Case("interleaved-right", new(["right"], false, 10, 222,
                "Cannot create directory right\n",
                [Create("right", 0, 222)]) { FaultCodes = [222] })
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void PrepareMakeDirEntry(Invocation invocation)
    {
        var definition = invocation.Definition.MakeDir!;
        var control = invocation.Process + 0x200;
        var vector = invocation.StackTop - 0x300;
        var next = vector + checked((uint)((definition.Names?.Length ?? 0) + 1) * 4);
        var pointers = new List<uint>();
        if (definition.Names is { } names)
        {
            for (var index = 0; index < names.Length; index++)
            {
                var bytes = Encoding.Latin1.GetBytes(names[index]);
                pointers.Add(next);
                bytes.CopyTo(Bus.Memory.AsSpan((int)next));
                Bus.Memory[next + (uint)bytes.Length] = 0;
                next = checked(next + (uint)bytes.Length + 1);
                Bus.Long(vector + (uint)index * 4, pointers[^1]);
            }
            Bus.Long(vector + (uint)names.Length * 4, 0);
        }
        var layout = new MakeDirEntryNativeLayout(control, vector)
        {
            NamePointers = pointers.ToArray()
        };
        invocation.MakeDirLayout = layout;
        Bus.Long(control, 0);
    }

    private void RegisterMakeDirEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.MakeDir!;
            var layout = invocation.MakeDirLayout!;
            var template = IsWorkbench31MakeDirEntrySuite(suite)
                ? Workbench31MakeDirCommand.Template
                : NativeMorphOSMakeDirCommand.Template;
            var resultCount = IsWorkbench31MakeDirEntrySuite(suite)
                ? Workbench31MakeDirCommand.ResultCount
                : NativeMorphOSMakeDirCommand.ResultCount;
            Require(Bus.CString(state.D[1]) == template &&
                state.D[3] == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "MakeDirResult").Size ==
                    resultCount * 4u,
                "MakeDir template/result ABI differs.");
            layout.ResultArray = state.D[2];
            layout.ReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            Bus.Long(state.D[2], definition.Names is null ? 0 :
                layout.NamesVector);
            if (!IsWorkbench31MakeDirEntrySuite(suite))
                Bus.Long(state.D[2] + 4, definition.All ? uint.MaxValue : 0);
            invocation.IoError = 0;
            layout.RdArgs = Bus.Allocate(invocation, 32, "MakeDirRdArgs", true);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            var layout = invocation.MakeDirLayout!;
            Require(layout.RdArgs == state.D[1] && layout.FreeArgsCalls == 0,
                "MakeDir FreeArgs ownership differs.");
            Bus.Release(invocation, state.D[1], "MakeDirRdArgs", 32);
            layout.FreeArgsCalls++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
            ConsumeMakeDirStep(invocation, state,
                MakeDirDirectoryOperation.Lock));
        Register(baseAddress, DosLvo.CreateDir, "CreateDir", (state,
            invocation) => ConsumeMakeDirStep(invocation, state,
                MakeDirDirectoryOperation.CreateDir));
        Register(baseAddress, DosLvo.Examine, "Examine", (state,
            invocation) =>
        {
            var step = ConsumeMakeDirStepDefinition(invocation, state,
                MakeDirDirectoryOperation.Examine);
            var layout = invocation.MakeDirLayout!;
            Require(state.D[2] == layout.Fib, "MakeDir Examine FIB differs.");
            Bus.Long(state.D[2] + (uint)FileInfoBlock.DirEntryTypeOffset,
                unchecked((uint)step.EntryType));
            return step.Result;
        });
        Register(baseAddress, DosLvo.ChangeMode, "ChangeMode", (state,
            invocation) =>
        {
            var step = ConsumeMakeDirStepDefinition(invocation, state,
                MakeDirDirectoryOperation.ChangeMode);
            Require(state.D[1] == (uint)DosChangeModeTarget.Lock &&
                state.D[2] == step.Argument &&
                state.D[3] == unchecked((uint)DOS.LockMode.Read),
                "MakeDir ChangeMode ABI differs.");
            return step.Result;
        });
        Register(baseAddress, DosLvo.CurrentDir, "CurrentDir", (state,
            invocation) =>
        {
            var step = ConsumeMakeDirStepDefinition(invocation, state,
                MakeDirDirectoryOperation.CurrentDir);
            Require(state.D[1] == step.Argument, "MakeDir CurrentDir lock differs.");
            return step.Result;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state,
            invocation) =>
        {
            var step = ConsumeMakeDirStepDefinition(invocation, state,
                MakeDirDirectoryOperation.UnLock);
            Require(state.D[1] == step.Argument, "MakeDir UnLock ownership differs.");
            return step.Result;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr &&
                Bus.CString(state.D[2]) == "No name given\n",
                "MakeDir no-name FPuts ABI differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes("No name given\n"));
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state,
            invocation) =>
        {
            var name = Bus.CString(Bus.Long(state.D[2]));
            var format = Bus.CString(state.D[1]);
            if (IsWorkbench31MakeDirEntrySuite(suite))
            {
                Require(format is "No name given\n" or
                    "Can't create directory %s\n" or "%s already exists\n",
                    "Workbench MakeDir format differs.");
                var output = format == "No name given\n"
                    ? "No name given\n"
                    : format.StartsWith("%s", StringComparison.Ordinal)
                        ? $"{name} already exists\n"
                        : $"Can't create directory {name}\n";
                if (format == "No name given\n")
                    Require(state.D[2] == 0,
                        "Workbench MakeDir no-name vector differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(output));
            }
            else
            {
                Require(format == "Cannot create directory %s\n",
                    "MakeDir failure format differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"Cannot create directory {name}\n"));
            }
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state,
            invocation) =>
        {
            var definition = invocation.Definition.MakeDir!;
            var code = unchecked((int)state.D[1]);
            Require(state.D[2] == 0 && definition.FaultCodes.Contains(code) &&
                invocation.MakeDirLayout!.PrintFaultCalls == 0,
                "MakeDir PrintFault request differs.");
            invocation.MakeDirLayout!.PrintFaultCalls++;
            invocation.IoError = code;
            return definition.PrintFaultFailure ? 0u : 1u;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            var previous = invocation.IoError;
            invocation.IoError = unchecked((int)state.D[1]);
            return unchecked((uint)previous);
        });
    }

    private uint ConsumeMakeDirStep(Invocation invocation,
        Copper68k.M68kCpuState state, MakeDirDirectoryOperation operation)
    {
        var step = ConsumeMakeDirStepDefinition(invocation, state, operation);
        var name = Bus.CString(state.D[1]);
        Require(name == step.Name, $"MakeDir {operation} name differs.");
        return step.Result;
    }

    private MakeDirDirectoryStep ConsumeMakeDirStepDefinition(Invocation invocation,
        Copper68k.M68kCpuState state, MakeDirDirectoryOperation operation)
    {
        var definition = invocation.Definition.MakeDir!;
        var index = invocation.MakeDirLayout!.DirectoryStepsConsumed;
        Require(index < definition.Operations.Length,
            $"Unexpected MakeDir {operation} call.");
        var step = definition.Operations[index];
        Require(step.Operation == operation,
            $"{invocation.Definition.Name}: MakeDir operation {operation} arrived as {step.Operation} at index {index}.");
        invocation.MakeDirLayout.DirectoryStepsConsumed++;
        invocation.IoError = step.Error;
        return step;
    }

    private void VerifyMakeDirEntry(Invocation invocation)
    {
        var definition = invocation.Definition.MakeDir!;
        var layout = invocation.MakeDirLayout!;
        if (invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos)
        {
            Require(layout.ReadArgsCalls == 0 &&
                layout.DirectoryStepsConsumed == 0,
                "MakeDir crossed an invalid startup boundary.");
            return;
        }
        if (definition.AllocationFailure)
        {
            Require(layout.ReadArgsCalls == 0 && layout.AllocMemCalls == 1,
                "MakeDir result allocation failure crossed ReadArgs.");
            return;
        }
        if (definition.ParserError != 0)
        {
            Require(layout.ReadArgsCalls == 1 && layout.FreeArgsCalls == 0 &&
                layout.DirectoryStepsConsumed == 0 && layout.AllocMemCalls == 1 &&
                layout.FreeMemCalls == 1 &&
                layout.PrintFaultCalls == definition.FaultCodes.Length,
                "MakeDir parser failure crossed its ownership boundary.");
            return;
        }
        Require(layout.ReadArgsCalls == 1 && layout.FreeArgsCalls == 1 &&
            layout.DirectoryStepsConsumed == definition.Operations.Length &&
            layout.AllocMemCalls == (definition.All ? 2 : 1) &&
            layout.FreeMemCalls == (definition.FibAllocationFailure ? 1 :
                layout.AllocMemCalls) &&
            layout.PrintFaultCalls == definition.FaultCodes.Length,
            $"{invocation.Definition.Name}: MakeDir parser, directory or cleanup lifetime differs: read={layout.ReadArgsCalls}, freeArgs={layout.FreeArgsCalls}, steps={layout.DirectoryStepsConsumed}/{definition.Operations.Length}, alloc={layout.AllocMemCalls}, freeMem={layout.FreeMemCalls}, faults={layout.PrintFaultCalls}/{definition.FaultCodes.Length}, events={string.Join(',', invocation.Events)}.");
        if (definition.FibAllocationFailure)
            Require(definition.All && definition.Operations.Length == 0,
                "MakeDir FIB failure traversed directory operations.");
    }
}
