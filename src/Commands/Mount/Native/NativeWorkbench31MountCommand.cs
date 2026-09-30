using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Mount profile.  The outer and inner ReadArgs templates are
/// kept byte-for-byte separate from the MorphOS profile because Workbench's
/// captured binary uses a different record order and result count.
/// </summary>
public static class NativeWorkbench31MountCommand
{
    public const string Template = "DEVICE/M,FROM/K";
    public const uint ResultCount = 2;

    // Captured from the Workbench 3.1 Mount binary at the inner template
    // string (the leading comma is part of the original grammar).
    public const string MountTemplate =
        ",SECTORSIZE=BLOCKSIZE,,SURFACES,SECTORSPERBLOCK," +
        "SECTORSPERTRACK=BLOCKSPERTRACK,RESERVED,PREALLOC,INTERLEAVE," +
        "LOWCYL,HIGHCYL,BUFFERS,BUFMEMTYPE,MAXTRANSFER,MASK,BOOTPRI," +
        "DOSTYPE,BAUD,CONTROL,DEVICE,UNIT,FLAGS,HANDLER,STACKSIZE," +
        "PRIORITY,GLOBVEC,FILESYSTEM,STARTUP,ACTIVATE=MOUNT,EHANDLER," +
        "FORCELOAD";
    public const uint MountResultCount = 31;

    public static int Run(out int ioError)
        => NativeMorphOSMountCommand.RunProfile(Template, ResultCount,
            MountTemplate, MountResultCount, true, out ioError);

    public static int RunWorkbench(APTR startup, out int ioError)
        => NativeMorphOSMountCommand.RunWorkbenchProfile(MountTemplate,
            MountResultCount, true, startup, out ioError);
}
