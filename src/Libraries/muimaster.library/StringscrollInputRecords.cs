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
	internal const uint Size = 24;
	internal const uint Cookie = 0x53535054u; // 'SSPT'
	internal const uint ActiveFlag = 1;
	internal const uint HorizontalAxis = 1;
	internal const uint VerticalAxis = 2;

	internal uint Magic;
	internal uint Axis;
	internal int GrabOffset;
	internal int StartScroll;
	internal int LastPointer;
	internal uint Flags;
}

internal enum MuiStringscrollPointerStateField : byte
{
	Magic,
	Axis,
	GrabOffset,
	StartScroll,
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
			case MuiStringscrollPointerStateField.LastPointer: offset = 16; break;
			case MuiStringscrollPointerStateField.Flags: offset = 20; break;
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

internal static class MuiStringscrollPointerStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollPointerState.Size) ||
			!MuiStringscrollPointerStateFieldCursorCodec.TryRead(ref platform,
				address, MuiStringscrollPointerStateField.Magic, out value.Magic) ||
			!MuiStringscrollPointerStateFieldCursorCodec.TryRead(ref platform,
				address, MuiStringscrollPointerStateField.Axis, out value.Axis) ||
			!MuiStringscrollPointerStateFieldCursorCodec.TryRead(ref platform,
				address, MuiStringscrollPointerStateField.GrabOffset,
				out var grabOffset) ||
			!MuiStringscrollPointerStateFieldCursorCodec.TryRead(ref platform,
				address, MuiStringscrollPointerStateField.StartScroll,
				out var startScroll) ||
			!MuiStringscrollPointerStateFieldCursorCodec.TryRead(ref platform,
				address, MuiStringscrollPointerStateField.LastPointer,
				out var lastPointer) ||
			!MuiStringscrollPointerStateFieldCursorCodec.TryRead(ref platform,
				address, MuiStringscrollPointerStateField.Flags, out value.Flags))
			return false;
		value.GrabOffset = unchecked((int)grabOffset);
		value.StartScroll = unchecked((int)startScroll);
		value.LastPointer = unchecked((int)lastPointer);
		return value.Magic == MuiStringscrollPointerState.Cookie;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollPointerState.Size)) return false;
		return MuiStringscrollPointerStateFieldCursorCodec.TryWrite(ref platform,
			address, MuiStringscrollPointerStateField.Magic, value.Magic) &&
			MuiStringscrollPointerStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiStringscrollPointerStateField.Axis, value.Axis) &&
			MuiStringscrollPointerStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiStringscrollPointerStateField.GrabOffset,
				unchecked((uint)value.GrabOffset)) &&
			MuiStringscrollPointerStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiStringscrollPointerStateField.StartScroll,
				unchecked((uint)value.StartScroll)) &&
			MuiStringscrollPointerStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiStringscrollPointerStateField.LastPointer,
				unchecked((uint)value.LastPointer)) &&
			MuiStringscrollPointerStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiStringscrollPointerStateField.Flags, value.Flags);
	}
}
