/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUI_MakeObject creates a short control-string for button/label preparse.
// Keep the fixed four-byte scratch payload named so construction code does not
// treat the guest allocation as an anonymous sequence of byte offsets.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiMakeObjectPreParseRecord
{
	internal const uint Size = 4;
	internal const uint FieldSize = 1;

	internal byte Escape;
	internal byte Command;
	internal byte Terminator;
	internal byte Reserved;
}

internal enum MuiMakeObjectPreParseField : byte
{
	Escape,
	Command,
	Terminator,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiMakeObjectPreParseFieldCursor
{
	internal APTR Record;
	internal MuiMakeObjectPreParseField Field;
}

internal static class MuiMakeObjectPreParseRecordMemoryCodec
{
	private static bool TryResolve(MuiMakeObjectPreParseField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiMakeObjectPreParseField.Escape:
				offset = 0;
				return true;
			case MuiMakeObjectPreParseField.Command:
				offset = 1;
				return true;
			case MuiMakeObjectPreParseField.Terminator:
				offset = 2;
				return true;
			case MuiMakeObjectPreParseField.Reserved:
				offset = 3;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiMakeObjectPreParseFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiMakeObjectPreParseRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiMakeObjectPreParseRecord.FieldSize);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		APTR record, MuiMakeObjectPreParseField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiMakeObjectPreParseFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		APTR record, MuiMakeObjectPreParseField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiMakeObjectPreParseFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

internal static class MuiMakeObjectPreParseRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiMakeObjectPreParseRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMakeObjectPreParseRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var escape) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var command) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var reserved)) return false;
		value.Escape = escape;
		value.Command = command;
		value.Terminator = terminator;
		value.Reserved = reserved;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiMakeObjectPreParseRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMakeObjectPreParseRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Escape) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Command) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Reserved)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMakeObjectPreParseRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiMakeObjectPreParseRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
