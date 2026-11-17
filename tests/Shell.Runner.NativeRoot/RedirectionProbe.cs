using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;

namespace CopperOS.Shell.Runner.NativeRoot;

/// <summary>Checks native redirection parsing before any DOS file is opened.</summary>
public static class RedirectionProbe
{
    [M68kEntryPoint]
    public static int Main()
    {
        var guest = new Guest();
        var memory = guest.AllocateRunnerMemory(512);
        var ok = false;
        if (memory.IsNotNull)
        {
            var workspace = new ShellRedirectionWorkspace(memory, 128,
                APTR.FromPointer(memory.Raw + 128), 128,
                APTR.FromPointer(memory.Raw + 256), 128,
                APTR.FromPointer(memory.Raw + 384), 128);
            ok = ShellRedirectionParser.Parse(ref guest, Text("Echo second >>SYS:out"),
                21, in workspace, out var spec, out var length) &&
                spec.HasOutput && spec.OutputAppend == 1 && spec.OutputLength == 7 &&
                length != 0 && APTR.ReadUInt32(spec.OutputPath, 0) == 0x5359533a;
            guest.FreeRunnerMemory(memory, 512);
        }
        var dos = Vectors.OpenLibrary(APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0)),
            Text("dos.library"), 37);
        if (dos.IsNull) return 20;
        var file = Vectors.Open(dos, ok ? Text("DH0:runner-ok") : Text("DH0:runner-fail"), 1006);
        if (file.IsNotNull) _ = Vectors.Close(dos, file);
        ExecVectors.CloseLibrary(APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0)), dos);
        return ok ? 0 : 10;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
