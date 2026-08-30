/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The MorphOS Family_AddHead/AddTail/Remove methods share the fixed
// {MethodID, object} packet. Keep that ABI boundary explicit rather than
// making the dispatcher know that the object lives at byte offset four.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyChildMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint ObjectOffset = 4;
	internal uint MethodId;
	internal APTR Object;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyInsertMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint ObjectOffset = 4;
	internal const uint PredecessorOffset = 8;
	internal uint MethodId;
	internal APTR Object;
	internal APTR Predecessor;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyTransferMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint FamilyOffset = 4;
	internal uint MethodId;
	internal APTR Family;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyReorderMessage
{
	internal const uint HeaderSize = 8;
	internal const uint MinimumSize = 12;
	internal const uint ArrayOffset = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint AfterOffset = 4;
	internal uint MethodId;
	internal APTR After;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilySortMessage
{
	internal const uint HeaderSize = 4;
	internal const uint MinimumSize = 8;
	internal const uint ArrayOffset = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

internal enum MuiFamilyPacketKind : byte
{
	Method,
	Child,
	Insert,
	Transfer,
	Reorder,
	Sort,
}

internal enum MuiFamilyPacketField : byte
{
	MethodId,
	Object,
	Predecessor,
	Family,
	After,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyPacketFieldCursor
{
	internal APTR Message;
	internal MuiFamilyPacketKind Packet;
	internal MuiFamilyPacketField Field;
}

internal static class MuiFamilyPacketMemoryCodec
{
	private static bool TryResolve(MuiFamilyPacketKind packet,
		MuiFamilyPacketField field, out uint offset, out uint packetSize)
	{
		offset = 0;
		packetSize = 0;
		if (packet == MuiFamilyPacketKind.Method ||
			packet == MuiFamilyPacketKind.Sort)
		{
			if (field != MuiFamilyPacketField.MethodId) return false;
			offset = packet == MuiFamilyPacketKind.Method
				? MuiFamilyMethodMessage.MethodIdOffset
				: MuiFamilySortMessage.MethodIdOffset;
			packetSize = packet == MuiFamilyPacketKind.Method
				? MuiFamilyMethodMessage.Size
				: MuiFamilySortMessage.MinimumSize;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Child)
		{
			packetSize = MuiFamilyChildMessage.Size;
			if (field == MuiFamilyPacketField.MethodId)
				offset = MuiFamilyChildMessage.MethodIdOffset;
			else if (field == MuiFamilyPacketField.Object)
				offset = MuiFamilyChildMessage.ObjectOffset;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Insert)
		{
			packetSize = MuiFamilyInsertMessage.Size;
			if (field == MuiFamilyPacketField.MethodId)
				offset = MuiFamilyInsertMessage.MethodIdOffset;
			else if (field == MuiFamilyPacketField.Object)
				offset = MuiFamilyInsertMessage.ObjectOffset;
			else if (field == MuiFamilyPacketField.Predecessor)
				offset = MuiFamilyInsertMessage.PredecessorOffset;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Transfer)
		{
			packetSize = MuiFamilyTransferMessage.Size;
			if (field == MuiFamilyPacketField.MethodId)
				offset = MuiFamilyTransferMessage.MethodIdOffset;
			else if (field == MuiFamilyPacketField.Family)
				offset = MuiFamilyTransferMessage.FamilyOffset;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Reorder)
		{
			packetSize = MuiFamilyReorderMessage.MinimumSize;
			if (field == MuiFamilyPacketField.MethodId)
				offset = MuiFamilyReorderMessage.MethodIdOffset;
			else if (field == MuiFamilyPacketField.After)
				offset = MuiFamilyReorderMessage.AfterOffset;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyPacketKind packet, MuiFamilyPacketField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var packetSize) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, packetSize))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyPacketKind packet, MuiFamilyPacketField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyPacketKind packet, MuiFamilyPacketField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	// Keep method-header admission scalar in native lowering while the public
	// decoder continues to expose the named method record.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyMethodMessage.Size)) return false;
		value = platform.ReadUInt32(message,
			(int)MuiFamilyMethodMessage.MethodIdOffset);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Family packet cursor. Live mutation codecs use the direct memory adapter.
internal static class MuiFamilyPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFamilyPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFamilyPacketMemoryCodec.TryGetAddress(ref platform, cursor.Message,
			cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyPacketKind packet, MuiFamilyPacketField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform, message, packet,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyPacketKind packet, MuiFamilyPacketField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform, message, packet,
			field, value);
}

// Central codec for the fixed MorphOS Family mutation packet family. The
// public/core paths consume named records; only this adapter carries packed
// guest offsets, method validation, and bounded array-header mapping checks.
internal static class MuiFamilyMutationMessageCodec
{
	internal static bool TryGetVectorBase<TPlatform>(ref TPlatform platform,
		APTR message, uint vectorOffset, out APTR vector)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetVectorEntry(ref platform, message, vectorOffset, 0, out vector);

	internal static bool TryGetVectorEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint vectorOffset, uint index, out APTR entry)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyInlineVectorMemoryCodec.TryGetEntry(ref platform, message,
			vectorOffset, index, out entry);

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Native qualification keeps selector admission scalar while the
	// dispatcher-facing overload above retains the named value-type record.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		return MuiFamilyPacketMemoryCodec.TryReadMethodId(ref platform, message,
			out methodId);
	}

	internal static bool TryReadChild<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiFamilyChildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyChildMessage.Size) ||
			(method != MuiFamilyMutationCore.AddHeadMethod &&
				method != MuiFamilyMutationCore.AddTailMethod &&
				method != MuiFamilyMutationCore.RemoveMethod) ||
			!TryReadMethodIdValue(ref platform, message, out var methodId) ||
			methodId != method) return false;
		packet.MethodId = methodId;
		if (!MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform,
			message, MuiFamilyPacketKind.Child, MuiFamilyPacketField.Object,
			out var rawObject)) return false;
		packet.Object = APTR.FromPointer(rawObject);
		return true;
	}

	internal static bool WriteChild<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR child)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyChildMessage.Size) ||
			(method != MuiFamilyMutationCore.AddHeadMethod &&
				method != MuiFamilyMutationCore.AddTailMethod &&
				method != MuiFamilyMutationCore.RemoveMethod)) return false;
		return MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiFamilyPacketKind.Child, MuiFamilyPacketField.MethodId,
			method) &&
			MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiFamilyPacketKind.Child, MuiFamilyPacketField.Object,
				child.Raw);
	}

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyInsertMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out var methodId) ||
			methodId != MuiFamilyMutationCore.InsertMethod)
			return false;
		if (!MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform,
			message, MuiFamilyPacketKind.Insert, MuiFamilyPacketField.Object,
			out var rawObject) ||
			!MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform,
				message, MuiFamilyPacketKind.Insert,
				MuiFamilyPacketField.Predecessor, out var rawPredecessor))
			return false;
		packet.MethodId = methodId;
		packet.Object = APTR.FromPointer(rawObject);
		packet.Predecessor = APTR.FromPointer(rawPredecessor);
		return true;
	}

	internal static bool WriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, APTR child, APTR predecessor)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyInsertMessage.Size)) return false;
		return MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiFamilyPacketKind.Insert, MuiFamilyPacketField.MethodId,
			MuiFamilyMutationCore.InsertMethod) &&
			MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiFamilyPacketKind.Insert, MuiFamilyPacketField.Object,
				child.Raw) &&
			MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiFamilyPacketKind.Insert,
				MuiFamilyPacketField.Predecessor, predecessor.Raw);
	}

	internal static bool TryReadTransfer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyTransferMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyTransferMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out var methodId) ||
			methodId != MuiFamilyMutationCore.TransferMethod)
			return false;
		if (!MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform,
			message, MuiFamilyPacketKind.Transfer, MuiFamilyPacketField.Family,
			out var rawFamily)) return false;
		packet.MethodId = methodId;
		packet.Family = APTR.FromPointer(rawFamily);
		return true;
	}

	internal static bool WriteTransfer<TPlatform>(ref TPlatform platform,
		APTR message, APTR family)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyTransferMessage.Size)) return false;
		return MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiFamilyPacketKind.Transfer,
			MuiFamilyPacketField.MethodId, MuiFamilyMutationCore.TransferMethod) &&
			MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiFamilyPacketKind.Transfer, MuiFamilyPacketField.Family,
				family.Raw);
	}

	internal static bool TryReadReorder<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyReorderMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyReorderMessage.MinimumSize) ||
			message.Raw > uint.MaxValue - MuiFamilyReorderMessage.ArrayOffset ||
			!TryReadMethodIdValue(ref platform, message, out var methodId) ||
			methodId != MuiFamilyMutationCore.ReorderMethod)
			return false;
		if (!MuiFamilyPacketMemoryCodec.TryReadUInt32(ref platform,
			message, MuiFamilyPacketKind.Reorder, MuiFamilyPacketField.After,
			out var rawAfter)) return false;
		packet.MethodId = methodId;
		packet.After = APTR.FromPointer(rawAfter);
		return true;
	}

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilySortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilySortMessage.MinimumSize) ||
			message.Raw > uint.MaxValue - MuiFamilySortMessage.ArrayOffset ||
			!TryReadMethodIdValue(ref platform, message, out var methodId) ||
			methodId != MuiFamilyMutationCore.SortMethod)
			return false;
		packet.MethodId = methodId;
		return true;
	}

	internal static bool WriteReorder<TPlatform>(ref TPlatform platform,
		APTR message, APTR after)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyReorderMessage.MinimumSize)) return false;
		return MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiFamilyPacketKind.Reorder, MuiFamilyPacketField.MethodId,
			MuiFamilyMutationCore.ReorderMethod) &&
			MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiFamilyPacketKind.Reorder, MuiFamilyPacketField.After,
				after.Raw);
	}

	internal static bool WriteSort<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilySortMessage.MinimumSize)) return false;
		return MuiFamilyPacketMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiFamilyPacketKind.Sort, MuiFamilyPacketField.MethodId,
			MuiFamilyMutationCore.SortMethod);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyMutationListRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint HeadOffset = 0;
	internal const uint TailOffset = 4;
	internal APTR Head;
	internal APTR Tail;
}

