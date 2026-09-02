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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HookOffset = 4;
	internal const uint Cookie = 0x47484F4Bu; // "GHOK"

	internal uint Magic;
	internal APTR Hook;
}

internal static class MuiGroupLayoutHookStateValidation
{
	internal static bool IsValidRecord(MuiGroupLayoutHookStateRecord value) =>
		value.Magic == MuiGroupLayoutHookStateRecord.Cookie;

	internal static bool IsValidState(MuiGroupLayoutHookStateRecord value) =>
		IsValidRecord(value);
}

internal static class MuiGroupLayoutHookStateAdmission
{
	internal static bool Validate(MuiGroupLayoutHookStateRecord value) =>
		MuiGroupLayoutHookStateValidation.IsValidRecord(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, group).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupLayoutHookStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupLayoutHookStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutHookStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupLayoutHookStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutHookStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupLayoutHookStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. The named hook relationship remains the
// semantic record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiGroupLayoutHookStateRecordMemoryCodec
{
	private static bool TryResolve(MuiGroupLayoutHookStateField field,
		out uint offset)
	{
		if (field == MuiGroupLayoutHookStateField.Magic)
			offset = MuiGroupLayoutHookStateRecord.MagicOffset;
		else if (field == MuiGroupLayoutHookStateField.Hook)
			offset = MuiGroupLayoutHookStateRecord.HookOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutHookStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiGroupLayoutHookStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiGroupLayoutHookStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutHookStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGroupLayoutHookStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGroupLayoutHookStateField.Magic)
			value = state.Magic;
		else if (field == MuiGroupLayoutHookStateField.Hook)
			value = state.Hook.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutHookStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupLayoutHookStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGroupLayoutHookStateField.Magic)
			state.Magic = value;
		else if (field == MuiGroupLayoutHookStateField.Hook)
			state.Hook = APTR.FromPointer(value);
		else return false;
		return MuiGroupLayoutHookStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiGroupLayoutHookStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupLayoutHookStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var hook)) return false;
		value.Hook = APTR.FromPointer(hook);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupLayoutHookStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Hook.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiGroupLayoutHookStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiGroupLayoutHookStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
