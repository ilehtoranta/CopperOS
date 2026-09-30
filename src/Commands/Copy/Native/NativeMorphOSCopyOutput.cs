using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source PrintName and PrintNotDone for the always-verbose build.</summary>
public static class NativeMorphOSCopyOutput
{
    /// <summary>arguments is four invocation-owned bytes for VPrintf.</summary>
    public static void PrintName(APTR name, uint depth, bool directory,
        bool textFollows, APTR arguments)
    {
        DOS.PutStr("   ");
        var remaining = depth;
        if (remaining != 0)
            while (--remaining != 0) DOS.PutStr("        ");
        if (directory) DOS.PutStr("     ");
        APTR.WriteUInt32(arguments, 0, name.Raw);
        if (directory) DOS.VPrintf("%s (Dir)", arguments);
        else DOS.VPrintf("%s", arguments);
        if (!directory && textFollows) DOS.VPrintf("..", arguments);
        DOS.Flush(DOS.Output());
    }

    /// <summary>
    /// This source build does not print name/depth here. It reads IoErr after
    /// the prefix output; preserving an earlier error would change behavior.
    /// </summary>
    public static void PrintNotDone(CString operation, APTR arguments)
    {
        APTR.WriteUInt32(arguments, 0, CString.ToUInt32(operation));
        DOS.VPrintf(" not %s: ", arguments);
        DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
    }
}
