using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record DeleteCommandProbeCase(string[]? Inputs, bool All = false,
    bool Quiet = true, bool Force = false, bool FollowLinks = false,
    int ParserError = 0, int FailIndex = -1,
    int DeleteError = (int)DOS.Error.DeleteProtected,
    bool ParsedEmptyVector = false);

internal sealed class DeleteCommandNativeLayout
{
    public uint ResultArray, RdArgs, Anchor;
    public int MatchFirstCalls, MatchNextCalls, MatchEndCalls;
    public int Locks, ParentLocks, Unlocks, Deletes, SetProtections;
    public int CurrentIndex = -1;
    public string CurrentSource = "";
}

internal sealed partial class ProbeFixture
{
    public const string DeleteCommandProbeSuite =
        "delete-command-native-entry-vector-fixture";
    public const string WorkbenchDeleteCommandProbeSuite =
        "wb31-delete-command-native-entry-vector-fixture";

    public static bool IsDeleteCommandEntrySuite(string value) =>
        value == DeleteCommandProbeSuite || value == WorkbenchDeleteCommandProbeSuite;

    public static bool IsWorkbenchDeleteCommandEntrySuite(string value) =>
        value == WorkbenchDeleteCommandProbeSuite;

    private List<object> RunDeleteCommandProbeCases()
    {
        ProbeCase[] cases =
        [
            Case("one-file", ["Work:One"]),
            Case("two-files", ["Work:One", "Work:Two"]),
            Case("force-file", ["Work:Protected"], force: true),
            Case("failed-then-success", ["Work:Denied", "Work:Two"], failIndex: 0),
            Case("parser-failure", null, parserError: 116),
            Case("empty-vector", [], parsedEmptyVector: true)
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute(
        [
            Case("interleaved-left", ["Work:One"]),
            Case("interleaved-right", ["Work:Denied"], failIndex: 0)
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Case(string name, string[]? inputs,
        bool force = false, int parserError = 0, int failIndex = -1,
        bool parsedEmptyVector = false) =>
        new(name, inputs is null ? "" : string.Join(" ", inputs),
            parserError != 0 ? DOS.RETURN_ERROR :
                parsedEmptyVector ? DOS.RETURN_FAIL :
                failIndex >= 0 ? DOS.RETURN_WARN : DOS.RETURN_OK,
            parserError != 0 ? parserError :
                failIndex >= 0 ? (int)DOS.Error.DeleteProtected :
                parsedEmptyVector ? (int)DOS.Error.BadTemplate : Invocation.InitialIoError,
            "")
        {
            DeleteCommand = new(inputs, Force: force, ParserError: parserError,
                FailIndex: failIndex, ParsedEmptyVector: parsedEmptyVector)
        };

    private void PrepareDeleteCommand(Invocation invocation)
    {
        // The command fixture supplies the DOS parser result, while the native
        // entry still reads the real startup bytes and exact template.
        var definition = invocation.Definition.DeleteCommand ??
            throw new InvalidOperationException("Missing Delete command definition.");
        if (definition.Inputs is not { Length: > 0 }) return;
        var text = invocation.Arguments + 128;
        foreach (var input in definition.Inputs)
        {
            Encoding.Latin1.GetBytes(input).CopyTo(Bus.Memory.AsSpan((int)text));
            Bus.Memory[text + (uint)input.Length] = 0;
            text += (uint)input.Length + 1;
        }
    }

    private void VerifyDeleteCommand(Invocation invocation)
    {
        var definition = invocation.Definition.DeleteCommand ??
            throw new InvalidOperationException("Missing Delete command definition.");
        var layout = invocation.DeleteCommandLayout;
        if (definition.ParserError != 0)
        {
            Require(invocation.Reads == 1 && invocation.FreeArgs == 0 &&
                invocation.Allocations == 2 && invocation.FreeMem == 2 &&
                layout is null, "Delete parser failure cleanup differs.");
            return;
        }
        if (definition.ParsedEmptyVector)
        {
            Require(invocation.Reads == 1 && invocation.FreeArgs == 1 &&
                invocation.Allocations == 2 && invocation.FreeMem == 2 &&
                layout is { MatchFirstCalls: 0, MatchNextCalls: 0,
                    MatchEndCalls: 0, Deletes: 0 },
                "Delete empty FILE vector cleanup differs.");
            invocation.DeleteCommandLayout = null;
            return;
        }
        var inputs = definition.Inputs!;
        if (layout is null) throw new InvalidOperationException(
            "Delete command layout was not retained.");
        Require(invocation.Reads == 1 && invocation.FreeArgs == 1 &&
            invocation.Allocations == 2 + inputs.Length &&
            invocation.FreeMem == invocation.Allocations,
            "Delete parser or workspace lifetime differs.");
        Require(layout.MatchFirstCalls == inputs.Length &&
            layout.MatchNextCalls == inputs.Length &&
            layout.MatchEndCalls == inputs.Length &&
            layout.Locks == inputs.Length &&
            layout.ParentLocks == inputs.Length &&
            layout.Unlocks == inputs.Length * 2 &&
            layout.Deletes == inputs.Length &&
            layout.SetProtections == (definition.Force ? inputs.Length : 0),
            "Delete matcher/lock/delete ordering differs.");
        Require(invocation.Events.IndexOf("ReadArgs") <
            invocation.Events.IndexOf("MatchFirst") &&
            invocation.Events.IndexOf("FreeArgs") <
            invocation.Events.LastIndexOf("FreeMem"),
            "Delete parser teardown ordering differs.");
        invocation.DeleteCommandLayout = null;
    }

    private void RegisterDeleteCommandDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var template = IsWorkbenchDeleteCommandEntrySuite(suite)
                ? "FILE/M/A,ALL/S,QUIET/S,FORCE/S"
                : "FILE/M/A,ALL/S,QUIET/S,FORCE/S,FOLLOWLINKS/S";
            var definition = invocation.Definition.DeleteCommand!;
            Require(Bus.CString(state.D[1]) == template && state.D[3] == 0,
                "Delete ReadArgs template or RDArgs ABI differs.");
            var resultBytes = IsWorkbenchDeleteCommandEntrySuite(suite) ? 16u : 20u;
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == resultBytes,
                "Delete ReadArgs result storage size differs.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "DeleteCommandRDArgs", true);
            var layout = new DeleteCommandNativeLayout
            {
                ResultArray = state.D[2],
                RdArgs = rdArgs
            };
            invocation.DeleteCommandLayout = layout;
            if (definition.ParsedEmptyVector)
            {
            for (var index = 0u; index < resultBytes / 4; index++)
                Bus.Long(state.D[2] + index * 4, 0);
            return rdArgs;
            }
            var vector = rdArgs + 8;
            var text = invocation.Arguments + 128;
            var inputs = definition.Inputs!;
            for (var index = 0; index < inputs.Length; index++)
            {
                Bus.Long(vector + (uint)(index * 4), text);
                text += (uint)inputs[index].Length + 1;
            }
            Bus.Long(vector + (uint)(inputs.Length * 4), 0);
            Bus.Long(state.D[2], vector);
            Bus.Long(state.D[2] + 4, definition.All ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + 8, definition.Quiet ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + 12, definition.Force ? uint.MaxValue : 0);
            if (!IsWorkbenchDeleteCommandEntrySuite(suite))
                Bus.Long(state.D[2] + 16, definition.FollowLinks ? uint.MaxValue : 0);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Require(invocation.DeleteCommandLayout is { RdArgs: var rdArgs } &&
                state.D[1] == rdArgs, "Delete FreeArgs object differs.");
            Bus.Release(invocation, state.D[1], "DeleteCommandRDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state, invocation) =>
        {
            var definition = invocation.Definition.DeleteCommand!;
            var layout = invocation.DeleteCommandLayout!;
            Require(layout.MatchFirstCalls < definition.Inputs!.Length &&
                invocation.FreeArgs == 0 && state.D[2] != 0,
                "Delete MatchFirst ordering differs.");
            layout.CurrentIndex = layout.MatchFirstCalls++;
            layout.CurrentSource = Bus.CString(state.D[1]);
            Require(layout.CurrentSource == definition.Inputs[layout.CurrentIndex],
                "Delete source vector order differs.");
            layout.Anchor = state.D[2];
            var fib = state.D[2] + (uint)DosLayout.AnchorPath.Info;
            Bus.Long(fib + FileInfoBlock.DirEntryTypeOffset, unchecked((uint)-3));
            var leaf = definition.Inputs[layout.CurrentIndex].Split(':').Last();
            Encoding.Latin1.GetBytes(leaf + "\0").CopyTo(Bus.Memory.AsSpan((int)(fib + FileInfoBlock.FileNameOffset)));
            Encoding.Latin1.GetBytes(layout.CurrentSource + "\0").CopyTo(
                Bus.Memory.AsSpan((int)(state.D[2] + (uint)DosLayout.AnchorPath.PathBuffer)));
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state, invocation) =>
        {
            var layout = invocation.DeleteCommandLayout!;
            Require(state.D[1] == layout.Anchor && invocation.FreeArgs == 0,
                "Delete MatchNext anchor or lifetime differs.");
            layout.MatchNextCalls++;
            return unchecked((uint)(int)DOS.Error.NoMoreEntries);
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state, invocation) =>
        {
            var layout = invocation.DeleteCommandLayout!;
            Require(state.D[1] == layout.Anchor && invocation.FreeArgs == 0,
                "Delete MatchEnd anchor or lifetime differs.");
            layout.MatchEndCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var layout = invocation.DeleteCommandLayout!;
            Require(Bus.CString(state.D[1]) == layout.CurrentSource &&
                state.D[2] == unchecked((uint)(int)DOS.LockMode.Shared),
                "Delete source lock ABI differs.");
            layout.Locks++;
            return 0x150;
        });
        Register(baseAddress, DosLvo.ParentDir, "ParentDir", (state, invocation) =>
        {
            var layout = invocation.DeleteCommandLayout!;
            Require(state.D[1] == 0x150, "Delete ParentDir lock differs.");
            layout.ParentLocks++;
            return 0x160;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            var layout = invocation.DeleteCommandLayout!;
            Require(state.D[1] == (layout.Unlocks % 2 == 0 ? 0x160u : 0x150u),
                "Delete unlock order differs.");
            layout.Unlocks++;
            return 0;
        });
        Register(baseAddress, DosLvo.SetProtection, "SetProtection", (state, invocation) =>
        {
            var definition = invocation.Definition.DeleteCommand!;
            var layout = invocation.DeleteCommandLayout!;
            Require(definition.Force && Bus.CString(state.D[1]) == layout.CurrentSource &&
                state.D[2] == 0, "Delete FORCE protection call differs.");
            layout.SetProtections++;
            return 1;
        });
        Register(baseAddress, DosLvo.DeleteFile, "DeleteFile", (state, invocation) =>
        {
            var definition = invocation.Definition.DeleteCommand!;
            var layout = invocation.DeleteCommandLayout!;
            Require(invocation.FreeArgs == 0 && Bus.CString(state.D[1]) == layout.CurrentSource &&
                layout.Unlocks == (layout.Deletes + 1) * 2,
                "DeleteFile source or lock order differs.");
            layout.Deletes++;
            if (layout.CurrentIndex == definition.FailIndex)
            {
                invocation.IoError = definition.DeleteError;
                return 0;
            }
            return 1;
        });
    }
}
