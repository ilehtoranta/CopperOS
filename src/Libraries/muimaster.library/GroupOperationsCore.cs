/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupMoveMemberMessage
{
	public const uint Size = 12;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ObjectOffset = 4;
	public const uint PositionOffset = 8;
	public uint MethodId;
	public uint Object;
	public int Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupReorderMessage
{
	public const uint Size = 12;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint AfterOffset = 4;
	public const uint ObjectsOffset = 8;
	public uint MethodId;
	public uint After;
	public uint Objects;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupSortMessage
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public const uint ObjectsOffset = 4;
	public uint MethodId;
	public uint Objects;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupOrderingMethodMessage
{
	public const uint Size = 4;
	public const uint FieldSize = 4;
	public const uint MethodIdOffset = 0;
	public uint MethodId;
}

internal enum MuiGroupOrderingPacketKind : byte
{
	Header,
	MoveMember,
	Reorder,
	Sort,
}

internal enum MuiGroupOrderingPacketField : byte
{
	MethodId,
	Object,
	Position,
	After,
	Objects,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupOrderingPacketFieldCursor
{
	internal APTR Message;
	internal MuiGroupOrderingPacketKind Packet;
	internal MuiGroupOrderingPacketField Field;
}

// Struct-first codec for the method-only Group ordering header. The shared
// ULONG storage helper keeps the packed one-field record address explicit for
// freestanding lowering while callers still receive the named message type.
internal static class MuiGroupOrderingMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupOrderingMethodMessage.Size, out var cursor) ||
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
			MuiGroupOrderingMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiGroupOrderingPacketMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, out uint index, out uint size,
		out uint fieldSize)
	{
		index = 0;
		size = 0;
		fieldSize = 0;
		if (packet == MuiGroupOrderingPacketKind.Header)
		{
			if (field != MuiGroupOrderingPacketField.MethodId) return false;
			size = MuiGroupOrderingMethodMessage.Size;
			fieldSize = MuiGroupOrderingMethodMessage.FieldSize;
			return true;
		}
		if (packet == MuiGroupOrderingPacketKind.MoveMember)
		{
			size = MuiGroupMoveMemberMessage.Size;
			fieldSize = MuiGroupMoveMemberMessage.FieldSize;
			if (field == MuiGroupOrderingPacketField.MethodId)
				index = 0;
			else if (field == MuiGroupOrderingPacketField.Object)
				index = 1;
			else if (field == MuiGroupOrderingPacketField.Position)
				index = 2;
			else return false;
			return true;
		}
		if (packet == MuiGroupOrderingPacketKind.Reorder)
		{
			size = MuiGroupReorderMessage.Size;
			fieldSize = MuiGroupReorderMessage.FieldSize;
			if (field == MuiGroupOrderingPacketField.MethodId)
				index = 0;
			else if (field == MuiGroupOrderingPacketField.After)
				index = 1;
			else if (field == MuiGroupOrderingPacketField.Objects)
				index = 2;
			else return false;
			return true;
		}
		if (packet == MuiGroupOrderingPacketKind.Sort)
		{
			size = MuiGroupSortMessage.Size;
			fieldSize = MuiGroupSortMessage.FieldSize;
			if (field == MuiGroupOrderingPacketField.MethodId)
				index = 0;
			else if (field == MuiGroupOrderingPacketField.Objects)
				index = 1;
			else return false;
			return true;
		}
		return false;
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolveFieldIndex(packet, field, out var index, out _, out _))
			return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGroupOrderingMethodMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(packet, field, out _, out var packetSize,
			out fieldSize) || message.IsNull ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, packetSize,
				out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, packet, field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address, out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address, out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	// Keep the four-byte method header scalar in native lowering. Its wire
	// position remains owned by the named method record.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupOrderingMethodHeaderCodec.TryReadValue(ref platform, message,
			out value);
}

