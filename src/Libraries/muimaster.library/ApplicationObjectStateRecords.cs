/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application-owned pointer relationships. DiskObject is a caller-owned
// Workbench structure; DropObject and Menustrip are guest MUI object pointers.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationObjectStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x414F5354u; // 'AOST'

	internal uint Magic;
	internal APTR DiskObject;
	internal APTR DropObject;
	internal APTR Menustrip;
}

internal enum MuiApplicationObjectStateField : byte
{
	Magic,
	DiskObject,
	DropObject,
	Menustrip,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationObjectStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationObjectStateField Field;
}

internal static class MuiApplicationObjectStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationObjectStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationObjectStateField.Magic:
			case MuiApplicationObjectStateField.DiskObject:
			case MuiApplicationObjectStateField.DropObject:
			case MuiApplicationObjectStateField.Menustrip:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationObjectStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationObjectStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationObjectStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationObjectStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationObjectStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationObjectStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationObjectStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationObjectStateRecord.Size) ||
			!MuiApplicationObjectStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationObjectStateField.Magic, out var magic) ||
			magic != MuiApplicationObjectStateRecord.Cookie ||
			!MuiApplicationObjectStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationObjectStateField.DiskObject,
				out var diskObject) ||
			!MuiApplicationObjectStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationObjectStateField.DropObject,
				out var dropObject) ||
			!MuiApplicationObjectStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationObjectStateField.Menustrip,
				out var menustrip)) return false;
		value.Magic = magic;
		value.DiskObject = APTR.FromPointer(diskObject);
		value.DropObject = APTR.FromPointer(dropObject);
		value.Menustrip = APTR.FromPointer(menustrip);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationObjectStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationObjectStateRecord.Size) || value.Magic !=
			MuiApplicationObjectStateRecord.Cookie) return false;
		return MuiApplicationObjectStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationObjectStateField.Magic,
			value.Magic) &&
			MuiApplicationObjectStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationObjectStateField.DiskObject,
			value.DiskObject.Raw) &&
			MuiApplicationObjectStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationObjectStateField.DropObject,
			value.DropObject.Raw) &&
			MuiApplicationObjectStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationObjectStateField.Menustrip,
			value.Menustrip.Raw);
	}
}
