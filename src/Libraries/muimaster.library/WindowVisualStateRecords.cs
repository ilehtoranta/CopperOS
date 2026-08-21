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

internal static class MuiWindowVisualStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowVisualStateRecord.Size) ||
			!MuiWindowVisualStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowVisualStateField.Magic, out var magic) ||
			magic != MuiWindowVisualStateRecord.Cookie ||
			!MuiWindowVisualStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowVisualStateField.NoMenus, out value.NoMenus) ||
			!MuiWindowVisualStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowVisualStateField.HasAlpha, out value.HasAlpha) ||
			!MuiWindowVisualStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowVisualStateField.Opacity, out value.Opacity) ||
			!MuiWindowVisualStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowVisualStateField.FancyDrawing,
				out value.FancyDrawing) ||
			!MuiWindowVisualStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowVisualStateField.MenuAction,
				out value.MenuAction)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowVisualStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowVisualStateRecord.Size) || value.Magic !=
			MuiWindowVisualStateRecord.Cookie) return false;
		return MuiWindowVisualStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiWindowVisualStateField.Magic, value.Magic) &&
			MuiWindowVisualStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowVisualStateField.NoMenus, value.NoMenus) &&
			MuiWindowVisualStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowVisualStateField.HasAlpha, value.HasAlpha) &&
			MuiWindowVisualStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowVisualStateField.Opacity, value.Opacity) &&
			MuiWindowVisualStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowVisualStateField.FancyDrawing,
				value.FancyDrawing) &&
			MuiWindowVisualStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowVisualStateField.MenuAction, value.MenuAction);
	}
}
