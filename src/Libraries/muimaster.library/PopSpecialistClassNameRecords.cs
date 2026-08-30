/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Exact fixed class-id records used by the Pop* specialist family. Each is a
// guest C string with a fixed wire length; named packed values keep the ABI
// boundary explicit and avoid consumer-side byte walks.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistPopstringClassNameRecord
{
	internal const uint Size = 14;
	internal uint Word0, Word1, Word2;
	internal byte Character, Terminator;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistPopobjectClassNameRecord
{
	internal const uint Size = 14;
	internal uint Word0, Word1, Word2;
	internal byte Character, Terminator;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistPoplistClassNameRecord
{
	internal const uint Size = 12;
	internal uint Word0, Word1, Word2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiPopSpecialistPopaslClassNameRecord
{
	internal const uint Size = 11;
	internal uint Word0, Word1;
	internal byte Character0, Character1, Terminator;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPopSpecialistPopscreenClassNameRecord
{
	internal const uint Size = 14;
	internal uint Word0, Word1, Word2;
	internal byte Character, Terminator;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiPopSpecialistPopcolorClassNameRecord
{
	internal const uint Size = 13;
	internal uint Word0, Word1, Word2;
	internal byte Terminator;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiPopSpecialistPoppenClassNameRecord
{
	internal const uint Size = 11;
	internal uint Word0, Word1;
	internal byte Character0, Character1, Terminator;
}

internal static class MuiPopSpecialistPopstringClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x506F7073u && value.Word1 == 0x7472696Eu &&
			value.Word2 == 0x672E6D75u && value.Character == (byte)'i' &&
			value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiPopSpecialistPopstringClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopstringClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character = character;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiPopSpecialistPopstringClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopstringClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPopSpecialistPopstringClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPopSpecialistPopstringClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiPopSpecialistPopobjectClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x506F706Fu && value.Word1 == 0x626A6563u &&
			value.Word2 == 0x742E6D75u && value.Character == (byte)'i' && value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiPopSpecialistPopobjectClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopobjectClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character = character;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiPopSpecialistPopobjectClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopobjectClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPopSpecialistPopobjectClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPopSpecialistPopobjectClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiPopSpecialistPoplistClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x506F706Cu && value.Word1 == 0x6973742Eu && value.Word2 == 0x6D756900u;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiPopSpecialistPoplistClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPoplistClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiPopSpecialistPoplistClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPoplistClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPopSpecialistPoplistClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPopSpecialistPoplistClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiPopSpecialistPopaslClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x506F7061u && value.Word1 == 0x736C2E6Du &&
			value.Character0 == (byte)'u' && value.Character1 == (byte)'i' && value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiPopSpecialistPopaslClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopaslClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character0) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character0 = character0;
		value.Character1 = character1;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiPopSpecialistPopaslClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopaslClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character0) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPopSpecialistPopaslClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPopSpecialistPopaslClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiPopSpecialistPopscreenClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x506F7073u && value.Word1 == 0x63726565u &&
			value.Word2 == 0x6E2E6D75u && value.Character == (byte)'i' && value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiPopSpecialistPopscreenClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopscreenClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character = character;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiPopSpecialistPopscreenClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopscreenClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPopSpecialistPopscreenClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPopSpecialistPopscreenClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiPopSpecialistPopcolorClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x506F7063u && value.Word1 == 0x6F6C6F72u && value.Word2 == 0x2E6D7569u && value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiPopSpecialistPopcolorClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopcolorClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiPopSpecialistPopcolorClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPopcolorClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPopSpecialistPopcolorClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPopSpecialistPopcolorClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiPopSpecialistPoppenClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x506F7070u && value.Word1 == 0x656E2E6Du &&
			value.Character0 == (byte)'u' && value.Character1 == (byte)'i' && value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiPopSpecialistPoppenClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPoppenClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character0) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character0 = character0;
		value.Character1 = character1;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiPopSpecialistPoppenClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPopSpecialistPoppenClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character0) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPopSpecialistPoppenClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPopSpecialistPoppenClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
