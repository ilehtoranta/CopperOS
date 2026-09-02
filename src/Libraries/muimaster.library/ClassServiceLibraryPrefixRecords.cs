/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
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

	// Resolve the variable class-id payload that follows the named four-byte
	// loader prefix. The wire arithmetic stays inside this adapter; callers
	// exchange the resulting bytes through a bounded string cursor.
	internal static bool TryGetPayloadAddress<TPlatform>(ref TPlatform platform,
		APTR address, out APTR payload)
		where TPlatform : struct, IMuiGuestMemory
	{
		payload = APTR.Null;
		if (address.IsNull || address.Raw >
			uint.MaxValue - MuiClassServiceLibraryPrefixRecord.Size ||
			!platform.IsMapped(address, MuiClassServiceLibraryPrefixRecord.Size))
			return false;
		payload = APTR.FromPointer(address.Raw +
			MuiClassServiceLibraryPrefixRecord.Size);
		return platform.IsMapped(payload, 1);
	}

	// Keep the named one-ULONG record API while routing the loader boundary
	// through the shared bounded ULONG codec. The prefix is still exposed as a
	// scalar helper so callers never need to handle guest slot addresses.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadPrefix<TPlatform>(ref TPlatform platform,
		APTR address, out uint prefix)
		where TPlatform : struct, IMuiGuestMemory
	{
		prefix = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceLibraryPrefixRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress)) return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, valueAddress,
			out prefix) && MuiGuestStructCursor.IsComplete(cursor);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WritePrefix<TPlatform>(ref TPlatform platform,
		APTR address, uint prefix)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceLibraryPrefixRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiGuestUlongStorage.Size, out var valueAddress)) return false;
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, valueAddress,
			prefix) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiClassServiceLibraryPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadPrefix(ref platform, address, out var prefix)) return false;
		value.Prefix = prefix;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiClassServiceLibraryPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WritePrefix(ref platform, address, value.Prefix);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiClassServiceLibraryPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiClassServiceLibraryPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
