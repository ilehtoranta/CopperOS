using Amiga;

namespace CopperOS.Shell;

/// <summary>Result of one bounded script-line attempt.</summary>
public enum ShellScriptStepStatus : int
{
    Executed = 0,
    Skipped = 1,
    Empty = 2,
    EndOfFile = 3,
    Malformed = 10,
    InvalidFrame = 11,
	PlatformFailure = 12,
	Interrupted = 13,
	Terminated = 14,
	StepLimit = 15,
	Waiting = 16,
}

/// <summary>
/// Fixed-width result returned by <see cref="ShellScriptEngine.Step"/>.
/// </summary>
public struct ShellScriptStepResult
{
    public ShellScriptStepResult(
        ShellScriptStepStatus status,
        ShellInternalCommand command,
        int commandResult,
        uint line,
        uint offset,
        uint lineLength)
    {
        Status = status;
        Command = command;
        CommandResult = commandResult;
        Line = line;
        Offset = offset;
        LineLength = lineLength;
    }

    public ShellScriptStepStatus Status { get; set; }
    public ShellInternalCommand Command { get; set; }
    public int CommandResult { get; set; }
    public uint Line { get; set; }
    public uint Offset { get; set; }
    public uint LineLength { get; set; }
}

/// <summary>
/// Fixed-width result for a bounded startup-script run. The caller supplies
/// the frame and reusable workspace; no managed collection or stack of nested
/// commands is created by the runner.
/// </summary>
public struct ShellScriptRunResult
{
	public ShellScriptRunResult(ShellScriptStepStatus status, int result,
		uint steps)
	{
		Status = status;
		Result = result;
		Steps = steps;
	}

	public ShellScriptStepStatus Status { get; set; }
	public int Result { get; set; }
	public uint Steps { get; set; }
}

/// <summary>
/// Caller-owned buffers used for one script-line step.  The command workspace
/// is reused by the dispatcher and is never retained by the engine.
/// </summary>
public struct ShellScriptStepWorkspace
{
    public ShellScriptStepWorkspace(
        APTR line,
        uint lineCapacity,
        APTR commandName,
        uint commandNameCapacity,
        in ShellCommandWorkspace commandWorkspace)
    {
        Line = line;
        LineCapacity = lineCapacity;
        CommandName = commandName;
        CommandNameCapacity = commandNameCapacity;
        CommandWorkspace = commandWorkspace;
        Redirection = default;
        AliasExpansion = default;
        Lookup = default;
    }

    public ShellScriptStepWorkspace(
        APTR line,
        uint lineCapacity,
        APTR commandName,
        uint commandNameCapacity,
        in ShellCommandWorkspace commandWorkspace,
        in ShellRedirectionWorkspace redirection)
    {
        Line = line;
        LineCapacity = lineCapacity;
        CommandName = commandName;
        CommandNameCapacity = commandNameCapacity;
        CommandWorkspace = commandWorkspace;
        Redirection = redirection;
        AliasExpansion = default;
        Lookup = default;
    }

    public ShellScriptStepWorkspace(
        APTR line,
        uint lineCapacity,
        APTR commandName,
        uint commandNameCapacity,
        in ShellCommandWorkspace commandWorkspace,
        in ShellRedirectionWorkspace redirection,
        in ShellScriptAliasWorkspace aliasExpansion)
    {
        Line = line;
        LineCapacity = lineCapacity;
        CommandName = commandName;
        CommandNameCapacity = commandNameCapacity;
        CommandWorkspace = commandWorkspace;
        Redirection = redirection;
        AliasExpansion = aliasExpansion;
        Lookup = default;
    }

    public ShellScriptStepWorkspace(
        APTR line,
        uint lineCapacity,
        APTR commandName,
        uint commandNameCapacity,
        in ShellCommandWorkspace commandWorkspace,
        in ShellRedirectionWorkspace redirection,
        in ShellScriptAliasWorkspace aliasExpansion,
        in ShellScriptLookupWorkspace lookup)
    {
        Line = line;
        LineCapacity = lineCapacity;
        CommandName = commandName;
        CommandNameCapacity = commandNameCapacity;
        CommandWorkspace = commandWorkspace;
        Redirection = redirection;
        AliasExpansion = aliasExpansion;
        Lookup = lookup;
    }

