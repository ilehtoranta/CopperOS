using Amiga;
using CopperOS.Shell;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>Runs the actual public background launcher with a continuation.</summary>
public static class PublicContinuationLaunchNativeProbe
{
    private const uint NameBytes = PublicShellTaskIdentity.NameBytes;

    [M68kEntryPoint]
    [M68kExport("copperos.shell.continuation-launch-probe")]
    public static int Main()
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return 20;
        var memory = new CopperSharpNativeDosPlatform();
        var entry = APTR.ExportAddress("copperos.shell.background");
        if (PublicShellBackgroundCore.IsPermanentShell(ref memory, dos, entry))
        {
            var result = Exercise(dos);
            global::Amiga.Exec.CloseLibrary(dos);
            return result ? 0 : 10;
        }
        var residentName = Text("CopperOS-Shell-Image");
        var ok = PublicShellResidentVectors.FindSegment(dos, residentName, APTR.Null, 1).IsNull;
        var image = ok ? NativeForegroundDosVectors.LoadSeg(dos, Text("SYS:C/runnerprobe")) : BPTR.Null;
        if (ok && image.IsNull) image = NativeForegroundDosVectors.LoadSeg(dos, Text("DF1:LongPath"));
        // This test registers a permanent system segment. Its lifetime is
        // the disposable guest boot, as on a normally installed system Shell.
        var registered = image.IsNotNull && PublicShellResidentVectors.AddSegment(dos, residentName, image, -1) != 0;
        if (registered)
        {
            var text = Text("\n");
            ok = NativeForegroundDosVectors.RunCommand(dos, image, 65_536, text, 1) == 0;
            var resident = PublicShellResidentVectors.FindSegment(dos, residentName, APTR.Null, 1);
            if (resident.IsNull || DosSegmentCodec.ReadUseCount(ref memory, resident) != -1) ok = false;
        }
        else
        {
            ok = false;
            if (image.IsNotNull) NativeForegroundDosVectors.UnLoadSeg(dos, image);
        }
        var marker = Text(ok ? (CString)"SYS:runner-ok" : (CString)"SYS:runner-fail");
        var file = PublicShellFileServices.OpenOutput(dos, marker, Length(marker), 0);
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        global::Amiga.Exec.CloseLibrary(dos);
        return ok ? 0 : 10;
    }

    private static bool Exercise(APTR dos)
    {
        var shell = new DosShellNativePlatform(APTR.Null);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var cliValue = DosCommandLineInterfaceCodec.Read(ref shell, cli);
        var record = Allocate(ShellProcessContinuationCodec.Size);
        var name = Allocate(NameBytes);
        var outputPath = Text("SYS:T/continuation-output");
        _ = PublicShellFileServices.Delete(dos, outputPath);
        var stages = 0u;
        var initial = new ShellProcessContinuation { ParentCli = cli, State = ShellProcessContinuationState.Running };
        var ok = record.IsNotNull && name.IsNotNull &&
            ShellProcessContinuationCodec.Initialize(ref shell, record, in initial);
        if (ok) stages |= 1;
        var parentDirectory = BPTR.FromRaw(shell.ReadUInt32(task, DosLayout.Process.CurrentDirectory));
        var directory = PublicShellPathVectors.Lock(dos, Text("SYS:S"), -2);
        ok = directory.IsNotNull && (parentDirectory.IsNull ||
            PublicShellPathVectors.SameLock(dos, parentDirectory, directory) != 0) && ok;
        var script = Text("SYS:T/continuation-directory-script");
        var relativeOutput = Text("SYS:S/continuation-lock-output");
        var scriptText = Text("CD >SYS:T/continuation-output\nEcho PASS >continuation-lock-output\n");
        _ = PublicShellFileServices.Delete(dos, relativeOutput);
        var scriptFile = PublicShellFileServices.OpenOutput(dos, script, Length(script), 0);
        if (scriptFile.IsNull) ok = false;
        else
        {
            var written = PublicShellFileServices.Write(dos, scriptFile, scriptText, Length(scriptText));
            var closed = PublicShellFileServices.Close(dos, scriptFile);
            ok = written == (int)Length(scriptText) && closed != 0 && ok;
        }
        var command = Text("Execute SYS:T/continuation-directory-script");
        var launched = ok && shell.TryRunCommand(cli, cliValue.CurrentInput, cliValue.CurrentOutput,
            DosProcessCodec.ReadCurrentError(ref shell, task), 0, directory, record,
            command, Length(command), 0, 0, 0, 0, 0, 0);
        ok = BPTR.FromRaw(shell.ReadUInt32(task, DosLayout.Process.CurrentDirectory)) == parentDirectory && ok;
        ok = launched && ok;
        if (launched) stages |= 2;
        if (launched && ShellProcessState.TryGetContinuationHead(ref shell, cli, out var present, out var owner) &&
            present && owner.IsNotNull)
        {
            stages |= 4;
            var taskName = PublicShellContinuationCore.TaskName(owner);
            for (var i = 0; i < (int)NameBytes; i++) APTR.WriteUInt8(name, i, APTR.ReadUInt8(taskName, i));
            var completed = false;
            var pollingFailed = false;
            // Loading and executing the extra script from floppy can exceed
            // ten seconds on CopperStart; use the same bound as the control.
            for (var attempt = 0; attempt < 3000 && !completed; attempt++)
            {
                if (!ShellProcessContinuationPolling.TryPoll(ref shell, cli, record, out var state, out var result))
                { pollingFailed = true; stages |= 0x10000000; ok = false; break; }
                if (state == ShellProcessContinuationState.Completed)
                {
                    completed = true;
                    stages |= 8;
                    var retired = result == 0 && shell.TryReadContinuationDiagnostics(cli, record, out var diagnostics) &&
                        diagnostics.ReturnCode == 0 && ShellProcessContinuationTeardown.TryRelease(ref shell, cli, record);
                    if (retired) stages |= 16;
                    ok = retired && ok;
                }
                else ChildProbeDosVectors.Delay(dos, 1);
            }
            if (!completed)
            {
                if (!pollingFailed) stages |= 0x20000000;
                ok = false;
                _ = PublicShellContinuationCore.DetachAll(ref shell, cli);
                // The resident copy cannot be unloaded while the worker uses it.
                while (global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw)).IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
            }
        }
        else if (launched)
        {
            // Without the identity we cannot establish that the worker has
            // stopped using the resident copy. Let the harness time out
            // instead of returning and unloading executable code underneath it.
            while (true) ChildProbeDosVectors.Delay(dos, 1);
        }
        var buffer = Allocate(32);
        var file = PublicShellFileServices.OpenInput(dos, outputPath, Length(outputPath));
        if (buffer.IsNull || file.IsNull) ok = false;
        else
        {
            var outputOk = PublicShellFileServices.ReadLine(dos, file, buffer, 32, out var length, out var eof) &&
                eof == 0 && SameText(buffer, length, Text("CopperOS:S"));
            var relative = PublicShellFileServices.OpenInput(dos, relativeOutput, Length(relativeOutput));
            if (relative.IsNull) outputOk = false;
            else
            {
                outputOk = PublicShellFileServices.ReadLine(dos, relative, buffer, 32, out var relativeLength,
                    out var relativeEof) && relativeEof == 0 && relativeLength == 4 &&
                    APTR.ReadUInt32(buffer, 0) == 0x50415353 && outputOk;
                _ = PublicShellFileServices.Close(dos, relative);
            }
            if (outputOk) stages |= 32;
            ok = outputOk && ok;
        }
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        if (ok) ok = ExerciseNewShell(ref shell, dos, cli, cliValue, directory, record, name,
            ShellLaunchKind.NewShell, ref stages);
        if (ok) ok = ExerciseNewShell(ref shell, dos, cli, cliValue, directory, record, name,
            ShellLaunchKind.NewCli, ref stages);
        ok = BPTR.FromRaw(shell.ReadUInt32(task, DosLayout.Process.CurrentDirectory)) == parentDirectory && ok;
        if (directory.IsNotNull) PublicShellPathVectors.UnLock(dos, directory);
        if (ok)
        {
            var control = ExerciseFixtureControl(ref shell, dos, cli, cliValue, record, name, ref stages);
            var detached = ExerciseDetached(dos, name, false, ref stages);
            var detachedOwnedInput = ExerciseDetached(dos, name, true, ref stages);
            ok = control && detached && detachedOwnedInput;
        }
        if (ok && ExerciseOwnedRun(ref shell, dos, cli, cliValue, name)) stages |= 0x40000000;
        else ok = false;
        if (buffer.IsNotNull)
        {
            APTR.WriteUInt32(buffer, 0, stages);
            var stagePath = Text("SYS:T/continuation-launch.bin");
            var stageFile = PublicShellFileServices.OpenOutput(dos, stagePath, Length(stagePath), 0);
            if (stageFile.IsNotNull)
            {
                _ = PublicShellFileServices.Write(dos, stageFile, buffer, 4);
                _ = PublicShellFileServices.Close(dos, stageFile);
            }
            global::Amiga.Exec.FreeMem(buffer.Raw, 32);
        }
        if (ok) _ = PublicShellFileServices.Delete(dos, outputPath);
        if (ok) _ = PublicShellFileServices.Delete(dos, relativeOutput);
        _ = PublicShellFileServices.Delete(dos, script);
        if (name.IsNotNull) global::Amiga.Exec.FreeMem(name.Raw, NameBytes);
        if (record.IsNotNull) global::Amiga.Exec.FreeMem(record.Raw, ShellProcessContinuationCodec.Size);
        return ok;
    }

    private static bool ExerciseOwnedRun(ref DosShellNativePlatform shell, APTR dos, APTR cli,
        CommandLineInterface cliValue, APTR name)
    {
        var frame = Allocate(ShellScriptFrameCodec.Size);
        var frameValue = new ShellScriptFrameState { Parent = cli, Cli = cli,
            Input = cliValue.CurrentInput, CurrentLine = 1, Flags = ShellScriptFrameFlags.Active };
        var bound = frame.IsNotNull &&
            ShellScriptFrameCodec.Initialize(ref shell, frame, in frameValue) &&
            ShellRunnerBinding.TryPublishFrame(ref shell, cli, frame, out _);
        var owned = bound ? ShellFrameResources.AllocateContinuation(ref shell, frame, 0, out _) : APTR.Null;
        var initial = new ShellProcessContinuation { ParentCli = cli, State = ShellProcessContinuationState.Running,
            Flags = (uint)ShellProcessContinuationFlags.RecordOwned };
        var path = Text("SYS:T/continuation-owned");
        var command = Text("Echo >SYS:T/continuation-owned owned");
        _ = PublicShellFileServices.Delete(dos, path);
        var initialized = owned.IsNotNull && ShellProcessContinuationCodec.Initialize(ref shell, owned, in initial);
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        // A standard V40 launch uses the Shell-owned process state even when
        // the caller also carries an unusable legacy context sentinel.
        var mixed = new DosShellNativePlatform(APTR.FromPointer(4));
        var launched = initialized && mixed.TryRunCommand(cli, cliValue.CurrentInput, cliValue.CurrentOutput,
            DosProcessCodec.ReadCurrentError(ref shell, task), 0, BPTR.Null, owned,
            command, Length(command), 0, 0, 0, 0, 0, 0);
        var ok = false;
        if (launched && ShellProcessState.TryGetContinuationHead(ref shell, cli, out var present, out var owner) &&
            present && owner.IsNotNull)
        {
            for (var i = 0; i < (int)NameBytes; i++)
                APTR.WriteUInt8(name, i, APTR.ReadUInt8(PublicShellContinuationCore.TaskName(owner), i));
            var completed = false;
            for (var attempt = 0; attempt < 3000 && !completed; attempt++)
            {
                if (!ShellProcessContinuationPolling.TryPoll(ref shell, cli, owned, out var state, out var result)) break;
                if (state == ShellProcessContinuationState.Completed)
                {
                    completed = true;
                    ok = result == 0 && ShellProcessContinuationTeardown.TryRelease(ref shell, cli, owned) &&
                        !ShellFrameResources.IsContinuationOwned(ref shell, frame, owned);
                }
                else ChildProbeDosVectors.Delay(dos, 1);
            }
            if (!completed)
            {
                _ = PublicShellContinuationCore.Detach(ref shell, cli, owned);
                while (global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw)).IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
            }
        }
        else if (launched)
        {
            while (true) ChildProbeDosVectors.Delay(dos, 1);
        }
        else if (owned.IsNotNull) _ = ShellFrameResources.FreeContinuation(ref shell, frame, owned);
        var buffer = Allocate(16);
        var file = PublicShellFileServices.OpenInput(dos, path, Length(path));
        ok = ok && buffer.IsNotNull && file.IsNotNull &&
            PublicShellFileServices.ReadLine(dos, file, buffer, 16, out var count, out var eof) &&
            eof == 0 && SameText(buffer, count, Text("owned"));
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        if (buffer.IsNotNull) global::Amiga.Exec.FreeMem(buffer.Raw, 16);
        _ = PublicShellFileServices.Delete(dos, path);
        if (bound) ok = ShellRunnerBinding.TryRemoveFrame(ref shell, cli, frame) && ok;
        if (frame.IsNotNull) global::Amiga.Exec.FreeMem(frame.Raw, ShellScriptFrameCodec.Size);
        return ok;
    }

    private static bool ExerciseFixtureControl(ref DosShellNativePlatform shell, APTR dos, APTR cli,
        CommandLineInterface cliValue, APTR record, APTR name, ref uint stages)
    {
        var command = Text("SYS:fixture");
        var lockValue = PublicShellFileServices.Lock(dos, command);
        if (lockValue.IsNull)
        {
            command = Text("DF1:fixture");
            lockValue = PublicShellFileServices.Lock(dos, command);
        }
        if (lockValue.IsNull) return false;
        PublicShellFileServices.UnLock(dos, lockValue);
        stages |= 131072;
        var path = Text("SYS:T/continuation-control");
        var output = PublicShellFileServices.OpenOutput(dos, path, Length(path), 0);
        var initial = new ShellProcessContinuation { ParentCli = cli, State = ShellProcessContinuationState.Running };
        var mixed = new DosShellNativePlatform(APTR.FromPointer(4));
        var initialized = output.IsNotNull && ShellProcessContinuationCodec.Initialize(ref shell, record, in initial);
        var rejected = initialized &&
            !mixed.TryRunCommand(cli, cliValue.CurrentInput, cliValue.CurrentOutput, output, 4,
                BPTR.Null, record, command, Length(command), 0, 0, 63, 1, 0, 0) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.BadNumber &&
            !mixed.TryRunCommand(cli, cliValue.CurrentInput, cliValue.CurrentOutput, output, 4,
                BPTR.Null, record, command, Length(command), 0, 0, 0, 0, 128, 1) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.BadNumber &&
            !mixed.TryRunCommand(cli, cliValue.CurrentInput, cliValue.CurrentOutput, output, 4,
                BPTR.Null, record, command, Length(command), 0, 0, 0, 2, 0, 0) &&
            PublicShellFileServices.GetError(dos) == (int)DOS.Error.BadNumber;
        var launched = rejected &&
            mixed.TryRunCommand(cli, cliValue.CurrentInput, cliValue.CurrentOutput, output, 4,
                BPTR.Null, record, command, Length(command), 0, 0, 98_304, 1, -3, 1);
        var result = (int)ShellCommandResult.Error;
        var error = PublicShellFileServices.GetError(dos);
        var ok = launched;
        if (launched) stages |= 262144;
        if (launched && ShellProcessState.TryGetContinuationHead(ref shell, cli, out var present, out var owner) &&
            present && owner.IsNotNull)
        {
            stages |= 524288;
            var taskName = PublicShellContinuationCore.TaskName(owner);
            for (var i = 0; i < (int)NameBytes; i++) APTR.WriteUInt8(name, i, APTR.ReadUInt8(taskName, i));
            var child = global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw));
            var childCli = child.IsNull ? APTR.Null : DosProcessCodec.ReadCommandLineInterface(ref shell, child).Address;
            var parent = global::Amiga.Exec.FindTask(CString.FromPointer(0));
            ok = child.IsNotNull && childCli.IsNotNull &&
                shell.ReadUInt8(child, ExecLayout.Node.Priority) == unchecked((byte)-3) &&
                ShellCliState.TryReadDefaultStack(ref shell, childCli, out var childStack) && childStack == 98_304 &&
                shell.ReadUInt32(child, ExecLayout.Task.StackUpper) -
                    shell.ReadUInt32(child, ExecLayout.Task.StackLower) >= 98_304 &&
                DosCommandLineInterfaceCodec.Read(ref shell, cli).DefaultStack == cliValue.DefaultStack &&
                child != parent && ok;
            var completed = false;
            for (var attempt = 0; attempt < 3000 && !completed; attempt++)
            {
                if (!ShellProcessContinuationPolling.TryPoll(ref shell, cli, record, out var state, out result))
                { ok = false; break; }
                if (state == ShellProcessContinuationState.Completed)
                {
                    completed = true;
                    stages |= 1048576;
                    var diagnosed = shell.TryReadContinuationDiagnostics(cli, record, out var diagnostics);
                    if (diagnosed) error = diagnostics.IoError;
                    var released = ShellProcessContinuationTeardown.TryRelease(ref shell, cli, record);
                    if (diagnosed && released) stages |= 2097152;
                    ok = result == 0 && diagnosed && released && ok;
                }
                // This parent has priority zero and the worker has priority
                // -3. Give the worker a substantial blocked interval. The
                // CopperStart capture with one-tick polls left this child at
                // its initial PC; this options test must let the lower-priority
                // worker execute, rather than keep its parent runnable.
                else ChildProbeDosVectors.Delay(dos, 25);
            }
            if (!completed)
            {
                ok = false;
                _ = PublicShellContinuationCore.DetachAll(ref shell, cli);
                while (global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw)).IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 25);
            }
        }
        else if (launched)
        {
            // Do not lose the only identity of a running worker.
            while (true) ChildProbeDosVectors.Delay(dos, 1);
        }
        else if (output.IsNotNull) _ = PublicShellFileServices.Close(dos, output);
        var buffer = Allocate(16);
        if (buffer.IsNotNull)
        {
            APTR.WriteUInt32(buffer, 0, unchecked((uint)result));
            APTR.WriteUInt32(buffer, 4, unchecked((uint)error));
            var diag = Text("SYS:T/continuation-control.bin");
            var file = PublicShellFileServices.OpenOutput(dos, diag, Length(diag), 0);
            if (file.IsNotNull)
            {
                _ = PublicShellFileServices.Write(dos, file, buffer, 8);
                _ = PublicShellFileServices.Close(dos, file);
            }
            file = PublicShellFileServices.OpenInput(dos, path, Length(path));
            var outputOk = file.IsNotNull && PublicShellFileServices.ReadLine(dos, file, buffer, 16,
                out var length, out var eof) && eof == 0 && length == 4 && APTR.ReadUInt32(buffer, 0) == 0x50415353;
            if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
            if (outputOk) stages |= 4194304;
            ok = outputOk && ok;
            global::Amiga.Exec.FreeMem(buffer.Raw, 16);
        }
        else ok = false;
        if (ok) _ = PublicShellFileServices.Delete(dos, path);
        return ok;
    }

    private static bool ExerciseDetached(APTR dos, APTR name, bool independentInput, ref uint stages)
    {
        var output = independentInput ? Text("SYS:T/detached-owned") : Text("SYS:T/detached-continuation");
        var shift = independentInput ? 11 : 0;
        _ = PublicShellFileServices.Delete(dos, output);
        _ = PublicShellFileServices.Delete(dos, Text("SYS:T/continuation-diag"));
        var fixture = Text("SYS:fixture");
        var lockValue = PublicShellFileServices.Lock(dos, fixture);
        if (lockValue.IsNull)
        {
            fixture = Text("DF1:fixture");
            lockValue = PublicShellFileServices.Lock(dos, fixture);
        }
        if (lockValue.IsNull) return false;
        PublicShellFileServices.UnLock(dos, lockValue);
        var message = Allocate(64);
        var tags = Allocate(40);
        var reply = global::Amiga.Exec.CreateMsgPort();
        var ok = message.IsNotNull && tags.IsNotNull && reply.IsNotNull;
        if (ok)
        {
            APTR.WriteUInt32(message, ExecLayout.Message.ReplyPort, reply.Raw);
            APTR.WriteUInt16(message, ExecLayout.Message.Length, 64);
            APTR.WriteUInt32(message, 20, fixture.Raw);
            APTR.WriteUInt32(message, 56, independentInput ? 1u : 0u);
            Tag(tags, 0, DosNewProcessTag.Entry, APTR.ExportAddress("copperos.shell.continuation-exit-parent").Raw);
            Tag(tags, 1, DosNewProcessTag.Cli, 1);
            Tag(tags, 2, DosNewProcessTag.StackSize, 65_536);
            Tag(tags, 3, DosNewProcessTag.Name, Text("CopperOS Continuation Exit Parent").Raw);
            var parent = ChildProbeDosVectors.CreateNewProc(dos, tags);
            if (parent.IsNull) ok = false;
            else
            {
                stages |= 4096u << shift;
                global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(parent), message);
                _ = global::Amiga.Exec.WaitPort(reply);
                ok = global::Amiga.Exec.GetMsg(reply) == message && APTR.ReadUInt32(message, 20) == 1;
                if (ok)
                {
                    stages |= 8192u << shift;
                    for (var i = 0; i < (int)NameBytes; i++) APTR.WriteUInt8(name, i, APTR.ReadUInt8(message, 24 + i));
                }
                while (global::Amiga.Exec.FindTask((CString)"CopperOS Continuation Exit Parent").IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
                stages |= 16384u << shift;
                if (ok)
                {
                    while (global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw)).IsNotNull)
                        ChildProbeDosVectors.Delay(dos, 1);
                    stages |= 32768u << shift;
                }
            }
        }
        if (reply.IsNotNull) global::Amiga.Exec.DeleteMsgPort(reply);
        if (tags.IsNotNull) global::Amiga.Exec.FreeMem(tags.Raw, 40);
        if (message.IsNotNull) global::Amiga.Exec.FreeMem(message.Raw, 64);
        var buffer = Allocate(32);
        var file = PublicShellFileServices.OpenInput(dos, output, Length(output));
        if (buffer.IsNull || file.IsNull) ok = false;
        else
        {
            var outputOk = PublicShellFileServices.ReadLine(dos, file, buffer, 32, out var length, out var eof) &&
                eof == 0 && length == 4 && APTR.ReadUInt32(buffer, 0) == 0x50415353;
            if (outputOk) stages |= 65536u << shift;
            ok = outputOk && ok;
        }
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        if (buffer.IsNotNull) global::Amiga.Exec.FreeMem(buffer.Raw, 32);
        if (ok) _ = PublicShellFileServices.Delete(dos, output);
        return ok;
    }

    [M68kExport("copperos.shell.continuation-exit-parent")]
    public static uint ExitParent()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        var command = APTR.FromPointer(APTR.ReadUInt32(message, 20));
        var independentInput = APTR.ReadUInt32(message, 56) != 0;
        var shell = new DosShellNativePlatform(APTR.Null);
        var cli = DosProcessCodec.ReadCommandLineInterface(ref shell, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var state = APTR.Null;
        var record = Allocate(ShellProcessContinuationCodec.Size);
        var output = BPTR.Null;
        var input = BPTR.Null;
        var launched = false;
        var ok = dos.IsNotNull && record.IsNotNull && ShellProcessState.TryInstall(ref shell, cli, out state);
        if (ok)
        {
            var path = independentInput ? Text("SYS:T/detached-owned") : Text("SYS:T/detached-continuation");
            output = PublicShellFileServices.OpenOutput(dos, path, Length(path), 0);
            var initial = new ShellProcessContinuation { ParentCli = cli, State = ShellProcessContinuationState.Running };
            var current = DosCommandLineInterfaceCodec.Read(ref shell, cli);
            input = independentInput ? PublicShellFileServices.OpenInput(dos, Text("NIL:"), 4) : current.CurrentInput;
            launched = output.IsNotNull && (!independentInput || input.IsNotNull) &&
                ShellProcessContinuationCodec.Initialize(ref shell, record, in initial) &&
                shell.TryRunCommand(cli, input, current.CurrentOutput, output, independentInput ? 5u : 4u,
                    BPTR.Null, record, command, Length(command), 0, 0, 0, 0, 0, 0);
            var owner = APTR.Null;
            ok = launched && ShellProcessState.TryGetContinuationHead(ref shell, cli, out var present, out owner) &&
                present && owner.IsNotNull;
            if (ok)
            {
                var taskName = PublicShellContinuationCore.TaskName(owner);
                for (var i = 0; i < (int)NameBytes; i++) APTR.WriteUInt8(message, 24 + i, APTR.ReadUInt8(taskName, i));
            }
            ok = PublicShellContinuationCore.DetachAll(ref shell, cli) && ok;
            ok = ShellProcessState.TryRemove(ref shell, cli, state) && ok;
        }
        if (!launched && output.IsNotNull) _ = PublicShellFileServices.Close(dos, output);
        if (!launched && independentInput && input.IsNotNull) _ = PublicShellFileServices.Close(dos, input);
        if (record.IsNotNull) global::Amiga.Exec.FreeMem(record.Raw, ShellProcessContinuationCodec.Size);
        if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
        APTR.WriteUInt32(message, 20, ok ? 1u : 0u);
        global::Amiga.Exec.ReplyMsg(message);
        return ok ? 0u : 10u;
    }

    private static bool ExerciseNewShell(ref DosShellNativePlatform shell, APTR dos, APTR cli,
        CommandLineInterface cliValue, BPTR directory, APTR record, APTR name,
        ShellLaunchKind kind, ref uint stages)
    {
        var startup = Text("SYS:T/continuation-startup");
        var output = Text("SYS:T/newshell-continuation");
        var relativeOutput = Text("SYS:S/newshell-lock-output");
        var text = Text("CD >SYS:T/newshell-continuation\nEcho PASS >newshell-lock-output\nEndCLI\n");
        _ = PublicShellFileServices.Delete(dos, output);
        _ = PublicShellFileServices.Delete(dos, relativeOutput);
        var file = PublicShellFileServices.OpenOutput(dos, startup, Length(startup), 0);
        var ready = file.IsNotNull;
        if (file.IsNotNull)
        {
            ready = PublicShellFileServices.Write(dos, file, text, Length(text)) == (int)Length(text);
            ready = PublicShellFileServices.Close(dos, file) != 0 && ready;
        }
        if (ready) stages |= 64;
        var initial = new ShellProcessContinuation { ParentCli = cli, State = ShellProcessContinuationState.Running };
        var mixed = new DosShellNativePlatform(APTR.FromPointer(4));
        var launched = ready && ShellProcessContinuationCodec.Initialize(ref shell, record, in initial) &&
            mixed.TryCreateShell(cli, kind, cliValue.CurrentInput,
                cliValue.CurrentOutput, BPTR.Null, directory, record, APTR.Null, 0, startup, Length(startup));
        if (launched) stages |= 128;
        var ok = launched;
        if (launched && ShellProcessState.TryGetContinuationHead(ref shell, cli, out var present, out var owner) &&
            present && owner.IsNotNull)
        {
            stages |= 256;
            var taskName = PublicShellContinuationCore.TaskName(owner);
            for (var i = 0; i < (int)NameBytes; i++) APTR.WriteUInt8(name, i, APTR.ReadUInt8(taskName, i));
            var completed = false;
            for (var attempt = 0; attempt < 3000 && !completed; attempt++)
            {
                if (!ShellProcessContinuationPolling.TryPoll(ref shell, cli, record, out var state, out var result))
                { ok = false; break; }
                if (state == ShellProcessContinuationState.Completed)
                {
                    completed = true;
                    stages |= 512;
                    var retired = result == 0 && shell.TryReadContinuationDiagnostics(cli, record, out var diagnostics) &&
                        diagnostics.ReturnCode == 0 && ShellProcessContinuationTeardown.TryRelease(ref shell, cli, record);
                    if (retired) stages |= 1024;
                    ok = retired && ok;
                }
                else ChildProbeDosVectors.Delay(dos, 1);
            }
            if (!completed)
            {
                ok = false;
                _ = PublicShellContinuationCore.DetachAll(ref shell, cli);
                while (global::Amiga.Exec.FindTask(CString.FromPointer(name.Raw)).IsNotNull)
                    ChildProbeDosVectors.Delay(dos, 1);
            }
        }
        else if (launched)
        {
            while (true) ChildProbeDosVectors.Delay(dos, 1);
        }
        var buffer = Allocate(32);
        file = PublicShellFileServices.OpenInput(dos, output, Length(output));
        if (buffer.IsNull || file.IsNull) ok = false;
        else
        {
            var outputOk = PublicShellFileServices.ReadLine(dos, file, buffer, 32, out var length, out var eof) &&
                eof == 0 && SameText(buffer, length, Text("CopperOS:S"));
            var relative = PublicShellFileServices.OpenInput(dos, relativeOutput, Length(relativeOutput));
            if (relative.IsNull) outputOk = false;
            else
            {
                outputOk = PublicShellFileServices.ReadLine(dos, relative, buffer, 32, out var relativeLength,
                    out var relativeEof) && relativeEof == 0 && relativeLength == 4 &&
                    APTR.ReadUInt32(buffer, 0) == 0x50415353 && outputOk;
                _ = PublicShellFileServices.Close(dos, relative);
            }
            if (outputOk) stages |= 2048;
            ok = outputOk && ok;
        }
        if (file.IsNotNull) _ = PublicShellFileServices.Close(dos, file);
        if (buffer.IsNotNull) global::Amiga.Exec.FreeMem(buffer.Raw, 32);
        _ = PublicShellFileServices.Delete(dos, startup);
        if (ok) _ = PublicShellFileServices.Delete(dos, output);
        if (ok) _ = PublicShellFileServices.Delete(dos, relativeOutput);
        return ok;
    }

    private static APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, (uint)tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
    private static APTR Text(CString text) => APTR.FromPointer(CString.ToUInt32(text));
    private static uint Length(APTR text)
    { uint length = 0; while (APTR.ReadUInt8(text, (int)length) != 0) length++; return length; }
    private static bool SameText(APTR actual, uint length, APTR expected)
    {
        if (length != Length(expected)) return false;
        for (var i = 0u; i < length; i++)
            if (APTR.ReadUInt8(actual, (int)i) != APTR.ReadUInt8(expected, (int)i)) return false;
        return true;
    }
}
