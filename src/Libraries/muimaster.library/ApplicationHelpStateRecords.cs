/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application AboutMUI/ShowHelp state shared by the presentation methods.
// Guest pointers remain caller-owned; this record only retains validated
// references and saturating request counters.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationHelpStateRecord
{
	internal const uint Size = 32;
	internal const uint Cookie = 0x41485354u; // 'AHST'

	internal uint Magic;
	internal APTR AboutReferenceWindow;
	internal uint AboutRequests;
	internal APTR HelpWindow;
	internal APTR HelpName;
	internal APTR HelpNode;
	internal uint HelpLine;
	internal uint HelpRequests;
}

internal enum MuiApplicationHelpStateField : byte
{
	Magic,
	AboutReferenceWindow,
	AboutRequests,
	HelpWindow,
	HelpName,
	HelpNode,
	HelpLine,
	HelpRequests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationHelpStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationHelpStateField Field;
}

internal static class MuiApplicationHelpStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationHelpStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationHelpStateField.Magic:
			case MuiApplicationHelpStateField.AboutReferenceWindow:
			case MuiApplicationHelpStateField.AboutRequests:
			case MuiApplicationHelpStateField.HelpWindow:
			case MuiApplicationHelpStateField.HelpName:
			case MuiApplicationHelpStateField.HelpNode:
			case MuiApplicationHelpStateField.HelpLine:
			case MuiApplicationHelpStateField.HelpRequests:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationHelpStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiApplicationHelpStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationHelpStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationHelpStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationHelpStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationHelpStateRecord.Size) ||
			!MuiApplicationHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationHelpStateField.Magic, out var magic) ||
			magic != MuiApplicationHelpStateRecord.Cookie ||
			!MuiApplicationHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationHelpStateField.AboutReferenceWindow,
				out var aboutWindow) ||
			!MuiApplicationHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationHelpStateField.AboutRequests,
				out value.AboutRequests) ||
			!MuiApplicationHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationHelpStateField.HelpWindow,
				out var helpWindow) ||
			!MuiApplicationHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationHelpStateField.HelpName,
				out var helpName) ||
			!MuiApplicationHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationHelpStateField.HelpNode,
				out var helpNode) ||
			!MuiApplicationHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationHelpStateField.HelpLine,
				out value.HelpLine) ||
			!MuiApplicationHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationHelpStateField.HelpRequests,
				out value.HelpRequests)) return false;
		value.Magic = magic;
		value.AboutReferenceWindow = APTR.FromPointer(aboutWindow);
		value.HelpWindow = APTR.FromPointer(helpWindow);
		value.HelpName = APTR.FromPointer(helpName);
		value.HelpNode = APTR.FromPointer(helpNode);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationHelpStateRecord.Size) || value.Magic !=
			MuiApplicationHelpStateRecord.Cookie) return false;
		return MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationHelpStateField.Magic, value.Magic) &&
			MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationHelpStateField.AboutReferenceWindow,
			value.AboutReferenceWindow.Raw) &&
			MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationHelpStateField.AboutRequests,
			value.AboutRequests) &&
			MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationHelpStateField.HelpWindow,
			value.HelpWindow.Raw) &&
			MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationHelpStateField.HelpName,
			value.HelpName.Raw) &&
			MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationHelpStateField.HelpNode,
			value.HelpNode.Raw) &&
			MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationHelpStateField.HelpLine, value.HelpLine) &&
			MuiApplicationHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationHelpStateField.HelpRequests,
			value.HelpRequests);
	}
}
