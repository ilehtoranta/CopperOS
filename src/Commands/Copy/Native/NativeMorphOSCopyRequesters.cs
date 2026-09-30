using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Invocation-owned original Copy pr_WindowPtr save/restore.</summary>
public struct NativeMorphOSCopyRequesters
{
    private APTR _process;
    private uint _previous;

    public static NativeMorphOSCopyRequesters Capture()
    {
        var scope = new NativeMorphOSCopyRequesters();
        scope._process = Exec.FindTask(CString.FromPointer(0));
        scope._previous = APTR.ReadUInt32(scope._process, DosLayout.Process.WindowPointer);
        return scope;
    }

    public void Suppress(bool noRequesters)
    {
        // QUIET alone must not disable requesters.
        if (noRequesters)
            APTR.WriteUInt32(_process, DosLayout.Process.WindowPointer, uint.MaxValue);
    }

    public void Restore()
    {
        if (_process.IsNull) return;
        APTR.WriteUInt32(_process, DosLayout.Process.WindowPointer, _previous);
        _process = APTR.Null;
    }
}
