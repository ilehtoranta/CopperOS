using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

public static class NativeMorphOSCopyOutputEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench=NativeCommandStartup.ReceiveWorkbenchMessage();
        if(!NativeCommandStartup.OpenDos(37))return NativeCommandStartup.Finish(DOS.RETURN_FAIL,0,workbench);
        if(workbench.IsNotNull||bytes!=24||control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,(int)DOS.Error.LineTooLong,workbench);
        var c=control.Address;
        var arguments=APTR.FromPointer(c.Raw+20);
        if(APTR.ReadUInt32(c,16)==0)
            NativeMorphOSCopyOutput.PrintName(APTR.FromPointer(APTR.ReadUInt32(c,0)),
                APTR.ReadUInt32(c,4),APTR.ReadUInt32(c,8)!=0,APTR.ReadUInt32(c,12)!=0,arguments);
        else NativeMorphOSCopyOutput.PrintNotDone(CString.FromPointer(APTR.ReadUInt32(c,0)),arguments);
        return NativeCommandStartup.Finish(DOS.RETURN_OK,0,APTR.Null);
    }
}
