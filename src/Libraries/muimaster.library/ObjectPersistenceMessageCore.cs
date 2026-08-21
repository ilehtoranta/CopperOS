/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiObjectPersistenceMessage
{
	internal const uint Size = 8;
	internal uint MethodId;
	internal APTR Dataspace;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiObjectPersistenceMethodMessage
{
	internal const uint Size = 4;
	internal uint MethodId;
}

internal enum MuiObjectPersistencePacketField : byte
{
	MethodId,
	Dataspace,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiObjectPersistencePacketFieldCursor
{
	internal APTR Message;
	internal MuiObjectPersistencePacketField Field;
}

internal static class MuiObjectPersistencePacketFieldCursorCodec
{
	private static bool TryResolve(MuiObjectPersistencePacketField field,
		out uint offset)
	{
		if (field == MuiObjectPersistencePacketField.MethodId) { offset = 0; return true; }
		if (field == MuiObjectPersistencePacketField.Dataspace) { offset = 4; return true; }
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiObjectPersistencePacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Message.IsNull ||
			cursor.Message.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectPersistencePacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiObjectPersistencePacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectPersistencePacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiObjectPersistencePacketFieldCursor);
		cursor.Message = message;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Central codec for the fixed MorphOS Export/Import packet pair. The public
// core below consumes the named record; only this adapter carries guest
// offsets and packet mapping checks.
internal static class MuiObjectPersistenceMessageCodec
{
	internal const uint ExportMethod = 0x80420F1C;
	internal const uint ImportMethod = 0x8042D012;

	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiObjectPersistenceMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiObjectPersistenceMethodMessage.Size)) return false;
		return MuiObjectPersistencePacketFieldCursorCodec.TryReadUInt32(
			ref platform, message, MuiObjectPersistencePacketField.MethodId,
			out packet.MethodId);
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

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiObjectPersistenceMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if ((method != ExportMethod && method != ImportMethod) ||
			message.IsNull || !platform.IsMapped(message,
			MuiObjectPersistenceMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != method) return false;
		if (!MuiObjectPersistencePacketFieldCursorCodec.TryReadUInt32(
			ref platform, message, MuiObjectPersistencePacketField.Dataspace,
			out var rawDataspace)) return false;
		packet.MethodId = header.MethodId;
		packet.Dataspace = APTR.FromPointer(rawDataspace);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method, MuiObjectPersistenceMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((method != ExportMethod && method != ImportMethod) ||
			message.IsNull || !platform.IsMapped(message,
			MuiObjectPersistenceMessage.Size)) return false;
		return MuiObjectPersistencePacketFieldCursorCodec.TryWriteUInt32(
			ref platform, message, MuiObjectPersistencePacketField.MethodId,
			method) &&
			MuiObjectPersistencePacketFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiObjectPersistencePacketField.Dataspace,
				packet.Dataspace.Raw);
	}
}

// Struct-first codec for the MorphOS MUIM_Export/MUIM_Import packet pair.
public static class MuiObjectPersistenceMessageCore
{
	public const uint ExportMethod = MuiObjectPersistenceMessageCodec.ExportMethod;
	public const uint ImportMethod = MuiObjectPersistenceMessageCodec.ImportMethod;

	public static bool WriteExportRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR dataspace) where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, storage, ExportMethod, dataspace);

	public static bool WriteImportRecord<TPlatform>(ref TPlatform platform,
		APTR storage, APTR dataspace) where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, storage, ImportMethod, dataspace);

	private static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint method, APTR dataspace)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiObjectPersistenceMessage);
		packet.MethodId = method;
		packet.Dataspace = dataspace;
		return MuiObjectPersistenceMessageCodec.TryWrite(ref platform, storage,
			method, packet);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		uint method, out MuiObjectPersistenceMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiObjectPersistenceMessageCodec.TryRead(ref platform, message,
			method, out packet);

	// Packet-only native qualification seam. The dataspace pointer is returned
	// as the observable decoded guest token; live ownership remains in the
	// existing persistence core.
	public static uint DispatchRecord<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiObjectPersistenceMessageCodec.TryReadMethod(ref platform,
			message, out var method)) return 0;
		return TryRead(ref platform, message, method, out var packet) ?
			packet.Dataspace.Raw : 0;
	}
}
