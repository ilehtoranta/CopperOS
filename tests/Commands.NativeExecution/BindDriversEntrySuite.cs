using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal enum BindDriversEntryPath
{
    None,
    DriverSuccess,
    MultipleProductPairs,
    DirectoryEntry,
    MissingIconObject,
    MissingProduct,
    MissingConfigDev,
    MissingResident,
    LoadSegFailure,
    InitResidentFailure,
    SecondHunkResident,
    ShortHunkLength,
    TruncatedHunkExtent,
    HunkLengthOverflow,
    CyclicHunkLink,
    InvalidHunkLink,
    NameFromLockFailure,
    AddPartFailure,
    WorkbenchLockFailure,
    WorkbenchInvalidDirectory,
    WorkbenchEmptyScan,
    WorkbenchMatchingMissingIcon,
    WorkbenchInvalidProduct,
    WorkbenchDriverSuccess,
    WorkbenchLoadSegFailure,
    WorkbenchMissingResident,
    WorkbenchInitResidentFailure
}

internal sealed record BindDriversEntryCase(
    bool MissingDos = false,
    bool IconAvailable = true,
    bool ExpansionAvailable = true,
    int MatchFirstResult = (int)DOS.Error.ObjectNotFound,
    BindDriversEntryPath EntryPath = BindDriversEntryPath.None);

internal sealed partial class ProbeFixture
{
    public const string BindDriversEntrySuite =
        "binddrivers-morphos-native-entry-vector-fixture";
    public const string BindDriversProductParserEntrySuite =
        "binddrivers-morphos-product-parser-native-entry-vector-fixture";
    public const string Workbench31BindDriversEntrySuite =
        "binddrivers-wb31-native-entry-vector-fixture";

    private const uint BindDriversCurrentBase = 0x25100;
    private const uint BindDriversDiskObjectBase = 0x53000;
    private const uint BindDriversToolTypeArrayBase = 0x53100;
    private const uint BindDriversProductBase = 0x53200;
    private const uint BindDriversSegmentBase = 0x20000;
    private const uint BindDriversSegmentSlotStride = 0x4000;
    private const uint BindDriversSecondSegmentOffset = 0x1000;
    private const byte BindDriversInitialAnchorFlags =
        (byte)(AnchorPathFlags.DidDirectory | AnchorPathFlags.DoWild |
            AnchorPathFlags.NoMemoryError);

    private static uint BindDriversSegmentAddress(Invocation invocation) =>
        BindDriversSegmentBase +
        unchecked((uint)invocation.Slot) * BindDriversSegmentSlotStride;

    private static uint BindDriversSecondSegmentAddress(
        Invocation invocation) => BindDriversSegmentAddress(invocation) +
        BindDriversSecondSegmentOffset;

    public static bool IsBindDriversEntrySuite(string value) =>
        value is BindDriversEntrySuite or BindDriversProductParserEntrySuite or
            Workbench31BindDriversEntrySuite;

