/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiWindowEventHandlerPacketInput
{
	public const uint Size = 8;
	public uint MethodId;
	public APTR Handler;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowEventHandlerPacket
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal APTR Handler;
}

// The public packet helper accepts the stable input record above, while this
// codec owns the packed guest boundary and named method validation. Keeping
// the method word in a typed header prevents callers from treating an
// arbitrary first ULONG as an event-handler request.
internal static class MuiWindowEventHandlerPacketCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowEventHandlerPacket packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiApplicationMethodHeaderCodec.TryRead(ref platform, address,
			out var header) || !IsMethod(header.MethodId) ||
			!MuiWindowEventHandlerPacketFieldCursorCodec.TryReadUInt32(ref platform,
				address, header.MethodId ==
					MuiApplicationDispatcher.WindowAddEventHandlerMethod
					? MuiWindowEventHandlerPacketKind.Add
					: MuiWindowEventHandlerPacketKind.Remove,
				MuiWindowEventHandlerPacketField.Handler, out var handler))
			return false;
		packet.MethodId = header.MethodId;
		packet.Handler = APTR.FromPointer(handler);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowEventHandlerPacket packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(packet.MethodId) || address.IsNull ||
			!MuiWindowEventHandlerPacketFieldCursorCodec.TryWriteUInt32(
				ref platform, address, packet.MethodId ==
					MuiApplicationDispatcher.WindowAddEventHandlerMethod
					? MuiWindowEventHandlerPacketKind.Add
					: MuiWindowEventHandlerPacketKind.Remove,
				MuiWindowEventHandlerPacketField.MethodId, packet.MethodId) ||
			!MuiWindowEventHandlerPacketFieldCursorCodec.TryWriteUInt32(
				ref platform, address, packet.MethodId ==
					MuiApplicationDispatcher.WindowAddEventHandlerMethod
					? MuiWindowEventHandlerPacketKind.Add
					: MuiWindowEventHandlerPacketKind.Remove,
				MuiWindowEventHandlerPacketField.Handler, packet.Handler.Raw))
			return false;
		return true;
	}

	private static bool IsMethod(uint method) =>
		method == MuiApplicationDispatcher.WindowAddEventHandlerMethod ||
		method == MuiApplicationDispatcher.WindowRemoveEventHandlerMethod;
}

public static class MuiWindowEventHandlerPacketCore
{
	public static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowEventHandlerPacketInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiWindowEventHandlerPacket);
		packet.MethodId = input.MethodId;
		packet.Handler = input.Handler;
		return MuiWindowEventHandlerPacketCodec.Write(ref platform, address,
			packet);
	}

	public static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowEventHandlerPacketInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		input = default;
		if (!MuiWindowEventHandlerPacketCodec.TryRead(ref platform, address,
			out var packet)) return false;
		input.MethodId = packet.MethodId;
		input.Handler = packet.Handler;
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowNodeRecord
{
	internal const uint Size = 20;
	// The Packet member is the first word of the inline method packet. The
	// allocated node reserves the full 20-byte record, but payload begins at
	// this ABI-defined member offset.
	internal const uint PayloadOffset = 16;
	internal APTR Next;
	internal APTR Value;
	internal uint Sequence;
	internal uint Auxiliary;
	internal uint Packet;
}

internal enum MuiApplicationWindowNodeField : byte
{
	Next,
	Value,
	Sequence,
	Auxiliary,
	Packet,
}

// Named view of the persistent 20-byte ApplicationWindow node record. The
// payload cursor below owns the separate inline-method boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowNodeFieldCursor
{
	internal APTR Address;
	internal MuiApplicationWindowNodeField Field;
}

internal static class MuiApplicationWindowNodeFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationWindowNodeField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationWindowNodeField.Next:
				offset = 0;
				return true;
			case MuiApplicationWindowNodeField.Value:
				offset = 4;
				return true;
			case MuiApplicationWindowNodeField.Sequence:
				offset = 8;
				return true;
			case MuiApplicationWindowNodeField.Auxiliary:
				offset = 12;
				return true;
			case MuiApplicationWindowNodeField.Packet:
				offset = 16;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowNodeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiApplicationWindowNodeRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationWindowNodeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationWindowNodeFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationWindowNodeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationWindowNodeFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

// Named view of the variable payload beginning at the node's Packet field.
// Keeping the node address and requested byte count together prevents callers
// from repeating the fixed record-to-payload boundary arithmetic.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowNodePayloadCursor
{
	internal APTR Node;
	internal uint ByteCount;
}

internal static class MuiApplicationWindowNodePayloadCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowNodePayloadCursor cursor, out APTR payload)
		where TPlatform : struct, IMuiGuestMemory
	{
		payload = APTR.Null;
		if (cursor.Node.IsNull || cursor.ByteCount == 0 || cursor.Node.Raw >
			uint.MaxValue - MuiApplicationWindowNodeRecord.PayloadOffset)
			return false;
		payload = APTR.FromPointer(cursor.Node.Raw +
			MuiApplicationWindowNodeRecord.PayloadOffset);
		if (payload.Raw > uint.MaxValue - cursor.ByteCount) return false;
		return platform.IsMapped(payload, cursor.ByteCount);
	}
}

internal static class MuiApplicationWindowNodeCodec
{
	internal static bool TryGetPayload<TPlatform>(ref TPlatform platform,
		APTR address, uint payloadBytes, out APTR payload)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationWindowNodePayloadCursor);
		cursor.Node = address;
		cursor.ByteCount = payloadBytes;
		return MuiApplicationWindowNodePayloadCursorCodec.TryGetAddress(
			ref platform, cursor, out payload);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationWindowNodeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiApplicationWindowNodeFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiApplicationWindowNodeField.Next, out var next) ||
			!MuiApplicationWindowNodeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowNodeField.Value, out var value) ||
			!MuiApplicationWindowNodeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowNodeField.Sequence, out record.Sequence) ||
			!MuiApplicationWindowNodeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowNodeField.Auxiliary,
				out record.Auxiliary) ||
			!MuiApplicationWindowNodeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationWindowNodeField.Packet, out record.Packet))
			return false;
		record.Next = APTR.FromPointer(next);
		record.Value = APTR.FromPointer(value);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationWindowNodeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationWindowNodeField.Next, record.Next.Raw) &&
			MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowNodeField.Value, record.Value.Raw) &&
			MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowNodeField.Sequence, record.Sequence) &&
			MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowNodeField.Auxiliary, record.Auxiliary) &&
			MuiApplicationWindowNodeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationWindowNodeField.Packet, record.Packet);
	}
}

// MUIM_Window_SetCycleChain receives a NULL-terminated APTR vector. Keep each
// caller-owned element as a named one-field record so the replacement path
// does not interpret the vector through an unexplained ULONG offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowCycleChainSlot
{
	internal const uint Size = 4;
	internal APTR Object;
}

internal static class MuiApplicationWindowCycleChainSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationWindowCycleChainSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowCycleChainSlot.Size)) return false;
		value.Object = APTR.FromPointer(platform.ReadUInt32(address, 0));
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationWindowCycleChainSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowCycleChainSlot.Size)) return false;
		platform.WriteUInt32(address, 0, value.Object.Raw);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowCycleChainCursor
{
	internal const uint EntrySize = MuiApplicationWindowCycleChainSlot.Size;
	internal const uint MaximumEntries = MuiHeadlessLayout.MaximumTraversal;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiApplicationWindowCycleChainVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowCycleChainCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >=
			MuiApplicationWindowCycleChainCursor.MaximumEntries ||
			cursor.Index > (uint.MaxValue - cursor.Base.Raw) /
			MuiApplicationWindowCycleChainCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiApplicationWindowCycleChainCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationWindowCycleChainCursor.EntrySize);
	}
}

// MUIA_Application_ReturnID/Signal input uses an optional caller-owned ULONG
// as signal storage. Keep that scalar named so input publication does not
// write an unexplained offset directly.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowSignalStorage
{
	internal const uint Size = 4;
	internal uint Signals;
}

internal static class MuiApplicationWindowSignalStorageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationWindowSignalStorage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowSignalStorage.Size)) return false;
		value.Signals = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationWindowSignalStorage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowSignalStorage.Size)) return false;
		platform.WriteUInt32(address, 0, value.Signals);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiEventHandlerNodeRecord
{
	public const uint Size = 24;
	public APTR NodeSuccessor;
	public APTR NodePredecessor;
	public byte Reserved;
	public sbyte Priority;
	public ushort Flags;
	public APTR Object;
	public APTR Class;
	public uint Events;
}

internal enum MuiEventHandlerNodeField : byte
{
	NodeSuccessor,
	NodePredecessor,
	Reserved,
	Priority,
	Flags,
	Object,
	Class,
	Events,
}

// Named view of the MorphOS MUI event-handler node. The record contains
// mixed-width fields, so all boundary arithmetic is kept in this resolver;
// callers work in terms of the ABI field names rather than numeric offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiEventHandlerNodeFieldCursor
{
	internal APTR Address;
	internal MuiEventHandlerNodeField Field;
}

internal static class MuiEventHandlerNodeFieldCursorCodec
{
	private static bool TryResolve(MuiEventHandlerNodeField field,
		out uint offset, out uint size)
	{
		switch (field)
		{
			case MuiEventHandlerNodeField.NodeSuccessor:
				offset = 0;
				size = 4;
				return true;
			case MuiEventHandlerNodeField.NodePredecessor:
				offset = 4;
				size = 4;
				return true;
			case MuiEventHandlerNodeField.Reserved:
				offset = 8;
				size = 1;
				return true;
			case MuiEventHandlerNodeField.Priority:
				offset = 9;
				size = 1;
				return true;
			case MuiEventHandlerNodeField.Flags:
				offset = 10;
				size = 2;
				return true;
			case MuiEventHandlerNodeField.Object:
				offset = 12;
				size = 4;
				return true;
			case MuiEventHandlerNodeField.Class:
				offset = 16;
				size = 4;
				return true;
			case MuiEventHandlerNodeField.Events:
				offset = 20;
				size = 4;
				return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiEventHandlerNodeFieldCursor cursor, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryResolve(cursor.Field, out var offset, out size) ||
			cursor.Address.IsNull || cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiEventHandlerNodeRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, size);
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiEventHandlerNodeField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiEventHandlerNodeFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress, out var size) ||
			size != 1) return false;
		value = platform.ReadUInt8(fieldAddress, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiEventHandlerNodeField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiEventHandlerNodeFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress, out var size) ||
			size != 2) return false;
		value = platform.ReadUInt16(fieldAddress, 0);
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiEventHandlerNodeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiEventHandlerNodeFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress, out var size) ||
			size != 4) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiEventHandlerNodeField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiEventHandlerNodeFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress, out var size) ||
			size != 1) return false;
		platform.WriteUInt8(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiEventHandlerNodeField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiEventHandlerNodeFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress, out var size) ||
			size != 2) return false;
		platform.WriteUInt16(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiEventHandlerNodeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiEventHandlerNodeFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress, out var size) ||
			size != 4) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiEventHandlerNodeCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiEventHandlerNodeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiEventHandlerNodeRecord.Size)) return false;
		if (!MuiEventHandlerNodeFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiEventHandlerNodeField.NodeSuccessor, out var successor) ||
			!MuiEventHandlerNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiEventHandlerNodeField.NodePredecessor, out var predecessor) ||
			!MuiEventHandlerNodeFieldCursorCodec.TryReadUInt8(ref platform, address,
				MuiEventHandlerNodeField.Reserved, out var reserved) ||
			!MuiEventHandlerNodeFieldCursorCodec.TryReadUInt8(ref platform, address,
				MuiEventHandlerNodeField.Priority, out var priority) ||
			!MuiEventHandlerNodeFieldCursorCodec.TryReadUInt16(ref platform, address,
				MuiEventHandlerNodeField.Flags, out var flags) ||
			!MuiEventHandlerNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiEventHandlerNodeField.Object, out var @object) ||
			!MuiEventHandlerNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiEventHandlerNodeField.Class, out var @class) ||
			!MuiEventHandlerNodeFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiEventHandlerNodeField.Events, out var events)) return false;
		record.NodeSuccessor = APTR.FromPointer(successor);
		record.NodePredecessor = APTR.FromPointer(predecessor);
		record.Reserved = reserved;
		record.Priority = unchecked((sbyte)priority);
		record.Flags = flags;
		record.Object = APTR.FromPointer(@object);
		record.Class = APTR.FromPointer(@class);
		record.Events = events;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiEventHandlerNodeRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiEventHandlerNodeRecord.Size)) return false;
		return MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiEventHandlerNodeField.NodeSuccessor, record.NodeSuccessor.Raw) &&
			MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiEventHandlerNodeField.NodePredecessor, record.NodePredecessor.Raw) &&
			MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt8(ref platform, address,
				MuiEventHandlerNodeField.Reserved, record.Reserved) &&
			MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt8(ref platform, address,
				MuiEventHandlerNodeField.Priority, unchecked((byte)record.Priority)) &&
			MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt16(ref platform, address,
				MuiEventHandlerNodeField.Flags, record.Flags) &&
			MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiEventHandlerNodeField.Object, record.Object.Raw) &&
			MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiEventHandlerNodeField.Class, record.Class.Raw) &&
			MuiEventHandlerNodeFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiEventHandlerNodeField.Events, record.Events);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiInputHandlerRecord
{
	internal const uint Size = 24;
	internal uint NodeSuccessor;
	internal uint NodePredecessor;
	internal APTR Object;
	internal uint Events;
	internal uint Reserved;
	internal uint Packet;
}

internal enum MuiInputHandlerField : byte
{
	NodeSuccessor,
	NodePredecessor,
	Object,
	Events,
	Reserved,
	Packet,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiInputHandlerFieldCursor
{
	internal APTR Address;
	internal MuiInputHandlerField Field;
}

internal static class MuiInputHandlerFieldCursorCodec
{
	private static bool TryResolve(MuiInputHandlerField field, out uint offset)
	{
		switch (field)
		{
			case MuiInputHandlerField.NodeSuccessor:
				offset = 0;
				return true;
			case MuiInputHandlerField.NodePredecessor:
				offset = 4;
				return true;
			case MuiInputHandlerField.Object:
				offset = 8;
				return true;
			case MuiInputHandlerField.Events:
				offset = 12;
				return true;
			case MuiInputHandlerField.Reserved:
				offset = 16;
				return true;
			case MuiInputHandlerField.Packet:
				offset = 20;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiInputHandlerFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiInputHandlerRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiInputHandlerField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiInputHandlerFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiInputHandlerField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiInputHandlerFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiInputHandlerCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiInputHandlerRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiInputHandlerRecord.Size)) return false;
		if (!MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiInputHandlerField.NodeSuccessor, out record.NodeSuccessor) ||
			!MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiInputHandlerField.NodePredecessor, out record.NodePredecessor) ||
			!MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiInputHandlerField.Object, out var @object) ||
			!MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiInputHandlerField.Events, out record.Events) ||
			!MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiInputHandlerField.Reserved, out record.Reserved) ||
			!MuiInputHandlerFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiInputHandlerField.Packet, out record.Packet)) return false;
		record.Object = APTR.FromPointer(@object);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiApplicationWindowNodeInput
{
	public APTR Next;
	public APTR Value;
	public uint Sequence;
	public uint Auxiliary;
	public uint Packet;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiEventHandlerNodeInput
{
	// MorphOS MUI_EventHandlerNode flags. The low event-routing bits are
	// caller-supplied; ISACTIVE and ISENABLED are maintained as read-only
	// registration state, while ISCALLING is transient during callbacks.
	// Keeping them on the ABI struct avoids scattering magic offsets or
	// untyped flag literals through the routing code.
	public const ushort MUI_EHF_ALWAYSKEYS = 0x0001;
	public const ushort MUI_EHF_GUIMODE = 0x0002;
	public const ushort MUI_EHF_PRIORITY = 0x0800;
	public const ushort MUI_EHF_ISACTIVEGRP = 0x1000;
	public const ushort MUI_EHF_ISACTIVE = 0x2000;
	public const ushort MUI_EHF_ISCALLING = 0x4000;
	public const ushort MUI_EHF_ISENABLED = 0x8000;
	public APTR Successor;
	public APTR Predecessor;
	public byte Reserved;
	public sbyte Priority;
	public ushort Flags;
	public APTR Object;
	public APTR Class;
	public uint Events;
}

// Struct-first qualification surfaces for the guest-resident Application and
// Window list nodes. Production list operations remain owned by the core; the
// public seams only prove their fixed layouts round-trip without managed state.
public static class MuiApplicationWindowRecordPacketCore
{
	public static bool WriteNode<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationWindowNodeInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiApplicationWindowNodeRecord);
		record.Next = input.Next;
		record.Value = input.Value;
		record.Sequence = input.Sequence;
		record.Auxiliary = input.Auxiliary;
		record.Packet = input.Packet;
		return MuiApplicationWindowNodeCodec.Write(ref platform, address, record);
	}

	public static uint DispatchNode<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, address,
			out var record)) return 0;
		return record.Next.Raw ^ record.Value.Raw ^ record.Sequence ^
			record.Auxiliary ^ record.Packet;
	}

	public static bool WriteEventHandler<TPlatform>(ref TPlatform platform,
		APTR address, MuiEventHandlerNodeInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiEventHandlerNodeRecord);
		record.NodeSuccessor = input.Successor;
		record.NodePredecessor = input.Predecessor;
		record.Reserved = input.Reserved;
		record.Priority = input.Priority;
		record.Flags = input.Flags;
		record.Object = input.Object;
		record.Class = input.Class;
		record.Events = input.Events;
		return MuiEventHandlerNodeCodec.Write(ref platform, address, record);
	}

	public static bool TryReadEventHandler<TPlatform>(ref TPlatform platform,
		APTR address, out MuiEventHandlerNodeInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		input = default;
		if (!MuiEventHandlerNodeCodec.TryRead(ref platform, address,
			out var record)) return false;
		input.Successor = record.NodeSuccessor;
		input.Predecessor = record.NodePredecessor;
		input.Reserved = record.Reserved;
		input.Priority = record.Priority;
		input.Flags = record.Flags;
		input.Object = record.Object;
		input.Class = record.Class;
		input.Events = record.Events;
		return true;
	}

	public static uint DispatchEventHandler<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiEventHandlerNodeCodec.TryRead(ref platform, address,
			out var record)) return 0;
			return record.NodeSuccessor.Raw ^ record.NodePredecessor.Raw ^
				record.Reserved ^ (uint)(byte)record.Priority ^ record.Flags ^ record.Object.Raw ^
			record.Class.Raw ^ record.Events;
	}
}

