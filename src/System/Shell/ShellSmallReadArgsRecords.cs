/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.Shell;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct ReadArgsPointerResultRecord
{
	internal const uint Size = sizeof(uint);

	internal APTR Value;
}

internal static class ReadArgsPointerResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out ReadArgsPointerResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			ReadArgsPointerResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Value) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct ReadArgsWordResultRecord
{
	internal const uint Size = sizeof(uint);

	internal uint Value;
}

internal static class ReadArgsWordResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out ReadArgsWordResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			ReadArgsWordResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Value) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct EmptyReadArgsResultRecord
{
	internal const uint Size = sizeof(uint);

	internal uint Unused;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct NameAndValueReadArgsResultRecord
{
	internal const uint Size = 2 * sizeof(uint);

	internal APTR Name;
	internal APTR Value;
}

internal static class NameAndValueReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out NameAndValueReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			NameAndValueReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Name) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Value) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct WindowFromReadArgsResultRecord
{
	internal const uint Size = 2 * sizeof(uint);

	internal APTR Window;
	internal APTR From;
}

internal static class WindowFromReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out WindowFromReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			WindowFromReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Window) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.From) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct SetenvReadArgsResultRecord
{
	internal const uint Size = 3 * sizeof(uint);

	internal APTR Name;
	internal uint Save;
	internal APTR Value;
}

internal static class SetenvReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out SetenvReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			SetenvReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Name) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Save) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Value) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct UnsetenvReadArgsResultRecord
{
	internal const uint Size = 2 * sizeof(uint);

	internal APTR Name;
	internal uint Save;
}

internal static class UnsetenvReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out UnsetenvReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			UnsetenvReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Name) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Save) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct SkipReadArgsResultRecord
{
	internal const uint Size = 2 * sizeof(uint);

	internal APTR Label;
	internal uint Back;
}

internal static class SkipReadArgsResultRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out SkipReadArgsResultRecord record)
		where TPlatform : struct, IShellPlatform
	{
		record = default;
		if (!ShellReadArgsResultCursorCodec.TryCreate(ref platform, address,
			SkipReadArgsResultRecord.Size, out var cursor) ||
			!ShellReadArgsResultCursorCodec.TryReadPointer(ref platform,
				ref cursor, out record.Label) ||
			!ShellReadArgsResultCursorCodec.TryReadUInt32(ref platform,
				ref cursor, out record.Back) ||
			!ShellReadArgsResultCursorCodec.IsComplete(cursor))
		{
			record = default;
			return false;
		}
		return true;
	}
}
