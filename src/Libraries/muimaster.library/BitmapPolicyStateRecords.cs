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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint AlphaOffset = 4;
	internal const uint MappingTableOffset = 8;
	internal const uint PrecisionOffset = 12;
	internal const uint SourceColorsOffset = 16;
	internal const uint TransparentOffset = 20;
	internal const uint UseFriendOffset = 24;
	internal const uint Cookie = 0x4D42504Cu; // 'MBPL'

	internal uint Magic;
	internal uint Alpha;
	internal uint MappingTable;
	internal uint Precision;
	internal uint SourceColors;
	internal uint Transparent;
	internal uint UseFriend;
}

// Bitmap policy retains two caller-owned pointer attributes and one BOOL. The
// scalar Alpha/Precision/Transparent values keep their full MorphOS ULONG/LONG
// representation; pointer fields must remain readable at their ABI element
// width and UseFriend is a canonical BOOL before state crosses a consumer
// boundary.
internal static class MuiBitmapPolicyStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiBitmapPolicyStateRecord.Cookie &&
		(value.MappingTable == 0 || platform.IsMapped(
			APTR.FromPointer(value.MappingTable), 1)) &&
		(value.SourceColors == 0 || platform.IsMapped(
			APTR.FromPointer(value.SourceColors), 4)) &&
		value.UseFriend <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Bitmap policy consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiBitmapPolicyStateRecordMemoryCodec
{
	private static bool TryResolve(MuiBitmapPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiBitmapPolicyStateField.Magic:
				offset = MuiBitmapPolicyStateRecord.MagicOffset;
				return true;
			case MuiBitmapPolicyStateField.Alpha:
				offset = MuiBitmapPolicyStateRecord.AlphaOffset;
				return true;
			case MuiBitmapPolicyStateField.MappingTable:
				offset = MuiBitmapPolicyStateRecord.MappingTableOffset;
				return true;
			case MuiBitmapPolicyStateField.Precision:
				offset = MuiBitmapPolicyStateRecord.PrecisionOffset;
				return true;
			case MuiBitmapPolicyStateField.SourceColors:
				offset = MuiBitmapPolicyStateRecord.SourceColorsOffset;
				return true;
			case MuiBitmapPolicyStateField.Transparent:
				offset = MuiBitmapPolicyStateRecord.TransparentOffset;
				return true;
			case MuiBitmapPolicyStateField.UseFriend:
				offset = MuiBitmapPolicyStateRecord.UseFriendOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiBitmapPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiBitmapPolicyStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiBitmapPolicyStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Magic, out value.Magic) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Alpha, out value.Alpha) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.MappingTable,
			out value.MappingTable) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Precision, out value.Precision) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.SourceColors,
			out value.SourceColors) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Transparent,
			out value.Transparent) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiBitmapPolicyStateField.UseFriend, out value.UseFriend);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiBitmapPolicyStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBitmapPolicyStateAdmission.Validate(ref platform, value))
			return false;
		return MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Magic, value.Magic) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Alpha, value.Alpha) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.MappingTable, value.MappingTable) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Precision, value.Precision) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.SourceColors, value.SourceColors) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.Transparent, value.Transparent) &&
			MuiBitmapPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapPolicyStateField.UseFriend, value.UseFriend);
	}
}
