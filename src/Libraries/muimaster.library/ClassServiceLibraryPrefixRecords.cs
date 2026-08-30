/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed loader-path prefix used by ClassServiceCore. The class id appended
// after this record remains variable C-string data and is copied separately.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiClassServiceLibraryPrefixRecord
{
	internal const uint Size = 4;
	internal uint Prefix;
}

internal static class MuiClassServiceLibraryPrefixRecordCodec
{
	internal const uint MuiSlash = 0x6D75692Fu; // "mui/"

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiClassServiceLibraryPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiClassServiceLibraryPrefixRecord.Size)) return false;
		value.Prefix = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiClassServiceLibraryPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceLibraryPrefixRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				(ushort)(value.Prefix >> 16)) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				(ushort)value.Prefix) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiClassServiceLibraryPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiClassServiceLibraryPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
