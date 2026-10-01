using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>
/// Private native entry for the bounded MorphOS Assign mutation candidate.
/// Packed-binary correspondence and the full listing/handler contract remain
/// open; this entry exists to qualify the resident native boundary.
/// </summary>
public static class NativeMorphOSAssignEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
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

        var result = NativeMorphOSAssignCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
