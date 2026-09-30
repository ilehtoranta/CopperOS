/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_DoubleClick is a getter-only signed LONG Area signal. Keep the full
// 32-bit value in a named state record so event producers and OM_GET share one
// typed contract without exposing a private object-layout offset.
public struct MuiAreaDoubleClickStateInput
{
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoubleClickStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x4144434Cu; // 'ADCL'

	internal uint Magic;
	internal int Value;
	internal uint Generation;
}

// DoubleClick is a getter-only signed LONG signal.  Its value is unrestricted
// and remains lossless; admission protects the named record's identity and
// initialization lifetime and verifies live ownership at consumer seams.
internal static class MuiAreaDoubleClickStateAdmission
{
	internal static bool Validate(MuiAreaDoubleClickStateRecord value) =>
		value.Magic == MuiAreaDoubleClickStateRecord.Cookie &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaDoubleClickStateField : byte
{
	Magic,
	Value,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoubleClickStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaDoubleClickStateField Field;
}

internal static class MuiAreaDoubleClickStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDoubleClickStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDoubleClickStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaDoubleClickStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoubleClickStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoubleClickStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area DoubleClick state is transferred as a named record. The bounded
// cursor walks the complete packed struct before selecting a field; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
internal static class MuiAreaDoubleClickStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaDoubleClickStateField field,
		out uint index)
	{
		if (field == MuiAreaDoubleClickStateField.Magic)
			index = 0;
		else if (field == MuiAreaDoubleClickStateField.Value)
			index = 1;
		else if (field == MuiAreaDoubleClickStateField.Generation)
			index = 2;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDoubleClickStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDoubleClickStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaDoubleClickStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaDoubleClickStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaDoubleClickStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaDoubleClickStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaDoubleClickStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaDoubleClickStateField.Value)
			value = unchecked((uint)state.Value);
		else if (field == MuiAreaDoubleClickStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaDoubleClickStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaDoubleClickStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaDoubleClickStateField.Value)
			state.Value = unchecked((int)value);
		else if (field == MuiAreaDoubleClickStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaDoubleClickStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaDoubleClickStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDoubleClickStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawValue) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		value.Value = unchecked((int)rawValue);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDoubleClickStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Value)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaDoubleClickStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDoubleClickStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaDoubleClickStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
