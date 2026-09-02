/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the caller-owned graphics.library Image pointer.
public struct MuiImageOldImageState
{
	public APTR Image;
}

// Guest-resident Image.mui OldImage state. The pointer remains caller-owned;
// this record only gives the object a named, validated state boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageOldImageStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ImageOffset = 4;
	internal const uint Cookie = 0x4D494F49u; // 'MIOI'

	internal uint Magic;
	internal APTR Image;
}

internal enum MuiImageOldImageStateField : byte
{
	Magic,
	Image,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageOldImageStateFieldCursor
{
	internal APTR Record;
	internal MuiImageOldImageStateField Field;
}

internal static class MuiImageOldImageStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageOldImageStateField field,
		out uint offset)
	{
		if (field == MuiImageOldImageStateField.Magic)
			offset = MuiImageOldImageStateRecord.MagicOffset;
		else if (field == MuiImageOldImageStateField.Image)
			offset = MuiImageOldImageStateRecord.ImageOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageOldImageStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiImageOldImageStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiImageOldImageStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageOldImageStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageOldImageStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageOldImageStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageOldImageStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Image consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiImageOldImageStateRecordMemoryCodec
{
	private static bool TryResolve(MuiImageOldImageStateField field,
		out uint offset)
	{
		if (field == MuiImageOldImageStateField.Magic)
			offset = MuiImageOldImageStateRecord.MagicOffset;
		else if (field == MuiImageOldImageStateField.Image)
			offset = MuiImageOldImageStateRecord.ImageOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageOldImageStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageOldImageStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiImageOldImageStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiImageOldImageStateField.Magic)
			value = state.Magic;
		else if (field == MuiImageOldImageStateField.Image)
			value = state.Image.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageOldImageStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiImageOldImageStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiImageOldImageStateField.Magic)
			state.Magic = value;
		else if (field == MuiImageOldImageStateField.Image)
			state.Image = APTR.FromPointer(value);
		else return false;
		return MuiImageOldImageStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiImageOldImageStateRecord.Size -
			MuiImageOldImageStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiImageOldImageStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiImageOldImageStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiImageOldImageStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageOldImageStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var image)) return false;
		value.Image = APTR.FromPointer(image);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageOldImageStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Image.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiImageOldImageStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiImageOldImageStateAdmission.Validate(ref platform, value) &&
		WriteRecord(ref platform, address, value);
}

internal static class MuiImageOldImageStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiImageOldImageStateRecord.Cookie &&
		(value.Image.IsNull || platform.IsMapped(value.Image, 4));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
