using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Resident MorphOS RequestFile command entry.</summary>
public static class NativeMorphOSRequestFileEntry
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

        // ASL's requester vectors are present from V36; the captured V37
        // request is retained as differential evidence.
        var asl = Exec.OpenLibraryRaw(ASL.Name, 36);
        if (asl.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
        ASL.ASLLibraryBase = asl;
        var result = NativeMorphOSRequestFileCommand.Run(out var error);
        Exec.CloseLibrary(asl);
        ASL.ASLLibraryBase = APTR.Null;
        return NativeCommandStartup.Finish(result, error, APTR.Null);
    }
}
