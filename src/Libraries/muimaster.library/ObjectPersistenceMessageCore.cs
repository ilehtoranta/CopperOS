/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiObjectPersistenceMessage
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint DataspaceOffset = 4;
	internal uint MethodId;
	internal APTR Dataspace;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiObjectPersistenceMethodMessage
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
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

// The fixed Export/Import records own their packed positions in this bounded
// adapter. Live consumers use it directly; the typed cursor remains only for
// compatibility callers and adapter-focused tests.
internal static class MuiObjectPersistenceMessageMemoryCodec
{
	private static bool TryResolve(MuiObjectPersistencePacketField field,
		out uint offset, out uint size)
	{
		if (field == MuiObjectPersistencePacketField.MethodId)
		{
			offset = MuiObjectPersistenceMethodMessage.MethodIdOffset;
			size = MuiObjectPersistenceMethodMessage.Size;
			return true;
		}
		if (field == MuiObjectPersistencePacketField.Dataspace)
		{
			offset = MuiObjectPersistenceMessage.DataspaceOffset;
			size = MuiObjectPersistenceMessage.Size;
			return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectPersistencePacketField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) || message.IsNull ||
			message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, size)) return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiObjectPersistenceMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectPersistencePacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectPersistencePacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiObjectPersistencePacketFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiObjectPersistencePacketFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiObjectPersistenceMessageMemoryCodec.TryGetAddress(ref platform,
			cursor.Message, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectPersistencePacketField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiObjectPersistenceMessageMemoryCodec.TryReadUInt32(ref platform, message,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiObjectPersistencePacketField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiObjectPersistenceMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, field, value);
}

// Sequential codecs for the fixed Export/Import records. The legacy field
// adapter remains available for compatibility diagnostics; production paths
// consume these declaration-ordered named structs.
internal static class MuiObjectPersistenceMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiObjectPersistenceMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out methodId) || !MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR message,
		out MuiObjectPersistenceMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiObjectPersistenceMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawDataspace) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		packet.Dataspace = APTR.FromPointer(rawDataspace);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method, APTR dataspace)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiObjectPersistenceMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, method) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				dataspace.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	// Keep native selector admission scalar while the named method record remains
	// the dispatcher-facing ABI type. Packed offsets stay inside this codec.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiObjectPersistenceMessageStructCodec.TryReadMethodIdValue(
			ref platform, message, out methodId);

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		method = 0;
		if (!TryReadMethodIdValue(ref platform, message, out method)) return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiObjectPersistenceMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != ExportMethod && method != ImportMethod)
		{
			packet = default;
			return false;
		}
		return MuiObjectPersistenceMessageStructCodec.TryRead(ref platform,
			message, out packet) && packet.MethodId == method;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method, MuiObjectPersistenceMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> (method == ExportMethod || method == ImportMethod) &&
			MuiObjectPersistenceMessageStructCodec.TryWrite(ref platform, message,
				method, packet.Dataspace);
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
