/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_Text_Copy is a public BOOL policy. Keep the normalized value in a
// named semantic view so Get/OM_GET, construction, and Text_Contents mutation
// share one source without exposing managed state or object offsets.
public struct MuiTextCopyState
{
	public uint Copy;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextCopyStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D544350u; // 'MTCP'

	internal uint Magic;
	internal uint Copy;
}

internal static class MuiTextCopyStateAdmission
{
	internal static bool Validate(MuiTextCopyStateRecord value) =>
		value.Magic == MuiTextCopyStateRecord.Cookie && value.Copy <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiTextCopyStateField : byte
{
	Magic,
	Copy,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextCopyStateFieldCursor
{
	internal APTR Record;
	internal MuiTextCopyStateField Field;
}

internal static class MuiTextCopyStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextCopyStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextCopyStateField.Magic => 0,
			MuiTextCopyStateField.Copy => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextCopyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiTextCopyStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextCopyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextCopyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Text consumers use the named copy-policy
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiTextCopyStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextCopyStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextCopyStateRecord.Size)) return false;
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

internal static class MuiTextCopyStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiTextCopyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) &&
			MuiTextCopyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Copy);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiTextCopyStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextCopyStateAdmission.Validate(value)) return false;
		return MuiTextCopyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiTextCopyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, value.Copy);
	}
}

// Keep the wire value lossless for malformed-state diagnostics. MUIA_Text_Copy
// is a MorphOS BOOL and must be canonical before ownership or getter consumers
// use the named policy state.
internal static class MuiTextCopyStateValidation
{
	internal static bool IsValidRecord(MuiTextCopyStateRecord value) =>
		value.Copy <= 1;

	internal static bool IsValidState(MuiTextCopyState value) =>
		value.Copy <= 1;
}
