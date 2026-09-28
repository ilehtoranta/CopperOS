using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyFlatTraversalProbeCase(string[] Paths, int FinalResult);

internal sealed class CopyFlatTraversalNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public int MatchFirsts, MatchNexts, MatchEnds, Index;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 12288;
}

internal sealed partial class ProbeFixture
{
    private const uint CopyPathBytes = 2048;
    private const uint CopyWorkRecordBytes = CopyPathBytes + FileInfoBlock.SizeInBytes;
    public const string CopyFlatTraversalProbeSuite =
        "copy-flat-traversal-native-entry-vector-fixture";

    private List<object> RunCopyFlatTraversalProbeCases()
    {
        var cases = new[]
        {
            C("two-files", ["SYS:one", "SYS:two"], (int)DOS.Error.NoMoreEntries),
            C("one-file", ["RAM:only"], (int)DOS.Error.NoMoreEntries),
            C("long-paths", ["SYS:" + new string('a', 1800), "RAM:" + new string('b', 1900)], (int)DOS.Error.NoMoreEntries),
            C("first-failure", [], (int)DOS.Error.ObjectNotFound),
            C("next-failure", ["RAM:before-error"], (int)DOS.Error.DeviceNotMounted),
        };
        var results = new List<object>();
        foreach (var item in cases) results.AddRange(Execute([item], false));
        results.AddRange(Execute([
            cases[0] with { Name = "interleaved-two-files" },
            cases[1] with { Name = "interleaved-one-file" },
        ], true));
        Bus.AssertImageUnchanged();
        return results;
    }

    private static ProbeCase C(string name, string[] paths, int terminal) =>
        new(name, "", DOS.RETURN_OK, 0, "")
        {
            EntryLength = 24,
            CopyFlatTraversal = new(paths, terminal),
        };

    private void PrepareCopyFlatTraversalProbe(Invocation invocation)
    {
        var control = invocation.Arguments;
        Bus.Memory.AsSpan((int)control, 12288).Clear();
        invocation.CopyFlatTraversalLayout = new(control);
        Encoding.Latin1.GetBytes("SYS:#?").CopyTo(Bus.Memory.AsSpan((int)(control + 64)));
        Bus.Long(control, control + 64);
        Bus.Long(control + 4, control + 256);
        Bus.Long(control + 8, control + 4096);
        Bus.Long(control + 12, 3 * CopyWorkRecordBytes);
    }

    private void VerifyCopyFlatTraversalProbe(Invocation invocation)
    {
        var expected = invocation.Definition.CopyFlatTraversal!;
        var layout = invocation.CopyFlatTraversalLayout!;
        var expectedNexts = expected.Paths.Length == 0 ? 0 : expected.Paths.Length;
        Require(unchecked((int)Bus.Long(layout.Control + 16)) == expected.FinalResult &&
            Bus.Long(layout.Control + 20) == (uint)expected.Paths.Length &&
            layout.MatchFirsts == 1 && layout.MatchNexts == expectedNexts &&
            layout.MatchEnds == 1,
            $"{invocation.Definition.Name}: Copy flat matcher lifecycle differs.");
        for (var index = 0; index < expected.Paths.Length; index++)
        {
            var record = layout.Control + 4096 +
                (uint)index * CopyWorkRecordBytes;
            Require(Bus.CString(record) == expected.Paths[index] &&
                Bus.Long(record + CopyPathBytes +
                    FileInfoBlock.DiskKeyOffset) == (uint)(index + 1) &&
                Bus.Memory[record + CopyPathBytes +
                    FileInfoBlock.FileNameOffset] == (byte)(0x40 + index),
                $"{invocation.Definition.Name}: deferred Copy work record {index} differs.");
        }
        invocation.CopyFlatTraversalLayout = null;
    }

    private void RegisterCopyFlatTraversalProbeDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state, invocation) =>
        {
            var layout = invocation.CopyFlatTraversalLayout!;
            Require(Bus.CString(state.D[1]) == "SYS:#?" &&
                Bus.Long(state.D[2] + (uint)DosLayout.AnchorPath.BreakBits) == (1u << 12) &&
                Bus.Word(state.D[2] + (uint)DosLayout.AnchorPath.StringLength) == 2048,
                "Copy flat MatchFirst ABI differs.");
            layout.MatchFirsts++;
            var expected = invocation.Definition.CopyFlatTraversal!;
            if (expected.Paths.Length == 0)
                return unchecked((uint)expected.FinalResult);
            PutCopyFlatEntry(state.D[2], expected.Paths[0], 0);
            layout.Index = 1;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state, invocation) =>
        {
            var layout = invocation.CopyFlatTraversalLayout!;
            var expected = invocation.Definition.CopyFlatTraversal!;
            Require(state.D[1] == layout.Control + 256,
                "Copy flat MatchNext ABI differs.");
            layout.MatchNexts++;
            if (layout.Index >= expected.Paths.Length)
                return unchecked((uint)expected.FinalResult);
            PutCopyFlatEntry(state.D[1], expected.Paths[layout.Index], layout.Index);
            layout.Index++;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state, invocation) =>
        {
            var layout = invocation.CopyFlatTraversalLayout!;
            Require(state.D[1] == layout.Control + 256,
                "Copy flat MatchEnd ABI differs.");
            layout.MatchEnds++;
            return 0;
        });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void PutCopyFlatEntry(uint anchor, string path, int index)
    {
        Encoding.Latin1.GetBytes(path).CopyTo(Bus.Memory.AsSpan((int)(anchor +
            (uint)DosLayout.AnchorPath.PathBuffer)));
        Bus.Memory[anchor + (uint)DosLayout.AnchorPath.PathBuffer +
            (uint)path.Length] = 0;
        Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
            FileInfoBlock.DiskKeyOffset, (uint)(index + 1));
        Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Info +
            FileInfoBlock.FileNameOffset] = (byte)(0x40 + index);
    }
}
