/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident editor cursor state for String.mui.  The public attributes
// remain synchronized for MorphOS callers, while editing and drawing consume
// one validated record instead of treating two unrelated scalar slots as a
// private widget layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringCursorStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint BufferPosOffset = 4;
	internal const uint DisplayPosOffset = 8;
	internal const uint Cookie = 0x4D534352u; // 'MSCR'

	internal uint Magic;
	internal int BufferPos;
	internal int DisplayPos;
}

internal static class MuiStringCursorStateAdmission
{
	internal static bool Validate(MuiStringCursorStateRecord value) =>
		value.Magic == MuiStringCursorStateRecord.Cookie &&
		value.BufferPos >= 0 && value.DisplayPos >= 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiStringCursorStateField : byte
{
	Magic,
	BufferPos,
	DisplayPos,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringCursorStateFieldCursor
{
	internal APTR Record;
	internal MuiStringCursorStateField Field;
}

internal static class MuiStringCursorStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringCursorStateField field,
		out uint offset)
	{
		if (field == MuiStringCursorStateField.Magic)
			offset = MuiStringCursorStateRecord.MagicOffset;
		else if (field == MuiStringCursorStateField.BufferPos)
			offset = MuiStringCursorStateRecord.BufferPosOffset;
		else if (field == MuiStringCursorStateField.DisplayPos)
			offset = MuiStringCursorStateRecord.DisplayPosOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringCursorStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringCursorStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiStringCursorStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringCursorStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringCursorStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw))
			return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));
}

// Struct-first guest-memory adapter. Cursor positions remain named semantic
// fields; this bounded adapter owns fixed guest-layout translation.
internal static class MuiStringCursorStateRecordMemoryCodec
{
	private static bool TryResolve(MuiStringCursorStateField field,
		out uint offset)
	{
		if (field == MuiStringCursorStateField.Magic)
			offset = MuiStringCursorStateRecord.MagicOffset;
		else if (field == MuiStringCursorStateField.BufferPos)
			offset = MuiStringCursorStateRecord.BufferPosOffset;
		else if (field == MuiStringCursorStateField.DisplayPos)
			offset = MuiStringCursorStateRecord.DisplayPosOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiStringCursorStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiStringCursorStateField.Magic)
			value = state.Magic;
		else if (field == MuiStringCursorStateField.BufferPos)
			value = unchecked((uint)state.BufferPos);
		else if (field == MuiStringCursorStateField.DisplayPos)
			value = unchecked((uint)state.DisplayPos);
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringCursorStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiStringCursorStateField.Magic)
			state.Magic = value;
		else if (field == MuiStringCursorStateField.BufferPos)
			state.BufferPos = unchecked((int)value);
		else if (field == MuiStringCursorStateField.DisplayPos)
			state.DisplayPos = unchecked((int)value);
		else return false;
		return MuiStringCursorStateRecordCodec.WriteStructural(ref platform, record,
			state);
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw)) return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringCursorStateRecord.Size -
			MuiStringCursorStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringCursorStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringCursorStateRecord.FieldSize);
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

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, offset, out var raw)) return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, offset, unchecked((uint)value));
}

internal static class MuiStringCursorStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringCursorStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var bufferPos) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var displayPos) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.BufferPos = unchecked((int)bufferPos);
		value.DisplayPos = unchecked((int)displayPos);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringCursorStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringCursorStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringCursorStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.BufferPos)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.DisplayPos)) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);

}
