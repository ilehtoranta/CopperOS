using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Copy's LinkFile operation; selection policy belongs to DoWork.</summary>
public static class NativeMorphOSCopyLinkOperation
{
    /// <summary>
    /// The source lock remains owned by the caller. A soft link uses the
    /// device-qualified name returned by NameFromLock, never a relative name.
    /// </summary>
    public static bool Run(BPTR source, APTR destination, bool soft)
    {
        var name = CString.FromPointer(destination.Raw);
        if (!soft)
            return DOS.MakeLink(name, unchecked((int)source.Raw), 0) != 0;

        var result = false;
        var buffer = Exec.AllocMem(2048, (Exec.MemoryFlags)0);
        if (buffer.IsNotNull)
        {
            if (DOS.NameFromLock(source, buffer, 2048) != 0)
                result = DOS.MakeLink(name, unchecked((int)buffer.Raw), 1) != 0;
            Exec.FreeMem(buffer, 2048);
        }
        return result;
    }
}
