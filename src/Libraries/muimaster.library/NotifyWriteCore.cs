/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS Notify superclass packets for the two bounded memory-write helpers.
// The guest ABI is represented as named records so callers do not duplicate
// packet offsets at each dispatch site.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyWriteMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWriteLongMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal uint Value;
	internal APTR Memory;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWriteStringMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal APTR String;
	internal APTR Memory;
}

internal enum MuiNotifyWritePacketKind : byte
{
	WriteLong,
	WriteString,
}

internal enum MuiNotifyWritePacketField : byte
{
	MethodId,
	Value,
	String,
	Memory,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyWritePacketFieldCursor
{
	internal APTR Message;
	internal MuiNotifyWritePacketKind Packet;
	internal MuiNotifyWritePacketField Field;
}

internal static class MuiNotifyWritePacketFieldCursorCodec
{
	private static bool TryResolve(MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, out uint offset)
	{
		switch (packet)
		{
			case MuiNotifyWritePacketKind.WriteLong:
				if (field == MuiNotifyWritePacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyWritePacketField.Value) { offset = 4; return true; }
				if (field == MuiNotifyWritePacketField.Memory) { offset = 8; return true; }
				break;
			case MuiNotifyWritePacketKind.WriteString:
				if (field == MuiNotifyWritePacketField.MethodId) { offset = 0; return true; }
				if (field == MuiNotifyWritePacketField.String) { offset = 4; return true; }
				if (field == MuiNotifyWritePacketField.Memory) { offset = 8; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyWritePacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiNotifyWritePacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiNotifyWritePacketFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Central codec for the fixed Notify write envelopes. The bounded memory-copy
// operations below consume named records and keep their packed offsets here.
internal static class MuiNotifyWriteMessageCodec
{
	internal const uint WriteLongMethod = 0x80428D86;
	internal const uint WriteStringMethod = 0x80424BF4;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiNotifyWriteMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiNotifyWriteMethodMessage.Size)) return false;
		return MuiNotifyWritePacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.MethodId, out packet.MethodId);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		method = 0;
		if (!TryReadMethodId(ref platform, message, out var packet)) return false;
		method = packet.MethodId;
		return true;
	}

	internal static bool TryReadWriteLong<TPlatform>(ref TPlatform platform,
		APTR message, out MuiWriteLongMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiWriteLongMessage.Size,
			WriteLongMethod)) return false;
		if (!MuiNotifyWritePacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.Value, out packet.Value) ||
			!MuiNotifyWritePacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiNotifyWritePacketKind.WriteLong,
				MuiNotifyWritePacketField.Memory, out var rawMemory)) return false;
		packet.MethodId = WriteLongMethod;
		packet.Memory = APTR.FromPointer(rawMemory);
		return true;
	}

	internal static bool TryReadWriteString<TPlatform>(ref TPlatform platform,
		APTR message, out MuiWriteStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiWriteStringMessage.Size,
			WriteStringMethod)) return false;
		if (!MuiNotifyWritePacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.String, out var rawString) ||
			!MuiNotifyWritePacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiNotifyWritePacketKind.WriteString,
				MuiNotifyWritePacketField.Memory, out var rawMemory)) return false;
		packet.MethodId = WriteStringMethod;
		packet.String = APTR.FromPointer(rawString);
		packet.Memory = APTR.FromPointer(rawMemory);
		return true;
	}

	internal static bool TryWriteWriteLong<TPlatform>(ref TPlatform platform,
		APTR message, MuiWriteLongMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiWriteLongMessage.Size))
			return false;
		return MuiNotifyWritePacketFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.MethodId, WriteLongMethod) &&
			MuiNotifyWritePacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiNotifyWritePacketKind.WriteLong,
				MuiNotifyWritePacketField.Value, packet.Value) &&
			MuiNotifyWritePacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiNotifyWritePacketKind.WriteLong,
				MuiNotifyWritePacketField.Memory, packet.Memory.Raw);
	}

	internal static bool TryWriteWriteString<TPlatform>(ref TPlatform platform,
		APTR message, MuiWriteStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMapped(ref platform, message, MuiWriteStringMessage.Size))
			return false;
		return MuiNotifyWritePacketFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiNotifyWritePacketKind.WriteString,
			MuiNotifyWritePacketField.MethodId, WriteStringMethod) &&
			MuiNotifyWritePacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiNotifyWritePacketKind.WriteString,
				MuiNotifyWritePacketField.String, packet.String.Raw) &&
			MuiNotifyWritePacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiNotifyWritePacketKind.WriteString,
				MuiNotifyWritePacketField.Memory, packet.Memory.Raw);
	}

	private static bool IsPacket<TPlatform>(ref TPlatform platform,
		APTR message, uint size, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		header.MethodId == method && IsMapped(ref platform, message, size);

	private static bool IsMapped<TPlatform>(ref TPlatform platform,
		APTR message, uint size) where TPlatform : struct, IMuiGuestMemory =>
		message.IsNotNull && platform.IsMapped(message, size);
}

