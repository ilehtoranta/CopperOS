using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied post-ReadArgs vectors for the bounded Workbench 3.1 and MorphOS
/// Which entries. These run resident HUNKs through DOS/Exec ABI fixtures and
/// verify invocation cleanup. They do not emulate either OS's filesystem or
/// resident registry, or either original command parser.
/// </summary>
internal sealed record WhichEntryCase(string File, bool NoResidents,
    bool ResidentsOnly, bool All, bool Internal, bool Resident, string? Path,
    bool NoAlias = false, bool Alias = false)
{
    public int Result { get; init; } = DOS.RETURN_OK;
    public int IoError { get; init; }
    public string[]? CliPathDirectories { get; init; }
    public int? CliPathCycleTo { get; init; }
    public bool CPath { get; init; }
    public bool Directory { get; init; }
    public bool DosObjectAllocationFailure { get; init; }
    public bool ExamineFailure { get; init; }
    public int ExamineError { get; init; }
    public int LockFailureError { get; init; } = (int)DOS.Error.ObjectNotFound;
    public bool AliasMatch { get; init; }
}

internal sealed partial class ProbeFixture
{
    public const string WorkbenchWhichEntrySuite =
        "which-wb31-native-entry-vector-fixture";
    public const string MorphOSWhichEntrySuite =
        "which-morphos-native-entry-vector-fixture";

    public static bool IsWhichEntrySuite(string value) =>
        value == WorkbenchWhichEntrySuite || value == MorphOSWhichEntrySuite;

    public static bool IsMorphOSWhichEntrySuite(string value) =>
        value == MorphOSWhichEntrySuite;

