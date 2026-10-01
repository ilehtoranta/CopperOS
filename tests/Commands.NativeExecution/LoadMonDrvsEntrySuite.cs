using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record LoadMonDrvsEntryCase(
    bool ParserFailure = false,
    bool MissingDos = false,
    string? From = null,
    string? Except = null,
    string DriverName = "PAL",
    string[]? MatchedDrivers = null,
    bool DirectoryMatch = false,
    int MatchFirstResult = (int)DOS.Error.ObjectNotFound,
    bool LoadSuccess = true,
    bool InitResidentSuccess = true,
    LoadMonDrvsHunkShape HunkShape = LoadMonDrvsHunkShape.Valid);

internal enum LoadMonDrvsHunkShape
{
    Valid,
    ResidentInSecondHunk,
    Undersized,
    Overflowing,
    UnmappedEnd,
    Cyclic
}

internal sealed partial class ProbeFixture
{
    public const string LoadMonDrvsEntrySuite =
        "morphos320-loadmondrvs-native-entry-vector-fixture";

    private const uint LoadMonDrvsFrom = 0x23000;
    private const uint LoadMonDrvsExcept = 0x23100;
    private const uint LoadMonDrvsAnchor = 0x24000;
    private const uint LoadMonDrvsCurrent = 0x24100;
    private const uint LoadMonDrvsInfo = 0x24200;
    private const uint LoadMonDrvsSegment = 0x20000;
    private const uint LoadMonDrvsSecondSegment = 0x21000;

