/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Levelmeter presentation state. Numeric range/value data remains in
// MuiNumericState; this record carries the Gauge_Horiz orientation consumed by
// Levelmeter rendering.
public struct MuiLevelmeterPresentationState
{
	public uint Horizontal;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterPresentationStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D4C564Cu; // 'MLVL'

	internal uint Magic;
	internal uint Horizontal;
}

internal enum MuiLevelmeterPresentationStateField : byte
{
	Magic,
	Horizontal,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiLevelmeterPresentationStateField Field;
}

internal static class MuiLevelmeterPresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiLevelmeterPresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiLevelmeterPresentationStateField.Magic => 0,
			MuiLevelmeterPresentationStateField.Horizontal => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiLevelmeterPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiLevelmeterPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiLevelmeterPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiLevelmeterPresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiLevelmeterPresentationStateRecord.Size) ||
			!MuiLevelmeterPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiLevelmeterPresentationStateField.Magic, out var magic) ||
			magic != MuiLevelmeterPresentationStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiLevelmeterPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiLevelmeterPresentationStateField.Horizontal,
			out value.Horizontal);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiLevelmeterPresentationStateRecord.Size) || value.Magic !=
			MuiLevelmeterPresentationStateRecord.Cookie) return false;
		return MuiLevelmeterPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiLevelmeterPresentationStateField.Magic, value.Magic) &&
			MuiLevelmeterPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiLevelmeterPresentationStateField.Horizontal,
			value.Horizontal);
	}
}
