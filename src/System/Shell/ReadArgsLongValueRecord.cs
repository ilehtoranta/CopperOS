/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

// ReadArgs /N returns a pointer to one signed 32-bit LONG. Keep that pointee
// layout named and validate its guest span in one codec before dereferencing.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct ReadArgsLongValueRecord
{
	internal const uint Size = sizeof(int);

	internal int Value;
}

internal static class ReadArgsLongValueRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out ReadArgsLongValueRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (address.IsNull || (address.Raw & 1u) != 0 ||
			address.Raw > uint.MaxValue -
			ReadArgsLongValueRecord.Size ||
			!platform.IsMapped(address, ReadArgsLongValueRecord.Size))
			return false;

		record.Value = unchecked((int)platform.ReadUInt32(address));
		return true;
	}
}
