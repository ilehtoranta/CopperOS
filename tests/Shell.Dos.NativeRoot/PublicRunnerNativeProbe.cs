using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Runs the shared synchronous Shell engine with no CopperStart DOS state.</summary>
public static class PublicRunnerNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-runner-probe")]
    public static int Main()
    {
        var ok = RunChild();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        #if COPPEROS_PROBE_SYS
        var marker = ok ? Text("SYS:runner-ok") : Text("SYS:runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, ok ? 13u : 15u, 0);
        #else
        var marker = ok ? Text("DH0:runner-ok") : Text("DH0:runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, ok ? 13u : 15u, 0);
        #endif
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static bool RunChild()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var memory = new DosShellNativePlatform(APTR.Null);
        var parent = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var previousPrivate = DosProcessCodec.ReadShellPrivate(ref memory, parent);
        var message = APTR.FromPointer(global::Amiga.Exec.AllocMem(32,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        var tags = APTR.FromPointer(global::Amiga.Exec.AllocMem(40,
            global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
        var reply = global::Amiga.Exec.CreateMsgPort();
        var ok = false;
        if (message.IsNotNull && tags.IsNotNull && reply.IsNotNull)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, 32);
            APTR.WriteUInt32(tags, 0, (uint)DosNewProcessTag.Entry);
            APTR.WriteUInt32(tags, 4, APTR.ExportAddress("copperos.shell.public-runner-worker").Raw);
            APTR.WriteUInt32(tags, 8, (uint)DosNewProcessTag.Cli);
            APTR.WriteUInt32(tags, 12, 1);
            APTR.WriteUInt32(tags, 16, (uint)DosNewProcessTag.StackSize);
            APTR.WriteUInt32(tags, 20, 65536);
            APTR.WriteUInt32(tags, 24, (uint)DosNewProcessTag.Name);
            APTR.WriteUInt32(tags, 28, Text("CopperOS Public Runner Probe").Raw);
            var child = ChildProbeDosVectors.CreateNewProc(dos, tags);
            if (child.IsNotNull)
            {
                global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(child), message);
                _ = global::Amiga.Exec.WaitPort(reply);
                ok = global::Amiga.Exec.GetMsg(reply) == message && APTR.ReadUInt32(message, 20) == 1;
                while (global::Amiga.Exec.FindTask(CString.FromPointer(Text("CopperOS Public Runner Probe").Raw)).IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
            }
        }
        ok = DosProcessCodec.ReadShellPrivate(ref memory, parent) == previousPrivate && ok;
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, 40);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, 32);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    [M68kExport("copperos.shell.public-runner-worker")]
    public static uint Worker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        var ok = Run();
        APTR.WriteUInt32(message, 20, ok ? 1u : 0u);
        global::Amiga.Exec.ReplyMsg(message);
        return ok ? 0u : 10u;
    }

    private static bool Run()
    {
        var shell = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var before = DosCommandLineInterfaceCodec.Read(ref shell, cli);
        var originalPrivate = DosProcessCodec.ReadShellPrivate(ref shell, task);
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var scratch = shell.AllocateRunnerMemory(512);
        if (dos.IsNull || scratch.IsNull)
        {
            if (scratch.IsNotNull) shell.FreeRunnerMemory(scratch, 512);
            if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var originalError = PublicShellFileServices.GetError(dos);
        // A .KEY script needs a temporary directory. Borrow an existing T:
        // assignment, or create and later remove only this fixture's assign.
        var previousWindow = DosProcessCodec.ReadWindowPointer(ref shell, task);
        DosProcessCodec.WriteWindowPointer(ref shell, task, APTR.FromPointer(uint.MaxValue));
        var existingTemporary = PublicShellPathVectors.Lock(dos, Text("T:"), -2);
        DosProcessCodec.WriteWindowPointer(ref shell, task, previousWindow);
        var assignedTemporary = false;
        if (existingTemporary.IsNotNull) PublicShellPathVectors.UnLock(dos, existingTemporary);
        else
        {
            #if COPPEROS_PROBE_SYS
            var temporaryLock = PublicShellPathVectors.Lock(dos, Text("SYS:T"), -2);
            #else
            var temporaryLock = PublicShellPathVectors.Lock(dos, Text("DH0:"), -2);
            #endif
            if (temporaryLock.IsNull || Vectors.Assign(dos, Text("T"), temporaryLock) == 0)
            {
                if (temporaryLock.IsNotNull) PublicShellPathVectors.UnLock(dos, temporaryLock);
                shell.FreeRunnerMemory(scratch, 512);
                global::Amiga.Exec.CloseLibrary(dos);
                return false;
            }
            assignedTemporary = true;
        }
        var commandFile = before.CommandFile.Address;
        var commandFileLength = commandFile.IsNull ? 0 : shell.ReadUInt8(commandFile);
        for (var i = 0; i < commandFileLength; i++)
            shell.WriteUInt8(scratch, i, shell.ReadUInt8(commandFile, i + 1));
        #if COPPEROS_PROBE_SYS
        var script = Text("Alias COSHAREDPROBE Echo []\nCOSHAREDPROBE \"HELLO WORLD\" >SYS:T/shared-output\nUnalias COSHAREDPROBE\n");
        var path = Text("SYS:T/shared-output"); const uint pathLength = 19;
        var fileScript = Text("SYS:T/shared-script"); const uint fileScriptLength = 19;
        var fileOutput = Text("SYS:T/file-output"); const uint fileOutputLength = 17;
        var fileScriptText = Text(".KEY value/A\nIf \"<value>\" EQ \"FILE OK\"\nEcho <value> >SYS:T/file-output\nElse\nEcho BAD >SYS:T/file-output\nEndIf\n");
        var startupOutput = Text("SYS:T/boot-output"); const uint startupOutputLength = 17;
        var startupText = Text("Echo BOOT OK >SYS:T/boot-output\n");
        #else
        var script = Text("Alias COSHAREDPROBE Echo []\nCOSHAREDPROBE \"HELLO WORLD\" >DH0:shared-output\nUnalias COSHAREDPROBE\n");
        var path = Text("DH0:shared-output"); const uint pathLength = 17;
        var fileScript = Text("DH0:shared-script"); const uint fileScriptLength = 17;
        var fileOutput = Text("DH0:file-output"); const uint fileOutputLength = 15;
        var fileScriptText = Text(".KEY value/A\nIf \"<value>\" EQ \"FILE OK\"\nEcho <value> >DH0:file-output\nElse\nEcho BAD >DH0:file-output\nEndIf\n");
        var startupOutput = Text("DH0:boot-output"); const uint startupOutputLength = 15;
        var startupText = Text("Echo BOOT OK >DH0:boot-output\n");
        #endif
        var fileScriptTextLength = 0u;
        while (shell.ReadUInt8(fileScriptText, (int)fileScriptTextLength) != 0) fileScriptTextLength++;
        var file = PublicShellFileServices.OpenOutput(dos, fileScript, fileScriptLength, 0);
        var scriptReady = file.IsNotNull;
        if (scriptReady)
        {
            scriptReady = PublicShellFileServices.Write(dos, file, fileScriptText, fileScriptTextLength) == (int)fileScriptTextLength;
            scriptReady = PublicShellFileServices.Close(dos, file) != 0 && scriptReady;
        }
        var length = 0u;
        while (shell.ReadUInt8(script, (int)length) != 0) length++;
        var processState = APTR.Null;
        var ok = scriptReady && ShellProcessState.TryInstall(ref shell, cli, out processState);
        if (ok)
        {
            var scriptArguments = Text("\"FILE OK\"");
            var fileStatus = shell.TryExecuteScript(cli, fileScript, fileScriptLength, scriptArguments, 9, out var fileResult);
            ok = fileStatus == ShellScriptExecutionStatus.Completed && fileResult == 0;
            var status = shell.TryExecuteCommand(cli, script, length, out var result);
            ok = status == ShellScriptExecutionStatus.Completed && result == 0 && ok;
            ok = ShellProcessState.TryRemove(ref shell, cli, processState) && ok;
            ok = DosProcessCodec.ReadShellPrivate(ref shell, task) == originalPrivate && ok;
        }
        DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, before.CurrentInput);
        DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, before.Interactive);
        DosCommandLineInterfaceCodec.WriteFailLevel(ref shell, cli, before.FailLevel);
        DosCommandLineInterfaceCodec.WriteReturnCode(ref shell, cli, before.ReturnCode);
        DosCommandLineInterfaceCodec.WriteResult2(ref shell, cli, before.Result2);
        if (commandFile.IsNotNull)
        {
            shell.WriteUInt8(commandFile, 0, (byte)commandFileLength);
            for (var i = 0; i < commandFileLength; i++)
                shell.WriteUInt8(commandFile, i + 1, shell.ReadUInt8(scratch, i));
        }
        file = PublicShellFileServices.OpenInput(dos, fileOutput, fileOutputLength);
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.ReadLine(dos, file, scratch, 512, out var received, out var eof) &&
                eof == 0 && received == 7 && ok;
            var expected = Text("FILE OK");
            for (var i = 0; i < 7; i++)
                if (shell.ReadUInt8(scratch, i) != shell.ReadUInt8(expected, i)) ok = false;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        _ = PublicShellFileServices.Delete(dos, fileScript);
        _ = PublicShellFileServices.Delete(dos, fileOutput);
        file = PublicShellFileServices.OpenInput(dos, path, pathLength);
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.ReadLine(dos, file, scratch, 512, out var received, out var eof) &&
                eof == 0 && received == 11 && ok;
            var expected = Text("HELLO WORLD");
            for (var i = 0; i < 11; i++)
                if (shell.ReadUInt8(scratch, i) != shell.ReadUInt8(expected, i)) ok = false;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        _ = PublicShellFileServices.Delete(dos, path);

        // Exercise the same initialized-CLI entry needed by the Kickstart
        // packet startup, with a borrowed input stream and no DOS-state owner.
        var startupLength = 0u;
        while (shell.ReadUInt8(startupText, (int)startupLength) != 0) startupLength++;
        file = PublicShellFileServices.OpenOutput(dos, fileScript, fileScriptLength, 0);
        var startupReady = file.IsNotNull;
        if (startupReady)
        {
            startupReady = PublicShellFileServices.Write(dos, file, startupText, startupLength) == (int)startupLength;
            startupReady = PublicShellFileServices.Close(dos, file) != 0 && startupReady;
        }
        file = startupReady ? PublicShellFileServices.OpenInput(dos, fileScript, fileScriptLength) : BPTR.Null;
        if (file.IsNull) ok = false;
        else
        {
            DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, file);
            DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, 0);
            var startupResult = DosShellNativeChildCore.RunFromInitializedCli();
            ok = startupResult == 0 && DosProcessCodec.ReadShellPrivate(ref shell, task) == originalPrivate &&
                DosCommandLineInterfaceCodec.Read(ref shell, cli).CurrentInput == file &&
                DosCommandLineInterfaceCodec.Read(ref shell, cli).CommandFile == before.CommandFile && ok;
            DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, before.CurrentInput);
            DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, before.Interactive);
            DosCommandLineInterfaceCodec.WriteFailLevel(ref shell, cli, before.FailLevel);
            DosCommandLineInterfaceCodec.WriteReturnCode(ref shell, cli, before.ReturnCode);
            DosCommandLineInterfaceCodec.WriteResult2(ref shell, cli, before.Result2);
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        file = PublicShellFileServices.OpenInput(dos, startupOutput, startupOutputLength);
        if (file.IsNull) ok = false;
        else
        {
            ok = PublicShellFileServices.ReadLine(dos, file, scratch, 512, out var received, out var eof) &&
                eof == 0 && received == 7 && ok;
            var expected = Text("BOOT OK");
            for (var i = 0; i < 7; i++)
                if (shell.ReadUInt8(scratch, i) != shell.ReadUInt8(expected, i)) ok = false;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        _ = PublicShellFileServices.Delete(dos, fileScript);
        _ = PublicShellFileServices.Delete(dos, startupOutput);
        var stages = ok ? 1u : 0u;
        var checkedCase = TestResume(ref shell, dos, task, cli, false);
        if (checkedCase) stages |= 2;
        ok = checkedCase && ok;
        checkedCase = TestResume(ref shell, dos, task, cli, true);
        if (checkedCase) stages |= 4;
        ok = checkedCase && ok;
        checkedCase = TestInputPrefix(ref shell, dos);
        if (checkedCase) stages |= 8;
        ok = checkedCase && ok;
        checkedCase = TestOpaqueProcessState(ref shell, dos, task, cli, scratch);
        if (checkedCase) stages |= 16;
        ok = checkedCase && ok;
        shell.WriteUInt32(scratch, 32, stages);
        #if COPPEROS_PROBE_SYS
        var diagnosticPath = Text("SYS:T/runner-state.bin");
        #else
        var diagnosticPath = Text("DH0:runner-state.bin");
        #endif
        var diagnostic = PublicShellFileServices.OpenOutput(dos, diagnosticPath, Length(diagnosticPath), 0);
        if (diagnostic.IsNotNull)
        {
            _ = PublicShellFileServices.Write(dos, diagnostic, scratch, 36);
            _ = PublicShellFileServices.Close(dos, diagnostic);
        }
        if (assignedTemporary) _ = Vectors.Assign(dos, Text("T"), BPTR.Null);
        shell.FreeRunnerMemory(scratch, 512);
        PublicShellFileServices.SetError(dos, (DOS.Error)originalError);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));

    private static bool TestResume(ref DosShellNativePlatform shell, APTR dos, APTR task,
        APTR cli, bool endFirst)
    {
        #if COPPEROS_PROBE_SYS
        var firstPath = Text("SYS:T/start-in"); var secondPath = Text("SYS:T/standard-in");
        var firstOutput = Text("SYS:T/start-out"); var secondOutput = Text("SYS:T/standard-out");
        var firstText = endFirst ? Text("EndCLI\n") : Text("Echo ALPHA >SYS:T/start-out\n");
        var secondText = Text("Echo OMEGA >SYS:T/standard-out\n");
        #else
        var firstPath = Text("DH0:start-in"); var secondPath = Text("DH0:standard-in");
        var firstOutput = Text("DH0:start-out"); var secondOutput = Text("DH0:standard-out");
        var firstText = endFirst ? Text("EndCLI\n") : Text("Echo ALPHA >DH0:start-out\n");
        var secondText = Text("Echo OMEGA >DH0:standard-out\n");
        #endif
        _ = PublicShellFileServices.Delete(dos, firstOutput);
        _ = PublicShellFileServices.Delete(dos, secondOutput);
        var ready = WriteFile(dos, firstPath, firstText) && WriteFile(dos, secondPath, secondText);
        var first = ready ? PublicShellFileServices.OpenInput(dos, firstPath, Length(firstPath)) : BPTR.Null;
        var second = first.IsNotNull ? PublicShellFileServices.OpenInput(dos, secondPath, Length(secondPath)) : BPTR.Null;
        var ok = first.IsNotNull && second.IsNotNull;
        if (ok)
        {
            var previous = DosCommandLineInterfaceCodec.Read(ref shell, cli);
            var previousPrivate = DosProcessCodec.ReadShellPrivate(ref shell, task);
            var selected = NativeForegroundDosVectors.SelectInput(dos, first);
            DosCommandLineInterfaceCodec.WriteStandardInput(ref shell, cli, second);
            DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, first);
            DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, 0);
            var result = DosShellNativeChildCore.RunFromInitializedCli(true);
            var after = DosCommandLineInterfaceCodec.Read(ref shell, cli);
            ok = result == 0 && after.CurrentInput == (endFirst ? first : second) &&
                after.CommandFile == previous.CommandFile &&
                DosProcessCodec.ReadShellPrivate(ref shell, task) == previousPrivate;
            _ = NativeForegroundDosVectors.SelectInput(dos, selected);
            DosCommandLineInterfaceCodec.WriteStandardInput(ref shell, cli, previous.StandardInput);
            DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, previous.CurrentInput);
            DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, previous.Interactive);
            DosCommandLineInterfaceCodec.WriteFailLevel(ref shell, cli, previous.FailLevel);
            DosCommandLineInterfaceCodec.WriteReturnCode(ref shell, cli, previous.ReturnCode);
            DosCommandLineInterfaceCodec.WriteResult2(ref shell, cli, previous.Result2);
            // The startup reader is closed by the handoff on EOF, but remains
            // owned by the caller if EndCLI or a failure prevents the handoff.
            if (after.CurrentInput == first) ok = PublicShellFileServices.Close(dos, first) != 0 && ok;
            ok = PublicShellFileServices.Close(dos, second) != 0 && ok;
            ok = HasFile(dos, firstOutput) == !endFirst && HasFile(dos, secondOutput) == !endFirst && ok;
        }
        else
        {
            if (first.IsNotNull) _ = PublicShellFileServices.Close(dos, first);
            if (second.IsNotNull) _ = PublicShellFileServices.Close(dos, second);
        }
        _ = PublicShellFileServices.Delete(dos, firstPath);
        _ = PublicShellFileServices.Delete(dos, secondPath);
        _ = PublicShellFileServices.Delete(dos, firstOutput);
        _ = PublicShellFileServices.Delete(dos, secondOutput);
        return ok;
    }

    private static bool TestInputPrefix(ref DosShellNativePlatform shell, APTR dos)
    {
        #if COPPEROS_PROBE_SYS
        var path = Text("SYS:T/prefix-input");
        #else
        var path = Text("DH0:prefix-input");
        #endif
        if (!WriteFile(dos, path, Text("Alpha\nTail\n"))) return false;
        var file = PublicShellFileServices.OpenInput(dos, path, Length(path));
        var buffer = shell.AllocateRunnerMemory(32);
        var ok = file.IsNotNull && buffer.IsNotNull;
        if (ok)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectInUse);
            ok = PublicShellArgumentServices.ConsumeInputPrefix(dos, file, Text("Alpha\n"), 6) &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectInUse &&
                PublicShellFileServices.ReadLine(dos, file, buffer, 32, out var length, out var eof) &&
                eof == 0 && length == 4 && shell.ReadUInt8(buffer, 0) == (byte)'T' &&
                shell.ReadUInt8(buffer, 3) == (byte)'l';
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
            file = PublicShellFileServices.OpenInput(dos, path, Length(path));
            if (file.IsNull) ok = false;
            else
            {
                ok = !PublicShellArgumentServices.ConsumeInputPrefix(dos, file, Text("Alx"), 3) &&
                    PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectWrongType &&
                    PublicShellFileVectors.FGetC(dos, file) == (int)'h' && ok;
                ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
            }
        }
        else if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        if (buffer.IsNotNull) shell.FreeRunnerMemory(buffer, 32);
        _ = PublicShellFileServices.Delete(dos, path);
        return ok;
    }

    private static bool TestOpaqueProcessState(ref DosShellNativePlatform shell, APTR dos,
        APTR task, APTR cli, APTR observations)
    {
        #if COPPEROS_PROBE_SYS
        var path = Text("SYS:T/argument-input");
        var output = Text("SYS:T/argument-output");
        var tail = Text("SYS:T/argument-tail");
        var command = Text("Echo ALPHA >SYS:T/argument-output\n");
        var text = Text("Echo ALPHA >SYS:T/argument-output\nEcho OMEGA >SYS:T/argument-tail\n");
        #else
        var path = Text("DH0:argument-input");
        var output = Text("DH0:argument-output");
        var tail = Text("DH0:argument-tail");
        var command = Text("Echo ALPHA >DH0:argument-output\n");
        var text = Text("Echo ALPHA >DH0:argument-output\nEcho OMEGA >DH0:argument-tail\n");
        #endif
        _ = PublicShellFileServices.Delete(dos, output);
        _ = PublicShellFileServices.Delete(dos, tail);
        if (!WriteFile(dos, path, text)) return false;
        var input = PublicShellFileServices.OpenInput(dos, path, Length(path));
        if (input.IsNull) { _ = PublicShellFileServices.Delete(dos, path); return false; }
        var previous = DosCommandLineInterfaceCodec.Read(ref shell, cli);
        var previousArguments = DosProcessCodec.ReadArguments(ref shell, task);
        var previousPrivate = DosProcessCodec.ReadShellPrivate(ref shell, task);
        var selected = NativeForegroundDosVectors.SelectInput(dos, input);
        DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, input);
        DosCommandLineInterfaceCodec.WriteStandardInput(ref shell, cli, input);
        DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, 0);
        DosProcessCodec.WriteArguments(ref shell, task, command);
        // This opaque prior value matches the old private DOS Execute flags.
        // It must be restored, not interpreted as permission to read a tail.
        const uint opaque = 0x53590010;
        DosProcessCodec.WriteShellPrivate(ref shell, task, opaque);
        var result = DosShellNativeChildCore.RunFromCurrentTask();
        var restored = DosProcessCodec.ReadShellPrivate(ref shell, task) == opaque;
        DosProcessCodec.WriteShellPrivate(ref shell, task, previousPrivate);
        DosProcessCodec.WriteArguments(ref shell, task, previousArguments);
        _ = NativeForegroundDosVectors.SelectInput(dos, selected);
        DosCommandLineInterfaceCodec.WriteCurrentInput(ref shell, cli, previous.CurrentInput);
        DosCommandLineInterfaceCodec.WriteStandardInput(ref shell, cli, previous.StandardInput);
        DosCommandLineInterfaceCodec.WriteInteractive(ref shell, cli, previous.Interactive);
        DosCommandLineInterfaceCodec.WriteFailLevel(ref shell, cli, previous.FailLevel);
        DosCommandLineInterfaceCodec.WriteReturnCode(ref shell, cli, previous.ReturnCode);
        DosCommandLineInterfaceCodec.WriteResult2(ref shell, cli, previous.Result2);
        var commandFileRestored = DosCommandLineInterfaceCodec.Read(ref shell, cli).CommandFile == previous.CommandFile;
        var outputPresent = HasFile(dos, output);
        var tailPresent = HasFile(dos, tail);
        var next = PublicShellFileVectors.FGetC(dos, input);
        shell.WriteUInt32(observations, 0, result);
        shell.WriteUInt32(observations, 4, restored ? 1u : 0u);
        shell.WriteUInt32(observations, 8, commandFileRestored ? 1u : 0u);
        shell.WriteUInt32(observations, 12, outputPresent ? 1u : 0u);
        shell.WriteUInt32(observations, 16, tailPresent ? 1u : 0u);
        shell.WriteUInt32(observations, 20, unchecked((uint)next));
        var ok = result == 0 && restored && commandFileRestored &&
            outputPresent && !tailPresent && next == (int)'E';
        ok = PublicShellFileServices.Close(dos, input) != 0 && ok;
        _ = PublicShellFileServices.Delete(dos, path);
        _ = PublicShellFileServices.Delete(dos, output);
        _ = PublicShellFileServices.Delete(dos, tail);
        return ok;
    }

    private static bool WriteFile(APTR dos, APTR path, APTR text)
    {
        var file = PublicShellFileServices.OpenOutput(dos, path, Length(path), 0);
        if (file.IsNull) return false;
        var length = Length(text);
        var ok = PublicShellFileServices.Write(dos, file, text, length) == (int)length;
        return PublicShellFileServices.Close(dos, file) != 0 && ok;
    }

    private static bool HasFile(APTR dos, APTR path)
    {
        var file = PublicShellFileServices.OpenInput(dos, path, Length(path));
        if (file.IsNull) return false;
        return PublicShellFileServices.Close(dos, file) != 0;
    }

    private static uint Length(APTR text)
    {
        var length = 0u;
        while (APTR.ReadUInt8(text, (int)length) != 0) length++;
        return length;
    }

    [AmigaLibrary("dos.library::runner-probe", AmigaLibraryBasePolicy.CallerProvided)]
    private static class Vectors
    {
        [AmigaLvo(DosLvo.AssignLock)] [return: M68kRegister(M68kRegister.D0)]
        internal static extern int Assign([M68kRegister(M68kRegister.A6)] APTR dos,
            [M68kRegister(M68kRegister.D1)] APTR name,
            [M68kRegister(M68kRegister.D2)] BPTR path);
    }
}
