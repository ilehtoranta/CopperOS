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

internal static class MuiScrollgroupPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiScrollgroupPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiScrollgroupPolicyStateField.Magic:
			case MuiScrollgroupPolicyStateField.Contents:
			case MuiScrollgroupPolicyStateField.FreeHorizontal:
			case MuiScrollgroupPolicyStateField.FreeVertical:
			case MuiScrollgroupPolicyStateField.HorizontalBar:
			case MuiScrollgroupPolicyStateField.VerticalBar:
			case MuiScrollgroupPolicyStateField.NoHorizontalBar:
			case MuiScrollgroupPolicyStateField.NoVerticalBar:
			case MuiScrollgroupPolicyStateField.AutoBars:
			case MuiScrollgroupPolicyStateField.UseWindowBorder:
				offset = (uint)field * 4;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiScrollgroupPolicyStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiScrollgroupPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScrollgroupPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScrollgroupPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupPolicyStateRecord.Size) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.Magic, out var magic) ||
			magic != MuiScrollgroupPolicyStateRecord.Cookie ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.Contents, out var contents) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.FreeHorizontal,
				out var freeHorizontal) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.FreeVertical,
				out var freeVertical) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.HorizontalBar,
				out var horizontalBar) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.VerticalBar,
				out var verticalBar) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.NoHorizontalBar,
				out var noHorizontalBar) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.NoVerticalBar,
				out var noVerticalBar) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.AutoBars, out var autoBars) ||
			!MuiScrollgroupPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupPolicyStateField.UseWindowBorder,
				out var useWindowBorder)) return false;
		value.Magic = magic;
		value.Contents = APTR.FromPointer(contents);
		value.FreeHorizontal = freeHorizontal;
		value.FreeVertical = freeVertical;
		value.HorizontalBar = APTR.FromPointer(horizontalBar);
		value.VerticalBar = APTR.FromPointer(verticalBar);
		value.NoHorizontalBar = noHorizontalBar;
		value.NoVerticalBar = noVerticalBar;
		value.AutoBars = autoBars;
		value.UseWindowBorder = useWindowBorder;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupPolicyStateRecord.Size) || value.Magic !=
			MuiScrollgroupPolicyStateRecord.Cookie) return false;
		return MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiScrollgroupPolicyStateField.Magic, value.Magic) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.Contents,
				value.Contents.Raw) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.FreeHorizontal,
				value.FreeHorizontal) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.FreeVertical,
				value.FreeVertical) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.HorizontalBar,
				value.HorizontalBar.Raw) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.VerticalBar,
				value.VerticalBar.Raw) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.NoHorizontalBar,
				value.NoHorizontalBar) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.NoVerticalBar,
				value.NoVerticalBar) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.AutoBars,
				value.AutoBars) &&
			MuiScrollgroupPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiScrollgroupPolicyStateField.UseWindowBorder,
				value.UseWindowBorder);
	}
}
