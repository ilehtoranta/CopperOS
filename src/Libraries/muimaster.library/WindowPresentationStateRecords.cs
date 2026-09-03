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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint TitleOffset = 4;
	internal const uint ScreenOffset = 8;
	internal const uint ScreenTitleOffset = 12;
	internal const uint PublicScreenOffset = 16;
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
		if (field == MuiWindowPresentationStateField.Magic)
			offset = MuiWindowPresentationStateRecord.MagicOffset;
		else if (field == MuiWindowPresentationStateField.Title)
			offset = MuiWindowPresentationStateRecord.TitleOffset;
		else if (field == MuiWindowPresentationStateField.Screen)
			offset = MuiWindowPresentationStateRecord.ScreenOffset;
		else if (field == MuiWindowPresentationStateField.ScreenTitle)
			offset = MuiWindowPresentationStateRecord.ScreenTitleOffset;
		else if (field == MuiWindowPresentationStateField.PublicScreen)
			offset = MuiWindowPresentationStateRecord.PublicScreenOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
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
		return platform.IsMapped(address, MuiWindowPresentationStateRecord.FieldSize);
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
	private static bool TryResolve(MuiWindowPresentationStateField field,
		out uint offset)
	{
		if (field == MuiWindowPresentationStateField.Magic)
			offset = MuiWindowPresentationStateRecord.MagicOffset;
		else if (field == MuiWindowPresentationStateField.Title)
			offset = MuiWindowPresentationStateRecord.TitleOffset;
		else if (field == MuiWindowPresentationStateField.Screen)
			offset = MuiWindowPresentationStateRecord.ScreenOffset;
		else if (field == MuiWindowPresentationStateField.ScreenTitle)
			offset = MuiWindowPresentationStateRecord.ScreenTitleOffset;
		else if (field == MuiWindowPresentationStateField.PublicScreen)
			offset = MuiWindowPresentationStateRecord.PublicScreenOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowPresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowPresentationStateField.Magic)
			value = state.Magic;
		else if (field == MuiWindowPresentationStateField.Title)
			value = state.Title.Raw;
		else if (field == MuiWindowPresentationStateField.Screen)
			value = state.Screen.Raw;
		else if (field == MuiWindowPresentationStateField.ScreenTitle)
			value = state.ScreenTitle.Raw;
		else if (field == MuiWindowPresentationStateField.PublicScreen)
			value = state.PublicScreen.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowPresentationStateField.Magic)
			state.Magic = value;
		else if (field == MuiWindowPresentationStateField.Title)
			state.Title = APTR.FromPointer(value);
		else if (field == MuiWindowPresentationStateField.Screen)
			state.Screen = APTR.FromPointer(value);
		else if (field == MuiWindowPresentationStateField.ScreenTitle)
			state.ScreenTitle = APTR.FromPointer(value);
		else if (field == MuiWindowPresentationStateField.PublicScreen)
			state.PublicScreen = APTR.FromPointer(value);
		else return false;
		return MuiWindowPresentationStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowPresentationStateRecord.Size -
			MuiWindowPresentationStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiWindowPresentationStateRecord.FieldSize);
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
	// Sequential named-struct path used by Window.mui. The cookie and four
	// caller-owned APTR fields are exchanged in declaration order; numeric
	// positions remain confined to the bounded compatibility adapter.
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowPresentationStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Title.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Screen.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ScreenTitle.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.PublicScreen.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowPresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var title) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var screen) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var screenTitle) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var publicScreen) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.Title = APTR.FromPointer(title);
		value.Screen = APTR.FromPointer(screen);
		value.ScreenTitle = APTR.FromPointer(screenTitle);
		value.PublicScreen = APTR.FromPointer(publicScreen);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

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
		return WriteRecord(ref platform, address, value);
	}
}
