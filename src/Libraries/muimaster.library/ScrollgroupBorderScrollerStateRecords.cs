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
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiScrollgroupBorderScrollerStateField.Magic,
			out var magic) ||
			!MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiScrollgroupBorderScrollerStateField.Window,
			out var window) ||
			!MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiScrollgroupBorderScrollerStateField.UseWindowBorder,
			out var useWindowBorder) ||
			!MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiScrollgroupBorderScrollerStateField.HorizontalRequested,
			out var horizontalRequested) ||
			!MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiScrollgroupBorderScrollerStateField.VerticalRequested,
			out var verticalRequested) ||
			!MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiScrollgroupBorderScrollerStateField.Applied,
			out var applied) ||
			!MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiScrollgroupBorderScrollerStateField.Reserved,
			out var reserved))
			return false;
		value.Magic = magic;
		value.Window = APTR.FromPointer(window);
		value.UseWindowBorder = useWindowBorder;
		value.HorizontalRequested = horizontalRequested;
		value.VerticalRequested = verticalRequested;
		value.Applied = applied;
		value.Reserved = reserved;
		return true;
	}

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
		return MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiScrollgroupBorderScrollerStateField.Magic,
			value.Magic) &&
			MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiScrollgroupBorderScrollerStateField.Window,
			value.Window.Raw) &&
			MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiScrollgroupBorderScrollerStateField.UseWindowBorder,
			value.UseWindowBorder) &&
			MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiScrollgroupBorderScrollerStateField.HorizontalRequested,
			value.HorizontalRequested) &&
			MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiScrollgroupBorderScrollerStateField.VerticalRequested,
			value.VerticalRequested) &&
			MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiScrollgroupBorderScrollerStateField.Applied,
			value.Applied) &&
			MuiScrollgroupBorderScrollerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiScrollgroupBorderScrollerStateField.Reserved,
			value.Reserved);
	}
}
