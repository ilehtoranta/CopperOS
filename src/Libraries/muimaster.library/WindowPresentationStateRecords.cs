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

// Presentation pointers are caller-owned guest capabilities.  Admission
// validates their mapped shape at the platform boundary without taking
// ownership or introducing a managed mirror.
internal static class MuiWindowPresentationStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Magic != MuiWindowPresentationStateRecord.Cookie)
			return false;
		if (value.Screen.IsNotNull && !platform.IsMapped(value.Screen, 1))
			return false;
		return IsCString(ref platform, value.Title) &&
			IsCString(ref platform, value.ScreenTitle) &&
			IsCString(ref platform, value.PublicScreen);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !window.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull;

	private static bool IsCString<TPlatform>(ref TPlatform platform, APTR value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.IsNull || CStringCodec.TryReadLength(ref platform, value, 65536,
			out _);
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

// Struct-first guest-memory adapter. Caller-owned strings and screen
// capabilities remain APTR fields in the semantic record; this bounded
// adapter owns only their fixed four-byte guest representation.
internal static class MuiWindowPresentationStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowPresentationStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowPresentationStateRecord.Size)) return false;
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

internal static class MuiWindowPresentationStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out var magic) ||
			!MuiWindowPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 4, out var title) ||
			!MuiWindowPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out var screen) ||
			!MuiWindowPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 12, out var screenTitle) ||
			!MuiWindowPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 16, out var publicScreen)) return false;
		value.Magic = magic;
		value.Title = APTR.FromPointer(title);
		value.Screen = APTR.FromPointer(screen);
		value.ScreenTitle = APTR.FromPointer(screenTitle);
		value.PublicScreen = APTR.FromPointer(publicScreen);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowPresentationStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowPresentationStateAdmission.Validate(ref platform, value))
			return false;
		return MuiWindowPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiWindowPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.Title.Raw) &&
			MuiWindowPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.Screen.Raw) &&
			MuiWindowPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 12, value.ScreenTitle.Raw) &&
			MuiWindowPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 16, value.PublicScreen.Raw);
	}
}
