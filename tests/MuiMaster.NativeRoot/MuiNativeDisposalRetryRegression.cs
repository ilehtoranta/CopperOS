using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// Simulated-provider native entry: tests real generated disposal control flow,
// not native Intuition or a font provider's refusal contract.
public static class MuiNativeDisposalRetryRegression
{
	public static uint BusyChildRetryRoot()
		=> RunBusyChildRetry(false);

	public static uint RootOwnedChildRetryRoot()
		=> RunBusyChildRetry(true);

	private static uint RunBusyChildRetry(bool rootDisposal)
	{
		var platform = default(MuiNativeHeadlessPlatform);
		platform.Reset();
		var state = APTR.FromPointer(0x00036000);
		var root = APTR.FromPointer(0x00035F00);
		var name = APTR.FromPointer(0x00036100);
		APTR.WriteUInt8(name, 0, (byte)'N');
		APTR.WriteUInt8(name, 1, 0);
		if (!MuiMasterLifecycleCore.Create(ref platform, root, state)) return 1;
		var cl = MuiHeadlessObjectCore.RegisterBuiltinClass(ref platform,
			state, name, APTR.Null, 8, APTR.FromPointer(1));
		if (cl.IsNull) return 2;
		var parent = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cl, APTR.Null);
		var child = MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cl, APTR.Null);
		if (parent.IsNull || child.IsNull ||
			!MuiFamilyCore.AddTail(ref platform, state, parent, child)) return 3;
		var childRecord = MuiHeadlessObjectCore.FindObject(ref platform, state, child);
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord, out var saved)) return 4;
		var busy = saved;
		busy.Flags |= MuiHeadlessObjectCore.ObjectProviderBusy;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord, busy)) return 5;
		if (MuiHeadlessObjectCore.DisposeObject(ref platform, state, parent)) return 6;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, parent).IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, child).IsNull) return 7;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, cl, out var retained) ||
			retained.ObjectCount != 2) return 8;
		if (MuiMasterLifecycleCore.Dispose(ref platform, root)) return 15;
		// Preserve the current detached-parent field and only release the
		// simulated provider's busy flag before resuming parent-owned cleanup.
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, childRecord, out busy)) return 9;
		busy.Flags &= ~MuiHeadlessObjectCore.ObjectProviderBusy;
		if (!MuiHeadlessObjectCodec.Write(ref platform, childRecord, busy)) return 10;
		if (MuiHeadlessObjectCore.DisposeObject(ref platform, state, child)) return 16;
		if (MuiHeadlessObjectCore.DisposeObjectState(ref platform, state, child, false)) return 17;
		if (rootDisposal)
		{
			if (!MuiMasterLifecycleCore.Dispose(ref platform, root)) return 18;
			if (!MuiMasterPrivateRootCodec.TryRead(ref platform, root, out var cleared) ||
				cleared.ClassRegistry != 0) return 19;
			return 42;
		}
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform, state, parent)) return 11;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, parent).IsNotNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, child).IsNotNull) return 12;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, cl, out var released) ||
			released.ObjectCount != 0) return 13;
		if (!MuiMasterLifecycleCore.Dispose(ref platform, root)) return 14;
		return 42;
	}
}
