/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Transient AppMessage delivery and Window_AppWindow participation.  The
// message pointer remains valid only during synchronous publication; both
// values are retained in a named guest snapshot rather than a managed mirror.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMessageRoutingStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x414D5254u; // 'AMRT'

	internal uint Magic;
	internal APTR AppMessage;
	internal uint WindowAppWindow;
}

internal enum MuiApplicationMessageRoutingStateField : byte
{
	Magic,
	AppMessage,
	WindowAppWindow,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMessageRoutingStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationMessageRoutingStateField Field;
}

internal static class MuiApplicationMessageRoutingStateFieldCursorCodec
{
	private static bool TryResolve(
		MuiApplicationMessageRoutingStateField field, out uint offset)
	{
		switch (field)
		{
			case MuiApplicationMessageRoutingStateField.Magic:
			case MuiApplicationMessageRoutingStateField.AppMessage:
			case MuiApplicationMessageRoutingStateField.WindowAppWindow:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationMessageRoutingStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationMessageRoutingStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationMessageRoutingStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationMessageRoutingStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationMessageRoutingStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationMessageRoutingStateRecord.Size) ||
			!MuiApplicationMessageRoutingStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.Magic, out var magic) ||
			magic != MuiApplicationMessageRoutingStateRecord.Cookie ||
			!MuiApplicationMessageRoutingStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.AppMessage,
				out var appMessage) ||
			!MuiApplicationMessageRoutingStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.WindowAppWindow,
				out value.WindowAppWindow)) return false;
		value.Magic = magic;
		value.AppMessage = APTR.FromPointer(appMessage);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationMessageRoutingStateRecord.Size) || value.Magic !=
			MuiApplicationMessageRoutingStateRecord.Cookie) return false;
		return MuiApplicationMessageRoutingStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationMessageRoutingStateField.Magic, value.Magic) &&
			MuiApplicationMessageRoutingStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.AppMessage,
				value.AppMessage.Raw) &&
			MuiApplicationMessageRoutingStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.WindowAppWindow,
				value.WindowAppWindow);
	}
}
