using Amiga;
using CopperOS.Shell;
using CopperOS.Shell.Dos;
using CopperSharp.Compiler;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Checks the production native Shell's parsed append flag.</summary>
public static class RedirectionNativeProbe
{
    [M68kEntryPoint]
    public static int Main()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var memory = APTR.FromPointer(global::Amiga.Exec.AllocMem(512,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        var ok = false;
        if (memory.IsNotNull)
        {
            var workspace = new ShellRedirectionWorkspace(memory, 128,
                APTR.FromPointer(memory.Raw + 128), 128,
                APTR.FromPointer(memory.Raw + 256), 128,
                APTR.FromPointer(memory.Raw + 384), 128);
            ok = ShellRedirectionParser.Parse(ref platform, Text("Echo second >>SYS:out"),
                21, in workspace, out var spec, out var length) &&
                spec.HasOutput && spec.OutputAppend == 1 && spec.OutputLength == 7 &&
                length != 0 && APTR.ReadUInt32(spec.OutputPath, 0) == 0x5359533a;
            global::Amiga.Exec.FreeMem(memory.Raw, 512);
        }
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var name = ok ? Text("DH0:runner-ok") : Text("DH0:runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, name, ok ? 13u : 15u, 0);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
