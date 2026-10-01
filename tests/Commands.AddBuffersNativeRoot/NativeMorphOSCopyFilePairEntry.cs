using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private control-block receipt entry for one Copy file pair.</summary>
public static class NativeMorphOSCopyFilePairEntry
{
    [M68kEntryPoint]
    public static int Main(int controlBytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, workbench);
        if (controlBytes != 16 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);
        var address = control.Address;
        var result = NativeMorphOSCopyFilePair.Run(
            APTR.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.FromPointer(APTR.ReadUInt32(address, 4)),
            APTR.FromPointer(APTR.ReadUInt32(address, 8)),
            unchecked((int)APTR.ReadUInt32(address, 12)), out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
