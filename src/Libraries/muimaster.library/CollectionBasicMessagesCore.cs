/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.CompilerServices;

namespace CopperOS.MuiMaster;

// Central codec for the fixed MorphOS 3.20 List basic packet family. The List
// core consumes named GetEntry/Select/method records; only this adapter owns
// their packed guest-memory boundaries and method validation.
internal enum MuiCollectionBasicPacketKind : byte
{
	Method,
	GetEntry,
	Select,
}

internal enum MuiCollectionBasicField : byte
{
	MethodId,
	Position,
	Storage,
	Select,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionBasicFieldCursor
{
	internal APTR Message;
	internal MuiCollectionBasicPacketKind Packet;
	internal MuiCollectionBasicField Field;
}

// Named packet adapters keep the MorphOS List basic records as structs at the
// consumer boundary. Only this memory layer translates their packed fields.
internal static class MuiCollectionBasicMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiCollectionBasicPacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiCollectionBasicPacketKind.Method:
				size = MuiCollectionMethodMessage.Size;
				return true;
			case MuiCollectionBasicPacketKind.GetEntry:
				size = MuiCollectionGetEntryMessage.Size;
				return true;
			case MuiCollectionBasicPacketKind.Select:
				size = MuiCollectionSelectMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryResolve(MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCollectionBasicPacketKind.Method:
				if (field == MuiCollectionBasicField.MethodId)
				{
					offset = MuiCollectionMethodMessage.MethodIdOffset;
					return true;
				}
				break;
			case MuiCollectionBasicPacketKind.GetEntry:
				if (field == MuiCollectionBasicField.MethodId)
				{
					offset = MuiCollectionGetEntryMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiCollectionBasicField.Position)
				{
					offset = MuiCollectionGetEntryMessage.PositionOffset;
					return true;
				}
				if (field == MuiCollectionBasicField.Storage)
				{
					offset = MuiCollectionGetEntryMessage.StorageOffset;
					return true;
				}
				break;
			case MuiCollectionBasicPacketKind.Select:
				if (field == MuiCollectionBasicField.MethodId)
				{
					offset = MuiCollectionSelectMessage.MethodIdOffset;
					return true;
				}
				if (field == MuiCollectionBasicField.Position)
				{
					offset = MuiCollectionSelectMessage.PositionOffset;
					return true;
				}
				if (field == MuiCollectionBasicField.Select)
				{
					offset = MuiCollectionSelectMessage.SelectOffset;
					return true;
				}
				if (field == MuiCollectionBasicField.Storage)
				{
					offset = MuiCollectionSelectMessage.StorageOffset;
					return true;
				}
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionBasicFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor.Message, cursor.Packet,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset) ||
			!TryGetPacketSize(packet, out var packetSize) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, packetSize))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiCollectionMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiCollectionBasicPacketKind.Method)
		{
			if (!MuiCollectionBasicStructPacketCodec.TryReadMethodRecord(
				ref platform, message, out var method)) return false;
			if (field != MuiCollectionBasicField.MethodId) return false;
			value = method.MethodId;
			return true;
		}
		if (packet == MuiCollectionBasicPacketKind.GetEntry)
		{
			if (!MuiCollectionBasicStructPacketCodec.TryReadGetEntry(ref platform,
				message, out var getEntry)) return false;
			if (field == MuiCollectionBasicField.MethodId)
				value = getEntry.MethodId;
			else if (field == MuiCollectionBasicField.Position)
				value = getEntry.Position;
			else if (field == MuiCollectionBasicField.Storage)
				value = getEntry.Storage;
			else return false;
			return true;
		}
		if (packet == MuiCollectionBasicPacketKind.Select)
		{
			if (!MuiCollectionBasicStructPacketCodec.TryReadSelect(ref platform,
				message, out var select)) return false;
			if (field == MuiCollectionBasicField.MethodId)
				value = select.MethodId;
			else if (field == MuiCollectionBasicField.Position)
				value = select.Position;
			else if (field == MuiCollectionBasicField.Select)
				value = select.Select;
			else if (field == MuiCollectionBasicField.Storage)
				value = select.Storage;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiCollectionBasicPacketKind.Method)
		{
			if (!MuiCollectionBasicStructPacketCodec.TryReadMethodRecord(
				ref platform, message, out var method) ||
				field != MuiCollectionBasicField.MethodId) return false;
			method.MethodId = value;
			return MuiCollectionBasicStructPacketCodec.TryWriteMethodRecord(
				ref platform, message, ref method);
		}
		if (packet == MuiCollectionBasicPacketKind.GetEntry)
		{
			if (!MuiCollectionBasicStructPacketCodec.TryReadGetEntry(ref platform,
				message, out var getEntry)) return false;
			if (field == MuiCollectionBasicField.MethodId)
				getEntry.MethodId = value;
			else if (field == MuiCollectionBasicField.Position)
				getEntry.Position = value;
			else if (field == MuiCollectionBasicField.Storage)
				getEntry.Storage = value;
			else return false;
			return MuiCollectionBasicStructPacketCodec.TryWriteGetEntry(ref platform,
				message, getEntry);
		}
		if (packet == MuiCollectionBasicPacketKind.Select)
		{
			if (!MuiCollectionBasicStructPacketCodec.TryReadSelect(ref platform,
				message, out var select)) return false;
			if (field == MuiCollectionBasicField.MethodId)
				select.MethodId = value;
			else if (field == MuiCollectionBasicField.Position)
				select.Position = value;
			else if (field == MuiCollectionBasicField.Select)
				select.Select = value;
			else if (field == MuiCollectionBasicField.Storage)
				select.Storage = value;
			else return false;
			return MuiCollectionBasicStructPacketCodec.TryWriteSelect(ref platform,
				message, select);
		}
		return false;
	}
}

