/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of Prop/Scrollbar policy scalars. Horizontal is the
// initializer-only orientation relationship; DeltaFactor and Slider remain
// runtime-settable, while UseWinBorder retains MorphOS's init-only policy.
public struct MuiPropPolicyState
{
	public uint Horizontal;
	public uint DeltaFactor;
	public uint Slider;
	public uint UseWinBorder;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPropPolicyStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x4D50504Cu; // 'MPPL'

	internal uint Magic;
	internal uint Horizontal;
	internal uint DeltaFactor;
	internal uint Slider;
	internal uint UseWinBorder;
}

internal enum MuiPropPolicyStateField : byte
{
	Magic,
	Horizontal,
	DeltaFactor,
	Slider,
	UseWinBorder,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPropPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiPropPolicyStateField Field;
}

internal static class MuiPropPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiPropPolicyStateField field,
		out uint offset)

	{
		offset = field switch
		{
			MuiPropPolicyStateField.Magic => 0,
			MuiPropPolicyStateField.Horizontal => 4,
			MuiPropPolicyStateField.DeltaFactor => 8,
			MuiPropPolicyStateField.Slider => 12,
			MuiPropPolicyStateField.UseWinBorder => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiPropPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiPropPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiPropPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Prop policy scalars remain named
// semantic fields; bounded fixed guest-layout translation lives here.
internal static class MuiPropPolicyStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiPropPolicyStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiPropPolicyStateRecord.Size)) return false;
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

internal static class MuiPropPolicyStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiPropPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic)) return false;
		return MuiPropPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Horizontal) &&
			MuiPropPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				8, out value.DeltaFactor) &&
			MuiPropPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				12, out value.Slider) &&
			MuiPropPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				16, out value.UseWinBorder);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiPropPolicyStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiPropPolicyStateAdmission.Validate(value)) return false;
		return MuiPropPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiPropPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			4, value.Horizontal) &&
			MuiPropPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			8, value.DeltaFactor) &&
			MuiPropPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			12, value.Slider) &&
			MuiPropPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			16, value.UseWinBorder);
	}
}

// Preserve the policy wire fields losslessly for malformed-state diagnostics.
// Horizontal and Slider are canonical MorphOS BOOLs; UseWinBorder is the
// existing four-state policy domain accepted by Prop/Scrollbar construction;
// DeltaFactor is an unrestricted LONG multiplier.
internal static class MuiPropPolicyStateAdmission
{
	internal static bool Validate(MuiPropPolicyStateRecord value) =>
		value.Magic == MuiPropPolicyStateRecord.Cookie &&
		value.Horizontal <= 1 && value.Slider <= 1 && value.UseWinBorder <= 3;

	internal static bool Validate(MuiPropPolicyState value) =>
		value.Horizontal <= 1 && value.Slider <= 1 && value.UseWinBorder <= 3;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing range-only call sites. New policy
// boundaries use MuiPropPolicyStateAdmission directly so live ownership is
// explicit at the consumer edge.
internal static class MuiPropPolicyStateValidation
{
	internal static bool IsValidRecord(MuiPropPolicyStateRecord value) =>
		MuiPropPolicyStateAdmission.Validate(value);

	internal static bool IsValidState(MuiPropPolicyState value) =>
		MuiPropPolicyStateAdmission.Validate(value);
}