internal enum MuiFamilyMutationListField : byte
{
	Head,
	Tail,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyMutationListFieldCursor
{
	internal APTR List;
	internal MuiFamilyMutationListField Field;
}

internal static class MuiFamilyMutationListMemoryCodec
{
	private static bool TryResolve(MuiFamilyMutationListField field,
		out uint offset, out uint recordSize)
	{
		recordSize = MuiFamilyMutationListRecord.Size;
		if (field == MuiFamilyMutationListField.Head)
		{
			offset = MuiFamilyMutationListRecord.HeadOffset;
			return true;
		}
		if (field == MuiFamilyMutationListField.Tail)
		{
			offset = MuiFamilyMutationListRecord.TailOffset;
			return true;
		}
		offset = 0;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR list, MuiFamilyMutationListField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var recordSize) ||
			list.IsNull || list.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(list, recordSize)) return false;
		address = APTR.FromPointer(list.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR list, MuiFamilyMutationListField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, list, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR list, MuiFamilyMutationListField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, list, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Family list cursor. Live list serialization uses the direct memory adapter.
internal static class MuiFamilyMutationListFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFamilyMutationListFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFamilyMutationListMemoryCodec.TryGetAddress(ref platform, cursor.List,
			cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR list, MuiFamilyMutationListField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFamilyMutationListMemoryCodec.TryRead(ref platform, list, field,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR list, MuiFamilyMutationListField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFamilyMutationListMemoryCodec.TryWrite(ref platform, list, field,
			value);
}

internal static class MuiFamilyMutationListCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiFamilyMutationListRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiFamilyMutationListRecord.Size)) return false;
		if (!MuiFamilyMutationListMemoryCodec.TryRead(ref platform, address,
			MuiFamilyMutationListField.Head, out var rawHead) ||
			!MuiFamilyMutationListMemoryCodec.TryRead(ref platform, address,
				MuiFamilyMutationListField.Tail, out var rawTail)) return false;
		record.Head = APTR.FromPointer(rawHead);
		record.Tail = APTR.FromPointer(rawTail);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiFamilyMutationListRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiFamilyMutationListRecord.Size)) return false;
		return MuiFamilyMutationListMemoryCodec.TryWrite(ref platform, address,
			MuiFamilyMutationListField.Head, record.Head.Raw) &&
			MuiFamilyMutationListMemoryCodec.TryWrite(ref platform, address,
				MuiFamilyMutationListField.Tail, record.Tail.Raw);
	}
}

