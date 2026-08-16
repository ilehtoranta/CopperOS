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
		uint promptLength)
	{
		if (cli.IsNull || input.IsNull || output.IsNull || prompt.IsNull ||
			promptLength == 0 || promptLength > 4095 ||
			prompt.Raw > uint.MaxValue - promptLength ||
			!Dos.IsMapped(prompt, promptLength) ||
			!DosShellNativeBridge.TryGetScriptFrame(ref Dos, State, cli,
				out var frame))
			return false;
		if (DosShellNativeBridge.Write(ref Dos, State, output, prompt,
			promptLength) < 0 || DosShellNativeBridge.WriteByte(ref Dos, State,
			output, (byte)'?') < 0 || DosShellNativeBridge.WriteByte(ref Dos,
			State, output, (byte)' ') < 0)
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
		var first = Dos.ReadUInt8(answer);
		var yes = first is (byte)'Y' or (byte)'y';
		var no = first is (byte)'N' or (byte)'n';
		Dos.FreeGuest(answer, AnswerCapacity);
		if (!yes && !no) return false;
		return ShellScriptFrameCodec.TryRecordCondition(ref this, frame,
			(uint)ShellIfCondition.Value, yes ? 0u : 1u);
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
			var handle = DosCore.Open(ref Dos, State, left, DOS.FileMode.OldFile);
			matched = handle.IsNotNull;
			if (handle.IsNotNull) DosCore.Close(ref Dos, State, handle);
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
		uint fileLength, out int result)
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
		var runnerValue = new DosShellScriptRunnerRecord
		{
			Cli = cli, Frame = frame, Input = input, Line = line,
			CommandName = commandName, Token = token, First = first,
			Second = second, Third = third, Fourth = fourth,
			ErrorCodes = errorCodes, RedirectionCommand = redirectionCommand,
			RedirectionInput = redirectionInput,
			RedirectionOutput = redirectionOutput,
			RedirectionError = redirectionError, AliasLine = aliasLine,
			LookupPath = lookupPath, State = DosShellScriptRunnerState.Running,
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
		var output = cliValue.CurrentOutput.IsNotNull
			? cliValue.CurrentOutput : cliValue.StandardOutput;
		var initial = new ShellScriptFrameState
		{
			Parent = cli, Cli = cli, Input = input, Output = output,
			Error = output, CurrentDirectory = cliValue.CurrentDirectoryName,
			CurrentLine = 1, CurrentOffset = 0,
			FailureLimit = cliValue.FailLevel > 0
				? unchecked((uint)cliValue.FailLevel) : 0,
			LastResult = (int)ShellCommandResult.Ok,
			Flags = ShellScriptFrameFlags.Active,
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
		return new ShellScriptStepWorkspace(stored.Line, ScriptLineCapacity,
			stored.CommandName, ScriptCommandNameCapacity, in commandWorkspace,
			in redirectionWorkspace, in aliasWorkspace, in lookupWorkspace);
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
				ScriptSmallCapacity);
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
		if (value.Input.IsNotNull)
		{
			DosShellNativeBridge.CloseScript(ref Dos, State, value.Input);
			value.Input = BPTR.Null;
		}
	}

	public bool TryRunCommand(APTR cli, BPTR input, BPTR output, BPTR error,
		BPTR currentDirectory, APTR continuation, APTR command, uint commandLength, uint detach,
		uint quiet, uint stack, uint stackPresent, int priority,
		uint priorityPresent)
	{
		var inheritance = new ShellChildInheritance(input, output, error,
			currentDirectory);
		_ = detach;
		_ = quiet;
		if (ExecBase.IsNull || cli.IsNull || command.IsNull || commandLength == 0 ||
			commandLength > 65_535 || command.Raw > uint.MaxValue - commandLength ||
			!Dos.IsMapped(command, commandLength))
			return false;

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
			nameLength, path, 512, out var lookupKind, out var pathLength);
		if (!found || pathLength == 0 || lookupKind ==
			DosShellNativeBridge.LookupKind.Script)
		{
			Dos.FreeGuest(name, nameLength + 1);
			Dos.FreeGuest(path, 512);
			return false;
		}

		var residentEntry = APTR.Null;
		var residentAcquired = false;
		BPTR segment;
		if (lookupKind == DosShellNativeBridge.LookupKind.Resident)
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
		var requestedStack = stackPresent != 0 ? stack : 4096u;
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
		if (priorityPresent != 0)
			WriteTaskTag(ref Dos, tags, 2, ExecConstants.TaskTagPriority,
				unchecked((uint)priority));
		WriteTaskTag(ref Dos, tags, 3, ExecConstants.TaskTagName, name.Raw);
		WriteTaskTag(ref Dos, tags, 4, ExecConstants.TagDone, 0);

		var startup = new DosChildCliStartup(name, nameLength, name, nameLength,
			path, pathLength, APTR.Null, 0);
		var task = DosChildProcessLaunchCore.CreateFromImageWithStartup<
			TDosPlatform, ClassicPolicy>(ref Dos, ExecBase, State, tags, segment,
			continuation, inheritance.Input, inheritance.Output,
			inheritance.CurrentDirectory, in startup);
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
		if (residentAcquired && !DosProcessImageCore.BindResident(ref Dos,
			State, task, residentEntry, segment))
		{
			// Prevent the generic task teardown from treating this shared resident
			// image as a loader-owned segment if metadata publication fails.
			DosProcessCodec.WriteSegmentList(ref Dos, task, BPTR.Null);
			DosProcessLifecycleCore.Terminate<TDosPlatform, ClassicPolicy>(ref Dos,
				ExecBase, State, task);
			DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			return false;
		}
		return true;
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

	public bool TryManageResident(APTR cli, BPTR output, APTR name,
		uint nameLength, APTR file, uint fileLength, APTR alias,
		uint aliasLength, uint remove, uint add, uint replace, uint force,
		uint system, uint defer) => DosShellNativeBridge.ManageResident(
		ref Dos, State, output, name, nameLength, file, fileLength, alias,
		aliasLength, remove, add, replace, force, system, defer);

	public bool TryCreateShell(APTR parentCli, ShellLaunchKind kind,
		BPTR input, BPTR output, BPTR error, BPTR currentDirectory,
		APTR continuation, APTR window,
		uint windowLength, APTR from, uint fromLength) => false;

	public bool TryPollShellContinuation(APTR cli, APTR continuation,
		out ShellProcessContinuationState state, out int result)
	{
		state = ShellProcessContinuationState.Failed;
		result = (int)ShellCommandResult.Error;
		if (!DosShellNativeBridge.PollChildContinuation(ref Dos, State, cli,
			continuation, out var childState, out result))
			return false;
		state = childState switch
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

	public bool TryReleaseShellContinuation(APTR cli, APTR continuation,
		uint ownedFlags)
	{
		if (ExecBase.IsNull || cli.IsNull || continuation.IsNull ||
			!DosChildContinuationCodec.TryRead(ref Dos, continuation,
				out var current) || current.ChildCli.IsNull ||
			(current.Flags & (uint)DosChildContinuationFlags.ResourcesClosed) == 0)
			return false;
		if (ownedFlags != (current.Flags & ~(uint)
			DosChildContinuationFlags.ResourcesClosed)) return false;
		if (!DosCore.TryFindProcessTaskByCli(ref Dos, State, current.ChildCli,
			out var task)) return false;
		var released = DosChildContinuationCore.TryReleaseAfterShellMark<
			TDosPlatform, ClassicPolicy>(ref Dos, ExecBase, State, task,
			continuation);
		if (!released) return false;
		if ((current.Flags & (uint)DosChildContinuationFlags.RecordOwned) != 0 &&
			DosShellNativeBridge.TryGetScriptFrame(ref Dos, State, cli,
				out var frame))
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame,
				continuation, 3);
		return true;
	}

	public bool TryReadArgs(APTR argumentText, uint argumentLength,
		APTR template, uint templateLength, APTR resultArray,
		uint resultBytes, out APTR rdArgs)
	{
		rdArgs = DosShellNativeBridge.ReadArgs(ref Dos, State, argumentText,
			argumentLength, template, resultArray);
		return rdArgs.IsNotNull;
	}

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

	public bool TryWriteWhy(BPTR output, APTR cli) => false;

	public bool TryWriteFault(BPTR output, APTR errorCodes,
		uint errorCount) => false;

	public bool TrySetPrompt(APTR cli, APTR value, uint valueLength,
		uint reset) => DosShellNativeBridge.SetPrompt(ref Dos, State, cli,
		value, valueLength, reset);

	public int Write(BPTR handle, APTR source, uint length) =>
		DosShellNativeBridge.Write(ref Dos, State, handle, source, length);

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
		uint nameLength, APTR path, uint pathCapacity,
		out ShellScriptLookupKind kind, out uint pathLength)
	{
		var result = DosShellNativeBridge.LookupCommand(ref Dos, State, cli,
			name, nameLength, path, pathCapacity, out var dosKind,
			out pathLength);
		kind = dosKind switch
		{
			DosShellNativeBridge.LookupKind.Resident =>
				ShellScriptLookupKind.Resident,
			DosShellNativeBridge.LookupKind.File =>
				ShellScriptLookupKind.ExplicitFile,
			DosShellNativeBridge.LookupKind.Script =>
				ShellScriptLookupKind.Script,
			DosShellNativeBridge.LookupKind.NotFound =>
				ShellScriptLookupKind.NotFound,
			_ => ShellScriptLookupKind.Malformed,
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

	public bool TryExecuteScriptCommand(APTR cli, APTR frame, APTR line,
		uint lineLength, ShellScriptLookupKind lookupKind,
		APTR resolvedPath, uint resolvedPathLength, BPTR input, BPTR output,
		BPTR error, out int result, out APTR continuation)
	{
		result = (int)ShellCommandResult.Error;
		continuation = APTR.Null;
		if (ExecBase.IsNull || cli.IsNull || frame.IsNull || line.IsNull ||
			lineLength == 0 || lineLength > 65_535 ||
			line.Raw > uint.MaxValue - lineLength || !Dos.IsMapped(line, lineLength) ||
			resolvedPath.IsNull || resolvedPathLength == 0 ||
			resolvedPathLength > 65_535 ||
			resolvedPath.Raw > uint.MaxValue - resolvedPathLength ||
			!Dos.IsMapped(resolvedPath, resolvedPathLength) ||
			lookupKind is ShellScriptLookupKind.NotFound or
			ShellScriptLookupKind.Script or ShellScriptLookupKind.Malformed)
			return false;
		var nameLength = FirstTokenLength(ref Dos, line, lineLength);
		if (nameLength == 0) return false;
		var name = Dos.AllocateGuest(nameLength + 1);
		var path = Dos.AllocateGuest(resolvedPathLength + 1);
		if (name.IsNull || path.IsNull || !Dos.IsMapped(name, nameLength + 1) ||
			!Dos.IsMapped(path, resolvedPathLength + 1))
		{
			if (name.IsNotNull) Dos.FreeGuest(name, nameLength + 1);
			if (path.IsNotNull) Dos.FreeGuest(path, resolvedPathLength + 1);
			return false;
		}
		Dos.Copy(line, name, nameLength);
		Dos.WriteUInt8(name, unchecked((int)nameLength), 0);
		Dos.Copy(resolvedPath, path, resolvedPathLength);
		Dos.WriteUInt8(path, unchecked((int)resolvedPathLength), 0);

		var residentEntry = APTR.Null;
		var residentAcquired = false;
		BPTR segment;
		if (lookupKind == ShellScriptLookupKind.Resident)
		{
			if (!DosShellNativeBridge.AcquireResident(ref Dos, State, name,
				nameLength, out segment, out residentEntry))
			{
				Dos.FreeGuest(name, nameLength + 1);
				Dos.FreeGuest(path, resolvedPathLength + 1);
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
			Dos.FreeGuest(path, resolvedPathLength + 1);
			return false;
		}

		var commandStorage = lineLength == uint.MaxValue ? 0u : lineLength + 1;
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
			Dos.FreeGuest(path, resolvedPathLength + 1);
			return false;
		}
		Dos.Copy(line, storedCommand, lineLength);
		Dos.WriteUInt8(storedCommand, unchecked((int)lineLength), 0);
		var initialContinuation = new DosChildContinuationRecord
		{
			ParentCli = cli,
			Command = storedCommand,
			CommandLength = lineLength,
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
			Dos.FreeGuest(path, resolvedPathLength + 1);
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
			Dos.FreeGuest(path, resolvedPathLength + 1);
			return false;
		}
		Dos.Clear(tags, TagItem.Size * 5);
		WriteTaskTag(ref Dos, tags, 0, ExecConstants.TaskTagProgramCounter,
			image.EntryPoint.Raw);
		WriteTaskTag(ref Dos, tags, 1, ExecConstants.TaskTagM68kStackSize, 4096);
		WriteTaskTag(ref Dos, tags, 2, ExecConstants.TaskTagName, name.Raw);
		WriteTaskTag(ref Dos, tags, 3, ExecConstants.TagDone, 0);
		// Keep a deterministic fifth slot for future foreground priority policy.
		WriteTaskTag(ref Dos, tags, 4, ExecConstants.TagDone, 0);
		var startup = new DosChildCliStartup(name, nameLength, name, nameLength,
			path, resolvedPathLength, APTR.Null, 0);
		var task = DosChildProcessLaunchCore.CreateFromImageWithStartup<
			TDosPlatform, ClassicPolicy>(ref Dos, ExecBase, State, tags, segment,
			record, input, output, DosCommandLineInterfaceCodec.Read(ref Dos,
				cli).CurrentDirectoryName, in startup);
		Dos.FreeGuest(tags, TagItem.Size * 5);
		Dos.FreeGuest(name, nameLength + 1);
		Dos.FreeGuest(path, resolvedPathLength + 1);
		if (task.IsNull)
		{
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, record, 3);
			if (residentAcquired)
				DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
			else
				DosSegmentLoaderCore.Unload(ref Dos, State, segment);
			return false;
		}

		// The child receives the argument tail from DOS-owned continuation
		// storage, not from the caller's reusable line buffer.
		var argumentStart = nameLength;
		while (argumentStart < lineLength &&
			Dos.ReadUInt8(storedCommand, unchecked((int)argumentStart)) is
				(byte)' ' or (byte)'\t') argumentStart++;
		DosProcessCodec.WriteArguments(ref Dos, task,
			argumentStart < lineLength
				? APTR.FromPointer(storedCommand.Raw + argumentStart)
				: APTR.Null);
		if (residentAcquired && !DosProcessImageCore.BindResident(ref Dos,
			State, task, residentEntry, segment))
		{
			DosProcessCodec.WriteSegmentList(ref Dos, task, BPTR.Null);
			DosProcessLifecycleCore.Terminate<TDosPlatform, ClassicPolicy>(ref Dos,
				ExecBase, State, task);
			DosShellNativeBridge.FreeScriptRecord(ref Dos, State, frame, record, 3);
			DosShellNativeBridge.ReleaseResident(ref Dos, State, residentEntry);
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
