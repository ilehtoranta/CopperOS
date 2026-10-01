using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyDirectoryExitProbeCase(string Kind, int Mode, int Depth,
    uint Current, uint Parent, uint ExpectedCurrent, int ExpectedDepth,
    bool Dispatch, bool ParentFailed, int Parents, int Unlocks);

internal sealed class CopyDirectoryExitNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public int Parents, Unlocks, Directories, Metadata;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 512;
}

internal sealed partial class ProbeFixture
{
    public const string CopyDirectoryExitProbeSuite =
        "copy-directory-exit-native-entry-vector-fixture";

    private List<object> RunCopyDirectoryExitProbeCases()
    {
        var cases = new[]
        {
            C("none", 0, 3, 0x220, 0, 0x220, 3, false, false, 0, 0),
            C("copy-destination", 0, 3, 0x100, 0x80, 0x80, 2, false, false, 1, 0),
            C("move-child", 1, 2, 0x220, 0x100, 0x100, 1, true, false, 1, 1),
            C("delete-parent-fail", 2, 1, 0x220, 0, 0, 0, true, true, 1, 1),
        };
        var results = new List<object>();
        foreach (var item in cases) results.AddRange(Execute([item], false));
        results.AddRange(Execute([
            cases[1] with { Name = "interleaved-copy" },
            cases[2] with { Name = "interleaved-move" },
        ], true));
        Bus.AssertImageUnchanged();
        return results;
    }

    private static ProbeCase C(string kind, int mode, int depth, uint current,
        uint parent, uint expectedCurrent, int expectedDepth, bool dispatch,
        bool parentFailed, int parents, int unlocks) =>
        new(kind, "", DOS.RETURN_OK, 0, "")
        {
            EntryLength = 44,
            CopyDirectoryExit = new(kind, mode, depth, current, parent,
                expectedCurrent, expectedDepth, dispatch, parentFailed, parents,
                unlocks),
        };

    private void PrepareCopyDirectoryExitProbe(Invocation invocation)
    {
        var control = invocation.Arguments;
        Bus.Memory.AsSpan((int)control, 512).Clear();
        invocation.CopyDirectoryExitLayout = new(control);
        var expected = invocation.Definition.CopyDirectoryExit!;
        Bus.Long(control, control + 64);
        Bus.Long(control + 4, unchecked((uint)expected.Mode));
        Bus.Long(control + 8, unchecked((uint)expected.Depth));
        Bus.Long(control + 12, 0x100);
        Bus.Long(control + 16, expected.Current);
        Bus.Long(control + 64 + (uint)DosLayout.AnchorPath.Info + FileInfoBlock.ProtectionOffset, 16);
        Bus.Memory[control + 64 + (uint)DosLayout.AnchorPath.Info + FileInfoBlock.FileNameOffset] = (byte)'d';
        if (expected.Kind != "none")
            Bus.Memory[control + 64 + (uint)DosLayout.AnchorPath.Flags] =
                (byte)AnchorPathFlags.DidDirectory;
    }

    private void VerifyCopyDirectoryExitProbe(Invocation invocation)
    {
        var expected = invocation.Definition.CopyDirectoryExit!;
        var layout = invocation.CopyDirectoryExitLayout!;
        var metadata = expected.Parents != 0 && !expected.ParentFailed && expected.Mode != 2;
        Require(layout.Directories == (metadata ? 2 : 0) && layout.Metadata == (metadata ? 1 : 0) &&
            Bus.Long(layout.Control + 36) == (expected.Kind == "none" ? 1u : 1u | (1u << 23)) &&
            Bus.Long(layout.Control + 40) == (expected.Parents == 0 ? 99u : 0),
            "Directory-exit flags, path reset or metadata lifecycle differs.");
        Require(Bus.Long(layout.Control + 20) == expected.ExpectedCurrent &&
            unchecked((int)Bus.Long(layout.Control + 24)) == expected.ExpectedDepth &&
            (Bus.Long(layout.Control + 28) != 0) == expected.Dispatch &&
            (Bus.Long(layout.Control + 32) != 0) == expected.ParentFailed &&
            layout.Parents == expected.Parents && layout.Unlocks == expected.Unlocks &&
            (Bus.Memory[layout.Control + 64 + (uint)DosLayout.AnchorPath.Flags] &
                (byte)AnchorPathFlags.DidDirectory) == 0,
            $"{invocation.Definition.Name}: Copy directory exit differs.");
        invocation.CopyDirectoryExitLayout = null;
    }

    private void RegisterCopyDirectoryExitProbeDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.CurrentDir, "CurrentDir", (state, invocation) => {
            var layout = invocation.CopyDirectoryExitLayout!;
            Require(state.D[1] == (layout.Directories == 0 ? invocation.Definition.CopyDirectoryExit!.Parent : 0x321u),
                "Directory metadata current-directory restore differs.");
            layout.Directories++;
            return 0x321;
        });
        Register(baseAddress, DosLvo.SetProtection, "SetProtection", (state, invocation) => {
            var layout = invocation.CopyDirectoryExitLayout!;
            Require(layout.Directories == 1 && Bus.CString(state.D[1]) == "d" && state.D[2] == 0,
                $"Directory metadata differs: directory calls={layout.Directories}, name={Bus.CString(state.D[1])}, protection={state.D[2]}.");
            layout.Metadata++;
            return 0; // Attribute failure does not suppress directory restoration.
        });
        Register(baseAddress, DosLvo.ParentDir, "ParentDir", (state, invocation) =>
        {
            var expected = invocation.Definition.CopyDirectoryExit!;
            var layout = invocation.CopyDirectoryExitLayout!;
            Require(state.D[1] == expected.Current, "Copy directory ParentDir ABI differs.");
            layout.Parents++;
            return expected.Parent;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            var expected = invocation.Definition.CopyDirectoryExit!;
            Require(state.D[1] == expected.Current, "Copy directory UnLock ABI differs.");
            invocation.CopyDirectoryExitLayout!.Unlocks++;
            return 0;
        });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}
