/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
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

// The packet records below are the public MorphOS operation boundary.  Keep
// their guest layout in small, named codecs rather than making the dispatcher
// select a field through the shared packet/field cursor above.  The numeric
// positions are therefore confined to these ABI adapters; callers work with
// the fixed-width structs declared in DirlistDispatcher.cs.
internal static class MuiDirlistPacketMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, uint packetSize, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (message.IsNull || offset > packetSize ||
			packetSize - offset < 4 || message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(message, packetSize) &&
			platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, uint packetSize, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packetSize, offset,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, uint packetSize, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packetSize, offset,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiDirlistMethodMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDirlistMethodMessage.Size, 0, out packet.MethodId);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistMethodMessage.Size, 0, method);
}

internal static class MuiDirlistSetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDirlistSetMessage.Size, 0, out packet.MethodId) &&
			MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDirlistSetMessage.Size, 4, out packet.Attribute) &&
			MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDirlistSetMessage.Size, 8, out packet.Value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistSetMessage.Size, 0, packet.MethodId) &&
			MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistSetMessage.Size, 4, packet.Attribute) &&
			MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistSetMessage.Size, 8, packet.Value);
	}
}

internal static class MuiDirlistRenameMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDirlistRenameMessage.Size, 0, out packet.MethodId) &&
			MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDirlistRenameMessage.Size, 4, out packet.Entry) &&
			MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDirlistRenameMessage.Size, 8, out packet.Name);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistRenameMessage.Size, 0, packet.MethodId) &&
			MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistRenameMessage.Size, 4, packet.Entry) &&
			MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistRenameMessage.Size, 8, packet.Name);
	}
}

internal static class MuiDirlistProtectionMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistProtectionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDirlistProtectionMessage.Size, 0, out packet.MethodId) &&
			MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDirlistProtectionMessage.Size, 4, out packet.Entry) &&
			MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDirlistProtectionMessage.Size, 8, out packet.Protection);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistProtectionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistProtectionMessage.Size, 0, packet.MethodId) &&
			MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistProtectionMessage.Size, 4, packet.Entry) &&
			MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistProtectionMessage.Size, 8, packet.Protection);
	}
}

internal static class MuiDirlistGetEntryMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
			MuiDirlistGetEntryMessage.Size, 0, out packet.MethodId) &&
			MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDirlistGetEntryMessage.Size, 4, out packet.Position) &&
			MuiDirlistPacketMemoryCodec.TryReadUInt32(ref platform, message,
				MuiDirlistGetEntryMessage.Size, 8, out packet.Storage);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
			MuiDirlistGetEntryMessage.Size, 0, packet.MethodId) &&
			MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistGetEntryMessage.Size, 4, packet.Position) &&
			MuiDirlistPacketMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiDirlistGetEntryMessage.Size, 8, packet.Storage);
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
		return TryReadMethodIdValue(ref platform, message, out packet.MethodId);
	}

	// Keep scalar selector admission at the guest ABI boundary; callers retain
	// the named method-header and operation records.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiDirlistMethodMessageCodec.TryRead(ref platform, message,
			out var packet))
		{
			methodId = 0;
			return false;
		}
		methodId = packet.MethodId;
		return true;
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
		return IsMethod(method) && MuiDirlistMethodMessageCodec.TryWrite(
			ref platform, message, method);
	}

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiDirlistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return IsSetMethod(method) && MuiDirlistSetMessageCodec.TryRead(
			ref platform, message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteSet<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint attribute, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSetMethod(method)) return false;
		var packet = default(MuiDirlistSetMessage);
		packet.MethodId = method;
		packet.Attribute = attribute;
		packet.Value = value;
		return MuiDirlistSetMessageCodec.TryWrite(ref platform, message, packet);
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
		return IsRenameMethod(method) && MuiDirlistRenameMessageCodec.TryRead(
			ref platform, message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteRename<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint entry, uint name)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsRenameMethod(method)) return false;
		var packet = default(MuiDirlistRenameMessage);
		packet.MethodId = method;
		packet.Entry = entry;
		packet.Name = name;
		return MuiDirlistRenameMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadProtection<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistProtectionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiDirlistProtectionMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == SetProtection;
	}

	internal static bool WriteProtection<TPlatform>(ref TPlatform platform,
		APTR message, uint entry, uint protection)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDirlistProtectionMessage);
		packet.MethodId = SetProtection;
		packet.Entry = entry;
		packet.Protection = protection;
		return MuiDirlistProtectionMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	internal static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiDirlistGetEntryMessageCodec.TryRead(ref platform, message,
			out packet) && packet.MethodId == ListGetEntry;
	}

	internal static bool WriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, uint position, uint storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiDirlistGetEntryMessage);
		packet.MethodId = ListGetEntry;
		packet.Position = position;
		packet.Storage = storage;
		return MuiDirlistGetEntryMessageCodec.TryWrite(ref platform, message,
			packet);
	}

	private static bool IsMethod(uint method) => method == ReRead ||
		method == ListClear;

	private static bool IsSetMethod(uint method) => method == Set ||
		method == NoNotifySet;

	private static bool IsRenameMethod(uint method) => method == Rename ||
		method == SetComment;

}
