/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

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

internal static class MuiCollectionBasicFieldCursorCodec
{
	private static bool TryResolve(MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCollectionBasicPacketKind.Method:
				if (field == MuiCollectionBasicField.MethodId) { offset = 0; return true; }
				break;
			case MuiCollectionBasicPacketKind.GetEntry:
				if (field == MuiCollectionBasicField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionBasicField.Position) { offset = 4; return true; }
				if (field == MuiCollectionBasicField.Storage) { offset = 8; return true; }
				break;
			case MuiCollectionBasicPacketKind.Select:
				if (field == MuiCollectionBasicField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionBasicField.Position) { offset = 4; return true; }
				if (field == MuiCollectionBasicField.Select) { offset = 8; return true; }
				if (field == MuiCollectionBasicField.Storage) { offset = 12; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionBasicFieldCursor cursor, out APTR address)
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
		APTR message, MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiCollectionBasicFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionBasicPacketKind packet,
		MuiCollectionBasicField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCollectionBasicFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionGetEntryMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != GetEntry) return false;
		packet.MethodId = header.MethodId;
		return MuiCollectionBasicFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionBasicPacketKind.GetEntry,
			MuiCollectionBasicField.Position, out packet.Position) &&
			MuiCollectionBasicFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionBasicPacketKind.GetEntry,
				MuiCollectionBasicField.Storage, out packet.Storage);
	}

	internal static bool WriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint position, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionGetEntryMessage.Size)) return false;
		return MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionBasicPacketKind.GetEntry,
			MuiCollectionBasicField.MethodId, GetEntry) &&
			MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionBasicPacketKind.GetEntry,
				MuiCollectionBasicField.Position, position) &&
			MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionBasicPacketKind.GetEntry,
				MuiCollectionBasicField.Storage, storage);
	}

	internal static bool TryReadSelect<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionSelectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionSelectMessage.Size) ||
			!TryReadMethodId(ref platform, message, out var header) ||
			header.MethodId != Select) return false;
		packet.MethodId = header.MethodId;
		return MuiCollectionBasicFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionBasicPacketKind.Select,
			MuiCollectionBasicField.Position, out packet.Position) &&
			MuiCollectionBasicFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionBasicPacketKind.Select,
				MuiCollectionBasicField.Select, out packet.Select) &&
			MuiCollectionBasicFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionBasicPacketKind.Select,
				MuiCollectionBasicField.Storage, out packet.Storage);
	}

	internal static bool WriteSelect<TPlatform>(ref TPlatform platform,
		APTR message, uint position, uint select, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionSelectMessage.Size)) return false;
		return MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionBasicPacketKind.Select,
			MuiCollectionBasicField.MethodId, Select) &&
			MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionBasicPacketKind.Select,
				MuiCollectionBasicField.Position, position) &&
			MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionBasicPacketKind.Select,
				MuiCollectionBasicField.Select, select) &&
			MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionBasicPacketKind.Select,
				MuiCollectionBasicField.Storage, storage);
	}

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodId(ref platform, message, out var header) ||
			!IsMethod(header.MethodId) || header.MethodId != method) return false;
		packet.MethodId = header.MethodId;
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionMethodMessage.Size)) return false;
		return MuiCollectionBasicFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionBasicPacketKind.Method,
			MuiCollectionBasicField.MethodId, out packet.MethodId);
	}

	// Native consumers that only need method validation use this scalar-return
	// form. It avoids materializing the one-field record in compiler paths where
	// a discarded out-struct would otherwise obscure the freestanding branch.
	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		IsMethod(header.MethodId) && header.MethodId == method;

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionMethodMessage.Size) || !IsMethod(method)) return false;
		return MuiCollectionBasicFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionBasicPacketKind.Method,
			MuiCollectionBasicField.MethodId, method);
	}

	private static bool IsMethod(uint method) => method == Clear || method == Sort;
}
