/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Window-owned object relationships.  These pointers remain caller-owned
// guest objects; the record is the named ABI snapshot consumed by public
// getters and relationship transitions.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowRelationshipStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x57524C54u; // 'WRLT'

	internal uint Magic;
	internal APTR RootObject;
	internal APTR Menustrip;
	internal APTR RefWindow;
}

internal enum MuiWindowRelationshipStateField : byte
{
	Magic,
	RootObject,
	Menustrip,
	RefWindow,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowRelationshipStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowRelationshipStateField Field;
}

internal static class MuiWindowRelationshipStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowRelationshipStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowRelationshipStateField.Magic:
			case MuiWindowRelationshipStateField.RootObject:
			case MuiWindowRelationshipStateField.Menustrip:
			case MuiWindowRelationshipStateField.RefWindow:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowRelationshipStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowRelationshipStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowRelationshipStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowRelationshipStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowRelationshipStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowRelationshipStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowRelationshipStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowRelationshipStateRecord.Size) ||
			!MuiWindowRelationshipStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowRelationshipStateField.Magic, out var magic) ||
			magic != MuiWindowRelationshipStateRecord.Cookie ||
			!MuiWindowRelationshipStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowRelationshipStateField.RootObject,
				out var rootObject) ||
			!MuiWindowRelationshipStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowRelationshipStateField.Menustrip,
				out var menustrip) ||
			!MuiWindowRelationshipStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowRelationshipStateField.RefWindow,
				out var refWindow)) return false;
		value.Magic = magic;
		value.RootObject = APTR.FromPointer(rootObject);
		value.Menustrip = APTR.FromPointer(menustrip);
		value.RefWindow = APTR.FromPointer(refWindow);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiWindowRelationshipStateRecord.Size) || value.Magic !=
			MuiWindowRelationshipStateRecord.Cookie) return false;
		return MuiWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiWindowRelationshipStateField.Magic,
			value.Magic) &&
			MuiWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowRelationshipStateField.RootObject,
				value.RootObject.Raw) &&
			MuiWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowRelationshipStateField.Menustrip,
				value.Menustrip.Raw) &&
			MuiWindowRelationshipStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiWindowRelationshipStateField.RefWindow,
				value.RefWindow.Raw);
	}
}
