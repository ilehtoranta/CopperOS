using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
using CopperStart.Dos;
using CopperOS.Shell;

namespace CopperOS.SystemImage;

// Kickstart's DOS objects do not carry CopperStart's private owner header.
// Use only public DOS vectors for this interactive compatibility entry.
internal static class KickstartShell
{
    // DOS starts a user Shell with a packet. BootShell's explicitly-created
    // child uses a separate entry path because it has no such packet.
    public static uint RunDosStartup(APTR task)
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var dos = ExecVectors.OpenLibrary(exec, Text("dos.library"), 37);
        if (dos.IsNull) return 20;
        var packet = KickstartVectors.WaitPkt(dos);
        if (packet.IsNull) { ExecVectors.CloseLibrary(exec, dos); return 20; }
        var guest = new CopperSharpNativeDosPlatform();
        var segments = DosProcessCodec.ReadSegmentList(ref guest, task).Address;
        if (segments.IsNotNull && guest.IsMapped(segments, 20) && APTR.ReadUInt32(segments, 16) == 0)
        {
            APTR.WriteUInt32(segments, 16, APTR.ReadUInt32(segments, 12));
            APTR.WriteUInt32(segments, 12, 0);
        }
        var kind = (APTR.ReadUInt32(packet, 12) != 0 ? 2 : 0) |
                   (APTR.ReadUInt32(packet, 16) != 0 ? 1 : 0);
        if (kind != 0 && kind != 2)
        {
            ReplyOriginal(dos, packet);
            ExecVectors.CloseLibrary(exec, dos);
            return 20;
        }
        var flags = kind == 0 ? KickstartVectors.CliInitRun(dos, packet) : KickstartVectors.CliInitNewcli(dos, packet);
        var valid = (unchecked((uint)flags) & 0x80000000u) != 0;
        if (!valid && unchecked((uint)KickstartVectors.IoErr(dos)) == task.Raw)
        {
            // CliInit has already returned the packet on this error path.
            ExecVectors.CloseLibrary(exec, dos);
            return 20;
        }
        if ((!valid && kind == 0) || kind == 2 || (valid && (flags & 12) == 12))
        {
            ReplyOriginal(dos, packet);
            packet = APTR.Null;
        }
        var cli = DosProcessCodec.ReadCommandLineInterface(ref guest, task).Address;
        if (!DosCommandLineInterfaceCodec.IsMapped(ref guest, cli))
        {
            if (packet.IsNotNull) KickstartVectors.ReplyPkt(dos, packet, 20, KickstartVectors.IoErr(dos));
            ExecVectors.CloseLibrary(exec, dos);
            return 20;
        }
        var cliData = DosCommandLineInterfaceCodec.Read(ref guest, cli);
        DosCommandLineInterfaceCodec.WriteReturnCode(ref guest, cli, 0);
        DosCommandLineInterfaceCodec.WriteResult2(ref guest, cli, 0);
        if (cliData.CurrentInput.IsNotNull) KickstartVectors.SelectInput(dos, cliData.CurrentInput);
        if (cliData.StandardOutput.IsNotNull) KickstartVectors.SelectOutput(dos, cliData.StandardOutput);
        var systemCall = valid && (flags & 4) != 0;
        _ = Run(!systemCall, kind == 2, true);
        cliData = DosCommandLineInterfaceCodec.Read(ref guest, cli);
        var result = cliData.ReturnCode;
        var result2 = cliData.Result2;
        var output = Vectors.Output(dos);
        if (output.IsNotNull) KickstartVectors.Flush(dos, output);
        // CliInitRun can supply a separate DOS-owned command stream. It is
        // distinct from the caller's standard input and must be closed on
        // completion, including synchronous System() calls.
        if (cliData.CurrentInput.IsNotNull && cliData.CurrentInput.Raw != cliData.StandardInput.Raw &&
            cliData.CurrentInput.Raw != cliData.StandardOutput.Raw)
        {
            if (cliData.StandardInput.IsNotNull) KickstartVectors.SelectInput(dos, cliData.StandardInput);
            Vectors.Close(dos, cliData.CurrentInput);
        }
        var asynchronous = valid && (flags & 8) != 0;
        if ((!valid || asynchronous || (flags & 2) == 0) && cliData.StandardInput.IsNotNull)
            Vectors.Close(dos, cliData.StandardInput);
        if ((!valid || asynchronous || (flags & 1) != 0) && cliData.StandardOutput.IsNotNull &&
            cliData.StandardOutput.Raw != cliData.StandardInput.Raw)
            Vectors.Close(dos, cliData.StandardOutput);
        if (packet.IsNotNull) KickstartVectors.ReplyPkt(dos, packet, result, result2);
        var directory = KickstartVectors.CurrentDir(dos, BPTR.Null);
        if (directory.IsNotNull) KickstartVectors.UnLock(dos, directory);
        ExecVectors.CloseLibrary(exec, dos);
        return unchecked((uint)result);
    }

    private static void ReplyOriginal(APTR dos, APTR packet)
        => KickstartVectors.ReplyPkt(dos, packet,
            unchecked((int)APTR.ReadUInt32(packet, 12)), unchecked((int)APTR.ReadUInt32(packet, 16)));

    public static uint Run(bool printPrompt = true, bool resumeStandardInput = false, bool bufferedInput = false)
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var dos = ExecVectors.OpenLibrary(exec, Text("dos.library"), 0);
        if (dos.IsNull) return 20;
        var memory = ExecVectors.AllocMem(exec, 10304, 0x10001);
        if (memory.IsNull) { ExecVectors.CloseLibrary(exec, dos); return 20; }
        var input = Vectors.Input(dos);
        var output = Vectors.Output(dos);
        var command = APTR.FromPointer(memory.Raw + 1024);
        var inputPath = APTR.FromPointer(memory.Raw + 2048);
        var outputPath = APTR.FromPointer(memory.Raw + 3072);
        var errorPath = APTR.FromPointer(memory.Raw + 4096);
        var path = APTR.FromPointer(memory.Raw + 5120);
        var scriptStack = APTR.FromPointer(memory.Raw + 6144);
        var paths = APTR.FromPointer(memory.Raw + 6208);
        APTR.WriteUInt8(paths, 0, (byte)'C'); APTR.WriteUInt8(paths, 1, (byte)':');
        var pathCount = 1;
        var depth = 0;
        var guest = new CopperSharpNativeDosPlatform();
        var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
        var cli = DosProcessCodec.ReadCommandLineInterface(ref guest, task).Address;
        if (!DosCommandLineInterfaceCodec.IsMapped(ref guest, cli))
        {
            ExecVectors.FreeMem(exec, memory, 10304);
            ExecVectors.CloseLibrary(exec, dos);
            return 20;
        }
        var standardInput = DosCommandLineInterfaceCodec.Read(ref guest, cli).StandardInput;
        while (true)
        {
            if (depth == 0 && printPrompt && (!resumeStandardInput || input.Raw == standardInput.Raw))
                SystemBootstrap.Print(dos, "CopperOS> ");
            var length = 0;
            var source = depth == 0 ? input : BPTR.FromRaw(APTR.ReadUInt32(scriptStack, (depth - 1) * 4));
            while (length < 1022)
            {
                if (!ReadByte(dos, source, memory, length, bufferedInput))
                {
                    if (depth == 0)
                    {
                        if (resumeStandardInput && input.Raw != standardInput.Raw && standardInput.IsNotNull)
                        {
                            KickstartVectors.SelectInput(dos, standardInput);
                            Vectors.Close(dos, input);
                            input = standardInput;
                            DosCommandLineInterfaceCodec.WriteCurrentInput(ref guest, cli, input);
                            resumeStandardInput = false;
                            if (length == 0) goto nextLine;
                            break;
                        }
                        goto done;
                    }
                    Vectors.Close(dos, source); depth--;
                    if (length == 0) goto nextLine;
                    break;
                }
                var c = APTR.ReadUInt8(memory, length);
                if (c == 10) break;
                if (c != 13) length++;
            }
            if (length == 1022)
            {
                // Drain an overlong line so its suffix can never become a
                // separate command. A line at the limit is rejected too.
                while (ReadByte(dos, source, memory, 1022, bufferedInput) &&
                    APTR.ReadUInt8(memory, 1022) != 10) { }
                Error(dos, task, "Command line too long\n");
                goto nextLine;
            }
            var workspace = new ShellRedirectionWorkspace(command, 1024,
                inputPath, 1024, outputPath, 1024, errorPath, 1024);
            if (!ShellRedirectionParser.Parse(ref guest, memory, (uint)length,
                in workspace, out var redirection, out var commandLength))
            { Error(dos, task, "Invalid command\n"); continue; }
            var cursor = new ShellTextCursor(command, commandLength);
            var parsed = ShellTextParser.NextToken(ref guest, ref cursor, path, 1021,
                out var nameLength, out _);
            if (parsed == (int)ShellTextTokenResult.End) continue;
            if (parsed != (int)ShellTextTokenResult.Token)
            { Error(dos, task, "Invalid command\n"); continue; }
            var args = cursor.Position;
            while (args < commandLength && (APTR.ReadUInt8(command, (int)args) == 32 || APTR.ReadUInt8(command, (int)args) == 9)) args++;
            APTR.WriteUInt8(command, (int)commandLength, 10);
            APTR.WriteUInt8(command, (int)commandLength + 1, 0);
            var redirectedInput = BPTR.Null;
            var redirectedOutput = BPTR.Null;
            var redirectedError = BPTR.Null;
            var commandResult = 0;
            var commandError = 0;
            var publishStatus = false;
            if (redirection.HasInput) redirectedInput = Vectors.Open(dos, inputPath, 1005);
            if (redirection.HasOutput)
            {
                redirectedOutput = Vectors.Open(dos, outputPath, redirection.OutputAppend != 0 ? 1004 : 1006);
                if (redirectedOutput.IsNull && redirection.OutputAppend != 0)
                    redirectedOutput = Vectors.Open(dos, outputPath, 1006);
                if (redirectedOutput.IsNotNull && redirection.OutputAppend != 0)
                    _ = KickstartVectors.Seek(dos, redirectedOutput, 0, 1);
            }
            if (redirection.HasErrorPath)
            {
                redirectedError = Vectors.Open(dos, errorPath, redirection.ErrorAppend != 0 ? 1004 : 1006);
                if (redirectedError.IsNull && redirection.ErrorAppend != 0)
                    redirectedError = Vectors.Open(dos, errorPath, 1006);
                if (redirectedError.IsNotNull && redirection.ErrorAppend != 0)
                    _ = KickstartVectors.Seek(dos, redirectedError, 0, 1);
            }
            if ((redirection.HasInput && redirectedInput.IsNull) ||
                (redirection.HasOutput && redirectedOutput.IsNull) ||
                (redirection.HasErrorPath && redirectedError.IsNull))
            {
                var errorStream = redirectedError.IsNotNull ? redirectedError : DosProcessCodec.ReadCurrentError(ref guest, task);
                if (errorStream.IsNull) errorStream = Vectors.Output(dos);
                WriteText(dos, errorStream, "Redirection failed\n");
                if (redirectedInput.IsNotNull) Vectors.Close(dos, redirectedInput);
                if (redirectedOutput.IsNotNull) Vectors.Close(dos, redirectedOutput);
                if (redirectedError.IsNotNull) Vectors.Close(dos, redirectedError);
                continue;
            }
            var oldInput = redirectedInput.IsNotNull ? KickstartVectors.SelectInput(dos, redirectedInput) : BPTR.Null;
            var oldOutput = redirectedOutput.IsNotNull ? KickstartVectors.SelectOutput(dos, redirectedOutput) : BPTR.Null;
            var oldError = DosProcessCodec.ReadCurrentError(ref guest, task);
            if (redirection.HasError || oldError.IsNull)
                DosProcessCodec.WriteCurrentError(ref guest, task,
                    redirection.ErrorToOutput != 0 || oldError.IsNull && !redirection.HasErrorPath
                        ? Vectors.Output(dos) : redirectedError);
            if (nameLength == 4 && ShellTextParser.EqualsPacked(ref guest, path, nameLength, 0x6563686f, 0))
            {
                Echo(dos, task, command, commandLength, args, path);
                goto closeStreams;
            }
            if (nameLength == 7 && ShellTextParser.EqualsPacked(ref guest, path, nameLength, 0x65786563, 0x75746500))
            {
                var fileCursor = new ShellTextCursor(command, commandLength) { Position = args };
                var status = ShellTextParser.NextToken(ref guest, ref fileCursor, path, 1024, out _, out _);
                var script = status == (int)ShellTextTokenResult.Token && depth < 16 ? Vectors.Open(dos, path, 1005) : BPTR.Null;
                if (script.IsNull) { commandError = KickstartVectors.IoErr(dos); commandResult = 10; publishStatus = true; Error(dos, task, "Cannot open script\n"); }
                else { APTR.WriteUInt32(scriptStack, depth * 4, script.Raw); depth++; }
                goto closeStreams;
            }
            if (nameLength == 2 && ShellTextParser.EqualsPacked(ref guest, path, nameLength, 0x63640000, 0))
            {
                var dirCursor = new ShellTextCursor(command, commandLength) { Position = args };
                var status = ShellTextParser.NextToken(ref guest, ref dirCursor, path, 1024, out _, out _);
                if (status == (int)ShellTextTokenResult.End)
                {
                    var current = DosProcessCodec.ReadCurrentDirectory(ref guest, task);
                    if (KickstartVectors.NameFromLock(dos, current, path, 1024) == 0)
                    { commandError = KickstartVectors.IoErr(dos); commandResult = 10; publishStatus = true; Error(dos, task, "Cannot read current directory\n"); }
                    else
                    {
                        var count = 0; while (count < 1023 && APTR.ReadUInt8(path, count) != 0) count++;
                        Vectors.Write(dos, Vectors.Output(dos), path, count);
                        SystemBootstrap.Print(dos, "\n");
                        commandError = 0; commandResult = 0; publishStatus = true;
                    }
                    goto closeStreams;
                }
                if (status == (int)ShellTextTokenResult.Token &&
                    ShellTextParser.NextToken(ref guest, ref dirCursor, inputPath, 1024, out _, out _) != (int)ShellTextTokenResult.End)
                { commandError = 118; commandResult = 10; publishStatus = true; Error(dos, task, "Too many arguments\n"); goto closeStreams; }
                var directory = status == (int)ShellTextTokenResult.Token ? KickstartVectors.Lock(dos, path, -2) : BPTR.Null;
                if (directory.IsNull) { commandError = KickstartVectors.IoErr(dos); commandResult = 10; publishStatus = true; Error(dos, task, "Cannot change directory\n"); }
                else
                {
                    var examined = KickstartVectors.Examine(dos, directory, inputPath) != 0;
                    if (!examined || unchecked((int)APTR.ReadUInt32(inputPath, 4)) <= 0)
                    {
                        commandError = examined ? 212 : KickstartVectors.IoErr(dos);
                        KickstartVectors.UnLock(dos, directory);
                        commandResult = 10; publishStatus = true; Error(dos, task, "Cannot change directory\n");
                    }
                    else if (KickstartVectors.NameFromLock(dos, directory, path, 1024) == 0 ||
                             KickstartVectors.SetCurrentDirName(dos, path) == 0)
                    {
                        commandError = KickstartVectors.IoErr(dos); KickstartVectors.UnLock(dos, directory);
                        commandResult = 10; publishStatus = true; Error(dos, task, "Cannot change directory\n");
                    }
                    else
                    {
                        var previous = KickstartVectors.CurrentDir(dos, directory);
                        if (previous.IsNotNull) KickstartVectors.UnLock(dos, previous);
                        commandError = 0; commandResult = 0; publishStatus = true;
                    }
                }
                goto closeStreams;
            }
            if (nameLength == 4 && ShellTextParser.EqualsPacked(ref guest, path, nameLength, 0x70617468, 0))
            {
                UpdatePath(dos, task, command, commandLength, args, path, paths, ref pathCount);
                goto closeStreams;
            }
            var segment = KickstartVectors.LoadSeg(dos, path);
            // Kickstart's CLI owns a BPTR-linked list of directory locks.
            // Search inherited entries without taking ownership of those locks,
            // and restore the caller's current directory after every attempt.
            var explicitPath = false;
            for (var i = 0; i < (int)nameLength; i++)
                if (APTR.ReadUInt8(path, i) == ':' || APTR.ReadUInt8(path, i) == '/') explicitPath = true;
            var pathNode = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref guest, cli);
            for (var guard = 0; segment.IsNull && !explicitPath && pathNode.IsNotNull && guard < 256; guard++)
            {
                var nodeAddress = pathNode.Address;
                if (!guest.IsMapped(nodeAddress, 8)) break;
                var nextNode = BPTR.FromRaw(APTR.ReadUInt32(nodeAddress, 0));
                var directory = BPTR.FromRaw(APTR.ReadUInt32(nodeAddress, 4));
                if (directory.IsNotNull)
                {
                    var previousDirectory = KickstartVectors.CurrentDir(dos, directory);
                    segment = KickstartVectors.LoadSeg(dos, path);
                    KickstartVectors.CurrentDir(dos, previousDirectory);
                }
                pathNode = nextNode;
            }
            for (var entry = 0; segment.IsNull && entry < pathCount; entry++)
            {
                var prefix = APTR.FromPointer(paths.Raw + (uint)(entry * 512));
                var prefixLength = 0;
                while (prefixLength < 511 && APTR.ReadUInt8(prefix, prefixLength) != 0) prefixLength++;
                var separator = prefixLength != 0 && APTR.ReadUInt8(prefix, prefixLength - 1) != ':' && APTR.ReadUInt8(prefix, prefixLength - 1) != '/' ? 1 : 0;
                if (prefixLength + separator + nameLength >= 1024) continue;
                for (var i = (int)nameLength; i >= 0; i--) APTR.WriteUInt8(path, prefixLength + separator + i, APTR.ReadUInt8(path, i));
                for (var i = 0; i < prefixLength; i++) APTR.WriteUInt8(path, i, APTR.ReadUInt8(prefix, i));
                if (separator != 0) APTR.WriteUInt8(path, prefixLength, (byte)'/');
                segment = KickstartVectors.LoadSeg(dos, path);
                if (segment.IsNull)
                    for (var i = 0; i <= (int)nameLength; i++) APTR.WriteUInt8(path, i, APTR.ReadUInt8(path, prefixLength + separator + i));
            }
            if (segment.IsNull)
            {
                commandError = KickstartVectors.IoErr(dos);
                commandResult = 10;
                publishStatus = true;
                Error(dos, task, "Command not found\n");
                goto closeStreams;
            }
            commandResult = KickstartVectors.RunCommand(dos, segment, 65536,
                APTR.FromPointer(command.Raw + args), (int)(commandLength - args + 1));
            commandError = KickstartVectors.IoErr(dos);
            publishStatus = true;
            KickstartVectors.UnLoadSeg(dos, segment);
        closeStreams:
            if (redirection.HasError || oldError.IsNull) DosProcessCodec.WriteCurrentError(ref guest, task, oldError);
            if (redirectedInput.IsNotNull) { KickstartVectors.SelectInput(dos, oldInput); Vectors.Close(dos, redirectedInput); }
            if (redirectedOutput.IsNotNull) { KickstartVectors.SelectOutput(dos, oldOutput); Vectors.Close(dos, redirectedOutput); }
            if (redirectedError.IsNotNull) Vectors.Close(dos, redirectedError);
            if (publishStatus)
            {
                DosCommandLineInterfaceCodec.WriteReturnCode(ref guest, cli, commandResult);
                DosCommandLineInterfaceCodec.WriteResult2(ref guest, cli, commandError);
                KickstartVectors.SetIoErr(dos, commandError);
            }
        nextLine:;
        }
    done:
        while (depth > 0) { depth--; Vectors.Close(dos, BPTR.FromRaw(APTR.ReadUInt32(scriptStack, depth * 4))); }
        ExecVectors.FreeMem(exec, memory, 10304);
        ExecVectors.CloseLibrary(exec, dos);
        return 0;
    }

    private static bool ReadByte(APTR dos, BPTR source, APTR destination, int offset, bool buffered)
    {
        if (!buffered)
            return Vectors.Read(dos, source, APTR.FromPointer(destination.Raw + (uint)offset), 1) == 1;
        var value = KickstartVectors.FGetC(dos, source);
        if (value < 0) return false;
        APTR.WriteUInt8(destination, offset, (byte)value);
        return true;
    }

    private static void UpdatePath(APTR dos, APTR task, APTR command, uint length, uint position,
        APTR token, APTR paths, ref int count)
    {
        var guest = new CopperSharpNativeDosPlatform();
        var cursor = new ShellTextCursor(command, length) { Position = position };
        var result = ShellTextParser.NextToken(ref guest, ref cursor, token, 512, out var size, out _);
        if (result == (int)ShellTextTokenResult.End ||
            (result == (int)ShellTextTokenResult.Token && size == 4 && ShellTextParser.EqualsPacked(ref guest, token, size, 0x73686f77, 0)))
        {
            for (var i = 0; i < count; i++)
            {
                var item = APTR.FromPointer(paths.Raw + (uint)(i * 512)); var n = 0;
                while (APTR.ReadUInt8(item, n) != 0 && n < 511) n++;
                Vectors.Write(dos, Vectors.Output(dos), item, n); SystemBootstrap.Print(dos, "\n");
            }
            return;
        }
        if (result != (int)ShellTextTokenResult.Token) { Error(dos, task, "Invalid path\n"); return; }
        var add = size == 3 && ShellTextParser.EqualsPacked(ref guest, token, size, 0x61646400, 0);
        if (add || (size == 5 && ShellTextParser.EqualsPacked(ref guest, token, size, 0x72657365, 0x74000000)))
        {
            if (!add) count = 0;
            result = ShellTextParser.NextToken(ref guest, ref cursor, token, 512, out size, out _);
        }
        else count = 0;
        while (result == (int)ShellTextTokenResult.Token && count < 8)
        {
            var item = APTR.FromPointer(paths.Raw + (uint)(count * 512));
            for (var i = 0; i <= (int)size; i++) APTR.WriteUInt8(item, i, APTR.ReadUInt8(token, i));
            count++;
            result = ShellTextParser.NextToken(ref guest, ref cursor, token, 512, out size, out _);
        }
        if (result != (int)ShellTextTokenResult.End) Error(dos, task, "Invalid path\n");
    }

    private static void Echo(APTR dos, APTR task, APTR command, uint length, uint position, APTR token)
    {
        var guest = new CopperSharpNativeDosPlatform();
        var cursor = new ShellTextCursor(command, length) { Position = position };
        var first = true;
        while (true)
        {
            var result = ShellTextParser.NextToken(ref guest, ref cursor, token, 1024, out var count, out _);
            if (result == (int)ShellTextTokenResult.End) break;
            if (result != (int)ShellTextTokenResult.Token) { Error(dos, task, "Invalid echo arguments\n"); return; }
            if (!first) SystemBootstrap.Print(dos, " ");
            if (count != 0) Vectors.Write(dos, Vectors.Output(dos), token, (int)count);
            first = false;
        }
        SystemBootstrap.Print(dos, "\n");
    }

    private static void Error(APTR dos, APTR task, CString message)
    {
        var guest = new CopperSharpNativeDosPlatform();
        var handle = DosProcessCodec.ReadCurrentError(ref guest, task);
        WriteText(dos, handle.IsNull ? Vectors.Output(dos) : handle, message);
    }

    private static void WriteText(APTR dos, BPTR handle, CString message)
    {
        var text = Text(message); var length = 0;
        while (APTR.ReadUInt8(text, length) != 0) length++;
        Vectors.Write(dos, handle, text, length);
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}

