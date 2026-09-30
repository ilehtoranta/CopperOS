using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed final object-removal stage of MorphOS Delete. Selection,
/// protection policy, matcher advancement, diagnostics, and retry ownership
/// remain with the command coordinator.
/// </summary>
public static class NativeMorphOSDeleteObject
{
    /// <summary>
    /// Removes one already-selected, deletable object through public DOS and
    /// returns the immediate handler error when DeleteFile fails.
    /// </summary>
    public static int Run(CString name, out int ioError)
    {
        ioError = 0;
        if (CString.ToUInt32(name) == 0)
        {
            ioError = (int)DOS.Error.BadTemplate;
            return DOS.RETURN_FAIL;
        }
        if (DOS.DeleteFile(name) != 0) return DOS.RETURN_OK;
        ioError = (int)DOS.IoErr();
        return DOS.RETURN_FAIL;
    }
}
