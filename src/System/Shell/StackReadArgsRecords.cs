/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

// SIZE/N produces either NULL or a pointer to the parsed LONG. Keep that
// distinction explicit instead of interpreting an anonymous result-array slot.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct StackReadArgsResultRecord
{
	internal const uint Size = sizeof(uint);

	internal APTR StackNumber;
}

internal static class StackReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out StackReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			StackReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.StackNumber) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor)) return false;
		return true;
	}
}
