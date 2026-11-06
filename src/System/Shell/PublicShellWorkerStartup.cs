using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Dispatches a child whose DOS-owned Shell image starts at its normal entry.</summary>
internal static class PublicShellWorkerStartup
{
    // NP_ExitData is set before the Process is scheduled. These values are
    // used only on a Process created by this Shell without an exit callback.
    public const uint Background = 0xc0530001;
    public const uint NewShell = 0xc0530002;

    public static bool TryRun(APTR task, out uint result)
    {
        result = 20;
        var memory = new CopperSharpNativeDosPlatform();
        if (!DosProcessCodec.IsMapped(ref memory, task) ||
            DosProcessCodec.ReadExitCode(ref memory, task).IsNotNull) return false;
        var marker = unchecked((uint)DosProcessCodec.ReadExitData(ref memory, task));
        if (marker != Background && marker != NewShell) return false;
        // Match the Shell-owned task identity as well as the private marker so
        // ordinary packet-started Shells retain their own exit data.
        var name = APTR.FromPointer(ExecNodeCodec.ReadName(ref memory, task).Raw);
        var expected = Text("CopperOS child Shell");
        if (marker == Background) expected = Text("CopperOS background Shell");
        if (!SameName(name, expected) && !PublicShellTaskIdentity.IsReservedName(name)) return false;
        DosProcessCodec.WriteExitData(ref memory, task, 0);
        result = marker == Background ? PublicShellBackgroundCore.Worker() : PublicShellNewProcessCore.Worker();
        return true;
    }

    private static bool SameName(APTR actual, APTR expected)
    {
        if (actual.IsNull) return false;
        for (var offset = 0; ; offset++)
        {
            var value = APTR.ReadUInt8(expected, offset);
            if (APTR.ReadUInt8(actual, offset) != value) return false;
            if (value == 0) return true;
        }
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
