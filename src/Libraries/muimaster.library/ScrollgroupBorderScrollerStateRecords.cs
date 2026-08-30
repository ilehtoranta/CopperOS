/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Scrollgroup border-scroller routing is retained as one named guest record.
// The layout core owns the policy decision; the Window object remains the
// source of the three public border-scroller attributes used at open time.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiScrollgroupBorderScrollerStateRecord
{
	public const uint Size = 28;
	public const uint FieldSize = 4;
	public const uint MagicOffset = 0;
	public const uint WindowOffset = 4;
	public const uint UseWindowBorderOffset = 8;
	public const uint HorizontalRequestedOffset = 12;
	public const uint VerticalRequestedOffset = 16;
	public const uint AppliedOffset = 20;
	public const uint ReservedOffset = 24;
	public const uint Cookie = 0x53474252u; // 'SGBR'

	public uint Magic;
	public APTR Window;
	public uint UseWindowBorder;
	public uint HorizontalRequested;
	public uint VerticalRequested;
	public uint Applied;
	public uint Reserved;
}

internal enum MuiScrollgroupBorderScrollerStateField : byte
{
	Magic,
	Window,
	UseWindowBorder,
	HorizontalRequested,
	VerticalRequested,
	Applied,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollgroupBorderScrollerStateFieldCursor
{
	internal APTR Record;
	internal MuiScrollgroupBorderScrollerStateField Field;
}

internal static class MuiScrollgroupBorderScrollerStateValidation
{
	internal static bool IsValid<TPlatform>(ref TPlatform platform,
		MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiScrollgroupBorderScrollerStateRecord.Cookie &&
		(value.Window.IsNull || platform.IsMapped(value.Window, 4)) &&
		value.UseWindowBorder <= 1 && value.HorizontalRequested <= 1 &&
		value.VerticalRequested <= 1 && value.Applied <= 1 &&
		value.Reserved == 0;
}

internal static class MuiScrollgroupBorderScrollerStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiScrollgroupBorderScrollerStateValidation.IsValid(ref platform, value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj,
		MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiScrollgroupBorderScrollerStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupBorderScrollerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupBorderScrollerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupBorderScrollerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter. Window and border-scroller policy remain
// named semantic fields; bounded fixed guest-layout translation lives here.
internal static class MuiScrollgroupBorderScrollerStateRecordMemoryCodec
{
	private static bool TryResolve(MuiScrollgroupBorderScrollerStateField field,
		out uint offset)
	{
		if (field == MuiScrollgroupBorderScrollerStateField.Magic)
			offset = MuiScrollgroupBorderScrollerStateRecord.MagicOffset;
		else if (field == MuiScrollgroupBorderScrollerStateField.Window)
			offset = MuiScrollgroupBorderScrollerStateRecord.WindowOffset;
		else if (field == MuiScrollgroupBorderScrollerStateField.UseWindowBorder)
			offset = MuiScrollgroupBorderScrollerStateRecord.UseWindowBorderOffset;
		else if (field == MuiScrollgroupBorderScrollerStateField.HorizontalRequested)
			offset = MuiScrollgroupBorderScrollerStateRecord.HorizontalRequestedOffset;
		else if (field == MuiScrollgroupBorderScrollerStateField.VerticalRequested)
			offset = MuiScrollgroupBorderScrollerStateRecord.VerticalRequestedOffset;
		else if (field == MuiScrollgroupBorderScrollerStateField.Applied)
			offset = MuiScrollgroupBorderScrollerStateRecord.AppliedOffset;
		else if (field == MuiScrollgroupBorderScrollerStateField.Reserved)
			offset = MuiScrollgroupBorderScrollerStateRecord.ReservedOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupBorderScrollerStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiScrollgroupBorderScrollerStateRecord.Size) &&
			platform.IsMapped(address, MuiScrollgroupBorderScrollerStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupBorderScrollerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupBorderScrollerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScrollgroupBorderScrollerStateRecordCodec
{
	// Declaration-order guest record: magic, Window, border policy flags, and
	// the reserved ULONG. APTR transport is kept at this named-struct cursor
	// boundary; the field adapter is retained only for diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiScrollgroupBorderScrollerStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var window) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UseWindowBorder) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HorizontalRequested) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.VerticalRequested) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Applied) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Reserved)) return false;
		value.Window = APTR.FromPointer(window);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiScrollgroupBorderScrollerStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Window.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseWindowBorder) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HorizontalRequested) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.VerticalRequested) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Applied) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Reserved) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiScrollgroupBorderScrollerStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScrollgroupBorderScrollerStateAdmission.Validate(ref platform,
			value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
