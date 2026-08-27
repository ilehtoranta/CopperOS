/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned String.mui contents pointer.
public struct MuiStringContentsState
{
	public APTR Contents;
}

// Guest-resident String contents state.  The pointer is backed by the
// object-owned StringCopyKey Dataspace entry after CopyContents completes.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringContentsStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D53434Eu; // 'MSCN'

	internal uint Magic;
	internal APTR Contents;
}

internal static class MuiStringContentsStateAdmission
{
	internal const int MaximumLength = 65536;

	internal static bool Validate(MuiStringContentsStateRecord value) =>
		value.Magic == MuiStringContentsStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringContentsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Contents.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Contents, MaximumLength, out _);
	}
}

internal enum MuiStringContentsStateField : byte
{
	Magic,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringContentsStateFieldCursor
{
	internal APTR Record;
	internal MuiStringContentsStateField Field;
}

internal static class MuiStringContentsStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringContentsStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringContentsStateField.Magic => 0,
			MuiStringContentsStateField.Contents => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringContentsStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringContentsStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringContentsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringContentsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringContentsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringContentsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Contents consumers use the named
// contents record; this bounded adapter is the only layer that translates its
// fixed guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiStringContentsStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringContentsStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringContentsStateRecord.Size)) return false;
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

internal static class MuiStringContentsStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringContentsStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiStringContentsStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var contents)) return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiStringContentsStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringContentsStateAdmission.Validate(value)) return false;
		return MuiStringContentsStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiStringContentsStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, value.Contents.Raw);
	}
}
