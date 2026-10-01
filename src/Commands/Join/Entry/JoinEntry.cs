using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Entries;

/// <summary>
/// <c>C:Join</c> (Workbench 3.1 body). Startup sequence copied verbatim from the
/// qualified <c>Workbench31JoinEntry</c> in tests/Commands.AddBuffersNativeRoot/.
/// Keep the two in step.
/// </summary>
public static class JoinEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
        {
            var process = Exec.FindTask(CString.FromPointer(0));
            if (process.IsNotNull)
                APTR.WriteUInt32(process, DosLayout.Process.Result2,
                    (uint)DOS.Error.InvalidResidentLibrary);
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
                (int)DOS.Error.InvalidResidentLibrary, workbench);
        }
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, workbench);
        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);
        var result = NativeWorkbench31JoinCommand.Run();
        var error = (int)DOS.IoErr();
        return NativeCommandStartup.Finish(result, error, APTR.Null);
    }
}
