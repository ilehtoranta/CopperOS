using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Checks the active runner's directory against its Process.</summary>
public static class PublicRunnerDirectoryCommand
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.runner-directory-command")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var shell = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var ok = ShellRunnerBinding.TryFind(ref shell, cli, out var binding, out var frame, out _) &&
            binding.IsNotNull && ShellScriptFrameCodec.TryRead(ref shell, frame, out var state) &&
            state.CurrentDirectory.IsNotNull &&
            state.CurrentDirectory.Raw == shell.ReadUInt32(task, DosLayout.Process.CurrentDirectory);
        var value = ok ? (CString)"PASS\n" : (CString)"FAIL\n";
        var text = APTR.FromPointer(CString.ToUInt32(value));
        ok = PublicShellFileServices.Write(dos, PublicShellFileServices.Output(dos), text, 5) == 5 && ok;
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }
}
