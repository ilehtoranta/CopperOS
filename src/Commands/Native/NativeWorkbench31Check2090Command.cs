using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 installation helper that checks for the A2090 controller.
/// The original resident command opens expansion.library, searches for the
/// manufacturer/product pair, and inspects the controller's autoboot flag.
/// The entry owns DOS; this body owns only its expansion.library lease.
/// </summary>
public static class NativeWorkbench31Check2090Command
{
    public const int ResultNoController = DOS.RETURN_OK;
    public const int ResultControllerNeedsCheck = 1;
    public const int ResultControllerReady = 2;
    public const int ResultOpenFailure = DOS.RETURN_FAIL;

    private const uint A2090Manufacturer = 0x202;
    private const uint A2090Product = 1;
    private const int ConfigDevFlagsOffset = 0x10;
    private const byte AutobootFlag = 0x10;
    // FindConfigDev is a V33 expansion.library vector. The captured helper
    // asks for V37, but that is differential evidence rather than the
    // capability floor of the API used by this replacement.
    private const uint ExpansionMinimumVersion = 33;

    public static int Run(out int ioError)
    {
        ioError = 0;
        var expansion = Exec.OpenLibraryRaw(Expansion.Name,
            ExpansionMinimumVersion);
        if (expansion.IsNull)
            return ResultOpenFailure;

        Expansion.ExpansionLibraryBase = expansion;
        var configDev = APTR.FromPointer(Expansion.FindConfigDev(
            APTR.Null, unchecked((int)A2090Manufacturer),
            unchecked((int)A2090Product)));
        var result = ResultNoController;
        if (configDev.IsNotNull)
        {
            result = (APTR.ReadUInt8(configDev, ConfigDevFlagsOffset) &
                AutobootFlag) != 0
                ? ResultControllerNeedsCheck
                : ResultControllerReady;
        }

        Exec.CloseLibrary(expansion);
        Expansion.ExpansionLibraryBase = APTR.Null;
        return result;
    }
}
