/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct ResidentReadArgsResultRecord
{
	internal const uint Size = 9 * sizeof(uint);

	internal APTR Name;
	internal APTR File;
	internal APTR Alias;
	internal uint Remove;
	internal uint Add;
	internal uint Replace;
	internal uint Force;
	internal uint System;
	internal uint Defer;
}

internal static class ResidentReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out ResidentReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			ResidentReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Name) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.File) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Alias) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Remove) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Add) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Replace) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Force) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.System) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Defer) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct RunReadArgsResultRecord
{
	internal const uint Size = 5 * sizeof(uint);

	internal uint Detach;
	internal uint Quiet;
	internal APTR Stack;
	internal APTR Priority;
	internal APTR Command;
}

internal static class RunReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out RunReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			RunReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Detach) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Quiet) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Stack) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Priority) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Command) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct PathReadArgsResultRecord
{
	internal const uint Size = 6 * sizeof(uint);

	internal APTR Paths;
	internal uint Add;
	internal uint Show;
	internal uint Reset;
	internal uint Remove;
	internal uint Quiet;
}

internal static class PathReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out PathReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			PathReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Paths) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Add) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Show) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Reset) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Remove) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Quiet) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct PathReadArgsVectorCursor
{
	internal APTR Base;
	internal uint Index;
}

internal static class PathReadArgsVectorCursorCodec
{
	internal static bool TryReadCurrent<TPlatform>(ref TPlatform platform,
		ref PathReadArgsVectorCursor cursor, out APTR value,
		out bool hasValue)
		where TPlatform : struct, IShellPlatform
	{
		value = APTR.Null;
		hasValue = false;
		if (cursor.Base.IsNull || cursor.Index > PathCommand.MaximumPathEntries ||
			cursor.Index >= uint.MaxValue / sizeof(uint)) return false;
		var offset = cursor.Index * sizeof(uint);
		var byteLength = offset + sizeof(uint);
		if (cursor.Base.Raw > uint.MaxValue - byteLength ||
			!platform.IsMapped(cursor.Base, byteLength)) return false;
		value = APTR.FromPointer(platform.ReadUInt32(cursor.Base,
			unchecked((int)offset)));
		hasValue = value.IsNotNull;
		return true;
	}

	internal static bool TryAdvance(ref PathReadArgsVectorCursor cursor)
	{
		if (cursor.Index >= PathCommand.MaximumPathEntries) return false;
		cursor.Index++;
		return true;
	}
}
