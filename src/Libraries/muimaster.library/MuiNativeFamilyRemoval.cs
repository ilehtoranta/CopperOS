/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeFamilyRemoveMessageRecord
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal APTR Object;
}

internal static class MuiNativeFamilyRemoveMessageCodec
{
	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiNativeFamilyRemoveMessageRecord value)
		where TMemory : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeFamilyRemoveMessageRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Object.Raw) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiNativeFamilyRemoveMessageRecord value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeFamilyRemoveMessageRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var objectRaw) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Object = APTR.FromPointer(objectRaw);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeFamilyListRecord
{
	internal const uint Size = 12;
	internal APTR Head;
	internal APTR Tail;
	internal APTR TailPred;
}

// A host-side snapshot of the collection exposed by a Family/Group parent.
// Keeping the collection identity with the membership result lets the caller
// detect a reentrant replacement instead of mistaking absence from a new list
// for detachment from the original one.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNativeFamilyChildMembershipRecord
{
	internal APTR List;
	internal uint ContainsChild;
}

internal static class MuiNativeFamilyListCodec
{
	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiNativeFamilyListRecord value)
		where TMemory : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeFamilyListRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Head.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Tail.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.TailPred.Raw) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiNativeFamilyListRecord value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiNativeFamilyListRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var head) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var tail) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var tailPred) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Head = APTR.FromPointer(head);
		value.Tail = APTR.FromPointer(tail);
		value.TailPred = APTR.FromPointer(tailPred);
		return true;
	}
}

// Family removal is ownership-sensitive. The public method result is not
// specified by MorphOS, so the native bridge verifies removal through the
// documented Family/List attribute and Intuition.NextObject before it updates
// its named parent/child records or disposes the child.
internal static class MuiNativeFamilyRemovalCore
{
	internal static bool TryRemove(ref MuiNativeClassPlatform platform,
		APTR parent, APTR child, uint listAttribute)
	{
		if (parent.IsNull || child.IsNull || platform.IntuitionBase.IsNull ||
			(listAttribute != MuiNativeGuiMode.FamilyList &&
			 listAttribute != MuiNativeGuiMode.GroupChildList)) return false;
		var scratch = platform.Allocate(MuiNativeFamilyRemoveMessageRecord.Size,
			MuiHeadlessLayout.AllocationFlags);
		if (scratch.IsNull) return false;
		if (!TryReadMembership(ref platform, parent, child, listAttribute,
			scratch, out var before) || before.ContainsChild == 0)
		{
			platform.Free(scratch, MuiNativeFamilyRemoveMessageRecord.Size);
			return false;
		}
		var message = default(MuiNativeFamilyRemoveMessageRecord);
		message.MethodId = MuiFamilyMutationCore.RemoveMethod;
		message.Object = child;
		if (!MuiNativeFamilyRemoveMessageCodec.Write(ref platform, scratch,
			message))
		{
			platform.Free(scratch, MuiNativeFamilyRemoveMessageRecord.Size);
			return false;
		}
		platform.DoMethod(parent, scratch);
		var removed = TryReadMembership(ref platform, parent, child,
			listAttribute, scratch, out var after) &&
			ConfirmsRemovalFromSameList(before, after);
		platform.Free(scratch, MuiNativeFamilyRemoveMessageRecord.Size);
		return removed;
	}

	internal static bool ConfirmsRemovalFromSameList(
		MuiNativeFamilyChildMembershipRecord before,
		MuiNativeFamilyChildMembershipRecord after) =>
		before.List.IsNotNull && before.List == after.List &&
		before.ContainsChild == 1 && after.ContainsChild == 0;

	private static bool TryReadMembership(ref MuiNativeClassPlatform platform,
		APTR parent, APTR child, uint listAttribute, APTR scratch,
		out MuiNativeFamilyChildMembershipRecord membership)
	{
		membership = default;
		if (!MuiGuestUlongStorageCodec.WriteValue(ref platform, scratch, 0) ||
			MuiNativeIntuitionCalls.GetAttr(platform.IntuitionBase,
				listAttribute, parent, scratch) == 0 ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, scratch,
				out var listRaw) || listRaw == 0) return false;
		var listAddress = APTR.FromPointer(listRaw);
		var memory = default(MuiNativeClassMemory);
		if (!TryReadListHead(ref memory, listAddress, listAttribute,
			out var listHead)) return false;
		membership.List = listAddress;
		var iterator = default(MuiNativeNextObjectStateRecord);
		iterator.Current = listHead;
		if (!MuiNativeNextObjectStateCodec.Write(ref platform, scratch,
			iterator)) return false;
		for (uint visited = 0; visited < MuiHeadlessLayout.MaximumTraversal;
			visited++)
		{
			var current = MuiNativeIntuitionCalls.NextObject(
				platform.IntuitionBase, scratch);
			if (current.IsNull) return true;
			if (current == child)
			{
				membership.ContainsChild = 1;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadListHead<TMemory>(ref TMemory memory,
		APTR listAddress, uint listAttribute, out APTR head)
		where TMemory : struct, IMuiGuestMemory
	{
		head = APTR.Null;
		if (listAttribute == MuiNativeGuiMode.GroupChildList)
		{
			if (!MuiGroupExecListCodec.TryRead(ref memory, listAddress,
				out var groupList)) return false;
			head = groupList.Head;
			return true;
		}
		if (listAttribute != MuiNativeGuiMode.FamilyList ||
			!MuiNativeFamilyListCodec.TryRead(ref memory, listAddress,
				out var familyList)) return false;
		head = familyList.Head;
		return true;
	}
}
