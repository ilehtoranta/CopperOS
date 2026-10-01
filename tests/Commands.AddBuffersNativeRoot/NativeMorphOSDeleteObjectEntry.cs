using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private control-block entry for the Delete object-stage receipt.</summary>
public static class NativeMorphOSDeleteObjectEntry
{
    [M68kEntryPoint]
    public static int Main(int controlBytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37)) return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull) return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.ObjectWrongType, workbench);
        if (controlBytes != 4 || control.IsNull) return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.LineTooLong, APTR.Null);
        var name = CString.FromPointer(APTR.ReadUInt32(control.Address, 0));
        var result = NativeMorphOSDeleteObject.Run(name, out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
