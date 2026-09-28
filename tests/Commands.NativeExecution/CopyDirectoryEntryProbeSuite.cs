using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyDirectoryEntryProbeCase(bool First, bool All,
    bool Allowed, uint ExpectedWork, bool ExpectedEntry);

internal sealed class CopyDirectoryEntryNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 512;
}

internal sealed partial class ProbeFixture
{
    public const string CopyDirectoryEntryProbeSuite =
        "copy-directory-entry-native-entry-vector-fixture";

    private List<object> RunCopyDirectoryEntryProbeCases()
    {
        ProbeCase[] cases = [
            DirectoryEntryCase("first", true, false, false, 0, true),
            DirectoryEntryCase("first-all", true, true, true, 0, true),
            DirectoryEntryCase("ordinary", false, false, true, 1, false),
            DirectoryEntryCase("recursive", false, true, true, 3, true),
            DirectoryEntryCase("dangling", false, true, false, 1, false),
        ];
        var results = new List<object>();
        foreach (var item in cases) results.AddRange(Execute([item], false));
        results.AddRange(Execute([
            cases[3] with { Name = "interleaved-recursive" },
            cases[4] with { Name = "interleaved-dangling" },
        ], true));
        Bus.AssertImageUnchanged();
        return results;
    }

    private static ProbeCase DirectoryEntryCase(string name, bool first,
        bool all, bool allowed, uint work, bool entry) =>
        new(name, "", DOS.RETURN_OK, 0, "") {
            EntryLength = 20,
            CopyDirectoryEntry = new(first, all, allowed, work, entry),
        };

    private void PrepareCopyDirectoryEntryProbe(Invocation invocation)
    {
        var control = invocation.Arguments;
        var expected = invocation.Definition.CopyDirectoryEntry!;
        Bus.Memory.AsSpan((int)control, 512).Clear();
        invocation.CopyDirectoryEntryLayout = new(control);
        Bus.Long(control, control + 64);
        Bus.Long(control + 4, expected.First ? 1u : 0);
        Bus.Long(control + 8, expected.All ? 1u : 0);
        Bus.Long(control + 12, expected.Allowed ? 1u : 0);
        Bus.Memory[control + 64 + (uint)DosLayout.AnchorPath.Flags] =
            (byte)AnchorPathFlags.IsWild;
    }

    private void VerifyCopyDirectoryEntryProbe(Invocation invocation)
    {
        var expected = invocation.Definition.CopyDirectoryEntry!;
        var control = invocation.CopyDirectoryEntryLayout!.Control;
        var flags = (byte)AnchorPathFlags.IsWild |
            (expected.ExpectedEntry ? (byte)AnchorPathFlags.DoDirectory : 0);
        Require(Bus.Long(control + 16) == expected.ExpectedWork &&
            Bus.Memory[control + 64 + (uint)DosLayout.AnchorPath.Flags] == flags,
            $"{invocation.Definition.Name}: directory entry or work decision differs.");
        invocation.CopyDirectoryEntryLayout = null;
    }

    private void RegisterCopyDirectoryEntryProbeDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) => {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}
