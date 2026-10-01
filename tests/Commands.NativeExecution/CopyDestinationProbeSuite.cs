using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyDestinationProbeCase(bool Locks, bool Allocates,
    bool Examines, bool Directory, bool DeleteSucceeds, bool DirectoryTarget,
    bool DontOverwrite, bool ForceOverwrite, int Outcome, int Error = 0);

internal sealed class CopyDestinationNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public int Locks, Allocations, Examines, Unlocks, ProtectionChanges, Deletes, Frees;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 64;
}

internal sealed partial class ProbeFixture
{
    public const string CopyDestinationProbeSuite = "copy-destination-native-entry-vector-fixture";

    private List<object> RunCopyDestinationProbeCases()
    {
        ProbeCase[] cases =
        [
            Case("missing", false, false, false, false, false, false, false, false, 0),
            Case("required-directory", true, true, true, true, false, true, false, false, 2),
            Case("dont-overwrite", true, true, true, false, false, false, true, false, -2, (int)DOS.Error.ObjectExists),
            Case("overwritten", true, true, true, false, true, false, false, false, 1),
            Case("force-overwrite", true, true, true, false, true, false, false, true, 1),
            Case("force-protection-failure", true, true, true, false, true, false, false, true, 1),
            Case("delete-failure", true, true, true, false, false, false, false, false, -1, 205),
            Case("fib-failure", true, false, false, false, false, false, false, false, -1, 103),
            Case("examine-failure", true, true, false, false, false, false, false, false, -1, 222),
            Case("cleanup-error", true, true, true, false, false, false, false, false, -1, 902)
        ];
        var result = new List<object>();
        foreach (var probeCase in cases)
            result.AddRange(Execute([probeCase], false));
        result.AddRange(Execute([cases[1] with { Name = "interleaved-dir" },
            cases[6] with { Name = "interleaved-delete-fail" }], true));
        Bus.AssertImageUnchanged();
        return result;
    }

    private static ProbeCase Case(string name, bool locks, bool allocates, bool examines,
        bool directory, bool deleteSucceeds, bool directoryTarget, bool dontOverwrite,
        bool forceOverwrite, int outcome, int error = 0) => new(name, "",
        outcome == -1 ? DOS.RETURN_FAIL : DOS.RETURN_OK, error, "")
    {
        EntryLength = 24,
        CopyDestination = new(locks, allocates, examines, directory, deleteSucceeds,
            directoryTarget, dontOverwrite, forceOverwrite, outcome, error)
    };

    private void PrepareCopyDestinationProbe(Invocation invocation)
    {
        var control = invocation.Arguments;
        Bus.Memory.AsSpan((int)control, 64).Clear();
        invocation.CopyDestinationLayout = new CopyDestinationNativeLayout(control);
        Encoding.Latin1.GetBytes("RAM:Target").CopyTo(Bus.Memory.AsSpan((int)(control + 32)));
        Bus.Memory[control + 42] = 0;
        var probe = invocation.Definition.CopyDestination!;
        Bus.Long(control, control + 32);
        Bus.Long(control + 4, probe.DirectoryTarget ? 1u : 0);
        Bus.Long(control + 8, probe.DontOverwrite ? 1u : 0);
        Bus.Long(control + 12, probe.ForceOverwrite ? 1u : 0);
    }

    private void VerifyCopyDestinationProbe(Invocation invocation)
    {
        var probe = invocation.Definition.CopyDestination!;
        var layout = invocation.CopyDestinationLayout!;
        Require(Bus.Long(layout.Control + 16) == unchecked((uint)probe.Outcome) &&
            unchecked((int)Bus.Long(layout.Control + 20)) == probe.Error,
            "Copy destination result control differs.");
        var reachedDelete = probe.Locks && probe.Allocates && probe.Examines &&
            !probe.DontOverwrite && !(probe.DirectoryTarget && probe.Directory);
        Require(layout.Locks == 1 && layout.Unlocks == (probe.Locks ? 1 : 0) &&
            layout.Allocations == (probe.Locks ? 1 : 0) &&
            layout.Examines == (probe.Locks && probe.Allocates ? 1 : 0) &&
            layout.ProtectionChanges == (reachedDelete && probe.ForceOverwrite ? 1 : 0) &&
            layout.Deletes == (reachedDelete ? 1 : 0), "Copy destination DOS ownership differs.");
        invocation.CopyDestinationLayout = null;
    }

    private void RegisterCopyDestinationProbeDos(uint dosBase)
    {
        Register(dosBase, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var probe = invocation.Definition.CopyDestination!;
            var layout = invocation.CopyDestinationLayout!;
            Require(Bus.CString(state.D[1]) == "RAM:Target" &&
                state.D[2] == unchecked((uint)DOS.LockMode.Shared), "Copy destination Lock ABI differs.");
            layout.Locks++; invocation.IoError = probe.Error;
            return probe.Locks ? 0x130u : 0;
        });
        Register(dosBase, -228, "AllocDosObject", (state, invocation) =>
        {
            var probe = invocation.Definition.CopyDestination!;
            Require(state.D[1] == (uint)DosObjectType.FileInfoBlock, "Copy destination FIB type differs.");
            invocation.CopyDestinationLayout!.Allocations++; invocation.IoError = probe.Error;
            return probe.Allocates ? Bus.Allocate(invocation, FileInfoBlock.SizeInBytes,
                "CopyDestinationFIB", true) : 0;
        });
        Register(dosBase, DosLvo.Examine, "Examine", (state, invocation) =>
        {
            var probe = invocation.Definition.CopyDestination!;
            invocation.CopyDestinationLayout!.Examines++;
            if (probe.Examines)
                Bus.Long(state.D[2] + FileInfoBlock.DirEntryTypeOffset,
                    probe.Directory ? 2u : unchecked((uint)-3));
            invocation.IoError = probe.Error;
            return probe.Examines ? 1u : 0;
        });
        Register(dosBase, -234, "FreeDosObject", (state, invocation) =>
            { invocation.CopyDestinationLayout!.Frees++; Bus.Release(invocation, state.D[2], "CopyDestinationFIB");
                if (invocation.Definition.Name == "cleanup-error") invocation.IoError = 902;
                return 0; });
        Register(dosBase, DosLvo.UnLock, "UnLock", (_, invocation) =>
            { invocation.CopyDestinationLayout!.Unlocks++; return 0; });
        Register(dosBase, -186, "SetProtection", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "RAM:Target" && state.D[2] == 0,
                "Copy destination SetProtection ABI differs.");
            invocation.CopyDestinationLayout!.ProtectionChanges++;
            if (invocation.Definition.Name == "force-protection-failure")
                invocation.IoError = 214;
            return 0;
        });
        Register(dosBase, DosLvo.DeleteFile, "DeleteFile", (_, invocation) =>
        {
            var probe = invocation.Definition.CopyDestination!;
            Require(invocation.CopyDestinationLayout!.Frees == 0 && invocation.CopyDestinationLayout.Unlocks == 1,
                "TestDest must delete after unlock and before FIB release.");
            invocation.CopyDestinationLayout!.Deletes++; invocation.IoError = probe.Error;
            if (invocation.Definition.Name == "cleanup-error") invocation.IoError = 205;
            return probe.DeleteSucceeds ? 1u : 0;
        });
        Register(dosBase, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)invocation.IoError));
        Register(dosBase, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
            { invocation.IoError = unchecked((int)state.D[1]); return 0; });
    }
}
