using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

public static class NativeMorphOSCopyFileTransferEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 28 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);
        var c=control.Address;
        var buffer=APTR.FromPointer(APTR.ReadUInt32(c,12));
        var size=APTR.ReadUInt32(c,16);
        var result=NativeMorphOSCopyFileTransfer.Run(
            BPTR.FromRaw(APTR.ReadUInt32(c,0)),BPTR.FromRaw(APTR.ReadUInt32(c,4)),
            APTR.ReadUInt32(c,8),APTR.ReadUInt32(c,20)!=0,ref buffer,ref size);
        APTR.WriteUInt32(c,12,buffer.Raw);
        APTR.WriteUInt32(c,16,size);
        APTR.WriteUInt32(c,24,unchecked((uint)result));
        if(buffer.IsNotNull) Exec.FreeMem(buffer,size);
        return NativeCommandStartup.Finish(DOS.RETURN_OK,0,APTR.Null);
    }
}
