/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_CustomFont is a caller-owned MUI font-specification string. Keep the
// pointer and presence separate so an explicit NULL value remains distinct
// from an omitted attribute, without retaining a managed string.
public struct MuiAreaCustomFontStateInput
{
	public APTR Spec;
	public uint Present;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCustomFontStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x4143464Eu; // 'ACFN'

	internal uint Magic;
	internal APTR Spec;
	internal uint Present;
	internal uint Generation;
}

internal static class MuiAreaCustomFontStateAdmission
{
	internal static bool Validate(MuiAreaCustomFontStateRecord value) =>
		value.Magic == MuiAreaCustomFontStateRecord.Cookie &&
		value.Present <= 1 && value.Generation != 0 &&
		(value.Present != 0 || value.Spec.IsNull);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaCustomFontStateField : byte
{
	Magic,
	Spec,
	Present,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCustomFontStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaCustomFontStateField Field;
}

internal static class MuiAreaCustomFontStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCustomFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCustomFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area CustomFont state is transferred as a named record. The bounded
// cursor walks the complete packed struct before selecting a field; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
internal static class MuiAreaCustomFontStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaCustomFontStateField field,
		out uint index)
	{
		if (field == MuiAreaCustomFontStateField.Magic) index = 0;
		else if (field == MuiAreaCustomFontStateField.Spec) index = 1;
		else if (field == MuiAreaCustomFontStateField.Present) index = 2;
		else if (field == MuiAreaCustomFontStateField.Generation) index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaCustomFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaCustomFontStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaCustomFontStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaCustomFontStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaCustomFontStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaCustomFontStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaCustomFontStateField.Spec)
			value = state.Spec.Raw;
		else if (field == MuiAreaCustomFontStateField.Present)
			value = state.Present;
		else if (field == MuiAreaCustomFontStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaCustomFontStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaCustomFontStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaCustomFontStateField.Spec)
			state.Spec = APTR.FromPointer(value);
		else if (field == MuiAreaCustomFontStateField.Present)
			state.Present = value;
		else if (field == MuiAreaCustomFontStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaCustomFontStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaCustomFontStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCustomFontStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var spec) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Present) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		value.Spec = APTR.FromPointer(spec);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCustomFontStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Spec.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Present) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadStructural(ref platform, address, out value) &&
			MuiAreaCustomFontStateAdmission.Validate(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaCustomFontStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
