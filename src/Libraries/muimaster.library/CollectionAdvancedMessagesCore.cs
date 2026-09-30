/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.CompilerServices;

namespace CopperOS.MuiMaster;

// Central codec for the fixed MorphOS 3.20 List advanced packet family. The
// collection core consumes named position/pair/image fields; only this adapter
// owns packed guest-memory offsets and method validation.
internal enum MuiCollectionAdvancedPacketKind : byte
{
	Method,
	InsertSingle,
	Insert,
	Position,
	Redraw,
	Pointer,
	Pair,
	CreateImage,
}

internal enum MuiCollectionAdvancedField : byte
{
	MethodId,
	Entry,
	Entries,
	Count,
	Position,
	Column,
	Pointer,
	First,
	Second,
	Image,
	Flags,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionAdvancedFieldCursor
{
	internal APTR Message;
	internal MuiCollectionAdvancedPacketKind Packet;
	internal MuiCollectionAdvancedField Field;
}

// Named packet adapters keep the MorphOS List advanced records as semantic
// structs. Only this memory layer translates their guest field boundaries.
internal static class MuiCollectionAdvancedMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiCollectionAdvancedPacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiCollectionAdvancedPacketKind.Method:
				size = MuiCollectionMethodMessage.Size;
				return true;
			case MuiCollectionAdvancedPacketKind.InsertSingle:
				size = MuiCollectionInsertSingleMessage.Size;
				return true;
			case MuiCollectionAdvancedPacketKind.Insert:
				size = MuiCollectionInsertMessage.Size;
				return true;
			case MuiCollectionAdvancedPacketKind.Position:
				size = MuiCollectionPositionMessage.Size;
				return true;
			case MuiCollectionAdvancedPacketKind.Redraw:
				size = MuiCollectionRedrawMessage.Size;
				return true;
			case MuiCollectionAdvancedPacketKind.Pointer:
				size = MuiCollectionPointerMessage.Size;
				return true;
			case MuiCollectionAdvancedPacketKind.Pair:
				size = MuiCollectionPairMessage.Size;
				return true;
			case MuiCollectionAdvancedPacketKind.CreateImage:
				size = MuiCollectionCreateImageMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint fieldIndex;
		switch (packet)
		{
			case MuiCollectionAdvancedPacketKind.Method:
				if (field != MuiCollectionAdvancedField.MethodId) return false;
				fieldIndex = 0;
				break;
			case MuiCollectionAdvancedPacketKind.InsertSingle:
				fieldIndex = field switch
				{
					MuiCollectionAdvancedField.MethodId => 0,
					MuiCollectionAdvancedField.Entry => 1,
					MuiCollectionAdvancedField.Position => 2,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionAdvancedPacketKind.Insert:
				fieldIndex = field switch
				{
					MuiCollectionAdvancedField.MethodId => 0,
					MuiCollectionAdvancedField.Entries => 1,
					MuiCollectionAdvancedField.Count => 2,
					MuiCollectionAdvancedField.Position => 3,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionAdvancedPacketKind.Position:
				fieldIndex = field switch
				{
					MuiCollectionAdvancedField.MethodId => 0,
					MuiCollectionAdvancedField.Position => 1,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionAdvancedPacketKind.Redraw:
				fieldIndex = field switch
				{
					MuiCollectionAdvancedField.MethodId => 0,
					MuiCollectionAdvancedField.Position => 1,
					MuiCollectionAdvancedField.Entry => 2,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionAdvancedPacketKind.Pointer:
				fieldIndex = field switch
				{
					MuiCollectionAdvancedField.MethodId => 0,
					MuiCollectionAdvancedField.Pointer => 1,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionAdvancedPacketKind.Pair:
				fieldIndex = field switch
				{
					MuiCollectionAdvancedField.MethodId => 0,
					MuiCollectionAdvancedField.First => 1,
					MuiCollectionAdvancedField.Second => 2,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionAdvancedPacketKind.CreateImage:
				fieldIndex = field switch
				{
					MuiCollectionAdvancedField.MethodId => 0,
					MuiCollectionAdvancedField.Image => 1,
					MuiCollectionAdvancedField.Flags => 2,
					_ => uint.MaxValue,
				};
				break;
			default:
				return false;
		}
		if (fieldIndex == uint.MaxValue) return false;
		if (fieldIndex > 0 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCollectionMethodMessage.FieldSize, out _)) return false;
		if (fieldIndex > 1 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCollectionMethodMessage.FieldSize, out _)) return false;
		if (fieldIndex > 2 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCollectionMethodMessage.FieldSize, out _)) return false;
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiCollectionMethodMessage.FieldSize, out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionAdvancedFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor.Message, cursor.Packet,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryGetPacketSize(packet, out var packetSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, packetSize,
				out var guestCursor) ||
			!TryTakeField(ref platform, ref guestCursor, packet, field,
				out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiCollectionAdvancedPacketKind.Method)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadMethod(ref platform,
				message, out var method)) return false;
			if (field != MuiCollectionAdvancedField.MethodId) return false;
			value = method.MethodId;
			return true;
		}
		if (packet == MuiCollectionAdvancedPacketKind.InsertSingle)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadInsertSingle(
				ref platform, message, out var insertSingle)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				value = insertSingle.MethodId;
			else if (field == MuiCollectionAdvancedField.Entry)
				value = insertSingle.Entry;
			else if (field == MuiCollectionAdvancedField.Position)
				value = insertSingle.Position;
			else return false;
			return true;
		}
		if (packet == MuiCollectionAdvancedPacketKind.Insert)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadInsert(ref platform,
				message, out var insert)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				value = insert.MethodId;
			else if (field == MuiCollectionAdvancedField.Entries)
				value = insert.Entries;
			else if (field == MuiCollectionAdvancedField.Count)
				value = insert.Count;
			else if (field == MuiCollectionAdvancedField.Position)
				value = insert.Position;
			else return false;
			return true;
		}
		if (packet == MuiCollectionAdvancedPacketKind.Position)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadPosition(ref platform,
				message, out var position)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				value = position.MethodId;
			else if (field == MuiCollectionAdvancedField.Position)
				value = position.Position;
			else return false;
			return true;
		}
		if (packet == MuiCollectionAdvancedPacketKind.Redraw)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadRedraw(ref platform,
				message, out var redraw)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				value = redraw.MethodId;
			else if (field == MuiCollectionAdvancedField.Position)
				value = redraw.Position;
			else if (field == MuiCollectionAdvancedField.Entry)
				value = redraw.Entry;
			else return false;
			return true;
		}
		if (packet == MuiCollectionAdvancedPacketKind.Pointer)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadPointer(ref platform,
				message, out var pointer)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				value = pointer.MethodId;
			else if (field == MuiCollectionAdvancedField.Pointer)
				value = pointer.Pointer;
			else return false;
			return true;
		}
		if (packet == MuiCollectionAdvancedPacketKind.Pair)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadPair(ref platform,
				message, out var pair)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				value = pair.MethodId;
			else if (field == MuiCollectionAdvancedField.First)
				value = pair.First;
			else if (field == MuiCollectionAdvancedField.Second)
				value = pair.Second;
			else return false;
			return true;
		}
		if (packet == MuiCollectionAdvancedPacketKind.CreateImage)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadCreateImage(
				ref platform, message, out var image)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				value = image.MethodId;
			else if (field == MuiCollectionAdvancedField.Image)
				value = image.Image;
			else if (field == MuiCollectionAdvancedField.Flags)
				value = image.Flags;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiCollectionAdvancedPacketKind.Method)
		{
			if (field != MuiCollectionAdvancedField.MethodId) return false;
			return MuiCollectionAdvancedStructPacketCodec.TryWriteMethod(ref platform,
				message, value);
		}
		if (packet == MuiCollectionAdvancedPacketKind.InsertSingle)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadInsertSingle(
				ref platform, message, out var insertSingle)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				insertSingle.MethodId = value;
			else if (field == MuiCollectionAdvancedField.Entry)
				insertSingle.Entry = value;
			else if (field == MuiCollectionAdvancedField.Position)
				insertSingle.Position = value;
			else return false;
			return MuiCollectionAdvancedStructPacketCodec.TryWriteInsertSingle(
				ref platform, message, insertSingle);
		}
		if (packet == MuiCollectionAdvancedPacketKind.Insert)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadInsert(ref platform,
				message, out var insert)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				insert.MethodId = value;
			else if (field == MuiCollectionAdvancedField.Entries)
				insert.Entries = value;
			else if (field == MuiCollectionAdvancedField.Count)
				insert.Count = value;
			else if (field == MuiCollectionAdvancedField.Position)
				insert.Position = value;
			else return false;
			return MuiCollectionAdvancedStructPacketCodec.TryWriteInsert(ref platform,
				message, insert);
		}
		if (packet == MuiCollectionAdvancedPacketKind.Position)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadPosition(ref platform,
				message, out var position)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				position.MethodId = value;
			else if (field == MuiCollectionAdvancedField.Position)
				position.Position = value;
			else return false;
			return MuiCollectionAdvancedStructPacketCodec.TryWritePosition(ref platform,
				message, position);
		}
		if (packet == MuiCollectionAdvancedPacketKind.Redraw)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadRedraw(ref platform,
				message, out var redraw)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				redraw.MethodId = value;
			else if (field == MuiCollectionAdvancedField.Position)
				redraw.Position = value;
			else if (field == MuiCollectionAdvancedField.Entry)
				redraw.Entry = value;
			else return false;
			return MuiCollectionAdvancedStructPacketCodec.TryWriteRedraw(ref platform,
				message, redraw);
		}
		if (packet == MuiCollectionAdvancedPacketKind.Pointer)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadPointer(ref platform,
				message, out var pointer)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				pointer.MethodId = value;
			else if (field == MuiCollectionAdvancedField.Pointer)
				pointer.Pointer = value;
			else return false;
			return MuiCollectionAdvancedStructPacketCodec.TryWritePointer(ref platform,
				message, pointer);
		}
		if (packet == MuiCollectionAdvancedPacketKind.Pair)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadPair(ref platform,
				message, out var pair)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				pair.MethodId = value;
			else if (field == MuiCollectionAdvancedField.First)
				pair.First = value;
			else if (field == MuiCollectionAdvancedField.Second)
				pair.Second = value;
			else return false;
			return MuiCollectionAdvancedStructPacketCodec.TryWritePair(ref platform,
				message, pair);
		}
		if (packet == MuiCollectionAdvancedPacketKind.CreateImage)
		{
			if (!MuiCollectionAdvancedStructPacketCodec.TryReadCreateImage(
				ref platform, message, out var image)) return false;
			if (field == MuiCollectionAdvancedField.MethodId)
				image.MethodId = value;
			else if (field == MuiCollectionAdvancedField.Image)
				image.Image = value;
			else if (field == MuiCollectionAdvancedField.Flags)
				image.Flags = value;
			else return false;
			return MuiCollectionAdvancedStructPacketCodec.TryWriteCreateImage(
				ref platform, message, image);
		}
		return false;
	}
}

internal static class MuiCollectionAdvancedFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionAdvancedFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionAdvancedMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionAdvancedMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionAdvancedPacketKind packet,
		MuiCollectionAdvancedField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionAdvancedMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Live List advanced packets are exchanged as declaration-order named
// structs. The field/offset adapter above is retained for compatibility and
// malformed-packet diagnostics only.
// The method-only header gets a scalar-safe entry point so its named packed
// record does not cross the freestanding ABI as a one-field struct.
internal static class MuiCollectionAdvancedMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCollectionMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiCollectionMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiCollectionAdvancedStructPacketCodec
{
	private static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR message, uint size, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message, size,
			out cursor);

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCollectionAdvancedMethodHeaderCodec.TryReadValue(ref platform,
			message, out packet.MethodId)) return false;
		return true;
	}

	internal static bool TryWriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionAdvancedMethodHeaderCodec.WriteValue(ref platform, message,
			method);

	internal static bool TryReadInsertSingle<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionInsertSingleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message,
			MuiCollectionInsertSingleMessage.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Position) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteInsertSingle<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionInsertSingleMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionInsertSingleMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Position) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionInsertMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entries) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Count) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Position) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionInsertMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entries) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Count) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Position) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadPosition<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionPositionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionPositionMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Position) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWritePosition<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionPositionMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionPositionMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Position) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRedraw<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionRedrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionRedrawMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Position) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteRedraw<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionRedrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionRedrawMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Position) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionPointerMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Pointer) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWritePointer<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionPointerMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Pointer) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionPairMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.First) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Second) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWritePair<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionPairMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.First) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Second) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadCreateImage<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCreateImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message,
			MuiCollectionCreateImageMessage.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Image) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteCreateImage<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionCreateImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionCreateImageMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Image) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Flags) && MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiCollectionAdvancedMessageCodec
{
	internal const uint InsertSingle = 0x804254D5u;
	internal const uint Insert = 0x80426C87u;
	internal const uint Remove = 0x8042647Eu;
	internal const uint NextSelected = 0x80425F17u;
	internal const uint SortEntries = 0x80429E32u;
	internal const uint Move = 0x804253C2u;
	internal const uint Exchange = 0x8042468Cu;
	internal const uint Jump = 0x8042BAABu;
	internal const uint Redraw = 0x80427993u;
	internal const uint CreateImage = 0x80429804u;
	internal const uint DeleteImage = 0x80420F58u;
	internal const uint FloattextAppend = 0x8042A221u;

	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		method = 0;
		if (!MuiCollectionAdvancedStructPacketCodec.TryReadMethod(ref platform,
			message, out var packet)) return false;
		method = packet.MethodId;
		return true;
	}

	// Keep the public packet seam as a named collection method record. Native
	// packet readers use the scalar adapter above only for selector admission;
	// operation payloads remain named value-type records.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint method;
		if (!TryReadMethodIdValue(ref platform, message, out method)) return false;
		packet.MethodId = method;
		return true;
	}

	private static bool TryWriteMethodId<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCollectionAdvancedStructPacketCodec.TryWriteMethod(ref platform,
			message, method);
	}

	internal static bool TryReadInsertSingle<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiCollectionInsertSingleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionAdvancedStructPacketCodec.TryReadInsertSingle(
			ref platform, message, out packet) && packet.MethodId == InsertSingle;
	}

	internal static bool WriteInsertSingle<TPlatform>(ref TPlatform platform,
		APTR message, uint entry, uint position)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionInsertSingleMessage);
		packet.MethodId = InsertSingle;
		packet.Entry = entry;
		packet.Position = position;
		return MuiCollectionAdvancedStructPacketCodec.TryWriteInsertSingle(
			ref platform, message, packet);
	}

	internal static bool TryReadInsert<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionInsertMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionAdvancedStructPacketCodec.TryReadInsert(ref platform,
			message, out packet) && packet.MethodId == Insert;
	}

	internal static bool WriteInsert<TPlatform>(ref TPlatform platform,
		APTR message, uint entries, uint count, uint position)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionInsertMessage);
		packet.MethodId = Insert;
		packet.Entries = entries;
		packet.Count = count;
		packet.Position = position;
		return MuiCollectionAdvancedStructPacketCodec.TryWriteInsert(ref platform,
			message, packet);
	}

	internal static bool TryReadPosition<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPositionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsPositionMethod(method) &&
			MuiCollectionAdvancedStructPacketCodec.TryReadPosition(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WritePosition<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint position)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPositionMethod(method)) return false;
		var packet = default(MuiCollectionPositionMessage);
		packet.MethodId = method;
		packet.Position = position;
		return MuiCollectionAdvancedStructPacketCodec.TryWritePosition(
			ref platform, message, packet);
	}

	internal static bool TryReadRedraw<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionRedrawMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionAdvancedStructPacketCodec.TryReadRedraw(ref platform,
			message, out packet) && packet.MethodId == Redraw;
	}

	internal static bool WriteRedraw<TPlatform>(ref TPlatform platform,
		APTR message, uint position, uint entry)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionRedrawMessage);
		packet.MethodId = Redraw;
		packet.Position = position;
		packet.Entry = entry;
		return MuiCollectionAdvancedStructPacketCodec.TryWriteRedraw(ref platform,
			message, packet);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsPointerMethod(method) &&
			MuiCollectionAdvancedStructPacketCodec.TryReadPointer(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint pointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPointerMethod(method)) return false;
		var packet = default(MuiCollectionPointerMessage);
		packet.MethodId = method;
		packet.Pointer = pointer;
		return MuiCollectionAdvancedStructPacketCodec.TryWritePointer(ref platform,
			message, packet);
	}

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsPairMethod(method) &&
			MuiCollectionAdvancedStructPacketCodec.TryReadPair(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPairMethod(method)) return false;
		var packet = default(MuiCollectionPairMessage);
		packet.MethodId = method;
		packet.First = first;
		packet.Second = second;
		return MuiCollectionAdvancedStructPacketCodec.TryWritePair(ref platform,
			message, packet);
	}

	internal static bool TryReadCreateImage<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCreateImageMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionAdvancedStructPacketCodec.TryReadCreateImage(
			ref platform, message, out packet) && packet.MethodId == CreateImage;
	}

	internal static bool WriteCreateImage<TPlatform>(ref TPlatform platform,
		APTR message, uint image, uint flags)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionCreateImageMessage);
		packet.MethodId = CreateImage;
		packet.Image = image;
		packet.Flags = flags;
		return MuiCollectionAdvancedStructPacketCodec.TryWriteCreateImage(
			ref platform, message, packet);
	}

	private static bool IsPositionMethod(uint method) => method == Remove ||
		method == Jump || method == Redraw;

	private static bool IsPointerMethod(uint method) => method == NextSelected ||
		method == SortEntries || method == DeleteImage || method == FloattextAppend;

	private static bool IsPairMethod(uint method) => method == Move ||
		method == Exchange;
}
