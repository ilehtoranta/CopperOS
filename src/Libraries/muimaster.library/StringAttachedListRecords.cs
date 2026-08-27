/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui/Listview relationship.  The pointer remains
// caller-owned and is validated as a live Listview object by the control core;
// this record only gives the relationship a stable named ABI seam.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringAttachedListStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D53414Cu; // 'MSAL'

	internal uint Magic;
	internal APTR Listview;
}

internal static class MuiStringAttachedListStateAdmission
{
	internal static bool Validate(MuiStringAttachedListStateRecord value) =>
		value.Magic == MuiStringAttachedListStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringAttachedListStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Listview.IsNull ||
			MuiListCore.Classify(ref platform, state, value.Listview) ==
			MuiCollectionClass.Listview;
	}
}

internal enum MuiStringAttachedListStateField : byte
{
	Magic,
	Listview,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringAttachedListStateFieldCursor
{
	internal APTR Record;
	internal MuiStringAttachedListStateField Field;
}

internal static class MuiStringAttachedListStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringAttachedListStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringAttachedListStateField.Magic => 0,
			MuiStringAttachedListStateField.Listview => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringAttachedListStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringAttachedListStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAttachedListStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringAttachedListStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAttachedListStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringAttachedListStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. The named Listview relationship remains
// the semantic record; this bounded adapter owns the fixed guest translation.
// The cursor codec remains available for compatibility and malformed-state
// diagnostics.
internal static class MuiStringAttachedListStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringAttachedListStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringAttachedListStateRecord.Size)) return false;
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

internal static class MuiStringAttachedListStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringAttachedListStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringAttachedListStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiStringAttachedListStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var listview)) return false;
		value.Listview = APTR.FromPointer(listview);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringAttachedListStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiStringAttachedListStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringAttachedListStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringAttachedListStateAdmission.Validate(value)) return false;
		return MuiStringAttachedListStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiStringAttachedListStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, value.Listview.Raw);
	}
}
