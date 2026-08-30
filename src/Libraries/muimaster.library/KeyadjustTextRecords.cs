/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Keyadjust passes one translated character to its policy recorder as a
// temporary NUL-terminated text record. Keep both bytes named so the input
// path does not treat the scratch allocation as an anonymous offset pair.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiKeyadjustTextRecord
{
	internal const uint Size = 2;
	internal const uint FieldSize = 1;

	internal byte Character;
	internal byte Terminator;
}

internal enum MuiKeyadjustTextField : byte
{
	Character,
	Terminator,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiKeyadjustTextFieldCursor
{
	internal APTR Record;
	internal MuiKeyadjustTextField Field;
}

internal static class MuiKeyadjustTextRecordMemoryCodec
{
	private static bool TryResolve(MuiKeyadjustTextField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiKeyadjustTextField.Character:
				offset = 0;
				return true;
			case MuiKeyadjustTextField.Terminator:
				offset = 1;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiKeyadjustTextFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiKeyadjustTextRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiKeyadjustTextRecord.FieldSize);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		APTR record, MuiKeyadjustTextField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiKeyadjustTextFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		APTR record, MuiKeyadjustTextField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiKeyadjustTextFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

internal static class MuiKeyadjustTextRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiKeyadjustTextRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiKeyadjustTextRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character = character;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiKeyadjustTextRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiKeyadjustTextRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiKeyadjustTextRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiKeyadjustTextRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
