/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Exact fixed class-id records used by the Color specialist family. These are
// guest C strings, but each official class identity has a fixed wire length;
// keeping that length in its own packed value avoids consumer-side byte walks.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiColorSpecialistPendisplayClassNameRecord
{
	internal const uint Size = 15;
	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal byte Character0;
	internal byte Character1;
	internal byte Terminator;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiColorSpecialistColorfieldClassNameRecord
{
	internal const uint Size = 15;
	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal byte Character0;
	internal byte Character1;
	internal byte Terminator;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistColoradjustClassNameRecord
{
	internal const uint Size = 16;
	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal uint Word3;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistPaletteClassNameRecord
{
	internal const uint Size = 12;
	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiColorSpecialistPenadjustClassNameRecord
{
	internal const uint Size = 14;
	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal byte Character;
	internal byte Terminator;
}

internal static class MuiColorSpecialistPendisplayClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x50656E64u && value.Word1 == 0x6973706Cu &&
			value.Word2 == 0x61792E6Du && value.Character0 == (byte)'u' &&
			value.Character1 == (byte)'i' && value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorSpecialistPendisplayClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistPendisplayClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2) ||
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
		MuiColorSpecialistPendisplayClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistPendisplayClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character0) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiColorSpecialistPendisplayClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorSpecialistPendisplayClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiColorSpecialistColorfieldClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x436F6C6Fu && value.Word1 == 0x72666965u &&
			value.Word2 == 0x6C642E6Du && value.Character0 == (byte)'u' &&
			value.Character1 == (byte)'i' && value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorSpecialistColorfieldClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistColorfieldClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2) ||
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
		MuiColorSpecialistColorfieldClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistColorfieldClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character0) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Character1) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiColorSpecialistColorfieldClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorSpecialistColorfieldClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiColorSpecialistColoradjustClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x436F6C6Fu && value.Word1 == 0x7261646Au &&
			value.Word2 == 0x7573742Eu && value.Word3 == 0x6D756900u;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorSpecialistColoradjustClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistColoradjustClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word3)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiColorSpecialistColoradjustClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistColoradjustClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word3) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiColorSpecialistColoradjustClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorSpecialistColoradjustClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiColorSpecialistPaletteClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x50616C65u && value.Word1 == 0x7474652Eu &&
			value.Word2 == 0x6D756900u;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorSpecialistPaletteClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistPaletteClassNameRecord.Size, out var cursor) ||
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
		MuiColorSpecialistPaletteClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistPaletteClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiColorSpecialistPaletteClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorSpecialistPaletteClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}

internal static class MuiColorSpecialistPenadjustClassNameRecordCodec
{
	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out var value)) return false;
		return value.Word0 == 0x50656E61u && value.Word1 == 0x646A7573u &&
			value.Word2 == 0x742E6D75u && value.Character == (byte)'i' &&
			value.Terminator == 0;
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiColorSpecialistPenadjustClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistPenadjustClassNameRecord.Size, out var cursor) ||
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
		MuiColorSpecialistPenadjustClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiColorSpecialistPenadjustClassNameRecord.Size, out var cursor) &&
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
		out MuiColorSpecialistPenadjustClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiColorSpecialistPenadjustClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
