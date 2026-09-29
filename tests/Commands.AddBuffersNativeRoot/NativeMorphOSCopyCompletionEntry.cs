using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's final result policy.</summary>
public static class NativeMorphOSCopyCompletionEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 16 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.LineTooLong, workbench);
        var address = control.Address;
        var policy = APTR.ReadUInt32(address, 8);
        var state = new NativeMorphOSCopyTraversalState {
            Result = (int)APTR.ReadUInt32(address, 0),
            SecondaryResult = (int)APTR.ReadUInt32(address, 4),
            Flags = ((policy & 2) != 0 ? 256u : 0) | ((policy & 4) != 0 ? 1024u : 0) };
        if ((policy & 8) != 0) { state.CopyBuffer = Exec.AllocMem(512, Exec.MemoryFlags.Public); state.CopyBufferBytes = 512; }
        var result = NativeMorphOSCopyResultPolicy.Complete(ref state);
        if (state.CopyBuffer.IsNotNull || state.CopyBufferBytes != 0) result = DOS.RETURN_FAIL;
        APTR.WriteUInt32(control, 12, unchecked((uint)result));
        return NativeCommandStartup.Finish(result, (int)DOS.IoErr(), APTR.Null);
    }
}
