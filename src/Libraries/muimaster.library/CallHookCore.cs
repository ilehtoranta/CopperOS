/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS MUIM_CallHook packet. The fixed ABI contains the hook pointer and
// first ULONG parameter; additional variadic parameters, when present, remain
// immediately after param1 in caller-owned guest memory.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCallHookMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint HookOffset = 4;
	// Compatibility metadata for the named Param1 field; live variadic-vector
	// admission obtains this boundary through MuiCallHookMessageMemoryCodec.
	internal const uint Param1Offset = 8;
	internal uint MethodId;
	internal APTR Hook;
	internal uint Param1;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCallHookMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

internal enum MuiCallHookPacketField : byte
{
	MethodId,
	Hook,
	Param1,
}

// Struct-first codec for the fixed four-byte MUIM_CallHook method header.
// Keeping this envelope explicit prevents selector reads from depending on
// anonymous offsets while still using the shared bounded ULONG representation.
internal static class MuiCallHookMethodMessageCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiCallHookMethodMessage.Size, out var cursor) ||
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
			MuiCallHookMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiCallHookMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadValue(ref platform, address, out var methodId)) return false;
		packet.MethodId = methodId;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiCallHookMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, address, packet.MethodId);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCallHookPacketFieldCursor
{
	internal APTR Message;
	internal MuiCallHookPacketField Field;
}

// Struct-first guest-memory adapter for the fixed CallHook envelope. The
// complete 12-byte record is admitted before any non-header member is used;
// the 4-byte method record remains a deliberate header-only exception.
internal static class MuiCallHookMessageMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiCallHookPacketField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint fieldIndex;
		switch (field)
		{
			case MuiCallHookPacketField.MethodId: fieldIndex = 0; break;
			case MuiCallHookPacketField.Hook: fieldIndex = 1; break;
			case MuiCallHookPacketField.Param1: fieldIndex = 2; break;
			default: return false;
		}
		if (fieldIndex > 0 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCallHookMessage.FieldSize, out _)) return false;
		if (fieldIndex > 1 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCallHookMessage.FieldSize, out _)) return false;
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiCallHookMessage.FieldSize, out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		var recordSize = field == MuiCallHookPacketField.MethodId
			? MuiCallHookMethodMessage.Size
			: MuiCallHookMessage.Size;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message, recordSize,
			out var cursor) || !TryTakeField(ref platform, ref cursor, field,
				out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (field == MuiCallHookPacketField.MethodId)
		{
			return MuiCallHookMethodMessageCodec.TryReadValue(ref platform,
				message, out value);
		}
		if (!MuiCallHookMessageStructCodec.TryRead(ref platform, message,
			out var packet)) return false;
		if (field == MuiCallHookPacketField.Hook) value = packet.Hook.Raw;
		else if (field == MuiCallHookPacketField.Param1) value = packet.Param1;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (field == MuiCallHookPacketField.MethodId)
		{
			return MuiCallHookMethodMessageCodec.WriteValue(ref platform,
				message, value);
		}
		if (!MuiCallHookMessageStructCodec.TryRead(ref platform, message,
			out var packet)) return false;
		if (field == MuiCallHookPacketField.Hook)
			packet.Hook = APTR.FromPointer(value);
		else if (field == MuiCallHookPacketField.Param1) packet.Param1 = value;
		else return false;
		return MuiCallHookMessageStructCodec.Write(ref platform, message, packet);
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codec routes through the named record
// adapter above.
internal static class MuiCallHookPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCallHookPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCallHookMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCallHookMessageMemoryCodec.TryReadUInt32(ref platform, message,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCallHookMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			field, value);
}

// Named cursor for the caller-owned ULONG vector beginning at param1 in a
// MUIM_CallHook packet. The first element is part of the fixed envelope; later
// elements are the optional variadic tail.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCallHookParameterRecord
{
	internal const uint Size = 4;
	internal uint Value;
}

// Struct-first codec for one caller-owned CallHook parameter. The hook ABI
// receives the address of this record; callers that need the scalar value can
// use this codec without reaching through an anonymous ULONG offset.
internal static class MuiCallHookParameterRecordCodec
{
	// Keep the named one-ULONG record API while routing the guest exchange
	// through the shared bounded ULONG codec. Hook implementations that need
	// only the scalar value still use these cursor entry points.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiCallHookParameterRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress)) return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
			out value) && MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiCallHookParameterRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress)) return false;
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
			value) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiCallHookParameterRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadValue(ref platform, address, out var rawValue)) return false;
		value.Value = rawValue;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiCallHookParameterRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, address, value.Value);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCallHookParameterCursor
{
	internal const uint FirstOffset = MuiCallHookMessage.Param1Offset;
	internal const uint EntrySize = MuiCallHookParameterRecord.Size;
	internal APTR Message;
	internal uint Index;
}

