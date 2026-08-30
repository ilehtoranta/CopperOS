/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_Image_Spec begins with a fixed kind byte and ':' separator. The
// remainder is a variable payload parsed by CommonControlCore.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiImageSpecPrefixRecord
{
	internal const uint Size = 2;
	internal byte Kind;
	internal byte Separator;
}

internal static class MuiImageSpecPrefixRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageSpecPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageSpecPrefixRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var kind) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var separator)) return false;
		value.Kind = kind;
		value.Separator = separator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiImageSpecPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageSpecPrefixRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Kind) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Separator)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageSpecPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);
}
