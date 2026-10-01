/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_Text_Copy is a public BOOL policy. Keep the normalized value in a
// named semantic view so Get/OM_GET, construction, and Text_Contents mutation
// share one source without exposing managed state or object offsets.
public struct MuiTextCopyState
{
	public uint Copy;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextCopyStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint CopyOffset = 4;
	internal const uint Cookie = 0x4D544350u; // 'MTCP'

	internal uint Magic;
	internal uint Copy;
}

internal static class MuiTextCopyStateAdmission
{
	internal static bool Validate(MuiTextCopyStateRecord value) =>
		value.Magic == MuiTextCopyStateRecord.Cookie && value.Copy <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiTextCopyStateField : byte
{
	Magic,
	Copy,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextCopyStateFieldCursor
{
	internal APTR Record;
	internal MuiTextCopyStateField Field;
}

internal static class MuiTextCopyStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextCopyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextCopyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextCopyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiTextCopyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextCopyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

// Struct-first guest-memory adapter. Text consumers use the named copy-policy
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiTextCopyStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiTextCopyStateField field,
		out uint index)
	{
		if (field == MuiTextCopyStateField.Magic)
			index = 0;
		else if (field == MuiTextCopyStateField.Copy)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextCopyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextCopyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiTextCopyStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiTextCopyStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiTextCopyStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiTextCopyStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiTextCopyStateField.Magic)
			value = state.Magic;
		else if (field == MuiTextCopyStateField.Copy)
			value = state.Copy;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextCopyStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiTextCopyStateField.Magic)
			state.Magic = value;
		else if (field == MuiTextCopyStateField.Copy)
			state.Copy = value;
		else return false;
		return MuiTextCopyStateRecordCodec.WriteStructural(ref platform, record,
			state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextCopyStateRecord.Size -
			MuiTextCopyStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextCopyStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiTextCopyStateRecord.FieldSize);
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

internal static class MuiTextCopyStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextCopyStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Copy) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiTextCopyStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextCopyStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextCopyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Copy) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}

// Keep the wire value lossless for malformed-state diagnostics. MUIA_Text_Copy
// is a MorphOS BOOL and must be canonical before ownership or getter consumers
// use the named policy state.
internal static class MuiTextCopyStateValidation
{
	internal static bool IsValidRecord(MuiTextCopyStateRecord value) =>
		value.Copy <= 1;

	internal static bool IsValidState(MuiTextCopyState value) =>
		value.Copy <= 1;
}
