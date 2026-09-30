/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Normalized coordinate pair copied from an IntuiMessage or the named
// Intuition Window record. Keep hit testing independent of guest-memory field
// offsets and of which source supplied the current pointer sample.
internal struct MuiNativePointerPosition
{
	internal int X;
	internal int Y;

	internal static MuiNativePointerPosition FromMessage(
		MuiIntuiPointerMessage message) => new()
		{
			X = message.MouseX,
			Y = message.MouseY,
		};

	internal static MuiNativePointerPosition FromWindow(Window window) => new()
	{
		X = window.MouseX,
		Y = window.MouseY,
	};
}

// Resolve MUIA_Window_MouseObject from public Area geometry and the
// library-owned parent registry. The native Window, IntuiMessage, object
// bindings, rectangles, and traversal limits all use named SDK/project
// records; no private BOOPSI fields or object-layout offsets are inspected.
internal static class MuiNativeWindowPointerHitTest
{
	internal static bool TryResolve(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR windowObject,
		APTR sidecarAddress, APTR intuiMessage, out APTR mouseObject)
	{
		mouseObject = APTR.Null;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
			ownerRoot, windowObject, out var windowBinding) ||
			windowBinding.Sidecar != sidecarAddress ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out var sidecar) || sidecar.NativeWindow.IsNull ||
			!memory.IsMapped(sidecar.NativeWindow, Window.Size) ||
			!MuiIntuiMessageCodec.TryReadPointerRecord(ref memory, intuiMessage,
				out var message) || message.Class == 0 ||
			!TryResolve(ref platform, publicObjects, ownerRoot, windowObject,
				sidecarAddress, MuiNativePointerPosition.FromMessage(message),
				out mouseObject)) return false;
		return true;
	}

	internal static bool TryResolve(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR windowObject,
		APTR sidecarAddress, MuiNativePointerPosition pointer,
		out APTR mouseObject)
	{
		mouseObject = APTR.Null;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
			ownerRoot, windowObject, out var windowBinding) ||
			windowBinding.Sidecar != sidecarAddress ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, sidecarAddress,
				out var sidecar) || sidecar.NativeWindow.IsNull ||
			!memory.IsMapped(sidecar.NativeWindow, Window.Size) ||
			!MuiNativePublicObjectRegistryCodec.TryRead(ref memory,
				publicObjects, out var registry)) return false;

		var storage = platform.Allocate(MuiGuestUlongStorage.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (storage.IsNull) return false;
		var resolved = APTR.Null;
		var bestDepth = 0u;
		var found = false;
		var current = registry.Head;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var binding) || binding.Signature !=
				MuiNativePublicObjectBinding.Magic || binding.OwnerRoot != ownerRoot)
				break;
			var next = binding.Next;
			if (binding.DisposeState == MuiNativePublicObjectBinding.StateLive &&
				binding.Object != windowObject && TryGetDepth(ref memory,
					publicObjects, ownerRoot, windowObject, binding.Object,
					out var depth) && (!found || depth >= bestDepth) &&
				MuiNativeGuiMode.IsVisibleForPointer(ref platform, publicObjects,
					ownerRoot, windowObject, binding.Object, storage) &&
				IsPointWithinObjectTree(ref platform, publicObjects, ownerRoot,
					windowObject, binding.Object, pointer.X, pointer.Y,
					storage))
			{
				// Deepest nested object always wins. For equal-depth overlaps,
				// honor the current public Group child order when the paths share a
				// Group parent. Keep registry order only as a deterministic fallback
				// when the public hierarchy does not provide a Group sibling list.
				if (!found || depth > bestDepth || depth == bestDepth &&
					IsLaterInGroupPaintOrder(ref platform, publicObjects, ownerRoot,
						binding.Object, resolved, storage))
				{
					resolved = binding.Object;
					bestDepth = depth;
					found = true;
				}
			}
			current = next;
		}
		var complete = current.IsNull && visited <=
			MuiHeadlessLayout.MaximumTraversal &&
			MuiNativePublicObjectRegistryCodec.TryRead(ref memory, publicObjects,
				out var currentRegistry) && currentRegistry.Mutation == registry.Mutation;
		platform.Free(storage, MuiGuestUlongStorage.Size);
		if (!complete) return false;
		mouseObject = resolved;
		return true;
	}

	private static bool TryGetDepth<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR windowObject, APTR obj,
		out uint depth)
		where TMemory : struct, IMuiGuestMemory
	{
		depth = 0;
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && current != windowObject && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, current, out var binding) || binding.Parent.IsNull)
				return false;
			current = binding.Parent;
			depth++;
		}
		return current == windowObject && depth != 0;
	}

	private static bool IsLaterInGroupPaintOrder(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR candidate, APTR incumbent, APTR storage)
	{
		var memory = default(MuiNativeClassMemory);
		var candidateBranch = candidate;
		var incumbentBranch = incumbent;
		uint visited = 0;
		while (candidateBranch.IsNotNull && incumbentBranch.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, candidateBranch, out var candidateBinding) ||
				!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
					ownerRoot, incumbentBranch, out var incumbentBinding)) return false;
			var candidateParent = candidateBinding.Parent;
			var incumbentParent = incumbentBinding.Parent;
			if (candidateParent.IsNull || incumbentParent.IsNull) return false;
			if (candidateParent == incumbentParent)
				return MuiNativeGuiMode.IsLaterGroupChildInList(ref platform,
					candidateParent, candidateBranch, incumbentBranch, storage);
			candidateBranch = candidateParent;
			incumbentBranch = incumbentParent;
		}
		return false;
	}

	private static bool IsPointWithinObjectTree(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR windowObject, APTR obj, int x, int y,
		APTR storage)
	{
		var memory = default(MuiNativeClassMemory);
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && current != windowObject && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, current, out var binding) ||
				!MuiNativeGuiMode.TryReadAreaRectangle(ref platform, current,
					storage, out var rectangle) ||
				!MuiNativeGuiMode.ContainsPoint(rectangle, x, y)) return false;
			current = binding.Parent;
		}
		return current == windowObject;
	}
}
