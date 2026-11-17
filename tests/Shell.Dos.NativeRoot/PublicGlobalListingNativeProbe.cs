using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Checks classic global listing using public command lookup and RunCommand.</summary>
public static class PublicGlobalListingNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-global-listing-probe")]
    public static int Main()
    {
        var ok = Run();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        #if COPPEROS_PROBE_SYS
        var marker = ok ? Text("SYS:T/runner-ok") : Text("SYS:T/runner-fail");
        #else
        var marker = ok ? Text("DH0:runner-ok") : Text("DH0:runner-fail");
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, marker, Length(marker), 0);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static bool Run()
    {
        var shell = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var buffer = shell.AllocateRunnerMemory(512);
        if (dos.IsNull || buffer.IsNull || cli.IsNull)
        {
            if (buffer.IsNotNull) shell.FreeRunnerMemory(buffer, 512);
            if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var oldError = PublicShellFileServices.GetError(dos);
        var oldPath = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref shell, cli);
        // Borrow the live list intact while each test searches only its current directory.
        DosCommandLineInterfaceCodec.WriteCommandDirectory(ref shell, cli, BPTR.Null);
        #if COPPEROS_PROBE_SYS
        var okDir = Text("SYS:T/list-ok"); var warnDir = Text("SYS:T/list-warn");
        var failDir = Text("SYS:T/list-fail"); var missingDir = Text("SYS:T/list-missing");
        #else
        var okDir = Text("DH0:list-ok"); var warnDir = Text("DH0:list-warn");
        var failDir = Text("DH0:list-fail"); var missingDir = Text("DH0:list-missing");
        #endif
        var ok = TestCase(ref shell, dos, buffer, okDir, 0, Text("MOCK LIST\n"), true);
        ok = TestCase(ref shell, dos, buffer, warnDir, 0, Text("MOCK LIST\n"), true) && ok;
        ok = TestCase(ref shell, dos, buffer, failDir, 0,
            Text("MOCK LIST\nLIST failed returncode 10\n"), true) && ok;
        ok = TestCase(ref shell, dos, buffer, missingDir, 0,
            Text("LIST: Unknown command\nLIST failed returncode 10\n"), false) && ok;
        ok = TestCommands(ref shell, dos, buffer, task, cli, okDir) && ok;
        DosCommandLineInterfaceCodec.WriteCommandDirectory(ref shell, cli, oldPath);
        shell.FreeRunnerMemory(buffer, 512);
        PublicShellFileServices.SetError(dos, (DOS.Error)oldError);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static bool TestCase(ref DosShellNativePlatform shell, APTR dos, APTR buffer,
        APTR directory, int expectedResult, APTR expectedOutput, bool expectArguments)
    {
        #if COPPEROS_PROBE_SYS
        var outputPath = Text("SYS:T/listing-output"); var argumentPath = Text("SYS:T/listargs");
        #else
        var outputPath = Text("DH0:listing-output"); var argumentPath = Text("DH0:listargs");
        #endif
        _ = PublicShellFileServices.Delete(dos, outputPath);
        _ = PublicShellFileServices.Delete(dos, argumentPath);
        var lockHandle = PublicShellFileServices.Lock(dos, directory);
        if (lockHandle.IsNull) return false;
        var previous = PublicShellPathVectors.CurrentDir(dos, lockHandle);
        var output = PublicShellFileServices.OpenOutput(dos, outputPath, Length(outputPath), 0);
        var ok = output.IsNotNull;
        if (ok)
        {
            var invoked = shell.TryWriteGlobalVariables(PublicShellFileServices.Input(dos),
                output, output, out var result);
            var error = PublicShellFileServices.GetError(dos);
            ok = invoked && result == expectedResult && error == 0;
            ok = PublicShellFileServices.Close(dos, output) != 0 && ok;
            ok = FileEquals(ref shell, dos, buffer, outputPath, expectedOutput) && ok;
            if (expectArguments)
                ok = FileEquals(ref shell, dos, buffer, argumentPath, Text("env: QUICK NOHEAD FILES\n")) && ok;
            else
            {
                var absent = PublicShellFileServices.OpenInput(dos, argumentPath, Length(argumentPath));
                if (absent.IsNotNull) { _ = PublicShellFileServices.Close(dos, absent); ok = false; }
            }
        }
        var released = PublicShellPathVectors.CurrentDir(dos, previous);
        if (released.IsNotNull) PublicShellPathVectors.UnLock(dos, released);
        _ = PublicShellFileServices.Delete(dos, outputPath);
        _ = PublicShellFileServices.Delete(dos, argumentPath);
        return ok;
    }

    private static bool FileEquals(ref DosShellNativePlatform shell, APTR dos, APTR buffer,
        APTR path, APTR expected)
    {
        var file = PublicShellFileServices.OpenInput(dos, path, Length(path));
        if (file.IsNull) return false;
        var length = Length(expected);
        var received = PublicShellFileServices.Read(dos, file, buffer, 512);
        var ok = received == (int)length;
        for (var i = 0u; i < length && ok; i++)
            if (shell.ReadUInt8(buffer, (int)i) != shell.ReadUInt8(expected, (int)i)) ok = false;
        return PublicShellFileServices.Close(dos, file) != 0 && ok;
    }

    private static bool TestCommands(ref DosShellNativePlatform shell, APTR dos, APTR buffer,
        APTR task, APTR cli, APTR directory)
    {
        // An interactive proof invokes this probe inside its live runner. Do
        // not replace that runner; the standalone ROM probe covers dispatch.
        if (!ShellRunnerBinding.TryFind(ref shell, cli, out var binding, out _, out _)) return false;
        if (binding.IsNotNull) return true;
        #if COPPEROS_PROBE_SYS
        var first = Text("SYS:T/listing-output"); var second = Text("SYS:T/listing-second");
        var argumentPath = Text("SYS:T/listargs");
        var commands = Text("SetEnv >SYS:T/listing-output\nUnSetEnv >SYS:T/listing-second\n");
        #else
        var first = Text("DH0:listing-output"); var second = Text("DH0:listing-second");
        var argumentPath = Text("DH0:listargs");
        var commands = Text("SetEnv >DH0:listing-output\nUnSetEnv >DH0:listing-second\n");
        #endif
        _ = PublicShellFileServices.Delete(dos, first);
        _ = PublicShellFileServices.Delete(dos, second);
        _ = PublicShellFileServices.Delete(dos, argumentPath);
        var lockHandle = PublicShellFileServices.Lock(dos, directory);
        if (lockHandle.IsNull) return false;
        var previousDirectory = PublicShellPathVectors.CurrentDir(dos, lockHandle);
        var before = DosCommandLineInterfaceCodec.Read(ref shell, cli);
        var previousPrivate = DosProcessCodec.ReadShellPrivate(ref shell, task);
        var ok = ShellProcessState.TryInstall(ref shell, cli, out var state);
        if (ok)
        {
            var status = shell.TryExecuteCommand(cli, commands, Length(commands), out var result);
            var after = DosCommandLineInterfaceCodec.Read(ref shell, cli);
            ok = status == ShellScriptExecutionStatus.Completed && result == 0 &&
                after.ReturnCode == 0 && after.Result2 == 0;
            ok = ShellProcessState.TryRemove(ref shell, cli, state) && ok;
            ok = DosProcessCodec.ReadShellPrivate(ref shell, task) == previousPrivate && ok;
        }
        DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, before.CurrentInput);
        DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, before.Interactive);
        DosCommandLineInterfaceCodec.WriteFailLevel(ref shell, cli, before.FailLevel);
        DosCommandLineInterfaceCodec.WriteReturnCode(ref shell, cli, before.ReturnCode);
        DosCommandLineInterfaceCodec.WriteResult2(ref shell, cli, before.Result2);
        ok = FileEquals(ref shell, dos, buffer, first, Text("MOCK LIST\n")) && ok;
        ok = FileEquals(ref shell, dos, buffer, second, Text("MOCK LIST\n")) && ok;
        ok = FileEquals(ref shell, dos, buffer, argumentPath, Text("env: QUICK NOHEAD FILES\n")) && ok;
        var released = PublicShellPathVectors.CurrentDir(dos, previousDirectory);
        if (released.IsNotNull) PublicShellPathVectors.UnLock(dos, released);
        _ = PublicShellFileServices.Delete(dos, first);
        _ = PublicShellFileServices.Delete(dos, second);
        _ = PublicShellFileServices.Delete(dos, argumentPath);
        return ok;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
    private static uint Length(APTR text)
    {
        var length = 0u;
        while (APTR.ReadUInt8(text, (int)length) != 0) length++;
        return length;
    }
}
