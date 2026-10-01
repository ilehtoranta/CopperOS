/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiMiscSpecialistPacketKind : byte
{
	Method,
	Lifecycle,
	Get,
	Set,
	Pointer,
	Pair,
	HandleInput,
	RegisterGadget,
}

internal enum MuiMiscSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	Pointer,
	First,
	Second,
	IntuiMessage,
	MuiKey,
	Gadget,
	Id,
	Parameters,
	Title,
	Label,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiMiscSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiMiscSpecialistPacketKind Packet;
	internal MuiMiscSpecialistField Field;
}

// Struct-first guest-memory adapter for the fixed Misc specialist packets.
// Packet kinds own complete MorphOS record spans; field names select members
// without exposing numeric positions to dispatch code.
internal static class MuiMiscSpecialistMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out uint index, out uint recordSize)
	{
		index = 0;
		recordSize = 0;
		switch (packet)
		{
			case MuiMiscSpecialistPacketKind.Method:
				recordSize = MuiMiscSpecialistMethodMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId) { index = 0; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Lifecycle:
				recordSize = MuiMiscLifecycleMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId) { index = 0; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Get:
				recordSize = MuiMiscSpecialistGetMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMiscSpecialistField.Attribute) { index = 1; return true; }
				if (field == MuiMiscSpecialistField.Storage) { index = 2; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Set:
				recordSize = MuiMiscSpecialistSetMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMiscSpecialistField.Attribute) { index = 1; return true; }
				if (field == MuiMiscSpecialistField.Value) { index = 2; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Pointer:
				recordSize = MuiMiscSpecialistPointerMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMiscSpecialistField.Pointer) { index = 1; return true; }
				break;
			case MuiMiscSpecialistPacketKind.Pair:
				recordSize = MuiMiscSpecialistPairMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMiscSpecialistField.First) { index = 1; return true; }
				if (field == MuiMiscSpecialistField.Second) { index = 2; return true; }
				break;
			case MuiMiscSpecialistPacketKind.HandleInput:
				recordSize = MuiMiscHandleInputMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMiscSpecialistField.IntuiMessage) { index = 1; return true; }
				if (field == MuiMiscSpecialistField.MuiKey) { index = 2; return true; }
				break;
			case MuiMiscSpecialistPacketKind.RegisterGadget:
				recordSize = MuiMiscSpecialistRegisterGadgetMessage.Size;
				if (field == MuiMiscSpecialistField.MethodId) { index = 0; return true; }
				if (field == MuiMiscSpecialistField.Gadget) { index = 1; return true; }
				if (field == MuiMiscSpecialistField.Id) { index = 2; return true; }
				if (field == MuiMiscSpecialistField.Parameters) { index = 3; return true; }
				if (field == MuiMiscSpecialistField.Title) { index = 4; return true; }
				if (field == MuiMiscSpecialistField.Attribute) { index = 5; return true; }
				if (field == MuiMiscSpecialistField.Label) { index = 6; return true; }
				break;
		}
		index = uint.MaxValue;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMiscSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMiscSpecialistFieldCursor cursor, out APTR address,
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
				MuiMiscSpecialistMethodMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiMiscSpecialistMethodMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMiscSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMiscSpecialistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, uint value)
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
internal static class MuiMiscSpecialistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMiscSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMiscSpecialistFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPacketKind packet,
		MuiMiscSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiMiscSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Complete sequential codecs for the fixed Misc specialist packet records.
// Method/lifecycle packets use scalar cursor helpers for one-ULONG records;
// all payload envelopes exchange every named field in declaration order.
internal static class MuiMiscSpecialistMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMiscSpecialistMethodHeaderCodec.TryReadValue(ref platform, message,
			out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiMiscSpecialistMethodHeaderCodec.WriteValue(ref platform, message,
			methodId);

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistGetMessage.Size, out var cursor) ||
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
		APTR message, MuiMiscSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistSetMessage.Size, out var cursor) ||
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
		APTR message, MuiMiscSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistPointerMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var pointer)) return false;
		value.MethodId = methodId;
		value.Pointer = pointer;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistPointerMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Pointer)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistPairMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistPairMessage.Size, out var cursor) ||
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
		APTR message, MuiMiscSpecialistPairMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistPairMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.First) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Second)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscHandleInputMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscHandleInputMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var intuiMessage) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMuiKey)) return false;
		value.MethodId = methodId;
		value.IntuiMessage = intuiMessage;
		value.MuiKey = unchecked((int)rawMuiKey);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscHandleInputMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscHandleInputMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.IntuiMessage) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.MuiKey))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRegisterGadget<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistRegisterGadgetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistRegisterGadgetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadget) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var id) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var parameters) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var title) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var label)) return false;
		value.MethodId = methodId;
		value.Gadget = gadget;
		value.Id = id;
		value.Parameters = parameters;
		value.Title = title;
		value.Attribute = attribute;
		value.Label = label;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRegisterGadget<TPlatform>(ref TPlatform platform,
		APTR message, MuiMiscSpecialistRegisterGadgetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiMiscSpecialistRegisterGadgetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Gadget) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Id) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Parameters) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Title) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Label)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Struct-first codec for the method-only Misc specialist lifecycle header.
