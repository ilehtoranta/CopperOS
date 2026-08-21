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
	internal const uint Cookie = 0x47524C50u; // 'GRLP'

	internal uint Magic;
	internal uint Horizontal;
	internal uint HorizontalSpacing;
	internal uint VerticalSpacing;
	internal uint SameWidth;
	internal uint SameHeight;
	internal uint PageMode;
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
	private static bool TryResolve(MuiGroupLayoutPolicyField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiGroupLayoutPolicyField.Magic:
			case MuiGroupLayoutPolicyField.Horizontal:
			case MuiGroupLayoutPolicyField.HorizontalSpacing:
			case MuiGroupLayoutPolicyField.VerticalSpacing:
			case MuiGroupLayoutPolicyField.SameWidth:
			case MuiGroupLayoutPolicyField.SameHeight:
			case MuiGroupLayoutPolicyField.PageMode:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupLayoutPolicyFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address,
				MuiGroupLayoutPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupLayoutPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGroupLayoutPolicyFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupLayoutPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGroupLayoutPolicyFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiGroupLayoutPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupLayoutPolicyStateRecord.Size) ||
			!MuiGroupLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutPolicyField.Magic, out var magic) ||
			magic != MuiGroupLayoutPolicyStateRecord.Cookie ||
			!MuiGroupLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutPolicyField.Horizontal, out value.Horizontal) ||
			!MuiGroupLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutPolicyField.HorizontalSpacing,
				out value.HorizontalSpacing) ||
			!MuiGroupLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutPolicyField.VerticalSpacing,
				out value.VerticalSpacing) ||
			!MuiGroupLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutPolicyField.SameWidth, out value.SameWidth) ||
			!MuiGroupLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutPolicyField.SameHeight, out value.SameHeight) ||
			!MuiGroupLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGroupLayoutPolicyField.PageMode, out value.PageMode))
			return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGroupLayoutPolicyStateRecord.Size) ||
			value.Magic != MuiGroupLayoutPolicyStateRecord.Cookie) return false;
		return MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGroupLayoutPolicyField.Magic, value.Magic) &&
			MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupLayoutPolicyField.Horizontal, value.Horizontal) &&
			MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupLayoutPolicyField.HorizontalSpacing, value.HorizontalSpacing) &&
			MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupLayoutPolicyField.VerticalSpacing, value.VerticalSpacing) &&
			MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupLayoutPolicyField.SameWidth, value.SameWidth) &&
			MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupLayoutPolicyField.SameHeight, value.SameHeight) &&
			MuiGroupLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiGroupLayoutPolicyField.PageMode, value.PageMode);
	}
}
