/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Stringscroll resolves the automatic scrollbar class through this fixed
// fourteen-byte C string. Keep the payload named while preserving its exact
// MorphOS wire size and big-endian longword contents.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollScrollbarClassNameRecord
{
	internal const uint Size = 14;

	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal byte TailCharacter;
	internal byte Terminator;
}

internal static class MuiStringscrollScrollbarClassNameRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiStringscrollScrollbarClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringscrollScrollbarClassNameRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word2) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var tailCharacter) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var terminator)) return false;
		value.TailCharacter = tailCharacter;
		value.Terminator = terminator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringscrollScrollbarClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringscrollScrollbarClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.TailCharacter) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Terminator) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollScrollbarClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollScrollbarClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
