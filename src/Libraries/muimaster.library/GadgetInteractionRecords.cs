/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Gadget interaction state. The ULONG fields retain MorphOS semantics while
// keyboard activation, selected/pressed transitions, and selected-visual policy
// consume one named value.
public struct MuiGadgetInteractionState
{
	public uint InputMode;
	public uint Selected;
	public uint Pressed;
	public uint ShowSelState;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGadgetInteractionStateRecord
{
	internal const uint Size = 20;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint InputModeOffset = 4;
	internal const uint SelectedOffset = 8;
	internal const uint PressedOffset = 12;
	internal const uint ShowSelStateOffset = 16;
	internal const uint Cookie = 0x4D474454u; // 'MGDT'

	internal uint Magic;
	internal uint InputMode;
	internal uint Selected;
	internal uint Pressed;
	internal uint ShowSelState;
}

internal enum MuiGadgetInteractionStateField : byte
{
	Magic,
	InputMode,
	Selected,
	Pressed,
	ShowSelState,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGadgetInteractionStateFieldCursor
{
	internal APTR Record;
	internal MuiGadgetInteractionStateField Field;
}

internal static class MuiGadgetInteractionStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGadgetInteractionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGadgetInteractionStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGadgetInteractionStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiGadgetInteractionStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGadgetInteractionStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Gadget consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiGadgetInteractionStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiGadgetInteractionStateField field,
		out uint index)
	{
		if (field == MuiGadgetInteractionStateField.Magic)
			index = 0;
		else if (field == MuiGadgetInteractionStateField.InputMode)
			index = 1;
		else if (field == MuiGadgetInteractionStateField.Selected)
			index = 2;
		else if (field == MuiGadgetInteractionStateField.Pressed)
			index = 3;
		else if (field == MuiGadgetInteractionStateField.ShowSelState)
			index = 4;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGadgetInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGadgetInteractionStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiGadgetInteractionStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiGadgetInteractionStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiGadgetInteractionStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGadgetInteractionStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGadgetInteractionStateField.Magic)
			value = state.Magic;
		else if (field == MuiGadgetInteractionStateField.InputMode)
			value = state.InputMode;
		else if (field == MuiGadgetInteractionStateField.Selected)
			value = state.Selected;
		else if (field == MuiGadgetInteractionStateField.Pressed)
			value = state.Pressed;
		else if (field == MuiGadgetInteractionStateField.ShowSelState)
			value = state.ShowSelState;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGadgetInteractionStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGadgetInteractionStateField.Magic)
			state.Magic = value;
		else if (field == MuiGadgetInteractionStateField.InputMode)
			state.InputMode = value;
		else if (field == MuiGadgetInteractionStateField.Selected)
			state.Selected = value;
		else if (field == MuiGadgetInteractionStateField.Pressed)
			state.Pressed = value;
		else if (field == MuiGadgetInteractionStateField.ShowSelState)
			state.ShowSelState = value;
		else return false;
		return MuiGadgetInteractionStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	// Legacy raw-offset adapter retained for bounded diagnostics and older
	// callers. Typed field access above is the preferred API.
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiGadgetInteractionStateRecord.Size -
			MuiGadgetInteractionStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiGadgetInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiGadgetInteractionStateRecord.FieldSize);
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

internal static class MuiGadgetInteractionStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGadgetInteractionStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.InputMode) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Selected) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Pressed) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ShowSelState) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGadgetInteractionStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.InputMode) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Selected) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Pressed) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ShowSelState) &&
			MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiGadgetInteractionStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGadgetInteractionStateAdmission.Validate(value) &&
		WriteRecord(ref platform, address, value);
}

// InputMode is the documented four-value enum and the remaining fields are
// MorphOS BOOL projections. Consumers must not turn an invalid named record
// into a usable interaction state by normalizing it.
internal static class MuiGadgetInteractionStateAdmission
{
	internal static bool Validate(MuiGadgetInteractionStateRecord value) =>
		value.Magic == MuiGadgetInteractionStateRecord.Cookie &&
		value.InputMode <= 3 && value.Selected <= 1 && value.Pressed <= 1 &&
		value.ShowSelState <= 1;

	internal static bool Validate(MuiGadgetInteractionState value) =>
		value.InputMode <= 3 && value.Selected <= 1 && value.Pressed <= 1 &&
		value.ShowSelState <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Kept as a named compatibility alias for existing range-only callers. New
// state publication and consumption use MuiGadgetInteractionStateAdmission so
// live ownership is checked at the object-store boundary.
internal static class MuiGadgetInteractionStateValidation
{
	internal static bool IsValidRecord(MuiGadgetInteractionStateRecord value) =>
		MuiGadgetInteractionStateAdmission.Validate(value);

	internal static bool IsValidState(MuiGadgetInteractionState value) =>
		MuiGadgetInteractionStateAdmission.Validate(value);
}
