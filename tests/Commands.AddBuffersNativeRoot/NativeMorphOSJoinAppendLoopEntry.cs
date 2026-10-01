using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private control-block entry for the Join append-loop receipt.</summary>
public static class NativeMorphOSJoinAppendLoopEntry
{
    [M68kEntryPoint]
    public static int Main(int controlBytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37)) return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull) return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.ObjectWrongType, workbench);
        if (controlBytes != 16 || control.IsNull) return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.LineTooLong, APTR.Null);
        var address = control.Address;
        var source = BPTR.FromRaw(APTR.ReadUInt32(address, 0));
        var destination = BPTR.FromRaw(APTR.ReadUInt32(address, 4));
        var buffer = APTR.FromPointer(APTR.ReadUInt32(address, 8));
        var bufferSize = unchecked((int)APTR.ReadUInt32(address, 12));
        var result = NativeMorphOSJoinAppendLoop.Run(destination, source, buffer, bufferSize, out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
