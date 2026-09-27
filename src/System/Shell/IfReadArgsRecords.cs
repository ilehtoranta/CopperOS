/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

// The MorphOS If template has eleven ULONG result slots.  Their meanings stay
// named here; only the codec knows that DOS lays them out sequentially.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct IfReadArgsResultRecord
{
	internal const uint Size = 11 * sizeof(uint);

	internal uint Not;
	internal uint Warn;
	internal uint Error;
	internal uint Fail;
	internal APTR Left;
	internal APTR Equal;
	internal APTR Greater;
	internal APTR GreaterEqual;
	internal uint Value;
	internal APTR Exists;
	internal uint NoRequester;
}

internal static class IfReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out IfReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			IfReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Not) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Warn) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Error) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Fail) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Left) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Equal) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Greater) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.GreaterEqual) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Value) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Exists) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.NoRequester) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}
