using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private parser/setup/all operations root; not a shipping Copy entry.</summary>
public static class NativeMorphOSCopyParsedOperationsEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 32 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.BadTemplate, workbench);
        var result = NativeMorphOSCopyArgumentGate.Read(out var arguments,
            out var mode, out var flags, out var error);
        if (result == DOS.RETURN_OK)
        {
            var scratch = Exec.AllocMem(4658u,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (scratch.IsNull) { result = DOS.RETURN_FAIL; error = (int)DOS.Error.NoFreeStore; }
            else
            {
                var state = new NativeMorphOSCopyTraversalState();
                NativeMorphOSCopyOptionSetup.Apply(ref arguments, mode, flags, scratch, ref state, out var options);
                state.DestinationName = APTR.FromPointer(scratch.Raw + 2610);
                state.Path = APTR.FromPointer(scratch.Raw + 294);
                state.Fib = APTR.FromPointer(scratch.Raw + 2342);
                state.WarningArguments = APTR.FromPointer(scratch.Raw + 2602);
                NativeMorphOSCopyOptionSetup.SelectVerbosity(ref options,
                    APTR.FromPointer(scratch.Raw + 12), ref state);
                NativeMorphOSCopyOperations.Run(ref options, APTR.FromPointer(scratch.Raw + 12), ref state);
                if (state.CopyBuffer.IsNotNull) Exec.FreeMem(state.CopyBuffer, state.CopyBufferBytes);
                var c = control.Address;
                APTR.WriteUInt32(c, 0, (uint)state.Mode);
                APTR.WriteUInt32(c, 4, state.Flags);
                APTR.WriteUInt32(c, 8, state.MetadataFlags);
                APTR.WriteUInt32(c, 12, state.BufferSize);
                APTR.WriteUInt32(c, 16, options.Direct ? 1u : 0);
                APTR.WriteUInt32(c, 20, options.NoRequesters ? 1u : 0);
                APTR.WriteUInt32(c, 24, (uint)state.Result);
                var count = 0;
                while (APTR.ReadUInt32(options.Sources, count * 4) != 0) count++;
                APTR.WriteUInt32(c, 28, (uint)state.SecondaryResult);
                Exec.FreeMem(scratch, 4658u);
            }
        }
        arguments.Release();
        return NativeCommandStartup.Finish(result, error, APTR.Null);
    }
}
