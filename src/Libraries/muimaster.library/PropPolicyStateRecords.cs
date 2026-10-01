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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HorizontalOffset = 4;
	internal const uint DeltaFactorOffset = 8;
	internal const uint SliderOffset = 12;
	internal const uint UseWinBorderOffset = 16;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropPolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiPropPolicyStateRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

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
// semantic fields; bounded fixed guest-layout translation lives here. Enum
// field selection walks the packed record; the numeric overload below is a
// compatibility bridge for older callers that already carry an ABI offset.
internal static class MuiPropPolicyStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiPropPolicyStateField field,
		out uint index)
	{
		if (field == MuiPropPolicyStateField.Magic)
			index = 0;
		else if (field == MuiPropPolicyStateField.Horizontal)
			index = 1;
		else if (field == MuiPropPolicyStateField.DeltaFactor)
			index = 2;
		else if (field == MuiPropPolicyStateField.Slider)
			index = 3;
		else if (field == MuiPropPolicyStateField.UseWinBorder)
			index = 4;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiPropPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropPolicyStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiPropPolicyStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiPropPolicyStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiPropPolicyStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiPropPolicyStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiPropPolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiPropPolicyStateField.Horizontal)
			value = state.Horizontal;
		else if (field == MuiPropPolicyStateField.DeltaFactor)
			value = state.DeltaFactor;
		else if (field == MuiPropPolicyStateField.Slider)
			value = state.Slider;
		else if (field == MuiPropPolicyStateField.UseWinBorder)
			value = state.UseWinBorder;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiPropPolicyStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiPropPolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiPropPolicyStateField.Horizontal)
			state.Horizontal = value;
		else if (field == MuiPropPolicyStateField.DeltaFactor)
			state.DeltaFactor = value;
		else if (field == MuiPropPolicyStateField.Slider)
			state.Slider = value;
		else if (field == MuiPropPolicyStateField.UseWinBorder)
			state.UseWinBorder = value;
		else return false;
		return MuiPropPolicyStateRecordCodec.WriteRecord(ref platform, record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiPropPolicyStateRecord.Size -
			MuiPropPolicyStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiPropPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiPropPolicyStateRecord.FieldSize);
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
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPropPolicyStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Horizontal) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.DeltaFactor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Slider) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UseWinBorder) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPropPolicyStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Horizontal) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DeltaFactor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Slider) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.UseWinBorder) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRecord(ref platform, address, out value);
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
		return MuiPropPolicyStateAdmission.Validate(value) &&
			WriteRecord(ref platform, address, value);
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
