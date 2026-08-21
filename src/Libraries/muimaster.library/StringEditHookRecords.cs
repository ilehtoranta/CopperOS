/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui edit-hook policy.  Hook remains a caller-owned
// guest struct Hook; LonelyEditHook is a canonical BOOL.  Keeping both in a
// named record avoids reconstructing policy from private widget offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringEditHookStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D534548u; // 'MSEH'

	internal uint Magic;
	internal APTR EditHook;
	internal uint LonelyEditHook;
}

internal enum MuiStringEditHookStateField : byte
{
	Magic,
	EditHook,
	LonelyEditHook,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringEditHookStateFieldCursor
{
	internal APTR Record;
	internal MuiStringEditHookStateField Field;
}

internal static class MuiStringEditHookStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringEditHookStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringEditHookStateField.Magic => 0,
			MuiStringEditHookStateField.EditHook => 4,
			MuiStringEditHookStateField.LonelyEditHook => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringEditHookStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringEditHookStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringEditHookStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringEditHookStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringEditHookStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringEditHookStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringEditHookStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringEditHookStateRecord.Size) ||
			!MuiStringEditHookStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringEditHookStateField.Magic, out var magic) ||
			magic != MuiStringEditHookStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiStringEditHookStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringEditHookStateField.EditHook, out var hook) ||
			!MuiStringEditHookStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditHookStateField.LonelyEditHook,
				out value.LonelyEditHook)) return false;
		value.EditHook = APTR.FromPointer(hook);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringEditHookStateRecord.Size) || value.Magic !=
			MuiStringEditHookStateRecord.Cookie) return false;
		return MuiStringEditHookStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiStringEditHookStateField.Magic,
			value.Magic) &&
			MuiStringEditHookStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiStringEditHookStateField.EditHook,
				value.EditHook.Raw) &&
			MuiStringEditHookStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiStringEditHookStateField.LonelyEditHook,
				value.LonelyEditHook);
	}
}
