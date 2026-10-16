using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptEngineTests
{
	[Fact]
	public void Interactive_readargs_error_preserves_diagnostics_and_accepts_the_next_command()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Stack bad\nEcho after\n";
		platform.Store.ScriptPromptText = "> ";
		var frame = InitializeFrame(ref platform,
			ShellScriptFrameFlags.Active | ShellScriptFrameFlags.Interactive);
		var workspace = CreateWorkspace();
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out var failed));
		Assert.Equal((int)ShellCommandResult.Error, failed.CommandResult);
		Assert.Equal((int)DOS.Error.BadNumber, platform.Store.PublishedDiagnostics.IoError);
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out var next));
		Assert.Equal((int)ShellCommandResult.Ok, next.CommandResult);
		Assert.Equal("> > after\n", platform.Store.OutputText);
	}

	[Theory]
	[InlineData(10)]
	[InlineData(20)]
	public void Interactive_async_child_error_accepts_the_next_command(int childResult)
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "external\nEcho after\n";
		platform.Store.ScriptExternalPending = true;
		platform.Store.ScriptPromptText = "> ";
		var frame = InitializeFrame(ref platform,
			ShellScriptFrameFlags.Active | ShellScriptFrameFlags.Interactive);
		var workspace = CreateWorkspace();
		Assert.Equal(ShellScriptStepStatus.Waiting,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		platform.Store.ContinuationObservedState = ShellProcessContinuationState.Completed;
		platform.Store.ContinuationResult = childResult;
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out var failed));
		Assert.Equal(childResult, failed.CommandResult);
		Assert.Equal(childResult, platform.Store.PublishedDiagnostics.ReturnCode);
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out var next));
		Assert.Equal((int)ShellCommandResult.Ok, next.CommandResult);
		Assert.Equal("> > after\n", platform.Store.OutputText);
	}

    [Fact]
    public void Interactive_prompt_is_preflighted_then_emitted_as_literal_segments()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptPromptText = "CopperOS> ";
        var frame = InitializeFrame(ref platform,
            ShellScriptFrameFlags.Active | ShellScriptFrameFlags.Interactive);
        var workspace = CreateWorkspace();

        var status = ShellScriptEngine.Step(ref platform, frame,
            in workspace, out _);

        Assert.Equal(ShellScriptStepStatus.EndOfFile, status);
        Assert.Equal("CopperOS> ", platform.Store.OutputText);
        Assert.Equal(0, platform.Store.PromptWriteCount);
    }

    [Fact]
    public void Interactive_prompt_with_unmatched_backtick_emits_no_partial_prefix()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptPromptText = "prefix `Echo value suffix";
        var frame = InitializeFrame(ref platform,
            ShellScriptFrameFlags.Active | ShellScriptFrameFlags.Interactive);
        var workspace = CreateWorkspace();

        var status = ShellScriptEngine.Step(ref platform, frame,
            in workspace, out _);

        Assert.Equal(ShellScriptStepStatus.PlatformFailure, status);
        Assert.Equal(string.Empty, platform.Store.OutputText);
        Assert.Equal(1, platform.Store.PromptWriteCount);
    }

	[Fact]
	public void Interactive_prompt_executes_backtick_command_and_captures_its_output()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptPromptText = "prefix `Echo value` suffix";
		var frame = InitializeFrame(ref platform,
			ShellScriptFrameFlags.Active | ShellScriptFrameFlags.Interactive);
		var workspace = CreatePromptWorkspace();

		var run = ShellScriptEngine.Run(ref platform, frame, in workspace, 8);

		Assert.Equal(ShellScriptStepStatus.EndOfFile, run.Status);
		Assert.Equal("prefix value\n suffix", platform.Store.OutputText);
		Assert.Equal(0, platform.Store.PromptWriteCount);
		Assert.Equal(1, platform.Store.ScriptDeleteCount);
	}

	[Fact]
	public void Interactive_prompt_continues_after_malformed_backtick_command()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptPromptText = "prefix `Echo \"unterminated` suffix";
		var frame = InitializeFrame(ref platform,
			ShellScriptFrameFlags.Active | ShellScriptFrameFlags.Interactive);
		var workspace = CreatePromptWorkspace();

		var run = ShellScriptEngine.Run(ref platform, frame, in workspace, 8);

		Assert.Equal(ShellScriptStepStatus.EndOfFile, run.Status);
		Assert.Equal("prefix  suffix", platform.Store.OutputText);
		Assert.Equal(0, platform.Store.PromptWriteCount);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var frameState));
		Assert.Equal((int)ShellCommandResult.Error, frameState.LastResult);
	}

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Final_async_error_survives_EOF_and_runner_cleanup_before_Shell_child_exit(bool completesDuringLaunch)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "external\n; trailing comment\n\n";
        platform.Store.ScriptExternalPending = true;
        platform.Store.ScriptChildCompletesDuringLaunch = completesDuringLaunch;
        platform.Store.ContinuationResult = (int)ShellCommandResult.Warn;
        platform.Store.ContinuationIoError = (int)DOS.Error.ObjectNotFound;
        platform.Store.ContinuationReleaseClearsRecord = true;
        var frame = InitializeFrame(ref platform);
        var workspace = CreateWorkspace();
        Assert.Equal(ShellScriptStepStatus.Waiting,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        platform.Store.ContinuationObservedState = ShellProcessContinuationState.Completed;
        var run = ShellScriptEngine.Run(ref platform, frame, in workspace, 8);
        Assert.Equal(ShellScriptStepStatus.EndOfFile, run.Status);
        Assert.Equal((int)ShellCommandResult.Warn, run.Result);
        Assert.Equal(0, platform.Store.CommandIoError);
        Assert.Equal((int)DOS.Error.ObjectNotFound, platform.Store.PublishedDiagnostics.IoError);

        Assert.True(ShellScriptCompletionDiagnostics.TryCapture(ref platform,
            new APTR(8), in run, out var captured));
        // Model runner close/free/unbind operations clearing the current error.
        platform.Store.CommandIoError = (int)DOS.Error.DiskFull;
        Assert.True(platform.TryBeginCommandDiagnostics(new APTR(8)));
        Assert.True(platform.TryPublishCommandDiagnostics(new APTR(8), in captured));
        Assert.True(platform.TryCaptureCommandDiagnostics(new APTR(8), run.Result,
            out var nativeExitSnapshot));
        Assert.Equal((int)ShellCommandResult.Warn, nativeExitSnapshot.ReturnCode);
        Assert.Equal((int)DOS.Error.ObjectNotFound, nativeExitSnapshot.IoError);
    }

    [Fact]
	public void Synchronous_ReadArgs_error_reaches_the_default_failure_limit()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Stack bad\nEcho must-not-run\n";
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();
		var run = ShellScriptEngine.Run(ref platform, frame, in workspace, 4);
		Assert.Equal(ShellScriptStepStatus.FailureLimitExceeded, run.Status);
		Assert.Equal((int)ShellCommandResult.Error, run.Result);
		Assert.Equal(1u, run.Steps);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var frameState));
		Assert.Equal(2u, frameState.CurrentLine);
		Assert.Equal((int)DOS.Error.BadNumber, platform.Store.CommandIoError);
		Assert.True(ShellScriptCompletionDiagnostics.TryCapture(ref platform,
			new APTR(8), in run, out var captured));
        Assert.True(platform.TryBeginCommandDiagnostics(new APTR(8)));
        Assert.True(platform.TryPublishCommandDiagnostics(new APTR(8), in captured));
		Assert.Equal((int)DOS.Error.BadNumber, platform.Store.CommandIoError);
	}

	[Fact]
	public void Conditional_and_runs_the_right_command_before_advancing_the_script()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Echo alpha && Echo beta\n";
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var deferred));
		Assert.Equal(ShellScriptDeferredCommandKind.ConditionalAnd,
			deferred.DeferredCommand.Kind);
		Assert.Equal(1u, deferred.CurrentLine);

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal("alpha\nbeta\n", platform.Store.OutputText);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var completed));
		Assert.Equal(ShellScriptDeferredCommandKind.None,
			completed.DeferredCommand.Kind);
		Assert.Equal(2u, completed.CurrentLine);
	}

	[Fact]
	public void Output_concatenation_runs_both_commands_on_the_inherited_stream()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Echo alpha || Echo beta\n";
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var deferred));
		Assert.Equal(ShellScriptDeferredCommandKind.OutputConcatenation,
			deferred.DeferredCommand.Kind);
		Assert.Equal(1u, deferred.CurrentLine);
		Assert.Equal("alpha\n", platform.Store.OutputText);

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal("alpha\nbeta\n", platform.Store.OutputText);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var completed));
		Assert.Equal(ShellScriptDeferredCommandKind.None,
			completed.DeferredCommand.Kind);
		Assert.Equal(2u, completed.CurrentLine);
	}

	[Fact]
	public void Output_concatenation_runs_right_command_after_left_failure()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Stack invalid || Echo after\n";
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var deferred));
		Assert.Equal((int)ShellCommandResult.Error, deferred.LastResult);
		Assert.Equal(ShellScriptDeferredCommandKind.OutputConcatenation,
			deferred.DeferredCommand.Kind);

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal("after\n", platform.Store.OutputText);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var completed));
		Assert.Equal((int)ShellCommandResult.Ok, completed.LastResult);
		Assert.Equal(2u, completed.CurrentLine);
	}

	[Fact]
	public void Unimplemented_pipe_is_not_dispatched_as_arguments()
	{
		const string script = "Echo before | Echo after\n";
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = script;
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		var status = ShellScriptEngine.Step(ref platform, frame,
			in workspace, out var step);

		Assert.Equal(ShellScriptStepStatus.UnsupportedOperator, status);
		Assert.Equal(ShellInternalCommand.Unknown, step.Command);
		Assert.Equal((int)ShellCommandResult.Error, step.CommandResult);
		Assert.Equal(string.Empty, platform.Store.OutputText);
		Assert.Equal(0, platform.Store.ReadArgsAttemptCount);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var state));
		Assert.Equal(2u, state.CurrentLine);
		Assert.Equal((int)ShellCommandResult.Error, state.LastResult);
	}

	[Fact]
	public void Output_concatenation_waits_for_external_command_before_continuing()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "external || Echo after\n";
		platform.Store.ScriptExternalPending = true;
		platform.Store.ContinuationResult = 20;
		platform.Store.ContinuationReleaseClearsRecord = true;
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		Assert.Equal(ShellScriptStepStatus.Waiting,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var pending));
		Assert.Equal(ShellScriptDeferredCommandKind.OutputConcatenation,
			pending.DeferredCommand.Kind);

		platform.Store.ContinuationObservedState =
			ShellProcessContinuationState.Completed;
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var resumed));
		Assert.Equal(1u, resumed.CurrentLine);
		Assert.Equal(ShellScriptDeferredCommandKind.OutputConcatenation,
			resumed.DeferredCommand.Kind);

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal("after\n", platform.Store.OutputText);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var completed));
		Assert.Equal(2u, completed.CurrentLine);
	}

	[Theory]
	[InlineData(5, ShellScriptStepStatus.Executed, 1u, "after\n")]
	[InlineData(20, ShellScriptStepStatus.FailureLimitExceeded, 2u, "")]
	public void Conditional_and_waits_for_external_result_before_deciding(
		int childResult, ShellScriptStepStatus expectedStatus,
		uint expectedLine, string expectedOutput)
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "external && Echo after\n";
		platform.Store.ScriptExternalPending = true;
		platform.Store.ContinuationResult = childResult;
		platform.Store.ContinuationReleaseClearsRecord = true;
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		Assert.Equal(ShellScriptStepStatus.Waiting,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var pending));
		Assert.Equal(ShellScriptDeferredCommandKind.ConditionalAnd,
			pending.DeferredCommand.Kind);

		platform.Store.ContinuationObservedState =
			ShellProcessContinuationState.Completed;
		Assert.Equal(expectedStatus,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var completed));
		Assert.Equal(expectedLine, completed.CurrentLine);
		if (childResult < 10)
			Assert.Equal(ShellScriptStepStatus.Executed,
				ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal(expectedOutput, platform.Store.OutputText);
	}

	[Fact]
	public void Failat_raises_the_frame_limit_and_allows_an_error_below_it()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Failat 11\nStack bad\nEcho reached\n";
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		var run = ShellScriptEngine.Run(ref platform, frame, in workspace, 5);

		Assert.Equal(ShellScriptStepStatus.EndOfFile, run.Status);
		Assert.Equal((int)ShellCommandResult.Ok, run.Result);
		Assert.Equal(11u, platform.Store.FailureLimit);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var frameState));
		Assert.Equal(11u, frameState.FailureLimit);
	}

    [Theory]
    [InlineData("")]
    [InlineData("; empty script\n\n")]
    [InlineData("Stack 4096\n")]
    public void Successful_script_exit_does_not_inherit_previous_CLI_error(string script)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = script;
        platform.Store.PublishedDiagnostics = new ShellCommandDiagnostics
        {
            ReturnCode = (int)ShellCommandResult.Error,
            IoError = (int)DOS.Error.BadNumber,
        };
        var frame = InitializeFrame(ref platform);
        var workspace = CreateWorkspace();
        var run = ShellScriptEngine.Run(ref platform, frame, in workspace, 5);
        Assert.Equal(ShellScriptStepStatus.EndOfFile, run.Status);
        Assert.Equal(0, run.Result);
        Assert.True(ShellScriptCompletionDiagnostics.TryCapture(ref platform,
            new APTR(8), in run, out var captured));
        Assert.Equal(0, captured.ReturnCode);
        Assert.Equal(0, captured.IoError);
    }

    [Fact]
    public void Non_EOF_failure_uses_fresh_error_before_cleanup_not_prior_CLI_result()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.CommandIoError = (int)DOS.Error.DiskFull;
        platform.Store.PublishedDiagnostics = new ShellCommandDiagnostics
        {
            ReturnCode = (int)ShellCommandResult.Error,
            IoError = (int)DOS.Error.BadNumber,
        };
        var run = new ShellScriptRunResult(ShellScriptStepStatus.PlatformFailure,
            (int)ShellCommandResult.Error, 1);
        Assert.True(ShellScriptCompletionDiagnostics.TryCapture(ref platform,
            new APTR(8), in run, out var captured));
        Assert.Equal((int)DOS.Error.DiskFull, captured.IoError);
        Assert.True(platform.TryBeginCommandDiagnostics(new APTR(8)));
        Assert.True(platform.TryPublishCommandDiagnostics(new APTR(8), in captured));
        Assert.Equal((int)DOS.Error.DiskFull, platform.Store.CommandIoError);
    }

    [Fact]
    public void EOF_result_mismatch_does_not_attach_an_unrelated_published_error()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.PublishedDiagnostics = new ShellCommandDiagnostics
        {
            ReturnCode = (int)ShellCommandResult.Error,
            IoError = (int)DOS.Error.BadNumber,
        };
        var run = new ShellScriptRunResult(ShellScriptStepStatus.EndOfFile,
            (int)ShellCommandResult.Warn, 1);
        Assert.True(ShellScriptCompletionDiagnostics.TryCapture(ref platform,
            new APTR(8), in run, out var captured));
        Assert.Equal((int)ShellCommandResult.Warn, captured.ReturnCode);
        Assert.Equal(0, captured.IoError);
    }

    [Fact]
    public void Pending_script_does_not_capture_a_final_process_outcome()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        var run = new ShellScriptRunResult(ShellScriptStepStatus.Waiting, 0, 1);
        Assert.False(ShellScriptCompletionDiagnostics.TryCapture(ref platform,
            new APTR(8), in run, out _));
    }

    [Fact]
    public void Child_already_completed_when_launch_returns_is_collected_on_next_step()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "external\nEcho after\n";
        platform.Store.ScriptExternalPending = true;
        platform.Store.ScriptChildCompletesDuringLaunch = true;
        platform.Store.ContinuationResult = 5;
        platform.Store.ContinuationIoError = (int)DOS.Error.ObjectNotFound;
        platform.Store.ContinuationReleaseClearsRecord = true;
        var frame = InitializeFrame(ref platform);
        var workspace = CreateWorkspace();
        Assert.Equal(ShellScriptStepStatus.Waiting,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out var completed));
        Assert.Equal(5, completed.CommandResult);
        Assert.Equal(1, platform.Store.ContinuationReleaseCount);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame, out var current));
        Assert.True(current.PendingCommand.IsNull);
        Assert.Equal(5, current.LastResult);
        Assert.Equal(5, platform.Store.PublishedDiagnostics.ReturnCode);
        Assert.Equal((int)DOS.Error.ObjectNotFound,
            platform.Store.PublishedDiagnostics.IoError);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Child_saved_secondary_error_reaches_Why_after_acknowledgement(bool completesDuringLaunch)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "external\nWhy\n";
        platform.Store.ScriptExternalPending = true;
        platform.Store.ScriptChildCompletesDuringLaunch = completesDuringLaunch;
        platform.Store.ScriptCommandIoError = (int)DOS.Error.BadNumber;
        platform.Store.ContinuationResult = (int)ShellCommandResult.Warn;
        platform.Store.ContinuationIoError = (int)DOS.Error.ObjectNotFound;
        platform.Store.ContinuationReleaseClearsRecord = true;
        var frame = InitializeFrame(ref platform);
        var workspace = CreateWorkspace();

        Assert.Equal(ShellScriptStepStatus.Waiting,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(0, platform.Store.DiagnosticsPublishCount);
        platform.Store.CommandIoError = (int)DOS.Error.DiskFull;
        platform.Store.ContinuationObservedState = ShellProcessContinuationState.Completed;
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal((int)DOS.Error.ObjectNotFound,
            platform.Store.PublishedDiagnostics.IoError);
        Assert.Equal((int)ShellCommandResult.Warn,
            platform.Store.PublishedDiagnostics.ReturnCode);
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal((int)DOS.Error.ObjectNotFound, platform.Store.LastWhyDiagnostics.IoError);
        Assert.Equal((int)ShellCommandResult.Warn, platform.Store.LastWhyDiagnostics.ReturnCode);
        Assert.Equal(1, platform.Store.ContinuationReleaseCount);
    }

    [Fact]
    public void Failed_child_diagnostics_read_does_not_acknowledge_or_advance()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "external\n";
        platform.Store.ScriptExternalPending = true;
        var frame = InitializeFrame(ref platform);
        var workspace = CreateWorkspace();
        Assert.Equal(ShellScriptStepStatus.Waiting,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        platform.Store.ContinuationObservedState = ShellProcessContinuationState.Completed;
        platform.Store.ContinuationDiagnosticsFailure = true;
        Assert.Equal(ShellScriptStepStatus.PlatformFailure,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(0, platform.Store.ContinuationReleaseCount);
        Assert.Equal(0, platform.Store.DiagnosticsPublishCount);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame, out var pending));
        Assert.True(pending.PendingCommand.IsNotNull);
        Assert.Equal(0u, pending.CurrentOffset);
        platform.Store.ContinuationDiagnosticsFailure = false;
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(1, platform.Store.ContinuationReleaseCount);
    }

    [Fact]
    public void Frame_registration_failure_does_not_invent_child_failure_after_publication()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "external\n";
        platform.Store.ScriptExternalPending = true;
        platform.Store.ScriptCorruptFrameAfterLaunch = true;
        var frame = InitializeFrame(ref platform);
        var workspace = CreateWorkspace();
        Assert.Equal(ShellScriptStepStatus.PlatformFailure,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.True(ShellProcessContinuationCodec.TryRead(ref platform,
            platform.Store.ScriptExternalContinuation, out var child));
        Assert.Equal(ShellProcessContinuationState.Running, child.State);
        Assert.Equal(0, platform.Store.ContinuationReleaseCount);
    }

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void Failed_command_diagnostics_reach_Why_after_redirection_cleanup(bool redirect)
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = redirect ? "Stack bad >out\nWhy\n" :
			"Stack bad\nWhy\n";
		platform.Store.FailureLimit = 11;
		var frame = InitializeFrame(ref platform);
		var workspace = CreateDiagnosticWorkspace();

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out var failed));
		Assert.Equal((int)ShellCommandResult.Error, failed.CommandResult);
		Assert.Equal((int)DOS.Error.BadNumber, platform.Store.PublishedDiagnostics.IoError);
		Assert.Equal((int)ShellCommandResult.Error,
			platform.Store.PublishedDiagnostics.ReturnCode);
		Assert.Equal(redirect ? 1 : 0, platform.Store.RedirectionCloseCount);
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal((int)DOS.Error.BadNumber, platform.Store.LastWhyDiagnostics.IoError);
		Assert.Equal((int)ShellCommandResult.Error, platform.Store.LastWhyDiagnostics.ReturnCode);
	}

	[Fact]
	public void Successful_command_clears_stale_error_in_published_diagnostics()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Stack 4096\n";
		platform.Store.CommandIoError = (int)DOS.Error.ObjectNotFound;
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out var result));
		Assert.Equal((int)ShellCommandResult.Ok, result.CommandResult);
		Assert.Equal(0, platform.Store.PublishedDiagnostics.ReturnCode);
		Assert.Equal(0, platform.Store.PublishedDiagnostics.IoError);
		Assert.Equal(0, platform.Store.CommandIoError);
	}

	[Fact]
	public void First_cleanup_failure_survives_later_successful_closes()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Echo hello <in >out *>err\nWhy\n";
		platform.Store.FailureLimit = 11;
		platform.Store.RedirectionCloseFailAt = 1;
		var frame = InitializeFrame(ref platform);
		var workspace = CreateDiagnosticWorkspace();

		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out var result));
		Assert.Equal(3, platform.Store.RedirectionCloseCount);
		Assert.Equal((int)ShellCommandResult.Error, result.CommandResult);
		Assert.Equal((int)DOS.Error.DiskFull, platform.Store.PublishedDiagnostics.IoError);
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal((int)DOS.Error.DiskFull, platform.Store.LastWhyDiagnostics.IoError);
	}

	[Fact]
	public void Pending_child_does_not_publish_its_parents_diagnostic_state()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "external\n";
		platform.Store.ScriptExternalPending = true;
		platform.Store.ScriptCommandIoError = (int)DOS.Error.DiskFull;
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();

		Assert.Equal(ShellScriptStepStatus.Waiting,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal(0, platform.Store.DiagnosticsPublishCount);
		platform.Store.ContinuationObservedState = ShellProcessContinuationState.Completed;
		platform.Store.ContinuationResult = (int)ShellCommandResult.Error;
		Assert.Equal(ShellScriptStepStatus.FailureLimitExceeded,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal(1, platform.Store.DiagnosticsPublishCount);
		Assert.Equal(0, platform.Store.PublishedDiagnostics.IoError);
	}

	[Fact]
	public void Nonzero_result_without_a_fresh_error_does_not_reuse_previous_failure()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Stack bad\nStack 0\n";
		platform.Store.FailureLimit = 11;
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal((int)DOS.Error.BadNumber, platform.Store.PublishedDiagnostics.IoError);
		Assert.Equal(ShellScriptStepStatus.Executed,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out var invalidStack));
		Assert.Equal((int)ShellCommandResult.Error, invalidStack.CommandResult);
		Assert.Equal((int)ShellCommandResult.Error, platform.Store.PublishedDiagnostics.ReturnCode);
		Assert.Equal(0, platform.Store.PublishedDiagnostics.IoError);
	}

	[Fact]
	public void Blank_line_and_eof_leave_previous_public_diagnostics_unchanged()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "\n";
		platform.Store.PublishedDiagnostics = new ShellCommandDiagnostics
		{
			ReturnCode = (int)ShellCommandResult.Error,
			IoError = (int)DOS.Error.BadNumber,
		};
		var frame = InitializeFrame(ref platform);
		var workspace = CreateWorkspace();
		Assert.Equal(ShellScriptStepStatus.Empty,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal(ShellScriptStepStatus.EndOfFile,
			ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
		Assert.Equal(0, platform.Store.DiagnosticsPublishCount);
		Assert.Equal((int)ShellCommandResult.Error, platform.Store.PublishedDiagnostics.ReturnCode);
		Assert.Equal((int)DOS.Error.BadNumber, platform.Store.PublishedDiagnostics.IoError);
	}

	private static ShellScriptStepWorkspace CreateDiagnosticWorkspace()
	{
		var command = CreateCommandWorkspace();
		var redirection = new ShellRedirectionWorkspace(new APTR(1600), 256,
			new APTR(1900), 64, new APTR(2000), 64, new APTR(2100), 64);
		return new ShellScriptStepWorkspace(new APTR(1100), 256,
			new APTR(1400), 64, in command, in redirection);
	}

	[Fact]
	public void Start_initializes_the_guest_frame_before_running_startup_script()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Echo hello\n";
		ShellScriptFrameState initial = new()
		{
			Cli = new APTR(8),
			Input = new BPTR(1),
			Output = new BPTR(1),
			Error = new BPTR(1),
			CurrentLine = 1,
			Flags = ShellScriptFrameFlags.Active,
		};
		ShellScriptStepWorkspace workspace = CreateWorkspace();
		ShellScriptStartRequest request = new(
			new APTR(3000), in initial, in workspace, 4);

		var result = ShellScriptEngine.Start(ref platform, in request);

		Assert.Equal(ShellScriptStepStatus.EndOfFile, result.Status);
		Assert.Equal((int)ShellCommandResult.Ok, result.Result);
		Assert.Equal(2u, result.Steps);
		Assert.Equal("hello\n", platform.Store.OutputText);
		Assert.True(ShellScriptFrameCodec.TryRead(
			ref platform, request.Frame, out var state));
		Assert.Equal(2u, state.CurrentLine);
		Assert.Equal(APTR.Null, platform.Store.BoundFrame);
		Assert.Equal(APTR.Null, platform.Store.BoundCli);
	}

	[Fact]
	public void Start_rejects_an_invalid_workspace_without_publishing_a_frame()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		ShellScriptFrameState initial = new()
		{
			Cli = new APTR(8),
			Input = new BPTR(1),
			Output = new BPTR(1),
			Error = new BPTR(1),
			CurrentLine = 1,
			Flags = ShellScriptFrameFlags.Active,
		};
		ShellScriptStepWorkspace workspace = default;
		ShellScriptStartRequest request = new(
			new APTR(3000), in initial, in workspace, 4);

		var result = ShellScriptEngine.Start(ref platform, in request);

		Assert.Equal(ShellScriptStepStatus.InvalidFrame, result.Status);
		Assert.Equal(0u, result.Steps);
		Assert.False(ShellScriptFrameCodec.TryRead(
			ref platform, request.Frame, out _));
	}

	[Fact]
	public void Run_consumes_startup_script_until_eof_without_managed_state()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Echo hello\nexternal command\n";
		APTR frame = InitializeFrame(ref platform);
		ShellScriptStepWorkspace workspace = CreateWorkspace();

		var result = ShellScriptEngine.Run(ref platform, frame, in workspace, 8);

		Assert.Equal(ShellScriptStepStatus.EndOfFile, result.Status);
		Assert.Equal((int)ShellCommandResult.Ok, result.Result);
		Assert.Equal(3u, result.Steps);
		Assert.Equal(1, platform.Store.ScriptExecuteCount);
	}

	[Fact]
	public void Pre_scan_keeps_a_no_dot_script_on_its_original_reader()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Echo direct\n";
		APTR frame = InitializePreScanFrame(ref platform, APTR.Null, 0);
		APTR sourcePath = platform.Store.PutAt(7700, "S:direct");
		ShellScriptStepWorkspace workspace = CreateWorkspace();

		var result = ShellScriptPreScanner.Prepare(ref platform, new APTR(8),
			frame, new BPTR(1), sourcePath, 8, new APTR(7800), 64,
			in workspace);

		Assert.Equal(ShellScriptPreScanResult.Direct, result);
		Assert.Equal(1, platform.Store.RedirectionOpenCount);
		Assert.Equal(1, platform.Store.RedirectionCloseCount);
		Assert.Equal(string.Empty, platform.Store.OutputText);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var state));
		Assert.Equal(0u, state.ScriptKeyTemplateLength);
	}

	[Fact]
	public void Pre_scan_preserves_late_brackets_for_source_ordered_dispatch()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.AcceptScriptKeyTemplate = true;
		platform.Store.ScriptText = ".KEY filename/A\nEcho {filename}\n.BRA {\n.KET }\n";
		APTR arguments = platform.Store.PutAt(3500, " value");
		APTR frame = InitializePreScanFrame(ref platform, arguments, 6);
		APTR sourcePath = platform.Store.PutAt(7700, "S:dot");
		APTR temporaryPath = new(7800);
		ShellScriptStepWorkspace workspace = CreateWorkspace();

		var result = ShellScriptPreScanner.Prepare(ref platform, new APTR(8),
			frame, new BPTR(1), sourcePath, 5, temporaryPath, 64,
			in workspace);

		Assert.Equal(ShellScriptPreScanResult.Transformed, result);
		Assert.Equal(".KEY filename/A\nEcho {filename}\n.BRA {\n.KET }\n",
			platform.Store.OutputText);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var state));
		Assert.Equal(0u, state.ScriptKeyTemplateLength);
	}

	[Fact]
	public void Pre_scan_short_write_deletes_the_unpublished_work_file()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.AcceptScriptKeyTemplate = true;
		platform.Store.ShortWrite = true;
		platform.Store.ScriptText = ".KEY filename/A\nEcho <filename>\n";
		APTR arguments = platform.Store.PutAt(3500, " value");
		APTR frame = InitializePreScanFrame(ref platform, arguments, 6);
		APTR sourcePath = platform.Store.PutAt(7700, "S:short");
		ShellScriptStepWorkspace workspace = CreateWorkspace();

		var result = ShellScriptPreScanner.Prepare(ref platform, new APTR(8),
			frame, new BPTR(1), sourcePath, 7, new APTR(7800), 64,
			in workspace);

		Assert.Equal(ShellScriptPreScanResult.Failed, result);
		Assert.Equal(1, platform.Store.ScriptDeleteCount);
		Assert.StartsWith("T:Execute.", platform.Store.LastDeletedScriptPath);
		Assert.Equal(3, platform.Store.RedirectionCloseCount);
	}

	[Fact]
	public void Pre_scan_preserves_a_late_default_for_source_ordered_dispatch()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.AcceptScriptKeyTemplate = true;
		platform.Store.ScriptText = ".KEY filename\nEcho <filename>\n.DEF filename fallback\n";
		APTR frame = InitializePreScanFrame(ref platform, APTR.Null, 0);
		APTR sourcePath = platform.Store.PutAt(7700, "S:default");
		ShellCommandWorkspace command = CreateCommandWorkspace();
		ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
			new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
		ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
			new APTR(1400), 64, in command, in redirection);

		var result = ShellScriptPreScanner.Prepare(ref platform, new APTR(8),
			frame, new BPTR(1), sourcePath, 9, new APTR(7800), 64,
			in workspace);

		Assert.Equal(ShellScriptPreScanResult.Transformed, result);
		Assert.Equal(".KEY filename\nEcho <filename>\n.DEF filename fallback\n",
			platform.Store.OutputText);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var state));
		Assert.Equal(0u, state.ScriptKeyTemplateLength);
	}

	[Fact]
	public void Foreground_external_line_yields_and_resumes_after_child_completion()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "external command\nEcho after\n";
		platform.Store.ScriptExternalPending = true;
		APTR frame = InitializeFrame(ref platform);
		ShellScriptStepWorkspace workspace = CreateWorkspace();

		var waiting = ShellScriptEngine.Step(ref platform, frame,
			in workspace, out var first);

		Assert.Equal(ShellScriptStepStatus.Waiting, waiting);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var pendingFrame));
		Assert.Equal(1u, pendingFrame.CurrentLine);
		Assert.True(pendingFrame.PendingCommand.IsNotNull);

		platform.Store.ScriptExternalPending = false;
		platform.Store.ContinuationObservedState =
			ShellProcessContinuationState.Completed;
		platform.Store.ContinuationResult = 7;
		var resumed = ShellScriptEngine.Step(ref platform, frame,
			in workspace, out var second);

		Assert.Equal(ShellScriptStepStatus.Executed, resumed);
		Assert.Equal(7, second.CommandResult);
		Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
			out var afterChild));
		Assert.Equal(2u, afterChild.CurrentLine);
		Assert.Equal(APTR.Null, afterChild.PendingCommand);
		Assert.Equal(1, platform.Store.ContinuationReleaseCount);
	}

    [Fact]
    public void Key_substitution_survives_a_pending_external_child()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptExternalPending = true;
        platform.Store.ScriptExternalContinuation = new APTR(64);
        platform.Store.ScriptText = ".KEY filename/A\nexternal <filename>\n";
        APTR arguments = platform.Store.PutAt(3500, " value");
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptArguments = arguments,
            ScriptArgumentLength = 6, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Waiting,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("external value", platform.Store.LastScriptExternalCommand);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
            out var pending));
        Assert.Equal("filename/A", platform.Store.ReadText(
            pending.ScriptKeyTemplate, pending.ScriptKeyTemplateLength));

        platform.Store.ScriptExternalPending = false;
        platform.Store.ContinuationObservedState =
            ShellProcessContinuationState.Completed;
        platform.Store.ContinuationResult = 3;
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out var resumed));
        Assert.Equal(3, resumed.CommandResult);
        Assert.Equal(1, platform.Store.ContinuationReleaseCount);
    }

	[Fact]
	public void Run_stops_at_explicit_step_limit_and_preserves_frame()
	{
		EchoCommandTests.TestShellPlatform platform = new();
		platform.Store.ScriptText = "Echo one\nEcho two\n";
		APTR frame = InitializeFrame(ref platform);
		ShellScriptStepWorkspace workspace = CreateWorkspace();

		var result = ShellScriptEngine.Run(ref platform, frame, in workspace, 1);

		Assert.Equal(ShellScriptStepStatus.StepLimit, result.Status);
		Assert.Equal((int)ShellCommandResult.Ok, result.Result);
		Assert.Equal(1u, result.Steps);
		Assert.True(ShellScriptFrameCodec.TryRead(
			ref platform, frame, out var state));
		Assert.Equal(2u, state.CurrentLine);
	}

	[Fact]
	public void Steps_internal_and_external_lines_then_reports_eof()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Echo hello\nexternal command\n";
        APTR frame = InitializeFrame(ref platform);
        ShellScriptStepWorkspace workspace = CreateWorkspace();

        var status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var first);

        Assert.Equal(ShellScriptStepStatus.Executed, status);
        Assert.Equal(ShellInternalCommand.Echo, first.Command);
        Assert.Equal((int)ShellCommandResult.Ok, first.CommandResult);
        Assert.Equal("hello\n", platform.Store.OutputText);
        Assert.Equal(2u, ReadLine(ref platform, frame));

        status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var second);

        Assert.Equal(ShellScriptStepStatus.Executed, status);
        Assert.Equal(ShellInternalCommand.Unknown, second.Command);
        Assert.Equal("external command", platform.Store.LastScriptExternalCommand);
        Assert.Equal("external", platform.Store.LastScriptCommandName);
        Assert.Equal("command", platform.Store.LastScriptArgumentTail);
        Assert.Equal(1, platform.Store.ScriptExecuteCount);
        Assert.Equal(1, platform.Store.ScriptLookupCount);
        Assert.Equal(ShellScriptLookupKind.CommandPath,
            platform.Store.LastScriptLookupKind);
        Assert.Equal(ShellScriptLookupOrigin.CommandPath,
            platform.Store.LastScriptLookupOrigin);

        status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var eof);

        Assert.Equal(ShellScriptStepStatus.EndOfFile, status);
        Assert.Equal(1, platform.Store.ScriptExecuteCount);
        Assert.Equal((int)ShellCommandResult.Ok, eof.CommandResult);
    }

    [Fact]
    public void First_key_line_is_copied_and_validated_against_the_owned_tail()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename/A\nEcho later\n";
        APTR arguments = platform.Store.PutAt(3500, " value");
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptArguments = arguments,
            ScriptArgumentLength = 6, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellScriptStepWorkspace workspace = CreateWorkspace();

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame,
            out var state));
        Assert.Equal("filename/A", platform.Store.ReadText(
            state.ScriptKeyTemplate, state.ScriptKeyTemplateLength));
        Assert.Equal(10u, state.ScriptKeyTemplateLength);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Key_template_substitutes_the_reparsed_tail_before_dispatch()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename/A\nEcho <filename>\n";
        APTR arguments = platform.Store.PutAt(3500, " value");
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptArguments = arguments,
            ScriptArgumentLength = 6, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("value\n", platform.Store.OutputText);
        Assert.Equal(3, platform.Store.ReadArgsCount);
        Assert.Equal(3, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Def_supplies_a_default_when_key_argument_is_absent()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF filename \"fallback value\"\nEcho <filename>\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("fallback value\n", platform.Store.OutputText);
        Assert.Equal(3, platform.Store.ReadArgsCount);
        Assert.Equal(3, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Empty_equals_default_suppresses_the_expanded_line()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF filename=\nEcho \"X<filename>Y\"\nEcho AFTER\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("AFTER\n", platform.Store.OutputText);
    }

    [Fact]
    public void Whitespace_empty_default_suppresses_the_expanded_line()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF filename \nEcho \"X<filename>Y\"\nEcho AFTER\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("AFTER\n", platform.Store.OutputText);
    }

    [Fact]
    public void Whitespace_default_ignores_surplus_value_tokens()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF filename first second\nEcho VALUE:<filename>\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("VALUE:first\n", platform.Store.OutputText);
    }

    [Fact]
    public void Equals_default_ignores_surplus_value_tokens()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF filename=first second\nEcho VALUE:<filename>\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("VALUE:first\n", platform.Store.OutputText);
    }

    [Fact]
    public void Bare_default_is_a_noop()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF\nEcho SHOULD-RUN\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("SHOULD-RUN\n", platform.Store.OutputText);
    }

    [Fact]
    public void Empty_default_name_stops_the_script_with_error()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF =fallback\nEcho SHOULD-RUN\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        var run = ShellScriptEngine.Run(ref platform, frame, in workspace, 8);

        Assert.Equal(ShellScriptStepStatus.Malformed, run.Status);
        Assert.Equal((int)ShellCommandResult.Error, run.Result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Fact]
    public void Explicit_key_argument_overrides_an_equals_default()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF filename=fallback\nEcho <filename>\n";
        APTR arguments = platform.Store.PutAt(3500, " explicit");
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptArguments = arguments,
            ScriptArgumentLength = 9, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("explicit\n", platform.Store.OutputText);
        Assert.Equal(3, platform.Store.ReadArgsCount);
        Assert.Equal(3, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Duplicate_def_keeps_the_first_value_and_runs_later_lines()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF filename first\n.DEF filename second\nEcho X<filename>\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out var result));
        Assert.Equal(ShellInternalCommand.Echo, result.Command);
        Assert.Equal(3, platform.Store.ReadArgsCount);
        Assert.Equal((int)ShellCommandResult.Ok, result.CommandResult);
        Assert.Equal("Xfirst\n", platform.Store.OutputText);
    }

    [Fact]
    public void Undeclared_def_is_ignored_and_later_lines_run()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DEF missing fallback\nEcho SHOULD-RUN\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("SHOULD-RUN\n", platform.Store.OutputText);
    }

    [Fact]
    public void Key_expansion_uses_a_directly_defined_nonempty_default()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        APTR template = platform.Store.PutAt(3600, "filename");
        platform.WriteUInt8(template, 8, 0);
        platform.Clear(new APTR(template.Raw + 9),
            ShellScriptKeyExpansion.TemplateBufferCapacity - 9);
        APTR name = platform.Store.PutAt(4200, "filename");
        APTR value = platform.Store.PutAt(4300, "first");
        APTR source = platform.Store.PutAt(1100, "Echo <filename>");

        Assert.True(ShellScriptKeyExpansion.TryInitializeDirectiveState(
            ref platform, template, 8));
        Assert.True(ShellScriptKeyExpansion.TryDefineDefault(ref platform,
            template, 8, name, 8, value, 5));
        Assert.True(ShellScriptKeyExpansion.TryExpand(ref platform, source,
            15, APTR.Null, 0, template, 8, new APTR(1000), 96,
            new APTR(1600), 256, out var length));
        Assert.Equal(10u, length);
        Assert.Equal("Echo first", platform.Store.ReadText(new APTR(1600),
            length));
    }

    [Fact]
    public void Key_expansion_replaces_shell_number_placeholder()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        APTR template = platform.Store.PutAt(3600, "filename");
        platform.WriteUInt8(template, 8, 0);
        platform.Clear(new APTR(template.Raw + 9),
            ShellScriptKeyExpansion.TemplateBufferCapacity - 9);
        APTR source = platform.Store.PutAt(1100, "Echo <$$>");

        Assert.True(ShellScriptKeyExpansion.TryInitializeDirectiveState(
            ref platform, template, 8));
        Assert.True(ShellScriptKeyExpansion.TryExpand(ref platform, source,
            9, APTR.Null, 0, template, 8, new APTR(1000), 96,
            2_147_483_647u, new APTR(1600), 256, out var length));
        Assert.Equal(15u, length);
        Assert.Equal("Echo 2147483647", platform.Store.ReadText(
            new APTR(1600), length));
    }

    [Fact]
    public void Bra_and_ket_change_the_substitution_delimiters()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename/A\nEcho {filename}\n.BRA {\n.KET }\nEcho \"<literal>\" {filename}\n";
        APTR arguments = platform.Store.PutAt(3500, " value");
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptArguments = arguments,
            ScriptArgumentLength = 6, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("{filename}\n<literal> value\n", platform.Store.OutputText);
    }

    [Fact]
    public void Dol_changes_the_per_reference_default_separator()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename\n.DOL #\nEcho <filename#fallback>\n";
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("fallback\n", platform.Store.OutputText);
    }

    [Fact]
    public void Dot_changes_the_prefix_for_following_directives()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AcceptScriptKeyTemplate = true;
        platform.Store.ScriptText = ".KEY filename/A\n.DOT !\n!BRA {\n!KET }\nEcho {filename}\n";
        APTR arguments = platform.Store.PutAt(3500, " value");
        APTR template = new(3600);
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active, ScriptArguments = arguments,
            ScriptArgumentLength = 6, ScriptKeyTemplate = template,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(new APTR(1600), 256,
            new APTR(2200), 64, new APTR(2300), 64, new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command, in redirection, in alias);

        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Empty,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace, out _));
        Assert.Equal("value\n", platform.Store.OutputText);
    }

    [Fact]
    public void Skipping_suppresses_normal_commands_but_allows_control_commands()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Echo hidden\nElse\n";
        APTR frame = InitializeFrame(ref platform,
            ShellScriptFrameFlags.Active | ShellScriptFrameFlags.Skipping |
            ShellScriptFrameFlags.ConditionFalse);
        ShellScriptStepWorkspace workspace = CreateWorkspace();

        var status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var skipped);

        Assert.Equal(ShellScriptStepStatus.Skipped, status);
        Assert.Equal(ShellInternalCommand.Echo, skipped.Command);
        Assert.Equal(string.Empty, platform.Store.OutputText);
        Assert.Equal(0, platform.Store.ScriptExecuteCount);

        status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var control);

        Assert.Equal(ShellScriptStepStatus.Executed, status);
        Assert.Equal(ShellInternalCommand.Else, control.Command);
        Assert.Equal(ShellControlAction.Else,
            platform.Store.LastControlAction);
        Assert.Equal(1, platform.Store.ControlCount);
    }

    [Fact]
    public void Malformed_line_records_error_and_advances()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "\"unterminated\n";
        APTR frame = InitializeFrame(ref platform);
        ShellScriptStepWorkspace workspace = CreateWorkspace();

        var status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var result);

        Assert.Equal(ShellScriptStepStatus.Malformed, status);
        Assert.Equal((int)ShellCommandResult.Error, result.CommandResult);
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var state));
        Assert.Equal((int)ShellCommandResult.Error, state.LastResult);
        Assert.Equal((uint)2, state.CurrentLine);
    }

    [Fact]
    public void Rejects_inactive_frames_and_platform_line_failures()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Echo hello\n";
        APTR frame = InitializeFrame(ref platform,
            ShellScriptFrameFlags.None);
        ShellScriptStepWorkspace workspace = CreateWorkspace();

        var status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var invalid);
        Assert.Equal(ShellScriptStepStatus.InvalidFrame, status);
        Assert.Equal(ShellInternalCommand.Unknown, invalid.Command);

        frame = InitializeFrame(ref platform);
        platform.Store.ScriptReadFailure = true;
        status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var failed);
        Assert.Equal(ShellScriptStepStatus.PlatformFailure, status);
        Assert.Equal((int)ShellCommandResult.Error, failed.CommandResult);
    }

    [Fact]
    public void Step_records_line_metadata_in_an_optional_guest_input_record()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Echo hello\n";
        APTR frame = InitializeFrame(ref platform);
        APTR inputRecord = new(2800);
        ShellScriptInputState input = new()
        {
            Handle = new BPTR(1),
            Buffer = new APTR(1100),
            Capacity = 256,
        };
        Assert.True(ShellScriptInputCodec.Initialize(
            ref platform, inputRecord, in input));
        Assert.True(ShellScriptFrameCodec.TrySetInputState(
            ref platform, frame, inputRecord));
        ShellScriptStepWorkspace workspace = CreateWorkspace();

        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out _));
        Assert.True(ShellScriptInputCodec.TryRead(
            ref platform, inputRecord, out var recorded));
        Assert.Equal((uint)10, recorded.Length);
        Assert.Equal((uint)1, recorded.Line);
        Assert.Equal((uint)0, recorded.Offset);
        Assert.Equal((uint)0, recorded.Cursor);
    }

    [Fact]
    public void Step_expands_a_DOS_owned_alias_before_internal_resolution()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "ll\n";
        platform.Store.ScriptAliasReplacement = "Echo hello";
        APTR frame = InitializeFrame(ref platform);
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = default;
        ShellScriptAliasWorkspace alias = new(new APTR(1600), 256);
        ShellScriptStepWorkspace workspace = new(
            new APTR(1100),
            256,
            new APTR(1400),
            64,
            in command,
            in redirection,
            in alias);

        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out var result));
        Assert.Equal(ShellInternalCommand.Echo, result.Command);
        Assert.Equal("ll", platform.Store.LastScriptAliasSource);
        Assert.Equal(1, platform.Store.ScriptAliasExpansionCount);
        Assert.Equal("hello\n", platform.Store.OutputText);
    }

    [Fact]
    public void Step_passes_platform_lookup_classification_and_path_to_external_execution()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "residentcmd\n";
        platform.Store.ScriptLookupKind = ShellScriptLookupKind.Resident;
        platform.Store.ScriptLookupPath = "SYS:Libs/residentcmd";
        APTR frame = InitializeFrame(ref platform);
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = default;
        ShellScriptAliasWorkspace alias = default;
        ShellScriptLookupWorkspace lookup = new(new APTR(2200), 128);
        ShellScriptStepWorkspace workspace = new(
            new APTR(1100), 256,
            new APTR(1400), 64,
            in command,
            in redirection,
            in alias,
            in lookup);

        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out var result));
        Assert.Equal(ShellInternalCommand.Unknown, result.Command);
        Assert.Equal("residentcmd", platform.Store.LastScriptLookupName);
        Assert.Equal(ShellScriptLookupKind.Resident,
            platform.Store.LastScriptLookupKind);
        Assert.Equal(ShellScriptLookupOrigin.Resident,
            platform.Store.LastScriptLookupOrigin);
        Assert.Equal("SYS:Libs/residentcmd",
            platform.Store.LastScriptResolvedPath);
    }

    [Fact]
    public void Alias_expansion_is_the_source_for_following_redirection_parse()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "ll\n";
        platform.Store.ScriptAliasReplacement = "Echo hello >out";
        APTR frame = InitializeFrame(ref platform);
        ShellCommandWorkspace command = CreateCommandWorkspace();
        ShellRedirectionWorkspace redirection = new(
            new APTR(1600), 256,
            new APTR(2200), 64,
            new APTR(2300), 64,
            new APTR(2400), 64);
        ShellScriptAliasWorkspace alias = new(new APTR(1900), 256);
        ShellScriptLookupWorkspace lookup = default;
        ShellScriptStepWorkspace workspace = new(
            new APTR(1100), 256,
            new APTR(1400), 64,
            in command,
            in redirection,
            in alias,
            in lookup);

        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out var result));
        Assert.Equal(ShellInternalCommand.Echo, result.Command);
        Assert.Equal("out", platform.Store.RedirectionOutputPath);
        Assert.Equal("hello\n", platform.Store.OutputText);
    }

    [Fact]
    public void Step_acknowledges_ctrl_c_before_reading_a_script_line()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Echo ignored\n";
        platform.Store.ScriptSignalFlags = ShellScriptSignalFlags.CtrlC;
        platform.Store.ScriptSignalResult = 130;
        platform.Store.ScriptSignalSequence = 7;
        APTR signalRecord = new(2800);
        ShellScriptSignalState signalState = default;
        Assert.True(ShellScriptSignalCodec.Initialize(
            ref platform, signalRecord, in signalState));
        APTR frame = InitializeFrame(ref platform,
            ShellScriptFrameFlags.Active, signalRecord);
        ShellScriptStepWorkspace workspace = CreateWorkspace();

        Assert.Equal(ShellScriptStepStatus.Interrupted,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out var result));
        Assert.Equal(130, result.CommandResult);
        Assert.Equal(1, platform.Store.ScriptSignalPollCount);
        Assert.Equal(1, platform.Store.ScriptSignalAcknowledgeCount);
        Assert.Equal(0, platform.Store.ScriptExecuteCount);
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var frameState));
        Assert.True((frameState.Flags &
            ShellScriptFrameFlags.QuitRequested) != 0);
        Assert.Equal(130, frameState.QuitResult);
        Assert.Equal((uint)1, frameState.CurrentLine);
        Assert.True(ShellScriptSignalCodec.TryRead(
            ref platform, signalRecord, out var recorded));
        Assert.Equal((uint)7, recorded.AcknowledgedSequence);
        Assert.Equal(ShellScriptSignalFlags.None, recorded.Pending);
    }

    [Fact]
    public void Step_marks_a_terminated_frame_inactive_without_consuming_input()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Echo ignored\n";
        platform.Store.ScriptSignalFlags = ShellScriptSignalFlags.Terminated;
        platform.Store.ScriptSignalResult = -9;
        APTR frame = InitializeFrame(ref platform);
        platform.Store.ScriptSignalSequence = 4;
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var before));
        ShellScriptStepWorkspace workspace = CreateWorkspace();

        Assert.Equal(ShellScriptStepStatus.Terminated,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out var result));
        Assert.Equal(-9, result.CommandResult);
        Assert.Equal(0, platform.Store.ScriptExecuteCount);
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var after));
        Assert.False((after.Flags & ShellScriptFrameFlags.Active) != 0);
        Assert.True((after.Flags & ShellScriptFrameFlags.EndRequested) != 0);
        Assert.Equal(before.CurrentLine, after.CurrentLine);
    }

	private static APTR InitializeFrame(
        ref EchoCommandTests.TestShellPlatform platform,
        ShellScriptFrameFlags flags = ShellScriptFrameFlags.Active,
        APTR signalState = default)
    {
        APTR frame = new(3000);
        ShellScriptFrameState state = new()
        {
            Cli = new APTR(8),
            Input = new BPTR(1),
            Output = new BPTR(1),
            Error = new BPTR(1),
            CurrentLine = 1,
            Flags = flags,
            SignalState = signalState,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in state));
        return frame;
	}

	private static APTR InitializePreScanFrame(
		ref EchoCommandTests.TestShellPlatform platform, APTR arguments,
		uint argumentLength)
	{
		APTR frame = new(3000);
		ShellScriptFrameState state = new()
		{
			Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
			Error = new BPTR(1), CurrentLine = 1,
			Flags = ShellScriptFrameFlags.Active, ScriptArguments = arguments,
			ScriptArgumentLength = argumentLength, ScriptKeyTemplate = new APTR(3600),
		};
		Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
			in state));
		return frame;
	}

    private static ShellScriptStepWorkspace CreateWorkspace()
    {
        ShellCommandWorkspace command = CreateCommandWorkspace();
        var workspace = new ShellScriptStepWorkspace(
            new APTR(1100),
            256,
            new APTR(1400),
            64,
            in command);
		workspace.PromptTemplate = new APTR(5000);
		workspace.PromptTemplateCapacity = 256;
		workspace.PromptCapturePath = new APTR(5300);
		workspace.PromptCapturePathCapacity = 512;
		return workspace;
    }

	private static ShellScriptStepWorkspace CreatePromptWorkspace()
	{
		var command = CreateCommandWorkspace();
		var redirection = new ShellRedirectionWorkspace(new APTR(1500), 1024,
			new APTR(2600), 256, new APTR(2900), 256, new APTR(3200), 256);
		var alias = new ShellScriptAliasWorkspace(new APTR(3500), 1024);
		var lookup = new ShellScriptLookupWorkspace(new APTR(4600), 256);
		var workspace = new ShellScriptStepWorkspace(new APTR(1100), 256,
			new APTR(1400), 64, in command, in redirection, in alias, in lookup);
		workspace.PromptTemplate = new APTR(5000);
		workspace.PromptTemplateCapacity = 256;
		workspace.PromptCapturePath = new APTR(5300);
		workspace.PromptCapturePathCapacity = 512;
		return workspace;
	}

    private static ShellCommandWorkspace CreateCommandWorkspace() =>
        new(
            new APTR(400), 96,
            new APTR(520), 96,
            new APTR(640), 96,
            new APTR(760), 96,
            new APTR(880), 96,
            new APTR(1000), 96);

    private static uint ReadLine(
        ref EchoCommandTests.TestShellPlatform platform,
        APTR frame)
    {
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var state));
        return state.CurrentLine;
    }
}
