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
	private static bool TryResolve(MuiGroupGridStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiGroupGridStateField.Magic:
			case MuiGroupGridStateField.Columns:
			case MuiGroupGridStateField.Rows:
			case MuiGroupGridStateField.HorizontalSpacing:
			case MuiGroupGridStateField.VerticalSpacing:
			case MuiGroupGridStateField.SameWidth:
			case MuiGroupGridStateField.SameHeight:
			case MuiGroupGridStateField.HorizontalCenter:
			case MuiGroupGridStateField.VerticalCenter:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupGridStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiGroupGridStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupGridStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupGridStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupGridStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupGridStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiGroupGridStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupGridStateRecord.Size) ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.Magic, out var magic) ||
			magic != MuiGroupGridStateRecord.Cookie ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.Columns, out value.Columns) ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.Rows, out value.Rows) ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.HorizontalSpacing,
				out value.HorizontalSpacing) ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.VerticalSpacing,
				out value.VerticalSpacing) ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.SameWidth, out value.SameWidth) ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.SameHeight, out value.SameHeight) ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.HorizontalCenter,
				out value.HorizontalCenter) ||
			!MuiGroupGridStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGroupGridStateField.VerticalCenter,
				out value.VerticalCenter)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupGridStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupGridStateRecord.Size) ||
			value.Magic != MuiGroupGridStateRecord.Cookie) return false;
		return MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupGridStateField.Magic, value.Magic) &&
			MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridStateField.Columns, value.Columns) &&
			MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridStateField.Rows, value.Rows) &&
			MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridStateField.HorizontalSpacing, value.HorizontalSpacing) &&
			MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridStateField.VerticalSpacing, value.VerticalSpacing) &&
			MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridStateField.SameWidth, value.SameWidth) &&
			MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridStateField.SameHeight, value.SameHeight) &&
			MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridStateField.HorizontalCenter, value.HorizontalCenter) &&
			MuiGroupGridStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupGridStateField.VerticalCenter, value.VerticalCenter);
	}
}
