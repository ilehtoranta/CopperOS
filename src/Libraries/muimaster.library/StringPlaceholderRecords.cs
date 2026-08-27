/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

public struct MuiStringPlaceholderState
{
	public APTR Contents;
}

// Guest-resident String.mui placeholder state.  The pointer identifies the
// object-owned bounded C string produced by CopyContents; callers never need a
// private widget offset or a managed text copy to render it.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringPlaceholderStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D535048u; // 'MSPH'

	internal uint Magic;
	internal APTR Contents;
}

internal static class MuiStringPlaceholderStateAdmission
{
	internal const int MaximumLength = 128;

	internal static bool Validate(MuiStringPlaceholderStateRecord value) =>
		value.Magic == MuiStringPlaceholderStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringPlaceholderStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Contents.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Contents, MaximumLength, out _);
	}
}

internal enum MuiStringPlaceholderStateField : byte
{
	Magic,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringPlaceholderStateFieldCursor
{
	internal APTR Record;
	internal MuiStringPlaceholderStateField Field;
}

internal static class MuiStringPlaceholderStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringPlaceholderStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringPlaceholderStateField.Magic => 0,
			MuiStringPlaceholderStateField.Contents => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringPlaceholderStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringPlaceholderStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPlaceholderStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringPlaceholderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPlaceholderStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringPlaceholderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Placeholder consumers use the named
// contents record; this bounded adapter is the only layer that translates its
// fixed guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiStringPlaceholderStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringPlaceholderStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringPlaceholderStateRecord.Size)) return false;
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

internal static class MuiStringPlaceholderStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringPlaceholderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringPlaceholderStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 0, out value.Magic) ||
			!MuiStringPlaceholderStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var contents)) return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringPlaceholderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiStringPlaceholderStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringPlaceholderStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringPlaceholderStateAdmission.Validate(value)) return false;
		return MuiStringPlaceholderStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiStringPlaceholderStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, value.Contents.Raw);
	}
}
