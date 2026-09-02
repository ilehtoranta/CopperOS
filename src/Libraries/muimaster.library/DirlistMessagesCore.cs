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

// Struct-first guest-memory adapter for the fixed Dirlist packet family.
// Header probing admits the 4-byte method record; operation fields require
// their complete named packet record before being accessed.
internal static class MuiDirlistMessageMemoryCodec
{
	private static bool TryResolve(MuiDirlistPacketKind packet,
		MuiDirlistField field, out uint offset, out uint packetSize)
	{
		packetSize = 0;
		switch (packet)
		{
			case MuiDirlistPacketKind.Method:
				if (field == MuiDirlistField.MethodId)
				{
					offset = MuiDirlistMethodMessage.MethodIdOffset;
					packetSize = MuiDirlistMethodMessage.Size;
					return true;
				}
				break;
			case MuiDirlistPacketKind.Set:
				packetSize = MuiDirlistSetMessage.Size;
				if (field == MuiDirlistField.MethodId) { offset = MuiDirlistSetMessage.MethodIdOffset; return true; }
				if (field == MuiDirlistField.Attribute) { offset = MuiDirlistSetMessage.AttributeOffset; return true; }
				if (field == MuiDirlistField.Value) { offset = MuiDirlistSetMessage.ValueOffset; return true; }
				break;
			case MuiDirlistPacketKind.Rename:
				packetSize = MuiDirlistRenameMessage.Size;
				if (field == MuiDirlistField.MethodId) { offset = MuiDirlistRenameMessage.MethodIdOffset; return true; }
				if (field == MuiDirlistField.Entry) { offset = MuiDirlistRenameMessage.EntryOffset; return true; }
				if (field == MuiDirlistField.Name) { offset = MuiDirlistRenameMessage.NameOffset; return true; }
				break;
			case MuiDirlistPacketKind.Protection:
				packetSize = MuiDirlistProtectionMessage.Size;
				if (field == MuiDirlistField.MethodId) { offset = MuiDirlistProtectionMessage.MethodIdOffset; return true; }
				if (field == MuiDirlistField.Entry) { offset = MuiDirlistProtectionMessage.EntryOffset; return true; }
				if (field == MuiDirlistField.Protection) { offset = MuiDirlistProtectionMessage.ProtectionOffset; return true; }
				break;
			case MuiDirlistPacketKind.GetEntry:
				packetSize = MuiDirlistGetEntryMessage.Size;
				if (field == MuiDirlistField.MethodId) { offset = MuiDirlistGetEntryMessage.MethodIdOffset; return true; }
				if (field == MuiDirlistField.Position) { offset = MuiDirlistGetEntryMessage.PositionOffset; return true; }
				if (field == MuiDirlistField.Storage) { offset = MuiDirlistGetEntryMessage.StorageOffset; return true; }
				break;
		}
		offset = 0;
		packetSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistPacketKind packet, MuiDirlistField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset, out var packetSize) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, packetSize))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiDirlistMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistPacketKind packet, MuiDirlistField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistPacketKind packet, MuiDirlistField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that construct the typed field
// cursor; live packet handling routes through the named-record adapter above.
internal static class MuiDirlistFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDirlistFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDirlistMessageMemoryCodec.TryGetAddress(ref platform, cursor.Message,
			cursor.Packet, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistPacketKind packet, MuiDirlistField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDirlistMessageMemoryCodec.TryReadUInt32(ref platform, message, packet,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistPacketKind packet, MuiDirlistField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDirlistMessageMemoryCodec.TryWriteUInt32(ref platform, message, packet,
			field, value);
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

// Sequential codecs for the fixed Dirlist/Volumelist operation records.  The
// packet-memory adapter above is retained for compatibility diagnostics, but
// production packet handling consumes these declaration-ordered named fields.
internal static class MuiDirlistMessageStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMethodHeaderCodec.TryReadValue(ref platform, message,
			out methodId);

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteMethodIdValue<TPlatform>(ref TPlatform platform,
		APTR message, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMethodHeaderCodec.WriteValue(ref platform, message, methodId);

	internal static bool TryReadMethod<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryReadMethodIdValue(ref platform, message, out packet.MethodId))
			return false;
		return true;
	}

	internal static bool TryWriteMethod<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMethodHeaderCodec.WriteValue(ref platform, message, method);

	internal static bool TryReadSet<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDirlistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Attribute) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Value) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryWriteSet<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDirlistSetMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Attribute) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRename<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDirlistRenameMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Name) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryWriteRename<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDirlistRenameMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Entry) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Name)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadProtection<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistProtectionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDirlistProtectionMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Protection) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryWriteProtection<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistProtectionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDirlistProtectionMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Entry) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Protection)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDirlistGetEntryMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Position) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Storage) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool TryWriteGetEntry<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, message,
			MuiDirlistGetEntryMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.MethodId) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Position) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				packet.Storage)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiDirlistMethodMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistMethodMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryReadMethod(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, uint method)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDirlistMessageStructCodec.TryWriteMethod(ref platform, message,
			method);
}

internal static class MuiDirlistSetMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryReadSet(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistSetMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryWriteSet(ref platform, message,
			packet);
}

internal static class MuiDirlistRenameMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryReadRename(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistRenameMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryWriteRename(ref platform, message,
			packet);
}

internal static class MuiDirlistProtectionMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistProtectionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryReadProtection(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistProtectionMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryWriteProtection(ref platform, message,
			packet);
}

internal static class MuiDirlistGetEntryMessageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR message, out MuiDirlistGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryReadGetEntry(ref platform, message,
			out packet);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR message, MuiDirlistGetEntryMessage packet)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiDirlistMessageStructCodec.TryWriteGetEntry(ref platform, message,
			packet);
}

// Struct-first codec for the method-only Dirlist/Volumelist header. The
// named one-ULONG record remains the ABI contract; shared guest storage keeps
// selector admission free of direct scalar lowering in freestanding 68k code.
internal static class MuiDirlistMethodHeaderCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		methodId = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
				out methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint methodId)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistMethodMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress) ||
			!MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
				methodId)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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
		return MuiDirlistMessageStructCodec.TryReadMethodIdValue(ref platform,
			message, out methodId);
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
