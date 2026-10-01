/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the live Text.mui contents pointer.
public struct MuiTextContentsState
{
	public APTR Contents;
}

// Guest-resident Text.mui contents state.  The pointer is caller-owned when
// MUIA_Text_Copy is FALSE and points at object-owned TextCopyKey storage when
// CopyContents has materialised a copy.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextContentsStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ContentsOffset = 4;
	internal const uint Cookie = 0x4D54434Eu; // 'MTCN'

	internal uint Magic;
	internal APTR Contents;
}

internal static class MuiTextContentsStateAdmission
{
	internal const int MaximumLength = 65536;

	internal static bool Validate(MuiTextContentsStateRecord value) =>
		value.Magic == MuiTextContentsStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Contents.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Contents, MaximumLength, out _);
	}
}

internal enum MuiTextContentsStateField : byte
{
	Magic,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextContentsStateFieldCursor
{
	internal APTR Record;
	internal MuiTextContentsStateField Field;
}

internal static class MuiTextContentsStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextContentsStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextContentsStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextContentsStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextContentsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextContentsStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextContentsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextContentsStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

// Struct-first guest-memory adapter. Text consumers use the named contents
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiTextContentsStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiTextContentsStateField field,
		out uint index)
	{
		if (field == MuiTextContentsStateField.Magic)
			index = 0;
		else if (field == MuiTextContentsStateField.Contents)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextContentsStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextContentsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextContentsStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiTextContentsStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiTextContentsStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiTextContentsStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextContentsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiTextContentsStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextContentsStateField.Magic)
			value = state.Magic;
		else if (field == MuiTextContentsStateField.Contents)
			value = state.Contents.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextContentsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextContentsStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextContentsStateField.Magic)
			state.Magic = value;
		else if (field == MuiTextContentsStateField.Contents)
			state.Contents = APTR.FromPointer(value);
		else return false;
		return MuiTextContentsStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextContentsStateRecord.Size -
			MuiTextContentsStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextContentsStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiTextContentsStateRecord.FieldSize);
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

internal static class MuiTextContentsStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextContentsStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var contents) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiTextContentsStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextContentsStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextContentsStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Contents.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
