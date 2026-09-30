using System.Runtime.CompilerServices;
using Amiga;
using CopperSharp.Compiler;
using CopperOS.Shell;
using CopperOS.Shell.Dos;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos.NativeRoot;

/// <summary>
/// Small compiler reachability root for the DOS-backed Shell capability.
/// Scheduler, requester, and external-image hooks intentionally remain
/// explicit failures until their DOS task boundaries are implemented. The
/// fixed-width runner, foreground-wait records, native Run handoff, child
/// shell startup, and continuation teardown are rooted here for the same
/// local-ABI assembly.
/// </summary>
public static class DosShellNativeRoots
{
	// Park-only qualification roots the actual scheduler boundary via its export.
	// It needs no process-launch or child-Shell capability roots.
	public static uint ParkCapabilityRoot() => 0;

	// A standalone Shell qualification HUNK must own the DOS task-return export
	// referenced by the production process publisher. In the installed system,
	// CopperStart's ROM root provides this ABI entry instead.
	[MethodImpl(MethodImplOptions.NoInlining)]
	[M68kExport("copperstart.dos.process-return")]
	[return: M68kRegister(M68kRegister.D0)]
	public static uint DosProcessReturn(
		[M68kRegister(M68kRegister.D0)] uint returnCode) =>
		DosNativeProcessReturn.Entry(returnCode);

	public static uint CapabilityRoot()
	{
		// Compile-time reachability probe, not a guest startup entry: the calls
		// below deliberately retain DOS capabilities using placeholder addresses.
		// The Execute exports are added explicitly by the qualification script.
		var dos = default(CopperSharpRomDosPlatform);
		var state = APTR.FromPointer(0x0003_2000);
		var wait = DosShellForegroundWaitCore.Allocate(ref dos, state,
			APTR.Null, APTR.Null, APTR.Null, APTR.Null, 1, 0);
		if (wait.IsNotNull) return 1;
		if (DosShellScriptRunnerCore.FindByCli(ref dos, state,
			APTR.Null).IsNotNull) return 2;
		var nativeShell = new DosShellNativePlatform(state);
		if (nativeShell.TryRunCommand(APTR.Null, BPTR.Null, BPTR.Null, BPTR.Null,
			BPTR.Null, APTR.Null, APTR.Null, 0, 0, 0, 0, 0, 0, 0)) return 3;
		if (nativeShell.TryCreateShell(APTR.FromPointer(4),
			ShellLaunchKind.NewShell, BPTR.Null, BPTR.Null, BPTR.Null,
			BPTR.Null, APTR.Null, APTR.Null, 0, APTR.FromPointer(12), 1))
			return 5;
		if (nativeShell.TryReleaseShellContinuation(APTR.FromPointer(4),
			APTR.FromPointer(8), 0)) return 4;
		return 0;
	}
}
