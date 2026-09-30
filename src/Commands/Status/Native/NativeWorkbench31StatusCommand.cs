namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Status profile boundary. The legacy DOS CLI-list path in the
/// shared status body is selected by the DOS36 entry; newer CLI snapshot APIs
/// remain unavailable on the original profile and are therefore not used.
/// </summary>
public static class NativeWorkbench31StatusCommand
{
    public static int Run(out int ioError) =>
        NativeMorphOSStatusCommand.Run(out ioError);
}
