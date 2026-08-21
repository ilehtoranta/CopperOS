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

internal static class MuiPropPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiPropPolicyStateRecord.Size) ||
			!MuiPropPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiPropPolicyStateField.Magic, out var magic) ||
			magic != MuiPropPolicyStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiPropPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiPropPolicyStateField.Horizontal, out value.Horizontal) &&
			MuiPropPolicyStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiPropPolicyStateField.DeltaFactor, out value.DeltaFactor) &&
			MuiPropPolicyStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiPropPolicyStateField.Slider, out value.Slider) &&
			MuiPropPolicyStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiPropPolicyStateField.UseWinBorder, out value.UseWinBorder);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPropPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiPropPolicyStateRecord.Size) || value.Magic !=
			MuiPropPolicyStateRecord.Cookie) return false;
		return MuiPropPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiPropPolicyStateField.Magic, value.Magic) &&
			MuiPropPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiPropPolicyStateField.Horizontal, value.Horizontal) &&
			MuiPropPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiPropPolicyStateField.DeltaFactor, value.DeltaFactor) &&
			MuiPropPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiPropPolicyStateField.Slider, value.Slider) &&
			MuiPropPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiPropPolicyStateField.UseWinBorder, value.UseWinBorder);
	}
}
