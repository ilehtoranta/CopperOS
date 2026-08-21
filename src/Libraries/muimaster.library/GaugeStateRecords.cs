/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Gauge progress state.  The ULONG fields retain MorphOS semantics,
// while construction, divide handling, clamping, and drawing consume one
// named value instead of separate anonymous attribute reads.
public struct MuiGaugeState
{
	public uint Maximum;
	public uint Current;
	public uint Divide;
	public uint Horizontal;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGaugeStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x4D474155u; // 'MGAU'

	internal uint Magic;
	internal uint Maximum;
	internal uint Current;
	internal uint Divide;
	internal uint Horizontal;
}

internal enum MuiGaugeStateField : byte
{
	Magic,
	Maximum,
	Current,
	Divide,
	Horizontal,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGaugeStateFieldCursor
{
	internal APTR Record;
	internal MuiGaugeStateField Field;
}

internal static class MuiGaugeStateFieldCursorCodec
{
	private static bool TryResolve(MuiGaugeStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiGaugeStateField.Magic => 0,
			MuiGaugeStateField.Maximum => 4,
			MuiGaugeStateField.Current => 8,
			MuiGaugeStateField.Divide => 12,
			MuiGaugeStateField.Horizontal => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiGaugeStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGaugeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGaugeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGaugeStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGaugeStateRecord.Size) ||
			!MuiGaugeStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiGaugeStateField.Magic, out var magic) ||
			magic != MuiGaugeStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiGaugeStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiGaugeStateField.Maximum, out value.Maximum) &&
			MuiGaugeStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiGaugeStateField.Current, out value.Current) &&
			MuiGaugeStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiGaugeStateField.Divide, out value.Divide) &&
			MuiGaugeStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiGaugeStateField.Horizontal, out value.Horizontal);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGaugeStateRecord.Size) || value.Magic !=
			MuiGaugeStateRecord.Cookie) return false;
		return MuiGaugeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGaugeStateField.Magic, value.Magic) &&
			MuiGaugeStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiGaugeStateField.Maximum, value.Maximum) &&
			MuiGaugeStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiGaugeStateField.Current, value.Current) &&
			MuiGaugeStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiGaugeStateField.Divide, value.Divide) &&
			MuiGaugeStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiGaugeStateField.Horizontal, value.Horizontal);
	}
}
