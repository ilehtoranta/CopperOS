using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Beep body. The released source contract opens
/// intuition.library v33, calls DisplayBeep(NULL), and closes the library.
/// </summary>
public static class NativeMorphOSBeepCommand
{
    public static int Run(out int ioError)
    {
        ioError = 0;
        var intuition = Exec.OpenLibraryRaw(Intuition.Name, 33);
        if (intuition.IsNull)
            return DOS.RETURN_FAIL;

        Intuition.IntuitionLibraryBase = intuition;
        Intuition.DisplayBeep(0);
        Exec.CloseLibrary(intuition);
        Intuition.IntuitionLibraryBase = APTR.Null;
        return DOS.RETURN_OK;
    }
}
