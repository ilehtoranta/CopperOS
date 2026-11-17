using Amiga;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Appends the preceding command's public CLI result fields.</summary>
public static class CliResultCaptureNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.cli-result-capture-probe")]
    public static int Main()
    {
        var memory = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address;
        if (cli.IsNull) return 20;
        var value = DosCommandLineInterfaceCodec.Read(ref memory, cli);
        var buffer = memory.AllocateGuest(8);
        if (buffer.IsNull) return 20;
        memory.WriteUInt32(buffer, 0, unchecked((uint)value.ReturnCode));
        memory.WriteUInt32(buffer, 4, unchecked((uint)value.Result2));
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) { memory.FreeGuest(buffer, 8); return 20; }
        var name = APTR.FromPointer(CString.ToUInt32((CString)"DH0:captured"));
        var file = PublicShellFileServices.OpenOutput(dos, name, 12, 1);
        var ok = file.IsNotNull && PublicShellFileServices.Write(dos, file, buffer, 8) == 8;
        if (file.IsNotNull) ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        memory.FreeGuest(buffer, 8);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 20;
    }
}
