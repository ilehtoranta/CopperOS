/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Text presentation and input state. Text contents and preparse remain
// in their dedicated pointer records; this record carries the scalar policy
// consumed by sizing, keyboard activation, shortening, and drawing.
public struct MuiTextPresentationState
{
	public uint SetMin;
	public uint SetMax;
	public uint SetVMax;
	public uint ControlChar;
	public uint Marking;
	public uint Shorten;
	public uint HiChar;
	public uint HiCharPresent;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextPresentationStateRecord
{
	internal const uint Size = 36;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint SetMinOffset = 4;
	internal const uint SetMaxOffset = 8;
	internal const uint SetVMaxOffset = 12;
	internal const uint ControlCharOffset = 16;
	internal const uint MarkingOffset = 20;
	internal const uint ShortenOffset = 24;
	internal const uint HiCharOffset = 28;
	internal const uint HiCharPresentOffset = 32;
	internal const uint Cookie = 0x4D545850u; // 'MTXP'

	internal uint Magic;
	internal uint SetMin;
	internal uint SetMax;
	internal uint SetVMax;
	internal uint ControlChar;
	internal uint Marking;
	internal uint Shorten;
	internal uint HiChar;
	internal uint HiCharPresent;
}

internal enum MuiTextPresentationStateField : byte
{
	Magic,
	SetMin,
	SetMax,
	SetVMax,
	ControlChar,
	Marking,
	Shorten,
	HiChar,
	HiCharPresent,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiTextPresentationStateField Field;
}

internal static class MuiTextPresentationStateFieldCursorCodec
{
	private static bool TryResolveFieldIndex(MuiTextPresentationStateField field,
		out uint index)
	{
		if (field == MuiTextPresentationStateField.Magic)
			index = 0;
		else if (field == MuiTextPresentationStateField.SetMin)
			index = 1;
		else if (field == MuiTextPresentationStateField.SetMax)
			index = 2;
		else if (field == MuiTextPresentationStateField.SetVMax)
			index = 3;
		else if (field == MuiTextPresentationStateField.ControlChar)
			index = 4;
		else if (field == MuiTextPresentationStateField.Marking)
			index = 5;
		else if (field == MuiTextPresentationStateField.Shorten)
			index = 6;
		else if (field == MuiTextPresentationStateField.HiChar)
			index = 7;
		else if (field == MuiTextPresentationStateField.HiCharPresent)
			index = 8;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiTextPresentationStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiTextPresentationStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiTextPresentationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Named Text presentation fields remain
// the semantic record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiTextPresentationStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiTextPresentationStateField field,
		out uint index)
	{
		if (field == MuiTextPresentationStateField.Magic)
			index = 0;
		else if (field == MuiTextPresentationStateField.SetMin)
			index = 1;
		else if (field == MuiTextPresentationStateField.SetMax)
			index = 2;
		else if (field == MuiTextPresentationStateField.SetVMax)
			index = 3;
		else if (field == MuiTextPresentationStateField.ControlChar)
			index = 4;
		else if (field == MuiTextPresentationStateField.Marking)
			index = 5;
		else if (field == MuiTextPresentationStateField.Shorten)
			index = 6;
		else if (field == MuiTextPresentationStateField.HiChar)
			index = 7;
		else if (field == MuiTextPresentationStateField.HiCharPresent)
			index = 8;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiTextPresentationStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiTextPresentationStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiTextPresentationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiTextPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextPresentationStateField.Magic)
			value = state.Magic;
		else if (field == MuiTextPresentationStateField.SetMin)
			value = state.SetMin;
		else if (field == MuiTextPresentationStateField.SetMax)
			value = state.SetMax;
		else if (field == MuiTextPresentationStateField.SetVMax)
			value = state.SetVMax;
		else if (field == MuiTextPresentationStateField.ControlChar)
			value = state.ControlChar;
		else if (field == MuiTextPresentationStateField.Marking)
			value = state.Marking;
		else if (field == MuiTextPresentationStateField.Shorten)
			value = state.Shorten;
		else if (field == MuiTextPresentationStateField.HiChar)
			value = state.HiChar;
		else if (field == MuiTextPresentationStateField.HiCharPresent)
			value = state.HiCharPresent;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextPresentationStateField.Magic)
			state.Magic = value;
		else if (field == MuiTextPresentationStateField.SetMin)
			state.SetMin = value;
		else if (field == MuiTextPresentationStateField.SetMax)
			state.SetMax = value;
		else if (field == MuiTextPresentationStateField.SetVMax)
			state.SetVMax = value;
		else if (field == MuiTextPresentationStateField.ControlChar)
			state.ControlChar = value;
		else if (field == MuiTextPresentationStateField.Marking)
			state.Marking = value;
		else if (field == MuiTextPresentationStateField.Shorten)
			state.Shorten = value;
		else if (field == MuiTextPresentationStateField.HiChar)
			state.HiChar = value;
		else if (field == MuiTextPresentationStateField.HiCharPresent)
			state.HiCharPresent = value;
		else return false;
		return MuiTextPresentationStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextPresentationStateRecord.Size -
			MuiTextPresentationStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiTextPresentationStateRecord.FieldSize);
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

internal static class MuiTextPresentationStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextPresentationStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SetMin) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SetMax) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SetVMax) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ControlChar) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Marking) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Shorten) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HiChar) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HiCharPresent) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiTextPresentationStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextPresentationStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextPresentationStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SetMin) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SetMax) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SetVMax) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ControlChar) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Marking) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Shorten) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HiChar) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HiCharPresent) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}

// MorphOS exposes the text presentation controls as a mixture of BOOL-like
// switches, byte-valued characters, and the documented shortening selector.
// Keep this admission check separate from the wire codec so malformed guest
// records are rejected without normalizing their bytes in place.
internal static class MuiTextPresentationStateValidation
{
	internal static bool IsValidState(MuiTextPresentationState value) =>
		value.SetMin <= 1 && value.SetMax <= 1 && value.SetVMax <= 1 &&
		value.ControlChar <= 0xFF && value.Marking <= 1 &&
		value.Shorten <= 2 && value.HiChar <= 0xFF &&
		value.HiCharPresent <= 1 &&
		(value.HiCharPresent != 0 || value.HiChar == 0);

	internal static bool IsValidRecord(MuiTextPresentationStateRecord value)
	{
		if (value.Magic != MuiTextPresentationStateRecord.Cookie) return false;
		var state = default(MuiTextPresentationState);
		state.SetMin = value.SetMin;
		state.SetMax = value.SetMax;
		state.SetVMax = value.SetVMax;
		state.ControlChar = value.ControlChar;
		state.Marking = value.Marking;
		state.Shorten = value.Shorten;
		state.HiChar = value.HiChar;
		state.HiCharPresent = value.HiCharPresent;
		return IsValidState(state);
	}
}

internal static class MuiTextPresentationStateAdmission
{
	internal static bool Validate(MuiTextPresentationStateRecord value) =>
		MuiTextPresentationStateValidation.IsValidRecord(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
