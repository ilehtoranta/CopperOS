using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyMetadataProbeCase(uint Flags, uint Protection, bool Posix,
    int ProtectionCalls, uint ExpectedProtection, int CommentCalls, int ClassicDateCalls,
    int PosixDateCalls);

internal sealed class CopyMetadataNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public int ProtectionCalls, CommentCalls, ClassicDateCalls, PosixDateCalls;
    public uint LastProtection;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 384;
}

internal sealed partial class ProbeFixture
{
    public const string CopyMetadataProbeSuite = "copy-metadata-native-entry-vector-fixture";
    private const uint CopyProtection = 1, CopyProtectionX = 2, CopyNoProtection = 4,
        CopyComment = 8, CopyDates = 16;

    private List<object> RunCopyMetadataProbeCases()
    {
        const uint archive = (uint)FileProtection.Archive;
        const uint delete = (uint)FileProtection.Delete;
        const uint execute = (uint)FileProtection.Execute;
        const uint pure = (uint)FileProtection.Pure;
        const uint script = (uint)FileProtection.Script;
        var cases = new[]
        {
            Case("normal-comment-classic", CopyProtection | CopyComment | CopyDates,
                archive | delete | execute, false, 1, delete | execute, 1, 1, 0),
            Case("prox", CopyProtectionX,
                archive | delete | execute | pure | script, false, 1, execute | pure | script, 0, 0, 0),
            Case("nopro-wins", CopyNoProtection | CopyProtection | CopyProtectionX,
                archive | delete | execute, false, 0, 0, 0, 0, 0),
            Case("posix-date", CopyDates, delete, true, 0, 0, 0, 0, 1),
            Case("no-flags", 0, delete, false, 0, 0, 0, 0, 0),
            Case("all", CopyProtection | CopyProtectionX | CopyComment | CopyDates,
                archive | delete | execute | pure | script, true, 1, delete | execute | pure | script, 1, 0, 1)
        };
        var result = new List<object>();
        foreach (var probeCase in cases)
            result.AddRange(Execute([probeCase], false));
        result.AddRange(Execute([cases[0] with { Name = "interleaved-classic" },
            cases[3] with { Name = "interleaved-posix" }], true));
        Bus.AssertImageUnchanged();
        return result;
    }

    private static ProbeCase Case(string name, uint flags, uint protection, bool posix,
        int protectionCalls, uint expectedProtection, int commentCalls, int classicDateCalls,
        int posixDateCalls) => new(name, "", DOS.RETURN_OK, 0, "")
    {
        EntryLength = 12,
        CopyMetadata = new(flags, protection, posix, protectionCalls, expectedProtection,
            commentCalls, classicDateCalls, posixDateCalls)
    };

    private void PrepareCopyMetadataProbe(Invocation invocation)
    {
        var control = invocation.Arguments;
        const uint nameOffset = 16, fibOffset = 64;
        Bus.Memory.AsSpan((int)control, 384).Clear();
        invocation.CopyMetadataLayout = new(control);
        Encoding.Latin1.GetBytes("RAM:Target").CopyTo(Bus.Memory.AsSpan((int)(control + nameOffset)));
        Bus.Memory[control + nameOffset + 10] = 0;
        var fib = control + fibOffset;
        var probe = invocation.Definition.CopyMetadata!;
        Bus.Long(fib + FileInfoBlock.ProtectionOffset, probe.Protection);
        Bus.Long(fib + FileInfoBlock.DateDaysOffset, 1234);
        Bus.Long(fib + FileInfoBlock.DateMinuteOffset, 567);
        Bus.Long(fib + FileInfoBlock.DateTickOffset, 89);
        Bus.Memory[fib + FileInfoBlock.ActualExtensionFlagsOffset] = probe.Posix
            ? (byte)FileInfoExtensionFlags.PosixDate : (byte)0;
        Encoding.Latin1.GetBytes("comment").CopyTo(Bus.Memory.AsSpan((int)(fib + FileInfoBlock.CommentOffset)));
        Bus.Memory[fib + FileInfoBlock.CommentOffset + 7] = 0;
        Bus.Long(control, control + nameOffset);
        Bus.Long(control + 4, fib);
        Bus.Long(control + 8, probe.Flags);
    }

    private void VerifyCopyMetadataProbe(Invocation invocation)
    {
        var probe = invocation.Definition.CopyMetadata!;
        var layout = invocation.CopyMetadataLayout!;
        Require(layout.ProtectionCalls == probe.ProtectionCalls &&
            (probe.ProtectionCalls == 0 || layout.LastProtection == probe.ExpectedProtection) &&
            layout.CommentCalls == probe.CommentCalls && layout.ClassicDateCalls == probe.ClassicDateCalls &&
            layout.PosixDateCalls == probe.PosixDateCalls, $"{invocation.Definition.Name}: Copy metadata DOS calls differ.");
        invocation.CopyMetadataLayout = null;
    }

    private void RegisterCopyMetadataProbeDos(uint dosBase)
    {
        Register(dosBase, DosLvo.SetProtection, "SetProtection", (state, invocation) =>
        {
            var layout = invocation.CopyMetadataLayout!;
            Require(Bus.CString(state.D[1]) == "RAM:Target", "Copy metadata protection target differs.");
            Require(layout.CommentCalls == 0 && layout.ClassicDateCalls == 0 && layout.PosixDateCalls == 0, "Protection must precede dates and comment.");
            layout.ProtectionCalls++; layout.LastProtection = state.D[2]; return 0;
        });
        Register(dosBase, DosLvo.SetComment, "SetComment", (state, invocation) =>
        {
            var layout = invocation.CopyMetadataLayout!;
            Require(Bus.CString(state.D[1]) == "RAM:Target" && state.D[2] == layout.Control + 64 + FileInfoBlock.CommentOffset,
                $"Copy metadata comment ABI differs: ${state.D[2]:X8}.");
            var probe = invocation.Definition.CopyMetadata!;
            Require(layout.ProtectionCalls == probe.ProtectionCalls && layout.ClassicDateCalls == probe.ClassicDateCalls && layout.PosixDateCalls == probe.PosixDateCalls, "Comment must follow protection and date calls, including failed calls.");
            layout.CommentCalls++; return 0;
        });
        Register(dosBase, DosLvo.SetFileDate, "SetFileDate", (state, invocation) =>
        {
            var layout = invocation.CopyMetadataLayout!;
            Require(Bus.CString(state.D[1]) == "RAM:Target" && state.D[2] == layout.Control + 64 + FileInfoBlock.DateDaysOffset,
                "Copy metadata classic-date ABI differs.");
            Require(layout.CommentCalls == 0 && layout.ProtectionCalls == invocation.Definition.CopyMetadata!.ProtectionCalls, "Classic date order differs.");
            layout.ClassicDateCalls++; return 0;
        });
        Register(dosBase, DosLvo.SetFilePosixDate, "SetFilePosixDate", (state, invocation) =>
        {
            var layout = invocation.CopyMetadataLayout!;
            Require(Bus.CString(state.D[1]) == "RAM:Target" && state.D[2] == layout.Control + 64 + FileInfoBlock.DateDaysOffset && state.D[3] == 0,
                "Copy metadata POSIX-date ABI differs.");
            Require(layout.CommentCalls == 0 && layout.ProtectionCalls == invocation.Definition.CopyMetadata!.ProtectionCalls, "POSIX date order differs.");
            layout.PosixDateCalls++; return 0;
        });
        Register(dosBase, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
            { invocation.IoError = unchecked((int)state.D[1]); return 0; });
    }
}