    public APTR Line { get; set; }
    public uint LineCapacity { get; set; }
    public APTR CommandName { get; set; }
    public uint CommandNameCapacity { get; set; }
    public ShellCommandWorkspace CommandWorkspace { get; set; }
    public ShellRedirectionWorkspace Redirection { get; set; }
    public ShellScriptAliasWorkspace AliasExpansion { get; set; }
    public ShellScriptLookupWorkspace Lookup { get; set; }
}

/// <summary>
/// Fixed-width startup handoff for one DOS-owned Shell script.
/// The caller owns the guest frame and all workspace buffers; the engine only
/// initializes the frame and runs it for the requested bounded number of
/// steps.  No script text or continuation is retained by this value.
/// </summary>
public readonly struct ShellScriptStartRequest
{
	public ShellScriptStartRequest(
		APTR frame,
		in ShellScriptFrameState initial,
		in ShellScriptStepWorkspace workspace,
		uint maximumSteps)
	{
		Frame = frame;
		Initial = initial;
		Workspace = workspace;
		MaximumSteps = maximumSteps;
	}

	public APTR Frame { get; }
	public ShellScriptFrameState Initial { get; }
	public ShellScriptStepWorkspace Workspace { get; }
	public uint MaximumSteps { get; }
}

/// <summary>
/// Executes one bounded script line against the guest-resident frame.  This
/// is deliberately a stepper, not a scheduler: DOS owns input buffering,
/// external lookup, process creation, and continuation policy.
/// </summary>
public static class ShellScriptEngine
{
	private static ShellScriptStepResult MakeStep(ShellScriptStepStatus status,
		ShellInternalCommand command, int commandResult, uint line,
		uint offset, uint lineLength)
	{
		var value = default(ShellScriptStepResult);
		value.Status = status;
		value.Command = command;
		value.CommandResult = commandResult;
		value.Line = line;
		value.Offset = offset;
		value.LineLength = lineLength;
		return value;
	}

	private static ShellScriptRunResult MakeRun(ShellScriptStepStatus status,
		int result, uint steps)
	{
		var value = default(ShellScriptRunResult);
		value.Status = status;
		value.Result = result;
		value.Steps = steps;
		return value;
	}

	/// <summary>
	/// Initializes a caller-owned guest frame and runs the bounded startup
	/// script.  This is the handoff used by a native DOS/Shell adapter: DOS
	/// supplies the initial handles/cursor and reusable buffers, while Shell
	/// performs no allocation and retains no process state.
	/// </summary>
	public static ShellScriptRunResult Start<TPlatform>(
		ref TPlatform platform,
		in ShellScriptStartRequest request)
		where TPlatform : struct, IShellPlatform, IShellScriptPlatform
	{
		if (request.MaximumSteps == 0)
			return MakeRun(ShellScriptStepStatus.StepLimit,
				(int)ShellCommandResult.Error, 0);

		var frame = request.Frame;
		var initial = request.Initial;
		var workspace = request.Workspace;
		if (frame.IsNull ||
			!ValidWorkspace(ref platform, in workspace) ||
			!ShellScriptFrameCodec.Initialize(
				ref platform, frame, in initial))
			return MakeRun(
				ShellScriptStepStatus.InvalidFrame,
				(int)ShellCommandResult.Error, 0);

		if (!platform.TryBindScriptFrame(initial.Cli, frame))
			return MakeRun(
				ShellScriptStepStatus.PlatformFailure,
				(int)ShellCommandResult.Error, 0);
		var result = Run(ref platform, frame, in workspace,
			request.MaximumSteps);
		// A persistent DOS runner owns the binding while a foreground child is
		// pending. Its poll path performs the eventual unbind after teardown.
		if (result.Status != ShellScriptStepStatus.Waiting)
			platform.TryUnbindScriptFrame(initial.Cli, frame);
		return result;
	}

