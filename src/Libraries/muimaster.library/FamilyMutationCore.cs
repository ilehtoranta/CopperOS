/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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
	private static bool TryResolveFieldIndex(MuiFamilyPacketKind packet,
		MuiFamilyPacketField field, out uint index, out uint packetSize)
	{
		index = uint.MaxValue;
		packetSize = 0;
		if (packet == MuiFamilyPacketKind.Method ||
			packet == MuiFamilyPacketKind.Sort)
		{
			if (field != MuiFamilyPacketField.MethodId) return false;
			index = 0;
			packetSize = packet == MuiFamilyPacketKind.Method
				? MuiFamilyMethodMessage.Size
				: MuiFamilySortMessage.MinimumSize;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Child)
		{
			packetSize = MuiFamilyChildMessage.Size;
			if (field == MuiFamilyPacketField.MethodId)
				index = 0;
			else if (field == MuiFamilyPacketField.Object)
				index = 1;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Insert)
		{
			packetSize = MuiFamilyInsertMessage.Size;
			if (field == MuiFamilyPacketField.MethodId)
				index = 0;
			else if (field == MuiFamilyPacketField.Object)
				index = 1;
			else if (field == MuiFamilyPacketField.Predecessor)
				index = 2;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Transfer)
		{
			packetSize = MuiFamilyTransferMessage.Size;
			if (field == MuiFamilyPacketField.MethodId)
				index = 0;
			else if (field == MuiFamilyPacketField.Family)
				index = 1;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Reorder)
		{
			packetSize = MuiFamilyReorderMessage.MinimumSize;
			if (field == MuiFamilyPacketField.MethodId)
				index = 0;
			else if (field == MuiFamilyPacketField.After)
				index = 1;
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
		return TryGetAddress(ref platform, message, packet, field,
			out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyPacketKind packet, MuiFamilyPacketField field,
		out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(packet, field, out var index,
			out var packetSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, packetSize,
				out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
				out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = 4;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyPacketKind packet, MuiFamilyPacketField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiFamilyPacketKind.Method)
			return field == MuiFamilyPacketField.MethodId &&
				MuiFamilyMethodHeaderCodec.TryReadValue(ref platform, message,
					out value);
		if (packet == MuiFamilyPacketKind.Sort)
			return field == MuiFamilyPacketField.MethodId &&
				platform.IsMapped(message, MuiFamilySortMessage.MinimumSize) &&
				MuiFamilyMutationMessageStructCodec.TryReadSortMethodIdValue(
					ref platform, message, out value);
		if (packet == MuiFamilyPacketKind.Child)
		{
			if (!MuiFamilyMutationMessageStructCodec.TryReadChild(ref platform,
				message, out var child)) return false;
			if (field == MuiFamilyPacketField.MethodId) value = child.MethodId;
			else if (field == MuiFamilyPacketField.Object) value = child.Object.Raw;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Insert)
		{
			if (!MuiFamilyMutationMessageStructCodec.TryReadInsert(ref platform,
				message, out var insert)) return false;
			if (field == MuiFamilyPacketField.MethodId) value = insert.MethodId;
			else if (field == MuiFamilyPacketField.Object) value = insert.Object.Raw;
			else if (field == MuiFamilyPacketField.Predecessor)
				value = insert.Predecessor.Raw;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Transfer)
		{
			if (!MuiFamilyMutationMessageStructCodec.TryReadTransfer(ref platform,
				message, out var transfer)) return false;
			if (field == MuiFamilyPacketField.MethodId) value = transfer.MethodId;
			else if (field == MuiFamilyPacketField.Family) value = transfer.Family.Raw;
			else return false;
			return true;
		}
		if (packet == MuiFamilyPacketKind.Reorder)
		{
			if (!platform.IsMapped(message, MuiFamilyReorderMessage.MinimumSize) ||
				!MuiFamilyMutationMessageStructCodec.TryReadReorder(ref platform,
					message, out var reorder)) return false;
			if (field == MuiFamilyPacketField.MethodId) value = reorder.MethodId;
			else if (field == MuiFamilyPacketField.After) value = reorder.After.Raw;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyPacketKind packet, MuiFamilyPacketField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiFamilyPacketKind.Method)
			return field == MuiFamilyPacketField.MethodId &&
				MuiFamilyMethodHeaderCodec.WriteValue(ref platform, message, value);
		if (packet == MuiFamilyPacketKind.Sort)
			return field == MuiFamilyPacketField.MethodId &&
				platform.IsMapped(message, MuiFamilySortMessage.MinimumSize) &&
				MuiFamilyMutationMessageStructCodec.TryWriteSort(ref platform,
					message, value);
		if (packet == MuiFamilyPacketKind.Child)
		{
			if (!MuiFamilyMutationMessageStructCodec.TryReadChild(ref platform,
				message, out var child)) return false;
			if (field == MuiFamilyPacketField.MethodId) child.MethodId = value;
			else if (field == MuiFamilyPacketField.Object)
				child.Object = APTR.FromPointer(value);
			else return false;
			return MuiFamilyMutationMessageStructCodec.WriteRecord(ref platform,
				message, child);
		}
		if (packet == MuiFamilyPacketKind.Insert)
		{
			if (!MuiFamilyMutationMessageStructCodec.TryReadInsert(ref platform,
				message, out var insert)) return false;
			if (field == MuiFamilyPacketField.MethodId) insert.MethodId = value;
			else if (field == MuiFamilyPacketField.Object)
				insert.Object = APTR.FromPointer(value);
			else if (field == MuiFamilyPacketField.Predecessor)
				insert.Predecessor = APTR.FromPointer(value);
			else return false;
			return MuiFamilyMutationMessageStructCodec.WriteRecord(ref platform,
				message, insert);
		}
		if (packet == MuiFamilyPacketKind.Transfer)
		{
			if (!MuiFamilyMutationMessageStructCodec.TryReadTransfer(ref platform,
				message, out var transfer)) return false;
			if (field == MuiFamilyPacketField.MethodId) transfer.MethodId = value;
			else if (field == MuiFamilyPacketField.Family)
				transfer.Family = APTR.FromPointer(value);
			else return false;
			return MuiFamilyMutationMessageStructCodec.WriteRecord(ref platform,
				message, transfer);
		}
		if (packet == MuiFamilyPacketKind.Reorder)
		{
			if (!platform.IsMapped(message, MuiFamilyReorderMessage.MinimumSize) ||
				!MuiFamilyMutationMessageStructCodec.TryReadReorder(ref platform,
					message, out var reorder)) return false;
			if (field == MuiFamilyPacketField.MethodId) reorder.MethodId = value;
			else if (field == MuiFamilyPacketField.After)
				reorder.After = APTR.FromPointer(value);
			else return false;
			return MuiFamilyMutationMessageStructCodec.WriteRecord(ref platform,
				message, reorder);
		}
		return false;
	}

	// Keep method-header admission scalar in native lowering while the public
	// decoder continues to expose the named method record. The named header
	// codec below owns the bounded guest-memory read.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMethodHeaderCodec.TryReadValue(ref platform, message,
			out value);
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

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFamilyPacketFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFamilyPacketMemoryCodec.TryGetAddress(ref platform, cursor.Message,
			cursor.Packet, cursor.Field, out address, out fieldSize);

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

// Sequential codecs for the fixed Family mutation packet headers. The legacy
// field adapter remains available for compatibility diagnostics; production
// reads and writes consume each declaration-ordered named record here.
internal static class MuiFamilyMutationMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMethodHeaderCodec.TryReadValue(ref platform, message,
			out methodId);

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out packet.MethodId))
			return false;
		return true;
	}

	internal static bool TryWriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMethodHeaderCodec.WriteValue(ref platform, message, methodId);

	internal static bool TryReadChild<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyChildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyChildMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawObject) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		packet.Object = APTR.FromPointer(rawObject);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyChildMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyChildMessage.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Object.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryWriteChild<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId, APTR child)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyChildMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, methodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				child.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyInsertMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawObject) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawPredecessor) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.Object = APTR.FromPointer(rawObject);
		packet.Predecessor = APTR.FromPointer(rawPredecessor);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyInsertMessage.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Object.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Predecessor.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryWriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId, APTR child, APTR predecessor)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyInsertMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, methodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				child.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				predecessor.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadTransfer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyTransferMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyTransferMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawFamily) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		packet.Family = APTR.FromPointer(rawFamily);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyTransferMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyTransferMessage.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Family.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryWriteTransfer<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId, APTR family)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyTransferMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, methodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				family.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadReorder<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyReorderMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyReorderMessage.HeaderSize, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawAfter) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		packet.After = APTR.FromPointer(rawAfter);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyReorderMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyReorderMessage.HeaderSize, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.After.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryWriteReorder<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId, APTR after)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiFamilyReorderMessage.HeaderSize, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, methodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				after.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadSortMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadMethodIdValue(ref platform, message, out methodId);

	internal static bool TryWriteSort<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> TryWriteMethod(ref platform, message, methodId);
}

