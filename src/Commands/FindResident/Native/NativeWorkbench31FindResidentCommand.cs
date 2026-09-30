using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 FindResident candidate. The captured classic
/// executable advertises MODULE/A and uses the public Exec FindResident
/// vector after opening DOS 36. The resident lookup itself has no host or
/// managed state; parser and diagnostic ownership remain invocation-local.
/// </summary>
public static class NativeWorkbench31FindResidentCommand
{
    public const string Template = "MODULE/A";
    public const uint ResultCount = 1;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_WARN;
        var module = APTR.Null;
        if (!arguments.TryGetResult(0, out var moduleName) || moduleName == 0)
        {
            ioError = (int)DOS.Error.RequiredArgumentMissing;
        }
        else
        {
            module = APTR.FromPointer(Exec.FindResident(
                CString.FromPointer(moduleName)));
            if (module.IsNull)
                ioError = (int)DOS.Error.ObjectNotFound;
            else
                result = DOS.RETURN_OK;
        }

        arguments.Release();
        if (ioError != 0)
        {
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            DOS.SetIoErr((DOS.Error)ioError);
        }
        else
            DOS.SetIoErr(DOS.Error.None);
        return result;
    }
}
