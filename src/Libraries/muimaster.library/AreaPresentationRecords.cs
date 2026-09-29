/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Area-derived presentation policy. The values retain MorphOS ULONG
// semantics while common-control input, sizing, and drawing consume one named
// guest-resident record.
public struct MuiAreaPresentationState
{
	public uint Disabled;
	public uint ShowMe;
	public uint Background;
	public uint Frame;
	public uint CustomBackfill;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaPresentationStateRecord
{
	internal const uint Size = 24;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint DisabledOffset = 4;
	internal const uint ShowMeOffset = 8;
	internal const uint BackgroundOffset = 12;
	internal const uint FrameOffset = 16;
	internal const uint CustomBackfillOffset = 20;
	internal const uint Cookie = 0x4D415052u; // 'MAPR'

	internal uint Magic;
	internal uint Disabled;
	internal uint ShowMe;
	internal uint Background;
	internal uint Frame;
	internal uint CustomBackfill;
}

internal enum MuiAreaPresentationStateField : byte
{
	Magic,
	Disabled,
	ShowMe,
	Background,
	Frame,
	CustomBackfill,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaPresentationStateField Field;
}

internal static class MuiAreaPresentationStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaPresentationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. The cursor codec above remains available
// for compatibility callers, while all named-record reads and writes resolve
// fields through the fixed MorphOS record layout and bounded guest memory.
internal static class MuiAreaPresentationStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaPresentationStateField field,
		out uint index)
	{
		if (field == MuiAreaPresentationStateField.Magic) index = 0;
		else if (field == MuiAreaPresentationStateField.Disabled) index = 1;
		else if (field == MuiAreaPresentationStateField.ShowMe) index = 2;
		else if (field == MuiAreaPresentationStateField.Background) index = 3;
		else if (field == MuiAreaPresentationStateField.Frame) index = 4;
		else if (field == MuiAreaPresentationStateField.CustomBackfill) index = 5;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaPresentationStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaPresentationStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaPresentationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaPresentationStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaPresentationStateField.Disabled)
			value = state.Disabled;
		else if (field == MuiAreaPresentationStateField.ShowMe)
			value = state.ShowMe;
		else if (field == MuiAreaPresentationStateField.Background)
			value = state.Background;
		else if (field == MuiAreaPresentationStateField.Frame)
			value = state.Frame;
		else if (field == MuiAreaPresentationStateField.CustomBackfill)
			value = state.CustomBackfill;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaPresentationStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaPresentationStateField.Disabled)
			state.Disabled = value;
		else if (field == MuiAreaPresentationStateField.ShowMe)
			state.ShowMe = value;
		else if (field == MuiAreaPresentationStateField.Background)
			state.Background = value;
		else if (field == MuiAreaPresentationStateField.Frame)
			state.Frame = value;
		else if (field == MuiAreaPresentationStateField.CustomBackfill)
			state.CustomBackfill = value;
		else return false;
		return MuiAreaPresentationStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaPresentationStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaPresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Disabled) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ShowMe) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Background) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Frame) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.CustomBackfill)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaPresentationStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Disabled) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ShowMe) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Background) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Frame) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.CustomBackfill) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaPresentationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaPresentationStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaPresentationStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

// MorphOS presents Disabled, ShowMe, and CustomBackfill as BOOL-like ULONGs.
// Background and Frame remain unrestricted ULONG selectors/pointers and must
// therefore be preserved losslessly. Keep validation separate from the codec
// so malformed guest state is rejected rather than normalized in place.
internal static class MuiAreaPresentationStateAdmission
{
	internal static bool Validate(MuiAreaPresentationState value) =>
		value.Disabled <= 1 && value.ShowMe <= 1 && value.CustomBackfill <= 1;

	internal static bool Validate(MuiAreaPresentationStateRecord value)
	{
		if (value.Magic != MuiAreaPresentationStateRecord.Cookie) return false;
		var state = default(MuiAreaPresentationState);
		state.Disabled = value.Disabled;
		state.ShowMe = value.ShowMe;
		state.Background = value.Background;
		state.Frame = value.Frame;
		state.CustomBackfill = value.CustomBackfill;
		return Validate(state);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing Area presentation call sites. New state
// boundaries use MuiAreaPresentationStateAdmission directly so live ownership
// is explicit.
internal static class MuiAreaPresentationStateValidation
{
	internal static bool IsValidState(MuiAreaPresentationState value) =>
		MuiAreaPresentationStateAdmission.Validate(value);

	internal static bool IsValidRecord(MuiAreaPresentationStateRecord value) =>
		MuiAreaPresentationStateAdmission.Validate(value);
}
