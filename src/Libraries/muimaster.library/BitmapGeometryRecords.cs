/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared semantic geometry for Bitmap.mui and Bodychunk.mui.  Width and
// Height remain MorphOS ULONG-compatible while layout and decoding consume
// one named value rather than separate anonymous attribute reads.
public struct MuiBitmapGeometryState
{
	public uint Width;
	public uint Height;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapGeometryStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint WidthOffset = 4;
	internal const uint HeightOffset = 8;
	internal const uint Cookie = 0x4D424759u; // 'MBGY'

	internal uint Magic;
	internal uint Width;
	internal uint Height;
}

internal enum MuiBitmapGeometryStateField : byte
{
	Magic,
	Width,
	Height,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapGeometryStateFieldCursor
{
	internal APTR Record;
	internal MuiBitmapGeometryStateField Field;
}

internal static class MuiBitmapGeometryStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapGeometryStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapGeometryStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiBitmapGeometryStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapGeometryStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Bitmap geometry consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiBitmapGeometryStateRecordMemoryCodec
{
	private static bool TryResolve(MuiBitmapGeometryStateField field,
		out uint offset)
	{
		if (field == MuiBitmapGeometryStateField.Magic)
			offset = MuiBitmapGeometryStateRecord.MagicOffset;
		else if (field == MuiBitmapGeometryStateField.Width)
			offset = MuiBitmapGeometryStateRecord.WidthOffset;
		else if (field == MuiBitmapGeometryStateField.Height)
			offset = MuiBitmapGeometryStateRecord.HeightOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiBitmapGeometryStateRecord.Size) &&
			platform.IsMapped(address, MuiBitmapGeometryStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiBitmapGeometryStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBitmapGeometryStateField.Magic)
			value = state.Magic;
		else if (field == MuiBitmapGeometryStateField.Width)
			value = state.Width;
		else if (field == MuiBitmapGeometryStateField.Height)
			value = state.Height;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBitmapGeometryStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBitmapGeometryStateField.Magic)
			state.Magic = value;
		else if (field == MuiBitmapGeometryStateField.Width)
			state.Width = value;
		else if (field == MuiBitmapGeometryStateField.Height)
			state.Height = value;
		else return false;
		return MuiBitmapGeometryStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	// Legacy raw-offset adapter retained for bounded diagnostics and older
	// callers. Typed field access above is the preferred API.
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiBitmapGeometryStateRecord.Size -
			MuiBitmapGeometryStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiBitmapGeometryStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiBitmapGeometryStateRecord.FieldSize);
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

internal static class MuiBitmapGeometryStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBitmapGeometryStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Height)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBitmapGeometryStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Width) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Height) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiBitmapGeometryStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiBitmapGeometryStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiBitmapGeometryStateAdmission
{
	internal static bool Validate(MuiBitmapGeometryStateRecord value) =>
		value.Magic == MuiBitmapGeometryStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
