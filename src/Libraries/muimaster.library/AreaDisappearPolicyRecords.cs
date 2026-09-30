/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The MorphOS disappearance priorities are signed LONG values.  Keep both
// related inputs in one fixed-width guest record so public Get/Set and future
// group layout selection use the same state without exposing object offsets.
public struct MuiAreaDisappearPolicyStateInput
{
	public int HorizDisappear;
	public int VertDisappear;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDisappearPolicyStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x41445052u; // 'ADPR'

	internal uint Magic;
	internal int HorizDisappear;
	internal int VertDisappear;
}

internal static class MuiAreaDisappearPolicyStateAdmission
{
	internal static bool Validate(MuiAreaDisappearPolicyStateRecord value) =>
		value.Magic == MuiAreaDisappearPolicyStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaDisappearPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaDisappearPolicyStateField : byte
{
	Magic,
	HorizDisappear,
	VertDisappear,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDisappearPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaDisappearPolicyStateField Field;
}

internal static class MuiAreaDisappearPolicyStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDisappearPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDisappearPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaDisappearPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDisappearPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDisappearPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDisappearPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDisappearPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area disappearance policy is transferred as a named record. The
// bounded cursor walks the complete packed struct before selecting a field; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
internal static class MuiAreaDisappearPolicyStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaDisappearPolicyStateField field,
		out uint index)
	{
		if (field == MuiAreaDisappearPolicyStateField.Magic)
			index = 0;
		else if (field == MuiAreaDisappearPolicyStateField.HorizDisappear)
			index = 1;
		else if (field == MuiAreaDisappearPolicyStateField.VertDisappear)
			index = 2;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDisappearPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDisappearPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDisappearPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaDisappearPolicyStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaDisappearPolicyStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaDisappearPolicyStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDisappearPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaDisappearPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaDisappearPolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaDisappearPolicyStateField.HorizDisappear)
			value = unchecked((uint)state.HorizDisappear);
		else if (field == MuiAreaDisappearPolicyStateField.VertDisappear)
			value = unchecked((uint)state.VertDisappear);
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDisappearPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaDisappearPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaDisappearPolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaDisappearPolicyStateField.HorizDisappear)
			state.HorizDisappear = unchecked((int)value);
		else if (field == MuiAreaDisappearPolicyStateField.VertDisappear)
			state.VertDisappear = unchecked((int)value);
		else return false;
		return MuiAreaDisappearPolicyStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiAreaDisappearPolicyStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaDisappearPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDisappearPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var horizontal) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var vertical)) return false;
		value.HorizDisappear = unchecked((int)horizontal);
		value.VertDisappear = unchecked((int)vertical);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaDisappearPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaDisappearPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.HorizDisappear)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.VertDisappear)) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaDisappearPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDisappearPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadStructural(ref platform, address, out value) &&
			MuiAreaDisappearPolicyStateAdmission.Validate(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDisappearPolicyStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaDisappearPolicyStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
