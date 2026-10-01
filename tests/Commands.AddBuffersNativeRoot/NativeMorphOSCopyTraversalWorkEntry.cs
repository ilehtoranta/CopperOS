using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Combined matcher and actual object-worker native receipt root.</summary>
public static class NativeMorphOSCopyTraversalWorkEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench=NativeCommandStartup.ReceiveWorkbenchMessage();
        if(!NativeCommandStartup.OpenDos(37))return NativeCommandStartup.Finish(DOS.RETURN_FAIL,0,workbench);
        if(workbench.IsNotNull||bytes!=64||control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,(int)DOS.Error.LineTooLong,workbench);
        var c=control.Address;
        // Private fixture roots must own scratch just as the normal command does.
        var examineTags = APTR.Null;
        if (APTR.ReadUInt32(c,40)!=0)
        {
            examineTags = Exec.AllocMem(16, Exec.MemoryFlags.Public);
            if (examineTags.IsNull)
                return NativeCommandStartup.Finish(DOS.RETURN_FAIL,(int)DOS.Error.NoFreeStore,APTR.Null);
        }
        var state=new NativeMorphOSCopyTraversalState {
            Path=APTR.FromPointer(APTR.ReadUInt32(c,4)),Fib=APTR.FromPointer(APTR.ReadUInt32(c,8)),
            DestinationName=APTR.FromPointer(APTR.ReadUInt32(c,12)),
            Destination=BPTR.FromRaw(APTR.ReadUInt32(c,16)),
            Mode=unchecked((int)APTR.ReadUInt32(c,20)),Flags=APTR.ReadUInt32(c,24),
            BufferSize=APTR.ReadUInt32(c,32),MetadataFlags=APTR.ReadUInt32(c,36),
            ExtendedExamine=APTR.ReadUInt32(c,40)!=0,ExamineTags=examineTags,Depth=1,
            WarningArguments=APTR.FromPointer(c.Raw+56),
        };
        NativeMorphOSCopyTraversal.RunFileSystem(APTR.FromPointer(APTR.ReadUInt32(c,0)),
            APTR.ReadUInt32(c,28)!=0,ref state);
        APTR.WriteUInt32(c,44,unchecked((uint)state.Result));
        APTR.WriteUInt32(c,48,unchecked((uint)state.SecondaryResult));
        APTR.WriteUInt32(c,52,state.Flags);
        if(state.CopyBuffer.IsNotNull)Exec.FreeMem(state.CopyBuffer,state.CopyBufferBytes);
        if(examineTags.IsNotNull)Exec.FreeMem(examineTags,16);
        return NativeCommandStartup.Finish(DOS.RETURN_OK,0,APTR.Null);
    }
}
