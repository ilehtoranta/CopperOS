/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Semantic view of the Bodychunk.mui BODY decoding format.  The values stay
// ULONG-compatible with MorphOS, while the decoder consumes this named state
// instead of reaching into an anonymous collection of attribute slots.
public struct MuiBodychunkFormatState
{
	public uint Compression;
	public uint Depth;
	public uint Masking;
}

// Guest-resident format state retained in the object's Dataspace.  A compact
// record makes the lifetime and ABI visible without introducing managed state.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBodychunkFormatStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4D424643u; // 'MBFC'

	internal uint Magic;
	internal uint Compression;
	internal uint Depth;
	internal uint Masking;
}

internal enum MuiBodychunkFormatStateField : byte
{
	Magic,
	Compression,
	Depth,
	Masking,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBodychunkFormatStateFieldCursor
{
	internal APTR Record;
	internal MuiBodychunkFormatStateField Field;
}

internal static class MuiBodychunkFormatStateFieldCursorCodec
{
	private static bool TryResolve(MuiBodychunkFormatStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiBodychunkFormatStateField.Magic => 0,
			MuiBodychunkFormatStateField.Compression => 4,
			MuiBodychunkFormatStateField.Depth => 8,
			MuiBodychunkFormatStateField.Masking => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBodychunkFormatStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiBodychunkFormatStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBodychunkFormatStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBodychunkFormatStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiBodychunkFormatStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiBodychunkFormatStateRecord.Size) ||
			!MuiBodychunkFormatStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiBodychunkFormatStateField.Magic, out var magic) ||
			magic != MuiBodychunkFormatStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiBodychunkFormatStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBodychunkFormatStateField.Compression,
			out value.Compression) ||
			!MuiBodychunkFormatStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBodychunkFormatStateField.Depth, out value.Depth) ||
			!MuiBodychunkFormatStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBodychunkFormatStateField.Masking, out value.Masking))
			return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiBodychunkFormatStateRecord.Size) || value.Magic !=
			MuiBodychunkFormatStateRecord.Cookie) return false;
		return MuiBodychunkFormatStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBodychunkFormatStateField.Magic, value.Magic) &&
			MuiBodychunkFormatStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBodychunkFormatStateField.Compression,
			value.Compression) &&
			MuiBodychunkFormatStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBodychunkFormatStateField.Depth, value.Depth) &&
			MuiBodychunkFormatStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBodychunkFormatStateField.Masking, value.Masking);
	}
}
