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
	internal const uint HookSize = 20;

	internal uint Magic;
	internal APTR EditHook;
	internal uint LonelyEditHook;
}

internal static class MuiStringEditHookStateAdmission
{
	internal static bool Validate(MuiStringEditHookStateRecord value) =>
		value.Magic == MuiStringEditHookStateRecord.Cookie &&
		value.LonelyEditHook <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull &&
		(value.EditHook.IsNull || platform.IsMapped(value.EditHook,
			MuiStringEditHookStateRecord.HookSize));
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

// Struct-first guest-memory adapter. Hook and BOOL remain named semantic
// fields; this bounded adapter owns fixed guest-layout translation.
internal static class MuiStringEditHookStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringEditHookStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringEditHookStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringEditHookStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringEditHookStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiStringEditHookStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var hook) ||
			!MuiStringEditHookStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out value.LonelyEditHook)) return false;
		value.EditHook = APTR.FromPointer(hook);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiStringEditHookStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringEditHookStateAdmission.Validate(value)) return false;
		return MuiStringEditHookStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiStringEditHookStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 4, value.EditHook.Raw) &&
			MuiStringEditHookStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 8, value.LonelyEditHook);
	}
}
