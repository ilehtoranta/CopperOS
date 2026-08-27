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
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic)) return false;
		return MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.SetMin) &&
			MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 8, out value.SetMax) &&
			MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 12, out value.SetVMax) &&
			MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 16, out value.ControlChar) &&
			MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 20, out value.Marking) &&
			MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 24, out value.Shorten) &&
			MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 28, out value.HiChar) &&
			MuiTextPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, 32, out value.HiCharPresent);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiTextPresentationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextPresentationStateAdmission.Validate(value)) return false;
		return MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.SetMin) &&
			MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.SetMax) &&
			MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 12, value.SetVMax) &&
			MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 16, value.ControlChar) &&
			MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 20, value.Marking) &&
			MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 24, value.Shorten) &&
			MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 28, value.HiChar) &&
			MuiTextPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 32, value.HiCharPresent);
	}
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