// Struct-first codec for the method-only Family mutation and sort headers.
// Keep the named one-ULONG record as the ABI contract while using the shared
// storage record to avoid direct scalar reads at the guest boundary.
internal static class MuiFamilyMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFamilyMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFamilyMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Central codec for the fixed MorphOS Family mutation packet family. The
// public/core paths consume named records; only this adapter carries packed
// guest offsets, method validation, and bounded array-header mapping checks.
internal static class MuiFamilyMutationMessageCodec
{
	internal static bool TryGetVectorBase<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyInlineVectorKind kind, out APTR vector)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetVectorEntry(ref platform, message, kind, 0, out vector);

	internal static bool TryGetVectorEntry<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyInlineVectorKind kind, uint index, out APTR entry)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyInlineVectorMemoryCodec.TryGetEntry(ref platform, message,
			kind, index, out entry);

	// Compatibility overloads retain the historical wire-offset API for older
	// callers. New production paths use the named vector-kind overloads above.
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
		=> MuiFamilyMutationMessageStructCodec.TryReadMethodIdValue(
			ref platform, message, out methodId);

	internal static bool TryReadChild<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiFamilyChildMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (method != MuiFamilyMutationCore.AddHeadMethod &&
			method != MuiFamilyMutationCore.AddTailMethod &&
			method != MuiFamilyMutationCore.RemoveMethod ||
			!MuiFamilyMutationMessageStructCodec.TryReadChild(ref platform,
				message, out packet) || packet.MethodId != method) return false;
		return true;
	}

	internal static bool WriteChild<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR child)
		where TPlatform : struct, IMuiGuestMemory
		=> (method == MuiFamilyMutationCore.AddHeadMethod ||
			method == MuiFamilyMutationCore.AddTailMethod ||
			method == MuiFamilyMutationCore.RemoveMethod) &&
			MuiFamilyMutationMessageStructCodec.TryWriteChild(ref platform,
				message, method, child);

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiFamilyMutationMessageStructCodec.TryReadInsert(ref platform,
			message, out packet) && packet.MethodId == MuiFamilyMutationCore.InsertMethod;
	}

	internal static bool WriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, APTR child, APTR predecessor)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageStructCodec.TryWriteInsert(ref platform,
			message, MuiFamilyMutationCore.InsertMethod, child, predecessor);

	internal static bool TryReadTransfer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyTransferMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiFamilyMutationMessageStructCodec.TryReadTransfer(ref platform,
			message, out packet) && packet.MethodId == MuiFamilyMutationCore.TransferMethod;
	}

	internal static bool WriteTransfer<TPlatform>(ref TPlatform platform,
		APTR message, APTR family)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationMessageStructCodec.TryWriteTransfer(ref platform,
			message, MuiFamilyMutationCore.TransferMethod, family);

	internal static bool TryReadReorder<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilyReorderMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyReorderMessage.MinimumSize) ||
			!MuiFamilyMutationMessageStructCodec.TryReadReorder(ref platform,
				message, out packet) ||
			packet.MethodId != MuiFamilyMutationCore.ReorderMethod)
			return false;
		return true;
	}

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR message, out MuiFamilySortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilySortMessage.MinimumSize) ||
			!MuiFamilyMutationMessageStructCodec.TryReadMethodIdValue(ref platform,
				message, out packet.MethodId) ||
			packet.MethodId != MuiFamilyMutationCore.SortMethod)
			return false;
		return true;
	}

	internal static bool WriteReorder<TPlatform>(ref TPlatform platform,
		APTR message, APTR after)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilyReorderMessage.MinimumSize)) return false;
		return MuiFamilyMutationMessageStructCodec.TryWriteReorder(ref platform,
			message, MuiFamilyMutationCore.ReorderMethod, after);
	}

	internal static bool WriteSort<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiFamilySortMessage.MinimumSize)) return false;
		return MuiFamilyMutationMessageStructCodec.TryWriteSort(ref platform,
			message, MuiFamilyMutationCore.SortMethod);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyMutationListRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
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
	private static bool TryResolveFieldIndex(MuiFamilyMutationListField field,
		out uint index)
	{
		if (field == MuiFamilyMutationListField.Head)
		{
			index = 0;
			return true;
		}
		if (field == MuiFamilyMutationListField.Tail)
		{
			index = 1;
			return true;
		}
		index = uint.MaxValue;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR list, MuiFamilyMutationListField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiFamilyMutationListFieldCursor);
		cursor.List = list;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFamilyMutationListFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.List,
				MuiFamilyMutationListRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiFamilyMutationListRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiFamilyMutationListRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR list, MuiFamilyMutationListField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiFamilyMutationListStructCodec.TryRead(ref platform, list,
			out var record)) return false;
		if (field == MuiFamilyMutationListField.Head)
			value = record.Head.Raw;
		else if (field == MuiFamilyMutationListField.Tail)
			value = record.Tail.Raw;
		else return false;
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR list, MuiFamilyMutationListField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiFamilyMutationListStructCodec.TryRead(ref platform, list,
			out var record)) return false;
		if (field == MuiFamilyMutationListField.Head)
			record.Head = APTR.FromPointer(value);
		else if (field == MuiFamilyMutationListField.Tail)
			record.Tail = APTR.FromPointer(value);
		else return false;
		return MuiFamilyMutationListStructCodec.WriteRecord(ref platform, list,
			record);
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Family list cursor. Live list serialization uses the named record adapter.
internal static class MuiFamilyMutationListFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFamilyMutationListFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiFamilyMutationListFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiFamilyMutationListMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

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

