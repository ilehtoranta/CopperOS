using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Checks process-owned Shell state across nested frame bindings.</summary>
public static class PublicProcessStateNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-process-state-probe")]
    public static int Main()
    {
        var ok = Run();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
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

    private static bool Run()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var old = DosProcessCodec.ReadShellPrivate(ref platform, task);
        var oldCommandFile = DosCommandLineInterfaceCodec.Read(ref platform, cli).CommandFile;
        var outer = platform.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        var inner = platform.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        if (outer.IsNull || inner.IsNull)
        {
            if (outer.IsNotNull) platform.FreeRunnerMemory(outer, ShellScriptFrameCodec.Size);
            if (inner.IsNotNull) platform.FreeRunnerMemory(inner, ShellScriptFrameCodec.Size);
            return false;
        }
        var initial = new ShellScriptFrameState { Cli = cli, Parent = cli,
            Flags = ShellScriptFrameFlags.Active, FailureLimit = 10 };
        var processState = APTR.Null;
        var ok = ShellScriptFrameCodec.Initialize(ref platform, outer, in initial) &&
            ShellScriptFrameCodec.Initialize(ref platform, inner, in initial) &&
            ShellProcessState.TryFind(ref platform, cli, out processState);
        var installed = false;
        if (ok && processState.IsNull)
        {
            installed = ShellProcessState.TryInstall(ref platform, cli, out processState);
            ok = installed;
        }
        if (ok)
        {
            if (installed) ok = DosCommandLineInterfaceCodec.Read(ref platform, cli).CommandFile != oldCommandFile && ok;
            ok = platform.TryGetInternalCommandEnabled(cli, (uint)ShellInternalCommand.Echo, out var enabled);
            var previousEnabled = enabled;
            ok = platform.TrySetInternalCommandEnabled(cli, (uint)ShellInternalCommand.Echo, 0) && ok;
            ok = platform.TryBindScriptFrame(cli, outer) && ok;
            ok = platform.TryBindScriptFrame(cli, inner) && ok;
            ok = platform.TryGetInternalCommandEnabled(cli, (uint)ShellInternalCommand.Echo, out enabled) && enabled == 0 && ok;
            ok = !ShellProcessState.TryRemove(ref platform, cli, processState) && ok;
            ok = platform.TryUnbindScriptFrame(cli, inner) && ok;
            ok = platform.TryUnbindScriptFrame(cli, outer) && ok;
            ok = platform.TryGetInternalCommandEnabled(cli, (uint)ShellInternalCommand.Echo, out enabled) && enabled == 0 && ok;
            ok = platform.TrySetInternalCommandEnabled(cli, (uint)ShellInternalCommand.Echo, previousEnabled) && ok;
            ok = platform.TryGetInternalCommandEnabled(cli, (uint)ShellInternalCommand.Echo, out enabled) && enabled == previousEnabled && ok;
            if (installed) ok = ShellProcessState.TryRemove(ref platform, cli, processState) && ok;
            else ok = !ShellProcessState.TryRemove(ref platform, cli, processState) && ok;
            ok = DosProcessCodec.ReadShellPrivate(ref platform, task) == old && ok;
            ok = DosCommandLineInterfaceCodec.Read(ref platform, cli).CommandFile == oldCommandFile && ok;
        }
        platform.FreeRunnerMemory(outer, ShellScriptFrameCodec.Size);
        platform.FreeRunnerMemory(inner, ShellScriptFrameCodec.Size);
        return ok;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
