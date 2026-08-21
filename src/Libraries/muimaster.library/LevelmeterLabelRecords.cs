/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned Levelmeter label string.
public struct MuiLevelmeterLabelState
{
	public APTR Label;
}

// Guest-resident Levelmeter label state.  The bounded copy is retained in the
// object's LevelmeterLabelKey Dataspace entry.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterLabelStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D4C424Cu; // 'MLBL'

	internal uint Magic;
	internal APTR Label;
}

internal enum MuiLevelmeterLabelStateField : byte
{
	Magic,
	Label,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterLabelStateFieldCursor
{
	internal APTR Record;
	internal MuiLevelmeterLabelStateField Field;
}

internal static class MuiLevelmeterLabelStateFieldCursorCodec
{
	private static bool TryResolve(MuiLevelmeterLabelStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiLevelmeterLabelStateField.Magic => 0,
			MuiLevelmeterLabelStateField.Label => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterLabelStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiLevelmeterLabelStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiLevelmeterLabelStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterLabelStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiLevelmeterLabelStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiLevelmeterLabelStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiLevelmeterLabelStateRecord.Size) ||
			!MuiLevelmeterLabelStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiLevelmeterLabelStateField.Magic, out var magic) ||
			magic != MuiLevelmeterLabelStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiLevelmeterLabelStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiLevelmeterLabelStateField.Label, out var label))
			return false;
		value.Label = APTR.FromPointer(label);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiLevelmeterLabelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiLevelmeterLabelStateRecord.Size) || value.Magic !=
			MuiLevelmeterLabelStateRecord.Cookie) return false;
		return MuiLevelmeterLabelStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiLevelmeterLabelStateField.Magic, value.Magic) &&
			MuiLevelmeterLabelStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiLevelmeterLabelStateField.Label, value.Label.Raw);
	}
}
