/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Group.mui class identity is a fixed ten-byte guest string. Keep the wire
// shape named so hierarchy checks can consume one bounded record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupClassNameRecord
{
	internal const uint Size = 10;

	internal uint Word0;
	internal uint Word1;
	internal byte Character;
	internal byte Terminator;
}

internal static class MuiGroupClassNameRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var character) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.Character = character;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupClassNameRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Word0) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Word1) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.Character) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
