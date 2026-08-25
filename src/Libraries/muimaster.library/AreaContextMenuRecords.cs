/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_ContextMenu and MUIA_ContextMenuTrigger are opaque MUI object
// relationships.  The pointers remain caller-owned; this record only keeps
// the public Area projection and the last trigger published by the default
// ContextMenuChoice path.
public struct MuiAreaContextMenuStateInput
{
	public APTR MenuStrip;
	public APTR Trigger;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaContextMenuStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x41434D50u; // 'ACMP'

	internal uint Magic;
	internal APTR MenuStrip;
	internal APTR Trigger;
	internal uint Generation;
}

internal enum MuiAreaContextMenuStateField : byte
{
	Magic,
	MenuStrip,
	Trigger,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaContextMenuStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaContextMenuStateField Field;
}

internal static class MuiAreaContextMenuStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaContextMenuStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaContextMenuStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaContextMenuStateField.MenuStrip:
				offset = 4;
				return true;
			case MuiAreaContextMenuStateField.Trigger:
				offset = 8;
				return true;
			case MuiAreaContextMenuStateField.Generation:
				offset = 12;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaContextMenuStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaContextMenuStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaContextMenuStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaContextMenuStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaContextMenuStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaContextMenuStateRecord.Size) ||
			!MuiAreaContextMenuStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaContextMenuStateField.Magic, out var magic) ||
			magic != MuiAreaContextMenuStateRecord.Cookie ||
			!MuiAreaContextMenuStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaContextMenuStateField.MenuStrip, out var menuStrip) ||
			!MuiAreaContextMenuStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaContextMenuStateField.Trigger, out var trigger) ||
			!MuiAreaContextMenuStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaContextMenuStateField.Generation,
				out value.Generation)) return false;
		value.Magic = magic;
		value.MenuStrip = APTR.FromPointer(menuStrip);
		value.Trigger = APTR.FromPointer(trigger);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaContextMenuStateRecord.Size) || value.Magic !=
			MuiAreaContextMenuStateRecord.Cookie) return false;
		return MuiAreaContextMenuStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaContextMenuStateField.Magic, value.Magic) &&
			MuiAreaContextMenuStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaContextMenuStateField.MenuStrip, value.MenuStrip.Raw) &&
			MuiAreaContextMenuStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaContextMenuStateField.Trigger, value.Trigger.Raw) &&
			MuiAreaContextMenuStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaContextMenuStateField.Generation,
			value.Generation);
	}
}
