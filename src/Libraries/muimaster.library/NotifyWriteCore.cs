/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS Notify superclass packets for the two bounded memory-write helpers.
// The guest ABI is represented as named records so callers do not duplicate
// packet offsets at each dispatch site.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNotifyWriteMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWriteLongMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint ValueOffset = 4;
	internal const uint MemoryOffset = 8;
	internal uint MethodId;
	internal uint Value;
	internal APTR Memory;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWriteStringMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint StringOffset = 4;
	internal const uint MemoryOffset = 8;
	internal uint MethodId;
	internal APTR String;
	internal APTR Memory;
}

internal static class MuiNotifyWriteMethodMessageCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNotifyWriteMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		uint methodId;
		if (!MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			address, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.MethodId,
			MuiNotifyWriteMethodMessage.Size, out methodId)) return false;
		value.MethodId = methodId;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNotifyWriteMethodMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.MethodId,
			MuiNotifyWriteMethodMessage.Size, value.MethodId);
	}
}

internal static class MuiWriteLongMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWriteLongMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWriteLongMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Value) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMemory)) return false;
		value.Memory = APTR.FromPointer(rawMemory);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWriteLongMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWriteLongMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Memory.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiWriteStringMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWriteStringMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWriteStringMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawString) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMemory)) return false;
		value.String = APTR.FromPointer(rawString);
		value.Memory = APTR.FromPointer(rawMemory);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWriteStringMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWriteStringMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.String.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Memory.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNotifyWritePacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyWritePacketMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Packet, cursor.Field,
			MuiNotifyWritePacketMemoryCodec.FullSize(cursor.Packet), out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform, message,
			packet, field, MuiNotifyWritePacketMemoryCodec.FullSize(packet),
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNotifyWritePacketMemoryCodec.TryWriteUInt32(ref platform, message,
			packet, field, MuiNotifyWritePacketMemoryCodec.FullSize(packet),
			value);
}

// Struct-first guest-memory adapter for both fixed Notify write envelopes.
// The packet kind selects the corresponding named record and size; callers
// only receive validated field addresses inside a complete admitted packet.
internal static class MuiNotifyWritePacketMemoryCodec
{
	internal static uint FullSize(MuiNotifyWritePacketKind packet) =>
		packet == MuiNotifyWritePacketKind.WriteLong ? MuiWriteLongMessage.Size :
		packet == MuiNotifyWritePacketKind.WriteString ? MuiWriteStringMessage.Size :
		0;

	private static bool TryResolve(MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, out uint offset)
	{
		offset = packet == MuiNotifyWritePacketKind.WriteLong ?
			field == MuiNotifyWritePacketField.MethodId ? MuiWriteLongMessage.MethodIdOffset :
			field == MuiNotifyWritePacketField.Value ? MuiWriteLongMessage.ValueOffset :
			field == MuiNotifyWritePacketField.Memory ? MuiWriteLongMessage.MemoryOffset :
			uint.MaxValue :
			packet == MuiNotifyWritePacketKind.WriteString ?
			field == MuiNotifyWritePacketField.MethodId ? MuiWriteStringMessage.MethodIdOffset :
			field == MuiNotifyWritePacketField.String ? MuiWriteStringMessage.StringOffset :
			field == MuiNotifyWritePacketField.Memory ? MuiWriteStringMessage.MemoryOffset :
			uint.MaxValue : uint.MaxValue;
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, uint availableSize, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		var size = FullSize(packet);
		if (size == 0 || !TryResolve(packet, field, out var offset) ||
			message.IsNull || availableSize < offset || availableSize - offset <
			MuiNotifyWriteMethodMessage.FieldSize ||
			message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, availableSize)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiNotifyWriteMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, uint availableSize, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field, availableSize,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiNotifyWritePacketKind packet,
		MuiNotifyWritePacketField field, uint availableSize, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field, availableSize,
			out var address)) return false;
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

	// Selector admission remains a scalar ABI seam; callers that need the
	// complete header use the named method-header record below.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (message.IsNotNull && platform.IsMapped(message,
			MuiWriteLongMessage.Size) &&
			MuiWriteLongMessageCodec.TryRead(ref platform, message,
				out var completeLong))
		{
			methodId = completeLong.MethodId;
			return true;
		}
		return MuiNotifyWritePacketMemoryCodec.TryReadUInt32(ref platform,
			message, MuiNotifyWritePacketKind.WriteLong,
			MuiNotifyWritePacketField.MethodId, MuiNotifyWriteMethodMessage.Size,
			out methodId);
	}

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiNotifyWriteMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
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
		if (!MuiWriteLongMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != WriteLongMethod)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryReadWriteString<TPlatform>(ref TPlatform platform,
		APTR message, out MuiWriteStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWriteStringMessageCodec.TryRead(ref platform, message,
			out packet) || packet.MethodId != WriteStringMethod)
		{
			packet = default;
			return false;
		}
		return true;
	}

	internal static bool TryWriteWriteLong<TPlatform>(ref TPlatform platform,
		APTR message, MuiWriteLongMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet.MethodId != WriteLongMethod) return false;
		return MuiWriteLongMessageCodec.Write(ref platform, message, packet);
	}

	internal static bool TryWriteWriteString<TPlatform>(ref TPlatform platform,
		APTR message, MuiWriteStringMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet.MethodId != WriteStringMethod) return false;
		return MuiWriteStringMessageCodec.Write(ref platform, message, packet);
	}

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
		// The focused freestanding seam can admit either complete record without
		// a separate selector call; the live dispatcher still selects by method
		// ID before invoking the corresponding typed reader.
		if (TryReadWriteLong(ref platform, message, out var writeLong))
			return writeLong.Memory.Raw;
		if (TryReadWriteString(ref platform, message, out var writeString))
			return writeString.Memory.Raw;
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
