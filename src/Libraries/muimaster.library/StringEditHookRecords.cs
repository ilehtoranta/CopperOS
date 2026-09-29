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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint EditHookOffset = 4;
	internal const uint LonelyEditHookOffset = 8;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringEditHookStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringEditHookStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringEditHookStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

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
	private static bool TryResolveFieldIndex(MuiStringEditHookStateField field,
		out uint index)
	{
		if (field == MuiStringEditHookStateField.Magic)
			index = 0;
		else if (field == MuiStringEditHookStateField.EditHook)
			index = 1;
		else if (field == MuiStringEditHookStateField.LonelyEditHook)
			index = 2;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringEditHookStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringEditHookStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringEditHookStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiStringEditHookStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiStringEditHookStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiStringEditHookStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringEditHookStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiStringEditHookStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringEditHookStateField.Magic)
			value = state.Magic;
		else if (field == MuiStringEditHookStateField.EditHook)
			value = state.EditHook.Raw;
		else if (field == MuiStringEditHookStateField.LonelyEditHook)
			value = state.LonelyEditHook;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringEditHookStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringEditHookStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiStringEditHookStateField.Magic)
			state.Magic = value;
		else if (field == MuiStringEditHookStateField.EditHook)
			state.EditHook = APTR.FromPointer(value);
		else if (field == MuiStringEditHookStateField.LonelyEditHook)
			state.LonelyEditHook = value;
		else return false;
		return MuiStringEditHookStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringEditHookStateRecord.Size -
			MuiStringEditHookStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringEditHookStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringEditHookStateRecord.FieldSize);
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
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringEditHookStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var hook) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.LonelyEditHook) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.EditHook = APTR.FromPointer(hook);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringEditHookStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringEditHookStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringEditHookStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.EditHook.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.LonelyEditHook) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringEditHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
