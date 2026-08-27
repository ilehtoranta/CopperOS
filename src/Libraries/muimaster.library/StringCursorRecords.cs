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
		offset = field switch
		{
			MuiStringCursorStateField.Magic => 0,
			MuiStringCursorStateField.BufferPos => 4,
			MuiStringCursorStateField.DisplayPos => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
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
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringCursorStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringCursorStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringCursorStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringCursorStateRecord.Size)) return false;
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
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringCursorStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic)) return false;
		return MuiStringCursorStateRecordMemoryCodec.TryReadInt32(ref platform,
			address, 4, out value.BufferPos) &&
			MuiStringCursorStateRecordMemoryCodec.TryReadInt32(ref platform,
			address, 8, out value.DisplayPos);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiStringCursorStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringCursorStateAdmission.Validate(value)) return false;
		return MuiStringCursorStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiStringCursorStateRecordMemoryCodec.TryWriteInt32(ref platform,
				address, 4, value.BufferPos) &&
			MuiStringCursorStateRecordMemoryCodec.TryWriteInt32(ref platform,
				address, 8, value.DisplayPos);
	}
}
