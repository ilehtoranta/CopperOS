/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident getter-only String.mui acknowledgement state.  The pointer
// names the current owned contents buffer; it is not a hidden widget offset or
// a managed copy of the text.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringAcknowledgeStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ContentsOffset = 4;
	internal const uint Cookie = 0x4D534143u; // 'MSAC'

	internal uint Magic;
	internal APTR Contents;
}

internal static class MuiStringAcknowledgeStateAdmission
{
	internal const int MaximumLength = 4096;

	internal static bool Validate(MuiStringAcknowledgeStateRecord value) =>
		value.Magic == MuiStringAcknowledgeStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Contents.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Contents, MaximumLength, out _);
	}
}

internal enum MuiStringAcknowledgeStateField : byte
{
	Magic,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringAcknowledgeStateFieldCursor
{
	internal APTR Record;
	internal MuiStringAcknowledgeStateField Field;
}

internal static class MuiStringAcknowledgeStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringAcknowledgeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringAcknowledgeStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringAcknowledgeStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAcknowledgeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringAcknowledgeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAcknowledgeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringAcknowledgeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Acknowledge consumers use the named
// contents record; this bounded adapter is the only layer that translates its
// fixed guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiStringAcknowledgeStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiStringAcknowledgeStateField field,
		out uint index)
	{
		if (field == MuiStringAcknowledgeStateField.Magic)
			index = 0;
		else if (field == MuiStringAcknowledgeStateField.Contents)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAcknowledgeStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringAcknowledgeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringAcknowledgeStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiStringAcknowledgeStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiStringAcknowledgeStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiStringAcknowledgeStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAcknowledgeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiStringAcknowledgeStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringAcknowledgeStateField.Magic)
			value = state.Magic;
		else if (field == MuiStringAcknowledgeStateField.Contents)
			value = state.Contents.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAcknowledgeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringAcknowledgeStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringAcknowledgeStateField.Magic)
			state.Magic = value;
		else if (field == MuiStringAcknowledgeStateField.Contents)
			state.Contents = APTR.FromPointer(value);
		else return false;
		return MuiStringAcknowledgeStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringAcknowledgeStateRecord.Size -
			MuiStringAcknowledgeStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringAcknowledgeStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringAcknowledgeStateRecord.FieldSize);
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

internal static class MuiStringAcknowledgeStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringAcknowledgeStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var contents) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringAcknowledgeStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringAcknowledgeStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
		=> MuiStringAcknowledgeStateAdmission.Validate(value) &&
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringAcknowledgeStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Contents.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
