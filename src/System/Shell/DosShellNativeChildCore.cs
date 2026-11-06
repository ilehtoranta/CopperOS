using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Entry-side logic for a native NewCLI/NewShell child.  The launch boundary
/// creates the Process/CLI; this value-type routine consumes the CLI's FROM
/// BSTR, converts it to a bounded C string, and drives the existing persistent
/// Shell-owned script runner until it completes. A child without FROM reads its
/// inherited CLI input through the same persistent runner.
/// </summary>
internal static class DosShellNativeChildCore
{
	private const uint MaximumFromLength = 255;
	private const uint MaximumArgumentLength = 65_535;

	public static uint RunFromCurrentTask()
	{
		// Exec creates the task with cleared saved registers, including A6.
		// FindTask uses the public Exec base and the runner owns its state.
		var dos = new CopperSharpNativeDosPlatform();
		var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
		if (!DosProcessCodec.IsMapped(ref dos, task))
			return unchecked((uint)ShellCommandResult.Error);
		var cli = DosProcessCodec.ReadCommandLineInterface(ref dos, task).Address;
		if (!DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
			return Complete(ref dos, task, unchecked((uint)
				ShellCommandResult.Error));
		return RunInstalled(ref dos, task, cli, false, false);
	}

	/// <summary>
	/// Runs the shared Shell against an already initialized public CLI input.
	/// Packet startup and stream ownership remain the caller's responsibility.
	/// </summary>
	public static uint RunFromInitializedCli(bool resumeStandardInput = false)
	{
		var dos = new CopperSharpNativeDosPlatform();
		var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
		if (!DosProcessCodec.IsMapped(ref dos, task))
			return unchecked((uint)ShellCommandResult.Error);
		var cli = DosProcessCodec.ReadCommandLineInterface(ref dos, task).Address;
		if (!DosCommandLineInterfaceCodec.IsMapped(ref dos, cli))
			return unchecked((uint)ShellCommandResult.Error);
		return RunInstalled(ref dos, task, cli, true, resumeStandardInput);
	}

	private static uint RunInstalled(ref CopperSharpNativeDosPlatform dos,
		APTR task, APTR cli, bool initializedInput, bool resumeStandardInput)
	{
		var shell = new DosShellNativePlatform(APTR.Null);
		if (!ShellProcessState.TryInstall(ref shell, cli, out var processState, out var failure))
		{
			_ = shell.SetShellError(failure == ShellProcessStateFailure.NoMemory
				? DOS.Error.NoFreeStore : DOS.Error.ObjectWrongType);
			return Complete(ref dos, task, unchecked((uint)ShellCommandResult.Error));
		}
		var result = RunWithProcessState(ref dos, ref shell, task, cli,
			initializedInput, resumeStandardInput);
		if (!PublicShellContinuationCore.DetachAll(ref shell, cli) ||
			!ShellProcessState.TryRemove(ref shell, cli, processState))
			return Complete(ref dos, task, unchecked((uint)ShellCommandResult.Error));
		return result;
	}

	private static uint RunWithProcessState(ref CopperSharpNativeDosPlatform dos,
		ref DosShellNativePlatform shell, APTR task, APTR cli,
		bool initializedInput, bool resumeStandardInput)
	{
		var arguments = initializedInput ? APTR.Null : DosProcessCodec.ReadArguments(ref dos, task);
		var startupCli = DosCommandLineInterfaceCodec.Read(ref dos, cli);
		var resumeReader = initializedInput && resumeStandardInput &&
			startupCli.StandardInput.IsNotNull &&
			startupCli.CurrentInput != startupCli.StandardInput;
		var interactiveStartup = arguments.IsNull && startupCli.Interactive != 0 &&
			startupCli.CurrentInput == startupCli.StandardInput && startupCli.StandardInput.IsNotNull;
		if (resumeReader) interactiveStartup = true;
		var commandFile = initializedInput ? APTR.Null :
			DosCommandLineInterfaceCodec.Read(ref dos, cli).CommandFile.Address;
		var path = APTR.Null;
		var pathLength = 0u;
		ShellScriptExecutionStatus status;
		int result;
		if (arguments.IsNotNull)
		{
			var argumentLength = CStringLength(ref dos, arguments,
				MaximumArgumentLength);
			if (argumentLength > MaximumArgumentLength)
				return Complete(ref dos, task, unchecked((uint)
					ShellCommandResult.Fail));
			// This entry consumes pr_Arguments directly. Drain only its startup
			// input prefix before running the text, so System continue-input
			// resumes the inherited stream without executing the command twice.
			var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
			if (library.IsNull)
				return Complete(ref dos, task, unchecked((uint)
					ShellCommandResult.Error));
			var consumed = PublicShellArgumentServices.ConsumeInputPrefix(library,
				PublicShellFileServices.Input(library), arguments, argumentLength);
			global::Amiga.Exec.CloseLibrary(library);
			if (!consumed)
				return Complete(ref dos, task, unchecked((uint)
					ShellCommandResult.Error));
			if (argumentLength == 0)
			{
				status = ShellScriptExecutionStatus.Completed;
				result = (int)ShellCommandResult.Ok;
			}
			else
				status = shell.TryExecuteCommand(cli, arguments, argumentLength,
					out result);
		}
		else if (commandFile.IsNull)
		{
			status = shell.TryStartInteractiveScript(cli, out result);
		}
		else if (!dos.IsMapped(commandFile, 1))
		{
			return Complete(ref dos, task, unchecked((uint)
				ShellCommandResult.Fail));
		}
		else
		{
			pathLength = dos.ReadUInt8(commandFile);
			if (pathLength == 0)
			{
				status = shell.TryStartInteractiveScript(cli, out result);
			}
			else if (pathLength > MaximumFromLength ||
				!dos.IsMapped(commandFile, pathLength + 1))
				return Complete(ref dos, task, unchecked((uint)
					ShellCommandResult.Fail));
			else
			{
				path = dos.AllocateGuest(pathLength + 1);
				if (path.IsNull || !dos.IsMapped(path, pathLength + 1))
				{
					if (path.IsNotNull) dos.FreeGuest(path, pathLength + 1);
					return Complete(ref dos, task, unchecked((uint)
						ShellCommandResult.Error));
				}
				for (var index = 0u; index < pathLength; index++)
					dos.WriteUInt8(path, unchecked((int)index),
						dos.ReadUInt8(commandFile, unchecked((int)index + 1)));
				dos.WriteUInt8(path, unchecked((int)pathLength), 0);
				status = shell.TryExecuteScript(cli, path, pathLength,
					APTR.Null, 0, out result);
			}
		}
		while (status == ShellScriptExecutionStatus.Pending)
		{
			if (!shell.TryPrepareScriptWait(cli) ||
				!shell.TryParkScriptWait(cli, uint.MaxValue) ||
				!shell.TryPollScriptExecution(cli, out status, out result))
			{
				_ = shell.AbortScriptExecution(cli);
				result = (int)ShellCommandResult.Error;
				status = ShellScriptExecutionStatus.Failed;
				break;
			}
		}
		if (status == ShellScriptExecutionStatus.Pending)
			result = (int)ShellCommandResult.Error;
		if (resumeReader && status == ShellScriptExecutionStatus.Completed)
		{
			if (!ShellProcessState.TryEndRequested(ref shell, cli, out var endRequested))
			{
				result = (int)ShellCommandResult.Error;
				status = ShellScriptExecutionStatus.Failed;
			}
			else if (!endRequested)
			{
				var library = global::Amiga.Exec.OpenLibraryRaw("dos.library", 37);
				if (library.IsNull)
				{
					result = (int)ShellCommandResult.Error;
					status = ShellScriptExecutionStatus.Failed;
				}
				else
				{
					var current = DosCommandLineInterfaceCodec.Read(ref dos, cli).CurrentInput;
					_ = NativeForegroundDosVectors.SelectInput(library, startupCli.StandardInput);
					DosCommandLineInterfaceCodec.WriteCurrentInput(ref dos, cli, startupCli.StandardInput);
					// Execute may already have closed the startup input and installed
					// its own reader. Close it only while the CLI still identifies it.
					if (current == startupCli.CurrentInput && current.IsNotNull &&
						current != startupCli.StandardOutput)
						_ = PublicShellFileServices.Close(library, current);
					global::Amiga.Exec.CloseLibrary(library);
					status = shell.TryStartInteractiveScript(cli, out result);
					while (status == ShellScriptExecutionStatus.Pending)
					{
						if (!shell.TryPrepareScriptWait(cli) ||
							!shell.TryParkScriptWait(cli, uint.MaxValue) ||
							!shell.TryPollScriptExecution(cli, out status, out result))
						{
							_ = shell.AbortScriptExecution(cli);
							result = (int)ShellCommandResult.Error;
							status = ShellScriptExecutionStatus.Failed;
							break;
						}
					}
				}
			}
		}
		// Execute can replace the reader of an interactive Shell with a work
		// file. Once that reader reaches EOF, resume the original console.
		// EndCLI and a console EOF clear cli_Interactive during teardown.
		while (interactiveStartup && status == ShellScriptExecutionStatus.Completed &&
			DosCommandLineInterfaceCodec.Read(ref dos, cli).Interactive != 0)
		{
			status = shell.TryStartInteractiveScript(cli, out result);
			while (status == ShellScriptExecutionStatus.Pending)
			{
				if (!shell.TryPrepareScriptWait(cli) ||
					!shell.TryParkScriptWait(cli, uint.MaxValue) ||
					!shell.TryPollScriptExecution(cli, out status, out result))
				{
					_ = shell.AbortScriptExecution(cli);
					result = (int)ShellCommandResult.Error;
					status = ShellScriptExecutionStatus.Failed;
					break;
				}
			}
		}
		if (path.IsNotNull) dos.FreeGuest(path, pathLength + 1);
		return Complete(ref dos, task, unchecked((uint)result));
	}

	private static uint CStringLength(ref CopperSharpNativeDosPlatform dos,
		APTR text, uint maximum)
	{
		for (var length = 0u; length <= maximum; length++)
		{
			if (text.Raw > uint.MaxValue - length ||
				!dos.IsMapped(APTR.FromPointer(text.Raw + length), 1))
				return maximum + 1;
			if (dos.ReadUInt8(text, unchecked((int)length)) == 0) return length;
		}
		return maximum + 1;
	}

	private static uint Complete(ref CopperSharpNativeDosPlatform dos,
		APTR task, uint result)
	{
		// D0 is the command return code. pr_Result2 already contains the DOS
		// error published by the runner; replacing it with D0 loses IoErr().
		_ = dos;
		_ = task;
		return result;
	}
}
