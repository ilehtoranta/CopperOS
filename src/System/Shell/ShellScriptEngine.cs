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
	FailureLimitExceeded = 17,
	UnsupportedOperator = 18,
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

internal enum ShellScriptPromptProgress : uint
{
	Ready = 0,
	Continue = 1,
	Waiting = 2,
	Failed = 3,
}

/// <summary>
/// Caller-owned buffers used for one script-line step. The engine retains no
/// managed pointer state, but a deferred command may refer to text in this
/// workspace until the current logical line finishes. Keep its guest buffers
/// mapped and unchanged across those steps.
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
        ShellNumber = 0;
		PromptTemplate = APTR.Null;
		PromptTemplateCapacity = 0;
		PromptCapturePath = APTR.Null;
		PromptCapturePathCapacity = 0;
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
        ShellNumber = 0;
		PromptTemplate = APTR.Null;
		PromptTemplateCapacity = 0;
		PromptCapturePath = APTR.Null;
		PromptCapturePathCapacity = 0;
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
        ShellNumber = 0;
		PromptTemplate = APTR.Null;
		PromptTemplateCapacity = 0;
		PromptCapturePath = APTR.Null;
		PromptCapturePathCapacity = 0;
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
        ShellNumber = 0;
		PromptTemplate = APTR.Null;
		PromptTemplateCapacity = 0;
		PromptCapturePath = APTR.Null;
		PromptCapturePathCapacity = 0;
    }

    public APTR Line { get; set; }
    public uint LineCapacity { get; set; }
    public APTR CommandName { get; set; }
    public uint CommandNameCapacity { get; set; }
    public ShellCommandWorkspace CommandWorkspace { get; set; }
    public ShellRedirectionWorkspace Redirection { get; set; }
    public ShellScriptAliasWorkspace AliasExpansion { get; set; }
    public ShellScriptLookupWorkspace Lookup { get; set; }
    /// <summary>Public CLI number used by Execute's &lt;$$&gt; substitution.</summary>
    public uint ShellNumber { get; set; }
	/// <summary>Runner-owned stable snapshot of the current prompt template.</summary>
	public APTR PromptTemplate { get; set; }
	public uint PromptTemplateCapacity { get; set; }
	/// <summary>Runner-owned T: path used for one backtick output capture.</summary>
	public APTR PromptCapturePath { get; set; }
	public uint PromptCapturePathCapacity { get; set; }
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

	private static bool TryRestoreDeferredText<TPlatform>(
		ref TPlatform platform,
		in ShellScriptTextSlice source,
		APTR destination,
		uint destinationCapacity)
		where TPlatform : struct, IShellPlatform
	{
		if (source.Data.IsNull || source.Length == 0 ||
			source.Length >= destinationCapacity || destination.IsNull ||
			source.Data.Raw > uint.MaxValue - source.Length ||
			destination.Raw > uint.MaxValue - destinationCapacity ||
			!platform.IsMapped(source.Data, source.Length) ||
			!platform.IsMapped(destination, destinationCapacity))
			return false;

		var sourceEnd = source.Data.Raw + source.Length;
		if (destination.Raw > source.Data.Raw &&
			destination.Raw < sourceEnd)
		{
			var position = source.Length;
			while (position != 0)
			{
				position--;
				platform.WriteUInt8(destination, (int)position,
					platform.ReadUInt8(source.Data, (int)position));
			}
		}
		else
		{
			for (var position = 0u; position < source.Length; position++)
				platform.WriteUInt8(destination, (int)position,
					platform.ReadUInt8(source.Data, (int)position));
		}
		platform.WriteUInt8(destination, (int)source.Length, 0);
		return true;
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
		{
			platform.TryUnbindScriptFrame(initial.Cli, frame);
			if (!platform.TryWriteCliFailureLimit(initial.Cli,
				ShellScriptFrameCodec.DefaultFailureLimit))
				return MakeRun(ShellScriptStepStatus.PlatformFailure,
					(int)ShellCommandResult.Error, result.Steps);
		}
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

		if (!platform.TryReadCliFailureLimit(state.Cli,
			out var failureLimit) || failureLimit == 0 ||
			(state.FailureLimit != failureLimit &&
			 !ShellScriptFrameCodec.TrySetFailureLimit(ref platform, frame,
				 failureLimit)))
		{
			step = MakeStep(ShellScriptStepStatus.PlatformFailure,
				ShellInternalCommand.Unknown,
				(int)ShellCommandResult.Error, state.CurrentLine,
				state.CurrentOffset, 0);
			return step.Status;
		}
		state.FailureLimit = failureLimit;

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
            var promptCommandPending = state.PromptExpansion.Phase ==
                ShellScriptPromptExpansionPhase.WaitingCommand;
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
			if (!platform.TryReadContinuationDiagnostics(state.Cli,
					continuation, out var childDiagnostics) ||
				childDiagnostics.ReturnCode != childResult ||
				!ShellProcessContinuationTeardown.TryRelease(ref platform,
					state.Cli, continuation) ||
				!ShellScriptFrameCodec.TrySetPendingCommand(ref platform,
					frame, APTR.Null, 0, 0) ||
				!platform.TryPublishCommandDiagnostics(state.Cli,
					in childDiagnostics) ||
				!ShellScriptFrameCodec.TryRecordResult(ref platform, frame,
					childResult))
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
			var continueDeferredCommand =
				state.DeferredCommand.Kind ==
					ShellScriptDeferredCommandKind.OutputConcatenation ||
				(state.DeferredCommand.Kind ==
					ShellScriptDeferredCommandKind.ConditionalAnd &&
					!MeetsFailureLimit(childResult, state.FailureLimit));
			if (!continueDeferredCommand &&
				((state.DeferredCommand.Kind !=
					ShellScriptDeferredCommandKind.None &&
				  !ShellScriptFrameCodec.TrySetDeferredCommand(ref platform,
					frame, default)) ||
				 !Advance(ref platform, frame, state.PendingNextLine,
					state.PendingNextOffset)))
			{
				step = MakeStep(ShellScriptStepStatus.PlatformFailure,
					ShellInternalCommand.Unknown,
					(int)ShellCommandResult.Error, state.CurrentLine,
					state.CurrentOffset, 0);
				return step.Status;
			}
			if (promptCommandPending)
			{
				if (!ShellScriptFrameCodec.TryRead(ref platform, frame,
					out var completedPromptFrame))
				{
					step = MakeStep(ShellScriptStepStatus.PlatformFailure,
						ShellInternalCommand.Unknown,
						(int)ShellCommandResult.Error, state.CurrentLine,
						state.CurrentOffset, 0);
					return step.Status;
				}
				var promptExpansion = completedPromptFrame.PromptExpansion;
				promptExpansion.Phase =
					ShellScriptPromptExpansionPhase.ReadingCapture;
				if (!ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
					frame, in promptExpansion))
				{
					step = MakeStep(ShellScriptStepStatus.PlatformFailure,
						ShellInternalCommand.Unknown,
						(int)ShellCommandResult.Error, state.CurrentLine,
						state.CurrentOffset, 0);
					return step.Status;
				}
			}
			var concatenationPending = state.DeferredCommand.Kind ==
				ShellScriptDeferredCommandKind.OutputConcatenation;
			var childStatus = MeetsFailureLimit(childResult,
				state.FailureLimit) && !promptCommandPending &&
				!concatenationPending
				? ShellScriptStepStatus.FailureLimitExceeded
				: ShellScriptStepStatus.Executed;
		step = MakeStep(
				childStatus,
				ShellInternalCommand.Unknown,
                childResult,
                state.PendingNextLine,
                state.PendingNextOffset,
                0);
            return step.Status;
        }

        // A pending child's completion belongs to DOS and returned above.
        // Start the next command with fresh process error state while the
        // public CLI retains the previous result for commands such as Why.
        if (!platform.TryBeginCommandDiagnostics(state.Cli))
            return FailAfterRead(ref platform, frame, state, 0, out step);

        if ((state.Flags & ShellScriptFrameFlags.Interactive) != 0)
        {
			var promptProgress = AdvanceInteractivePrompt(ref platform, frame,
				in state, in workspace, out var promptCommand,
				out var promptCommandResult);
			if (promptProgress == ShellScriptPromptProgress.Failed)
			{
				step = MakeStep(ShellScriptStepStatus.PlatformFailure,
					promptCommand, (int)ShellCommandResult.Error,
					state.CurrentLine, state.CurrentOffset, 0);
				return step.Status;
			}
			if (promptProgress == ShellScriptPromptProgress.Waiting)
			{
				step = MakeStep(ShellScriptStepStatus.Waiting, promptCommand,
					state.LastResult, state.CurrentLine, state.CurrentOffset, 0);
				return step.Status;
			}
			if (promptProgress == ShellScriptPromptProgress.Continue)
			{
				step = MakeStep(ShellScriptStepStatus.Empty, promptCommand,
					promptCommandResult, state.CurrentLine,
					state.CurrentOffset, 0);
				return step.Status;
			}
        }

		var fromDeferredCommand = state.DeferredCommand.Kind !=
			ShellScriptDeferredCommandKind.None;
		uint lineLength;
		uint nextLine;
		uint nextOffset;
		uint endOfFile;
		if (fromDeferredCommand)
		{
			var deferred = state.DeferredCommand;
			if (deferred.Text.Length >= workspace.LineCapacity ||
				!TryRestoreDeferredText(ref platform, in deferred.Text,
					workspace.Line, workspace.LineCapacity) ||
				!ShellScriptFrameCodec.TrySetDeferredCommand(ref platform,
					frame, default))
			{
				step = MakeStep(ShellScriptStepStatus.PlatformFailure,
					ShellInternalCommand.Unknown,
					(int)ShellCommandResult.Error, state.CurrentLine,
					state.CurrentOffset, 0);
				return step.Status;
			}
			lineLength = deferred.Text.Length;
			nextLine = deferred.NextLine;
			nextOffset = deferred.NextOffset;
			endOfFile = 0;
		}
		else if (!platform.TryReadScriptLine(
				state.Cli,
				state.Input,
				state.CurrentLine,
				state.CurrentOffset,
				workspace.Line,
				workspace.LineCapacity,
				out lineLength,
				out nextLine,
				out nextOffset,
				out endOfFile) ||
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

		if (!fromDeferredCommand && state.InputState.IsNotNull)
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

        var directiveDot = (byte)'.';
        if (state.ScriptKeyTemplateLength != 0 &&
            !ShellScriptKeyExpansion.TryGetDot(ref platform,
                state.ScriptKeyTemplate, state.ScriptKeyTemplateLength,
                out directiveDot))
            return FailAfterRead(ref platform, frame, state, lineLength,
                out step);
        if (!ShellScriptDirective.TryParse(ref platform, workspace.Line,
                lineLength, directiveDot, out var directive,
                out var directiveArgument))
            return FailAfterRead(ref platform, frame, state, lineLength,
                out step);
        if (directive == ShellScriptDirectiveKind.Comment ||
            (lineLength != 0 && platform.ReadUInt8(workspace.Line, 0) ==
                (byte)';'))
        {
            if (!Advance(ref platform, frame, nextLine, nextOffset))
                return FailAfterRead(ref platform, frame, state, lineLength,
                    out step);
            step = MakeStep(ShellScriptStepStatus.Empty,
                ShellInternalCommand.Unknown, state.LastResult, nextLine,
                nextOffset, lineLength);
            return step.Status;
        }
        if (directive == ShellScriptDirectiveKind.Invalid)
        {
            RecordFailureAndAdvance(ref platform, frame, nextLine,
                nextOffset, (int)ShellCommandResult.Error);
            step = MakeStep(ShellScriptStepStatus.Malformed,
                ShellInternalCommand.Unknown, (int)ShellCommandResult.Error,
                nextLine, nextOffset, lineLength);
            return step.Status;
        }
        if (directive == ShellScriptDirectiveKind.Key)
        {
            var templateLength = lineLength - directiveArgument;
            if (state.CurrentLine != 1 || state.ScriptKeyTemplate.IsNull ||
                state.ScriptKeyTemplateLength != 0 || templateLength == 0 ||
                templateLength >= 4096 ||
                !platform.IsMapped(state.ScriptKeyTemplate, 4096) ||
                workspace.CommandWorkspace.ErrorCodes.IsNull ||
                workspace.CommandWorkspace.ErrorCodeCapacity < 4)
            {
                RecordFailureAndAdvance(ref platform, frame, nextLine,
                    nextOffset, (int)ShellCommandResult.Error);
                step = MakeStep(ShellScriptStepStatus.Malformed,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error, nextLine, nextOffset,
                    lineLength);
                return step.Status;
            }
            platform.Copy(APTR.FromPointer(workspace.Line.Raw +
                directiveArgument), state.ScriptKeyTemplate, templateLength);
            platform.WriteUInt8(state.ScriptKeyTemplate, (int)templateLength,
                0);
            platform.Clear(APTR.FromPointer(state.ScriptKeyTemplate.Raw +
                templateLength + 1), ShellScriptKeyExpansion.TemplateBufferCapacity -
                templateLength - 1);
            if (!ShellScriptKeyExpansion.TryInitializeDirectiveState(ref platform,
                    state.ScriptKeyTemplate, templateLength))
                return FailAfterRead(ref platform, frame, state, lineLength,
                    out step);
            if (!platform.TryReadArgs(state.ScriptArguments,
                    state.ScriptArgumentLength, state.ScriptKeyTemplate,
                    templateLength, workspace.CommandWorkspace.ErrorCodes,
                    workspace.CommandWorkspace.ErrorCodeCapacity, out var rdArgs) ||
                rdArgs.IsNull)
            {
                RecordFailureAndAdvance(ref platform, frame, nextLine,
                    nextOffset, (int)ShellCommandResult.Error);
                step = MakeStep(ShellScriptStepStatus.Malformed,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error, nextLine, nextOffset,
                    lineLength);
                return step.Status;
            }
            platform.FreeArgs(rdArgs);
            if (!ShellScriptFrameCodec.TrySetScriptKeyTemplate(ref platform,
                    frame, state.ScriptKeyTemplate, templateLength) ||
                !Advance(ref platform, frame, nextLine, nextOffset))
                return FailAfterRead(ref platform, frame, state, lineLength,
                    out step);
            step = MakeStep(ShellScriptStepStatus.Empty,
                ShellInternalCommand.Unknown, state.LastResult, nextLine,
                nextOffset, lineLength);
            return step.Status;
        }
        if (directive is ShellScriptDirectiveKind.Bra or
            ShellScriptDirectiveKind.Ket or ShellScriptDirectiveKind.Dollar or
            ShellScriptDirectiveKind.Dot)
        {
            var delimiterCursor = new ShellTextCursor(APTR.FromPointer(
                workspace.Line.Raw + directiveArgument),
                lineLength - directiveArgument);
            var delimiterResult = ShellTextParser.NextToken(ref platform,
                ref delimiterCursor, workspace.CommandName,
                workspace.CommandNameCapacity, out var delimiterLength, out _);
            var endResult = ShellTextParser.NextToken(ref platform,
                ref delimiterCursor, workspace.CommandWorkspace.Token,
                workspace.CommandWorkspace.TokenCapacity, out _, out _);
            if (state.ScriptKeyTemplateLength == 0 || delimiterLength != 1 ||
                delimiterResult != (int)ShellTextTokenResult.Token ||
                endResult != (int)ShellTextTokenResult.End ||
                !(directive == ShellScriptDirectiveKind.Dot
                    ? ShellScriptKeyExpansion.TrySetDot(ref platform,
                        state.ScriptKeyTemplate, state.ScriptKeyTemplateLength,
                        platform.ReadUInt8(workspace.CommandName, 0))
                    : directive == ShellScriptDirectiveKind.Dollar
                    ? ShellScriptKeyExpansion.TrySetDollar(ref platform,
                        state.ScriptKeyTemplate, state.ScriptKeyTemplateLength,
                        platform.ReadUInt8(workspace.CommandName, 0))
                    : ShellScriptKeyExpansion.TrySetBracket(ref platform,
                        state.ScriptKeyTemplate, state.ScriptKeyTemplateLength,
                        directive == ShellScriptDirectiveKind.Bra ? 0u : 1u,
                        platform.ReadUInt8(workspace.CommandName, 0))))
            {
                RecordFailureAndAdvance(ref platform, frame, nextLine,
                    nextOffset, (int)ShellCommandResult.Error);
                step = MakeStep(ShellScriptStepStatus.Malformed,
                    ShellInternalCommand.Unknown, (int)ShellCommandResult.Error,
                    nextLine, nextOffset, lineLength);
                return step.Status;
            }
            if (!Advance(ref platform, frame, nextLine, nextOffset))
                return FailAfterRead(ref platform, frame, state, lineLength,
                    out step);
            step = MakeStep(ShellScriptStepStatus.Empty,
                ShellInternalCommand.Unknown, state.LastResult, nextLine,
                nextOffset, lineLength);
            return step.Status;
        }
        if (directive == ShellScriptDirectiveKind.Default)
        {
            var defaultsWorkspace = workspace.Redirection;
            var defaultCursor = new ShellTextCursor(APTR.FromPointer(
                workspace.Line.Raw + directiveArgument),
                lineLength - directiveArgument);
            var nameResult = ShellTextParser.NextToken(ref platform,
                ref defaultCursor, workspace.CommandName,
                workspace.CommandNameCapacity, out var nameLength, out _);
            var valueResult = ShellTextParser.NextToken(ref platform,
                ref defaultCursor, defaultsWorkspace.Command,
                defaultsWorkspace.CommandCapacity, out var valueLength, out _);
            if (nameResult == (int)ShellTextTokenResult.End &&
                valueResult == (int)ShellTextTokenResult.End)
            {
                // Workbench 3.1 accepts a bare .DEF as a no-op.
                if (!Advance(ref platform, frame, nextLine, nextOffset))
                    return FailAfterRead(ref platform, frame, state, lineLength,
                        out step);
                step = MakeStep(ShellScriptStepStatus.Empty,
                    ShellInternalCommand.Unknown, state.LastResult, nextLine,
                    nextOffset, lineLength);
                return step.Status;
            }
            var embeddedSeparator = FindByte(ref platform, workspace.CommandName,
                nameLength, (byte)'=');
            if (embeddedSeparator != uint.MaxValue)
            {
                var embeddedValueLength = nameLength - embeddedSeparator - 1;
                if (embeddedSeparator == 0 ||
                    embeddedValueLength >= defaultsWorkspace.CommandCapacity)
                {
                    RecordFailureAndAdvance(ref platform, frame, nextLine,
                        nextOffset, (int)ShellCommandResult.Error);
                    step = MakeStep(ShellScriptStepStatus.Malformed,
                        ShellInternalCommand.Unknown, (int)ShellCommandResult.Error,
                        nextLine, nextOffset, lineLength);
                    return step.Status;
                }
                if (embeddedValueLength != 0)
                    platform.Copy(APTR.FromPointer(workspace.CommandName.Raw +
                        embeddedSeparator + 1), defaultsWorkspace.Command,
                        embeddedValueLength);
                platform.WriteUInt8(defaultsWorkspace.Command,
                    (int)embeddedValueLength, 0);
                nameLength = embeddedSeparator;
                valueLength = embeddedValueLength;
                valueResult = (int)ShellTextTokenResult.Token;
            }
            if (state.ScriptKeyTemplate.IsNull ||
                state.ScriptKeyTemplateLength == 0 ||
                !defaultsWorkspace.IsEnabled ||
                nameResult != (int)ShellTextTokenResult.Token ||
                (valueResult != (int)ShellTextTokenResult.Token &&
                    valueResult != (int)ShellTextTokenResult.End) ||
                // Workbench 3.1 accepts surplus .DEF tokens after either
                // separator spelling and retains the first value.
                !ShellScriptKeyExpansion.TryDefineDefault(ref platform,
                    state.ScriptKeyTemplate, state.ScriptKeyTemplateLength,
                    workspace.CommandName, nameLength, defaultsWorkspace.Command,
                    valueResult == (int)ShellTextTokenResult.Token
                        ? valueLength : 0))
            {
                RecordFailureAndAdvance(ref platform, frame, nextLine,
                    nextOffset, (int)ShellCommandResult.Error);
                step = MakeStep(ShellScriptStepStatus.Malformed,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error, nextLine, nextOffset,
                    lineLength);
                return step.Status;
            }
            if (!Advance(ref platform, frame, nextLine, nextOffset))
                return FailAfterRead(ref platform, frame, state, lineLength,
                    out step);
            step = MakeStep(ShellScriptStepStatus.Empty,
                ShellInternalCommand.Unknown, state.LastResult, nextLine,
                nextOffset, lineLength);
            return step.Status;
        }

		var compoundSource = new ShellScriptTextSlice(workspace.Line,
			lineLength);
		var compoundStatus = ShellScriptCompoundParser.SplitFirstOperator(
			ref platform, in compoundSource, out var compoundSplit);
		if (compoundStatus !=
				(int)ShellScriptCompoundParseStatus.NoOperator &&
			compoundStatus !=
				(int)ShellScriptCompoundParseStatus.Operator)
		{
			RecordFailureAndAdvance(ref platform, frame, nextLine,
				nextOffset, (int)ShellCommandResult.Error);
			step = MakeStep(compoundStatus ==
					(int)ShellScriptCompoundParseStatus.Malformed
					? ShellScriptStepStatus.Malformed
					: ShellScriptStepStatus.PlatformFailure,
				ShellInternalCommand.Unknown,
				(int)ShellCommandResult.Error, nextLine, nextOffset,
				lineLength);
			return step.Status;
		}
		if (compoundStatus ==
				(int)ShellScriptCompoundParseStatus.Operator &&
			compoundSplit.Operator !=
				ShellScriptCompoundOperator.ConditionalAnd &&
			compoundSplit.Operator !=
				ShellScriptCompoundOperator.OutputConcatenation)
		{
			RecordFailureAndAdvance(ref platform, frame, nextLine,
				nextOffset, (int)ShellCommandResult.Error);
			step = MakeStep(ShellScriptStepStatus.UnsupportedOperator,
				ShellInternalCommand.Unknown,
				(int)ShellCommandResult.Error, nextLine, nextOffset,
				lineLength);
			return step.Status;
		}
		var hasDeferredCommand = compoundStatus ==
			(int)ShellScriptCompoundParseStatus.Operator;
		var deferredText = compoundSplit.Deferred;
		var commandSource = compoundSplit.First.Data;
		var commandLength = compoundSplit.First.Length;
        if (state.ScriptKeyTemplateLength != 0)
        {
            var substitution = workspace.Redirection;
            var aliasWorkspace = workspace.AliasExpansion;
            if (!substitution.IsEnabled || !aliasWorkspace.IsEnabled ||
                !ShellScriptKeyExpansion.TryExpand(ref platform,
                    commandSource, commandLength, state.ScriptArguments,
                    state.ScriptArgumentLength, state.ScriptKeyTemplate,
                    state.ScriptKeyTemplateLength,
                    workspace.CommandWorkspace.ErrorCodes,
                    workspace.CommandWorkspace.ErrorCodeCapacity,
                    workspace.ShellNumber,
                    substitution.Command, substitution.CommandCapacity,
                    out commandLength))
            {
                RecordFailureAndAdvance(ref platform, frame, nextLine,
                    nextOffset, (int)ShellCommandResult.Error);
                step = MakeStep(ShellScriptStepStatus.Malformed,
                    ShellInternalCommand.Unknown,
                    (int)ShellCommandResult.Error, nextLine, nextOffset,
                    lineLength);
                return step.Status;
            }
            commandSource = substitution.Command;
            if (commandLength == 0)
            {
                if (!Advance(ref platform, frame, nextLine, nextOffset))
                    return FailAfterRead(ref platform, frame, state,
                        lineLength, out step);
                step = MakeStep(ShellScriptStepStatus.Empty,
                    ShellInternalCommand.Unknown, state.LastResult, nextLine,
                    nextOffset, lineLength);
                return step.Status;
            }
        }
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
            else if (state.ScriptKeyTemplateLength != 0)
            {
                if (commandLength >= aliasWorkspace.Capacity)
                {
                    RecordFailureAndAdvance(ref platform, frame, nextLine,
                        nextOffset, (int)ShellCommandResult.Error);
                    step = MakeStep(ShellScriptStepStatus.Malformed,
                        ShellInternalCommand.Unknown,
                        (int)ShellCommandResult.Error, nextLine, nextOffset,
                        lineLength);
                    return step.Status;
                }
                platform.Copy(commandSource, aliasWorkspace.Line,
                    commandLength);
                platform.WriteUInt8(aliasWorkspace.Line,
                    (int)commandLength, 0);
                commandSource = aliasWorkspace.Line;
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

        var command = ShellInternalCommandResolver.ResolveAvailable(
            ref platform,
            state.Cli,
            workspace.CommandName,
            commandNameLength);
        var lookup = default(ShellScriptLookupResult);
        if (command == ShellInternalCommand.Unknown)
        {
            var lookupWorkspace = workspace.Lookup;
            if (!platform.TryLookupScriptCommand(
                    state.Cli,
                    workspace.CommandName,
                    commandNameLength,
                    in lookupWorkspace,
                    out lookup) ||
                (uint)lookup.Kind > (uint)ShellScriptLookupKind.Malformed ||
                (uint)lookup.Origin > (uint)ShellScriptLookupOrigin.CommandPath ||
                (lookup.PathLength != 0 &&
                 (lookup.ResolvedPath.IsNull ||
                  lookup.ResolvedPath.Raw != lookupWorkspace.Path.Raw ||
                  lookup.PathLength >= lookupWorkspace.Capacity ||
                  !platform.IsMapped(lookup.ResolvedPath, lookup.PathLength))) ||
                (lookup.Kind == ShellScriptLookupKind.Script &&
                 ((lookup.Origin is not ShellScriptLookupOrigin.ExplicitFile and
                    not ShellScriptLookupOrigin.CurrentDirectory and
                    not ShellScriptLookupOrigin.CommandPath) ||
                  (lookup.Protection & FileProtection.Script) == 0)))
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
            var externalCommand = new ShellScriptCommandInvocation
            {
                Line = commandSource,
                LineLength = commandLength,
                CommandName = workspace.CommandName,
                CommandNameLength = commandNameLength,
                Arguments = new ShellScriptTextSlice(
                    cursor.Position < commandLength
                        ? APTR.FromPointer(commandSource.Raw + cursor.Position)
                        : APTR.Null,
                    commandLength - cursor.Position),
            };
            platformSuccess = platform.TryExecuteScriptCommand(
                    state.Cli,
                    frame,
                    in externalCommand,
                    in lookup,
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

        // ReadArgs, output, and command completion use the current process's
        // IoErr. Snapshot it before closing redirections can overwrite it.
        // Pending children own a separate outcome; the parent's IoErr is not
        // evidence of the child's result and is never published for them.
        var diagnostics = default(ShellCommandDiagnostics);
        var diagnosticsCaptured = pendingContinuation.IsNull &&
            platform.TryCaptureCommandDiagnostics(state.Cli,
                platformSuccess ? commandResult : (int)ShellCommandResult.Error,
                out diagnostics);
        if (!ShellRedirectionTransaction.Close(
                ref platform, in state, ref redirectionHandles,
                out var closeFailure, out var closeFailureCaptured))
        {
            commandResult = (int)ShellCommandResult.Error;
            if (pendingContinuation.IsNull)
            {
                diagnostics = closeFailure;
                diagnosticsCaptured = closeFailureCaptured;
            }
        }

        if (!platformSuccess)
        {
            if (diagnosticsCaptured)
                platform.TryPublishCommandDiagnostics(state.Cli, in diagnostics);
            ShellScriptFrameCodec.TryRecordResult(ref platform, frame,
                (int)ShellCommandResult.Error);
            ShellScriptFrameCodec.TryAdvance(ref platform, frame, nextLine,
                nextOffset);
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
			var deferred = default(ShellScriptDeferredCommandState);
			if (hasDeferredCommand)
			{
				deferred.Kind = compoundSplit.Operator ==
					ShellScriptCompoundOperator.ConditionalAnd
					? ShellScriptDeferredCommandKind.ConditionalAnd
					: ShellScriptDeferredCommandKind.OutputConcatenation;
				deferred.Text = deferredText;
				deferred.NextLine = nextLine;
				deferred.NextOffset = nextOffset;
			}
			if (!ShellProcessContinuationCodec.TryRead(ref platform,
					pendingContinuation, out var pendingState) ||
				pendingState.ParentCli.Raw != state.Cli.Raw ||
				(hasDeferredCommand &&
				 !ShellScriptFrameCodec.TrySetDeferredCommand(ref platform,
					frame, in deferred)) ||
				!ShellScriptFrameCodec.TrySetPendingCommand(ref platform,
					frame, pendingContinuation, nextLine, nextOffset))
            {
                // Publication succeeded: a frame failure is not a child
                // failure. The owning runner detaches all of its DOS-owned
                // continuations before reclaiming its records and frame.
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

        if (!diagnosticsCaptured ||
            !platform.TryPublishCommandDiagnostics(state.Cli, in diagnostics) ||
            !ShellScriptFrameCodec.TryRecordResult(
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
		if (!ShellScriptFrameCodec.TryRead(ref platform, frame,
				out var afterCommand))
		{
			step = MakeStep(ShellScriptStepStatus.PlatformFailure, command,
				(int)ShellCommandResult.Error, nextLine, nextOffset,
				lineLength);
			return step.Status;
		}
		var cursorUnchanged = afterCommand.CurrentLine == state.CurrentLine &&
			afterCommand.CurrentOffset == state.CurrentOffset;
		var commandStopsShell =
			(afterCommand.Flags & (ShellScriptFrameFlags.QuitRequested |
				ShellScriptFrameFlags.EndRequested)) != 0 ||
			(afterCommand.Flags & ShellScriptFrameFlags.Active) == 0;
		var continueCompound = false;
		var deferRightCommand = hasDeferredCommand &&
			(compoundSplit.Operator ==
				ShellScriptCompoundOperator.OutputConcatenation ||
			 !MeetsFailureLimit(commandResult, afterCommand.FailureLimit));
		if (cursorUnchanged && !commandStopsShell && deferRightCommand)
		{
			var deferred = default(ShellScriptDeferredCommandState);
			deferred.Kind = compoundSplit.Operator ==
				ShellScriptCompoundOperator.ConditionalAnd
				? ShellScriptDeferredCommandKind.ConditionalAnd
				: ShellScriptDeferredCommandKind.OutputConcatenation;
			deferred.Text = deferredText;
			deferred.NextLine = nextLine;
			deferred.NextOffset = nextOffset;
			continueCompound = ShellScriptFrameCodec.TrySetDeferredCommand(
				ref platform, frame, in deferred);
		}
		if ((cursorUnchanged && !commandStopsShell && deferRightCommand &&
			!continueCompound) ||
			(cursorUnchanged && !continueCompound &&
			 !Advance(ref platform, frame, nextLine, nextOffset)))
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

		var commandStatus = !continueCompound && MeetsFailureLimit(commandResult,
			afterCommand.FailureLimit)
			? ShellScriptStepStatus.FailureLimitExceeded
			: ShellScriptStepStatus.Executed;
		step = MakeStep(
			commandStatus,
            command,
            commandResult,
            nextLine,
            nextOffset,
            lineLength);
        return step.Status;
    }

    private static ShellScriptPromptProgress AdvanceInteractivePrompt<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        in ShellScriptFrameState state,
        in ShellScriptStepWorkspace workspace,
        out ShellInternalCommand command,
        out int commandResult)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
		command = ShellInternalCommand.Unknown;
		commandResult = (int)ShellCommandResult.Ok;
		if (!ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var current))
			return ShellScriptPromptProgress.Failed;
		var expansion = current.PromptExpansion;
		ShellScriptPromptTemplate template;
		if (expansion.Phase == ShellScriptPromptExpansionPhase.None)
		{
			if (workspace.PromptTemplate.IsNull ||
				workspace.PromptTemplateCapacity <=
				ShellScriptPromptParser.MaximumTemplateLength ||
				!platform.TryCopyScriptPromptTemplate(state.Cli,
					workspace.PromptTemplate, workspace.PromptTemplateCapacity,
					out template))
				return ShellScriptPromptProgress.Failed;
			if (!PreflightPromptTemplate(ref platform, in template))
			{
				// Preserve DOS's historical ActionNotKnown diagnostic for a
				// malformed or unmatched backtick template.
				platform.TryWriteScriptPrompt(state.Cli, state.Output);
				return ShellScriptPromptProgress.Failed;
			}
			expansion = default;
			expansion.Phase = ShellScriptPromptExpansionPhase.Expanding;
			expansion.TemplateLength = template.Length;
			if (!ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
				frame, in expansion))
				return ShellScriptPromptProgress.Failed;
		}
		else
		{
			if (expansion.Phase ==
				ShellScriptPromptExpansionPhase.WaitingCommand ||
				expansion.Phase != ShellScriptPromptExpansionPhase.Expanding &&
				expansion.Phase !=
					ShellScriptPromptExpansionPhase.ReadingCapture ||
				expansion.TemplateLength >
					ShellScriptPromptParser.MaximumTemplateLength ||
				expansion.Cursor.Position > expansion.TemplateLength ||
				workspace.PromptTemplate.IsNull ||
				workspace.PromptTemplateCapacity <
				expansion.TemplateLength + 1 ||
				!platform.IsMapped(workspace.PromptTemplate,
					expansion.TemplateLength + 1))
				return ShellScriptPromptProgress.Failed;
			template = default;
			template.Text = workspace.PromptTemplate;
			template.Length = expansion.TemplateLength;
		}

		if (expansion.Phase ==
			ShellScriptPromptExpansionPhase.ReadingCapture)
			return ReadPromptCapture(ref platform, frame, in state, in workspace,
				ref expansion, out commandResult);

		var cursor = expansion.Cursor;
		while (ShellScriptPromptParser.TryNext(ref platform, in template,
			in cursor, out var segment))
		{
			if (segment.Kind == ShellScriptPromptSegmentKind.End)
			{
				expansion = default;
				return ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
					frame, in expansion)
					? ShellScriptPromptProgress.Ready
					: ShellScriptPromptProgress.Failed;
			}
			if (segment.Kind == ShellScriptPromptSegmentKind.Literal)
			{
				if (!platform.TryWriteScriptPromptLiteral(state.Cli,
					state.Output, in segment))
					return ShellScriptPromptProgress.Failed;
				expansion.Cursor = segment.Next;
				if (!ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
					frame, in expansion))
					return ShellScriptPromptProgress.Failed;
				cursor = segment.Next;
				continue;
			}
			if (segment.Kind != ShellScriptPromptSegmentKind.Command)
				return ShellScriptPromptProgress.Failed;
			return ExecutePromptCommand(ref platform, frame, in state,
				in workspace, in segment, ref expansion, out command,
				out commandResult);
		}
		return ShellScriptPromptProgress.Failed;
	}

	private static bool PreflightPromptTemplate<TPlatform>(ref TPlatform platform,
		in ShellScriptPromptTemplate template)
		where TPlatform : struct, IShellPlatform
	{
		var cursor = default(ShellScriptPromptCursor);
		while (ShellScriptPromptParser.TryNext(ref platform, in template,
			in cursor, out var segment))
		{
			if (segment.Kind == ShellScriptPromptSegmentKind.End) return true;
			if (segment.Kind is not ShellScriptPromptSegmentKind.Literal and
				not ShellScriptPromptSegmentKind.Command)
				return false;
			cursor = segment.Next;
		}
		return false;
	}

	private static ShellScriptPromptProgress ReadPromptCapture<TPlatform>(
		ref TPlatform platform, APTR frame, in ShellScriptFrameState state,
		in ShellScriptStepWorkspace workspace,
		ref ShellScriptPromptExpansionState expansion,
		out int commandResult)
		where TPlatform : struct, IShellPlatform, IShellScriptPlatform
	{
		commandResult = state.LastResult;
		if (workspace.PromptCapturePath.IsNull ||
			workspace.PromptCapturePathCapacity <=
			expansion.CapturePathLength ||
			expansion.CapturePathLength == 0 ||
			!platform.IsMapped(workspace.PromptCapturePath,
				workspace.PromptCapturePathCapacity))
			return ShellScriptPromptProgress.Failed;
		var captureInput = expansion.CaptureInput;
		if (captureInput.IsNull)
		{
			if (!platform.TryOpenScriptInput(state.Cli,
				workspace.PromptCapturePath, expansion.CapturePathLength,
				out captureInput) || captureInput.IsNull ||
				!platform.TrySetScriptPromptCapture(state.Cli,
					workspace.PromptCapturePath, expansion.CapturePathLength,
					captureInput))
			{
				if (captureInput.IsNotNull)
					platform.TryCloseScriptRedirection(state.Cli, captureInput);
				return ShellScriptPromptProgress.Failed;
			}
			expansion.CaptureInput = captureInput;
			if (!ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
				frame, in expansion))
				return ShellScriptPromptProgress.Failed;
		}
		var token = workspace.CommandWorkspace.Token;
		var capacity = workspace.CommandWorkspace.TokenCapacity;
		if (token.IsNull || capacity == 0 ||
			!platform.IsMapped(token, capacity))
			return ShellScriptPromptProgress.Failed;
		var count = platform.Read(captureInput, token, capacity);
		if (count < 0 || unchecked((uint)count) > capacity)
			return ShellScriptPromptProgress.Failed;
		if (count != 0)
			return platform.Write(state.Output, token, unchecked((uint)count)) == count
				? ShellScriptPromptProgress.Continue
				: ShellScriptPromptProgress.Failed;
		if (!platform.TryCloseScriptRedirection(state.Cli, captureInput) ||
			!platform.TrySetScriptPromptCapture(state.Cli,
				workspace.PromptCapturePath, expansion.CapturePathLength,
				BPTR.Null) ||
			!platform.TryDeleteScriptPath(state.Cli,
				workspace.PromptCapturePath, expansion.CapturePathLength))
			return ShellScriptPromptProgress.Failed;
		expansion.CaptureInput = BPTR.Null;
		expansion.CapturePathLength = 0;
		expansion.Phase = ShellScriptPromptExpansionPhase.Expanding;
		return ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
			frame, in expansion)
			? ShellScriptPromptProgress.Continue
			: ShellScriptPromptProgress.Failed;
	}

	private static ShellScriptPromptProgress ExecutePromptCommand<TPlatform>(
		ref TPlatform platform, APTR frame, in ShellScriptFrameState state,
		in ShellScriptStepWorkspace workspace,
		in ShellScriptPromptSegment segment,
		ref ShellScriptPromptExpansionState expansion,
		out ShellInternalCommand command, out int commandResult)
		where TPlatform : struct, IShellPlatform, IShellScriptPlatform
	{
		command = ShellInternalCommand.Unknown;
		commandResult = state.LastResult;
		var commandSource = segment.Text;
		var commandLength = segment.Length;
		if (commandLength == 0)
		{
			expansion.Cursor = segment.Next;
			return ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
				frame, in expansion)
				? ShellScriptPromptProgress.Continue
				: ShellScriptPromptProgress.Failed;
		}
		var aliasWorkspace = workspace.AliasExpansion;
		if (!aliasWorkspace.IsEnabled || !workspace.Redirection.IsEnabled ||
			!platform.TryExpandScriptAlias(state.Cli, commandSource,
				commandLength, aliasWorkspace.Line, aliasWorkspace.Capacity,
				out var expanded, out var expandedLength) || expanded > 1 ||
			(expanded == 0 && expandedLength != 0) ||
			expandedLength >= aliasWorkspace.Capacity ||
			(expanded != 0 && !platform.IsMapped(aliasWorkspace.Line,
				expandedLength)))
			return ShellScriptPromptProgress.Failed;
		if (expanded != 0)
		{
			commandSource = aliasWorkspace.Line;
			commandLength = expandedLength;
		}
		var redirectionWorkspace = workspace.Redirection;
		if (!ShellRedirectionParser.Parse(ref platform, commandSource,
			commandLength, in redirectionWorkspace, out var redirection,
			out commandLength))
			return RecordPromptCommandFailure(ref platform, frame, in state,
				in segment, ref expansion, out commandResult);
		commandSource = redirectionWorkspace.Command;
		var cursor = new ShellTextCursor(commandSource, commandLength);
		var tokenResult = ShellTextParser.NextToken(ref platform, ref cursor,
			workspace.CommandName, workspace.CommandNameCapacity,
			out var commandNameLength, out _);
		if (tokenResult == (int)ShellTextTokenResult.End)
		{
			expansion.Cursor = segment.Next;
			return ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
				frame, in expansion)
				? ShellScriptPromptProgress.Continue
				: ShellScriptPromptProgress.Failed;
		}
		if (tokenResult != (int)ShellTextTokenResult.Token ||
			commandNameLength == 0)
			return RecordPromptCommandFailure(ref platform, frame, in state,
				in segment, ref expansion, out commandResult);
		command = ShellInternalCommandResolver.ResolveAvailable(ref platform,
			state.Cli, workspace.CommandName, commandNameLength);
		var lookup = default(ShellScriptLookupResult);
		if (command == ShellInternalCommand.Unknown)
		{
			var lookupWorkspace = workspace.Lookup;
			if (!lookupWorkspace.IsEnabled ||
				!platform.TryLookupScriptCommand(state.Cli,
					workspace.CommandName, commandNameLength, in lookupWorkspace,
					out lookup) ||
				(uint)lookup.Kind > (uint)ShellScriptLookupKind.Malformed ||
				(uint)lookup.Origin > (uint)ShellScriptLookupOrigin.CommandPath ||
				(lookup.PathLength != 0 &&
					(lookup.ResolvedPath.IsNull ||
					 lookup.ResolvedPath.Raw != lookupWorkspace.Path.Raw ||
					 lookup.PathLength >= lookupWorkspace.Capacity ||
					 !platform.IsMapped(lookup.ResolvedPath,
						 lookup.PathLength))) ||
				(lookup.Kind == ShellScriptLookupKind.Script &&
					((lookup.Origin is not
						ShellScriptLookupOrigin.ExplicitFile and not
						ShellScriptLookupOrigin.CurrentDirectory and not
						ShellScriptLookupOrigin.CommandPath) ||
					 (lookup.Protection & FileProtection.Script) == 0)))
				return ShellScriptPromptProgress.Failed;
		}
		if (workspace.PromptCapturePath.IsNull ||
			!ShellScriptTemporaryPath.TryBuildPrompt(ref platform, frame,
				workspace.PromptCapturePath,
				workspace.PromptCapturePathCapacity, out var capturePathLength) ||
			!platform.TrySetScriptPromptCapture(state.Cli,
				workspace.PromptCapturePath, capturePathLength, BPTR.Null) ||
			!platform.TryOpenScriptOutput(state.Cli, workspace.PromptCapturePath,
				capturePathLength, 0, out var captureOutput) ||
			captureOutput.IsNull)
			return ShellScriptPromptProgress.Failed;
		var commandFrame = state;
		commandFrame.Output = captureOutput;
		if (!ShellRedirectionTransaction.TryOpen(ref platform,
			in commandFrame, in redirection, out var handles))
		{
			platform.TryCloseScriptRedirection(state.Cli, captureOutput);
			return ShellScriptPromptProgress.Failed;
		}
		var commandCursor = cursor;
		var platformSuccess = true;
		var continuation = APTR.Null;
		if (command == ShellInternalCommand.Unknown)
		{
			var invocation = new ShellScriptCommandInvocation
			{
				Line = commandSource,
				LineLength = commandLength,
				CommandName = workspace.CommandName,
				CommandNameLength = commandNameLength,
				Arguments = new ShellScriptTextSlice(
					commandCursor.Position < commandLength
						? APTR.FromPointer(commandSource.Raw +
							commandCursor.Position)
						: APTR.Null,
					commandLength - commandCursor.Position),
			};
			platformSuccess = platform.TryExecuteScriptCommand(state.Cli,
				frame, in invocation, in lookup, handles.Input, handles.Output,
				handles.Error, out commandResult, out continuation);
		}
		else
		{
			var argumentText = commandCursor.Position < commandLength
				? APTR.FromPointer(commandSource.Raw + commandCursor.Position)
				: APTR.Null;
			var argumentLength = commandLength - commandCursor.Position;
			var invocation = new CommandInvocation(argumentText,
				argumentLength, APTR.Null, APTR.Null, handles.Input,
				handles.Output, handles.Error, state.CurrentDirectory,
				state.Cli, 0, 0);
			var commandWorkspace = workspace.CommandWorkspace;
			commandResult = ShellCommandDispatcher.Dispatch(ref platform,
				in invocation, command, in commandWorkspace);
		}
		var diagnostics = default(ShellCommandDiagnostics);
		var diagnosticsCaptured = continuation.IsNull &&
			platform.TryCaptureCommandDiagnostics(state.Cli,
				platformSuccess ? commandResult :
					(int)ShellCommandResult.Error, out diagnostics);
		if (!ShellRedirectionTransaction.Close(ref platform,
			in commandFrame, ref handles, out var closeFailure,
			out var closeFailureCaptured))
		{
			commandResult = (int)ShellCommandResult.Error;
			if (continuation.IsNull)
			{
				diagnostics = closeFailure;
				diagnosticsCaptured = closeFailureCaptured;
			}
		}
		if (!platform.TryCloseScriptRedirection(state.Cli, captureOutput))
		{
			commandResult = (int)ShellCommandResult.Error;
			if (continuation.IsNull)
				diagnosticsCaptured = platform.TryCaptureCommandDiagnostics(
					state.Cli, commandResult, out diagnostics);
		}
		expansion.Cursor = segment.Next;
		expansion.CapturePathLength = capturePathLength;
		expansion.CaptureInput = BPTR.Null;
		if (continuation.IsNotNull)
		{
			expansion.Phase =
				ShellScriptPromptExpansionPhase.WaitingCommand;
				if (!ShellProcessContinuationCodec.TryRead(ref platform,
					continuation, out var pendingState) ||
					pendingState.ParentCli.Raw != state.Cli.Raw ||
					!ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
						frame, in expansion) ||
					!ShellScriptFrameCodec.TrySetPendingCommand(ref platform,
						frame, continuation, state.CurrentLine,
						state.CurrentOffset))
					return ShellScriptPromptProgress.Failed;
			return ShellScriptPromptProgress.Waiting;
		}
		if (!platformSuccess) commandResult =
			(int)ShellCommandResult.Error;
		if (!diagnosticsCaptured ||
			!platform.TryPublishCommandDiagnostics(state.Cli, in diagnostics) ||
			!ShellScriptFrameCodec.TryRecordResult(ref platform, frame,
				commandResult))
			return ShellScriptPromptProgress.Failed;
		expansion.Phase = ShellScriptPromptExpansionPhase.ReadingCapture;
		return ShellScriptFrameCodec.TrySetPromptExpansion(ref platform,
			frame, in expansion)
			? ShellScriptPromptProgress.Continue
			: ShellScriptPromptProgress.Failed;
	}

	private static ShellScriptPromptProgress RecordPromptCommandFailure<TPlatform>(
		ref TPlatform platform, APTR frame, in ShellScriptFrameState state,
		in ShellScriptPromptSegment segment,
		ref ShellScriptPromptExpansionState expansion,
		out int commandResult)
		where TPlatform : struct, IShellPlatform, IShellScriptPlatform
	{
		commandResult = (int)ShellCommandResult.Error;
		var diagnosticsCaptured = platform.TryCaptureCommandDiagnostics(
			state.Cli, commandResult, out var diagnostics);
		expansion.Cursor = segment.Next;
		return diagnosticsCaptured &&
			platform.TryPublishCommandDiagnostics(state.Cli, in diagnostics) &&
			ShellScriptFrameCodec.TryRecordResult(ref platform, frame,
				commandResult) &&
			ShellScriptFrameCodec.TrySetPromptExpansion(ref platform, frame,
				in expansion)
			? ShellScriptPromptProgress.Continue
			: ShellScriptPromptProgress.Failed;
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
        if (ShellScriptFrameCodec.TryRead(ref platform, frame, out var state) &&
            platform.TryCaptureCommandDiagnostics(state.Cli, result,
                out var diagnostics))
            platform.TryPublishCommandDiagnostics(state.Cli, in diagnostics);
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

	private static bool MeetsFailureLimit(int result, uint failureLimit) =>
		result >= 0 && failureLimit != 0 && unchecked((uint)result) >=
			failureLimit;

    private static uint FindByte<TPlatform>(ref TPlatform platform, APTR value,
        uint length, byte wanted) where TPlatform : struct, IShellPlatform
    {
        for (var offset = 0u; offset < length; offset++)
            if (platform.ReadUInt8(value, (int)offset) == wanted) return offset;
        return uint.MaxValue;
    }
}
