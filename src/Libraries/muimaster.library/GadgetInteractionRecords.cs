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
	private static bool TryResolve(MuiGadgetInteractionStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiGadgetInteractionStateField.Magic => 0,
			MuiGadgetInteractionStateField.InputMode => 4,
			MuiGadgetInteractionStateField.Selected => 8,
			MuiGadgetInteractionStateField.Pressed => 12,
			MuiGadgetInteractionStateField.ShowSelState => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGadgetInteractionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiGadgetInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGadgetInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGadgetInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Gadget consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiGadgetInteractionStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiGadgetInteractionStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiGadgetInteractionStateRecord.Size)) return false;
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
