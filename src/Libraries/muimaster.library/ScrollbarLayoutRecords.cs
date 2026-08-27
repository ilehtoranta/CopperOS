/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Scrollbar group geometry state.  The fields retain MorphOS ULONG
// semantics while child construction, layout, and drawing consume one named
// value instead of repeatedly decoding the Group/Scrollbar attributes.
public struct MuiScrollbarLayoutState
{
	public uint Horizontal;
	public uint Type;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollbarLayoutStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D534C59u; // 'MSLY'

	internal uint Magic;
	internal uint Horizontal;
	internal uint Type;
}

internal enum MuiScrollbarLayoutStateField : byte
{
	Magic,
	Horizontal,
	Type,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollbarLayoutStateFieldCursor
{
	internal APTR Record;
	internal MuiScrollbarLayoutStateField Field;
}

internal static class MuiScrollbarLayoutStateFieldCursorCodec
{
	private static bool TryResolve(MuiScrollbarLayoutStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiScrollbarLayoutStateField.Magic => 0,
			MuiScrollbarLayoutStateField.Horizontal => 4,
			MuiScrollbarLayoutStateField.Type => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollbarLayoutStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiScrollbarLayoutStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiScrollbarLayoutStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScrollbarLayoutStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Horizontal and Type remain named
// semantic fields; fixed guest-layout translation is bounded to this adapter.
internal static class MuiScrollbarLayoutStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiScrollbarLayoutStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiScrollbarLayoutStateRecord.Size)) return false;
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

internal static class MuiScrollbarLayoutStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiScrollbarLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic)) return false;
		return MuiScrollbarLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Horizontal) &&
			MuiScrollbarLayoutStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out value.Type);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiScrollbarLayoutStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScrollbarLayoutStateAdmission.Validate(value)) return false;
		return MuiScrollbarLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiScrollbarLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, value.Horizontal) &&
			MuiScrollbarLayoutStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 8, value.Type);
	}
}

// Keep the Scrollbar layout wire record lossless for malformed-state
// diagnostics, but admit only MorphOS's canonical Group_Horiz BOOL and the
// documented default/bottom/top/symmetric/none type values.
internal static class MuiScrollbarLayoutStateAdmission
{
	internal static bool Validate(MuiScrollbarLayoutStateRecord value)
	{
		if (value.Magic != MuiScrollbarLayoutStateRecord.Cookie) return false;
		var state = default(MuiScrollbarLayoutState);
		state.Horizontal = value.Horizontal;
		state.Type = value.Type;
		return Validate(state);
	}

	internal static bool Validate(MuiScrollbarLayoutState value) =>
		value.Horizontal <= 1 && value.Type <= 4;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing layout-only call sites. New Scrollbar
// state boundaries use MuiScrollbarLayoutStateAdmission directly so live
// ownership is explicit.
internal static class MuiScrollbarLayoutStateValidation
{
	internal static bool IsValidRecord(MuiScrollbarLayoutStateRecord value) =>
		MuiScrollbarLayoutStateAdmission.Validate(value);

	internal static bool IsValidState(MuiScrollbarLayoutState value) =>
		MuiScrollbarLayoutStateAdmission.Validate(value);
}
