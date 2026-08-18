using Amiga;
using CopperStart.Dos;
using CopperStart.Exec;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Four-byte native Shell context.  The value carries only the DOS-state
/// pointer; ExecBase is stored in the reserved tail word of that DOS record.
/// This keeps calls into the generic Shell engine within CopperSharp's shared
/// single-slot representation while all mutable state remains DOS-owned.
/// </summary>
public struct DosShellNativePlatform : IShellPlatform, IShellScriptPlatform
{
	public DosShellNativePlatform(APTR state) => State = state;

	public APTR State;

	private CopperSharpNativeDosPlatform CreateDos()
	{
		var dos = default(CopperSharpNativeDosPlatform);
		dos.SetExecBase(DosShellNativeContextCore.ReadExecBase(ref dos, State));
		return dos;
	}

	private const uint ScriptLineCapacity = 4096;
	private const uint ScriptCommandNameCapacity = 256;
	private const uint ScriptTokenCapacity = 4096;
	private const uint ScriptSmallCapacity = 512;
	private const uint ScriptErrorCodeCapacity = FaultCommand.MaximumErrorCodes * 4;
	private const uint ScriptMaximumSteps = 4096;

	public byte ReadUInt8(APTR address, int offset = 0)
	{
		var dos = CreateDos();
		return dos.ReadUInt8(address, offset);
	}

	public ushort ReadUInt16(APTR address, int offset = 0)
	{
		var dos = CreateDos();
		return dos.ReadUInt16(address, offset);
	}

	public uint ReadUInt32(APTR address, int offset = 0)
	{
		var dos = CreateDos();
		return dos.ReadUInt32(address, offset);
	}

	public void WriteUInt8(APTR address, int offset, byte value)
	{
		var dos = CreateDos();
		dos.WriteUInt8(address, offset, value);
	}

	public void WriteUInt16(APTR address, int offset, ushort value)
	{
		var dos = CreateDos();
		dos.WriteUInt16(address, offset, value);
	}

	public void WriteUInt32(APTR address, int offset, uint value)
	{
		var dos = CreateDos();
		dos.WriteUInt32(address, offset, value);
	}

	public void Clear(APTR address, uint byteCount)
	{
		var dos = CreateDos();
		dos.Clear(address, byteCount);
	}

	public void Copy(APTR source, APTR destination, uint byteCount)
	{
		var dos = CreateDos();
		dos.Copy(source, destination, byteCount);
	}

	public bool IsMapped(APTR address, uint byteSize)
	{
		var dos = CreateDos();
		return dos.IsMapped(address, byteSize);
	}