    private List<object> RunLoadMonDrvsEntryCases()
    {
        ProbeCase[] cases =
        [
            new("default-no-match", "", DOS.RETURN_WARN,
                (int)DOS.Error.ObjectNotFound, "")
            { LoadMonDrvs = new() },
            new("except-skips-driver-case-insensitively", "FROM DEVS:Monitors EXCEPT pal\n",
                DOS.RETURN_OK, 0, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", Except: "pal", MatchFirstResult: 0) },
            new("except-allows-other-driver", "FROM SYS:Monitors EXCEPT NTSC\n",
                DOS.RETURN_OK, 0, "")
            { LoadMonDrvs = new(From: "SYS:Monitors", Except: "NTSC", MatchFirstResult: 0) },
            new("multiple-matches-skip-except-then-load", "FROM SYS:Monitors EXCEPT PAL\n",
                DOS.RETURN_OK, 0, "")
            { LoadMonDrvs = new(From: "SYS:Monitors", Except: "PAL",
                MatchFirstResult: 0, MatchedDrivers: ["PAL", "NTSC"]) },
            new("directory-match-skipped", "FROM DEVS:Monitors\n",
                DOS.RETURN_OK, 0, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0,
                DirectoryMatch: true) },
            new("driver-load-and-init", "FROM DEVS:Monitors\n", DOS.RETURN_OK, 0, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0) },
            new("driver-init-failure", "FROM DEVS:Monitors\n", DOS.RETURN_WARN,
                (int)DOS.Error.ObjectNotFound, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0,
                InitResidentSuccess: false) },
            new("driver-load-failure", "FROM DEVS:Monitors\n", DOS.RETURN_OK, 0, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0,
                LoadSuccess: false) },
            new("resident-in-second-hunk", "FROM DEVS:Monitors\n", DOS.RETURN_OK, 0, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0,
                HunkShape: LoadMonDrvsHunkShape.ResidentInSecondHunk) },
            new("undersized-hunk", "FROM DEVS:Monitors\n", DOS.RETURN_WARN,
                Invocation.InitialIoError, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0,
                HunkShape: LoadMonDrvsHunkShape.Undersized) },
            new("unmapped-hunk-end", "FROM DEVS:Monitors\n", DOS.RETURN_WARN,
                Invocation.InitialIoError, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0,
                HunkShape: LoadMonDrvsHunkShape.UnmappedEnd) },
            new("overflowing-hunk-size", "FROM DEVS:Monitors\n", DOS.RETURN_WARN,
                Invocation.InitialIoError, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0,
                HunkShape: LoadMonDrvsHunkShape.Overflowing) },
            new("cyclic-segment-list", "FROM DEVS:Monitors\n", DOS.RETURN_WARN,
                Invocation.InitialIoError, "")
            { LoadMonDrvs = new(From: "DEVS:Monitors", MatchFirstResult: 0,
                HunkShape: LoadMonDrvsHunkShape.Cyclic) },
            new("parser-failure", "FROM bad\n", DOS.RETURN_ERROR, 116, "")
            { LoadMonDrvs = new(ParserFailure: true) },
            new("missing-dos", "", DOS.RETURN_FAIL, Invocation.InitialIoError, "")
            { MissingDos = true, LoadMonDrvs = new(MissingDos: true) }
        ];

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-default-no-match" },
            cases[2] with { Name = "repeat-driver-load-and-init" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void PrepareLoadMonDrvsEntry(Invocation invocation)
    {
        Bus.Memory.AsSpan((int)LoadMonDrvsFrom, 256).Clear();
        Bus.Memory.AsSpan((int)LoadMonDrvsExcept, 256).Clear();
        Bus.Memory.AsSpan((int)LoadMonDrvsAnchor, 256).Clear();
        Bus.Memory.AsSpan((int)LoadMonDrvsCurrent, 64).Clear();
        Bus.Memory.AsSpan((int)LoadMonDrvsInfo, 256).Clear();
        Bus.Memory.AsSpan((int)(LoadMonDrvsSegment << 2) - 16, 256).Clear();
        Bus.Memory.AsSpan((int)(LoadMonDrvsSecondSegment << 2) - 16, 256).Clear();
        var definition = invocation.Definition.LoadMonDrvs!;
        var firstMemory = LoadMonDrvsSegment << 2;
        var secondMemory = LoadMonDrvsSecondSegment << 2;
        var firstSize = definition.HunkShape switch
        {
            LoadMonDrvsHunkShape.Undersized => 1u,
            LoadMonDrvsHunkShape.Overflowing => 0x40000000u,
            LoadMonDrvsHunkShape.UnmappedEnd => 0x00100000u,
            LoadMonDrvsHunkShape.ResidentInSecondHunk => 2u,
            _ => 16u
        };
        var firstNext = definition.HunkShape switch
        {
            LoadMonDrvsHunkShape.ResidentInSecondHunk => LoadMonDrvsSecondSegment,
            LoadMonDrvsHunkShape.Cyclic => LoadMonDrvsSegment,
            _ => 0u
        };
        Bus.Long(firstMemory, firstNext);
        Bus.Long(firstMemory - 4, firstSize);
        if (definition.HunkShape == LoadMonDrvsHunkShape.Valid)
            WriteLoadMonResident(firstMemory + 4);
        if (definition.HunkShape == LoadMonDrvsHunkShape.ResidentInSecondHunk)
        {
            Bus.Long(secondMemory, 0);
            Bus.Long(secondMemory - 4, 16);
            WriteLoadMonResident(secondMemory + 4);
        }
        invocation.LoadMonDrvsAnchor = LoadMonDrvsAnchor;
        invocation.LoadMonDrvsCurrent = LoadMonDrvsCurrent;
        invocation.LoadMonDrvsInfo = LoadMonDrvsInfo;
        invocation.LoadMonDrvsSegment = LoadMonDrvsSegment;
        invocation.LoadMonDrvsSecondSegment = LoadMonDrvsSecondSegment;
        invocation.LoadMonDrvsResident = definition.HunkShape ==
            LoadMonDrvsHunkShape.ResidentInSecondHunk
                ? secondMemory + 4 : firstMemory + 4;
    }

    private void WriteLoadMonResident(uint resident)
    {
        Bus.Word(resident, 0x4afc);
        Bus.Long(resident + 2, resident);
    }

    private static string[] LoadMonDrvsMatches(LoadMonDrvsEntryCase definition) =>
        definition.MatchedDrivers ?? (definition.MatchFirstResult == 0
            ? [definition.DriverName]
            : []);

    private void SetLoadMonDrvsMatch(Invocation invocation, uint anchor,
        string driverName)
    {
        Bus.Long(anchor + (uint)DosLayout.AnchorPath.Current,
            invocation.LoadMonDrvsCurrent);
        Bus.Long(invocation.LoadMonDrvsCurrent + 8, 0x1234);
        var info = anchor + (uint)DosLayout.AnchorPath.Info;
        Bus.Long(info + (uint)FileInfoBlock.DirEntryTypeOffset,
            invocation.Definition.LoadMonDrvs!.DirectoryMatch ? 1u : 0u);
        WriteLoadMonCString(info + (uint)FileInfoBlock.FileNameOffset,
            driverName);
    }

    private void RegisterLoadMonDrvsEntryExec()
    {
        Register(ExecBase, ExecLvo.TypeOfMem, "LoadMonDrvs TypeOfMem",
            (state, invocation) =>
            {
                Require(state.A[6] == ExecBase,
                    "LoadMonDrvs TypeOfMem base ABI differs.");
                invocation.LoadMonDrvsTypeOfMemCalls++;
                var address = state.A[1];
                var shape = invocation.Definition.LoadMonDrvs!.HunkShape;
                var firstMemory = invocation.LoadMonDrvsSegment << 2;
                var firstBytes = shape switch
                {
                    LoadMonDrvsHunkShape.ResidentInSecondHunk => 8u,
                    LoadMonDrvsHunkShape.Undersized => 4u,
                    LoadMonDrvsHunkShape.UnmappedEnd => 64u,
                    LoadMonDrvsHunkShape.Overflowing => 64u,
                    _ => 64u
                };
                if (ContainsLoadMonDrvsAddress(address,
                        firstMemory - 4, firstBytes + 4))
                    return 1;
                if (shape == LoadMonDrvsHunkShape.ResidentInSecondHunk)
                {
                    var secondMemory = invocation.LoadMonDrvsSecondSegment << 2;
                    if (ContainsLoadMonDrvsAddress(address,
                            secondMemory - 4, 68))
                        return 1;
                }
                return 0;
            });
        Register(ExecBase, ExecLvo.InitResident, "LoadMonDrvs InitResident",
            (state, invocation) =>
            {
                Require(state.A[0] == invocation.LoadMonDrvsResident &&
                    state.D[1] == invocation.LoadMonDrvsSegment,
                    "LoadMonDrvs InitResident ABI differs.");
                invocation.LoadMonDrvsInitResidentCalls++;
                if (!invocation.Definition.LoadMonDrvs!.InitResidentSuccess)
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return invocation.Definition.LoadMonDrvs!.InitResidentSuccess
                    ? state.A[0] : 0u;
        });
    }

    private static bool ContainsLoadMonDrvsAddress(uint address,
        uint start, uint length) => address >= start && address - start < length;

    private void RegisterLoadMonDrvsEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "LoadMonDrvs ReadArgs",
            (state, invocation) =>
            {
                var definition = invocation.Definition.LoadMonDrvs!;
                Require(Bus.CString(state.D[1]) == NativeMorphOSLoadMonDrvsCommand.Template &&
                    state.D[3] == 0 && state.D[2] % 4 == 0 &&
                    Bus.OwnedAllocation(invocation, state.D[2], "LoadMonDrvsResults").Size ==
                        NativeMorphOSLoadMonDrvsCommand.ResultCount * 4u,
                    "LoadMonDrvs ReadArgs ABI differs.");
                Require(Bus.Long(state.D[2]) == 0 && Bus.Long(state.D[2] + 4) == 0,
                    "LoadMonDrvs ReadArgs results were not cleared.");
                invocation.LoadMonDrvsReadArgsCalls++;
                if (definition.ParserFailure)
                {
                    invocation.IoError = 116;
                    return 0;
                }
                if (definition.From is { } from)
                {
                    WriteLoadMonCString(LoadMonDrvsFrom, from);
                    Bus.Long(state.D[2], LoadMonDrvsFrom);
                }
                if (definition.Except is { } except)
                {
                    WriteLoadMonCString(LoadMonDrvsExcept, except);
                    Bus.Long(state.D[2] + 4, LoadMonDrvsExcept);
                }
                return Bus.Allocate(invocation, 40, "RDArgs", true);
            });
        Register(baseAddress, DosLvo.FreeArgs, "LoadMonDrvs FreeArgs",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.D[1], "RDArgs");
                invocation.LoadMonDrvsFreeArgsCalls++;
                invocation.IoError = 901;
                return 0;
            });
        Register(baseAddress, DosLvo.AddPart, "LoadMonDrvs AddPart",
            (state, invocation) =>
            {
                Require(state.D[2] != 0 && state.D[3] == 1024,
                    "LoadMonDrvs AddPart ABI differs.");
                var path = Bus.CString(state.D[1]);
                var part = Bus.CString(state.D[2]);
                var definition = invocation.Definition.LoadMonDrvs!;
                var directory = definition.From ?? "DEVS:Monitors";
                var info = invocation.LoadMonDrvsAnchor +
                    (uint)DosLayout.AnchorPath.Info;
                var currentName = Bus.CString(info +
                    (uint)FileInfoBlock.FileNameOffset);
                Require((part == "#?" && path == directory) ||
                    (part == currentName && path == directory),
                    "LoadMonDrvs path source differs.");
                invocation.LoadMonDrvsAddPartCalls++;
                WriteLoadMonCString(state.D[1], part == "#?"
                    ? (path.EndsWith(":", StringComparison.Ordinal) ? path + "#?" : path + "/#?")
                    : path + "/" + part);
                return 1;
            });
        Register(baseAddress, DosLvo.MatchFirst, "LoadMonDrvs MatchFirst",
            (state, invocation) =>
            {
                Require(state.D[1] != 0 && state.D[2] != 0 &&
                    Bus.CString(state.D[1]).EndsWith("/#?", StringComparison.Ordinal),
                    "LoadMonDrvs MatchFirst pattern or anchor differs.");
                invocation.LoadMonDrvsMatchFirstCalls++;
                var definition = invocation.Definition.LoadMonDrvs!;
                var matches = LoadMonDrvsMatches(definition);
                if (definition.MatchFirstResult == 0 && matches.Length != 0)
                {
                    invocation.LoadMonDrvsAnchor = state.D[2];
                    SetLoadMonDrvsMatch(invocation, invocation.LoadMonDrvsAnchor,
                        matches[0]);
                }
                return unchecked((uint)invocation.Definition.LoadMonDrvs!.MatchFirstResult);
            });
        Register(baseAddress, DosLvo.MatchNext, "LoadMonDrvs MatchNext",
            (state, invocation) =>
            {
                Require(state.D[1] != 0 && invocation.LoadMonDrvsMatchFirstCalls == 1,
                    "LoadMonDrvs MatchNext anchor lifetime differs.");
                invocation.LoadMonDrvsMatchNextCalls++;
                var matches = LoadMonDrvsMatches(
                    invocation.Definition.LoadMonDrvs!);
                if (invocation.LoadMonDrvsMatchNextCalls < matches.Length)
                {
                    SetLoadMonDrvsMatch(invocation, state.D[1],
                        matches[invocation.LoadMonDrvsMatchNextCalls]);
                    return 0;
                }
                return unchecked((uint)DOS.Error.NoMoreEntries);
            });
        Register(baseAddress, DosLvo.MatchEnd, "LoadMonDrvs MatchEnd",
            (state, invocation) =>
            {
                Require(state.D[1] != 0 && invocation.LoadMonDrvsMatchEndCalls == 0,
                    "LoadMonDrvs MatchEnd anchor differs.");
                invocation.LoadMonDrvsMatchEndCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.NameFromLock, "LoadMonDrvs NameFromLock",
            (state, invocation) =>
            {
                Require(state.D[1] == 0x1234 && state.D[2] == invocation.LoadMonDrvsAnchor +
                    ((uint)DosLayout.AnchorPath.Size + 3u & ~3u) && state.D[3] == 1024,
                    "LoadMonDrvs NameFromLock ABI differs.");
                invocation.LoadMonDrvsNameFromLockCalls++;
                WriteLoadMonCString(state.D[2], invocation.Definition.LoadMonDrvs!.From ??
                    "DEVS:Monitors");
                return 1;
            });
        Register(baseAddress, -150, "LoadMonDrvs LoadSeg", (state, invocation) =>
        {
                var from = invocation.Definition.LoadMonDrvs!.From ?? "DEVS:Monitors";
                var info = invocation.LoadMonDrvsAnchor +
                    (uint)DosLayout.AnchorPath.Info;
                var driverName = Bus.CString(info +
                    (uint)FileInfoBlock.FileNameOffset);
                var expectedPath = from.EndsWith(":", StringComparison.Ordinal)
                    ? from + driverName
                    : from + "/" + driverName;
                Require(Bus.CString(state.D[1]) == expectedPath,
                    $"LoadMonDrvs LoadSeg path differs: '{Bus.CString(state.D[1])}' != '{expectedPath}'.");
            invocation.LoadMonDrvsLoadSegCalls++;
            if (!invocation.Definition.LoadMonDrvs!.LoadSuccess)
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return 0;
            }
            return invocation.LoadMonDrvsSegment;
        });
        Register(baseAddress, -156, "LoadMonDrvs UnLoadSeg", (state, invocation) =>
        {
            Require(state.D[1] == invocation.LoadMonDrvsSegment,
                "LoadMonDrvs unloaded the wrong segment.");
            invocation.LoadMonDrvsUnLoadSegCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "LoadMonDrvs PrintFault",
            (_, invocation) => { invocation.Events.Add("PrintFault"); return 0; });
        Register(baseAddress, DosLvo.IoErr, "LoadMonDrvs IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "LoadMonDrvs SetIoErr",
            (state, invocation) =>
            {
                invocation.IoError = unchecked((int)state.D[1]);
                return 0;
            });
    }

    private void WriteLoadMonCString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }

    private void VerifyLoadMonDrvsEntry(Invocation invocation)
    {
        var definition = invocation.Definition.LoadMonDrvs!;
        if (definition.MissingDos)
        {
            Require(invocation.LoadMonDrvsReadArgsCalls == 0 &&
                invocation.Allocations == 0 && invocation.LoadMonDrvsMatchFirstCalls == 0,
                "LoadMonDrvs used providers after a DOS open failure.");
            return;
        }
        if (definition.ParserFailure)
        {
            Require(invocation.LoadMonDrvsReadArgsCalls == 1 &&
                invocation.LoadMonDrvsFreeArgsCalls == 0 &&
                invocation.Allocations == 1 && invocation.FreeMem == 1 &&
                invocation.LoadMonDrvsMatchFirstCalls == 0,
                "LoadMonDrvs parser failure ownership differs.");
            return;
        }
        var matches = LoadMonDrvsMatches(definition);
        var residentPresent = definition.HunkShape is
            LoadMonDrvsHunkShape.Valid or
            LoadMonDrvsHunkShape.ResidentInSecondHunk;
        var processedMatches = 0;
        var matchNextCalls = 0;
        var aborted = false;
        if (definition.MatchFirstResult == 0)
        {
            for (var index = 0; index < matches.Length; index++)
            {
                var skipped = definition.DirectoryMatch ||
                    definition.Except is not null && matches[index].Equals(
                        definition.Except, StringComparison.OrdinalIgnoreCase);
                if (!skipped)
                {
                    processedMatches++;
                    if (definition.LoadSuccess &&
                        (!residentPresent || !definition.InitResidentSuccess))
                    {
                        matchNextCalls = index;
                        aborted = true;
                        break;
                    }
                }
                matchNextCalls = index + 1;
            }
        }
        var segmentLoads = definition.LoadSuccess ? processedMatches : 0;
        var initCalls = segmentLoads > 0 && residentPresent ? segmentLoads : 0;
        var initialized = definition.InitResidentSuccess ? initCalls :
            Math.Max(0, initCalls - 1);
        var segmentLoaded = segmentLoads != 0;
        Require(invocation.LoadMonDrvsReadArgsCalls == 1 &&
            invocation.LoadMonDrvsFreeArgsCalls == 1 &&
            invocation.Allocations == 2 && invocation.FreeMem == 2 &&
            invocation.LoadMonDrvsAddPartCalls == processedMatches + 1 &&
            invocation.LoadMonDrvsMatchFirstCalls == 1 &&
            invocation.LoadMonDrvsMatchEndCalls == 1 &&
            invocation.LoadMonDrvsMatchNextCalls == matchNextCalls &&
            (!aborted || invocation.LoadMonDrvsMatchNextCalls < matches.Length),
            $"LoadMonDrvs parser/matcher ownership differs (read={invocation.LoadMonDrvsReadArgsCalls}, freeArgs={invocation.LoadMonDrvsFreeArgsCalls}, alloc={invocation.Allocations}, free={invocation.FreeMem}, add={invocation.LoadMonDrvsAddPartCalls}, first={invocation.LoadMonDrvsMatchFirstCalls}, next={invocation.LoadMonDrvsMatchNextCalls}, end={invocation.LoadMonDrvsMatchEndCalls}).");
        Require(invocation.LoadMonDrvsNameFromLockCalls == processedMatches &&
            invocation.LoadMonDrvsLoadSegCalls == processedMatches &&
            invocation.LoadMonDrvsInitResidentCalls == initCalls &&
            invocation.LoadMonDrvsUnLoadSegCalls == segmentLoads - initialized,
            $"LoadMonDrvs driver segment lifecycle differs (processed={processedMatches}, loaded={segmentLoads}, init={invocation.LoadMonDrvsInitResidentCalls}, unload={invocation.LoadMonDrvsUnLoadSegCalls}).");
        Require(segmentLoaded
                ? invocation.LoadMonDrvsTypeOfMemCalls > 0
                : invocation.LoadMonDrvsTypeOfMemCalls == 0,
            "LoadMonDrvs resident discovery did not use TypeOfMem safely.");
    }
}
