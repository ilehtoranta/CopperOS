/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Owning Application UsedClasses vector pointer. The vector entries and
// strings remain caller-owned guest memory and are validated by the existing
// named vector codec before this record is published.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationUsedClassesStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint VectorOffset = 4;
	internal const uint Cookie = 0x41554354u; // 'AUCT'

	internal uint Magic;
	internal APTR Vector;
}

// Admission for the caller-owned UsedClasses vector.  The record itself only
// carries a guest pointer; the pointed-to NULL-terminated vector is validated
// through its named cursor/entry codecs before it is exposed to MUI callers.
internal static class MuiApplicationUsedClassesStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationUsedClassesStateRecord.Cookie &&
		MuiApplicationUsedClassesVectorMemoryCodec.TryValidate(ref platform,
			value.Vector);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application,
		MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
}

internal enum MuiApplicationUsedClassesStateField : byte
{
	Magic,
	Vector,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationUsedClassesStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationUsedClassesStateField Field;
}

internal static class MuiApplicationUsedClassesStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationUsedClassesStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationUsedClassesStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationUsedClassesStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application UsedClasses state is transferred as a named record. The
// bounded cursor walks the complete packed struct before selecting a field;
// offset constants remain ABI documentation/compatibility aliases only.
internal static class MuiApplicationUsedClassesStateRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiApplicationUsedClassesStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field == MuiApplicationUsedClassesStateField.Magic)
			return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiApplicationUsedClassesStateRecord.FieldSize, out address);
		if (field == MuiApplicationUsedClassesStateField.Vector)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiApplicationUsedClassesStateRecord.FieldSize, out _)) return false;
			return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiApplicationUsedClassesStateRecord.FieldSize, out address);
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiApplicationUsedClassesStateRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address))
			return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationUsedClassesStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationUsedClassesStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationUsedClassesStateField.Vector)
			value = state.Vector.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationUsedClassesStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationUsedClassesStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationUsedClassesStateField.Vector)
			state.Vector = APTR.FromPointer(value);
		else return false;
		return MuiApplicationUsedClassesStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationUsedClassesStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationUsedClassesStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var vector)) return false;
		value.Vector = APTR.FromPointer(vector);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationUsedClassesStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Vector.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationUsedClassesStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationUsedClassesStateAdmission.Validate(
			ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
