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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint IdOffset = 4;
	internal const uint DisableKeysOffset = 8;
	internal const uint VisibleOnMaximizeOffset = 12;
	internal const uint IsSubWindowOffset = 16;
	internal const uint NeedsMouseObjectOffset = 20;
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
		if (field == MuiWindowControlStateField.Magic)
			offset = MuiWindowControlStateRecord.MagicOffset;
		else if (field == MuiWindowControlStateField.Id)
			offset = MuiWindowControlStateRecord.IdOffset;
		else if (field == MuiWindowControlStateField.DisableKeys)
			offset = MuiWindowControlStateRecord.DisableKeysOffset;
		else if (field == MuiWindowControlStateField.VisibleOnMaximize)
			offset = MuiWindowControlStateRecord.VisibleOnMaximizeOffset;
		else if (field == MuiWindowControlStateField.IsSubWindow)
			offset = MuiWindowControlStateRecord.IsSubWindowOffset;
		else if (field == MuiWindowControlStateField.NeedsMouseObject)
			offset = MuiWindowControlStateRecord.NeedsMouseObjectOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
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
		return platform.IsMapped(address, MuiWindowControlStateRecord.FieldSize);
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
	private static bool TryResolve(MuiWindowControlStateField field,
		out uint offset)
	{
		if (field == MuiWindowControlStateField.Magic)
			offset = MuiWindowControlStateRecord.MagicOffset;
		else if (field == MuiWindowControlStateField.Id)
			offset = MuiWindowControlStateRecord.IdOffset;
		else if (field == MuiWindowControlStateField.DisableKeys)
			offset = MuiWindowControlStateRecord.DisableKeysOffset;
		else if (field == MuiWindowControlStateField.VisibleOnMaximize)
			offset = MuiWindowControlStateRecord.VisibleOnMaximizeOffset;
		else if (field == MuiWindowControlStateField.IsSubWindow)
			offset = MuiWindowControlStateRecord.IsSubWindowOffset;
		else if (field == MuiWindowControlStateField.NeedsMouseObject)
			offset = MuiWindowControlStateRecord.NeedsMouseObjectOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowControlStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowControlStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowControlStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowControlStateField.Magic)
			value = state.Magic;
		else if (field == MuiWindowControlStateField.Id)
			value = state.Id;
		else if (field == MuiWindowControlStateField.DisableKeys)
			value = state.DisableKeys;
		else if (field == MuiWindowControlStateField.VisibleOnMaximize)
			value = state.VisibleOnMaximize;
		else if (field == MuiWindowControlStateField.IsSubWindow)
			value = state.IsSubWindow;
		else if (field == MuiWindowControlStateField.NeedsMouseObject)
			value = state.NeedsMouseObject;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowControlStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowControlStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowControlStateField.Magic)
			state.Magic = value;
		else if (field == MuiWindowControlStateField.Id)
			state.Id = value;
		else if (field == MuiWindowControlStateField.DisableKeys)
			state.DisableKeys = value;
		else if (field == MuiWindowControlStateField.VisibleOnMaximize)
			state.VisibleOnMaximize = value;
		else if (field == MuiWindowControlStateField.IsSubWindow)
			state.IsSubWindow = value;
		else if (field == MuiWindowControlStateField.NeedsMouseObject)
			state.NeedsMouseObject = value;
		else return false;
		return MuiWindowControlStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowControlStateRecord.Size -
			MuiWindowControlStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowControlStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiWindowControlStateRecord.FieldSize);
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
	// Sequential named-struct path used by Window scalar-control projections.
	// Cookie, identifiers, and canonical BOOLs are exchanged in declaration
	// order; numeric positions remain confined to the compatibility adapter.
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowControlStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Id) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.DisableKeys) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.VisibleOnMaximize) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.IsSubWindow) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NeedsMouseObject) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowControlStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var id) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var disableKeys) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var visibleOnMaximize) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var isSubWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var needsMouseObject) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.Id = id;
		value.DisableKeys = disableKeys;
		value.VisibleOnMaximize = visibleOnMaximize;
		value.IsSubWindow = isSubWindow;
		value.NeedsMouseObject = needsMouseObject;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowControlStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

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
		return WriteRecord(ref platform, address, value);
	}
}
