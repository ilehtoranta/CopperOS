/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the MorphOS String.mui scroll metrics.  All values
// are guest ULONGs; the record contains no managed text or host geometry.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint WidthOffset = 4;
	internal const uint HeightOffset = 8;
	internal const uint VisibleWidthOffset = 12;
	internal const uint VisibleHeightOffset = 16;
	internal const uint LeftOffset = 20;
	internal const uint TopOffset = 24;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringScrollMetricsStateFieldCursor cursor, out APTR address)
	where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringScrollMetricsStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
	where TPlatform : struct, IMuiGuestMemory
		=> MuiStringScrollMetricsStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

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
	private static bool TryResolveFieldIndex(MuiStringScrollMetricsStateField field,
		out uint index)
	{
		if (field == MuiStringScrollMetricsStateField.Magic)
			index = 0;
		else if (field == MuiStringScrollMetricsStateField.Width)
			index = 1;
		else if (field == MuiStringScrollMetricsStateField.Height)
			index = 2;
		else if (field == MuiStringScrollMetricsStateField.VisibleWidth)
			index = 3;
		else if (field == MuiStringScrollMetricsStateField.VisibleHeight)
			index = 4;
		else if (field == MuiStringScrollMetricsStateField.Left)
			index = 5;
		else if (field == MuiStringScrollMetricsStateField.Top)
			index = 6;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringScrollMetricsStateField field, out APTR address)
	where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringScrollMetricsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringScrollMetricsStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiStringScrollMetricsStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiStringScrollMetricsStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiStringScrollMetricsStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringScrollMetricsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiStringScrollMetricsStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringScrollMetricsStateField.Magic)
			value = state.Magic;
		else if (field == MuiStringScrollMetricsStateField.Width)
			value = state.Width;
		else if (field == MuiStringScrollMetricsStateField.Height)
			value = state.Height;
		else if (field == MuiStringScrollMetricsStateField.VisibleWidth)
			value = state.VisibleWidth;
		else if (field == MuiStringScrollMetricsStateField.VisibleHeight)
			value = state.VisibleHeight;
		else if (field == MuiStringScrollMetricsStateField.Left)
			value = state.Left;
		else if (field == MuiStringScrollMetricsStateField.Top)
			value = state.Top;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringScrollMetricsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringScrollMetricsStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringScrollMetricsStateField.Magic)
			state.Magic = value;
		else if (field == MuiStringScrollMetricsStateField.Width)
			state.Width = value;
		else if (field == MuiStringScrollMetricsStateField.Height)
			state.Height = value;
		else if (field == MuiStringScrollMetricsStateField.VisibleWidth)
			state.VisibleWidth = value;
		else if (field == MuiStringScrollMetricsStateField.VisibleHeight)
			state.VisibleHeight = value;
		else if (field == MuiStringScrollMetricsStateField.Left)
			state.Left = value;
		else if (field == MuiStringScrollMetricsStateField.Top)
			state.Top = value;
		else return false;
		return MuiStringScrollMetricsStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringScrollMetricsStateRecord.Size -
			MuiStringScrollMetricsStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringScrollMetricsStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringScrollMetricsStateRecord.FieldSize);
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
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringScrollMetricsStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Width) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Height) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.VisibleWidth) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.VisibleHeight) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Left) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Top) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringScrollMetricsStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringScrollMetricsStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringScrollMetricsStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Width) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Height) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.VisibleWidth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.VisibleHeight) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Left) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Top) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringScrollMetricsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
