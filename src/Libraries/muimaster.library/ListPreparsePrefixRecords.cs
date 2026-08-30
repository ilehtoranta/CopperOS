/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The List FORMAT PREPARSE parser recognizes a small set of fixed control
// prefixes before continuing through variable guest text. The third byte is
// optional because ESC and '*' forms need only a two-byte lookahead.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiListPreparsePrefixRecord
{
	internal const uint Size = 3;
	internal byte First;
	internal byte Second;
	internal byte Third;
}

internal static class MuiListPreparsePrefixRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		uint byteCount, out MuiListPreparsePrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (byteCount != 2 && byteCount != MuiListPreparsePrefixRecord.Size)
			return false;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address, byteCount,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var first) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var second)) return false;
		var third = (byte)0;
		if (byteCount == MuiListPreparsePrefixRecord.Size &&
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out third)) return false;
		value.First = first;
		value.Second = second;
		value.Third = third;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}
