/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Mutable Window identity/presentation pointers retained at the object
// boundary.  These are caller-owned guest strings/capabilities; the record
// carries only their fixed-width APTR values and never introduces managed
// ownership.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowPresentationStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x57505253u; // 'WPRS'

	internal uint Magic;
	internal APTR Title;
	internal APTR Screen;
	internal APTR ScreenTitle;
	internal APTR PublicScreen;
}

internal enum MuiWindowPresentationStateField : byte
{
	Magic,
	Title,
	Screen,
	ScreenTitle,
	PublicScreen,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowPresentationStateField Field;
}

internal static class MuiWindowPresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowPresentationStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowPresentationStateField.Magic:
			case MuiWindowPresentationStateField.Title:
			case MuiWindowPresentationStateField.Screen:
			case MuiWindowPresentationStateField.ScreenTitle:
			case MuiWindowPresentationStateField.PublicScreen:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowPresentationStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowPresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowPresentationStateRecord.Size) ||
			!MuiWindowPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowPresentationStateField.Magic, out var magic) ||
			magic != MuiWindowPresentationStateRecord.Cookie ||
			!MuiWindowPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowPresentationStateField.Title,
				out var title) ||
			!MuiWindowPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowPresentationStateField.Screen,
				out var screen) ||
			!MuiWindowPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowPresentationStateField.ScreenTitle,
				out var screenTitle) ||
			!MuiWindowPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowPresentationStateField.PublicScreen,
				out var publicScreen)) return false;
		value.Magic = magic;
		value.Title = APTR.FromPointer(title);
		value.Screen = APTR.FromPointer(screen);
		value.ScreenTitle = APTR.FromPointer(screenTitle);
		value.PublicScreen = APTR.FromPointer(publicScreen);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowPresentationStateRecord.Size) || value.Magic !=
			MuiWindowPresentationStateRecord.Cookie) return false;
		return MuiWindowPresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiWindowPresentationStateField.Magic,
			value.Magic) &&
			MuiWindowPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowPresentationStateField.Title, value.Title.Raw) &&
			MuiWindowPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowPresentationStateField.Screen, value.Screen.Raw) &&
			MuiWindowPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowPresentationStateField.ScreenTitle,
				value.ScreenTitle.Raw) &&
			MuiWindowPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowPresentationStateField.PublicScreen,
				value.PublicScreen.Raw);
	}
}
