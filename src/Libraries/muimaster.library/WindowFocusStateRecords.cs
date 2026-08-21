/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Window keyboard-focus projections.  ActiveObject and DefaultObject are
// caller-owned MUI object capabilities; the record keeps the validated guest
// pointers together without a managed object mirror.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowFocusStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x57464F53u; // 'WFOS'

	internal uint Magic;
	internal APTR ActiveObject;
	internal APTR DefaultObject;
}

internal enum MuiWindowFocusStateField : byte
{
	Magic,
	ActiveObject,
	DefaultObject,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowFocusStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowFocusStateField Field;
}

internal static class MuiWindowFocusStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowFocusStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowFocusStateField.Magic:
			case MuiWindowFocusStateField.ActiveObject:
			case MuiWindowFocusStateField.DefaultObject:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowFocusStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiWindowFocusStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowFocusStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowFocusStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowFocusStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowFocusStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowFocusStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull ||
			!MuiWindowFocusStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowFocusStateField.Magic, out var magic) ||
			magic != MuiWindowFocusStateRecord.Cookie ||
			!MuiWindowFocusStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowFocusStateField.ActiveObject, out var active) ||
			!MuiWindowFocusStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiWindowFocusStateField.DefaultObject, out var @default))
			return false;
		value.Magic = magic;
		value.ActiveObject = APTR.FromPointer(active);
		value.DefaultObject = APTR.FromPointer(@default);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowFocusStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || value.Magic != MuiWindowFocusStateRecord.Cookie)
			return false;
		return MuiWindowFocusStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiWindowFocusStateField.Magic, value.Magic) &&
			MuiWindowFocusStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiWindowFocusStateField.ActiveObject, value.ActiveObject.Raw) &&
			MuiWindowFocusStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiWindowFocusStateField.DefaultObject, value.DefaultObject.Raw);
	}
}
