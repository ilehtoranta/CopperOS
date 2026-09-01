using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.NativeRoot;

/// <summary>
/// Private CLI-only MakeDir entry for the independent original/native vector
/// fixture. It is not a shipping entry or evidence of Workbench launch support.
/// Input is supplied by DOS; entry D0/A0 are not a replacement parser protocol.
/// </summary>
public static class Workbench31MakeDirProbe
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (!NativeCommandStartup.OpenDos(36))
        {
            // The original CLI entry writes pr_Result2 without calling DOS
            // when that library cannot be opened. A valid Process is required.
            var execBase = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
            var process = APTR.FromPointer(APTR.ReadUInt32(execBase, ExecLayout.ExecBase.ThisTask));
            APTR.WriteUInt32(process, DosLayout.Process.Result2,
                (uint)DOS.Error.InvalidResidentLibrary);
            return DOS.RETURN_FAIL;
        }

        var result = Workbench31MakeDirCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
