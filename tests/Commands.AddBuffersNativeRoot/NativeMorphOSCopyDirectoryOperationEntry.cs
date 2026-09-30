using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

public static class NativeMorphOSCopyDirectoryOperationEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench=NativeCommandStartup.ReceiveWorkbenchMessage();
        if(!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,0,workbench);
        if(workbench.IsNotNull||bytes!=44||control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,(int)DOS.Error.LineTooLong,workbench);
        var c=control.Address;
        var state=new NativeMorphOSCopyTraversalState {
            Path=APTR.FromPointer(APTR.ReadUInt32(c,0)),
            DestinationName=APTR.FromPointer(APTR.ReadUInt32(c,4)),
            Destination=BPTR.FromRaw(APTR.ReadUInt32(c,8)),
            CurrentDestination=BPTR.FromRaw(APTR.ReadUInt32(c,12)),
            Mode=unchecked((int)APTR.ReadUInt32(c,16)),Flags=APTR.ReadUInt32(c,20),
            DestinationPathSize=99,
        };
        var outcome=NativeMorphOSCopyDirectoryOperation.Run(
            BPTR.FromRaw(APTR.ReadUInt32(c,24)),ref state);
        APTR.WriteUInt32(c,28,unchecked((uint)outcome));
        APTR.WriteUInt32(c,32,unchecked((uint)state.Result));
        APTR.WriteUInt32(c,36,state.CurrentDestination.Raw);
        APTR.WriteUInt32(c,40,unchecked((uint)state.DestinationPathSize));
        return NativeCommandStartup.Finish(DOS.RETURN_OK,0,APTR.Null);
    }
}
