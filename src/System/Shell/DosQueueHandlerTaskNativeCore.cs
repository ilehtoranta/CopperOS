using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Bootstraps a DOS-owned Queue Handler task from Task.UserData.</summary>
internal static class DosQueueHandlerTaskNativeCore
{
	public static uint RunFromCurrentTask()
	{
		// Exec task entry frames clear saved registers, so read the task through
		// the canonical ExecBase before constructing its DOS-state adapter.
		var bootstrap = new CopperSharpNativeDosPlatform(0);
		var task = bootstrap.CurrentDosTask;
		if (!ExecTaskCodec.IsMapped(ref bootstrap, task))
			return (uint)DosQueueHandlerTaskExit.InvalidState;
		var recordAddress = ExecTaskCodec.Read(ref bootstrap, task).UserData;
		if (!DosQueueHandlerTaskRecordCodec.TryRead(ref bootstrap,
			recordAddress, out var record))
			return (uint)DosQueueHandlerTaskExit.InvalidState;
		var platform = new CopperSharpNativeDosPlatform(record.DosState.Raw);
		return DosQueueHandlerTaskCore.Run(ref platform, recordAddress);
	}
}
