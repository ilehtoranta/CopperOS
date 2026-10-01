/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_ShortHelp is an opaque OBString pointer. Keep the public seam as a
// value type so a future bubble service can consume the guest object without
// exposing a managed string or a private Area offset.
public struct MuiAreaShortHelpStateInput
{
	public APTR Text;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaShortHelpStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x41534850u; // 'ASHP'

	internal uint Magic;
	internal APTR Text;
	internal uint Generation;
}

// ShortHelp is an opaque caller-owned OBString pointer. Preserve it
// losslessly, while admitting record identity and publication lifetime; live
// consumers additionally verify that the record belongs to the current object.
internal static class MuiAreaShortHelpStateAdmission
{
	internal static bool Validate(MuiAreaShortHelpStateRecord value) =>
		value.Magic == MuiAreaShortHelpStateRecord.Cookie &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaShortHelpStateField : byte
{
	Magic,
	Text,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaShortHelpStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaShortHelpStateField Field;
}

internal static class MuiAreaShortHelpStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaShortHelpStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaShortHelpStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaShortHelpStateRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaShortHelpStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaShortHelpStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area ShortHelp state is transferred as a named record. The typed
// cursor is the canonical address path; the enum overload below is only a
// bounded compatibility adapter for existing callers.
internal static class MuiAreaShortHelpStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaShortHelpStateField field,
		out uint index)
	{
		if (field == MuiAreaShortHelpStateField.Magic) index = 0;
		else if (field == MuiAreaShortHelpStateField.Text) index = 1;
		else if (field == MuiAreaShortHelpStateField.Generation) index = 2;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaShortHelpStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaShortHelpStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaShortHelpStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaShortHelpStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaShortHelpStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaShortHelpStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaShortHelpStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaShortHelpStateField.Text)
			value = state.Text.Raw;
		else if (field == MuiAreaShortHelpStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaShortHelpStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaShortHelpStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaShortHelpStateField.Text)
			state.Text = APTR.FromPointer(value);
		else if (field == MuiAreaShortHelpStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaShortHelpStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaShortHelpStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaShortHelpStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var text) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		value.Text = APTR.FromPointer(text);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaShortHelpStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Text.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaShortHelpStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaShortHelpStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
