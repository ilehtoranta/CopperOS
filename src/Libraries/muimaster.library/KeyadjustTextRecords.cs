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
	internal const uint CharacterOffset = 0;
	internal const uint TerminatorOffset = 1;

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
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiKeyadjustTextField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field == MuiKeyadjustTextField.Character)
			return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiKeyadjustTextRecord.FieldSize, out address);
		if (field == MuiKeyadjustTextField.Terminator)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiKeyadjustTextRecord.FieldSize, out _)) return false;
			return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiKeyadjustTextRecord.FieldSize, out address);
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiKeyadjustTextFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
			MuiKeyadjustTextRecord.Size, out var guestCursor) ||
			!TryTakeField(ref platform, ref guestCursor, cursor.Field,
				out address)) return false;
		return true;
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		APTR record, MuiKeyadjustTextField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiKeyadjustTextRecordCodec.TryReadRecord(ref platform, record,
			out var state)) return false;
		if (field == MuiKeyadjustTextField.Character)
			value = state.Character;
		else if (field == MuiKeyadjustTextField.Terminator)
			value = state.Terminator;
		else return false;
		return true;
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		APTR record, MuiKeyadjustTextField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiKeyadjustTextRecordCodec.TryReadRecord(ref platform, record,
			out var state)) return false;
		if (field == MuiKeyadjustTextField.Character)
			state.Character = value;
		else if (field == MuiKeyadjustTextField.Terminator)
			state.Terminator = value;
		else return false;
		return MuiKeyadjustTextRecordCodec.WriteRecord(ref platform, record, state);
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
