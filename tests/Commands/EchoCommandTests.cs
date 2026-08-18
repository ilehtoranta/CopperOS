using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class EchoCommandTests
{
    [Fact]
    public void Writes_message_and_default_line_feed()
    {
        TestShellPlatform platform = new();
        APTR text = platform.Store.Put("hello");
        CommandInvocation invocation = CommandInvocation.ForOutput(
            text,
            5,
            new BPTR(1));

        int result = EchoCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("hello\n", platform.Store.OutputText);
    }

    [Fact]
    public void Empty_message_still_advances_to_next_line()
    {
        TestShellPlatform platform = new();
        CommandInvocation invocation = CommandInvocation.ForOutput(
            APTR.Null,
            0,
            new BPTR(1));

        int result = EchoCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("\n", platform.Store.OutputText);
    }

    [Fact]
    public void Rejects_unmapped_argument_without_partial_output()
    {
        TestShellPlatform platform = new();
        CommandInvocation invocation = CommandInvocation.ForOutput(
            new APTR(4095),
            4,
            new BPTR(1));

        int result = EchoCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Fact]
    public void Rejects_a_short_guest_write_as_an_error()
    {
        TestShellPlatform platform = new();
        platform.Store.ShortWrite = true;
        APTR text = platform.Store.Put("hello");
        CommandInvocation invocation = CommandInvocation.ForOutput(
            text,
            5,
            new BPTR(1));

        int result = EchoCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Fact]
    public void Rejects_an_argument_larger_than_the_bounded_contract()
    {
        TestShellPlatform platform = new();
        CommandInvocation invocation = CommandInvocation.ForOutput(
            new APTR(64),
            EchoCommand.MaximumArgumentLength + 1,
            new BPTR(1));

        int result = EchoCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Fact]
    public void Rejects_a_missing_output_handle()
    {
        TestShellPlatform platform = new();
        CommandInvocation invocation = CommandInvocation.ForOutput(
            APTR.Null,
            0,
            BPTR.Null);

        int result = EchoCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Fact]
    public void Parses_quoted_items_and_star_control_escapes()
    {
        TestShellPlatform platform = new();
        string commandLine = "one \"two three\" *n";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CommandInvocation.ForOutput(
            source,
            (uint)commandLine.Length,
            new BPTR(1));

        int result = EchoCommand.ParseAndExecute(
            ref platform,
            in invocation,
            new APTR(80),
            96,
            new APTR(176),
            48,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("one two three \n\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Applies_first_and_len_and_suppresses_the_line_feed()
    {
        TestShellPlatform platform = new();
        string commandLine = "\"Hello out there!\" NOLINE FIRST 1 LEN 5";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CommandInvocation.ForOutput(
            source,
            (uint)commandLine.Length,
            new BPTR(1));

        int result = EchoCommand.ParseAndExecute(
            ref platform,
            in invocation,
            new APTR(80),
            96,
            new APTR(176),
            48,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("Hello", platform.Store.OutputText);
    }

    [Fact]
    public void Len_without_first_selects_the_rightmost_message_bytes()
    {
        TestShellPlatform platform = new();
        string commandLine = "abcdef LEN 2";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CommandInvocation.ForOutput(
            source,
            (uint)commandLine.Length,
            new BPTR(1));

        int result = EchoCommand.ParseAndExecute(
            ref platform,
            in invocation,
            new APTR(80),
            96,
            new APTR(176),
            48,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("ef\n", platform.Store.OutputText);
    }

    [Fact]
    public void To_opens_and_closes_a_command_owned_output()
    {
        TestShellPlatform platform = new();
        string commandLine = "hello TO RAM:echo NOLINE";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CommandInvocation.ForOutput(
            source,
            (uint)commandLine.Length,
            BPTR.Null);

        int result = EchoCommand.ParseAndExecute(
            ref platform,
            in invocation,
            new APTR(80),
            96,
            new APTR(176),
            48,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("RAM:echo", platform.Store.OpenedPath);
        Assert.Equal((uint)2, platform.Store.ClosedHandle.Raw);
        Assert.Equal("hello", platform.Store.OutputText);
    }

    [Fact]
    public void Quoted_keyword_is_preserved_as_message_text()
    {
        TestShellPlatform platform = new();
        string commandLine = "\"NOLINE\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CommandInvocation.ForOutput(
            source,
            (uint)commandLine.Length,
            new BPTR(1));

        int result = EchoCommand.ParseAndExecute(
            ref platform,
            in invocation,
            new APTR(80),
            96,
            new APTR(176),
            48,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("NOLINE\n", platform.Store.OutputText);
    }

    [Fact]
    public void Rejects_an_unterminated_quote_and_unknown_item_after_an_option()
    {
        TestShellPlatform platform = new();
        string unterminated = "\"hello";
        APTR source = platform.Store.PutAt(16, unterminated);
        CommandInvocation invocation = CommandInvocation.ForOutput(
            source,
            (uint)unterminated.Length,
            new BPTR(1));

        int result = EchoCommand.ParseAndExecute(
            ref platform,
            in invocation,
            new APTR(80),
            96,
            new APTR(176),
            48,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);

        platform = new TestShellPlatform();
        string unknownAfterOption = "hello NOLINE trailing";
        source = platform.Store.PutAt(16, unknownAfterOption);
        invocation = CommandInvocation.ForOutput(
            source,
            (uint)unknownAfterOption.Length,
            new BPTR(1));

        result = EchoCommand.ParseAndExecute(
            ref platform,
            in invocation,
            new APTR(80),
            96,
            new APTR(176),
            48,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    public struct TestShellPlatform : IShellPlatform, IShellScriptPlatform
    {
        public TestShellPlatform()
        {
            Store = new GuestStore();
        }

        public GuestStore Store;

        public byte ReadUInt8(APTR address, int offset = 0) =>
            Store.Memory[checked((int)address.Raw) + offset];

        public bool TryPollScriptSignal(
            APTR cli,
            out ShellScriptSignalEvent signal)
        {
            signal = new ShellScriptSignalEvent(
                ShellScriptSignalFlags.None, 0, Store.ScriptSignalSequence);
            if (cli.IsNull || Store.ScriptSignalPollFailure)
                return false;
            Store.ScriptSignalPollCount++;
            signal = new ShellScriptSignalEvent(
                Store.ScriptSignalFlags,
                Store.ScriptSignalResult,
                Store.ScriptSignalSequence);
            return true;
        }

        public bool TryAcknowledgeScriptSignal(
            APTR cli,
            in ShellScriptSignalEvent signal)
        {
            if (cli.IsNull || Store.ScriptSignalAcknowledgeFailure)
                return false;
            Store.LastScriptSignal = signal;
            Store.ScriptSignalAcknowledgeCount++;
            Store.ScriptSignalFlags = ShellScriptSignalFlags.None;
            return true;
        }

        public ushort ReadUInt16(APTR address, int offset = 0) =>
            (ushort)((ReadUInt8(address, offset) << 8) | ReadUInt8(address, offset + 1));

        public uint ReadUInt32(APTR address, int offset = 0) =>
            ((uint)ReadUInt16(address, offset) << 16) | ReadUInt16(address, offset + 2);

        public void WriteUInt8(APTR address, int offset, byte value) =>
            Store.Memory[checked((int)address.Raw) + offset] = value;

        public void WriteUInt16(APTR address, int offset, ushort value)
        {
            WriteUInt8(address, offset, (byte)(value >> 8));
            WriteUInt8(address, offset + 1, (byte)value);
        }

        public void WriteUInt32(APTR address, int offset, uint value)
        {
            WriteUInt16(address, offset, (ushort)(value >> 16));
            WriteUInt16(address, offset + 2, (ushort)value);
        }

        public void Clear(APTR address, uint byteCount)
        {
            Array.Clear(Store.Memory, checked((int)address.Raw), checked((int)byteCount));
        }

        public void Copy(APTR source, APTR destination, uint byteCount)
        {
            Array.Copy(
                Store.Memory,
                checked((int)source.Raw),
                Store.Memory,
                checked((int)destination.Raw),
                checked((int)byteCount));
        }

        public bool IsMapped(APTR address, uint byteSize) =>
            address.Raw <= Store.Memory.Length &&
            byteSize <= (uint)(Store.Memory.Length - address.Raw);

        public int Write(BPTR handle, APTR source, uint length)
        {
            if (handle.IsNull || Store.ShortWrite)
                return Store.ShortWrite ? checked((int)length - 1) : -1;

            Store.Append(source, length);
            return checked((int)length);
        }

        public int WriteByte(BPTR handle, byte value)
        {
            if (handle.IsNull)
                return -1;

            Store.Output.Add(value);
            return 1;
        }

        public BPTR OpenOutput(APTR path, uint pathLength)
        {
            if (path.IsNull || pathLength == 0 ||
                !IsMapped(path, pathLength))
                return BPTR.Null;

            Store.OpenedPath = Store.ReadText(path, pathLength);
            return new BPTR(2);
        }

        public bool CloseOutput(BPTR handle)
        {
            Store.ClosedHandle = handle;
            return !handle.IsNull;
        }

        public bool TryReadCliDefaultStack(APTR cli, out int stackBytes)
        {
            if (cli.IsNull || Store.ReadStackFailure)
            {
                stackBytes = 0;
                return false;
            }

            stackBytes = Store.DefaultStack;
            return true;
        }

        public bool TryWriteCliDefaultStack(APTR cli, int stackBytes)
        {
            if (cli.IsNull || Store.WriteStackFailure)
                return false;

            Store.DefaultStack = stackBytes;
            Store.WriteStackCount++;
            return true;
        }

        public bool TryWriteCliFailureLimit(APTR cli, uint failureLimit)
        {
            if (cli.IsNull || Store.WriteFailureLimitFailure)
                return false;

            Store.FailureLimit = failureLimit;
            Store.WriteFailureLimitCount++;
            return true;
        }

        public bool TryGetCurrentDirectory(
            APTR cli,
            APTR path,
            uint pathCapacity,
            out uint pathLength)
        {
            pathLength = 0;
            if (cli.IsNull || Store.CurrentDirectoryFailure || path.IsNull ||
                pathCapacity == 0 || !IsMapped(path, pathCapacity))
                return false;

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(
                Store.CurrentDirectory);
            if ((uint)bytes.Length >= pathCapacity)
                return false;
            bytes.CopyTo(Store.Memory, checked((int)path.Raw));
            WriteUInt8(path, bytes.Length, 0);
            pathLength = (uint)bytes.Length;
            return true;
        }

        public bool TryChangeCurrentDirectory(APTR cli, APTR path, uint pathLength)
        {
            if (cli.IsNull || Store.ChangeDirectoryFailure || path.IsNull ||
                pathLength == 0 || !IsMapped(path, pathLength))
                return false;

            Store.CurrentDirectory = Store.ReadText(path, pathLength);
            Store.ChangeDirectoryCount++;
            return true;
        }

        public bool TrySetAlias(
            APTR cli,
            APTR name,
            uint nameLength,
            APTR replacement,
            uint replacementLength)
        {
            if (cli.IsNull || Store.AliasSetFailure || name.IsNull ||
                nameLength == 0 || !IsMapped(name, nameLength) ||
                (replacementLength != 0 &&
                 (replacement.IsNull || !IsMapped(replacement, replacementLength))))
                return false;

            Store.AliasName = Store.ReadText(name, nameLength);
            Store.AliasValue = replacementLength == 0
                ? string.Empty
                : Store.ReadText(replacement, replacementLength);
            Store.AliasSetCount++;
            return true;
        }

        public bool TryRemoveAlias(APTR cli, APTR name, uint nameLength)
        {
            if (cli.IsNull || Store.AliasRemoveFailure || name.IsNull ||
                nameLength == 0 || !IsMapped(name, nameLength))
                return false;
            if (!string.Equals(
                    Store.ReadText(name, nameLength),
                    Store.AliasName,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            Store.AliasName = string.Empty;
            Store.AliasValue = string.Empty;
            Store.AliasRemoveCount++;
            return true;
        }

        public bool TryWriteAliases(BPTR output, APTR cli)
        {
            if (output.IsNull || cli.IsNull || Store.AliasListFailure)
                return false;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(
                Store.AliasListing);
            Store.Output.AddRange(bytes);
            return true;
        }

        public bool TryUpdateCommandPath(
            APTR cli,
            APTR pathBuffer,
            uint pathBytes,
            uint pathCount,
            uint operation,
            uint quiet)
        {
            if (cli.IsNull || Store.CommandPathUpdateFailure ||
                pathBuffer.IsNull || !IsMapped(pathBuffer, pathBytes))
                return false;

            Store.CommandPathEntries.Clear();
            uint position = 0;
            for (uint entry = 0; entry < pathCount; entry++)
            {
                uint start = position;
                while (position < pathBytes &&
                       ReadUInt8(pathBuffer, checked((int)position)) != 0)
                    position++;
                if (position >= pathBytes)
                    return false;
                Store.CommandPathEntries.Add(Store.ReadText(
                    new APTR(pathBuffer.Raw + start),
                    position - start));
                position++;
            }

            if (position != pathBytes)
                return false;
            Store.CommandPathOperation = operation;
            Store.CommandPathQuiet = quiet;
            Store.CommandPathUpdateCount++;
            return true;
        }

        public bool TryWriteCommandPath(BPTR output, APTR cli, uint quiet)
        {
            if (output.IsNull || cli.IsNull || Store.CommandPathListFailure)
                return false;
            Store.CommandPathQuiet = quiet;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(
                Store.CommandPathListing);
            Store.Output.AddRange(bytes);
            return true;
        }

        public bool TryBindScriptFrame(APTR cli, APTR frame)
        {
            if (cli.IsNull || frame.IsNull || !IsMapped(frame,
                    ShellScriptFrameCodec.Size))
                return false;
            Store.BoundCli = cli;
            Store.BoundFrame = frame;
            return true;
        }

        public bool TryUnbindScriptFrame(APTR cli, APTR frame)
        {
            if (cli.IsNull || Store.BoundCli != cli ||
                (frame.IsNotNull && Store.BoundFrame != frame))
                return false;
            Store.BoundCli = APTR.Null;
            Store.BoundFrame = APTR.Null;
            return true;
        }

        public bool TryRequestShellControl(
            APTR cli,
            ShellControlAction action,
            int returnCode)
        {
            if (cli.IsNull || Store.ControlFailure)
                return false;
            Store.LastControlAction = action;
            Store.LastControlReturnCode = returnCode;
            Store.ControlCount++;
            return true;
        }

        public bool TryDefineScriptLabel(APTR cli, APTR label, uint labelLength)
        {
            if (cli.IsNull || Store.LabelDefineFailure || label.IsNull ||
                labelLength == 0 || !IsMapped(label, labelLength))
                return false;
            Store.LastLabel = Store.ReadText(label, labelLength);
            Store.LabelDefineCount++;
            return true;
        }

        public bool TrySkipToLabel(
            APTR cli,
            APTR label,
            uint labelLength,
            uint back)
        {
            if (cli.IsNull || Store.SkipFailure ||
                (labelLength != 0 &&
                 (label.IsNull || !IsMapped(label, labelLength))))
                return false;
            Store.LastSkipLabel = labelLength == 0
                ? string.Empty
                : Store.ReadText(label, labelLength);
            Store.SkipBack = back;
            Store.SkipCount++;
            return true;
        }

        public bool TryAsk(
            APTR cli,
            BPTR input,
            BPTR output,
            APTR prompt,
            uint promptLength)
        {
            if (cli.IsNull || input.IsNull || output.IsNull ||
                Store.AskFailure || prompt.IsNull || promptLength == 0 ||
                !IsMapped(prompt, promptLength))
                return false;
            Store.AskPrompt = Store.ReadText(prompt, promptLength);
            Store.AskCount++;
            return true;
        }

        public bool TryEvaluateIf(
            APTR cli,
            uint condition,
            uint threshold,
            uint negate,
            uint noRequester,
            uint numeric,
            APTR left,
            uint leftLength,
            APTR right,
            uint rightLength)
        {
            if (cli.IsNull || Store.IfFailure ||
                (leftLength != 0 &&
                 (left.IsNull || !IsMapped(left, leftLength))) ||
                (rightLength != 0 &&
                 (right.IsNull || !IsMapped(right, rightLength))))
                return false;
            Store.IfCondition = condition;
            Store.IfThreshold = threshold;
            Store.IfNegate = negate;
            Store.IfNoRequester = noRequester;
            Store.IfNumeric = numeric;
            Store.IfLeft = leftLength == 0
                ? string.Empty
                : Store.ReadText(left, leftLength);
            Store.IfRight = rightLength == 0
                ? string.Empty
                : Store.ReadText(right, rightLength);
            Store.IfCount++;
            return true;
        }

        public ShellScriptExecutionStatus TryExecuteScript(APTR cli,
            APTR file, uint fileLength, out int result)
        {
            if (cli.IsNull || Store.ExecuteFailure || file.IsNull ||
                fileLength == 0 || !IsMapped(file, fileLength))
            {
                result = (int)ShellCommandResult.Fail;
                return ShellScriptExecutionStatus.Failed;
            }
            Store.ExecutedScript = Store.ReadText(file, fileLength);
            Store.ExecuteCount++;
            result = (int)ShellCommandResult.Ok;
            return ShellScriptExecutionStatus.Completed;
        }

        public bool TryPollScriptExecution(APTR cli,
            out ShellScriptExecutionStatus status, out int result)
        {
            status = ShellScriptExecutionStatus.Failed;
            result = (int)ShellCommandResult.Fail;
            return false;
        }

        public bool TryPrepareScriptWait(APTR cli) => false;

        public bool TryParkScriptWait(APTR cli, uint timeoutTicks) => false;

        public bool TryExpandScriptAlias(
            APTR cli,
            APTR source,
            uint sourceLength,
            APTR destination,
            uint destinationCapacity,
            out uint expanded,
            out uint expandedLength)
        {
            expanded = 0;
            expandedLength = 0;
            if (cli.IsNull || Store.ScriptAliasFailure || source.IsNull ||
                !IsMapped(source, sourceLength))
                return false;
            Store.LastScriptAliasSource = Store.ReadText(source, sourceLength);
            if (Store.ScriptAliasReplacement.Length == 0)
                return true;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(
                Store.ScriptAliasReplacement);
            if (destination.IsNull || destinationCapacity < 2 ||
                (uint)bytes.Length >= destinationCapacity ||
                !IsMapped(destination, destinationCapacity))
                return false;
            bytes.CopyTo(Store.Memory, checked((int)destination.Raw));
            WriteUInt8(destination, bytes.Length, 0);
            expanded = 1;
            expandedLength = (uint)bytes.Length;
            Store.ScriptAliasExpansionCount++;
            return true;
        }

        public bool TryLookupScriptCommand(
            APTR cli,
            APTR name,
            uint nameLength,
            APTR path,
            uint pathCapacity,
            out ShellScriptLookupKind kind,
            out uint pathLength)
        {
            kind = ShellScriptLookupKind.NotFound;
            pathLength = 0;
            if (cli.IsNull || Store.ScriptLookupFailure || name.IsNull ||
                nameLength == 0 || !IsMapped(name, nameLength))
                return false;
            Store.LastScriptLookupName = Store.ReadText(name, nameLength);
            Store.ScriptLookupCount++;
            kind = Store.ScriptLookupKind;
            if (Store.ScriptLookupPath.Length == 0)
                return true;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(
                Store.ScriptLookupPath);
            if (path.IsNull || pathCapacity < 2 ||
                (uint)bytes.Length >= pathCapacity ||
                !IsMapped(path, pathCapacity))
                return false;
            bytes.CopyTo(Store.Memory, checked((int)path.Raw));
            WriteUInt8(path, bytes.Length, 0);
            pathLength = (uint)bytes.Length;
            return true;
        }

        public bool TryReadScriptLine(
            APTR cli,
            BPTR input,
            uint currentLine,
            uint currentOffset,
            APTR destination,
            uint destinationCapacity,
            out uint lineLength,
            out uint nextLine,
            out uint nextOffset,
            out uint endOfFile)
        {
            lineLength = 0;
            nextLine = currentLine;
            nextOffset = currentOffset;
            endOfFile = 0;
            if (cli.IsNull || input.IsNull || Store.ScriptReadFailure ||
                destination.IsNull || destinationCapacity < 2 ||
                !IsMapped(destination, destinationCapacity) ||
                currentOffset > Store.ScriptText.Length ||
                currentLine == uint.MaxValue)
                return false;

            if (currentOffset == Store.ScriptText.Length)
            {
                endOfFile = 1;
                return true;
            }

            var start = checked((int)currentOffset);
            var end = start;
            while (end < Store.ScriptText.Length &&
                Store.ScriptText[end] is not '\r' and not '\n')
                end++;
            var length = end - start;
            if ((uint)length >= destinationCapacity)
                return false;
            var bytes = System.Text.Encoding.ASCII.GetBytes(
                Store.ScriptText.Substring(start, length));
            bytes.CopyTo(Store.Memory, checked((int)destination.Raw));
            WriteUInt8(destination, length, 0);
            lineLength = (uint)length;
            while (end < Store.ScriptText.Length &&
                Store.ScriptText[end] is '\r' or '\n')
                end++;
            nextLine = currentLine + 1;
            nextOffset = (uint)end;
            return true;
        }

        public bool TryExecuteScriptCommand(
            APTR cli,
            APTR frame,
            APTR line,
            uint lineLength,
            ShellScriptLookupKind lookupKind,
            APTR resolvedPath,
            uint resolvedPathLength,
            BPTR input,
            BPTR output,
            BPTR error,
            out int result,
            out APTR continuation)
        {
            result = (int)ShellCommandResult.Error;
            continuation = APTR.Null;
            if (cli.IsNull || frame.IsNull || Store.ScriptExecuteFailure ||
                line.IsNull || !IsMapped(line, lineLength) ||
                input.IsNull || output.IsNull || error.IsNull)
                return false;
            Store.LastScriptExternalCommand = Store.ReadText(line, lineLength);
            Store.LastScriptLookupKind = lookupKind;
            Store.LastScriptResolvedPath = resolvedPathLength == 0
                ? string.Empty
                : Store.ReadText(resolvedPath, resolvedPathLength);
            Store.LastScriptInput = input;
            Store.LastScriptOutput = output;
            Store.LastScriptError = error;
            Store.ScriptExecuteCount++;
            result = Store.ScriptCommandResult;
            if (Store.ScriptExternalPending)
            {
                continuation = Store.ScriptExternalContinuation.IsNotNull
                    ? Store.ScriptExternalContinuation
                    : new APTR(3600);
                Store.ScriptExternalContinuation = continuation;
                var initial = new ShellProcessContinuation
                {
                    ParentCli = cli,
                    State = ShellProcessContinuationState.Pending,
                };
                if (!ShellProcessContinuationCodec.Initialize(ref this,
                        continuation, in initial) ||
                    !ShellProcessContinuationTransitions.TryStart(ref this,
                        continuation))
                {
                    continuation = APTR.Null;
                    return false;
                }
            }
            return true;
        }

        public bool TryOpenScriptInput(
            APTR cli,
            APTR path,
            uint pathLength,
            out BPTR handle)
        {
            handle = BPTR.Null;
            if (cli.IsNull || Store.RedirectionInputFailure ||
                path.IsNull || pathLength == 0 || !IsMapped(path, pathLength))
                return false;
            Store.RedirectionInputPath = Store.ReadText(path, pathLength);
            Store.RedirectionOpenCount++;
            handle = new BPTR(20);
            return true;
        }

        public bool TryOpenScriptOutput(
            APTR cli,
            APTR path,
            uint pathLength,
            uint append,
            out BPTR handle)
        {
            handle = BPTR.Null;
            if (cli.IsNull || Store.RedirectionOutputFailure ||
                path.IsNull || pathLength == 0 || append > 1 ||
                !IsMapped(path, pathLength))
                return false;
            Store.RedirectionOutputPath = Store.ReadText(path, pathLength);
            Store.RedirectionOutputAppend = append;
            Store.RedirectionOpenCount++;
            handle = new BPTR(21);
            return true;
        }

        public bool TryCloseScriptRedirection(APTR cli, BPTR handle)
        {
            if (cli.IsNull || handle.IsNull)
                return false;
            Store.RedirectionCloseCount++;
            Store.LastClosedRedirection = handle;
            return !Store.RedirectionCloseFailure;
        }

        public bool TryRunCommand(
            APTR cli,
            BPTR input,
            BPTR output,
            BPTR error,
            BPTR currentDirectory,
            APTR continuation,
            APTR command,
            uint commandLength,
            uint detach,
            uint quiet,
            uint stack,
            uint stackPresent,
            int priority,
            uint priorityPresent)
        {
            if (cli.IsNull || Store.RunFailure || command.IsNull ||
                !IsMapped(command, commandLength))
                return false;
            Store.RunCommand = Store.ReadText(command, commandLength);
            Store.RunInput = input;
            Store.RunOutput = output;
            Store.RunError = error;
            Store.RunCurrentDirectory = currentDirectory;
            Store.RunContinuation = continuation;
            Store.RunDetach = detach;
            Store.RunQuiet = quiet;
            Store.RunStack = stack;
            Store.RunStackPresent = stackPresent;
            Store.RunPriority = priority;
            Store.RunPriorityPresent = priorityPresent;
            Store.RunCount++;
            return true;
        }

        public bool TryManageResident(
            APTR cli,
            BPTR output,
            APTR name,
            uint nameLength,
            APTR file,
            uint fileLength,
            APTR alias,
            uint aliasLength,
            uint remove,
            uint add,
            uint replace,
            uint force,
            uint system,
            uint defer)
        {
            if (cli.IsNull || output.IsNull || Store.ResidentFailure ||
                (nameLength != 0 && (name.IsNull || !IsMapped(name, nameLength))) ||
                (fileLength != 0 && (file.IsNull || !IsMapped(file, fileLength))) ||
                (aliasLength != 0 && (alias.IsNull || !IsMapped(alias, aliasLength))))
                return false;
            Store.ResidentName = name.IsNull ? string.Empty : Store.ReadText(name, nameLength);
            Store.ResidentFile = file.IsNull ? string.Empty : Store.ReadText(file, fileLength);
            Store.ResidentAlias = alias.IsNull ? string.Empty : Store.ReadText(alias, aliasLength);
            Store.ResidentRemove = remove;
            Store.ResidentAdd = add;
            Store.ResidentReplace = replace;
            Store.ResidentForce = force;
            Store.ResidentSystem = system;
            Store.ResidentDefer = defer;
            Store.ResidentCount++;
            return true;
        }

        public bool TryCreateShell(
            APTR parentCli,
            ShellLaunchKind kind,
            BPTR input,
            BPTR output,
            BPTR error,
            BPTR currentDirectory,
            APTR continuation,
            APTR window,
            uint windowLength,
            APTR from,
            uint fromLength)
        {
            if (parentCli.IsNull || Store.ShellLaunchFailure ||
                (windowLength != 0 && (window.IsNull ||
                    !IsMapped(window, windowLength))) ||
                (fromLength != 0 && (from.IsNull ||
                    !IsMapped(from, fromLength))))
                return false;
            Store.ShellLaunchKind = kind;
            Store.ShellInput = input;
            Store.ShellOutput = output;
            Store.ShellError = error;
            Store.ShellCurrentDirectory = currentDirectory;
            Store.ShellContinuation = continuation;
            Store.ShellWindow = window.IsNull
                ? string.Empty
                : Store.ReadText(window, windowLength);
            Store.ShellFrom = from.IsNull
                ? string.Empty
                : Store.ReadText(from, fromLength);
            Store.ShellLaunchCount++;
            return true;
        }

        public bool TryPollShellContinuation(
            APTR cli,
            APTR continuation,
            out ShellProcessContinuationState state,
            out int result)
        {
            state = ShellProcessContinuationState.Failed;
            result = 0;
            if (cli.IsNull || continuation.IsNull ||
                Store.ContinuationPollFailure)
                return false;
            state = Store.ContinuationObservedState;
            result = Store.ContinuationResult;
            Store.ContinuationPollCount++;
            return true;
        }

        public bool TryReleaseShellContinuation(
            APTR cli,
            APTR continuation,
            uint ownedFlags)
        {
            if (cli.IsNull || continuation.IsNull ||
                Store.ContinuationReleaseFailure)
                return false;
            Store.LastReleasedContinuation = continuation;
            Store.LastReleasedFlags = ownedFlags;
            Store.ContinuationReleaseCount++;
            return true;
        }

        public bool TryReadArgs(
            APTR argumentText,
            uint argumentLength,
            APTR template,
            uint templateLength,
            APTR resultArray,
            uint resultBytes,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            if (Store.ReadArgsFailure || template.IsNull || resultArray.IsNull ||
                (argumentLength != 0 && (argumentText.IsNull ||
                 !IsMapped(argumentText, argumentLength))) ||
                !IsMapped(template, templateLength) ||
                resultBytes < 4 || !IsMapped(resultArray, resultBytes))
                return false;

            string templateText = Store.ReadText(template, templateLength);
            if (string.Equals(templateText,
                    "MESSAGE/M,NOLINE/S,FIRST/N,LEN/N,TO/K",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadEchoArgs(argumentText, argumentLength,
                    resultArray, resultBytes, out rdArgs);
            if (string.Equals(templateText, "STACK/N",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadSingleNumberArgs(argumentText, argumentLength,
                    resultArray, required: false, out rdArgs);
            if (string.Equals(templateText, "RCLIM/A/N",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadSingleNumberArgs(argumentText, argumentLength,
                    resultArray, required: true, out rdArgs);
            if (string.Equals(templateText, "RC/N",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadSingleNumberArgs(argumentText, argumentLength,
                    resultArray, required: false, out rdArgs);
            if (string.Equals(templateText, "ERROR/N/M",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadFaultArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "NAME/A",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadNameArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "RESET/S",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadResetArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "NAME/A,SAVE/S",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadUnsetenvArgs(argumentText, argumentLength,
                    resultArray, required: true, out rdArgs);
            if (string.Equals(templateText, "NAME,SAVE/S",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadUnsetenvArgs(argumentText, argumentLength,
                    resultArray, required: false, out rdArgs);
            if (string.Equals(templateText, "NAME/A,STRING/F",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadSetArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "NAME/A,SAVE/S,STRING/F",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadSetenvArgs(argumentText, argumentLength,
                    resultArray, required: true, out rdArgs);
            if (string.Equals(templateText, "NAME,SAVE/S,STRING/F",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadSetenvArgs(argumentText, argumentLength,
                    resultArray, required: false, out rdArgs);
            if (string.Equals(templateText, "NAME,STRING/F",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadAliasArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "PROMPT/A/F",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadAskArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "PROMPT/F",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadPromptArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "LABEL/A",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadLabArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "DIR",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadOptionalNameArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "NAME",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadOptionalNameArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "LABEL,BACK/S",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadSkipArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText,
                    "PATH/M,ADD/S,SHOW/S,RESET/S,REMOVE/S,QUIET/S",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadPathArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText,
                    "NOT/S,WARN/S,ERROR/S,FAIL/S,,EQ/K,GT/K,GE/K,VAL/S,EXISTS/K,NOREQ/S",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadIfArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText,
                    "DETACH/S,QUIET/S,STACK/K/N,PRI/K/N,COMMAND/F",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadRunArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText,
                    "NAME,FILE,ALIAS/K,REMOVE/S,ADD/S,REPLACE/S,PURE=FORCE/S,SYSTEM/S,DEFER/S",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadResidentArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (string.Equals(templateText, "WINDOW,FROM",
                    StringComparison.OrdinalIgnoreCase))
                return TryReadWindowFromArgs(argumentText, argumentLength,
                    resultArray, out rdArgs);
            if (templateText.Length == 0)
                return TryReadEmptyArgs(argumentText, argumentLength,
                    out rdArgs);
            if (!string.Equals(templateText, "FILE/A",
                    StringComparison.OrdinalIgnoreCase))
                return false;

            string value = Store.ReadText(argumentText, argumentLength).Trim();
            bool quoted = value.Length >= 2 && value[0] == '"' &&
                value[^1] == '"';
            if (value.StartsWith('"') && !quoted)
                return false;
            if (quoted)
                value = value[1..^1];
            if (value.Length == 0 || (!quoted && value.IndexOf(' ') >= 0))
                return false;
            APTR file = Store.PutAt(192, value);
            WriteUInt8(file, value.Length, 0);
            WriteUInt32(resultArray, 0, file.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadEmptyArgs(
            APTR argumentText,
            uint argumentLength,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(256);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out _, out _);
            if (tokenResult != (int)ShellTextTokenResult.End)
                return false;
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadRunArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 20);
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(256);
            var command = string.Empty;
            var stackSet = false;
            var prioritySet = false;
            while (true)
            {
                var tokenResult = ShellTextParser.NextToken(ref this,
                    ref cursor, token, 96, out var tokenLength,
                    out var tokenFlags);
                if (tokenResult == (int)ShellTextTokenResult.End)
                    break;
                if (tokenResult != (int)ShellTextTokenResult.Token ||
                    tokenLength == 0)
                    return false;

                var value = Store.ReadText(token, tokenLength);
                if (tokenFlags == 0 && value.Equals("DETACH",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (ReadUInt32(resultArray, 0) != 0) return false;
                    WriteUInt32(resultArray, 0, 1);
                    continue;
                }
                if (tokenFlags == 0 && value.Equals("QUIET",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (ReadUInt32(resultArray, 4) != 0) return false;
                    WriteUInt32(resultArray, 4, 1);
                    continue;
                }
                if (tokenFlags == 0 && value.Equals("STACK",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (stackSet || !TryReadRunNumber(ref this, ref cursor,
                            token, resultArray, 8, false)) return false;
                    stackSet = true;
                    continue;
                }
                if (tokenFlags == 0 && value.Equals("PRI",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (prioritySet || !TryReadRunNumber(ref this, ref cursor,
                            token, resultArray, 12, true)) return false;
                    prioritySet = true;
                    continue;
                }

                if (command.Length != 0)
                    command += " ";
                command += value;
                while (true)
                {
                    tokenResult = ShellTextParser.NextToken(ref this,
                        ref cursor, token, 96, out tokenLength, out _);
                    if (tokenResult == (int)ShellTextTokenResult.End)
                        break;
                    if (tokenResult != (int)ShellTextTokenResult.Token ||
                        tokenLength == 0)
                        return false;
                    command += " " + Store.ReadText(token, tokenLength);
                }
                break;
            }

            if (command.Length == 0)
                return false;
            var commandAddress = Store.PutAt(1024, command);
            WriteUInt8(commandAddress, command.Length, 0);
            WriteUInt32(resultArray, 16, commandAddress.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadRunNumber(
            ref TestShellPlatform platform,
            ref ShellTextCursor cursor,
            APTR token,
            APTR resultArray,
            int offset,
            bool signed)
        {
            var tokenResult = ShellTextParser.NextToken(ref platform,
                ref cursor, token, 96, out var tokenLength,
                out var tokenFlags);
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                tokenFlags != 0 || tokenLength == 0)
                return false;
            var text = Store.ReadText(token, tokenLength);
            if (signed)
            {
                if (!int.TryParse(text, out var number)) return false;
                var numberAddress = new APTR(offset == 8 ? 184u : 188u);
                WriteUInt32(numberAddress, 0, unchecked((uint)number));
                WriteUInt32(resultArray, offset, numberAddress.Raw);
                return true;
            }
            if (!uint.TryParse(text, out var unsignedNumber)) return false;
            var unsignedAddress = new APTR(offset == 8 ? 184u : 188u);
            WriteUInt32(unsignedAddress, 0, unsignedNumber);
            WriteUInt32(resultArray, offset, unsignedAddress.Raw);
            return true;
        }

        private bool TryReadResidentArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 36);
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(256);
            var positional = 0;
            var valueAddress = 1024;
            while (true)
            {
                var tokenResult = ShellTextParser.NextToken(ref this,
                    ref cursor, token, 96, out var tokenLength,
                    out var tokenFlags);
                if (tokenResult == (int)ShellTextTokenResult.End)
                    break;
                if (tokenResult != (int)ShellTextTokenResult.Token ||
                    tokenLength == 0)
                    return false;
                var value = Store.ReadText(token, tokenLength);
                var switchOffset = tokenFlags == 0 && value.Equals("REMOVE",
                        StringComparison.OrdinalIgnoreCase) ? 12 :
                    tokenFlags == 0 && value.Equals("ADD",
                        StringComparison.OrdinalIgnoreCase) ? 16 :
                    tokenFlags == 0 && value.Equals("REPLACE",
                        StringComparison.OrdinalIgnoreCase) ? 20 :
                    tokenFlags == 0 && value.Equals("FORCE",
                        StringComparison.OrdinalIgnoreCase) ? 24 :
                    tokenFlags == 0 && value.Equals("SYSTEM",
                        StringComparison.OrdinalIgnoreCase) ? 28 :
                    tokenFlags == 0 && value.Equals("DEFER",
                        StringComparison.OrdinalIgnoreCase) ? 32 : -1;
                if (switchOffset >= 0)
                {
                    if (ReadUInt32(resultArray, switchOffset) != 0)
                        return false;
                    WriteUInt32(resultArray, switchOffset, 1);
                    continue;
                }
                if (tokenFlags == 0 && (value.Equals("ALIAS",
                        StringComparison.OrdinalIgnoreCase) ||
                        value.StartsWith("ALIAS=",
                            StringComparison.OrdinalIgnoreCase)))
                {
                    if (ReadUInt32(resultArray, 8) != 0)
                        return false;
                    string alias;
                    if (value.StartsWith("ALIAS=",
                            StringComparison.OrdinalIgnoreCase))
                        alias = value[6..];
                    else
                    {
                        tokenResult = ShellTextParser.NextToken(ref this,
                            ref cursor, token, 96, out var aliasLength,
                            out var aliasFlags);
                        if (tokenResult != (int)ShellTextTokenResult.Token ||
                            aliasFlags != 0 || aliasLength == 0)
                            return false;
                        alias = Store.ReadText(token, aliasLength);
                    }
                    var aliasAddress = new APTR((uint)valueAddress);
                    Store.PutAt(valueAddress, alias);
                    WriteUInt8(aliasAddress, alias.Length, 0);
                    WriteUInt32(resultArray, 8, aliasAddress.Raw);
                    valueAddress += 128;
                    continue;
                }
                if (positional >= 2)
                    return false;
                var address = new APTR((uint)valueAddress);
                Store.PutAt(valueAddress, value);
                WriteUInt8(address, value.Length, 0);
                WriteUInt32(resultArray, positional * 4, address.Raw);
                positional++;
                valueAddress += 128;
            }

            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadWindowFromArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 8);
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(256);
            var position = 0;
            while (true)
            {
                var tokenResult = ShellTextParser.NextToken(ref this,
                    ref cursor, token, 96, out var tokenLength,
                    out _);
                if (tokenResult == (int)ShellTextTokenResult.End)
                    break;
                if (tokenResult != (int)ShellTextTokenResult.Token ||
                    tokenLength == 0 || position >= 2)
                    return false;
                var value = Store.ReadText(token, tokenLength);
                var address = new APTR((uint)(1024 + position * 128));
                Store.PutAt((int)address.Raw, value);
                WriteUInt8(address, value.Length, 0);
                WriteUInt32(resultArray, position * 4, address.Raw);
                position++;
            }
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadNameArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            if (argumentLength == 0)
                return false;
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var tokenLength, out _);
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                tokenLength == 0)
                return false;
            var name = Store.ReadText(token, tokenLength);
            tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out _, out _);
            if (tokenResult != (int)ShellTextTokenResult.End)
                return false;
            var nameAddress = Store.PutAt(192, name);
            WriteUInt8(nameAddress, name.Length, 0);
            WriteUInt32(resultArray, 0, nameAddress.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadResetArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 4);
            if (argumentLength == 0)
            {
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var tokenLength, out var tokenFlags);
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                tokenFlags != 0 || tokenLength != 5 ||
                !string.Equals(Store.ReadText(token, tokenLength), "RESET",
                    StringComparison.OrdinalIgnoreCase))
                return false;
            tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out _, out _);
            if (tokenResult != (int)ShellTextTokenResult.End)
                return false;
            WriteUInt32(resultArray, 0, 1);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadUnsetenvArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            bool required,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 8);
            if (argumentLength == 0)
            {
                if (required)
                    return false;
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var tokenLength, out var firstFlags);
            if (tokenResult == (int)ShellTextTokenResult.End)
            {
                if (required)
                    return false;
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                tokenLength == 0)
                return false;
            var save = 0u;
            string name = string.Empty;
            var nameSet = false;
            var isSave = firstFlags == 0 && tokenLength == 4 &&
                string.Equals(Store.ReadText(token, tokenLength), "SAVE",
                    StringComparison.OrdinalIgnoreCase);
            if (isSave)
                save = 1;
            else
            {
                name = Store.ReadText(token, tokenLength);
                nameSet = true;
            }

            if (!nameSet)
            {
                tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                    token, 96, out tokenLength, out var nextFlags);
                if (tokenResult == (int)ShellTextTokenResult.Token)
                {
                    if (nextFlags == 0 && tokenLength == 4 &&
                        string.Equals(Store.ReadText(token, tokenLength),
                            "SAVE", StringComparison.OrdinalIgnoreCase))
                        return false;
                    name = Store.ReadText(token, tokenLength);
                    nameSet = true;
                }
                else if (tokenResult != (int)ShellTextTokenResult.End)
                    return false;
            }

            var lookahead = cursor;
            tokenResult = ShellTextParser.NextToken(ref this, ref lookahead,
                token, 96, out var optionLength, out var optionFlags);
            if (tokenResult == (int)ShellTextTokenResult.Token &&
                optionFlags == 0 && optionLength == 4 &&
                string.Equals(Store.ReadText(token, optionLength), "SAVE",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (save != 0)
                    return false;
                save = 1;
                cursor = lookahead;
            }
            tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out _, out _);
            if (tokenResult != (int)ShellTextTokenResult.End)
                return false;
            if (nameSet)
            {
                var nameAddress = Store.PutAt(192, name);
                WriteUInt8(nameAddress, name.Length, 0);
                WriteUInt32(resultArray, 0, nameAddress.Raw);
            }
            WriteUInt32(resultArray, 4, save != 0 ? 1u : 0u);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadSetArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            if (argumentLength == 0)
                return false;
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var nameLength, out _);
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                nameLength == 0)
                return false;
            var name = Store.ReadText(token, nameLength);
            var valueResult = ShellTextParser.ReadFinal(ref this, ref cursor,
                token, 96, out var valueLength, out _);
            if (valueResult != (int)ShellTextTokenResult.Token &&
                valueResult != (int)ShellTextTokenResult.End)
                return false;
            var value = valueResult == (int)ShellTextTokenResult.End
                ? string.Empty : Store.ReadText(token, valueLength);
            WriteNamedFinalResult(resultArray, name, value);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadSetenvArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            bool required,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 12);
            if (argumentLength == 0)
            {
                if (required)
                    return false;
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var nameLength, out var firstFlags);
            if (tokenResult == (int)ShellTextTokenResult.End)
            {
                if (required)
                    return false;
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                nameLength == 0)
                return false;
            var save = 0u;
            string name = string.Empty;
            var nameSet = false;
            var first = Store.ReadText(token, nameLength);
            if (firstFlags == 0 &&
                string.Equals(first, "SAVE", StringComparison.OrdinalIgnoreCase))
                save = 1;
            else
            {
                name = first;
                nameSet = true;
            }

            if (!nameSet)
            {
                tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                    token, 96, out nameLength, out var nextFlags);
                if (tokenResult == (int)ShellTextTokenResult.Token)
                {
                    if (nextFlags == 0 && nameLength == 4 &&
                        string.Equals(Store.ReadText(token, nameLength), "SAVE",
                            StringComparison.OrdinalIgnoreCase))
                        return false;
                    name = Store.ReadText(token, nameLength);
                    nameSet = true;
                }
                else if (tokenResult != (int)ShellTextTokenResult.End)
                    return false;
            }

            var lookahead = cursor;
            tokenResult = ShellTextParser.NextToken(ref this, ref lookahead,
                token, 96, out var optionLength, out var optionFlags);
            if (tokenResult == (int)ShellTextTokenResult.Token &&
                optionFlags == 0 && optionLength == 4 &&
                string.Equals(Store.ReadText(token, optionLength), "SAVE",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (save != 0)
                    return false;
                save = 1;
                cursor = lookahead;
            }
            var valueResult = ShellTextParser.ReadFinal(ref this, ref cursor,
                token, 96, out var valueLength, out _);
            if (valueResult != (int)ShellTextTokenResult.Token &&
                valueResult != (int)ShellTextTokenResult.End)
                return false;
            var value = valueResult == (int)ShellTextTokenResult.End
                ? string.Empty : Store.ReadText(token, valueLength);
            if (!nameSet)
                return false;
            WriteNamedFinalResult(resultArray, name, value);
            WriteUInt32(resultArray, 4, save != 0 ? 1u : 0u);
            WriteUInt32(resultArray, 8, new APTR(220).Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadAliasArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 8);
            if (argumentLength == 0)
            {
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var nameLength, out _);
            if (tokenResult == (int)ShellTextTokenResult.End)
            {
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                nameLength == 0)
                return false;
            var name = Store.ReadText(token, nameLength);
            var valueResult = ShellTextParser.ReadFinal(ref this, ref cursor,
                token, 96, out var valueLength, out _);
            if (valueResult != (int)ShellTextTokenResult.Token &&
                valueResult != (int)ShellTextTokenResult.End)
                return false;
            var value = valueResult == (int)ShellTextTokenResult.End
                ? string.Empty : Store.ReadText(token, valueLength);
            WriteNamedFinalResult(resultArray, name, value);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadAskArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            if (argumentLength == 0)
                return false;
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var valueResult = ShellTextParser.ReadFinal(ref this, ref cursor,
                token, 96, out var valueLength, out _);
            if (valueResult != (int)ShellTextTokenResult.Token ||
                valueLength == 0)
                return false;
            var value = Store.ReadText(token, valueLength);
            var valueAddress = Store.PutAt(192, value);
            WriteUInt8(valueAddress, value.Length, 0);
            WriteUInt32(resultArray, 0, valueAddress.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadPromptArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 4);
            if (argumentLength == 0)
            {
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var valueResult = ShellTextParser.ReadFinal(ref this, ref cursor,
                token, 96, out var valueLength, out _);
            if (valueResult == (int)ShellTextTokenResult.End)
            {
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            if (valueResult != (int)ShellTextTokenResult.Token)
                return false;
            var value = Store.ReadText(token, valueLength);
            var valueAddress = Store.PutAt(192, value);
            WriteUInt8(valueAddress, value.Length, 0);
            WriteUInt32(resultArray, 0, valueAddress.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadLabArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            if (argumentLength == 0)
                return false;
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var labelLength, out _);
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                labelLength == 0)
                return false;
            var label = Store.ReadText(token, labelLength);
            tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out _, out _);
            if (tokenResult != (int)ShellTextTokenResult.End)
                return false;
            var labelAddress = Store.PutAt(192, label);
            WriteUInt8(labelAddress, label.Length, 0);
            WriteUInt32(resultArray, 0, labelAddress.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadOptionalNameArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 4);
            if (argumentLength == 0)
            {
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }

            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var tokenLength, out _);
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                tokenLength == 0)
                return false;
            var value = Store.ReadText(token, tokenLength);
            tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out _, out _);
            if (tokenResult != (int)ShellTextTokenResult.End)
                return false;
            var valueAddress = Store.PutAt(192, value);
            WriteUInt8(valueAddress, value.Length, 0);
            WriteUInt32(resultArray, 0, valueAddress.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadSkipArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 8);
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var tokenLength, out var tokenFlags);
            if (tokenResult == (int)ShellTextTokenResult.End)
            {
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                tokenLength == 0)
                return false;

            var value = Store.ReadText(token, tokenLength);
            if (tokenFlags == 0 && string.Equals(value, "BACK",
                    StringComparison.OrdinalIgnoreCase))
            {
                tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                    token, 96, out _, out _);
                if (tokenResult != (int)ShellTextTokenResult.End)
                    return false;
                WriteUInt32(resultArray, 4, 1);
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }

            var labelAddress = Store.PutAt(192, value);
            WriteUInt8(labelAddress, value.Length, 0);
            WriteUInt32(resultArray, 0, labelAddress.Raw);
            tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var optionLength, out var optionFlags);
            if (tokenResult == (int)ShellTextTokenResult.Token)
            {
                if (optionFlags != 0 || optionLength != 4 ||
                    !string.Equals(Store.ReadText(token, optionLength),
                        "BACK", StringComparison.OrdinalIgnoreCase))
                    return false;
                WriteUInt32(resultArray, 4, 1);
                tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                    token, 96, out _, out _);
            }
            if (tokenResult != (int)ShellTextTokenResult.End)
                return false;
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadPathArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 24);
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var listAddress = new APTR(768);
            var pathCount = 0;
            var optionsStarted = false;
            Clear(listAddress, 260);

            while (true)
            {
                var tokenResult = ShellTextParser.NextToken(ref this,
                    ref cursor, token, 96, out var tokenLength,
                    out var tokenFlags);
                if (tokenResult == (int)ShellTextTokenResult.End)
                    break;
                if (tokenResult != (int)ShellTextTokenResult.Token ||
                    tokenLength == 0)
                    return false;

                var value = Store.ReadText(token, tokenLength);
                var optionOffset = value.Equals("ADD",
                        StringComparison.OrdinalIgnoreCase) ? 4 :
                    value.Equals("SHOW", StringComparison.OrdinalIgnoreCase)
                        ? 8 : value.Equals("RESET",
                            StringComparison.OrdinalIgnoreCase) ? 12 :
                    value.Equals("REMOVE", StringComparison.OrdinalIgnoreCase)
                        ? 16 : value.Equals("QUIET",
                            StringComparison.OrdinalIgnoreCase) ? 20 : -1;
                if (tokenFlags == 0 && optionOffset >= 0)
                {
                    if (ReadUInt32(resultArray, optionOffset) != 0)
                        return false;
                    WriteUInt32(resultArray, optionOffset, 1);
                    optionsStarted = true;
                    continue;
                }
                if (optionsStarted || pathCount >= 64)
                    return false;

                var valueAddress = new APTR((uint)(1024 + pathCount * 128));
                var text = Store.ReadText(token, tokenLength);
                Store.PutAt((int)valueAddress.Raw, text);
                WriteUInt8(valueAddress, text.Length, 0);
                WriteUInt32(listAddress, pathCount * 4,
                    valueAddress.Raw);
                pathCount++;
            }

            if (pathCount != 0)
            {
                WriteUInt32(listAddress, pathCount * 4, 0);
                WriteUInt32(resultArray, 0, listAddress.Raw);
            }
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadIfArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 44);
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var leftSet = false;
            var valueAddress = 1024;
            while (true)
            {
                var tokenResult = ShellTextParser.NextToken(ref this,
                    ref cursor, token, 96, out var tokenLength,
                    out var tokenFlags);
                if (tokenResult == (int)ShellTextTokenResult.End)
                    break;
                if (tokenResult != (int)ShellTextTokenResult.Token ||
                    tokenLength == 0)
                    return false;

                var name = Store.ReadText(token, tokenLength);
                var switchOffset = name.Equals("NOT",
                        StringComparison.OrdinalIgnoreCase) ? 0 :
                    name.Equals("WARN", StringComparison.OrdinalIgnoreCase)
                        ? 4 : name.Equals("ERROR",
                            StringComparison.OrdinalIgnoreCase) ? 8 :
                    name.Equals("FAIL", StringComparison.OrdinalIgnoreCase)
                        ? 12 : name.Equals("VAL",
                            StringComparison.OrdinalIgnoreCase) ? 32 :
                    name.Equals("NOREQ", StringComparison.OrdinalIgnoreCase)
                        ? 40 : -1;
                if (tokenFlags == 0 && switchOffset >= 0)
                {
                    if (ReadUInt32(resultArray, switchOffset) != 0)
                        return false;
                    WriteUInt32(resultArray, switchOffset, 1);
                    continue;
                }

                var keywordOffset = name.Equals("EQ",
                        StringComparison.OrdinalIgnoreCase) ? 20 :
                    name.Equals("GT", StringComparison.OrdinalIgnoreCase)
                        ? 24 : name.Equals("GE",
                            StringComparison.OrdinalIgnoreCase) ? 28 :
                        name.Equals("EXISTS",
                            StringComparison.OrdinalIgnoreCase) ? 36 : -1;
                if (tokenFlags == 0 && keywordOffset >= 0)
                {
                    if (ReadUInt32(resultArray, keywordOffset) != 0)
                        return false;
                    tokenResult = ShellTextParser.NextToken(ref this,
                        ref cursor, token, 96, out var valueLength,
                        out _);
                    if (tokenResult != (int)ShellTextTokenResult.Token ||
                        valueLength == 0)
                        return false;
                    var value = Store.ReadText(token, valueLength);
                    var address = new APTR((uint)valueAddress);
                    Store.PutAt(valueAddress, value);
                    WriteUInt8(address, value.Length, 0);
                    WriteUInt32(resultArray, keywordOffset, address.Raw);
                    valueAddress += 128;
                    continue;
                }

                if (leftSet)
                    return false;
                var left = Store.ReadText(token, tokenLength);
                var leftAddress = new APTR((uint)valueAddress);
                Store.PutAt(valueAddress, left);
                WriteUInt8(leftAddress, left.Length, 0);
                WriteUInt32(resultArray, 16, leftAddress.Raw);
                valueAddress += 128;
                leftSet = true;
            }

            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private void WriteNamedFinalResult(APTR resultArray,
            string name, string value)
        {
            var nameAddress = Store.PutAt(192, name);
            WriteUInt8(nameAddress, name.Length, 0);
            var valueAddress = Store.PutAt(220, value);
            WriteUInt8(valueAddress, value.Length, 0);
            WriteUInt32(resultArray, 0, nameAddress.Raw);
            WriteUInt32(resultArray, 4, valueAddress.Raw);
        }

        private bool TryReadSingleNumberArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            bool required,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 4);
            if (argumentLength == 0)
            {
                if (required)
                    return false;
                rdArgs = new APTR(240);
                Store.ReadArgsCount++;
                return true;
            }

            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out var tokenLength, out var tokenFlags);
            if (tokenResult != (int)ShellTextTokenResult.Token ||
                tokenFlags != 0 || !ShellNumberParser.TryParseUnsigned(
                    ref this, token, tokenLength, out var number))
                return false;
            tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                token, 96, out _, out _);
            if (tokenResult != (int)ShellTextTokenResult.End)
                return false;

            var numberAddress = new APTR(184);
            WriteUInt32(numberAddress, 0, number);
            WriteUInt32(resultArray, 0, numberAddress.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadFaultArgs(
            APTR argumentText,
            uint argumentLength,
            APTR resultArray,
            out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, 4);
            if (argumentLength == 0)
                return false;

            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var count = 0;
            var listAddress = new APTR(184);
            while (true)
            {
                var tokenResult = ShellTextParser.NextToken(ref this,
                    ref cursor, token, 96, out var tokenLength,
                    out var tokenFlags);
                if (tokenResult == (int)ShellTextTokenResult.End)
                    break;
                if (tokenResult != (int)ShellTextTokenResult.Token ||
                    tokenFlags != 0 || count >= 8 ||
                    !ShellNumberParser.TryParseUnsigned(ref this, token,
                        tokenLength, out var number))
                    return false;
                var numberAddress = new APTR((uint)(220 + count * 4));
                WriteUInt32(numberAddress, 0, number);
                WriteUInt32(listAddress, count * 4, numberAddress.Raw);
                count++;
            }
            if (count == 0)
                return false;
            WriteUInt32(listAddress, count * 4, 0);
            WriteUInt32(resultArray, 0, listAddress.Raw);
            rdArgs = new APTR(240);
            Store.ReadArgsCount++;
            return true;
        }

        private bool TryReadEchoArgs(APTR argumentText, uint argumentLength,
            APTR resultArray, uint resultBytes, out APTR rdArgs)
        {
            rdArgs = APTR.Null;
            Clear(resultArray, resultBytes);
            var cursor = new ShellTextCursor(argumentText, argumentLength);
            var token = new APTR(64);
            var tokenCapacity = 96u;
            var message = string.Empty;
            var optionsStarted = false;
            var to = string.Empty;
            while (true)
            {
                var tokenResult = ShellTextParser.NextToken(ref this, ref cursor,
                    token, tokenCapacity, out var tokenLength, out var tokenFlags);
                if (tokenResult == (int)ShellTextTokenResult.End) break;
                if (tokenResult != (int)ShellTextTokenResult.Token)
                    return false;
                var value = Store.ReadText(token, tokenLength);
                if (tokenFlags == 0 && string.Equals(value, "NOLINE",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (ReadUInt32(resultArray, 4) != 0) return false;
                    WriteUInt32(resultArray, 4, 1);
                    optionsStarted = true;
                    continue;
                }
                if (tokenFlags == 0 && (string.Equals(value, "FIRST",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "LEN", StringComparison.OrdinalIgnoreCase)))
                {
                    var numberResult = ShellTextParser.NextToken(ref this,
                        ref cursor, token, tokenCapacity, out var numberLength,
                        out var numberFlags);
                    if (numberResult != (int)ShellTextTokenResult.Token ||
                        numberFlags != 0 || !ShellNumberParser.TryParseUnsigned(
                            ref this, token, numberLength, out var number))
                        return false;
                    var destination = string.Equals(value, "FIRST",
                        StringComparison.OrdinalIgnoreCase) ? 8 : 12;
                    if (ReadUInt32(resultArray, destination) != 0) return false;
                    var numberAddress = destination == 8 ? new APTR(184) :
                        new APTR(188);
                    WriteUInt32(numberAddress, 0, number);
                    WriteUInt32(resultArray, destination, numberAddress.Raw);
                    optionsStarted = true;
                    continue;
                }
                if (tokenFlags == 0 && string.Equals(value, "TO",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (to.Length != 0) return false;
                    var pathResult = ShellTextParser.NextToken(ref this,
                        ref cursor, token, tokenCapacity, out var pathLength,
                        out var pathFlags);
                    if (pathResult != (int)ShellTextTokenResult.Token ||
                        pathLength == 0)
                        return false;
                    to = Store.ReadText(token, pathLength);
                    var path = Store.PutAt(240, to);
                    WriteUInt8(path, to.Length, 0);
                    WriteUInt32(resultArray, 16, path.Raw);
                    optionsStarted = true;
                    continue;
                }
                if (optionsStarted) return false;
                if (message.Length != 0) message += " ";
                message += value;
            }
            var messageAddress = Store.PutAt(192, message);
            WriteUInt8(messageAddress, message.Length, 0);
            WriteUInt32(new APTR(176), 0, messageAddress.Raw);
            WriteUInt32(new APTR(180), 0, 0);
            WriteUInt32(resultArray, 0, new APTR(176).Raw);
            rdArgs = new APTR(240 - 4);
            Store.ReadArgsCount++;
            return true;
        }

        public void FreeArgs(APTR rdArgs)
        {
            if (rdArgs.IsNotNull)
                Store.FreeArgsCount++;
        }

        public bool TryGetLocalVariable(
            APTR cli,
            APTR name,
            uint nameLength,
            APTR value,
            uint valueCapacity,
            out uint valueLength)
        {
            valueLength = 0;
            if (cli.IsNull || Store.LocalLookupFailure || name.IsNull ||
                value.IsNull || !IsMapped(name, nameLength) ||
                !IsMapped(value, valueCapacity))
                return false;

            string requestedName = Store.ReadText(name, nameLength);
            if (!string.Equals(requestedName, Store.LocalVariableName,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(
                Store.LocalVariableValue);
            if ((uint)bytes.Length > valueCapacity)
                return false;
            bytes.CopyTo(Store.Memory, checked((int)value.Raw));
            valueLength = (uint)bytes.Length;
            return true;
        }

        public bool TrySetLocalVariable(
            APTR cli,
            APTR name,
            uint nameLength,
            APTR value,
            uint valueLength)
        {
            if (cli.IsNull || Store.LocalSetFailure || name.IsNull ||
                !IsMapped(name, nameLength) ||
                (valueLength != 0 &&
                 (value.IsNull || !IsMapped(value, valueLength))))
                return false;

            Store.LocalVariableName = Store.ReadText(name, nameLength);
            Store.LocalVariableValue = valueLength == 0
                ? string.Empty
                : Store.ReadText(value, valueLength);
            Store.LocalSetCount++;
            return true;
        }

        public bool TryWriteLocalVariables(BPTR output, APTR cli)
        {
            if (output.IsNull || cli.IsNull || Store.LocalListFailure)
                return false;
            Store.LocalListCount++;
            Store.Output.AddRange(System.Text.Encoding.ASCII.GetBytes(
                Store.LocalVariableListing));
            return true;
        }

        public bool TryGetGlobalVariable(
            APTR name,
            uint nameLength,
            APTR value,
            uint valueCapacity,
            out uint valueLength)
        {
            valueLength = 0;
            if (Store.GlobalLookupFailure || name.IsNull || value.IsNull ||
                !IsMapped(name, nameLength) || !IsMapped(value, valueCapacity))
                return false;

            string requestedName = Store.ReadText(name, nameLength);
            if (!string.Equals(requestedName, Store.GlobalVariableName,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(
                Store.GlobalVariableValue);
            if ((uint)bytes.Length > valueCapacity)
                return false;
            bytes.CopyTo(Store.Memory, checked((int)value.Raw));
            valueLength = (uint)bytes.Length;
            return true;
        }

        public bool TrySetGlobalVariable(
            APTR name,
            uint nameLength,
            APTR value,
            uint valueLength,
            uint save)
        {
            if (Store.GlobalSetFailure || name.IsNull ||
                !IsMapped(name, nameLength) ||
                (valueLength != 0 &&
                 (value.IsNull || !IsMapped(value, valueLength))))
                return false;

            Store.GlobalVariableName = Store.ReadText(name, nameLength);
            Store.GlobalVariableValue = valueLength == 0
                ? string.Empty
                : Store.ReadText(value, valueLength);
            Store.GlobalSaveFlag = save;
            Store.GlobalSetCount++;
            return true;
        }

        public bool TryWriteGlobalVariables(BPTR output)
        {
            if (output.IsNull || Store.GlobalListFailure)
                return false;
            Store.GlobalListCount++;
            Store.Output.AddRange(System.Text.Encoding.ASCII.GetBytes(
                Store.GlobalVariableListing));
            return true;
        }

        public bool TryRemoveLocalVariable(APTR cli, APTR name, uint nameLength)
        {
            if (cli.IsNull || Store.LocalRemoveFailure || name.IsNull ||
                !IsMapped(name, nameLength))
                return false;

            if (!string.Equals(Store.ReadText(name, nameLength),
                    Store.LocalVariableName, StringComparison.OrdinalIgnoreCase))
                return false;
            Store.LocalVariableName = string.Empty;
            Store.LocalVariableValue = string.Empty;
            Store.LocalRemoveCount++;
            return true;
        }

        public bool TryRemoveGlobalVariable(APTR name, uint nameLength, uint save)
        {
            if (Store.GlobalRemoveFailure || name.IsNull ||
                !IsMapped(name, nameLength))
                return false;

            if (!string.Equals(Store.ReadText(name, nameLength),
                    Store.GlobalVariableName, StringComparison.OrdinalIgnoreCase))
                return false;
            Store.GlobalVariableName = string.Empty;
            Store.GlobalVariableValue = string.Empty;
            Store.GlobalRemoveSaveFlag = save;
            Store.GlobalRemoveCount++;
            return true;
        }

        public bool ClearConsole(BPTR output, uint reset)
        {
            if (output.IsNull || Store.ClearConsoleFailure)
                return false;
            Store.ClearConsoleReset = reset;
            Store.ClearConsoleCount++;
            return true;
        }

        public bool TryWriteWhy(BPTR output, APTR cli)
        {
            if (output.IsNull || cli.IsNull || Store.WhyFailure)
                return false;
            Store.WhyCount++;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(Store.WhyText);
            Store.Output.AddRange(bytes);
            return true;
        }

        public bool TryWriteFault(BPTR output, APTR errorCodes, uint errorCount)
        {
            if (output.IsNull || Store.FaultFailure || errorCodes.IsNull ||
                errorCodes.Raw > uint.MaxValue - errorCount * 4u ||
                !IsMapped(errorCodes, errorCount * 4u))
                return false;

            Store.FaultCodes.Clear();
            for (uint index = 0; index < errorCount; index++)
                Store.FaultCodes.Add(ReadUInt32(
                    errorCodes,
                    checked((int)(index * 4u))));
            Store.FaultCount = errorCount;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(Store.FaultText);
            Store.Output.AddRange(bytes);
            return true;
        }

        public bool TrySetPrompt(APTR cli, APTR value, uint valueLength, uint reset)
        {
            if (cli.IsNull || Store.PromptFailure ||
                (valueLength != 0 &&
                 (value.IsNull || !IsMapped(value, valueLength))))
                return false;

            Store.PromptReset = reset;
            Store.PromptValue = valueLength == 0
                ? string.Empty
                : Store.ReadText(value, valueLength);
            Store.PromptCount++;
            return true;
        }
    }

    public sealed class GuestStore
    {
        public readonly byte[] Memory = new byte[4096];
        public readonly List<byte> Output = new();
        public bool ShortWrite;
        public string OpenedPath = string.Empty;
        public BPTR ClosedHandle;
        public int DefaultStack = 8192;
        public int RunningStack = 4096;
        public int WriteStackCount;
        public bool ReadStackFailure;
        public bool WriteStackFailure;
        public bool LocalLookupFailure;
        public bool LocalListFailure;
        public string LocalVariableListing = string.Empty;
        public int LocalListCount;
        public bool WriteFailureLimitFailure;
        public uint FailureLimit = 10;
        public int WriteFailureLimitCount;
        public bool CurrentDirectoryFailure;
        public bool ChangeDirectoryFailure;
        public string CurrentDirectory = "SYS:";
        public int ChangeDirectoryCount;
        public bool AliasSetFailure;
        public bool AliasRemoveFailure;
        public bool AliasListFailure;
        public string AliasName = string.Empty;
        public string AliasValue = string.Empty;
        public string AliasListing = string.Empty;
        public int AliasSetCount;
        public int AliasRemoveCount;
        public bool CommandPathUpdateFailure;
        public bool CommandPathListFailure;
        public readonly List<string> CommandPathEntries = new();
        public string CommandPathListing = string.Empty;
        public uint CommandPathOperation;
        public uint CommandPathQuiet;
        public int CommandPathUpdateCount;
        public bool ControlFailure;
        public APTR BoundCli;
        public APTR BoundFrame;
        public ShellControlAction LastControlAction;
        public int LastControlReturnCode;
        public int ControlCount;
        public bool LabelDefineFailure;
        public string LastLabel = string.Empty;
        public int LabelDefineCount;
        public bool SkipFailure;
        public string LastSkipLabel = string.Empty;
        public uint SkipBack;
        public int SkipCount;
        public bool AskFailure;
        public string AskPrompt = string.Empty;
        public int AskCount;
        public bool IfFailure;
        public uint IfCondition;
        public uint IfThreshold;
        public uint IfNegate;
        public uint IfNoRequester;
        public uint IfNumeric;
        public string IfLeft = string.Empty;
        public string IfRight = string.Empty;
        public int IfCount;
        public bool ExecuteFailure;
        public string ExecutedScript = string.Empty;
        public int ExecuteCount;
        public bool ScriptReadFailure;
        public bool ScriptExecuteFailure;
        public bool ScriptAliasFailure;
        public bool ScriptLookupFailure;
        public bool ScriptSignalPollFailure;
        public bool ScriptSignalAcknowledgeFailure;
        public ShellScriptSignalFlags ScriptSignalFlags;
        public int ScriptSignalResult;
        public uint ScriptSignalSequence = 1;
        public int ScriptSignalPollCount;
        public int ScriptSignalAcknowledgeCount;
        public ShellScriptSignalEvent LastScriptSignal;
        public string ScriptText = string.Empty;
        public string LastScriptExternalCommand = string.Empty;
        public string LastScriptAliasSource = string.Empty;
        public string ScriptAliasReplacement = string.Empty;
        public int ScriptAliasExpansionCount;
        public ShellScriptLookupKind ScriptLookupKind =
            ShellScriptLookupKind.CommandPath;
        public string ScriptLookupPath = string.Empty;
        public string LastScriptLookupName = string.Empty;
        public ShellScriptLookupKind LastScriptLookupKind;
        public string LastScriptResolvedPath = string.Empty;
        public int ScriptLookupCount;
        public BPTR LastScriptInput;
        public BPTR LastScriptOutput;
        public BPTR LastScriptError;
        public int ScriptExecuteCount;
        public int ScriptCommandResult = (int)ShellCommandResult.Ok;
        public bool ScriptExternalPending;
        public APTR ScriptExternalContinuation;
        public bool RedirectionInputFailure;
        public bool RedirectionOutputFailure;
        public bool RedirectionCloseFailure;
        public string RedirectionInputPath = string.Empty;
        public string RedirectionOutputPath = string.Empty;
        public uint RedirectionOutputAppend;
        public int RedirectionOpenCount;
        public int RedirectionCloseCount;
        public BPTR LastClosedRedirection;
        public bool RunFailure;
        public string RunCommand = string.Empty;
        public uint RunDetach;
        public uint RunQuiet;
        public uint RunStack;
        public uint RunStackPresent;
        public int RunPriority;
        public uint RunPriorityPresent;
        public BPTR RunInput;
        public BPTR RunOutput;
        public BPTR RunError;
        public BPTR RunCurrentDirectory;
        public APTR RunContinuation;
        public int RunCount;
        public bool ResidentFailure;
        public string ResidentName = string.Empty;
        public string ResidentFile = string.Empty;
        public string ResidentAlias = string.Empty;
        public uint ResidentRemove;
        public uint ResidentAdd;
        public uint ResidentReplace;
        public uint ResidentForce;
        public uint ResidentSystem;
        public uint ResidentDefer;
        public int ResidentCount;
        public bool ShellLaunchFailure;
        public ShellLaunchKind ShellLaunchKind;
        public BPTR ShellInput;
        public BPTR ShellOutput;
        public BPTR ShellError;
        public BPTR ShellCurrentDirectory;
        public APTR ShellContinuation;
        public bool ContinuationPollFailure;
        public ShellProcessContinuationState ContinuationObservedState =
            ShellProcessContinuationState.Running;
        public int ContinuationResult;
        public int ContinuationPollCount;
        public bool ContinuationReleaseFailure;
        public APTR LastReleasedContinuation;
        public uint LastReleasedFlags;
        public int ContinuationReleaseCount;
        public string ShellWindow = string.Empty;
        public string ShellFrom = string.Empty;
        public int ShellLaunchCount;
        public bool ReadArgsFailure;
        public int ReadArgsCount;
        public int FreeArgsCount;
        public string LocalVariableName = string.Empty;
        public string LocalVariableValue = string.Empty;
        public bool LocalSetFailure;
        public int LocalSetCount;
        public bool GlobalLookupFailure;
        public bool GlobalListFailure;
        public string GlobalVariableListing = string.Empty;
        public int GlobalListCount;
        public bool GlobalSetFailure;
        public string GlobalVariableName = string.Empty;
        public string GlobalVariableValue = string.Empty;
        public uint GlobalSaveFlag;
        public int GlobalSetCount;
        public bool LocalRemoveFailure;
        public bool GlobalRemoveFailure;
        public uint GlobalRemoveSaveFlag;
        public int LocalRemoveCount;
        public int GlobalRemoveCount;
        public bool ClearConsoleFailure;
        public uint ClearConsoleReset;
        public int ClearConsoleCount;
        public bool WhyFailure;
        public string WhyText = string.Empty;
        public int WhyCount;
        public bool FaultFailure;
        public string FaultText = string.Empty;
        public readonly List<uint> FaultCodes = new();
        public uint FaultCount;
        public bool PromptFailure;
        public string PromptValue = string.Empty;
        public uint PromptReset;
        public int PromptCount;

        public APTR Put(string value)
        {
            return PutAt(32, value);
        }

        public APTR PutAt(int address, string value)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
            bytes.CopyTo(Memory, address);
            return new APTR((uint)address);
        }

        public void Append(APTR source, uint length)
        {
            for (uint index = 0; index < length; index++)
                Output.Add(Memory[checked((int)source.Raw + (int)index)]);
        }

        public string ReadText(APTR source, uint length) =>
            System.Text.Encoding.ASCII.GetString(
                Memory,
                checked((int)source.Raw),
                checked((int)length));

        public string OutputText => System.Text.Encoding.ASCII.GetString(Output.ToArray());
    }
}