public static class MuiNotifyWriteCore
{
	public const uint WriteLongMethod = MuiNotifyWriteMessageCodec.WriteLongMethod;
	public const uint WriteStringMethod = MuiNotifyWriteMessageCodec.WriteStringMethod;
	public const uint MaximumStringLength = 4096;

	internal static bool TryReadWriteLong<TPlatform>(ref TPlatform platform,
		APTR message, out MuiWriteLongMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyWriteMessageCodec.TryReadWriteLong(ref platform, message,
			out packet);

	internal static bool TryReadWriteString<TPlatform>(ref TPlatform platform,
		APTR message, out MuiWriteStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyWriteMessageCodec.TryReadWriteString(ref platform, message,
			out packet);

	public static bool WriteLongRecord<TPlatform>(ref TPlatform platform,
		APTR message, uint value, APTR memory)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiWriteLongMessage);
		packet.MethodId = WriteLongMethod;
		packet.Value = value;
		packet.Memory = memory;
		return MuiNotifyWriteMessageCodec.TryWriteWriteLong(ref platform,
			message, packet);
	}

	public static bool WriteStringRecord<TPlatform>(ref TPlatform platform,
		APTR message, APTR source, APTR memory)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiWriteStringMessage);
		packet.MethodId = WriteStringMethod;
		packet.String = source;
		packet.Memory = memory;
		return MuiNotifyWriteMessageCodec.TryWriteWriteString(ref platform,
			message, packet);
	}

	// Struct-only native qualification seam. It proves both packet forms and
	// keeps the live object/store dispatcher out of the focused closure.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiNotifyWriteMessageCodec.TryReadMethod(ref platform, message,
			out var method)) return 0;
		switch (method)
		{
			case WriteLongMethod:
				return TryReadWriteLong(ref platform, message, out var writeLong) ?
					writeLong.Memory.Raw : 0;
			case WriteStringMethod:
				return TryReadWriteString(ref platform, message, out var writeString) ?
					writeString.Memory.Raw : 0;
		}
		return 0;
	}

	public static bool WriteLong<TPlatform>(ref TPlatform platform, uint value,
		APTR memory) where TPlatform : struct, IMuiGuestMemory
	{
		if (memory.IsNull || !platform.IsMapped(memory, 4)) return false;
		platform.WriteUInt32(memory, 0, value);
		return true;
	}

	public static bool WriteString<TPlatform>(ref TPlatform platform, APTR source,
		APTR memory) where TPlatform : struct, IMuiGuestMemory
	{
		if (source.IsNull || memory.IsNull ||
			!CStringCodec.TryReadLength(ref platform, source,
				MaximumStringLength, out var length))
			return false;
		var byteSize = length + 1;
		if (memory.Raw > uint.MaxValue - byteSize ||
			!platform.IsMapped(memory, byteSize)) return false;
		for (var index = 0u; index < byteSize; index++)
			platform.WriteUInt8(APTR.FromPointer(memory.Raw + index), 0,
				platform.ReadUInt8(APTR.FromPointer(source.Raw + index)));
		return true;
	}

}
