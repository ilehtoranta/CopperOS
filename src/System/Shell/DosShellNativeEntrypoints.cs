using System.Runtime.CompilerServices;
using Amiga;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Small native ABI for the DOS-backed Shell runner.  The Shell segment owns
/// command dispatch; these entries only bridge the persistent Execute state
/// to DOS's prepared wait primitives and return fixed-width status/result
/// pairs.  No entry spins or invokes a child recursively.
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
	[M68kExport("copperos.shell.child")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint ShellChildEntry(
		[M68kRegister(M68kRegister.A6)] uint execBase) =>
		DosShellNativeChildCore.RunFromCurrentTask(
			APTR.FromPointer(execBase));

	public static APTR AddressOfShellChild() =>
		APTR.ExportAddress("copperos.shell.child");
	public static APTR AddressOfExecutePark() =>
		DosShellNativeParkEntrypoint.AddressOfExecutePark();

	private static ulong Begin(APTR cli, APTR file, uint fileLength,
		APTR dosState, APTR execBase)
	{
		var dos = new CopperSharpNativeDosPlatform(execBase);
		if (!DosShellNativeContextCore.WriteExecBase(ref dos, dosState,
			execBase)) return ReturnPair((uint)ShellScriptExecutionStatus.Failed,
			unchecked((uint)ShellCommandResult.Error));
		var platform = new DosShellNativePlatform(dosState);
		var status = platform.TryExecuteScript(cli, file, fileLength,
			out var result);
		return ReturnPair((uint)status, unchecked((uint)result));
	}

	private static ulong Poll(APTR cli, APTR dosState, APTR execBase)
	{
		var dos = new CopperSharpNativeDosPlatform(execBase);
		if (!DosShellNativeContextCore.WriteExecBase(ref dos, dosState,
			execBase)) return ReturnPair((uint)ShellScriptExecutionStatus.Failed,
			unchecked((uint)ShellCommandResult.Error));
		var platform = new DosShellNativePlatform(dosState);
		if (!platform.TryPollScriptExecution(cli, out var status,
			out var result))
			return ReturnPair((uint)ShellScriptExecutionStatus.Failed,
				unchecked((uint)ShellCommandResult.Error));
		return ReturnPair((uint)status, unchecked((uint)result));
	}

	private static ulong ReturnPair(uint status, uint result) => unchecked((ulong)
		CopperSharp.Compiler.M68kRuntime.CombineInt64(status, result));
}
