/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Canonical shared Area layout inputs.  Public MUI attributes remain the
// projection; min/max and weighted layout consume this one guest record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaLayoutPolicyStateRecord
{
	internal const uint Size = 48;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ShowMeOffset = 4;
	internal const uint FixWidthOffset = 8;
	internal const uint FixHeightOffset = 12;
	internal const uint MaxWidthOffset = 16;
	internal const uint MaxHeightOffset = 20;
	internal const uint InnerLeftOffset = 24;
	internal const uint InnerRightOffset = 28;
	internal const uint InnerTopOffset = 32;
	internal const uint InnerBottomOffset = 36;
	internal const uint HorizontalWeightOffset = 40;
	internal const uint VerticalWeightOffset = 44;
	internal const uint Cookie = 0x414C5053u; // 'ALPS'

	internal uint Magic;
	internal uint ShowMe;
	internal uint FixWidth;
	internal uint FixHeight;
	internal uint MaxWidth;
	internal uint MaxHeight;
	internal uint InnerLeft;
	internal uint InnerRight;
	internal uint InnerTop;
	internal uint InnerBottom;
	internal uint HorizontalWeight;
	internal uint VerticalWeight;
}

internal static class MuiAreaLayoutPolicyStateAdmission
{
	internal static bool Validate(MuiAreaLayoutPolicyStateRecord value) =>
		MuiAreaLayoutPolicyStateValidation.IsValidState(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiAreaLayoutPolicyStateValidation
{
	// MUIA_ShowMe is a public BOOL.  The remaining fields are ULONG inputs and
	// must remain lossless here; their range/relationship semantics are applied
	// by the layout consumers rather than silently rewritten at the ABI seam.
	internal static bool IsValidState(MuiAreaLayoutPolicyStateRecord value) =>
		value.Magic == MuiAreaLayoutPolicyStateRecord.Cookie && value.ShowMe <= 1;

	internal static bool IsValidRecord(MuiAreaLayoutPolicyStateRecord value) =>
		IsValidState(value);
}

internal enum MuiAreaLayoutPolicyField : byte
{
	Magic,
	ShowMe,
	FixWidth,
	FixHeight,
	MaxWidth,
	MaxHeight,
	InnerLeft,
	InnerRight,
	InnerTop,
	InnerBottom,
	HorizontalWeight,
	VerticalWeight,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaLayoutPolicyFieldCursor
{
	internal APTR Address;
	internal MuiAreaLayoutPolicyField Field;
}

internal static class MuiAreaLayoutPolicyFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaLayoutPolicyFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaLayoutPolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaLayoutPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaLayoutPolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaLayoutPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaLayoutPolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, field, value);
	}
}