// Sequential codec for the guest-resident Family projection list record.
// Production list reads and writes use the named Head/Tail members in order;
// the field adapter remains only for compatibility diagnostics.
internal static class MuiFamilyMutationListStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiFamilyMutationListRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFamilyMutationListRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawHead) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawTail) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		record.Head = APTR.FromPointer(rawHead);
		record.Tail = APTR.FromPointer(rawTail);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiFamilyMutationListRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFamilyMutationListRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Head.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Tail.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiFamilyMutationListRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, record);
}

internal static class MuiFamilyMutationListCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiFamilyMutationListRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationListStructCodec.TryRead(ref platform, address,
			out record);

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiFamilyMutationListRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationListStructCodec.TryWrite(ref platform, address,
			record);
}

// Named cursor for an inline pointer vector carried by a Family method packet.
// The packet header remains a fixed wire record; this view keeps the vector's
// base, element ordinal, and mapping/overflow policy together instead of
// repeating message-plus-offset arithmetic at each call site.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiFamilyInlineVectorCursor
{
	internal APTR Message;
	internal MuiFamilyInlineVectorKind Kind;
	// Compatibility-only wire selector retained for older callers. New code
	// selects the named Reorder/Sort record through Kind.
	internal uint ArrayOffset;
	internal uint Index;
}

