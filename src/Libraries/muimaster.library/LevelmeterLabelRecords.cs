/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned Levelmeter label string.
public struct MuiLevelmeterLabelState
{
	public APTR Label;
}

// Guest-resident Levelmeter label state.  The bounded copy is retained in the
// object's LevelmeterLabelKey Dataspace entry.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterLabelStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint LabelOffset = 4;
	internal const uint Cookie = 0x4D4C424Cu; // 'MLBL'

	internal uint Magic;
	internal APTR Label;
}

internal static class MuiLevelmeterLabelStateAdmission
{
	internal const int MaximumLength = 64;

	internal static bool Validate(MuiLevelmeterLabelStateRecord value) =>
		value.Magic == MuiLevelmeterLabelStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Label.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Label, MaximumLength, out _);
	}
}

internal enum MuiLevelmeterLabelStateField : byte
{
	Magic,
	Label,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterLabelStateFieldCursor
{
	internal APTR Record;
	internal MuiLevelmeterLabelStateField Field;
}

internal static class MuiLevelmeterLabelStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterLabelStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterLabelStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiLevelmeterLabelStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiLevelmeterLabelStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiLevelmeterLabelStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter. Levelmeter consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiLevelmeterLabelStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiLevelmeterLabelStateField field,
		out uint index)
	{
		if (field == MuiLevelmeterLabelStateField.Magic)
			index = 0;
		else if (field == MuiLevelmeterLabelStateField.Label)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiLevelmeterLabelStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterLabelStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiLevelmeterLabelStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiLevelmeterLabelStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiLevelmeterLabelStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiLevelmeterLabelStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiLevelmeterLabelStateField.Magic)
			value = state.Magic;
		else if (field == MuiLevelmeterLabelStateField.Label)
			value = state.Label.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiLevelmeterLabelStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiLevelmeterLabelStateField.Magic)
			state.Magic = value;
		else if (field == MuiLevelmeterLabelStateField.Label)
			state.Label = APTR.FromPointer(value);
		else return false;
		return MuiLevelmeterLabelStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiLevelmeterLabelStateRecord.Size -
			MuiLevelmeterLabelStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiLevelmeterLabelStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiLevelmeterLabelStateRecord.FieldSize);
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

internal static class MuiLevelmeterLabelStateRecordCodec
{
	// Production access is sequential and struct-shaped. The field-address
	// adapters above remain available for compatibility diagnostics only.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiLevelmeterLabelStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var label) || !MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.Label = APTR.FromPointer(label);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiLevelmeterLabelStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiLevelmeterLabelStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Label.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiLevelmeterLabelStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiLevelmeterLabelStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
