using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Entries;

/// <summary>
/// <c>C:MakeDir</c> (Workbench 3.1 template <c>NAME/M</c>). CLI-only, like the
/// qualified <c>Workbench31MakeDirProbe</c> in tests/Commands.NativeRoot;
/// Workbench launch is not handled or qualified.
/// </summary>
public static class MakeDirEntry
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