public static class MuiApplicationWindowCore
{
	private const uint ReturnHead = 0x7FFE0001;
	private const uint ReturnTail = 0x7FFE0002;
	private const uint InputHandlers = 0x7FFE0003;
	private const uint SignalMask = 0x7FFE0004;
	private const uint PushHead = 0x7FFE0005;
	private const uint PushTail = 0x7FFE0006;
	private const uint WindowOwner = 0x7FFE0010;
	private const uint NativeWindow = 0x7FFE0011;
	private const uint EventHandlers = 0x7FFE0012;
	private const uint EventMask = 0x7FFE0013;
	private const uint WindowSleepDepth = 0x7FFE003D;
	private const uint WindowSleepDisabled = 0x7FFE003E;
	private const uint ApplicationSleepDepth = 0x7FFE003F;
	// A guest-resident marker remembers a window that was open immediately
	// before application iconification, or that was explicitly opened while
	// the application was already iconified.  It is deliberately separate
	// from MUIA_Window_Open, which describes the currently native window.
	private const uint WindowIconifiedOpen = 0x7FFE0040;
	private const uint ActiveObject = 0x80427925;
	private const uint DefaultObject = 0x804294D7;
	private const uint WindowDisableKeys = MuiWindowPublicCore.DisableKeys;
	private const uint WindowActivate = 0x80428D2F;
	private const uint WindowSleep = 0x8042E7DB;
	private const ushort EventHandlerGuiMode =
		MuiEventHandlerNodeInput.MUI_EHF_GUIMODE;
	private const uint EventClassActiveWindow = 0x00040000;
	private const uint EventClassInactiveWindow = 0x00080000;
	private const uint EventClassChangeWindow = 0x02000000;
	private const uint EventHandlerEat = 1;
	private const ushort EventHandlerAlwaysKeys =
		MuiEventHandlerNodeInput.MUI_EHF_ALWAYSKEYS;
	private const ushort EventHandlerPriority =
		MuiEventHandlerNodeInput.MUI_EHF_PRIORITY;
	private const ushort EventHandlerCalling =
		MuiEventHandlerNodeInput.MUI_EHF_ISCALLING;
	private const ushort EventHandlerEnabled =
		MuiEventHandlerNodeInput.MUI_EHF_ISENABLED;
	private const ushort EventHandlerActive =
		MuiEventHandlerNodeInput.MUI_EHF_ISACTIVE;
	private const uint DispatchPriorityProbe = 0x80000000u;
	private const uint DispatchPrioritySkip = 0x40000000u;
	private const uint DispatchPriorityOnly = 0x20000000u;
	private const uint DispatchPriorityVisited = 2;
	private const int MuiKeyNone = -1;
	private const uint Disabled = 0x80423661;
	private const uint ShowMe = 0x80429BA8;
	private const uint IsShown = 0x7FFF0003;
	private const uint GroupPageMode = 0x80421A5F;
	private const uint GroupActivePage = 0x80424199;
	private const uint VirtgroupWidth = 0x80427C49;
	private const uint VirtgroupHeight = 0x80423038;
	private const uint AreaLeftEdge = 0x8042BEC6;
	private const uint AreaTopEdge = 0x8042509B;
	private const uint AreaWidth = 0x8042B59C;
	private const uint AreaHeight = 0x80423237;
	private const uint ActiveObjectNext = uint.MaxValue;
	private const uint ActiveObjectPrevious = uint.MaxValue - 1;
	private const uint ActiveObjectLeft = uint.MaxValue - 2;
	private const uint ActiveObjectRight = uint.MaxValue - 3;
	private const uint ActiveObjectUp = uint.MaxValue - 4;
	private const uint ActiveObjectDown = uint.MaxValue - 5;
	private const uint LeftEdge = 0x8042BEC6;
	private const uint TopEdge = 0x8042509B;
	private const uint RightEdge = 0x8042BA82;
	private const uint BottomEdge = 0x8042E552;
	private const uint WindowOpen = MuiWindowPublicCore.Open;
	private const uint WindowId = MuiWindowPublicCore.Id;
	private const uint WindowCloseRequest = MuiWindowPublicCore.CloseRequest;
	private const uint WindowSnapshotFlags = 0x7FFE0037;
	private const uint WindowSnapshotRequests = 0x7FFE0038;
	private const uint WindowCycleChainHead = 0x7FFE0039;
	private const uint WindowCycleChainCount = 0x7FFE003A;
	private const uint WindowCycleChainRequests = 0x7FFE003B;
	private const uint ApplicationIconified = 0x8042A07F;
	private const uint ApplicationActive = 0x804260AB;
	private const uint ApplicationDoubleStart = 0x80423BC6;
	private const uint ApplicationSingleTask = 0x8042A2C8;
	private const uint ApplicationForceQuit = 0x804257DF;
	private const uint ApplicationUseRexx = 0x80422387;
	private const uint ApplicationUseCommodities = 0x80425EE5;
	private const uint ApplicationDiskObject = 0x804235CB;
	private const uint ApplicationDropObject = 0x80421266;
	private const uint ApplicationMenustrip = 0x804252D9;
	private const uint ApplicationMenuAction = 0x80428961;
	private const uint ApplicationMenuHelp = 0x8042540B;
	private const uint ObjectUserData = 0x80420313;
	private const uint ApplicationAuthor = 0x80424842;
	private const uint ApplicationBase = 0x8042E07A;
	private const uint ApplicationCopyright = 0x8042EF4D;
	private const uint ApplicationDescription = 0x80421FC6;
	private const uint ApplicationTitle = 0x804281B8;
	private const uint ApplicationVersion = 0x8042B33F;
	private const uint ApplicationHelpFile = 0x804293F4;
	private const uint ApplicationIconifyTitle = 0x80422CB8;
	private const uint ApplicationUseScreenNotify = 0x80420861;
	private const uint ApplicationWindow = 0x8042BFE0;
	private const uint ApplicationUsedClasses = 0x8042E9A7;
	private const uint ApplicationSleep = 0x80425711;
	private const uint ApplicationInitialized = 0x7FFE0044;
	private const uint ApplicationAboutRefWindow = 0x7FFE0020;
	private const uint ApplicationAboutRequests = 0x7FFE0021;
	private const uint ApplicationRefreshChecks = 0x7FFE0022;
	private const uint ApplicationRefreshWindows = 0x7FFE0023;
	private const uint ApplicationHelpWindow = 0x7FFE0024;
	private const uint ApplicationHelpName = 0x7FFE0025;
	private const uint ApplicationHelpNode = 0x7FFE0026;
	private const uint ApplicationHelpLine = 0x7FFE0027;
	private const uint ApplicationHelpRequests = 0x7FFE0028;
	private const uint ApplicationDefaultConfigId = 0x7FFE0029;
	private const uint ApplicationDefaultConfigValue = 0x7FFE002A;
	private const uint ApplicationDefaultConfigRequests = 0x7FFE002B;
	private const uint ApplicationConfigWindowFlags = 0x7FFE002C;
	private const uint ApplicationConfigWindowClassId = 0x7FFE002D;
	private const uint ApplicationConfigWindowRequests = 0x7FFE002E;
	private const uint ApplicationSettingsPanelNumber = 0x7FFE002F;
	private const uint ApplicationSettingsPanelObject = 0x7FFE0030;
	private const uint ApplicationSettingsPanelRequests = 0x7FFE0031;
	private const uint ApplicationSettingsOperation = 0x7FFE0032;
	private const uint ApplicationSettingsName = 0x7FFE0033;
	private const uint ApplicationSettingsRequests = 0x7FFE0034;
	private const uint ApplicationSettingsSaves = 0x7FFE0035;
	private const uint ApplicationSettingsLoads = 0x7FFE0036;
	private const uint ApplicationSetConfigItemState = 0x7FFE003C;
	private const uint ApplicationLifecycleStateKey = 0x7F0A0001u;
	private const uint WindowLifecycleStateKey = 0x7F0A0002u;
	private const uint WindowOpenPolicyStateKey = 0x7F0A0003u;
	private const uint WindowSleepStateKey = 0x7F0A0006u;
	private const uint ApplicationSleepStateKey = 0x7F0A0007u;
	private const uint ApplicationSchedulerStateKey = 0x7F0A0008u;
	private const uint WindowInteractionStateKey = 0x7F0A0009u;
	private const uint WindowEventStateKey = 0x7F0A000Au;
	private const uint ApplicationHelpStateKey = 0x7F0A000Bu;
	private const uint ApplicationDefaultConfigStateKey = 0x7F0A000Cu;
	private const uint ApplicationConfigWindowStateKey = 0x7F0A000Du;
	private const uint ApplicationSettingsPanelStateKey = 0x7F0A000Eu;
	private const uint ApplicationSettingsPersistenceStateKey = 0x7F0A000Fu;
	private const uint ApplicationRefreshStateKey = 0x7F0A0010u;
	private const uint ApplicationMenuStateKey = 0x7F0A0011u;
	private const uint ApplicationObjectStateKey = 0x7F0A0012u;
	private const uint ApplicationTextStateKey = 0x7F0A0013u;
	private const uint ApplicationIdentityStateKey = 0x7F0A0014u;
	private const uint ApplicationPolicyStateKey = 0x7F0A0015u;
	private const uint ApplicationUsedClassesStateKey = 0x7F0A0016u;
	private const uint ApplicationWindowRelationshipStateKey = 0x7F0A0017u;
	private const uint WindowFocusStateKey = 0x7F0A001Cu;
	private const uint HelpFirstOpenWindow = uint.MaxValue;
	private const uint SignalBreakCtrlC = 1u << 12;
	private const uint MaximumRunIterations = 65535;
	private struct MuiSpatialBox
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	internal static bool TryGetApplicationLifecycleState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationLifecycleStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationLifecycleStateKey) !=
			unchecked((int)MuiApplicationLifecycleStateRecord.Size)) return false;
		return MuiApplicationLifecycleStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiApplicationLifecycleStateRecord ReadApplicationLifecycle<TPlatform>(
		ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationLifecycle(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationLifecycleStateRecord.Cookie;
		FillApplicationLifecycle(ref platform, state, application, ref value);
		return value;
	}

	private static bool PublishApplicationLifecycle<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationLifecycleStateKey);
		if (TryGetApplicationLifecycleState(ref platform, state, application,
			out value))
		{
			FillApplicationLifecycle(ref platform, state, application, ref value);
			return MuiApplicationLifecycleStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiApplicationLifecycleStateRecord.Cookie;
		FillApplicationLifecycle(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationLifecycleStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationLifecycleStateRecord.Size);
		var written = MuiApplicationLifecycleStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationLifecycleStateKey, scratch,
			unchecked((int)MuiApplicationLifecycleStateRecord.Size));
		platform.Clear(scratch, MuiApplicationLifecycleStateRecord.Size);
		platform.Free(scratch, MuiApplicationLifecycleStateRecord.Size);
		return added;
	}

	private static void FillApplicationLifecycle<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, ref MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationInitialized, out var initialized)) initialized = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationIconified, out var iconified)) iconified = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationActive, out var active)) active = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationSingleTask, out var singleTask)) singleTask = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationDoubleStart, out var doubleStart)) doubleStart = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationForceQuit, out var forceQuit)) forceQuit = 0;
		value.Initialized = initialized != 0 ? 1u : 0u;
		value.Iconified = iconified != 0 ? 1u : 0u;
		value.Active = active != 0 ? 1u : 0u;
		value.SingleTask = singleTask != 0 ? 1u : 0u;
		value.DoubleStart = doubleStart != 0 ? 1u : 0u;
		value.ForceQuit = forceQuit != 0 ? 1u : 0u;
	}

	private static bool WriteApplicationLifecycle<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, MuiApplicationLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!PublishApplicationLifecycle(ref platform, state, application,
			out _)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationLifecycleStateKey);
		if (!MuiApplicationLifecycleStateRecordCodec.Write(ref platform, block,
			value)) return false;
		return Set(ref platform, state, application, ApplicationInitialized,
			value.Initialized) &&
			Set(ref platform, state, application, ApplicationIconified,
				value.Iconified) &&
			Set(ref platform, state, application, ApplicationActive,
				value.Active) &&
			Set(ref platform, state, application, ApplicationSingleTask,
				value.SingleTask) &&
			Set(ref platform, state, application, ApplicationDoubleStart,
				value.DoubleStart) &&
			Set(ref platform, state, application, ApplicationForceQuit,
				value.ForceQuit);
	}

	internal static bool TryGetWindowLifecycleState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowLifecycleStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowLifecycleStateKey) !=
			unchecked((int)MuiWindowLifecycleStateRecord.Size)) return false;
		return MuiWindowLifecycleStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiWindowLifecycleStateRecord ReadWindowLifecycle<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowLifecycle(ref platform, state, window, out var value))
			return value;
		value = default;
		value.Magic = MuiWindowLifecycleStateRecord.Cookie;
		FillWindowLifecycle(ref platform, state, window, ref value);
		return value;
	}

	private static bool PublishWindowLifecycle<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, out MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowLifecycleStateKey);
		if (TryGetWindowLifecycleState(ref platform, state, window, out value))
		{
			FillWindowLifecycle(ref platform, state, window, ref value);
			return MuiWindowLifecycleStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiWindowLifecycleStateRecord.Cookie;
		FillWindowLifecycle(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowLifecycleStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowLifecycleStateRecord.Size);
		var written = MuiWindowLifecycleStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowLifecycleStateKey, scratch,
			unchecked((int)MuiWindowLifecycleStateRecord.Size));
		platform.Clear(scratch, MuiWindowLifecycleStateRecord.Size);
		platform.Free(scratch, MuiWindowLifecycleStateRecord.Size);
		return added;
	}

	private static void FillWindowLifecycle<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, ref MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			NativeWindow, out var nativeWindow)) nativeWindow = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			WindowOpen, out var open)) open = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			EventMask, out var eventMask)) eventMask = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			WindowIconifiedOpen, out var iconifiedOpen)) iconifiedOpen = 0;
		value.NativeWindow = APTR.FromPointer(nativeWindow);
		value.Open = open != 0 ? 1u : 0u;
		value.EventMask = eventMask;
		value.IconifiedOpen = iconifiedOpen != 0 ? 1u : 0u;
	}

	private static bool WriteWindowLifecycle<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, MuiWindowLifecycleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!PublishWindowLifecycle(ref platform, state, window, out _)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowLifecycleStateKey);
		if (!MuiWindowLifecycleStateRecordCodec.Write(ref platform, block, value))
			return false;
		return Set(ref platform, state, window, NativeWindow,
			value.NativeWindow.Raw) &&
			Set(ref platform, state, window, WindowOpen, value.Open) &&
			Set(ref platform, state, window, EventMask, value.EventMask) &&
			Set(ref platform, state, window, WindowIconifiedOpen,
				value.IconifiedOpen);
	}

	internal static bool TryGetWindowOpenPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowOpenPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowOpenPolicyStateKey) !=
			unchecked((int)MuiWindowOpenPolicyStateRecord.Size)) return false;
		return MuiWindowOpenPolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiWindowOpenPolicyStateRecord ReadWindowOpenPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowOpenPolicy(ref platform, state, window, out var value))
			return value;
		value = default;
		value.Magic = MuiWindowOpenPolicyStateRecord.Cookie;
		FillWindowOpenPolicy(ref platform, state, window, ref value);
		return value;
	}

	internal static bool PublishWindowOpenPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, out MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowOpenPolicyStateKey);
		if (TryGetWindowOpenPolicyState(ref platform, state, window, out value))
		{
			FillWindowOpenPolicy(ref platform, state, window, ref value);
			return MuiWindowOpenPolicyStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiWindowOpenPolicyStateRecord.Cookie;
		FillWindowOpenPolicy(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowOpenPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowOpenPolicyStateRecord.Size);
		var written = MuiWindowOpenPolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowOpenPolicyStateKey, scratch,
			unchecked((int)MuiWindowOpenPolicyStateRecord.Size));
		platform.Clear(scratch, MuiWindowOpenPolicyStateRecord.Size);
		platform.Free(scratch, MuiWindowOpenPolicyStateRecord.Size);
		return added;
	}

	private static uint ReadWindowOpenPolicyRaw<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			attribute, out var value) ? value : 0;

	private static void FillWindowOpenPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, ref MuiWindowOpenPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.AlternateHeight = unchecked((int)ReadWindowOpenPolicyRaw(ref platform,
			state, window,
			MuiWindowPublicCore.AltHeight));
		value.AlternateWidth = unchecked((int)ReadWindowOpenPolicyRaw(ref platform,
			state, window,
			MuiWindowPublicCore.AltWidth));
		value.AlternateLeftEdge = unchecked((int)ReadWindowOpenPolicyRaw(ref platform,
			state, window,
			MuiWindowPublicCore.AltLeftEdge));
		value.AlternateTopEdge = unchecked((int)ReadWindowOpenPolicyRaw(ref platform,
			state, window,
			MuiWindowPublicCore.AltTopEdge));
		value.Height = unchecked((int)ReadWindowOpenPolicyRaw(ref platform, state,
			window,
			MuiWindowPublicCore.Height));
		value.Width = unchecked((int)ReadWindowOpenPolicyRaw(ref platform, state,
			window,
			MuiWindowPublicCore.Width));
		value.LeftEdge = unchecked((int)ReadWindowOpenPolicyRaw(ref platform, state,
			window,
			MuiWindowPublicCore.LeftEdge));
		value.TopEdge = unchecked((int)ReadWindowOpenPolicyRaw(ref platform, state,
			window,
			MuiWindowPublicCore.TopEdge));
		value.CloseGadget = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.CloseGadget);
		value.DepthGadget = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.DepthGadget);
		value.DragBar = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.DragBar);
		value.SizeGadget = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.SizeGadget);
		value.SizeRight = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.SizeRight);
		value.AppWindow = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.AppWindow);
		value.Backdrop = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.Backdrop);
		value.Borderless = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.Borderless);
		value.PanelWindow = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.PanelWindow);
		value.TabletMessages = ReadWindowOpenPolicyRaw(ref platform, state, window,
			MuiWindowPublicCore.TabletMessages);
		value.UseBottomBorderScroller = ReadWindowOpenPolicyRaw(ref platform, state,
			window,
			MuiWindowPublicCore.UseBottomBorderScroller);
		value.UseLeftBorderScroller = ReadWindowOpenPolicyRaw(ref platform, state,
			window,
			MuiWindowPublicCore.UseLeftBorderScroller);
		value.UseRightBorderScroller = ReadWindowOpenPolicyRaw(ref platform, state,
			window,
			MuiWindowPublicCore.UseRightBorderScroller);
	}

	internal static bool TryGetWindowSleepState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform => TryGetSleepState(
		ref platform, state, window, WindowSleepStateKey, WindowSleepDepth,
		WindowSleepDisabled, WindowSleep, out value);

	internal static bool TryGetApplicationSleepState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform => TryGetSleepState(
		ref platform, state, application, ApplicationSleepStateKey,
		ApplicationSleepDepth, 0, ApplicationSleep, out value);

	private static MuiSleepStateRecord ReadWindowSleepState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform => ReadSleepState(
		ref platform, state, window, WindowSleepStateKey, WindowSleepDepth,
		WindowSleepDisabled, WindowSleep);

	private static MuiSleepStateRecord ReadApplicationSleepState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform => ReadSleepState(
		ref platform, state, application, ApplicationSleepStateKey,
		ApplicationSleepDepth, 0, ApplicationSleep);

	internal static bool PublishWindowSleepState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform => PublishSleepState(
		ref platform, state, window, WindowSleepStateKey, WindowSleepDepth,
		WindowSleepDisabled, WindowSleep, out _);

	private static bool PublishApplicationSleepState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform => PublishSleepState(
		ref platform, state, application, ApplicationSleepStateKey,
		ApplicationSleepDepth, 0, ApplicationSleep, out _);

	private static bool TryGetSleepState<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, uint key, uint depthAttribute,
		uint savedDisabledAttribute, uint requestAttribute,
		out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, owner, key);
		if (MuiStoreCore.DataspaceLength(ref platform, state, owner, key) !=
			unchecked((int)MuiSleepStateRecord.Size)) return false;
		return MuiSleepStateRecordCodec.TryRead(ref platform, block, out value);
	}

	private static MuiSleepStateRecord ReadSleepState<TPlatform>(
		ref TPlatform platform, APTR state, APTR owner, uint key,
		uint depthAttribute, uint savedDisabledAttribute, uint requestAttribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishSleepState(ref platform, state, owner, key, depthAttribute,
			savedDisabledAttribute, requestAttribute, out var value)) return value;
		value = default;
		value.Magic = MuiSleepStateRecord.Cookie;
		FillSleepState(ref platform, state, owner, depthAttribute,
			savedDisabledAttribute, requestAttribute, ref value);
		return value;
	}

	private static bool PublishSleepState<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, uint key, uint depthAttribute,
		uint savedDisabledAttribute, uint requestAttribute,
		out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, owner, key);
		if (TryGetSleepState(ref platform, state, owner, key, depthAttribute,
			savedDisabledAttribute, requestAttribute, out value))
		{
			FillSleepState(ref platform, state, owner, depthAttribute,
				savedDisabledAttribute, requestAttribute, ref value);
			return MuiSleepStateRecordCodec.Write(ref platform, block, value);
		}

		value = default;
		value.Magic = MuiSleepStateRecord.Cookie;
		FillSleepState(ref platform, state, owner, depthAttribute,
			savedDisabledAttribute, requestAttribute, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiSleepStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiSleepStateRecord.Size);
		var written = MuiSleepStateRecordCodec.Write(ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, owner,
			key, scratch, unchecked((int)MuiSleepStateRecord.Size));
		platform.Clear(scratch, MuiSleepStateRecord.Size);
		platform.Free(scratch, MuiSleepStateRecord.Size);
		return added;
	}

	private static void FillSleepState<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, uint depthAttribute, uint savedDisabledAttribute,
		uint requestAttribute, ref MuiSleepStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, owner,
			depthAttribute, out var depth)) depth = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, owner,
			requestAttribute, out var request)) request = 0;
		value.Depth = depth;
		if (savedDisabledAttribute == 0 ||
			!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, owner,
				savedDisabledAttribute, out var savedDisabled)) savedDisabled = 0;
		value.SavedDisabled = savedDisabled;
		value.Request = request;
	}

	internal static bool TryGetApplicationSchedulerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationSchedulerStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationSchedulerStateKey) !=
			unchecked((int)MuiApplicationSchedulerStateRecord.Size)) return false;
		return MuiApplicationSchedulerStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiApplicationSchedulerStateRecord
		ReadApplicationSchedulerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationSchedulerState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationSchedulerStateRecord.Cookie;
		FillApplicationSchedulerState(ref platform, state, application, ref value);
		return value;
	}

	private static bool PublishApplicationSchedulerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationSchedulerStateKey);
		if (TryGetApplicationSchedulerState(ref platform, state, application,
			out value))
		{
			FillApplicationSchedulerState(ref platform, state, application,
				ref value);
			return MuiApplicationSchedulerStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationSchedulerStateRecord.Cookie;
		FillApplicationSchedulerState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationSchedulerStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationSchedulerStateRecord.Size);
		var written = MuiApplicationSchedulerStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationSchedulerStateKey, scratch,
			unchecked((int)MuiApplicationSchedulerStateRecord.Size));
		platform.Clear(scratch, MuiApplicationSchedulerStateRecord.Size);
		platform.Free(scratch, MuiApplicationSchedulerStateRecord.Size);
		return added;
	}

	private static void FillApplicationSchedulerState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationSchedulerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.ReturnHead = APTR.FromPointer(Read(ref platform, state, application,
			ReturnHead));
		value.ReturnTail = APTR.FromPointer(Read(ref platform, state, application,
			ReturnTail));
		value.InputHandlers = APTR.FromPointer(Read(ref platform, state,
			application, InputHandlers));
		value.SignalMask = Read(ref platform, state, application, SignalMask);
		value.PushHead = APTR.FromPointer(Read(ref platform, state, application,
			PushHead));
		value.PushTail = APTR.FromPointer(Read(ref platform, state, application,
			PushTail));
	}

	internal static bool TryGetWindowInteractionState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowInteractionStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowInteractionStateKey) !=
			unchecked((int)MuiWindowInteractionStateRecord.Size)) return false;
		return MuiWindowInteractionStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiWindowInteractionStateRecord
		ReadWindowInteractionState<TPlatform>(ref TPlatform platform, APTR state,
		APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowInteractionState(ref platform, state, window,
			out var value)) return value;
		value = default;
		value.Magic = MuiWindowInteractionStateRecord.Cookie;
		FillWindowInteractionState(ref platform, state, window, ref value);
		return value;
	}

	private static bool PublishWindowInteractionState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowInteractionStateKey);
		if (TryGetWindowInteractionState(ref platform, state, window, out value))
		{
			FillWindowInteractionState(ref platform, state, window, ref value);
			return MuiWindowInteractionStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiWindowInteractionStateRecord.Cookie;
		FillWindowInteractionState(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowInteractionStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowInteractionStateRecord.Size);
		var written = MuiWindowInteractionStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowInteractionStateKey, scratch,
			unchecked((int)MuiWindowInteractionStateRecord.Size));
		platform.Clear(scratch, MuiWindowInteractionStateRecord.Size);
		platform.Free(scratch, MuiWindowInteractionStateRecord.Size);
		return added;
	}

	private static void FillWindowInteractionState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		ref MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.SnapshotFlags = Read(ref platform, state, window,
			WindowSnapshotFlags);
		value.SnapshotRequests = Read(ref platform, state, window,
			WindowSnapshotRequests);
		value.CycleChainHead = APTR.FromPointer(Read(ref platform, state, window,
			WindowCycleChainHead));
		value.CycleChainCount = Read(ref platform, state, window,
			WindowCycleChainCount);
		value.CycleChainRequests = Read(ref platform, state, window,
			WindowCycleChainRequests);
	}

	internal static bool TryGetWindowFocusState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowFocusStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowFocusStateKey) != unchecked((int)MuiWindowFocusStateRecord.Size))
			return false;
		return MuiWindowFocusStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static MuiWindowFocusStateRecord ReadWindowFocusState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowFocusState(ref platform, state, window, out var value))
			return value;
		value = default;
		value.Magic = MuiWindowFocusStateRecord.Cookie;
		FillWindowFocusState(ref platform, state, window, ref value);
		return value;
	}

	private static bool PublishWindowFocusState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowFocusStateKey);
		if (TryGetWindowFocusState(ref platform, state, window, out value))
		{
			FillWindowFocusState(ref platform, state, window, ref value);
			return MuiWindowFocusStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiWindowFocusStateRecord.Cookie;
		FillWindowFocusState(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowFocusStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowFocusStateRecord.Size);
		var written = MuiWindowFocusStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowFocusStateKey, scratch,
			unchecked((int)MuiWindowFocusStateRecord.Size));
		platform.Clear(scratch, MuiWindowFocusStateRecord.Size);
		platform.Free(scratch, MuiWindowFocusStateRecord.Size);
		return added;
	}

	private static void FillWindowFocusState<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, ref MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			ActiveObject, out var active)) active = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			DefaultObject, out var @default)) @default = 0;
		value.ActiveObject = APTR.FromPointer(active);
		value.DefaultObject = APTR.FromPointer(@default);
	}

	// Application and Window getter attributes are projected from the named
	// lifecycle, policy, identity, relationship, and focus records below. Keep
	// the admission predicate beside the typed getter so common-control OM_GET
	// does not confuse these classes with common controls merely because their
	// class names are outside the common-control classifier.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ApplicationDiskObject ||
		attribute == ApplicationDropObject ||
		attribute == ApplicationMenustrip ||
		attribute == ApplicationAuthor || attribute == ApplicationBase ||
		attribute == ApplicationCopyright || attribute == ApplicationDescription ||
		attribute == ApplicationTitle || attribute == ApplicationVersion ||
		attribute == ApplicationHelpFile || attribute == ApplicationIconifyTitle ||
		attribute == ApplicationUseRexx ||
		attribute == ApplicationUseCommodities ||
		attribute == ApplicationUseScreenNotify ||
		attribute == ApplicationUsedClasses ||
		attribute == ApplicationWindow || attribute == ApplicationSleep ||
		attribute == ApplicationMenuAction || attribute == ApplicationMenuHelp ||
		attribute == ActiveObject || attribute == DefaultObject ||
		attribute == ApplicationInitialized || attribute == ApplicationIconified ||
		attribute == ApplicationActive || attribute == ApplicationSingleTask ||
		attribute == ApplicationDoubleStart || attribute == ApplicationForceQuit;

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = IsPublicGetterAttribute(attribute);
		if (!handled) return false;
		if (attribute == ApplicationDiskObject ||
			attribute == ApplicationDropObject || attribute == ApplicationMenustrip)
		{
			if (!PublishApplicationObjectState(ref platform, state, obj,
				out var objectState)) return false;
			value = attribute == ApplicationDiskObject ? objectState.DiskObject.Raw :
				attribute == ApplicationDropObject ? objectState.DropObject.Raw :
				objectState.Menustrip.Raw;
			return true;
		}
		if (attribute == ApplicationAuthor || attribute == ApplicationBase ||
			attribute == ApplicationCopyright || attribute == ApplicationDescription ||
			attribute == ApplicationTitle || attribute == ApplicationVersion)
		{
			if (!PublishApplicationIdentityState(ref platform, state, obj,
				out var identityState)) return false;
			value = attribute == ApplicationAuthor ? identityState.Author.Raw :
				attribute == ApplicationBase ? identityState.Base.Raw :
				attribute == ApplicationCopyright ? identityState.Copyright.Raw :
				attribute == ApplicationDescription ? identityState.Description.Raw :
				attribute == ApplicationTitle ? identityState.Title.Raw :
				identityState.Version.Raw;
			return true;
		}
		if (attribute == ApplicationHelpFile ||
			attribute == ApplicationIconifyTitle)
		{
			if (!PublishApplicationTextState(ref platform, state, obj,
				out var textState)) return false;
			value = attribute == ApplicationHelpFile ? textState.HelpFile.Raw :
				textState.IconifyTitle.Raw;
			return true;
		}
		if (attribute == ApplicationUseRexx ||
			attribute == ApplicationUseCommodities ||
			attribute == ApplicationUseScreenNotify)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryReadApplicationPolicyStateRecord(ref platform, state, obj,
					out _))
			{
				handled = false;
				return false;
			}
			if (!PublishApplicationPolicyState(ref platform, state, obj,
				out var policyState)) return false;
			value = attribute == ApplicationUseRexx ? policyState.UseRexx :
				attribute == ApplicationUseCommodities ? policyState.UseCommodities :
				policyState.UseScreenNotify;
			return true;
		}
		if (attribute == ApplicationUsedClasses)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				ApplicationUsedClasses, out _) &&
				!TryReadApplicationUsedClassesStateRecord(ref platform, state, obj,
					out _))
			{
				handled = false;
				return false;
			}
			if (!PublishApplicationUsedClassesState(ref platform, state, obj,
				out var usedClassesState)) return false;
			value = usedClassesState.Vector.Raw;
			return true;
		}
		if (attribute == ApplicationWindow)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				ApplicationWindow, out _) &&
				!TryReadApplicationWindowRelationshipStateRecord(ref platform, state,
					obj, out _))
			{
				handled = false;
				return false;
			}
			if (!PublishApplicationWindowRelationshipState(ref platform, state, obj,
				out var relationshipState)) return false;
			value = relationshipState.LastWindow.Raw;
			return true;
		}
		if (attribute == ApplicationSleep)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				ApplicationSleep, out _) &&
				!TryGetApplicationSleepState(ref platform, state, obj, out _))
			{
				handled = false;
				return false;
			}
			if (!PublishApplicationSleepState(ref platform, state, obj) ||
				!TryGetApplicationSleepState(ref platform, state, obj,
					out var sleepState)) return false;
			value = sleepState.Request;
			return true;
		}
		if (attribute == ApplicationMenuAction || attribute == ApplicationMenuHelp)
		{
			if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out _) &&
				!TryReadApplicationMenuStateRecord(ref platform, state, obj, out _))
			{
				handled = false;
				return false;
			}
			if (!PublishApplicationMenuState(ref platform, state, obj,
				out var menuState)) return false;
			value = attribute == ApplicationMenuAction ? menuState.MenuAction :
				menuState.MenuHelp;
			return true;
		}
		if (attribute == ActiveObject || attribute == DefaultObject)
		{
			if (!PublishWindowFocusState(ref platform, state, obj, out var focus))
				return false;
			value = attribute == ActiveObject ? focus.ActiveObject.Raw :
				focus.DefaultObject.Raw;
			return true;
		}
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out _) && !TryGetApplicationLifecycleState(ref platform,
			state, obj, out _))
		{
			handled = false;
			return false;
		}
		if (!PublishApplicationLifecycle(ref platform, state, obj,
			out var lifecycle)) return false;
		value = attribute == ApplicationInitialized ? lifecycle.Initialized :
			attribute == ApplicationIconified ? lifecycle.Iconified :
			attribute == ApplicationActive ? lifecycle.Active :
			attribute == ApplicationSingleTask ? lifecycle.SingleTask :
			attribute == ApplicationDoubleStart ? lifecycle.DoubleStart :
			lifecycle.ForceQuit;
		return true;
	}

	internal static bool TryGetWindowEventState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishWindowEventState(ref platform, state, window, out value);

	private static MuiWindowEventStateRecord ReadWindowEventState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishWindowEventState(ref platform, state, window,
			out var value)) return value;
		value = default;
		value.Magic = MuiWindowEventStateRecord.Cookie;
		FillWindowEventState(ref platform, state, window, ref value);
		return value;
	}

	private static bool TryReadWindowEventStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowEventStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, window,
			WindowEventStateKey) !=
			unchecked((int)MuiWindowEventStateRecord.Size)) return false;
		return MuiWindowEventStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishWindowEventState<TPlatform>(
		ref TPlatform platform, APTR state, APTR window,
		out MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, window,
			WindowEventStateKey);
		if (TryReadWindowEventStateRecord(ref platform, state, window, out value))
		{
			FillWindowEventState(ref platform, state, window, ref value);
			return MuiWindowEventStateRecordCodec.Write(ref platform, block, value);
		}

		value = default;
		value.Magic = MuiWindowEventStateRecord.Cookie;
		FillWindowEventState(ref platform, state, window, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiWindowEventStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiWindowEventStateRecord.Size);
		var written = MuiWindowEventStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, window,
			WindowEventStateKey, scratch,
			unchecked((int)MuiWindowEventStateRecord.Size));
		platform.Clear(scratch, MuiWindowEventStateRecord.Size);
		platform.Free(scratch, MuiWindowEventStateRecord.Size);
		return added;
	}

	private static void FillWindowEventState<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, ref MuiWindowEventStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			WindowCloseRequest, out var closeRequest)) closeRequest = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			MuiWindowPublicCore.InputEvent, out var inputEvent)) inputEvent = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			MuiWindowPublicCore.MouseObject, out var mouseObject)) mouseObject = 0;
		value.CloseRequest = closeRequest != 0 ? 1u : 0u;
		value.InputEvent = APTR.FromPointer(inputEvent);
		value.MouseObject = APTR.FromPointer(mouseObject);
	}

	internal static bool TryGetApplicationHelpState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationHelpState(ref platform, state, application, out value);

	private static MuiApplicationHelpStateRecord ReadApplicationHelpState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationHelpState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationHelpStateRecord.Cookie;
		FillApplicationHelpState(ref platform, state, application, ref value);
		return value;
	}

	private static bool TryReadApplicationHelpStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationHelpStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationHelpStateKey) !=
			unchecked((int)MuiApplicationHelpStateRecord.Size)) return false;
		return MuiApplicationHelpStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishApplicationHelpState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationHelpStateKey);
		if (TryReadApplicationHelpStateRecord(ref platform, state, application,
			out value))
		{
			FillApplicationHelpState(ref platform, state, application, ref value);
			return MuiApplicationHelpStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiApplicationHelpStateRecord.Cookie;
		FillApplicationHelpState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationHelpStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationHelpStateRecord.Size);
		var written = MuiApplicationHelpStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationHelpStateKey, scratch,
			unchecked((int)MuiApplicationHelpStateRecord.Size));
		platform.Clear(scratch, MuiApplicationHelpStateRecord.Size);
		platform.Free(scratch, MuiApplicationHelpStateRecord.Size);
		return added;
	}

	private static void FillApplicationHelpState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, ref MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.AboutReferenceWindow = APTR.FromPointer(Read(ref platform, state,
			application, ApplicationAboutRefWindow));
		value.AboutRequests = Read(ref platform, state, application,
			ApplicationAboutRequests);
		value.HelpWindow = APTR.FromPointer(Read(ref platform, state, application,
			ApplicationHelpWindow));
		value.HelpName = APTR.FromPointer(Read(ref platform, state, application,
			ApplicationHelpName));
		value.HelpNode = APTR.FromPointer(Read(ref platform, state, application,
			ApplicationHelpNode));
		value.HelpLine = Read(ref platform, state, application, ApplicationHelpLine);
		value.HelpRequests = Read(ref platform, state, application,
			ApplicationHelpRequests);
	}

	internal static bool TryGetApplicationDefaultConfigState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationDefaultConfigState(ref platform, state, application,
			out value);

	private static MuiApplicationDefaultConfigStateRecord
		ReadApplicationDefaultConfigState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationDefaultConfigState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationDefaultConfigStateRecord.Cookie;
		FillApplicationDefaultConfigState(ref platform, state, application,
			ref value);
		return value;
	}

	private static bool TryReadApplicationDefaultConfigStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationDefaultConfigStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationDefaultConfigStateKey) !=
			unchecked((int)MuiApplicationDefaultConfigStateRecord.Size)) return false;
		return MuiApplicationDefaultConfigStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static bool PublishApplicationDefaultConfigState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationDefaultConfigStateKey);
		if (TryReadApplicationDefaultConfigStateRecord(ref platform, state,
			application, out value))
		{
			FillApplicationDefaultConfigState(ref platform, state, application,
				ref value);
			return MuiApplicationDefaultConfigStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationDefaultConfigStateRecord.Cookie;
		FillApplicationDefaultConfigState(ref platform, state, application,
			ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationDefaultConfigStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationDefaultConfigStateRecord.Size);
		var written = MuiApplicationDefaultConfigStateRecordCodec.Write(
			ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationDefaultConfigStateKey, scratch,
			unchecked((int)MuiApplicationDefaultConfigStateRecord.Size));
		platform.Clear(scratch, MuiApplicationDefaultConfigStateRecord.Size);
		platform.Free(scratch, MuiApplicationDefaultConfigStateRecord.Size);
		return added;
	}

	private static void FillApplicationDefaultConfigState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.ConfigId = Read(ref platform, state, application,
			ApplicationDefaultConfigId);
		value.Value = Read(ref platform, state, application,
			ApplicationDefaultConfigValue);
		value.Requests = Read(ref platform, state, application,
			ApplicationDefaultConfigRequests);
	}

	internal static bool TryGetApplicationConfigWindowState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationConfigWindowState(ref platform, state, application,
			out value);

	private static MuiApplicationConfigWindowStateRecord
		ReadApplicationConfigWindowState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationConfigWindowState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationConfigWindowStateRecord.Cookie;
		FillApplicationConfigWindowState(ref platform, state, application,
			ref value);
		return value;
	}

	private static bool TryReadApplicationConfigWindowStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationConfigWindowStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationConfigWindowStateKey) !=
			unchecked((int)MuiApplicationConfigWindowStateRecord.Size)) return false;
		return MuiApplicationConfigWindowStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static bool PublishApplicationConfigWindowState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationConfigWindowStateKey);
		if (TryReadApplicationConfigWindowStateRecord(ref platform, state,
			application, out value))
		{
			FillApplicationConfigWindowState(ref platform, state, application,
				ref value);
			return MuiApplicationConfigWindowStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationConfigWindowStateRecord.Cookie;
		FillApplicationConfigWindowState(ref platform, state, application,
			ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationConfigWindowStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationConfigWindowStateRecord.Size);
		var written = MuiApplicationConfigWindowStateRecordCodec.Write(
			ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationConfigWindowStateKey, scratch,
			unchecked((int)MuiApplicationConfigWindowStateRecord.Size));
		platform.Clear(scratch, MuiApplicationConfigWindowStateRecord.Size);
		platform.Free(scratch, MuiApplicationConfigWindowStateRecord.Size);
		return added;
	}

	private static void FillApplicationConfigWindowState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Flags = Read(ref platform, state, application,
			ApplicationConfigWindowFlags);
		value.ClassId = APTR.FromPointer(Read(ref platform, state, application,
			ApplicationConfigWindowClassId));
		value.Requests = Read(ref platform, state, application,
			ApplicationConfigWindowRequests);
	}

	internal static bool TryGetApplicationSettingsPanelState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationSettingsPanelState(ref platform, state, application,
			out value);

	private static MuiApplicationSettingsPanelStateRecord
		ReadApplicationSettingsPanelState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationSettingsPanelState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationSettingsPanelStateRecord.Cookie;
		FillApplicationSettingsPanelState(ref platform, state, application,
			ref value);
		return value;
	}

	private static bool TryReadApplicationSettingsPanelStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationSettingsPanelStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationSettingsPanelStateKey) !=
			unchecked((int)MuiApplicationSettingsPanelStateRecord.Size)) return false;
		return MuiApplicationSettingsPanelStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static bool PublishApplicationSettingsPanelState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationSettingsPanelStateKey);
		if (TryReadApplicationSettingsPanelStateRecord(ref platform, state,
			application, out value))
		{
			FillApplicationSettingsPanelState(ref platform, state, application,
				ref value);
			return MuiApplicationSettingsPanelStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationSettingsPanelStateRecord.Cookie;
		FillApplicationSettingsPanelState(ref platform, state, application,
			ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationSettingsPanelStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationSettingsPanelStateRecord.Size);
		var written = MuiApplicationSettingsPanelStateRecordCodec.Write(
			ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationSettingsPanelStateKey, scratch,
			unchecked((int)MuiApplicationSettingsPanelStateRecord.Size));
		platform.Clear(scratch, MuiApplicationSettingsPanelStateRecord.Size);
		platform.Free(scratch, MuiApplicationSettingsPanelStateRecord.Size);
		return added;
	}

	private static void FillApplicationSettingsPanelState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Number = Read(ref platform, state, application,
			ApplicationSettingsPanelNumber);
		value.Panel = APTR.FromPointer(Read(ref platform, state, application,
			ApplicationSettingsPanelObject));
		value.Requests = Read(ref platform, state, application,
			ApplicationSettingsPanelRequests);
	}

	internal static bool TryGetApplicationSettingsPersistenceState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationSettingsPersistenceState(ref platform, state,
			application, out value);

	private static MuiApplicationSettingsPersistenceStateRecord
		ReadApplicationSettingsPersistenceState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationSettingsPersistenceState(ref platform, state,
			application, out var value)) return value;
		value = default;
		value.Magic = MuiApplicationSettingsPersistenceStateRecord.Cookie;
		FillApplicationSettingsPersistenceState(ref platform, state, application,
			ref value);
		return value;
	}

	private static bool TryReadApplicationSettingsPersistenceStateRecord<
		TPlatform>(ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationSettingsPersistenceStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationSettingsPersistenceStateKey) != unchecked((int)
			MuiApplicationSettingsPersistenceStateRecord.Size)) return false;
		return MuiApplicationSettingsPersistenceStateRecordCodec.TryRead(
			ref platform, block, out value);
	}

	private static bool PublishApplicationSettingsPersistenceState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationSettingsPersistenceStateKey);
		if (TryReadApplicationSettingsPersistenceStateRecord(ref platform, state,
			application, out value))
		{
			FillApplicationSettingsPersistenceState(ref platform, state, application,
				ref value);
			return MuiApplicationSettingsPersistenceStateRecordCodec.Write(
				ref platform, block, value);
		}

		value = default;
		value.Magic = MuiApplicationSettingsPersistenceStateRecord.Cookie;
		FillApplicationSettingsPersistenceState(ref platform, state, application,
			ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationSettingsPersistenceStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationSettingsPersistenceStateRecord.Size);
		var written = MuiApplicationSettingsPersistenceStateRecordCodec.Write(
			ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationSettingsPersistenceStateKey, scratch,
			unchecked((int)MuiApplicationSettingsPersistenceStateRecord.Size));
		platform.Clear(scratch, MuiApplicationSettingsPersistenceStateRecord.Size);
		platform.Free(scratch, MuiApplicationSettingsPersistenceStateRecord.Size);
		return added;
	}

	private static void FillApplicationSettingsPersistenceState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Operation = Read(ref platform, state, application,
			ApplicationSettingsOperation);
		value.Name = APTR.FromPointer(Read(ref platform, state, application,
			ApplicationSettingsName));
		value.Requests = Read(ref platform, state, application,
			ApplicationSettingsRequests);
		value.Saves = Read(ref platform, state, application,
			ApplicationSettingsSaves);
		value.Loads = Read(ref platform, state, application,
			ApplicationSettingsLoads);
	}

	internal static bool TryGetApplicationRefreshState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationRefreshState(ref platform, state, application,
			out value);

	private static MuiApplicationRefreshStateRecord
		ReadApplicationRefreshState<TPlatform>(ref TPlatform platform, APTR state,
		APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationRefreshState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationRefreshStateRecord.Cookie;
		FillApplicationRefreshState(ref platform, state, application, ref value);
		return value;
	}

	private static bool TryReadApplicationRefreshStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationRefreshStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationRefreshStateKey) != unchecked((int)
			MuiApplicationRefreshStateRecord.Size)) return false;
		return MuiApplicationRefreshStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishApplicationRefreshState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationRefreshStateKey);
		if (TryReadApplicationRefreshStateRecord(ref platform, state, application,
			out value))
		{
			FillApplicationRefreshState(ref platform, state, application, ref value);
			return MuiApplicationRefreshStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiApplicationRefreshStateRecord.Cookie;
		FillApplicationRefreshState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationRefreshStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationRefreshStateRecord.Size);
		var written = MuiApplicationRefreshStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationRefreshStateKey, scratch,
			unchecked((int)MuiApplicationRefreshStateRecord.Size));
		platform.Clear(scratch, MuiApplicationRefreshStateRecord.Size);
		platform.Free(scratch, MuiApplicationRefreshStateRecord.Size);
		return added;
	}

	private static void FillApplicationRefreshState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Checks = Read(ref platform, state, application,
			ApplicationRefreshChecks);
		value.RefreshedWindows = Read(ref platform, state, application,
			ApplicationRefreshWindows);
	}

	internal static bool TryGetApplicationMenuState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationMenuState(ref platform, state, application, out value);

	private static MuiApplicationMenuStateRecord ReadApplicationMenuState<
		TPlatform>(ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationMenuState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationMenuStateRecord.Cookie;
		FillApplicationMenuState(ref platform, state, application, ref value);
		return value;
	}

	private static bool TryReadApplicationMenuStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationMenuStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationMenuStateKey) != unchecked((int)
			MuiApplicationMenuStateRecord.Size)) return false;
		return MuiApplicationMenuStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishApplicationMenuState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationMenuStateKey);
		if (TryReadApplicationMenuStateRecord(ref platform, state, application,
			out value))
		{
			FillApplicationMenuState(ref platform, state, application, ref value);
			return MuiApplicationMenuStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiApplicationMenuStateRecord.Cookie;
		FillApplicationMenuState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationMenuStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationMenuStateRecord.Size);
		var written = MuiApplicationMenuStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationMenuStateKey, scratch,
			unchecked((int)MuiApplicationMenuStateRecord.Size));
		platform.Clear(scratch, MuiApplicationMenuStateRecord.Size);
		platform.Free(scratch, MuiApplicationMenuStateRecord.Size);
		return added;
	}

	private static void FillApplicationMenuState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationMenuAction, out var menuAction)) menuAction = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationMenuHelp, out var menuHelp)) menuHelp = 0;
		value.MenuAction = menuAction;
		value.MenuHelp = menuHelp;
	}

	internal static bool TryGetApplicationObjectState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationObjectState(ref platform, state, application, out value);

	private static MuiApplicationObjectStateRecord ReadApplicationObjectState<
		TPlatform>(ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationObjectState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationObjectStateRecord.Cookie;
		FillApplicationObjectState(ref platform, state, application, ref value);
		return value;
	}

	private static bool TryReadApplicationObjectStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationObjectStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationObjectStateKey) != unchecked((int)
			MuiApplicationObjectStateRecord.Size)) return false;
		return MuiApplicationObjectStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishApplicationObjectState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationObjectStateKey);
		if (TryReadApplicationObjectStateRecord(ref platform, state, application,
			out value))
		{
			FillApplicationObjectState(ref platform, state, application, ref value);
			return MuiApplicationObjectStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiApplicationObjectStateRecord.Cookie;
		FillApplicationObjectState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationObjectStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationObjectStateRecord.Size);
		var written = MuiApplicationObjectStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationObjectStateKey, scratch,
			unchecked((int)MuiApplicationObjectStateRecord.Size));
		platform.Clear(scratch, MuiApplicationObjectStateRecord.Size);
		platform.Free(scratch, MuiApplicationObjectStateRecord.Size);
		return added;
	}

	private static void FillApplicationObjectState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationDiskObject, out var diskObject)) diskObject = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationDropObject, out var dropObject)) dropObject = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationMenustrip, out var menustrip)) menustrip = 0;
		value.DiskObject = APTR.FromPointer(diskObject);
		value.DropObject = APTR.FromPointer(dropObject);
		value.Menustrip = APTR.FromPointer(menustrip);
	}

	internal static bool TryGetApplicationTextState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationTextState(ref platform, state, application, out value);

	private static MuiApplicationTextStateRecord ReadApplicationTextState<
		TPlatform>(ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationTextState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationTextStateRecord.Cookie;
		FillApplicationTextState(ref platform, state, application, ref value);
		return value;
	}

	private static bool TryReadApplicationTextStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationTextStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationTextStateKey) != unchecked((int)
			MuiApplicationTextStateRecord.Size)) return false;
		return MuiApplicationTextStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishApplicationTextState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationTextStateKey);
		if (TryReadApplicationTextStateRecord(ref platform, state, application,
			out value))
		{
			FillApplicationTextState(ref platform, state, application, ref value);
			return MuiApplicationTextStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiApplicationTextStateRecord.Cookie;
		FillApplicationTextState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationTextStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationTextStateRecord.Size);
		var written = MuiApplicationTextStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationTextStateKey, scratch,
			unchecked((int)MuiApplicationTextStateRecord.Size));
		platform.Clear(scratch, MuiApplicationTextStateRecord.Size);
		platform.Free(scratch, MuiApplicationTextStateRecord.Size);
		return added;
	}

	private static void FillApplicationTextState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationHelpFile, out var helpFile)) helpFile = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationIconifyTitle, out var iconifyTitle)) iconifyTitle = 0;
		value.HelpFile = APTR.FromPointer(helpFile);
		value.IconifyTitle = APTR.FromPointer(iconifyTitle);
	}

	internal static bool TryGetApplicationIdentityState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationIdentityState(ref platform, state, application,
			out value);

	private static MuiApplicationIdentityStateRecord
		ReadApplicationIdentityState<TPlatform>(ref TPlatform platform, APTR state,
		APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationIdentityState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationIdentityStateRecord.Cookie;
		FillApplicationIdentityState(ref platform, state, application, ref value);
		return value;
	}

	private static bool TryReadApplicationIdentityStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationIdentityStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationIdentityStateKey) != unchecked((int)
			MuiApplicationIdentityStateRecord.Size)) return false;
		return MuiApplicationIdentityStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishApplicationIdentityState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationIdentityStateKey);
		if (TryReadApplicationIdentityStateRecord(ref platform, state, application,
			out value))
		{
			FillApplicationIdentityState(ref platform, state, application,
				ref value);
			return MuiApplicationIdentityStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationIdentityStateRecord.Cookie;
		FillApplicationIdentityState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationIdentityStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationIdentityStateRecord.Size);
		var written = MuiApplicationIdentityStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationIdentityStateKey, scratch,
			unchecked((int)MuiApplicationIdentityStateRecord.Size));
		platform.Clear(scratch, MuiApplicationIdentityStateRecord.Size);
		platform.Free(scratch, MuiApplicationIdentityStateRecord.Size);
		return added;
	}

	private static void FillApplicationIdentityState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationAuthor, out var author)) author = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationBase, out var @base)) @base = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationCopyright, out var copyright)) copyright = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationDescription, out var description)) description = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationTitle, out var title)) title = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationVersion, out var version)) version = 0;
		value.Author = APTR.FromPointer(author);
		value.Base = APTR.FromPointer(@base);
		value.Copyright = APTR.FromPointer(copyright);
		value.Description = APTR.FromPointer(description);
		value.Title = APTR.FromPointer(title);
		value.Version = APTR.FromPointer(version);
	}

	internal static bool TryGetApplicationPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationPolicyState(ref platform, state, application, out value);

	private static MuiApplicationPolicyStateRecord ReadApplicationPolicyState<
		TPlatform>(ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationPolicyState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationPolicyStateRecord.Cookie;
		FillApplicationPolicyState(ref platform, state, application, ref value);
		return value;
	}

	private static bool TryReadApplicationPolicyStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationPolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationPolicyStateKey) != unchecked((int)
			MuiApplicationPolicyStateRecord.Size)) return false;
		return MuiApplicationPolicyStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishApplicationPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationPolicyStateKey);
		if (TryReadApplicationPolicyStateRecord(ref platform, state, application,
			out value))
		{
			FillApplicationPolicyState(ref platform, state, application, ref value);
			return MuiApplicationPolicyStateRecordCodec.Write(ref platform, block,
				value);
		}

		value = default;
		value.Magic = MuiApplicationPolicyStateRecord.Cookie;
		FillApplicationPolicyState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationPolicyStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationPolicyStateRecord.Size);
		var written = MuiApplicationPolicyStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationPolicyStateKey, scratch,
			unchecked((int)MuiApplicationPolicyStateRecord.Size));
		platform.Clear(scratch, MuiApplicationPolicyStateRecord.Size);
		platform.Free(scratch, MuiApplicationPolicyStateRecord.Size);
		return added;
	}

	private static void FillApplicationPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationUseRexx, out var useRexx)) useRexx = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationUseCommodities, out var useCommodities)) useCommodities = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationUseScreenNotify, out var useScreenNotify)) useScreenNotify = 0;
		value.UseRexx = useRexx == 0 ? 0u : 1u;
		value.UseCommodities = useCommodities == 0 ? 0u : 1u;
		value.UseScreenNotify = useScreenNotify == 0 ? 0u : 1u;
	}

	internal static bool TryGetApplicationUsedClassesState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationUsedClassesState(ref platform, state, application,
			out value);

	private static MuiApplicationUsedClassesStateRecord
		ReadApplicationUsedClassesState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationUsedClassesState(ref platform, state, application,
			out var value)) return value;
		value = default;
		value.Magic = MuiApplicationUsedClassesStateRecord.Cookie;
		FillApplicationUsedClassesState(ref platform, state, application,
			ref value);
		return value;
	}

	private static bool TryReadApplicationUsedClassesStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationUsedClassesStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationUsedClassesStateKey) != unchecked((int)
			MuiApplicationUsedClassesStateRecord.Size)) return false;
		return MuiApplicationUsedClassesStateRecordCodec.TryRead(ref platform,
			block, out value);
	}

	private static bool PublishApplicationUsedClassesState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationUsedClassesStateKey);
		if (TryReadApplicationUsedClassesStateRecord(ref platform, state,
			application, out value))
		{
			FillApplicationUsedClassesState(ref platform, state, application,
				ref value);
			return MuiApplicationUsedClassesStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationUsedClassesStateRecord.Cookie;
		FillApplicationUsedClassesState(ref platform, state, application,
			ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationUsedClassesStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationUsedClassesStateRecord.Size);
		var written = MuiApplicationUsedClassesStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationUsedClassesStateKey, scratch,
			unchecked((int)MuiApplicationUsedClassesStateRecord.Size));
		platform.Clear(scratch, MuiApplicationUsedClassesStateRecord.Size);
		platform.Free(scratch, MuiApplicationUsedClassesStateRecord.Size);
		return added;
	}

	private static void FillApplicationUsedClassesState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationUsedClasses, out var vector)) vector = 0;
		value.Vector = APTR.FromPointer(vector);
	}

	internal static bool TryGetApplicationWindowRelationshipState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		PublishApplicationWindowRelationshipState(ref platform, state, application,
			out value);

	private static MuiApplicationWindowRelationshipStateRecord
		ReadApplicationWindowRelationshipState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishApplicationWindowRelationshipState(ref platform, state,
			application, out var value)) return value;
		value = default;
		value.Magic = MuiApplicationWindowRelationshipStateRecord.Cookie;
		FillApplicationWindowRelationshipState(ref platform, state, application,
			ref value);
		return value;
	}

	private static bool TryReadApplicationWindowRelationshipStateRecord<
		TPlatform>(ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationWindowRelationshipStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationWindowRelationshipStateKey) != unchecked((int)
			MuiApplicationWindowRelationshipStateRecord.Size)) return false;
		return MuiApplicationWindowRelationshipStateRecordCodec.TryRead(
			ref platform, block, out value);
	}

	private static bool PublishApplicationWindowRelationshipState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationWindowRelationshipStateKey);
		if (TryReadApplicationWindowRelationshipStateRecord(ref platform, state,
			application, out value))
		{
			FillApplicationWindowRelationshipState(ref platform, state, application,
				ref value);
			return MuiApplicationWindowRelationshipStateRecordCodec.Write(
				ref platform, block, value);
		}

		value = default;
		value.Magic = MuiApplicationWindowRelationshipStateRecord.Cookie;
		FillApplicationWindowRelationshipState(ref platform, state, application,
			ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationWindowRelationshipStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationWindowRelationshipStateRecord.Size);
		var written = MuiApplicationWindowRelationshipStateRecordCodec.Write(
			ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, ApplicationWindowRelationshipStateKey, scratch,
			unchecked((int)MuiApplicationWindowRelationshipStateRecord.Size));
		platform.Clear(scratch, MuiApplicationWindowRelationshipStateRecord.Size);
		platform.Free(scratch, MuiApplicationWindowRelationshipStateRecord.Size);
		return added;
	}

	private static bool WriteApplicationWindowRelationshipState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			ApplicationWindowRelationshipStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			ApplicationWindowRelationshipStateKey) != unchecked((int)
			MuiApplicationWindowRelationshipStateRecord.Size)) return false;
		return MuiApplicationWindowRelationshipStateRecordCodec.Write(ref platform,
			block, value);
	}

	private static void FillApplicationWindowRelationshipState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			ApplicationWindow, out var lastWindow)) lastWindow = 0;
		value.LastWindow = APTR.FromPointer(lastWindow);
	}

	public static bool InitializeApplication<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, uint signalMask)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var lifecycle = ReadApplicationLifecycle(ref platform, state, application);
		if (lifecycle.Initialized != 0)
			return true;
		var singleTask = lifecycle.SingleTask != 0;
		var useRexx = true;
		var useCommodities = true;
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, application,
			ApplicationUseRexx, out var configuredUseRexx))
			useRexx = configuredUseRexx != 0;
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, application,
			ApplicationUseCommodities, out var configuredUseCommodities))
			useCommodities = configuredUseCommodities != 0;
		if (!TryFindSingleTaskApplication(ref platform, state, application,
			out var existingSingleTask)) return false;
		if (singleTask && existingSingleTask.IsNotNull)
		{
			// MorphOS reports the second start through the already-running
			// application. The candidate remains uninitialized so its caller can
			// dispose it without ever exposing a live application anchor.
			var existingLifecycle = ReadApplicationLifecycle(ref platform, state,
				existingSingleTask);
			existingLifecycle.DoubleStart = 1;
			WriteApplicationLifecycle(ref platform, state, existingSingleTask,
				existingLifecycle);
			return false;
		}
		if (!(Set(ref platform, state, application, ReturnHead, 0) &&
			Set(ref platform, state, application, ReturnTail, 0) &&
			Set(ref platform, state, application, InputHandlers, 0) &&
			Set(ref platform, state, application, PushHead, 0) &&
			Set(ref platform, state, application, PushTail, 0) &&
			Set(ref platform, state, application, SignalMask, signalMask) &&
			Set(ref platform, state, application, ApplicationSingleTask,
				singleTask ? 1u : 0u) &&
			Set(ref platform, state, application, ApplicationDoubleStart, 0) &&
			Set(ref platform, state, application, ApplicationForceQuit, 0) &&
			Set(ref platform, state, application, ApplicationUseRexx,
				useRexx ? 1u : 0u) &&
			Set(ref platform, state, application, ApplicationUseCommodities,
				useCommodities ? 1u : 0u) &&
			Set(ref platform, state, application, ApplicationUseScreenNotify,
				Read(ref platform, state, application, ApplicationUseScreenNotify) != 0 ?
					1u : 0u) &&
			Set(ref platform, state, application, ApplicationMenuAction, 0) &&
			Set(ref platform, state, application, ApplicationMenuHelp, 0) &&
			Set(ref platform, state, application, ApplicationInitialized, 1)))
			return false;
		if (!PublishApplicationSchedulerState(ref platform, state, application,
			out _) || !PublishApplicationPolicyState(ref platform, state,
			application, out _)) return false;
		lifecycle.Magic = MuiApplicationLifecycleStateRecord.Cookie;
		lifecycle.Initialized = 1;
		lifecycle.SingleTask = singleTask ? 1u : 0u;
		lifecycle.DoubleStart = 0;
		lifecycle.ForceQuit = 0;
		return WriteApplicationLifecycle(ref platform, state, application,
			lifecycle);
	}

	// The headless object list is the guest-resident application registry. A
	// marker distinguishes initialized Application objects from ordinary MUI
	// objects, and the scan stays bounded by the same traversal limit as the
	// rest of the object model. No managed collection is introduced.
	private static bool TryFindSingleTaskApplication<TPlatform>(
		ref TPlatform platform, APTR state, APTR candidate, out APTR existing)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		existing = APTR.Null;
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return false;
		var current = stateValue.Objects;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, current,
				out var objectValue)) return false;
			var objectPointer = objectValue.Boopsi;
			if (objectPointer != candidate &&
				Read(ref platform, state, objectPointer, ApplicationInitialized) != 0)
			{
				var lifecycle = ReadApplicationLifecycle(ref platform, state,
					objectPointer);
				if (lifecycle.Initialized != 0 && lifecycle.SingleTask != 0)
				{
					existing = objectPointer;
					return true;
				}
			}
			current = objectValue.Next;
		}
		return current.IsNull;
	}

	public static bool AddWindow<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR window)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiFamilyCore.AddTail(ref platform, state, application, window))
			return false;
		if (Set(ref platform, state, window, WindowOwner, application.Raw))
		{
			// Application sleep is inherited by windows added after the
			// application enters the sleeping state. Apply the complete named
			// depth in one transition so a large nesting count cannot turn into
			// an unbounded loop of single-step updates.
			var applicationSleep = ReadApplicationSleepState(ref platform, state,
				application).Depth;
			var windowSleep = ReadWindowSleepState(ref platform, state, window).Depth;
			if (applicationSleep <= uint.MaxValue - windowSleep &&
				SetWindowSleepDepth(ref platform, state, window,
					windowSleep + applicationSleep)) return true;
		}
		MuiFamilyCore.Remove(ref platform, state, application, window);
		return false;
	}

	public static bool RemoveWindow<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR window)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var applicationSleep = ReadApplicationSleepState(ref platform, state,
			application).Depth;
		if (applicationSleep != 0)
		{
			var windowSleep = ReadWindowSleepState(ref platform, state, window).Depth;
			var retainedSleep = windowSleep < applicationSleep ? 0u :
				windowSleep - applicationSleep;
			if (!SetWindowSleepDepth(ref platform, state, window, retainedSleep))
				return false;
		}
		CloseWindow(ref platform, state, window);
		if (!MuiFamilyCore.Remove(ref platform, state, application, window))
			return false;
		return Set(ref platform, state, window, WindowOwner, 0);
	}

	public static bool OpenWindow<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, uint eventMask)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var lifecycle = ReadWindowLifecycle(ref platform, state, window);
		if (lifecycle.NativeWindow.IsNotNull) return true;
		var requestedEvents = lifecycle.EventMask | eventMask;
		// MorphOS remembers an open request made while the application is
		// iconified and realizes it after uniconification.  Keep that request in
		// the guest object rather than crossing the native window capability.
		var owner = APTR.FromPointer(Read(ref platform, state, window,
			WindowOwner));
		if (owner.IsNotNull &&
			!MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull &&
			Read(ref platform, state, owner, ApplicationIconified) != 0)
		{
			lifecycle.EventMask = requestedEvents;
			lifecycle.IconifiedOpen = 1;
			return WriteWindowLifecycle(ref platform, state, window, lifecycle);
		}
		var nativeWindow = platform.OpenMuiWindow(window);
		if (nativeWindow.IsNull) return false;
		var openPolicy = ReadWindowOpenPolicy(ref platform, state, window);
		if (!platform.ConfigureWindowEvents(nativeWindow, requestedEvents))
		{
			platform.CloseMuiWindow(nativeWindow);
			return false;
		}
		if (!platform.SetMuiWindowTabletMessages(nativeWindow,
			openPolicy.TabletMessages != 0))
		{
			platform.CloseMuiWindow(nativeWindow);
			return false;
		}
		if (!platform.SetMuiWindowBorderScrollers(nativeWindow,
			openPolicy.UseBottomBorderScroller != 0,
			openPolicy.UseLeftBorderScroller != 0,
			openPolicy.UseRightBorderScroller != 0))
		{
			platform.CloseMuiWindow(nativeWindow);
			return false;
		}
		MuiWindowPublicCore.MuiWindowAlternateGeometry alternateGeometry = default;
		alternateGeometry.Height = openPolicy.AlternateHeight;
		alternateGeometry.Width = openPolicy.AlternateWidth;
		alternateGeometry.LeftEdge = openPolicy.AlternateLeftEdge;
		alternateGeometry.TopEdge = openPolicy.AlternateTopEdge;
		if (!platform.ConfigureMuiWindowAlternateGeometry(nativeWindow,
			alternateGeometry))
		{
			platform.CloseMuiWindow(nativeWindow);
			return false;
		}
		MuiWindowPublicCore.MuiWindowGeometry geometry = default;
		geometry.Height = openPolicy.Height;
		geometry.Width = openPolicy.Width;
		geometry.LeftEdge = openPolicy.LeftEdge;
		geometry.TopEdge = openPolicy.TopEdge;
		if (!platform.ConfigureMuiWindowGeometry(nativeWindow, geometry))
		{
			platform.CloseMuiWindow(nativeWindow);
			return false;
		}
		MuiWindowPublicCore.MuiWindowGadgetPolicy gadgetPolicy = default;
		gadgetPolicy.CloseGadget = openPolicy.CloseGadget;
		gadgetPolicy.DepthGadget = openPolicy.DepthGadget;
		gadgetPolicy.DragBar = openPolicy.DragBar;
		gadgetPolicy.SizeGadget = openPolicy.SizeGadget;
		gadgetPolicy.SizeRight = openPolicy.SizeRight;
		if (!platform.ConfigureMuiWindowGadgets(nativeWindow, gadgetPolicy))
		{
			platform.CloseMuiWindow(nativeWindow);
			return false;
		}
		MuiWindowPublicCore.MuiWindowModePolicy modePolicy = default;
		modePolicy.AppWindow = openPolicy.AppWindow;
		modePolicy.Backdrop = openPolicy.Backdrop;
		modePolicy.Borderless = openPolicy.Borderless;
		modePolicy.PanelWindow = openPolicy.PanelWindow;
		if (!platform.ConfigureMuiWindowMode(nativeWindow, modePolicy))
		{
			platform.CloseMuiWindow(nativeWindow);
			return false;
		}
		lifecycle.NativeWindow = nativeWindow;
		lifecycle.Open = 1;
		lifecycle.EventMask = requestedEvents;
		if (!WriteWindowLifecycle(ref platform, state, window, lifecycle))
		{
			platform.CloseMuiWindow(nativeWindow);
			return false;
		}
		// A window may have been put to sleep before it was opened. MorphOS
		// applies the busy pointer when that sleeping window becomes native.
		if (ReadWindowSleepState(ref platform, state, window).Depth != 0 &&
			!platform.SetMuiWindowBusy(nativeWindow, true))
		{
			platform.CloseMuiWindow(nativeWindow);
			lifecycle.NativeWindow = APTR.Null;
			lifecycle.Open = 0;
			WriteWindowLifecycle(ref platform, state, window, lifecycle);
			return false;
		}
		lifecycle.IconifiedOpen = 0;
		return WriteWindowLifecycle(ref platform, state, window, lifecycle);
	}

	// Public MUI_RequestIDCMP/MUI_RejectIDCMP compatibility. Requests made
	// before a window opens are retained in the object record and applied by
	// OpenWindow; changes made while open are forwarded to Intuition.
	public static bool RequestIDCMP<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, uint flags) where TPlatform : struct, IMuiApplicationPlatform =>
		ChangeIDCMP(ref platform, state, window, flags, true);

	public static bool RejectIDCMP<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, uint flags) where TPlatform : struct, IMuiApplicationPlatform =>
		ChangeIDCMP(ref platform, state, window, flags, false);

	private static bool CloseWindowCore<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, bool preserveIconifiedOpen)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var lifecycle = ReadWindowLifecycle(ref platform, state, window);
		var nativeWindow = lifecycle.NativeWindow;
		if (nativeWindow.IsNotNull)
		{
			// Closing a sleeping window releases the platform busy pointer. The
			// logical sleep depth remains on the MUI object and is replayed when
			// a later OpenWindow creates another native window.
			if (ReadWindowSleepState(ref platform, state, window).Depth != 0)
				platform.SetMuiWindowBusy(nativeWindow, false);
			platform.CloseMuiWindow(nativeWindow);
		}
		lifecycle.NativeWindow = APTR.Null;
		lifecycle.Open = 0;
		if (!preserveIconifiedOpen) lifecycle.IconifiedOpen = 0;
		return WriteWindowLifecycle(ref platform, state, window, lifecycle);
	}

	public static bool CloseWindow<TPlatform>(ref TPlatform platform, APTR state,
		APTR window) where TPlatform : struct, IMuiApplicationPlatform =>
		CloseWindowCore(ref platform, state, window, false);

	// MUIA_Window_Open is the public BOOL view of the existing native-window
	// lifecycle. The typed route publishes TRUE only after a native window has
	// opened and clears it on close, so guest state cannot drift from reality.
	public static bool SetWindowOpenValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint value)
		where TPlatform : struct, IMuiApplicationPlatform =>
		value != 0 ? OpenWindow(ref platform, state, window, 0) :
		CloseWindow(ref platform, state, window);

	public static uint PushMethod<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR destination, int count, APTR parameters)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		// MorphOS documents a maximum of seven arguments for this packet. The
		// first copied word is the destination method and is also the selector
		// used by MUIM_Application_UnpushMethod's third argument.
		if (destination.IsNull || count <= 0 || count > 7 || parameters.IsNull)
			return 0;
		var payloadBytes = (uint)count * 4u;
		if (!platform.IsMapped(parameters, payloadBytes)) return 0;
		var size = MuiApplicationWindowNodeRecord.Size + payloadBytes;
		var node = MuiHeadlessMemory.Allocate(ref platform, size);
		if (node.IsNull) return 0;
		var methodId = MuiHeadlessMemory.NextSequence(ref platform, state);
		var record = default(MuiApplicationWindowNodeRecord);
		record.Value = destination;
		record.Sequence = methodId;
		record.Auxiliary = unchecked((uint)count);
		if (!MuiApplicationWindowNodeCodec.Write(ref platform, node, record))
		{
			platform.Clear(node, size);
			platform.Free(node, size);
			return 0;
		}
		if (!MuiApplicationWindowNodeCodec.TryGetPayload(ref platform, node,
			payloadBytes, out var payload))
		{
			platform.Clear(node, size);
			platform.Free(node, size);
			return 0;
		}
		platform.Copy(parameters, payload, payloadBytes);
		var scheduler = ReadApplicationSchedulerState(ref platform, state,
			application);
		var tail = scheduler.PushTail;
		if (tail.IsNotNull)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, tail,
				out var tailRecord))
			{
				platform.Clear(node, size);
				platform.Free(node, size);
				return 0;
			}
			tailRecord.Next = node;
			MuiApplicationWindowNodeCodec.Write(ref platform, tail, tailRecord);
		}
		else if (!Set(ref platform, state, application, PushHead, node.Raw))
		{
			platform.Clear(node, size);
			platform.Free(node, size);
			return 0;
		}
		Set(ref platform, state, application, PushTail, node.Raw);
		PublishApplicationSchedulerState(ref platform, state, application, out _);
		return methodId;
	}

	public static uint DispatchPushedMethod<TPlatform>(ref TPlatform platform,
		APTR state, APTR application)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return 0;
		var scheduler = ReadApplicationSchedulerState(ref platform, state,
			application);
		var node = scheduler.PushHead;
		if (node.IsNull || !MuiApplicationWindowNodeCodec.TryRead(ref platform,
			node, out var record)) return 0;
		Set(ref platform, state, application, PushHead, record.Next.Raw);
		if (record.Next.IsNull) Set(ref platform, state, application, PushTail, 0);
		var result = 0u;
		var payloadBytes = record.Auxiliary * 4u;
		if (record.Value.IsNotNull &&
			MuiApplicationWindowNodeCodec.TryGetPayload(ref platform, node,
				payloadBytes, out var payload))
			result = platform.DoMethod(record.Value, payload);
		var size = MuiApplicationWindowNodeRecord.Size + record.Auxiliary * 4u;
		platform.Clear(node, size);
		platform.Free(node, size);
		PublishApplicationSchedulerState(ref platform, state, application, out _);
		return result;
	}

	public static uint UnpushMethod<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR destination, uint methodId, uint method)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var scheduler = ReadApplicationSchedulerState(ref platform, state,
			application);
		var current = scheduler.PushHead;
		var previous = APTR.Null;
		uint removed = 0;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, current,
				out var record)) break;
			var next = record.Next;
			var count = record.Auxiliary;
			var targetMatches = destination.IsNull || record.Value == destination;
			var recordMatches = count != 0 && count <= 7;
			var idMatches = methodId == 0 ||
				(recordMatches && record.Sequence == methodId);
			var methodMatches = method == 0 ||
				(recordMatches && record.Packet == method);
			var matches = targetMatches && idMatches && methodMatches;
			if (matches)
			{
				if (previous.IsNull) Set(ref platform, state, application, PushHead,
					next.Raw);
				else
				{
					if (!MuiApplicationWindowNodeCodec.TryRead(ref platform,
						previous, out var previousRecord)) return removed;
					previousRecord.Next = next;
					MuiApplicationWindowNodeCodec.Write(ref platform, previous,
						previousRecord);
				}
				if (scheduler.PushTail == current)
					Set(ref platform, state, application, PushTail, previous.Raw);
				var size = MuiApplicationWindowNodeRecord.Size + count * 4u;
				platform.Clear(current, size);
				platform.Free(current, size);
				removed++;
			}
			else previous = current;
			current = next;
		}
		PublishApplicationSchedulerState(ref platform, state, application, out _);
		return removed;
	}

	public static bool ReturnId<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, uint returnId)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var scheduler = ReadApplicationSchedulerState(ref platform, state,
			application);
		var node = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationWindowNodeRecord.Size);
		if (node.IsNull) return false;
		var record = default(MuiApplicationWindowNodeRecord);
		record.Value = APTR.FromPointer(returnId);
		record.Sequence = MuiHeadlessMemory.NextSequence(ref platform, state);
		if (!MuiApplicationWindowNodeCodec.Write(ref platform, node, record))
		{
			platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
			platform.Free(node, MuiApplicationWindowNodeRecord.Size);
			return false;
		}
		var tail = scheduler.ReturnTail;
		if (tail.IsNotNull)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, tail,
				out var tailRecord))
			{
				platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
				platform.Free(node, MuiApplicationWindowNodeRecord.Size);
				return false;
			}
			tailRecord.Next = node;
			MuiApplicationWindowNodeCodec.Write(ref platform, tail, tailRecord);
		}
		else if (!Set(ref platform, state, application, ReturnHead, node.Raw))
		{
			platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
			platform.Free(node, MuiApplicationWindowNodeRecord.Size);
			return false;
		}
		Set(ref platform, state, application, ReturnTail, node.Raw);
		var task = platform.CurrentTaskToken();
		var mask = scheduler.SignalMask;
		if (task != 0 && mask != 0) platform.SignalTask(task, mask);
		PublishApplicationSchedulerState(ref platform, state, application, out _);
		return true;
	}

	public static uint Input<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR signalStorage)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return 0;
		var scheduler = ReadApplicationSchedulerState(ref platform, state,
			application);
		var head = scheduler.ReturnHead;
		if (head.IsNotNull && MuiApplicationWindowNodeCodec.TryRead(ref platform,
			head, out var record))
		{
			var next = record.Next;
			var result = record.Value.Raw;
			Set(ref platform, state, application, ReturnHead, next.Raw);
			if (next.IsNull) Set(ref platform, state, application, ReturnTail, 0);
			platform.Clear(head, MuiApplicationWindowNodeRecord.Size);
			platform.Free(head, MuiApplicationWindowNodeRecord.Size);
			var emptySignals = default(MuiApplicationWindowSignalStorage);
			MuiApplicationWindowSignalStorageCodec.Write(ref platform,
				signalStorage, emptySignals);
			PublishApplicationSchedulerState(ref platform, state, application, out _);
			return result;
		}
		var signals = platform.ReadSignals(scheduler.SignalMask);
		var signalValue = default(MuiApplicationWindowSignalStorage);
		signalValue.Signals = signals;
		MuiApplicationWindowSignalStorageCodec.Write(ref platform, signalStorage,
			signalValue);
		PublishApplicationSchedulerState(ref platform, state, application, out _);
		return 0;
	}

	public static uint RunIteration<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR signalStorage, APTR eventStorage)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		DispatchPushedMethod(ref platform, state, application);
		var result = Input(ref platform, state, application, signalStorage);
		uint delivered = 0;
		if (MuiApplicationWindowSignalStorageCodec.TryRead(ref platform,
			signalStorage, out var signalValue))
			delivered = signalValue.Signals;
		DispatchInputHandlers(ref platform, state, application, delivered);
		PollWindowEvents(ref platform, state, application, eventStorage);
		return result;
	}

	// MUIM_Application_Execute and MUIM_Application_Run share the documented
	// ideal MUI loop. The platform wait is deliberately non-consuming: Input()
	// reads and clears the resulting signals on the next iteration. The bound
	// keeps malformed/native test environments from turning a guest call into
	// an unbounded host loop while preserving normal ReturnID_Quit behavior.
	public static uint RunApplication<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR signalStorage, APTR eventStorage)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return 0;
		for (var iteration = 0u; iteration < MaximumRunIterations; iteration++)
		{
			var result = RunIteration(ref platform, state, application,
				signalStorage, eventStorage);
			if (result == uint.MaxValue) return result;
			var mask = ReadApplicationSchedulerState(ref platform, state,
				application).SignalMask;
			if (mask == 0) continue;
			var signals = platform.WaitMuiSignals(mask | SignalBreakCtrlC);
			if ((signals & SignalBreakCtrlC) != 0) return uint.MaxValue;
		}
		return 0;
	}

	public static uint DispatchInputHandlers<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, uint deliveredSignals)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return 0;
		if (ReadApplicationSleepState(ref platform, state, application).Depth != 0)
			return 0;
		var current = ReadApplicationSchedulerState(ref platform, state,
			application).InputHandlers;
		var ticks = platform.ReadTicks();
		uint dispatched = 0;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, current,
				out var record)) break;
			var next = record.Next;
			var handler = record.Value;
			if (MuiInputHandlerCodec.TryRead(ref platform, handler,
				out var handlerRecord))
			{
				var value = handlerRecord.Events;
				var millis = value >> 16;
				var last = record.Auxiliary;
				var due = millis == 0 ? (value & deliveredSignals) != 0 :
					ticks - last >= millis;
				if (due)
				{
					record.Auxiliary = ticks;
					MuiApplicationWindowNodeCodec.Write(ref platform, current,
						record);
					var target = handlerRecord.Object;
					if (target.IsNotNull)
					{
						if (MuiApplicationWindowNodeCodec.TryGetPayload(ref platform,
							current, MuiApplicationMethodHeaderMessage.Size,
							out var payload))
						{
							platform.DoMethod(target, payload);
							dispatched++;
						}
					}
				}
			}
			current = next;
		}
		return dispatched;
	}

	public static uint PollWindowEvents<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR eventStorage)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (eventStorage.IsNull || !platform.IsMapped(eventStorage,
			global::Amiga.InputEvent.Size)) return 0;
		uint dispatched = 0;
		for (var index = 0; index < 65535; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				index, APTR.Null);
			if (window.IsNull) break;
			var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
				.NativeWindow;
			if (nativeWindow.IsNull) continue;
			for (var eventIndex = 0; eventIndex < 16; eventIndex++)
			{
				var eventClass = platform.ReadWindowEvent(nativeWindow, eventStorage);
				if (eventClass == 0) break;
				PublishWindowInputEventValue(ref platform, state, window,
					eventStorage);
				if (eventClass == 0x00000200)
				{
					Set(ref platform, state, window, WindowCloseRequest, 1);
					PublishWindowEventState(ref platform, state, window, out _);
				}
				dispatched += DispatchWindowEvent(ref platform, state, window,
					eventStorage, eventClass);
			}
		}
		return dispatched;
	}

	public static bool AddInputHandler<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR handler)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		return AddHandler(ref platform, state, application, InputHandlers, handler,
			MuiInputHandlerRecord.Size);
	}

	public static bool RemoveInputHandler<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR handler)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		return RemoveHandler(ref platform, state, application, InputHandlers,
			handler);
	}

	public static bool AddEventHandler<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, APTR handler)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return AddEventHandlerRegistration(ref platform, state, window, handler);
	}

	// Event-handler registration itself only needs the headless BOOPSI and
	// guest-memory capabilities. Keeping this seam separate from input-handler
	// registration avoids making FamilyCore depend on the application timer or
	// native-window capabilities merely to reconcile MUIA_HandledEvents.
	internal static bool AddEventHandlerRegistration<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, APTR handler)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		if (handler.IsNull || !platform.IsMapped(handler,
			MuiEventHandlerNodeRecord.Size) ||
			!MuiEventHandlerNodeCodec.TryRead(ref platform, handler,
				out var eventHandler)) return false;
		var node = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationWindowNodeRecord.Size);
		if (node.IsNull) return false;
		var enabledHandler = eventHandler;
		enabledHandler.Flags = (ushort)(enabledHandler.Flags |
			EventHandlerEnabled);
		if (!MuiEventHandlerNodeCodec.Write(ref platform, handler,
			enabledHandler))
		{
			platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
			platform.Free(node, MuiApplicationWindowNodeRecord.Size);
			return false;
		}
		var sequence = MuiHeadlessMemory.NextSequence(ref platform, state);
		if (!InsertEventHandler(ref platform, state, window, handler, node,
			enabledHandler, sequence))
		{
			MuiEventHandlerNodeCodec.Write(ref platform, handler, eventHandler);
			return false;
		}
		// MUI maintains ISACTIVE as registration state derived from the
		// window's current active/default objects. Refresh the named records
		// after insertion so callers can observe the state immediately.
		RefreshEventHandlerActiveFlags(ref platform, state, window);
		return true;
	}

	public static bool RemoveEventHandler<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, APTR handler)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		if (!RemoveHandler(ref platform, state, window, EventHandlers, handler))
			return false;
		return SetEventHandlerState(ref platform, handler, false, false);
	}

	public static uint DispatchWindowEvent<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, APTR eventMessage, uint eventClass)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		// A sleeping window is disabled until every matching wake request has
		// arrived. Do this before event-handler reconciliation so sleeping input
		// cannot trigger callbacks or mutate read-only handler state.
		if (ReadWindowSleepState(ref platform, state, window).Depth != 0)
			return 0;
		// Active/default object changes are represented by the window's named
		// attributes. Reconcile all registered records before routing, without
		// keeping a managed shadow list.
		RefreshEventHandlerActiveFlags(ref platform, state, window);
		var active = APTR.FromPointer(Read(ref platform, state, window,
			ActiveObject));
		var defaultObject = APTR.FromPointer(Read(ref platform, state, window,
			DefaultObject));
		var keyEvent = IsMuiKeyEvent(ref platform, eventMessage);
		// MorphOS applies MUIA_Window_DisableKeys to preprocessed MUI key
		// packets before consulting the event-handler queue. The mask is a
		// named window attribute and only the non-negative key index is used.
		if (keyEvent && IsWindowKeyDisabled(ref platform, state, window,
			eventMessage)) return 0;
		var firstEventClass = eventClass | DispatchPriorityProbe;
		var routeResult = 0u;
		if (active.IsNotNull)
			routeResult = DispatchWindowEventPass(ref platform, state, window,
				eventMessage, firstEventClass, active, APTR.Null, APTR.Null,
				APTR.Null);
		else if (defaultObject.IsNotNull)
			routeResult = DispatchWindowEventPass(ref platform, state, window,
				eventMessage, firstEventClass, defaultObject, APTR.Null, APTR.Null,
				APTR.Null);
		else
			routeResult = DispatchWindowEventPass(ref platform, state, window,
				eventMessage, firstEventClass | DispatchPriorityOnly, APTR.Null,
				APTR.Null, APTR.Null, APTR.Null);
		if (routeResult == EventHandlerEat) return EventHandlerEat;
		var hasPriority = routeResult == DispatchPriorityVisited;
		var remainingEventClass = hasPriority ?
			eventClass | DispatchPrioritySkip : eventClass;
		var activeParentRoot = APTR.Null;
		if (keyEvent && active.IsNotNull)
		{
			activeParentRoot = active;
			var parent = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				active);
			uint parentVisited = 0;
			while (parent.IsNotNull &&
				parentVisited++ < MuiHeadlessLayout.MaximumTraversal)
			{
				if (parent != defaultObject &&
					DispatchWindowEventPass(ref platform, state, window,
						eventMessage, remainingEventClass, parent, APTR.Null,
						APTR.Null, APTR.Null) == EventHandlerEat)
					return EventHandlerEat;
				if (APTR.FromPointer(Read(ref platform, state, window,
					ActiveObject)) != active) break;
				parent = MuiHeadlessObjectCore.ParentObject(ref platform, state,
					parent);
			}
		}
		if (active.IsNotNull && defaultObject.IsNotNull && defaultObject != active &&
			DispatchWindowEventPass(ref platform, state, window, eventMessage,
				remainingEventClass, defaultObject, APTR.Null, APTR.Null,
				activeParentRoot) == EventHandlerEat)
			return EventHandlerEat;
		return DispatchWindowEventPass(ref platform, state, window, eventMessage,
			remainingEventClass, APTR.Null, active, defaultObject,
			activeParentRoot);
	}

	// MorphOS checks handlers belonging to the active object first, then the
	// active object's parent chain for MUI key packets, then the default object,
	// and only then the remaining priority-ordered queue. Each pass re-reads the
	// named guest list, so no managed traversal state or shadow handler array is
	// required.
	private static uint DispatchWindowEventPass<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, APTR eventMessage,
		uint eventClass, APTR preferredObject, APTR excludedObject,
		APTR secondExcludedObject, APTR excludedActiveParentRoot)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var probeState = (eventClass & DispatchPriorityProbe) != 0 ? 1u : 0u;
		var skipState = (eventClass & DispatchPrioritySkip) != 0 ? 1u : 0u;
		var priorityOnly = (eventClass & DispatchPriorityOnly) != 0;
		var dispatchEventClass = eventClass &
			~(DispatchPriorityProbe | DispatchPrioritySkip | DispatchPriorityOnly);
		var current = APTR.FromPointer(Read(ref platform, state, window,
			EventHandlers));
		var boundary = current.Raw;
		var prioritySeen = false;
		var keyEvent = IsMuiKeyEvent(ref platform, eventMessage);
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, current,
				out var record)) return 0;
			var next = record.Next;
			if (probeState != 0)
			{
				if (probeState == 1)
				{
					if (record.Auxiliary != 0)
					{
						probeState = 2;
						prioritySeen = true;
					}
					else probeState = 0;
				}
				if (probeState == 2)
				{
					if (record.Auxiliary == 0) probeState = 0;
					else
					{
						RefreshEventHandlerActiveFlag(ref platform, state, window,
							record.Value);
						if (DispatchEventHandlerNode(ref platform, state, record.Value,
							eventMessage, dispatchEventClass) == EventHandlerEat)
							return EventHandlerEat;
						current = next;
						continue;
					}
				}
			}
			if (skipState != 0)
			{
				if (record.Auxiliary != 0)
				{
					current = next;
					continue;
				}
				skipState = 0;
			}
			if (priorityOnly)
			{
				current = next;
				continue;
			}
			if (MuiEventHandlerNodeCodec.TryRead(ref platform, record.Value,
				out var eventHandler))
			{
				var excludedActiveParent = preferredObject.IsNull &&
					excludedActiveParentRoot.IsNotNull &&
					IsActiveParentObject(ref platform, state,
						excludedActiveParentRoot, eventHandler.Object);
				var selected = preferredObject.IsNotNull ?
					eventHandler.Object == preferredObject :
					!excludedActiveParent && eventHandler.Object != excludedObject &&
					eventHandler.Object != secondExcludedObject;
				var keyboardEligible = !keyEvent || preferredObject.IsNotNull ||
					(eventHandler.Flags & EventHandlerAlwaysKeys) != 0;
				if (selected && keyboardEligible)
				{
					RefreshEventHandlerActiveFlag(ref platform, state, window,
						record.Value);
					if (DispatchEventHandlerNode(ref platform, state, record.Value,
						eventMessage, dispatchEventClass) == EventHandlerEat)
						return EventHandlerEat;
				}
			}
			if (current.Raw == boundary) boundary = 0;
			current = next;
		}
		return prioritySeen ? DispatchPriorityVisited : 0;
	}

	private static bool IsActiveParentObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR activeObject, APTR candidate)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
			activeObject);
		uint visited = 0;
		while (current.IsNotNull &&
			visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (current == candidate) return true;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return false;
	}

	private static bool IsMuiKeyEvent<TPlatform>(ref TPlatform platform,
		APTR eventMessage)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		return MuiCommonControlPacketCore.TryReadHandleEvent(ref platform,
			eventMessage, out var packet) && packet.MuiKey != MuiKeyNone;
	}

	private static bool IsWindowKeyDisabled<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, APTR eventMessage)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiCommonControlPacketCore.TryReadHandleEvent(ref platform,
			eventMessage, out var packet) || packet.MuiKey < 0 ||
			packet.MuiKey >= 32) return false;
		var mask = Read(ref platform, state, window, WindowDisableKeys);
		return (mask & (1u << packet.MuiKey)) != 0;
	}

	// Deliver one registered MUI_EventHandlerNode. The window walk above owns
	// list traversal; this typed helper owns the MorphOS GUI-mode/event-mask
	// gate and the final DoMethod/CoerceMethod callback. MorphOS uses ehn_Class
	// to bypass the object's subclass chain and invoke MUIM_HandleEvent through
	// that exact dispatcher.
	public static uint DispatchEventHandlerNode<TPlatform>(ref TPlatform platform,
		APTR handler, APTR eventMessage, uint eventClass)
		where TPlatform : struct, IMuiApplicationPlatform
		=> DispatchEventHandlerNodeCore(ref platform, APTR.Null, handler,
			eventMessage, eventClass);

	public static uint DispatchEventHandlerNode<TPlatform>(ref TPlatform platform,
		APTR state, APTR handler, APTR eventMessage, uint eventClass)
		where TPlatform : struct, IMuiApplicationPlatform
		=> DispatchEventHandlerNodeCore(ref platform, state, handler,
			eventMessage, eventClass);

	private static uint DispatchEventHandlerNodeCore<TPlatform>(
		ref TPlatform platform, APTR state, APTR handler, APTR eventMessage,
		uint eventClass)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!TryReadEventHandler(ref platform, handler, out var eventHandler) ||
			(eventHandler.Flags & EventHandlerGuiMode) == 0 ||
			(eventHandler.Events & eventClass) == 0 || eventHandler.Object.IsNull)
			return 0;
		if (state.IsNotNull && !GuiModeAllows(ref platform, state,
			eventHandler.Object, eventClass)) return 0;
		// MUI marks a node while its callback is in flight. Update the named
		// guest record before calling out, then re-read it afterwards so callback
		// mutations to priority, flags, object, or class are preserved. No
		// managed shadow state is needed and the bit is cleared even when the
		// callback changed other fields.
		eventHandler.Flags = (ushort)(eventHandler.Flags | EventHandlerCalling);
		MuiEventHandlerNodeCodec.Write(ref platform, handler, eventHandler);
		var result = eventHandler.Class.IsNotNull ?
			platform.CoerceMethod(eventHandler.Class, eventHandler.Object,
				eventMessage) :
			platform.DoMethod(eventHandler.Object, eventMessage);
		if (MuiEventHandlerNodeCodec.TryRead(ref platform, handler,
			out var completedHandler))
		{
			completedHandler.Flags = (ushort)(completedHandler.Flags &
				~EventHandlerCalling);
			MuiEventHandlerNodeCodec.Write(ref platform, handler,
				completedHandler);
		}
		// Preserve the full MorphOS method result at this typed boundary. The
		// window queue consumes only MUI_EventHandlerRC_Eat (1); other non-zero
		// values are observable by direct callers but do not stop the queue.
		return result;
	}

	private static bool GuiModeAllows<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint eventClass)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		// MorphOS still delivers window state transitions in GUI mode even when
		// the object is disabled or hidden.
		if ((eventClass & (EventClassActiveWindow | EventClassInactiveWindow |
			EventClassChangeWindow)) != 0) return true;
		var current = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		uint visited = 0;
		while (current.IsNotNull && visited < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, current,
				out var objectValue)) return false;
			if (visited == 0 && objectValue.Parent.IsNotNull &&
				!GuiModeVirtualGroupAllows(ref platform, current)) return false;
			if (!GuiModeObjectAllows(ref platform, state, objectValue.Boopsi) ||
				!GuiModePageAllows(ref platform, state, objectValue.Boopsi,
					objectValue.Parent))
				return false;
			visited++;
			current = objectValue.Parent;
		}
		return current.IsNull;
	}

	private static bool GuiModeVirtualGroupAllows<TPlatform>(
		ref TPlatform platform, APTR objectRecord)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, objectRecord,
			out var objectValue)) return false;
		var obj = objectValue.Boopsi;
		var parentRecord = objectValue.Parent;
		if (parentRecord.IsNull) return true;
		var objectRectRead = false;
		var objectLeft = 0;
		var objectTop = 0;
		var objectWidth = 0;
		var objectHeight = 0;
		uint visited = 0;
		while (parentRecord.IsNotNull && visited++ <
			MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, parentRecord,
				out var parentValue)) return false;
			var parent = parentValue.Boopsi;
			if (MuiHeadlessObjectCore.GetAttributeList(ref platform,
				parentValue.Attributes, VirtgroupWidth, out _) &&
				MuiHeadlessObjectCore.GetAttributeList(ref platform,
					parentValue.Attributes, VirtgroupHeight, out _))
			{
				if (!objectRectRead)
				{
					if (!TryReadAreaRect(ref platform, objectValue.Attributes,
						out objectLeft, out objectTop, out objectWidth,
						out objectHeight)) return true;
					objectRectRead = true;
				}
				if (TryReadAreaRect(ref platform, parentValue.Attributes,
					out var parentLeft,
					out var parentTop, out var parentWidth, out var parentHeight) &&
					!RectanglesIntersect(objectLeft, objectTop, objectWidth,
						objectHeight, parentLeft, parentTop, parentWidth,
						parentHeight)) return false;
			}
			parentRecord = parentValue.Parent;
		}
		return parentRecord.IsNull;
	}

	private static bool TryReadAreaRect<TPlatform>(ref TPlatform platform,
		APTR attributes, out int left, out int top, out int width, out int height)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		left = top = width = height = 0;
		if (!MuiHeadlessObjectCore.GetAttributeList(ref platform, attributes,
			AreaLeftEdge, out var leftRaw) || !MuiHeadlessObjectCore.GetAttributeList(
			ref platform, attributes, AreaTopEdge, out var topRaw) ||
			!MuiHeadlessObjectCore.GetAttributeList(ref platform, attributes,
				AreaWidth, out var widthRaw) || !MuiHeadlessObjectCore.GetAttributeList(
				ref platform, attributes, AreaHeight, out var heightRaw)) return false;
		left = unchecked((int)leftRaw);
		top = unchecked((int)topRaw);
		width = unchecked((int)widthRaw);
		height = unchecked((int)heightRaw);
		return true;
	}

	private static bool RectanglesIntersect(int left, int top, int width,
		int height, int clipLeft, int clipTop, int clipWidth, int clipHeight)
	{
		if (width <= 0 || height <= 0 || clipWidth <= 0 || clipHeight <= 0)
			return false;
		return AxisIntersects(left, width, clipLeft, clipWidth) &&
			AxisIntersects(top, height, clipTop, clipHeight);
	}

	private static bool AxisIntersects(int start, int length, int clipStart,
		int clipLength) => start >= clipStart ? start - clipStart < clipLength :
		clipStart - start < length;

	private static bool GuiModeObjectAllows<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			Disabled, out var disabled) && disabled != 0) return false;
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			ShowMe, out var showMe) && showMe == 0) return false;
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			IsShown, out var shown) && shown == 0) return false;
		return true;
	}

	private static bool GuiModePageAllows<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR parentRecord)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (parentRecord.IsNull) return true;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, parentRecord,
			out var parentValue)) return false;
		var parent = parentValue.Boopsi;
		if (parent.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			state, parent, GroupPageMode, out var pageMode) || pageMode == 0)
			return true;
		var count = 0u;
		var childIndex = uint.MaxValue;
		while (count < MuiHeadlessLayout.MaximumTraversal)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, parent,
				unchecked((int)count), APTR.Null);
			if (child.IsNull) break;
			if (child == obj) childIndex = count;
			count++;
		}
		if (childIndex == uint.MaxValue || count == 0) return false;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, parent,
			GroupActivePage, out var active)) active = 0;
		if (active >= count) active = 0;
		return childIndex == active;
	}

	public static bool Activate<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, APTR target)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var previous = ReadWindowFocusState(ref platform, state, window)
			.ActiveObject;
		var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
			.NativeWindow;
		if (nativeWindow.IsNull || !platform.ActivateMuiWindow(nativeWindow))
			return false;
		if (!Set(ref platform, state, window, ActiveObject, target.Raw))
			return false;
		if (!PublishWindowFocusState(ref platform, state, window, out _))
		{
			Set(ref platform, state, window, ActiveObject, previous.Raw);
			PublishWindowFocusState(ref platform, state, window, out _);
			return false;
		}
		RefreshEventHandlerActiveFlags(ref platform, state, window);
		return true;
	}

	// MUIA_Window_DefaultObject accepts a MUI area (or NULL) that receives
	// keyboard input whenever the window has no active object.  Unlike the
	// active-object setter, this attribute is not a cycle-chain selector; it
	// is stored directly and the read-only event-handler active flags are
	// refreshed immediately so subsequent dispatch sees the updated default.
	public static bool SetDefaultObjectValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, APTR target)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		if (target.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
			state, target).IsNull) return false;
		var previous = ReadWindowFocusState(ref platform, state, window)
			.DefaultObject;
		if (!Set(ref platform, state, window, DefaultObject, target.Raw))
			return false;
		if (!PublishWindowFocusState(ref platform, state, window, out _))
		{
			Set(ref platform, state, window, DefaultObject, previous.Raw);
			PublishWindowFocusState(ref platform, state, window, out _);
			return false;
		}
		return RefreshEventHandlerActiveFlags(ref platform, state, window);
	}

	// MUIA_Window_Activate is a write-one window request.  MorphOS ignores a
	// FALSE write; a TRUE write requires an open native window and records the
	// resulting active state only after the platform accepts the activation.
	public static bool SetActivateValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		if (value == 0) return true;
		var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
			.NativeWindow;
		if (nativeWindow.IsNull || !platform.ActivateMuiWindow(nativeWindow))
			return false;
		return Set(ref platform, state, window, WindowActivate, 1);
	}

	private static bool SetWindowSleepDepth<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint next)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		var sleep = ReadWindowSleepState(ref platform, state, window);
		var depth = sleep.Depth;
		if (depth == next) return PublishWindowSleepState(ref platform, state,
			window);
		var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
			.NativeWindow;
		if (next > depth)
		{
			var disabled = Read(ref platform, state, window, Disabled);
			if (depth == 0)
			{
				if (nativeWindow.IsNotNull &&
					!platform.SetMuiWindowBusy(nativeWindow, true)) return false;
				if (!Set(ref platform, state, window, WindowSleepDisabled,
					disabled) || !Set(ref platform, state, window, Disabled, 1))
				{
					if (nativeWindow.IsNotNull)
						platform.SetMuiWindowBusy(nativeWindow, false);
					return false;
				}
			}
			if (!Set(ref platform, state, window, WindowSleepDepth, next) ||
				!Set(ref platform, state, window, WindowSleep, next))
			{
				Set(ref platform, state, window, WindowSleepDepth, depth);
				Set(ref platform, state, window, WindowSleep, depth);
				if (depth == 0)
				{
					Set(ref platform, state, window, Disabled, disabled);
					if (nativeWindow.IsNotNull)
						platform.SetMuiWindowBusy(nativeWindow, false);
				}
				return false;
			}
			return PublishWindowSleepState(ref platform, state, window);
		}

		var restoredDisabled = ReadWindowSleepState(ref platform, state, window)
			.SavedDisabled;
		if (next == 0)
		{
			if (nativeWindow.IsNotNull &&
				!platform.SetMuiWindowBusy(nativeWindow, false)) return false;
			if (!Set(ref platform, state, window, Disabled, restoredDisabled))
			{
				if (nativeWindow.IsNotNull)
					platform.SetMuiWindowBusy(nativeWindow, true);
				return false;
			}
		}
		if (!Set(ref platform, state, window, WindowSleepDepth, next) ||
			!Set(ref platform, state, window, WindowSleep, next))
		{
			Set(ref platform, state, window, WindowSleepDepth, depth);
			Set(ref platform, state, window, WindowSleep, depth);
			if (next == 0)
			{
				Set(ref platform, state, window, Disabled, 1);
				if (nativeWindow.IsNotNull)
					platform.SetMuiWindowBusy(nativeWindow, true);
			}
			return false;
		}
		return PublishWindowSleepState(ref platform, state, window);
	}

	// MUIA_Window_Sleep is a nesting counter. A non-zero write sleeps once and
	// a zero write wakes once; the window's prior disabled state is restored
	// only after the final matching wake request.
	public static bool SetSleepValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		var depth = ReadWindowSleepState(ref platform, state, window).Depth;
		if (value != 0)
			return depth != uint.MaxValue && SetWindowSleepDepth(ref platform,
				state, window, depth + 1);
		return depth == 0 || SetWindowSleepDepth(ref platform, state, window,
			depth - 1);
	}

	// MUIA_Window_ActiveObject special inputs. MorphOS uses zero for None,
	// -1/-2 for Next/Prev, and -3..-6 for directional selection. Spatial
	// selection is bounded to the copied cycle chain and the published Area
	// rectangle attributes; no managed traversal state is retained.
	public static bool SetActiveObjectValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		if (value == 0)
		{
			var previous = ReadWindowFocusState(ref platform, state, window)
				.ActiveObject;
			if (!Set(ref platform, state, window, ActiveObject, 0)) return false;
			if (!PublishWindowFocusState(ref platform, state, window, out _))
			{
				Set(ref platform, state, window, ActiveObject, previous.Raw);
				PublishWindowFocusState(ref platform, state, window, out _);
				return false;
			}
			RefreshEventHandlerActiveFlags(ref platform, state, window);
			return true;
		}
		if (value == ActiveObjectNext)
			return CycleActive(ref platform, state, window, true);
		if (value == ActiveObjectPrevious)
			return CycleActive(ref platform, state, window, false);
		if (value == ActiveObjectLeft || value == ActiveObjectRight ||
			value == ActiveObjectUp || value == ActiveObjectDown)
			return SelectSpatialActive(ref platform, state, window, value);
		var target = APTR.FromPointer(value);
		return IsCycleChainMember(ref platform, state, window, target) &&
			Activate(ref platform, state, window, target);
	}

	private static bool SelectSpatialActive<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint selector)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var active = APTR.FromPointer(Read(ref platform, state, window,
			ActiveObject));
		if (active.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			active).IsNull) return false;
		var interaction = ReadWindowInteractionState(ref platform, state, window);
		var chainCount = interaction.CycleChainCount;
		if (chainCount == 0) return false;
		var limit = chainCount > MuiHeadlessLayout.MaximumTraversal ?
			MuiHeadlessLayout.MaximumTraversal : chainCount;
		var node = interaction.CycleChainHead;
		var activeBox = default(MuiSpatialBox);
		var foundActive = false;
		for (var index = 0u; index < limit; index++)
		{
			if (node.IsNull || !MuiApplicationWindowNodeCodec.TryRead(ref platform,
				node, out var record)) return false;
			var member = record.Value;
			if (member == active)
			{
				if (!TryReadSpatialBox(ref platform, state, member, out activeBox))
					return false;
				foundActive = true;
				break;
			}
			node = record.Next;
		}
		if (!foundActive) return false;

		node = interaction.CycleChainHead;
		var best = APTR.Null;
		var bestPerpendicularGap = uint.MaxValue;
		var bestPrimaryGap = uint.MaxValue;
		var bestCenterDistance = uint.MaxValue;
		for (var index = 0u; index < limit; index++)
		{
			if (node.IsNull || !MuiApplicationWindowNodeCodec.TryRead(ref platform,
				node, out var record)) return false;
			var member = record.Value;
			if (member != active &&
				!MuiHeadlessObjectCore.FindObject(ref platform, state, member).IsNull &&
				TryReadSpatialBox(ref platform, state, member, out var candidate) &&
				TrySpatialScore(activeBox, candidate, selector,
					out var perpendicularGap, out var primaryGap,
					out var centerDistance) &&
				IsBetterSpatialCandidate(perpendicularGap, primaryGap,
					centerDistance, bestPerpendicularGap, bestPrimaryGap,
					bestCenterDistance))
			{
				best = member;
				bestPerpendicularGap = perpendicularGap;
				bestPrimaryGap = primaryGap;
				bestCenterDistance = centerDistance;
			}
			node = record.Next;
		}
		return best.IsNotNull && Activate(ref platform, state, window, best);
	}

	private static bool TryReadSpatialBox<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiSpatialBox box)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		box = default;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			LeftEdge, out var left) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				TopEdge, out var top) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				RightEdge, out var right) ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
				BottomEdge, out var bottom)) return false;
		box.Left = unchecked((int)left);
		box.Top = unchecked((int)top);
		box.Right = unchecked((int)right);
		box.Bottom = unchecked((int)bottom);
		return box.Right >= box.Left && box.Bottom >= box.Top;
	}

	private static bool TrySpatialScore(MuiSpatialBox active,
		MuiSpatialBox candidate, uint selector, out uint perpendicularGap,
		out uint primaryGap, out uint centerDistance)
	{
		perpendicularGap = 0;
		primaryGap = 0;
		centerDistance = 0;
		var activeCenterX = Midpoint(active.Left, active.Right);
		var activeCenterY = Midpoint(active.Top, active.Bottom);
		var candidateCenterX = Midpoint(candidate.Left, candidate.Right);
		var candidateCenterY = Midpoint(candidate.Top, candidate.Bottom);
		if (selector == ActiveObjectLeft || selector == ActiveObjectRight)
		{
			if (selector == ActiveObjectLeft)
			{
				if (candidateCenterX >= activeCenterX) return false;
				primaryGap = PositiveGap(active.Left, candidate.Right);
			}
			else
			{
				if (candidateCenterX <= activeCenterX) return false;
				primaryGap = PositiveGap(candidate.Left, active.Right);
			}
			perpendicularGap = IntervalGap(active.Top, active.Bottom,
				candidate.Top, candidate.Bottom);
			centerDistance = AbsoluteDistance(activeCenterY, candidateCenterY);
			return true;
		}
		if (selector == ActiveObjectUp)
		{
			if (candidateCenterY >= activeCenterY) return false;
			primaryGap = PositiveGap(active.Top, candidate.Bottom);
		}
		else if (selector == ActiveObjectDown)
		{
			if (candidateCenterY <= activeCenterY) return false;
			primaryGap = PositiveGap(candidate.Top, active.Bottom);
		}
		else return false;
		perpendicularGap = IntervalGap(active.Left, active.Right,
			candidate.Left, candidate.Right);
		centerDistance = AbsoluteDistance(activeCenterX, candidateCenterX);
		return true;
	}

	private static bool IsBetterSpatialCandidate(uint perpendicularGap,
		uint primaryGap, uint centerDistance, uint bestPerpendicularGap,
		uint bestPrimaryGap, uint bestCenterDistance) =>
		perpendicularGap < bestPerpendicularGap ||
		(perpendicularGap == bestPerpendicularGap &&
			(primaryGap < bestPrimaryGap ||
				(primaryGap == bestPrimaryGap &&
					centerDistance < bestCenterDistance)));

	private static uint PositiveGap(int left, int right) => left > right ?
		unchecked((uint)left - (uint)right) : 0;

	private static uint IntervalGap(int firstLeft, int firstRight,
		int secondLeft, int secondRight)
	{
		if (firstRight < secondLeft) return PositiveGap(secondLeft, firstRight);
		if (secondRight < firstLeft) return PositiveGap(firstLeft, secondRight);
		return 0;
	}

	private static uint AbsoluteDistance(int left, int right) => left >= right ?
		unchecked((uint)left - (uint)right) : unchecked((uint)right - (uint)left);

	private static int Midpoint(int left, int right) =>
		(left & right) + ((left ^ right) >> 1);

	public static bool CycleActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, bool forward)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var active = APTR.FromPointer(Read(ref platform, state, window,
			ActiveObject));
		var interaction = ReadWindowInteractionState(ref platform, state, window);
		var chainCount = interaction.CycleChainCount;
		if (chainCount == 0) return false;
		var limit = chainCount > MuiHeadlessLayout.MaximumTraversal ?
			MuiHeadlessLayout.MaximumTraversal : chainCount;
		var node = interaction.CycleChainHead;
		var first = APTR.Null;
		var last = APTR.Null;
		var previous = APTR.Null;
		var activePrevious = APTR.Null;
		var activeNextNode = APTR.Null;
		var activeFound = false;
		for (var index = 0u; index < limit; index++)
		{
			if (node.IsNull || !MuiApplicationWindowNodeCodec.TryRead(ref platform,
				node, out var record)) return false;
			var member = record.Value;
			if (member.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
				state, member).IsNull) return false;
			if (first.IsNull) first = member;
			last = member;
			var nextNode = record.Next;
			if (member == active)
			{
				activeFound = true;
				activePrevious = previous;
				activeNextNode = nextNode;
			}
			previous = member;
			node = nextNode;
		}
		var target = APTR.Null;
		if (forward)
		{
			if (activeFound && activeNextNode.IsNotNull)
			{
				if (!MuiApplicationWindowNodeCodec.TryRead(ref platform,
					activeNextNode, out var activeNextRecord)) return false;
				target = activeNextRecord.Value;
				if (target.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
					state, target).IsNull) return false;
			}
			if (target.IsNull) target = first;
		}
		else
		{
			if (activeFound) target = activePrevious;
			if (target.IsNull) target = last;
		}
		return target.IsNotNull && Activate(ref platform, state, window, target);
	}

	private static bool IsCycleChainMember<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, APTR target)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (target.IsNull) return false;
		var interaction = ReadWindowInteractionState(ref platform, state, window);
		var chainCount = interaction.CycleChainCount;
		if (chainCount == 0) return false;
		var limit = chainCount > MuiHeadlessLayout.MaximumTraversal ?
			MuiHeadlessLayout.MaximumTraversal : chainCount;
		var node = interaction.CycleChainHead;
		for (var index = 0u; index < limit; index++)
		{
			if (node.IsNull || !MuiApplicationWindowNodeCodec.TryRead(ref platform,
				node, out var record)) return false;
			if (record.Value == target)
				return !MuiHeadlessObjectCore.FindObject(ref platform, state,
					target).IsNull;
			node = record.Next;
		}
		return false;
	}

	public static bool MoveWindow<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, bool toFront)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
			.NativeWindow;
		return nativeWindow.IsNotNull && platform.MoveMuiWindow(nativeWindow,
			toFront);
	}

	// MUIM_Window_ScreenToBack/ScreenToFront. The MorphOS packet is valid only
	// while the window owns a native window; the platform moves its containing
	// screen and owns the native depth operation.
	public static bool MoveScreen<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, bool toFront)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
			.NativeWindow;
		return nativeWindow.IsNotNull && platform.MoveMuiScreen(nativeWindow,
			toFront);
	}

	// MUIM_Window_Snapshot. MorphOS uses flags 0 to clear a remembered window
	// position and 1 to store the current position. Snapshotting is only valid
	// for a window with a non-zero MUIA_Window_ID; the platform owns the actual
	// settings store and may service the request while the window is closed.
	public static bool SnapshotWindow<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint flags)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (flags > 1 || MuiHeadlessObjectCore.FindObject(ref platform, state,
			window).IsNull) return false;
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, window,
			WindowId, out var id) || id == 0) return false;
		if (!platform.SnapshotMuiWindow(window, flags)) return false;
		if (!Set(ref platform, state, window, WindowSnapshotFlags, flags))
			return false;
		var interaction = ReadWindowInteractionState(ref platform, state, window);
		var requests = interaction.SnapshotRequests;
		if (!Set(ref platform, state, window, WindowSnapshotRequests,
			requests == uint.MaxValue ? uint.MaxValue : requests + 1)) return false;
		return PublishWindowInteractionState(ref platform, state, window, out _);
	}

	// MUIM_Window_SetCycleChain. The vector pointer addresses one or more MUI
	// object pointers immediately after the fixed `{MethodID, first-object}`
	// header and terminates with Null. Copy the caller's
	// vector into guest-resident nodes so the chain remains valid after the
	// caller's packet is gone. Replacement is failure-atomic and old nodes are
	// released by the ordinary object cleanup path.
	public static bool SetCycleChain<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, APTR vector)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		var interaction = ReadWindowInteractionState(ref platform, state, window);
		var oldHead = interaction.CycleChainHead;
		var oldCount = interaction.CycleChainCount;
		var oldRequests = interaction.CycleChainRequests;
		if (!Set(ref platform, state, window, WindowCycleChainHead, oldHead.Raw) ||
			!Set(ref platform, state, window, WindowCycleChainCount, oldCount) ||
			!Set(ref platform, state, window, WindowCycleChainRequests,
				oldRequests))
			return false;

		APTR head = APTR.Null;
		APTR tail = APTR.Null;
		uint count = 0;
		var terminated = false;
		var cursor = default(MuiApplicationWindowCycleChainCursor);
		cursor.Base = vector;
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal;
			index++)
		{
			cursor.Index = index;
			if (!MuiApplicationWindowCycleChainVectorCodec.TryGetEntry(
				ref platform, cursor, out var address))
			{
				FreeNodes(ref platform, head);
				return false;
			}
			if (!MuiApplicationWindowCycleChainSlotCodec.TryRead(
				ref platform, address, out var slotValue))
			{
				FreeNodes(ref platform, head);
				return false;
			}
			var member = slotValue.Object;
			if (member.IsNull)
			{
				terminated = true;
				break;
			}
			if (MuiHeadlessObjectCore.FindObject(ref platform, state,
				member).IsNull)
			{
				FreeNodes(ref platform, head);
				return false;
			}
			var node = MuiHeadlessMemory.Allocate(ref platform,
				MuiApplicationWindowNodeRecord.Size);
			if (node.IsNull)
			{
				FreeNodes(ref platform, head);
				return false;
			}
			var record = default(MuiApplicationWindowNodeRecord);
			record.Value = member;
			record.Sequence = count + 1;
			if (!MuiApplicationWindowNodeCodec.Write(ref platform, node, record))
			{
				platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
				platform.Free(node, MuiApplicationWindowNodeRecord.Size);
				FreeNodes(ref platform, head);
				return false;
			}
			if (tail.IsNull) head = node;
			else
			{
				if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, tail,
					out var tailRecord))
				{
					platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
					platform.Free(node, MuiApplicationWindowNodeRecord.Size);
					FreeNodes(ref platform, head);
					return false;
				}
				tailRecord.Next = node;
				MuiApplicationWindowNodeCodec.Write(ref platform, tail, tailRecord);
			}
			tail = node;
			count++;
		}
		if (!terminated)
		{
			FreeNodes(ref platform, head);
			return false;
		}

		if (!Set(ref platform, state, window, WindowCycleChainHead, head.Raw) ||
			!Set(ref platform, state, window, WindowCycleChainCount, count))
		{
			FreeNodes(ref platform, head);
			Set(ref platform, state, window, WindowCycleChainHead, oldHead.Raw);
			Set(ref platform, state, window, WindowCycleChainCount, oldCount);
			return false;
		}
		FreeNodes(ref platform, oldHead);
		if (!Set(ref platform, state, window, WindowCycleChainRequests,
			oldRequests == uint.MaxValue ? uint.MaxValue : oldRequests + 1))
			return false;
		return PublishWindowInteractionState(ref platform, state, window, out _);
	}

	public static bool SetMenu<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, uint menuId, bool enabled, bool check, bool checkedState)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
			.NativeWindow;
		return nativeWindow.IsNotNull && platform.SetMuiMenuState(nativeWindow,
			menuId, enabled, check, checkedState);
	}

	public static uint GetMenu<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, uint menuId, bool check)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return 0;
		var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
			.NativeWindow;
		bool value;
		return nativeWindow.IsNotNull && platform.GetMuiMenuState(nativeWindow,
			menuId, check, out value) && value ? 1u : 0u;
	}

	private static void RollbackApplicationWindowSleep<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint lastIndex,
		bool wake)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		for (var index = 0u; index <= lastIndex; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)index), APTR.Null);
			if (window.IsNull) break;
			if (Read(ref platform, state, window, WindowOwner) != application.Raw)
				continue;
			var depth = ReadWindowSleepState(ref platform, state, window).Depth;
			var next = wake ? (depth == uint.MaxValue ? depth : depth + 1) :
				(depth == 0 ? 0u : depth - 1);
			SetWindowSleepDepth(ref platform, state, window, next);
		}
	}

	private static bool SetApplicationWindowSleep<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, bool wake)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var changed = 0u;
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)index), APTR.Null);
			if (window.IsNull) return true;
			if (Read(ref platform, state, window, WindowOwner) != application.Raw)
				continue;
			var depth = ReadWindowSleepState(ref platform, state, window).Depth;
			if (!wake && depth == uint.MaxValue)
			{
				if (changed != 0) RollbackApplicationWindowSleep(ref platform,
					state, application, changed - 1, true);
				return false;
			}
			var next = wake ? (depth == 0 ? 0u : depth - 1) : depth + 1;
			if (!SetWindowSleepDepth(ref platform, state, window, next))
			{
				if (changed != 0) RollbackApplicationWindowSleep(ref platform,
					state, application, changed - 1, !wake);
				return false;
			}
			changed = index + 1;
		}
		return false;
	}

	// MUIA_Application_Sleep is a nesting counter over all windows owned by
	// the application. Window sleep depth remains a named per-window state, so
	// a window added while the application sleeps inherits the complete depth.
	public static bool SetApplicationSleepValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var depth = ReadApplicationSleepState(ref platform, state,
			application).Depth;
		if (value != 0)
		{
			if (depth == uint.MaxValue ||
				!SetApplicationWindowSleep(ref platform, state, application, false))
				return false;
			var next = depth + 1;
			if (!Set(ref platform, state, application, ApplicationSleepDepth, next) ||
				!Set(ref platform, state, application, ApplicationSleep, next))
			{
				Set(ref platform, state, application, ApplicationSleepDepth, depth);
				Set(ref platform, state, application, ApplicationSleep, depth);
				SetApplicationWindowSleep(ref platform, state, application, true);
				return false;
			}
			return PublishApplicationSleepState(ref platform, state, application);
		}
		if (depth == 0) return true;
		if (!SetApplicationWindowSleep(ref platform, state, application, true))
			return false;
		var remaining = depth - 1;
		if (!Set(ref platform, state, application, ApplicationSleepDepth,
			remaining) || !Set(ref platform, state, application, ApplicationSleep,
			remaining))
		{
			Set(ref platform, state, application, ApplicationSleepDepth, depth);
			Set(ref platform, state, application, ApplicationSleep, depth);
			SetApplicationWindowSleep(ref platform, state, application, false);
			return false;
		}
		return PublishApplicationSleepState(ref platform, state, application);
	}

	private static bool CaptureIconifiedWindows<TPlatform>(ref TPlatform platform,
		APTR state, APTR application)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)index), APTR.Null);
			if (window.IsNull) return true;
			if (Read(ref platform, state, window, WindowOwner) != application.Raw)
				continue;
			var lifecycle = ReadWindowLifecycle(ref platform, state, window);
			if (lifecycle.NativeWindow.IsNull) continue;
			lifecycle.IconifiedOpen = 1;
			if (!WriteWindowLifecycle(ref platform, state, window, lifecycle) ||
				!CloseWindowCore(ref platform, state, window, true)) return false;
		}
		return false;
	}

	private static bool RestoreIconifiedWindows<TPlatform>(ref TPlatform platform,
		APTR state, APTR application)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)index), APTR.Null);
			if (window.IsNull) return true;
			if (Read(ref platform, state, window, WindowOwner) != application.Raw)
				continue;
			var lifecycle = ReadWindowLifecycle(ref platform, state, window);
			if (lifecycle.IconifiedOpen == 0) continue;
			if (lifecycle.NativeWindow.IsNotNull)
			{
				lifecycle.IconifiedOpen = 0;
				if (!WriteWindowLifecycle(ref platform, state, window, lifecycle))
					return false;
				continue;
			}
			if (!OpenWindow(ref platform, state, window, 0)) return false;
		}
		return false;
	}

	private static bool ClearIconifiedWindowMarkers<TPlatform>(
		ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)index), APTR.Null);
			if (window.IsNull) return true;
			if (Read(ref platform, state, window, WindowOwner) == application.Raw)
			{
				var lifecycle = ReadWindowLifecycle(ref platform, state, window);
				lifecycle.IconifiedOpen = 0;
				if (!WriteWindowLifecycle(ref platform, state, window, lifecycle))
					return false;
			}
		}
		return false;
	}

	// MUIA_Application_Iconified closes all currently native child windows and
	// remembers their open requests.  A later FALSE write restores those
	// requests; an OpenWindow call made while iconified is recorded by
	// OpenWindow and follows the same path.
	public static bool SetIconified<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, bool iconified)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var lifecycle = ReadApplicationLifecycle(ref platform, state, application);
		var current = lifecycle.Iconified != 0;
		if (current == iconified) return true;
		if (iconified)
		{
			if (!CaptureIconifiedWindows(ref platform, state, application) ||
				!platform.SetApplicationIconified(application, true))
			{
				RestoreIconifiedWindows(ref platform, state, application);
				ClearIconifiedWindowMarkers(ref platform, state, application);
				platform.SetApplicationIconified(application, false);
				lifecycle.Iconified = 0;
				WriteApplicationLifecycle(ref platform, state, application, lifecycle);
				return false;
			}
			lifecycle.Iconified = 1;
			if (!WriteApplicationLifecycle(ref platform, state, application,
				lifecycle))
			{
				RestoreIconifiedWindows(ref platform, state, application);
				ClearIconifiedWindowMarkers(ref platform, state, application);
				platform.SetApplicationIconified(application, false);
				lifecycle.Iconified = 0;
				WriteApplicationLifecycle(ref platform, state, application, lifecycle);
				return false;
			}
			return true;
		}

		if (!platform.SetApplicationIconified(application, false))
		{
			platform.SetApplicationIconified(application, true);
			return false;
		}
		lifecycle.Iconified = 0;
		if (!WriteApplicationLifecycle(ref platform, state, application,
			lifecycle))
		{
			platform.SetApplicationIconified(application, true);
			lifecycle.Iconified = 1;
			WriteApplicationLifecycle(ref platform, state, application, lifecycle);
			return false;
		}
		if (RestoreIconifiedWindows(ref platform, state, application)) return true;

		// A failed restore is kept transactional as far as the platform allows:
		// capture any windows that did reopen, return the application to its
		// iconified state, and retain the guest markers for a later retry.
		CaptureIconifiedWindows(ref platform, state, application);
		lifecycle.Iconified = 1;
		WriteApplicationLifecycle(ref platform, state, application, lifecycle);
		platform.SetApplicationIconified(application, true);
		return false;
	}

	// MUIA_Application_Active is a commodities-facing boolean state.  MUI
	// itself does not act on the value; canonicalize the guest write to the
	// MorphOS BOOL representation while keeping the value in named object
	// storage.  No host-side mirror or runtime service is needed.
	public static bool SetApplicationActiveValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var lifecycle = ReadApplicationLifecycle(ref platform, state, application);
		lifecycle.Active = value == 0 ? 0u : 1u;
		return WriteApplicationLifecycle(ref platform, state, application,
			lifecycle);
	}

	// MUIA_Application_SingleTask is an initializer contract. Before an
	// application is initialized, a TRUE write claims the single-task slot; if
	// another initialized single-task application exists, MorphOS reports the
	// attempt through that application's MUIA_Application_DoubleStart state.
	// Once initialized, changing the initializer value is rejected.
	public static bool SetApplicationSingleTaskValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var requested = value == 0 ? 0u : 1u;
		if (Read(ref platform, state, application, ApplicationInitialized) != 0)
			return Read(ref platform, state, application, ApplicationSingleTask) ==
				requested;
		if (requested != 0)
		{
			if (!TryFindSingleTaskApplication(ref platform, state, application,
				out var existing)) return false;
			if (existing.IsNotNull)
			{
				if (!Set(ref platform, state, existing, ApplicationDoubleStart, 1))
					return false;
				return false;
			}
		}
		return Set(ref platform, state, application, ApplicationSingleTask,
			requested);
	}

	// MUIA_Application_DoubleStart is a guest-visible lifecycle flag. MUI
	// sets it on the already-running single-task application; canonicalize
	// direct writes to the MorphOS BOOL representation as well.
	public static bool SetApplicationDoubleStartValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		return Set(ref platform, state, application, ApplicationDoubleStart,
			value == 0 ? 0u : 1u);
	}

	// MUIA_Application_ForceQuit is queried by the application after receiving
	// MUIV_Application_ReturnID_Quit. A TRUE value means the application should
	// exit without safety requesters; the flag itself remains guest-resident and
	// has no host-side exit behavior.
	public static bool SetApplicationForceQuitValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		return Set(ref platform, state, application, ApplicationForceQuit,
			value == 0 ? 0u : 1u);
	}

	// MUIA_Application_UseRexx is an initializer-only policy. MorphOS enables
	// the ARexx interface by default; a FALSE initializer disables that
	// interface. The current core records the policy in guest state and leaves
	// the eventual ARexx transport to its own platform service.
	public static bool SetApplicationUseRexxValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var requested = value == 0 ? 0u : 1u;
		if (Read(ref platform, state, application, ApplicationInitialized) != 0)
			return false;
		if (!Set(ref platform, state, application, ApplicationUseRexx, requested))
			return false;
		return PublishApplicationPolicyState(ref platform, state, application,
			out _);
	}

	// MUIA_Application_UseCommodities is an initializer-only policy. MorphOS
	// enables the commodities interface by default; a FALSE initializer keeps
	// the application out of that interface. The actual commodities objects
	// remain a separate platform capability, while this BOOL stays named guest
	// state and is immutable after application initialization.
	public static bool SetApplicationUseCommoditiesValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		if (Read(ref platform, state, application, ApplicationInitialized) != 0)
			return false;
		if (!Set(ref platform, state, application, ApplicationUseCommodities,
			value == 0 ? 0u : 1u)) return false;
		return PublishApplicationPolicyState(ref platform, state, application,
			out _);
	}

	// MUIA_Application_UseScreenNotify is an initializer-only BOOL. Keep the
	// conservative MorphOS default (disabled) in named guest state and reject
	// changes after application initialization; the screen-notify transport is
	// a separate platform service boundary.
	public static bool SetApplicationUseScreenNotifyValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		if (Read(ref platform, state, application, ApplicationInitialized) != 0)
			return false;
		if (!Set(ref platform, state, application, ApplicationUseScreenNotify,
			value == 0 ? 0u : 1u)) return false;
		return PublishApplicationPolicyState(ref platform, state, application,
			out _);
	}

	// MUIA_Application_DiskObject is a mutable [ISG] pointer to a public
	// Workbench DiskObject. Retain the caller-owned guest structure and validate
	// the complete fixed ABI record; AppIcon presentation remains a platform
	// capability for the iconification implementation.
	public static bool SetApplicationDiskObjectValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var diskObject = APTR.FromPointer(value);
		if (diskObject.IsNotNull && !platform.IsMapped(diskObject,
			DiskObject.Size)) return false;
		if (!Set(ref platform, state, application, ApplicationDiskObject, value))
			return false;
		return PublishApplicationObjectState(ref platform, state, application,
			out _);
	}

	// MUIA_Application_DropObject is a mutable [IS.] pointer to a live MUI
	// object that receives AppMessages while the application is iconified.
	// Validate object identity at the guest boundary and retain only the
	// caller-owned pointer; message delivery is a separate platform capability.
	public static bool SetApplicationDropObjectValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var dropObject = APTR.FromPointer(value);
		if (dropObject.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
			state, dropObject).IsNull) return false;
		if (!Set(ref platform, state, application, ApplicationDropObject, value))
			return false;
		return PublishApplicationObjectState(ref platform, state, application,
			out _);
	}

	// MUIA_Application_Menustrip is an [I.G] owned relationship. Accept only a
	// live Menustrip.mui object before application initialization, attach it to
	// the application's named family, and retain the caller-owned object pointer
	// in the attribute record. The family retains ownership; no managed menu
	// graph is created.
	public static bool SetApplicationMenustripValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull || value == 0 ||
			Read(ref platform, state, application, ApplicationInitialized) != 0)
			return false;
		var strip = APTR.FromPointer(value);
		if (strip == application || MuiHeadlessObjectCore.FindObject(ref platform,
			state, strip).IsNull || MuiMenuSpecialistCore.Classify(ref platform,
				state, strip) != MuiMenuSpecialistClass.Menustrip ||
			MuiHeadlessObjectCore.ParentObject(ref platform, state, strip).IsNotNull)
			return false;
		if (!MuiFamilyCore.AddTail(ref platform, state, application, strip))
			return false;
		if (Set(ref platform, state, application, ApplicationMenustrip, value) &&
			PublishApplicationObjectState(ref platform, state, application,
				out _)) return true;
		Set(ref platform, state, application, ApplicationMenustrip, 0);
		MuiFamilyCore.Remove(ref platform, state, application, strip);
		return false;
	}

	// MUIA_Application_MenuAction is a mutable [ISG] ULONG event state. The
	// application may initialize it, while the menu transport updates it when a
	// menu item is selected. Keep the value in the named guest attribute store;
	// no managed event shadow is needed.
	public static bool SetApplicationMenuActionValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		if (!Set(ref platform, state, application, ApplicationMenuAction, value))
			return false;
		return PublishApplicationMenuState(ref platform, state, application,
			out _);
	}

	// MUIA_Window_MenuAction is a mutable [ISG] ULONG event state. Menustrip
	// transport may publish selected UserData through this typed helper;
	// ordinary Set/NoNotifySet packets use the same named attribute record.
	public static bool SetWindowMenuActionValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		return Set(ref platform, state, window, MuiWindowPublicCore.MenuAction,
			value);
	}

	// MUIA_Window_MouseObject is getter-only. A future pointer-tracking seam
	// may publish the deepest live object through this helper; callers cannot
	// write it through ordinary SetAttrs-style packets.
	public static bool PublishWindowMouseObjectValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, APTR value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		if (value == window || (value.IsNotNull &&
			MuiHeadlessObjectCore.FindObject(ref platform, state, value).IsNull))
			return false;
		if (!Set(ref platform, state, window, MuiWindowPublicCore.MouseObject,
			value.Raw)) return false;
		return PublishWindowEventState(ref platform, state, window, out _);
	}

	// MUIA_Window_InputEvent is a getter-only pointer to the current standard
	// InputEvent record. The event storage remains caller-owned guest memory;
	// only its validated address is retained in named window state.
	public static bool PublishWindowInputEventValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, APTR eventStorage)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		if (eventStorage.IsNotNull && !platform.IsMapped(eventStorage,
			global::Amiga.InputEvent.Size)) return false;
		if (!Set(ref platform, state, window, MuiWindowPublicCore.InputEvent,
			eventStorage.Raw)) return false;
		return PublishWindowEventState(ref platform, state, window, out _);
	}

	// MenuHelp is a getter-only [..G] event attribute. The menu/input transport
	// publishes its UserData through this typed seam; callers cannot write the
	// attribute through ordinary SetAttrs-style packets.
	public static bool PublishApplicationMenuHelpValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		if (!Set(ref platform, state, application, ApplicationMenuHelp, value))
			return false;
		return PublishApplicationMenuState(ref platform, state, application,
			out _);
	}

	// Publish a selected Menuitem's UserData to the owning application. The
	// parent chain is the guest-resident menu/window/application hierarchy; it
	// is walked with a fixed bound and never mirrored into managed objects.
	public static bool PublishApplicationMenuItemSelection<TPlatform>(
		ref TPlatform platform, APTR state, APTR menuItem, bool help)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, menuItem).IsNull)
			return false;
		var current = menuItem;
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal; index++)
		{
			var parent = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
			if (parent.IsNull) return false;
			if (Read(ref platform, state, parent, ApplicationInitialized) != 0)
			{
				MuiHeadlessObjectCore.GetAttribute(ref platform, state, menuItem,
					ObjectUserData, out var userData);
				return help ? PublishApplicationMenuHelpValue(ref platform, state,
					parent, userData) : SetApplicationMenuActionValue(ref platform,
					state, parent, userData);
			}
			current = parent;
		}
		return false;
	}

	// MorphOS application identity strings are [I.G] initializer attributes.
	// Keep the caller-owned guest pointer in the named object attribute record;
	// do not copy it into managed storage. A non-null pointer must identify a
	// bounded guest C string, while NULL remains a valid way to omit metadata.
	public static bool SetApplicationInitializerStringValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint attribute,
		uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		if (Read(ref platform, state, application, ApplicationInitialized) != 0)
			return false;
		var text = APTR.FromPointer(value);
		if (text.IsNotNull && !CStringCodec.TryReadLength(ref platform, text,
			65536, out _)) return false;
		if (attribute != ApplicationAuthor && attribute != ApplicationBase &&
			attribute != ApplicationCopyright && attribute != ApplicationDescription &&
			attribute != ApplicationTitle && attribute != ApplicationVersion)
			return false;
		if (!Set(ref platform, state, application, attribute, value)) return false;
		return PublishApplicationIdentityState(ref platform, state, application,
			out _);
	}

	// MUIA_Application_HelpFile is a mutable [ISG] guest C-string pointer.
	// Keep ownership with the caller and validate only the guest address/string
	// boundary; the presentation platform consumes the pointer when ShowHelp
	// is dispatched.
	public static bool SetApplicationHelpFileValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var helpFile = APTR.FromPointer(value);
		if (helpFile.IsNotNull && !CStringCodec.TryReadLength(ref platform,
			helpFile, 65536, out _)) return false;
		if (!Set(ref platform, state, application, ApplicationHelpFile, value))
			return false;
		return PublishApplicationTextState(ref platform, state, application,
			out _);
	}

	// MUIA_Application_IconifyTitle is a mutable [ISG] guest C-string pointer.
	// Keep the caller-owned pointer in named object state and validate its
	// bounded guest string without copying it into managed storage.
	public static bool SetApplicationIconifyTitleValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var title = APTR.FromPointer(value);
		if (title.IsNotNull && !CStringCodec.TryReadLength(ref platform, title,
			65536, out _)) return false;
		if (!Set(ref platform, state, application, ApplicationIconifyTitle, value))
			return false;
		return PublishApplicationTextState(ref platform, state, application,
			out _);
	}

	// MUIA_Application_Window is an initializer-only relationship tag. Each
	// occurrence adds one live MUI object to the application's named family;
	// ownership and application-sleep inheritance are delegated to AddWindow.
	public static bool SetApplicationWindowValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull || value == 0 ||
			Read(ref platform, state, application, ApplicationInitialized) != 0)
			return false;
		var window = APTR.FromPointer(value);
		if (window == application || MuiHeadlessObjectCore.FindObject(ref platform,
			state, window).IsNull) return false;
		if (!AddWindow(ref platform, state, application, window)) return false;
		if (Set(ref platform, state, application, ApplicationWindow, value))
		{
			var relationship = ReadApplicationWindowRelationshipState(ref platform,
				state, application);
			relationship.AddedCount = relationship.AddedCount == uint.MaxValue
				? uint.MaxValue : relationship.AddedCount + 1;
			if (WriteApplicationWindowRelationshipState(ref platform, state,
				application, relationship)) return true;
		}
		Set(ref platform, state, application, ApplicationWindow, 0);
		RemoveWindow(ref platform, state, application, window);
		return false;
	}

	// MUIA_Application_UsedClasses is a mutable [ISG] pointer to a guest
	// NULL-terminated STRPTR vector. Validate the complete bounded vector but
	// retain only the caller-owned pointer in the existing named attribute state.
	public static bool SetApplicationUsedClassesValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			application).IsNull) return false;
		var vector = APTR.FromPointer(value);
		if (!MuiApplicationUsedClassesVectorCodec.TryValidate(ref platform, vector))
			return false;
		if (!Set(ref platform, state, application, ApplicationUsedClasses, value))
			return false;
		return PublishApplicationUsedClassesState(ref platform, state, application,
			out _);
	}

	// MUIM_Application_AboutMUI. MorphOS defines refwindow as a MUI Window
	// object, not an Intuition struct Window. Keep the boundary honest: the
	// application must be live, a non-null reference must be a live MUI object,
	// and the platform presentation seam must accept the request. The last
	// reference and request count are guest-resident state, making the accepted
	// operation observable without a managed object or exception path.
	public static bool AboutMUI<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR refWindow)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		if (refWindow.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
			state, refWindow).IsNull) return false;
		if (!platform.ShowMuiAbout(application, refWindow)) return false;
		if (!Set(ref platform, state, application, ApplicationAboutRefWindow,
			refWindow.Raw)) return false;
		var helpState = ReadApplicationHelpState(ref platform, state, application);
		if (!Set(ref platform, state, application, ApplicationAboutRequests,
			helpState.AboutRequests == uint.MaxValue ? uint.MaxValue :
			helpState.AboutRequests + 1)) return false;
		return PublishApplicationHelpState(ref platform, state, application,
			out _);
	}

	// MUIM_Application_ShowHelp. MorphOS accepts a MUI Window object, Null for
	// the default public screen, or (Object *)-1 to select the first open child
	// window. Strings remain caller-owned guest pointers and are only validated
	// as bounded C strings; the platform owns the actual help presentation.
	public static bool ShowHelp<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR window, APTR name, APTR node, int line)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		var textState = ReadApplicationTextState(ref platform, state, application);
		var helpFile = name;
		if (helpFile.IsNull)
			helpFile = textState.HelpFile;
		if (helpFile.IsNotNull && !CStringCodec.TryReadLength(ref platform, helpFile,
			65536, out _)) return false;
		if (node.IsNotNull && !CStringCodec.TryReadLength(ref platform, node,
			65536, out _)) return false;

		var reference = window;
		if (window.Raw == HelpFirstOpenWindow)
		{
			reference = APTR.Null;
			for (var index = 0; index < 65535; index++)
			{
				var child = MuiFamilyCore.GetChild(ref platform, state, application,
					index, APTR.Null);
				if (child.IsNull) break;
				if (ReadWindowLifecycle(ref platform, state, child).NativeWindow.IsNotNull)
				{
					reference = child;
					break;
				}
			}
		}
		else if (window.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
			state, window).IsNull) return false;

		if (!platform.ShowMuiHelp(application, reference, helpFile, node, line))
			return false;
		if (!Set(ref platform, state, application, ApplicationHelpWindow,
			reference.Raw) || !Set(ref platform, state, application,
			ApplicationHelpName, helpFile.Raw) || !Set(ref platform, state, application,
			ApplicationHelpNode, node.Raw) || !Set(ref platform, state, application,
			ApplicationHelpLine, unchecked((uint)line))) return false;
		var helpState = ReadApplicationHelpState(ref platform, state, application);
		if (!Set(ref platform, state, application, ApplicationHelpRequests,
			helpState.HelpRequests == uint.MaxValue ? uint.MaxValue :
			helpState.HelpRequests + 1)) return false;
		return PublishApplicationHelpState(ref platform, state, application,
			out _);
	}

	// MUIM_Application_DefaultConfigItem. This is an application override hook,
	// not a config-file parser: the explicit platform seam supplies the value
	// for the requested item and the accepted result is retained in guest state.
	public static uint DefaultConfigItem<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, uint configId)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return 0;
		if (!platform.GetApplicationDefaultConfigItem(application, configId,
			out var value)) return 0;
		if (!Set(ref platform, state, application, ApplicationDefaultConfigId,
			configId) || !Set(ref platform, state, application,
			ApplicationDefaultConfigValue, value)) return 0;
		var configState = ReadApplicationDefaultConfigState(ref platform, state,
			application);
		if (!Set(ref platform, state, application,
			ApplicationDefaultConfigRequests,
			configState.Requests == uint.MaxValue ? uint.MaxValue :
			configState.Requests + 1)) return 0;
		if (!PublishApplicationDefaultConfigState(ref platform, state,
			application, out _)) return 0;
		return value;
	}

	// MUIM_Application_SetConfigItem. MorphOS documents this V11 method as a
	// private PSI boundary. The data pointer is intentionally opaque here: no
	// preferences format is assumed or copied. The exact item/data pair and a
	// saturating request count are retained in a named guest-resident record so
	// callers can observe the accepted ABI without introducing a managed store.
	public static bool SetConfigItem<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, uint item, APTR data)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		// The payload is opaque and is never dereferenced, but a non-null caller
		// pointer must still name at least one mapped guest byte.
		if (data.IsNotNull && !platform.IsMapped(data, 1)) return false;
		var block = EnsureSetConfigItemState(ref platform, state, application);
		if (block.IsNull || !TryReadSetConfigItemState(ref platform, block,
			out var value)) return false;
		value.Item = item;
		value.Data = data;
		value.Requests = value.Requests == uint.MaxValue
			? uint.MaxValue : value.Requests + 1;
		return WriteSetConfigItemState(ref platform, block, value);
	}

	// Readback helper used by host tests and the focused native seam. This is
	// not an additional public MUI vector; it exposes only the bounded state needed to
	// qualify the private SetConfigItem boundary.
	public static bool ReadSetConfigItemState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, out uint item, out uint data,
		out uint requests) where TPlatform : struct, IMuiHeadlessPlatform
	{
		item = 0;
		data = 0;
		requests = 0;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		var block = APTR.FromPointer(MuiHeadlessObjectCore.GetAttribute(ref platform,
			state, application, ApplicationSetConfigItemState, out var raw)
			? raw : 0);
		if (!TryReadSetConfigItemState(ref platform, block, out var value))
			return false;
		item = value.Item;
		data = value.Data.Raw;
		requests = value.Requests;
		return true;
	}

	// Struct-first native qualification seam for the exact guest record used by
	// SetConfigItem. The public MUI method above remains the owner of object
	// validation and request counting.
	public static bool WriteSetConfigItemRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint item, uint data, uint requests)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiApplicationSetConfigItemStateRecord.Size)) return false;
		var value = default(MuiApplicationSetConfigItemStateRecord);
		value.Magic = MuiApplicationSetConfigItemStateRecord.Cookie;
		value.Item = item;
		value.Data = APTR.FromPointer(data);
		value.Requests = requests;
		return WriteSetConfigItemState(ref platform, storage, value);
	}

	private static APTR EnsureSetConfigItemState<TPlatform>(ref TPlatform platform,
		APTR state, APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, application,
			ApplicationSetConfigItemState));
		if (TryReadSetConfigItemState(ref platform, block, out _)) return block;
		block = MuiHeadlessMemory.Allocate(ref platform,
		MuiApplicationSetConfigItemStateRecord.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiApplicationSetConfigItemStateRecord);
		value.Magic = MuiApplicationSetConfigItemStateRecord.Cookie;
		if (!WriteSetConfigItemState(ref platform, block, value))
		{
			platform.Clear(block, MuiApplicationSetConfigItemStateRecord.Size);
			platform.Free(block, MuiApplicationSetConfigItemStateRecord.Size);
			return APTR.Null;
		}
		if (Set(ref platform, state, application,
			ApplicationSetConfigItemState, block.Raw)) return block;
		platform.Clear(block, MuiApplicationSetConfigItemStateRecord.Size);
		platform.Free(block, MuiApplicationSetConfigItemStateRecord.Size);
		return APTR.Null;
	}

	private static bool WriteSetConfigItemState<TPlatform>(ref TPlatform platform,
		APTR block, MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationSetConfigItemStateRecordCodec.Write(ref platform, block,
			value);

	private static bool TryReadSetConfigItemState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationSetConfigItemStateRecordCodec.TryRead(ref platform, block,
			out value);

	// MUIM_Application_OpenConfigWindow. MorphOS describes this as a
	// non-blocking request to show the application's MUI preferences window.
	// No flags are currently defined, so the raw flags word is retained but not
	// interpreted. The optional class id remains caller-owned guest memory and
	// is only bounded-validated before crossing the platform capability seam.
	public static bool OpenConfigWindow<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, uint flags, APTR classId)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		if (classId.IsNotNull && !CStringCodec.TryReadLength(ref platform, classId,
			65536, out _)) return false;
		if (!platform.OpenMuiConfigWindow(application, flags, classId)) return false;
		if (!Set(ref platform, state, application, ApplicationConfigWindowFlags,
			flags) || !Set(ref platform, state, application,
			ApplicationConfigWindowClassId, classId.Raw)) return false;
		var configState = ReadApplicationConfigWindowState(ref platform, state,
			application);
		if (!Set(ref platform, state, application,
			ApplicationConfigWindowRequests,
			configState.Requests == uint.MaxValue ? uint.MaxValue :
			configState.Requests + 1)) return false;
		return PublishApplicationConfigWindowState(ref platform, state,
			application, out _);
	}

	// MUIM_Application_BuildSettingsPanel. This is an application override
	// hook: the platform may return a live MUI object for the requested panel
	// number or Null when that panel is not provided. The core validates a
	// non-null result before exposing it to the guest and records only bounded
	// guest-resident telemetry.
	public static APTR BuildSettingsPanel<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, uint number)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return APTR.Null;
		var panel = platform.BuildMuiSettingsPanel(application, number);
		if (panel.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
			state, panel).IsNull) return APTR.Null;
		if (!Set(ref platform, state, application, ApplicationSettingsPanelNumber,
			number) || !Set(ref platform, state, application,
			ApplicationSettingsPanelObject, panel.Raw)) return APTR.Null;
		var panelState = ReadApplicationSettingsPanelState(ref platform, state,
			application);
		if (!Set(ref platform, state, application,
			ApplicationSettingsPanelRequests,
			panelState.Requests == uint.MaxValue ? uint.MaxValue :
			panelState.Requests + 1))
			return APTR.Null;
		if (!PublishApplicationSettingsPanelState(ref platform, state, application,
			out _)) return APTR.Null;
		return panel;
	}

	// MUIM_Application_Save and MUIM_Application_Load. MorphOS accepts the
	// special Null/((STRPTR)-1) ENV and ENVARC selectors as well as a caller-
	// owned C-string path. The object graph's actual import/export remains a
	// platform capability; this core validates the ABI boundary and records the
	// last operation in guest state without a managed persistence store.
	public static bool SaveApplicationSettings<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR name)
		where TPlatform : struct, IMuiApplicationPlatform =>
		PersistApplicationSettings(ref platform, state, application, name, true);

	public static bool LoadApplicationSettings<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR name)
		where TPlatform : struct, IMuiApplicationPlatform =>
		PersistApplicationSettings(ref platform, state, application, name, false);

	private static bool PersistApplicationSettings<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR name, bool save)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		if (name.Raw != uint.MaxValue && name.IsNotNull &&
			!CStringCodec.TryReadLength(ref platform, name, 65536, out _)) return false;
		var accepted = save ? platform.SaveMuiApplicationSettings(state,
			application, name) : platform.LoadMuiApplicationSettings(state,
			application, name);
		if (!accepted) return false;
		if (!Set(ref platform, state, application, ApplicationSettingsOperation,
			save ? 1u : 0u) || !Set(ref platform, state, application,
			ApplicationSettingsName, name.Raw)) return false;
		var settingsState = ReadApplicationSettingsPersistenceState(ref platform,
			state, application);
		if (!Set(ref platform, state, application, ApplicationSettingsRequests,
			settingsState.Requests == uint.MaxValue ? uint.MaxValue :
			settingsState.Requests + 1)) return false;
		var counterAttribute = save ? ApplicationSettingsSaves :
			ApplicationSettingsLoads;
		var count = save ? settingsState.Saves : settingsState.Loads;
		if (!Set(ref platform, state, application, counterAttribute,
			count == uint.MaxValue ? uint.MaxValue : count + 1)) return false;
		return PublishApplicationSettingsPersistenceState(ref platform, state,
			application, out _);
	}

	// MUIM_Application_CheckRefresh. MUI uses this boundary when a synchronous
	// requester may have consumed IDCMP_REFRESHWINDOW messages. Walk the
	// application-owned child windows, select only windows with a live native
	// window handle, and ask the platform to refresh each one. The check and
	// refreshed-window counts are guest-resident telemetry; the public method's
	// result is intentionally only an accepted/rejected boundary indicator.
	public static bool CheckRefresh<TPlatform>(ref TPlatform platform, APTR state,
		APTR application) where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		uint refreshed = 0;
		for (var index = 0; index < 65535; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				index, APTR.Null);
			if (window.IsNull) break;
			if (ReadWindowLifecycle(ref platform, state, window).NativeWindow.IsNull)
				continue;
			if (platform.RefreshMuiWindow(window)) refreshed++;
		}
		var refreshState = ReadApplicationRefreshState(ref platform, state,
			application);
		if (!Set(ref platform, state, application, ApplicationRefreshChecks,
			refreshState.Checks == uint.MaxValue ? uint.MaxValue :
				refreshState.Checks + 1) ||
			!Set(ref platform, state, application, ApplicationRefreshWindows,
				refreshed)) return false;
		return PublishApplicationRefreshState(ref platform, state, application,
			out _);
	}

	// Application-level menu compatibility. MorphOS asks subwindows in order
	// and returns the first menu item found for GetMenu; SetMenu updates every
	// matching item. The platform capability owns the native menu lookup/update,
	// while this core supplies the application child-window traversal and skips
	// windows that are not currently open.
	public static uint GetApplicationMenu<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, uint menuId, bool check)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return 0;
		for (var index = 0; index < 65535; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				index, APTR.Null);
			if (window.IsNull) break;
			var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
				.NativeWindow;
			if (nativeWindow.IsNull) continue;
			if (platform.GetMuiMenuState(nativeWindow, menuId, check,
				out var value)) return value ? 1u : 0u;
		}
		return 0;
	}

	public static uint SetApplicationMenu<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, uint menuId, bool enabled, bool check,
		bool checkedState) where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return 0;
		uint updated = 0;
		for (var index = 0; index < 65535; index++)
		{
			var window = MuiFamilyCore.GetChild(ref platform, state, application,
				index, APTR.Null);
			if (window.IsNull) break;
			var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
				.NativeWindow;
			if (nativeWindow.IsNull) continue;
			if (platform.SetMuiMenuState(nativeWindow, menuId, enabled, check,
				checkedState)) updated++;
		}
		return updated;
	}

	public static bool Requester<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, APTR window, APTR requester, bool open)
		where TPlatform : struct, IMuiApplicationPlatform =>
		platform.CoordinateRequester(application, window, requester, open);

	private static bool AddHandler<TPlatform>(ref TPlatform platform, APTR state,
		APTR owner, uint listAttribute, APTR handler, uint handlerSize)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (handler.IsNull || !platform.IsMapped(handler, handlerSize)) return false;
		var eventHandler = default(MuiEventHandlerNodeRecord);
		if (listAttribute == EventHandlers &&
			!MuiEventHandlerNodeCodec.TryRead(ref platform, handler,
				out eventHandler)) return false;
		var node = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationWindowNodeRecord.Size);
		if (node.IsNull) return false;
		var record = default(MuiApplicationWindowNodeRecord);
		record.Value = handler;
		record.Sequence = MuiHeadlessMemory.NextSequence(ref platform, state);
		if (listAttribute == InputHandlers)
		{
			record.Auxiliary = platform.ReadTicks();
			if (!MuiInputHandlerCodec.TryRead(ref platform, handler,
				out var inputHandler))
			{
				platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
				platform.Free(node, MuiApplicationWindowNodeRecord.Size);
				return false;
			}
			record.Packet = inputHandler.Packet;
		}
		if (listAttribute == EventHandlers)
		{
			// MorphOS exposes ISENABLED as read-only state. Set it on the
			// caller-owned named node only for the duration of a successful
			// registration, restoring the original flags if queue insertion is
			// rejected.
			var enabledHandler = eventHandler;
			enabledHandler.Flags = (ushort)(enabledHandler.Flags |
				EventHandlerEnabled);
			if (!MuiEventHandlerNodeCodec.Write(ref platform, handler,
				enabledHandler))
			{
				platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
				platform.Free(node, MuiApplicationWindowNodeRecord.Size);
				return false;
			}
			var inserted = InsertEventHandler(ref platform, state, owner, handler,
				node, enabledHandler, record.Sequence);
			if (inserted) return true;
			MuiEventHandlerNodeCodec.Write(ref platform, handler, eventHandler);
			return false;
		}
		var listHead = listAttribute == InputHandlers ?
			ReadApplicationSchedulerState(ref platform, state, owner).InputHandlers :
			APTR.FromPointer(Read(ref platform, state, owner, listAttribute));
		record.Next = listHead;
		if (!MuiApplicationWindowNodeCodec.Write(ref platform, node, record))
		{
			platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
			platform.Free(node, MuiApplicationWindowNodeRecord.Size);
			return false;
		}
		var added = Set(ref platform, state, owner, listAttribute, node.Raw);
		if (added && listAttribute == InputHandlers)
			PublishApplicationSchedulerState(ref platform, state, owner, out _);
		return added;
	}

	private static bool InsertEventHandler<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, APTR handler, APTR node,
		MuiEventHandlerNodeRecord eventHandler, uint sequence)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = APTR.FromPointer(Read(ref platform, state, owner,
			EventHandlers));
		var previous = APTR.Null;
		var priority = eventHandler.Priority;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, current,
				out var currentNode) ||
				!MuiEventHandlerNodeCodec.TryRead(ref platform, currentNode.Value,
					out var currentHandler))
			{
				platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
				platform.Free(node, MuiApplicationWindowNodeRecord.Size);
				return false;
			}
			var priorityHandler = (eventHandler.Flags & EventHandlerPriority) != 0;
			var currentPriorityHandler = (currentHandler.Flags &
				EventHandlerPriority) != 0;
			// Absolute-focus handlers form a leading priority partition. Within
			// each partition, higher signed BYTE priorities run first and equal
			// priorities stay FIFO.
			if (priorityHandler != currentPriorityHandler)
			{
				if (priorityHandler) break;
			}
			else if (currentHandler.Priority < priority) break;
			previous = current;
			current = currentNode.Next;
		}
		var record = default(MuiApplicationWindowNodeRecord);
		record.Value = handler;
		record.Sequence = sequence;
		record.Auxiliary = (eventHandler.Flags & EventHandlerPriority) != 0 ? 1u : 0u;
		record.Next = current;
		if (!MuiApplicationWindowNodeCodec.Write(ref platform, node, record))
		{
			platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
			platform.Free(node, MuiApplicationWindowNodeRecord.Size);
			return false;
		}
		if (previous.IsNull)
		{
			if (Set(ref platform, state, owner, EventHandlers, node.Raw))
				return true;
			platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
			platform.Free(node, MuiApplicationWindowNodeRecord.Size);
			return false;
		}
		if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, previous,
			out var previousRecord))
		{
			platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
			platform.Free(node, MuiApplicationWindowNodeRecord.Size);
			return false;
		}
		previousRecord.Next = node;
		if (MuiApplicationWindowNodeCodec.Write(ref platform, previous,
			previousRecord)) return true;
		platform.Clear(node, MuiApplicationWindowNodeRecord.Size);
		platform.Free(node, MuiApplicationWindowNodeRecord.Size);
		return false;
	}

	internal static void CleanupRecords<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var scheduler = ReadApplicationSchedulerState(ref platform, state, obj);
		var interaction = ReadWindowInteractionState(ref platform, state, obj);
		FreeNodes(ref platform, scheduler.ReturnHead);
		FreeNodes(ref platform, scheduler.InputHandlers);
		FreeEventHandlerNodes(ref platform, APTR.FromPointer(Read(ref platform,
			state, obj, EventHandlers)));
		FreeNodes(ref platform, interaction.CycleChainHead);
		FreePushNodes(ref platform, scheduler.PushHead);
		var configState = APTR.FromPointer(Read(ref platform, state, obj,
			ApplicationSetConfigItemState));
		if (TryReadSetConfigItemState(ref platform, configState, out _))
		{
			platform.Clear(configState, MuiApplicationSetConfigItemStateRecord.Size);
			platform.Free(configState, MuiApplicationSetConfigItemStateRecord.Size);
			Set(ref platform, state, obj, ApplicationSetConfigItemState, 0);
		}
	}

	private static void FreeNodes<TPlatform>(ref TPlatform platform, APTR current)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var item = current;
		uint visited = 0;
		while (item.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, item,
				out var record)) return;
			var next = record.Next;
			platform.Clear(item, MuiApplicationWindowNodeRecord.Size);
			platform.Free(item, MuiApplicationWindowNodeRecord.Size);
			item = next;
		}
	}

	private static void FreeEventHandlerNodes<TPlatform>(ref TPlatform platform,
		APTR current) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var item = current;
		uint visited = 0;
		while (item.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, item,
				out var record)) return;
			var next = record.Next;
			SetEventHandlerState(ref platform, record.Value, false, false);
			platform.Clear(item, MuiApplicationWindowNodeRecord.Size);
			platform.Free(item, MuiApplicationWindowNodeRecord.Size);
			item = next;
		}
	}

	private static void FreePushNodes<TPlatform>(ref TPlatform platform,
		APTR current) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var item = current;
		uint visited = 0;
		while (item.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, item,
				out var record)) return;
			var next = record.Next;
			var size = MuiApplicationWindowNodeRecord.Size + record.Auxiliary * 4u;
			if (!platform.IsMapped(item, size)) return;
			platform.Clear(item, size);
			platform.Free(item, size);
			item = next;
		}
	}

	private static bool RemoveHandler<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, uint listAttribute, APTR handler)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = listAttribute == InputHandlers ?
			ReadApplicationSchedulerState(ref platform, state, owner).InputHandlers :
			APTR.FromPointer(Read(ref platform, state, owner, listAttribute));
		var previous = APTR.Null;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, current,
				out var record)) return false;
			var next = record.Next;
			if (record.Value == handler)
			{
				if (previous.IsNull) Set(ref platform, state, owner, listAttribute,
					next.Raw);
				else
				{
					if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, previous,
						out var previousRecord)) return false;
					previousRecord.Next = next;
					MuiApplicationWindowNodeCodec.Write(ref platform, previous,
						previousRecord);
				}
				platform.Clear(current, MuiApplicationWindowNodeRecord.Size);
				platform.Free(current, MuiApplicationWindowNodeRecord.Size);
				if (listAttribute == InputHandlers)
					PublishApplicationSchedulerState(ref platform, state, owner,
						out _);
				return true;
			}
			previous = current;
			current = next;
		}
		return false;
	}

	private static bool TryReadEventHandler<TPlatform>(ref TPlatform platform,
		APTR handler, out MuiEventHandlerNodeRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiEventHandlerNodeCodec.TryRead(ref platform, handler, out record);

	private static bool SetEventHandlerState<TPlatform>(ref TPlatform platform,
		APTR handler, bool enabled, bool active)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiEventHandlerNodeCodec.TryRead(ref platform, handler,
			out var record)) return false;
		record.Flags = enabled ?
			(ushort)(record.Flags | EventHandlerEnabled) :
			(ushort)(record.Flags & ~EventHandlerEnabled);
		record.Flags = active ?
			(ushort)(record.Flags | EventHandlerActive) :
			(ushort)(record.Flags & ~EventHandlerActive);
		return MuiEventHandlerNodeCodec.Write(ref platform, handler, record);
	}

	private static bool RefreshEventHandlerActiveFlag<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, APTR handler)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiEventHandlerNodeCodec.TryRead(ref platform, handler,
			out var record)) return false;
		var active = APTR.FromPointer(Read(ref platform, state, window,
			ActiveObject));
		var defaultObject = APTR.FromPointer(Read(ref platform, state, window,
			DefaultObject));
		var isActive = record.Object.IsNotNull &&
			(record.Object == active || record.Object == defaultObject);
		if (!isActive && record.Object.IsNotNull &&
			(record.Flags & MuiEventHandlerNodeInput.MUI_EHF_ISACTIVEGRP) != 0)
		{
			// ISACTIVEGRP is a relationship state: a handler attached to a
			// group is active while the active or default object is somewhere
			// below that group. Walk the named Parent links with the same bound
			// used by all guest topology traversals; no managed object graph is
			// created and malformed cycles fail closed.
			isActive = IsObjectInParentChain(ref platform, state, active,
				record.Object) || IsObjectInParentChain(ref platform, state,
				defaultObject, record.Object);
		}
		record.Flags = isActive ?
			(ushort)(record.Flags | EventHandlerActive) :
			(ushort)(record.Flags & ~EventHandlerActive);
		return MuiEventHandlerNodeCodec.Write(ref platform, handler, record);
	}

	private static bool IsObjectInParentChain<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR ancestor)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (obj.IsNull || ancestor.IsNull) return false;
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (current == ancestor) return true;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return false;
	}

	private static bool RefreshEventHandlerActiveFlags<TPlatform>(
		ref TPlatform platform, APTR state, APTR window)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = APTR.FromPointer(Read(ref platform, state, window,
			EventHandlers));
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationWindowNodeCodec.TryRead(ref platform, current,
				out var node)) return false;
			if (!RefreshEventHandlerActiveFlag(ref platform, state, window,
				node.Value)) return false;
			current = node.Next;
		}
		return current.IsNull;
	}

	private static bool Set<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, false);

	// MorphOS exposes the three border-scroller attributes through ordinary
	// Set/NoNotifySet methods. Keep their values in the named object record and,
	// when a native window already exists, apply the complete policy atomically
	// through the typed platform seam. A failed native update restores the old
	// guest value without introducing a managed shadow copy.
	internal static bool SetBorderScroller<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (attribute != MuiWindowPublicCore.UseBottomBorderScroller &&
			attribute != MuiWindowPublicCore.UseLeftBorderScroller &&
			attribute != MuiWindowPublicCore.UseRightBorderScroller)
			return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, window);
		if (record.IsNull) return false;
		var oldValue = Read(ref platform, state, window, attribute);
		if (!MuiHeadlessObjectCore.SetRecordAttribute(ref platform, state, record,
			attribute, value, notify)) return false;
		var nativeWindow = ReadWindowLifecycle(ref platform, state, window)
			.NativeWindow;
		if (nativeWindow.IsNull) return true;
		if (platform.SetMuiWindowBorderScrollers(nativeWindow,
			Read(ref platform, state, window,
				MuiWindowPublicCore.UseBottomBorderScroller) != 0,
			Read(ref platform, state, window,
				MuiWindowPublicCore.UseLeftBorderScroller) != 0,
			Read(ref platform, state, window,
				MuiWindowPublicCore.UseRightBorderScroller) != 0)) return true;
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			attribute, oldValue, false);
		return false;
	}

	private static bool ChangeIDCMP<TPlatform>(ref TPlatform platform, APTR state,
		APTR window, uint flags, bool request)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull)
			return false;
		var lifecycle = ReadWindowLifecycle(ref platform, state, window);
		var oldMask = lifecycle.EventMask;
		var nextMask = request ? oldMask | flags : oldMask & ~flags;
		var nativeWindow = lifecycle.NativeWindow;
		if (nativeWindow.IsNotNull &&
			!platform.ConfigureWindowEvents(nativeWindow, nextMask)) return false;
		lifecycle.EventMask = nextMask;
		if (WriteWindowLifecycle(ref platform, state, window, lifecycle)) return true;
		if (nativeWindow.IsNotNull)
			platform.ConfigureWindowEvents(nativeWindow, oldMask);
		return false;
	}

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute) where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint value;
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out value);
		return value;
	}
}
