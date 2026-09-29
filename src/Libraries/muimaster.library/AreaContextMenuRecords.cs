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
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x41434D50u; // 'ACMP'

	internal uint Magic;
	internal APTR MenuStrip;
	internal APTR Trigger;
	internal uint Generation;
}

// Menu-strip and trigger pointers are opaque caller-owned MUI objects.  The
// record's integrity boundary therefore consists of its cookie and non-zero
// generation, plus live-owner validation at the consumer seam; pointer bits
// are retained losslessly rather than guessed or dereferenced here.
internal static class MuiAreaContextMenuStateAdmission
{
	internal static bool Validate(MuiAreaContextMenuStateRecord value) =>
		value.Magic == MuiAreaContextMenuStateRecord.Cookie &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaContextMenuStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaContextMenuStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaContextMenuStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaContextMenuStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaContextMenuStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area ContextMenu state is transferred as a named record. The bounded
// cursor walks the complete packed struct before selecting a field; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
internal static class MuiAreaContextMenuStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaContextMenuStateField field,
		out uint index)
	{
		if (field == MuiAreaContextMenuStateField.Magic) index = 0;
		else if (field == MuiAreaContextMenuStateField.MenuStrip) index = 1;
		else if (field == MuiAreaContextMenuStateField.Trigger) index = 2;
		else if (field == MuiAreaContextMenuStateField.Generation) index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaContextMenuStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaContextMenuStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaContextMenuStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaContextMenuStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaContextMenuStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaContextMenuStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaContextMenuStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaContextMenuStateField.MenuStrip)
			value = state.MenuStrip.Raw;
		else if (field == MuiAreaContextMenuStateField.Trigger)
			value = state.Trigger.Raw;
		else if (field == MuiAreaContextMenuStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaContextMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaContextMenuStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaContextMenuStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaContextMenuStateField.MenuStrip)
			state.MenuStrip = APTR.FromPointer(value);
		else if (field == MuiAreaContextMenuStateField.Trigger)
			state.Trigger = APTR.FromPointer(value);
		else if (field == MuiAreaContextMenuStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaContextMenuStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaContextMenuStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaContextMenuStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var menuStrip) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var trigger) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		value.MenuStrip = APTR.FromPointer(menuStrip);
		value.Trigger = APTR.FromPointer(trigger);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaContextMenuStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MenuStrip.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Trigger.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaContextMenuStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaContextMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaContextMenuStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
