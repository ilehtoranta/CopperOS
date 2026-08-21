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
	internal uint MethodId;
	internal APTR Hook;
	internal uint Param1;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCallHookMethodMessage
{
	internal const uint Size = 4;
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

internal static class MuiCallHookPacketFieldCursorCodec
{
	private static bool TryResolve(MuiCallHookPacketField field,
		out uint offset)
	{
		if (field == MuiCallHookPacketField.MethodId) { offset = 0; return true; }
		if (field == MuiCallHookPacketField.Hook) { offset = 4; return true; }
		if (field == MuiCallHookPacketField.Param1) { offset = 8; return true; }
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCallHookPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiCallHookPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCallHookPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCallHookPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiCallHookMethodMessage.Size)) return false;
		return MuiCallHookPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCallHookPacketField.MethodId, out packet.MethodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCallHookMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCallHookMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != Method) return false;
		if (!MuiCallHookPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCallHookPacketField.Hook, out var rawHook) ||
			!MuiCallHookPacketFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCallHookPacketField.Param1, out packet.Param1)) return false;
		packet.MethodId = header.MethodId;
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