// Struct-first guest-memory adapter. Layout consumers use the named policy
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiAreaLayoutPolicyStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaLayoutPolicyField field,
		out uint offset)
	{
		if (field == MuiAreaLayoutPolicyField.Magic)
			offset = MuiAreaLayoutPolicyStateRecord.MagicOffset;
		else if (field == MuiAreaLayoutPolicyField.ShowMe)
			offset = MuiAreaLayoutPolicyStateRecord.ShowMeOffset;
		else if (field == MuiAreaLayoutPolicyField.FixWidth)
			offset = MuiAreaLayoutPolicyStateRecord.FixWidthOffset;
		else if (field == MuiAreaLayoutPolicyField.FixHeight)
			offset = MuiAreaLayoutPolicyStateRecord.FixHeightOffset;
		else if (field == MuiAreaLayoutPolicyField.MaxWidth)
			offset = MuiAreaLayoutPolicyStateRecord.MaxWidthOffset;
		else if (field == MuiAreaLayoutPolicyField.MaxHeight)
			offset = MuiAreaLayoutPolicyStateRecord.MaxHeightOffset;
		else if (field == MuiAreaLayoutPolicyField.InnerLeft)
			offset = MuiAreaLayoutPolicyStateRecord.InnerLeftOffset;
		else if (field == MuiAreaLayoutPolicyField.InnerRight)
			offset = MuiAreaLayoutPolicyStateRecord.InnerRightOffset;
		else if (field == MuiAreaLayoutPolicyField.InnerTop)
			offset = MuiAreaLayoutPolicyStateRecord.InnerTopOffset;
		else if (field == MuiAreaLayoutPolicyField.InnerBottom)
			offset = MuiAreaLayoutPolicyStateRecord.InnerBottomOffset;
		else if (field == MuiAreaLayoutPolicyField.HorizontalWeight)
			offset = MuiAreaLayoutPolicyStateRecord.HorizontalWeightOffset;
		else if (field == MuiAreaLayoutPolicyField.VerticalWeight)
			offset = MuiAreaLayoutPolicyStateRecord.VerticalWeightOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaLayoutPolicyField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaLayoutPolicyStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaLayoutPolicyStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaLayoutPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaLayoutPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaLayoutPolicyField.Magic)
			value = state.Magic;
		else if (field == MuiAreaLayoutPolicyField.ShowMe)
			value = state.ShowMe;
		else if (field == MuiAreaLayoutPolicyField.FixWidth)
			value = state.FixWidth;
		else if (field == MuiAreaLayoutPolicyField.FixHeight)
			value = state.FixHeight;
		else if (field == MuiAreaLayoutPolicyField.MaxWidth)
			value = state.MaxWidth;
		else if (field == MuiAreaLayoutPolicyField.MaxHeight)
			value = state.MaxHeight;
		else if (field == MuiAreaLayoutPolicyField.InnerLeft)
			value = state.InnerLeft;
		else if (field == MuiAreaLayoutPolicyField.InnerRight)
			value = state.InnerRight;
		else if (field == MuiAreaLayoutPolicyField.InnerTop)
			value = state.InnerTop;
		else if (field == MuiAreaLayoutPolicyField.InnerBottom)
			value = state.InnerBottom;
		else if (field == MuiAreaLayoutPolicyField.HorizontalWeight)
			value = state.HorizontalWeight;
		else if (field == MuiAreaLayoutPolicyField.VerticalWeight)
			value = state.VerticalWeight;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaLayoutPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaLayoutPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaLayoutPolicyField.Magic)
			state.Magic = value;
		else if (field == MuiAreaLayoutPolicyField.ShowMe)
			state.ShowMe = value;
		else if (field == MuiAreaLayoutPolicyField.FixWidth)
			state.FixWidth = value;
		else if (field == MuiAreaLayoutPolicyField.FixHeight)
			state.FixHeight = value;
		else if (field == MuiAreaLayoutPolicyField.MaxWidth)
			state.MaxWidth = value;
		else if (field == MuiAreaLayoutPolicyField.MaxHeight)
			state.MaxHeight = value;
		else if (field == MuiAreaLayoutPolicyField.InnerLeft)
			state.InnerLeft = value;
		else if (field == MuiAreaLayoutPolicyField.InnerRight)
			state.InnerRight = value;
		else if (field == MuiAreaLayoutPolicyField.InnerTop)
			state.InnerTop = value;
		else if (field == MuiAreaLayoutPolicyField.InnerBottom)
			state.InnerBottom = value;
		else if (field == MuiAreaLayoutPolicyField.HorizontalWeight)
			state.HorizontalWeight = value;
		else if (field == MuiAreaLayoutPolicyField.VerticalWeight)
			state.VerticalWeight = value;
		else return false;
		return MuiAreaLayoutPolicyStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaLayoutPolicyStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaLayoutPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ShowMe) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FixWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FixHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MaxWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MaxHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.InnerLeft) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.InnerRight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.InnerTop) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.InnerBottom) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HorizontalWeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.VerticalWeight)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaLayoutPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ShowMe) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FixWidth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FixHeight) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MaxWidth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MaxHeight) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.InnerLeft) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.InnerRight) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.InnerTop) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.InnerBottom) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HorizontalWeight) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.VerticalWeight) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiAreaLayoutPolicyStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaLayoutPolicyStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
