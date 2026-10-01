/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Effective Group layout policy.  The public MUI attributes remain the source
// projection, while layout decisions consume this one named guest record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupLayoutPolicyStateRecord
{
	internal const uint Size = 28;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HorizontalOffset = 4;
	internal const uint HorizontalSpacingOffset = 8;
	internal const uint VerticalSpacingOffset = 12;
	internal const uint SameWidthOffset = 16;
	internal const uint SameHeightOffset = 20;
	internal const uint PageModeOffset = 24;
	internal const uint Cookie = 0x47524C50u; // 'GRLP'

	internal uint Magic;
	internal uint Horizontal;
	internal uint HorizontalSpacing;
	internal uint VerticalSpacing;
	internal uint SameWidth;
	internal uint SameHeight;
	internal uint PageMode;
}

internal static class MuiGroupLayoutPolicyStateValidation
{
	private const int DefaultSpacing = -100;
	private const int MaximumPixelSpacing = 10000;

	private static bool IsBool(uint value) => value <= 1;

	private static bool IsSpacing(uint raw)
	{
		var value = unchecked((int)raw);
		if (value >= 0) return value <= MaximumPixelSpacing;
		// MorphOS encodes the documented default and percentage spacing inputs as
		// signed LONG values.  -100 is the default sentinel; the remaining
		// negative range represents percentages from 1 through 100.
		return value >= DefaultSpacing;
	}

	internal static bool IsValidRecord(MuiGroupLayoutPolicyStateRecord value) =>
		value.Magic == MuiGroupLayoutPolicyStateRecord.Cookie &&
		IsBool(value.Horizontal) && IsSpacing(value.HorizontalSpacing) &&
		IsSpacing(value.VerticalSpacing) && IsBool(value.SameWidth) &&
		IsBool(value.SameHeight) && IsBool(value.PageMode);

	internal static bool IsValidState(MuiGroupLayoutPolicyStateRecord value) =>
		IsValidRecord(value);
}

internal static class MuiGroupLayoutPolicyStateAdmission
{
	internal static bool Validate(MuiGroupLayoutPolicyStateRecord value) =>
		MuiGroupLayoutPolicyStateValidation.IsValidRecord(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, group).IsNull;
}

internal enum MuiGroupLayoutPolicyField : byte
{
	Magic,
	Horizontal,
	HorizontalSpacing,
	VerticalSpacing,
	SameWidth,
	SameHeight,
	PageMode,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupLayoutPolicyFieldCursor
{
	internal APTR Address;
	internal MuiGroupLayoutPolicyField Field;
}

internal static class MuiGroupLayoutPolicyFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupLayoutPolicyFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupLayoutPolicyFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupLayoutPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupLayoutPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupLayoutPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupLayoutPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupLayoutPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, field, value);
	}
}

// Struct-first guest-memory adapter. Named layout policy fields remain the
// semantic record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiGroupLayoutPolicyStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiGroupLayoutPolicyField field,
		out uint index)
	{
		if (field == MuiGroupLayoutPolicyField.Magic)
			index = 0;
		else if (field == MuiGroupLayoutPolicyField.Horizontal)
			index = 1;
		else if (field == MuiGroupLayoutPolicyField.HorizontalSpacing)
			index = 2;
		else if (field == MuiGroupLayoutPolicyField.VerticalSpacing)
			index = 3;
		else if (field == MuiGroupLayoutPolicyField.SameWidth)
			index = 4;
		else if (field == MuiGroupLayoutPolicyField.SameHeight)
			index = 5;
		else if (field == MuiGroupLayoutPolicyField.PageMode)
			index = 6;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutPolicyField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupLayoutPolicyFieldCursor);
		cursor.Address = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupLayoutPolicyFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Address,
				MuiGroupLayoutPolicyStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiGroupLayoutPolicyStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiGroupLayoutPolicyStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGroupLayoutPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGroupLayoutPolicyField.Magic)
			value = state.Magic;
		else if (field == MuiGroupLayoutPolicyField.Horizontal)
			value = state.Horizontal;
		else if (field == MuiGroupLayoutPolicyField.HorizontalSpacing)
			value = state.HorizontalSpacing;
		else if (field == MuiGroupLayoutPolicyField.VerticalSpacing)
			value = state.VerticalSpacing;
		else if (field == MuiGroupLayoutPolicyField.SameWidth)
			value = state.SameWidth;
		else if (field == MuiGroupLayoutPolicyField.SameHeight)
			value = state.SameHeight;
		else if (field == MuiGroupLayoutPolicyField.PageMode)
			value = state.PageMode;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupLayoutPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupLayoutPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGroupLayoutPolicyField.Magic)
			state.Magic = value;
		else if (field == MuiGroupLayoutPolicyField.Horizontal)
			state.Horizontal = value;
		else if (field == MuiGroupLayoutPolicyField.HorizontalSpacing)
			state.HorizontalSpacing = value;
		else if (field == MuiGroupLayoutPolicyField.VerticalSpacing)
			state.VerticalSpacing = value;
		else if (field == MuiGroupLayoutPolicyField.SameWidth)
			state.SameWidth = value;
		else if (field == MuiGroupLayoutPolicyField.SameHeight)
			state.SameHeight = value;
		else if (field == MuiGroupLayoutPolicyField.PageMode)
			state.PageMode = value;
		else return false;
		return MuiGroupLayoutPolicyStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiGroupLayoutPolicyStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupLayoutPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Horizontal) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HorizontalSpacing) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.VerticalSpacing) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SameWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SameHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.PageMode)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupLayoutPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Horizontal) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HorizontalSpacing) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.VerticalSpacing) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SameWidth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SameHeight) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.PageMode) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiGroupLayoutPolicyStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiGroupLayoutPolicyStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
