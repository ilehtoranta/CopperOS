/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application_Window initializer relationship state. LastWindow mirrors the
// public attribute projection; AddedCount records accepted repeated
// initializer occurrences without introducing a managed collection.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowRelationshipStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41575254u; // 'AWRT'

	internal uint Magic;
	internal APTR LastWindow;
	internal uint AddedCount;
}

internal enum MuiApplicationWindowRelationshipStateField : byte
{
	Magic,
	LastWindow,
	AddedCount,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowRelationshipStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationWindowRelationshipStateField Field;
}

internal static class MuiApplicationWindowRelationshipStateFieldCursorCodec
{
	private static bool TryResolve(
		MuiApplicationWindowRelationshipStateField field, out uint offset)
	{
		switch (field)
		{
			case MuiApplicationWindowRelationshipStateField.Magic:
			case MuiApplicationWindowRelationshipStateField.LastWindow:
			case MuiApplicationWindowRelationshipStateField.AddedCount:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowRelationshipStateFieldCursor cursor,
		out APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationWindowRelationshipStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowRelationshipStateField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationWindowRelationshipStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowRelationshipStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationWindowRelationshipStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationWindowRelationshipStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowRelationshipStateRecord.Size) ||
			!MuiApplicationWindowRelationshipStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationWindowRelationshipStateField.Magic, out var magic) ||
			magic != MuiApplicationWindowRelationshipStateRecord.Cookie ||
			!MuiApplicationWindowRelationshipStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationWindowRelationshipStateField.LastWindow,
				out var lastWindow) ||
			!MuiApplicationWindowRelationshipStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationWindowRelationshipStateField.AddedCount,
				out value.AddedCount)) return false;
		value.Magic = magic;
		value.LastWindow = APTR.FromPointer(lastWindow);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationWindowRelationshipStateRecord.Size) || value.Magic !=
			MuiApplicationWindowRelationshipStateRecord.Cookie) return false;
		return MuiApplicationWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationWindowRelationshipStateField.Magic, value.Magic) &&
			MuiApplicationWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationWindowRelationshipStateField.LastWindow,
			value.LastWindow.Raw) &&
			MuiApplicationWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationWindowRelationshipStateField.AddedCount,
			value.AddedCount);
	}
}
