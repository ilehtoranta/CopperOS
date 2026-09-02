/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Scrollbar group geometry state.  The fields retain MorphOS ULONG
// semantics while child construction, layout, and drawing consume one named
// value instead of repeatedly decoding the Group/Scrollbar attributes.
public struct MuiScrollbarLayoutState
{
	public uint Horizontal;
	public uint Type;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollbarLayoutStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HorizontalOffset = 4;
	internal const uint TypeOffset = 8;
	internal const uint Cookie = 0x4D534C59u; // 'MSLY'

	internal uint Magic;
	internal uint Horizontal;
	internal uint Type;
}

internal enum MuiScrollbarLayoutStateField : byte
{
	Magic,
	Horizontal,
	Type,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollbarLayoutStateFieldCursor
{
	internal APTR Record;
	internal MuiScrollbarLayoutStateField Field;
}

internal static class MuiScrollbarLayoutStateFieldCursorCodec
{
	private static bool TryResolve(MuiScrollbarLayoutStateField field,
		out uint offset)
	{
		if (field == MuiScrollbarLayoutStateField.Magic)
			offset = MuiScrollbarLayoutStateRecord.MagicOffset;
		else if (field == MuiScrollbarLayoutStateField.Horizontal)
			offset = MuiScrollbarLayoutStateRecord.HorizontalOffset;
		else if (field == MuiScrollbarLayoutStateField.Type)
			offset = MuiScrollbarLayoutStateRecord.TypeOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollbarLayoutStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiScrollbarLayoutStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiScrollbarLayoutStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiScrollbarLayoutStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScrollbarLayoutStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Horizontal and Type remain named
// semantic fields; fixed guest-layout translation is bounded to this adapter.
internal static class MuiScrollbarLayoutStateRecordMemoryCodec
{
	private static bool TryResolve(MuiScrollbarLayoutStateField field,
		out uint offset)
	{
		if (field == MuiScrollbarLayoutStateField.Magic)
			offset = MuiScrollbarLayoutStateRecord.MagicOffset;
		else if (field == MuiScrollbarLayoutStateField.Horizontal)
			offset = MuiScrollbarLayoutStateRecord.HorizontalOffset;
		else if (field == MuiScrollbarLayoutStateField.Type)
			offset = MuiScrollbarLayoutStateRecord.TypeOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiScrollbarLayoutStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiScrollbarLayoutStateField.Magic)
			value = state.Magic;
		else if (field == MuiScrollbarLayoutStateField.Horizontal)
			value = state.Horizontal;
		else if (field == MuiScrollbarLayoutStateField.Type)
			value = state.Type;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScrollbarLayoutStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiScrollbarLayoutStateField.Magic)
			state.Magic = value;
		else if (field == MuiScrollbarLayoutStateField.Horizontal)
			state.Horizontal = value;
		else if (field == MuiScrollbarLayoutStateField.Type)
			state.Type = value;
		else return false;
		return MuiScrollbarLayoutStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiScrollbarLayoutStateRecord.Size -
			MuiScrollbarLayoutStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiScrollbarLayoutStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiScrollbarLayoutStateRecord.FieldSize);
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

internal static class MuiScrollbarLayoutStateRecordCodec
{
	// Declaration-order guest record: { Magic, Horizontal, Type }.  Keep the
	// production boundary cursor-based so callers exchange the named record,
	// while the field adapter remains available for malformed-state probes.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiScrollbarLayoutStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Horizontal) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Type) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiScrollbarLayoutStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Horizontal) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Type) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiScrollbarLayoutStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScrollbarLayoutStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

// Keep the Scrollbar layout wire record lossless for malformed-state
// diagnostics, but admit only MorphOS's canonical Group_Horiz BOOL and the
// documented default/bottom/top/symmetric/none type values.
internal static class MuiScrollbarLayoutStateAdmission
{
	internal static bool Validate(MuiScrollbarLayoutStateRecord value)
	{
		if (value.Magic != MuiScrollbarLayoutStateRecord.Cookie) return false;
		var state = default(MuiScrollbarLayoutState);
		state.Horizontal = value.Horizontal;
		state.Type = value.Type;
		return Validate(state);
	}

	internal static bool Validate(MuiScrollbarLayoutState value) =>
		value.Horizontal <= 1 && value.Type <= 4;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing layout-only call sites. New Scrollbar
// state boundaries use MuiScrollbarLayoutStateAdmission directly so live
// ownership is explicit.
internal static class MuiScrollbarLayoutStateValidation
{
	internal static bool IsValidRecord(MuiScrollbarLayoutStateRecord value) =>
		MuiScrollbarLayoutStateAdmission.Validate(value);

	internal static bool IsValidState(MuiScrollbarLayoutState value) =>
		MuiScrollbarLayoutStateAdmission.Validate(value);
}
