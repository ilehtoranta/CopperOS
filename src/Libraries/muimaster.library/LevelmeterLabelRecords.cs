/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned Levelmeter label string.
public struct MuiLevelmeterLabelState
{
	public APTR Label;
}

// Guest-resident Levelmeter label state.  The bounded copy is retained in the
// object's LevelmeterLabelKey Dataspace entry.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterLabelStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D4C424Cu; // 'MLBL'

	internal uint Magic;
	internal APTR Label;
}

internal static class MuiLevelmeterLabelStateAdmission
{
	internal const int MaximumLength = 64;

	internal static bool Validate(MuiLevelmeterLabelStateRecord value) =>
		value.Magic == MuiLevelmeterLabelStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Label.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Label, MaximumLength, out _);
	}
}

internal enum MuiLevelmeterLabelStateField : byte
{
	Magic,
	Label,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterLabelStateFieldCursor
{
	internal APTR Record;
	internal MuiLevelmeterLabelStateField Field;
}

internal static class MuiLevelmeterLabelStateFieldCursorCodec
{
	private static bool TryResolve(MuiLevelmeterLabelStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiLevelmeterLabelStateField.Magic => 0,
			MuiLevelmeterLabelStateField.Label => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterLabelStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiLevelmeterLabelStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiLevelmeterLabelStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiLevelmeterLabelStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Levelmeter consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiLevelmeterLabelStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiLevelmeterLabelStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiLevelmeterLabelStateRecord.Size)) return false;
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

internal static class MuiLevelmeterLabelStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiLevelmeterLabelStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiLevelmeterLabelStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var label)) return false;
		value.Label = APTR.FromPointer(label);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiLevelmeterLabelStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiLevelmeterLabelStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiLevelmeterLabelStateAdmission.Validate(value)) return false;
		return MuiLevelmeterLabelStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiLevelmeterLabelStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.Label.Raw);
	}
}
