/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident state for one bounded Stringscroll thumb drag.  The drag
// keeps the pointer's grab offset rather than an opaque host event object, so
// every transition remains a named fixed-width record on the 68k side.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollPointerState
{
	internal const uint Size = 32;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint AxisOffset = 4;
	internal const uint GrabOffsetOffset = 8;
	internal const uint StartScrollOffset = 12;
	internal const uint StartXOffset = 16;
	internal const uint StartYOffset = 20;
	internal const uint LastPointerOffset = 24;
	internal const uint FlagsOffset = 28;
	internal const uint Cookie = 0x53535054u; // 'SSPT'
	internal const uint ActiveFlag = 1;
	internal const uint CapturedFlag = 2;
	internal const uint HorizontalAxis = 1;
	internal const uint VerticalAxis = 2;

	internal uint Magic;
	internal uint Axis;
	internal int GrabOffset;
	internal int StartScroll;
	internal int StartX;
	internal int StartY;
	internal int LastPointer;
	internal uint Flags;
}

internal enum MuiStringscrollPointerStateField : byte
{
	Magic,
	Axis,
	GrabOffset,
	StartScroll,
	StartX,
	StartY,
	LastPointer,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollPointerStateFieldCursor
{
	internal APTR Address;
	internal MuiStringscrollPointerStateField Field;
}

internal static class MuiStringscrollPointerStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringscrollPointerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiStringscrollPointerStateRecordMemoryCodec.TryResolve(cursor.Field,
			out var offset) || cursor.Address.IsNull || cursor.Address.Raw >
			uint.MaxValue - offset || !platform.IsMapped(cursor.Address,
			MuiStringscrollPointerState.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, MuiStringscrollPointerState.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollPointerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringscrollPointerStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, field, out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollPointerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringscrollPointerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, field, value);
	}

	private static bool Read<TPlatform>(ref TPlatform platform, APTR address,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (address.IsNull || !platform.IsMapped(address, 4)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	private static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address, 4)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. The named drag state remains the
// semantic record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiStringscrollPointerStateRecordMemoryCodec
{
	internal static bool TryResolve(MuiStringscrollPointerStateField field,
		out uint offset)
	{
		if (field == MuiStringscrollPointerStateField.Magic)
			offset = MuiStringscrollPointerState.MagicOffset;
		else if (field == MuiStringscrollPointerStateField.Axis)
			offset = MuiStringscrollPointerState.AxisOffset;
		else if (field == MuiStringscrollPointerStateField.GrabOffset)
			offset = MuiStringscrollPointerState.GrabOffsetOffset;
		else if (field == MuiStringscrollPointerStateField.StartScroll)
			offset = MuiStringscrollPointerState.StartScrollOffset;
		else if (field == MuiStringscrollPointerStateField.StartX)
			offset = MuiStringscrollPointerState.StartXOffset;
		else if (field == MuiStringscrollPointerStateField.StartY)
			offset = MuiStringscrollPointerState.StartYOffset;
		else if (field == MuiStringscrollPointerStateField.LastPointer)
			offset = MuiStringscrollPointerState.LastPointerOffset;
		else if (field == MuiStringscrollPointerStateField.Flags)
			offset = MuiStringscrollPointerState.FlagsOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollPointerStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollPointerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiStringscrollPointerStateCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiStringscrollPointerStateField.Magic)
			value = state.Magic;
		else if (field == MuiStringscrollPointerStateField.Axis)
			value = state.Axis;
		else if (field == MuiStringscrollPointerStateField.GrabOffset)
			value = unchecked((uint)state.GrabOffset);
		else if (field == MuiStringscrollPointerStateField.StartScroll)
			value = unchecked((uint)state.StartScroll);
		else if (field == MuiStringscrollPointerStateField.StartX)
			value = unchecked((uint)state.StartX);
		else if (field == MuiStringscrollPointerStateField.StartY)
			value = unchecked((uint)state.StartY);
		else if (field == MuiStringscrollPointerStateField.LastPointer)
			value = unchecked((uint)state.LastPointer);
		else if (field == MuiStringscrollPointerStateField.Flags)
			value = state.Flags;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollPointerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringscrollPointerStateCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiStringscrollPointerStateField.Magic)
			state.Magic = value;
		else if (field == MuiStringscrollPointerStateField.Axis)
			state.Axis = value;
		else if (field == MuiStringscrollPointerStateField.GrabOffset)
			state.GrabOffset = unchecked((int)value);
		else if (field == MuiStringscrollPointerStateField.StartScroll)
			state.StartScroll = unchecked((int)value);
		else if (field == MuiStringscrollPointerStateField.StartX)
			state.StartX = unchecked((int)value);
		else if (field == MuiStringscrollPointerStateField.StartY)
			state.StartY = unchecked((int)value);
		else if (field == MuiStringscrollPointerStateField.LastPointer)
			state.LastPointer = unchecked((int)value);
		else if (field == MuiStringscrollPointerStateField.Flags)
			state.Flags = value;
		else return false;
		return MuiStringscrollPointerStateCodec.WriteRecord(ref platform, record,
			state);
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollPointerStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw)) return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollPointerStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringscrollPointerState.Size -
			MuiStringscrollPointerState.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringscrollPointerState.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringscrollPointerState.FieldSize);
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

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, offset, out var raw)) return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, offset, unchecked((uint)value));
}

internal static class MuiStringscrollPointerStateCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringscrollPointerState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Axis) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var grabOffset) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startScroll) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var lastPointer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.GrabOffset = unchecked((int)grabOffset);
		value.StartScroll = unchecked((int)startScroll);
		value.StartX = unchecked((int)startX);
		value.StartY = unchecked((int)startY);
		value.LastPointer = unchecked((int)lastPointer);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringscrollPointerState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Axis) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.GrabOffset)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.StartScroll)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.StartX)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.StartY)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.LastPointer)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadStructural(ref platform, address, out value) &&
			value.Magic == MuiStringscrollPointerState.Cookie;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
