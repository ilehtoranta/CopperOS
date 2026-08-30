/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Scrollgroup policy is kept as one guest-resident record.  The public shape
// uses named fields for the pointer and BOOL values; the raw attribute list is
// only a compatibility/bootstrap seam.
public struct MuiScrollgroupPolicyState
{
	public APTR Contents;
	public uint FreeHorizontal;
	public uint FreeVertical;
	public APTR HorizontalBar;
	public APTR VerticalBar;
	public uint NoHorizontalBar;
	public uint NoVerticalBar;
	public uint AutoBars;
	public uint UseWindowBorder;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollgroupPolicyStateRecord
{
	internal const uint Size = 40;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ContentsOffset = 4;
	internal const uint FreeHorizontalOffset = 8;
	internal const uint FreeVerticalOffset = 12;
	internal const uint HorizontalBarOffset = 16;
	internal const uint VerticalBarOffset = 20;
	internal const uint NoHorizontalBarOffset = 24;
	internal const uint NoVerticalBarOffset = 28;
	internal const uint AutoBarsOffset = 32;
	internal const uint UseWindowBorderOffset = 36;
	internal const uint Cookie = 0x53504750u; // 'SPGP'

	internal uint Magic;
	internal APTR Contents;
	internal uint FreeHorizontal;
	internal uint FreeVertical;
	internal APTR HorizontalBar;
	internal APTR VerticalBar;
	internal uint NoHorizontalBar;
	internal uint NoVerticalBar;
	internal uint AutoBars;
	internal uint UseWindowBorder;
}

internal enum MuiScrollgroupPolicyStateField : byte
{
	Magic,
	Contents,
	FreeHorizontal,
	FreeVertical,
	HorizontalBar,
	VerticalBar,
	NoHorizontalBar,
	NoVerticalBar,
	AutoBars,
	UseWindowBorder,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollgroupPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiScrollgroupPolicyStateField Field;
}

internal static class MuiScrollgroupPolicyStateValidation
{
	private static bool IsValidPointer<TPlatform>(ref TPlatform platform,
		APTR pointer) where TPlatform : struct, IMuiGuestMemory => pointer.IsNull ||
		platform.IsMapped(pointer, 4);

	internal static bool IsValid<TPlatform>(ref TPlatform platform,
		MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiScrollgroupPolicyStateRecord.Cookie &&
		value.FreeHorizontal <= 1 && value.FreeVertical <= 1 &&
		value.NoHorizontalBar <= 1 && value.NoVerticalBar <= 1 &&
		value.AutoBars <= 1 && value.UseWindowBorder <= 1 &&
		IsValidPointer(ref platform, value.Contents) &&
		IsValidPointer(ref platform, value.HorizontalBar) &&
		IsValidPointer(ref platform, value.VerticalBar);
}

internal static class MuiScrollgroupPolicyStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiScrollgroupPolicyStateValidation.IsValid(ref platform, value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiScrollgroupPolicyStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Pointer and BOOL policy values remain
// named semantic fields; bounded fixed guest-layout translation lives here.
internal static class MuiScrollgroupPolicyStateRecordMemoryCodec
{
	private static bool TryResolve(MuiScrollgroupPolicyStateField field,
		out uint offset)
	{
		if (field == MuiScrollgroupPolicyStateField.Magic)
			offset = MuiScrollgroupPolicyStateRecord.MagicOffset;
		else if (field == MuiScrollgroupPolicyStateField.Contents)
			offset = MuiScrollgroupPolicyStateRecord.ContentsOffset;
		else if (field == MuiScrollgroupPolicyStateField.FreeHorizontal)
			offset = MuiScrollgroupPolicyStateRecord.FreeHorizontalOffset;
		else if (field == MuiScrollgroupPolicyStateField.FreeVertical)
			offset = MuiScrollgroupPolicyStateRecord.FreeVerticalOffset;
		else if (field == MuiScrollgroupPolicyStateField.HorizontalBar)
			offset = MuiScrollgroupPolicyStateRecord.HorizontalBarOffset;
		else if (field == MuiScrollgroupPolicyStateField.VerticalBar)
			offset = MuiScrollgroupPolicyStateRecord.VerticalBarOffset;
		else if (field == MuiScrollgroupPolicyStateField.NoHorizontalBar)
			offset = MuiScrollgroupPolicyStateRecord.NoHorizontalBarOffset;
		else if (field == MuiScrollgroupPolicyStateField.NoVerticalBar)
			offset = MuiScrollgroupPolicyStateRecord.NoVerticalBarOffset;
		else if (field == MuiScrollgroupPolicyStateField.AutoBars)
			offset = MuiScrollgroupPolicyStateRecord.AutoBarsOffset;
		else if (field == MuiScrollgroupPolicyStateField.UseWindowBorder)
			offset = MuiScrollgroupPolicyStateRecord.UseWindowBorderOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiScrollgroupPolicyStateRecord.Size) &&
			platform.IsMapped(address, MuiScrollgroupPolicyStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScrollgroupPolicyStateRecordCodec
{
	// Declaration-order guest record: magic, content handle, policy BOOLs,
	// bar handles, and remaining policy BOOLs.  APTRs are transported as raw
	// ULONGs only at this cursor boundary; callers use the named record fields.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiScrollgroupPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var contents) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FreeHorizontal) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FreeVertical) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var horizontalBar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var verticalBar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.NoHorizontalBar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.NoVerticalBar) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.AutoBars) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UseWindowBorder)) return false;
		value.Contents = APTR.FromPointer(contents);
		value.HorizontalBar = APTR.FromPointer(horizontalBar);
		value.VerticalBar = APTR.FromPointer(verticalBar);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiScrollgroupPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Contents.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FreeHorizontal) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FreeVertical) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HorizontalBar.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.VerticalBar.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NoHorizontalBar) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NoVerticalBar) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AutoBars) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseWindowBorder) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiScrollgroupPolicyStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScrollgroupPolicyStateAdmission.Validate(ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
