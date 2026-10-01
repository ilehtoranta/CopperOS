using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.TypeNativeRoot;

/// <summary>Private DOS-vector root for the bounded Type text stream stage.</summary>
public static class TypeTextIoProbe
{
    public const int ControlBytes = 48;
    public const uint InputHandle = 0x123;
    public const uint OutputHandle = 0x456;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || argumentLength != ControlBytes ||
            argumentText.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.BadTemplate, workbench);

        var control = argumentText.Address;
        var result = NativeMorphOSTypeTextIo.Copy(
            BPTR.FromRaw(InputHandle), BPTR.FromRaw(OutputHandle),
            APTR.ReadUInt32(control, 0) != 0,
            APTR.ReadUInt32(control, 4) != 0,
            APTR.FromPointer(APTR.ReadUInt32(control, 8)),
            APTR.ReadUInt32(control, 12),
            APTR.FromPointer(APTR.ReadUInt32(control, 16)),
            APTR.ReadUInt32(control, 20), out var ioError);
        APTR.WriteUInt32(control, 24, unchecked((uint)result));
        APTR.WriteUInt32(control, 28, unchecked((uint)ioError));
        APTR.WriteUInt32(control, 44, 0x5454494f); // TTIO
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
