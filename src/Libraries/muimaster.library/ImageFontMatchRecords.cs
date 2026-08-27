/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the optional Image.mui font-match string pointer.
public struct MuiImageFontMatchStringState
{
	public bool Present;
	public APTR MatchString;
}

// Guest-resident FontMatchString state. The pointer remains caller-owned; the
// record provides a named presence and lifetime boundary for the attribute.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageFontMatchStringStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D49464Du; // 'MIFM'

	internal uint Magic;
	internal uint Present;
	internal APTR MatchString;
}

internal static class MuiImageFontMatchStringStateAdmission
{
	internal const int MaximumLength = 128;

	internal static bool Validate(MuiImageFontMatchStringStateRecord value) =>
		value.Magic == MuiImageFontMatchStringStateRecord.Cookie &&
		value.Present <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiImageFontMatchStringStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.MatchString.IsNull || CStringCodec.TryReadLength(ref platform,
			value.MatchString, MaximumLength, out _);
	}
}

internal enum MuiImageFontMatchStringStateField : byte
{
	Magic,
	Present,
	MatchString,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageFontMatchStringStateFieldCursor
{
	internal APTR Record;
	internal MuiImageFontMatchStringStateField Field;
}

internal static class MuiImageFontMatchStringStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageFontMatchStringStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiImageFontMatchStringStateField.Magic => 0,
			MuiImageFontMatchStringStateField.Present => 4,
			MuiImageFontMatchStringStateField.MatchString => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageFontMatchStringStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiImageFontMatchStringStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStringStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageFontMatchStringStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStringStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageFontMatchStringStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Font-match consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiImageFontMatchStringStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiImageFontMatchStringStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiImageFontMatchStringStateRecord.Size)) return false;
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

internal static class MuiImageFontMatchStringStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageFontMatchStringStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiImageFontMatchStringStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 0, out value.Magic) ||
			!MuiImageFontMatchStringStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out value.Present) ||
			!MuiImageFontMatchStringStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 8, out var matchString)) return false;
		value.MatchString = APTR.FromPointer(matchString);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageFontMatchStringStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiImageFontMatchStringStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageFontMatchStringStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiImageFontMatchStringStateAdmission.Validate(value)) return false;
		return MuiImageFontMatchStringStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiImageFontMatchStringStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, value.Present) &&
			MuiImageFontMatchStringStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 8, value.MatchString.Raw);
	}
}
