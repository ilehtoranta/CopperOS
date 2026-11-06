using Amiga;
using CopperStart.Dos;
using CopperStart.Exec;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Public Exec/DOS services and Shell-owned records. The reserved native slot
/// fits CopperSharp's single-slot generic value transport; it holds no DOS
/// context. The constructor argument is retained for source/entry ABI callers
/// and is never dereferenced or retained.
/// </summary>
public partial struct DosShellNativePlatform : IShellPlatform, IShellScriptPlatform,
	IShellRunnerCleanupPlatform, IShellRunnerBindingPlatform, IShellLocalVariableListPlatform
{
	public DosShellNativePlatform(APTR state) { _ = state; Reserved = 0; }
	public readonly uint Reserved;
	public bool TryFlushShellOutput(BPTR output)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var result = PublicShellFileServices.Flush(library, output);
		global::Amiga.Exec.CloseLibrary(library);
		return result != 0;
	}
	public bool TryFormatShellError(int error, APTR buffer, uint capacity, out uint length)
	{
		length = 0;
		if (buffer.IsNull || capacity < 2 || capacity > int.MaxValue ||
			!IsMapped(buffer, capacity)) return false;
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var savedError = PublicShellFileServices.GetError(library);
		WriteUInt8(buffer, 0, 0);
		_ = PublicShellFaultVectors.Fault(library, error, APTR.Null, buffer, (int)capacity);
		while (length < capacity && ReadUInt8(buffer, (int)length) != 0) length++;
		PublicShellFileServices.SetError(library, (DOS.Error)savedError);
		global::Amiga.Exec.CloseLibrary(library);
		return length != 0 && length < capacity;
	}
	public APTR AllocateRunnerMemory(uint size) => APTR.FromPointer(
		global::Amiga.Exec.AllocMem(size,
			global::Amiga.Exec.MemoryFlags.Public | global::Amiga.Exec.MemoryFlags.Clear));
	public void FreeRunnerMemory(APTR address, uint size)
	{
		if (address.IsNotNull && size != 0) global::Amiga.Exec.FreeMem(address.Raw, size);
	}
	public APTR CurrentRunnerProcess() => global::Amiga.Exec.FindTask(CString.FromPointer(0));
	public APTR CurrentVariableProcess() => CurrentRunnerProcess();
	public void SetVariableListError(DOS.Error error) => _ = SetShellError(error);
	internal uint CurrentShellNumber(APTR cli)
	{
		var dos = CreateDos();
		var task = CurrentRunnerProcess();
		if (cli.IsNull || task.IsNull || !DosProcessCodec.IsMapped(ref dos, task) ||
			DosProcessCodec.ReadCommandLineInterface(ref dos, task).Address != cli) return 0;
		var number = DosProcessCodec.ReadTaskNumber(ref dos, task);
		return number > 0 ? unchecked((uint)number) : 0;
	}
	internal bool TryCurrentScriptProcess(APTR cli, out BPTR directory, out BPTR error)
	{
		directory = BPTR.Null; error = BPTR.Null;
		var dos = CreateDos();
		var process = CurrentRunnerProcess();
		if (cli.IsNull || process.IsNull || !DosProcessCodec.IsMapped(ref dos, process) ||
			DosProcessCodec.ReadCommandLineInterface(ref dos, process).Address != cli) return false;
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var savedError = PublicShellFileServices.GetError(library);
		// CurrentDir exchanges a borrowed lock. Restore it immediately; neither
		// lock is duplicated or released, including the valid zero root lock.
		directory = PublicShellPathVectors.CurrentDir(library, BPTR.Null);
		_ = PublicShellPathVectors.CurrentDir(library, directory);
		error = DosProcessCodec.ReadCurrentError(ref dos, process);
		if (error.IsNull) error = PublicShellFileServices.Output(library);
		PublicShellFileServices.SetError(library, (DOS.Error)savedError);
		global::Amiga.Exec.CloseLibrary(library);
		return true;
	}
	public bool IsRunnerBindingMemory(APTR address, uint size)
	{
		if (address.IsNull || address.Raw > uint.MaxValue - size) return false;
		for (var offset = 0u; offset < size; offset += 4)
			if (global::Amiga.Exec.TypeOfMem(APTR.FromPointer(address.Raw + offset)) == 0) return false;
		return true;
	}
	public void CloseRunnerInput(BPTR input)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return;
		_ = PublicShellFileServices.Close(library, input);
		global::Amiga.Exec.CloseLibrary(library);
	}
	public void DeleteRunnerPath(APTR path)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return;
		_ = PublicShellFileServices.Delete(library, path);
		global::Amiga.Exec.CloseLibrary(library);
	}
	internal APTR FindRunner(APTR cli)
	{
		var dos = CreateDos();
		return ShellRunnerBinding.TryFind(ref this, cli, out _, out var frame, out var runner) &&
			runner.IsNotNull && ShellScriptRunnerCodec.TryRead(ref dos, runner, out var value) &&
			value.Cli == cli && value.Frame == frame ? runner : APTR.Null;
	}
	internal bool TryFindFrame(APTR cli, out APTR frame) =>
		ShellRunnerBinding.TryFind(ref this, cli, out var binding, out frame, out _) && binding.IsNotNull;
	private bool ReleaseRunner(APTR runner)
	{
		var dos = CreateDos();
		if (!ShellScriptRunnerCodec.TryRead(ref dos, runner, out var stored) ||
			!ShellRunnerCleanup.CanRelease(ref this, runner, true) ||
			!ShellRunnerBinding.TryRemove(ref this, stored.Cli, stored.Frame, runner) ||
			!ShellRunnerCleanup.Release(ref this, runner, true)) return false;
		ShellRunnerArena.Free(ref this, runner, true);
		return true;
	}
	private bool ReleaseUnpublishedRunner(APTR runner)
	{
		var dos = CreateDos();
		if (!ShellScriptRunnerCodec.TryRead(ref dos, runner, out _) ||
			!ShellRunnerCleanup.Release(ref this, runner, true)) return false;
		ShellRunnerArena.Free(ref this, runner, true);
		return true;
	}
	private bool ReleaseBoundRunner(APTR runner, APTR cli, APTR frame)
	{
		return ShellRunnerCleanup.CanRelease(ref this, runner, true) &&
			ShellFrameResources.CanRelease(ref this, frame) &&
			ReleaseFrameContinuations(frame) &&
			ShellFrameResources.Release(ref this, frame) &&
			ReleaseRunner(runner);
	}
	private bool ReleaseFrameContinuations(APTR frame)
	{
		if (!ShellScriptFrameCodec.TryRead(ref this, frame, out var value)) return false;
		if (value.PendingCommand.IsNotNull)
		{
			if (!PublicShellContinuationCore.Poll(ref this, value.Cli, value.PendingCommand,
				out var publicOwner, out _, out _, out _)) return false;
			if (publicOwner)
			{
				if (!PublicShellContinuationCore.Detach(ref this, value.Cli, value.PendingCommand) ||
					!ShellScriptFrameCodec.TrySetPendingCommand(ref this, frame, APTR.Null, 0, 0)) return false;
			}
			else return false;
		}
		return PublicShellContinuationCore.DetachFrame(ref this, value.Cli, frame);
	}
	/// <summary>Aborts the current Shell-owned runner and releases its resources.</summary>
	public bool AbortScriptExecution(APTR cli)
	{
		var dos = CreateDos();
		var runner = FindRunner(cli);
		return runner.IsNotNull && ShellScriptRunnerCodec.TryRead(ref dos, runner, out var stored) &&
			ReleaseBoundRunner(runner, cli, stored.Frame);
	}
	private CopperSharpNativeDosPlatform CreateDos()
		=> new();

	private const uint ScriptLineCapacity = 4096;
	private const uint ScriptCommandNameCapacity = 256;
	private const uint ScriptTokenCapacity = 4096;
	private const uint ScriptSmallCapacity = 512;
	private const uint ScriptPromptCapacity = ShellScriptRunnerCodec.PromptTemplateBytes;
	private const uint ScriptErrorCodeCapacity = FaultCommand.MaximumErrorCodes * 4;
	private const uint ScriptMaximumSteps = 1;

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
		var success = ShellCliState.TryReadDefaultStack(ref dos, cli, out stackBytes);
		_ = SetShellError(success ? DOS.Error.None : DOS.Error.ObjectWrongType);
		return success;
	}

	public bool TryReadCliFailureLimit(APTR cli, out uint failureLimit)
	{
		var dos = CreateDos();
		var success = ShellCliState.TryReadFailureLimit(ref dos, cli, out failureLimit);
		_ = SetShellError(success ? DOS.Error.None : DOS.Error.ObjectWrongType);
		return success;
	}

	public bool TryWriteCliDefaultStack(APTR cli, int stackBytes)
	{
		var dos = CreateDos();
		var success = ShellCliState.TryWriteDefaultStack(ref dos, cli, stackBytes);
		_ = SetShellError(success ? DOS.Error.None : DOS.Error.ObjectWrongType);
		return success;
	}

	public bool TryWriteCliFailureLimit(APTR cli, uint failureLimit)
	{
		var dos = CreateDos();
		var success = ShellCliState.TryWriteFailureLimit(ref dos, cli, failureLimit);
		_ = SetShellError(success ? DOS.Error.None : DOS.Error.ObjectWrongType);
		return success;
	}

	public bool TryGetCurrentDirectory(APTR cli, APTR path,
		uint pathCapacity, out uint pathLength)
	{
		pathLength = 0;
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull)
		{
			_ = SetShellError(DOS.Error.ObjectNotFound);
			return false;
		}
		var platform = new PublicShellPathPlatform(library);
		var result = platform.TryGetCurrentDirectory(cli, path, pathCapacity, out pathLength);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryChangeCurrentDirectory(APTR cli, APTR path,
		uint pathLength)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull)
		{
			_ = SetShellError(DOS.Error.ObjectNotFound);
			return false;
		}
		var platform = new PublicShellPathPlatform(library);
		var result = platform.TryChangeCurrentDirectory(cli, path, pathLength);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TrySetAlias(APTR cli, APTR name, uint nameLength,
		APTR replacement, uint replacementLength)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var result = PublicShellVariableServices.Set(ref this, library, cli, name,
			nameLength, replacement, replacementLength, true);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryRemoveAlias(APTR cli, APTR name, uint nameLength)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var result = PublicShellVariableServices.Remove(ref this, library, cli, name, nameLength, true);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryGetAlias(APTR cli, APTR name, uint nameLength, APTR value,
		uint capacity, out uint valueLength, out uint missing)
	{
		valueLength = 0; missing = 0;
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var result = PublicShellVariableServices.Get(ref this, library, cli, name,
			nameLength, value, capacity, true, out valueLength);
		if (!result && PublicShellFileServices.GetError(library) == (int)DOS.Error.ObjectNotFound) missing = 1;
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryWriteAliases(BPTR output, APTR cli)
	{
		return ShellLocalVariableList.Write(ref this, cli, output, true);
	}

	public bool TryUpdateCommandPath(APTR cli, APTR pathBuffer,
		uint pathBytes, uint pathCount, uint operation, uint quiet)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull)
		{
			_ = SetShellError(DOS.Error.ObjectNotFound);
			return false;
		}
		var platform = new PublicShellPathPlatform(library);
		var result = ShellCommandPath.TryUpdatePacked(ref platform, cli, pathBuffer,
			pathBytes, pathCount, operation, quiet);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryWriteCommandPath(BPTR output, APTR cli, uint quiet)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull)
		{
			_ = SetShellError(DOS.Error.ObjectNotFound);
			return false;
		}
		var platform = new PublicShellPathPlatform(library);
		var result = platform.TryWriteCommandPath(output, cli, quiet);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryBindScriptFrame(APTR cli, APTR frame)
	{
		var result = ShellRunnerBinding.TryPublishFrame(ref this, cli, frame, out var failure);
		_ = SetShellError(result ? DOS.Error.None :
			failure == ShellRunnerBindingFailure.NoMemory ? DOS.Error.NoFreeStore : DOS.Error.ObjectWrongType);
		return result;
	}

	public bool TryUnbindScriptFrame(APTR cli, APTR frame)
	{
		return ShellRunnerBinding.TryFind(ref this, cli, out var binding, out var found, out var runner) &&
			binding.IsNotNull && found == frame && runner.IsNull &&
			ShellFrameResources.CanRelease(ref this, frame) &&
			ReleaseFrameContinuations(frame) &&
			ShellFrameResources.Release(ref this, frame) &&
			ShellRunnerBinding.TryRemoveFrame(ref this, cli, frame);
	}

	public bool TryRequestShellControl(APTR cli, ShellControlAction action,
		int returnCode)
	{
		var dos = CreateDos();
		if (!TryFindFrame(cli, out var frame)) return false;
		if (action == ShellControlAction.Else)
			return ShellScriptControlTransitions.TryElse(ref this, frame);
		if (action is ShellControlAction.EndIf or ShellControlAction.EndSkip)
		{
			APTR closed;
			bool accepted;
			if (action == ShellControlAction.EndSkip)
				accepted = ShellScriptControlTransitions.TryEndSkip(ref this, frame, out closed);
			else accepted = ShellScriptControlTransitions.TryClose(ref this, frame,
				ShellScriptBlockKind.If, out closed);
			if (!accepted) return false;
			var recorded = ShellScriptFrameCodec.TryRecordControl(ref this,
				frame, action, returnCode);
			var released = closed.IsNull || ShellFrameResources.FreeControl(ref this, frame, closed);
			if (released) _ = SetShellError(DOS.Error.None);
			return recorded && released;
		}
		if (action is not (ShellControlAction.EndCli or
			ShellControlAction.EndShell or ShellControlAction.Quit)) return false;
		if (!ShellScriptFrameCodec.TryRecordControl(ref this, frame, action,
			returnCode)) return false;
		// The frame is freed at completion. Preserve an explicit Shell exit in
		// the CLI so the startup owner does not open another interactive reader.
		if (action is ShellControlAction.EndCli or ShellControlAction.EndShell)
			DosCommandLineInterfaceCodec.WriteInteractive(ref dos, cli, 0);
		return true;
	}

	public bool TryDefineScriptLabel(APTR cli, APTR label,
		uint labelLength)
	{
		var dos = CreateDos();
		if (!TryFindFrame(cli, out var frame) || !ShellScriptFrameCodec.TryRead(ref this, frame,
			out var frameState)) return false;
		if (label.IsNull || labelLength == 0 || labelLength > 65_535 ||
			label.Raw > uint.MaxValue - labelLength || !dos.IsMapped(label,
			labelLength)) return false;
		var record = ShellFrameResources.AllocateLabel(ref this, frame, labelLength, out var storedName);
		if (record.IsNull || storedName.IsNull)
		{ _ = SetShellError(DOS.Error.NoFreeStore); return false; }
		_ = SetShellError(DOS.Error.None);
		dos.Copy(label, storedName, labelLength);
		if (ShellScriptLabelTransitions.TryDefine(ref this, frame, record,
			storedName, labelLength, frameState.CurrentLine,
			frameState.CurrentOffset)) return true;
		if (ShellFrameResources.FreeLabel(ref this, frame, record))
			_ = SetShellError(DOS.Error.None);
		return false;
	}

	public bool TrySkipToLabel(APTR cli, APTR label, uint labelLength,
		uint back)
		=> TrySkipToLabel(cli, label, labelLength, back, out _);

	public bool TrySkipToLabel(APTR cli, APTR label, uint labelLength,
		uint back, out int result)
	{
		result = (int)ShellCommandResult.Fail;
		var dos = CreateDos();
		if (back > 1 || !TryFindFrame(cli, out var frame) ||
			!ShellScriptFrameCodec.TryRead(ref this, frame, out var state) ||
			(state.Flags & ShellScriptFrameFlags.Interactive) != 0) return false;
		// Input is inherited by commands and may still be the console while
		// the runner reads an Execute work file or a System command source.
		// TryReadScriptLine resolves that Shell-owned command source.
		const uint scratchSize = ScriptLineCapacity + ScriptSmallCapacity;
		var scratch = dos.AllocateGuest(scratchSize);
		if (scratch.IsNull) { _ = SetShellError(DOS.Error.NoFreeStore); return false; }
		bool found;
		if (back != 0)
			found = ShellScriptLabelTransitions.TryScanFromBeginning(ref this, frame, label,
				labelLength, scratch, ScriptLineCapacity,
				APTR.FromPointer(scratch.Raw + ScriptLineCapacity), ScriptSmallCapacity, out result);
		else found = ShellScriptLabelTransitions.TryScanForward(ref this, frame, label,
			labelLength, scratch, ScriptLineCapacity,
			APTR.FromPointer(scratch.Raw + ScriptLineCapacity), ScriptSmallCapacity, out result);
		dos.FreeGuest(scratch, scratchSize);
		if (found) _ = SetShellError(result == (int)ShellCommandResult.Error
			? DOS.Error.ObjectNotFound : DOS.Error.None);
		return found;
	}

	public bool TryAsk(APTR cli, BPTR input, BPTR output, APTR prompt,
		uint promptLength, out ShellAskResponse response)
	{
		response = default;
		var dos = CreateDos();
		if (cli.IsNull || input.IsNull || output.IsNull || prompt.IsNull ||
			promptLength == 0 || promptLength > 4095 ||
			prompt.Raw > uint.MaxValue - promptLength ||
			!dos.IsMapped(prompt, promptLength) ||
			!TryFindFrame(cli, out _)) return false;
		var answer = dos.AllocateGuest(256);
		if (answer.IsNull || !dos.IsMapped(answer, 256))
		{
			if (answer.IsNotNull) dos.FreeGuest(answer, 256);
			return false;
		}
		var retried = false;
		while (true)
		{
			dos.WriteUInt8(answer, 0, (byte)' ');
			if (Write(output, prompt, promptLength) != (int)promptLength ||
				Write(output, answer, 1) != 1 || ReadScriptString(input, answer, 256).IsNull)
			{
				dos.FreeGuest(answer, 256);
				return false;
			}
			if (ShellAskResponse.TryDecode(ref this, answer, 256, out response)) break;
			retried = true;
		}
		dos.FreeGuest(answer, 256);
		// V40 retains ERROR_TOO_MANY_ARGS after an invalid answer, even if a
		// later answer is accepted. Keep that diagnostic through later I/O.
		if (retried) _ = SetShellError(DOS.Error.TooManyArguments);
		return true;
	}

	public bool TryEvaluateIf(APTR cli, uint condition, uint threshold,
		uint negate, uint noRequester, uint numeric, APTR left,
		uint leftLength, APTR right, uint rightLength)
	{
		if (negate > 1 || noRequester > 1 || numeric > 1 ||
			!TryFindFrame(cli, out var frame) || condition < (uint)ShellIfCondition.PreviousResult ||
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
			var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
			if (library.IsNull) return false;
			var paths = new PublicShellPathPlatform(library);
			if (!paths.TryEnterPathRequesters(noRequester, out var process,
				out var previousWindow))
			{
				PublicShellFileServices.SetError(library, DOS.Error.ObjectWrongType);
				global::Amiga.Exec.CloseLibrary(library);
				return false;
			}
			// Lock recognizes files and directories, unlike opening a file handle.
			var objectLock = PublicShellFileServices.Lock(library, left);
			matched = objectLock.IsNotNull;
			if (objectLock.IsNotNull)
				PublicShellFileServices.UnLock(library, objectLock);
			else
				PublicShellFileServices.SetError(library, DOS.Error.None);
			paths.ExitPathRequesters(process, previousWindow);
			global::Amiga.Exec.CloseLibrary(library);
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
		var control = ShellFrameResources.AllocateControl(ref this, frame);
		if (control.IsNull) { _ = SetShellError(DOS.Error.NoFreeStore); return false; }
		_ = SetShellError(DOS.Error.None);
		if (!ShellScriptControlTransitions.TryOpen(ref this, frame, control,
			ShellScriptBlockKind.If, frameState.CurrentLine,
			frameState.CurrentOffset, matched ? 0u : 1u))
		{
			if (ShellFrameResources.FreeControl(ref this, frame, control))
				_ = SetShellError(DOS.Error.None);
			return false;
		}
		if (ShellScriptFrameCodec.TrySetCondition(ref this, frame, condition))
			return true;
		if (ShellScriptControlTransitions.TryClose(ref this, frame,
			ShellScriptBlockKind.If, out var closed))
			if (ShellFrameResources.FreeControl(ref this, frame, closed))
				_ = SetShellError(DOS.Error.None);
		return false;
	}

	public ShellScriptExecutionStatus TryExecuteScript(APTR cli, APTR file,
		uint fileLength, APTR scriptArguments, uint scriptArgumentLength,
		out int result)
	{
		var dos = CreateDos();
		result = (int)ShellCommandResult.Error;
		if (cli.IsNull || file.IsNull || fileLength == 0 || fileLength > 65_535 ||
			file.Raw > uint.MaxValue - fileLength - 1 ||
			!dos.IsMapped(file, fileLength + 1) ||
			!DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
			return ShellScriptExecutionStatus.Failed;
		var existing = FindRunner(cli);
		if (existing.IsNotNull) return RunScriptRunner(existing, out result);
		if (!TryOpenScriptInput(cli, file, fileLength, out var input))
			return ShellScriptExecutionStatus.Failed;
		// The script is the Shell's reader, not the commands' standard input.
		return StartScriptRunner(cli, input, CurrentInput(), 1, scriptArguments,
			scriptArgumentLength, file, fileLength, out result, out _);
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
		if (input.IsNull)
		{
			DosCommandLineInterfaceCodec.WriteInteractive(ref dos, cli, 0);
			return ShellScriptExecutionStatus.Failed;
		}
		// CliInitRun can supply a separate command stream. Commands still receive
		// the CLI's standard input, which can be a console or caller-owned file.
		var commandInput = cliValue.StandardInput.IsNotNull ? cliValue.StandardInput : input;
		var status = StartScriptRunner(cli, input, commandInput, 0, APTR.Null, 0,
			APTR.Null, 0, out result, out var started);
		if (!started) DosCommandLineInterfaceCodec.WriteInteractive(ref dos, cli, 0);
		return status;
	}

	/// <summary>
	/// Runs one command supplied in Process.pr_Arguments while preserving the
	/// CLI input stream as the command's standard input.
	/// </summary>
	public ShellScriptExecutionStatus TryExecuteCommand(APTR cli, APTR command,
		uint commandLength, out int result)
	{
		var dos = CreateDos();
		result = (int)ShellCommandResult.Error;
		if (cli.IsNull || command.IsNull || commandLength == 0 ||
			commandLength > 65_535 ||
			!DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
			return ShellScriptExecutionStatus.Failed;
		if (command.Raw > uint.MaxValue - commandLength ||
			!dos.IsMapped(command, commandLength))
		{
			_ = SetShellError(DOS.Error.ObjectWrongType);
			return ShellScriptExecutionStatus.Failed;
		}
		var existing = FindRunner(cli);
		if (existing.IsNotNull) return RunScriptRunner(existing, out result);
		var commandInput = ShellCommandSource.Create(ref this, command, commandLength);
		if (commandInput.IsNull)
		{
			_ = SetShellError(DOS.Error.NoFreeStore);
			return ShellScriptExecutionStatus.Failed;
		}
		var cliValue = DosCommandLineInterfaceCodec.Read(ref dos, cli);
		var standardInput = cliValue.CurrentInput.IsNotNull ?
			cliValue.CurrentInput : cliValue.StandardInput;
		if (standardInput.IsNull)
		{
			_ = ShellCommandSource.Release(ref this, commandInput);
			return ShellScriptExecutionStatus.Failed;
		}
		return StartScriptRunner(cli, commandInput, standardInput, 2,
			APTR.Null, 0, APTR.Null, 0, out result, out _);
	}

	private ShellScriptExecutionStatus StartScriptRunner(APTR cli,
		BPTR readerInput, BPTR commandInput, uint inputOwned,
		APTR scriptArguments, uint scriptArgumentLength, APTR scriptSourcePath,
		uint scriptSourcePathLength, out int result, out bool started)
	{
		var dos = CreateDos();
		started = false;
		result = (int)ShellCommandResult.Error;
		if (!RunnerWorkspaceMatches())
		{
			ReleaseOwnedReader(readerInput, inputOwned);
			return ShellScriptExecutionStatus.Failed;
		}
		var cliValue = DosCommandLineInterfaceCodec.Read(ref dos, cli);
        var interactiveReader = inputOwned == 0 && cliValue.Interactive != 0 &&
            cliValue.CurrentInput == cliValue.StandardInput;
        var ownedArguments = CopyScriptArguments(ref dos, scriptArguments,
			scriptArgumentLength);
		if (scriptArgumentLength != 0 && ownedArguments.IsNull)
		{
			ReleaseOwnedReader(readerInput, inputOwned);
			return ShellScriptExecutionStatus.Failed;
		}
		var ownedSourcePath = CopyScriptPath(ref dos, scriptSourcePath,
			scriptSourcePathLength);
		if (scriptSourcePathLength != 0 && ownedSourcePath.IsNull)
		{
			ReleaseScriptGuest(ref dos, ownedArguments, scriptArgumentLength);
			ReleaseOwnedReader(readerInput, inputOwned);
			return ShellScriptExecutionStatus.Failed;
		}
		var frame = dos.AllocateGuest(ShellScriptFrameCodec.Size);
		var runner = APTR.Null;
		var fixedWorkspace = APTR.Null;
		if (frame.IsNotNull) ShellRunnerArena.TryAllocate(ref this, true, out runner, out fixedWorkspace);
        var line = fixedWorkspace;
        var commandName = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Name);
        var token = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Token);
        var first = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.First);
        var second = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Second);
        var third = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Third);
        var fourth = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Fourth);
		// An unknown first line can declare .KEY. Named file scripts use the pre-scan.
        var errorCodes = scriptSourcePathLength == 0 ? dos.AllocateGuest(ScriptErrorCodeCapacity) : APTR.Null;
		var redirectionCommand = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Redirection);
		var redirectionInput = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Input);
		var redirectionOutput = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Output);
		var redirectionError = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Error);
		var aliasLine = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Alias);
		var lookupPath = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Lookup);
		var keyTemplate = scriptSourcePathLength == 0
            ? dos.AllocateGuest(ScriptLineCapacity) : APTR.Null;
		var temporaryPath = WorkspacePart(fixedWorkspace, ShellScriptWorkspaceLayout.Temporary);
		// Noninteractive readers allocate their backtick buffer only when expansion begins.
		var promptTemplate = interactiveReader ? dos.AllocateGuest(ScriptPromptCapacity) : APTR.Null;
		var runnerValue = new ShellScriptRunnerRecord
		{
			Cli = cli, Frame = frame, Input = readerInput, Line = line,
			InputOwned = inputOwned,
			CommandName = commandName, Token = token, First = first,
			Second = second, Third = third, Fourth = fourth,
			ErrorCodes = errorCodes, ErrorCodeBytes = errorCodes.IsNull ? 0 : ScriptErrorCodeCapacity, RedirectionCommand = redirectionCommand,
			RedirectionInput = redirectionInput,
			RedirectionOutput = redirectionOutput,
			RedirectionError = redirectionError, AliasLine = aliasLine,
			LookupPath = lookupPath, ScriptKeyTemplate = keyTemplate,
			ScriptTemporaryPath = temporaryPath,
			PromptTemplate = promptTemplate,
			ScriptSourcePath = ownedSourcePath,
			ScriptSourcePathLength = scriptSourcePathLength,
			ScriptArguments = ownedArguments,
			ScriptArgumentLength = scriptArgumentLength,
			State = ShellScriptRunnerState.Running,
		};
        if (runner.IsNull || !ValidScriptRunnerBuffers(ref dos, in runnerValue, interactiveReader) ||
            !ShellScriptRunnerCodec.Initialize(ref dos, runner, in runnerValue))
        {
			if (runner.IsNotNull)
			{
				ClearPackedWorkspace(ref runnerValue);
				ShellRunnerArena.Free(ref this, runner, true);
			}
            CleanupUnpublishedScript(ref dos, ref runnerValue);
			_ = SetShellError(DOS.Error.NoFreeStore);
            return ShellScriptExecutionStatus.Failed;
        }
		if (!ShellRunnerBinding.TryPublish(ref this, cli, frame, runner, out var bindingFailure))
		{
			ReleaseUnpublishedRunner(runner);
			_ = SetShellError(bindingFailure == ShellRunnerBindingFailure.NoMemory
				? DOS.Error.NoFreeStore : DOS.Error.ObjectWrongType);
			return ShellScriptExecutionStatus.Failed;
		}
		var output = cliValue.CurrentOutput.IsNotNull
			? cliValue.CurrentOutput : cliValue.StandardOutput;
		if (!TryCurrentScriptProcess(cli, out var currentDirectory, out var errorOutput))
		{
			ReleaseRunner(runner);
			return ShellScriptExecutionStatus.Failed;
		}
		// Returning from an Execute work file starts another console frame,
		// but IF ERROR and the prompt still belong to the CLI's last command.
		var initialResult = (int)ShellCommandResult.Ok;
		if (interactiveReader) initialResult = cliValue.ReturnCode;
		var initial = new ShellScriptFrameState
		{
			Parent = cli, Cli = cli, Input = commandInput, Output = output,
			Error = errorOutput, CurrentDirectory = currentDirectory,
			CurrentLine = 1, CurrentOffset = 0,
			FailureLimit = cliValue.FailLevel > 0
				? unchecked((uint)cliValue.FailLevel)
				: ShellScriptFrameCodec.DefaultFailureLimit,
			LastResult = initialResult,
			Flags = ShellScriptFrameFlags.Active |
				(interactiveReader
					? ShellScriptFrameFlags.Interactive
					: ShellScriptFrameFlags.None),
			ScriptArguments = ownedArguments,
			ScriptArgumentLength = scriptArgumentLength,
			ScriptKeyTemplate = keyTemplate,
		};
		if (!ShellScriptFrameCodec.Initialize(ref this, frame, in initial))
		{
			ReleaseRunner(runner);
			return ShellScriptExecutionStatus.Failed;
		}
		if (scriptSourcePath.IsNotNull)
		{
			if (!ShellProcessState.TrySetCommandFile(ref this, cli, ownedSourcePath,
				scriptSourcePathLength, out var ownedCommandFile))
			{
				ReleaseBoundRunner(runner, cli, frame);
				_ = SetShellError(DOS.Error.ObjectWrongType);
				return ShellScriptExecutionStatus.Failed;
			}
			if (!ownedCommandFile)
			{
				ReleaseBoundRunner(runner, cli, frame);
				_ = SetShellError(DOS.Error.ObjectWrongType);
				return ShellScriptExecutionStatus.Failed;
			}
			_ = SetShellError(DOS.Error.None);
			DosCommandLineInterfaceCodec.WriteCurrentInput(ref dos, cli, readerInput);
			DosCommandLineInterfaceCodec.WriteInteractive(ref dos, cli, 0);
		}
		started = true;
		return RunScriptRunner(runner, out result);
	}

	private static bool RunnerWorkspaceMatches() =>
		ShellRunnerBuffers.RecordSize == ShellScriptRunnerCodec.Size &&
		ShellRunnerBuffers.PromptBytes == ShellScriptRunnerCodec.PromptTemplateBytes &&
		ShellRunnerBuffers.KeyBytes == ShellScriptRunnerCodec.KeyTemplateBytes &&
		ShellRunnerBuffers.KeyResultsBytes == ShellScriptRunnerCodec.KeyResultBytes &&
		ShellScriptFrameCodec.Size == ShellScriptRunnerCodec.FrameBytes &&
		ShellScriptWorkspaceLayout.Bytes == ShellScriptRunnerCodec.WorkspaceBytes &&
		ShellScriptWorkspaceLayout.Name == ShellScriptRunnerCodec.WorkspaceName &&
		ShellScriptWorkspaceLayout.Token == ShellScriptRunnerCodec.WorkspaceToken &&
		ShellScriptWorkspaceLayout.First == ShellScriptRunnerCodec.WorkspaceFirst &&
		ShellScriptWorkspaceLayout.Second == ShellScriptRunnerCodec.WorkspaceSecond &&
		ShellScriptWorkspaceLayout.Third == ShellScriptRunnerCodec.WorkspaceThird &&
		ShellScriptWorkspaceLayout.Fourth == ShellScriptRunnerCodec.WorkspaceFourth &&
		ShellScriptWorkspaceLayout.Redirection == ShellScriptRunnerCodec.WorkspaceRedirection &&
		ShellScriptWorkspaceLayout.Input == ShellScriptRunnerCodec.WorkspaceInput &&
		ShellScriptWorkspaceLayout.Output == ShellScriptRunnerCodec.WorkspaceOutput &&
		ShellScriptWorkspaceLayout.Error == ShellScriptRunnerCodec.WorkspaceError &&
		ShellScriptWorkspaceLayout.Alias == ShellScriptRunnerCodec.WorkspaceAlias &&
		ShellScriptWorkspaceLayout.Lookup == ShellScriptRunnerCodec.WorkspaceLookup &&
		ShellScriptWorkspaceLayout.Temporary == ShellScriptRunnerCodec.WorkspaceTemporary;

	public bool TryPollScriptExecution(APTR cli,
		out ShellScriptExecutionStatus status, out int result)
	{
		var dos = CreateDos();
		status = ShellScriptExecutionStatus.Failed;
		result = (int)ShellCommandResult.Error;
		if (cli.IsNull) return false;
		var runner = FindRunner(cli);
		if (runner.IsNull) return false;
		status = RunScriptRunner(runner, out result);
		return true;
	}

	public bool TryPrepareScriptWait(APTR cli)
	{
		var dos = CreateDos();
		if (cli.IsNull || dos.CurrentDosTask.IsNull) return false;
		var runner = FindRunner(cli);
		if (runner.IsNull || !ShellScriptRunnerCodec.TryRead(ref dos, runner, out var stored) ||
			!ShellScriptFrameCodec.TryRead(ref this, stored.Frame,
				out var frameState) || frameState.PendingCommand.IsNull) return false;
		if (!ShellRunnerBuffers.ReleaseInactivePrompt(ref this, runner,
			ref stored.PromptTemplate, ref stored.PromptCapturePathLength,
			stored.PromptCaptureInput, frameState.Flags, frameState.PromptExpansion.Phase,
			frameState.DeferredCommand.Kind)) return false;
		return PublicShellContinuationCore.Poll(ref this, cli, frameState.PendingCommand,
			out var found, out _, out _, out _) && found;
	}

	public bool TryParkScriptWait(APTR cli, uint timeoutTicks)
	{
		var dos = CreateDos();
		if (cli.IsNull) return false;
		var runner = FindRunner(cli);
		if (runner.IsNull || !ShellScriptRunnerCodec.TryRead(ref dos, runner, out var stored)) return false;
		if (!ShellScriptFrameCodec.TryRead(ref this, stored.Frame, out var frame) ||
			frame.PendingCommand.IsNull ||
			!PublicShellContinuationCore.Poll(ref this, cli, frame.PendingCommand,
				out var publicOwner, out _, out _, out _)) return false;
		if (publicOwner)
		{
			var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
			if (library.IsNull) return false;
			var savedError = PublicShellFileServices.GetError(library);
			var ok = true;
			var elapsed = 0u;
			var delay = 1u;
			while (true)
			{
				if (!PublicShellContinuationCore.Poll(ref this, cli, frame.PendingCommand,
					out var found, out var status, out _, out _) || !found) { ok = false; break; }
				if (status == ShellProcessContinuationState.Completed ||
					(timeoutTicks != uint.MaxValue && elapsed >= timeoutTicks)) break;
				// A short timer can expire during DOS/Exec call overhead before
				// the caller blocks. Repeating Delay(1) can then starve a ready
				// lower-priority worker. Back off without exceeding a finite
				// caller's remaining budget; no OS-specific scheduler is used.
				var remaining = timeoutTicks == uint.MaxValue ? delay : timeoutTicks - elapsed;
				var slice = delay < remaining ? delay : remaining;
				PublicShellContinuationDosVectors.Delay(library, slice);
				elapsed += slice;
				if (delay < 25) delay = delay < 13 ? delay * 2 : 25;
			}
			PublicShellFileServices.SetError(library, (DOS.Error)savedError);
			global::Amiga.Exec.CloseLibrary(library);
			return ok;
		}
		return false;
	}

	private ShellScriptExecutionStatus RunScriptRunner(APTR runner,
		out int result)
	{
		var dos = CreateDos();
		result = (int)ShellCommandResult.Error;
		if (!ShellScriptRunnerCodec.TryRead(ref dos, runner,
			out var stored)) return ShellScriptExecutionStatus.Failed;
		if (!ShellScriptFrameCodec.TryRead(ref this, stored.Frame,
			out var initialFrame))
		{
			ReleaseBoundRunner(runner, stored.Cli, stored.Frame);
			return ShellScriptExecutionStatus.Failed;
		}
		if (!ShellRunnerBuffers.ReleaseInactiveKey(ref this, runner, stored.Frame,
			ref stored.ScriptKeyTemplate, stored.ScriptKeyTemplateLength, initialFrame.CurrentLine,
			initialFrame.ScriptKeyTemplate, initialFrame.ScriptKeyTemplateLength) ||
			!ShellRunnerBuffers.ReleaseInactivePrompt(ref this, runner,
				ref stored.PromptTemplate, ref stored.PromptCapturePathLength,
				stored.PromptCaptureInput, initialFrame.Flags, initialFrame.PromptExpansion.Phase,
				initialFrame.DeferredCommand.Kind))
		{
			ReleaseBoundRunner(runner, stored.Cli, stored.Frame);
			return ShellScriptExecutionStatus.Failed;
		}
		var workspace = BuildScriptWorkspace(in stored);
		if (stored.ScriptSourcePathLength != 0 &&
			stored.ScriptTemporaryPathLength == 0)
		{
			var prepared = ShellScriptPreScanner.Prepare(ref this, stored.Cli,
				stored.Frame, stored.Input, stored.ScriptSourcePath,
				stored.ScriptSourcePathLength, stored.ScriptTemporaryPath,
				ScriptSmallCapacity, in workspace, out var needsKeyTemplate);
			if (prepared == ShellScriptPreScanResult.Failed)
			{
				ReleaseBoundRunner(runner, stored.Cli, stored.Frame);
				return ShellScriptExecutionStatus.Failed;
			}
			if (prepared == ShellScriptPreScanResult.Transformed)
			{
				if (!ShellScriptRunnerCodec.TryRead(ref dos, runner, out var refreshed))
				{
					ReleaseBoundRunner(runner, stored.Cli, stored.Frame);
					return ShellScriptExecutionStatus.Failed;
				}
				stored = refreshed;
			}
			if (needsKeyTemplate && (!ShellRunnerBuffers.EnsureKey(ref this, runner, stored.Frame, ref stored.ScriptKeyTemplate) ||
                !ShellRunnerBuffers.EnsureKeyResults(ref this, runner, ref stored.ErrorCodes, ref stored.ErrorCodeBytes)))
			{
				ReleaseBoundRunner(runner, stored.Cli, stored.Frame);
				_ = SetShellError(DOS.Error.NoFreeStore);
				return ShellScriptExecutionStatus.Failed;
			}
            if (needsKeyTemplate) workspace = BuildScriptWorkspace(in stored);
		}
		ShellScriptRunResult run;
        // Refresh ownership between completed engine steps. Native disk commands
        // run synchronously, so one large batch would retain finished capture
        // storage until the entire Startup-Sequence reaches EOF.
        do
        {
            // CD and external commands may replace and release the Process's
            // directory lock. The frame only borrows it, so refresh it before
            // passing the directory to the next command.
            var process = CurrentRunnerProcess();
            if (process.IsNull || !DosProcessCodec.IsMapped(ref dos, process) ||
                DosProcessCodec.ReadCommandLineInterface(ref dos, process).Address != stored.Cli ||
                !ShellScriptFrameCodec.TrySetCurrentDirectory(ref this, stored.Frame,
                    BPTR.FromRaw(dos.ReadUInt32(process, DosLayout.Process.CurrentDirectory))))
            {
                ReleaseBoundRunner(runner, stored.Cli, stored.Frame);
                _ = SetShellError(DOS.Error.ObjectWrongType);
                return ShellScriptExecutionStatus.Failed;
            }
            run = ShellScriptEngine.Run(ref this, stored.Frame, in workspace, ScriptMaximumSteps);
            if (run.Status == ShellScriptStepStatus.StepLimit &&
                ShellScriptFrameCodec.TryRead(ref this, stored.Frame, out initialFrame))
            {
                stored.PromptTemplate = APTR.FromPointer(dos.ReadUInt32(runner, 124));
                stored.PromptCaptureInput = BPTR.FromRaw(dos.ReadUInt32(runner, 128));
                stored.PromptCapturePathLength = dos.ReadUInt32(runner, 132);
                if (!ShellRunnerBuffers.ReleaseInactivePrompt(ref this, runner,
                    ref stored.PromptTemplate, ref stored.PromptCapturePathLength,
                    stored.PromptCaptureInput, initialFrame.Flags, initialFrame.PromptExpansion.Phase,
                    initialFrame.DeferredCommand.Kind) ||
                    !ShellRunnerBuffers.ReleaseInactiveKey(ref this, runner, stored.Frame,
                    ref stored.ScriptKeyTemplate, stored.ScriptKeyTemplateLength, initialFrame.CurrentLine,
                    initialFrame.ScriptKeyTemplate, initialFrame.ScriptKeyTemplateLength))
                {
                    ReleaseBoundRunner(runner, stored.Cli, stored.Frame);
                    return ShellScriptExecutionStatus.Failed;
                }
                // Expansion may have allocated, retained or retired the owner.
                // Never carry a released pointer into the next engine step.
                workspace.PromptTemplate = stored.PromptTemplate;
            }
        }
		while (run.Status == ShellScriptStepStatus.StepLimit);
		result = run.Result;
		if (run.Status == ShellScriptStepStatus.Waiting)
		{
			ShellScriptRunnerCodec.TrySetState(ref dos, runner,
				ShellScriptRunnerState.Pending, run.Result, run.Steps);
			if (dos.CurrentDosTask.IsNotNull) TryPrepareScriptWait(stored.Cli);
			return ShellScriptExecutionStatus.Pending;
		}
		var terminal = ShellScriptEngine.IsCompletedRunStatus(run.Status);
		var diagnosticsCaptured = ShellScriptCompletionDiagnostics.TryCapture(
			ref this, stored.Cli, in run, out var diagnostics);
		FinishNativeReader(stored.Cli, stored.Frame);
		ShellScriptRunnerCodec.TrySetState(ref dos, runner,
			terminal ? ShellScriptRunnerState.Completed :
			ShellScriptRunnerState.Failed, run.Result, run.Steps);
		var released = ReleaseBoundRunner(runner, stored.Cli, stored.Frame);
		// Restore after all cleanup so the native Process-return owner captures
		// the script's outcome, not the last successful close/free operation.
		if (!released || !diagnosticsCaptured ||
			!TryPublishCommandDiagnostics(stored.Cli, in diagnostics))
		{
			result = (int)ShellCommandResult.Error;
			return ShellScriptExecutionStatus.Failed;
		}
		return terminal ? ShellScriptExecutionStatus.Completed :
			ShellScriptExecutionStatus.Failed;
	}

	private ShellScriptStepWorkspace BuildScriptWorkspace(
		in ShellScriptRunnerRecord stored)
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
		workspace.PromptTemplate = stored.PromptTemplate;
		workspace.PromptTemplateCapacity = ScriptPromptCapacity;
		workspace.PromptCapturePath = APTR.FromPointer(stored.ScriptTemporaryPath.Raw + ShellScriptRunnerCodec.PromptCapturePathOffset);
		workspace.PromptCapturePathCapacity = ScriptSmallCapacity - ShellScriptRunnerCodec.PromptCapturePathOffset;
		workspace.ShellNumber = CurrentShellNumber(stored.Cli);
		return workspace;
	}

	private bool ValidScriptRunnerBuffers(ref CopperSharpNativeDosPlatform dos,
		in ShellScriptRunnerRecord value, bool promptRequired) => value.Frame.IsNotNull &&
		dos.IsMapped(value.Frame, ShellScriptFrameCodec.Size) &&
		value.Line.IsNotNull && dos.IsMapped(value.Line, ScriptLineCapacity) &&
		value.CommandName.IsNotNull && dos.IsMapped(value.CommandName,
			ScriptCommandNameCapacity) && value.Token.IsNotNull &&
		dos.IsMapped(value.Token, ScriptTokenCapacity) && value.First.IsNotNull &&
		dos.IsMapped(value.First, ScriptSmallCapacity) && value.Second.IsNotNull &&
		dos.IsMapped(value.Second, ScriptSmallCapacity) && value.Third.IsNotNull &&
		dos.IsMapped(value.Third, ScriptSmallCapacity) && value.Fourth.IsNotNull &&
		dos.IsMapped(value.Fourth, ScriptSmallCapacity) &&
		(value.ErrorCodes.IsNull ? value.ErrorCodeBytes == 0 : dos.IsMapped(value.ErrorCodes,
			ScriptErrorCodeCapacity)) && value.RedirectionCommand.IsNotNull &&
		dos.IsMapped(value.RedirectionCommand, ScriptLineCapacity) &&
		value.RedirectionInput.IsNotNull && dos.IsMapped(value.RedirectionInput,
			ScriptSmallCapacity) && value.RedirectionOutput.IsNotNull &&
		dos.IsMapped(value.RedirectionOutput, ScriptSmallCapacity) &&
		value.RedirectionError.IsNotNull && dos.IsMapped(value.RedirectionError,
			ScriptSmallCapacity) && value.AliasLine.IsNotNull &&
		dos.IsMapped(value.AliasLine, ScriptLineCapacity) &&
		value.LookupPath.IsNotNull && dos.IsMapped(value.LookupPath,
			ScriptSmallCapacity) && (value.ScriptKeyTemplate.IsNull ? value.ScriptKeyTemplateLength == 0 :
			dos.IsMapped(value.ScriptKeyTemplate, ScriptLineCapacity)) &&
		value.ScriptTemporaryPath.IsNotNull && dos.IsMapped(value.ScriptTemporaryPath,
			ScriptSmallCapacity) &&
		(value.PromptTemplate.IsNull ? !promptRequired :
			dos.IsMapped(value.PromptTemplate, ScriptPromptCapacity)) &&
		(value.ScriptSourcePathLength == 0
			? value.ScriptSourcePath.IsNull
			: value.ScriptSourcePath.IsNotNull &&
				value.ScriptSourcePathLength <= 65_535 &&
				dos.IsMapped(value.ScriptSourcePath,
					value.ScriptSourcePathLength + 1) &&
				dos.ReadUInt8(value.ScriptSourcePath,
					unchecked((int)value.ScriptSourcePathLength)) == 0) &&
		(value.ScriptArgumentLength == 0
			? value.ScriptArguments.IsNull
			: value.ScriptArguments.IsNotNull &&
				value.ScriptArgumentLength <= 65_535 &&
				dos.IsMapped(value.ScriptArguments,
					value.ScriptArgumentLength));

    private static APTR WorkspacePart(APTR workspace, uint offset) =>
        workspace.IsNull ? APTR.Null : APTR.FromPointer(workspace.Raw + offset);

	private static void ClearPackedWorkspace(ref ShellScriptRunnerRecord value)
	{
		value.Line = APTR.Null; value.CommandName = APTR.Null; value.Token = APTR.Null;
		value.First = APTR.Null; value.Second = APTR.Null; value.Third = APTR.Null; value.Fourth = APTR.Null;
		value.RedirectionCommand = APTR.Null; value.RedirectionInput = APTR.Null;
		value.RedirectionOutput = APTR.Null; value.RedirectionError = APTR.Null;
		value.AliasLine = APTR.Null; value.LookupPath = APTR.Null; value.ScriptTemporaryPath = APTR.Null;
	}

	private void CleanupUnpublishedScript(ref CopperSharpNativeDosPlatform dos,
		ref ShellScriptRunnerRecord value)
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
		ReleaseScriptGuest(ref dos, value.First, ScriptSmallCapacity); value.First = APTR.Null;
		ReleaseScriptGuest(ref dos, value.Second, ScriptSmallCapacity); value.Second = APTR.Null;
		ReleaseScriptGuest(ref dos, value.Third, ScriptSmallCapacity); value.Third = APTR.Null;
		ReleaseScriptGuest(ref dos, value.Fourth, ScriptSmallCapacity); value.Fourth = APTR.Null;
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
		ReleaseScriptGuest(ref dos, value.AliasLine, ScriptLineCapacity); value.AliasLine = APTR.Null;
		ReleaseScriptGuest(ref dos, value.LookupPath, ScriptSmallCapacity);
		value.LookupPath = APTR.Null;
		ReleaseScriptGuest(ref dos, value.ScriptKeyTemplate,
			ScriptLineCapacity);
		value.ScriptKeyTemplate = APTR.Null;
		ReleaseScriptGuest(ref dos, value.PromptTemplate,
			ScriptPromptCapacity);
		value.PromptTemplate = APTR.Null;
		ReleaseScriptGuest(ref dos, value.ScriptTemporaryPath,
			ScriptSmallCapacity);
		value.ScriptTemporaryPath = APTR.Null;
		value.ScriptTemporaryPathLength = 0;
		ReleaseScriptGuest(ref dos, value.ScriptSourcePath,
			value.ScriptSourcePathLength == 0 ? 0 :
				value.ScriptSourcePathLength + 1);
		value.ScriptSourcePath = APTR.Null;
		value.ScriptSourcePathLength = 0;
		ReleaseScriptGuest(ref dos, value.ScriptArguments,
			value.ScriptArgumentLength);
		value.ScriptArguments = APTR.Null;
		value.ScriptArgumentLength = 0;
		value.ScriptKeyTemplateLength = 0;
		if (value.InputOwned != 0 && value.Input.IsNotNull)
		{
			ReleaseOwnedReader(value.Input, value.InputOwned);
			value.Input = BPTR.Null;
		}
	}

	private void ReleaseOwnedReader(BPTR input, uint ownership)
	{
		if (ownership == 1) CloseScriptFile(input);
		else if (ownership == 2) _ = ShellCommandSource.Release(ref this, input);
	}

	private static void ReleaseScriptGuest(ref CopperSharpNativeDosPlatform dos,
		APTR address, uint size)
	{
		if (address.IsNotNull)
			dos.FreeGuest(address, size);
	}

	private static APTR CopyScriptPath(ref CopperSharpNativeDosPlatform dos,
		APTR source, uint length)
	{
		if (length == 0) return APTR.Null;
		if (source.IsNull || length > 65_535 ||
			source.Raw > uint.MaxValue - length - 1 ||
			!dos.IsMapped(source, length + 1) ||
			dos.ReadUInt8(source, unchecked((int)length)) != 0)
			return APTR.Null;
		var owned = dos.AllocateGuest(length + 1);
		if (owned.IsNull || !dos.IsMapped(owned, length + 1))
		{
			if (owned.IsNotNull) dos.FreeGuest(owned, length + 1);
			return APTR.Null;
		}
		dos.Copy(source, owned, length + 1);
		return owned;
	}

	private static APTR CopyScriptArguments(ref CopperSharpNativeDosPlatform dos,
		APTR source, uint length)
	{
		if (length == 0) return APTR.Null;
		if (source.IsNull || length > 65_535 ||
			source.Raw > uint.MaxValue - length || !dos.IsMapped(source, length))
			return APTR.Null;
		var owned = dos.AllocateGuest(length);
		if (owned.IsNull || !dos.IsMapped(owned, length))
		{
			if (owned.IsNotNull) dos.FreeGuest(owned, length);
			return APTR.Null;
		}
		dos.Copy(source, owned, length);
		return owned;
	}

	public bool TryManageResident(APTR cli,
		in ShellResidentManagementRequest request)
	{
		var dos = CreateDos();
		// Newer DOS implementations expose deferred segment creation as a
		// public tag-list vector. The V40 ABI has no such vector.
		if (request.Alias.IsNull && request.Defer != 0 && request.Add != 0 &&
			PublicShellResidentMutation.SupportsDeferred(ref dos))
			return PublicShellResidentMutation.AddDeferred(ref dos, in request);
		if (request.Alias.IsNull && request.Defer == 0 &&
			(request.Remove != 0 || request.Add != 0 || request.Replace != 0))
			return PublicShellResidentMutation.Manage(ref dos, in request);
		// V40 has no deferred-segment or resident-alias creation API. A
		// shared Shell has no private CopperStart registry to fall back to.
		_ = SetShellError(DOS.Error.NotImplemented);
		return false;
	}

	public bool TryListResidents(APTR cli, BPTR output, uint includeSystem) =>
		PublicShellResidentListing.List(ref this, cli, output, includeSystem);

	public bool TryGetInternalCommandEnabled(APTR cli, uint commandIdentity,
		out uint enabled)
	{
		if (!ShellProcessState.TryGetCommand(ref this, cli, commandIdentity,
			out var present, out enabled)) { _ = SetShellError(DOS.Error.ObjectWrongType); return false; }
		if (present) return SetShellError(DOS.Error.None);
		_ = SetShellError(DOS.Error.ObjectWrongType);
		return false;
	}

	public bool TrySetInternalCommandEnabled(APTR cli, uint commandIdentity,
		uint enabled)
	{
		if (!ShellProcessState.TrySetCommand(ref this, cli, commandIdentity,
			enabled, out var present)) { _ = SetShellError(DOS.Error.ObjectWrongType); return false; }
		if (present) return SetShellError(DOS.Error.None);
		_ = SetShellError(DOS.Error.ObjectWrongType);
		return false;
	}

	public bool RunChildParsesRedirections() => true;

	public bool TryRunCommand(APTR cli, BPTR input, BPTR output, BPTR error, uint ownedStreams,
		BPTR currentDirectory, APTR continuation, APTR command, uint commandLength, uint detach,
		uint quiet, uint stack, uint stackPresent, int priority,
		uint priorityPresent)
	{
		// V40 passes the Run remainder as command text. Newer direct-launch
		// extensions have no public worker implementation on this interface.
		if (detach != 0 || quiet != 0)
		{ _ = SetShellError(DOS.Error.NotImplemented); return false; }
		if (continuation.IsNull)
			return PublicShellBackgroundCore.Start(cli, input, output, error, ownedStreams,
				command, commandLength, APTR.Null, BPTR.Null,
				stack, stackPresent, priority, priorityPresent);
		return PublicShellBackgroundCore.Start(cli, input, output, error, ownedStreams,
			command, commandLength, continuation, currentDirectory,
			stack, stackPresent, priority, priorityPresent);
	}

	public bool TryCreateShell(APTR parentCli, ShellLaunchKind kind,
		BPTR input, BPTR output, BPTR error, BPTR currentDirectory,
		APTR continuation, APTR window,
		uint windowLength, APTR from, uint fromLength)
	{
		if (continuation.IsNull)
			return PublicShellNewProcessCore.Start(parentCli, kind, window,
				windowLength, from, fromLength, APTR.Null, BPTR.Null);
		return PublicShellNewProcessCore.Start(parentCli, kind, window,
			windowLength, from, fromLength, continuation, currentDirectory);
	}

	public bool TryPollShellContinuation(APTR cli, APTR continuation,
		out ShellProcessContinuationState state, out int result)
	{
		state = ShellProcessContinuationState.Failed;
		result = (int)ShellCommandResult.Error;
		if (!PublicShellContinuationCore.Poll(ref this, cli, continuation,
			out var owner, out var observed, out var observedResult, out _) || !owner) return false;
		state = observed;
		result = observedResult;
		return true;
	}

	public bool TryReadContinuationDiagnostics(APTR cli, APTR continuation,
		out ShellCommandDiagnostics diagnostics)
	{
		diagnostics = default;
		if (!PublicShellContinuationCore.Poll(ref this, cli, continuation,
			out var owner, out var state, out var result, out var error) || !owner ||
			state != ShellProcessContinuationState.Completed) return false;
		diagnostics.ReturnCode = result;
		diagnostics.IoError = error;
		return true;
	}

	public bool TryReleaseShellContinuation(APTR cli, APTR continuation,
		uint ownedFlags)
	{
		return PublicShellContinuationCore.Release(ref this, cli, continuation, ownedFlags,
			out var owner) && owner;
	}

	public bool TryExecuteScriptCommand(APTR cli, APTR frame,
		in ShellScriptCommandInvocation command,
		in ShellScriptLookupResult lookup, BPTR input,
		BPTR output, BPTR error,
		out int result, out APTR continuation)
	{
		return ExecuteForeground(cli, frame, in command, in lookup, input,
			output, error, out result, out continuation);
	}

	public bool TryReadArgs(APTR argumentText, uint argumentLength,
		APTR template, uint templateLength, APTR resultArray, uint resultBytes,
		out APTR rdArgs)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) { rdArgs = APTR.Null; return false; }
		// Kickstart ReadArgs expects the caller to initialize its result slots;
		// the former CopperStart-owned adapter cleared them on every call.
		if (resultArray.IsNull || resultArray.Raw > uint.MaxValue - resultBytes)
		{
			PublicShellFileServices.SetError(dos, DOS.Error.BufferOverflow);
			global::Amiga.Exec.CloseLibrary(dos);
			rdArgs = APTR.Null;
			return false;
		}
		Clear(resultArray, resultBytes);
		rdArgs = PublicShellArgumentServices.Read(dos, argumentText,
			argumentLength, template, resultArray);
		global::Amiga.Exec.CloseLibrary(dos);
		return rdArgs.IsNotNull;
	}

	public bool TryReadScriptFilePrefix(APTR argumentText,
		uint argumentLength, out uint consumed)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) { consumed = 0; return false; }
		var result = PublicShellArgumentServices.ReadFirstItem(dos, argumentText,
			argumentLength, out consumed);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public void FreeArgs(APTR rdArgs)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return;
		PublicShellArgumentServices.Free(dos, rdArgs);
		global::Amiga.Exec.CloseLibrary(dos);
	}

	public bool TryGetLocalVariable(APTR cli, APTR name, uint nameLength,
		APTR value, uint valueCapacity, out uint valueLength)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) { valueLength = 0; return false; }
		var result = PublicShellVariableServices.Get(ref this, library, cli, name,
			nameLength, value, valueCapacity, false, out valueLength);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TrySetLocalVariable(APTR cli, APTR name, uint nameLength,
		APTR value, uint valueLength)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var result = PublicShellVariableServices.Set(ref this, library, cli, name, nameLength, value, valueLength, false);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryWriteLocalVariables(BPTR output, APTR cli)
	{
		return ShellLocalVariableList.Write(ref this, cli, output, false);
	}

	public bool TryGetGlobalVariable(APTR name, uint nameLength, APTR value,
		uint valueCapacity, out uint valueLength)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) { valueLength = 0; return false; }
		var result = PublicShellVariableServices.GetGlobal(ref this, dos, name,
			nameLength, value, valueCapacity, out valueLength);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public bool TrySetGlobalVariable(APTR name, uint nameLength, APTR value,
		uint valueLength, uint save)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellVariableServices.SetGlobal(ref this, dos, name,
			nameLength, value, valueLength, save);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public bool TryWriteGlobalVariables(BPTR input, BPTR output, BPTR error, out int result)
	{
		var dos = new CopperSharpNativeDosPlatform();
		return PublicShellGlobalListing.Execute(dos, input, output, error, out result);
	}

	public bool TryRemoveLocalVariable(APTR cli, APTR name, uint nameLength)
	{
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var result = PublicShellVariableServices.Remove(ref this, library, cli, name, nameLength, false);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryRemoveGlobalVariable(APTR name, uint nameLength, uint save)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellVariableServices.RemoveGlobal(ref this, dos, name,
			nameLength, save);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public bool ClearConsole(BPTR output, uint reset)
		=> ShellConsoleOutput.Clear(ref this, output, reset);

	public bool TryWriteWhy(BPTR output, APTR cli)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellFaultServices.WriteWhy(ref this, dos, output, cli);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public bool TryWriteFault(BPTR output, APTR errorCodes, uint errorCount)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellFaultServices.WriteFaults(ref this, dos, output,
			errorCodes, errorCount);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public bool TrySetPrompt(APTR cli, APTR value, uint valueLength,
		uint reset)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellPromptServices.Set(ref this, dos, cli, value, valueLength, reset);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public bool TryWriteScriptPrompt(APTR cli, BPTR output)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellPromptServices.WritePrompt(ref this, dos, cli, output);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public bool TryCopyScriptPromptTemplate(APTR cli, APTR destination,
		uint destinationCapacity, out ShellScriptPromptTemplate template)
	{
		template = default;
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellPromptServices.CopyTemplate(ref this, dos, cli,
			destination, destinationCapacity, out var length);
		global::Amiga.Exec.CloseLibrary(dos);
		if (!result) return false;
		template.Text = destination;
		template.Length = length;
		return true;
	}

	public bool TryWriteScriptPromptLiteral(APTR cli, BPTR output,
		in ShellScriptPromptSegment segment)
	{
		if (segment.Kind != ShellScriptPromptSegmentKind.Literal) return false;
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellPromptServices.WriteLiteral(ref this, dos, cli,
			output, segment.Text, segment.Length);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public int Write(BPTR handle, APTR source, uint length)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return -1;
		var result = PublicShellFileServices.Write(dos, handle, source, length);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public int Read(BPTR handle, APTR destination, uint length)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return -1;
		var result = PublicShellFileServices.Read(dos, handle, destination, length);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public int WriteByte(BPTR handle, byte value)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return -1;
		var result = PublicShellFileServices.WriteByte(dos, handle, value);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	public BPTR OpenOutput(APTR path, uint pathLength)
	{
		return OpenScriptOutput(path, pathLength, 0);
	}

	public bool CloseOutput(BPTR handle)
	{
		return CloseScriptFile(handle);
	}

	private BPTR OpenScriptOutput(APTR path, uint length, uint append)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return BPTR.Null;
		var result = PublicShellFileServices.OpenOutput(dos, path, length, append);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	private bool CloseScriptFile(BPTR handle)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		var result = PublicShellFileServices.Close(dos, handle) != 0;
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	private int FileIsInteractive(BPTR handle)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return -1;
		var result = PublicShellFileServices.IsInteractive(dos, handle);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	private BPTR CurrentInput()
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return BPTR.Null;
		var result = PublicShellFileServices.Input(dos);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	private APTR ReadScriptString(BPTR handle, APTR buffer, int capacity)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return APTR.Null;
		var result = PublicShellFileServices.ReadString(dos, handle, buffer, capacity);
		global::Amiga.Exec.CloseLibrary(dos);
		return result;
	}

	/// <summary>Publishes an error through the current process's DOS interface.</summary>
	public bool SetShellError(DOS.Error error)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		PublicShellFileServices.SetError(dos, error);
		global::Amiga.Exec.CloseLibrary(dos);
		return true;
	}

	private bool TryGetShellError(out int error)
	{
		error = 0;
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) return false;
		error = PublicShellFileServices.GetError(dos);
		global::Amiga.Exec.CloseLibrary(dos);
		return true;
	}

	public bool TryPollScriptSignal(APTR cli,
		out ShellScriptSignalEvent signal)
	{
		signal = default;
		var dos = CreateDos();
		if (cli.IsNull || !DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
		{
			_ = SetShellError(DOS.Error.ObjectWrongType);
			return true;
		}
		if (!SetShellError(DOS.Error.None)) return false;
		const uint ctrlC = 1u << 12, ctrlD = 1u << 13;
		var received = global::Amiga.Exec.SetSignal(0, ctrlC | ctrlD) & (ctrlC | ctrlD);
		if ((received & ctrlC) != 0)
		{
			signal.Flags = ShellScriptSignalFlags.Break |
				ShellScriptSignalFlags.CtrlC;
			signal.Result = (int)DOS.Error.Break;
			signal.Sequence = ctrlC;
		}
		else if ((received & ctrlD) != 0)
		{
			signal.Flags = ShellScriptSignalFlags.CtrlD;
			signal.Sequence = ctrlD;
		}
		return true;
	}

	public bool TryCaptureCommandDiagnostics(APTR cli, int returnCode,
		out ShellCommandDiagnostics diagnostics)
	{
		var dos = CreateDos();
		diagnostics = new ShellCommandDiagnostics { ReturnCode = returnCode };
		if (!DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
		{
			_ = SetShellError(DOS.Error.ObjectWrongType);
			return false;
		}
		if (returnCode != 0)
		{
			if (!TryGetShellError(out var error)) return false;
			diagnostics.IoError = error;
		}
		return true;
	}

	public bool TryReadPublishedCommandDiagnostics(APTR cli,
		out ShellCommandDiagnostics diagnostics)
	{
		var dos = CreateDos();
		diagnostics = default;
		if (!DosCommandLineInterfaceCodec.IsMapped(ref dos, cli)) return false;
		var published = DosCommandLineInterfaceCodec.Read(ref dos, cli);
		diagnostics.ReturnCode = published.ReturnCode;
		diagnostics.IoError = published.Result2;
		return true;
	}

	public bool TryBeginCommandDiagnostics(APTR cli)
	{
		var dos = CreateDos();
		if (DosCommandLineInterfaceCodec.IsMapped(ref dos, cli)) return SetShellError(DOS.Error.None);
		_ = SetShellError(DOS.Error.ObjectWrongType);
		return false;
	}

	public bool TryPublishCommandDiagnostics(APTR cli,
		in ShellCommandDiagnostics diagnostics)
	{
		var dos = CreateDos();
		if (!DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
		{
			_ = SetShellError(DOS.Error.ObjectWrongType);
			return false;
		}
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		DosCommandLineInterfaceCodec.WriteReturnCode(ref dos, cli, diagnostics.ReturnCode);
		DosCommandLineInterfaceCodec.WriteResult2(ref dos, cli, diagnostics.IoError);
		PublicShellFileServices.SetError(library, (DOS.Error)diagnostics.IoError);
		global::Amiga.Exec.CloseLibrary(library);
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
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) { expanded = 0; expandedLength = 0; return false; }
		var result = PublicShellVariableServices.ExpandAlias(ref this, library, cli,
			source, sourceLength, destination, destinationCapacity, out expanded, out expandedLength);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryLookupScriptCommand(APTR cli, APTR name,
		uint nameLength, in ShellScriptLookupWorkspace workspace,
		out ShellScriptLookupResult lookup)
	{
		var dos = CreateDos();
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) { lookup = default; return false; }
		var identity = ShellInternalCommandResolver.Resolve(ref this, name, nameLength);
		var ignoreInternal = false;
		if (identity != ShellInternalCommand.Unknown)
		{
			if (!TryGetInternalCommandEnabled(cli, (uint)identity, out var enabled))
			{ lookup = default; global::Amiga.Exec.CloseLibrary(library); return false; }
			ignoreInternal = enabled == 0;
		}
		var result = PublicShellCommandLookup.Find(ref dos, library, cli, name,
			nameLength, in workspace, ignoreInternal, out lookup);
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryReadScriptLine(APTR cli, BPTR input,
		uint currentLine, uint currentOffset, APTR destination,
		uint destinationCapacity, out uint lineLength, out uint nextLine,
		out uint nextOffset, out uint endOfFile)
	{
		var dos = CreateDos();
		// A System command has its own text reader while commands inherit the
		// caller's standard input. Probe handles used by the pre-scanner remain
		// independent of both streams.
		var reader = input;
		var shellCommandReader = false;
		var runner = FindRunner(cli);
		if (runner.IsNotNull && ShellScriptRunnerCodec.TryRead(ref dos, runner, out var stored) &&
			ShellScriptFrameCodec.TryRead(ref this, stored.Frame, out var frame) &&
			input == frame.Input)
		{
			reader = stored.Input;
			shellCommandReader = stored.InputOwned == 2;
		}
		lineLength = 0;
		nextLine = currentLine;
		nextOffset = currentOffset;
		endOfFile = 0;
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		if (currentLine == uint.MaxValue || destination.IsNull || destinationCapacity < 2 ||
			destinationCapacity > ScriptLineCapacity || destination.Raw > uint.MaxValue - destinationCapacity)
		{
			PublicShellFileServices.SetError(library, DOS.Error.BufferOverflow);
			global::Amiga.Exec.CloseLibrary(library);
			return false;
		}
		if (shellCommandReader)
		{
			var readCommand = ShellCommandSource.TryRead(ref this, reader, currentOffset,
				destination, destinationCapacity, out lineLength, out nextOffset, out endOfFile);
			if (readCommand) nextLine = endOfFile != 0 ? currentLine : currentLine + 1;
			PublicShellFileServices.SetError(library, readCommand ? DOS.Error.None : DOS.Error.ObjectWrongType);
			global::Amiga.Exec.CloseLibrary(library);
			return readCommand;
		}
		var seekable = PublicShellFileServices.IsInteractive(library, reader) == 0;
		var read = !seekable || PublicShellFileServices.Seek(library, reader,
			unchecked((int)currentOffset), (int)DosConstants.OffsetBeginning) >= 0;
		if (read)
			read = PublicShellFileServices.ReadLine(library, reader, destination,
				destinationCapacity, out lineLength, out endOfFile);
		if (read)
		{
			nextLine = endOfFile != 0 ? currentLine : currentLine + 1;
			nextOffset = 0;
			if (seekable)
			{
				var position = PublicShellFileServices.Seek(library, reader, 0,
					(int)DosConstants.OffsetCurrent);
				if (position < 0) read = false;
				else nextOffset = unchecked((uint)position);
			}
		}
		global::Amiga.Exec.CloseLibrary(library);
		return read;
	}

	public bool TryOpenScriptInput(APTR cli, APTR path, uint pathLength,
		out BPTR handle)
	{
		var dos = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (dos.IsNull) { handle = BPTR.Null; return false; }
		handle = PublicShellFileServices.OpenInput(dos, path, pathLength);
		if (handle.IsNotNull) PublicShellFileServices.BufferOwnedScriptInput(dos, handle);
		global::Amiga.Exec.CloseLibrary(dos);
		return handle.IsNotNull;
	}

	public bool TryOpenScriptOutput(APTR cli, APTR path, uint pathLength,
		uint append, out BPTR handle)
	{
		handle = OpenScriptOutput(path, pathLength, append);
		return handle.IsNotNull;
	}

	public bool TryPublishScriptInput(APTR cli, APTR frame, BPTR source,
		BPTR replacement, APTR temporaryPath, uint temporaryPathLength)
	{
		var dos = CreateDos();
		var runner = FindRunner(cli);
		return runner.IsNotNull && ShellScriptRunnerCodec.TryRead(ref dos, runner, out var stored) && stored.Frame.Raw == frame.Raw &&
			stored.Input.Raw == source.Raw &&
			ShellScriptFrameCodec.TryRead(ref this, frame, out var frameState) &&
			frameState.InputState.IsNull &&
			CloseScriptFile(source) &&
			ShellScriptRunnerCodec.TrySetTemporaryInput(ref dos, runner,
				replacement, temporaryPath, temporaryPathLength);
	}

	public bool TryDeleteScriptPath(APTR cli, APTR path, uint pathLength)
	{
		var dos = CreateDos();
		if (!(cli.IsNotNull && path.IsNotNull && pathLength != 0 &&
			pathLength < uint.MaxValue &&
			path.Raw <= uint.MaxValue - pathLength - 1 &&
			dos.IsMapped(path, pathLength + 1) &&
			dos.ReadUInt8(path, unchecked((int)pathLength)) == 0)) return false;
		var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
		if (library.IsNull) return false;
		var result = PublicShellFileServices.Delete(library, path) != 0;
		global::Amiga.Exec.CloseLibrary(library);
		return result;
	}

	public bool TryEnsureScriptPromptTemplate(APTR cli, out APTR buffer, out uint capacity)
    {
        var dos = CreateDos();
        buffer = APTR.Null; capacity = 0;
        var runner = FindRunner(cli);
        if (runner.IsNull || !ShellScriptRunnerCodec.TryRead(ref dos, runner, out var value) ||
            !ShellRunnerBuffers.EnsurePrompt(ref this, runner, ref value.PromptTemplate)) return false;
        buffer = value.PromptTemplate; capacity = ShellRunnerBuffers.PromptBytes;
        return true;
    }

    public bool TrySelectScriptPromptDirectory(APTR cli, APTR path, uint capacity, ref uint length)
    {
        if (cli.IsNull) return false;
        var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
        if (library.IsNull) return false;
        var pathPlatform = new PublicShellPathPlatform(library);
        var success = ShellScriptTemporaryPath.TrySelectPromptDirectory(ref pathPlatform, path, capacity, ref length);
        global::Amiga.Exec.CloseLibrary(library);
        return success;
    }

    public bool TrySetScriptPromptCapture(APTR cli, APTR path,
		uint pathLength, BPTR input)
	{
		var dos = CreateDos();
		var runner = FindRunner(cli);
		return cli.IsNotNull && path.IsNotNull && pathLength != 0 &&
			pathLength < ScriptSmallCapacity &&
			dos.IsMapped(path, pathLength + 1) &&
			dos.ReadUInt8(path, unchecked((int)pathLength)) == 0 &&
			runner.IsNotNull && ShellScriptRunnerCodec.TrySetPromptCapture(
				ref dos, runner, path, pathLength, input);
	}

	public bool TryCloseScriptRedirection(APTR cli, BPTR handle)
	{
		return CloseScriptFile(handle);
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