internal static class MuiCollectionBasicFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionBasicFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionBasicMessageMemoryCodec.TryGetAddress(ref platform, cursor,
			out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionBasicMessageMemoryCodec.TryReadUInt32(ref platform, message,
			packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionBasicMessageMemoryCodec.TryWriteUInt32(ref platform, message,
			packet, field, value);
}

// Live List packet transport is declaration-order based.  The public packet
// declarations are named structs in CollectionDispatcher.cs; this bounded
// codec owns their sequential guest-memory exchange.  The field/offset layer
// above remains available only to compatibility and malformed-packet probes.
internal static class MuiCollectionBasicStructPacketCodec
{
	private static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR message, uint size, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message, size,
			out cursor);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.TryReadValue(ref platform, message,
			out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.WriteValue(ref platform, message, methodId);

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodValue(ref platform, message, out var methodId))
			return false;
		packet.MethodId = methodId;
		return true;
	}

	internal static bool TryReadMethodRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionMethodMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteMethodRecord<TPlatform>(ref TPlatform platform,
		APTR message, ref MuiCollectionMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryCreate(ref platform, message, MuiCollectionMethodMessage.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteMethodValue(ref platform, message, method);

	internal static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionGetEntryMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Position) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Storage) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionGetEntryMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Position) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Storage) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadSelect<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionSelectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionSelectMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Position) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Select) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Storage) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteSelect<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionSelectMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionSelectMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Position) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Select) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Storage) && MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiCollectionBasicMessageCodec
{
	internal const uint Clear = 0x8042AD89u;
	internal const uint GetEntry = 0x804280ECu;
	internal const uint Select = 0x804252D8u;
	internal const uint Sort = 0x80422275u;

	internal static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionBasicStructPacketCodec.TryReadGetEntry(ref platform,
			message, out packet) && packet.MethodId == GetEntry;
	}

	internal static bool WriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint position, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionGetEntryMessage);
		packet.MethodId = GetEntry;
		packet.Position = position;
		packet.Storage = storage;
		return MuiCollectionBasicStructPacketCodec.TryWriteGetEntry(ref platform,
			message, packet);
	}

	internal static bool TryReadSelect<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionSelectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionBasicStructPacketCodec.TryReadSelect(ref platform,
			message, out packet) && packet.MethodId == Select;
	}

	internal static bool WriteSelect<TPlatform>(ref TPlatform platform,
		APTR message, uint position, uint select, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionSelectMessage);
		packet.MethodId = Select;
		packet.Position = position;
		packet.Select = select;
		packet.Storage = storage;
		return MuiCollectionBasicStructPacketCodec.TryWriteSelect(ref platform,
			message, packet);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId) ||
			!IsMethod(methodId) || methodId != method) return false;
		packet.MethodId = methodId;
		return true;
	}

	// Read the common fixed method header without constraining the selector.
	// Collection dispatchers use this named record for method selection, while
	// method-specific codecs continue to validate their complete packet shape.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		uint methodId;
		if (!TryReadMethodIdValue(ref platform, message, out methodId)) return false;
		packet.MethodId = methodId;
		return true;
	}

	// Keep the dispatcher-facing method header as a named struct, but provide a
	// scalar admission path for native closures that only need to validate the
	// selector before decoding a larger named packet. Packed field knowledge
	// remains confined to the cursor codec above.
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiCollectionBasicStructPacketCodec.TryReadMethodValue(ref platform,
			message, out methodId);
	}

	// Native consumers that only need method validation use this scalar-return
	// form. It avoids materializing the one-field record in compiler paths where
	// a discarded out-struct would otherwise obscure the freestanding branch.
	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodIdValue(ref platform, message, out var methodId) &&
		IsMethod(methodId) && methodId == method;

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		return IsMethod(method) &&
			MuiCollectionBasicStructPacketCodec.TryWriteMethod(ref platform,
				message, method);
	}

	private static bool IsMethod(uint method) => method == Clear || method == Sort;
}