	public static ShellScriptStepStatus Step<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        in ShellScriptStepWorkspace workspace,
        out ShellScriptStepResult step)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        step = MakeStep(
            ShellScriptStepStatus.InvalidFrame,
            ShellInternalCommand.Unknown,
            (int)ShellCommandResult.Error,
            0,
            0,
            0);

        if (!ValidWorkspace(ref platform, in workspace) ||
            !ShellScriptFrameCodec.TryRead(ref platform, frame,
                out var state) || state.Cli.IsNull ||
            (state.Flags & ShellScriptFrameFlags.Active) == 0)
            return step.Status;

        if (!platform.TryPollScriptSignal(state.Cli, out var signal) ||
            !ValidSignal(signal.Flags))
        {
        step = MakeStep(
                ShellScriptStepStatus.PlatformFailure,
                ShellInternalCommand.Unknown,
                (int)ShellCommandResult.Error,
                state.CurrentLine,
                state.CurrentOffset,
                0);
		return step.Status;
	}

        if (signal.Flags != ShellScriptSignalFlags.None)
        {
            if ((state.SignalState.IsNotNull &&
                 (!ShellScriptSignalCodec.TryRecord(
                     ref platform, state.SignalState, in signal) ||
                  !platform.TryAcknowledgeScriptSignal(
                      state.Cli, in signal) ||
                  !ShellScriptSignalCodec.TryAcknowledge(
                      ref platform, state.SignalState, signal.Sequence))) ||
                (state.SignalState.IsNull &&
                 !platform.TryAcknowledgeScriptSignal(
                     state.Cli, in signal)) ||
                !ShellScriptFrameCodec.TryApplySignal(
                    ref platform, frame, signal.Flags, signal.Result))
            {
        step = MakeStep(
                    ShellScriptStepStatus.PlatformFailure,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error,
                    state.CurrentLine,
                    state.CurrentOffset,
                    0);
                return step.Status;
            }

            var signalStatus = (signal.Flags &
                ShellScriptSignalFlags.Terminated) != 0
                ? ShellScriptStepStatus.Terminated
                : ShellScriptStepStatus.Interrupted;
        step = MakeStep(
                signalStatus,
                ShellInternalCommand.Unknown,
                signal.Result,
                state.CurrentLine,
                state.CurrentOffset,
                0);
            return step.Status;
        }

        // A foreground external command keeps the current line and its
        // resume cursor in the guest frame. Poll once and yield; never spin
        // in the Shell or invoke child code recursively.
        if (state.PendingCommand.IsNotNull)
        {
            if (!ShellProcessContinuationPolling.TryPoll(ref platform,
                    state.Cli, state.PendingCommand, out var childState,
                    out var childResult))
            {
        step = MakeStep(
                    ShellScriptStepStatus.PlatformFailure,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error,
                    state.CurrentLine,
                    state.CurrentOffset,
                    0);
                return step.Status;
            }
            if (childState is ShellProcessContinuationState.Pending or
                ShellProcessContinuationState.Running)
            {
        step = MakeStep(
                    ShellScriptStepStatus.Waiting,
                    ShellInternalCommand.Unknown,
                    state.LastResult,
                    state.CurrentLine,
                    state.CurrentOffset,
                    0);
                return step.Status;
            }
            var continuation = state.PendingCommand;
            if (!ShellProcessContinuationTeardown.TryRelease(ref platform,
                    state.Cli, continuation) ||
                !ShellScriptFrameCodec.TrySetPendingCommand(ref platform,
                    frame, APTR.Null, 0, 0) ||
                !ShellScriptFrameCodec.TryRecordResult(ref platform, frame,
                    childResult) ||
                !Advance(ref platform, frame, state.PendingNextLine,
                    state.PendingNextOffset))
            {
        step = MakeStep(
                    ShellScriptStepStatus.PlatformFailure,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error,
                    state.CurrentLine,
                    state.CurrentOffset,
                    0);
                return step.Status;
            }
        step = MakeStep(
                ShellScriptStepStatus.Executed,
                ShellInternalCommand.Unknown,
                childResult,
                state.PendingNextLine,
                state.PendingNextOffset,
                0);
            return step.Status;
        }

        if (!platform.TryReadScriptLine(
                state.Cli,
                state.Input,
                state.CurrentLine,
                state.CurrentOffset,
                workspace.Line,
                workspace.LineCapacity,
                out var lineLength,
                out var nextLine,
                out var nextOffset,
                out var endOfFile) ||
            endOfFile > 1 ||
            lineLength >= workspace.LineCapacity ||
            nextLine < state.CurrentLine ||
            nextOffset < state.CurrentOffset)
        {
        step = MakeStep(
                ShellScriptStepStatus.PlatformFailure,
                ShellInternalCommand.Unknown,
                (int)ShellCommandResult.Error,
                state.CurrentLine,
                state.CurrentOffset,
                0);
            return step.Status;
        }

        if (state.InputState.IsNotNull)
        {
            if (!ShellScriptInputCodec.TryRead(
                    ref platform, state.InputState, out var inputState) ||
                inputState.Handle.Raw != state.Input.Raw ||
                inputState.Buffer.Raw != workspace.Line.Raw ||
                inputState.Capacity > workspace.LineCapacity ||
                !ShellScriptInputCodec.TryRecordLine(
                    ref platform,
                    state.InputState,
                    state.CurrentLine,
                    state.CurrentOffset,
                    lineLength,
                    endOfFile))
            {
        step = MakeStep(
                    ShellScriptStepStatus.PlatformFailure,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error,
                    state.CurrentLine,
                    state.CurrentOffset,
                    0);
                return step.Status;
            }
        }

        if (endOfFile != 0 && lineLength == 0)
        {
        step = MakeStep(
                ShellScriptStepStatus.EndOfFile,
                ShellInternalCommand.Unknown,
                state.LastResult,
                state.CurrentLine,
                state.CurrentOffset,
                0);
            return step.Status;
        }

        var commandSource = workspace.Line;
        var commandLength = lineLength;
        if (workspace.AliasExpansion.IsEnabled && commandLength != 0)
        {
            var aliasWorkspace = workspace.AliasExpansion;
            if (!platform.TryExpandScriptAlias(
                    state.Cli,
                    commandSource,
                    commandLength,
                    aliasWorkspace.Line,
                    aliasWorkspace.Capacity,
                out var expanded,
                out var expandedLength) ||
                expanded > 1 ||
                (expanded == 0 && expandedLength != 0) ||
                expandedLength >= aliasWorkspace.Capacity ||
                (expanded != 0 &&
                 !platform.IsMapped(aliasWorkspace.Line, expandedLength)))
            {
                RecordFailureAndAdvance(ref platform, frame, nextLine,
                    nextOffset, (int)ShellCommandResult.Error);
        step = MakeStep(
                    ShellScriptStepStatus.PlatformFailure,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error,
                    nextLine,
                    nextOffset,
                    lineLength);
                return step.Status;
            }
            if (expanded != 0)
            {
                commandSource = aliasWorkspace.Line;
                commandLength = expandedLength;
            }
        }

        var redirection = default(ShellRedirectionSpec);
        if (workspace.Redirection.IsEnabled)
        {
            var redirectionWorkspace = workspace.Redirection;
            if (!ShellRedirectionParser.Parse(
                    ref platform,
                    commandSource,
                    commandLength,
                    in redirectionWorkspace,
                    out redirection,
                    out commandLength))
            {
                RecordFailureAndAdvance(ref platform, frame, nextLine,
                    nextOffset, (int)ShellCommandResult.Error);
        step = MakeStep(
                    ShellScriptStepStatus.Malformed,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error,
                    nextLine,
                    nextOffset,
                    lineLength);
                return step.Status;
            }
            commandSource = redirectionWorkspace.Command;
        }

        var cursor = new ShellTextCursor(commandSource, commandLength);
        var tokenResult = ShellTextParser.NextToken(
            ref platform,
            ref cursor,
            workspace.CommandName,
            workspace.CommandNameCapacity,
            out var commandNameLength,
            out _);
        if (tokenResult == (int)ShellTextTokenResult.End)
        {
            if (!Advance(ref platform, frame, nextLine, nextOffset))
                return FailAfterRead(ref platform, frame, state,
                    lineLength, out step);
        step = MakeStep(
                ShellScriptStepStatus.Empty,
                ShellInternalCommand.Unknown,
                state.LastResult,
                nextLine,
                nextOffset,
                lineLength);
            return step.Status;
        }

        if (tokenResult != (int)ShellTextTokenResult.Token ||
            commandNameLength == 0)
        {
            RecordFailureAndAdvance(ref platform, frame, nextLine,
                nextOffset, (int)ShellCommandResult.Error);
        step = MakeStep(
                ShellScriptStepStatus.Malformed,
                ShellInternalCommand.Unknown,
                (int)ShellCommandResult.Error,
                nextLine,
                nextOffset,
                lineLength);
            return step.Status;
        }

        var command = ShellInternalCommandResolver.Resolve(
            ref platform,
            workspace.CommandName,
            commandNameLength);
        var lookupKind = ShellScriptLookupKind.NotFound;
        var resolvedPath = workspace.Lookup.Path;
        var resolvedPathLength = 0u;
        if (command == ShellInternalCommand.Unknown)
        {
            var lookupWorkspace = workspace.Lookup;
            if (!platform.TryLookupScriptCommand(
                    state.Cli,
                    workspace.CommandName,
                    commandNameLength,
                    lookupWorkspace.Path,
                    lookupWorkspace.Capacity,
                    out lookupKind,
                    out resolvedPathLength) ||
                (uint)lookupKind > (uint)ShellScriptLookupKind.Malformed ||
                (resolvedPathLength != 0 &&
                 (resolvedPath.IsNull ||
                  resolvedPathLength >= lookupWorkspace.Capacity ||
                  !platform.IsMapped(resolvedPath, resolvedPathLength))))
            {
                RecordFailureAndAdvance(ref platform, frame, nextLine,
                    nextOffset, (int)ShellCommandResult.Error);
        step = MakeStep(
                    ShellScriptStepStatus.PlatformFailure,
                    command,
                    (int)ShellCommandResult.Error,
                    nextLine,
                    nextOffset,
                    lineLength);
                return step.Status;
            }
        }
        var skipping = (state.Flags & ShellScriptFrameFlags.Skipping) != 0;
        if (skipping && !AllowedWhileSkipping(command))
        {
            if (!Advance(ref platform, frame, nextLine, nextOffset))
                return FailAfterRead(ref platform, frame, state,
                    lineLength, out step);
        step = MakeStep(
                ShellScriptStepStatus.Skipped,
                command,
                state.LastResult,
                nextLine,
                nextOffset,
                lineLength);
            return step.Status;
        }

        var redirectionHandles = default(ShellRedirectionHandles);
        if (!ShellRedirectionTransaction.TryOpen(
                ref platform, in state, in redirection,
                out redirectionHandles))
        {
            RecordFailureAndAdvance(ref platform, frame, nextLine,
                nextOffset, (int)ShellCommandResult.Error);
        step = MakeStep(
                ShellScriptStepStatus.PlatformFailure,
                command,
                (int)ShellCommandResult.Error,
                nextLine,
                nextOffset,
                lineLength);
            return step.Status;
        }

        int commandResult;
        var platformSuccess = true;
        var pendingContinuation = APTR.Null;
        if (command == ShellInternalCommand.Unknown)
        {
            platformSuccess = platform.TryExecuteScriptCommand(
                    state.Cli,
                    frame,
                    commandSource,
                    commandLength,
                    lookupKind,
                    resolvedPath,
                    resolvedPathLength,
                    redirectionHandles.Input,
                    redirectionHandles.Output,
                    redirectionHandles.Error,
                    out commandResult,
                    out pendingContinuation);
        }
        else
        {
            var argumentText = commandSource;
            uint argumentLength = commandLength;
            if (cursor.Position <= commandLength)
            {
                argumentText = commandLength == cursor.Position
                    ? APTR.Null
                    : APTR.FromPointer(commandSource.Raw + cursor.Position);
                argumentLength = commandLength - cursor.Position;
            }

            var invocation = new CommandInvocation(
                argumentText,
                argumentLength,
                APTR.Null,
                APTR.Null,
                redirectionHandles.Input,
                redirectionHandles.Output,
                redirectionHandles.Error,
                state.CurrentDirectory,
                state.Cli,
                0,
                0);
            var commandWorkspace = workspace.CommandWorkspace;
            commandResult = ShellCommandDispatcher.Dispatch(
                ref platform,
                in invocation,
                command,
                in commandWorkspace);
        }

        if (!ShellRedirectionTransaction.Close(
                ref platform, in state, ref redirectionHandles))
            commandResult = (int)ShellCommandResult.Error;

        if (!platformSuccess)
        {
            RecordFailureAndAdvance(ref platform, frame, nextLine,
                nextOffset, (int)ShellCommandResult.Error);
        step = MakeStep(
                ShellScriptStepStatus.PlatformFailure,
                command,
                (int)ShellCommandResult.Error,
                nextLine,
                nextOffset,
                lineLength);
            return step.Status;
        }

        if (pendingContinuation.IsNotNull)
        {
            if (!ShellProcessContinuationCodec.TryRead(ref platform,
                    pendingContinuation, out var pendingState) ||
                pendingState.ParentCli.Raw != state.Cli.Raw ||
                pendingState.State is not (ShellProcessContinuationState.Pending
                    or ShellProcessContinuationState.Running) ||
                !ShellScriptFrameCodec.TrySetPendingCommand(ref platform,
                    frame, pendingContinuation, nextLine, nextOffset))
            {
                if (pendingContinuation.IsNotNull)
                    ShellProcessContinuationTransitions.TryFail(ref platform,
                        pendingContinuation, (int)ShellCommandResult.Error);
        step = MakeStep(
                    ShellScriptStepStatus.PlatformFailure,
                    command,
                    (int)ShellCommandResult.Error,
                    nextLine,
                    nextOffset,
                    lineLength);
                return step.Status;
            }
        step = MakeStep(
                ShellScriptStepStatus.Waiting,
                command,
                state.LastResult,
                state.CurrentLine,
                state.CurrentOffset,
                lineLength);
            return step.Status;
        }

        if (!ShellScriptFrameCodec.TryRecordResult(
                ref platform, frame, commandResult))
        {
        step = MakeStep(
                ShellScriptStepStatus.PlatformFailure,
                command,
                (int)ShellCommandResult.Error,
                nextLine,
                nextOffset,
                lineLength);
            return step.Status;
        }

        // A Skip/label owner may have moved the frame to a target.  Preserve
        // that guest-owned jump; ordinary commands advance to the next line.
        if (ShellScriptFrameCodec.TryRead(ref platform, frame,
                out var afterCommand) &&
            afterCommand.CurrentLine == state.CurrentLine &&
            afterCommand.CurrentOffset == state.CurrentOffset &&
            !Advance(ref platform, frame, nextLine, nextOffset))
        {
        step = MakeStep(
                ShellScriptStepStatus.PlatformFailure,
                command,
                (int)ShellCommandResult.Error,
                nextLine,
                nextOffset,
                lineLength);
            return step.Status;
        }

        step = MakeStep(
            ShellScriptStepStatus.Executed,
            command,
            commandResult,
            nextLine,
            nextOffset,
            lineLength);
        return step.Status;
    }

	/// <summary>
	/// Runs the active frame until EOF, a terminal signal, a platform failure,
	/// or the caller's explicit step bound. This is the synchronous execution
	/// boundary used for startup scripts; scheduling and process ownership stay
	/// with the platform implementation.
	/// </summary>
	public static ShellScriptRunResult Run<TPlatform>(
		ref TPlatform platform,
		APTR frame,
		in ShellScriptStepWorkspace workspace,
		uint maximumSteps)
		where TPlatform : struct, IShellPlatform, IShellScriptPlatform
	{
		if (maximumSteps == 0)
			return MakeRun(ShellScriptStepStatus.StepLimit,
				(int)ShellCommandResult.Error, 0);
		for (var steps = 0u; steps < maximumSteps; steps++)
		{
			var status = Step(ref platform, frame, in workspace, out var step);
			if (status is ShellScriptStepStatus.Executed or
				ShellScriptStepStatus.Skipped or ShellScriptStepStatus.Empty)
				continue;
			return MakeRun(status, step.CommandResult, steps + 1);
		}
		var result = (int)ShellCommandResult.Error;
		if (ShellScriptFrameCodec.TryRead(ref platform, frame, out var state))
			result = state.LastResult;
		return MakeRun(ShellScriptStepStatus.StepLimit,
			result, maximumSteps);
	}

    private static bool ValidWorkspace<TPlatform>(
        ref TPlatform platform,
        in ShellScriptStepWorkspace workspace)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        if (workspace.Line.IsNull || workspace.CommandName.IsNull ||
            workspace.LineCapacity < 2 || workspace.CommandNameCapacity < 2 ||
            workspace.Line.Raw > uint.MaxValue - workspace.LineCapacity ||
            workspace.CommandName.Raw >
                uint.MaxValue - workspace.CommandNameCapacity ||
            !platform.IsMapped(workspace.Line, workspace.LineCapacity) ||
            !platform.IsMapped(workspace.CommandName,
                workspace.CommandNameCapacity))
            return false;
        if (workspace.AliasExpansion.IsEnabled &&
            (workspace.AliasExpansion.Line.Raw >
                 uint.MaxValue - workspace.AliasExpansion.Capacity ||
             !platform.IsMapped(workspace.AliasExpansion.Line,
                 workspace.AliasExpansion.Capacity)))
            return false;
        if (workspace.Lookup.IsEnabled &&
            (workspace.Lookup.Path.Raw >
                 uint.MaxValue - workspace.Lookup.Capacity ||
             !platform.IsMapped(workspace.Lookup.Path,
                 workspace.Lookup.Capacity)))
            return false;
        return true;
    }

    private static bool AllowedWhileSkipping(ShellInternalCommand command) =>
        command is ShellInternalCommand.If or ShellInternalCommand.Else or
        ShellInternalCommand.EndIf or ShellInternalCommand.EndSkip or
        ShellInternalCommand.Lab or ShellInternalCommand.Skip or
        ShellInternalCommand.EndCLI or ShellInternalCommand.EndShell or
        ShellInternalCommand.Quit;

    private static bool Advance<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint line,
        uint offset)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform =>
        ShellScriptFrameCodec.TryAdvance(ref platform, frame, line, offset);

    private static void RecordFailureAndAdvance<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint line,
        uint offset,
        int result)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        ShellScriptFrameCodec.TryRecordResult(ref platform, frame, result);
        ShellScriptFrameCodec.TryAdvance(ref platform, frame, line, offset);
    }

    private static ShellScriptStepStatus FailAfterRead<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        in ShellScriptFrameState state,
        uint lineLength,
        out ShellScriptStepResult step)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        step = MakeStep(
            ShellScriptStepStatus.PlatformFailure,
            ShellInternalCommand.Unknown,
            (int)ShellCommandResult.Error,
            state.CurrentLine,
            state.CurrentOffset,
            lineLength);
        return step.Status;
    }

    private static bool ValidSignal(ShellScriptSignalFlags signal) =>
        ((uint)signal & ~(uint)(ShellScriptSignalFlags.Break |
            ShellScriptSignalFlags.CtrlC |
            ShellScriptSignalFlags.CtrlD |
            ShellScriptSignalFlags.Terminated)) == 0;
}
