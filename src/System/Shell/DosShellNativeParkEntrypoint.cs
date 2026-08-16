using System.Runtime.CompilerServices;
using Amiga;
using CopperSharp.Compiler;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>
/// Native scheduler boundary for one already-prepared Execute wait.  This is
/// kept in its own reachability unit so the fixed-width park ABI can also be
/// qualified independently of the full Shell runner.
/// </summary>
public static class DosShellNativeParkEntrypoint
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperos.shell.execute-park")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint ExecuteParkEntry(
		[M68kRegister(M68kRegister.D0)] uint cli,
		[M68kRegister(M68kRegister.D1)] uint timeoutTicks,
		[M68kRegister(M68kRegister.A0)] uint dosState,
		[M68kRegister(M68kRegister.A6)] uint execBase)
	{
		var platform = new CopperSharpNativeDosPlatform(
			APTR.FromPointer(execBase));
		var state = APTR.FromPointer(dosState);
		var parent = APTR.FromPointer(cli);
		if (parent.IsNull) return 0;
		var runner = DosShellNativeBridge.FindScriptRunner(ref platform, state,
			parent);
		if (runner.IsNull || !DosShellNativeBridge.ReadScriptRunner(ref platform,
			state, runner, out var stored)) return 0;
		var wait = DosShellNativeBridge.FindForegroundWaitByFrame(ref platform,
			state, stored.Frame);
		return wait.IsNotNull && DosShellNativeBridge.ParkForegroundWait(
			ref platform, state, wait, timeoutTicks) ? 1u : 0u;
	}

	public static APTR AddressOfExecutePark() =>
		APTR.ExportAddress("copperos.shell.execute-park");
}
