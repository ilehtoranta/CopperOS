using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private lifecycle root; its worker reports names, not file actions.</summary>
public static class NativeMorphOSCopyTraversalEntry
{
    private struct ProbeWork : INativeMorphOSCopyWork
    {
        public uint Reserved;
        public void Execute(APTR name, ref NativeMorphOSCopyTraversalState state)
        {
            if (state.Result > ((state.Flags & 1024) != 0 ? DOS.RETURN_OK : DOS.RETURN_WARN) ||
                state.SecondaryResult != 0) return;
            // Private vector protocol: positive values change the primary
            // result; negative values set the secondary result. This exposes
            // worker-to-loop state mutation without implementing file actions.
            var result = DOS.PutStr(CString.FromPointer(name.Raw));
            if (result > 0) state.Result = result;
            else if (result < 0) state.SecondaryResult = -result;
        }
    }

    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 48 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);
        var c = control.Address;
        var state = new NativeMorphOSCopyTraversalState {
            Path = APTR.FromPointer(APTR.ReadUInt32(c, 4)),
            Fib = APTR.FromPointer(APTR.ReadUInt32(c, 8)),
            Pattern = APTR.FromPointer(APTR.ReadUInt32(c, 12)),
            WarningArguments = APTR.FromPointer(c.Raw + 40),
            Destination = BPTR.FromRaw(APTR.ReadUInt32(c, 16)),
            Mode = unchecked((int)APTR.ReadUInt32(c, 20)),
            Flags = APTR.ReadUInt32(c, 24),
        };
        var worker = new ProbeWork { Reserved = 0 };
        NativeMorphOSCopyTraversal.RunFileSystem(
            APTR.FromPointer(APTR.ReadUInt32(c, 0)),
            APTR.ReadUInt32(c, 28) != 0, ref state, ref worker);
        APTR.WriteUInt32(c, 32, unchecked((uint)state.Result));
        APTR.WriteUInt32(c, 36, unchecked((uint)state.SecondaryResult));
        return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
    }
}
