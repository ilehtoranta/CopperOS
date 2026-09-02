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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ListviewOffset = 4;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringAttachedListStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringAttachedListStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAttachedListStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringAttachedListStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAttachedListStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringAttachedListStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter. The named Listview relationship remains
// the semantic record; this bounded adapter owns the fixed guest translation.
// The cursor codec remains available for compatibility and malformed-state
// diagnostics.
internal static class MuiStringAttachedListStateRecordMemoryCodec
{
	private static bool TryResolve(MuiStringAttachedListStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringAttachedListStateField.Magic:
				offset = MuiStringAttachedListStateRecord.MagicOffset;
				return true;
			case MuiStringAttachedListStateField.Listview:
				offset = MuiStringAttachedListStateRecord.ListviewOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAttachedListStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringAttachedListStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringAttachedListStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAttachedListStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiStringAttachedListStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringAttachedListStateField.Magic)
			value = state.Magic;
		else if (field == MuiStringAttachedListStateField.Listview)
			value = state.Listview.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAttachedListStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringAttachedListStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringAttachedListStateField.Magic)
			state.Magic = value;
		else if (field == MuiStringAttachedListStateField.Listview)
			state.Listview = APTR.FromPointer(value);
		else return false;
		return MuiStringAttachedListStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}
}

internal static class MuiStringAttachedListStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringAttachedListStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringAttachedListStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var listview) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Listview = APTR.FromPointer(listview);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringAttachedListStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringAttachedListStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringAttachedListStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringAttachedListStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
		=> MuiStringAttachedListStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringAttachedListStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringAttachedListStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Listview.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringAttachedListStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
