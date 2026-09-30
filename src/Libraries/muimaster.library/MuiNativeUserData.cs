/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// The native UserData walk uses a guest-resident stack of complete frames.
// The public registry is a linked ownership list, so NextBinding is the named
// cursor into that list used to enumerate children by their Parent field. No
// managed traversal object or native-object layout offset is retained.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeUDataTraversalFrame
{
	internal const uint Size = 12;
	internal APTR Object;
	internal APTR NextBinding;
	internal uint Expanded;
}

internal static class MuiNativeUDataTraversalFrameCodec
{
	internal static bool TryRead<T>(ref T memory, APTR address,
		out MuiNativeUDataTraversalFrame value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeUDataTraversalFrame.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var nextBinding) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.Expanded) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Object = APTR.FromPointer(obj);
		value.NextBinding = APTR.FromPointer(nextBinding);
		return value.Expanded <= 1;
	}

	internal static bool Write<T>(ref T memory, APTR address,
		MuiNativeUDataTraversalFrame value)
		where T : struct, IMuiGuestMemory =>
		value.Expanded <= 1 &&
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeUDataTraversalFrame.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.NextBinding.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Expanded) &&
		MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiNativeUserDataCore
{
	private const uint UserDataAttribute = 0x80420313;
	private const uint NotVisited = 0;
	private const uint Expanded = 1;
	private const uint MaximumDepth = 256;

	internal static bool FindObject(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR root, APTR findme)
	{
		if (root.IsNull || findme.IsNull ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, root, out _) ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, findme, out _)) return false;
		var current = findme;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (current == root) return true;
			if (!TryGetSidecar(ref platform, publicObjects, ownerRoot,
				current, out var binding)) return false;
			var memory = default(MuiNativeClassMemory);
			if (!MuiNativeMuiObjectCodec.TryRead(ref memory, binding.Sidecar,
				out var sidecar)) return false;
			current = sidecar.Parent;
		}
		return false;
	}

	internal static APTR Find(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR root, uint userData)
	{
		if (!TryBegin(ref platform, publicObjects, ownerRoot, root,
			out var stack, out var stackBytes)) return APTR.Null;
		var memory = default(MuiNativeClassMemory);
		var depth = 1u;
		var visited = 0u;
		while (depth != 0)
		{
			if (!TryReadFrame(ref memory, stack, depth, out var frameAddress,
				out var frame)) return Finish(ref platform, stack, stackBytes,
				APTR.Null);
			if (frame.Expanded == NotVisited)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
					!MarkExpanded(ref memory, frameAddress, ref frame))
					return Finish(ref platform, stack, stackBytes, APTR.Null);
				if (Matches(ref platform, publicObjects, ownerRoot,
					frame.Object, userData))
					return Finish(ref platform, stack, stackBytes, frame.Object);
			}
			if (!Descend(ref platform, ref memory, publicObjects, ownerRoot,
				frameAddress, ref frame, ref depth, stack))
				return Finish(ref platform, stack, stackBytes, APTR.Null);
		}
		return Finish(ref platform, stack, stackBytes, APTR.Null);
	}

	internal static bool Get(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR root, uint userData,
		uint attribute, APTR storage)
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiGuestUlongStorage.Size) ||
			!TryBegin(ref platform, publicObjects, ownerRoot, root,
				out var stack, out var stackBytes)) return false;
		var memory = default(MuiNativeClassMemory);
		var depth = 1u;
		var visited = 0u;
		while (depth != 0)
		{
			if (!TryReadFrame(ref memory, stack, depth, out var frameAddress,
				out var frame)) return Finish(ref platform, stack, stackBytes,
				false);
			if (frame.Expanded == NotVisited)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
					!MarkExpanded(ref memory, frameAddress, ref frame))
					return Finish(ref platform, stack, stackBytes, false);
				if (TryGetUserData(ref platform, publicObjects, ownerRoot,
					frame.Object, out var currentUserData) &&
					currentUserData == userData)
				{
					if (!TryGetSidecar(ref platform, publicObjects, ownerRoot,
						frame.Object, out var sidecar) ||
						!MuiNativeObjectStateCore.TryGetAttribute(ref memory,
							sidecar.Sidecar, attribute, out var value) ||
						!MuiGuestUlongStorageCodec.WriteValue(ref platform, storage,
							value))
						return Finish(ref platform, stack, stackBytes, false);
					return Finish(ref platform, stack, stackBytes, true);
				}
			}
			if (!Descend(ref platform, ref memory, publicObjects, ownerRoot,
				frameAddress, ref frame, ref depth, stack))
				return Finish(ref platform, stack, stackBytes, false);
		}
		return Finish(ref platform, stack, stackBytes, false);
	}

	internal static bool Set(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR root, uint userData,
		uint attribute, uint value, bool once)
	{
		if (!TryBegin(ref platform, publicObjects, ownerRoot, root,
			out var stack, out var stackBytes)) return false;
		var memory = default(MuiNativeClassMemory);
		var depth = 1u;
		var visited = 0u;
		var matched = false;
		while (depth != 0)
		{
			if (!TryReadFrame(ref memory, stack, depth, out var frameAddress,
				out var frame)) return Finish(ref platform, stack, stackBytes,
				false);
			if (frame.Expanded == NotVisited)
			{
				if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
					!MarkExpanded(ref memory, frameAddress, ref frame))
					return Finish(ref platform, stack, stackBytes, false);
				if (TryGetUserData(ref platform, publicObjects, ownerRoot,
					frame.Object, out var currentUserData) &&
					currentUserData == userData)
				{
					if (!TryGetSidecar(ref platform, publicObjects, ownerRoot,
						frame.Object, out var sidecar) ||
						!MuiNativeObjectStateCore.SetAttribute(ref platform,
							sidecar.Sidecar, attribute, value, true))
						return Finish(ref platform, stack, stackBytes, false);
					matched = true;
					if (once) return Finish(ref platform, stack, stackBytes, true);
				}
			}
			if (!Descend(ref platform, ref memory, publicObjects, ownerRoot,
				frameAddress, ref frame, ref depth, stack))
				return Finish(ref platform, stack, stackBytes, false);
		}
		return Finish(ref platform, stack, stackBytes, matched);
	}

	private static bool TryBegin(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR root, out APTR stack,
		out uint stackBytes)
	{
		stack = APTR.Null;
		stackBytes = MaximumDepth * MuiNativeUDataTraversalFrame.Size;
		if (publicObjects.IsNull || ownerRoot.IsNull || root.IsNull ||
			!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
				ownerRoot, root, out _)) return false;
		stack = platform.Allocate(stackBytes, MuiHeadlessLayout.AllocationFlags);
		if (stack.IsNull || !platform.IsMapped(stack, stackBytes))
		{
			if (stack.IsNotNull) platform.Free(stack, stackBytes);
			stack = APTR.Null;
			return false;
		}
		platform.Clear(stack, stackBytes);
		var memory = default(MuiNativeClassMemory);
		if (!TryGetFrameAddress(ref memory, stack, 0, out var frameAddress))
		{
			platform.Free(stack, stackBytes);
			stack = APTR.Null;
			return false;
		}
		var frame = default(MuiNativeUDataTraversalFrame);
		frame.Object = root;
		frame.NextBinding = APTR.Null;
		frame.Expanded = NotVisited;
		if (!MuiNativeUDataTraversalFrameCodec.Write(ref memory, frameAddress,
			frame))
		{
			platform.Free(stack, stackBytes);
			stack = APTR.Null;
			return false;
		}
		return true;
	}

	private static bool TryReadFrame(ref MuiNativeClassMemory memory,
		APTR stack, uint depth, out APTR address,
		out MuiNativeUDataTraversalFrame frame)
	{
		address = APTR.Null;
		frame = default;
		if (depth == 0 || depth > MaximumDepth) return false;
		if (!TryGetFrameAddress(ref memory, stack, depth - 1, out address))
			return false;
		return MuiNativeUDataTraversalFrameCodec.TryRead(ref memory, address,
			out frame) && frame.Object.IsNotNull;
	}

	private static bool MarkExpanded(ref MuiNativeClassMemory memory,
		APTR address, ref MuiNativeUDataTraversalFrame frame)
	{
		frame.Expanded = Expanded;
		return MuiNativeUDataTraversalFrameCodec.Write(ref memory, address,
			frame);
	}

	private static bool Descend(ref MuiNativeClassPlatform platform,
		ref MuiNativeClassMemory memory, APTR publicObjects, APTR ownerRoot,
		APTR frameAddress, ref MuiNativeUDataTraversalFrame frame,
		ref uint depth, APTR stack)
	{
		if (!TryFindNextChild(ref platform, publicObjects, ownerRoot,
			frame.Object, frame.NextBinding, out var bindingAddress,
			out var child, out var found)) return false;
		if (found)
		{
			frame.NextBinding = bindingAddress;
			if (!MuiNativeUDataTraversalFrameCodec.Write(ref memory, frameAddress,
				frame) || depth >= MaximumDepth) return false;
			depth++;
			if (!TryGetFrameAddress(ref memory, stack, depth - 1,
				out var address)) return false;
			var childFrame = default(MuiNativeUDataTraversalFrame);
			childFrame.Object = child;
			return MuiNativeUDataTraversalFrameCodec.Write(ref memory, address,
				childFrame);
		}
		depth--;
		return true;
	}

	// This is the bounded vector adapter for the named frame record. The only
	// arithmetic here derives an element address from the complete record size;
	// no field is addressed by a consumer-visible native-object offset.
	private static bool TryGetFrameAddress(ref MuiNativeClassMemory memory,
		APTR stack, uint index, out APTR address)
	{
		address = APTR.Null;
		if (stack.IsNull || index >= MaximumDepth ||
			index > (uint.MaxValue - MuiNativeUDataTraversalFrame.Size) /
				MuiNativeUDataTraversalFrame.Size) return false;
		var offset = index * MuiNativeUDataTraversalFrame.Size;
		if (stack.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(stack.Raw + offset);
		return memory.IsMapped(address, MuiNativeUDataTraversalFrame.Size);
	}

	private static bool TryFindNextChild(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR parent, APTR afterBinding,
		out APTR bindingAddress, out APTR child, out bool found)
	{
		bindingAddress = APTR.Null;
		child = APTR.Null;
		found = false;
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectRegistryCodec.TryRead(ref memory,
			publicObjects, out var registry)) return false;
		var current = registry.Head;
		var afterReached = afterBinding.IsNull;
		uint visited = 0;
		while (current.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiNativePublicObjectBindingCodec.TryRead(ref memory, current,
				out var candidate) || candidate.Signature !=
				MuiNativePublicObjectBinding.Magic || candidate.OwnerRoot != ownerRoot)
				return false;
			if (!afterReached)
			{
				if (current == afterBinding) afterReached = true;
				current = candidate.Next;
				continue;
			}
			if (candidate.DisposeState == MuiNativePublicObjectBinding.StateLive &&
				candidate.Parent == parent)
			{
				if (!MuiNativePublicObjectCore.TryFindLive(ref platform,
					publicObjects, ownerRoot, candidate.Object, out _)) return false;
				bindingAddress = current;
				child = candidate.Object;
				found = true;
				return true;
			}
			current = candidate.Next;
		}
		return current.IsNull && (afterReached || afterBinding.IsNull);
	}

	private static bool Matches(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, uint userData) =>
		TryGetUserData(ref platform, publicObjects, ownerRoot, obj,
			out var value) && value == userData;

	private static bool TryGetUserData(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj, out uint value)
	{
		value = 0;
		if (!TryGetSidecar(ref platform, publicObjects, ownerRoot, obj,
			out var binding)) return false;
		var memory = default(MuiNativeClassMemory);
		return MuiNativeObjectStateCore.TryGetAttribute(ref memory,
			binding.Sidecar, UserDataAttribute, out value);
	}

	private static bool TryGetSidecar(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR obj,
		out MuiNativePublicObjectBinding binding)
	{
		binding = default;
		return MuiNativePublicObjectCore.TryFindLive(ref platform,
			publicObjects, ownerRoot, obj, out binding) &&
			binding.Sidecar.IsNotNull;
	}

	private static T Finish<T>(ref MuiNativeClassPlatform platform, APTR stack,
		uint stackBytes, T result)
	{
		platform.Clear(stack, stackBytes);
		platform.Free(stack, stackBytes);
		return result;
	}
}