[AmigaLibrary("dos.library::kickstart-shell", AmigaLibraryBasePolicy.CallerProvided)]
internal static class KickstartVectors
{
    [AmigaLvo(DosLvo.WaitPkt)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern APTR WaitPkt([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.ReplyPkt)]
    internal static extern void ReplyPkt([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] APTR packet, [M68kRegister(M68kRegister.D2)] int result1,
        [M68kRegister(M68kRegister.D3)] int result2);
    [AmigaLvo(DosLvo.CliInitRun)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int CliInitRun([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.A0)] APTR packet);
    [AmigaLvo(DosLvo.CliInitNewcli)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int CliInitNewcli([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.A0)] APTR packet);
    [AmigaLvo(DosLvo.Flush)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Flush([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.FGetC)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int FGetC([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.IoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int IoErr([M68kRegister(M68kRegister.A6)] APTR dos);
    [AmigaLvo(DosLvo.SetIoErr)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetIoErr([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] int error);
    [AmigaLvo(DosLvo.Lock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR Lock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR path,
        [M68kRegister(M68kRegister.D2)] int mode);
    [AmigaLvo(DosLvo.UnLock)]
    internal static extern void UnLock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory);
    [AmigaLvo(DosLvo.CurrentDir)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR CurrentDir([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory);
    [AmigaLvo(DosLvo.Examine)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Examine([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory,
        [M68kRegister(M68kRegister.D2)] APTR info);
    [AmigaLvo(DosLvo.NameFromLock)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int NameFromLock([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR directory,
        [M68kRegister(M68kRegister.D2)] APTR buffer, [M68kRegister(M68kRegister.D3)] int length);
    [AmigaLvo(DosLvo.SetCurrentDirName)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int SetCurrentDirName([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR path);
    [AmigaLvo(DosLvo.SelectInput)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR SelectInput([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.SelectOutput)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR SelectOutput([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR file);
    [AmigaLvo(DosLvo.Seek)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int Seek([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR file,
        [M68kRegister(M68kRegister.D2)] int offset, [M68kRegister(M68kRegister.D3)] int mode);
    [AmigaLvo(DosLvo.LoadSeg)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern BPTR LoadSeg([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] APTR name);
    [AmigaLvo(DosLvo.UnLoadSeg)]
    internal static extern void UnLoadSeg([M68kRegister(M68kRegister.A6)] APTR dos, [M68kRegister(M68kRegister.D1)] BPTR segment);
    [AmigaLvo(DosLvo.RunCommand)] [return: M68kRegister(M68kRegister.D0)]
    internal static extern int RunCommand([M68kRegister(M68kRegister.A6)] APTR dos,
        [M68kRegister(M68kRegister.D1)] BPTR segment, [M68kRegister(M68kRegister.D2)] int stack,
        [M68kRegister(M68kRegister.D3)] APTR args, [M68kRegister(M68kRegister.D4)] int length);
}
