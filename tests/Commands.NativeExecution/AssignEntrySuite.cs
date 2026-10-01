using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal enum AssignEntryMode
{
    Lock,
    Add,
    Remove,
    Path,
    Defer,
    Unsupported
}

internal sealed record AssignEntryCase(
    string Name,
    string[] Targets,
    AssignEntryMode Mode,
    bool TargetLockSucceeds = true,
    bool OperationSucceeds = true,
    int Error = 0,
    int ParserError = 0)
{
    public bool AllocationFailure { get; init; }
}

internal sealed class AssignNativeLayout(uint control, uint name,
    uint targetVector)
{
    public uint Control { get; } = control;
    public uint Name { get; } = name;
    public uint TargetVector { get; } = targetVector;
    public uint[] TargetPointers { get; set; } = [];
    public uint ResultArray { get; set; }
    public uint RdArgs { get; set; }
    public int ReadArgsCalls { get; set; }
    public int FreeArgsCalls { get; set; }
    public int AllocMemCalls { get; set; }
    public int FreeMemCalls { get; set; }
    public int LockCalls { get; set; }
    public int UnLockCalls { get; set; }
    public int OperationCalls { get; set; }
    public int PrintFaultCalls { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string Workbench31AssignEntrySuite =
        "workbench31-assign-native-entry-vector-fixture";
    public const string MorphOSAssignEntrySuite =
        "morphos320-assign-native-entry-vector-fixture";

    public static bool IsAssignEntrySuite(string value) =>
        value is Workbench31AssignEntrySuite or MorphOSAssignEntrySuite;

    public static bool IsWorkbench31AssignEntrySuite(string value) =>
        value == Workbench31AssignEntrySuite;

    private void RegisterAssignEntryExec()
    {
        // Assign uses only DOS vectors after startup. Keep this hook explicit
        // so adding resource or handler calls cannot silently widen coverage.
    }

    private List<object> RunAssignEntryCases() =>
        IsWorkbench31AssignEntrySuite(suite)
            ? RunAssignProfileCases(true)
            : RunAssignProfileCases(false);

    private List<object> RunAssignProfileCases(bool workbench)
    {
        static ProbeCase Case(string label, AssignEntryCase value,
            bool startup = false, bool missingDos = false,
            int? entryLength = null, bool nullArgument = false)
        {
            var failed = missingDos || value.AllocationFailure ||
                value.Mode == AssignEntryMode.Unsupported || value.Targets.Length == 0 ||
                (value.Mode is AssignEntryMode.Path or AssignEntryMode.Defer &&
                    value.Targets.Length != 1) || value.ParserError != 0 ||
                !value.OperationSucceeds || !value.TargetLockSucceeds;
            var result = failed ? DOS.RETURN_FAIL :
                startup || entryLength is < 0 || nullArgument
                    ? DOS.RETURN_ERROR : DOS.RETURN_OK;
            var error = missingDos ? (int)DOS.Error.InvalidResidentLibrary :
                startup ? (int)DOS.Error.ObjectWrongType :
                entryLength is < 0 || nullArgument ? (int)DOS.Error.LineTooLong :
                value.ParserError != 0 ? value.ParserError : value.Error;
            return new ProbeCase(label, "ignored\n", result, error, "")
            {
                Assign = value,
                Workbench = startup,
                MissingDos = missingDos,
                WritesOwnProcessError = missingDos,
                EntryLength = entryLength,
                NullArgumentPointer = nullArgument
            };
        }

        var cases = new List<ProbeCase>
        {
            Case("lock-success", new("Work", ["DH0:"],
                AssignEntryMode.Lock)),
            Case("add-success", new("Work", ["DH0:"],
                AssignEntryMode.Add)),
            Case("remove-success", new("Work", ["DH0:"],
                AssignEntryMode.Remove)),
            Case("path-success", new("Work", ["DH0:"],
                AssignEntryMode.Path)),
            Case("defer-success", new("Work", ["DH0:"],
                AssignEntryMode.Defer)),
            Case("multi-add-success", new("Work", ["DH0:", "DH1:"],
                AssignEntryMode.Add)),
            Case("target-lock-failure", new("Work", ["Missing"],
                AssignEntryMode.Lock, false, false, 205)),
            Case("operation-failure", new("Work", ["DH0:"],
                AssignEntryMode.Lock, true, false, 203)),
            Case("unsupported-mode", new("Work", ["DH0:"],
                AssignEntryMode.Unsupported, Error: (int)DOS.Error.BadTemplate)),
            Case("parser-failure", new("Work", ["DH0:"],
                AssignEntryMode.Lock, ParserError: 116)),
            Case("result-allocation-failure", new AssignEntryCase("Work",
                ["DH0:"], AssignEntryMode.Lock,
                Error: (int)DOS.Error.NoFreeStore)
                { AllocationFailure = true }),
            Case("empty-target-vector", new("Work", [],
                AssignEntryMode.Lock, Error: (int)DOS.Error.BadTemplate)),
            Case("path-multiple-targets", new("Work", ["DH0:", "DH1:"],
                AssignEntryMode.Path, Error: (int)DOS.Error.BadTemplate)),
            Case("missing-dos", new("Work", ["DH0:"],
                AssignEntryMode.Lock, Error: (int)DOS.Error.InvalidResidentLibrary), missingDos: true),
            Case("workbench-startup", new("Work", ["DH0:"],
                AssignEntryMode.Lock), startup: true),
            Case("negative-entry-length", new("Work", ["DH0:"],
                AssignEntryMode.Lock), entryLength: -1),
            Case("null-entry-buffer", new("Work", ["DH0:"],
                AssignEntryMode.Lock), entryLength: 4, nullArgument: true)
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

    private void PrepareAssignEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Assign!;
        var control = invocation.Process + 0x200;
        var vector = invocation.StackTop - 0x600;
        var next = checked(vector + (uint)((definition.Targets.Length + 1) * 4));
        var name = next;
        PutAssignString(name, definition.Name);
        next = checked(name + (uint)definition.Name.Length + 1);
        var pointers = new uint[definition.Targets.Length];
        for (var index = 0; index < definition.Targets.Length; index++)
        {
            pointers[index] = next;
            PutAssignString(next, definition.Targets[index]);
            next = checked(next + (uint)definition.Targets[index].Length + 1);
            Bus.Long(vector + (uint)index * 4, pointers[index]);
        }
        Bus.Long(vector + (uint)definition.Targets.Length * 4, 0);
        invocation.AssignLayout = new AssignNativeLayout(control, name, vector)
        {
            TargetPointers = pointers
        };
        Bus.Long(control, 0);
    }

    private void RegisterAssignEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.Assign!;
            var layout = invocation.AssignLayout!;
            Require(Bus.CString(state.D[1]) ==
                (IsWorkbench31AssignEntrySuite(suite)
                    ? NativeWorkbench31AssignCommand.Template
                    : NativeMorphOSAssignCommand.Template) &&
                state.D[3] == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "AssignResult").Size ==
                    (IsWorkbench31AssignEntrySuite(suite)
                        ? NativeWorkbench31AssignCommand.ResultCount
                        : NativeMorphOSAssignCommand.ResultCount) * 4u,
                "Assign template/result ABI differs.");
            layout.ResultArray = state.D[2];
            layout.ReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }

            Bus.Long(state.D[2], layout.Name);
            Bus.Long(state.D[2] + 4, definition.Targets.Length == 0
                ? 0u : layout.TargetVector);
            var dismountIndex = IsWorkbench31AssignEntrySuite(suite) ? 4u : 3u;
            var deferIndex = IsWorkbench31AssignEntrySuite(suite) ? 5u : 4u;
            var pathIndex = IsWorkbench31AssignEntrySuite(suite) ? 6u : 5u;
            var addIndex = IsWorkbench31AssignEntrySuite(suite) ? 7u : 6u;
            var removeIndex = IsWorkbench31AssignEntrySuite(suite) ? 8u : 7u;
            var volumesIndex = IsWorkbench31AssignEntrySuite(suite) ? 9u : 8u;
            var dirsIndex = IsWorkbench31AssignEntrySuite(suite) ? 10u : 9u;
            var devicesIndex = IsWorkbench31AssignEntrySuite(suite) ? 11u : 10u;
            Bus.Long(state.D[2] + 8,
                definition.Mode == AssignEntryMode.Unsupported ? 1u : 0u);
            Bus.Long(state.D[2] + dismountIndex * 4, 0);
            Bus.Long(state.D[2] + deferIndex * 4,
                definition.Mode == AssignEntryMode.Defer ? 1u : 0u);
            Bus.Long(state.D[2] + pathIndex * 4,
                definition.Mode == AssignEntryMode.Path ? 1u : 0u);
            Bus.Long(state.D[2] + addIndex * 4,
                definition.Mode == AssignEntryMode.Add ? 1u : 0u);
            Bus.Long(state.D[2] + removeIndex * 4,
                definition.Mode == AssignEntryMode.Remove ? 1u : 0u);
            Bus.Long(state.D[2] + volumesIndex * 4, 0);
            Bus.Long(state.D[2] + dirsIndex * 4, 0);
            Bus.Long(state.D[2] + devicesIndex * 4, 0);
            invocation.IoError = 0;
            layout.RdArgs = Bus.Allocate(invocation, 32, "AssignRdArgs", true);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            var layout = invocation.AssignLayout!;
            layout.FreeArgsCalls++;
            if (state.D[1] != 0)
            {
                Require(state.D[1] == layout.RdArgs,
                    "Assign FreeArgs RDArgs ownership differs.");
                Bus.Release(invocation, state.D[1], "AssignRdArgs", 32);
            }
            return 0;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var definition = invocation.Definition.Assign!;
            var layout = invocation.AssignLayout!;
            var index = layout.LockCalls++;
            Require(index < definition.Targets.Length &&
                Bus.CString(state.D[1]) == definition.Targets[index] &&
                state.D[2] == unchecked((uint)DOS.LockMode.Shared),
                "Assign target Lock ABI differs.");
            invocation.IoError = definition.Error;
            return definition.TargetLockSucceeds ? 0x301u + (uint)index : 0;
        });
        Register(baseAddress, DosLvo.AssignLock, "AssignLock", (state,
            invocation) => ConsumeAssignOperation(invocation, state,
                AssignEntryMode.Lock));
        Register(baseAddress, DosLvo.AssignAdd, "AssignAdd", (state,
            invocation) => ConsumeAssignOperation(invocation, state,
                AssignEntryMode.Add));
        Register(baseAddress, DosLvo.RemAssignList, "RemAssignList",
            (state, invocation) => ConsumeAssignOperation(invocation, state,
                AssignEntryMode.Remove));
        Register(baseAddress, DosLvo.AssignPath, "AssignPath", (state,
            invocation) => ConsumeAssignStringOperation(invocation, state,
                AssignEntryMode.Path));
        Register(baseAddress, DosLvo.AssignLate, "AssignLate", (state,
            invocation) => ConsumeAssignStringOperation(invocation, state,
                AssignEntryMode.Defer));
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            var layout = invocation.AssignLayout!;
            Require(layout.UnLockCalls < layout.LockCalls && state.D[1] >= 0x301,
                "Assign UnLock ownership differs.");
            layout.UnLockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state,
            invocation) =>
        {
            var definition = invocation.Definition.Assign!;
            var expected = definition.ParserError != 0
                ? definition.ParserError
                : definition.Error != 0 ? definition.Error :
                    (int)DOS.Error.BadTemplate;
            Require(unchecked((int)state.D[1]) == expected && state.D[2] == 0,
                "Assign PrintFault request differs.");
            invocation.AssignLayout!.PrintFaultCalls++;
            invocation.IoError = expected;
            return 1;
        });
    }

    private uint ConsumeAssignOperation(Invocation invocation,
        Copper68k.M68kCpuState state, AssignEntryMode mode)
    {
        var definition = invocation.Definition.Assign!;
        var layout = invocation.AssignLayout!;
        Require(definition.Mode == mode && layout.OperationCalls < definition.Targets.Length &&
            Bus.CString(state.D[1]) == definition.Name &&
            state.D[2] >= 0x301,
            "Assign mutation ABI differs.");
        layout.OperationCalls++;
        invocation.IoError = definition.Error;
        return definition.OperationSucceeds ? 1u : 0u;
    }

    private uint ConsumeAssignStringOperation(Invocation invocation,
        Copper68k.M68kCpuState state, AssignEntryMode mode)
    {
        var definition = invocation.Definition.Assign!;
        var layout = invocation.AssignLayout!;
        Require(definition.Mode == mode && layout.OperationCalls < definition.Targets.Length &&
            Bus.CString(state.D[1]) == definition.Name &&
            Bus.CString(state.D[2]) == definition.Targets[0],
            "Assign string mutation ABI differs.");
        layout.OperationCalls++;
        invocation.IoError = definition.Error;
        return definition.OperationSucceeds ? 1u : 0u;
    }

    private void VerifyAssignEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Assign!;
        var layout = invocation.AssignLayout!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(layout.ReadArgsCalls == 0 && layout.OperationCalls == 0,
                "Assign crossed an invalid startup boundary.");
            return;
        }
        if (definition.AllocationFailure)
        {
            Require(layout.ReadArgsCalls == 0 && layout.AllocMemCalls == 1 &&
                layout.FreeMemCalls == 0 && layout.PrintFaultCalls == 1 &&
                layout.FreeArgsCalls == (IsWorkbench31AssignEntrySuite(suite) ? 1 : 0),
                "Assign allocation failure crossed ReadArgs.");
            return;
        }
        if (definition.ParserError != 0)
        {
            Require(layout.ReadArgsCalls == 1 &&
                layout.FreeArgsCalls == (IsWorkbench31AssignEntrySuite(suite) ? 1 : 0) &&
                layout.FreeMemCalls == 1 && layout.OperationCalls == 0 &&
                layout.PrintFaultCalls == 1,
                "Assign parser failure ownership differs.");
            return;
        }
        var unsupported = definition.Mode == AssignEntryMode.Unsupported ||
            definition.Targets.Length == 0 ||
            (definition.Mode is AssignEntryMode.Path or AssignEntryMode.Defer &&
                definition.Targets.Length != 1);
        var expectedLocks = unsupported || definition.Mode is AssignEntryMode.Path or
            AssignEntryMode.Defer ? 0 : definition.Targets.Length;
        var expectedUnlocks = unsupported || definition.Mode is AssignEntryMode.Path or
            AssignEntryMode.Defer ? 0 :
            definition.OperationSucceeds && definition.Mode != AssignEntryMode.Remove
                ? 0 : definition.TargetLockSucceeds ? definition.Targets.Length : 0;
        var expectedOperations = unsupported || !definition.TargetLockSucceeds ? 0 :
            definition.Mode is AssignEntryMode.Path or AssignEntryMode.Defer
                ? 1 : definition.Targets.Length;
        Require(layout.ReadArgsCalls == 1 && layout.FreeArgsCalls == 1 &&
            layout.FreeMemCalls == 1 && layout.LockCalls == expectedLocks &&
            layout.UnLockCalls == expectedUnlocks &&
            layout.OperationCalls == expectedOperations &&
            layout.PrintFaultCalls == (unsupported || !definition.OperationSucceeds ||
                !definition.TargetLockSucceeds ? 1 : 0),
            $"Assign ownership differs for {invocation.Definition.Name}: " +
            $"locks={layout.LockCalls}/{expectedLocks}, unlocks={layout.UnLockCalls}/{expectedUnlocks}, " +
            $"ops={layout.OperationCalls}, faults={layout.PrintFaultCalls}.");
    }

    private void PutAssignString(uint address, string value)
    {
        Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)value.Length] = 0;
    }
}
