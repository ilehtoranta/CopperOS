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
		switch (field)
		{
			case MuiGroupLayoutHookStateField.Magic:
				offset = MuiGroupLayoutHookStateRecord.MagicOffset;
				return true;
			case MuiGroupLayoutHookStateField.Hook:
				offset = MuiGroupLayoutHookStateRecord.HookOffset;
				return true;
		}
		offset = 0;
		return false;
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
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutHookStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGroupLayoutHookStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGroupLayoutHookStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiGroupLayoutHookStateField.Magic, out value.Magic) ||
			!MuiGroupLayoutHookStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiGroupLayoutHookStateField.Hook, out var hook)) return false;
		value.Hook = APTR.FromPointer(hook);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiGroupLayoutHookStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupLayoutHookStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupLayoutHookStateAdmission.Validate(value)) return false;
		return MuiGroupLayoutHookStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiGroupLayoutHookStateField.Magic, value.Magic) &&
			MuiGroupLayoutHookStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiGroupLayoutHookStateField.Hook, value.Hook.Raw);
	}
}
