using System.Runtime.CompilerServices;
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Starts an independent Shell using public DOS process interfaces.</summary>
internal static class PublicShellNewProcessCore
{
    private const uint MessageBytes = 552;
    private const uint TagBytes = 104;
    private const int PromptOffset = 40;
    private const int DirectoryOffset = 296;
    private const uint ShellStack = 65536;

    public static bool Start(APTR cli, ShellLaunchKind kind, APTR window,
        uint windowLength, APTR from, uint fromLength, APTR continuation, BPTR currentDirectory)
    {
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (dos.IsNull) return false;
        var memory = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (cli.IsNull || task.IsNull || !DosProcessCodec.IsMapped(ref memory, task) ||
            DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address != cli ||
            (kind != ShellLaunchKind.NewCli && kind != ShellLaunchKind.NewShell &&
             kind != ShellLaunchKind.Cli))
        {
            PublicShellFileServices.SetError(dos, DOS.Error.ObjectWrongType);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var entry = APTR.ExportAddress("copperos.shell.new-process");
        var windowName = window;
        var actualWindowLength = windowLength;
        if (window.IsNull)
        {
            windowName = Text("CON:0/0/640/200/CopperOS Shell/CLOSE");
            actualWindowLength = Length(windowName);
        }
        var startupName = from;
        var actualFromLength = fromLength;
        if (from.IsNull)
        {
            startupName = Text("S:Shell-Startup");
            actualFromLength = Length(startupName);
        }
        var message = Allocate(MessageBytes);
        var tags = Allocate(TagBytes);
        if (message.IsNull || tags.IsNull)
        {
            Free(message, MessageBytes); Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, DOS.Error.NoFreeStore);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var prompt = APTR.FromPointer(message.Raw + PromptOffset);
        var directory = APTR.FromPointer(message.Raw + DirectoryOffset);
        if (!PublicShellPromptServices.CopyTemplate(ref memory, dos, cli, prompt, 256, out _) ||
            PublicShellPathVectors.GetCurrentDirName(dos, directory, 256) == 0 ||
            !ShellCliState.TryReadDefaultStack(ref memory, cli, out var defaultStack) || defaultStack < 0)
        {
            var error = PublicShellFileServices.GetError(dos);
            Free(message, MessageBytes); Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, (DOS.Error)error);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var input = PublicShellFileServices.OpenInput(dos, windowName, actualWindowLength);
        if (input.IsNull)
        {
            var error = PublicShellFileServices.GetError(dos);
            Free(message, MessageBytes); Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, (DOS.Error)error);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var handler = APTR.FromPointer(memory.ReadUInt32(input.Address, DosLayout.FileHandle.Type));
        var oldConsole = BackgroundDosVectors.SetConsoleTask(dos, handler);
        var output = PublicShellFileServices.OpenOutput(dos, Text("*"), 1, 0);
        var openError = PublicShellFileServices.GetError(dos);
        _ = BackgroundDosVectors.SetConsoleTask(dos, oldConsole);
        if (output.IsNull)
        {
            _ = PublicShellFileServices.Close(dos, input);
            Free(message, MessageBytes); Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, (DOS.Error)openError);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        // A missing startup file leaves the new Shell interactive, as with
        // the conventional optional S:Shell-Startup file.
        var startup = PublicShellFileServices.OpenInput(dos, startupName, actualFromLength);
        if (!PublicShellImageLease.Prepare(ref memory, dos, task, entry, out var image))
        {
            var error = PublicShellFileServices.GetError(dos);
            if (startup.IsNotNull) _ = PublicShellFileServices.Close(dos, startup);
            _ = PublicShellFileServices.Close(dos, output);
            _ = PublicShellFileServices.Close(dos, input);
            Free(message, MessageBytes); Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, (DOS.Error)error);
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
            if (startup.IsNotNull) _ = PublicShellFileServices.Close(dos, startup);
            _ = PublicShellFileServices.Close(dos, output);
            _ = PublicShellFileServices.Close(dos, input);
            Free(message, MessageBytes); Free(tags, TagBytes);
            PublicShellFileServices.SetError(dos, ownerFailure);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        var childDirectory = BPTR.Null;
        if (continuation.IsNotNull && currentDirectory.IsNotNull)
        {
            childDirectory = PublicShellPathVectors.DupLock(dos, currentDirectory);
            if (childDirectory.IsNull || PublicShellPathVectors.NameFromLock(dos, childDirectory, directory, 256) == 0)
            {
                var directoryError = PublicShellFileServices.GetError(dos);
                if (childDirectory.IsNotNull) PublicShellPathVectors.UnLock(dos, childDirectory);
                _ = PublicShellContinuationCore.AbortLaunch(ref shell, cli, owner, reservation);
                PublicShellImageLease.Abort(dos, image);
                if (startup.IsNotNull) _ = PublicShellFileServices.Close(dos, startup);
                _ = PublicShellFileServices.Close(dos, output);
                _ = PublicShellFileServices.Close(dos, input);
                Free(message, MessageBytes); Free(tags, TagBytes);
                PublicShellFileServices.SetError(dos, (DOS.Error)directoryError);
                global::Amiga.Exec.CloseLibrary(dos);
                return false;
            }
        }
        APTR.WriteUInt16(message, ExecLayout.Message.Length, (ushort)MessageBytes);
        APTR.WriteUInt32(message, 20, input.Raw);
        APTR.WriteUInt32(message, 24, output.Raw);
        APTR.WriteUInt32(message, 28, startup.Raw);
        APTR.WriteUInt32(message, 32, unchecked((uint)defaultStack));
        APTR.WriteUInt32(message, 36, owner.Raw);
        if (image.IsNull) Tag(tags, 0, DosNewProcessTag.Entry, entry.Raw);
        else Tag(tags, 0, DosNewProcessTag.SegmentList, image.Raw);
        Tag(tags, 1, DosNewProcessTag.Cli, 1);
        Tag(tags, 2, DosNewProcessTag.Input, input.Raw);
        Tag(tags, 3, DosNewProcessTag.Output, output.Raw);
        Tag(tags, 4, DosNewProcessTag.CloseInput, 1);
        Tag(tags, 5, DosNewProcessTag.CloseOutput, 1);
        Tag(tags, 6, DosNewProcessTag.StackSize,
            defaultStack > ShellStack ? unchecked((uint)defaultStack) : ShellStack);
        Tag(tags, 7, DosNewProcessTag.ConsoleTask, handler.Raw);
        Tag(tags, 8, DosNewProcessTag.Name, owner.IsNull
            ? Text("CopperOS child Shell").Raw : PublicShellContinuationCore.TaskName(owner).Raw);
        Tag(tags, 9, image.IsNull ? (DosNewProcessTag)1 : DosNewProcessTag.FreeSegmentList, 1);
        Tag(tags, 10, image.IsNull ? (DosNewProcessTag)1 : DosNewProcessTag.ExitData, PublicShellWorkerStartup.NewShell);
        // Avoid transferring NP_CurrentDir: DOS inherits its own duplicate
        // while this temporary caller-owned lock remains safe on failure.
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
            if (startup.IsNotNull) _ = PublicShellFileServices.Close(dos, startup);
            _ = PublicShellFileServices.Close(dos, output);
            _ = PublicShellFileServices.Close(dos, input);
            Free(message, MessageBytes);
            PublicShellFileServices.SetError(dos, (DOS.Error)failure);
            global::Amiga.Exec.CloseLibrary(dos);
            return false;
        }
        if (owner.IsNotNull) PublicShellTaskIdentity.Attach(child, reservation);
        global::Amiga.Exec.PutMsg(DosProcessCodec.MessagePortAddress(child), message);
        PublicShellFileServices.SetError(dos, DOS.Error.None);
        global::Amiga.Exec.CloseLibrary(dos);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [M68kExport("copperos.shell.new-process")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Worker()
    {
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        if (task.IsNull) return 20;
        var port = DosProcessCodec.MessagePortAddress(task);
        _ = global::Amiga.Exec.WaitPort(port);
        var message = global::Amiga.Exec.GetMsg(port);
        if (message.IsNull) return 20;
        var input = BPTR.FromRaw(APTR.ReadUInt32(message, 20));
        var output = BPTR.FromRaw(APTR.ReadUInt32(message, 24));
        var startup = BPTR.FromRaw(APTR.ReadUInt32(message, 28));
        var stack = unchecked((int)APTR.ReadUInt32(message, 32));
        var owner = APTR.FromPointer(APTR.ReadUInt32(message, 36));
        var memory = new CopperSharpNativeDosPlatform();
        var cli = DosProcessCodec.ReadCommandLineInterface(ref memory, task).Address;
        var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        var result = 20u;
        var continuationError = 0;
        if (dos.IsNotNull && cli.IsNotNull && DosCommandLineInterfaceCodec.IsMapped(ref memory, cli))
        {
            var reader = input;
            if (startup.IsNotNull) reader = startup;
            DosCommandLineInterfaceCodec.WriteStandardInput(ref memory, cli, input);
            DosCommandLineInterfaceCodec.WriteCurrentInput(ref memory, cli, reader);
            DosCommandLineInterfaceCodec.WriteStandardOutput(ref memory, cli, output);
            DosCommandLineInterfaceCodec.WriteCurrentOutput(ref memory, cli, output);
            DosCommandLineInterfaceCodec.WriteInteractive(ref memory, cli, -1);
            DosCommandLineInterfaceCodec.WriteBackground(ref memory, cli, 0);
            _ = ShellCliState.TryWriteDefaultStack(ref memory, cli, stack);
            DosProcessCodec.WriteCurrentError(ref memory, task, output);
            _ = NativeForegroundDosVectors.SelectInput(dos, reader);
            _ = NativeForegroundDosVectors.SelectOutput(dos, output);
            var prompt = APTR.FromPointer(message.Raw + PromptOffset);
            var directory = APTR.FromPointer(message.Raw + DirectoryOffset);
            var ready = PublicShellPromptVectors.SetPrompt(dos, prompt) != 0 &&
                PublicShellPathVectors.SetCurrentDirName(dos, directory) != 0;
            Free(message, MessageBytes);
            message = APTR.Null;
            if (ready) result = DosShellNativeChildCore.RunFromInitializedCli(startup.IsNotNull);
            if (owner.IsNotNull) continuationError = PublicShellFileServices.GetError(dos);
            var current = DosCommandLineInterfaceCodec.Read(ref memory, cli).CurrentInput;
            if (startup.IsNotNull && current == startup)
            {
                _ = NativeForegroundDosVectors.SelectInput(dos, input);
                DosCommandLineInterfaceCodec.WriteCurrentInput(ref memory, cli, input);
                _ = PublicShellFileServices.Close(dos, startup);
            }
        }
        else if (startup.IsNotNull && dos.IsNotNull)
            _ = PublicShellFileServices.Close(dos, startup);
        if (message.IsNotNull) Free(message, MessageBytes);
        if (dos.IsNotNull) global::Amiga.Exec.CloseLibrary(dos);
        PublicShellContinuationCore.Complete(owner, unchecked((int)result), continuationError);
        return result;
    }

    private static APTR Allocate(uint bytes) => APTR.FromPointer(global::Amiga.Exec.AllocMem(bytes,
        global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
    private static void Free(APTR address, uint bytes)
    { if (address.IsNotNull) global::Amiga.Exec.FreeMem(address.Raw, bytes); }
    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
    private static uint Length(APTR text)
    { uint length = 0; while (APTR.ReadUInt8(text, (int)length) != 0) length++; return length; }
    private static void Tag(APTR tags, int index, DosNewProcessTag tag, uint value)
    { APTR.WriteUInt32(tags, index * 8, (uint)tag); APTR.WriteUInt32(tags, index * 8 + 4, value); }
}
