using Amiga;
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
	public static uint CapabilityRoot()
	{
		// Keep this callable root side-effect free; the Execute exports are added
		// explicitly by the native qualification script.
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
