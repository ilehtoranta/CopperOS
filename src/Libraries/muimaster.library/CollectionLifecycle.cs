/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Keeps MG08 teardown out of the frozen generic object closure. Collection
// class dispatchers call this entry point for OM_DISPOSE; it pre-cleans every
// collection object in the owned child tree, then delegates structural object
// teardown to the shared headless core.
public static class MuiCollectionLifecycle
{
	public static bool DisposeObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		CleanupTree(ref platform, state, record, 0);
		return MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
	}

	private static void CleanupTree<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint depth)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (record.IsNull || depth >= MuiHeadlessLayout.MaximumTraversal ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue)) return;
		var childNode = objectValue.ChildrenHead;
		uint visited = 0;
		while (childNode.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, childNode,
				out var childValue)) break;
			var child = childValue.Object;
			CleanupTree(ref platform, state, child, depth + 1);
			childNode = childValue.Next;
		}
		var obj = objectValue.Boopsi;
		if (MuiListtreeCore.IsListtree(ref platform, state, obj))
			MuiListtreeCore.CleanupRecords(ref platform, state, obj);
		else if (MuiListCore.Classify(ref platform, state, obj) ==
			MuiCollectionClass.Listview)
			MuiListviewCore.CleanupRecords(ref platform, state, obj);
		else if (MuiListCore.IsListBacked(MuiListCore.Classify(ref platform,
			state, obj)))
			MuiListCore.CleanupRecords(ref platform, state, obj);
	}
}
