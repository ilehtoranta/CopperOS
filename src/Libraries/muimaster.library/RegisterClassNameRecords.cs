/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Register.mui class identity is a fixed thirteen-byte guest string.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiRegisterClassNameRecord
{
	internal const uint Size = 13;

	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal byte Terminator;
}

internal static class MuiRegisterClassNameRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiRegisterClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiRegisterClassNameRecord.Size, out var cursor) ||
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
		MuiRegisterClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiRegisterClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRegisterClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRegisterClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
