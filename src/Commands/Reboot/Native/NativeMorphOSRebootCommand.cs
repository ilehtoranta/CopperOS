using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-observed MorphOS/AROS Reboot command body.</summary>
public static class NativeMorphOSRebootCommand
{
    public const string Template = "";
    private const uint CtrlCMask = 1u << 12;

    private struct Cells
    {
        public uint Unused;

        public static APTR AddressOf(ref Cells cells) =>
            throw new System.NotSupportedException(
                "Reboot.Cells.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Consumes the empty DOS command template, checks Ctrl-C, and requests an
    /// Exec cold reboot. The startup adapter owns DOS and Workbench teardown.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        Cells cells = default;
        cells.Unused = 0;
        var readArgs = DOS.ReadArgs(Template, Cells.AddressOf(ref cells),
            APTR.Null);
        if (readArgs.IsNotNull)
            DOS.FreeArgs(readArgs);
        else
            ioError = (int)DOS.IoErr();

        // The released source deliberately ignores an empty-template
        // ReadArgs failure. It still checks Ctrl-C and requests ColdReboot;
        // preserve the parser IoErr when the reboot call returns.
        if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
        {
            ioError = (int)DOS.Error.Break;
            DOS.PrintFault(DOS.Error.Break, CString.FromPointer(0));
            return DOS.RETURN_ERROR;
        }

        Exec.ColdReboot();
        return 666;
    }
}
