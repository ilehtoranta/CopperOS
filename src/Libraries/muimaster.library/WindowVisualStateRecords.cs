/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Mutable Window visual/event policy. BOOL values are canonical ULONGs and
// Opacity remains the MorphOS bounded 0..255 value; MenuAction is caller-owned
// event data with no managed mirror.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowVisualStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x57565354u; // 'WVST'

	internal uint Magic;
	internal uint NoMenus;
	internal uint HasAlpha;
	internal uint Opacity;
	internal uint FancyDrawing;
	internal uint MenuAction;
}

// Structural decoding is separate from live admission so ABI fixtures can
// inspect malformed records without making them consumable by Window.mui.
internal static class MuiWindowVisualStateAdmission
{
	internal static bool Validate(MuiWindowVisualStateRecord value) =>
		value.Magic == MuiWindowVisualStateRecord.Cookie &&
		value.NoMenus <= 1 && value.HasAlpha <= 1 &&
		value.Opacity <= 255 && value.FancyDrawing <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !window.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull;
}

internal enum MuiWindowVisualStateField : byte
{
	Magic,
	NoMenus,
	HasAlpha,
	Opacity,
	FancyDrawing,
	MenuAction,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowVisualStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowVisualStateField Field;
}

internal static class MuiWindowVisualStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowVisualStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowVisualStateField.Magic:
			case MuiWindowVisualStateField.NoMenus:
			case MuiWindowVisualStateField.HasAlpha:
			case MuiWindowVisualStateField.Opacity:
			case MuiWindowVisualStateField.FancyDrawing:
			case MuiWindowVisualStateField.MenuAction:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowVisualStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowVisualStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowVisualStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowVisualStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowVisualStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowVisualStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Visual policy values remain named
// semantic fields; this bounded adapter owns their fixed guest slots.
internal static class MuiWindowVisualStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowVisualStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowVisualStateRecord.Size)) return false;
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

internal static class MuiWindowVisualStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		uint noMenus;
		uint hasAlpha;
		uint opacity;
		uint fancyDrawing;
		uint menuAction;
		if (!MuiWindowVisualStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out var magic) ||
			!MuiWindowVisualStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 4, out noMenus) ||
			!MuiWindowVisualStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out hasAlpha) ||
			!MuiWindowVisualStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 12, out opacity) ||
			!MuiWindowVisualStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 16, out fancyDrawing) ||
			!MuiWindowVisualStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 20, out menuAction)) return false;
		value.Magic = magic;
		value.NoMenus = noMenus;
		value.HasAlpha = hasAlpha;
		value.Opacity = opacity;
		value.FancyDrawing = fancyDrawing;
		value.MenuAction = menuAction;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowVisualStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowVisualStateAdmission.Validate(value)) return false;
		return MuiWindowVisualStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiWindowVisualStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.NoMenus) &&
			MuiWindowVisualStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.HasAlpha) &&
			MuiWindowVisualStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 12, value.Opacity) &&
			MuiWindowVisualStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 16, value.FancyDrawing) &&
			MuiWindowVisualStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 20, value.MenuAction);
	}
}
