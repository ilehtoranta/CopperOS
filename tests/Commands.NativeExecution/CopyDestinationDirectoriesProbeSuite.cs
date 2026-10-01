using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyDestinationDirectoriesProbeCase(string Kind, int Result,
    int Error, int Locks, int Allocations, int Creates, int Deletes, int Unlocks);

internal sealed class CopyDestinationDirectoriesNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public int Locks, Allocations, Examines, Creates, Deletes, Unlocks;
    public List<string> LockNames { get; } = [];
    public Queue<bool> DirectoryExaminations { get; } = new();
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 96;
}

internal sealed partial class ProbeFixture
{
    public const string CopyDestinationDirectoriesProbeSuite = "copy-destination-directories-native-entry-vector-fixture";

    private List<object> RunCopyDestinationDirectoriesProbeCases()
    {
        ProbeCase[] cases =
        [
            Case("existing", DOS.RETURN_OK, 0, 3, 2, 0, 0, 3),
            Case("missing", DOS.RETURN_OK, 0, 3, 1, 1, 0, 3),
            Case("replaced", DOS.RETURN_OK, 0, 3, 2, 1, 1, 4),
            Case("create-failure", DOS.RETURN_FAIL, 301, 1, 0, 1, 0, 0),
            Case("final-lock-failure", DOS.RETURN_FAIL, 302, 2, 1, 0, 0, 1)
        ];
        var result = new List<object>();
        foreach (var probeCase in cases)
            result.AddRange(Execute([probeCase], false));
        result.AddRange(Execute([cases[0] with { Name = "interleaved-existing" },
            cases[2] with { Name = "interleaved-replaced" }], true));
        Bus.AssertImageUnchanged();
        return result;
    }

    private static ProbeCase Case(string kind, int result, int error, int locks,
        int allocations, int creates, int deletes, int unlocks) => new(kind, "",
        result, error, "")
    {
        EntryLength = 20,
        CopyDestinationDirectories = new(kind, result, error, locks, allocations,
            creates, deletes, unlocks)
    };

    private void PrepareCopyDestinationDirectoriesProbe(Invocation invocation)
    {
        var control = invocation.Arguments;
        Bus.Memory.AsSpan((int)control, 96).Clear();
        invocation.CopyDestinationDirectoriesLayout = new(control);
        var path = invocation.Definition.CopyDestinationDirectories!.Kind == "create-failure" ||
            invocation.Definition.CopyDestinationDirectories.Kind == "final-lock-failure" ? "RAM:a" : "RAM:a/b";
        Encoding.Latin1.GetBytes(path).CopyTo(Bus.Memory.AsSpan((int)(control + 32)));
        Bus.Memory[control + 32 + (uint)path.Length] = 0;
        Bus.Long(control, control + 32);
    }

    private void VerifyCopyDestinationDirectoriesProbe(Invocation invocation)
    {
        var probe = invocation.Definition.CopyDestinationDirectories!;
        var layout = invocation.CopyDestinationDirectoriesLayout!;
        Require((Bus.Long(layout.Control + 8) != 0) == (probe.Result == DOS.RETURN_OK) &&
            unchecked((int)Bus.Long(layout.Control + 12)) == probe.Error &&
            Bus.Memory[layout.Control + 32] == (byte)'R', "Copy directory result control differs.");
        Require(layout.Locks == probe.Locks && layout.Allocations == probe.Allocations &&
            layout.Creates == probe.Creates && layout.Deletes == probe.Deletes &&
            layout.Unlocks == probe.Unlocks, "Copy directory DOS ownership differs.");
        invocation.CopyDestinationDirectoriesLayout = null;
    }

    private void RegisterCopyDestinationDirectoriesProbeDos(uint dosBase)
    {
        Register(dosBase, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var probe = invocation.Definition.CopyDestinationDirectories!;
            var layout = invocation.CopyDestinationDirectoriesLayout!;
            var name = Bus.CString(state.D[1]);
            Require(state.D[2] == unchecked((uint)DOS.LockMode.Shared), "Copy directory Lock ABI differs.");
            layout.Locks++; layout.LockNames.Add(name);
            var call = layout.Locks;
            var existing = probe.Kind switch
            {
                "existing" => call <= 2,
                "missing" => call == 1,
                "replaced" => call <= 2,
                "final-lock-failure" => call == 1,
                _ => false
            };
            if (existing)
            {
                layout.DirectoryExaminations.Enqueue(probe.Kind != "replaced" || call == 1);
                return 0x120u + (uint)call;
            }
            if (probe.Kind == "final-lock-failure" && call == 2)
                invocation.IoError = 302;
            return call == probe.Locks && probe.Kind is not "create-failure" and not "final-lock-failure"
                ? 0x180u : 0;
        });
        Register(dosBase, -228, "AllocDosObject", (state, invocation) =>
        {
            Require(state.D[1] == (uint)DosObjectType.FileInfoBlock, "Copy directory FIB type differs.");
            invocation.CopyDestinationDirectoriesLayout!.Allocations++;
            return Bus.Allocate(invocation, FileInfoBlock.SizeInBytes, "CopyDirectoriesFIB", true);
        });
        Register(dosBase, DosLvo.Examine, "Examine", (state, invocation) =>
        {
            var layout = invocation.CopyDestinationDirectoriesLayout!;
            layout.Examines++;
            Require(layout.DirectoryExaminations.TryDequeue(out var directory), "Unexpected Copy directory Examine.");
            Bus.Long(state.D[2] + FileInfoBlock.DirEntryTypeOffset,
                directory ? 2u : unchecked((uint)-3));
            return 1;
        });
        Register(dosBase, -234, "FreeDosObject", (state, invocation) =>
            { Bus.Release(invocation, state.D[2], "CopyDirectoriesFIB"); return 0; });
        Register(dosBase, DosLvo.DeleteFile, "DeleteFile", (_, invocation) =>
            { invocation.CopyDestinationDirectoriesLayout!.Deletes++; return 1; });
        Register(dosBase, -120, "CreateDir", (state, invocation) =>
        {
            var probe = invocation.Definition.CopyDestinationDirectories!;
            var layout = invocation.CopyDestinationDirectoriesLayout!;
            layout.Creates++;
            if (probe.Kind == "create-failure") { invocation.IoError = 301; return 0; }
            return 0x170u;
        });
        Register(dosBase, DosLvo.UnLock, "UnLock", (_, invocation) =>
            { invocation.CopyDestinationDirectoriesLayout!.Unlocks++; return 0; });
        Register(dosBase, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)invocation.IoError));
        Register(dosBase, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
            { invocation.IoError = unchecked((int)state.D[1]); return 0; });
    }
}
