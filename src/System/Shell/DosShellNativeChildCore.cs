using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Entry-side logic for a native NewCLI/NewShell child.  The launch boundary
/// creates the Process/CLI; this value-type routine consumes the CLI's FROM
/// BSTR, converts it to a bounded C string, and drives the existing persistent
/// DOS-owned script runner until it completes.  A child without FROM reads its
/// inherited CLI input through the same persistent runner.
/// </summary>
internal static class DosShellNativeChildCore
{
	private const uint ObjectHeaderSize = 28;
	private const uint MaximumFromLength = 255;
	private const uint MaximumArgumentLength = 65_535;
	private const uint MaximumWaitPolls = 4096;

	public static uint RunFromCurrentTask(APTR execBase)
	{
		if (execBase.IsNull) return unchecked((uint)ShellCommandResult.Error);
		var dos = new CopperSharpNativeDosPlatform(execBase);
		var task = dos.CurrentDosTask;
		if (task.IsNull || !dos.IsMapped(task, global::Amiga.Process.Size))
			return unchecked((uint)ShellCommandResult.Error);
		var cli = APTR.FromPointer(DosProcessCore.Cli(ref dos));
		if (cli.IsNull || !DosCommandLineInterfaceCodec.IsMapped(ref dos, cli) ||
			cli.Raw < ObjectHeaderSize)
			return Complete(ref dos, task, unchecked((uint)
				ShellCommandResult.Error));

		var allocation = APTR.FromPointer(cli.Raw - ObjectHeaderSize);
		if (!dos.IsMapped(allocation, ObjectHeaderSize) ||
			dos.ReadUInt32(allocation) != 0x444F_424A ||
			dos.ReadUInt32(allocation, 20) !=
			(uint)DosObjectType.CommandLineInterface)
			return Complete(ref dos, task, unchecked((uint)
				ShellCommandResult.Error));
		var state = APTR.FromPointer(dos.ReadUInt32(allocation, 4));
		if (state.IsNull || dos.ReadUInt32(state) != DosCore.StateMagic ||
			dos.ReadUInt32(state, 4) != DosCore.StateVersion)
			return Complete(ref dos, task, unchecked((uint)
				ShellCommandResult.Error));

		var shell = new DosShellNativePlatform(state);
		var arguments = DosProcessCodec.ReadArguments(ref dos, task);
		var commandFile = DosCommandLineInterfaceCodec.Read(ref dos, cli)
			.CommandFile.Address;
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
				status = shell.TryExecuteScript(cli, path, pathLength, out result);
			}
		}
		for (var poll = 0u; status == ShellScriptExecutionStatus.Pending &&
			poll < MaximumWaitPolls; poll++)
		{
			if (!shell.TryPrepareScriptWait(cli) ||
				!shell.TryParkScriptWait(cli, uint.MaxValue) ||
				!shell.TryPollScriptExecution(cli, out status, out result))
			{
				result = (int)ShellCommandResult.Error;
				status = ShellScriptExecutionStatus.Failed;
				break;
			}
		}
		if (status == ShellScriptExecutionStatus.Pending)
			result = (int)ShellCommandResult.Error;
		if (arguments.IsNotNull && DosSystemCore.ContinuesWithInput(
			DosProcessCodec.ReadShellPrivate(ref dos, task)) &&
			status == ShellScriptExecutionStatus.Completed)
		{
			status = shell.TryStartInteractiveScript(cli, out result);
			for (var poll = 0u; status == ShellScriptExecutionStatus.Pending &&
				poll < MaximumWaitPolls; poll++)
			{
				if (!shell.TryPrepareScriptWait(cli) ||
					!shell.TryParkScriptWait(cli, uint.MaxValue) ||
					!shell.TryPollScriptExecution(cli, out status, out result))
				{
					result = (int)ShellCommandResult.Error;
					status = ShellScriptExecutionStatus.Failed;
					break;
				}
			}
			if (status == ShellScriptExecutionStatus.Pending)
				result = (int)ShellCommandResult.Error;
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
		dos.WriteUInt32(task, DosLayout.Process.Result2, result);
		return result;
	}
}