// Struct-first adapter for the caller-owned parameter vector beginning at
// Param1 in a MUIM_CallHook packet. The fixed packet record and selected
// parameter slot must both be completely mapped before an address is exposed;
// the variadic tail remains guest-owned storage.
internal static class MuiCallHookParameterMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (message.IsNull ||
			!MuiCallHookMessageMemoryCodec.TryGetAddress(ref platform, message,
				MuiCallHookPacketField.Param1, out var baseAddress)) return false;
		if (index > (uint.MaxValue - baseAddress) /
			MuiCallHookParameterRecord.Size) return false;
		var offset = index * MuiCallHookParameterRecord.Size;
		if (baseAddress > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(baseAddress + offset);
		return platform.IsMapped(address,
			MuiCallHookParameterRecord.Size);
	}
}

internal static class MuiCallHookParameterCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiCallHookParameterCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCallHookParameterMemoryCodec.TryGetEntry(ref platform,
			cursor.Message, cursor.Index, out address);
}

// Central codec for the fixed CallHook envelope. The variadic tail remains
// caller-owned guest storage; only this record's packed fields are decoded
// here.
internal static class MuiCallHookMessageCodec
{
	internal const uint Method = 0x8042B96Bu;

	internal static bool TryGetFirstParameter<TPlatform>(
		ref TPlatform platform, APTR message, out APTR parameter)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCallHookParameterCursor);
		cursor.Message = message;
		cursor.Index = 0;
		return MuiCallHookParameterCursorCodec.TryGetEntry(ref platform, cursor,
			out parameter);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCallHookMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCallHookMethodMessageCodec.TryRead(ref platform, message,
			out packet);
	}

	// Method admission remains scalar for callers that only need the selector,
	// but it is read from the named method-header record in declaration order.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCallHookMethodMessageCodec.TryReadValue(ref platform, message,
			out methodId);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCallHookMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCallHookMessageStructCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != Method) return false;
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		// The dispatch-facing writer preserves the fixed MUIM_CallHook selector;
		// callers that need an arbitrary structural record use the named codec
		// directly (as the field adapter does).
		packet.MethodId = Method;
		return MuiCallHookMessageStructCodec.Write(ref platform, message,
			packet);
	}
}

// Complete named codec for the fixed CallHook envelope.  Unlike the
// dispatch-facing codec above, this structural form does not validate the
// method selector, so field mutation can preserve a caller-owned record while
// the semantic dispatch path still enforces MUIM_CallHook.
internal static class MuiCallHookMessageStructCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiCallHookMessage record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiCallHookMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawHook) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Param1) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Hook = APTR.FromPointer(rawHook);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiCallHookMessage record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiCallHookMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Hook.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Param1)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

public static class MuiCallHookCore
{
	public const uint Method = MuiCallHookMessageCodec.Method;
	public const uint PacketSize = MuiCallHookMessage.Size;

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiCallHookMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiCallHookMessageCodec.TryRead(ref platform, message, out packet);

	public static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR message,
		APTR hook, uint param1) where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCallHookMessage);
		packet.MethodId = Method;
		packet.Hook = hook;
		packet.Param1 = param1;
		return MuiCallHookMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	public static uint Dispatch<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryRead(ref platform, message, out var packet) ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return 0;
		return Invoke(ref platform, obj, message, packet);
	}

	// Focused native seam for the fixed packet plus the existing callback
	// capability. A1 is the address of param1, not the ULONG value itself; this
	// preserves the guest variadic tail exactly as the MorphOS hook contract.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR objectAddress, APTR message)
		where TPlatform : struct, IMuiGuestMemory, IMuiCallbackCapability
	{
		if (!TryRead(ref platform, message, out var packet)) return 0;
		return Invoke(ref platform, objectAddress, message, packet);
	}

	private static uint Invoke<TPlatform>(ref TPlatform platform,
		APTR objectAddress, APTR message, MuiCallHookMessage packet)
		where TPlatform : struct, IMuiGuestMemory, IMuiCallbackCapability
	{
		if (packet.Hook.IsNull || !platform.IsMapped(packet.Hook, 20) ||
			!MuiCallHookMessageCodec.TryGetFirstParameter(ref platform, message,
				out var firstParameter)) return 0;
		return platform.InvokeHook(packet.Hook, objectAddress, firstParameter);
	}
}
