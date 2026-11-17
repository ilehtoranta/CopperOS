using Amiga;
using CopperOS.Shell;
using CopperOS.Shell.Dos;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Exercises the production native Shell's public file boundary.</summary>
public static class PublicFileNativeProbe
{
    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-file-probe")]
    public static int Main()
    {
        var ok = RunVariables() && RunWhy() && RunCliCommandFile() && RunCommandSource() && RunCommandReader() && Run() && RunArguments() && RunDiagnostics() && RunFrame() && RunForeground() && RunLookup();
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        #if COPPEROS_PROBE_SYS
        var marker = ok ? Text("SYS:T/runner-ok") : Text("SYS:T/runner-fail");
        var markerLength = ok ? 15u : 17u;
        #else
        var marker = ok ? Text("DH0:runner-ok") : Text("DH0:runner-fail");
        var markerLength = ok ? 13u : 15u;
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, marker,
            markerLength, 0);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    [M68kEntryPoint]
    [M68kExport("copperos.shell.public-why-probe")]
    public static int WhyMain()
    {
        var ok = RunWhy();
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

    private static bool RunWhy()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var buffer = platform.AllocateRunnerMemory(768);
        if (dos.IsNull || buffer.IsNull)
        {
            if (buffer.IsNotNull) platform.FreeRunnerMemory(buffer, 768);
            if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        #if COPPEROS_PROBE_SYS
        var path = Text("SYS:T/why-probe"); const uint pathLength = 15;
        #else
        var path = Text("DH0:why-probe"); const uint pathLength = 13;
        #endif
        var previousError = PublicShellFileServices.GetError(dos);
        var previous = DosCommandLineInterfaceCodec.Read(ref platform, cli).Result2;
        var actual = APTR.FromPointer(buffer.Raw + 384);
        var expected = Text("Last command failed because : object not found\n");
        var expectedLength = 47;
        DosCommandLineInterfaceCodec.WriteResult2(ref platform, cli, 205);
        var file = PublicShellFileServices.OpenOutput(dos, path, pathLength, 0);
        var ok = file.IsNotNull && platform.TryWriteWhy(file, cli) &&
            PublicShellFileServices.GetError(dos) == 205;
        if (file.IsNotNull) ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        file = PublicShellFileServices.OpenInput(dos, path, pathLength);
        if (file.IsNull) ok = false;
        else
        {
            var received = PublicShellFileServices.Read(dos, file, actual, 384);
            ok = received == expectedLength && ok;
            for (var i = 0; i < expectedLength; i++)
                if (platform.ReadUInt8(actual, i) != platform.ReadUInt8(expected, i)) ok = false;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        _ = PublicShellFileServices.Delete(dos, path);
        DosCommandLineInterfaceCodec.WriteResult2(ref platform, cli, 0);
        expected = Text("The last command did not set a return code\n");
        expectedLength = 43;
        file = PublicShellFileServices.OpenOutput(dos, path, pathLength, 0);
        if (file.IsNull) ok = false;
        else
        {
            ok = platform.TryWriteWhy(file, cli) && PublicShellFileServices.GetError(dos) == 0 && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
            file = PublicShellFileServices.OpenInput(dos, path, pathLength);
            if (file.IsNull) ok = false;
            else
            {
                var received = PublicShellFileServices.Read(dos, file, actual, 384);
                ok = received == expectedLength && ok;
                for (var i = 0; i < expectedLength; i++)
                    if (platform.ReadUInt8(actual, i) != platform.ReadUInt8(expected, i)) ok = false;
                ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
            }
        }
        _ = PublicShellFileServices.Delete(dos, path);
        DosCommandLineInterfaceCodec.WriteResult2(ref platform, cli, previous);
        platform.FreeRunnerMemory(buffer, 768);
        PublicShellFileServices.SetError(dos, (DOS.Error)previousError);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static bool RunVariables()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var buffer = platform.AllocateRunnerMemory(32);
        if (dos.IsNull || buffer.IsNull)
        {
            if (buffer.IsNotNull) platform.FreeRunnerMemory(buffer, 32);
            if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var name = Text("CopperProbeLocal");
        var value = Text("A\nB");
        var ok = platform.TrySetLocalVariable(cli, name, 16, value, 3) &&
            platform.TryGetLocalVariable(cli, name, 16, buffer, 32, out var length) &&
            length == 3 && platform.ReadUInt8(buffer) == (byte)'A' &&
            platform.ReadUInt8(buffer, 1) == (byte)'\n' && platform.ReadUInt8(buffer, 2) == (byte)'B';
        var overflow = !platform.TryGetLocalVariable(cli, name, 16, buffer, 2, out var truncated) &&
            truncated == 3 && PublicShellFileServices.GetError(dos) == (int)DOS.Error.BufferOverflow;
        ok = overflow && ok;
        ok = platform.TrySetAlias(cli, name, 16, Text("Echo []"), 7) && ok;
        ok = PublicShellVariableServices.Get(ref platform, dos, cli, name, 16, buffer, 32, true, out length) &&
            length == 7 && platform.ReadUInt8(buffer) == (byte)'E' && ok;
        ok = platform.TryGetAlias(cli, name, 16, buffer, 32, out length, out var missing) &&
            missing == 0 && length == 7 && platform.ReadUInt8(buffer) == (byte)'E' && ok;
        ok = CheckAliasQuery(ref platform, dos, cli, buffer, name) && ok;
        ok = CheckVariableListing(ref platform, dos, cli, buffer, name, true) && ok;
        ok = CheckVariableListing(ref platform, dos, cli, buffer, name, false) && ok;
        var source = Text("CopperProbeLocal A B");
        ok = platform.TryExpandScriptAlias(cli, source, 20, buffer, 32, out var expanded, out length) &&
            expanded == 1 && length == 8 && platform.ReadUInt8(buffer, 0) == (byte)'E' &&
            platform.ReadUInt8(buffer, 5) == (byte)'A' && platform.ReadUInt8(buffer, 7) == (byte)'B' && ok;
        ok = !platform.TryExpandScriptAlias(cli, source, 20, buffer, 4, out _, out _) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.BufferOverflow && ok;
        ok = platform.TrySetAlias(cli, name, 16, Text("Echo"), 4) &&
            platform.TryExpandScriptAlias(cli, source, 20, buffer, 32, out expanded, out length) &&
            expanded == 1 && length == 8 && platform.ReadUInt8(buffer, 5) == (byte)'A' &&
            platform.ReadUInt8(buffer, 7) == (byte)'B' && ok;
        ok = platform.TrySetAlias(cli, name, 16, Text("[] []"), 5) &&
            platform.TryExpandScriptAlias(cli, source, 20, buffer, 32, out expanded, out length) &&
            expanded == 1 && length == 7 && platform.ReadUInt8(buffer, 0) == (byte)'A' &&
            platform.ReadUInt8(buffer, 4) == (byte)'A' && platform.ReadUInt8(buffer, 6) == (byte)'B' && ok;
        PublicShellFileServices.SetError(dos, DOS.Error.ObjectInUse);
        ok = platform.TryExpandScriptAlias(cli, Text("NotAProbe"), 9, buffer, 32, out expanded, out length) &&
            expanded == 0 && PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectInUse && ok;
        ok = platform.TryRemoveAlias(cli, name, 16) && ok;
        ok = !platform.TryGetAlias(cli, name, 16, buffer, 32, out length, out missing) && missing == 1 &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectNotFound && ok;
        ok = platform.TryGetLocalVariable(cli, name, 16, buffer, 32, out length) && length == 3 && ok;
        ok = platform.TrySetLocalVariable(cli, name, 16, APTR.Null, 0) &&
            platform.TryGetLocalVariable(cli, name, 16, buffer, 32, out length) && length == 0 && ok;
        ok = platform.TryRemoveLocalVariable(cli, name, 16) && ok;
        ok = !platform.TryGetLocalVariable(cli, name, 16, buffer, 32, out length) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectNotFound && ok;
        _ = platform.TryRemoveAlias(cli, name, 16);
        _ = platform.TryRemoveLocalVariable(cli, name, 16);
        platform.FreeRunnerMemory(buffer, 32);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static bool CheckAliasQuery(ref DosShellNativePlatform platform, APTR dos, APTR cli,
        APTR buffer, APTR name)
    {
        #if COPPEROS_PROBE_SYS
        var path = Text("SYS:T/alias-query"); const uint pathLength = 17;
        #else
        var path = Text("DH0:alias-query"); const uint pathLength = 15;
        #endif
        var scratch = platform.AllocateRunnerMemory(128);
        if (scratch.IsNull) return false;
        var value = APTR.FromPointer(scratch.Raw + 64);
        var file = PublicShellFileServices.OpenOutput(dos, path, pathLength, 0);
        if (file.IsNull) { platform.FreeRunnerMemory(scratch, 128); return false; }
        var invocation = new CommandInvocation(name, 16, dos, APTR.Null, BPTR.Null,
            file, BPTR.Null, BPTR.Null, cli, 0, 0);
        var result = AliasCommand.Execute(ref platform, in invocation, scratch, 64, value, 64);
        var closed = PublicShellFileServices.Close(dos, file) != 0;
        var ok = result == (int)ShellCommandResult.Ok && closed;
        file = PublicShellFileServices.OpenInput(dos, path, pathLength);
        if (file.IsNotNull)
        {
            ok = PublicShellFileServices.ReadLine(dos, file, buffer, 32, out var length, out var eof) &&
                eof == 0 && length == 7 && platform.ReadUInt8(buffer) == (byte)'E' &&
                platform.ReadUInt8(buffer, 5) == (byte)'[' && platform.ReadUInt8(buffer, 6) == (byte)']' && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        else ok = false;
        _ = PublicShellFileServices.Delete(dos, path);
        file = PublicShellFileServices.OpenOutput(dos, path, pathLength, 0);
        if (file.IsNotNull)
        {
            var absent = new CommandInvocation(Text("NoSuchAlias"), 11, dos, APTR.Null,
                BPTR.Null, file, BPTR.Null, BPTR.Null, cli, 0, 0);
            result = AliasCommand.Execute(ref platform, in absent, scratch, 64, value, 64);
            ok = result == (int)ShellCommandResult.Warn &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectNotFound && ok;
            ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        }
        else ok = false;
        _ = PublicShellFileServices.Delete(dos, path);
        platform.FreeRunnerMemory(scratch, 128);
        return ok;
    }

    private static bool CheckVariableListing(ref DosShellNativePlatform platform, APTR dos, APTR cli,
        APTR buffer, APTR expectedName, bool aliases)
    {
        #if COPPEROS_PROBE_SYS
        var path = Text("SYS:T/variable-list"); const uint pathLength = 19;
        #else
        var path = Text("DH0:variable-list"); const uint pathLength = 17;
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, path, pathLength, 0);
        if (file.IsNull) return false;
        var ok = aliases ? platform.TryWriteAliases(file, cli) : platform.TryWriteLocalVariables(file, cli);
        ok = PublicShellFileServices.Close(dos, file) != 0 && ok;
        file = PublicShellFileServices.OpenInput(dos, path, pathLength);
        if (file.IsNull) { _ = PublicShellFileServices.Delete(dos, path); return false; }
        var found = false;
        for (var line = 0; line < 1024; line++)
        {
            if (!PublicShellFileServices.ReadLine(dos, file, buffer, 32, out var length, out var eof)) { ok = false; break; }
            if (eof != 0) break;
            if (length < 19) continue;
            var same = true;
            for (var i = 0; i < 16; i++)
                if (platform.ReadUInt8(buffer, i) != platform.ReadUInt8(expectedName, i)) same = false;
            if (same && platform.ReadUInt8(buffer, 16) == (byte)' ' && platform.ReadUInt8(buffer, 17) == (byte)' ' &&
                platform.ReadUInt8(buffer, 18) == (aliases ? (byte)'E' : (byte)'A')) found = true;
        }
        ok = PublicShellFileServices.Close(dos, file) != 0 && found && ok;
        _ = PublicShellFileServices.Delete(dos, path);
        return ok;
    }

    private static bool RunCliCommandFile()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        if (cli.IsNull || !DosCommandLineInterfaceCodec.IsMapped(ref platform, cli)) return false;
        var name = DosCommandLineInterfaceCodec.Read(ref platform, cli).CommandFile.Address;
        if (name.IsNull || !platform.IsMapped(name, 1)) return false;
        var saved = platform.ReadUInt8(name);
        platform.WriteUInt8(name, 0, 1);
        var ok = ShellCliState.TryClearCommandFile(ref platform, cli) && platform.ReadUInt8(name) == 0;
        platform.WriteUInt8(name, 0, saved);
        return ok;
    }

    private static bool RunCommandReader()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        if (!ShellRunnerBinding.TryFind(ref platform, cli, out var existing, out _, out _)) return false;
        // A nested test command must not replace its enclosing live runner.
        if (existing.IsNotNull) return true;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var input = PublicShellFileServices.Input(dos);
        var frame = platform.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        var source = ShellCommandSource.Create(ref platform, Text("X\nY"), 3);
        var allocated = ShellRunnerArena.TryAllocate(ref platform, true, out var runner, out var workspace);
        if (input.IsNull || frame.IsNull || source.IsNull || !allocated)
        {
            if (source.IsNotNull) _ = ShellCommandSource.Release(ref platform, source);
            if (frame.IsNotNull) platform.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
            if (allocated) ShellRunnerArena.Free(ref platform, runner, true);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var initial = new ShellScriptFrameState { Cli = cli, Parent = cli, Input = input,
            Flags = ShellScriptFrameFlags.Active, FailureLimit = 10 };
        var stored = new ShellScriptRunnerRecord
        {
            Cli = cli, Frame = frame, Input = source, InputOwned = 2,
            Line = workspace, CommandName = Part(workspace, ShellScriptWorkspaceLayout.Name),
            Token = Part(workspace, ShellScriptWorkspaceLayout.Token),
            First = Part(workspace, ShellScriptWorkspaceLayout.First),
            Second = Part(workspace, ShellScriptWorkspaceLayout.Second),
            Third = Part(workspace, ShellScriptWorkspaceLayout.Third),
            Fourth = Part(workspace, ShellScriptWorkspaceLayout.Fourth),
            RedirectionCommand = Part(workspace, ShellScriptWorkspaceLayout.Redirection),
            RedirectionInput = Part(workspace, ShellScriptWorkspaceLayout.Input),
            RedirectionOutput = Part(workspace, ShellScriptWorkspaceLayout.Output),
            RedirectionError = Part(workspace, ShellScriptWorkspaceLayout.Error),
            AliasLine = Part(workspace, ShellScriptWorkspaceLayout.Alias),
            LookupPath = Part(workspace, ShellScriptWorkspaceLayout.Lookup),
            ScriptTemporaryPath = Part(workspace, ShellScriptWorkspaceLayout.Temporary),
            State = ShellScriptRunnerState.Running,
        };
        var initialized = ShellScriptFrameCodec.Initialize(ref platform, frame, in initial) &&
            ShellScriptRunnerCodec.Initialize(ref platform, runner, in stored);
        if (!initialized)
        {
            _ = ShellCommandSource.Release(ref platform, source);
            platform.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
            ShellRunnerArena.Free(ref platform, runner, true);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var published = ShellRunnerBinding.TryPublish(ref platform, cli, frame, runner);
        var ok = published;
        if (published)
        {
            ok = !platform.TryReadScriptLine(cli, input, 1, 0, workspace, 1,
                out _, out _, out _, out _) &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.BufferOverflow;
            ok = platform.TryReadScriptLine(cli, input, 1, 0, workspace, 16,
                out var length, out var line, out var offset, out var eof) &&
                length == 1 && line == 2 && offset == 2 && eof == 0 && platform.ReadUInt8(workspace) == (byte)'X' && ok;
            ok = platform.TryReadScriptLine(cli, input, line, offset, workspace, 16,
                out length, out line, out offset, out eof) &&
                length == 1 && line == 3 && offset == 4 && eof == 0 && platform.ReadUInt8(workspace) == (byte)'Y' && ok;
            ok = platform.TryReadScriptLine(cli, input, line, offset, workspace, 16,
                out length, out line, out offset, out eof) && length == 0 && eof == 1 && ok;
            if (!ShellRunnerBinding.TryRemove(ref platform, cli, frame, runner))
            {
                global::Amiga.Exec.CloseLibrary(dos);
                return false;
            }
        }
        ok = ShellRunnerCleanup.Release(ref platform, runner, true) && ok;
        ShellRunnerArena.Free(ref platform, runner, true);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static APTR Part(APTR workspace, uint offset) => APTR.FromPointer(workspace.Raw + offset);

    private static bool RunCommandSource()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var source = ShellCommandSource.Create(ref platform, Text("ABC\r\nDEF"), 8);
        var buffer = platform.AllocateRunnerMemory(16);
        var runner = platform.AllocateRunnerMemory(ShellRunnerBuffers.RecordSize);
        if (source.IsNull || buffer.IsNull || runner.IsNull)
        {
            if (source.IsNotNull) _ = ShellCommandSource.Release(ref platform, source);
            if (buffer.IsNotNull) platform.FreeRunnerMemory(buffer, 16);
            if (runner.IsNotNull) platform.FreeRunnerMemory(runner, ShellRunnerBuffers.RecordSize);
            return false;
        }
        var ok = ShellCommandSource.TryRead(ref platform, source, 0, buffer, 16,
            out var length, out var offset, out var eof) && length == 3 && offset == 5 && eof == 0 &&
            platform.ReadUInt8(buffer, 0) == (byte)'A' && platform.ReadUInt8(buffer, 2) == (byte)'C';
        ok = ShellCommandSource.TryRead(ref platform, source, offset, buffer, 16,
            out length, out offset, out eof) && length == 3 && offset == 9 && eof == 0 &&
            platform.ReadUInt8(buffer, 0) == (byte)'D' && platform.ReadUInt8(buffer, 2) == (byte)'F' && ok;
        ok = ShellCommandSource.TryRead(ref platform, source, offset, buffer, 16,
            out length, out offset, out eof) && length == 0 && eof == 1 && ok;
        platform.WriteUInt32(runner, 0, ShellScriptRunnerCodec.Magic);
        platform.WriteUInt32(runner, 4, ShellScriptRunnerCodec.Version);
        platform.WriteUInt32(runner, 16, source.Raw);
        platform.WriteUInt32(runner, 88, 2);
        ok = ShellRunnerCleanup.Release(ref platform, runner, false) && ok;
        platform.FreeRunnerMemory(runner, ShellRunnerBuffers.RecordSize);
        platform.FreeRunnerMemory(buffer, 16);
        return ok;
    }

    private static bool RunLookup()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var buffer = platform.AllocateRunnerMemory(512);
        if (dos.IsNull || buffer.IsNull)
        {
            if (buffer.IsNotNull) platform.FreeRunnerMemory(buffer, 512);
            if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var workspace = new ShellScriptLookupWorkspace(buffer, 512);
        #if COPPEROS_PROBE_SYS
        var fixture = Text("SYS:C/fixture"); const uint fixtureLength = 13;
        var target = Text("SYS:C");
        var missing = Text("SYS:C/__missing_foreground__"); const uint missingLength = 28;
        var script = Text("SYS:T/lookup-script"); const uint scriptLength = 19;
        #else
        var fixture = Text("DH0:fixture"); const uint fixtureLength = 11;
        var target = Text("DH0:");
        var missing = Text("DH0:__missing_foreground__"); const uint missingLength = 26;
        var script = Text("DH0:lookup-script"); const uint scriptLength = 17;
        #endif
        var failed = 0;
        var ok = platform.TryLookupScriptCommand(cli, fixture, fixtureLength, in workspace, out var lookup) &&
            lookup.Kind == ShellScriptLookupKind.ExplicitFile && lookup.Origin == ShellScriptLookupOrigin.ExplicitFile;
        if (!ok) failed = 1;
        var missingOk = platform.TryLookupScriptCommand(cli, missing, missingLength, in workspace, out lookup) &&
            lookup.Kind == ShellScriptLookupKind.NotFound &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectNotFound;
        if (!missingOk && failed == 0) failed = 2;
        ok = missingOk && ok;
        var directory = PublicShellFileServices.Lock(dos, target);
        var elsewhere = PublicShellFileServices.Lock(dos, Text("SYS:S"));
        var node = platform.AllocateRunnerMemory(PathLock.Size);
        if (directory.IsNull || elsewhere.IsNull || node.IsNull)
        {
            if (failed == 0) failed = 3;
            ok = false;
        }
        else
        {
            var previousDir = PublicShellPathVectors.CurrentDir(dos, directory);
            var expectedError = DosProcessCodec.ReadCurrentError(ref platform, task);
            if (expectedError.IsNull) expectedError = PublicShellFileServices.Output(dos);
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectInUse);
            var contextOk = platform.TryCurrentScriptProcess(cli, out var observedDir, out var observedError) &&
                observedDir == directory && observedError == expectedError &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectInUse;
            if (!contextOk && failed == 0) failed = 7;
            ok = contextOk && ok;
            var currentOk = platform.TryLookupScriptCommand(cli, Text("fixture"), 7, in workspace, out lookup) &&
                lookup.Kind == ShellScriptLookupKind.CurrentDirectory && lookup.Origin == ShellScriptLookupOrigin.CurrentDirectory;
            if (!currentOk && failed == 0) failed = 4;
            ok = currentOk && ok;
            _ = PublicShellPathVectors.CurrentDir(dos, elsewhere);
            var previousPath = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref platform, cli);
            platform.WriteUInt32(node, 0, previousPath.Raw);
            platform.WriteUInt32(node, 4, directory.Raw);
            DosCommandLineInterfaceCodec.WriteCommandDirectory(ref platform, cli, BPTR.FromAddress(node));
            var pathOk = platform.TryLookupScriptCommand(cli, Text("fixture"), 7, in workspace, out lookup) &&
                lookup.Kind == ShellScriptLookupKind.CommandPath && lookup.Origin == ShellScriptLookupOrigin.CommandPath;
            if (!pathOk && failed == 0) failed = 5;
            ok = pathOk && ok;
            DosCommandLineInterfaceCodec.WriteCommandDirectory(ref platform, cli, previousPath);
            var nonRoot = PublicShellPathVectors.CurrentDir(dos, BPTR.Null);
            var rootOk = platform.TryCurrentScriptProcess(cli, out observedDir, out observedError) &&
                observedDir.IsNull && observedError == expectedError;
            if (!rootOk && failed == 0) failed = 8;
            ok = rootOk && ok;
            _ = PublicShellPathVectors.CurrentDir(dos, nonRoot);
            _ = PublicShellPathVectors.CurrentDir(dos, previousDir);
        }
        if (node.IsNotNull) platform.FreeRunnerMemory(node, PathLock.Size);
        if (directory.IsNotNull) PublicShellFileServices.UnLock(dos, directory);
        if (elsewhere.IsNotNull) PublicShellFileServices.UnLock(dos, elsewhere);
        var output = PublicShellFileServices.OpenOutput(dos, script, scriptLength, 0);
        var scriptOk = output.IsNotNull;
        if (output.IsNotNull)
        {
            scriptOk = PublicShellFileServices.Close(dos, output) != 0 &&
                PublicShellLookupVectors.SetProtection(dos, script, (int)FileProtection.Script) != 0 &&
                platform.TryLookupScriptCommand(cli, script, scriptLength, in workspace, out lookup) &&
                lookup.Kind == ShellScriptLookupKind.Script &&
                (lookup.Protection & FileProtection.Script) != 0;
            _ = PublicShellFileServices.Delete(dos, script);
        }
        if (!scriptOk && failed == 0) failed = 6;
        ok = scriptOk && ok;
        if (failed != 0) LookupFailure(dos, failed);
        platform.FreeRunnerMemory(buffer, 512);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static void LookupFailure(APTR dos, int stage)
    {
        #if COPPEROS_PROBE_SYS
        var name = Text("SYS:T/lookup-error"); const uint length = 18;
        #else
        var name = Text("DH0:lookup-error"); const uint length = 16;
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, name, length, 0);
        if (file.IsNotNull)
        {
            _ = PublicShellFileServices.WriteByte(dos, file, (byte)('0' + stage));
            _ = PublicShellFileServices.Close(dos, file);
        }
    }

    private static bool RunForeground()
    {
        var memory = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var names = memory.AllocateGuest(512);
        if (names.IsNull) { global::Amiga.Exec.CloseLibrary(dos); return false; }
        var afterName = APTR.FromPointer(names.Raw + 256);
        var beforeInput = PublicShellFileServices.Input(dos);
        var beforeOutput = PublicShellFileServices.Output(dos);
        var beforeError = DosProcessCodec.ReadCurrentError(ref memory, task);
        var beforeHome = NativeForegroundDosVectors.GetProgramDir(dos);
        var beforeModule = memory.ReadUInt32(cli, DosLayout.CommandLineInterface.Module);
        var ok = NativeForegroundDosVectors.GetProgramName(dos, names, 256) != 0;
        #if COPPEROS_PROBE_SYS
        var fixture = Text("SYS:C/fixture");
        const uint fixtureLength = 13;
        var outputName = Text("SYS:T/foreground-output");
        const uint outputLength = 23;
        var missing = Text("SYS:C/__missing_foreground__");
        const uint missingLength = 28;
        #else
        var fixture = Text("DH0:fixture");
        const uint fixtureLength = 11;
        var outputName = Text("DH0:foreground-output");
        const uint outputLength = 21;
        var missing = Text("DH0:__missing_foreground__");
        const uint missingLength = 26;
        #endif
        var redirected = PublicShellFileServices.OpenOutput(dos, outputName, outputLength, 0);
        var command = new ShellScriptCommandInvocation
        {
            CommandName = Text("fixture"), CommandNameLength = 7,
            Arguments = new ShellScriptTextSlice(Text("ABC"), 3),
        };
        var lookup = new ShellScriptLookupResult
        {
            Kind = ShellScriptLookupKind.ExplicitFile,
            ResolvedPath = fixture, PathLength = fixtureLength,
        };
        ok = redirected.IsNotNull && ok;
        if (redirected.IsNotNull)
        {
            ok = DosShellNativeForegroundCore.Execute(memory, cli,
                in command, in lookup, beforeInput, redirected, redirected, out var result) &&
                result == 7 && ok;
            command.Arguments = new ShellScriptTextSlice(Text("BAD"), 3);
            ok = DosShellNativeForegroundCore.Execute(memory, cli,
                in command, in lookup, beforeInput, redirected, redirected, out result) &&
                result == 9 && ok;
            lookup.ResolvedPath = missing;
            lookup.PathLength = missingLength;
            ok = !DosShellNativeForegroundCore.Execute(memory, cli,
                in command, in lookup, beforeInput, redirected, redirected, out result) &&
                PublicShellFileServices.GetError(dos) == (int)DOS.Error.ObjectNotFound && ok;
            ok = RunResident(ref memory, dos, cli, fixture, beforeInput,
                redirected, in command) && ok;
            #if COPPEROS_PROBE_SYS
            ok = RunDeferredResident(ref memory, dos, fixture) && ok;
            #endif
            ok = PublicShellFileServices.Input(dos) == beforeInput &&
                PublicShellFileServices.Output(dos) == beforeOutput &&
                DosProcessCodec.ReadCurrentError(ref memory, task) == beforeError &&
                NativeForegroundDosVectors.GetProgramDir(dos) == beforeHome &&
                memory.ReadUInt32(cli, DosLayout.CommandLineInterface.Module) == beforeModule &&
                NativeForegroundDosVectors.GetProgramName(dos, afterName, 256) != 0 &&
                SameText(ref memory, names, afterName) && ok;
            ok = PublicShellFileServices.Close(dos, redirected) != 0 && ok;
            ok = PublicShellFileServices.Delete(dos, outputName) != 0 && ok;
        }
        memory.FreeGuest(names, 512);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    #if COPPEROS_PROBE_SYS
    private static bool RunDeferredResident(ref CopperSharpNativeDosPlatform memory,
        APTR dos, APTR fixture)
    {
        if (memory.ReadUInt16(dos, ExecLayout.Library.Version) < 50 ||
            memory.ReadUInt16(dos, ExecLayout.Library.NegativeSize) < 1008)
        {
            var skipped = PublicShellFileServices.OpenOutput(dos,
                Text("SYS:T/deferred-unavailable"), 26, 0);
            if (skipped.IsNotNull) _ = PublicShellFileServices.Close(dos, skipped);
            return true;
        }
        var tags = memory.AllocateGuest(32);
        if (tags.IsNull) return false;
        var name = Text("COPPEROS_DEFER_PROBE");
        memory.WriteUInt32(tags, 0, (uint)DosAddSegmentTag.Name);
        memory.WriteUInt32(tags, 4, name.Raw);
        memory.WriteUInt32(tags, 8, (uint)DosAddSegmentTag.FileName);
        memory.WriteUInt32(tags, 12, fixture.Raw);
        memory.WriteUInt32(tags, 16, (uint)DosAddSegmentTag.Type);
        memory.WriteUInt32(tags, 20, 1);
        memory.WriteUInt32(tags, 24, 0);
        memory.WriteUInt32(tags, 28, 0);
        var added = PublicShellResidentVectors.AddSegmentTagList(dos, tags) != 0;
        memory.FreeGuest(tags, 32);
        if (!added) { ResidentFailure(dos, 12); return false; }
        var image = PublicShellResidentServices.Acquire(ref memory, dos, name, out var resident);
        var ok = image.IsNotNull && resident.IsNotNull &&
            DosSegmentCodec.ReadUseCount(ref memory, resident) == 2;
        if (resident.IsNotNull) PublicShellResidentServices.Release(ref memory, resident);
        else
        {
            global::Amiga.Exec.Forbid();
            resident = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 0);
            global::Amiga.Exec.Permit();
        }
        if (resident.IsNotNull)
        {
            ok = DosSegmentCodec.ReadUseCount(ref memory, resident) == 1 && ok;
            global::Amiga.Exec.Forbid();
            DosSegmentCodec.WriteUseCount(ref memory, resident, 0);
            var removed = PublicShellResidentVectors.RemSegment(dos, resident);
            global::Amiga.Exec.Permit();
            ok = removed != 0 && ok;
        }
        if (!ok) ResidentFailure(dos, 13);
        else
        {
            var marker = PublicShellFileServices.OpenOutput(dos, Text("SYS:T/deferred-ok"), 17, 0);
            if (marker.IsNotNull) _ = PublicShellFileServices.Close(dos, marker);
        }
        return ok;
    }
    #endif

    private static bool RunResident(ref CopperSharpNativeDosPlatform memory, APTR dos,
        APTR cli, APTR fixture, BPTR input, BPTR output,
        in ShellScriptCommandInvocation original)
    {
        var failed = 0;
        var name = Text("COPPEROS_PROBE_FIXTURE");
        var image = NativeForegroundDosVectors.LoadSeg(dos, fixture);
        if (image.IsNull) { ResidentFailure(dos, 1); return false; }
        // Kickstart 3.1's FindSegment ignores a regular entry with use count
        // zero despite the older autodoc suggesting zero as the initial value.
        if (PublicShellResidentVectors.AddSegment(dos, name, image, 1) == 0)
        {
            NativeForegroundDosVectors.UnLoadSeg(dos, image);
            ResidentFailure(dos, 2);
            return false;
        }
        var acquired = PublicShellResidentServices.Acquire(ref memory, dos, name, out var resident);
        var ok = acquired == image && resident.IsNotNull &&
            DosSegmentCodec.ReadUseCount(ref memory, resident) == 2;
        if (!ok)
        {
            failed = 3;
            ResidentDetails(ref memory, dos, image, acquired, resident);
        }
        if (resident.IsNull)
        {
            global::Amiga.Exec.Forbid();
            resident = PublicShellResidentVectors.FindSegment(dos, name, APTR.Null, 0);
            global::Amiga.Exec.Permit();
        }
        if (resident.IsNotNull && acquired.IsNotNull)
        {
            global::Amiga.Exec.Forbid();
            var removedWhileBusy = PublicShellResidentVectors.RemSegment(dos, resident);
            global::Amiga.Exec.Permit();
            ok = removedWhileBusy == 0 && ok;
            if (removedWhileBusy != 0 && failed == 0) failed = 4;
            PublicShellResidentServices.Release(ref memory, resident);
            var releasedCount = DosSegmentCodec.ReadUseCount(ref memory, resident);
            if (releasedCount != 1 && failed == 0) failed = 5;
            ok = releasedCount == 1 && ok;
        }
        var lookupBuffer = memory.AllocateGuest(512);
        if (lookupBuffer.IsNull) { if (failed == 0) failed = 14; ok = false; }
        else
        {
            var platform = new DosShellNativePlatform(APTR.Null);
            var workspace = new ShellScriptLookupWorkspace(lookupBuffer, 512);
            var found = platform.TryLookupScriptCommand(cli, name, 22, in workspace, out var foundValue) &&
                foundValue.Kind == ShellScriptLookupKind.Resident &&
                foundValue.Origin == ShellScriptLookupOrigin.Resident;
            if (!found && failed == 0) failed = 15;
            ok = found && ok;
            memory.FreeGuest(lookupBuffer, 512);
        }
        var command = original;
        command.Arguments = new ShellScriptTextSlice(Text("ABC"), 3);
        var lookup = new ShellScriptLookupResult { Kind = ShellScriptLookupKind.Resident,
            ResolvedPath = name, PathLength = 22 };
        var ran = DosShellNativeForegroundCore.Execute(memory, cli,
            in command, in lookup, input, output, output, out var result);
        if ((!ran || result != 7) && failed == 0) failed = ran ? 7 : 6;
        ok = ran && result == 7 && ok;
        if (resident.IsNotNull)
        {
            if (DosSegmentCodec.ReadUseCount(ref memory, resident) != 1 && failed == 0) failed = 8;
            ok = DosSegmentCodec.ReadUseCount(ref memory, resident) == 1 && ok;
            global::Amiga.Exec.Forbid();
            DosSegmentCodec.WriteUseCount(ref memory, resident, -2);
            global::Amiga.Exec.Permit();
            var systemImage = PublicShellResidentServices.Acquire(ref memory, dos, name, out var systemResident);
            var systemOk = systemImage == image && systemResident == resident &&
                DosSegmentCodec.ReadUseCount(ref memory, resident) == -2;
            if (systemResident.IsNotNull) PublicShellResidentServices.Release(ref memory, systemResident);
            systemOk = systemOk && DosSegmentCodec.ReadUseCount(ref memory, resident) == -2;
            if (!systemOk && failed == 0) failed = 10;
            ok = systemOk && ok;
            global::Amiga.Exec.Forbid();
            DosSegmentCodec.WriteUseCount(ref memory, resident, -999);
            global::Amiga.Exec.Permit();
            var disabled = PublicShellResidentServices.Acquire(ref memory, dos, name, out var disabledResident);
            var disabledOk = disabled.IsNull && disabledResident.IsNull &&
                DosSegmentCodec.ReadUseCount(ref memory, resident) == -999;
            if (!disabledOk && failed == 0) failed = 11;
            ok = disabledOk && ok;
            global::Amiga.Exec.Forbid();
            DosSegmentCodec.WriteUseCount(ref memory, resident, 0);
            var removed = PublicShellResidentVectors.RemSegment(dos, resident);
            global::Amiga.Exec.Permit();
            if (removed == 0 && failed == 0) failed = 9;
            ok = removed != 0 && ok;
        }
        if (failed != 0) ResidentFailure(dos, failed);
        return ok;
    }

    private static void ResidentDetails(ref CopperSharpNativeDosPlatform memory, APTR dos,
        BPTR image, BPTR acquired, APTR resident)
    {
        var error = PublicShellFileServices.GetError(dos);
        var buffer = memory.AllocateGuest(20);
        if (buffer.IsNull) return;
        memory.WriteUInt32(buffer, 0, image.Raw);
        memory.WriteUInt32(buffer, 4, acquired.Raw);
        memory.WriteUInt32(buffer, 8, resident.Raw);
        memory.WriteUInt32(buffer, 12, resident.IsNull ? 0xffffffffu :
            unchecked((uint)DosSegmentCodec.ReadUseCount(ref memory, resident)));
        memory.WriteUInt32(buffer, 16, unchecked((uint)error));
        #if COPPEROS_PROBE_SYS
        var name = Text("SYS:T/resident-details");
        const uint length = 22;
        #else
        var name = Text("DH0:resident-details");
        const uint length = 20;
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, name, length, 0);
        if (file.IsNotNull)
        {
            _ = PublicShellFileServices.Write(dos, file, buffer, 20);
            _ = PublicShellFileServices.Close(dos, file);
        }
        memory.FreeGuest(buffer, 20);
    }

    private static void ResidentFailure(APTR dos, int stage)
    {
        #if COPPEROS_PROBE_SYS
        var name = Text("SYS:T/resident-error");
        const uint length = 20;
        #else
        var name = Text("DH0:resident-error");
        const uint length = 18;
        #endif
        var file = PublicShellFileServices.OpenOutput(dos, name, length, 0);
        if (file.IsNotNull)
        {
            _ = PublicShellFileServices.WriteByte(dos, file, (byte)('0' + stage));
            _ = PublicShellFileServices.Close(dos, file);
        }
    }

    private static bool SameText(ref CopperSharpNativeDosPlatform memory, APTR a, APTR b)
    {
        for (var i = 0; i < 256; i++)
        {
            var value = memory.ReadUInt8(a, i);
            if (value != memory.ReadUInt8(b, i)) return false;
            if (value == 0) return true;
        }
        return false;
    }

    private static bool RunFrame()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var previousPrivate = DosProcessCodec.ReadShellPrivate(ref platform, task);
        var previousError = PublicShellFileServices.GetError(dos);
        var previousCli = DosCommandLineInterfaceCodec.Read(ref platform, cli);
        var fieldsOk = platform.TryReadCliDefaultStack(cli, out var stack) &&
            platform.TryWriteCliDefaultStack(cli, stack) &&
            platform.TryReadCliFailureLimit(cli, out var limit) &&
            platform.TryWriteCliFailureLimit(cli, limit);
        DosCommandLineInterfaceCodec.WriteDefaultStack(ref platform, cli, previousCli.DefaultStack);
        DosCommandLineInterfaceCodec.WriteFailLevel(ref platform, cli, previousCli.FailLevel);
        var frame = platform.AllocateRunnerMemory(ShellScriptFrameCodec.Size);
        if (frame.IsNull) { global::Amiga.Exec.CloseLibrary(dos); return false; }
        var initial = new ShellScriptFrameState { Cli = cli, Parent = cli,
            Flags = ShellScriptFrameFlags.Active, FailureLimit = 10 };
        var ok = fieldsOk && ShellScriptFrameCodec.Initialize(ref platform, frame, in initial) &&
            platform.TryBindScriptFrame(cli, frame);
        var bound = ok;
        if (bound)
        {
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectNotFound);
            ok = platform.TryEvaluateIf(cli, (uint)ShellIfCondition.PreviousResult,
                0, 0, 0, 0, APTR.Null, 0, APTR.Null, 0) &&
                PublicShellFileServices.GetError(dos) == 0 && ok;
            ok = platform.TryDefineScriptLabel(cli, Text("MARK"), 4) && ok;
            ok = platform.TryRequestShellControl(cli, ShellControlAction.EndIf, 0) && ok;
            ok = CheckExists(ref platform, dos, task, cli, frame,
                Text("SYS:S/Startup-Sequence"), 22, true) && ok;
            ok = CheckExists(ref platform, dos, task, cli, frame,
                Text("SYS:S"), 5, true) && ok;
            ok = CheckExists(ref platform, dos, task, cli, frame,
                Text("SYS:__missing__"), 15, false) && ok;
            ok = ShellScriptFrameCodec.TrySetPendingCommand(ref platform, frame, frame, 1, 0) && ok;
            ok = !platform.TryUnbindScriptFrame(cli, frame) && ok;
            ok = ShellScriptFrameCodec.TrySetPendingCommand(ref platform, frame, APTR.Null, 0, 0) && ok;
            ok = platform.TryUnbindScriptFrame(cli, frame) && ok;
            ok = DosProcessCodec.ReadShellPrivate(ref platform, task) == previousPrivate && ok;
        }
        platform.FreeRunnerMemory(frame, ShellScriptFrameCodec.Size);
        PublicShellFileServices.SetError(dos, (DOS.Error)previousError);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static bool CheckExists(ref DosShellNativePlatform platform, APTR dos,
        APTR task, APTR cli, APTR frame, APTR path, uint length, bool expected)
    {
        var window = DosProcessCodec.ReadWindowPointer(ref platform, task);
        var opened = platform.TryEvaluateIf(cli, (uint)ShellIfCondition.Exists,
            0, 0, 1, 0, path, length, APTR.Null, 0);
        var ok = opened && ShellScriptFrameCodec.TryRead(ref platform, frame, out var state) &&
            ((state.Flags & ShellScriptFrameFlags.ConditionFalse) == 0) == expected &&
            DosProcessCodec.ReadWindowPointer(ref platform, task) == window &&
            PublicShellFileServices.GetError(dos) == 0;
        if (opened) ok = platform.TryRequestShellControl(cli, ShellControlAction.EndIf, 0) && ok;
        return ok;
    }

