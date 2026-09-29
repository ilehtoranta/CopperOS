using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

public static class NativeMorphOSCopyFileOperationEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench=NativeCommandStartup.ReceiveWorkbenchMessage();
        if(!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,0,workbench);
        if(workbench.IsNotNull||bytes!=36||control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,(int)DOS.Error.LineTooLong,workbench);
        var c=control.Address;
        var lock_=BPTR.FromRaw(APTR.ReadUInt32(c,16));
        var buffer=APTR.Null;
        var size=0u;
        var success=NativeMorphOSCopyFileOperation.Run(
            APTR.FromPointer(APTR.ReadUInt32(c,0)),APTR.FromPointer(APTR.ReadUInt32(c,4)),
            APTR.ReadUInt32(c,8)!=0,APTR.ReadUInt32(c,12)!=0,
            APTR.ReadUInt32(c,20)!=0,ref lock_,512,false,ref buffer,ref size,out var opened);
        APTR.WriteUInt32(c,24,success?1u:0);
        APTR.WriteUInt32(c,28,opened?1u:0);
        APTR.WriteUInt32(c,32,lock_.Raw);
        if(buffer.IsNotNull)Exec.FreeMem(buffer,size);
        if(lock_.IsNotNull)DOS.UnLock(lock_);
        return NativeCommandStartup.Finish(DOS.RETURN_OK,0,APTR.Null);
    }
}
