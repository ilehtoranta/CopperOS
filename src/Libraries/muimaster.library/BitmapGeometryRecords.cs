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
	private static bool TryResolve(MuiBitmapGeometryStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiBitmapGeometryStateField.Magic => 0,
			MuiBitmapGeometryStateField.Width => 4,
			MuiBitmapGeometryStateField.Height => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapGeometryStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiBitmapGeometryStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBitmapGeometryStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBitmapGeometryStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Bitmap geometry consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiBitmapGeometryStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiBitmapGeometryStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiBitmapGeometryStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
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
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiBitmapGeometryStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) &&
			MuiBitmapGeometryStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Width) &&
			MuiBitmapGeometryStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out value.Height);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiBitmapGeometryStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBitmapGeometryStateAdmission.Validate(value)) return false;
		return MuiBitmapGeometryStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiBitmapGeometryStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, value.Width) &&
			MuiBitmapGeometryStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 8, value.Height);
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
