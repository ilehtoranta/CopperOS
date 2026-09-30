using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private explicit RDArgs/setup/operations root; full startup/finalization parity remains open.</summary>
public static class NativeMorphOSCopyExplicitParserOperationsEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 32 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.BadTemplate, workbench);
        var slots = Exec.AllocMem(96, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (slots.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, (int)DOS.Error.NoFreeStore, APTR.Null);
        var parsed = NativeMorphOSCopyParser.Read(slots, out var parser, out var arguments);
        var mode = 0;
        uint flags = 0;
        var error = parsed ? 0 : (int)DOS.IoErr();
        var result = parsed ? NativeMorphOSCopyArgumentGate.Evaluate(ref arguments,
            out mode, out flags, out error) : DOS.RETURN_FAIL;
        if (parsed)
        {
            var scratch = Exec.AllocMem(4658u,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (scratch.IsNull) { result = DOS.RETURN_FAIL; error = (int)DOS.Error.NoFreeStore; }
            else
            {
                var requesters = NativeMorphOSCopyRequesters.Capture();
                var state = new NativeMorphOSCopyTraversalState();
                NativeMorphOSCopyOptionSetup.Apply(ref arguments, mode, flags, scratch, ref state, out var options);
                requesters.Suppress(options.NoRequesters);
                state.DestinationName = APTR.FromPointer(scratch.Raw + 2610);
                state.Path = APTR.FromPointer(scratch.Raw + 294);
                state.Fib = APTR.FromPointer(scratch.Raw + 2342);
                state.WarningArguments = APTR.FromPointer(scratch.Raw + 2602);
                NativeMorphOSCopyOptionSetup.SelectVerbosity(ref options,
                    APTR.FromPointer(scratch.Raw + 12), ref state);
                if (result == DOS.RETURN_OK)
                    NativeMorphOSCopyOperations.Run(ref options, APTR.FromPointer(scratch.Raw + 12), ref state);
                else DOS.SetIoErr((DOS.Error)error);
                requesters.Restore();
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
        NativeMorphOSCopyParser.Release(ref arguments, ref parser);
        Exec.FreeMem(slots, 96);
        return NativeCommandStartup.Finish(result, error, APTR.Null);
    }
}
