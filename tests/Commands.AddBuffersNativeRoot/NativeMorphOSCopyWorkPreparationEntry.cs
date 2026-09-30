using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

public static class NativeMorphOSCopyWorkPreparationEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 40 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);
        var c=control.Address;
        var state=new NativeMorphOSCopyTraversalState {
            DestinationName=APTR.FromPointer(APTR.ReadUInt32(c,4)),
            CurrentDestination=BPTR.FromRaw(0x120),
            Mode=unchecked((int)APTR.ReadUInt32(c,8)),
            Flags=APTR.ReadUInt32(c,12),
            Result=unchecked((int)APTR.ReadUInt32(c,16)),
            SecondaryResult=unchecked((int)APTR.ReadUInt32(c,20)),
            DestinationPathSize=unchecked((int)APTR.ReadUInt32(c,24)),
        };
        var ready=NativeMorphOSCopyWorkPreparation.Prepare(
            APTR.FromPointer(APTR.ReadUInt32(c,0)),ref state);
        APTR.WriteUInt32(c,28,ready?1u:0);
        APTR.WriteUInt32(c,32,unchecked((uint)state.DestinationPathSize));
        APTR.WriteUInt32(c,36,unchecked((uint)state.SecondaryResult));
        return NativeCommandStartup.Finish(DOS.RETURN_OK,0,APTR.Null);
    }
}
