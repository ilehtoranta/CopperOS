using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    private const uint WorkbenchBindDriversDirectory = 0x1777;
    private const uint WorkbenchBindDriversOldDirectory = 0x1555;

    private List<object> RunWorkbench31BindDriversEntryCases()
    {
        ProbeCase[] cases =
        [
            new("expansion-open-failure", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(ExpansionAvailable: false,
                    EntryPath: BindDriversEntryPath.WorkbenchLockFailure)
            },
            new("icon-open-failure", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(IconAvailable: false,
                    EntryPath: BindDriversEntryPath.WorkbenchLockFailure)
            },
            new("expansion-lock-failure", "", DOS.RETURN_WARN,
                (int)DOS.Error.ObjectNotFound, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchLockFailure)
            },
            new("valid-lock-not-directory", "", DOS.RETURN_WARN,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchInvalidDirectory)
            },
            new("valid-empty-expansion-directory", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchEmptyScan)
            },
            new("matching-info-without-icon", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchMatchingMissingIcon)
            },
            new("strict-product-parser-rejects-sign", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchInvalidProduct)
            },
            new("resident-driver-initialized", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchDriverSuccess)
            },
            new("loadseg-failure", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchLoadSegFailure)
            },
            new("loaded-segment-without-resident", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchMissingResident)
            },
            new("initresident-rejected-unloads-segment", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchInitResidentFailure)
            },
            new("workbench-startup-reply-after-cleanup", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                Workbench = true,
                BindDrivers = new(EntryPath:
                    BindDriversEntryPath.WorkbenchEmptyScan)
            }
        ];

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void RegisterWorkbench31BindDriversDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Lock, "Workbench BindDrivers Lock",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[1]) == "SYS:Expansion" &&
                    state.D[2] == unchecked((uint)(int)DOS.LockMode.Read),
                    "Workbench BindDrivers directory path or lock mode differs.");
                invocation.BindDriversLockCalls++;
                if (invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.WorkbenchLockFailure)
                {
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
                return WorkbenchBindDriversDirectory;
            });
        Register(baseAddress, DosLvo.Examine,
            "Workbench BindDrivers Examine", (state, invocation) =>
            {
                Require(state.D[1] == WorkbenchBindDriversDirectory &&
                    state.D[2] != 0 && (state.D[2] & 3) == 0,
                    "Workbench BindDrivers Examine lock/FIB differs.");
                invocation.BindDriversExamineCalls++;
                invocation.WorkbenchBindDriversFib = state.D[2];
                var validDirectory = invocation.Definition.BindDrivers!
                    .EntryPath != BindDriversEntryPath.WorkbenchInvalidDirectory;
                Bus.Long(state.D[2] + (uint)FileInfoBlock.DirEntryTypeOffset,
                    validDirectory ? 1u : 0u);
                return 1;
            });
        Register(baseAddress, -108, "Workbench BindDrivers ExNext",
            (state, invocation) =>
            {
                Require(state.D[1] == WorkbenchBindDriversDirectory &&
                    state.D[2] == invocation.WorkbenchBindDriversFib,
                    "Workbench BindDrivers ExNext lock/FIB differs.");
                invocation.BindDriversExNextCalls++;
                if ((invocation.Definition.BindDrivers!.EntryPath is
                    BindDriversEntryPath.WorkbenchMatchingMissingIcon or
                    BindDriversEntryPath.WorkbenchInvalidProduct or
                    BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchLoadSegFailure or
                    BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure) &&
                    invocation.BindDriversExNextCalls == 1)
                {
                    // Workbench does not filter ExNext entries by type before
                    // offering a case-insensitive .info name to icon.library.
                    Bus.Long(state.D[2] +
                        (uint)FileInfoBlock.DirEntryTypeOffset, 1);
                    WriteLoadMonCString(state.D[2] +
                        (uint)FileInfoBlock.FileNameOffset, "driver.INFO");
                    return 1;
                }
                return 0;
            });
        Register(baseAddress, DosLvo.CurrentDir,
            "Workbench BindDrivers CurrentDir", (state, invocation) =>
            {
                var call = invocation.BindDriversCurrentDirCalls++;
                if (call == 0)
                {
                    Require(state.D[1] == WorkbenchBindDriversDirectory,
                        "Workbench BindDrivers did not enter SYS:Expansion.");
                    return WorkbenchBindDriversOldDirectory;
                }
                Require(call == 1 &&
                    state.D[1] == WorkbenchBindDriversOldDirectory,
                    "Workbench BindDrivers did not restore the prior directory.");
                return WorkbenchBindDriversDirectory;
            });
        Register(baseAddress, DosLvo.UnLock,
            "Workbench BindDrivers UnLock", (state, invocation) =>
            {
                Require(state.D[1] == WorkbenchBindDriversDirectory,
                    "Workbench BindDrivers unlocked the wrong directory.");
                invocation.BindDriversUnLockCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "Workbench BindDrivers IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr,
            "Workbench BindDrivers SetIoErr", (state, invocation) =>
            {
                invocation.IoError = unchecked((int)state.D[1]);
                return 0;
            });
        Register(baseAddress, -150, "Workbench BindDrivers LoadSeg",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[1]) == "driver",
                    "Workbench BindDrivers LoadSeg did not receive the relative suffix-free filename.");
                invocation.BindDriversLoadSegCalls++;
                return invocation.Definition.BindDrivers!.EntryPath is
                    BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure
                    ? invocation.BindDriversSegment : 0;
            });
        Register(baseAddress, -156, "Workbench BindDrivers UnLoadSeg",
            (state, invocation) =>
            {
                Require(state.D[1] == invocation.BindDriversSegment,
                    "Workbench BindDrivers unloaded the wrong segment.");
                invocation.BindDriversUnLoadSegCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.StrToLong,
            "Workbench BindDrivers StrToLong", (state, invocation) =>
            {
                var text = Bus.CString(state.D[1]);
                invocation.BindDriversStrToLongCalls++;
                var digits = 0;
                while (digits < text.Length &&
                    text[digits] is >= '0' and <= '9')
                    digits++;
                if (digits == 0 || !uint.TryParse(text[..digits], out var value))
                    return uint.MaxValue;
                Bus.Long(state.D[2], value);
                return unchecked((uint)digits);
            });
    }

    private void RegisterWorkbench31BindDriversExec(uint baseAddress)
    {
        Register(baseAddress, ExecLvo.TypeOfMem,
            "Workbench BindDrivers TypeOfMem", (state, invocation) =>
            {
                invocation.BindDriversTypeOfMemCalls++;
                var segment = invocation.BindDriversSegment << 2;
                return state.A[1] >= segment - 4 &&
                    state.A[1] < segment + 64 ? 1u : 0u;
            });
        Register(baseAddress, ExecLvo.InitResident,
            "Workbench BindDrivers InitResident", (state, invocation) =>
            {
                Require(state.A[1] == invocation.BindDriversResident &&
                    state.D[1] == invocation.BindDriversSegment,
                    "Workbench BindDrivers InitResident arguments differ.");
                invocation.BindDriversInitResidentCalls++;
                return invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.WorkbenchInitResidentFailure
                    ? 0u : state.A[1];
            });
    }

    private void RegisterWorkbench31BindDriversExpansion(uint baseAddress)
    {
        Register(baseAddress, ExpansionLvo.ObtainConfigBinding,
            "Workbench BindDrivers ObtainConfigBinding", (_, invocation) =>
            {
                invocation.BindDriversObtainCalls++;
                return 0;
            });
        Register(baseAddress, ExpansionLvo.ReleaseConfigBinding,
            "Workbench BindDrivers ReleaseConfigBinding", (_, invocation) =>
            {
                invocation.BindDriversReleaseCalls++;
                return 0;
            });
        Register(baseAddress, ExpansionLvo.FindConfigDev,
            "Workbench BindDrivers FindConfigDev", (state, invocation) =>
            {
                var previous = state.A[0];
                var manufacturer = unchecked((int)state.D[0]);
                var product = unchecked((int)state.D[1]);
                invocation.BindDriversFindConfigDevQueries.Add((previous,
                    manufacturer, product));
                Require(manufacturer == 514 && product == 2,
                    "Workbench BindDrivers PRODUCT parse result differs.");
                if (previous == 0)
                {
                    invocation.BindDriversConfigDevFirst = Bus.Allocate(
                        invocation, 56, "WorkbenchBindDriversConfigDev", true);
                    return invocation.BindDriversConfigDevFirst;
                }
                return 0;
            });
        Register(baseAddress, ExpansionLvo.SetCurrentBinding,
            "Workbench BindDrivers SetCurrentBinding", (state, invocation) =>
            {
                var binding = state.A[0];
                Require(state.D[0] == (uint)CurrentBinding.Size &&
                    Bus.Long(binding) == invocation.BindDriversConfigDevFirst &&
                    Bus.Long(binding + 4) ==
                        invocation.WorkbenchBindDriversFib +
                            (uint)FileInfoBlock.FileNameOffset &&
                    Bus.Long(binding + 8) == invocation.BindDriversProduct &&
                    Bus.Long(binding + 12) == invocation.BindDriversToolTypeArray &&
                    Bus.CString(Bus.Long(binding + 4)) == "driver" &&
                    Bus.Long(invocation.BindDriversConfigDevFirst + 48) == 0 &&
                    invocation.BindDriversFindConfigDevQueries.SequenceEqual([
                        (0u, 514, 2),
                        (invocation.BindDriversConfigDevFirst, 514, 2)]) &&
                    invocation.BindDriversSetCurrentBindingCalls == 0,
                    "Workbench BindDrivers CurrentBinding layout or lookup order differs.");
                invocation.BindDriversSetCurrentBindingCalls++;
                return 0;
            });
    }

    private void RegisterWorkbench31BindDriversIcon(uint baseAddress)
    {
        Register(baseAddress, IconLvo.GetDiskObject,
            "Workbench BindDrivers GetDiskObject", (state, invocation) =>
            {
                Require(Bus.CString(state.A[0]) == "driver",
                    "Workbench BindDrivers did not strip the case-insensitive .info suffix.");
                invocation.BindDriversGetDiskObjectCalls++;
                return invocation.Definition.BindDrivers!.EntryPath ==
                    BindDriversEntryPath.WorkbenchMatchingMissingIcon
                    ? 0u : invocation.BindDriversDiskObject;
            });
        Register(baseAddress, IconLvo.FindToolType,
            "Workbench BindDrivers FindToolType", (state, invocation) =>
            {
                Require(state.A[0] == invocation.BindDriversToolTypeArray &&
                    Bus.CString(state.A[1]) == "PRODUCT",
                    "Workbench BindDrivers FindToolType arguments differ.");
                invocation.BindDriversFindToolTypeCalls++;
                return invocation.BindDriversProduct;
            });
        Register(baseAddress, IconLvo.FreeDiskObject,
            "Workbench BindDrivers FreeDiskObject", (state, invocation) =>
            {
                Require(state.A[0] == invocation.BindDriversDiskObject,
                    "Workbench BindDrivers freed the wrong DiskObject.");
                invocation.BindDriversFreeDiskObjectCalls++;
                if (invocation.BindDriversConfigDevFirst != 0)
                {
                    Bus.Release(invocation,
                        invocation.BindDriversConfigDevFirst,
                        "WorkbenchBindDriversConfigDev", 56);
                    invocation.BindDriversConfigDevFirst = 0;
                }
                return 0;
            });
    }

    private void VerifyWorkbench31BindDriversEntry(Invocation invocation)
    {
        var definition = invocation.Definition.BindDrivers!;
        var path = definition.EntryPath;
        var directoryAcquired = path is
            BindDriversEntryPath.WorkbenchInvalidDirectory or
            BindDriversEntryPath.WorkbenchEmptyScan or
            BindDriversEntryPath.WorkbenchMatchingMissingIcon or
            BindDriversEntryPath.WorkbenchInvalidProduct or
            BindDriversEntryPath.WorkbenchDriverSuccess or
            BindDriversEntryPath.WorkbenchLoadSegFailure or
            BindDriversEntryPath.WorkbenchMissingResident or
            BindDriversEntryPath.WorkbenchInitResidentFailure;
        var directoryExamined = path !=
            BindDriversEntryPath.WorkbenchLockFailure &&
            definition.ExpansionAvailable && definition.IconAvailable;
        var scanStarted = directoryExamined && path !=
            BindDriversEntryPath.WorkbenchInvalidDirectory;
        var expectedNextCalls = path switch
        {
            BindDriversEntryPath.WorkbenchEmptyScan => 1,
            BindDriversEntryPath.WorkbenchMatchingMissingIcon or
                BindDriversEntryPath.WorkbenchInvalidProduct or
                BindDriversEntryPath.WorkbenchDriverSuccess or
                BindDriversEntryPath.WorkbenchLoadSegFailure or
                BindDriversEntryPath.WorkbenchMissingResident or
                BindDriversEntryPath.WorkbenchInitResidentFailure => 2,
            _ => 0
        };
        Require(invocation.Opens == 1 && invocation.Closes == 1 &&
            invocation.BindDriversExpansionOpens == 1 &&
            invocation.BindDriversIconOpens ==
                (definition.ExpansionAvailable ? 1 : 0) &&
            invocation.BindDriversExpansionCloses ==
                (definition.ExpansionAvailable ? 1 : 0) &&
            invocation.BindDriversIconCloses ==
                (definition.ExpansionAvailable && definition.IconAvailable ? 1 : 0) &&
            invocation.BindDriversLockCalls ==
                (definition.ExpansionAvailable && definition.IconAvailable ? 1 : 0) &&
            invocation.BindDriversExamineCalls ==
                (directoryExamined ? 1 : 0) &&
            invocation.BindDriversObtainCalls == (scanStarted ? 1 : 0) &&
            invocation.BindDriversReleaseCalls == (scanStarted ? 1 : 0) &&
            invocation.BindDriversCurrentDirCalls == (scanStarted ? 2 : 0) &&
            invocation.BindDriversExNextCalls == expectedNextCalls &&
            invocation.BindDriversUnLockCalls == (directoryAcquired ? 1 : 0) &&
            invocation.BindDriversGetDiskObjectCalls ==
                (path is BindDriversEntryPath.WorkbenchMatchingMissingIcon or
                    BindDriversEntryPath.WorkbenchInvalidProduct or
                    BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchLoadSegFailure or
                    BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 1 : 0) &&
            invocation.BindDriversFindToolTypeCalls ==
                (path is BindDriversEntryPath.WorkbenchInvalidProduct or
                    BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchLoadSegFailure or
                    BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 1 : 0) &&
            invocation.BindDriversFreeDiskObjectCalls ==
                (path is BindDriversEntryPath.WorkbenchInvalidProduct or
                    BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchLoadSegFailure or
                    BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 1 : 0) &&
            invocation.BindDriversLoadSegCalls ==
                (path is BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchLoadSegFailure or
                    BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 1 : 0) &&
            invocation.BindDriversSetCurrentBindingCalls ==
                (path is BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 1 : 0) &&
            invocation.BindDriversInitResidentCalls ==
                (path is BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 1 : 0) &&
            invocation.BindDriversUnLoadSegCalls ==
                (path is BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 1 : 0) &&
            invocation.BindDriversTypeOfMemCalls ==
                (path is BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 3 :
                 path == BindDriversEntryPath.WorkbenchMissingResident ? 2 : 0) &&
            invocation.BindDriversStrToLongCalls ==
                (path is BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchLoadSegFailure or
                    BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 2 :
                    path == BindDriversEntryPath.WorkbenchInvalidProduct ? 1 : 0) &&
            invocation.BindDriversFindConfigDevQueries.Count ==
                (path is BindDriversEntryPath.WorkbenchDriverSuccess or
                    BindDriversEntryPath.WorkbenchLoadSegFailure or
                    BindDriversEntryPath.WorkbenchMissingResident or
                    BindDriversEntryPath.WorkbenchInitResidentFailure ? 2 : 0) &&
            invocation.Allocations == 0 && invocation.FreeMem == 0,
            $"Workbench BindDrivers vector or cleanup differs for {path}: " +
            $"lib={invocation.Opens}/{invocation.Closes}, " +
            $"exp={invocation.BindDriversExpansionOpens}/{invocation.BindDriversExpansionCloses}, " +
            $"icon={invocation.BindDriversIconOpens}/{invocation.BindDriversIconCloses}, " +
            $"scan={invocation.BindDriversLockCalls}/{invocation.BindDriversExamineCalls}/" +
            $"{invocation.BindDriversObtainCalls}/{invocation.BindDriversCurrentDirCalls}/" +
            $"{invocation.BindDriversExNextCalls}/{invocation.BindDriversReleaseCalls}/" +
            $"{invocation.BindDriversUnLockCalls}, getIcon={invocation.BindDriversGetDiskObjectCalls}, " +
            $"tool/free/load/bind/init/unload/mem/str/config=" +
            $"{invocation.BindDriversFindToolTypeCalls}/" +
            $"{invocation.BindDriversFreeDiskObjectCalls}/" +
            $"{invocation.BindDriversLoadSegCalls}/" +
            $"{invocation.BindDriversSetCurrentBindingCalls}/" +
            $"{invocation.BindDriversInitResidentCalls}/" +
            $"{invocation.BindDriversUnLoadSegCalls}/" +
            $"{invocation.BindDriversTypeOfMemCalls}/" +
            $"{invocation.BindDriversStrToLongCalls}/" +
            $"{invocation.BindDriversFindConfigDevQueries.Count}, " +
            $"alloc={invocation.Allocations}/{invocation.FreeMem}, " +
            $"fib=${invocation.WorkbenchBindDriversFib:X8}, " +
            $"name='{(invocation.WorkbenchBindDriversFib == 0 ? "" : Bus.CString(invocation.WorkbenchBindDriversFib + (uint)FileInfoBlock.FileNameOffset))}', " +
            $"events={string.Join(",", invocation.Events)}.");
    }
}