// The named one-ULONG record remains the ABI contract while shared guest
// storage keeps selector admission safe for freestanding lowering.
internal static class MuiMiscSpecialistMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMiscSpecialistMethodMessage.Size, out var cursor) ||
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
			MuiMiscSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Shared fixed-packet boundary for the Misc specialist family. The public
// records remain the consumer-facing shape; only this adapter owns their
// typed cursor traversal and mapping checks. Both standalone and object-aware
// dispatchers use this codec so their ABI validation cannot drift apart.
internal static class MuiMiscSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;
	internal const uint HandleInput = 0x80422A1Au;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Selector admission stays scalar for callers that only need MethodID, but
	// the value is read from the named one-ULONG record in declaration order.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiMiscSpecialistMessageStructCodec.TryReadMethodIdValue(
			ref platform, message, out methodId);
	}

	internal static bool TryReadLifecycle<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscLifecycleMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!IsLifecycleMethod(method) || message.IsNull ||
			!platform.IsMapped(message, MuiMiscLifecycleMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != method) return false;
		packet.MethodId = methodId;
		return true;
	}

	internal static bool WriteLifecycle<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		return IsLifecycleMethod(method) &&
			MuiMiscSpecialistMessageStructCodec.TryWriteMethodIdValue(
				ref platform, message, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiMiscSpecialistMessageStructCodec.TryReadGet(ref platform,
			message, out packet) && packet.MethodId == OmGet;
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiMiscSpecialistGetMessage);
		packet.MethodId = OmGet;
		packet.Attribute = attribute;
		packet.Storage = storage;
		return MuiMiscSpecialistMessageStructCodec.WriteGet(ref platform,
			message, packet);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != method) return false;
		packet.MethodId = methodId;
		return true;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiMiscSpecialistMessageStructCodec.TryWriteMethodIdValue(
			ref platform, message, method);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSetMethod(method) &&
			MuiMiscSpecialistMessageStructCodec.TryReadSet(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method)) return false;
		var packet = default(MuiMiscSpecialistSetMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiMiscSpecialistMessageStructCodec.WriteSet(ref platform,
			message, packet);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistPointerMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiMiscSpecialistMessageStructCodec.TryReadPointer(ref platform,
			message, out packet) && packet.MethodId == method;
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint pointer)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiMiscSpecialistPointerMessage);
		packet.MethodId = method;
		packet.Pointer = pointer;
		return MuiMiscSpecialistMessageStructCodec.WritePointer(ref platform,
			message, packet);
	}

	internal static bool TryReadPair<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiMiscSpecialistPairMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiMiscSpecialistMessageStructCodec.TryReadPair(ref platform,
			message, out packet) && packet.MethodId == method;
	}

	internal static bool WritePair<TPlatform>(ref TPlatform platform, APTR message,
		uint method, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiMiscSpecialistPairMessage);
		packet.MethodId = method;
		packet.First = first;
		packet.Second = second;
		return MuiMiscSpecialistMessageStructCodec.WritePair(ref platform,
			message, packet);
	}

	internal static bool TryReadHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, out MuiMiscHandleInputMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiMiscSpecialistMessageStructCodec.TryReadHandleInput(
			ref platform, message, out packet) && packet.MethodId == HandleInput;
	}

	internal static bool WriteHandleInput<TPlatform>(ref TPlatform platform,
		APTR message, uint intuiMessage, int muiKey)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiMiscHandleInputMessage);
		packet.MethodId = HandleInput;
		packet.IntuiMessage = intuiMessage;
		packet.MuiKey = muiKey;
		return MuiMiscSpecialistMessageStructCodec.WriteHandleInput(ref platform,
			message, packet);
	}

	internal static bool TryReadRegisterGadget<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiMiscSpecialistRegisterGadgetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiMiscSpecialistMessageStructCodec.TryReadRegisterGadget(
			ref platform, message, out packet) && packet.MethodId ==
			MuiMiscAttributes.Mccprefs_RegisterGadget;
	}

	internal static bool WriteRegisterGadget<TPlatform>(ref TPlatform platform,
		APTR message, uint gadget, uint id, uint parameters, uint title,
		uint attribute, uint label) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiMiscSpecialistRegisterGadgetMessage);
		packet.MethodId = MuiMiscAttributes.Mccprefs_RegisterGadget;
		packet.Gadget = gadget;
		packet.Id = id;
		packet.Parameters = parameters;
		packet.Title = title;
		packet.Attribute = attribute;
		packet.Label = label;
		return MuiMiscSpecialistMessageStructCodec.WriteRegisterGadget(ref platform,
			message, packet);
	}

	private static bool IsLifecycleMethod(uint method) =>
		method == MuiMiscAttributes.Setup || method == MuiMiscAttributes.Cleanup;

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		return methodId == method;
	}
}
