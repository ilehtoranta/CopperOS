/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Sanitized Group-grid policy retained at the object boundary.  The public
// MUI attributes remain the source projection and the existing
// MuiGroupGridSpec remains the value view used by the grid algorithms.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupGridStateRecord
{
	internal const uint Size = 36;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ColumnsOffset = 4;
	internal const uint RowsOffset = 8;
	internal const uint HorizontalSpacingOffset = 12;
	internal const uint VerticalSpacingOffset = 16;
	internal const uint SameWidthOffset = 20;
	internal const uint SameHeightOffset = 24;
	internal const uint HorizontalCenterOffset = 28;
	internal const uint VerticalCenterOffset = 32;
	internal const uint Cookie = 0x47475250u; // 'GGRP'

	internal uint Magic;
	internal uint Columns;
	internal uint Rows;
	internal uint HorizontalSpacing;
	internal uint VerticalSpacing;
	internal uint SameWidth;
	internal uint SameHeight;
	internal uint HorizontalCenter;
	internal uint VerticalCenter;
}

internal static class MuiGroupGridStateValidation
{
	private const int DefaultSpacing = -100;
	private const int MaximumPixelSpacing = 10000;
	private const uint MaximumAxis = 256;

	private static bool IsBool(uint value) => value <= 1;

	private static bool IsSpacing(uint raw)
	{
		var value = unchecked((int)raw);
		if (value >= 0) return value <= MaximumPixelSpacing;
		return value >= DefaultSpacing;
	}

	internal static bool IsValidRecord(MuiGroupGridStateRecord value) =>
		value.Magic == MuiGroupGridStateRecord.Cookie &&
		value.Columns <= MaximumAxis && value.Rows <= MaximumAxis &&
		IsSpacing(value.HorizontalSpacing) &&
		IsSpacing(value.VerticalSpacing) && IsBool(value.SameWidth) &&
		IsBool(value.SameHeight) && value.HorizontalCenter <= 2 &&
		value.VerticalCenter <= 2;

	internal static bool IsValidState(MuiGroupGridStateRecord value) =>
		IsValidRecord(value);
}

internal static class MuiGroupGridStateAdmission
{
	internal static bool Validate(MuiGroupGridStateRecord value) =>
		MuiGroupGridStateValidation.IsValidRecord(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, group).IsNull;
}

internal enum MuiGroupGridStateField : byte
{
	Magic,
	Columns,
	Rows,
	HorizontalSpacing,
	VerticalSpacing,
	SameWidth,
	SameHeight,
	HorizontalCenter,
	VerticalCenter,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupGridStateFieldCursor
{
	internal APTR Address;
	internal MuiGroupGridStateField Field;
}

internal static class MuiGroupGridStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupGridStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupGridStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupGridStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupGridStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupGridStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupGridStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, field, value);
	}
}

// Struct-first guest-memory adapter. Named grid policy fields remain the
// semantic record; this bounded adapter owns fixed guest-layout translation.
internal static class MuiGroupGridStateRecordMemoryCodec
{
	private static bool TryResolve(MuiGroupGridStateField field,
		out uint offset)
	{
		if (field == MuiGroupGridStateField.Magic)
			offset = MuiGroupGridStateRecord.MagicOffset;
		else if (field == MuiGroupGridStateField.Columns)
			offset = MuiGroupGridStateRecord.ColumnsOffset;
		else if (field == MuiGroupGridStateField.Rows)
			offset = MuiGroupGridStateRecord.RowsOffset;
		else if (field == MuiGroupGridStateField.HorizontalSpacing)
			offset = MuiGroupGridStateRecord.HorizontalSpacingOffset;
		else if (field == MuiGroupGridStateField.VerticalSpacing)
			offset = MuiGroupGridStateRecord.VerticalSpacingOffset;
		else if (field == MuiGroupGridStateField.SameWidth)
			offset = MuiGroupGridStateRecord.SameWidthOffset;
		else if (field == MuiGroupGridStateField.SameHeight)
			offset = MuiGroupGridStateRecord.SameHeightOffset;
		else if (field == MuiGroupGridStateField.HorizontalCenter)
			offset = MuiGroupGridStateRecord.HorizontalCenterOffset;
		else if (field == MuiGroupGridStateField.VerticalCenter)
			offset = MuiGroupGridStateRecord.VerticalCenterOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiGroupGridStateRecord.Size) &&
			platform.IsMapped(address, MuiGroupGridStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupGridStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGroupGridStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupGridStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Columns) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Rows) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HorizontalSpacing) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.VerticalSpacing) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SameWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SameHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HorizontalCenter) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.VerticalCenter) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupGridStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Columns) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Rows) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.HorizontalSpacing) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.VerticalSpacing) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SameWidth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SameHeight) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.HorizontalCenter) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.VerticalCenter)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiGroupGridStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGroupGridStateAdmission.Validate(value) &&
			WriteRecord(ref platform, address, value);
	}
}
