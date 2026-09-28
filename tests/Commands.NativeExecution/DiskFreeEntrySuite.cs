using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record DiskFreeEntryCase(
    string? Volume = null,
    bool NoPostfix = false,
    bool Percent = false,
    bool LockSucceeds = true,
    bool InfoSucceeds = true,
    bool AllocationFailure = false,
    int ParserError = 0,
    uint TotalBlocks = 100,
    uint UsedBlocks = 40,
    uint BlockBytes = 1024);

internal sealed class DiskFreeNativeLayout(uint control, uint volume)
{
    public uint Control { get; } = control;
    public uint Volume { get; } = volume;
    public uint Info { get; set; }
    public uint Lock { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string DiskFreeEntrySuite =
        "morphos320-diskfree-native-entry-vector-fixture";

    private List<object> RunDiskFreeEntryCases()
    {
        ProbeCase[] cases =
        [
            DiskFreeCase("default", new(Volume: null, NoPostfix: false,
                Percent: false, LockSucceeds: true, InfoSucceeds: true,
                AllocationFailure: false), "", DOS.RETURN_OK, 0,
                "60 KB\n"),
            DiskFreeCase("explicit-volume", new(Volume: "DH0:"),
                "DH0:", DOS.RETURN_OK, 0, "60 KB\n"),
            DiskFreeCase("no-postfix", new(NoPostfix: true),
                "NOPOSTFIX", DOS.RETURN_OK, 0, "61440\n"),
            DiskFreeCase("percent", new(Percent: true),
                "PERCENT", DOS.RETURN_OK, 0, "60%\n"),
            DiskFreeCase("large-byte-count", new(NoPostfix: true,
                TotalBlocks: 0x100000, UsedBlocks: 0x100,
                BlockBytes: 0x1000), "NOPOSTFIX", DOS.RETURN_OK, 0,
                "4293918720\n"),
            DiskFreeCase("large-percent", new(Percent: true,
                TotalBlocks: uint.MaxValue, UsedBlocks: 1), "PERCENT",
                DOS.RETURN_OK, 0, "99%\n"),
            DiskFreeCase("parser-failure", new(ParserError: 116),
                "", DOS.RETURN_ERROR, 116, ""),
            DiskFreeCase("lock-failure", new(LockSucceeds: false),
                "", DOS.RETURN_FAIL, (int)DOS.Error.ObjectNotFound, ""),
            DiskFreeCase("info-failure", new(InfoSucceeds: false),
                "", DOS.RETURN_FAIL, (int)DOS.Error.ObjectWrongType, ""),
            DiskFreeCase("allocation-failure", new(AllocationFailure: true),
                "", DOS.RETURN_FAIL, (int)DOS.Error.NoFreeStore, ""),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { DiskFree = new() , Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { DiskFree = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { DiskFree = new(), EntryLength = 4, NullArgumentPointer = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            DiskFreeCase("interleaved-left", new(NoPostfix: true),
                "NOPOSTFIX", DOS.RETURN_OK, 0, "61440\n"),
            DiskFreeCase("interleaved-right", new(Percent: true),
                "PERCENT", DOS.RETURN_OK, 0, "60%\n")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase DiskFreeCase(string name,
        DiskFreeEntryCase definition, string arguments, int result,
        int error, string output) => new(name, arguments, result, error,
        output) { DiskFree = definition };

    private void PrepareDiskFreeEntry(Invocation invocation)
    {
        var definition = invocation.Definition.DiskFree ?? new();
        var control = invocation.Process + 0x200;
        var volume = control + 32;
        invocation.DiskFreeLayout = new(control, volume);
        PutCString(volume, definition.Volume ?? "DH0:");
    }

    private void RegisterDiskFreeEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.DiskFree!;
            Require(Bus.CString(state.D[1]) ==
                NativeMorphOSDiskFreeCommand.Template && state.D[3] == 0,
                "DiskFree template or ABI differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                NativeMorphOSDiskFreeCommand.ResultCount * 4u,
                "DiskFree result storage differs.");
            invocation.DiskFreeReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }

            var result = state.D[2];
            if (definition.Volume is not null)
                Bus.Long(result, invocation.DiskFreeLayout!.Volume);
            Bus.Long(result + 4, definition.NoPostfix ? uint.MaxValue : 0);
            Bus.Long(result + 8, definition.Percent ? uint.MaxValue : 0);
            invocation.DiskFreeRdArgs = Bus.Allocate(invocation, 40,
                "DiskFreeRdArgs", true);
            return invocation.DiskFreeRdArgs.Value;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            Bus.Release(invocation, state.D[1], "DiskFreeRdArgs");
            invocation.DiskFreeFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var definition = invocation.Definition.DiskFree!;
            var name = state.D[1] == 0 ? "" : Bus.CString(state.D[1]);
            if (definition.Volume is null)
                Require(name.Length == 0, "DiskFree current-volume lock differs.");
            else
                Require(name == definition.Volume,
                    "DiskFree volume lock differs.");
            Require(state.D[2] == unchecked((uint)DOS.LockMode.Shared),
                "DiskFree lock mode differs.");
            invocation.DiskFreeLockCalls++;
            if (!definition.LockSucceeds)
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return 0;
            }
            invocation.DiskFreeLayout!.Lock = 0x131u +
                (uint)invocation.Slot;
            return invocation.DiskFreeLayout.Lock;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state,
            invocation) =>
        {
            Require(state.D[1] == invocation.DiskFreeLayout!.Lock,
                "DiskFree unlock differs.");
            invocation.DiskFreeUnlockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Info, "Info", (state, invocation) =>
        {
            var definition = invocation.Definition.DiskFree!;
            Require(state.D[1] == invocation.DiskFreeLayout!.Lock &&
                state.D[2] == invocation.DiskFreeLayout.Info,
                "DiskFree Info ABI differs.");
            invocation.DiskFreeInfoCalls++;
            if (!definition.InfoSucceeds)
            {
                invocation.IoError = (int)DOS.Error.ObjectWrongType;
                return 0;
            }
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.NumberOfBlocks,
                definition.TotalBlocks);
            Bus.Long(state.D[2] +
                (uint)DosLayout.InfoData.NumberOfBlocksUsed,
                definition.UsedBlocks);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.BytesPerBlock,
                definition.BlockBytes);
            return 1;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state,
            invocation) =>
        {
            var definition = invocation.Definition.DiskFree!;
            var format = Bus.CString(state.D[1]);
            var args = state.D[2];
            var freeBlocks = definition.TotalBlocks >= definition.UsedBlocks
                ? definition.TotalBlocks - definition.UsedBlocks : 0u;
            var freeBytes = (ulong)freeBlocks * definition.BlockBytes;
            if (format == "%ld%%\n")
            {
                var expectedPercent = definition.TotalBlocks == 0
                    ? 0ul
                    : ((ulong)freeBlocks * 100ul) /
                    definition.TotalBlocks;
                Require(Bus.Long(args) == expectedPercent,
                    "DiskFree percentage differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{expectedPercent}%\n"));
            }
            else if (format == "%llu\n")
            {
                var high = Bus.Long(args);
                var low = Bus.Long(args + 4);
                Require(((ulong)high << 32 | low) == freeBytes,
                    "DiskFree byte count differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{freeBytes}\n"));
            }
            else
            {
                Require(format == "%ld %s\n",
                    "DiskFree display format differs.");
                var suffix = Bus.CString(Bus.Long(args + 4));
                var expected = freeBytes >= 1024u * 1024u * 1024u
                    ? ((uint)(freeBytes / (1024u * 1024u * 1024u)), "GB")
                    : freeBytes >= 1024u * 1024u
                    ? ((uint)(freeBytes / (1024u * 1024u)), "MB")
                    : ((uint)(freeBytes / 1024u), "KB");
                Require(Bus.Long(args) == expected.Item1 &&
                    suffix == expected.Item2, "DiskFree display differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{expected.Item1} {expected.Item2}\n"));
            }
            invocation.DiskFreeVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, invocation) =>
        {
            invocation.DiskFreePrintFaultCalls++;
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
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
    }

    private void VerifyDiskFreeEntry(Invocation invocation)
    {
        var definition = invocation.Definition.DiskFree!;
        if (invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.DiskFreeReadArgsCalls == 0 &&
                invocation.DiskFreeFreeArgsCalls == 0 &&
                invocation.DiskFreeLockCalls == 0 &&
                invocation.DiskFreeInfoCalls == 0 &&
                invocation.DiskFreeUnlockCalls == 0,
                "DiskFree rejected startup input too late.");
            return;
        }
        if (definition.AllocationFailure)
        {
            Require(invocation.DiskFreeReadArgsCalls == 0 &&
                invocation.DiskFreeFreeArgsCalls == 0 &&
                invocation.DiskFreeLockCalls == 0 &&
                invocation.DiskFreeInfoCalls == 0 &&
                invocation.DiskFreeUnlockCalls == 0,
                "DiskFree result allocation failure reached DOS providers.");
            return;
        }
        var parsed = definition.ParserError == 0;
        Require(invocation.DiskFreeReadArgsCalls == 1 &&
            invocation.DiskFreeFreeArgsCalls == (parsed ? 1 : 0),
            "DiskFree parser ownership differs.");
        if (!parsed)
        {
            Require(invocation.DiskFreeLockCalls == 0 &&
                invocation.DiskFreeInfoCalls == 0 &&
                invocation.DiskFreeUnlockCalls == 0,
                "DiskFree parser failure reached DOS providers.");
            return;
        }
        Require(invocation.DiskFreeLockCalls == 1,
            "DiskFree lock count differs.");
        if (!definition.LockSucceeds)
        {
            Require(invocation.DiskFreeInfoCalls == 0 &&
                invocation.DiskFreeUnlockCalls == 0 &&
                invocation.DiskFreeVPrintfCalls == 0,
                "DiskFree lock failure leaked into Info/output.");
            return;
        }
        Require(invocation.DiskFreeUnlockCalls == 1 &&
            invocation.DiskFreeInfoCalls == 1,
            "DiskFree Info lock lifetime differs.");
        Require(invocation.DiskFreeVPrintfCalls ==
            (definition.InfoSucceeds ? 1 : 0),
            "DiskFree output count differs.");
    }
}
