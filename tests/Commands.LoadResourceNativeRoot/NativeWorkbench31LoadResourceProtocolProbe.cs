using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.LoadResourceNativeRoot;

/// <summary>
/// Qualification-only caller of the production synchronous protocol. A0 is
/// the supplied worker port; D0=1 deliberately supplies an inactive parser
/// lease. This is not the LoadResource command's startup implementation.
/// </summary>
public static class NativeWorkbench31LoadResourceProtocolProbe
{
    [M68kEntryPoint]
    public static int Main(int mode, CONST_STRPTR servicePort)
    {
        var dosBase = Exec.OpenLibraryRaw("dos.library", 39);
        if (dosBase.IsNull) return DOS.RETURN_FAIL;
        DOS.DOSLibraryBase = dosBase;

        var arguments = default(NativeCommandArguments);
        int result;
        int error;
        if (mode != 1 && !NativeCommandArguments.TryRead(
            "NAME/M,LOCK/S,UNLOCK/S", 3, out arguments))
        {
            result = arguments.ReturnLevel;
            error = arguments.IoError;
        }
        else
        {
            result = NativeWorkbench31LoadResourceProtocol.Send(
                servicePort.Address, ref arguments, out error);
        }

        DOS.SetIoErr((DOS.Error)error);
        arguments.Release();
        Exec.CloseLibrary(dosBase);
        DOS.DOSLibraryBase = APTR.Null;
        return result;
    }
}
