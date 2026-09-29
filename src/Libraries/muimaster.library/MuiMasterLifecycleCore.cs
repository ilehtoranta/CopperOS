/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

public static class MuiMasterLifecycleCore
{
	internal const uint RootDisposing = 1;

	public static bool Create<TPlatform>(ref TPlatform platform, APTR privateRoot,
		APTR headlessState) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (privateRoot.IsNull ||
			!platform.IsMapped(privateRoot, MuiMasterPrivateRoot.Size) ||
			!MuiHeadlessObjectCore.Initialize(ref platform, headlessState))
			return false;
		var root = default(MuiMasterPrivateRoot);
		root.ClassRegistry = headlessState.Raw;
		return MuiMasterPrivateRootCodec.Write(ref platform, privateRoot, root);
	}

	public static bool Dispose<TPlatform>(ref TPlatform platform,
		APTR privateRoot) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (privateRoot.IsNull ||
			!platform.IsMapped(privateRoot, MuiMasterPrivateRoot.Size)) return false;
		if (!MuiMasterPrivateRootCodec.TryRead(ref platform, privateRoot,
			out var root)) return false;
		// Receipts can own a native object not yet visible in the object registry.
		// Never clear that recovery authority during ordinary root teardown.
		if (root.PendingConstructionHead.IsNotNull || (root.Flags & RootDisposing) != 0) return false;
		var state = APTR.FromPointer(root.ClassRegistry);
		if (state.IsNull || !MuiHeadlessStateCodec.TryRead(ref platform, state,
			out _))
			return false;
		if (!MuiMasterPrivateRootRecordMemoryCodec.TryGetAddress(ref platform, privateRoot,
			MuiMasterPrivateRootField.Flags, out var flags)) return false;
		// The caller keeps root storage alive. Admit its named flag field before
		// any cleanup callbacks; recursive teardown and receipt creation must not
		// race the final root clear. A refused cleanup releases this operation bit.
		platform.WriteUInt32(flags, 0, root.Flags | RootDisposing);
		var disposed = DisposeContents(ref platform, privateRoot, state);
		if (!disposed)
			platform.WriteUInt32(flags, 0, platform.ReadUInt32(flags, 0) & ~RootDisposing);
		return disposed;
	}

	private static bool DisposeContents<TPlatform>(ref TPlatform platform,
		APTR privateRoot, APTR state) where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint visited = 0;
		while (true)
		{
			if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
				out var stateValue)) return false;
			if (stateValue.Objects.IsNull) break;
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal) return false;
			if (!TrySelectDisposalRoot(ref platform, stateValue.Objects,
				out var current)) return false;
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, current,
				out var objectValue)) return false;
			var boopsi = objectValue.Boopsi;
			if (!MuiHeadlessObjectCore.DisposeObject(ref platform, state, boopsi))
				return false;
		}

		visited = 0;
		while (true)
		{
			if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
				out var stateValue)) return false;
			if (stateValue.Classes.IsNull) break;
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal) return false;
			var current = stateValue.Classes;
			if (!MuiHeadlessObjectCore.DeleteClass(ref platform, state, current))
				return false;
		}

		platform.Clear(state, MuiHeadlessStateRecord.Size);
		platform.Clear(privateRoot, MuiMasterPrivateRoot.Size);
		return true;
	}

	// Pending cleanup clears the child's Parent to prevent it from unlinking
	// the retained link. Therefore the owning lists, not Parent or registry
	// insertion order, remain authoritative during root teardown.
	private static bool TrySelectDisposalRoot<TPlatform>(ref TPlatform platform,
		APTR objects, out APTR selected)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		selected = objects;
		for (uint depth = 0; depth < MuiHeadlessLayout.MaximumTraversal; depth++)
		{
			var owner = APTR.Null;
			var current = objects;
			uint visited = 0;
			while (current.IsNotNull)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
					!MuiHeadlessObjectCodec.TryRead(ref platform, current,
						out var value)) return false;
				var link = value.ChildrenHead;
				uint children = 0;
				while (link.IsNotNull)
				{
					if (children++ >= MuiHeadlessLayout.MaximumTraversal ||
						!MuiHeadlessChildCodec.TryRead(ref platform, link,
							out var child)) return false;
					if (child.Object.Raw == selected.Raw)
					{
						// Multiple owning links or self ownership are corrupt.
						if (owner.IsNotNull || current.Raw == selected.Raw)
							return false;
						owner = current;
					}
					link = child.Next;
				}
				current = value.Next;
			}
			if (owner.IsNull) return true;
			selected = owner;
		}
		return false;
	}
}
