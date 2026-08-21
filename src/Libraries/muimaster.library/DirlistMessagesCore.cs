/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Central codec for the fixed MorphOS 3.20 Dirlist.mui/Volumelist.mui
// packets. The dispatcher consumes the named records declared next to its
// public surface; only this adapter owns their packed guest-memory layout.
internal enum MuiDirlistPacketKind : byte
{
	Method,
	Set,
	Rename,
	Protection,
	GetEntry,
}

internal enum MuiDirlistField : byte
{
	MethodId,
	Attribute,
	Value,
	Entry,
	Name,
	Protection,
	Position,
	Storage,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistFieldCursor
{
	internal APTR Message;
	internal MuiDirlistPacketKind Packet;
	internal MuiDirlistField Field;
}

internal static class MuiDirlistFieldCursorCodec
{
	private static bool TryResolve(MuiDirlistPacketKind packet,
		MuiDirlistField field, out uint offset)
	{
		switch (packet)
		{
			case MuiDirlistPacketKind.Method:
				if (field == MuiDirlistField.MethodId) { offset = 0; return true; }
				break;
			case MuiDirlistPacketKind.Set:
				if (field == MuiDirlistField.MethodId) { offset = 0; return true; }
				if (field == MuiDirlistField.Attribute) { offset = 4; return true; }
				if (field == MuiDirlistField.Value) { offset = 8; return true; }
				break;
			case MuiDirlistPacketKind.Rename:
				if (field == MuiDirlistField.MethodId) { offset = 0; return true; }
				if (field == MuiDirlistField.Entry) { offset = 4; return true; }
				if (field == MuiDirlistField.Name) { offset = 8; return true; }
				break;
			case MuiDirlistPacketKind.Protection:
				if (field == MuiDirlistField.MethodId) { offset = 0; return true; }
				if (field == MuiDirlistField.Entry) { offset = 4; return true; }
				if (field == MuiDirlistField.Protection) { offset = 8; return true; }
				break;
			case MuiDirlistPacketKind.GetEntry:
				if (field == MuiDirlistField.MethodId) { offset = 0; return true; }
				if (field == MuiDirlistField.Position) { offset = 4; return true; }
				if (field == MuiDirlistField.Storage) { offset = 8; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDirlistFieldCursor cursor, out APTR address)
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
		APTR message, MuiDirlistPacketKind packet, MuiDirlistField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiDirlistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistPacketKind packet, MuiDirlistField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiDirlistFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiDirlistMessageCodec
{
	internal const uint ReRead = 0x80422d71u;
	internal const uint Rename = 0x8042d336u;
	internal const uint SetComment = 0x8042b378u;
	internal const uint SetProtection = 0x804202bbu;
	internal const uint Set = 0x8042549Au;
	internal const uint NoNotifySet = 0x8042216Fu;
	internal const uint ListGetEntry = 0x804280ECu;
	internal const uint ListClear = 0x8042AD89u;

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiDirlistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsMethod(method) || !TryReadMethodId(ref platform, message,
			out var header) || header.MethodId != method) return false;
		packet.MethodId = header.MethodId;
		return true;
	}

	// Read the fixed Dirlist method header without constraining the selector.
	// Dispatcher selection uses this named record; method-specific codecs retain
	// validation of the complete packet shape.
	internal static bool TryReadMethodId<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiDirlistMethodMessage.Size)) return false;
		return MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiDirlistPacketKind.Method, MuiDirlistField.MethodId,
			out packet.MethodId);
	}

	// Keep method-only validation scalar for native roots and dispatcher switch
	// arms: materializing a one-field out record can obscure the freestanding
	// branch in some MC68000 compiler paths.
	internal static bool IsValidMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		IsMethod(method) && TryReadMethodId(ref platform, message,
			out var header) && header.MethodId == method;

	internal static bool WriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsMethod(method) || message.IsNull || !platform.IsMapped(message,
			MuiDirlistMethodMessage.Size)) return false;
		return MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistPacketKind.Method, MuiDirlistField.MethodId, method);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiDirlistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsSetMethod(method) || !IsPacket(ref platform, message,
			MuiDirlistSetMessage.Size, method)) return false;
		packet.MethodId = method;
		return MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiDirlistPacketKind.Set, MuiDirlistField.Attribute,
			out packet.Attribute) &&
			MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiDirlistPacketKind.Set, MuiDirlistField.Value, out packet.Value);
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiDirlistSetMessage.Size)) return false;
		return MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistPacketKind.Set, MuiDirlistField.MethodId, method) &&
			MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistPacketKind.Set, MuiDirlistField.Attribute, attribute) &&
			MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistPacketKind.Set, MuiDirlistField.Value, value);
	}

	internal static bool TryReadRename<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRename(ref platform, message, Rename, out packet);

	internal static bool TryReadRename<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiDirlistRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsRenameMethod(method) || !IsPacket(ref platform, message,
			MuiDirlistRenameMessage.Size, method)) return false;
		packet.MethodId = method;
		return MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiDirlistPacketKind.Rename, MuiDirlistField.Entry,
			out packet.Entry) &&
			MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiDirlistPacketKind.Rename, MuiDirlistField.Name,
				out packet.Name);
	}

	internal static bool WriteRename<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint entry, uint name)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsRenameMethod(method) || message.IsNull || !platform.IsMapped(
			message, MuiDirlistRenameMessage.Size)) return false;
		return MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistPacketKind.Rename, MuiDirlistField.MethodId, method) &&
			MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistPacketKind.Rename, MuiDirlistField.Entry, entry) &&
			MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistPacketKind.Rename, MuiDirlistField.Name, name);
	}

	internal static bool TryReadProtection<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistProtectionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDirlistProtectionMessage.Size,
			SetProtection)) return false;
		packet.MethodId = SetProtection;
		return MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiDirlistPacketKind.Protection, MuiDirlistField.Entry,
			out packet.Entry) &&
			MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiDirlistPacketKind.Protection, MuiDirlistField.Protection,
				out packet.Protection);
	}

	internal static bool WriteProtection<TPlatform>(ref TPlatform platform,
		APTR message, uint entry, uint protection)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiDirlistProtectionMessage.Size)) return false;
		return MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistPacketKind.Protection, MuiDirlistField.MethodId,
			SetProtection) &&
			MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistPacketKind.Protection, MuiDirlistField.Entry, entry) &&
			MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistPacketKind.Protection, MuiDirlistField.Protection,
				protection);
	}

	internal static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!IsPacket(ref platform, message, MuiDirlistGetEntryMessage.Size,
			ListGetEntry)) return false;
		packet.MethodId = ListGetEntry;
		return MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
			MuiDirlistPacketKind.GetEntry, MuiDirlistField.Position,
			out packet.Position) &&
			MuiDirlistFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiDirlistPacketKind.GetEntry, MuiDirlistField.Storage,
				out packet.Storage);
	}

	internal static bool WriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint position, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiDirlistGetEntryMessage.Size)) return false;
		return MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistPacketKind.GetEntry, MuiDirlistField.MethodId,
			ListGetEntry) &&
			MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistPacketKind.GetEntry, MuiDirlistField.Position, position) &&
			MuiDirlistFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistPacketKind.GetEntry, MuiDirlistField.Storage, storage);
	}

	private static bool IsMethod(uint method) => method == ReRead ||
		method == ListClear;

	private static bool IsSetMethod(uint method) => method == Set ||
		method == NoNotifySet;

	private static bool IsRenameMethod(uint method) => method == Rename ||
		method == SetComment;

	private static bool IsPacket<TPlatform>(ref TPlatform platform,
		APTR message, uint size, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadMethodId(ref platform, message, out var header) &&
		header.MethodId == method && platform.IsMapped(message, size);
}
