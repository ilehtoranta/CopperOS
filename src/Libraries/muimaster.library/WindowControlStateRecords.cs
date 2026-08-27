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

// Structural decoding remains available for ABI fixtures; this predicate is
// the live contract consumed by Window.mui control getters and setters.
internal static class MuiWindowControlStateAdmission
{
	internal static bool Validate(MuiWindowControlStateRecord value) =>
		value.Magic == MuiWindowControlStateRecord.Cookie &&
		value.VisibleOnMaximize <= 1 && value.IsSubWindow <= 1 &&
		value.NeedsMouseObject <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR window, MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !window.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, window).IsNull;
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

// Struct-first guest-memory adapter. The named control record remains the
// semantic API; this bounded boundary is the only place that translates its
// fixed guest representation into byte addresses.
internal static class MuiWindowControlStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowControlStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowControlStateRecord.Size)) return false;
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

internal static class MuiWindowControlStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out var magic) ||
			!MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 4, out value.Id) ||
			!MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out value.DisableKeys) ||
			!MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 12, out value.VisibleOnMaximize) ||
			!MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 16, out value.IsSubWindow) ||
			!MuiWindowControlStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 20, out value.NeedsMouseObject)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowControlStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowControlStateAdmission.Validate(value)) return false;
		return MuiWindowControlStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiWindowControlStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.Id) &&
			MuiWindowControlStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.DisableKeys) &&
			MuiWindowControlStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 12, value.VisibleOnMaximize) &&
			MuiWindowControlStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 16, value.IsSubWindow) &&
			MuiWindowControlStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 20, value.NeedsMouseObject);
	}
}
