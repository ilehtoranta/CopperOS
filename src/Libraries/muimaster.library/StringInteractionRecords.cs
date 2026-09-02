/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui interaction policy.  The three BOOL attributes
// share one validated record so the editor's input gate and CR behavior do not
// depend on a private scalar ordering.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringInteractionStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint EditableOffset = 4;
	internal const uint AdvanceOnCROffset = 8;
	internal const uint MultilineOffset = 12;
	internal const uint Cookie = 0x4D534952u; // 'MSIR'

	internal uint Magic;
	internal uint Editable;
	internal uint AdvanceOnCR;
	internal uint Multiline;
}

internal static class MuiStringInteractionStateAdmission
{
	internal static bool Validate(MuiStringInteractionStateRecord value) =>
		value.Magic == MuiStringInteractionStateRecord.Cookie &&
		value.Editable <= 1 && value.AdvanceOnCR <= 1 && value.Multiline <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiStringInteractionStateField : byte
{
	Magic,
	Editable,
	AdvanceOnCR,
	Multiline,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringInteractionStateFieldCursor
{
	internal APTR Record;
	internal MuiStringInteractionStateField Field;
}

internal static class MuiStringInteractionStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringInteractionStateField field,
		out uint offset)
	{
		if (field == MuiStringInteractionStateField.Magic)
			offset = MuiStringInteractionStateRecord.MagicOffset;
		else if (field == MuiStringInteractionStateField.Editable)
			offset = MuiStringInteractionStateRecord.EditableOffset;
		else if (field == MuiStringInteractionStateField.AdvanceOnCR)
			offset = MuiStringInteractionStateRecord.AdvanceOnCROffset;
		else if (field == MuiStringInteractionStateField.Multiline)
			offset = MuiStringInteractionStateRecord.MultilineOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringInteractionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiStringInteractionStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. The named interaction BOOLs remain the
// semantic record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiStringInteractionStateRecordMemoryCodec
{
	private static bool TryResolve(MuiStringInteractionStateField field,
		out uint offset)
	{
		if (field == MuiStringInteractionStateField.Magic)
			offset = MuiStringInteractionStateRecord.MagicOffset;
		else if (field == MuiStringInteractionStateField.Editable)
			offset = MuiStringInteractionStateRecord.EditableOffset;
		else if (field == MuiStringInteractionStateField.AdvanceOnCR)
			offset = MuiStringInteractionStateRecord.AdvanceOnCROffset;
		else if (field == MuiStringInteractionStateField.Multiline)
			offset = MuiStringInteractionStateRecord.MultilineOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteractionStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiStringInteractionStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringInteractionStateField.Magic)
			value = state.Magic;
		else if (field == MuiStringInteractionStateField.Editable)
			value = state.Editable;
		else if (field == MuiStringInteractionStateField.AdvanceOnCR)
			value = state.AdvanceOnCR;
		else if (field == MuiStringInteractionStateField.Multiline)
			value = state.Multiline;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringInteractionStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringInteractionStateField.Magic)
			state.Magic = value;
		else if (field == MuiStringInteractionStateField.Editable)
			state.Editable = value;
		else if (field == MuiStringInteractionStateField.AdvanceOnCR)
			state.AdvanceOnCR = value;
		else if (field == MuiStringInteractionStateField.Multiline)
			state.Multiline = value;
		else return false;
		return MuiStringInteractionStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringInteractionStateRecord.Size -
			MuiStringInteractionStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringInteractionStateRecord.FieldSize);
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

internal static class MuiStringInteractionStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringInteractionStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Editable) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.AdvanceOnCR) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Multiline) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringInteractionStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringInteractionStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringInteractionStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Editable) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AdvanceOnCR) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Multiline) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);

}
