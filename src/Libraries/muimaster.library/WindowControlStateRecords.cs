/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Window scalar control state.  These values are public attribute projections,
// but the canonical snapshot stays in one fixed-width guest record so control
// paths do not depend on private object offsets or managed mirrors.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowControlStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x5743544Cu; // 'WCTL'

	internal uint Magic;
	internal uint Id;
	internal uint DisableKeys;
	internal uint VisibleOnMaximize;
	internal uint IsSubWindow;
	internal uint NeedsMouseObject;
}

internal enum MuiWindowControlStateField : byte
{
	Magic,
	Id,
	DisableKeys,
	VisibleOnMaximize,
	IsSubWindow,
	NeedsMouseObject,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowControlStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowControlStateField Field;
}

internal static class MuiWindowControlStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowControlStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowControlStateField.Magic:
			case MuiWindowControlStateField.Id:
			case MuiWindowControlStateField.DisableKeys:
			case MuiWindowControlStateField.VisibleOnMaximize:
			case MuiWindowControlStateField.IsSubWindow:
			case MuiWindowControlStateField.NeedsMouseObject:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowControlStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowControlStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowControlStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowControlStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowControlStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowControlStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowControlStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowControlStateRecord.Size) ||
			!MuiWindowControlStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowControlStateField.Magic, out var magic) ||
			magic != MuiWindowControlStateRecord.Cookie ||
			!MuiWindowControlStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowControlStateField.Id, out value.Id) ||
			!MuiWindowControlStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowControlStateField.DisableKeys,
				out value.DisableKeys) ||
			!MuiWindowControlStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowControlStateField.VisibleOnMaximize,
				out value.VisibleOnMaximize) ||
			!MuiWindowControlStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowControlStateField.IsSubWindow,
				out value.IsSubWindow) ||
			!MuiWindowControlStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowControlStateField.NeedsMouseObject,
				out value.NeedsMouseObject)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowControlStateRecord.Size) || value.Magic !=
			MuiWindowControlStateRecord.Cookie) return false;
		return MuiWindowControlStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiWindowControlStateField.Magic, value.Magic) &&
			MuiWindowControlStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowControlStateField.Id, value.Id) &&
			MuiWindowControlStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowControlStateField.DisableKeys,
				value.DisableKeys) &&
			MuiWindowControlStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowControlStateField.VisibleOnMaximize,
				value.VisibleOnMaximize) &&
			MuiWindowControlStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowControlStateField.IsSubWindow,
				value.IsSubWindow) &&
			MuiWindowControlStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowControlStateField.NeedsMouseObject,
				value.NeedsMouseObject);
	}
}