    private List<object> RunBindDriversEntryCases()
    {
        if (workbench31BindDrivers)
            return RunWorkbench31BindDriversEntryCases();

        ProbeCase[] cases =
        [
            new("no-match", "", DOS.RETURN_OK, Invocation.InitialIoError, "")
            {
                BindDrivers = new()
            },
            new("empty-successful-scan", "", DOS.RETURN_OK, Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0)
            },
            new("icon-open-failure", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(IconAvailable: false)
            },
            new("expansion-open-failure", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(ExpansionAvailable: false)
            },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MissingDos: true)
            },
            new("driver-load-and-init", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.DriverSuccess)
            },
            new("multiple-product-pairs", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.MultipleProductPairs)
            },
            new("directory-entry", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.DirectoryEntry)
            },
            new("missing-icon-object", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.MissingIconObject)
            },
            new("missing-product", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.MissingProduct)
            },
            new("missing-configdev", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.MissingConfigDev)
            },
            new("missing-resident", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.MissingResident)
            },
            new("loadseg-failure", "", DOS.RETURN_OK,
                (int)DOS.Error.ObjectNotFound, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.LoadSegFailure)
            },
            new("initresident-failure", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.InitResidentFailure)
            },
            new("resident-in-second-hunk", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.SecondHunkResident)
            },
            new("truncated-hunk-extent", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.TruncatedHunkExtent)
            },
            new("short-hunk-length", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.ShortHunkLength)
            },
            new("hunk-length-overflow", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.HunkLengthOverflow)
            },
            new("cyclic-hunk-link", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.CyclicHunkLink)
            },
            new("invalid-hunk-link", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.InvalidHunkLink)
            },
            new("name-from-lock-failure", "", DOS.RETURN_WARN,
                (int)DOS.Error.ObjectNotFound, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.NameFromLockFailure)
            },
            new("add-part-failure", "", DOS.RETURN_WARN,
                (int)DOS.Error.ObjectNotFound, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.AddPartFailure)
            }
        ];

        // A classic external command has writable HUNK_DATA per execution.
        // Keep this reference probe to one invocation until the fixture can
        // model independent loaded segments for interleaved processes.
        if (bindDriversSingleInvocationProbe)
            return Execute([cases[0]], false);

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-no-match" },
            cases[1] with { Name = "repeat-empty-successful-scan" }
        ], true));
        reports.AddRange(Execute([
            new ProbeCase("interleaved-driver-success", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.DriverSuccess)
            },
            new ProbeCase("interleaved-multiple-product-pairs", "",
                DOS.RETURN_OK, Invocation.InitialIoError, "")
            {
                BindDrivers = new(MatchFirstResult: 0,
                    EntryPath: BindDriversEntryPath.MultipleProductPairs)
            }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void RegisterWorkbenchBindDriversReferenceDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.MatchFirst, "Workbench BindDrivers MatchFirst",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:MatchFirst:a6=${state.A[6]:X8}:pattern=${state.D[1]:X8}:anchor=${state.D[2]:X8}");
                invocation.BindDriversMatchFirstCalls++;
                invocation.BindDriversAnchor = state.D[2];
                return unchecked((uint)invocation.Definition.BindDrivers!.MatchFirstResult);
            });
        Register(baseAddress, DosLvo.MatchNext, "Workbench BindDrivers MatchNext",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:MatchNext:a6=${state.A[6]:X8}:anchor=${state.D[1]:X8}");
                invocation.BindDriversMatchNextCalls++;
                return unchecked((uint)DOS.Error.NoMoreEntries);
            });
        Register(baseAddress, DosLvo.MatchEnd, "Workbench BindDrivers MatchEnd",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:MatchEnd:a6=${state.A[6]:X8}:anchor=${state.D[1]:X8}");
                invocation.BindDriversMatchEndCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.NameFromLock,
            "Workbench BindDrivers NameFromLock", (state, invocation) =>
            {
                invocation.Events.Add($"WB31:NameFromLock:a6=${state.A[6]:X8}:lock=${state.D[1]:X8}:buffer=${state.D[2]:X8}:size=${state.D[3]}");
                return 0;
            });
        Register(baseAddress, DosLvo.AddPart, "Workbench BindDrivers AddPart",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:AddPart:a6=${state.A[6]:X8}:path=${state.D[1]:X8}:part=${state.D[2]:X8}:size=${state.D[3]}");
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "Workbench BindDrivers IoErr",
            (state, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.PrintFault, "Workbench BindDrivers PrintFault",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:PrintFault:a6=${state.A[6]:X8}:error=${state.D[0]}:header=${state.A[0]:X8}");
                return 1;
            });
        Register(baseAddress, DosLvo.LoadSeg, "Workbench BindDrivers LoadSeg",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:LoadSeg:a6=${state.A[6]:X8}:name=${state.D[1]:X8}");
                return 0;
            });
        Register(baseAddress, DosLvo.UnLoadSeg, "Workbench BindDrivers UnLoadSeg",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:UnLoadSeg:a6=${state.A[6]:X8}:segment=${state.D[1]:X8}");
                return 0;
            });
    }

    private void RegisterWorkbenchBindDriversReferenceExec(uint baseAddress)
    {
        Register(baseAddress, ExecLvo.TypeOfMem, "Workbench BindDrivers TypeOfMem",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:TypeOfMem:a6=${state.A[6]:X8}:address=${state.A[1]:X8}");
                return 0;
            });
        Register(baseAddress, ExecLvo.InitResident, "Workbench BindDrivers InitResident",
            (state, invocation) =>
            {
                invocation.Events.Add($"WB31:InitResident:a6=${state.A[6]:X8}:resident=${state.A[1]:X8}:segment=${state.D[1]:X8}");
                return 0;
            });
    }

    private void RegisterWorkbenchBindDriversReferenceExpansion(uint baseAddress)
    {
        Register(baseAddress, ExpansionLvo.FindConfigDev,
            "Workbench BindDrivers FindConfigDev", (state, invocation) =>
            {
                invocation.Events.Add($"WB31:FindConfigDev:a6=${state.A[6]:X8}:previous=${state.A[0]:X8}:manufacturer=${state.D[0]}:product=${state.D[1]}");
                return 0;
            });
        Register(baseAddress, ExpansionLvo.ObtainConfigBinding,
            "Workbench BindDrivers ObtainConfigBinding", (state, invocation) =>
            {
                invocation.Events.Add($"WB31:ObtainConfigBinding:a6=${state.A[6]:X8}");
                return 0;
            });
        Register(baseAddress, ExpansionLvo.ReleaseConfigBinding,
            "Workbench BindDrivers ReleaseConfigBinding", (state, invocation) =>
            {
                invocation.Events.Add($"WB31:ReleaseConfigBinding:a6=${state.A[6]:X8}");
                return 0;
            });
        Register(baseAddress, ExpansionLvo.SetCurrentBinding,
            "Workbench BindDrivers SetCurrentBinding", (state, invocation) =>
            {
                invocation.Events.Add($"WB31:SetCurrentBinding:a6=${state.A[6]:X8}:a0=${state.A[0]:X8}:d0=${state.D[0]}");
                return 0;
            });
    }

    private void RegisterWorkbenchBindDriversReferenceIcon(uint baseAddress)
    {
        Register(baseAddress, IconLvo.GetDiskObjectNew,
            "Workbench BindDrivers GetDiskObjectNew", (state, invocation) =>
            {
                invocation.Events.Add($"WB31:GetDiskObjectNew:a6=${state.A[6]:X8}:name=${state.A[0]:X8}");
                return 0;
            });
        Register(baseAddress, IconLvo.FindToolType,
            "Workbench BindDrivers FindToolType", (state, invocation) =>
            {
                invocation.Events.Add($"WB31:FindToolType:a6=${state.A[6]:X8}:array=${state.A[0]:X8}:name=${state.A[1]:X8}");
                return 0;
            });
        Register(baseAddress, IconLvo.FreeDiskObject,
            "Workbench BindDrivers FreeDiskObject", (state, invocation) =>
            {
                invocation.Events.Add($"WB31:FreeDiskObject:a6=${state.A[6]:X8}:object=${state.A[0]:X8}");
                return 0;
            });
    }

    private List<object> RunBindDriversProductParserEntryCases() =>
        Execute([
            new ProbeCase("source-atoi-product-parser", "",
                DOS.RETURN_OK, Invocation.InitialIoError, "")
            {
                BindDrivers = new()
            }
        ], false);

    private void RegisterBindDriversEntryExec(uint baseAddress)
    {
        Register(baseAddress, ExecLvo.TypeOfMem, "BindDrivers TypeOfMem",
            (state, invocation) =>
            {
                Require(state.A[6] == baseAddress,
                    "BindDrivers TypeOfMem base ABI differs.");
                invocation.BindDriversTypeOfMemCalls++;
                var address = state.A[1];
                var entryPath = invocation.Definition.BindDrivers!.EntryPath;
                var segment = invocation.BindDriversSegment;
                var secondSegment = invocation.BindDriversSecondSegment;
                var firstStart = (segment << 2) - 4;
                if (address >= firstStart &&
                    address < (segment << 2) + 64)
                    return 1;
                if ((entryPath is BindDriversEntryPath.SecondHunkResident or
                        BindDriversEntryPath.CyclicHunkLink) &&
                    address >= (secondSegment << 2) - 4 &&
                    address < (secondSegment << 2) + 64)
                    return 1;
                return 0;
            });
        Register(baseAddress, ExecLvo.InitResident, "BindDrivers InitResident",
            (state, invocation) =>
            {
                Require(state.A[1] == invocation.BindDriversResident &&
                    state.D[1] == invocation.BindDriversSegment &&
                    invocation.BindDriversInitResidentCalls == 0,
                    $"BindDrivers InitResident ABI differs (A1=${state.A[1]:X8}, D1=${state.D[1]:X8}).");
                invocation.BindDriversInitResidentCalls++;
                return invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.InitResidentFailure ? 0u : state.A[1];
            });
    }

    private void PrepareBindDriversEntry(Invocation invocation)
    {
        var slotOffset = unchecked((uint)invocation.Slot) * 0x1000;
        var current = BindDriversCurrentBase + slotOffset;
        var diskObject = BindDriversDiskObjectBase + slotOffset;
        var toolTypeArray = BindDriversToolTypeArrayBase + slotOffset;
        var product = BindDriversProductBase + slotOffset;
        var segment = BindDriversSegmentAddress(invocation);
        var secondSegment = BindDriversSecondSegmentAddress(invocation);
        Bus.Memory.AsSpan((int)current, 64).Clear();
        Bus.Memory.AsSpan((int)diskObject, 64).Clear();
        Bus.Memory.AsSpan((int)toolTypeArray, 16).Clear();
        Bus.Memory.AsSpan((int)product, 64).Clear();
        Bus.Memory.AsSpan((int)(segment << 2) - 16, 256).Clear();
        Bus.Memory.AsSpan((int)(secondSegment << 2) - 16, 256).Clear();
        Bus.Long(diskObject + 0x38, toolTypeArray);
        Bus.Long(toolTypeArray, product);
        Bus.Long(toolTypeArray + 4, 0);
        var entryPath = invocation.Definition.BindDrivers!.EntryPath;
        WriteLoadMonCString(product, entryPath switch
        {
            BindDriversEntryPath.MultipleProductPairs => "PRODUCT=514/2|33/-4",
            BindDriversEntryPath.WorkbenchDriverSuccess or
                BindDriversEntryPath.WorkbenchLoadSegFailure or
                BindDriversEntryPath.WorkbenchMissingResident or
                BindDriversEntryPath.WorkbenchInitResidentFailure => "514/2",
            BindDriversEntryPath.WorkbenchInvalidProduct => "514/+2",
            _ => "PRODUCT=514/2"
        });
        var secondHunk = entryPath == BindDriversEntryPath.SecondHunkResident;
        var cyclicHunk = entryPath == BindDriversEntryPath.CyclicHunkLink;
        var hasSecondHunk = secondHunk || cyclicHunk;
        var hunkLength = entryPath switch
        {
            BindDriversEntryPath.ShortHunkLength => 1u,
            BindDriversEntryPath.TruncatedHunkExtent => 256u,
            BindDriversEntryPath.HunkLengthOverflow => 0x40000000u,
            BindDriversEntryPath.WorkbenchDriverSuccess or
                BindDriversEntryPath.WorkbenchInitResidentFailure => 64u,
            _ => 16u
        };
        var firstHunkLink = hasSecondHunk ? secondSegment :
            cyclicHunk ? secondSegment :
            entryPath == BindDriversEntryPath.InvalidHunkLink
                ? uint.MaxValue : 0;
        Bus.Long(segment << 2,
            firstHunkLink);
        Bus.Long((segment << 2) - 4, hunkLength);
        var residentMatch = entryPath is not
            (BindDriversEntryPath.MissingResident or
             BindDriversEntryPath.SecondHunkResident or
             BindDriversEntryPath.ShortHunkLength or
             BindDriversEntryPath.TruncatedHunkExtent or
             BindDriversEntryPath.HunkLengthOverflow or
             BindDriversEntryPath.CyclicHunkLink or
             BindDriversEntryPath.InvalidHunkLink);
        Bus.Word((segment << 2) + 4,
            residentMatch ? (ushort)0x4afc : (ushort)0);
        Bus.Long((segment << 2) + 6,
            residentMatch ? (segment << 2) + 4 : 0);
        var resident = (segment << 2) + 4;
        if (hasSecondHunk)
        {
            Bus.Long(secondSegment << 2,
                cyclicHunk ? segment : 0);
            Bus.Long((secondSegment << 2) - 4, 16);
            Bus.Word((secondSegment << 2) + 4,
                secondHunk ? (ushort)0x4afc : (ushort)0);
            Bus.Long((secondSegment << 2) + 6,
                secondHunk ? (secondSegment << 2) + 4 : 0);
            if (secondHunk)
                resident = (secondSegment << 2) + 4;
        }
        invocation.BindDriversCurrent = current;
        invocation.BindDriversDiskObject = diskObject;
        invocation.BindDriversToolTypeArray = toolTypeArray;
        invocation.BindDriversProduct = product;
        invocation.BindDriversSegment = segment;
        invocation.BindDriversSecondSegment = secondSegment;
        invocation.BindDriversResident = resident;
    }

    private void RegisterBindDriversEntryIcon(uint baseAddress)
    {
        Register(baseAddress, IconLvo.GetDiskObjectNew,
            "BindDrivers GetDiskObjectNew", (state, invocation) =>
            {
                Require(state.A[6] == baseAddress &&
                    Bus.CString(state.A[0]) == "SYS:Expansion/driver.info" &&
                    invocation.BindDriversGetDiskObjectNewCalls == 0,
                    "BindDrivers GetDiskObjectNew path differs.");
                invocation.BindDriversGetDiskObjectNewCalls++;
                return invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.MissingIconObject ? 0u :
                    invocation.BindDriversDiskObject;
            });
        Register(baseAddress, IconLvo.FindToolType,
            "BindDrivers FindToolType", (state, invocation) =>
            {
                Require(state.A[6] == baseAddress &&
                    state.A[0] == invocation.BindDriversToolTypeArray &&
                    Bus.CString(state.A[1]) == "PRODUCT" &&
                    invocation.BindDriversFindToolTypeCalls == 0,
                    "BindDrivers FindToolType arguments differ.");
                invocation.BindDriversFindToolTypeCalls++;
                return invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.MissingProduct ? 0u :
                    invocation.BindDriversProduct;
            });
        Register(baseAddress, IconLvo.FreeDiskObject,
            "BindDrivers FreeDiskObject", (state, invocation) =>
            {
                Require(state.A[6] == baseAddress &&
                    state.A[0] == invocation.BindDriversDiskObject &&
                    invocation.BindDriversFreeDiskObjectCalls == 0,
                    "BindDrivers FreeDiskObject ownership differs.");
                invocation.BindDriversFreeDiskObjectCalls++;
                if (invocation.BindDriversConfigDevFirst != 0)
                    Bus.Release(invocation,
                        invocation.BindDriversConfigDevFirst,
                        "BindDriversConfigDevFirst", 56);
                if (invocation.BindDriversConfigDevSecond != 0)
                    Bus.Release(invocation,
                        invocation.BindDriversConfigDevSecond,
                        "BindDriversConfigDevSecond", 56);
                if (invocation.BindDriversConfigDevThird != 0)
                    Bus.Release(invocation,
                        invocation.BindDriversConfigDevThird,
                        "BindDriversConfigDevThird", 56);
                return 0;
            });
    }

    private void RegisterBindDriversEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.MatchFirst, "BindDrivers MatchFirst",
            (state, invocation) =>
            {
                var definition = invocation.Definition.BindDrivers!;
                Require(Bus.CString(state.D[1]) == "SYS:Expansion/#?.info" &&
                    state.D[2] != 0,
                    "BindDrivers MatchFirst pattern or anchor differs.");
                invocation.BindDriversMatchFirstCalls++;
                if (definition.EntryPath != BindDriversEntryPath.None &&
                    definition.MatchFirstResult == 0)
                {
                    invocation.BindDriversAnchor = state.D[2];
                    Bus.Long(state.D[2] + (uint)DosLayout.AnchorPath.Current,
                        invocation.BindDriversCurrent);
                    Bus.Memory[state.D[2] +
                        (uint)DosLayout.AnchorPath.Flags] =
                        BindDriversInitialAnchorFlags;
                    Bus.Long(invocation.BindDriversCurrent + 8, 0x1234);
                    var info = state.D[2] + (uint)DosLayout.AnchorPath.Info;
                    Bus.Long(info + (uint)FileInfoBlock.DirEntryTypeOffset,
                        definition.EntryPath == BindDriversEntryPath.DirectoryEntry
                            ? 1u : 0u);
                    WriteLoadMonCString(info + (uint)FileInfoBlock.FileNameOffset,
                        "driver.info");
                }
                return unchecked((uint)definition.MatchFirstResult);
            });
        Register(baseAddress, DosLvo.MatchNext, "BindDrivers MatchNext",
            (state, invocation) =>
            {
                Require(state.D[1] != 0 && invocation.BindDriversMatchFirstCalls == 1,
                    "BindDrivers MatchNext anchor lifetime differs.");
                var entryPath = invocation.Definition.BindDrivers!.EntryPath;
                var expectedFlags = entryPath ==
                    BindDriversEntryPath.DirectoryEntry
                    ? (byte)(BindDriversInitialAnchorFlags &
                        ~(byte)AnchorPathFlags.DidDirectory)
                    : entryPath == BindDriversEntryPath.None
                        ? (byte)0 : BindDriversInitialAnchorFlags;
                Require(Bus.Memory[state.D[1] +
                        (uint)DosLayout.AnchorPath.Flags] == expectedFlags,
                    "BindDrivers AnchorPath directory flags differ before MatchNext.");
                invocation.BindDriversMatchNextCalls++;
                return unchecked((uint)DOS.Error.NoMoreEntries);
            });
        Register(baseAddress, DosLvo.MatchEnd, "BindDrivers MatchEnd",
            (state, invocation) =>
            {
                Require(state.D[1] != 0 && invocation.BindDriversMatchEndCalls == 0,
                    "BindDrivers MatchEnd anchor differs.");
                invocation.BindDriversMatchEndCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.NameFromLock,
            "BindDrivers NameFromLock", (state, invocation) =>
            {
                var pathOffset = ((uint)DosLayout.AnchorPath.Size + 3u) & ~3u;
                var entryPath = invocation.Definition.BindDrivers!.EntryPath;
                Require(entryPath != BindDriversEntryPath.None &&
                    state.D[1] == 0x1234 &&
                    state.D[2] == invocation.BindDriversAnchor + pathOffset &&
                    state.D[3] == 1024 &&
                    invocation.BindDriversNameFromLockCalls == 0,
                    "BindDrivers NameFromLock ABI differs.");
                invocation.BindDriversNameFromLockCalls++;
                if (entryPath == BindDriversEntryPath.NameFromLockFailure)
                {
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
                WriteLoadMonCString(state.D[2], "SYS:Expansion");
                return 1;
            });
        Register(baseAddress, DosLvo.AddPart, "BindDrivers AddPart",
            (state, invocation) =>
            {
                Require(state.D[3] == 1024 &&
                    Bus.CString(state.D[1]) == "SYS:Expansion" &&
                    Bus.CString(state.D[2]) == "driver.info",
                    "BindDrivers AddPart path components differ.");
                invocation.BindDriversAddPartCalls++;
                if (invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.AddPartFailure)
                {
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
                WriteLoadMonCString(state.D[1], "SYS:Expansion/driver.info");
                return 1;
            });
        Register(baseAddress, -150, "BindDrivers LoadSeg",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[1]) == "SYS:Expansion/driver",
                    "BindDrivers LoadSeg path did not remove .info.");
                invocation.BindDriversLoadSegCalls++;
                if (invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.LoadSegFailure)
                {
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
                return invocation.BindDriversSegment;
            });
        Register(baseAddress, -156, "BindDrivers UnLoadSeg",
            (state, invocation) =>
            {
                Require(state.D[1] == invocation.BindDriversSegment,
                    "BindDrivers unloaded the wrong segment.");
                invocation.BindDriversUnLoadSegCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "BindDrivers IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "BindDrivers SetIoErr",
            (state, invocation) =>
            {
                invocation.BindDriversSetIoErrCalls++;
                invocation.IoError = unchecked((int)state.D[1]);
                return 0;
            });
        Register(baseAddress, DosLvo.PrintFault, "BindDrivers PrintFault",
            (state, invocation) =>
            {
                var operation = invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.NameFromLockFailure
                    ? "Error on NameFromLock" : "Error on AddPart";
                Require(unchecked((int)state.D[1]) ==
                        (int)DOS.Error.ObjectNotFound &&
                    Bus.CString(state.D[2]) == operation,
                    "BindDrivers PrintFault details differ.");
                invocation.BindDriversPrintFaultCalls++;
                return 0;
            });
    }

    private void RegisterBindDriversEntryExpansion(uint baseAddress)
    {
        Register(baseAddress, -72, "BindDrivers FindConfigDev",
            (state, invocation) =>
            {
                Require(state.A[6] == baseAddress,
                    "BindDrivers FindConfigDev base ABI differs.");
                var queryIndex = invocation.BindDriversFindConfigDevQueries.Count;
                var previous = state.A[0];
                var manufacturer = unchecked((int)state.D[0]);
                var product = unchecked((int)state.D[1]);
                invocation.BindDriversFindConfigDevQueries.Add((previous,
                    manufacturer, product));
                if (invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.MissingConfigDev)
                    return 0;
                if (invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.MultipleProductPairs)
                {
                    if (manufacturer == 514 && product == 2)
                    {
                        if (previous == 0)
                        {
                            invocation.BindDriversConfigDevFirst = Bus.Allocate(
                                invocation, 56,
                                "BindDriversConfigDevFirst", true);
                            return invocation.BindDriversConfigDevFirst;
                        }
                        if (previous == invocation.BindDriversConfigDevFirst)
                        {
                            invocation.BindDriversConfigDevSecond = Bus.Allocate(
                                invocation, 56,
                                "BindDriversConfigDevSecond", true);
                            return invocation.BindDriversConfigDevSecond;
                        }
                    }
                    else if (manufacturer == 33 && product == -4 &&
                        previous == 0)
                    {
                        invocation.BindDriversConfigDevThird = Bus.Allocate(
                            invocation, 56, "BindDriversConfigDevThird", true);
                        return invocation.BindDriversConfigDevThird;
                    }
                    return 0;
                }
                if (queryIndex == 0)
                {
                    invocation.BindDriversConfigDevFirst = Bus.Allocate(
                        invocation, 56, "BindDriversConfigDevFirst", true);
                    return invocation.BindDriversConfigDevFirst;
                }
                if (queryIndex == 1)
                {
                    invocation.BindDriversConfigDevSecond = Bus.Allocate(
                        invocation, 56, "BindDriversConfigDevSecond", true);
                    return invocation.BindDriversConfigDevSecond;
                }
                if (invocation.Definition.BindDrivers!.EntryPath ==
                        BindDriversEntryPath.MultipleProductPairs &&
                    queryIndex == 2)
                {
                    invocation.BindDriversConfigDevThird = Bus.Allocate(
                        invocation, 56, "BindDriversConfigDevThird", true);
                    return invocation.BindDriversConfigDevThird;
                }
                return 0;
            });
        Register(baseAddress, ExpansionLvo.ObtainConfigBinding,
            "BindDrivers ObtainConfigBinding", (_, invocation) =>
            {
                Require(invocation.BindDriversExpansionOpens == 1 &&
                    invocation.BindDriversObtainCalls == 0,
                    "BindDrivers obtained an unowned configuration binding.");
                invocation.BindDriversObtainCalls++;
                return 0;
            });
        Register(baseAddress, ExpansionLvo.ReleaseConfigBinding,
            "BindDrivers ReleaseConfigBinding", (_, invocation) =>
            {
                Require(invocation.BindDriversObtainCalls == 1 &&
                    invocation.BindDriversReleaseCalls == 0,
                    "BindDrivers released an unowned configuration binding.");
                invocation.BindDriversReleaseCalls++;
                return 0;
            });
        Register(baseAddress, ExpansionLvo.SetCurrentBinding,
            "BindDrivers SetCurrentBinding", (state, invocation) =>
            {
                var pathOffset = ((uint)DosLayout.AnchorPath.Size + 3u) & ~3u;
                var path = invocation.BindDriversAnchor + pathOffset;
                var binding = state.A[0];
                var multiplePairs = invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.MultipleProductPairs;
                var expectedConfigDev = multiplePairs
                    ? invocation.BindDriversConfigDevThird
                    : invocation.BindDriversConfigDevSecond;
                var expectedQueries = multiplePairs
                    ? invocation.BindDriversFindConfigDevQueries.SequenceEqual([
                        (0u, 514, 2),
                        (invocation.BindDriversConfigDevFirst, 514, 2),
                        (invocation.BindDriversConfigDevSecond, 514, 2),
                        (0u, 33, -4),
                        (invocation.BindDriversConfigDevThird, 33, -4)])
                    : invocation.BindDriversFindConfigDevQueries.Count == 3;
                Require(state.A[6] == baseAddress &&
                    state.D[0] == (uint)CurrentBinding.Size &&
                    Bus.Long(binding) == expectedConfigDev &&
                    Bus.Long(binding + 4) == path &&
                    Bus.Long(binding + 8) == invocation.BindDriversProduct &&
                    Bus.Long(binding + 12) == invocation.BindDriversToolTypeArray &&
                    Bus.CString(path) == "SYS:Expansion/driver" &&
                    Bus.Long(invocation.BindDriversConfigDevFirst + 48) == 0 &&
                    Bus.Long(invocation.BindDriversConfigDevSecond + 48) ==
                        invocation.BindDriversConfigDevFirst &&
                    (!multiplePairs ||
                        Bus.Long(invocation.BindDriversConfigDevThird + 48) ==
                            invocation.BindDriversConfigDevSecond) &&
                    expectedQueries &&
                    invocation.BindDriversSetCurrentBindingCalls == 0,
                    "BindDrivers SetCurrentBinding fields differ.");
                invocation.BindDriversConfigChainValid = true;
                invocation.BindDriversSetCurrentBindingCalls++;
                return 0;
            });
    }

    private void VerifyBindDriversEntry(Invocation invocation)
    {
        if (workbench31BindDrivers)
        {
            VerifyWorkbench31BindDriversEntry(invocation);
            return;
        }

        var definition = invocation.Definition.BindDrivers!;
        if (definition.MissingDos)
        {
            Require(invocation.BindDriversIconOpens == 0 &&
                invocation.BindDriversExpansionOpens == 0 &&
                invocation.Allocations == 0 &&
                invocation.BindDriversMatchFirstCalls == 0,
                "BindDrivers used providers after a DOS open failure.");
            return;
        }
        if (!definition.IconAvailable)
        {
            Require(invocation.BindDriversIconOpens == 1 &&
                invocation.BindDriversExpansionOpens == 0 &&
                invocation.Opens == 1 && invocation.Closes == 1 &&
                invocation.Allocations == 0,
                "BindDrivers icon-library failure cleanup differs.");
            return;
        }
        if (!definition.ExpansionAvailable)
        {
            Require(invocation.BindDriversIconOpens == 1 &&
                invocation.BindDriversExpansionOpens == 1 &&
                invocation.BindDriversExpansionCloses == 0 &&
                invocation.Opens == 1 && invocation.Closes == 1 &&
                invocation.Allocations == 0,
                "BindDrivers expansion-library failure cleanup differs.");
            return;
        }
        var stopAfterPathFailure = definition.EntryPath is
            BindDriversEntryPath.NameFromLockFailure or
            BindDriversEntryPath.AddPartFailure;
        var expectedMatchNextCalls = definition.MatchFirstResult == 0 &&
            !stopAfterPathFailure ? 1 : 0;
        Require(invocation.BindDriversIconOpens == 1 &&
            invocation.BindDriversExpansionOpens == 1 &&
            invocation.BindDriversExpansionCloses == 1 &&
            invocation.BindDriversIconCloses == 1 &&
            invocation.Opens == 1 && invocation.Closes == 1 &&
            invocation.Allocations == 1 && invocation.FreeMem == 1 &&
            invocation.BindDriversExpansionOpens == invocation.BindDriversReleaseCalls &&
            invocation.BindDriversObtainCalls == invocation.BindDriversReleaseCalls &&
            invocation.BindDriversMatchFirstCalls == 1 &&
            invocation.BindDriversMatchEndCalls == 1 &&
            invocation.BindDriversMatchNextCalls == expectedMatchNextCalls,
            "BindDrivers scan result or ownership differs.");

        var expected = definition.EntryPath switch
        {
            BindDriversEntryPath.None => (0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            BindDriversEntryPath.DriverSuccess => (1, 1, 1, 1, 1, 1, 1, 1, 0, 3, 0, 0, 3),
            BindDriversEntryPath.MultipleProductPairs => (1, 1, 1, 1, 1, 1, 1, 1, 0, 5, 0, 0, 3),
            BindDriversEntryPath.DirectoryEntry => (1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            BindDriversEntryPath.MissingIconObject => (1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            BindDriversEntryPath.MissingProduct => (1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0),
            BindDriversEntryPath.MissingConfigDev => (1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0),
            BindDriversEntryPath.MissingResident => (1, 1, 1, 1, 1, 1, 0, 0, 1, 3, 0, 0, 4),
            BindDriversEntryPath.LoadSegFailure => (1, 1, 1, 1, 1, 1, 0, 0, 0, 3, 0, 0, 0),
            BindDriversEntryPath.InitResidentFailure => (1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 0, 0, 3),
            BindDriversEntryPath.SecondHunkResident => (1, 1, 1, 1, 1, 1, 1, 1, 0, 3, 0, 0, 8),
            BindDriversEntryPath.ShortHunkLength => (1, 1, 1, 1, 1, 1, 0, 0, 1, 3, 0, 0, 2),
            BindDriversEntryPath.TruncatedHunkExtent => (1, 1, 1, 1, 1, 1, 0, 0, 1, 3, 0, 0, 3),
            BindDriversEntryPath.HunkLengthOverflow => (1, 1, 1, 1, 1, 1, 0, 0, 1, 3, 0, 0, 2),
            BindDriversEntryPath.CyclicHunkLink => (1, 1, 1, 1, 1, 1, 0, 0, 1, 3, 0, 0, 10),
            BindDriversEntryPath.InvalidHunkLink => (1, 1, 1, 1, 1, 1, 0, 0, 1, 3, 0, 0, 4),
            BindDriversEntryPath.NameFromLockFailure => (1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0),
            BindDriversEntryPath.AddPartFailure => (1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0),
            _ => throw new InvalidOperationException("Unknown BindDrivers fixture path.")
        };
        var configChainExpected = definition.EntryPath is
            BindDriversEntryPath.DriverSuccess or
            BindDriversEntryPath.MultipleProductPairs or
            BindDriversEntryPath.SecondHunkResident or
            BindDriversEntryPath.InitResidentFailure;
        Require(invocation.BindDriversNameFromLockCalls == expected.Item1 &&
            invocation.BindDriversAddPartCalls == expected.Item2 &&
            invocation.BindDriversGetDiskObjectNewCalls == expected.Item3 &&
            invocation.BindDriversFindToolTypeCalls == expected.Item4 &&
            invocation.BindDriversFreeDiskObjectCalls == expected.Item5 &&
            invocation.BindDriversLoadSegCalls == expected.Item6 &&
            invocation.BindDriversSetCurrentBindingCalls == expected.Item7 &&
            invocation.BindDriversInitResidentCalls == expected.Item8 &&
            invocation.BindDriversUnLoadSegCalls == expected.Item9 &&
            invocation.BindDriversFindConfigDevQueries.Count == expected.Item10 &&
            invocation.BindDriversPrintFaultCalls == expected.Item11 &&
            invocation.BindDriversSetIoErrCalls == expected.Item12 &&
            invocation.BindDriversTypeOfMemCalls == expected.Item13 &&
            invocation.BindDriversConfigChainValid == configChainExpected,
            $"BindDrivers path {definition.EntryPath} differs (lock={invocation.BindDriversNameFromLockCalls}, add={invocation.BindDriversAddPartCalls}, icon={invocation.BindDriversGetDiskObjectNewCalls}, tool={invocation.BindDriversFindToolTypeCalls}, freeIcon={invocation.BindDriversFreeDiskObjectCalls}, load={invocation.BindDriversLoadSegCalls}, binding={invocation.BindDriversSetCurrentBindingCalls}, init={invocation.BindDriversInitResidentCalls}, unload={invocation.BindDriversUnLoadSegCalls}, findConfigDev={invocation.BindDriversFindConfigDevQueries.Count}, TypeOfMem={invocation.BindDriversTypeOfMemCalls}, faults={invocation.BindDriversPrintFaultCalls}, setIoErr={invocation.BindDriversSetIoErrCalls}).");
    }

    private void VerifyBindDriversProductParserEntry(Invocation invocation)
    {
        (uint Previous, int Manufacturer, int Product)[] expected =
        [
            (0, 514, 2),
            (invocation.BindDriversConfigDevFirst, 514, 2),
            (invocation.BindDriversConfigDevSecond, 514, 2),
            (0, 33, -4), (0, 0, -1), (0, -12, 7), (0, 123, -1),
            (0, 11, 22), (0, 33, 44)
        ];
        Require(invocation.BindDriversExpansionOpens == 1 &&
            invocation.BindDriversExpansionCloses == 1 &&
            invocation.BindDriversFindConfigDevQueries.Count == expected.Length,
            "BindDrivers PRODUCT parser lease or query count differs.");
        for (var index = 0; index < expected.Length; index++)
            Require(invocation.BindDriversFindConfigDevQueries[index] ==
                    expected[index],
                $"BindDrivers PRODUCT parser query {index} differs.");
        Require(Bus.Long(invocation.BindDriversConfigDevFirst + 48) == 0 &&
            Bus.Long(invocation.BindDriversConfigDevSecond + 48) ==
                invocation.BindDriversConfigDevFirst,
            "BindDrivers ConfigDev prepend chain differs.");
        Bus.Release(invocation, invocation.BindDriversConfigDevFirst,
            "BindDriversConfigDevFirst", 56);
        Bus.Release(invocation, invocation.BindDriversConfigDevSecond,
            "BindDriversConfigDevSecond", 56);
    }
}
