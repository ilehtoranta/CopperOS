/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The initialize-only MUIA_Group_LayoutHook value is retained in a small
// guest-resident record.  Keeping the hook pointer named makes the layout
// bridge independent of the generic attribute-list slot and gives Get/OM_GET
// one stable, struct-shaped projection.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupLayoutHookStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x47484F4Bu; // "GHOK"

	internal uint Magic;
	internal APTR Hook;
}

internal enum MuiGroupLayoutHookStateField : byte
{
	Magic,
	Hook,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupLayoutHookStateFieldCursor
{
	internal APTR Record;
	internal MuiGroupLayoutHookStateField Field;
}

internal static class MuiGroupLayoutHookStateFieldCursorCodec
{
	private static bool TryResolve(MuiGroupLayoutHookStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiGroupLayoutHookStateField.Magic:
				offset = 0;
				return true;
			case MuiGroupLayoutHookStateField.Hook:
				offset = 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupLayoutHookStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			!platform.IsMapped(cursor.Record, MuiGroupLayoutHookStateRecord.Size))
			return false;
		if (cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutHookStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupLayoutHookStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutHookStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupLayoutHookStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGroupLayoutHookStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull ||
			!MuiGroupLayoutHookStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutHookStateField.Magic, out var magic) ||
			magic != MuiGroupLayoutHookStateRecord.Cookie ||
			!MuiGroupLayoutHookStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutHookStateField.Hook, out var hook)) return false;
		value.Magic = magic;
		value.Hook = APTR.FromPointer(hook);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || value.Magic != MuiGroupLayoutHookStateRecord.Cookie ||
			!MuiGroupLayoutHookStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGroupLayoutHookStateField.Magic, value.Magic)) return false;
		return MuiGroupLayoutHookStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupLayoutHookStateField.Hook, value.Hook.Raw);
	}
}
