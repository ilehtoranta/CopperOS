/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiMenuSpecialistPacketKind : byte
{
	Method,
	Get,
	Set,
	Pointer,
	Pair,
	Popup,
}

internal enum MuiMenuSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	ObjectPointer,
	First,
	Second,
	Window,
	X,
	Y,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMenuSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiMenuSpecialistPacketKind Packet;
	internal MuiMenuSpecialistField Field;
}

// Struct-first guest-memory adapter for the fixed Menustrip/Menu/Menuitem
// packets. Packet kinds own complete MorphOS record spans; field names select
// members without exposing numeric positions to dispatch code.
internal static class MuiMenuSpecialistMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out uint index, out uint recordSize)
	{
		index = 0;
		recordSize = 0;
		switch (packet)
		{
			case MuiMenuSpecialistPacketKind.Method:
				recordSize = MuiMenuSpecialistMethodMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId) { index = 0; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Get:
				recordSize = MuiMenuSpecialistGetMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMenuSpecialistField.Attribute) { index = 1; return true; }
				if (field == MuiMenuSpecialistField.Storage) { index = 2; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Set:
				recordSize = MuiMenuSpecialistSetMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMenuSpecialistField.Attribute) { index = 1; return true; }
				if (field == MuiMenuSpecialistField.Value) { index = 2; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Pointer:
				recordSize = MuiMenuSpecialistPointerMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMenuSpecialistField.ObjectPointer) { index = 1; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Pair:
				recordSize = MuiMenuSpecialistPairMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMenuSpecialistField.First) { index = 1; return true; }
				if (field == MuiMenuSpecialistField.Second) { index = 2; return true; }
				break;
			case MuiMenuSpecialistPacketKind.Popup:
				recordSize = MuiMenuSpecialistPopupMessage.Size;
				if (field == MuiMenuSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMenuSpecialistField.Window) { index = 1; return true; }
				if (field == MuiMenuSpecialistField.X) { index = 2; return true; }
				if (field == MuiMenuSpecialistField.Y) { index = 3; return true; }
				break;
		}
		index = uint.MaxValue;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMenuSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuSpecialistFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Packet, cursor.Field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Message,
				recordSize, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiMenuSpecialistMethodMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiMenuSpecialistMethodMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMenuSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codecs route to the struct adapter above.
internal static class MuiMenuSpecialistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMenuSpecialistMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMenuSpecialistFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMenuSpecialistMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMenuSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPacketKind packet,
		MuiMenuSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMenuSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Complete sequential codecs for the fixed Menustrip/Menu/Menuitem packet
// records. Method-only packets use scalar cursor helpers for the one-ULONG
// record; all payload envelopes exchange every named field in declaration order.
internal static class MuiMenuSpecialistMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuSpecialistMethodHeaderCodec.TryReadValue(ref platform, message,
			out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMenuSpecialistMethodHeaderCodec.WriteValue(ref platform, message,
			methodId);

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var storage)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Storage = storage;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var valueWord)) return false;
		value.MethodId = methodId;
		value.Attribute = attribute;
		value.Value = valueWord;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistPointerMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var objectPointer)) return false;
		value.MethodId = methodId;
		value.ObjectPointer = objectPointer;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistPointerMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ObjectPointer)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistPairMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistPairMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var first) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var second)) return false;
		value.MethodId = methodId;
		value.First = first;
		value.Second = second;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPairMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistPairMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.First) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Second)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadPopup<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistPopupMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistPopupMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var window) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var x) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var y)) return false;
		value.MethodId = methodId;
		value.Window = window;
		value.X = x;
		value.Y = y;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WritePopup<TPlatform>(ref TPlatform platform,
		APTR message, MuiMenuSpecialistPopupMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMenuSpecialistPopupMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Window) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.X) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Y)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Struct-first codec for the method-only Menustrip/Menu/Menuitem header. The
// named one-ULONG record remains the ABI contract while shared guest storage
// keeps selector admission safe for freestanding lowering.
internal static class MuiMenuSpecialistMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMenuSpecialistMethodMessage.Size, out var cursor) ||
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
			MuiMenuSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Central codec for the fixed MorphOS Menustrip/Menu/Menuitem packet family.
