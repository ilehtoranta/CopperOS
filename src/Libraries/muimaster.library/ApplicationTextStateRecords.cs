/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Mutable caller-owned Application text pointers. The strings remain in guest
// memory; this record only retains their validated APTR values.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationTextStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41545354u; // 'ATST'

	internal uint Magic;
	internal APTR HelpFile;
	internal APTR IconifyTitle;
}

internal enum MuiApplicationTextStateField : byte
{
	Magic,
	HelpFile,
	IconifyTitle,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationTextStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationTextStateField Field;
}

internal static class MuiApplicationTextStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationTextStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationTextStateField.Magic:
			case MuiApplicationTextStateField.HelpFile:
			case MuiApplicationTextStateField.IconifyTitle:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationTextStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationTextStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationTextStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationTextStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationTextStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationTextStateRecord.Size) ||
			!MuiApplicationTextStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationTextStateField.Magic, out var magic) ||
			magic != MuiApplicationTextStateRecord.Cookie ||
			!MuiApplicationTextStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationTextStateField.HelpFile, out var helpFile) ||
			!MuiApplicationTextStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationTextStateField.IconifyTitle,
				out var iconifyTitle)) return false;
		value.Magic = magic;
		value.HelpFile = APTR.FromPointer(helpFile);
		value.IconifyTitle = APTR.FromPointer(iconifyTitle);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationTextStateRecord.Size) || value.Magic !=
			MuiApplicationTextStateRecord.Cookie) return false;
		return MuiApplicationTextStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationTextStateField.Magic,
			value.Magic) &&
			MuiApplicationTextStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationTextStateField.HelpFile,
			value.HelpFile.Raw) &&
			MuiApplicationTextStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationTextStateField.IconifyTitle,
			value.IconifyTitle.Raw);
	}
}
