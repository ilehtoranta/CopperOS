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

internal static class MuiStringScrollMetricsStateAdmission
{
	internal static bool Validate(MuiStringScrollMetricsStateRecord value) =>
		value.Magic == MuiStringScrollMetricsStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
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

// Struct-first guest-memory adapter. Named scroll metrics remain the semantic
// record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiStringScrollMetricsStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringScrollMetricsStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringScrollMetricsStateRecord.Size)) return false;
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

internal static class MuiStringScrollMetricsStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic)) return false;
		return MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Width) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out value.Height) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 12, out value.VisibleWidth) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 16, out value.VisibleHeight) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 20, out value.Left) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 24, out value.Top);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiStringScrollMetricsStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringScrollMetricsStateAdmission.Validate(value)) return false;
		return MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 4, value.Width) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 8, value.Height) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 12, value.VisibleWidth) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 16, value.VisibleHeight) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 20, value.Left) &&
			MuiStringScrollMetricsStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 24, value.Top);
	}
}
