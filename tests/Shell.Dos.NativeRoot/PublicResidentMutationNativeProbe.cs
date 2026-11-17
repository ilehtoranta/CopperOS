using Amiga;
using CopperOS.Shell.Dos;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Exercises public Resident mutations against the live DOS list.</summary>
public static class PublicResidentMutationNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-resident-mutation-probe")]
    public static int Main()
    {
        var ok = Run();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        #if COPPEROS_PROBE_SYS
        var marker = ok ? Text("SYS:T/resident-ok") : Text("SYS:T/resident-fail");
        #else
        var marker = ok ? Text("DH0:resident-ok") : Text("DH0:resident-fail");
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, marker, Length(marker), 0);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-resident-command-probe")]
    public static int EngineMain()
    {
        var ok = RunCommands(out var failures);
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var marker = ok ? Text("DH0:resident-engine-ok") : Text("DH0:resident-engine-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, Length(marker), 0);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        var details = Text("DH0:resident-engine-details");
        file = PublicShellFileServices.OpenOutput(dos, details, Length(details), 0);
        var buffer = APTR.FromPointer(global::Amiga.Exec.AllocMem(4, global::Amiga.Exec.MemoryFlags.Public));
        if (file.IsNotNull && buffer.IsNotNull)
        {
            APTR.WriteUInt32(buffer, 0, failures);
            _ = PublicShellFileServices.Write(dos, file, buffer, 4);
        }
        if (buffer.IsNotNull) global::Amiga.Exec.FreeMem(buffer.Raw, 4);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static bool RunCommands(out uint failures)
    {
        failures = 0;
        var shell = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var before = DosCommandLineInterfaceCodec.Read(ref shell, cli);
        var oldPrivate = DosProcessCodec.ReadShellPrivate(ref shell, task);
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) { failures = 1; return false; }
        var name = Text("COPPEROS_RESIDENT_ENGINE");
        // Keep an expected failure from terminating the command source early.
        DosCommandLineInterfaceCodec.WriteFailLevel(ref shell, cli, 21);
        var ok = ShellProcessState.TryInstall(ref shell, cli, out var state);
        if (!ok) failures |= 2;
        if (ok)
        {
            var add = Text("Resident COPPEROS_RESIDENT_ENGINE DH0:fixture ADD PURE");
            var remove = Text("Resident COPPEROS_RESIDENT_ENGINE REMOVE");
            var replace = Text("Resident COPPEROS_RESIDENT_ENGINE DH0:fixture REPLACE PURE");
            if (!Execute(ref shell, cli, add, 0)) failures |= 4;
            var first = Find(dos, name, APTR.Null);
            if (first.IsNull) failures |= 8;
            if (!Execute(ref shell, cli, add, 0)) failures |= 16;
            var second = Find(dos, name, APTR.Null);
            if (second.IsNull || second == first || Find(dos, name, second) != first) failures |= 32;
            if (!Execute(ref shell, cli, replace, 0)) failures |= 64;
            var replacement = Find(dos, name, APTR.Null);
            if (replacement.IsNull || Find(dos, name, replacement) != first) failures |= 128;
            if (!Execute(ref shell, cli, remove, 0)) failures |= 256;
            if (!Execute(ref shell, cli, remove, 0)) failures |= 512;
            if (!Execute(ref shell, cli, replace, 5)) failures |= 1024;
            if (DosCommandLineInterfaceCodec.Read(ref shell, cli).Result2 != (int)DOS.Error.ObjectNotFound) failures |= 2048;
            if (!ShellProcessState.TryRemove(ref shell, cli, state)) failures |= 4096;
        }
        DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, before.CurrentInput);
        DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, before.Interactive);
        DosCommandLineInterfaceCodec.WriteFailLevel(ref shell, cli, before.FailLevel);
        DosCommandLineInterfaceCodec.WriteReturnCode(ref shell, cli, before.ReturnCode);
        DosCommandLineInterfaceCodec.WriteResult2(ref shell, cli, before.Result2);
        if (DosProcessCodec.ReadShellPrivate(ref shell, task) != oldPrivate) failures |= 8192;
        global::Amiga.Exec.CloseLibrary(dos);
        return failures == 0;
    }

    private static bool Execute(ref DosShellNativePlatform shell, APTR cli, APTR command, int expected)
    {
        var status = shell.TryExecuteCommand(cli, command, Length(command), out var result);
        return status == ShellScriptExecutionStatus.Completed && result == expected;
    }

    private static bool Run()
    {
        var memory = new CopperSharpNativeDosPlatform();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        #if COPPEROS_PROBE_SYS
        var path = Text("SYS:C/fixture"); var outputPath = Text("SYS:T/resident-output");
        #else
        var path = Text("DH0:fixture"); var outputPath = Text("DH0:resident-output");
        #endif
        var output = PublicShellFileServices.OpenOutput(dos, outputPath, Length(outputPath), 0);
        var name = Text("COPPEROS_RESIDENT_PROBE");
        var request = new ShellResidentManagementRequest
        {
            Name = name, NameLength = Length(name), File = path, FileLength = Length(path),
            Output = output, Add = 1,
        };
        var ok = output.IsNotNull;
        // The test fixture has no PURE protection bit.
        ok = !PublicShellResidentMutation.Manage(ref memory, in request) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectWrongType && ok;
        request.Force = 1;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        var first = Find(dos, name, APTR.Null);
        ok = first.IsNotNull && ok;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        var second = Find(dos, name, APTR.Null);
        var older = Find(dos, name, second);
        ok = first.IsNotNull && second.IsNotNull && second != first && older == first && ok;
        if (second.IsNotNull)
        {
            global::Amiga.Exec.Forbid();
            DosSegmentCodec.WriteUseCount(ref memory, second, 2);
            global::Amiga.Exec.Permit();
            var removeBusy = request;
            removeBusy.Remove = 1; removeBusy.Add = 0;
            ok = !PublicShellResidentMutation.Manage(ref memory, in removeBusy) &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectInUse && ok;
            global::Amiga.Exec.Forbid();
            DosSegmentCodec.WriteUseCount(ref memory, second, 1);
            global::Amiga.Exec.Permit();
        }
        request.Add = 0; request.Replace = 1; request.RequireExisting = 1;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        var replacement = Find(dos, name, APTR.Null);
        older = Find(dos, name, replacement);
        ok = replacement.IsNotNull && older == first && ok;
        request.Replace = 0; request.Remove = 1; request.RequireExisting = 0;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        ok = Find(dos, name, APTR.Null) == first && ok;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        ok = !PublicShellResidentMutation.Manage(ref memory, in request) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectNotFound && ok;
        request.Remove = 0; request.Replace = 1; request.RequireExisting = 1;
        ok = !PublicShellResidentMutation.Manage(ref memory, in request) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectNotFound && ok;
        request.RequireExisting = 0;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        request.Replace = 0; request.Remove = 1;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        // SYSTEM entries are permanent; use a unique name on this disposable
        // guest and leave their reclamation to the test machine's shutdown.
        var systemName = Text("COPPEROS_SYSTEM_RESIDENT_PROBE");
        request.Name = systemName; request.NameLength = Length(systemName);
        request.Remove = 0; request.Add = 1; request.System = 1;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        var permanent = Find(dos, systemName, APTR.Null, 1);
        ok = permanent.IsNotNull && DosSegmentCodec.ReadUseCount(ref memory, permanent) == -1 && ok;
        request.Add = 0; request.Remove = 1; request.System = 0;
        ok = !PublicShellResidentMutation.Manage(ref memory, in request) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectInUse && ok;
        request.Remove = 0; request.Replace = 1; request.RequireExisting = 1;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        var normal = Find(dos, systemName, APTR.Null);
        ok = normal.IsNotNull && Find(dos, systemName, APTR.Null, 1) == permanent && ok;
        request.Replace = 0; request.Remove = 1; request.RequireExisting = 0;
        ok = PublicShellResidentMutation.Manage(ref memory, in request) && ok;
        ok = Find(dos, systemName, APTR.Null).IsNull &&
            Find(dos, systemName, APTR.Null, 1) == permanent && ok;
        if (output.IsNotNull) ok = PublicShellFileServices.Close(dos, output) != 0 && ok;
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static APTR Find(APTR dos, APTR name, APTR start, int system = 0)
    {
        global::Amiga.Exec.Forbid();
        var result = PublicShellResidentVectors.FindSegment(dos, name, start, system);
        global::Amiga.Exec.Permit();
        return result;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
    private static uint Length(APTR value)
    {
        var length = 0u;
        while (APTR.ReadUInt8(value, (int)length) != 0) length++;
        return length;
    }
}