// Dispatch consumers use the named records declared at the public boundary;
// only this adapter owns their packed guest-memory layout.
internal static class MuiMenuSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMenuSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!IsMethod(method) || !TryReadMethodIdValue(ref platform, message,
			out methodId) || methodId != method) return false;
		packet.MethodId = methodId;
		return true;
	}

	// Read the fixed menu-specialist method header without constraining the
	// selector. Dispatch selection uses this named record; specialized codecs
	// retain validation of complete packet shapes.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Native qualification keeps method-header admission scalar while the
	// dispatcher-facing overload above retains the named value-type record.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiMenuSpecialistMessageStructCodec.TryReadMethodIdValue(
			ref platform, message, out methodId);
	}

	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method)) return false;
		uint methodId;
		return TryReadMethodIdValue(ref platform, message, out methodId) &&
			methodId == method;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			MuiMenuSpecialistMessageStructCodec.TryWriteMethodIdValue(
				ref platform, message, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiMenuSpecialistMessageStructCodec.TryReadGet(ref platform,
			message, out packet) && packet.MethodId == OmGet;
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiMenuSpecialistGetMessage);
		packet.MethodId = OmGet;
		packet.Attribute = attribute;
		packet.Storage = storage;
		return MuiMenuSpecialistMessageStructCodec.WriteGet(ref platform,
			message, packet);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMenuSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSetMethod(method) &&
			MuiMenuSpecialistMessageStructCodec.TryReadSet(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method)) return false;
		var packet = default(MuiMenuSpecialistSetMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiMenuSpecialistMessageStructCodec.WriteSet(ref platform,
			message, packet);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMenuSpecialistPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsPointerMethod(method) &&
			MuiMenuSpecialistMessageStructCodec.TryReadPointer(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint objectPointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPointerMethod(method)) return false;
		var packet = default(MuiMenuSpecialistPointerMessage);
		packet.MethodId = method;
		packet.ObjectPointer = objectPointer;
		return MuiMenuSpecialistMessageStructCodec.WritePointer(ref platform,
			message, packet);
	}

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMenuSpecialistPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsPairMethod(method) &&
			MuiMenuSpecialistMessageStructCodec.TryReadPair(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsPairMethod(method)) return false;
		var packet = default(MuiMenuSpecialistPairMessage);
		packet.MethodId = method;
		packet.First = first;
		packet.Second = second;
		return MuiMenuSpecialistMessageStructCodec.WritePair(ref platform,
			message, packet);
	}

	internal static bool TryReadPopup<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMenuSpecialistPopupMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiMenuSpecialistMessageStructCodec.TryReadPopup(ref platform,
			message, out packet) && packet.MethodId ==
			MuiMenuAttributes.Menustrip_Popup;
	}

	internal static bool WritePopup<TPlatform>(ref TPlatform platform,
		APTR message, uint window, uint x, uint y)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiMenuSpecialistPopupMessage);
		packet.MethodId = MuiMenuAttributes.Menustrip_Popup;
		packet.Window = window;
		packet.X = x;
		packet.Y = y;
		return MuiMenuSpecialistMessageStructCodec.WritePopup(ref platform,
			message, packet);
	}

	private static bool IsMethod(uint method) => method == OmDispose ||
		method == MuiMenuAttributes.Menustrip_InitChange ||
		method == MuiMenuAttributes.Menustrip_ExitChange ||
		method == MuiMenuAttributes.Menustrip_WillOpen;

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsPointerMethod(uint method) =>
		method == MuiMenuAttributes.Family_AddTail ||
		method == MuiMenuAttributes.Family_AddHead ||
		method == MuiMenuAttributes.Family_Remove ||
		method == MuiMenuAttributes.Family_Sort ||
		method == MuiMenuAttributes.Family_Transfer;

	private static bool IsPairMethod(uint method) =>
		method == MuiMenuAttributes.Family_Insert ||
		method == MuiMenuAttributes.Family_Reorder;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodIdValue(ref platform, message, out uint methodId) &&
		methodId == method && platform.IsMapped(message, size);
}
