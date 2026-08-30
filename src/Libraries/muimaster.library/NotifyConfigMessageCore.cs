/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetConfigItemMessage
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint ConfigIdOffset = 4;
	internal const uint StorageOffset = 8;
	internal uint MethodId;
	internal uint ConfigId;
	internal APTR Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetConfigItemMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal uint MethodId;
}

internal enum MuiGetConfigItemPacketField : byte
{
	MethodId,
	ConfigId,
	Storage,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetConfigItemPacketFieldCursor
{
	internal APTR Message;
	internal MuiGetConfigItemPacketField Field;
}

// Struct-first guest-memory adapter for the fixed MUIM_GetConfigItem records.
// The packet kind is inferred from the field, and the complete record span is
// validated before a member address is returned.
internal static class MuiGetConfigItemMessageMemoryCodec
{
	private static bool TryResolve(MuiGetConfigItemPacketField field,
		out uint offset, out uint size)
	{
		if (field == MuiGetConfigItemPacketField.MethodId)
		{
			offset = MuiGetConfigItemMethodMessage.MethodIdOffset;
			size = MuiGetConfigItemMethodMessage.Size;
			return true;
		}
		if (field == MuiGetConfigItemPacketField.ConfigId)
		{
			offset = MuiGetConfigItemMessage.ConfigIdOffset;
			size = MuiGetConfigItemMessage.Size;
			return true;
		}
		if (field == MuiGetConfigItemPacketField.Storage)
		{
			offset = MuiGetConfigItemMessage.StorageOffset;
			size = MuiGetConfigItemMessage.Size;
			return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiGetConfigItemMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// field cursor. The live message codec routes to the struct adapter above.
internal static class MuiGetConfigItemPacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGetConfigItemPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGetConfigItemMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGetConfigItemMessageMemoryCodec.TryReadUInt32(ref platform,
			message, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGetConfigItemMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, field, value);
}

// Central codec for the fixed MorphOS MUIM_GetConfigItem envelope. The public
// core below exposes the named record while this adapter owns guest offsets.
internal static class MuiGetConfigItemMessageCodec
{
	internal const uint Method = 0x80423EDB;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGetConfigItemMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Selector admission remains scalar for callers that only need MethodID, but
	// it is read from the named one-ULONG method record in declaration order.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiGetConfigItemMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMethodId) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		methodId = rawMethodId;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message,
			MuiGetConfigItemMessage.Size) ||
			!TryReadMethodIdValue(ref platform, message, out methodId) ||
			methodId != Method) return false;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiGetConfigItemMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var configId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawStorage) ||
			!MuiGuestStructCursor.IsComplete(cursor) ||
			rawMethodId != Method) return false;
		packet.MethodId = methodId;
		packet.ConfigId = configId;
		packet.Storage = APTR.FromPointer(rawStorage);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiGetConfigItemMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				Method) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.ConfigId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Storage.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Struct-first codec for the MorphOS MUIM_GetConfigItem packet. This small
// boundary stays separate from the broad Notify implementation so packet-only
// freestanding qualification does not import unrelated Notify methods.
public static class MuiNotifyConfigMessageCore
{
	public const uint GetConfigItemMethod = MuiGetConfigItemMessageCodec.Method;

	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint configId, APTR resultStorage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiGetConfigItemMessage);
		packet.MethodId = GetConfigItemMethod;
		packet.ConfigId = configId;
		packet.Storage = resultStorage;
		return MuiGetConfigItemMessageCodec.TryWrite(ref platform, storage,
			packet);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGetConfigItemMessageCodec.TryRead(ref platform, message,
			out packet);

	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, message, out var packet) ? packet.Storage.Raw : 0;
}
