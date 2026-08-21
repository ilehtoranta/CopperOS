/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of Bitmap.mui's policy/source scalars. Pointer-valued
// fields remain raw guest addresses; no managed bitmap or palette wrapper is
// introduced at the ABI boundary.
public struct MuiBitmapPolicyState
{
	public uint Alpha;
	public uint MappingTable;
	public uint Precision;
	public uint SourceColors;
	public uint Transparent;
	public uint UseFriend;
}

// Guest-resident Bitmap-only policy state. Bodychunk format has a separate
// named record because these attributes do not belong to the Bodychunk class.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapPolicyStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x4D42504Cu; // 'MBPL'

	internal uint Magic;
	internal uint Alpha;
	internal uint MappingTable;
	internal uint Precision;
	internal uint SourceColors;
	internal uint Transparent;
	internal uint UseFriend;
}

internal enum MuiBitmapPolicyStateField : byte
{
	Magic,
	Alpha,
	MappingTable,
	Precision,
	SourceColors,
	Transparent,
	UseFriend,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiBitmapPolicyStateField Field;
}

internal static class MuiBitmapPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiBitmapPolicyStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiBitmapPolicyStateField.Magic => 0,
			MuiBitmapPolicyStateField.Alpha => 4,
			MuiBitmapPolicyStateField.MappingTable => 8,
			MuiBitmapPolicyStateField.Precision => 12,
			MuiBitmapPolicyStateField.SourceColors => 16,
			MuiBitmapPolicyStateField.Transparent => 20,
			MuiBitmapPolicyStateField.UseFriend => 24,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiBitmapPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBitmapPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBitmapPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiBitmapPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiBitmapPolicyStateRecord.Size) ||
			!MuiBitmapPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiBitmapPolicyStateField.Magic, out var magic) ||
			magic != MuiBitmapPolicyStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiBitmapPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Alpha, out value.Alpha) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.MappingTable,
			out value.MappingTable) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Precision, out value.Precision) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.SourceColors,
			out value.SourceColors) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Transparent,
			out value.Transparent) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.UseFriend, out value.UseFriend);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiBitmapPolicyStateRecord.Size) || value.Magic !=
			MuiBitmapPolicyStateRecord.Cookie) return false;
		return MuiBitmapPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Magic, value.Magic) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Alpha, value.Alpha) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.MappingTable,
			value.MappingTable) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Precision, value.Precision) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.SourceColors,
			value.SourceColors) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Transparent,
			value.Transparent) &&
			MuiBitmapPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.UseFriend, value.UseFriend);
	}
}
