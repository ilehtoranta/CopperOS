using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.NativeRoot;

/// <summary>
/// Private CC04 I/O fixture, never a shipping command or DOS implementation.
/// The 40-byte big-endian CCIO block has five input and five output LONGs;
/// its layout and vector-fixture matrix are in native-io-contract.md.
/// </summary>
public static class NativeIoProbe
{
    private const uint ProtocolMagic = 0x4343494f;
    private const int ProtocolBytes = 40;
    private const int ProbePoison = 0x5a17;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);

        if (workbench.IsNotNull || argumentLength != ProtocolBytes ||
            argumentText.IsNull || APTR.ReadUInt32(argumentText.Address, 0) != ProtocolMagic)
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate, workbench);

        var block = argumentText.Address;
        var operation = APTR.ReadUInt32(block, 4);
        var file = BPTR.FromRaw(APTR.ReadUInt32(block, 8));
        var buffer = APTR.FromPointer(APTR.ReadUInt32(block, 12));
        var length = unchecked((int)APTR.ReadUInt32(block, 16));
        NativeCommandIoError error = default;
        int result;

        if (operation == 0)
            result = NativeCommandIo.ReadOnce(file, buffer, length, out error);
        else if (operation == 1)
            result = NativeCommandIo.WriteOnce(file, buffer, length, out error);
        else if (operation == 2)
            result = NativeCommandIo.ReadOnce(DOS.Input(), buffer, length, out error);
        else if (operation == 3)
            result = NativeCommandIo.WriteOnce(DOS.Output(), buffer, length, out error);
        else if (operation == 4)
            result = NativeCommandIo.IsCtrlCPending() ? 1 : 0;
        else if (operation == 5)
            result = unchecked((int)DOS.Input().Raw);
        else if (operation == 6)
            result = unchecked((int)DOS.Output().Raw);
        else
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate, workbench);

        // These calls are probe instrumentation, not helper policy. Keep the
        // result and captured record live across a later clobbering DOS call.
        var ambientError = (int)DOS.IoErr();
        var oldError = (int)DOS.SetIoErr((DOS.Error)ProbePoison);
        APTR.WriteUInt32(block, 20, unchecked((uint)result));
        APTR.WriteUInt32(block, 24, error.IsCaptured ? 1u : 0u);
        APTR.WriteUInt32(block, 28, unchecked((uint)error.Value));
        APTR.WriteUInt32(block, 32, unchecked((uint)ambientError));
        APTR.WriteUInt32(block, 36, unchecked((uint)oldError));

        // A transfer failure is still a completed fixture observation. The
        // host checks the output fields; this is not a command success policy.
        return NativeCommandStartup.Finish(DOS.RETURN_OK, ProbePoison, workbench);
    }
}
