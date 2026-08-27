/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Levelmeter presentation state. Numeric range/value data remains in
// MuiNumericState; this record carries the Gauge_Horiz orientation consumed by
// Levelmeter rendering.
public struct MuiLevelmeterPresentationState
{
	public uint Horizontal;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterPresentationStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D4C564Cu; // 'MLVL'

	internal uint Magic;
	internal uint Horizontal;
}

internal enum MuiLevelmeterPresentationStateField : byte
{
	Magic,
	Horizontal,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiLevelmeterPresentationStateField Field;
}

internal static class MuiLevelmeterPresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiLevelmeterPresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiLevelmeterPresentationStateField.Magic => 0,
			MuiLevelmeterPresentationStateField.Horizontal => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiLevelmeterPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiLevelmeterPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiLevelmeterPresentationStateFieldCursor);
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
internal static class MuiLevelmeterPresentationStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiLevelmeterPresentationStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiLevelmeterPresentationStateRecord.Size)) return false;
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

internal static class MuiLevelmeterPresentationStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiLevelmeterPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 0, out value.Magic) &&
			MuiLevelmeterPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out value.Horizontal);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiLevelmeterPresentationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiLevelmeterPresentationStateAdmission.Validate(value)) return false;
		return MuiLevelmeterPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiLevelmeterPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, value.Horizontal);
	}
}

// Keep the wire field lossless for malformed-state diagnostics. Levelmeter's
// Gauge_Horiz value is a MorphOS BOOL and must be canonical before layout or
// drawing consumers use the named presentation state.
internal static class MuiLevelmeterPresentationStateAdmission
{
	internal static bool Validate(MuiLevelmeterPresentationStateRecord value) =>
		value.Magic == MuiLevelmeterPresentationStateRecord.Cookie &&
		value.Horizontal <= 1;

	internal static bool Validate(MuiLevelmeterPresentationState value) =>
		value.Horizontal <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing Levelmeter presentation call sites. New
// state boundaries use MuiLevelmeterPresentationStateAdmission directly so
// live ownership is explicit.
internal static class MuiLevelmeterPresentationStateValidation
{
	internal static bool IsValidRecord(MuiLevelmeterPresentationStateRecord value) =>
		MuiLevelmeterPresentationStateAdmission.Validate(value);

	internal static bool IsValidState(MuiLevelmeterPresentationState value) =>
		MuiLevelmeterPresentationStateAdmission.Validate(value);
}
