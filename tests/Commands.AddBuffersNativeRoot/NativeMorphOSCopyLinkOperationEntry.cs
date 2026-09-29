using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

public static class NativeMorphOSCopyLinkOperationEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench=NativeCommandStartup.ReceiveWorkbenchMessage();
        if(!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,0,workbench);
        if(workbench.IsNotNull||bytes!=16||control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,(int)DOS.Error.LineTooLong,workbench);
        var c=control.Address;
        var success=NativeMorphOSCopyLinkOperation.Run(BPTR.FromRaw(APTR.ReadUInt32(c,0)),
            APTR.FromPointer(APTR.ReadUInt32(c,4)),APTR.ReadUInt32(c,8)!=0);
        APTR.WriteUInt32(c,12,success?1u:0);
        return NativeCommandStartup.Finish(DOS.RETURN_OK,0,APTR.Null);
    }
}
