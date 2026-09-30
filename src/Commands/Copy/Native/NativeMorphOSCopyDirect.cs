using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Original DIRECT branch; intentionally bypasses normal object checks.</summary>
public static class NativeMorphOSCopyDirect
{
    /// <summary>Requires an admitted DIRECT COPY or DELETE invocation.</summary>
    public static void Run(ref NativeMorphOSCopyOptions options,
        ref NativeMorphOSCopyTraversalState state)
    {
        if (state.Mode == NativeMorphOSCopyModeSelection.Copy)
        {
            var source = APTR.ReadUInt32(options.Sources, 0);
            // DIRECT has its own input-first ordering and does not remove a
            // partial destination when transfer fails, unlike normal DoWork.
            var input = DOS.OpenRaw(CString.FromPointer(source), DOS.FileMode.OldFile);
            if (input.IsNotNull)
            {
                var output = DOS.OpenRaw(CString.FromPointer(options.Target.Raw), DOS.FileMode.NewFile);
                if (output.IsNotNull)
                {
                    state.SecondaryResult = NativeMorphOSCopyFileTransfer.Run(input, output,
                        state.BufferSize, state.ExtendedExamine,
                        ref state.CopyBuffer, ref state.CopyBufferBytes);
                    DOS.Close(output);
                }
                DOS.Close(input);
            }
        }
        else
        {
            var offset = 0;
            uint source;
            while ((source = APTR.ReadUInt32(options.Sources, offset)) != 0)
            {
                var name = CString.FromPointer(source);
                if ((state.Flags & 32) != 0) DOS.SetProtection(name, 0);
                DOS.DeleteFile(name);
                offset += 4;
            }
            state.SecondaryResult = DOS.RETURN_OK;
        }
    }
}
