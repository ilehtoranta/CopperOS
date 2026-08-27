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
		uint offset;
		switch (cursor.Field)
		{
			case MuiStringscrollPointerStateField.Magic: offset = 0; break;
			case MuiStringscrollPointerStateField.Axis: offset = 4; break;
			case MuiStringscrollPointerStateField.GrabOffset: offset = 8; break;
			case MuiStringscrollPointerStateField.StartScroll: offset = 12; break;
			case MuiStringscrollPointerStateField.StartX: offset = 16; break;
			case MuiStringscrollPointerStateField.StartY: offset = 20; break;
			case MuiStringscrollPointerStateField.LastPointer: offset = 24; break;
			case MuiStringscrollPointerStateField.Flags: offset = 28; break;
			default: return false;
		}
		if (cursor.Address.IsNull || cursor.Address.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollPointerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringscrollPointerStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out var slot) &&
			Read(ref platform, slot, out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollPointerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringscrollPointerStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out var slot) &&
			Write(ref platform, slot, value);
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringscrollPointerState.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringscrollPointerState.Size)) return false;
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
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringscrollPointerStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiStringscrollPointerStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 4, out value.Axis) ||
			!MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(ref platform,
				address, 8, out value.GrabOffset) ||
			!MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(ref platform,
				address, 12, out value.StartScroll) ||
			!MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(ref platform,
				address, 16, out value.StartX) ||
			!MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(ref platform,
				address, 20, out value.StartY) ||
			!MuiStringscrollPointerStateRecordMemoryCodec.TryReadInt32(ref platform,
				address, 24, out value.LastPointer) ||
			!MuiStringscrollPointerStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 28, out value.Flags)) return false;
		return true;
	}

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
	{
		return MuiStringscrollPointerStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiStringscrollPointerStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 4, value.Axis) &&
			MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
				ref platform, address, 8, value.GrabOffset) &&
			MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
				ref platform, address, 12, value.StartScroll) &&
			MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
				ref platform, address, 16, value.StartX) &&
			MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
				ref platform, address, 20, value.StartY) &&
			MuiStringscrollPointerStateRecordMemoryCodec.TryWriteInt32(
				ref platform, address, 24, value.LastPointer) &&
			MuiStringscrollPointerStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 28, value.Flags);
	}
}
