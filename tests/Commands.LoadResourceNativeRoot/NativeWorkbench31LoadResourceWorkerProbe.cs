using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.LoadResourceNativeRoot;

/// <summary>Supplied worker leases and process-owned request/registry; no startup emulation.</summary>
public static class NativeWorkbench31LoadResourceWorkerProbe
{
    [M68kEntryPoint]
    public static int Main(int receive, CONST_STRPTR context)
    {
        var control = context.Address;
        DOS.DOSLibraryBase = APTR.FromPointer(APTR.ReadUInt32(control, 12));
        Utility.UtilityLibraryBase = APTR.FromPointer(0x8000);
        Graphics.GraphicsLibraryBase = APTR.FromPointer(0x9000);
        Locale.LocaleLibraryBase = APTR.FromPointer(APTR.ReadUInt32(control, 16));
        var message = APTR.FromPointer(APTR.ReadUInt32(control, 0));
        var registry = APTR.FromPointer(APTR.ReadUInt32(control, 4));
        var catalogState = APTR.FromPointer(APTR.ReadUInt32(control, 8));
        if (receive != 0)
            NativeWorkbench31LoadResourceWorker.ReceiveAndDispatch(
                APTR.FromPointer(APTR.ReadUInt32(control, 20)), registry, catalogState);
        else
            NativeWorkbench31LoadResourceWorker.Dispatch(message, registry, catalogState);
        // ReplyMsg transfers the request back to the sender. Do not inspect it here.
        return 0;
    }
}
