using System.Runtime.CompilerServices;
using Amiga;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Small native ABI for the DOS-backed Shell runner.  The Shell segment owns
/// command dispatch; these entries resume its owned Execute state and return
/// fixed-width status/result pairs. Legacy context register arguments are ignored;
/// state belongs to the current Shell process. No entry invokes a child recursively.
/// </summary>
public static class DosShellNativeEntrypoints
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperos.shell.execute-begin")]
	[return: M68kRegister(M68kRegister.D0)]
	public static ulong ExecuteBeginEntry(
		[M68kRegister(M68kRegister.D0)] uint cli,
		[M68kRegister(M68kRegister.D1)] uint file,
		[M68kRegister(M68kRegister.D2)] uint fileLength,
		[M68kRegister(M68kRegister.A0)] uint dosState,
		[M68kRegister(M68kRegister.A6)] uint execBase) =>
		Begin(APTR.FromPointer(cli), APTR.FromPointer(file), fileLength,
			APTR.FromPointer(dosState), APTR.FromPointer(execBase));

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperos.shell.execute-poll")]
	[return: M68kRegister(M68kRegister.D0)]
	public static ulong ExecutePollEntry(
		[M68kRegister(M68kRegister.D0)] uint cli,
		[M68kRegister(M68kRegister.A0)] uint dosState,
		[M68kRegister(M68kRegister.A6)] uint execBase) =>
		Poll(APTR.FromPointer(cli), APTR.FromPointer(dosState),
			APTR.FromPointer(execBase));

	public static APTR AddressOfExecuteBegin() =>
		APTR.ExportAddress("copperos.shell.execute-begin");
	public static APTR AddressOfExecutePoll() =>
		APTR.ExportAddress("copperos.shell.execute-poll");

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperos.shell.entry")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint ShellTaskEntry() =>
		DosShellNativeChildCore.RunFromCurrentTask();

	/// <summary>Starts a Shell launched by a standard DOS startup packet.</summary>
	public static uint PacketShellTaskEntry()
	{
		var task = global::Amiga.Exec.FindTask(CString.FromPointer(0));
		return task.IsNull ? 20u : DosShellPacketStartup.Run(task);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperos.shell.child")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint ShellChildEntry(
		[M68kRegister(M68kRegister.A6)] uint execBase)
	{
		// Preserve the exported register signature, but a new Exec task does
		// not inherit A6. The canonical ExecBase pointer is held at address 4.
		_ = execBase;
		return ShellTaskEntry();
	}

	public static APTR AddressOfShellTaskEntry() =>
		APTR.ExportAddress("copperos.shell.entry");
	public static APTR AddressOfShellChild() =>
		APTR.ExportAddress("copperos.shell.child");

	// The legacy native image declares export labels in its primary assembly;
	// its small forwarding entries call these shared worker implementations.
	public static uint BackgroundWorker() => PublicShellBackgroundCore.Worker();
	public static uint NewProcessWorker() => PublicShellNewProcessCore.Worker();

	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperos.shell.queue-handler-task")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint QueueHandlerTaskEntry() =>
		DosQueueHandlerTaskNativeCore.RunFromCurrentTask();

	public static APTR AddressOfQueueHandlerTaskEntry() =>
		APTR.ExportAddress("copperos.shell.queue-handler-task");
	public static APTR AddressOfExecutePark() =>
		DosShellNativeParkEntrypoint.AddressOfExecutePark();

	private static ulong Begin(APTR cli, APTR file, uint fileLength,
		APTR dosState, APTR execBase)
	{
		_ = dosState;
		_ = execBase;
		var platform = new DosShellNativePlatform(APTR.Null);
		var status = platform.TryExecuteScript(cli, file, fileLength,
			APTR.Null, 0, out var result);
		return ReturnPair((uint)status, unchecked((uint)result));
	}

	private static ulong Poll(APTR cli, APTR dosState, APTR execBase)
	{
		_ = dosState;
		_ = execBase;
		var platform = new DosShellNativePlatform(APTR.Null);
		if (!platform.TryPollScriptExecution(cli, out var status,
			out var result))
			return ReturnPair((uint)ShellScriptExecutionStatus.Failed,
				unchecked((uint)ShellCommandResult.Error));
		return ReturnPair((uint)status, unchecked((uint)result));
	}

	private static ulong ReturnPair(uint status, uint result) => unchecked((ulong)
		CopperSharp.Compiler.M68kRuntime.CombineInt64(status, result));
}
