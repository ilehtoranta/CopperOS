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
	private static bool TryResolve(MuiTextPresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextPresentationStateField.Magic => 0,
			MuiTextPresentationStateField.SetMin => 4,
			MuiTextPresentationStateField.SetMax => 8,
			MuiTextPresentationStateField.SetVMax => 12,
			MuiTextPresentationStateField.ControlChar => 16,
			MuiTextPresentationStateField.Marking => 20,
			MuiTextPresentationStateField.Shorten => 24,
			MuiTextPresentationStateField.HiChar => 28,
			MuiTextPresentationStateField.HiCharPresent => 32,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiTextPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Named Text presentation fields remain
// the semantic record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiTextPresentationStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextPresentationStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
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
		MuiGuestStructCursor.TryCreate(ref platform, address,
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
