using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Entries;

/// <summary>
/// <c>C:Execute</c> (Workbench 3.1 body). Same startup sequence as the other
/// Workbench 3.1 entries (see MakeDirEntry); Execute additionally needs the raw
/// command tail, because the script's arguments are not part of FILE/A.
/// There is no separate test-root entry: the native runner suite
/// <c>wb31-execute-native-entry-vector-fixture</c> (tests/Commands.NativeExecution/
/// ExecuteEntrySuite.cs) runs this shipping entry directly.
/// </summary>
public static class ExecuteEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
        {
            var process = Exec.FindTask(CString.FromPointer(0));
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

        var result = Workbench31ExecuteCommand.Run(argumentLength,
            APTR.FromPointer(CONST_STRPTR.ToUInt32(argumentText)), out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
