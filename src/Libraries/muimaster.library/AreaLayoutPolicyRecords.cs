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
	private static bool TryResolve(MuiAreaLayoutPolicyField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaLayoutPolicyField.Magic:
			case MuiAreaLayoutPolicyField.ShowMe:
			case MuiAreaLayoutPolicyField.FixWidth:
			case MuiAreaLayoutPolicyField.FixHeight:
			case MuiAreaLayoutPolicyField.MaxWidth:
			case MuiAreaLayoutPolicyField.MaxHeight:
			case MuiAreaLayoutPolicyField.InnerLeft:
			case MuiAreaLayoutPolicyField.InnerRight:
			case MuiAreaLayoutPolicyField.InnerTop:
			case MuiAreaLayoutPolicyField.InnerBottom:
			case MuiAreaLayoutPolicyField.HorizontalWeight:
			case MuiAreaLayoutPolicyField.VerticalWeight:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaLayoutPolicyFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address,
				MuiAreaLayoutPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaLayoutPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaLayoutPolicyFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaLayoutPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaLayoutPolicyFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiAreaLayoutPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaLayoutPolicyStateRecord.Size) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.Magic, out var magic) ||
			magic != MuiAreaLayoutPolicyStateRecord.Cookie ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.ShowMe, out value.ShowMe) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.FixWidth, out value.FixWidth) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.FixHeight, out value.FixHeight) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.MaxWidth, out value.MaxWidth) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.MaxHeight, out value.MaxHeight) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.InnerLeft, out value.InnerLeft) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.InnerRight, out value.InnerRight) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.InnerTop, out value.InnerTop) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.InnerBottom, out value.InnerBottom) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.HorizontalWeight,
				out value.HorizontalWeight) ||
			!MuiAreaLayoutPolicyFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.VerticalWeight, out value.VerticalWeight))
			return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaLayoutPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaLayoutPolicyStateRecord.Size) ||
			value.Magic != MuiAreaLayoutPolicyStateRecord.Cookie) return false;
		return MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaLayoutPolicyField.Magic, value.Magic) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.ShowMe, value.ShowMe) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.FixWidth, value.FixWidth) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.FixHeight, value.FixHeight) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.MaxWidth, value.MaxWidth) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.MaxHeight, value.MaxHeight) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.InnerLeft, value.InnerLeft) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.InnerRight, value.InnerRight) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.InnerTop, value.InnerTop) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.InnerBottom, value.InnerBottom) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.HorizontalWeight, value.HorizontalWeight) &&
			MuiAreaLayoutPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaLayoutPolicyField.VerticalWeight, value.VerticalWeight);
	}
}
