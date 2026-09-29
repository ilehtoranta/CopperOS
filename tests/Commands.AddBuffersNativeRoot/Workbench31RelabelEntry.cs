using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private resident entry for the bounded Workbench 3.1 Relabel candidate.</summary>
public static class Workbench31RelabelEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
        {
            if (workbench.IsNull)
            {
                var execBase = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
                var process = APTR.FromPointer(APTR.ReadUInt32(execBase, ExecLayout.ExecBase.ThisTask));
                APTR.WriteUInt32(process, DosLayout.Process.Result2,
                    (uint)DOS.Error.InvalidResidentLibrary);
            }
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        }
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, workbench);
        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);

        var result = NativeWorkbench31RelabelCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
