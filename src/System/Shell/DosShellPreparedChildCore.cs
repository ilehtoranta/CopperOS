using Amiga;
using CopperStart.Dos;
using CopperStart.Exec;

namespace CopperOS.Shell.Dos;

/// <summary>Caller-owned resources awaiting the final DOS child publication.</summary>
internal struct DosShellPreparedChild
{
	public APTR State;
	public APTR ExecBase;
	public APTR Task;
	public APTR Entry;
	public APTR Continuation;
	public APTR Arguments;
	public uint ArgumentLength;
	/// <summary>One prepares a ReadArgs command line, including an empty LF line;
	/// zero preserves literal Shell startup text or the absence of arguments.</summary>
	public uint CommandArguments;
	public BPTR Segment;
	public APTR ResidentEntry;
}

internal static class DosShellPreparedChildCore
{
	/// <summary>
	/// Copies all transient text and attaches every child-owned resource before
	/// the only publication call. A successful rollback releases the unpublished
	/// child and the caller's image claim. Deferred retirement moves those
	/// claims to a typed DOS ticket.
	/// A successful publication must be followed by no
	/// further child access: it may already have run and terminated.
	/// </summary>
	public static bool Publish<TPlatform>(ref TPlatform platform,
		in DosShellPreparedChild child)
		where TPlatform : struct, IDosPlatform, IExecMemoryPlatform =>
		Publish(ref platform, in child, out _);

	public static bool Publish<TPlatform>(ref TPlatform platform,
		in DosShellPreparedChild child, out bool rollbackComplete)
		where TPlatform : struct, IDosPlatform, IExecMemoryPlatform
	{
		rollbackComplete = false;
		var residentBound = false;
		var ready = child.CommandArguments <= 1 &&
			(child.CommandArguments != 0
				? DosChildProcessLaunchCore.CopyPreparedCommandArguments(ref platform,
					child.State, child.Task, child.Arguments, child.ArgumentLength)
				: child.Arguments.IsNull && child.ArgumentLength == 0 ||
					DosChildProcessLaunchCore.CopyPreparedArguments(ref platform,
						child.State, child.Task, child.Arguments, child.ArgumentLength));
		if (ready && child.ResidentEntry.IsNotNull)
		{
			residentBound = DosProcessImageCore.BindResident(ref platform,
				child.State, child.Task, child.ResidentEntry, child.Segment);
			ready = residentBound;
		}
		if (ready && DosChildProcessLaunchCore.PublishPrepared(ref platform,
			child.ExecBase, child.State, child.Task, child.Entry,
			child.Continuation, child.ResidentEntry.IsNull && child.Segment.IsNotNull))
			return true;

		var error = DosCore.IoErr(ref platform, child.State);
		if (error == DOS.Error.None) error = DOS.Error.ObjectWrongType;
		var imageClaims = new DosChildProcessLaunchCore.DosPreparedRollbackImageClaims(
			child.Segment, child.ResidentEntry, residentBound);
		if (!DosChildProcessLaunchCore.AbortPreparedWithImageClaims(ref platform,
			child.ExecBase, child.State, child.Task, child.Continuation,
			in imageClaims, out var recoveryQueued))
		{
			DosCore.SetIoErr(ref platform, child.State, error);
			if (recoveryQueued)
			{
				// The ticket now owns the child and image claims. The continuation
				// has been made terminal, so its caller may release frame storage.
				rollbackComplete = true;
			}
			return false;
		}
		if (child.ResidentEntry.IsNotNull)
		{
			// Binding transferred the acquired resident reference to DOS; abort
			// releases it. Only an unbound reference remains ours to release.
			if (!residentBound)
				DosResidentRegistryCore.Release(ref platform, child.State,
					child.ResidentEntry);
		}
		else if (child.Segment.IsNotNull)
			DosSegmentLoaderCore.Unload(ref platform, child.State, child.Segment);
		DosCore.SetIoErr(ref platform, child.State, error);
		rollbackComplete = true;
		return false;
	}
}
