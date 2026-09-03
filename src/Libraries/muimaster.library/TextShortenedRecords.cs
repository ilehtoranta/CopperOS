/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Renderer-produced MUIA_Text_Shortened status.  The public attribute remains
// a guest ULONG, while the live value is kept in a named record so drawing and
// getters share one state seam without a private Text offset or managed flag.
public struct MuiTextShortenedState
{
	public uint Shortened;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextShortenedStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ShortenedOffset = 4;
	internal const uint Cookie = 0x4D545853u; // 'MTXS'

	internal uint Magic;
	internal uint Shortened;
}

internal enum MuiTextShortenedStateField : byte
{
	Magic,
	Shortened,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextShortenedStateFieldCursor
{
	internal APTR Record;
	internal MuiTextShortenedStateField Field;
}

internal static class MuiTextShortenedStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextShortenedStateField field,
		out uint offset)
	{
		if (field == MuiTextShortenedStateField.Magic)
			offset = MuiTextShortenedStateRecord.MagicOffset;
		else if (field == MuiTextShortenedStateField.Shortened)
			offset = MuiTextShortenedStateRecord.ShortenedOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextShortenedStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiTextShortenedStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiTextShortenedStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextShortenedStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextShortenedStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextShortenedStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextShortenedStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Text consumers use the named shortened
// status record; this bounded adapter is the only layer that translates its
// fixed guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiTextShortenedStateRecordMemoryCodec
{
	private static bool TryResolve(MuiTextShortenedStateField field,
		out uint offset)
	{
		if (field == MuiTextShortenedStateField.Magic)
			offset = MuiTextShortenedStateRecord.MagicOffset;
		else if (field == MuiTextShortenedStateField.Shortened)
			offset = MuiTextShortenedStateRecord.ShortenedOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextShortenedStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextShortenedStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiTextShortenedStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextShortenedStateField.Magic)
			value = state.Magic;
		else if (field == MuiTextShortenedStateField.Shortened)
			value = state.Shortened;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextShortenedStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextShortenedStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextShortenedStateField.Magic)
			state.Magic = value;
		else if (field == MuiTextShortenedStateField.Shortened)
			state.Shortened = value;
		else return false;
		return MuiTextShortenedStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextShortenedStateRecord.Size -
			MuiTextShortenedStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextShortenedStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiTextShortenedStateRecord.FieldSize);
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

internal static class MuiTextShortenedStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextShortenedStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Shortened) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiTextShortenedStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextShortenedStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextShortenedStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Shortened) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}

// Keep the wire value lossless for malformed-state diagnostics. The renderer's
// MUIA_Text_Shortened status is a MorphOS BOOL and must be canonical before
// getter or status consumers use the named record.
internal static class MuiTextShortenedStateValidation
{
	internal static bool IsValidRecord(MuiTextShortenedStateRecord value) =>
		value.Shortened <= 1;

	internal static bool IsValidState(MuiTextShortenedState value) =>
		value.Shortened <= 1;
}

internal static class MuiTextShortenedStateAdmission
{
	internal static bool Validate(MuiTextShortenedStateRecord value) =>
		value.Magic == MuiTextShortenedStateRecord.Cookie &&
		MuiTextShortenedStateValidation.IsValidRecord(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