// Named cursor for an inline pointer vector carried by a Family method packet.
// The packet header remains a fixed wire record; this view keeps the vector's
// base, element ordinal, and mapping/overflow policy together instead of
// repeating message-plus-offset arithmetic at each call site.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyInlineVectorCursor
{
	internal APTR Message;
	internal uint ArrayOffset;
	internal uint Index;
}

// Struct-first adapter for the inline object vectors carried by the Family
// Reorder and Sort packets. The packet header size is resolved from its named
// struct, then the selected vector entry is range-checked; raw offsets never
// escape this guest-memory boundary.
internal static class MuiFamilyInlineVectorMemoryCodec
{
	private static bool TryResolve(uint vectorOffset, out uint packetSize)
	{
		if (vectorOffset == MuiFamilyReorderMessage.ArrayOffset)
		{
			packetSize = MuiFamilyReorderMessage.HeaderSize;
			return true;
		}
		if (vectorOffset == MuiFamilySortMessage.ArrayOffset)
		{
			packetSize = MuiFamilySortMessage.HeaderSize;
			return true;
		}
		packetSize = 0;
		return false;
	}

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint vectorOffset, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(vectorOffset, out var packetSize) || message.IsNull ||
			!platform.IsMapped(message, packetSize) || message.Raw >
			uint.MaxValue - vectorOffset) return false;
		var vector = message.Raw + vectorOffset;
		if (index > (uint.MaxValue - vector) /
			MuiFamilyMutationVectorEntry.Size) return false;
		var offset = index * MuiFamilyMutationVectorEntry.Size;
		if (vector > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector + offset);
		return platform.IsMapped(address, MuiFamilyMutationVectorEntry.Size);
	}
}

internal static class MuiFamilyInlineVectorCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiFamilyInlineVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyInlineVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Message, cursor.ArrayOffset, cursor.Index, out address);
}

// A Family reorder/sort vector is an inline array of guest object pointers.
// Keep its one-word element as a named record so projection code never reads
// the vector payload as an anonymous offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyMutationVectorEntry
{
	internal const uint Size = 4;
	internal APTR Object;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyMutationVectorCursor
{
	internal const uint EntrySize = MuiFamilyMutationVectorEntry.Size;
	internal const uint MaximumEntries = 65535;
	internal APTR Base;
	internal uint Index;
}

// Struct-first guest-memory adapter for the NULL-terminated Family reorder
// and sort vectors. The bounded index and complete four-byte object record
// are admitted here; the cursor below is retained as a compatibility wrapper.
internal static class MuiFamilyMutationVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiFamilyMutationVectorCursor.MaximumEntries ||
			index > (uint.MaxValue - vector.Raw) /
			MuiFamilyMutationVectorEntry.Size) return false;
		var offset = index * MuiFamilyMutationVectorEntry.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiFamilyMutationVectorEntry.Size);
	}
}

// Canonical codec for one named Family reorder/sort vector entry.  The
// complete four-byte record is admitted before its Object member is decoded;
// callers outside this ABI boundary pass the struct rather than a field
// offset or compatibility cursor.
internal static class MuiFamilyMutationVectorEntryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiFamilyMutationVectorEntry record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiFamilyMutationVectorEntry.Size)) return false;
		record.Object = APTR.FromPointer(platform.ReadUInt32(address, 0));
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiFamilyMutationVectorEntry record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiFamilyMutationVectorEntry.Size)) return false;
		platform.WriteUInt32(address, 0, record.Object.Raw);
		return true;
	}
}

// Compatibility wrapper retained for callers that still model the vector as
// a cursor.  New production paths use the named entry codec directly.
internal static class MuiFamilyMutationVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiFamilyMutationVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiFamilyMutationVectorEntry record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationVectorEntryCodec.TryRead(ref platform, address,
			out record);

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiFamilyMutationVectorEntry record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationVectorEntryCodec.Write(ref platform, address, record);
}

internal static class MuiFamilyMutationChildRecord
{
	internal const uint Size = MuiHeadlessChildRecord.Size;
}

