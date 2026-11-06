using System.Runtime.CompilerServices;
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Starts a Shell-owned background runner using public DOS and Exec.</summary>
internal static class PublicShellBackgroundCore
{
    private const uint HeaderBytes = 52;
    private const uint NoticeBytes = 24;
    private const uint DirectoryBytes = 256;
    private const uint TagBytes = 96;
    private const uint StackBytes = 65536;

    public static bool Start(APTR cli, BPTR input, BPTR output, BPTR error, uint ownedStreams,
        APTR command, uint length, APTR continuation, BPTR currentDirectory) =>
        Start(cli, input, output, error, ownedStreams, command, length, continuation,
            currentDirectory, 0, 0, 0, 0);

    public static bool Start(APTR cli, BPTR input, BPTR output, BPTR error, uint ownedStreams,
        APTR command, uint length, APTR continuation, BPTR currentDirectory,
        uint stack, uint stackPresent, int priority, uint priorityPresent)
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        if (stackPresent > 1 || priorityPresent > 1 ||
            (stackPresent != 0 && (stack < 64 || stack > 16u * 1024u * 1024u)) ||
            (priorityPresent != 0 && (priority < -128 || priority > 127)))
        {
            PublicShellFileServices.SetError(dos, DOS.Error.BadNumber);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var memory = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (cli.IsNull || task.IsNull || !DosProcessCodec.IsMapped(ref memory, task) ||
            DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address != cli ||
            (ownedStreams & ~7u) != 0 || command.IsNull || length > 65535 || command.Raw > uint.MaxValue - length ||
            (length != 0 && !memory.IsMapped(command, length)))
        {
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var entry = APTR.ExportAddress("copperos.shell.background");
        var hasDirectory = continuation.IsNotNull && currentDirectory.IsNotNull;
        var directoryOffset = HeaderBytes + length + 1 + NoticeBytes;
        var bytes = directoryOffset + (hasDirectory ? DirectoryBytes : 0);
        var message = Allocate(bytes);
        var tags = Allocate(TagBytes);
        if (message.IsNull || tags.IsNull)
        {
            Free(message, bytes);
            Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, DOS.Error.NoFreeStore);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        // RunCommand temporarily replaces the buffer of its current input
        // handle. A background command must not make that change on the
        // interactive Shell's own console handle. Reopen the same console
        // handler through the public DOS "*" name, giving the worker its own
        // file handle and buffer while preserving the underlying console.
        var privateInput = BPTR.Null;
        var launchInput = input;
        // V40 RUN writes its notice to the launcher's redirected output.
        // An explicit NIL: is passed on; otherwise the child gets a separate
        // console or NIL: handle instead of the caller's standard output.
        var standardOutput = DosCommandLineInterfaceCodec.Read(ref memory, cli).StandardOutput;
        var console = DosProcessCodec.ReadConsoleTask(ref memory, task);
        var ownsOutput = (ownedStreams & 2) != 0;
        var nilOutput = ownsOutput && output.IsNotNull &&
            DosFileHandleCodec.IsMapped(ref memory, output.Address) &&
            memory.ReadUInt32(output.Address, DosLayout.FileHandle.Type) == 0;
        if ((ownedStreams & 1) == 0 && input.IsNotNull &&
            DosFileHandleCodec.IsMapped(ref memory, input.Address))
        {
            var handler = APTR.FromPointer(memory.ReadUInt32(input.Address, DosLayout.FileHandle.Type));
            if (handler.IsNull)
            {
                // NIL: has no handler port. Reopen it so a background worker
                // does not borrow a handle the launching Process may close.
                privateInput = PublicShellFileServices.OpenInput(dos, Text("NIL:"), 4);
                if (privateInput.IsNull)
                {
                    var openFailure = PublicShellFileServices.GetError(dos);
                    Free(message, bytes);
                    Free(tags, TagBytes);
                    PublicShellFileServices.SetError(dos, (DOS.Error)openFailure);
                    global::Amiga.Exec.CloseLibrary(dos);
                    return false;
                }
                launchInput = privateInput;
            }
            else if (
                PublicShellFileServices.IsInteractive(dos, input) != 0)
            {
                // "*" is resolved through the caller's console task. The
                // input can belong to a different handler, or the process
                // may not yet have a console task; select its handler only
                // for the open and restore the caller's public process state.
                var changeConsole = console != handler;
                if (changeConsole) _ = BackgroundDosVectors.SetConsoleTask(dos, handler);
                privateInput = PublicShellFileServices.OpenInput(dos, Text("*"), 1);
                var openFailure = privateInput.IsNull ? PublicShellFileServices.GetError(dos) : 0;
                if (changeConsole) _ = BackgroundDosVectors.SetConsoleTask(dos, console);
                if (privateInput.IsNull)
                {
                    Free(message, bytes);
                    Free(tags, TagBytes);
                    PublicShellFileServices.SetError(dos, (DOS.Error)openFailure);
                    global::Amiga.Exec.CloseLibrary(dos);
                    return false;
                }
                launchInput = privateInput;
            }
        }
        var privateOutput = BPTR.Null;
        var destination = ShellRunOutputPolicy.Destination(console.IsNotNull, nilOutput);
        if (destination != ShellRunOutputDestination.ExplicitNil)
        {
            var consoleOutput = destination == ShellRunOutputDestination.Console;
            privateOutput = consoleOutput
                ? PublicShellFileServices.OpenOutput(dos, Text("*"), 1, 0)
                : PublicShellFileServices.OpenOutput(dos, Text("NIL:"), 4, 0);
            if (privateOutput.IsNull)
            {
                var openFailure = PublicShellFileServices.GetError(dos);
                if (privateInput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateInput);
                Free(message, bytes);
                Free(tags, TagBytes);
                PublicShellFileServices.SetError(dos, (DOS.Error)openFailure);
                global::Amiga.Exec.CloseLibrary(dos);
                return false;
            }
            standardOutput = privateOutput;
        }
        ShellRunOutputPolicy.Select(output, error, standardOutput, ownedStreams,
            nilOutput, out var launchOutput, out var launchOwnedStreams,
            out var closeLauncherOutput);
        if (privateInput.IsNotNull) launchOwnedStreams |= 1;
        if (privateOutput.IsNotNull) launchOwnedStreams |= 2;
        if (!PublicShellStreamLeaseCore.TryPrepare(dos, cli, launchInput, launchOutput, error,
                launchOwnedStreams, out var leases, out var created, out var retained, out var leaseFailure))
        {
            if (privateInput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateInput);
            if (privateOutput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateOutput);
            Free(message, bytes);
            Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, leaseFailure);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        APTR.WriteUInt16(message, ExecLayout.Message.Length, (ushort)HeaderBytes);
        APTR.WriteUInt32(message, 20, length);
        APTR.WriteUInt32(message, 24, error.Raw);
        APTR.WriteUInt32(message, 28, bytes);
        APTR.WriteUInt32(message, 32, leases.Input.Raw);
        APTR.WriteUInt32(message, 36, leases.Output.Raw);
        APTR.WriteUInt32(message, 40, leases.Error.Raw);
        for (var i = 0u; i < length; i++)
            APTR.WriteUInt8(message, (int)(HeaderBytes + i), APTR.ReadUInt8(command, (int)i));
        if (!PublicShellImageLease.Prepare(ref memory, dos, task, entry, out var image))
        {
            var leaseError = PublicShellFileServices.GetError(dos);
            PublicShellStreamLeaseCore.Abort(dos, in leases, created, retained);
            if (privateInput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateInput);
            if (privateOutput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateOutput);
            Free(message, bytes);
            Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, (DOS.Error)leaseError);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var owner = APTR.Null;
        var reservation = APTR.Null;
        var ownerFailure = DOS.Error.None;
        var shell = new DosShellNativePlatform(APTR.Null);
        if (continuation.IsNotNull &&
            !PublicShellContinuationCore.Prepare(ref shell, cli, continuation, out owner, out reservation, out ownerFailure))
        {
            PublicShellImageLease.Abort(dos, image);
            PublicShellStreamLeaseCore.Abort(dos, in leases, created, retained);
            if (privateInput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateInput);
            if (privateOutput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateOutput);
            Free(message, bytes);
            Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, ownerFailure);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var childDirectory = BPTR.Null;
        if (hasDirectory)
        {
            childDirectory = PublicShellPathVectors.DupLock(dos, currentDirectory);
            if (childDirectory.IsNull || PublicShellPathVectors.NameFromLock(dos, childDirectory,
                    APTR.FromPointer(message.Raw + directoryOffset), (int)DirectoryBytes) == 0)
            {
                var directoryError = PublicShellFileServices.GetError(dos);
                if (childDirectory.IsNotNull) PublicShellPathVectors.UnLock(dos, childDirectory);
                _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
                PublicShellImageLease.Abort(dos, image);
                PublicShellStreamLeaseCore.Abort(dos, in leases, created, retained);
                if (privateInput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateInput);
                if (privateOutput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateOutput);
                Free(message, bytes);
                Free(tags, TagBytes);
                PublicShellFileServices.SetError(dos, (DOS.Error)directoryError);
                global::Amiga.Exec.CloseLibrary(dos);
                return false;
            }
        }
        APTR.WriteUInt32(message, 44, owner.Raw);
        APTR.WriteUInt32(message, 48, hasDirectory ? directoryOffset : 0);
        if (image.IsNull) Tag(tags, 0, (uint)DosNewProcessTag.Entry, entry.Raw);
        else Tag(tags, 0, (uint)DosNewProcessTag.SegmentList, image.Raw);
        Tag(tags, 1, (uint)DosNewProcessTag.Cli, 1);
        Tag(tags, 2, launchInput.IsNotNull ? (uint)DosNewProcessTag.Input : 1, launchInput.Raw);
        Tag(tags, 3, launchInput.IsNotNull ? (uint)DosNewProcessTag.CloseInput : 1, 0);
        Tag(tags, 4, launchOutput.IsNotNull ? (uint)DosNewProcessTag.Output : 1, launchOutput.Raw);
        Tag(tags, 5, launchOutput.IsNotNull ? (uint)DosNewProcessTag.CloseOutput : 1, 0);
        Tag(tags, 6, (uint)DosNewProcessTag.StackSize,
            stackPresent != 0 && stack > StackBytes ? stack : StackBytes);
        Tag(tags, 7, (uint)DosNewProcessTag.Name, owner.IsNull
            ? Text("CopperOS background Shell").Raw : PublicShellContinuationCore.TaskName(owner).Raw);
        Tag(tags, 8, image.IsNull ? 1u : (uint)DosNewProcessTag.FreeSegmentList, 1);
        Tag(tags, 9, image.IsNull ? 1u : (uint)DosNewProcessTag.ExitData, PublicShellWorkerStartup.Background);
        Tag(tags, 10, priorityPresent != 0 ? (uint)DosNewProcessTag.Priority : 1u, unchecked((uint)priority));
        // Have DOS duplicate the caller's temporarily selected directory.
        // Unlike an explicit NP_CurrentDir, our temporary lock remains ours
        // through every CreateNewProc failure point and can always be freed.
        var previousDirectory = childDirectory.IsNull ? BPTR.Null :
            PublicShellPathVectors.CurrentDir(dos, childDirectory);
        var child = BackgroundDosVectors.CreateNewProc(dos, tags);
        var failure = PublicShellFileServices.GetError(dos);
        if (childDirectory.IsNotNull)
        {
            _ = PublicShellPathVectors.CurrentDir(dos, previousDirectory);
            PublicShellPathVectors.UnLock(dos, childDirectory);
        }
        Free(tags, TagBytes);
        if (child.IsNull)
        {
            if (owner.IsNotNull) _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
            PublicShellImageLease.Abort(dos, image);
            PublicShellStreamLeaseCore.Abort(dos, in leases, created, retained);
            if (privateInput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateInput);
            if (privateOutput.IsNotNull) _ = PublicShellFileServices.Close(dos, privateOutput);
            Free(message, bytes);
            PublicShellFileServices.SetError(dos, (DOS.Error)failure);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        if (owner.IsNotNull) PublicShellTaskIdentity.Attach(child, reservation);
        var childCli = DosProcessCodec.ReadCommandLineInterface(ref memory, child).Address;
        if (childCli.IsNotNull)
        {
            if (stackPresent != 0)
                _ = ShellCliState.TryWriteDefaultStack(ref memory, childCli, unchecked((int)stack));
            DosCommandLineInterfaceCodec.WriteInteractive(ref memory, childCli, 0);
            DosCommandLineInterfaceCodec.WriteBackground(ref memory, childCli, -1);
        }
        var number = DosProcessCodec.ReadTaskNumber(ref memory, child);
        if (output.IsNotNull && number > 0)
            WriteNotice(dos, output, APTR.FromPointer(message.Raw + HeaderBytes + length + 1),
                unchecked((uint)number));
        if (closeLauncherOutput) _ = PublicShellFileServices.Close(dos, output);
        // The child waits on its own public message port until all fields are
        // initialized. It owns and frees the message after executing the line.
        global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(child), message);
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        global::Amiga.Exec.CloseLibrary(dos);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [M68kExport("copperos.shell.background")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Worker()
    {
        var memory = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (task.IsNull) return 20;
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        var length = APTR.ReadUInt32(message, 20);
        var error = BPTR.FromRaw(APTR.ReadUInt32(message, 24));
        var bytes = APTR.ReadUInt32(message, 28);
        var owner = APTR.FromPointer(APTR.ReadUInt32(message, 44));
        var directoryOffset = APTR.ReadUInt32(message, 48);
        var leases = new ShellStreamLeases
        {
            Input = APTR.FromPointer(APTR.ReadUInt32(message, 32)),
            Output = APTR.FromPointer(APTR.ReadUInt32(message, 36)),
            Error = APTR.FromPointer(APTR.ReadUInt32(message, 40)),
        };
        var cli = DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address;
        DosProcessCodec.WriteCurrentError(ref memory, task, error);
        var shell = new DosShellNativePlatform(APTR.Null);
        var result = (int)ShellCommandResult.Error;
        var continuationError = 0;
        var directoryReady = true;
        if (directoryOffset != 0)
        {
            var directoryDos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
            if (directoryDos.IsNull) directoryReady = false;
            else
            {
                directoryReady = PublicShellPathVectors.SetCurrentDirName(directoryDos,
                    APTR.FromPointer(message.Raw + directoryOffset)) != 0;
                if (!directoryReady) continuationError = PublicShellFileServices.GetError(directoryDos);
                global::Amiga.Exec.CloseLibrary(directoryDos);
            }
        }
        if (directoryReady && cli.IsNotNull && ShellProcessState.TryInstall(ref shell, cli, out var state))
        {
            if (ShellProcessState.TrySetStreamLeases(ref shell, cli, state, in leases))
            {
                if (length == 0) result = (int)ShellCommandResult.Ok;
                else
                {
                    var status = shell.TryExecuteCommand(cli, APTR.FromPointer(message.Raw + HeaderBytes),
                        length, out result);
                    if (status == ShellScriptExecutionStatus.Pending)
                    {
                        _ = shell.AbortScriptExecution(cli);
                        result = (int)ShellCommandResult.Error;
                    }
                }
            }
            if (owner.IsNotNull)
            {
                var outcomeDos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
                if (outcomeDos.IsNotNull)
                {
                    continuationError = PublicShellFileServices.GetError(outcomeDos);
                    global::Amiga.Exec.CloseLibrary(outcomeDos);
                }
            }
            if (!PublicShellContinuationCore.DetachAll(ref shell, cli) ||
                !ShellProcessState.TryRemove(ref shell, cli, state))
                result = (int)ShellCommandResult.Error;
        }
        // Borrowed output is not closed by DOS on process exit. Complete the
        // Shell's buffered writes while that handle is still available.
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNotNull)
        {
            var ioError = PublicShellFileServices.GetError(dos);
            // An empty Run executes no command and writes nothing in the
            // worker. Its borrowed output can already have been closed by an
            // external owner once the launching Shell returns.
            if (length != 0)
            {
                var currentOutput = PublicShellFileServices.Output(dos);
                if (currentOutput.IsNotNull) _ = ShellPacketDosVectors.Flush(dos, currentOutput);
            }
            PublicShellStreamLeaseCore.Release(dos, in leases);
            PublicShellFileServices.SetError(dos, (DOS.Error)ioError);
            global::Amiga.Exec.CloseLibrary(dos);
        }
        Free(message, bytes);
        PublicShellContinuationCore.Complete(owner, result, continuationError);
        return unchecked((uint)result);
    }

    internal static bool IsPermanentShell(ref CopperSharpNativeDosPlatform memory,
        APTR dos, APTR entry)
    {
        var found = false;
        global::Amiga.Exec.Forbid();
        found = ContainsPermanentImage(ref memory, dos, entry, Text("Shell")) ||
            ContainsPermanentImage(ref memory, dos, entry, Text("CopperOS-Shell-Image"));
        global::Amiga.Exec.Permit();
        return found;
    }

    private static bool ContainsPermanentImage(ref CopperSharpNativeDosPlatform memory,
        APTR dos, APTR entry, APTR name)
    {
        var resident = APTR.Null;
        while ((resident = PublicShellResidentVectors.FindSegment(dos, name, resident, 1)).IsNotNull)
        {
            if (!DosSegmentCodec.IsMapped(ref memory, resident) ||
                DosSegmentCodec.ReadUseCount(ref memory, resident) != -1) continue;
            var segment = DosSegmentCodec.ReadSegmentList(ref memory, resident);
            while (segment.IsNotNull)
            {
                var address = segment.Address;
                if (address.Raw < 4 || address.Raw > uint.MaxValue - 4 ||
                    !memory.IsMapped(APTR.FromPointer(address.Raw - 4), 8)) break;
                var allocation = address.Raw - 4;
                var size = memory.ReadUInt32(APTR.FromPointer(allocation));
                var found = size >= 8 && allocation <= uint.MaxValue - size &&
                    entry.Raw >= address.Raw + 4 && entry.Raw < allocation + size;
                if (found) return true;
                segment = BPTR.FromRaw(APTR.ReadUInt32(address, 0));
            }
        }
        return false;
    }

    private static void WriteNotice(APTR dos, BPTR output, APTR buffer, uint number)
    {
        APTR.WriteUInt8(buffer, 0, (byte)'[');
        APTR.WriteUInt8(buffer, 1, (byte)'C');
        APTR.WriteUInt8(buffer, 2, (byte)'L');
        APTR.WriteUInt8(buffer, 3, (byte)'I');
        APTR.WriteUInt8(buffer, 4, (byte)' ');
        var divisor = 1_000_000_000u;
        var position = 5;
        var started = false;
        while (divisor != 0)
        {
            var digit = 0u;
            while (number >= divisor) { number -= divisor; digit++; }
            if (digit != 0 || started || divisor == 1)
            {
                APTR.WriteUInt8(buffer, position++, (byte)('0' + digit));
                started = true;
            }
            divisor /= 10u;
        }
        APTR.WriteUInt8(buffer, position++, (byte)']');
        APTR.WriteUInt8(buffer, position++, (byte)'\n');
        _ = ShellPacketDosVectors.Flush(dos, output);
        _ = PublicShellFileServices.Write(dos, output, buffer, unchecked((uint)position));
    }

    private static APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static void Free(APTR address, uint bytes)
    { if (address.IsNotNull) global::Amiga.Exec.FreeMem(address.Raw, bytes); }
    private static APTR Text(CString text) => APTR.FromPointer(CString.ToUInt32(text));
    private static void Tag(APTR tags, int index, uint tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
}

[AmigaLibrary("dos.library::shell-background", AmigaLibraryBasePolicy.CallerProvided)]
internal static class BackgroundDosVectors
{
    [AmigaLvo(DosLvo.CreateNewProc)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR CreateNewProc([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR tags);
    [AmigaLvo(DosLvo.SetConsoleTask)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR SetConsoleTask([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR task);
}