    private static bool RunDiagnostics()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var before = DosCommandLineInterfaceCodec.Read(ref platform, cli);
        var status = new PublicShellPathPlatform(dos);
        var previousError = status.GetPathError();
        PublicShellFileServices.SetError(dos, DOS.Error.ObjectNotFound);
        var ok = platform.TryBeginCommandDiagnostics(cli) && status.GetPathError() == 0;
        var unchanged = DosCommandLineInterfaceCodec.Read(ref platform, cli);
        ok = unchanged.ReturnCode == before.ReturnCode && unchanged.Result2 == before.Result2 && ok;
        PublicShellFileServices.SetError(dos, DOS.Error.ObjectNotFound);
        ok = platform.TryCaptureCommandDiagnostics(cli, 10, out var captured) &&
            captured.ReturnCode == 10 && captured.IoError == (int)DOS.Error.ObjectNotFound && ok;
        ok = platform.TryCaptureCommandDiagnostics(cli, 0, out var success) &&
            success.ReturnCode == 0 && success.IoError == 0 &&
            status.GetPathError() == (int)DOS.Error.ObjectNotFound && ok;
        var published = new ShellCommandDiagnostics { ReturnCode = 10, IoError = (int)DOS.Error.ObjectNotFound };
        ok = platform.TryPublishCommandDiagnostics(cli, in published) &&
            platform.TryReadPublishedCommandDiagnostics(cli, out var actual) &&
            actual.ReturnCode == 10 && actual.IoError == (int)DOS.Error.ObjectNotFound &&
            status.GetPathError() == (int)DOS.Error.ObjectNotFound && ok;
        const uint ctrlC = 1u << 12, ctrlD = 1u << 13, other = 1u << 14, mask = ctrlC | ctrlD;
        var previousSignals = global::Amiga.Exec.SetSignal(0, mask | other) & (mask | other);
        _ = global::Amiga.Exec.SetSignal(mask | other, mask | other);
        ok = platform.TryPollScriptSignal(cli, out var signal) &&
            signal.Flags == (ShellScriptSignalFlags.Break | ShellScriptSignalFlags.CtrlC) &&
            signal.Sequence == ctrlC && signal.Result == (int)DOS.Error.Break && ok;
        ok = (global::Amiga.Exec.SetSignal(0, mask) & (mask | other)) == other && ok;
        _ = global::Amiga.Exec.SetSignal(ctrlD, mask);
        ok = platform.TryPollScriptSignal(cli, out signal) &&
            signal.Flags == ShellScriptSignalFlags.CtrlD && signal.Sequence == ctrlD && ok;
        _ = global::Amiga.Exec.SetSignal(previousSignals, mask | other);
        DosCommandLineInterfaceCodec.WriteReturnCode(ref platform, cli, before.ReturnCode);
        DosCommandLineInterfaceCodec.WriteResult2(ref platform, cli, before.Result2);
        PublicShellFileServices.SetError(dos, (DOS.Error)previousError);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static bool RunArguments()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var results = platform.AllocateRunnerMemory(16);
        if (results.IsNull) { global::Amiga.Exec.CloseLibrary(dos); return false; }
        var source = Text("\"S:My Startup\" 7 QUIET");
        var template = Text("FILE/A,COUNT/N,QUIET/S");
        PublicShellFileServices.SetError(dos, (DOS.Error)0x1234);
        var ok = platform.TryReadArgs(source, 22, template, 22, results, 12, out var owner);
        var status = new PublicShellPathPlatform(dos);
        if (owner.IsNotNull)
        {
            var file = APTR.FromPointer(platform.ReadUInt32(results));
            var number = APTR.FromPointer(platform.ReadUInt32(results, 4));
            ok = file.IsNotNull && number.IsNotNull && platform.ReadUInt8(file) == (byte)'S' &&
                platform.ReadUInt8(file, 1) == (byte)':' && platform.ReadUInt8(file, 4) == (byte)' ' &&
                platform.ReadUInt32(number) == 7 && platform.ReadUInt32(results, 8) != 0 && ok;
            platform.FreeArgs(owner);
            ok = status.GetPathError() == 0x1234 && ok;
        }
        platform.WriteUInt32(results, 0, 0xdeadbeef);
        platform.WriteUInt32(results, 4, 0xdeadbeef);
        platform.WriteUInt32(results, 8, 0xdeadbeef);
        ok = platform.TryReadArgs(Text("single"), 6, template, 22, results, 12, out owner) && ok;
        if (owner.IsNotNull)
        {
            ok = platform.ReadUInt32(results) != 0 && platform.ReadUInt32(results, 4) == 0 &&
                platform.ReadUInt32(results, 8) == 0 && ok;
            platform.FreeArgs(owner);
        }
        ok = platform.TryReadArgs(Text("solo"), 4, Text("NAME,STRING/F"), 13, results, 8, out owner) && ok;
        if (owner.IsNotNull)
        {
            ok = platform.ReadUInt32(results) != 0 && platform.ReadUInt32(results, 4) == 0 && ok;
            platform.FreeArgs(owner);
        }
        ok = platform.TryReadArgs(Text("solo \"\""), 7, Text("NAME,STRING/F"), 13, results, 8, out owner) && ok;
        if (owner.IsNotNull)
        {
            var final = APTR.FromPointer(platform.ReadUInt32(results, 4));
            ok = final.IsNotNull && platform.ReadUInt8(final) == 0 && ok;
            platform.FreeArgs(owner);
        }
        ok = platform.TryReadArgs(Text("one \"two words\" three"), 21, Text("VALUES/M"), 8,
            results, 4, out owner) && ok;
        if (owner.IsNotNull)
        {
            var list = APTR.FromPointer(platform.ReadUInt32(results));
            if (list.IsNull) ok = false;
            else
            {
                var second = APTR.FromPointer(platform.ReadUInt32(list, 4));
                ok = platform.ReadUInt32(list) != 0 && second.IsNotNull &&
                    platform.ReadUInt8(second) == (byte)'t' && platform.ReadUInt8(second, 3) == (byte)' ' &&
                    platform.ReadUInt32(list, 8) != 0 && platform.ReadUInt32(list, 12) == 0 && ok;
            }
            platform.FreeArgs(owner);
        }
        var prefix = Text("\"S:My Startup\" FIRST=one two");
        ok = platform.TryReadScriptFilePrefix(prefix, 28, out var consumed) &&
            consumed >= 14 && consumed <= 15 && ok;
        ok = platform.TryReadArgs(prefix, consumed, Text("FILE/A"), 6, results, 4, out owner) && ok;
        if (owner.IsNotNull)
        {
            var file = APTR.FromPointer(platform.ReadUInt32(results));
            ok = file.IsNotNull && platform.ReadUInt8(file) == (byte)'S' &&
                platform.ReadUInt8(file, 4) == (byte)' ' && ok;
            platform.FreeArgs(owner);
        }
        ok = !platform.TryReadScriptFilePrefix(APTR.Null, 0, out consumed) && consumed == 0 && ok;
        ok = !platform.TryReadScriptFilePrefix(Text("\"unterminated"), 13, out consumed) && ok;
        var missing = platform.TryReadArgs(APTR.Null, 0, Text("FILE/A"), 6, results, 4, out owner);
        ok = !missing && owner.IsNull && status.GetPathError() == (int)DOS.Error.RequiredArgumentMissing && ok;
        if (owner.IsNotNull) platform.FreeArgs(owner);
        platform.FreeRunnerMemory(results, 16);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok;
    }

    private static bool Run()
    {
        var platform = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref platform, task).Address;
        var taskNumber = DosProcessCodec.ReadTaskNumber(ref platform, task);
        var expectedNumber = taskNumber > 0 ? unchecked((uint)taskNumber) : 0;
        if (platform.CurrentShellNumber(cli) != expectedNumber ||
            platform.CurrentShellNumber(APTR.Null) != 0) return false;
        #if COPPEROS_PROBE_SYS
        var name = Text("SYS:T/io-test");
        const uint nameLength = 13;
        #else
        var name = Text("DH0:io-test");
        const uint nameLength = 11;
        #endif
        var buffer = platform.AllocateRunnerMemory(16);
        if (buffer.IsNull) return false;
        var ok = platform.TryOpenScriptOutput(cli, name, nameLength, 0, out var file);
        if (file.IsNotNull)
        {
            ok = platform.Write(file, Text("AB\r\nC\n"), 6) == 6 && ok;
            ok = platform.TryCloseScriptRedirection(cli, file) && ok;
        }
        ok = platform.TryOpenScriptOutput(cli, name, nameLength, 1, out file) && ok;
        if (file.IsNotNull)
        {
            ok = platform.WriteByte(file, (byte)'Z') == (byte)'Z' && ok;
            ok = platform.TryCloseScriptRedirection(cli, file) && ok;
        }
        ok = platform.TryOpenScriptInput(cli, name, nameLength, out file) && ok;
        if (file.IsNotNull)
        {
            uint line = 1, offset = 0;
            ok = platform.TryReadScriptLine(cli, file, line, offset, buffer, 16,
                out var length, out line, out offset, out var eof) &&
                length == 2 && line == 2 && offset == 4 && eof == 0 && ok;
            ok = platform.TryReadScriptLine(cli, file, line, offset, buffer, 16,
                out length, out line, out offset, out eof) &&
                length == 1 && line == 3 && offset == 6 && eof == 0 && ok;
            ok = platform.TryReadScriptLine(cli, file, line, offset, buffer, 16,
                out length, out line, out offset, out eof) &&
                length == 1 && line == 4 && offset == 7 && eof == 0 && ok;
            ok = platform.TryReadScriptLine(cli, file, line, offset, buffer, 16,
                out length, out line, out offset, out eof) && eof == 1 && ok;
            ok = platform.TryCloseScriptRedirection(cli, file) && ok;
        }
        ok = platform.TryDeleteScriptPath(cli, name, nameLength) && ok;
        platform.FreeRunnerMemory(buffer, 16);
        return ok;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
