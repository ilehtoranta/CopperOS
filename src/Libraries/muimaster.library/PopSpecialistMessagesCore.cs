/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

internal enum MuiPopSpecialistPacketKind : byte
{
	Method,
	Get,
	Set,
	Close,
}

internal enum MuiPopSpecialistField : byte
{
	MethodId,
	Attribute,
	Storage,
	Value,
	Result,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistFieldCursor
{
	internal APTR Message;
	internal MuiPopSpecialistPacketKind Packet;
	internal MuiPopSpecialistField Field;
}

// Struct-first guest-memory adapter for the fixed Popstring/Popobject/Popasl
// packets. Packet kinds own complete MorphOS record spans; field names select
// members without exposing numeric positions to dispatch code.
internal static class MuiPopSpecialistMessageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out uint index, out uint recordSize)
	{
		index = uint.MaxValue;
		recordSize = 0;
		if (packet == MuiPopSpecialistPacketKind.Method &&
			field == MuiPopSpecialistField.MethodId)
		{
			index = 0; recordSize = MuiPopSpecialistMethodMessage.Size; return true;
		}
		if (packet == MuiPopSpecialistPacketKind.Get)
		{
			if (field == MuiPopSpecialistField.MethodId) { index = 0; recordSize = MuiPopSpecialistGetMessage.Size; return true; }
			if (field == MuiPopSpecialistField.Attribute) { index = 1; recordSize = MuiPopSpecialistGetMessage.Size; return true; }
			if (field == MuiPopSpecialistField.Storage) { index = 2; recordSize = MuiPopSpecialistGetMessage.Size; return true; }
		}
		else if (packet == MuiPopSpecialistPacketKind.Set)
		{
			if (field == MuiPopSpecialistField.MethodId) { index = 0; recordSize = MuiPopSpecialistSetMessage.Size; return true; }
			if (field == MuiPopSpecialistField.Attribute) { index = 1; recordSize = MuiPopSpecialistSetMessage.Size; return true; }
			if (field == MuiPopSpecialistField.Value) { index = 2; recordSize = MuiPopSpecialistSetMessage.Size; return true; }
		}
		else if (packet == MuiPopSpecialistPacketKind.Close)
		{
			if (field == MuiPopSpecialistField.MethodId) { index = 0; recordSize = MuiPopSpecialistCloseMessage.Size; return true; }
			if (field == MuiPopSpecialistField.Result) { index = 1; recordSize = MuiPopSpecialistCloseMessage.Size; return true; }
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetAddress(ref platform, message, packet, field,
			out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(packet, field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, recordSize,
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
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, uint value)
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
internal static class MuiPopSpecialistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPopSpecialistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPopSpecialistFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistPacketKind packet,
		MuiPopSpecialistField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiPopSpecialistMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Complete sequential codecs for the fixed Popstring/Popobject/Popasl packet
// records. Method-only packets use scalar cursor helpers for the one-ULONG
// record; every multi-field envelope exchanges its named fields in order.
internal static class MuiPopSpecialistMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiPopSpecialistMethodHeaderCodec.TryReadValue(ref platform, message,
			out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiPopSpecialistMethodHeaderCodec.WriteValue(ref platform, message,
			methodId);

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiPopSpecialistGetMessage.Size, out var cursor) ||
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
		APTR message, MuiPopSpecialistGetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiPopSpecialistGetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiPopSpecialistSetMessage.Size, out var cursor) ||
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
		APTR message, MuiPopSpecialistSetMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiPopSpecialistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadClose<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistCloseMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiPopSpecialistCloseMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var methodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var result)) return false;
		value.MethodId = methodId;
		value.Result = result;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteClose<TPlatform>(ref TPlatform platform,
		APTR message, MuiPopSpecialistCloseMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiPopSpecialistCloseMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Result)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Struct-first codec for the method-only Popstring/Popobject/Popasl header.
// The named one-ULONG record remains the ABI contract while shared guest
// storage keeps scalar selector admission safe for freestanding lowering.
internal static class MuiPopSpecialistMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistMethodMessage.Size, out var cursor) ||
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
			MuiPopSpecialistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Central codec for the fixed MorphOS Popstring/Popobject/Popasl packet
// family. Dispatch consumers use the named records declared at the public
// boundary; only this adapter owns their packed guest-memory layout.
internal static class MuiPopSpecialistMessageCodec
{
	internal const uint OmDispose = 0x00000102u;
	internal const uint OmGet = 0x00000104u;
	internal const uint MethodSet = 0x8042549Au;
	internal const uint MethodNoNotifySet = 0x8042216Fu;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistMethodMessage packet)
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
		return MuiPopSpecialistMessageStructCodec.TryReadMethodIdValue(
			ref platform, message, out methodId);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiPopSpecialistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsValidMethod(ref platform, message, method)) return false;
		packet.MethodId = method;
		return true;
	}

	// Native method-only consumers use this scalar form to avoid materializing
	// a one-field out record in compiler paths where it can widen the branch.
	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			TryReadMethodIdValue(ref platform, message, out var methodId) &&
			methodId == method;
	}

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			MuiPopSpecialistMessageStructCodec.TryWriteMethodIdValue(
				ref platform, message, method);
	}

	internal static bool TryReadGet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistGetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiPopSpecialistMessageStructCodec.TryReadGet(ref platform,
			message, out packet) && packet.MethodId == OmGet;
	}

	internal static bool WriteGet<TPlatform>(ref TPlatform platform,
		APTR message, uint attribute, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiPopSpecialistGetMessage);
		packet.MethodId = OmGet;
		packet.Attribute = attribute;
		packet.Storage = storage;
		return MuiPopSpecialistMessageStructCodec.WriteGet(ref platform,
			message, packet);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiPopSpecialistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSetMethod(method) &&
			MuiPopSpecialistMessageStructCodec.TryReadSet(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method)) return false;
		var packet = default(MuiPopSpecialistSetMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiPopSpecialistMessageStructCodec.WriteSet(ref platform,
			message, packet);
	}

	internal static bool TryReadClose<TPlatform>(ref TPlatform platform,
		APTR message, out MuiPopSpecialistCloseMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out var methodId) ||
			methodId != MuiPopAttributes.Popstring_Close) return false;
		packet.MethodId = methodId;
		// Preserve the MorphOS-compatible tolerant boundary: a method-only close
		// frame means result FALSE, while the documented second word is consumed
		// when present.
		if (platform.IsMapped(message, MuiPopSpecialistCloseMessage.Size) &&
			MuiPopSpecialistMessageStructCodec.TryReadClose(ref platform, message,
				out var close))
			packet.Result = close.Result;
		return true;
	}

	internal static bool WriteClose<TPlatform>(ref TPlatform platform,
		APTR message, uint result)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiPopSpecialistCloseMessage);
		packet.MethodId = MuiPopAttributes.Popstring_Close;
		packet.Result = result;
		return MuiPopSpecialistMessageStructCodec.WriteClose(ref platform,
			message, packet);
	}

	private static bool IsMethod(uint method) => method == OmDispose ||
		method == MuiPopAttributes.Popstring_Open ||
		method == MuiPopAttributes.HandleInput ||
		method == MuiPopAttributes.Setup ||
		method == MuiPopAttributes.Cleanup;

	private static bool IsSetMethod(uint method) => method == MethodSet ||
		method == MethodNoNotifySet;

	private static bool IsPacket<TPlatform>(ref TPlatform platform, APTR message,
		uint size, uint method) where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message, size) ||
			!TryReadMethodIdValue(ref platform, message, out var methodId)) return false;
		return methodId == method;
	}
}
