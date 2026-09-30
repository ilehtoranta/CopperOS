using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Normal command ABI candidate; not yet qualified for packaging.</summary>
public static class NativeMorphOSCopyCommandEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR commandLine)
    {
        var process = Exec.FindTask(CString.FromPointer(0));
        if (APTR.ReadUInt32(process, DosLayout.Process.CommandLineInterface) == 0)
        {
            var port = APTR.FromPointer(process.Raw + (uint)DosLayout.Process.MessagePort);
            Exec.WaitPort(port);
            Exec.Forbid();
            Exec.ReplyMsg(Exec.GetMsg(port));
            return DOS.RETURN_FAIL;
        }
        NativeCommandStartup.OpenDos(37);
        var result = NativeMorphOSCopyCommand.Run();
        // Copy does not restore an earlier IoErr across its final frees.
        if (DOS.DOSLibraryBase.IsNotNull)
        {
            Exec.CloseLibrary(DOS.DOSLibraryBase);
            DOS.DOSLibraryBase = APTR.Null;
        }
        return result;
    }
}