    private List<object> RunWorkbenchWhichEntryCases()
    {
        ProbeCase[] cases =
        [
            Which("internal", "dir", false, false, false, true, false, null,
                "INTERNAL dir\n"),
            Which("nores-internal", "dir", true, false, false, true, false,
                null, "") with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            Which("resident", "echo", false, false, false, false, true, null,
                "RES echo\n"),
            Which("direct-path", "list", false, false, false, false, false,
                "RAM:C/list", "RAM:C/list\n"),
            Which("all-internal-only", "dir", false, false, true, true, false,
                null, "INTERNAL dir\n") with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            Which("nores-all-internal", "dir", true, false, true, true, false,
                null, "") with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            Which("res-all-internal", "dir", false, true, true, true, false,
                null, "INTERNAL dir\n"),
            Which("nores-res-conflict", "dir", true, true, false, true, false,
                null, "") with { Result = DOS.RETURN_WARN },
            Which("nores-res-all-internal", "dir", true, true, true, true,
                false, null, "") with { Result = DOS.RETURN_WARN },
            Which("res-no-match", "missing", false, true, false, false, false,
                null, "") with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            new ProbeCase("readargs-missing-file", "",
                DOS.RETURN_WARN, 116, "required argument missing\n")
            {
                Which = new("missing", false, false, false, false, false, null),
                ParserError = 116
            },
            new ProbeCase("readargs-failure", "", DOS.RETURN_WARN, 118,
                "wrong number of arguments\n")
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
            Which("all-resident-duplicate-path", "Execute", false, false,
                true, false, true, null,
                "RES Execute\nWorkbench3.1:C/Execute\nWorkbench3.1:C/Execute\n",
                cliPathDirectories: ["Workbench3.1:C", "Workbench3.1:C"]),
            Which("all-c-path-fallback", "Execute", false, false,
                true, false, false, null,
                "Workbench3.1:C/Execute\nWorkbench3.1:C/Execute\n",
                cliPathDirectories: ["Workbench3.1:C"], cPath: true),
            Which("all-explicit-path-skips-search", "C:Execute", false,
                false, true, false, false, "Workbench3.1:C/Execute",
                "Workbench3.1:C/Execute\n",
                cliPathDirectories: Enumerable.Repeat("Workbench3.1:C", 8)
                    .ToArray()),
            Which("all-path-over-64-nodes", "Execute", false, false,
                true, false, false, null,
                string.Concat(Enumerable.Repeat("Workbench3.1:C/Execute\n", 65)),
                cliPathDirectories: Enumerable.Repeat("Workbench3.1:C", 65)
                    .ToArray()),
            Which("all-cyclic-path", "Execute", false, false,
                true, false, false, null,
                "Workbench3.1:C/Execute\nWorkbench3.1:D/Execute\n",
                cliPathDirectories: ["Workbench3.1:C", "Workbench3.1:D"],
                cliPathCycleTo: 0) with
                { Result = DOS.RETURN_ERROR, Error = (int)DOS.Error.ObjectWrongType },
            Which("bare-directory-is-not-command", "S", false, false,
                false, false, false, "Workbench3.1:S", "",
                directory: true) with
                {
                    Result = DOS.RETURN_WARN,
                    Error = (int)DOS.Error.ObjectNotFound
                },
            Which("trailing-slash-directory-is-not-match", "S/", false,
                false, false, false, false, "Workbench3.1:S", "",
                directory: true) with { Result = DOS.RETURN_WARN },
            Which("assign-root-directory-match", "C:", false, false,
                false, false, false, "Workbench3.1:C", "Workbench3.1:C\n",
                directory: true),
            Which("sys-assign-root-directory-match", "SYS:", false, false,
                false, false, false, "Workbench3.1:", "Workbench3.1:\n",
                directory: true),
            Which("sys-assign-subdirectory-is-not-match", "SYS:S", false,
                false, false, false, false, "Workbench3.1:S", "",
                directory: true) with { Result = DOS.RETURN_WARN },
            Which("sys-assign-file-reports-canonical-path",
                "SYS:S/Startup-Sequence", false, false, false, false,
                false, "Workbench3.1:S/Startup-Sequence",
                "Workbench3.1:S/Startup-Sequence\n"),
            Which("sys-parent-path-miss-preserves-object-not-found",
                "SYS:S/..", false, false, false, false, false, null, "",
                lockFailureError: (int)DOS.Error.ObjectNotFound) with
                {
                    Result = DOS.RETURN_WARN,
                    Error = (int)DOS.Error.ObjectNotFound
                },
            Which("file-info-allocation-failure", "missing", false, false,
                false, false, false, null, "",
                dosObjectAllocationFailure: true) with
                {
                    Result = DOS.RETURN_FAIL,
                    Error = (int)DOS.Error.NoFreeStore
                },
            Which("examine-failure-releases-lock", "C:entry", false, false,
                false, false, false, "Workbench3.1:C/entry", "",
                examineFailure: true,
                examineError: (int)DOS.Error.ObjectWrongType) with
                {
                    Result = DOS.RETURN_ERROR,
                    Error = (int)DOS.Error.ObjectWrongType
                },
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-internal" },
            cases[2] with { Name = "repeat-direct" }
        ], true));
        reports.AddRange(Execute([
            cases[7] with { Name = "interleaved-conflict", StackBytes = 4096 },
            cases[2] with { Name = "interleaved-resident" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> RunMorphOSWhichEntryCases()
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
            Which("alias-only-hit", "echo", false, false, false, false,
                false, null, "ALIAS echo\n", alias: true,
                aliasMatch: true),
            Which("alias-only-miss", "missing", false, false, false, false,
                false, null, "", alias: true) with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            Which("alias-only-miss-does-not-fall-through", "echo", false,
                false, true, true, true, "RAM:C/echo", "", alias: true) with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            Which("default-alias-hit", "echo", false, false, false, false,
                false, null, "ALIAS echo\n", aliasMatch: true),
            Which("noalias-suppresses-alias", "echo", false, false, false,
                false, false, null, "", noAlias: true, aliasMatch: true) with
                { Result = DOS.RETURN_WARN, Error = (int)DOS.Error.ObjectNotFound },
            Which("noalias-falls-through-to-file", "echo", false, false,
                false, false, false, "RAM:C/echo", "RAM:C/echo\n",
                noAlias: true, aliasMatch: true),
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
            new ProbeCase("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            {
                Which = new("missing", false, false, false, false, false, null),
                Workbench = true
            },
            new ProbeCase("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            {
                Which = new("missing", false, false, false, false, false, null),
                EntryLength = -1
            },
            new ProbeCase("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            {
                Which = new("missing", false, false, false, false, false, null),
                EntryLength = 4, NullArgumentPointer = true
            },
            new ProbeCase("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                Which = new("missing", false, false, false, false, false, null),
                MissingDos = true
            },
            Which("all-resident-duplicate-path", "Execute", false, false,
                true, false, true, null,
                "RES Execute\nWorkbench3.1:C/Execute\nWorkbench3.1:C/Execute\n",
                cliPathDirectories: ["Workbench3.1:C", "Workbench3.1:C"]),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-internal" },
            cases[2] with { Name = "repeat-direct" }
        ], true));
        reports.AddRange(Execute([
            cases[4] with { Name = "interleaved-alias", StackBytes = 4096 },
            cases[1] with { Name = "interleaved-resident" }
        ], true));
        var optionMatrix = new List<ProbeCase>(64);
        for (var mask = 0; mask < 32; mask++)
        {
            var noAlias = (mask & 1) != 0;
            var alias = (mask & 2) != 0;
            var noResidents = (mask & 4) != 0;
            var residentsOnly = (mask & 8) != 0;
            var all = (mask & 16) != 0;
            optionMatrix.Add(MorphOSWhichOptionMatrixCase(mask,
                targetExists: true, noAlias, alias, noResidents, residentsOnly,
                all));
            optionMatrix.Add(MorphOSWhichOptionMatrixCase(mask,
                targetExists: false, noAlias, alias, noResidents, residentsOnly,
                all));
        }
        foreach (var test in optionMatrix)
            reports.AddRange(Execute([test], false));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase MorphOSWhichOptionMatrixCase(int mask,
        bool targetExists, bool noAlias, bool alias, bool noResidents,
        bool residentsOnly, bool all)
    {
        // These vectors exercise the candidate's documented-option matrix.
        // They are not MorphOS command captures and do not establish the
        // shipped command's conflicting-switch or output behavior.
        var file = $"which-matrix-{mask:X2}";
        var path = targetExists ? $"Workbench3.1:C/{file}" : null;
        var cliPath = targetExists
            ? new[] { "Workbench3.1:C", "Workbench3.1:C" }
            : Array.Empty<string>();
        var output = new StringBuilder();
        var result = DOS.RETURN_OK;
        var error = 0;
        var found = false;
        var foundNonInternal = false;
        var stopped = false;

        void Report(string line, bool nonInternal)
        {
            output.Append(line);
            found = true;
            foundNonInternal |= nonInternal;
            stopped = !all;
        }

        if (alias)
        {
            if (!noAlias && targetExists)
                Report($"ALIAS {file}\n", true);
            else
            {
                result = DOS.RETURN_WARN;
                error = (int)DOS.Error.ObjectNotFound;
            }
        }
        else
        {
            if (!noAlias && !residentsOnly && targetExists)
                Report($"ALIAS {file}\n", true);

            if (!stopped && !noResidents && targetExists)
            {
                Report($"INTERNAL {file}\n", false);
                if (!stopped) Report($"RES {file}\n", true);
            }

            if (residentsOnly)
            {
                if (!found)
                {
                    result = DOS.RETURN_WARN;
                    error = noResidents ? 0 :
                        (int)DOS.Error.ObjectNotFound;
                }
            }
            else
            {
                if (!stopped && targetExists)
                    Report(path + "\n", true);
                if (!stopped && all && targetExists)
                {
                    Report(cliPath[0] + "/" + file + "\n", true);
                    Report(cliPath[1] + "/" + file + "\n", true);
                }

                if (!found || (found && !foundNonInternal && all))
                {
                    result = DOS.RETURN_WARN;
                    error = (int)DOS.Error.ObjectNotFound;
                }
            }
        }

        return Which(
            $"option-matrix-{mask:X2}-{(targetExists ? "found" : "missing")}",
            file, noResidents, residentsOnly, all, targetExists,
            targetExists, path, output.ToString(), noAlias: noAlias,
            alias: alias, cliPathDirectories: cliPath,
            aliasMatch: targetExists) with
        {
            Result = result,
            Error = error
        };
    }

    private static ProbeCase Which(string name, string file, bool noResidents,
        bool residentsOnly, bool all, bool internalSegment, bool resident,
        string? path, string output, bool noAlias = false,
        bool alias = false, string[]? cliPathDirectories = null,
        int? cliPathCycleTo = null, bool cPath = false,
        bool directory = false, bool dosObjectAllocationFailure = false,
        bool examineFailure = false, int examineError = 0,
        int lockFailureError = (int)DOS.Error.ObjectNotFound,
        bool aliasMatch = false) => new(name, "",
            DOS.RETURN_OK, 0, output)
        {
            Which = new(file, noResidents, residentsOnly, all, internalSegment,
                resident, path, noAlias, alias)
            {
                CliPathDirectories = cliPathDirectories,
                CliPathCycleTo = cliPathCycleTo,
                CPath = cPath,
                Directory = directory,
                DosObjectAllocationFailure = dosObjectAllocationFailure,
                ExamineFailure = examineFailure,
                ExamineError = examineError,
                LockFailureError = lockFailureError,
                AliasMatch = aliasMatch
            }
        };

    private void VerifyWorkbenchWhichEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Which ??
            throw new InvalidOperationException("Missing Which definition.");
        var startupBoundary = invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos;
        if (startupBoundary)
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0,
                "Which crossed an invalid startup boundary.");
            return;
        }
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

        var morphos = IsMorphOSWhichEntrySuite(suite);
        var aliasOnly = morphos && definition.Alias;
        var aliasMatch = morphos && !definition.NoAlias &&
            (definition.Alias || !definition.ResidentsOnly) &&
            definition.AliasMatch;
        var residentMatch = !definition.NoResidents &&
            !aliasOnly && !(aliasMatch && !definition.All) &&
            (definition.Internal || definition.Resident);
        var shouldAllocateFileInfo = !morphos &&
            !definition.ResidentsOnly && !(residentMatch && !definition.All);
        var expectedFileInfoAllocations = shouldAllocateFileInfo ? 1 : 0;
        var expectedFileInfoFrees = shouldAllocateFileInfo &&
            !definition.DosObjectAllocationFailure ? 1 : 0;
        Require(CountEvent(invocation, "AllocDosObject") ==
                expectedFileInfoAllocations &&
            CountEvent(invocation, "FreeDosObject") == expectedFileInfoFrees,
            $"Which FileInfoBlock allocation/cleanup differs for {invocation.Definition.Name}: " +
            $"expected {expectedFileInfoAllocations}/{expectedFileInfoFrees}, observed " +
            $"{CountEvent(invocation, "AllocDosObject")}/{CountEvent(invocation, "FreeDosObject")}; " +
            $"events={string.Join(',', invocation.Events)}.");
        Require(invocation.Allocations == 2 && invocation.FreeMem == 2,
            "Which must release argument slots and scratch storage once.");
        Require(invocation.Events.IndexOf("ReadArgs") < invocation.Events.IndexOf("FreeArgs") &&
            invocation.Events.IndexOf("FreeArgs") < invocation.Events.IndexOf("CloseLibrary"),
            "Which released RDArgs after parser use or after closing DOS.");
        Require(invocation.Events.Count(value => value == "Forbid") ==
            invocation.Events.Count(value => value == "Permit"),
            "Which must balance each resident lookup Forbid with Permit.");
        var pathCount = definition.CliPathDirectories?.Length ?? 0;
        var pathQualified = definition.File.Contains(':') ||
            definition.File.Contains('/');
        var searchedPathCount = pathQualified ? 0 : pathCount;
        var completesCliPathWalk = definition.CliPathCycleTo is null;
        var stopsAfterResident = residentMatch && !definition.All;
        var stopsAfterAlias = aliasMatch && !definition.All;
        var skipsFilesystem = aliasOnly || stopsAfterAlias ||
            definition.ResidentsOnly || stopsAfterResident ||
            definition.DosObjectAllocationFailure;
        var directLockFound = definition.Path is not null;
        var assignRoot = pathQualified && definition.File.EndsWith(':');
        var directPathMatch = directLockFound &&
            (!definition.Directory || assignRoot);
        var stopsAfterDirectPath = directPathMatch && !definition.All;
        var expectedLocks = skipsFilesystem
            ? 0 : 1 + (stopsAfterDirectPath ? 0 : searchedPathCount +
                (!pathQualified && completesCliPathWalk ? 1 : 0));
        var expectedUnlocks = skipsFilesystem
            ? 0 : (directLockFound ? 1 : 0) +
                (stopsAfterDirectPath ? 0 : searchedPathCount +
                    (definition.CPath && !pathQualified &&
                        completesCliPathWalk ? 1 : 0));
        var actualLocks = CountEvent(invocation, "Lock");
        var actualUnlocks = CountEvent(invocation, "UnLock");
        Require(actualLocks == expectedLocks &&
            actualUnlocks == expectedUnlocks,
            $"Which lock traversal differs for {invocation.Definition.Name}: " +
            $"expected {expectedLocks}/{expectedUnlocks}, observed {actualLocks}/{actualUnlocks}.");
        var expectedAliasLookups = morphos && !definition.NoAlias &&
            (definition.Alias || !definition.ResidentsOnly) ? 1 : 0;
        Require(CountEvent(invocation, "FindVar") == expectedAliasLookups,
            $"Which alias lookup count differs for {invocation.Definition.Name}.");
        if (shouldAllocateFileInfo && !definition.DosObjectAllocationFailure)
            Require(invocation.Events.IndexOf("AllocDosObject") <
                    invocation.Events.IndexOf("FreeDosObject") &&
                invocation.Events.IndexOf("FreeDosObject") <
                    invocation.Events.IndexOf("CloseLibrary"),
                "Which freed its FileInfoBlock after closing DOS.");
        else if (shouldAllocateFileInfo)
            Require(invocation.Events.IndexOf("AllocDosObject") >= 0 &&
                    !invocation.Events.Contains("FreeDosObject"),
                "Which allocated a FileInfoBlock after an allocation failure.");
    }

    private void RegisterWorkbenchWhichEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Which ??
                throw new InvalidOperationException("Missing Which definition.");
            var morphos = IsMorphOSWhichEntrySuite(suite);
            Require(Bus.CString(state.D[1]) == (morphos
                    ? "FILE/A,NOALIAS/S,ALIAS/S,NORES/S,RES/S,ALL/S"
                    : "FILE/A,NORES/S,RES/S,ALL/S") &&
                state.D[3] == 0, "Which ReadArgs template/source ABI mismatch.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size ==
                    (morphos ? 24u : 16u),
                "Which result slots have the wrong profile size.");
            for (var offset = 0u; offset < (morphos ? 24u : 16u); offset += 4)
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
            if (morphos && definition.NoAlias) Bus.Long(results + 4, 1);
            if (morphos && definition.Alias) Bus.Long(results + 8, 1);
            var noResidentsOffset = morphos ? 12u : 4u;
            var residentsOnlyOffset = morphos ? 16u : 8u;
            var allOffset = morphos ? 20u : 12u;
            if (definition.NoResidents)
                Bus.Long(results + noResidentsOffset, 1);
            if (definition.ResidentsOnly)
                Bus.Long(results + residentsOnlyOffset, 1);
            if (definition.All)
                Bus.Long(results + allOffset, 1);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                var parserError = invocation.Definition.ParserError;
                Require((parserError == 116 || parserError == 118) &&
                    state.D[1] == unchecked((uint)parserError) &&
                    state.D[2] == 0,
                    "Workbench Which parser PrintFault arguments differ.");
                var text = parserError == 116
                    ? "required argument missing\n"
                    : "wrong number of arguments\n";
                invocation.Output.Write(Encoding.Latin1.GetBytes(text));
                invocation.Events.Add(
                    $"PrintFault:{state.D[1]}:{state.D[2]:X8}");
                return 1;
            });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.FilePart, "FilePart", (state, _) =>
        {
            var path = Bus.CString(state.D[1]);
            var separator = Math.Max(path.LastIndexOf(':'),
                path.LastIndexOf('/'));
            return state.D[1] + (uint)(separator + 1);
        });
        Register(baseAddress, DosLvo.AllocDosObject, "AllocDosObject",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Which ??
                    throw new InvalidOperationException("Missing Which definition.");
                Require(!IsMorphOSWhichEntrySuite(suite) &&
                    state.D[1] == (uint)DosObjectType.FileInfoBlock &&
                    state.D[2] == 0,
                    "Which FileInfoBlock allocation ABI differs.");
                if (definition.DosObjectAllocationFailure)
                {
                    invocation.IoError = (int)DOS.Error.NoFreeStore;
                    return 0;
                }
                var fib = Bus.Allocate(invocation, FileInfoBlock.SizeInBytes,
                    "WhichFIB", true);
                return fib;
            });
        Register(baseAddress, DosLvo.FreeDosObject, "FreeDosObject",
            (state, invocation) =>
            {
                Require(state.D[1] == (uint)DosObjectType.FileInfoBlock,
                    "Which FileInfoBlock free ABI differs.");
                Bus.Release(invocation, state.D[2], "WhichFIB");
                return 0;
            });
        Register(baseAddress, DosLvo.Examine, "Examine",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Which ??
                    throw new InvalidOperationException("Missing Which definition.");
                var knownLock = state.D[1] == 0x260u &&
                    definition.Path is not null;
                knownLock |= state.D[1] == 0x280u && definition.CPath;
                if (definition.CliPathDirectories is { } directories)
                    for (var index = 0; index < directories.Length; index++)
                        knownLock |= state.D[1] == ResultPathLock(index);
                Require(knownLock &&
                    Bus.OwnedAllocation(invocation, state.D[2], "WhichFIB")
                        .Size == FileInfoBlock.SizeInBytes,
                    "Which Examine lock/FIB arguments differ.");
                if (definition.ExamineFailure)
                {
                    invocation.IoError = definition.ExamineError;
                    return 0;
                }
                Bus.Long(state.D[2] +
                    (uint)FileInfoBlock.DirEntryTypeOffset,
                    definition.Directory ? 2u : unchecked((uint)-3));
                invocation.IoError = 0;
                return 1;
            });
        if (IsMorphOSWhichEntrySuite(suite))
            Register(baseAddress, DosLvo.FindVar, "FindVar",
                (state, invocation) =>
                {
                    var definition = invocation.Definition.Which ??
                        throw new InvalidOperationException(
                            "Missing Which definition.");
                    Require(Bus.CString(state.D[1]) == definition.File &&
                        state.D[2] == (uint)LocalVariableType.Alias,
                        "Which FindVar alias-name/type ABI mismatch.");
                    if (definition.AliasMatch)
                    {
                        invocation.IoError = 0;
                        return 0x23000u;
                    }
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
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
            var candidate = Bus.CString(state.D[1]);
            Require(state.D[2] == unchecked((uint)DOS.LockMode.Shared),
                "Which direct Lock ABI differs from the supplied path.");
            if (candidate == definition.File && definition.Path is not null)
            {
                invocation.IoError = 0;
                return 0x260u;
            }
            if (definition.CPath && candidate == "C:" + definition.File)
            {
                invocation.IoError = 0;
                return 0x280u;
            }
            var directories = definition.CliPathDirectories;
            if (directories is not null)
            for (var index = 0; index < directories.Length; index++)
                if (candidate == JoinPath(directories[index], definition.File))
                {
                    var resultLock = ResultPathLock(index);
                    invocation.IoError = 0;
                    return resultLock;
                }
            invocation.IoError = definition.LockFailureError;
            return 0;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            var definition = invocation.Definition.Which ??
                throw new InvalidOperationException("Missing Which definition.");
            var owned = state.D[1] == 0x260u;
            owned |= state.D[1] == 0x280u && definition.CPath;
            if (!owned && definition.CliPathDirectories is { } directories)
                for (var index = 0; index < directories.Length; index++)
                    owned |= state.D[1] == ResultPathLock(index);
            Require(owned, "Which unlocked an unowned lock.");
            return 1;
        });
        Register(baseAddress, DosLvo.NameFromLock, "NameFromLock", (state, invocation) =>
        {
            var definition = invocation.Definition.Which ??
                throw new InvalidOperationException("Missing Which definition.");
            string? name = state.D[1] == 0x260u ? definition.Path : null;
            if (state.D[1] == 0x280u && definition.CPath)
                name = definition.Path ?? "Workbench3.1:C/" + definition.File;
            if (definition.CliPathDirectories is { } directories)
                for (var index = 0; index < directories.Length; index++)
                {
                    if (state.D[1] == DirectoryLock(invocation, index))
                        name = directories[index];
                    else if (state.D[1] == ResultPathLock(index))
                        name = JoinPath(directories[index], definition.File);
                }
            Require(name is not null && state.D[3] == 1024,
                "Which NameFromLock ABI differs from the supplied path.");
            var bytes = Encoding.Latin1.GetBytes(name!);
            bytes.CopyTo(Bus.Memory.AsSpan((int)state.D[2]));
            Bus.Memory[state.D[2] + (uint)bytes.Length] = 0;
            return 1;
        });
        Register(baseAddress, DosLvo.AddPart, "AddPart", (state, _) =>
        {
            var directory = Bus.CString(state.D[1]);
            var file = Bus.CString(state.D[2]);
            Require(state.D[3] == 1024,
                "Which AddPart path capacity differs from its buffer.");
            var name = JoinPath(directory, file);
            var bytes = Encoding.Latin1.GetBytes(name);
            Require(bytes.Length < state.D[3],
                "Which AddPart fixture path exceeds the supplied buffer.");
            bytes.CopyTo(Bus.Memory.AsSpan((int)state.D[1]));
            Bus.Memory[state.D[1] + (uint)bytes.Length] = 0;
            return 1;
        });
        Register(baseAddress, DosLvo.Cli, "Cli", (_, invocation) =>
        {
            var definition = invocation.Definition.Which ??
                throw new InvalidOperationException("Missing Which definition.");
            if (definition.CliPathDirectories is not { Length: > 0 } directories)
                return 0;
            var cli = 0x2000u + (uint)invocation.Slot * 0x1000u;
            var firstNode = PathNode(invocation, 0);
            Bus.Long(cli + (uint)DosLayout.CommandLineInterface.CommandDirectory,
                firstNode >> 2);
            for (var index = 0; index < directories.Length; index++)
            {
                var node = PathNode(invocation, index);
                var next = index + 1 < directories.Length
                    ? PathNode(invocation, index + 1) >> 2 : 0u;
                if (definition.CliPathCycleTo is { } cycleTo &&
                    index == directories.Length - 1)
                {
                    Require(cycleTo >= 0 && cycleTo < directories.Length,
                        "Which CLI path cycle target is outside the supplied list.");
                    next = PathNode(invocation, cycleTo) >> 2;
                }
                Bus.Long(node + (uint)DosLayout.PathLock.Next, next);
                Bus.Long(node + (uint)DosLayout.PathLock.Lock,
                    DirectoryLock(invocation, index));
            }
            return cli;
        });
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

    private static uint PathNode(Invocation invocation, int index) =>
        0x2100u + (uint)invocation.Slot * 0x1000u + (uint)index * 8u;

    private static uint DirectoryLock(Invocation invocation, int index) =>
        (0x2200u + (uint)invocation.Slot * 0x1000u + (uint)index * 0x20u) >> 2;

    private static uint ResultPathLock(int index) => 0x2a0u + (uint)index;

    private static string JoinPath(string directory, string file) =>
        directory.EndsWith(':') ? directory + file : directory + "/" + file;
}