internal enum MuiFamilyInlineVectorKind : byte
{
	// Zero is deliberately the compatibility/default state so cursors created
	// by older offset-based callers continue to select ArrayOffset.
	None,
	Reorder,
	Sort,
}

// Struct-first adapter for the inline object vectors carried by the Family
// Reorder and Sort packets. The packet header size is resolved from its named
// struct, then the selected vector entry is range-checked; raw offsets never
// escape this guest-memory boundary.
internal static class MuiFamilyInlineVectorMemoryCodec
{
	private static bool TryResolveKind(MuiFamilyInlineVectorKind kind,
		out uint vectorOffset, out uint packetSize)
	{
		if (kind == MuiFamilyInlineVectorKind.Reorder)
		{
			vectorOffset = MuiFamilyReorderMessage.ArrayOffset;
			packetSize = MuiFamilyReorderMessage.HeaderSize;
			return true;
		}
		if (kind == MuiFamilyInlineVectorKind.Sort)
		{
			vectorOffset = MuiFamilySortMessage.ArrayOffset;
			packetSize = MuiFamilySortMessage.HeaderSize;
			return true;
		}
		vectorOffset = 0;
		packetSize = 0;
		return false;
	}

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyInlineVectorKind kind, uint index,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveKind(kind, out var vectorOffset, out var packetSize) ||
			message.IsNull || !platform.IsMapped(message, packetSize) || message.Raw >
			uint.MaxValue - vectorOffset) return false;
		var vector = message.Raw + vectorOffset;
		if (index > (uint.MaxValue - vector) /
			MuiFamilyMutationVectorEntry.Size) return false;
		var offset = index * MuiFamilyMutationVectorEntry.Size;
		if (vector > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector + offset);
		return platform.IsMapped(address, MuiFamilyMutationVectorEntry.Size);
	}

	// Compatibility overload for callers that still pass the packet's wire
	// array offset. Production paths use the named vector-kind overload above.
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
		if (!TryResolve(vectorOffset, out _)) return false;
		var kind = vectorOffset == MuiFamilyReorderMessage.ArrayOffset ?
			MuiFamilyInlineVectorKind.Reorder : MuiFamilyInlineVectorKind.Sort;
		return TryGetEntry(ref platform, message, kind, index, out address);
	}
}

internal static class MuiFamilyInlineVectorCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiFamilyInlineVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (cursor.Kind == MuiFamilyInlineVectorKind.None)
			return MuiFamilyInlineVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Message, cursor.ArrayOffset, cursor.Index, out address);
		return MuiFamilyInlineVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Message, cursor.Kind, cursor.Index, out address);
	}

	internal static bool TryReadObject<TPlatform>(ref TPlatform platform,
		MuiFamilyInlineVectorCursor cursor, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!TryReadObjectValue(ref platform, cursor, out var rawObject))
			return false;
		value = APTR.FromPointer(rawObject);
		return true;
	}

	// The scalar form is a native-qualification aid for CopperSharp's current
	// one-ULONG lowering. It still admits the complete named vector record before
	// exposing its Object member; production callers should prefer TryReadObject.
	internal static bool TryReadObjectValue<TPlatform>(ref TPlatform platform,
		MuiFamilyInlineVectorCursor cursor, out uint rawObject)
		where TPlatform : struct, IMuiGuestMemory
	{
		rawObject = 0;
		if (!TryGetEntry(ref platform, cursor, out var address) ||
			!MuiFamilyMutationVectorEntryStructCodec.TryReadObjectValue(
				ref platform, address, out rawObject)) return false;
		return true;
	}

	internal static bool TryWriteObjectValue<TPlatform>(ref TPlatform platform,
		MuiFamilyInlineVectorCursor cursor, uint rawObject)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiFamilyMutationVectorEntry.Size, out var entryCursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref entryCursor,
				rawObject)) return false;
		return MuiGuestStructCursor.IsComplete(entryCursor);
	}

	internal static bool TryWriteObject<TPlatform>(ref TPlatform platform,
		MuiFamilyInlineVectorCursor cursor, APTR value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryWriteObjectValue(ref platform, cursor, value.Raw);
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

// Sequential codec for one caller-owned Family reorder/sort vector element.
// The vector address/ordinal policy is kept in the bounded adapter; this
// codec exchanges the named Object member as a complete declaration-ordered
// record.
internal static class MuiFamilyMutationVectorEntryStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadObjectValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint rawObject)
		where TPlatform : struct, IMuiGuestMemory
	{
		rawObject = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFamilyMutationVectorEntry.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out rawObject) || !MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiFamilyMutationVectorEntry record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!TryReadObjectValue(ref platform, address, out var rawObject))
			return false;
		record.Object = APTR.FromPointer(rawObject);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiFamilyMutationVectorEntry record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiFamilyMutationVectorEntry.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Object.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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
		=> MuiFamilyMutationVectorEntryStructCodec.TryRead(ref platform, address,
			out record);

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiFamilyMutationVectorEntry record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiFamilyMutationVectorEntryStructCodec.TryWrite(ref platform, address,
			record);
}

