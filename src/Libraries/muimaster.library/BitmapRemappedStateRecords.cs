/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the renderer-produced remapped Bitmap/Bodychunk
// pointer. The pointer remains guest-owned and may be null when no decoded or
// remapped source is available.
public struct MuiBitmapRemappedState
{
	public APTR Remapped;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapRemappedStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x4D425253u; // 'MBRS'

	internal uint Magic;
	internal APTR Remapped;
}

// Remapped is renderer-produced guest state. NULL means no decoded/remapped
// source; otherwise the pointer must remain mapped and the owning Bitmap or
// Bodychunk object must still be live before the state crosses a consumer
// boundary.
internal static class MuiBitmapRemappedStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiBitmapRemappedStateRecord.Cookie &&
		(value.Remapped.IsNull || platform.IsMapped(value.Remapped, 1));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiBitmapRemappedStateField : byte
{
	Magic,
	Remapped,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapRemappedStateFieldCursor
{
	internal APTR Record;
	internal MuiBitmapRemappedStateField Field;
}

internal static class MuiBitmapRemappedStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapRemappedStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapRemappedStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiBitmapRemappedStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapRemappedStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapRemappedStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapRemappedStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapRemappedStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Remapped-source consumers use the named
// record; the bounded cursor walks its complete packed shape before selecting
// a field. The cursor codec remains for compatibility and malformed-state
// diagnostics.
internal static class MuiBitmapRemappedStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiBitmapRemappedStateField field,
		out uint index)
	{
		if (field == MuiBitmapRemappedStateField.Magic)
			index = 0;
		else if (field == MuiBitmapRemappedStateField.Remapped)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapRemappedStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBitmapRemappedStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapRemappedStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiBitmapRemappedStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiBitmapRemappedStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiBitmapRemappedStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapRemappedStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiBitmapRemappedStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBitmapRemappedStateField.Magic)
			value = state.Magic;
		else if (field == MuiBitmapRemappedStateField.Remapped)
			value = state.Remapped.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapRemappedStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBitmapRemappedStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBitmapRemappedStateField.Magic)
			state.Magic = value;
		else if (field == MuiBitmapRemappedStateField.Remapped)
			state.Remapped = APTR.FromPointer(value);
		else return false;
		return MuiBitmapRemappedStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiBitmapRemappedStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBitmapRemappedStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var remapped)) return false;
		value.Remapped = APTR.FromPointer(remapped);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBitmapRemappedStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Remapped.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiBitmapRemappedStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiBitmapRemappedStateAdmission.Validate(ref platform,
			value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
