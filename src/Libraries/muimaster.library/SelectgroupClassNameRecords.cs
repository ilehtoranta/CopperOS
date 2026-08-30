/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Selectgroup.mui class identity is a fixed sixteen-byte guest string.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSelectgroupClassNameRecord
{
	internal const uint Size = 16;

	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal uint Word3;
}

internal static class MuiSelectgroupClassNameRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiSelectgroupClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSelectgroupClassNameRecord.Size, out var cursor) ||
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
		MuiSelectgroupClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSelectgroupClassNameRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word3) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSelectgroupClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSelectgroupClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