// Compatibility wrapper retained for callers that still model the vector as
// a cursor.  New production paths use the named entry codec directly.
internal static class MuiFamilyMutationVectorCodec
{
	internal static bool TryReadObjectValue<TPlatform>(ref TPlatform platform,
		MuiFamilyMutationVectorCursor cursor, out uint rawObject)
		where TPlatform : struct, IMuiGuestMemory
	{
		rawObject = 0;
		if (!TryGetEntry(ref platform, cursor, out var address) ||
			!MuiFamilyMutationVectorEntryStructCodec.TryReadObjectValue(
				ref platform, address, out rawObject)) return false;
		return true;
	}

	internal static bool TryReadObject<TPlatform>(ref TPlatform platform,
		MuiFamilyMutationVectorCursor cursor, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!TryReadObjectValue(ref platform, cursor, out var rawObject))
			return false;
		value = APTR.FromPointer(rawObject);
		return true;
	}

	// Complete named-entry bridge for indexed Reorder/Sort consumers. The
	// bounded adapter owns vector arithmetic; callers receive the semantic
	// Object capability rather than a guest slot address.
	internal static bool TryReadObjectValue<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out uint rawObject)
		where TPlatform : struct, IMuiGuestMemory
	{
		rawObject = 0;
		if (!MuiFamilyMutationVectorMemoryCodec.TryGetEntry(ref platform, vector,
			index, out var address) ||
			!MuiFamilyMutationVectorEntryStructCodec.TryReadObjectValue(
				ref platform, address, out rawObject)) return false;
		return true;
	}

	internal static bool TryReadObject<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		if (!TryReadObjectValue(ref platform, vector, index, out var rawObject))
			return false;
		value = APTR.FromPointer(rawObject);
		return true;
	}

	internal static bool TryWriteObject<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiFamilyMutationVectorMemoryCodec.TryGetEntry(ref platform, vector,
			index, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiFamilyMutationVectorEntry.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteObject<TPlatform>(ref TPlatform platform,
		MuiFamilyMutationVectorCursor cursor, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiFamilyMutationVectorEntry.Size, out var entryCursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref entryCursor,
				value.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(entryCursor);
	}

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

	internal static bool WriteVectorEntry<TPlatform>(ref TPlatform platform,
		APTR message, MuiFamilyInlineVectorKind kind, uint index, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiFamilyInlineVectorCursor);
		cursor.Message = message;
		cursor.Kind = kind;
		cursor.Index = index;
		return MuiFamilyInlineVectorCursorCodec.TryWriteObject(ref platform,
			cursor, value);
	}

	public static bool WriteVectorEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint offset, uint index, APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiFamilyInlineVectorCursor);
		cursor.Message = message;
		cursor.ArrayOffset = offset;
		cursor.Index = index;
		return MuiFamilyInlineVectorCursorCodec.TryWriteObject(ref platform,
			cursor, value);
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
			MuiFamilyInlineVectorKind.Reorder, out var vector)) return 0;
		return MuiFamilyCore.Reorder(ref platform, state, family, packet.After,
			vector) ? 1u : 0u;
	}

	public static uint DispatchSort<TPlatform>(ref TPlatform platform, APTR state,
		APTR family, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSort(ref platform, message, out _)) return 0;
		if (!MuiFamilyMutationMessageCodec.TryGetVectorBase(ref platform, message,
			MuiFamilyInlineVectorKind.Sort, out var vector)) return 0;
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
			MuiFamilyInlineVectorKind.Reorder, packet.After) ? 1u : 0u;
	}

	public static uint DispatchSortProjection<TPlatform>(ref TPlatform platform,
		APTR list, APTR message)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadSort(ref platform, message, out _) || list.IsNull ||
			!platform.IsMapped(list, MuiFamilyMutationListRecord.Size)) return 0;
		return ReorderProjection(ref platform, list, message,
			MuiFamilyInlineVectorKind.Sort, APTR.Null) ? 1u : 0u;
	}

	private static bool ReorderProjection<TPlatform>(ref TPlatform platform,
		APTR list, APTR message, MuiFamilyInlineVectorKind kind, APTR after)
		where TPlatform : struct, IMuiGuestMemory
	{
		var predecessor = after;
		for (var index = 0u; index < 65535; index++)
		{
			var cursor = default(MuiFamilyInlineVectorCursor);
			cursor.Message = message;
			cursor.Kind = kind;
			cursor.Index = index;
			if (!MuiFamilyInlineVectorCursorCodec.TryReadObject(ref platform,
				cursor, out var objectAddress)) return false;
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
