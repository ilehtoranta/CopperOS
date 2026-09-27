/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

// DOS ReadArgs stores each Echo template result as one ULONG-sized slot.
// Keep the semantic field order here so Echo does not depend on positional
// offsets into that result array.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct EchoReadArgsResultRecord
{
	internal const uint Size = 20;

	internal APTR MessageList;
	internal uint NoLine;
	internal APTR First;
	internal APTR Length;
	internal APTR To;
}

internal static class EchoReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out EchoReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			EchoReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.MessageList) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.NoLine) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.First) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Length) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.To) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

// ReadArgs /M returns a NULL-terminated guest pointer vector. Its cursor keeps
// the traversal position and bound explicit; only this codec translates an
// entry number to the ABI's ULONG-vector byte position.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct EchoReadArgsStringVectorCursor
{
	internal const uint MaximumEntries = 256;

	internal APTR Base;
	internal uint Index;
}

internal static class EchoReadArgsStringVectorCursorCodec
{
	internal static bool TryReadCurrent<TPlatform>(ref TPlatform platform,
		EchoReadArgsStringVectorCursor cursor, out APTR value,
		out bool hasValue)
		where TPlatform : struct, IShellPlatform
	{
		value = APTR.Null;
		hasValue = false;
		if (cursor.Base.IsNull || cursor.Index >=
			EchoReadArgsStringVectorCursor.MaximumEntries ||
			cursor.Index > uint.MaxValue / sizeof(uint)) return false;
		var offset = cursor.Index * sizeof(uint);
		if (offset > uint.MaxValue - sizeof(uint) ||
			cursor.Base.Raw > uint.MaxValue - offset - sizeof(uint) ||
			!platform.IsMapped(cursor.Base, offset + sizeof(uint))) return false;
		value = APTR.FromPointer(platform.ReadUInt32(cursor.Base,
			unchecked((int)offset)));
		hasValue = value.IsNotNull;
		return true;
	}

	internal static bool TryAdvance(ref EchoReadArgsStringVectorCursor cursor)
	{
		if (cursor.Index + 1 >= EchoReadArgsStringVectorCursor.MaximumEntries)
			return false;
		cursor.Index++;
		return true;
	}
}