public static class MuiFamilyMutationCore
{
	public const uint AddHeadMethod = 0x8042E200;
	public const uint AddTailMethod = 0x8042D752;
	public const uint RemoveMethod = 0x8042F8A9;
	public const uint InsertMethod = 0x80424D34;
	public const uint TransferMethod = 0x8042C14A;
	public const uint ReorderMethod = 0x80426008;
	public const uint SortMethod = 0x80421C49;
	public const uint PacketSize = MuiFamilyChildMessage.Size;
	public const uint InsertPacketSize = MuiFamilyInsertMessage.Size;
	public const uint TransferPacketSize = MuiFamilyTransferMessage.Size;
	public const uint ReorderArrayOffset = MuiFamilyReorderMessage.ArrayOffset;
	public const uint SortArrayOffset = MuiFamilySortMessage.ArrayOffset;

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiFamilyChildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.TryReadChild(ref platform, message,
			method, out packet);

	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR child)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.WriteChild(ref platform, message,
			method, child);

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.TryReadInsert(ref platform, message,
			out packet);

	public static bool WriteInsertRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR child, APTR predecessor)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.WriteInsert(ref platform, message,
			child, predecessor);

	internal static bool TryReadTransfer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyTransferMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.TryReadTransfer(ref platform, message,
			out packet);

	public static bool WriteTransferRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR family)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.WriteTransfer(ref platform, message,
			family);

	internal static bool TryReadReorder<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyReorderMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.TryReadReorder(ref platform, message,
			out packet);

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilySortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.TryReadSort(ref platform, message,
			out packet);

	public static bool WriteReorderRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR after)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.WriteReorder(ref platform, message,
			after);

	public static bool WriteSortRecord<TPlatform>(ref TPlatform platform,
		APTR message)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageCodec.WriteSort(ref platform, message);

	public static bool WriteVectorEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, uint index, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiFamilyMutationMessageCodec.TryGetVectorEntry(ref platform,
			message, offset, index, out var entry)) return false;
		var record = default(MuiFamilyMutationVectorEntry);
		record.Object = value;
		return MuiFamilyMutationVectorEntryCodec.Write(ref platform, entry, record);
	}

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return DispatchRecord(ref platform, state, family, message);
	}

	public static uint DispatchInsert<TPlatform>(ref TPlatform platform,
		APTR state, APTR family, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadInsert(ref platform, message, out var packet)) return 0;
		return MuiFamilyCore.Insert(ref platform, state, family, packet.Object,
			packet.Predecessor, false) ? 1u : 0u;
	}

	public static uint DispatchTransfer<TPlatform>(ref TPlatform platform,
		APTR state, APTR destination, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadTransfer(ref platform, message, out var packet)) return 0;
		return MuiFamilyCore.Transfer(ref platform, state, destination,
			packet.Family) ? 1u : 0u;
	}

	public static uint DispatchReorder<TPlatform>(ref TPlatform platform,
		APTR state, APTR family, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadReorder(ref platform, message, out var packet)) return 0;
		if (!MuiFamilyMutationMessageCodec.TryGetVectorBase(ref platform, message,
			MuiFamilyReorderMessage.ArrayOffset, out var vector)) return 0;
		return MuiFamilyCore.Reorder(ref platform, state, family, packet.After,
			vector) ? 1u : 0u;
	}

	public static uint DispatchSort<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSort(ref platform, message, out _)) return 0;
		if (!MuiFamilyMutationMessageCodec.TryGetVectorBase(ref platform, message,
			MuiFamilySortMessage.ArrayOffset, out var vector)) return 0;
		return MuiFamilyCore.Sort(ref platform, state, family,
			vector) ? 1u : 0u;
	}

	// Focused native seam for the fixed packet. Keeping this entry point next
	// to the packet codec prevents native closure qualification from pulling in
	// the unrelated aggregate dispatcher and its other method families.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR family, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint method;
		if (!MuiFamilyMutationMessageCodec.TryReadMethodIdValue(ref platform,
			message, out method)) return 0;
		if (!TryRead(ref platform, message, method, out var packet)) return 0;
		if (method == RemoveMethod)
			return MuiFamilyCore.Remove(ref platform, state, family,
				packet.Object) ? 1u : 0u;
		var added = method == AddHeadMethod ?
			MuiFamilyCore.AddHead(ref platform, state, family, packet.Object) :
			MuiFamilyCore.AddTail(ref platform, state, family, packet.Object);
		return added ? 1u : 0u;
	}

	public static bool WriteListRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR head, APTR tail)
		where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiFamilyMutationListRecord);
		record.Head = head;
		record.Tail = tail;
		return MuiFamilyMutationListCodec.Write(ref platform, storage, record);
	}

	public static bool WriteChildRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR next, APTR previous, APTR child)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiHeadlessChildPacketCore.WriteRecord(ref platform, storage,
			next, previous, child, APTR.Null);
	}

	// A small guest-only projection for native packet qualification. It keeps
	// the fixed packet proof independent of the complete object allocator while
	// preserving the same doubly-linked head/tail mutation used by the live
	// Family core.
	public static uint DispatchProjection<TPlatform>(ref TPlatform platform,
		APTR list, APTR node, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		uint method;
		if (!MuiFamilyMutationMessageCodec.TryReadMethodIdValue(ref platform,
			message, out method)) return 0;
		if (!TryRead(ref platform, message, method, out var packet) ||
			list.IsNull || node.IsNull || !platform.IsMapped(list,
				MuiFamilyMutationListRecord.Size) || !platform.IsMapped(node,
				MuiFamilyMutationChildRecord.Size) || packet.Object.IsNull) return 0;
		if (!MuiFamilyMutationListCodec.TryRead(ref platform, list,
			out var listRecord)) return 0;
		var head = listRecord.Head;
		var tail = listRecord.Tail;
		if (method == RemoveMethod)
		{
			var current = head;
			var visited = 0u;
			while (current.IsNotNull && visited++ < 65535)
			{
				if (!MuiHeadlessChildCodec.TryRead(ref platform, current,
					out var currentValue)) return 0;
				if (currentValue.Object.Raw == packet.Object.Raw)
				{
					var previous = currentValue.Previous;
					var next = currentValue.Next;
					if (previous.IsNotNull)
					{
						if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
							out var previousValue)) return 0;
						previousValue.Next = next;
						if (!MuiHeadlessChildCodec.Write(ref platform, previous,
							previousValue)) return 0;
					}
					else head = next;
					if (next.IsNotNull)
					{
						if (!MuiHeadlessChildCodec.TryRead(ref platform, next,
							out var nextValue)) return 0;
						nextValue.Previous = previous;
						if (!MuiHeadlessChildCodec.Write(ref platform, next,
							nextValue)) return 0;
					}
					else tail = previous;
					platform.Clear(current, MuiFamilyMutationChildRecord.Size);
					return WriteListRecord(ref platform, list, head, tail) ? 1u : 0u;
				}
				current = currentValue.Next;
			}
			return 0;
		}
		if (method == AddHeadMethod)
		{
			if (head.IsNotNull && !platform.IsMapped(head,
				MuiFamilyMutationChildRecord.Size)) return 0;
			if (!WriteChildRecord(ref platform, node, head, APTR.Null,
				packet.Object)) return 0;
			if (head.IsNotNull)
			{
				if (!MuiHeadlessChildCodec.TryRead(ref platform, head,
					out var headValue)) return 0;
				headValue.Previous = node;
				if (!MuiHeadlessChildCodec.Write(ref platform, head,
					headValue)) return 0;
			}
			else tail = node;
			head = node;
		}
		else
		{
			if (tail.IsNotNull && !platform.IsMapped(tail,
				MuiFamilyMutationChildRecord.Size)) return 0;
			if (!WriteChildRecord(ref platform, node, APTR.Null, tail,
				packet.Object)) return 0;
			if (tail.IsNotNull)
			{
				if (!MuiHeadlessChildCodec.TryRead(ref platform, tail,
					out var tailValue)) return 0;
				tailValue.Next = node;
				if (!MuiHeadlessChildCodec.Write(ref platform, tail,
					tailValue)) return 0;
			}
			else head = node;
			tail = node;
		}
		return WriteListRecord(ref platform, list, head, tail) ? 1u : 0u;
	}

	public static uint DispatchInsertProjection<TPlatform>(
		ref TPlatform platform, APTR list, APTR node, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message, 4) ||
			!TryReadInsert(ref platform, message, out var packet) ||
			list.IsNull || node.IsNull || packet.Object.IsNull ||
			!platform.IsMapped(list, MuiFamilyMutationListRecord.Size) ||
			!platform.IsMapped(node, MuiFamilyMutationChildRecord.Size)) return 0;
		if (!MuiFamilyMutationListCodec.TryRead(ref platform, list,
			out var listRecord)) return 0;
		var head = listRecord.Head;
		var tail = listRecord.Tail;
		var current = head;
		var visited = 0u;
		while (current.IsNotNull && visited++ < 65535)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, current,
				out var currentValue)) return 0;
			if (currentValue.Object.Raw == packet.Object.Raw)
				return 0;
			current = currentValue.Next;
		}
		if (visited >= 65535 && current.IsNotNull) return 0;

		APTR previous;
		APTR next;
		if (packet.Predecessor.IsNull)
		{
			previous = tail;
			next = APTR.Null;
		}
		else
		{
			previous = FindProjectionNode(ref platform, head,
				packet.Predecessor);
			if (previous.IsNull) return 0;
			if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
				out var previousValue)) return 0;
			next = previousValue.Next;
		}
		if (previous.IsNotNull && !platform.IsMapped(previous,
			MuiFamilyMutationChildRecord.Size)) return 0;
		if (next.IsNotNull && !platform.IsMapped(next,
			MuiFamilyMutationChildRecord.Size)) return 0;
		if (!WriteChildRecord(ref platform, node, next, previous,
			packet.Object)) return 0;
		if (previous.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
				out var previousLink)) return 0;
			previousLink.Next = node;
			if (!MuiHeadlessChildCodec.Write(ref platform, previous,
				previousLink)) return 0;
		}
		else head = node;
		if (next.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, next,
				out var nextValue)) return 0;
			nextValue.Previous = node;
			if (!MuiHeadlessChildCodec.Write(ref platform, next, nextValue))
				return 0;
		}
		else tail = node;
		return WriteListRecord(ref platform, list, head, tail) ? 1u : 0u;
	}

	public static uint DispatchTransferProjection<TPlatform>(
		ref TPlatform platform, APTR destinationList, APTR sourceList,
		APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message, 4) ||
			!TryReadTransfer(ref platform, message, out var packet) ||
			destinationList.IsNull || sourceList.IsNull ||
			packet.Family.Raw != sourceList.Raw ||
			!platform.IsMapped(destinationList, MuiFamilyMutationListRecord.Size) ||
			!platform.IsMapped(sourceList, MuiFamilyMutationListRecord.Size)) return 0;
		if (destinationList.Raw == sourceList.Raw) return 1;
		if (!MuiFamilyMutationListCodec.TryRead(ref platform, destinationList,
			out var destinationRecord) ||
			!MuiFamilyMutationListCodec.TryRead(ref platform, sourceList,
				out var sourceRecord)) return 0;
		var destinationHead = destinationRecord.Head;
		var destinationTail = destinationRecord.Tail;
		var sourceHead = sourceRecord.Head;
		var sourceTail = sourceRecord.Tail;
		if (sourceHead.IsNull)
			return sourceTail.IsNull ? 1u : 0u;
		if (sourceTail.IsNull || !platform.IsMapped(sourceHead,
			MuiFamilyMutationChildRecord.Size) || !platform.IsMapped(sourceTail,
			MuiFamilyMutationChildRecord.Size)) return 0;
		var current = sourceHead;
		var visited = 0u;
		while (current.IsNotNull && visited++ < 65535)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, current,
				out var currentValue)) return 0;
			current = currentValue.Next;
		}
		if (current.IsNotNull || visited == 0) return 0;
		if (destinationTail.IsNotNull && !platform.IsMapped(destinationTail,
			MuiFamilyMutationChildRecord.Size)) return 0;
		if (destinationTail.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, destinationTail,
				out var destinationTailValue) ||
				!MuiHeadlessChildCodec.TryRead(ref platform, sourceHead,
				out var sourceHeadValue)) return 0;
			destinationTailValue.Next = sourceHead;
			sourceHeadValue.Previous = destinationTail;
			if (!MuiHeadlessChildCodec.Write(ref platform, destinationTail,
				destinationTailValue) || !MuiHeadlessChildCodec.Write(ref platform,
				sourceHead, sourceHeadValue)) return 0;
		}
		else destinationHead = sourceHead;
		destinationTail = sourceTail;
		if (!WriteListRecord(ref platform, destinationList, destinationHead,
			destinationTail)) return 0;
		return WriteListRecord(ref platform, sourceList, APTR.Null,
			APTR.Null) ? 1u : 0u;
	}

	public static uint DispatchReorderProjection<TPlatform>(
		ref TPlatform platform, APTR list, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadReorder(ref platform, message, out var packet) ||
			list.IsNull || !platform.IsMapped(list,
				MuiFamilyMutationListRecord.Size)) return 0;
		return ReorderProjection(ref platform, list, message,
			MuiFamilyReorderMessage.ArrayOffset, packet.After) ? 1u : 0u;
	}

	public static uint DispatchSortProjection<TPlatform>(ref TPlatform platform,
		APTR list, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadSort(ref platform, message, out _) || list.IsNull ||
			!platform.IsMapped(list, MuiFamilyMutationListRecord.Size)) return 0;
		return ReorderProjection(ref platform, list, message,
			MuiFamilySortMessage.ArrayOffset, APTR.Null) ? 1u : 0u;
	}

	private static bool ReorderProjection<TPlatform>(ref TPlatform platform,
		APTR list, APTR message, uint vectorOffset, APTR after)
		where TPlatform : struct, IMuiGuestMemory
	{
		var predecessor = after;
		for (var index = 0u; index < 65535; index++)
		{
			if (!MuiFamilyMutationMessageCodec.TryGetVectorEntry(ref platform,
				message, vectorOffset, index, out var entry)) return false;
			if (!MuiFamilyMutationVectorEntryCodec.TryRead(ref platform, entry,
				out var vectorEntry)) return false;
			var objectAddress = vectorEntry.Object;
			if (objectAddress.IsNull) return true;
			if (!MoveProjectionAfter(ref platform, list, objectAddress,
				predecessor)) return false;
			predecessor = objectAddress;
		}
		return false;
	}

	private static bool MoveProjectionAfter<TPlatform>(ref TPlatform platform,
		APTR list, APTR objectAddress, APTR predecessor)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiFamilyMutationListCodec.TryRead(ref platform, list,
			out var listRecord)) return false;
		var head = listRecord.Head;
		var tail = listRecord.Tail;
		var node = FindProjectionNode(ref platform, head, objectAddress);
		if (node.IsNull) return false;
		var previous = predecessor.IsNull ? APTR.Null :
			FindProjectionNode(ref platform, head, predecessor);
		if (predecessor.IsNotNull && previous.IsNull) return false;
		if (node.Raw == previous.Raw) return true;
		if (!MuiHeadlessChildCodec.TryRead(ref platform, node,
			out var nodeValue)) return false;
		var oldPrevious = nodeValue.Previous;
		var oldNext = nodeValue.Next;
		if (oldPrevious.IsNotNull && !platform.IsMapped(oldPrevious,
			MuiFamilyMutationChildRecord.Size)) return false;
		if (oldNext.IsNotNull && !platform.IsMapped(oldNext,
			MuiFamilyMutationChildRecord.Size)) return false;
		if (previous.IsNotNull && !platform.IsMapped(previous,
			MuiFamilyMutationChildRecord.Size)) return false;
		if (oldPrevious.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, oldPrevious,
				out var oldPreviousValue)) return false;
			oldPreviousValue.Next = oldNext;
			if (!MuiHeadlessChildCodec.Write(ref platform, oldPrevious,
				oldPreviousValue)) return false;
		}
		else head = oldNext;
		if (oldNext.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, oldNext,
				out var oldNextValue)) return false;
			oldNextValue.Previous = oldPrevious;
			if (!MuiHeadlessChildCodec.Write(ref platform, oldNext,
				oldNextValue)) return false;
		}
		else tail = oldPrevious;
		var next = head;
		if (previous.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
				out var previousValue)) return false;
			next = previousValue.Next;
		}
		if (next.IsNotNull && !platform.IsMapped(next,
			MuiFamilyMutationChildRecord.Size)) return false;
		nodeValue.Previous = previous;
		nodeValue.Next = next;
		if (!MuiHeadlessChildCodec.Write(ref platform, node, nodeValue))
			return false;
		if (previous.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, previous,
				out var previousLink)) return false;
			previousLink.Next = node;
			if (!MuiHeadlessChildCodec.Write(ref platform, previous,
				previousLink)) return false;
		}
		else head = node;
		if (next.IsNotNull)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, next,
				out var nextLink)) return false;
			nextLink.Previous = node;
			if (!MuiHeadlessChildCodec.Write(ref platform, next, nextLink))
				return false;
		}
		else tail = node;
		return WriteListRecord(ref platform, list, head, tail);
	}

	private static APTR FindProjectionNode<TPlatform>(ref TPlatform platform,
		APTR head, APTR target)
		where TPlatform : struct, IMuiGuestMemory
	{
		var current = head;
		var visited = 0u;
		while (current.IsNotNull && visited++ < 65535)
		{
			if (!MuiHeadlessChildCodec.TryRead(ref platform, current,
				out var currentValue)) return APTR.Null;
			if (currentValue.Object.Raw == target.Raw) return current;
			current = currentValue.Next;
		}
		return APTR.Null;
	}
}
