/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands;

// Execute's FILE/A template yields a pointer to the DOS-owned file string.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct ExecuteReadArgsResultRecord
{
	internal const uint Size = sizeof(uint);

	internal APTR File;
}

internal static class ExecuteReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out ExecuteReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (address.IsNull ||
			address.Raw > uint.MaxValue - ExecuteReadArgsResultRecord.Size ||
			!platform.IsMapped(address, ExecuteReadArgsResultRecord.Size))
			return false;
		record.File = APTR.FromPointer(platform.ReadUInt32(address));
		return true;
	}
}
