using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptEngineTests
{
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
        Assert.Equal(1, platform.Store.ScriptExecuteCount);
        Assert.Equal(1, platform.Store.ScriptLookupCount);
        Assert.Equal(ShellScriptLookupKind.CommandPath,
            platform.Store.LastScriptLookupKind);

        status = ShellScriptEngine.Step(
            ref platform, frame, in workspace, out var eof);

        Assert.Equal(ShellScriptStepStatus.EndOfFile, status);
        Assert.Equal(1, platform.Store.ScriptExecuteCount);
        Assert.Equal((int)ShellCommandResult.Ok, eof.CommandResult);
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

    private static ShellScriptStepWorkspace CreateWorkspace()
    {
        ShellCommandWorkspace command = CreateCommandWorkspace();
        return new ShellScriptStepWorkspace(
            new APTR(1100),
            256,
            new APTR(1400),
            64,
            in command);
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
