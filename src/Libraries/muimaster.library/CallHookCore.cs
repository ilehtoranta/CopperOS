/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
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
	private static bool TryResolve(MuiCallHookPacketField field,
		out uint offset, out uint recordSize)
	{
		recordSize = MuiCallHookMessage.Size;
		if (field == MuiCallHookPacketField.MethodId)
		{
			offset = MuiCallHookMessage.MethodIdOffset;
			recordSize = MuiCallHookMethodMessage.Size;
			return true;
		}
		if (field == MuiCallHookPacketField.Hook)
		{
			offset = MuiCallHookMessage.HookOffset;
			return true;
		}
		if (field == MuiCallHookPacketField.Param1)
		{
			offset = MuiCallHookMessage.Param1Offset;
			return true;
		}
		offset = 0;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var recordSize) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, recordSize)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiCallHookMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
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
internal struct MuiCallHookParameterCursor
{
	internal const uint FirstOffset = 8;
	internal const uint EntrySize = 4;
	internal APTR Message;
	internal uint Index;
}

internal static class MuiCallHookParameterCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiCallHookParameterCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue -
			MuiCallHookParameterCursor.FirstOffset) return false;
		var baseAddress = APTR.FromPointer(cursor.Message.Raw +
			MuiCallHookParameterCursor.FirstOffset);
		if (cursor.Index > (uint.MaxValue - baseAddress.Raw) /
			MuiCallHookParameterCursor.EntrySize) return false;
		var offset = cursor.Index * MuiCallHookParameterCursor.EntrySize;
		if (baseAddress.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(baseAddress.Raw + offset);
		return platform.IsMapped(address, MuiCallHookParameterCursor.EntrySize);
	}
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
		return MuiCallHookParameterCursorCodec.TryGetEntry(ref platform,
			cursor, out parameter);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCallHookMethodMessage packet)
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
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCallHookMethodMessage.Size)) return false;
		return MuiCallHookPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCallHookPacketField.MethodId, out methodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCallHookMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != Method || !platform.IsMapped(message,
			MuiCallHookMessage.Size)) return false;
		if (!MuiCallHookPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCallHookPacketField.Hook, out var rawHook) ||
			!MuiCallHookPacketFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCallHookPacketField.Param1, out packet.Param1)) return false;
		packet.MethodId = methodId;
		packet.Hook = APTR.FromPointer(rawHook);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCallHookMessage.Size)) return false;
		return MuiCallHookPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCallHookPacketField.MethodId, Method) &&
			MuiCallHookPacketFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCallHookPacketField.Hook, packet.Hook.Raw) &&
			MuiCallHookPacketFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCallHookPacketField.Param1, packet.Param1);
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
