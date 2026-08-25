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

internal static class MuiScrollgroupBorderScrollerStateFieldCursorCodec
{
	private static bool TryResolve(MuiScrollgroupBorderScrollerStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiScrollgroupBorderScrollerStateField.Magic:
			case MuiScrollgroupBorderScrollerStateField.Window:
			case MuiScrollgroupBorderScrollerStateField.UseWindowBorder:
			case MuiScrollgroupBorderScrollerStateField.HorizontalRequested:
			case MuiScrollgroupBorderScrollerStateField.VerticalRequested:
			case MuiScrollgroupBorderScrollerStateField.Applied:
			case MuiScrollgroupBorderScrollerStateField.Reserved:
				offset = (uint)field * 4;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupBorderScrollerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiScrollgroupBorderScrollerStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupBorderScrollerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiScrollgroupBorderScrollerStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupBorderScrollerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScrollgroupBorderScrollerStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScrollgroupBorderScrollerStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupBorderScrollerStateRecord.Size) ||
			!Read(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.Magic, out var magic) ||
			magic != MuiScrollgroupBorderScrollerStateRecord.Cookie ||
			!Read(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.Window, out var window) ||
			!Read(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.UseWindowBorder,
				out var useWindowBorder) ||
			!Read(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.HorizontalRequested,
				out var horizontalRequested) ||
			!Read(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.VerticalRequested,
				out var verticalRequested) ||
			!Read(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.Applied, out var applied) ||
			!Read(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.Reserved, out var reserved))
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

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupBorderScrollerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupBorderScrollerStateRecord.Size) || value.Magic !=
			MuiScrollgroupBorderScrollerStateRecord.Cookie) return false;
		return Write(ref platform, address,
			MuiScrollgroupBorderScrollerStateField.Magic, value.Magic) &&
			Write(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.Window, value.Window.Raw) &&
			Write(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.UseWindowBorder,
				value.UseWindowBorder) &&
			Write(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.HorizontalRequested,
				value.HorizontalRequested) &&
			Write(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.VerticalRequested,
				value.VerticalRequested) &&
			Write(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.Applied, value.Applied) &&
			Write(ref platform, address,
				MuiScrollgroupBorderScrollerStateField.Reserved, value.Reserved);
	}

	private static bool Read<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupBorderScrollerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiScrollgroupBorderScrollerStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, field, out value);

	private static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupBorderScrollerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiScrollgroupBorderScrollerStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, field, value);
}
