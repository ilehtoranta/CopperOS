using Amiga;
using CopperStart.Dos;
using CopperStart.Exec;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Native-safe Shell capability owner backed by CopperStart DOS.
///
/// The value carries only the DOS provider and guest DOS-state pointer. DOS
/// owns all allocations, CLI records, aliases, variables, handles, resident
/// entries, and task continuations. Methods which require the not-yet-wired
/// scheduler or interactive requester surface return a deterministic failure;
/// they do not substitute managed callbacks or host services.
/// </summary>
public struct DosShellPlatform<TDosPlatform> : IShellPlatform,
	IShellScriptPlatform where TDosPlatform : struct, IExecMemoryPlatform,
	IDosPlatform
{
	private struct ShellLaunchResources
	{
		public BPTR Input;
		public BPTR Output;
		public BPTR Error;
		public BPTR Window;
		public BPTR From;
	}

	private const uint ShellLaunchTagBytes = TagItem.Size * 5;
	private const uint DefaultShellStartupLength = 15;
	private const uint DefaultShellStartupStorage =
		DefaultShellStartupLength + 1;

	public DosShellPlatform(TDosPlatform dos, APTR state)
	{
		Dos = dos;
		State = state;
		ExecBase = APTR.Null;
	}

	public DosShellPlatform(TDosPlatform dos, APTR state, APTR execBase)
	{
		Dos = dos;
		State = state;
		ExecBase = execBase;
	}

	public TDosPlatform Dos;
	public APTR State;
	/// <summary>ExecBase supplied by the live DOS/Exec owner for child launch.</summary>
	public APTR ExecBase;

	private const uint ScriptLineCapacity = 4096;
	private const uint ScriptCommandNameCapacity = 256;
	private const uint ScriptTokenCapacity = 4096;
	private const uint ScriptSmallCapacity = 512;
	private const uint ScriptPromptCapacity = 256;
	private const uint ScriptErrorCodeCapacity = FaultCommand.MaximumErrorCodes * 4;
	private const uint ScriptMaximumSteps = 4096;

	public byte ReadUInt8(APTR address, int offset = 0) => Dos.ReadUInt8(address, offset);
	public ushort ReadUInt16(APTR address, int offset = 0) => Dos.ReadUInt16(address, offset);
	public uint ReadUInt32(APTR address, int offset = 0) => Dos.ReadUInt32(address, offset);
	public void WriteUInt8(APTR address, int offset, byte value) => Dos.WriteUInt8(address, offset, value);
	public void WriteUInt16(APTR address, int offset, ushort value) => Dos.WriteUInt16(address, offset, value);
	public void WriteUInt32(APTR address, int offset, uint value) => Dos.WriteUInt32(address, offset, value);
	public void Clear(APTR address, uint byteCount) => Dos.Clear(address, byteCount);
	public void Copy(APTR source, APTR destination, uint byteCount) => Dos.Copy(source, destination, byteCount);
	public bool IsMapped(APTR address, uint byteSize) => Dos.IsMapped(address, byteSize);

	public bool TryReadCliDefaultStack(APTR cli, out int stackBytes) =>
		DosShellNativeBridge.ReadCliDefaultStack(ref Dos, State, cli,
			out stackBytes);

	public bool TryReadCliFailureLimit(APTR cli, out uint failureLimit) =>
		DosShellNativeBridge.ReadCliFailureLimit(ref Dos, State, cli,
			out failureLimit);

	public bool TryWriteCliDefaultStack(APTR cli, int stackBytes) =>
		DosShellNativeBridge.WriteCliDefaultStack(ref Dos, State, cli,
			stackBytes);

	public bool TryWriteCliFailureLimit(APTR cli, uint failureLimit) =>
		DosShellNativeBridge.WriteCliFailureLimit(ref Dos, State, cli,
			failureLimit);

	public bool TryGetCurrentDirectory(APTR cli, APTR path,
		uint pathCapacity, out uint pathLength) =>
		DosShellNativeBridge.GetCurrentDirectory(ref Dos, State, cli, path,
			pathCapacity, out pathLength);

	public bool TryChangeCurrentDirectory(APTR cli, APTR path,
		uint pathLength) => DosShellNativeBridge.ChangeCurrentDirectory(
		ref Dos, State, cli, path, pathLength);

	public bool TrySetAlias(APTR cli, APTR name, uint nameLength,
		APTR replacement, uint replacementLength) =>
		DosShellNativeBridge.SetAlias(ref Dos, State, cli, name, nameLength,
			replacement, replacementLength);

	public bool TryRemoveAlias(APTR cli, APTR name, uint nameLength) =>
		DosShellNativeBridge.RemoveAlias(ref Dos, State, cli, name, nameLength);

	public bool TryWriteAliases(BPTR output, APTR cli) =>
		DosShellNativeBridge.WriteAliases(ref Dos, State, output, cli);

	public bool TryUpdateCommandPath(APTR cli, APTR pathBuffer,
		uint pathBytes, uint pathCount, uint operation, uint quiet) =>
		DosShellNativeBridge.UpdateCommandPath(ref Dos, State, cli, pathBuffer,
			pathBytes, pathCount, operation, quiet);

	public bool TryWriteCommandPath(BPTR output, APTR cli, uint quiet) =>
		DosShellNativeBridge.WriteCommandPath(ref Dos, State, output, cli,
			quiet);

	public bool TryBindScriptFrame(APTR cli, APTR frame) =>
		DosShellNativeBridge.BindScriptFrame(ref Dos, State, cli, frame);

	public bool TryUnbindScriptFrame(APTR cli, APTR frame) =>
		DosShellNativeBridge.UnbindScriptFrame(ref Dos, State, cli, frame);

	public bool TryRequestShellControl(APTR cli, ShellControlAction action,
		int returnCode)
	{
		if (!DosShellNativeBridge.TryGetScriptFrame(ref Dos, State, cli,
			out var frame))
			return false;
		if (action == ShellControlAction.Else)
			return ShellScriptControlTransitions.TryElse(ref this, frame);
		if (action is ShellControlAction.EndIf or ShellControlAction.EndSkip)
		{
			var expected = action == ShellControlAction.EndIf
				? ShellScriptBlockKind.If : ShellScriptBlockKind.Skip;
			if (!ShellScriptControlTransitions.TryClose(ref this, frame,
				expected, out var closed))
				return false;
			var recorded = ShellScriptFrameCodec.TryRecordControl(ref this,
				frame, action, returnCode);
			var released = DosShellNativeBridge.FreeScriptRecord(ref Dos,
				State, frame, closed, 1);
			return recorded && released;
		}
		if (action is not (ShellControlAction.EndCli or
			ShellControlAction.EndShell or ShellControlAction.Quit))
			return false;
		return ShellScriptFrameCodec.TryRecordControl(ref this, frame, action,
			returnCode);
	}

	public bool TryDefineScriptLabel(APTR cli, APTR label,
		uint labelLength)
	{
		if (!DosShellNativeBridge.TryGetScriptFrame(ref Dos, State, cli,
			out var frame) ||
			!ShellScriptFrameCodec.TryRead(ref this, frame, out var state))
			return false;
		if (label.IsNull || labelLength == 0 || labelLength > 65_535 ||
			label.Raw > uint.MaxValue - labelLength ||
			!Dos.IsMapped(label, labelLength))
			return false;
		var record = DosShellNativeBridge.AllocateScriptRecord(ref Dos,
			State, frame, ShellScriptLabelCodec.Size, 2, labelLength,
			out var storedName);
		if (record.IsNull || storedName.IsNull) return false;
		Dos.Copy(label, storedName, labelLength);
		if (ShellScriptLabelTransitions.TryDefine(ref this, frame, record,
			storedName, labelLength, state.CurrentLine, state.CurrentOffset))
			return true;
		DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, record, 2);
		return false;
	}

	public bool TrySkipToLabel(APTR cli, APTR label, uint labelLength,
		uint back)
	{
		if (!DosShellNativeBridge.TryGetScriptFrame(ref Dos, State, cli,
			out var frame))
			return false;
		return ShellScriptLabelTransitions.TrySkip(ref this, frame, label,
			labelLength, back);
	}

	public bool TryAsk(APTR cli, BPTR input, BPTR output, APTR prompt,
		uint promptLength, out ShellAskResponse response)
	{
		response = default;
		if (cli.IsNull || input.IsNull || output.IsNull || prompt.IsNull ||
			promptLength == 0 || promptLength > 4095 ||
			prompt.Raw > uint.MaxValue - promptLength ||
			!Dos.IsMapped(prompt, promptLength) ||
			!DosShellNativeBridge.TryGetScriptFrame(ref Dos, State, cli,
				out _))
			return false;
		if (DosShellNativeBridge.Write(ref Dos, State, output, prompt,
			promptLength) < 0)
			return false;

		const uint AnswerCapacity = 256;
		var answer = Dos.AllocateGuest(AnswerCapacity);
		if (answer.IsNull || !Dos.IsMapped(answer, AnswerCapacity))
		{
			if (answer.IsNotNull) Dos.FreeGuest(answer, AnswerCapacity);
			return false;
		}
		var read = DosCore.FGets(ref Dos, State, input, answer,
			AnswerCapacity);
		if (read.IsNull)
		{
			Dos.FreeGuest(answer, AnswerCapacity);
			return false;
		}
		var decoded = ShellAskResponse.TryDecode(ref this, answer,
			AnswerCapacity, out response);
		Dos.FreeGuest(answer, AnswerCapacity);
		return decoded;
	}

	public bool TryEvaluateIf(APTR cli, uint condition, uint threshold,
		uint negate, uint noRequester, uint numeric, APTR left,
		uint leftLength, APTR right, uint rightLength)
	{
		if (negate > 1 || noRequester > 1 || numeric > 1 ||
			!DosShellNativeBridge.TryGetScriptFrame(ref Dos, State, cli,
				out var frame) || condition < (uint)ShellIfCondition.PreviousResult ||
			condition > (uint)ShellIfCondition.Exists)
			return false;
		if (condition == (uint)ShellIfCondition.PreviousResult &&
			(left.IsNotNull || right.IsNotNull || numeric != 0))
			return false;
		if (condition is (uint)ShellIfCondition.Equal or
			(uint)ShellIfCondition.Greater or
			(uint)ShellIfCondition.GreaterEqual)
		{
			if (!ValidText(left, leftLength) || !ValidText(right, rightLength))
				return false;
		}
		else if (condition == (uint)ShellIfCondition.Exists)
		{
			if (!ValidText(left, leftLength) || right.IsNotNull ||
				numeric != 0)
				return false;
		}

		var matched = false;
		if (condition == (uint)ShellIfCondition.PreviousResult)
		{
			if (!ShellScriptFrameCodec.TryRead(ref this, frame,
				out var state)) return false;
			matched = state.LastResult >= unchecked((int)threshold);
		}
		else if (condition == (uint)ShellIfCondition.Exists)
		{
			// Lock resolves either a file or directory. Open(OldFile) is only a
			// file-handle probe and incorrectly makes directory EXISTS false.
			var requesterScope = default(DosRequesterScope);
			if (!DosRequesterPolicy.TryEnter(ref Dos, noRequester,
				out requesterScope))
			{
				DosCore.SetIoErr(ref Dos, State, DOS.Error.ObjectWrongType);
				return false;
			}
			var objectLock = DosCore.Lock(ref Dos, State, left,
				DOS.LockMode.Shared);
			matched = objectLock.IsNotNull;
			if (objectLock.IsNotNull)
				DosCore.UnLock(ref Dos, State, objectLock);
			else
				DosCore.SetIoErr(ref Dos, State, DOS.Error.None);
			DosRequesterPolicy.Exit(ref Dos, ref requesterScope);
		}
		else
		{
			var comparison = CompareText(left, leftLength, right, rightLength);
			if (numeric != 0 && !TryCompareNumbers(left, leftLength, right,
				rightLength, out comparison))
				return false;
			matched = condition == (uint)ShellIfCondition.Equal
				? comparison == 0
				: condition == (uint)ShellIfCondition.Greater
					? comparison > 0 : comparison >= 0;
		}
		if (negate != 0) matched = !matched;

		if (!ShellScriptFrameCodec.TryRead(ref this, frame,
			out var frameState)) return false;
		var control = DosShellNativeBridge.AllocateScriptRecord(ref Dos,
			State, frame, ShellScriptControlCodec.Size, 1);
		if (control.IsNull) return false;
		var conditionFalse = matched ? 0u : 1u;
		if (!ShellScriptControlTransitions.TryOpen(ref this, frame, control,
			ShellScriptBlockKind.If, frameState.CurrentLine,
			frameState.CurrentOffset, conditionFalse))
		{
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, control,
				1);
			return false;
		}
		if (ShellScriptFrameCodec.TrySetCondition(ref this, frame, condition))
			return true;
		if (ShellScriptControlTransitions.TryClose(ref this, frame,
			ShellScriptBlockKind.If, out var closed))
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, closed,
				1);
		return false;
	}

	public ShellScriptExecutionStatus TryExecuteScript(APTR cli, APTR file,
		uint fileLength, APTR scriptArguments, uint scriptArgumentLength,
		out int result)
	{
		result = (int)ShellCommandResult.Error;
		if (cli.IsNull || file.IsNull || fileLength == 0 || fileLength > 65_535 ||
			file.Raw > uint.MaxValue - fileLength - 1 ||
			!Dos.IsMapped(file, fileLength + 1) ||
			!DosCommandLineInterfaceCodec.IsMapped(ref Dos, cli))
			return ShellScriptExecutionStatus.Failed;
		var existing = DosShellNativeBridge.FindScriptRunner(ref Dos, State, cli);
		if (existing.IsNotNull)
			return RunScriptRunner(existing, out result);

		var input = DosShellNativeBridge.OpenScript(ref Dos, State, file,
			fileLength);
		if (input.IsNull) return ShellScriptExecutionStatus.Failed;
		var ownedArguments = CopyScriptArguments(scriptArguments,
			scriptArgumentLength);
		if (scriptArgumentLength != 0 && ownedArguments.IsNull)
		{
			DosShellNativeBridge.CloseScript(ref Dos, State, input);
			return ShellScriptExecutionStatus.Failed;
		}
		var sourcePath = CopyScriptPath(file, fileLength);
		if (sourcePath.IsNull)
		{
			ReleaseScriptGuest(ref ownedArguments, scriptArgumentLength);
			DosShellNativeBridge.CloseScript(ref Dos, State, input);
			return ShellScriptExecutionStatus.Failed;
		}
		var frame = Dos.AllocateGuest(ShellScriptFrameCodec.Size);
		var line = Dos.AllocateGuest(ScriptLineCapacity);
		var commandName = Dos.AllocateGuest(ScriptCommandNameCapacity);
		var token = Dos.AllocateGuest(ScriptTokenCapacity);
		var first = Dos.AllocateGuest(ScriptSmallCapacity);
		var second = Dos.AllocateGuest(ScriptSmallCapacity);
		var third = Dos.AllocateGuest(ScriptSmallCapacity);
		var fourth = Dos.AllocateGuest(ScriptSmallCapacity);
		var errorCodes = Dos.AllocateGuest(ScriptErrorCodeCapacity);
		var redirectionCommand = Dos.AllocateGuest(ScriptLineCapacity);
		var redirectionInput = Dos.AllocateGuest(ScriptSmallCapacity);
		var redirectionOutput = Dos.AllocateGuest(ScriptSmallCapacity);
		var redirectionError = Dos.AllocateGuest(ScriptSmallCapacity);
		var aliasLine = Dos.AllocateGuest(ScriptLineCapacity);
		var lookupPath = Dos.AllocateGuest(ScriptSmallCapacity);
		var keyTemplate = Dos.AllocateGuest(ScriptLineCapacity);
		var temporaryPath = Dos.AllocateGuest(ScriptSmallCapacity);
		var promptTemplate = Dos.AllocateGuest(ScriptPromptCapacity);
		var runnerValue = new DosShellScriptRunnerRecord
		{
			Cli = cli, Frame = frame, Input = input, Line = line,
			InputOwned = 1,
			CommandName = commandName, Token = token, First = first,
			Second = second, Third = third, Fourth = fourth,
			ErrorCodes = errorCodes, RedirectionCommand = redirectionCommand,
			RedirectionInput = redirectionInput,
			RedirectionOutput = redirectionOutput,
			RedirectionError = redirectionError, AliasLine = aliasLine,
			LookupPath = lookupPath, ScriptKeyTemplate = keyTemplate,
			ScriptTemporaryPath = temporaryPath,
			PromptTemplate = promptTemplate,
			ScriptSourcePath = sourcePath,
			ScriptSourcePathLength = fileLength,
			ScriptArguments = ownedArguments,
			ScriptArgumentLength = scriptArgumentLength,
			State = DosShellScriptRunnerState.Running,
		};
		if (!ValidScriptRunnerBuffers(in runnerValue))
		{
			CleanupUnpublishedScript(ref runnerValue);
			return ShellScriptExecutionStatus.Failed;
		}
		var runner = DosShellNativeBridge.AllocateScriptRunner(ref Dos, State,
			frame, in runnerValue);
		if (runner.IsNull)
		{
			CleanupUnpublishedScript(ref runnerValue);
			return ShellScriptExecutionStatus.Failed;
		}
		var cliValue = DosCommandLineInterfaceCodec.Read(ref Dos, cli);
		if (!DosCore.TryGetShellProcessContext(ref Dos, State, cli,
			out var processContext))
		{
			DosShellNativeBridge.FreeScriptRunner(ref Dos, State, runner);
			return ShellScriptExecutionStatus.Failed;
		}
		var output = cliValue.CurrentOutput.IsNotNull
			? cliValue.CurrentOutput : cliValue.StandardOutput;
		var initial = new ShellScriptFrameState
		{
			Parent = cli, Cli = cli, Input = input, Output = output,
			Error = processContext.Error, CurrentDirectory = processContext.CurrentDirectory,
			CurrentLine = 1, CurrentOffset = 0,
			FailureLimit = cliValue.FailLevel > 0
				? unchecked((uint)cliValue.FailLevel)
				: ShellScriptFrameCodec.DefaultFailureLimit,
			LastResult = (int)ShellCommandResult.Ok,
			Flags = ShellScriptFrameFlags.Active,
			ScriptArguments = ownedArguments,
			ScriptArgumentLength = scriptArgumentLength,
			ScriptKeyTemplate = keyTemplate,
		};
		if (!ShellScriptFrameCodec.Initialize(ref this, frame, in initial) ||
			!DosShellNativeBridge.BindScriptFrame(ref Dos, State, cli, frame))
		{
			DosShellNativeBridge.FreeScriptRunner(ref Dos, State, runner);
			return ShellScriptExecutionStatus.Failed;
		}
		return RunScriptRunner(runner, out result);
	}

	public bool TryPollScriptExecution(APTR cli,
		out ShellScriptExecutionStatus status, out int result)
	{
		status = ShellScriptExecutionStatus.Failed;
		result = (int)ShellCommandResult.Error;
		if (cli.IsNull) return false;
		var runner = DosShellNativeBridge.FindScriptRunner(ref Dos, State, cli);
		if (runner.IsNull) return false;
		status = RunScriptRunner(runner, out result);
		return true;
	}

	public bool TryPrepareScriptWait(APTR cli)
	{
		if (cli.IsNull || Dos.CurrentDosTask.IsNull) return false;
		var runner = DosShellNativeBridge.FindScriptRunner(ref Dos, State, cli);
		if (runner.IsNull || !DosShellNativeBridge.ReadScriptRunner(ref Dos,
			State, runner, out var stored) ||
			!ShellScriptFrameCodec.TryRead(ref this, stored.Frame,
				out var frameState) || frameState.PendingCommand.IsNull)
			return false;
		return DosShellNativeBridge.PrepareForegroundWait(ref Dos, State,
			stored.Frame, cli, Dos.CurrentDosTask, frameState.PendingCommand,
			frameState.PendingNextLine, frameState.PendingNextOffset, out _,
			out _);
	}

	public bool TryParkScriptWait(APTR cli, uint timeoutTicks)
	{
		if (cli.IsNull) return false;
		var runner = DosShellNativeBridge.FindScriptRunner(ref Dos, State, cli);
		if (runner.IsNull || !DosShellNativeBridge.ReadScriptRunner(ref Dos,
			State, runner, out var stored)) return false;
		var wait = DosShellNativeBridge.FindForegroundWaitByFrame(ref Dos, State,
			stored.Frame);
		return wait.IsNotNull && DosShellNativeBridge.ParkForegroundWait(
			ref Dos, State, wait, timeoutTicks);
	}

	private ShellScriptExecutionStatus RunScriptRunner(APTR runner,
		out int result)
	{
		result = (int)ShellCommandResult.Error;
		if (!DosShellNativeBridge.ReadScriptRunner(ref Dos, State, runner,
			out var stored)) return ShellScriptExecutionStatus.Failed;
		if (!ShellScriptFrameCodec.TryRead(ref this, stored.Frame,
			out var frameState))
		{
			DosShellNativeBridge.FreeScriptRunner(ref Dos, State, runner);
			DosShellNativeBridge.UnbindScriptFrame(ref Dos, State, stored.Cli,
				stored.Frame);
			return ShellScriptExecutionStatus.Failed;
		}
		var workspace = BuildScriptWorkspace(in stored);
		if (stored.ScriptSourcePathLength != 0 &&
			stored.ScriptTemporaryPathLength == 0)
		{
			var prepared = ShellScriptPreScanner.Prepare(ref this, stored.Cli,
				stored.Frame, stored.Input, stored.ScriptSourcePath,
				stored.ScriptSourcePathLength, stored.ScriptTemporaryPath,
				ScriptSmallCapacity, in workspace);
			if (prepared == ShellScriptPreScanResult.Failed)
			{
				DosShellNativeBridge.FreeScriptRunner(ref Dos, State, runner);
				DosShellNativeBridge.UnbindScriptFrame(ref Dos, State, stored.Cli,
					stored.Frame);
				return ShellScriptExecutionStatus.Failed;
			}
			if (prepared == ShellScriptPreScanResult.Transformed &&
				!DosShellNativeBridge.ReadScriptRunner(ref Dos, State, runner,
					out stored))
			{
				DosShellNativeBridge.FreeScriptRunner(ref Dos, State, runner);
				DosShellNativeBridge.UnbindScriptFrame(ref Dos, State,
					stored.Cli, stored.Frame);
				return ShellScriptExecutionStatus.Failed;
			}
		}
		var run = ShellScriptEngine.Run(ref this, stored.Frame, in workspace,
			ScriptMaximumSteps);
		result = run.Result;
		if (run.Status == ShellScriptStepStatus.Waiting)
		{
			DosShellNativeBridge.SetScriptRunnerState(ref Dos, runner,
				DosShellScriptRunnerState.Pending, run.Result, run.Steps);
			if (Dos.CurrentDosTask.IsNotNull)
				TryPrepareScriptWait(stored.Cli);
			return ShellScriptExecutionStatus.Pending;
		}
		var terminal = run.Status == ShellScriptStepStatus.EndOfFile;
		var diagnosticsCaptured = ShellScriptCompletionDiagnostics.TryCapture(
			ref this, stored.Cli, in run, out var diagnostics);
		DosShellNativeBridge.SetScriptRunnerState(ref Dos, runner,
			terminal ? DosShellScriptRunnerState.Completed :
			DosShellScriptRunnerState.Failed, run.Result, run.Steps);
		var cli = stored.Cli;
		var wait = DosShellNativeBridge.FindForegroundWaitByFrame(ref Dos,
			State, stored.Frame);
		if (wait.IsNotNull)
			DosShellNativeBridge.FreeForegroundWait(ref Dos, State, wait);
		DosShellNativeBridge.FreeScriptRunner(ref Dos, State, runner);
		DosShellNativeBridge.UnbindScriptFrame(ref Dos, State, cli,
			stored.Frame);
		var failureLimitRestored = TryWriteCliFailureLimit(cli,
			ShellScriptFrameCodec.DefaultFailureLimit);
		if (!failureLimitRestored || !diagnosticsCaptured ||
			!TryPublishCommandDiagnostics(cli, in diagnostics))
		{
			result = (int)ShellCommandResult.Error;
			return ShellScriptExecutionStatus.Failed;
		}
		return terminal ? ShellScriptExecutionStatus.Completed :
			ShellScriptExecutionStatus.Failed;
	}

	private ShellScriptStepWorkspace BuildScriptWorkspace(
		in DosShellScriptRunnerRecord stored)
	{
		var commandWorkspace = new ShellCommandWorkspace(stored.Token,
			ScriptTokenCapacity, stored.First, ScriptSmallCapacity, stored.Second,
			ScriptSmallCapacity, stored.Third, ScriptSmallCapacity, stored.Fourth,
			ScriptSmallCapacity, stored.ErrorCodes, ScriptErrorCodeCapacity);
		var redirectionWorkspace = new ShellRedirectionWorkspace(
			stored.RedirectionCommand, ScriptLineCapacity, stored.RedirectionInput,
			ScriptSmallCapacity, stored.RedirectionOutput, ScriptSmallCapacity,
			stored.RedirectionError, ScriptSmallCapacity);
		var aliasWorkspace = new ShellScriptAliasWorkspace(stored.AliasLine,
			ScriptLineCapacity);
		var lookupWorkspace = new ShellScriptLookupWorkspace(stored.LookupPath,
			ScriptSmallCapacity);
		var workspace = new ShellScriptStepWorkspace(stored.Line, ScriptLineCapacity,
			stored.CommandName, ScriptCommandNameCapacity, in commandWorkspace,
			in redirectionWorkspace, in aliasWorkspace, in lookupWorkspace);
		workspace.PromptTemplate = stored.PromptTemplate;
		workspace.PromptTemplateCapacity = ScriptPromptCapacity;
		workspace.PromptCapturePath = stored.ScriptTemporaryPath;
		workspace.PromptCapturePathCapacity = ScriptSmallCapacity;
		if (DosCore.TryGetShellProcessContext(ref Dos, State, stored.Cli,
			out var processContext))
		{
			var shellNumber = DosProcessCodec.ReadTaskNumber(ref Dos,
				processContext.Task);
			if (shellNumber > 0)
				workspace.ShellNumber = unchecked((uint)shellNumber);
		}
		return workspace;
	}

	private bool ValidScriptRunnerBuffers(in DosShellScriptRunnerRecord value)
	{
		return value.Frame.IsNotNull && Dos.IsMapped(value.Frame,
			ShellScriptFrameCodec.Size) && value.Line.IsNotNull &&
			Dos.IsMapped(value.Line, ScriptLineCapacity) &&
			value.CommandName.IsNotNull && Dos.IsMapped(value.CommandName,
				ScriptCommandNameCapacity) && value.Token.IsNotNull &&
			Dos.IsMapped(value.Token, ScriptTokenCapacity) &&
			value.First.IsNotNull && Dos.IsMapped(value.First, ScriptSmallCapacity) &&
			value.Second.IsNotNull && Dos.IsMapped(value.Second, ScriptSmallCapacity) &&
			value.Third.IsNotNull && Dos.IsMapped(value.Third, ScriptSmallCapacity) &&
			value.Fourth.IsNotNull && Dos.IsMapped(value.Fourth, ScriptSmallCapacity) &&
			value.ErrorCodes.IsNotNull && Dos.IsMapped(value.ErrorCodes,
				ScriptErrorCodeCapacity) && value.RedirectionCommand.IsNotNull &&
			Dos.IsMapped(value.RedirectionCommand, ScriptLineCapacity) &&
			value.RedirectionInput.IsNotNull && Dos.IsMapped(value.RedirectionInput,
				ScriptSmallCapacity) && value.RedirectionOutput.IsNotNull &&
			Dos.IsMapped(value.RedirectionOutput, ScriptSmallCapacity) &&
			value.RedirectionError.IsNotNull && Dos.IsMapped(value.RedirectionError,
				ScriptSmallCapacity) && value.AliasLine.IsNotNull &&
			Dos.IsMapped(value.AliasLine, ScriptLineCapacity) &&
			value.LookupPath.IsNotNull && Dos.IsMapped(value.LookupPath,
				ScriptSmallCapacity) && value.ScriptKeyTemplate.IsNotNull &&
			Dos.IsMapped(value.ScriptKeyTemplate, ScriptLineCapacity) &&
			value.ScriptTemporaryPath.IsNotNull && Dos.IsMapped(
				value.ScriptTemporaryPath, ScriptSmallCapacity) &&
			value.PromptTemplate.IsNotNull && Dos.IsMapped(
				value.PromptTemplate, ScriptPromptCapacity) &&
			(value.ScriptSourcePathLength == 0
				? value.ScriptSourcePath.IsNull
				: value.ScriptSourcePath.IsNotNull &&
					value.ScriptSourcePathLength <= 65_535 &&
					Dos.IsMapped(value.ScriptSourcePath,
						value.ScriptSourcePathLength + 1) &&
					Dos.ReadUInt8(value.ScriptSourcePath,
						unchecked((int)value.ScriptSourcePathLength)) == 0) &&
			(value.ScriptArgumentLength == 0
				? value.ScriptArguments.IsNull
				: value.ScriptArguments.IsNotNull &&
					value.ScriptArgumentLength <= 65_535 &&
					Dos.IsMapped(value.ScriptArguments,
						value.ScriptArgumentLength));
	}

	private void CleanupUnpublishedScript(ref DosShellScriptRunnerRecord value)
	{
		ReleaseScriptGuest(ref value.Frame, ShellScriptFrameCodec.Size);
		ReleaseScriptGuest(ref value.Line, ScriptLineCapacity);
		ReleaseScriptGuest(ref value.CommandName, ScriptCommandNameCapacity);
		ReleaseScriptGuest(ref value.Token, ScriptTokenCapacity);
		ReleaseScriptGuest(ref value.First, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.Second, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.Third, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.Fourth, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.ErrorCodes, ScriptErrorCodeCapacity);
		ReleaseScriptGuest(ref value.RedirectionCommand, ScriptLineCapacity);
		ReleaseScriptGuest(ref value.RedirectionInput, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.RedirectionOutput, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.RedirectionError, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.AliasLine, ScriptLineCapacity);
		ReleaseScriptGuest(ref value.LookupPath, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.ScriptKeyTemplate, ScriptLineCapacity);
		ReleaseScriptGuest(ref value.ScriptTemporaryPath, ScriptSmallCapacity);
		ReleaseScriptGuest(ref value.PromptTemplate, ScriptPromptCapacity);
		ReleaseScriptGuest(ref value.ScriptSourcePath,
			value.ScriptSourcePathLength == 0 ? 0 :
				value.ScriptSourcePathLength + 1);
		ReleaseScriptGuest(ref value.ScriptArguments,
			value.ScriptArgumentLength);
		value.ScriptArgumentLength = 0;
		value.ScriptSourcePathLength = 0;
		value.ScriptKeyTemplateLength = 0;
		if (value.Input.IsNotNull)
		{
			DosShellNativeBridge.CloseScript(ref Dos, State, value.Input);
			value.Input = BPTR.Null;
		}
	}

	private APTR CopyScriptPath(APTR source, uint length)
	{
		if (source.IsNull || length == 0 || length > 65_535 ||
			source.Raw > uint.MaxValue - length - 1 ||
			!Dos.IsMapped(source, length + 1) ||
			Dos.ReadUInt8(source, unchecked((int)length)) != 0)
			return APTR.Null;
		var owned = Dos.AllocateGuest(length + 1);
		if (owned.IsNull || !Dos.IsMapped(owned, length + 1))
		{
			if (owned.IsNotNull) Dos.FreeGuest(owned, length + 1);
			return APTR.Null;
		}
		Dos.Copy(source, owned, length + 1);
		return owned;
	}

	private APTR CopyScriptArguments(APTR source, uint length)
	{
		if (length == 0) return APTR.Null;
		if (source.IsNull || length > 65_535 ||
			source.Raw > uint.MaxValue - length || !Dos.IsMapped(source, length))
			return APTR.Null;
		var owned = Dos.AllocateGuest(length);
		if (owned.IsNull || !Dos.IsMapped(owned, length))
		{
			if (owned.IsNotNull) Dos.FreeGuest(owned, length);
			return APTR.Null;
		}
		Dos.Copy(source, owned, length);
		return owned;
	}

	public bool TryRunCommand(APTR cli, BPTR input, BPTR output, BPTR error,
		BPTR currentDirectory, APTR continuation, APTR command, uint commandLength, uint detach,
		uint quiet, uint stack, uint stackPresent, int priority,
		uint priorityPresent)
		=> TryRunCommandCore(cli, input, output, error, currentDirectory,
			continuation, command, commandLength, detach, quiet, stack,
			stackPresent, priority, priorityPresent, out _);

	private bool TryRunCommandCore(APTR cli, BPTR input, BPTR output,
		BPTR error, BPTR currentDirectory, APTR continuation, APTR command,
		uint commandLength, uint detach, uint quiet, uint stack,
		uint stackPresent, int priority, uint priorityPresent,
		out bool rollbackComplete)
	{
		rollbackComplete = false;
		_ = detach;
		_ = quiet;
		if (ExecBase.IsNull || cli.IsNull || command.IsNull || commandLength == 0 ||
			commandLength > 65_535 || command.Raw > uint.MaxValue - commandLength ||
			!Dos.IsMapped(command, commandLength))
			return false;
		if (!DosCore.TryGetShellProcessContext(ref Dos, State, cli,
			out var processContext)) return false;
		var inheritance = new ShellChildInheritance(input, output, error,
			processContext.CurrentDirectory, processContext.CliState);

		var nameLength = FirstTokenLength(ref Dos, command, commandLength);
		if (nameLength == 0) return false;
		var name = Dos.AllocateGuest(nameLength + 1);
		var path = Dos.AllocateGuest(512);
		if (name.IsNull || path.IsNull || !Dos.IsMapped(name, nameLength + 1) ||
			!Dos.IsMapped(path, 512))
		{
			if (name.IsNotNull) Dos.FreeGuest(name, nameLength + 1);
			if (path.IsNotNull) Dos.FreeGuest(path, 512);
			return false;
		}
		Dos.Copy(command, name, nameLength);
		Dos.WriteUInt8(name, unchecked((int)nameLength), 0);

		var found = DosShellNativeBridge.LookupCommand(ref Dos, State, cli, name,
			nameLength, path, 512, out var lookup);
		if (!found || lookup.PathLength == 0 || lookup.Kind ==
			DosShellNativeBridge.LookupKind.Script)
		{
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, 512);
			return false;
		}

		var residentEntry = APTR.Null;
		var residentAcquired = false;
		BPTR segment;
		if (lookup.Kind == DosShellNativeBridge.LookupKind.Resident)
		{
			if (!DosShellNativeBridge.AcquireResident(ref Dos, State, name,
				nameLength, out segment, out residentEntry))
			{
				Dos.FreeGuest(name, nameLength + 1);
				Dos.FreeGuest(path, 512);
				return false;
			}
			residentAcquired = true;
		}
		else
			segment = DosSegmentLoaderCore.Load(ref Dos, State, path);
		if (segment.IsNull || !DosCommandImageCore.TryInspect(ref Dos, State,
			segment, out var image))
		{
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else if (segment.IsNotNull)
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, 512);
			return false;
		}

		var tags = Dos.AllocateGuest(TagItem.Size * 5);
		if (tags.IsNull || !Dos.IsMapped(tags, TagItem.Size * 5))
		{
			if (tags.IsNotNull) Dos.FreeGuest(tags, TagItem.Size * 5);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, 512);
			return false;
		}
		Dos.Clear(tags, TagItem.Size * 5);
		WriteTaskTag(ref Dos, tags, 0, ExecConstants.TaskTagProgramCounter,
			image.EntryPoint.Raw);
		if (stackPresent > 1 ||
			(stackPresent == 0 && inheritance.CliState.DefaultStack < 0))
		{
			Dos.FreeGuest(tags, TagItem.Size * 5);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else if (segment.IsNotNull)
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, 512);
			return false;
		}
		var inheritedStack = inheritance.CliState.DefaultStack;
		var requestedStack = stackPresent != 0 ? stack : inheritedStack == 0
			? 4096u : unchecked((uint)inheritedStack);
		if (requestedStack < 64 || requestedStack > 16u * 1024u * 1024u)
		{
			Dos.FreeGuest(tags, TagItem.Size * 5);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, 512);
			return false;
		}
		WriteTaskTag(ref Dos, tags, 1, ExecConstants.TaskTagM68kStackSize,
			requestedStack);
		WriteTaskTag(ref Dos, tags, 2, ExecConstants.TaskTagName, name.Raw);
		// Keep required fields before the optional slot, which is TagDone when absent.
		if (priorityPresent != 0)
			WriteTaskTag(ref Dos, tags, 3, ExecConstants.TaskTagPriority,
				unchecked((uint)priority));
		WriteTaskTag(ref Dos, tags, 4, ExecConstants.TagDone, 0);

		var startup = new DosChildCliStartup(APTR.Null, 0, name, nameLength,
			path, lookup.PathLength, APTR.Null, 0);
		startup.ParentCli = cli;
		var inheritedResources = new DosChildInheritedResources(
			inheritance.Input, inheritance.Output, inheritance.Error,
			inheritance.CurrentDirectory);
		var task = DosChildProcessLaunchCore.PrepareFromImageWithStartup<
			TDosPlatform, ClassicPolicy>(ref Dos, ExecBase, State, tags, segment,
			continuation, in inheritedResources, in startup);
		Dos.FreeGuest(tags, TagItem.Size * 5);
		Dos.FreeGuest(name, nameLength + 1);
		Dos.FreeGuest(path, 512);
		if (task.IsNull)
		{
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			return false;
		}
		var argumentStart = nameLength;
		while (argumentStart < commandLength &&
			Dos.ReadUInt8(command, unchecked((int)argumentStart)) is
				(byte)' ' or (byte)'\t') argumentStart++;
		var child = new DosShellPreparedChild
		{
			State = State, ExecBase = ExecBase, Task = task,
			Entry = image.EntryPoint, Continuation = continuation,
			Arguments = APTR.FromPointer(argumentStart < commandLength
				? command.Raw + argumentStart : 0u),
			ArgumentLength = commandLength - argumentStart,
			CommandArguments = 1,
			Segment = segment, ResidentEntry = residentEntry,
		};
		return DosShellPreparedChildCore.Publish(ref Dos, in child,
			out rollbackComplete);
	}

	private static uint FirstTokenLength<TMemory>(ref TMemory memory,
		APTR source, uint length) where TMemory : struct, IAmigaGuestMemory
	{
		var count = 0u;
		while (count < length)
		{
			var value = memory.ReadUInt8(source, unchecked((int)count));
			if (value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
				break;
			count++;
		}
		return count;
	}

	private static void WriteTaskTag<TMemory>(ref TMemory memory, APTR tags,
		uint index, uint tag, uint data) where TMemory : struct, IAmigaGuestMemory
	{
		var item = APTR.FromPointer(tags.Raw + index * TagItem.Size);
		UtilityTagItemCodec.Write(ref memory, item, new TagItem
		{
			Tag = tag,
			Data = data,
		});
	}

	public bool TryCreateShell(APTR parentCli, ShellLaunchKind kind,
		BPTR input, BPTR output, BPTR error, BPTR currentDirectory,
		APTR continuation, APTR window,
		uint windowLength, APTR from, uint fromLength)
	{
		if (ExecBase.IsNull || State.IsNull || parentCli.IsNull ||
			(kind != ShellLaunchKind.NewCli && kind != ShellLaunchKind.NewShell &&
			 kind != ShellLaunchKind.Cli) ||
			(window.IsNull ? windowLength != 0 : windowLength == 0) ||
			(from.IsNull && fromLength != 0) || fromLength > 255 ||
			(from.IsNotNull && (from.Raw > uint.MaxValue - fromLength - 1 ||
				!Dos.IsMapped(from, fromLength + 1) ||
				Dos.ReadUInt8(from, unchecked((int)fromLength)) != 0)))
			return false;
		if (!DosCore.TryGetShellProcessContext(ref Dos, State, parentCli,
			out var processContext) ||
			!DosShellNativeLaunchCore.TryResolveChildStack(
				processContext.CliState.DefaultStack, 0, 0, out var childStack))
			return false;

		var startupScript = APTR.Null;
		var startupScriptLength = 0u;
		var ownedStartupScript = APTR.Null;
		if (kind == ShellLaunchKind.NewShell)
		{
			startupScript = from;
			startupScriptLength = fromLength;
			if (startupScript.IsNull)
			{
				ownedStartupScript = Dos.AllocateGuest(
					DefaultShellStartupStorage);
				if (ownedStartupScript.IsNull || !Dos.IsMapped(
					ownedStartupScript, DefaultShellStartupStorage))
				{
					if (ownedStartupScript.IsNotNull)
						Dos.FreeGuest(ownedStartupScript,
							DefaultShellStartupStorage);
					return false;
				}
				DosShellNativeLaunchCore.WriteDefaultShellStartup(ref Dos,
					ownedStartupScript);
				startupScript = ownedStartupScript;
				startupScriptLength = DefaultShellStartupLength;
			}
		}

		var resources = new ShellLaunchResources
		{
			Input = input,
			Output = output,
			Error = error,
		};
		if (window.IsNotNull)
		{
			resources.Window = DosCore.OpenConsoleName(ref Dos, State,
				window, windowLength);
			if (resources.Window.IsNull)
			{
				if (ownedStartupScript.IsNotNull)
					Dos.FreeGuest(ownedStartupScript,
						DefaultShellStartupStorage);
				return false;
			}
			resources.Input = resources.Window;
			resources.Output = resources.Window;
			resources.Error = resources.Window;
		}
		if (kind == ShellLaunchKind.NewCli && from.IsNotNull)
		{
			resources.From = DosCore.OpenConsoleName(ref Dos, State, from,
				fromLength);
			if (resources.From.IsNull)
			{
				var openError = DosCore.IoErr(ref Dos, State);
				CloseLaunchHandles(in resources, openError);
				if (ownedStartupScript.IsNotNull)
					Dos.FreeGuest(ownedStartupScript,
						DefaultShellStartupStorage);
				return false;
			}
			resources.Input = resources.From;
			if (resources.Window.IsNull)
			{
				resources.Output = resources.From;
				resources.Error = resources.From;
			}
		}
		if (resources.Window.IsNull && resources.From.IsNull)
		{
			resources.Window = DosCore.OpenDefaultConsole(ref Dos, State);
			if (resources.Window.IsNull)
			{
				var openError = DosCore.IoErr(ref Dos, State);
				CloseLaunchHandles(in resources, openError);
				if (ownedStartupScript.IsNotNull)
					Dos.FreeGuest(ownedStartupScript,
						DefaultShellStartupStorage);
				return false;
			}
			resources.Input = resources.Window;
			resources.Output = resources.Window;
			resources.Error = resources.Window;
		}

		var childEntry = DosShellNativeEntrypoints.AddressOfShellChild();
		if (childEntry.IsNull)
		{
			var launchError = DosCore.IoErr(ref Dos, State);
			CloseLaunchHandles(in resources, launchError);
			if (ownedStartupScript.IsNotNull)
				Dos.FreeGuest(ownedStartupScript, DefaultShellStartupStorage);
			return false;
		}
		var tags = Dos.AllocateGuest(ShellLaunchTagBytes);
		if (tags.IsNull || !Dos.IsMapped(tags, ShellLaunchTagBytes))
		{
			var launchError = DosCore.IoErr(ref Dos, State);
			if (tags.IsNotNull) Dos.FreeGuest(tags, ShellLaunchTagBytes);
			CloseLaunchHandles(in resources, launchError);
			if (ownedStartupScript.IsNotNull)
				Dos.FreeGuest(ownedStartupScript, DefaultShellStartupStorage);
			return false;
		}
		Dos.Clear(tags, ShellLaunchTagBytes);
		WriteTaskTag(ref Dos, tags, 0,
			ExecConstants.TaskTagProgramCounter, childEntry.Raw);
		WriteTaskTag(ref Dos, tags, 1, ExecConstants.TaskTagM68kStackSize,
			childStack);
		WriteTaskTag(ref Dos, tags, 2, ExecConstants.TagDone, 0);
		WriteTaskTag(ref Dos, tags, 3, ExecConstants.TagDone, 0);
		WriteTaskTag(ref Dos, tags, 4, ExecConstants.TagDone, 0);

		var startup = new DosChildCliStartup
		{
			CommandFile = startupScript,
			CommandFileLength = startupScriptLength,
			ParentCli = parentCli,
			InteractivePresent = 1,
			Interactive = 1,
		};
		var inherited = new DosChildInheritedResources(resources.Input,
			resources.Output, resources.Error, processContext.CurrentDirectory);
		var task = DosChildProcessLaunchCore.PrepareWithStartup<
			TDosPlatform, ClassicPolicy>(ref Dos, ExecBase, State, tags,
			continuation, in inherited, in startup);
		Dos.FreeGuest(tags, ShellLaunchTagBytes);
		if (ownedStartupScript.IsNotNull)
			Dos.FreeGuest(ownedStartupScript, DefaultShellStartupStorage);

		var published = false;
		if (task.IsNotNull)
		{
			var child = new DosShellPreparedChild
			{
				State = State, ExecBase = ExecBase, Task = task, Entry = childEntry,
				Continuation = continuation,
				CommandArguments = 0,
			};
			published = DosShellPreparedChildCore.Publish(ref Dos, in child);
		}
		var finalError = DosCore.IoErr(ref Dos, State);
		CloseLaunchHandles(in resources, finalError);
		if (!published) DosCore.SetIoErr(ref Dos, State, finalError);
		return published;
	}

	private void CloseLaunchHandles(in ShellLaunchResources resources,
		DOS.Error restoreError)
	{
		if (resources.Window.IsNotNull)
			DosCore.Close(ref Dos, State, resources.Window);
		if (resources.From.IsNotNull && resources.From != resources.Window)
			DosCore.Close(ref Dos, State, resources.From);
		DosCore.SetIoErr(ref Dos, State, restoreError);
	}

	public bool TryManageResident(APTR cli,
		in ShellResidentManagementRequest request)
	{
		var dosRequest = new DosResidentManageRequest
		{
			Output = request.Output,
			Name = request.Name,
			NameLength = request.NameLength,
			File = request.File,
			FileLength = request.FileLength,
			Alias = request.Alias,
			AliasLength = request.AliasLength,
			Remove = request.Remove,
			Add = request.Add,
			Replace = request.Replace,
			Force = request.Force,
			System = request.System,
			Defer = request.Defer,
		};
		return DosShellNativeBridge.ManageResident(ref Dos, State,
			in dosRequest);
	}

	public bool TryGetInternalCommandEnabled(APTR cli, uint commandIdentity,
		out uint enabled) => DosShellNativeBridge.TryGetInternalCommandEnabled(
		ref Dos, State, cli, commandIdentity, out enabled);

	public bool TrySetInternalCommandEnabled(APTR cli, uint commandIdentity,
		uint enabled) => DosShellNativeBridge.TrySetInternalCommandEnabled(
		ref Dos, State, cli, commandIdentity, enabled);

	public bool TryPollShellContinuation(APTR cli, APTR continuation,
		out ShellProcessContinuationState state, out int result)
	{
		state = ShellProcessContinuationState.Failed;
		result = (int)ShellCommandResult.Error;
		if (!DosChildCompletionCore.TryPoll(ref Dos, State, cli,
			continuation, out var outcome))
			return false;
		result = outcome.Result;
		state = outcome.State switch
		{
			DosChildContinuationState.Pending =>
				ShellProcessContinuationState.Pending,
			DosChildContinuationState.Running =>
				ShellProcessContinuationState.Running,
			DosChildContinuationState.Completed =>
				ShellProcessContinuationState.Completed,
			DosChildContinuationState.Aborted =>
				ShellProcessContinuationState.Aborted,
			DosChildContinuationState.Failed =>
				ShellProcessContinuationState.Failed,
			_ => ShellProcessContinuationState.Failed,
		};
		return true;
	}

	public bool TryReadContinuationDiagnostics(APTR cli, APTR continuation,
		out ShellCommandDiagnostics diagnostics)
	{
		diagnostics = default;
		if (!DosChildCompletionCore.TryPoll(ref Dos, State, cli,
			continuation, out var outcome) || outcome.ResourcesRetired == 0 ||
			outcome.State is DosChildContinuationState.Pending or
				DosChildContinuationState.Running) return false;
		diagnostics.ReturnCode = outcome.Result;
		diagnostics.IoError = outcome.IoError;
		return true;
	}

	public bool TryReleaseShellContinuation(APTR cli, APTR continuation,
		uint ownedFlags)
	{
		if (cli.IsNull || continuation.IsNull ||
			!DosChildContinuationCodec.TryRead(ref Dos, continuation,
				out var current) || current.ParentCli != cli ||
			(current.Flags & (uint)DosChildContinuationFlags.ResourcesClosed) == 0)
			return false;
		if (ownedFlags != (current.Flags & ~(uint)
			DosChildContinuationFlags.ResourcesClosed)) return false;
		// DOS has already retired the child's process and handles. Only the
		// durable completion is acknowledged; ChildCli and BPTR snapshots may
		// now name unrelated allocations and must not be dereferenced.
		if ((current.Flags & (uint)DosChildContinuationFlags.RecordOwned) != 0)
			return DosShellNativeBridge.TryGetScriptFrame(ref Dos, State, cli,
				out var frame) && DosChildCompletionCore.TryAcknowledgeScriptRecord(
					ref Dos, State, cli, continuation, frame);
		// Caller-owned Run/NewShell records remain caller-owned.
		return DosChildCompletionCore.TryAcknowledge(ref Dos, State, cli,
			continuation);
	}

	public bool TryReadArgs(APTR argumentText, uint argumentLength,
		APTR template, uint templateLength, APTR resultArray,
		uint resultBytes, out APTR rdArgs)
	{
		rdArgs = DosShellNativeBridge.ReadArgs(ref Dos, State, argumentText,
			argumentLength, template, resultArray);
		return rdArgs.IsNotNull;
	}

	public bool TryReadScriptFilePrefix(APTR argumentText,
		uint argumentLength, out uint consumed) =>
		DosShellNativeBridge.TryReadScriptFilePrefix(ref Dos, State,
			argumentText, argumentLength, out consumed);

	public void FreeArgs(APTR rdArgs) =>
		DosShellNativeBridge.FreeArgs(ref Dos, State, rdArgs);

	public bool TryGetLocalVariable(APTR cli, APTR name, uint nameLength,
		APTR value, uint valueCapacity, out uint valueLength) =>
		DosShellNativeBridge.GetLocalVariable(ref Dos, State, cli, name,
			nameLength, value, valueCapacity, out valueLength);

	public bool TrySetLocalVariable(APTR cli, APTR name, uint nameLength,
		APTR value, uint valueLength) =>
		DosShellNativeBridge.SetLocalVariable(ref Dos, State, cli, name,
			nameLength, value, valueLength);

	public bool TryWriteLocalVariables(BPTR output, APTR cli) =>
		DosShellNativeBridge.WriteLocalVariables(ref Dos, State, output, cli);

	public bool TryGetGlobalVariable(APTR name, uint nameLength, APTR value,
		uint valueCapacity, out uint valueLength) =>
		DosShellNativeBridge.GetGlobalVariable(ref Dos, State, name,
			nameLength, value, valueCapacity, out valueLength);

	public bool TrySetGlobalVariable(APTR name, uint nameLength, APTR value,
		uint valueLength, uint save) =>
		DosShellNativeBridge.SetGlobalVariable(ref Dos, State, name,
			nameLength, value, valueLength, save);

	public bool TryWriteGlobalVariables(BPTR output) =>
		DosShellNativeBridge.WriteGlobalVariables(ref Dos, State, output);

	public bool TryRemoveLocalVariable(APTR cli, APTR name,
		uint nameLength) => DosShellNativeBridge.RemoveLocalVariable(ref Dos,
		State, cli, name, nameLength);

	public bool TryRemoveGlobalVariable(APTR name, uint nameLength,
		uint save) => DosShellNativeBridge.RemoveGlobalVariable(ref Dos, State,
		name, nameLength, save);

	public bool ClearConsole(BPTR output, uint reset)
	{
		if (reset > 1 || output.IsNull) return false;
		return DosShellNativeBridge.WriteByte(ref Dos, State, output,
			(byte)'\f') >= 0;
	}

	public bool TryWriteWhy(BPTR output, APTR cli) =>
		DosShellNativeBridge.WriteWhy(ref Dos, State, output, cli);

	public bool TryWriteFault(BPTR output, APTR errorCodes,
		uint errorCount) => DosShellNativeBridge.WriteFault(ref Dos, State,
			output, errorCodes, errorCount);

	public bool TrySetPrompt(APTR cli, APTR value, uint valueLength,
		uint reset) => DosShellNativeBridge.SetPrompt(ref Dos, State, cli,
		value, valueLength, reset);

	public bool TryWriteScriptPrompt(APTR cli, BPTR output) =>
		DosShellNativeBridge.WriteShellPrompt(ref Dos, State, cli, output);

	public bool TryCopyScriptPromptTemplate(APTR cli, APTR destination,
		uint destinationCapacity, out ShellScriptPromptTemplate template)
	{
		template = default;
		if (!DosShellNativeBridge.ReadShellPromptTemplate(ref Dos, State, cli,
			destination, destinationCapacity, out var length)) return false;
		template.Text = destination;
		template.Length = length;
		return true;
	}

	public bool TryWriteScriptPromptLiteral(APTR cli, BPTR output,
		in ShellScriptPromptSegment segment)
	{
		if (segment.Kind != ShellScriptPromptSegmentKind.Literal) return false;
		var span = new DosShellPromptTextSpan
		{
			Text = segment.Text,
			Length = segment.Length,
		};
		return DosShellNativeBridge.WriteShellPromptLiteral(ref Dos, State,
			cli, output, in span);
	}

	public int Write(BPTR handle, APTR source, uint length) =>
		DosShellNativeBridge.Write(ref Dos, State, handle, source, length);

	public int Read(BPTR handle, APTR destination, uint length) =>
		DosShellNativeBridge.Read(ref Dos, State, handle, destination, length);

	public int WriteByte(BPTR handle, byte value) =>
		DosShellNativeBridge.WriteByte(ref Dos, State, handle, value);

	public BPTR OpenOutput(APTR path, uint pathLength) =>
		DosShellNativeBridge.OpenOutput(ref Dos, State, path, pathLength, 0);

	public bool CloseOutput(BPTR handle) =>
		DosShellNativeBridge.CloseOutput(ref Dos, State, handle);

	public bool TryPollScriptSignal(APTR cli,
		out ShellScriptSignalEvent signal)
	{
		signal = default;
		var received = DosShellNativeBridge.TakeShellSignals(ref Dos, State, cli);
		if ((received & DosShellNativeBridge.SignalCtrlCMask) != 0)
		{
			signal.Flags = ShellScriptSignalFlags.Break |
				ShellScriptSignalFlags.CtrlC;
			signal.Result = (int)DOS.Error.Break;
			signal.Sequence = DosShellNativeBridge.SignalCtrlCMask;
		}
		else if ((received & DosShellNativeBridge.SignalCtrlDMask) != 0)
		{
			signal.Flags = ShellScriptSignalFlags.CtrlD;
			signal.Sequence = DosShellNativeBridge.SignalCtrlDMask;
		}
		return true;
	}

	public bool TryCaptureCommandDiagnostics(APTR cli, int returnCode,
		out ShellCommandDiagnostics diagnostics)
	{
		diagnostics = new ShellCommandDiagnostics { ReturnCode = returnCode };
		if (!DosShellNativeBridge.TryCaptureCommandError(ref Dos, State, cli,
			returnCode, out var error)) return false;
		diagnostics.IoError = error;
		return true;
	}

	public bool TryReadPublishedCommandDiagnostics(APTR cli,
		out ShellCommandDiagnostics diagnostics)
	{
		diagnostics = default;
		if (!DosCommandLineInterfaceCodec.IsMapped(ref Dos, cli)) return false;
		var published = DosCommandLineInterfaceCodec.Read(ref Dos, cli);
		diagnostics.ReturnCode = published.ReturnCode;
		diagnostics.IoError = published.Result2;
		return true;
	}

	public bool TryBeginCommandDiagnostics(APTR cli) =>
		DosShellNativeBridge.BeginCommandDiagnostics(ref Dos, State, cli);

	public bool TryPublishCommandDiagnostics(APTR cli,
		in ShellCommandDiagnostics diagnostics) =>
		DosShellNativeBridge.PublishCommandDiagnostics(ref Dos, State, cli,
			diagnostics.ReturnCode, diagnostics.IoError);

	public bool TryAcknowledgeScriptSignal(APTR cli,
		in ShellScriptSignalEvent signal)
	{
		_ = cli;
		_ = signal;
		return true;
	}

	public bool TryExpandScriptAlias(APTR cli, APTR source,
		uint sourceLength, APTR destination, uint destinationCapacity,
		out uint expanded, out uint expandedLength) =>
		DosShellNativeBridge.ExpandAlias(ref Dos, State, cli, source,
			sourceLength, destination, destinationCapacity, out expanded,
			out expandedLength);

	public bool TryLookupScriptCommand(APTR cli, APTR name,
		uint nameLength, in ShellScriptLookupWorkspace workspace,
		out ShellScriptLookupResult lookup)
	{
		var result = DosShellNativeBridge.LookupCommand(ref Dos, State, cli,
			name, nameLength, workspace.Path, workspace.Capacity,
			out var dosLookup);
		lookup = new ShellScriptLookupResult
		{
			Kind = dosLookup.Kind switch
			{
				DosShellNativeBridge.LookupKind.Resident =>
					ShellScriptLookupKind.Resident,
				DosShellNativeBridge.LookupKind.File =>
					ShellScriptLookupKind.ExplicitFile,
				DosShellNativeBridge.LookupKind.CurrentDirectory =>
					ShellScriptLookupKind.CurrentDirectory,
				DosShellNativeBridge.LookupKind.CommandPath =>
					ShellScriptLookupKind.CommandPath,
				DosShellNativeBridge.LookupKind.Script =>
					ShellScriptLookupKind.Script,
				DosShellNativeBridge.LookupKind.NotFound =>
					ShellScriptLookupKind.NotFound,
				_ => ShellScriptLookupKind.Malformed,
			},
			Origin = dosLookup.Origin switch
			{
				DosShellNativeBridge.LookupOrigin.Resident =>
					ShellScriptLookupOrigin.Resident,
				DosShellNativeBridge.LookupOrigin.ExplicitFile =>
					ShellScriptLookupOrigin.ExplicitFile,
				DosShellNativeBridge.LookupOrigin.CurrentDirectory =>
					ShellScriptLookupOrigin.CurrentDirectory,
				DosShellNativeBridge.LookupOrigin.CommandPath =>
					ShellScriptLookupOrigin.CommandPath,
				_ => ShellScriptLookupOrigin.None,
			},
			Protection = dosLookup.Protection,
			ResolvedPath = workspace.Path,
			PathLength = dosLookup.PathLength,
		};
		return result;
	}

	public bool TryReadScriptLine(APTR cli, BPTR input,
		uint currentLine, uint currentOffset, APTR destination,
		uint destinationCapacity, out uint lineLength, out uint nextLine,
		out uint nextOffset, out uint endOfFile) =>
		DosShellNativeBridge.ReadScriptLine(ref Dos, State, input, currentLine,
			currentOffset, destination, destinationCapacity, out lineLength,
			out nextLine, out nextOffset, out endOfFile);

	public bool TryExecuteScriptCommand(APTR cli, APTR frame,
		in ShellScriptCommandInvocation command,
		in ShellScriptLookupResult lookup,
		BPTR input, BPTR output,
		BPTR error, out int result, out APTR continuation)
	{
		result = (int)ShellCommandResult.Error;
		continuation = APTR.Null;
		if (lookup.Kind == ShellScriptLookupKind.Script)
			return TryLaunchScriptThroughExecute(cli, frame, in command,
				in lookup, input, output, error, out result, out continuation);
		if (ExecBase.IsNull || cli.IsNull || frame.IsNull ||
			command.Line.IsNull || command.LineLength == 0 ||
			command.LineLength > 65_535 ||
			command.Line.Raw > uint.MaxValue - command.LineLength ||
			!Dos.IsMapped(command.Line, command.LineLength) ||
			command.CommandName.IsNull || command.CommandNameLength == 0 ||
			command.CommandNameLength > 65_535 ||
			command.CommandName.Raw > uint.MaxValue - command.CommandNameLength ||
			!Dos.IsMapped(command.CommandName, command.CommandNameLength) ||
			!command.Arguments.IsContainedBy(command.Line, command.LineLength) ||
			lookup.ResolvedPath.IsNull || lookup.PathLength == 0 ||
			lookup.PathLength > 65_535 ||
			lookup.ResolvedPath.Raw > uint.MaxValue - lookup.PathLength ||
			!Dos.IsMapped(lookup.ResolvedPath, lookup.PathLength) ||
			lookup.Kind is ShellScriptLookupKind.NotFound or
			ShellScriptLookupKind.Script or ShellScriptLookupKind.Malformed)
			return false;
		if (!DosCore.TryGetShellProcessContext(ref Dos, State, cli,
			out var processContext)) return false;
		if (processContext.CliState.DefaultStack < 0) return false;
		var inheritedStack = processContext.CliState.DefaultStack == 0
			? 4096u : unchecked((uint)processContext.CliState.DefaultStack);
		if (inheritedStack < 64 || inheritedStack > 16u * 1024u * 1024u)
			return false;
		var nameLength = command.CommandNameLength;
		var name = Dos.AllocateGuest(nameLength + 1);
		var path = Dos.AllocateGuest(lookup.PathLength + 1);
		if (name.IsNull || path.IsNull || !Dos.IsMapped(name, nameLength + 1) ||
			!Dos.IsMapped(path, lookup.PathLength + 1))
		{
			if (name.IsNotNull) Dos.FreeGuest(name, nameLength + 1);
			if (path.IsNotNull) Dos.FreeGuest(path, lookup.PathLength + 1);
			return false;
		}
		Dos.Copy(command.CommandName, name, nameLength);
		Dos.WriteUInt8(name, unchecked((int)nameLength), 0);
		Dos.Copy(lookup.ResolvedPath, path, lookup.PathLength);
		Dos.WriteUInt8(path, unchecked((int)lookup.PathLength), 0);

		var residentEntry = APTR.Null;
		var residentAcquired = false;
		BPTR segment;
		if (lookup.Kind == ShellScriptLookupKind.Resident)
		{
			if (!DosShellNativeBridge.AcquireResident(ref Dos, State, name,
				nameLength, out segment, out residentEntry))
			{
				Dos.FreeGuest(name, nameLength + 1);
				Dos.FreeGuest(path, lookup.PathLength + 1);
				return false;
			}
			residentAcquired = true;
		}
		else
			segment = DosSegmentLoaderCore.Load(ref Dos, State, path);
		if (segment.IsNull || !DosCommandImageCore.TryInspect(ref Dos, State,
			segment, out var image))
		{
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else if (segment.IsNotNull)
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, lookup.PathLength + 1);
			return false;
		}

		var commandStorage = command.LineLength + 1;
		var record = DosShellNativeBridge.AllocateScriptRecord(ref Dos, State,
			frame, DosChildContinuationCodec.Size, 3, commandStorage,
			out var storedCommand);
		if (record.IsNull || storedCommand.IsNull)
		{
			if (record.IsNotNull)
				DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, lookup.PathLength + 1);
			return false;
		}
		Dos.Copy(command.Line, storedCommand, command.LineLength);
		Dos.WriteUInt8(storedCommand, unchecked((int)command.LineLength), 0);
		var initialContinuation = new DosChildContinuationRecord
		{
			ParentCli = cli,
			Command = storedCommand,
			CommandLength = command.LineLength,
			State = DosChildContinuationState.Pending,
			Flags = (uint)DosChildContinuationFlags.RecordOwned,
		};
		if (!DosChildContinuationCodec.Initialize(ref Dos, record,
			in initialContinuation))
		{
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, lookup.PathLength + 1);
			return false;
		}

		var tags = Dos.AllocateGuest(TagItem.Size * 5);
		if (tags.IsNull || !Dos.IsMapped(tags, TagItem.Size * 5))
		{
			if (tags.IsNotNull) Dos.FreeGuest(tags, TagItem.Size * 5);
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, lookup.PathLength + 1);
			return false;
		}
		Dos.Clear(tags, TagItem.Size * 5);
		WriteTaskTag(ref Dos, tags, 0, ExecConstants.TaskTagProgramCounter,
			image.EntryPoint.Raw);
		WriteTaskTag(ref Dos, tags, 1, ExecConstants.TaskTagM68kStackSize,
			inheritedStack);
		WriteTaskTag(ref Dos, tags, 2, ExecConstants.TaskTagName, name.Raw);
		WriteTaskTag(ref Dos, tags, 3, ExecConstants.TagDone, 0);
		// Keep a deterministic fifth slot for future foreground priority policy.
		WriteTaskTag(ref Dos, tags, 4, ExecConstants.TagDone, 0);
		var startup = new DosChildCliStartup(APTR.Null, 0, name, nameLength,
			path, lookup.PathLength, APTR.Null, 0);
		startup.ParentCli = cli;
		var inheritedResources = new DosChildInheritedResources(input, output,
			error, processContext.CurrentDirectory);
		var task = DosChildProcessLaunchCore.PrepareFromImageWithStartup<
			TDosPlatform, ClassicPolicy>(ref Dos, ExecBase, State, tags, segment,
			record, in inheritedResources, in startup);
		Dos.FreeGuest(tags, TagItem.Size * 5);
		Dos.FreeGuest(name, nameLength + 1);
		Dos.FreeGuest(path, lookup.PathLength + 1);
		if (task.IsNull)
		{
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			return false;
		}

		// DOS copies the argument tail into independent child-owned storage
		// before publication; neither the caller nor continuation owns its life.
		var arguments = command.Arguments;
		while (arguments.Length != 0 &&
			Dos.ReadUInt8(arguments.Data, 0) is (byte)' ' or (byte)'\t')
		{
			arguments.Data = APTR.FromPointer(arguments.Data.Raw + 1);
			arguments.Length--;
		}
		var child = new DosShellPreparedChild
		{
			State = State, ExecBase = ExecBase, Task = task,
			Entry = image.EntryPoint, Continuation = record,
			Arguments = arguments.Length == 0 ? APTR.Null : arguments.Data,
			ArgumentLength = arguments.Length,
			CommandArguments = 1,
			Segment = segment, ResidentEntry = residentEntry,
		};
		if (!DosShellPreparedChildCore.Publish(ref Dos, in child, out var rollbackComplete))
		{
			if (rollbackComplete)
				DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, record, 3);
			return false;
		}
		continuation = record;
		result = (int)ShellCommandResult.Ok;
		return true;
	}

	private bool TryLaunchScriptThroughExecute(APTR cli, APTR frame,
		in ShellScriptCommandInvocation command,
		in ShellScriptLookupResult lookup, BPTR input, BPTR output, BPTR error,
		out int result, out APTR continuation)
	{
		result = (int)ShellCommandResult.Error;
		continuation = APTR.Null;
		if (ExecBase.IsNull || State.IsNull || cli.IsNull || frame.IsNull ||
			lookup.Origin != ShellScriptLookupOrigin.CommandPath ||
			(lookup.Protection & FileProtection.Script) == 0 ||
			lookup.ResolvedPath.IsNull || lookup.PathLength == 0 ||
			lookup.PathLength > 65_535 ||
			lookup.ResolvedPath.Raw > uint.MaxValue - lookup.PathLength ||
			!Dos.IsMapped(lookup.ResolvedPath, lookup.PathLength) ||
			command.Line.IsNull || command.LineLength == 0 ||
			command.LineLength > 65_535 ||
			command.Line.Raw > uint.MaxValue - command.LineLength ||
			!Dos.IsMapped(command.Line, command.LineLength) ||
			!command.Arguments.IsContainedBy(command.Line, command.LineLength))
			return false;

		var arguments = command.Arguments;
		while (arguments.Length != 0 &&
			Dos.ReadUInt8(arguments.Data, 0) is
				(byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
		{
			arguments.Data = APTR.FromPointer(arguments.Data.Raw + 1);
			arguments.Length--;
		}
		var argumentLength = arguments.Length;
		var quotedPathLength = lookup.PathLength;
		for (var index = 0u; index < lookup.PathLength; index++)
		{
			var value = Dos.ReadUInt8(lookup.ResolvedPath,
				unchecked((int)index));
			if (value == 0) return false;
			if (value is (byte)'*' or (byte)'"') quotedPathLength++;
		}
		const uint executeNameLength = 7;
		var executeLineLength = executeNameLength + 1 + 1 +
			quotedPathLength + 1;
		if (argumentLength != 0)
		{
			if (executeLineLength >= 65_535 ||
				argumentLength > 65_535 - executeLineLength - 1)
				return false;
			executeLineLength += 1 + argumentLength;
		}
		if (executeLineLength == 0 || executeLineLength > 65_535)
			return false;

		var record = DosShellNativeBridge.AllocateScriptRecord(ref Dos, State,
			frame, DosChildContinuationCodec.Size, 3, executeLineLength + 1,
			out var executeCommand);
		if (record.IsNull || executeCommand.IsNull)
		{
			if (record.IsNotNull)
				DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame,
					record, 3);
			return false;
		}

		var cursor = 0u;
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'E');
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'x');
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'e');
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'c');
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'u');
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'t');
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'e');
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)' ');
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'"');
		for (var index = 0u; index < lookup.PathLength; index++)
		{
			var value = Dos.ReadUInt8(lookup.ResolvedPath,
				unchecked((int)index));
			if (value is (byte)'*' or (byte)'"')
				Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'*');
			Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), value);
		}
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)'"');
		if (argumentLength != 0)
		{
			Dos.WriteUInt8(executeCommand, unchecked((int)cursor++), (byte)' ');
			Dos.Copy(arguments.Data,
				APTR.FromPointer(executeCommand.Raw + cursor), argumentLength);
			cursor += argumentLength;
		}
		Dos.WriteUInt8(executeCommand, unchecked((int)cursor), 0);
		var initialContinuation = new DosChildContinuationRecord
		{
			ParentCli = cli,
			Command = executeCommand,
			CommandLength = executeLineLength,
			State = DosChildContinuationState.Pending,
			Flags = (uint)DosChildContinuationFlags.RecordOwned,
		};
		if (!DosChildContinuationCodec.Initialize(ref Dos, record,
			in initialContinuation))
		{
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame,
				record, 3);
			return false;
		}

		var launched = TryRunCommandCore(cli, input, output, error, BPTR.Null,
			record, executeCommand, executeLineLength, 0, 0, 0, 0, 0, 0,
			out var rollbackComplete);
		if (!launched)
		{
			if (rollbackComplete)
				DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame,
					record, 3);
			return false;
		}
		continuation = record;
		result = (int)ShellCommandResult.Ok;
		return true;
	}

	public bool TryOpenScriptInput(APTR cli, APTR path, uint pathLength,
		out BPTR handle)
	{
		handle = DosShellNativeBridge.OpenScript(ref Dos, State, path,
			pathLength);
		return handle.IsNotNull;
	}

	public bool TryOpenScriptOutput(APTR cli, APTR path, uint pathLength,
		uint append, out BPTR handle)
	{
		handle = DosShellNativeBridge.OpenOutput(ref Dos, State, path,
			pathLength, append);
		return handle.IsNotNull;
	}

	public bool TryPublishScriptInput(APTR cli, APTR frame, BPTR source,
		BPTR replacement, APTR temporaryPath, uint temporaryPathLength)
	{
		var runner = DosShellNativeBridge.FindScriptRunner(ref Dos, State, cli);
		return runner.IsNotNull && DosShellNativeBridge.ReadScriptRunner(ref Dos,
			State, runner, out var stored) && stored.Frame.Raw == frame.Raw &&
			stored.Input.Raw == source.Raw &&
			ShellScriptFrameCodec.TryRead(ref this, frame, out var frameState) &&
			frameState.Input.Raw == source.Raw && frameState.InputState.IsNull &&
			DosShellNativeBridge.CloseScript(ref Dos, State, source) &&
			DosShellNativeBridge.SetScriptRunnerTemporaryInput(ref Dos, runner,
				replacement, temporaryPath, temporaryPathLength) &&
			ShellScriptFrameCodec.TryReplaceInput(ref this, frame, replacement);
	}

	public bool TryDeleteScriptPath(APTR cli, APTR path, uint pathLength) =>
		cli.IsNotNull && path.IsNotNull && pathLength != 0 &&
		pathLength < uint.MaxValue &&
		path.Raw <= uint.MaxValue - pathLength - 1 &&
		Dos.IsMapped(path, pathLength + 1) &&
		Dos.ReadUInt8(path, unchecked((int)pathLength)) == 0 &&
		DosCore.DeleteFile(ref Dos, State, path) != 0;

	public bool TrySetScriptPromptCapture(APTR cli, APTR path,
		uint pathLength, BPTR input) => cli.IsNotNull && path.IsNotNull &&
		pathLength != 0 && pathLength < ScriptSmallCapacity &&
		Dos.IsMapped(path, pathLength + 1) &&
		Dos.ReadUInt8(path, unchecked((int)pathLength)) == 0 &&
		DosShellNativeBridge.SetScriptRunnerPromptCapture(ref Dos, State,
			cli, path, pathLength, input);

	public bool TryCloseScriptRedirection(APTR cli, BPTR handle) =>
		DosShellNativeBridge.CloseScript(ref Dos, State, handle);

	private void ReleaseScriptGuest(ref APTR address, uint size)
	{
		if (address.IsNotNull)
		{
			Dos.FreeGuest(address, size);
			address = APTR.Null;
		}
	}

	private bool ValidText(APTR address, uint length) =>
		!address.IsNull && length != 0 && length <= 65_535 &&
		address.Raw <= uint.MaxValue - length && Dos.IsMapped(address, length);

	private int CompareText(APTR left, uint leftLength, APTR right,
		uint rightLength)
	{
		var count = leftLength < rightLength ? leftLength : rightLength;
		for (var index = 0u; index < count; index++)
		{
			var a = Fold(ReadUInt8(left, unchecked((int)index)));
			var b = Fold(ReadUInt8(right, unchecked((int)index)));
			if (a != b) return a < b ? -1 : 1;
		}
		return leftLength == rightLength ? 0 : leftLength < rightLength ? -1 : 1;
	}

	private bool TryCompareNumbers(APTR left, uint leftLength, APTR right,
		uint rightLength, out int comparison)
	{
		comparison = 0;
		if (!TryReadNumber(left, leftLength, out var leftValue) ||
			!TryReadNumber(right, rightLength, out var rightValue))
			return false;
		comparison = leftValue == rightValue ? 0 : leftValue < rightValue ? -1 : 1;
		return true;
	}

	private bool TryReadNumber(APTR address, uint length, out int value)
	{
		value = 0;
		if (!ValidText(address, length)) return false;
		var index = 0u;
		var negative = false;
		var first = ReadUInt8(address);
		if (first is (byte)'+' or (byte)'-')
		{
			negative = first == (byte)'-';
			if (++index == length) return false;
		}
		var magnitude = 0u;
		for (; index < length; index++)
		{
			var digit = ReadUInt8(address, unchecked((int)index));
			if (digit < (byte)'0' || digit > (byte)'9') return false;
			digit = unchecked((byte)(digit - (byte)'0'));
			if (magnitude > (uint.MaxValue - digit) / 10u) return false;
			magnitude = magnitude * 10u + digit;
		}
		if (negative)
		{
			if (magnitude > 0x8000_0000u) return false;
			value = magnitude == 0x8000_0000u ? int.MinValue :
				-unchecked((int)magnitude);
		}
		else
		{
			if (magnitude > 0x7FFF_FFFFu) return false;
			value = unchecked((int)magnitude);
		}
		return true;
	}

	private static byte Fold(byte value) => value is >= (byte)'a' and <=
		(byte)'z' ? unchecked((byte)(value - ((byte)'a' - (byte)'A'))) : value;
}
