using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Bounded final return, break, ERRWARN, and PrintFault policy from MorphOS Copy.</summary>
public static class NativeMorphOSCopyResultPolicy
{
    public const uint CtrlC = 1, Quiet = 2, ErrWarn = 4;

    /// <summary>
    /// Run after pattern cleanup, requester restoration and parser teardown.
    /// Observe Ctrl-C only while secondary is OK, report/select the result,
    /// then release the transfer cache. Cleanup may change IoErr afterward,
    /// as in the source; this stage does not close DOS or free command state.
    /// </summary>
    public static int Complete(ref NativeMorphOSCopyTraversalState state)
    {
        uint flags = 0;
        if ((state.Flags & 256) != 0) flags |= Quiet;
        if ((state.Flags & 1024) != 0) flags |= ErrWarn;
        if (state.SecondaryResult == DOS.RETURN_OK && NativeCommandIo.IsCtrlCPending())
            flags |= CtrlC;
        var result = Finalize(state.Result, state.SecondaryResult, flags);
        state.SecondaryResult = result;
        if (state.CopyBuffer.IsNotNull)
        {
            Exec.FreeMem(state.CopyBuffer, state.CopyBufferBytes);
            state.CopyBuffer = APTR.Null;
            state.CopyBufferBytes = 0;
        }
        return result;
    }
    /// <summary>
    /// Applies Copy's post-cleanup result selection. A pre-existing primary
    /// result overrides RetVal2; otherwise Ctrl-C becomes WARN/ERROR_BREAK,
    /// and ERRWARN promotes WARN. The source prints the final fault only when
    /// RetVal2 failed, QUIET is clear, and no primary RetVal overrides it.
    /// </summary>
    public static int Finalize(int retVal, int retVal2, uint flags)
    {
        var selected = retVal2;
        if (selected == DOS.RETURN_OK && (flags & CtrlC) != 0)
        {
            DOS.SetIoErr(DOS.Error.Break);
            selected = DOS.RETURN_WARN;
        }
        if (selected != DOS.RETURN_OK && (flags & Quiet) == 0 && retVal == DOS.RETURN_OK)
            DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
        if (retVal != DOS.RETURN_OK)
            selected = retVal;
        if ((flags & ErrWarn) != 0 && selected == DOS.RETURN_WARN)
            selected = DOS.RETURN_ERROR;
        return selected;
    }
}
