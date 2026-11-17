using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Runs only on an isolated writable DH0: with env and envarc directories.</summary>
public static class PublicGlobalVariableNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-global-variable-probe")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        #if COPPEROS_PROBE_SYS
        var env = PublicShellPathVectors.Lock(dos, Text("SYS:T/env"), -2);
        var archive = PublicShellPathVectors.Lock(dos, Text("SYS:T/envarc"), -2);
        #else
        var env = PublicShellPathVectors.Lock(dos, Text("DH0:env"), -2);
        var archive = PublicShellPathVectors.Lock(dos, Text("DH0:envarc"), -2);
        #endif
        var ok = env.IsNotNull && archive.IsNotNull;
        if (ok) ok = Assign(dos, Text("ENV"), env) != 0;
        if (ok) ok = Assign(dos, Text("ENVARC"), archive) != 0;
        if (ok) ok = Run(dos);
        _ = Assign(dos, Text("ENV"), BPTR.Null);
        _ = Assign(dos, Text("ENVARC"), BPTR.Null);
        #if COPPEROS_PROBE_SYS
        var marker = ok ? Text("SYS:T/runner-ok") : Text("SYS:T/runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, ok ? 15u : 17u, 0);
        #else
        var marker = ok ? Text("DH0:runner-ok") : Text("DH0:runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, ok ? 13u : 15u, 0);
        #endif
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static bool Run(APTR dos)
    {
        var shell = new DosShellNativePlatform(APTR.Null);
        var buffer = shell.AllocateRunnerMemory(32);
        if (buffer.IsNull) return false;
        var name = Text("CopperProbeGlobal");
        var ok = shell.TrySetGlobalVariable(name, 17, Text("A\nB"), 3, 1) &&
            shell.TryGetGlobalVariable(name, 17, buffer, 32, out var length) &&
            length == 3 && shell.ReadUInt8(buffer) == (byte)'A' &&
            shell.ReadUInt8(buffer, 1) == (byte)'\n' && shell.ReadUInt8(buffer, 2) == (byte)'B';
        ok = !shell.TryGetGlobalVariable(name, 17, buffer, 2, out var truncated) &&
            truncated == 3 && PublicShellFileServices.GetError(dos) == (int)DOS.Error.BufferOverflow && ok;
        #if COPPEROS_PROBE_SYS
        var live = Text("SYS:T/env/CopperProbeGlobal");
        var archive = Text("SYS:T/envarc/CopperProbeGlobal");
        #else
        var live = Text("DH0:env/CopperProbeGlobal");
        var archive = Text("DH0:envarc/CopperProbeGlobal");
        #endif
        ok = Exists(dos, live) && Exists(dos, archive) && ok;
        ok = shell.TryRemoveGlobalVariable(name, 17, 1) && ok;
        ok = !Exists(dos, live) && !Exists(dos, archive) && ok;
        ok = shell.TrySetGlobalVariable(name, 17, APTR.Null, 0, 0) &&
            shell.TryGetGlobalVariable(name, 17, buffer, 32, out length) && length == 0 && ok;
        ok = shell.TryRemoveGlobalVariable(name, 17, 0) && ok;
        ok = !shell.TryGetGlobalVariable(name, 17, buffer, 32, out length) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectNotFound && ok;
        shell.FreeRunnerMemory(buffer, 32);
        return ok;
    }

    private static bool Exists(APTR dos, APTR name)
    {
        var value = PublicShellPathVectors.Lock(dos, name, -2);
        if (value.IsNull) return false;
        PublicShellPathVectors.UnLock(dos, value);
        return true;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));

    [AmigaLibrary("dos.library::global-variable-probe", AmigaLibraryBasePolicy.CallerProvided)]
    private static class Vectors
    {
        [AmigaLvo(DosLvo.AssignLock)] [return: M68kRegister(M68kRegister.D0)]
        internal static extern int Assign([M68kRegister(M68kRegister.A6)] APTR dos,
            [M68kRegister(M68kRegister.D1)] APTR name, [M68kRegister(M68kRegister.D2)] BPTR value);
    }

    private static int Assign(APTR dos, APTR name, BPTR value) => Vectors.Assign(dos, name, value);
}
