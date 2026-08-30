/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The NewMenu command-key compatibility path needs only the first character
// and its terminator. Keep that two-byte lookahead as a named record rather
// than exposing a consumer-side byte offset.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiMakeObjectMenuBarLabelRecord
{
	internal const uint Size = 2;
	internal byte Character;
	internal byte Terminator;
}

internal static class MuiMakeObjectMenuBarLabelRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiMakeObjectMenuBarLabelRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiMakeObjectMenuBarLabelRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out value.Character) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out value.Terminator)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiMakeObjectMenuBarLabelRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);
}
