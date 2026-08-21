/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupMoveMemberMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint Object;
	public int Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupReorderMessage
{
	public const uint Size = 12;
	public uint MethodId;
	public uint After;
	public uint Objects;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupSortMessage
{
	public const uint Size = 8;
	public uint MethodId;
	public uint Objects;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupOrderingMethodMessage
{
	public const uint Size = 4;
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

internal static class MuiGroupOrderingPacketFieldCursorCodec
{
	private static bool TryResolve(MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, out uint offset, out uint size,
		out uint fieldSize)
	{
		offset = 0;
		size = 0;
		fieldSize = 0;
		switch (packet)
		{
			case MuiGroupOrderingPacketKind.Header:
				size = MuiGroupOrderingMethodMessage.Size;
				offset = field == MuiGroupOrderingPacketField.MethodId ? 0u :
					uint.MaxValue;
				fieldSize = 4;
				break;
			case MuiGroupOrderingPacketKind.MoveMember:
				size = MuiGroupMoveMemberMessage.Size;
				offset = field switch
				{
					MuiGroupOrderingPacketField.MethodId => 0,
					MuiGroupOrderingPacketField.Object => 4,
					MuiGroupOrderingPacketField.Position => 8,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
			case MuiGroupOrderingPacketKind.Reorder:
				size = MuiGroupReorderMessage.Size;
				offset = field switch
				{
					MuiGroupOrderingPacketField.MethodId => 0,
					MuiGroupOrderingPacketField.After => 4,
					MuiGroupOrderingPacketField.Objects => 8,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
			case MuiGroupOrderingPacketKind.Sort:
				size = MuiGroupSortMessage.Size;
				offset = field switch
				{
					MuiGroupOrderingPacketField.MethodId => 0,
					MuiGroupOrderingPacketField.Objects => 4,
					_ => uint.MaxValue,
				};
				fieldSize = 4;
				break;
			default:
				offset = uint.MaxValue;
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupOrderingPacketFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset,
			out var packetSize, out fieldSize) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Message, packetSize)) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupOrderingPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGroupOrderingPacketKind packet,
		MuiGroupOrderingPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupOrderingPacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGroupOrderingMessageCodec
{
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupOrderingMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupOrderingPacketKind.Header,
			MuiGroupOrderingPacketField.MethodId, out value.MethodId);
	}

	internal static bool WriteMoveMember<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupMoveMemberMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupOrderingPacketKind.MoveMember,
			MuiGroupOrderingPacketField.MethodId, value.MethodId) &&
			MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupOrderingPacketKind.MoveMember,
				MuiGroupOrderingPacketField.Object, value.Object) &&
			MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupOrderingPacketKind.MoveMember,
				MuiGroupOrderingPacketField.Position, unchecked((uint)value.Position));
	}

	internal static bool WriteReorder<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupReorderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupOrderingPacketKind.Reorder,
			MuiGroupOrderingPacketField.MethodId, value.MethodId) &&
			MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupOrderingPacketKind.Reorder,
				MuiGroupOrderingPacketField.After, value.After) &&
			MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupOrderingPacketKind.Reorder,
				MuiGroupOrderingPacketField.Objects, value.Objects);
	}

	internal static bool WriteSort<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupSortMessage value) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupOrderingPacketKind.Sort,
			MuiGroupOrderingPacketField.MethodId, value.MethodId) &&
			MuiGroupOrderingPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupOrderingPacketKind.Sort,
				MuiGroupOrderingPacketField.Objects, value.Objects);
	}

	internal static bool TryReadMoveMember<TPlatform>(ref TPlatform platform,
		APTR address, uint method, out MuiGroupMoveMemberMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, address, out var header) ||
			header.MethodId != method || !platform.IsMapped(address,
			MuiGroupMoveMemberMessage.Size)) return false;
		value.MethodId = header.MethodId;
		if (!MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupOrderingPacketKind.MoveMember,
			MuiGroupOrderingPacketField.Object, out value.Object) ||
			!MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupOrderingPacketKind.MoveMember,
				MuiGroupOrderingPacketField.Position, out var position)) return false;
		value.Position = unchecked((int)position);
		return true;
	}

	internal static bool TryReadReorder<TPlatform>(ref TPlatform platform,
		APTR address, uint method, out MuiGroupReorderMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, address, out var header) ||
			header.MethodId != method || !platform.IsMapped(address,
			MuiGroupReorderMessage.Size)) return false;
		value.MethodId = header.MethodId;
		if (!MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupOrderingPacketKind.Reorder,
			MuiGroupOrderingPacketField.After, out value.After) ||
			!MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupOrderingPacketKind.Reorder,
				MuiGroupOrderingPacketField.Objects, out value.Objects)) return false;
		return true;
	}

	internal static bool TryReadSort<TPlatform>(ref TPlatform platform,
		APTR address, uint method, out MuiGroupSortMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadMethodId(ref platform, address, out var header) ||
			header.MethodId != method || !platform.IsMapped(address,
			MuiGroupSortMessage.Size)) return false;
		value.MethodId = header.MethodId;
		if (!MuiGroupOrderingPacketFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGroupOrderingPacketKind.Sort,
			MuiGroupOrderingPacketField.Objects, out value.Objects)) return false;
		return true;
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
		if (!MuiFamilyMutationVectorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return false;
		if (!MuiFamilyMutationVectorCodec.TryRead(ref platform, address,
			out var entry)) return false;
		value = entry.Object;
		return true;
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
