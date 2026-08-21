/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the MorphOS String.mui scroll metrics.  All values
// are guest ULONGs; the record contains no managed text or host geometry.
public struct MuiStringScrollMetricsState
{
	public uint Width;
	public uint Height;
	public uint VisibleWidth;
	public uint VisibleHeight;
	public uint Left;
	public uint Top;
}

// Guest-resident canonical metrics and pixel offsets.  The public MUI
// attributes remain ABI-compatible scalar values while this record gives
// metric calculation, clamping, and generic getters one named state shape.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringScrollMetricsStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x53534D54u; // 'SSMT'

	internal uint Magic;
	internal uint Width;
	internal uint Height;
	internal uint VisibleWidth;
	internal uint VisibleHeight;
	internal uint Left;
	internal uint Top;
}

internal enum MuiStringScrollMetricsStateField : byte
{
	Magic,
	Width,
	Height,
	VisibleWidth,
	VisibleHeight,
	Left,
	Top,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringScrollMetricsStateFieldCursor
{
	internal APTR Record;
	internal MuiStringScrollMetricsStateField Field;
}

internal static class MuiStringScrollMetricsStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringScrollMetricsStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringScrollMetricsStateField.Magic => 0,
			MuiStringScrollMetricsStateField.Width => 4,
			MuiStringScrollMetricsStateField.Height => 8,
			MuiStringScrollMetricsStateField.VisibleWidth => 12,
			MuiStringScrollMetricsStateField.VisibleHeight => 16,
			MuiStringScrollMetricsStateField.Left => 20,
			MuiStringScrollMetricsStateField.Top => 24,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringScrollMetricsStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiStringScrollMetricsStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringScrollMetricsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringScrollMetricsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringScrollMetricsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringScrollMetricsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringScrollMetricsStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringScrollMetricsStateRecord.Size) ||
			!MuiStringScrollMetricsStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringScrollMetricsStateField.Magic, out var magic) ||
			magic != MuiStringScrollMetricsStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiStringScrollMetricsStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringScrollMetricsStateField.Width, out value.Width) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringScrollMetricsStateField.Height, out value.Height) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringScrollMetricsStateField.VisibleWidth,
			out value.VisibleWidth) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringScrollMetricsStateField.VisibleHeight,
			out value.VisibleHeight) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringScrollMetricsStateField.Left, out value.Left) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringScrollMetricsStateField.Top, out value.Top);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringScrollMetricsStateRecord.Size) || value.Magic !=
			MuiStringScrollMetricsStateRecord.Cookie) return false;
		return MuiStringScrollMetricsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringScrollMetricsStateField.Magic,
			value.Magic) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringScrollMetricsStateField.Width,
			value.Width) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringScrollMetricsStateField.Height,
			value.Height) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringScrollMetricsStateField.VisibleWidth,
			value.VisibleWidth) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringScrollMetricsStateField.VisibleHeight,
			value.VisibleHeight) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringScrollMetricsStateField.Left,
			value.Left) &&
			MuiStringScrollMetricsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringScrollMetricsStateField.Top,
			value.Top);
	}
}
