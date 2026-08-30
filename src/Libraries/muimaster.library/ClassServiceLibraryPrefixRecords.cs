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

	// CopperSharp's freestanding generic lowering has a known fault for a
	// one-ULONG struct crossing a by-value call boundary. Keep the public
	// named-record API, but expose scalar-safe cursor entry points for the
	// actual loader boundary and native proof.
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadPrefix<TPlatform>(ref TPlatform platform,
		APTR address, out uint prefix)
		where TPlatform : struct, IMuiGuestMemory
	{
		prefix = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceLibraryPrefixRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var high) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var low) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		prefix = ((uint)high << 16) | low;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool WritePrefix<TPlatform>(ref TPlatform platform,
		APTR address, uint prefix)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiClassServiceLibraryPrefixRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				(ushort)(prefix >> 16)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				(ushort)prefix)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
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
