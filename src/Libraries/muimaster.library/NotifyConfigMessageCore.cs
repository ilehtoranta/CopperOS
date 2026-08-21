/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetConfigItemMessage
{
	internal const uint Size = 12;
	internal uint MethodId;
	internal uint ConfigId;
	internal APTR Storage;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGetConfigItemMethodMessage
{
	internal const uint Size = 4;
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

internal static class MuiGetConfigItemPacketFieldCursorCodec
{
	private static bool TryResolve(MuiGetConfigItemPacketField field,
		out uint offset)
	{
		if (field == MuiGetConfigItemPacketField.MethodId) { offset = 0; return true; }
		if (field == MuiGetConfigItemPacketField.ConfigId) { offset = 4; return true; }
		if (field == MuiGetConfigItemPacketField.Storage) { offset = 8; return true; }
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGetConfigItemPacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGetConfigItemPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemPacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGetConfigItemPacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiGetConfigItemMethodMessage.Size)) return false;
		return MuiGetConfigItemPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiGetConfigItemPacketField.MethodId, out packet.MethodId);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiGetConfigItemMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != Method) return false;
		if (!MuiGetConfigItemPacketFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiGetConfigItemPacketField.ConfigId, out packet.ConfigId) ||
			!MuiGetConfigItemPacketFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiGetConfigItemPacketField.Storage,
				out var rawStorage)) return false;
		packet.MethodId = header.MethodId;
		packet.Storage = APTR.FromPointer(rawStorage);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiGetConfigItemMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiGetConfigItemMessage.Size)) return false;
		return MuiGetConfigItemPacketFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiGetConfigItemPacketField.MethodId, Method) &&
			MuiGetConfigItemPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiGetConfigItemPacketField.ConfigId, packet.ConfigId) &&
			MuiGetConfigItemPacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiGetConfigItemPacketField.Storage,
				packet.Storage.Raw);
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
