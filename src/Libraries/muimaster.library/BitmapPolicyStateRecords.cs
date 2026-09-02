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
		if (field == MuiBitmapPolicyStateField.Magic)
			offset = MuiBitmapPolicyStateRecord.MagicOffset;
		else if (field == MuiBitmapPolicyStateField.Alpha)
			offset = MuiBitmapPolicyStateRecord.AlphaOffset;
		else if (field == MuiBitmapPolicyStateField.MappingTable)
			offset = MuiBitmapPolicyStateRecord.MappingTableOffset;
		else if (field == MuiBitmapPolicyStateField.Precision)
			offset = MuiBitmapPolicyStateRecord.PrecisionOffset;
		else if (field == MuiBitmapPolicyStateField.SourceColors)
			offset = MuiBitmapPolicyStateRecord.SourceColorsOffset;
		else if (field == MuiBitmapPolicyStateField.Transparent)
			offset = MuiBitmapPolicyStateRecord.TransparentOffset;
		else if (field == MuiBitmapPolicyStateField.UseFriend)
			offset = MuiBitmapPolicyStateRecord.UseFriendOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
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
		if (!MuiBitmapPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBitmapPolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiBitmapPolicyStateField.Alpha)
			value = state.Alpha;
		else if (field == MuiBitmapPolicyStateField.MappingTable)
			value = state.MappingTable;
		else if (field == MuiBitmapPolicyStateField.Precision)
			value = state.Precision;
		else if (field == MuiBitmapPolicyStateField.SourceColors)
			value = state.SourceColors;
		else if (field == MuiBitmapPolicyStateField.Transparent)
			value = state.Transparent;
		else if (field == MuiBitmapPolicyStateField.UseFriend)
			value = state.UseFriend;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBitmapPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBitmapPolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiBitmapPolicyStateField.Alpha)
			state.Alpha = value;
		else if (field == MuiBitmapPolicyStateField.MappingTable)
			state.MappingTable = value;
		else if (field == MuiBitmapPolicyStateField.Precision)
			state.Precision = value;
		else if (field == MuiBitmapPolicyStateField.SourceColors)
			state.SourceColors = value;
		else if (field == MuiBitmapPolicyStateField.Transparent)
			state.Transparent = value;
		else if (field == MuiBitmapPolicyStateField.UseFriend)
			state.UseFriend = value;
		else return false;
		return MuiBitmapPolicyStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiBitmapPolicyStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBitmapPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Alpha) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MappingTable) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Precision) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SourceColors) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Transparent) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UseFriend)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBitmapPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Alpha) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MappingTable) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Precision) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SourceColors) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Transparent) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseFriend) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiBitmapPolicyStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiBitmapPolicyStateAdmission.Validate(ref platform,
			value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
