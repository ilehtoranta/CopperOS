using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied post-ReadArgs vectors for the bounded Workbench 3.1 Which entry.
/// These run the resident HUNK through its DOS/Exec ABI and verify invocation
/// cleanup. They do not emulate a Workbench filesystem, resident registry, or
/// original command parser.
/// </summary>
internal sealed record WhichEntryCase(string File, bool NoResidents,
    bool ResidentsOnly, bool All, bool Internal, bool Resident, string? Path)
{
    public int Result { get; init; } = DOS.RETURN_OK;
    public int IoError { get; init; }
}

internal sealed partial class ProbeFixture
{
    public const string WorkbenchWhichEntrySuite =
        "which-wb31-native-entry-vector-fixture";

    private List<object> RunWorkbenchWhichEntryCases()
    {
        ProbeCase[] cases =
        [
            Which("internal", "dir", false, false, false, true, false, null,
                "INTERNAL dir\n"),
            Which("resident", "echo", false, false, false, false, true, null,
                "RES echo\n"),
            Which("direct-path", "list", false, false, false, false, false,
                "RAM:C/list", "RAM:C/list\n"),
            Which("all-internal-only", "dir", false, false, true, true, false,
                null, "INTERNAL dir\n") with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            Which("nores-res-conflict", "dir", true, true, false, true, false,
                null, "") with { Result = DOS.RETURN_WARN },
            Which("res-no-match", "missing", false, true, false, false, false,
                null, "") with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            new ProbeCase("readargs-failure", "", DOS.RETURN_ERROR, 118, "")
            {
                Which = new("missing", false, false, false, false, false, null),
                ParserError = 118
            },
            new ProbeCase("result-allocation-failure", "", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            {
                Which = new("missing", false, false, false, false, false, null),
                AllocationFailure = true
            },
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-internal" },
            cases[2] with { Name = "repeat-direct" }
        ], true));
        reports.AddRange(Execute([
            cases[4] with { Name = "interleaved-conflict", StackBytes = 4096 },
            cases[1] with { Name = "interleaved-resident" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Which(string name, string file, bool noResidents,
        bool residentsOnly, bool all, bool internalSegment, bool resident,
        string? path, string output) => new(name, "", DOS.RETURN_OK, 0, output)
        {
            Which = new(file, noResidents, residentsOnly, all, internalSegment,
                resident, path)
        };

    private void VerifyWorkbenchWhichEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Which ??
            throw new InvalidOperationException("Missing Which definition.");
        var parsed = invocation.Definition.ParserError == 0 &&
            !invocation.Definition.AllocationFailure;
        Require(invocation.Reads == (invocation.Definition.AllocationFailure ? 0 : 1),
            "Which ReadArgs count differs from the supplied path.");
        Require(invocation.FreeArgs == (parsed ? 1 : 0),
            "Which RDArgs lifetime differs from the supplied path.");
        if (!parsed)
        {
            Require(invocation.Allocations == 1 &&
                invocation.FreeMem == (invocation.Definition.ParserError == 0 ? 0 : 1),
                "Which parser allocation/cleanup differs from the supplied path.");
            return;
        }

        Require(invocation.Allocations == 2 && invocation.FreeMem == 2,
            "Which must release argument slots and scratch storage once.");
        Require(invocation.Events.IndexOf("ReadArgs") < invocation.Events.IndexOf("FreeArgs") &&
            invocation.Events.IndexOf("FreeArgs") < invocation.Events.IndexOf("CloseLibrary"),
            "Which released RDArgs after parser use or after closing DOS.");
        Require(invocation.Events.Count(value => value == "Forbid") ==
            invocation.Events.Count(value => value == "Permit"),
            "Which must balance each resident lookup Forbid with Permit.");
        if (definition.Path is not null)
            Require(CountEvent(invocation, "Lock") == 1 &&
                CountEvent(invocation, "UnLock") == 1,
                "Which direct lock lifetime differs from the supplied path.");
    }

    private void RegisterWorkbenchWhichEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Which ??
                throw new InvalidOperationException("Missing Which definition.");
            Require(Bus.CString(state.D[1]) == "FILE/A,NORES/S,RES/S,ALL/S" &&
                state.D[3] == 0, "Which ReadArgs template/source ABI mismatch.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == 16,
                "Which result slots must be four owned LONGs.");
            for (var offset = 0u; offset < 16; offset += 4)
                Require(Bus.Long(results + offset) == 0,
                    "Which result slots were not cleared before ReadArgs.");
            invocation.Reads++;
            if (invocation.Definition.ParserError != 0)
            {
                invocation.IoError = invocation.Definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 256, "RDArgs", true);
            var bytes = Encoding.Latin1.GetBytes(definition.File);
            Require(bytes.Length + 1 <= 128, "Which fixture string storage overflow.");
            bytes.CopyTo(Bus.Memory.AsSpan((int)rdArgs + 128));
            Bus.Memory[rdArgs + 128u + (uint)bytes.Length] = 0;
            Bus.Long(results, rdArgs + 128);
            if (definition.NoResidents) Bus.Long(results + 4, 1);
            if (definition.ResidentsOnly) Bus.Long(results + 8, 1);
            if (definition.All) Bus.Long(results + 12, 1);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.FindSegment, "FindSegment", (state, invocation) =>
        {
            var definition = invocation.Definition.Which ??
                throw new InvalidOperationException("Missing Which definition.");
            Require(Bus.CString(state.D[1]) == definition.File && state.D[2] == 0,
                "Which FindSegment name/anchor ABI mismatch.");
            var system = state.D[3] != 0;
            return system ? definition.Internal ? 0x24000u : 0u :
                definition.Resident ? 0x25000u : 0u;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var definition = invocation.Definition.Which ??
                throw new InvalidOperationException("Missing Which definition.");
            Require(Bus.CString(state.D[1]) == definition.File &&
                state.D[2] == unchecked((uint)DOS.LockMode.Shared),
                "Which direct Lock ABI differs from the supplied path.");
            return definition.Path is null ? 0u : 0x260u;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            Require(state.D[1] == 0x260u, "Which unlocked an unowned lock.");
            return 1;
        });
        Register(baseAddress, DosLvo.NameFromLock, "NameFromLock", (state, invocation) =>
        {
            var definition = invocation.Definition.Which ??
                throw new InvalidOperationException("Missing Which definition.");
            Require(definition.Path is not null && state.D[1] == 0x260u &&
                state.D[3] == 1024, "Which NameFromLock ABI differs from the supplied path.");
            var bytes = Encoding.Latin1.GetBytes(definition.Path!);
            bytes.CopyTo(Bus.Memory.AsSpan((int)state.D[2]));
            Bus.Memory[state.D[2] + (uint)bytes.Length] = 0;
            return 1;
        });
        Register(baseAddress, DosLvo.Cli, "Cli", (_, _) => 0);
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "Which changed its borrowed output handle.");
            invocation.Output.Write(Bus.Memory, checked((int)state.D[2]), checked((int)state.D[3]));
            return state.D[3];
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            var previous = invocation.IoError;
            invocation.IoError = unchecked((int)state.D[1]);
            Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, state.D[1]);
            return unchecked((uint)previous);
        });
    }
}