// Compatibility wrapper retained for callers that still construct the typed
// packet cursor. Live group-ordering codecs use the direct memory adapter.
internal static class MuiGroupOrderingPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupOrderingPacketFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupOrderingPacketMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupOrderingPacketMemoryCodec.TryReadUInt32(ref platform, message,
			packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupOrderingPacketMemoryCodec.TryWriteUInt32(ref platform, message,
			packet, field, value);
}

internal static class MuiGroupOrderingMessageCodec
{
	internal static bool TryReadMoveMemberRecord<TPlatform>(
		ref TPlatform platform, APTR address,
		out MuiGroupMoveMemberMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupMoveMemberMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Object) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawPosition) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Position = unchecked((int)rawPosition);
		return true;
	}

	internal static bool WriteMoveMemberRecord<TPlatform>(
		ref TPlatform platform, APTR address, MuiGroupMoveMemberMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupMoveMemberMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Position))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadReorderRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupReorderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupReorderMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.After) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Objects) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool WriteReorderRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupReorderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupReorderMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.After) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Objects)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSortRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupSortMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupSortMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Objects) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool WriteSortRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupSortMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupSortMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Objects)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupOrderingMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodIdValue(ref platform, address, out var methodId))
			return false;
		value.MethodId = methodId;
		return true;
	}

	// Native selector admission stays scalar so compiler paths do not need to
	// materialize a temporary one-field record. Public packet consumers still
	// receive the named method and payload structs.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		return MuiGroupOrderingPacketMemoryCodec.TryReadMethodId(ref platform,
			address, out methodId);
	}

	internal static bool WriteMoveMember<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupMoveMemberMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteMoveMemberRecord(ref platform, address, value);

	internal static bool WriteReorder<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupReorderMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteReorderRecord(ref platform, address, value);

	internal static bool WriteSort<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupSortMessage value) where TPlatform : struct, IMuiGuestMemory
		=> WriteSortRecord(ref platform, address, value);

	internal static bool TryReadMoveMember<TPlatform>(ref TPlatform platform,
		APTR address, uint method, out MuiGroupMoveMemberMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadMoveMemberRecord(ref platform, address, out value) &&
			value.MethodId == method;
	}

	internal static bool TryReadReorder<TPlatform>(ref TPlatform platform,
		APTR address, uint method, out MuiGroupReorderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadReorderRecord(ref platform, address, out value) &&
			value.MethodId == method;
	}

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR address, uint method, out MuiGroupSortMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadSortRecord(ref platform, address, out value) &&
			value.MethodId == method;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupMoveMemberRecordInput
{
	public APTR Object;
	public int Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupReorderRecordInput
{
	public APTR After;
	public APTR Objects;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupSortRecordInput
{
	public APTR Objects;
}

// Group-specific ordering methods share the frozen Family child topology but
// keep their MorphOS packet ids and Group-class validation at this boundary.
public static class MuiGroupOperationsCore
{
	public const uint MoveMemberMethod = 0x8042FF4E;
	public const uint ReorderMethod = 0x80426C3F;
	public const uint SortMethod = 0x80427417;

	// Struct-first packet seams used by the freestanding qualification roots.
	// The public operations below still perform live Group validation and child
	// topology mutation; these helpers isolate the fixed guest packet layouts.
	public static bool WriteMoveMemberRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR child, int position)
		where TPlatform : struct, IMuiGuestMemory
	{
		var input = default(MuiGroupMoveMemberRecordInput);
		input.Object = child;
		input.Position = position;
		return WriteMoveMemberRecord(ref platform, storage, input);
	}

	public static bool WriteMoveMemberRecord<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGroupMoveMemberRecordInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiGroupMoveMemberMessage);
		packet.MethodId = MoveMemberMethod;
		packet.Object = input.Object.Raw;
		packet.Position = input.Position;
		return MuiGroupOrderingMessageCodec.WriteMoveMember(ref platform,
			storage, packet);
	}

	public static bool WriteReorderRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR after, APTR objects)
		where TPlatform : struct, IMuiGuestMemory
	{
		var input = default(MuiGroupReorderRecordInput);
		input.After = after;
		input.Objects = objects;
		return WriteReorderRecord(ref platform, storage, input);
	}

	public static bool WriteReorderRecord<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGroupReorderRecordInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiGroupReorderMessage);
		packet.MethodId = ReorderMethod;
		packet.After = input.After.Raw;
		packet.Objects = input.Objects.Raw;
		return MuiGroupOrderingMessageCodec.WriteReorder(ref platform, storage,
			packet);
	}

	public static bool WriteSortRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR objects) where TPlatform : struct, IMuiGuestMemory
	{
		var input = default(MuiGroupSortRecordInput);
		input.Objects = objects;
		return WriteSortRecord(ref platform, storage, input);
}

	public static bool WriteSortRecord<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGroupSortRecordInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiGroupSortMessage);
		packet.MethodId = SortMethod;
		packet.Objects = input.Objects.Raw;
		return MuiGroupOrderingMessageCodec.WriteSort(ref platform, storage,
			packet);
	}

	internal static bool TryReadMoveMember<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGroupMoveMemberMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupOrderingMessageCodec.TryReadMoveMember(ref platform, message,
			MoveMemberMethod, out packet);

	internal static bool TryReadReorder<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGroupReorderMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupOrderingMessageCodec.TryReadReorder(ref platform, message,
			ReorderMethod, out packet);

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGroupSortMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupOrderingMessageCodec.TryReadSort(ref platform, message,
			SortMethod, out packet);

	public static uint DispatchMoveMemberRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupOrderingMessageCodec.TryReadMoveMember(ref platform,
			storage, MoveMemberMethod, out var value)) return 0;
		return value.Object ^ unchecked((uint)value.Position);
	}

	public static uint DispatchReorderRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupOrderingMessageCodec.TryReadReorder(ref platform, storage,
			ReorderMethod, out var value)) return 0;
		return value.After ^ value.Objects;
	}

	public static uint DispatchSortRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupOrderingMessageCodec.TryReadSort(ref platform, storage,
			SortMethod, out var value)) return 0;
		return value.Objects;
	}

	public static bool MoveMember<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, APTR child, int position)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiGroupChangeCore.IsGroupObject(ref platform, state, group) ||
			child.IsNull) return false;
		var count = CountChildren(ref platform, state, group, child,
			out var contains);
		if (!contains || count == 0) return false;
		APTR predecessor;
		if (position == 0) predecessor = APTR.Null;
		else if (position == -1)
			predecessor = ChildAt(ref platform, state, group, count - 1);
		else if (position > 0)
		{
			if ((uint)position > count) return false;
			predecessor = ChildAt(ref platform, state, group,
				(uint)position - 1);
		}
		else
		{
			var rank = (uint)(-(position + 1)) + 1u;
			if (rank > count) return false;
			predecessor = ChildAt(ref platform, state, group, count - rank);
		}
		if (position != 0 && predecessor.IsNull) return false;
		return MuiFamilyCore.MoveAfter(ref platform, state, group, child,
			predecessor);
	}

	public static bool Reorder<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, APTR after, APTR objects)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiGroupChangeCore.IsGroupObject(ref platform, state, group))
			return false;
		if (!TryValidateVector(ref platform, state, group, objects, false,
			out var count)) return false;
		if (after.Raw == uint.MaxValue)
			return ReorderAfterExisting(ref platform, state, group, objects,
				count);
		if (after.IsNotNull && !IsDirectChild(ref platform, state, group,
			after)) return false;
		return MuiFamilyCore.Reorder(ref platform, state, group, after, objects);
	}

	public static bool Sort<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, APTR objects)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiGroupChangeCore.IsGroupObject(ref platform, state, group))
			return false;
		if (!TryValidateVector(ref platform, state, group, objects, true,
			out _)) return false;
		return MuiFamilyCore.Sort(ref platform, state, group, objects);
	}

	private static uint CountChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, APTR target, out bool contains)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		contains = false;
		var count = 0u;
		while (count < MuiHeadlessLayout.MaximumTraversal)
		{
			var child = ChildAt(ref platform, state, group, count);
			if (child.IsNull) break;
			if (child.Raw == target.Raw) contains = true;
			count++;
		}
		return count;
	}

	private static bool TryValidateVector<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, APTR objects, bool requireAll, out uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		count = 0;
		var total = ChildCount(ref platform, state, group);
		if (objects.IsNull) return requireAll && total == 0;
		while (count < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!TryVectorAt(ref platform, objects, count, out var child))
				return false;
			if (child.IsNull) return !requireAll || count == total;
			if (!IsDirectChild(ref platform, state, group, child)) return false;
			for (var previous = 0u; previous < count; previous++)
			{
				if (VectorAt(ref platform, objects, previous).Raw == child.Raw)
					return false;
			}
			count++;
		}
		return false;
	}

	private static bool ReorderAfterExisting<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, APTR objects, uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var predecessor = APTR.Null;
		var total = ChildCount(ref platform, state, group);
		for (var index = 0u; index < total; index++)
		{
			var child = ChildAt(ref platform, state, group, index);
			if (!VectorContains(ref platform, objects, count, child))
				predecessor = child;
		}
		for (var index = 0u; index < count; index++)
		{
			var child = VectorAt(ref platform, objects, index);
			if (!MuiFamilyCore.MoveAfter(ref platform, state, group, child,
				predecessor)) return false;
			predecessor = child;
		}
		return true;
	}

	private static bool VectorContains<TPlatform>(ref TPlatform platform,
		APTR objects, uint count, APTR target)
		where TPlatform : struct, IMuiGuestMemory
	{
		for (var index = 0u; index < count; index++)
		{
			if (VectorAt(ref platform, objects, index).Raw == target.Raw)
				return true;
		}
		return false;
	}

	private static bool IsDirectChild<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, APTR target)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = ChildCount(ref platform, state, group);
		for (var index = 0u; index < count; index++)
		{
			if (ChildAt(ref platform, state, group, index).Raw == target.Raw)
				return true;
		}
		return false;
	}

	private static uint ChildCount<TPlatform>(ref TPlatform platform, APTR state,
		APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = 0u;
		while (count < MuiHeadlessLayout.MaximumTraversal &&
			ChildAt(ref platform, state, group, count).IsNotNull) count++;
		return count;
	}

	private static APTR VectorAt<TPlatform>(ref TPlatform platform, APTR objects,
		uint index) where TPlatform : struct, IMuiGuestMemory
	{
		return TryVectorAt(ref platform, objects, index, out var value) ? value :
			APTR.Null;
	}

	private static bool TryVectorAt<TPlatform>(ref TPlatform platform,
		APTR objects, uint index, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		var cursor = default(MuiFamilyMutationVectorCursor);
		cursor.Base = objects;
		cursor.Index = index;
		return MuiFamilyMutationVectorCodec.TryReadObject(ref platform, cursor,
			out value);
	}

	private static APTR ChildAt<TPlatform>(ref TPlatform platform, APTR state,
		APTR group, uint index) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiFamilyCore.GetChild(ref platform, state, group, (int)index,
			APTR.Null);

	private static void WriteMoveMember<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGroupMoveMemberMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupOrderingMessageCodec.WriteMoveMember(ref platform, storage,
			packet);

	private static void WriteReorder<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGroupReorderMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupOrderingMessageCodec.WriteReorder(ref platform, storage,
			packet);

	private static void WriteSort<TPlatform>(ref TPlatform platform, APTR storage,
		MuiGroupSortMessage packet) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupOrderingMessageCodec.WriteSort(ref platform, storage, packet);
}
