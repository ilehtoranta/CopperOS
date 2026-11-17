using Amiga;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Writes through a transferred error stream after the launcher exits.</summary>
public static class PublicContinuationDelayedCommand
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.continuation-delayed-command")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        ChildProbeDosVectors.Delay(dos, 250);
        var memory = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var parent = global::Amiga.Exec.FindTask((CString)"CopperOS Continuation Exit Parent");
        var output = task.IsNull ? BPTR.Null : DosProcessCodec.ReadCurrentError(ref memory, task);
        var text = parent.IsNull ? (CString)"PASS\n" : (CString)"EARLY\n";
        var address = APTR.FromPointer(CString.ToUInt32(text));
        var length = parent.IsNull ? 5u : 6u;
        var written = output.IsNotNull ? PublicShellFileServices.Write(dos, output, address, length) : -1;
        var error = PublicShellFileServices.GetError(dos);
        var ok = written == (int)length;
        var diagnostic = APTR.FromPointer(global::Amiga.Exec.AllocMem(16,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        if (diagnostic.IsNotNull)
        {
            APTR.WriteUInt32(diagnostic, 0, parent.Raw);
            APTR.WriteUInt32(diagnostic, 4, output.Raw);
            APTR.WriteUInt32(diagnostic, 8, unchecked((uint)written));
            APTR.WriteUInt32(diagnostic, 12, unchecked((uint)error));
            var path = APTR.FromPointer(CString.ToUInt32((CString)"SYS:T/continuation-diag"));
            var file = PublicShellFileServices.OpenOutput(dos, path, 23, 0);
            if (file.IsNotNull)
            {
                _ = PublicShellFileServices.Write(dos, file, diagnostic, 16);
                _ = PublicShellFileServices.Close(dos, file);
            }
            global::Amiga.Exec.FreeMem(diagnostic.Raw, 16);
        }
        global::Amiga.Exec.CloseLibrary(dos);
        return ok && parent.IsNull ? 0 : 10;
    }
}
