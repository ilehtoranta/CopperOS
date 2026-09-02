/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_ControlChar is a character-valued Area policy. Keep the normalized
// low-byte value in a named record; Text.mui's separate MUIA_Text_ControlChar
// state remains class-specific and is not silently aliased to this field.
public struct MuiAreaControlCharStateInput
{
	public uint Character;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaControlCharStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint CharacterOffset = 4;
	internal const uint GenerationOffset = 8;
	internal const uint Cookie = 0x41434348u; // 'ACCH'

	internal uint Magic;
	internal uint Character;
	internal uint Generation;
}

// ControlChar is stored as the normalized low-byte character.  Keep the
// structural codec lossless so an out-of-range guest write is diagnosable;
// strict admission accepts only the canonical byte and a live generation.
internal static class MuiAreaControlCharStateAdmission
{
	internal static bool Validate(MuiAreaControlCharStateRecord value) =>
		value.Magic == MuiAreaControlCharStateRecord.Cookie &&
		value.Character <= 0xFFu && value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaControlCharStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaControlCharStateField : byte
{
	Magic,
	Character,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaControlCharStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaControlCharStateField Field;
}

internal static class MuiAreaControlCharStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaControlCharStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaControlCharStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaControlCharStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaControlCharStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaControlCharStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaControlCharStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area ControlChar state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaControlCharStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaControlCharStateField field,
		out uint offset)
	{
		if (field == MuiAreaControlCharStateField.Magic)
			offset = MuiAreaControlCharStateRecord.MagicOffset;
		else if (field == MuiAreaControlCharStateField.Character)
			offset = MuiAreaControlCharStateRecord.CharacterOffset;
		else if (field == MuiAreaControlCharStateField.Generation)
			offset = MuiAreaControlCharStateRecord.GenerationOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaControlCharStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaControlCharStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaControlCharStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaControlCharStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaControlCharStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaControlCharStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaControlCharStateField.Character)
			value = state.Character;
		else if (field == MuiAreaControlCharStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaControlCharStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaControlCharStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaControlCharStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaControlCharStateField.Character)
			state.Character = value;
		else if (field == MuiAreaControlCharStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaControlCharStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaControlCharStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaControlCharStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaControlCharStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Character) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaControlCharStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaControlCharStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Character) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaControlCharStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaControlCharStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaControlCharStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaControlCharStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaControlCharStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
