using Amiga;

namespace CopperOS.MuiMaster.NativeRoot;

// Simulated retain provider: generated construction ownership and cleanup,
// not real native Intuition callbacks or physical allocator reuse.
public static class MuiNativeConstructionTransferRegression
{
	public static uint ConstructionTransferRoot()
	{
		var accepted = RunTransfer(0);
		if (accepted != 42) return accepted;
		return RunTransfer(1);
	}

	public static uint ReservedCreationRoot()
	{
		var accepted = RunTransfer(2);
		if (accepted != 42) return accepted;
		return RunTransfer(3);
	}

	private static uint RunTransfer(uint mode)
	{
		var refuse = mode & 1;
		var reservedCreation = (mode & 2) != 0;
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
		if (parent.IsNull) return 3;
		var owner = MuiHeadlessObjectCore.FindObject(ref platform, state, parent);
		if (!MuiConstructionChildSlotsCore.Reserve(ref platform, owner, out var slots)) return 4;
		var child = reservedCreation ?
			MuiConstructionChildSlotsCore.CreateOwned(ref platform, state, owner, slots.Second, cl) :
			MuiHeadlessObjectCore.CreateObjectA(ref platform, state, cl, APTR.Null);
		if (child.IsNull) return 3;
		if (reservedCreation)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, slots.Second, out var registered) ||
				registered.Object.IsNull) return 13;
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, registered.Object, out var beforeBind) ||
				beforeBind.Boopsi.Raw != child.Raw ||
				(beforeBind.Flags & MuiHeadlessObjectCore.ObjectInitialized) == 0) return 14;
			if (MuiConstructionChildSlotsCore.CreateOwned(ref platform, state, owner,
				slots.Second, cl).IsNotNull) return 15;
			if (!MuiHeadlessClassCodec.TryRead(ref platform, cl, out var beforeCount) ||
				beforeCount.ObjectCount != 2) return 16;
		}
		platform.RefuseConstructionRetain = refuse;
		var bound = MuiConstructionChildSlotsCore.BindCreated(ref platform, state,
			owner, slots.Second, child, out var transferred);
		if (!transferred || bound != (refuse == 0)) return 5;
		if (!MuiHeadlessChildCodec.TryRead(ref platform, slots.Second, out var link) ||
			link.Object.IsNull || link.Owner.Raw != owner.Raw) return 6;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, link.Object, out var childState) ||
			childState.Parent.Raw != owner.Raw ||
			(childState.Flags & MuiHeadlessObjectCore.ObjectConstructionBinding) != 0) return 7;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner, out var ownerState) ||
			(ownerState.Flags & MuiHeadlessObjectCore.ObjectConstructionBinding) != 0) return 8;
		if (!MuiHeadlessObjectCore.DisposeObject(ref platform, state, parent)) return 9;
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state, out var registry) ||
			registry.Objects.IsNotNull) return 10;
		if (!MuiHeadlessClassCodec.TryRead(ref platform, cl, out var classState) ||
			classState.ObjectCount != 0) return 11;
		if (!MuiMasterLifecycleCore.Dispose(ref platform, root)) return 12;
		return 42;
	}
}
