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
		MuiApplicationUsedClassesVectorCodec.TryValidate(ref platform,
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

// Fixed application UsedClasses state is transferred as a named record.
// Numeric guest positions are confined to this ABI adapter; the compatibility
// cursor above remains available only to legacy callers and malformed-state
// diagnostics.
internal static class MuiApplicationUsedClassesStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationUsedClassesStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationUsedClassesStateField.Magic:
				offset = MuiApplicationUsedClassesStateRecord.MagicOffset;
				return true;
			case MuiApplicationUsedClassesStateField.Vector:
				offset = MuiApplicationUsedClassesStateRecord.VectorOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationUsedClassesStateRecord.Size) &&
			platform.IsMapped(address, MuiApplicationUsedClassesStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationUsedClassesStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationUsedClassesStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationUsedClassesStateField.Magic,
			out var magic) ||
			!MuiApplicationUsedClassesStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationUsedClassesStateField.Vector,
				out var vector))
			return false;
		value.Magic = magic;
		value.Vector = APTR.FromPointer(vector);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationUsedClassesStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationUsedClassesStateRecord.Size) ||
			!MuiApplicationUsedClassesStateAdmission.Validate(ref platform, value))
			return false;
		return MuiApplicationUsedClassesStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationUsedClassesStateField.Magic,
			value.Magic) &&
			MuiApplicationUsedClassesStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationUsedClassesStateField.Vector,
				value.Vector.Raw);
	}
}
