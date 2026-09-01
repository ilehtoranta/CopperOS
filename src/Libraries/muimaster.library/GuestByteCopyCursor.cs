/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Named bounded source/destination cursor for copying guest-owned byte spans.
// Specialist-owned C strings use this adapter so copy loops carry explicit
// bases, a logical index, and a complete length instead of repeating raw
// pointer arithmetic in each class implementation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGuestByteCopyCursor
{
	internal const uint MaximumLength = 16u * 1024u;
	internal APTR Source;
	internal APTR Destination;
	internal uint Index;
	internal uint Length;
}

internal static class MuiGuestByteCopyCursorCodec
{
	internal static bool TryCopyByte<TPlatform>(ref TPlatform platform,
		MuiGuestByteCopyCursor cursor)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (cursor.Length > MuiGuestByteCopyCursor.MaximumLength)
			return false;
		var source = default(MuiCStringByteCursor);
		source.Base = cursor.Source;
		source.Index = cursor.Index;
		source.Limit = cursor.Length;
		if (!MuiCStringByteCursorCodec.TryReadByte(ref platform, source,
			out var value))
			return false;
		var destination = default(MuiCStringByteCursor);
		destination.Base = cursor.Destination;
		destination.Index = cursor.Index;
		destination.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryWriteByte(ref platform, destination,
			value);
	}
}
