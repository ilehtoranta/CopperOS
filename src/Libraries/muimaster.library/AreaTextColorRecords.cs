/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public Area projection of MorphOS MUIA_TextColor. Color is a packed
// 00RRGGBB value; Active is non-zero only during MUIM_Setup..MUIM_Cleanup.
public struct MuiAreaTextColorState
{
	public uint Color;
	public uint Active;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTextColorStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ColorOffset = 4;
	internal const uint ActiveOffset = 8;
	internal const uint GenerationOffset = 12;
	internal const uint Cookie = 0x4D544352u; // 'MTCR'

	internal uint Magic;
	internal uint Color;
	internal uint Active;
	internal uint Generation;
}

// TextColor is a setup-scoped packed RGB value. The color is constrained to
// the documented 24-bit payload, Active is a canonical BOOL, and generation
// identifies the current setup/cleanup publication. Live consumers additionally
// require ownership by the current object.
internal static class MuiAreaTextColorStateAdmission
{
	internal static bool Validate(MuiAreaTextColorStateRecord value) =>
		value.Magic == MuiAreaTextColorStateRecord.Cookie &&
		value.Color <= 0x00FFFFFFu && value.Active <= 1 &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaTextColorStateField : byte
{
	Magic,
	Color,
	Active,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTextColorStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaTextColorStateField Field;
}

internal static class MuiAreaTextColorStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaTextColorStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaTextColorStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaTextColorStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaTextColorStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area TextColor state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaTextColorStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaTextColorStateField field,
		out uint index)
	{
		if (field == MuiAreaTextColorStateField.Magic) index = 0;
		else if (field == MuiAreaTextColorStateField.Color) index = 1;
		else if (field == MuiAreaTextColorStateField.Active) index = 2;
		else if (field == MuiAreaTextColorStateField.Generation) index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaTextColorStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaTextColorStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaTextColorStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaTextColorStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaTextColorStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaTextColorStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaTextColorStateField.Color)
			value = state.Color;
		else if (field == MuiAreaTextColorStateField.Active)
			value = state.Active;
		else if (field == MuiAreaTextColorStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaTextColorStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaTextColorStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaTextColorStateField.Color)
			state.Color = value;
		else if (field == MuiAreaTextColorStateField.Active)
			state.Active = value;
		else if (field == MuiAreaTextColorStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaTextColorStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaTextColorStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaTextColorStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Color) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaTextColorStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Color) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Active) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaTextColorStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaTextColorStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