	public bool TryReadCliDefaultStack(APTR cli, out int stackBytes)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.ReadCliDefaultStack(ref dos, State, cli,
			out stackBytes);
	}

	public bool TryWriteCliDefaultStack(APTR cli, int stackBytes)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.WriteCliDefaultStack(ref dos, State, cli,
			stackBytes);
	}

	public bool TryWriteCliFailureLimit(APTR cli, uint failureLimit)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.WriteCliFailureLimit(ref dos, State, cli,
			failureLimit);
	}

	public bool TryGetCurrentDirectory(APTR cli, APTR path,
		uint pathCapacity, out uint pathLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.GetCurrentDirectory(ref dos, State, cli,
			path, pathCapacity, out pathLength);
	}

	public bool TryChangeCurrentDirectory(APTR cli, APTR path,
		uint pathLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.ChangeCurrentDirectory(ref dos, State, cli,
			path, pathLength);
	}

	public bool TrySetAlias(APTR cli, APTR name, uint nameLength,
		APTR replacement, uint replacementLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.SetAlias(ref dos, State, cli, name, nameLength,
			replacement, replacementLength);
	}

	public bool TryRemoveAlias(APTR cli, APTR name, uint nameLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.RemoveAlias(ref dos, State, cli, name,
			nameLength);
	}

	public bool TryWriteAliases(BPTR output, APTR cli)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.WriteAliases(ref dos, State, output, cli);
	}

	public bool TryUpdateCommandPath(APTR cli, APTR pathBuffer,
		uint pathBytes, uint pathCount, uint operation, uint quiet)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.UpdateCommandPath(ref dos, State, cli,
			pathBuffer, pathBytes, pathCount, operation, quiet);
	}

	public bool TryWriteCommandPath(BPTR output, APTR cli, uint quiet)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.WriteCommandPath(ref dos, State, output, cli,
			quiet);
	}

	public bool TryBindScriptFrame(APTR cli, APTR frame)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.BindScriptFrame(ref dos, State, cli, frame);
	}

	public bool TryUnbindScriptFrame(APTR cli, APTR frame)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.UnbindScriptFrame(ref dos, State, cli, frame);
	}

	public bool TryRequestShellControl(APTR cli, ShellControlAction action,
		int returnCode)
	{
		var dos = CreateDos();
		if (!DosShellNativeBridge.TryGetScriptFrame(ref dos, State, cli,
			out var frame)) return false;
		if (action == ShellControlAction.Else)
			return ShellScriptControlTransitions.TryElse(ref this, frame);
		if (action is ShellControlAction.EndIf or ShellControlAction.EndSkip)
		{
			var expected = action == ShellControlAction.EndIf
				? ShellScriptBlockKind.If : ShellScriptBlockKind.Skip;
			if (!ShellScriptControlTransitions.TryClose(ref this, frame,
				expected, out var closed)) return false;
			var recorded = ShellScriptFrameCodec.TryRecordControl(ref this,
				frame, action, returnCode);
			var released = DosShellNativeBridge.FreeScriptRecord(ref dos, State,
				frame, closed, 1);
			return recorded && released;
		}
		if (action is not (ShellControlAction.EndCli or
			ShellControlAction.EndShell or ShellControlAction.Quit)) return false;
		return ShellScriptFrameCodec.TryRecordControl(ref this, frame, action,
			returnCode);
	}

	public bool TryDefineScriptLabel(APTR cli, APTR label,
		uint labelLength)
	{
		var dos = CreateDos();
		if (!DosShellNativeBridge.TryGetScriptFrame(ref dos, State, cli,
			out var frame) || !ShellScriptFrameCodec.TryRead(ref this, frame,
			out var frameState)) return false;
		if (label.IsNull || labelLength == 0 || labelLength > 65_535 ||
			label.Raw > uint.MaxValue - labelLength || !dos.IsMapped(label,
			labelLength)) return false;
		var record = DosShellNativeBridge.AllocateScriptRecord(ref dos, State,
			frame, ShellScriptLabelCodec.Size, 2, labelLength,
			out var storedName);
		if (record.IsNull || storedName.IsNull) return false;
		dos.Copy(label, storedName, labelLength);
		if (ShellScriptLabelTransitions.TryDefine(ref this, frame, record,
			storedName, labelLength, frameState.CurrentLine,
			frameState.CurrentOffset)) return true;
		DosShellNativeBridge.FreeScriptRecord(ref dos, State, frame, record, 2);
		return false;
	}

	public bool TrySkipToLabel(APTR cli, APTR label, uint labelLength,
		uint back)
	{
		var dos = CreateDos();
		if (!DosShellNativeBridge.TryGetScriptFrame(ref dos, State, cli,
			out var frame)) return false;
		return ShellScriptLabelTransitions.TrySkip(ref this, frame, label,
			labelLength, back);
	}

	public bool TryAsk(APTR cli, BPTR input, BPTR output, APTR prompt,
		uint promptLength)
	{
		var dos = CreateDos();
		if (cli.IsNull || input.IsNull || output.IsNull || prompt.IsNull ||
			promptLength == 0 || promptLength > 4095 ||
			prompt.Raw > uint.MaxValue - promptLength ||
			!dos.IsMapped(prompt, promptLength) ||
			!DosShellNativeBridge.TryGetScriptFrame(ref dos, State, cli,
				out var frame)) return false;
		if (DosShellNativeBridge.Write(ref dos, State, output, prompt,
			promptLength) < 0 || DosShellNativeBridge.WriteByte(ref dos, State,
			output, (byte)'?') < 0 || DosShellNativeBridge.WriteByte(ref dos,
			State, output, (byte)' ') < 0) return false;
		var answer = dos.AllocateGuest(256);
		if (answer.IsNull || !dos.IsMapped(answer, 256))
		{
			if (answer.IsNotNull) dos.FreeGuest(answer, 256);
			return false;
		}
		var read = DosCore.FGets(ref dos, State, input, answer, 256);
		var yes = read.IsNotNull && dos.ReadUInt8(answer, 0) is (byte)'Y' or
			(byte)'y';
		var no = read.IsNotNull && dos.ReadUInt8(answer, 0) is (byte)'N' or
			(byte)'n';
		dos.FreeGuest(answer, 256);
		if (!yes && !no) return false;
		return ShellScriptFrameCodec.TrySetCondition(ref this, frame,
			yes ? 1u : 0u);
	}

	public bool TryEvaluateIf(APTR cli, uint condition, uint threshold,
		uint negate, uint noRequester, uint numeric, APTR left,
		uint leftLength, APTR right, uint rightLength)
	{
		var dos = CreateDos();
		if (negate > 1 || noRequester > 1 || numeric > 1 ||
			!DosShellNativeBridge.TryGetScriptFrame(ref dos, State, cli,
				out var frame) || condition < (uint)ShellIfCondition.PreviousResult ||
			condition > (uint)ShellIfCondition.Exists) return false;
		if (condition == (uint)ShellIfCondition.PreviousResult &&
			(left.IsNotNull || right.IsNotNull || numeric != 0)) return false;
		if (condition is (uint)ShellIfCondition.Equal or
			(uint)ShellIfCondition.Greater or
			(uint)ShellIfCondition.GreaterEqual)
		{
			if (!ValidText(left, leftLength) || !ValidText(right, rightLength))
				return false;
		}
		else if (condition == (uint)ShellIfCondition.Exists)
		{
			if (!ValidText(left, leftLength) || right.IsNotNull || numeric != 0)
				return false;
		}
		var matched = false;
		if (condition == (uint)ShellIfCondition.PreviousResult)
		{
			if (!ShellScriptFrameCodec.TryRead(ref this, frame,
				out var previous)) return false;
			matched = previous.LastResult >= unchecked((int)threshold);
		}
		else if (condition == (uint)ShellIfCondition.Exists)
		{
			var handle = DosCore.Open(ref dos, State, left, DOS.FileMode.OldFile);
			matched = handle.IsNotNull;
			if (handle.IsNotNull) DosCore.Close(ref dos, State, handle);
		}
		else
		{
			var comparison = CompareText(left, leftLength, right, rightLength);
			if (numeric != 0 && !TryCompareNumbers(left, leftLength, right,
				rightLength, out comparison)) return false;
			matched = condition == (uint)ShellIfCondition.Equal
				? comparison == 0
				: condition == (uint)ShellIfCondition.Greater
					? comparison > 0 : comparison >= 0;
		}
		if (negate != 0) matched = !matched;
		if (!ShellScriptFrameCodec.TryRead(ref this, frame,
			out var frameState)) return false;
		var control = DosShellNativeBridge.AllocateScriptRecord(ref dos, State,
			frame, ShellScriptControlCodec.Size, 1);
		if (control.IsNull) return false;
		if (!ShellScriptControlTransitions.TryOpen(ref this, frame, control,
			ShellScriptBlockKind.If, frameState.CurrentLine,
			frameState.CurrentOffset, matched ? 0u : 1u))
		{
			DosShellNativeBridge.FreeScriptRecord(ref dos, State, frame, control,
				1);
			return false;
		}
		if (ShellScriptFrameCodec.TrySetCondition(ref this, frame, condition))
			return true;
		if (ShellScriptControlTransitions.TryClose(ref this, frame,
			ShellScriptBlockKind.If, out var closed))
			DosShellNativeBridge.FreeScriptRecord(ref dos, State, frame, closed,
				1);
		return false;
	}

	public ShellScriptExecutionStatus TryExecuteScript(APTR cli, APTR file,
		uint fileLength, out int result)
	{
		var dos = CreateDos();
		result = (int)ShellCommandResult.Error;
		if (cli.IsNull || file.IsNull || fileLength == 0 || fileLength > 65_535 ||
			file.Raw > uint.MaxValue - fileLength - 1 ||
			!dos.IsMapped(file, fileLength + 1) ||
			!DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
			return ShellScriptExecutionStatus.Failed;
		var existing = DosShellNativeBridge.FindScriptRunner(ref dos, State, cli);
		if (existing.IsNotNull) return RunScriptRunner(existing, out result);
		var input = DosShellNativeBridge.OpenScript(ref dos, State, file,
			fileLength);
		if (input.IsNull) return ShellScriptExecutionStatus.Failed;
		return StartScriptRunner(cli, input, 1, out result);
	}

	/// <summary>
	/// Starts a shell reader on the CLI's inherited input stream.  The stream is
	/// borrowed from the child Process and therefore remains open when the
	/// runner reaches EOF or fails.
	/// </summary>
	public ShellScriptExecutionStatus TryStartInteractiveScript(APTR cli,
		out int result)
	{
		var dos = CreateDos();
		result = (int)ShellCommandResult.Error;
		if (cli.IsNull || !DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
			return ShellScriptExecutionStatus.Failed;
		var cliValue = DosCommandLineInterfaceCodec.Read(ref dos, cli);
		var input = cliValue.CurrentInput.IsNotNull ? cliValue.CurrentInput :
			cliValue.StandardInput;
		if (input.IsNull) return ShellScriptExecutionStatus.Failed;
		return StartScriptRunner(cli, input, 0, out result);
	}

	private ShellScriptExecutionStatus StartScriptRunner(APTR cli, BPTR input,
		uint inputOwned, out int result)
	{
		var dos = CreateDos();
		result = (int)ShellCommandResult.Error;
		var frame = dos.AllocateGuest(ShellScriptFrameCodec.Size);
		var line = dos.AllocateGuest(ScriptLineCapacity);
		var commandName = dos.AllocateGuest(ScriptCommandNameCapacity);
		var token = dos.AllocateGuest(ScriptTokenCapacity);
		var first = dos.AllocateGuest(ScriptSmallCapacity);
		var second = dos.AllocateGuest(ScriptSmallCapacity);
		var third = dos.AllocateGuest(ScriptSmallCapacity);
		var fourth = dos.AllocateGuest(ScriptSmallCapacity);
		var errorCodes = dos.AllocateGuest(ScriptErrorCodeCapacity);
		var redirectionCommand = dos.AllocateGuest(ScriptLineCapacity);
		var redirectionInput = dos.AllocateGuest(ScriptSmallCapacity);
		var redirectionOutput = dos.AllocateGuest(ScriptSmallCapacity);
		var redirectionError = dos.AllocateGuest(ScriptSmallCapacity);
		var aliasLine = dos.AllocateGuest(ScriptLineCapacity);
		var lookupPath = dos.AllocateGuest(ScriptSmallCapacity);
		var runnerValue = new DosShellScriptRunnerRecord
		{
			Cli = cli, Frame = frame, Input = input, Line = line,
			InputOwned = inputOwned,
			CommandName = commandName, Token = token, First = first,
			Second = second, Third = third, Fourth = fourth,
			ErrorCodes = errorCodes, RedirectionCommand = redirectionCommand,
			RedirectionInput = redirectionInput,
			RedirectionOutput = redirectionOutput,
			RedirectionError = redirectionError, AliasLine = aliasLine,
			LookupPath = lookupPath, State = DosShellScriptRunnerState.Running,
		};
		if (!ValidScriptRunnerBuffers(ref dos, in runnerValue))
		{
			CleanupUnpublishedScript(ref dos, ref runnerValue);
			return ShellScriptExecutionStatus.Failed;
		}
		var runner = DosShellNativeBridge.AllocateScriptRunner(ref dos, State,
			frame, in runnerValue);
		if (runner.IsNull)
		{
			CleanupUnpublishedScript(ref dos, ref runnerValue);
			return ShellScriptExecutionStatus.Failed;
		}
		var cliValue = DosCommandLineInterfaceCodec.Read(ref dos, cli);
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
			!DosShellNativeBridge.BindScriptFrame(ref dos, State, cli, frame))
		{
			DosShellNativeBridge.FreeScriptRunner(ref dos, State, runner);
			return ShellScriptExecutionStatus.Failed;
		}
		return RunScriptRunner(runner, out result);
	}

	public bool TryPollScriptExecution(APTR cli,
		out ShellScriptExecutionStatus status, out int result)
	{
		var dos = CreateDos();
		status = ShellScriptExecutionStatus.Failed;
		result = (int)ShellCommandResult.Error;
		if (cli.IsNull) return false;
		var runner = DosShellNativeBridge.FindScriptRunner(ref dos, State, cli);
		if (runner.IsNull) return false;
		status = RunScriptRunner(runner, out result);
		return true;
	}

	public bool TryPrepareScriptWait(APTR cli)
	{
		var dos = CreateDos();
		if (cli.IsNull || dos.CurrentDosTask.IsNull) return false;
		var runner = DosShellNativeBridge.FindScriptRunner(ref dos, State, cli);
		if (runner.IsNull || !DosShellNativeBridge.ReadScriptRunner(ref dos,
			State, runner, out var stored) ||
			!ShellScriptFrameCodec.TryRead(ref this, stored.Frame,
				out var frameState) || frameState.PendingCommand.IsNull) return false;
		var wait = DosShellNativeBridge.FindForegroundWaitByFrame(ref dos, State,
			stored.Frame);
		if (wait.IsNotNull && DosShellNativeBridge.ReadForegroundWait(ref dos,
			State, wait, out _)) return true;
		wait = DosShellNativeBridge.AllocateForegroundWait(ref dos, State,
			stored.Frame, cli, dos.CurrentDosTask, frameState.PendingCommand,
			frameState.PendingNextLine, frameState.PendingNextOffset);
		if (wait.IsNull) return false;
		var cookie = dos.PrepareDosWait(dos.CurrentDosTask);
		if (cookie == 0)
		{
			DosShellNativeBridge.FreeForegroundWait(ref dos, State, wait);
			return false;
		}
		var token = DosShellNativeBridge.ReadForegroundWaitToken(ref dos, State) + 1;
		if (token == 0) token = 1;
		DosShellNativeBridge.WriteForegroundWaitToken(ref dos, State, token);
		if (DosShellNativeBridge.SetPreparedForegroundWait(ref dos, State, wait,
			cookie, token)) return true;
		dos.CancelPreparedDosWait(dos.CurrentDosTask, cookie);
		DosShellNativeBridge.FreeForegroundWait(ref dos, State, wait);
		return false;
	}

	public bool TryParkScriptWait(APTR cli, uint timeoutTicks)
	{
		var dos = CreateDos();
		if (cli.IsNull) return false;
		var runner = DosShellNativeBridge.FindScriptRunner(ref dos, State, cli);
		if (runner.IsNull || !DosShellNativeBridge.ReadScriptRunner(ref dos,
			State, runner, out var stored)) return false;
		var wait = DosShellNativeBridge.FindForegroundWaitByFrame(ref dos, State,
			stored.Frame);
		return wait.IsNotNull && DosShellNativeBridge.ParkForegroundWait(
			ref dos, State, wait, timeoutTicks);
	}

	private ShellScriptExecutionStatus RunScriptRunner(APTR runner,
		out int result)
	{
		var dos = CreateDos();
		result = (int)ShellCommandResult.Error;
		if (!DosShellNativeBridge.ReadScriptRunner(ref dos, State, runner,
			out var stored)) return ShellScriptExecutionStatus.Failed;
		if (!ShellScriptFrameCodec.TryRead(ref this, stored.Frame,
			out _))
		{
			DosShellNativeBridge.FreeScriptRunner(ref dos, State, runner);
			DosShellNativeBridge.UnbindScriptFrame(ref dos, State, stored.Cli,
				stored.Frame);
			return ShellScriptExecutionStatus.Failed;
		}
		var workspace = BuildScriptWorkspace(in stored);
		var run = ShellScriptEngine.Run(ref this, stored.Frame, in workspace,
			ScriptMaximumSteps);
		result = run.Result;
		if (run.Status == ShellScriptStepStatus.Waiting)
		{
			DosShellNativeBridge.SetScriptRunnerState(ref dos, runner,
				DosShellScriptRunnerState.Pending, run.Result, run.Steps);
			if (dos.CurrentDosTask.IsNotNull) TryPrepareScriptWait(stored.Cli);
			return ShellScriptExecutionStatus.Pending;
		}
		var terminal = run.Status == ShellScriptStepStatus.EndOfFile;
		DosShellNativeBridge.SetScriptRunnerState(ref dos, runner,
			terminal ? DosShellScriptRunnerState.Completed :
			DosShellScriptRunnerState.Failed, run.Result, run.Steps);
		var wait = DosShellNativeBridge.FindForegroundWaitByFrame(ref dos, State,
			stored.Frame);
		if (wait.IsNotNull)
			DosShellNativeBridge.FreeForegroundWait(ref dos, State, wait);
		DosShellNativeBridge.FreeScriptRunner(ref dos, State, runner);
		DosShellNativeBridge.UnbindScriptFrame(ref dos, State, stored.Cli,
			stored.Frame);
		return terminal ? ShellScriptExecutionStatus.Completed :
			ShellScriptExecutionStatus.Failed;
	}

	private ShellScriptStepWorkspace BuildScriptWorkspace(
		in DosShellScriptRunnerRecord stored)
	{
		var commandWorkspace = default(ShellCommandWorkspace);
		commandWorkspace.Token = stored.Token;
		commandWorkspace.TokenCapacity = ScriptTokenCapacity;
		commandWorkspace.First = stored.First;
		commandWorkspace.FirstCapacity = ScriptSmallCapacity;
		commandWorkspace.Second = stored.Second;
		commandWorkspace.SecondCapacity = ScriptSmallCapacity;
		commandWorkspace.Third = stored.Third;
		commandWorkspace.ThirdCapacity = ScriptSmallCapacity;
		commandWorkspace.Fourth = stored.Fourth;
		commandWorkspace.FourthCapacity = ScriptSmallCapacity;
		commandWorkspace.ErrorCodes = stored.ErrorCodes;
		commandWorkspace.ErrorCodeCapacity = ScriptErrorCodeCapacity;
		var redirectionWorkspace = default(ShellRedirectionWorkspace);
		redirectionWorkspace.Command = stored.RedirectionCommand;
		redirectionWorkspace.CommandCapacity = ScriptLineCapacity;
		redirectionWorkspace.InputPath = stored.RedirectionInput;
		redirectionWorkspace.InputCapacity = ScriptSmallCapacity;
		redirectionWorkspace.OutputPath = stored.RedirectionOutput;
		redirectionWorkspace.OutputCapacity = ScriptSmallCapacity;
		redirectionWorkspace.ErrorPath = stored.RedirectionError;
		redirectionWorkspace.ErrorCapacity = ScriptSmallCapacity;
		var aliasWorkspace = default(ShellScriptAliasWorkspace);
		aliasWorkspace.Line = stored.AliasLine;
		aliasWorkspace.Capacity = ScriptLineCapacity;
		var lookupWorkspace = default(ShellScriptLookupWorkspace);
		lookupWorkspace.Path = stored.LookupPath;
		lookupWorkspace.Capacity = ScriptSmallCapacity;
		var workspace = default(ShellScriptStepWorkspace);
		workspace.Line = stored.Line;
		workspace.LineCapacity = ScriptLineCapacity;
		workspace.CommandName = stored.CommandName;
		workspace.CommandNameCapacity = ScriptCommandNameCapacity;
		workspace.CommandWorkspace = commandWorkspace;
		workspace.Redirection = redirectionWorkspace;
		workspace.AliasExpansion = aliasWorkspace;
		workspace.Lookup = lookupWorkspace;
		return workspace;
	}

	private bool ValidScriptRunnerBuffers(ref CopperSharpNativeDosPlatform dos,
		in DosShellScriptRunnerRecord value) => value.Frame.IsNotNull &&
		dos.IsMapped(value.Frame, ShellScriptFrameCodec.Size) &&
		value.Line.IsNotNull && dos.IsMapped(value.Line, ScriptLineCapacity) &&
		value.CommandName.IsNotNull && dos.IsMapped(value.CommandName,
			ScriptCommandNameCapacity) && value.Token.IsNotNull &&
		dos.IsMapped(value.Token, ScriptTokenCapacity) && value.First.IsNotNull &&
		dos.IsMapped(value.First, ScriptSmallCapacity) && value.Second.IsNotNull &&
		dos.IsMapped(value.Second, ScriptSmallCapacity) && value.Third.IsNotNull &&
		dos.IsMapped(value.Third, ScriptSmallCapacity) && value.Fourth.IsNotNull &&
		dos.IsMapped(value.Fourth, ScriptSmallCapacity) &&
		value.ErrorCodes.IsNotNull && dos.IsMapped(value.ErrorCodes,
			ScriptErrorCodeCapacity) && value.RedirectionCommand.IsNotNull &&
		dos.IsMapped(value.RedirectionCommand, ScriptLineCapacity) &&
		value.RedirectionInput.IsNotNull && dos.IsMapped(value.RedirectionInput,
			ScriptSmallCapacity) && value.RedirectionOutput.IsNotNull &&
		dos.IsMapped(value.RedirectionOutput, ScriptSmallCapacity) &&
		value.RedirectionError.IsNotNull && dos.IsMapped(value.RedirectionError,
			ScriptSmallCapacity) && value.AliasLine.IsNotNull &&
		dos.IsMapped(value.AliasLine, ScriptLineCapacity) &&
		value.LookupPath.IsNotNull && dos.IsMapped(value.LookupPath,
			ScriptSmallCapacity);

	private void CleanupUnpublishedScript(ref CopperSharpNativeDosPlatform dos,
		ref DosShellScriptRunnerRecord value)
	{
		ReleaseScriptGuest(ref dos, value.Frame, ShellScriptFrameCodec.Size);
		value.Frame = APTR.Null;
		ReleaseScriptGuest(ref dos, value.Line, ScriptLineCapacity);
		value.Line = APTR.Null;
		ReleaseScriptGuest(ref dos, value.CommandName,
			ScriptCommandNameCapacity);
		value.CommandName = APTR.Null;
		ReleaseScriptGuest(ref dos, value.Token, ScriptTokenCapacity);
		value.Token = APTR.Null;
		ReleaseScriptGuest(ref dos, value.First, ScriptSmallCapacity);
		value.First = APTR.Null;
		ReleaseScriptGuest(ref dos, value.Second, ScriptSmallCapacity);
		value.Second = APTR.Null;
		ReleaseScriptGuest(ref dos, value.Third, ScriptSmallCapacity);
		value.Third = APTR.Null;
		ReleaseScriptGuest(ref dos, value.Fourth, ScriptSmallCapacity);
		value.Fourth = APTR.Null;
		ReleaseScriptGuest(ref dos, value.ErrorCodes, ScriptErrorCodeCapacity);
		value.ErrorCodes = APTR.Null;
		ReleaseScriptGuest(ref dos, value.RedirectionCommand, ScriptLineCapacity);
		value.RedirectionCommand = APTR.Null;
		ReleaseScriptGuest(ref dos, value.RedirectionInput, ScriptSmallCapacity);
		value.RedirectionInput = APTR.Null;
		ReleaseScriptGuest(ref dos, value.RedirectionOutput, ScriptSmallCapacity);
		value.RedirectionOutput = APTR.Null;
		ReleaseScriptGuest(ref dos, value.RedirectionError, ScriptSmallCapacity);
		value.RedirectionError = APTR.Null;
		ReleaseScriptGuest(ref dos, value.AliasLine, ScriptLineCapacity);
		value.AliasLine = APTR.Null;
		ReleaseScriptGuest(ref dos, value.LookupPath, ScriptSmallCapacity);
		value.LookupPath = APTR.Null;
		if (value.Input.IsNotNull)
		{
			DosShellNativeBridge.CloseScript(ref dos, State, value.Input);
			value.Input = BPTR.Null;
		}
	}

	private static void ReleaseScriptGuest(ref CopperSharpNativeDosPlatform dos,
		APTR address, uint size)
	{
		if (address.IsNotNull)
			dos.FreeGuest(address, size);
	}

	public bool TryManageResident(APTR cli, BPTR output, APTR name,
		uint nameLength, APTR file, uint fileLength, APTR alias,
		uint aliasLength, uint remove, uint add, uint replace, uint force,
		uint system, uint defer)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.ManageResident(ref dos, State, output, name,
			nameLength, file, fileLength, alias, aliasLength, remove, add, replace,
			force, system, defer);
	}

	public bool TryRunCommand(APTR cli, BPTR input, BPTR output, BPTR error,
		BPTR currentDirectory, APTR continuation, APTR command, uint commandLength, uint detach,
		uint quiet, uint stack, uint stackPresent, int priority,
		uint priorityPresent)
	{
		var dos = CreateDos();
		return DosShellNativeLaunchCore.TryRunCommand(dos, State,
			DosShellNativeContextCore.ReadExecBase(ref dos, State), cli, input, output, error,
			currentDirectory, continuation, command, commandLength, detach, quiet,
			stack, stackPresent, priority, priorityPresent);
	}

	public bool TryCreateShell(APTR parentCli, ShellLaunchKind kind,
		BPTR input, BPTR output, BPTR error, BPTR currentDirectory,
		APTR continuation, APTR window,
		uint windowLength, APTR from, uint fromLength)
	{
		var dos = CreateDos();
		return DosShellNativeLaunchCore.TryCreateShell(dos, State,
			DosShellNativeContextCore.ReadExecBase(ref dos, State), parentCli,
			kind, input, output, error, currentDirectory, continuation, window,
			windowLength, from, fromLength);
	}

	public bool TryPollShellContinuation(APTR cli, APTR continuation,
		out ShellProcessContinuationState state, out int result)
	{
		var dos = CreateDos();
		state = ShellProcessContinuationState.Failed;
		result = (int)ShellCommandResult.Error;
		if (!DosShellNativeBridge.PollChildContinuation(ref dos, State, cli,
			continuation, out var childState, out result)) return false;
		state = childState switch
		{
			DosChildContinuationState.Pending => ShellProcessContinuationState.Pending,
			DosChildContinuationState.Running => ShellProcessContinuationState.Running,
			DosChildContinuationState.Completed => ShellProcessContinuationState.Completed,
			DosChildContinuationState.Aborted => ShellProcessContinuationState.Aborted,
			DosChildContinuationState.Failed => ShellProcessContinuationState.Failed,
			_ => ShellProcessContinuationState.Failed,
		};
		return true;
	}

	public bool TryReleaseShellContinuation(APTR cli, APTR continuation,
		uint ownedFlags)
	{
		var dos = CreateDos();
		return DosShellNativeLaunchCore.TryReleaseContinuation(dos, State,
			DosShellNativeContextCore.ReadExecBase(ref dos, State), cli, continuation,
			ownedFlags);
	}

	public bool TryExecuteScriptCommand(APTR cli, APTR frame, APTR line,
		uint lineLength, ShellScriptLookupKind lookupKind, APTR resolvedPath,
		uint resolvedPathLength, BPTR input, BPTR output, BPTR error,
		out int result, out APTR continuation)
	{
		var dos = CreateDos();
		return DosShellNativeLaunchCore.TryExecuteScriptCommand(dos, State,
			DosShellNativeContextCore.ReadExecBase(ref dos, State), cli, frame,
			line, lineLength, lookupKind, resolvedPath, resolvedPathLength, input,
			output, error, out result, out continuation);
	}

	public bool TryReadArgs(APTR argumentText, uint argumentLength,
		APTR template, uint templateLength, APTR resultArray, uint resultBytes,
		out APTR rdArgs)
	{
		var dos = CreateDos();
		rdArgs = DosShellNativeBridge.ReadArgs(ref dos, State, argumentText,
			argumentLength, template, resultArray);
		return rdArgs.IsNotNull;
	}

	public void FreeArgs(APTR rdArgs)
	{
		var dos = CreateDos();
		DosShellNativeBridge.FreeArgs(ref dos, State, rdArgs);
	}

	public bool TryGetLocalVariable(APTR cli, APTR name, uint nameLength,
		APTR value, uint valueCapacity, out uint valueLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.GetLocalVariable(ref dos, State, cli, name,
			nameLength, value, valueCapacity, out valueLength);
	}

	public bool TrySetLocalVariable(APTR cli, APTR name, uint nameLength,
		APTR value, uint valueLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.SetLocalVariable(ref dos, State, cli, name,
			nameLength, value, valueLength);
	}

	public bool TryWriteLocalVariables(BPTR output, APTR cli)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.WriteLocalVariables(ref dos, State, output,
			cli);
	}

	public bool TryGetGlobalVariable(APTR name, uint nameLength, APTR value,
		uint valueCapacity, out uint valueLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.GetGlobalVariable(ref dos, State, name,
			nameLength, value, valueCapacity, out valueLength);
	}

	public bool TrySetGlobalVariable(APTR name, uint nameLength, APTR value,
		uint valueLength, uint save)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.SetGlobalVariable(ref dos, State, name,
			nameLength, value, valueLength, save);
	}

	public bool TryWriteGlobalVariables(BPTR output)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.WriteGlobalVariables(ref dos, State, output);
	}

	public bool TryRemoveLocalVariable(APTR cli, APTR name, uint nameLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.RemoveLocalVariable(ref dos, State, cli,
			name, nameLength);
	}

	public bool TryRemoveGlobalVariable(APTR name, uint nameLength, uint save)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.RemoveGlobalVariable(ref dos, State, name,
			nameLength, save);
	}

	public bool ClearConsole(BPTR output, uint reset)
	{
		if (reset > 1 || output.IsNull) return false;
		var dos = CreateDos();
		return DosShellNativeBridge.WriteByte(ref dos, State, output,
			(byte)'\f') >= 0;
	}

	public bool TryWriteWhy(BPTR output, APTR cli)
	{
		_ = output; _ = cli;
		return false;
	}

	public bool TryWriteFault(BPTR output, APTR errorCodes, uint errorCount)
	{
		_ = output; _ = errorCodes; _ = errorCount;
		return false;
	}

	public bool TrySetPrompt(APTR cli, APTR value, uint valueLength,
		uint reset)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.SetPrompt(ref dos, State, cli, value,
			valueLength, reset);
	}

	public int Write(BPTR handle, APTR source, uint length)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.Write(ref dos, State, handle, source, length);
	}

	public int WriteByte(BPTR handle, byte value)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.WriteByte(ref dos, State, handle, value);
	}

	public BPTR OpenOutput(APTR path, uint pathLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.OpenOutput(ref dos, State, path, pathLength,
			0);
	}

	public bool CloseOutput(BPTR handle)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.CloseOutput(ref dos, State, handle);
	}

	public bool TryPollScriptSignal(APTR cli,
		out ShellScriptSignalEvent signal)
	{
		signal = default;
		var dos = CreateDos();
		var received = DosShellNativeBridge.TakeShellSignals(ref dos, State, cli);
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
		_ = cli; _ = signal;
		return true;
	}

	public bool TryExpandScriptAlias(APTR cli, APTR source,
		uint sourceLength, APTR destination, uint destinationCapacity,
		out uint expanded, out uint expandedLength)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.ExpandAlias(ref dos, State, cli, source,
			sourceLength, destination, destinationCapacity, out expanded,
			out expandedLength);
	}

	public bool TryLookupScriptCommand(APTR cli, APTR name,
		uint nameLength, APTR path, uint pathCapacity,
		out ShellScriptLookupKind kind, out uint pathLength)
	{
		var dos = CreateDos();
		var result = DosShellNativeBridge.LookupCommand(ref dos, State, cli,
			name, nameLength, path, pathCapacity, out var dosKind,
			out pathLength);
		kind = dosKind switch
		{
			DosShellNativeBridge.LookupKind.Resident => ShellScriptLookupKind.Resident,
			DosShellNativeBridge.LookupKind.File => ShellScriptLookupKind.ExplicitFile,
			DosShellNativeBridge.LookupKind.Script => ShellScriptLookupKind.Script,
			DosShellNativeBridge.LookupKind.NotFound => ShellScriptLookupKind.NotFound,
			_ => ShellScriptLookupKind.Malformed,
		};
		return result;
	}

	public bool TryReadScriptLine(APTR cli, BPTR input,
		uint currentLine, uint currentOffset, APTR destination,
		uint destinationCapacity, out uint lineLength, out uint nextLine,
		out uint nextOffset, out uint endOfFile)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.ReadScriptLine(ref dos, State, input,
			currentLine, currentOffset, destination, destinationCapacity,
			out lineLength, out nextLine, out nextOffset, out endOfFile);
	}

	public bool TryOpenScriptInput(APTR cli, APTR path, uint pathLength,
		out BPTR handle)
	{
		var dos = CreateDos();
		handle = DosShellNativeBridge.OpenScript(ref dos, State, path,
			pathLength);
		return handle.IsNotNull;
	}

	public bool TryOpenScriptOutput(APTR cli, APTR path, uint pathLength,
		uint append, out BPTR handle)
	{
		var dos = CreateDos();
		handle = DosShellNativeBridge.OpenOutput(ref dos, State, path,
			pathLength, append);
		return handle.IsNotNull;
	}

	public bool TryCloseScriptRedirection(APTR cli, BPTR handle)
	{
		var dos = CreateDos();
		return DosShellNativeBridge.CloseScript(ref dos, State, handle);
	}

	private bool ValidText(APTR address, uint length)
	{
		var dos = CreateDos();
		return !address.IsNull && length != 0 && length <= 65_535 &&
			address.Raw <= uint.MaxValue - length && dos.IsMapped(address, length);
	}

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
		return leftLength == rightLength ? 0 :
			leftLength < rightLength ? -1 : 1;
	}

	private bool TryCompareNumbers(APTR left, uint leftLength, APTR right,
		uint rightLength, out int comparison)
	{
		comparison = 0;
		if (!TryReadNumber(left, leftLength, out var leftValue) ||
			!TryReadNumber(right, rightLength, out var rightValue)) return false;
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

	private static byte Fold(byte value) => value is >= (byte)'a' and <= (byte)'z'
		? unchecked((byte)(value - ((byte)'a' - (byte)'A'))) : value;
}
