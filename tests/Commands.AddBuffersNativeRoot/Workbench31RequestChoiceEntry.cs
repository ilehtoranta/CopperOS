using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Resident Workbench 3.1 RequestChoice syntax candidate.</summary>
public static class Workbench31RequestChoiceEntry
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

        var intuition = Exec.OpenLibraryRaw(Intuition.Name, 33);
        if (intuition.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
        Intuition.IntuitionLibraryBase = intuition;
        var result = NativeWorkbench31RequestChoiceCommand.Run(out var error);
        Exec.CloseLibrary(intuition);
        Intuition.IntuitionLibraryBase = APTR.Null;
        return NativeCommandStartup.Finish(result, error, APTR.Null);
    }
}
