using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.LoadResourceNativeRoot;

public static class NativeWorkbench31LoadResourceLaunchProbe
{
    [M68kEntryPoint]
    public static int Main(int inactive, CONST_STRPTR contextPointer)
    {
        DOS.DOSLibraryBase = APTR.FromPointer(0x8000);
        var context = contextPointer.Address;
        var arguments = default(NativeCommandArguments);
        if (inactive == 0)
            NativeCommandArguments.TryRead(CString.FromLiteral("NAME/M,LOCK/S,UNLOCK/S"), 3, out arguments);
        var result = NativeWorkbench31LoadResourceLaunch.Send(
            CString.FromPointer(APTR.ReadUInt32(context, 4)),
            BPTR.FromRaw(APTR.ReadUInt32(context, 0)), ref arguments, out var error);
        APTR.WriteUInt32(context, 8, unchecked((uint)error));
        arguments.Release();
        return result;
    }
}
