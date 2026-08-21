/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Numeric-family value state.  The fields remain ULONG-compatible with
// MorphOS and are consumed by Numeric, Slider, Knob, and Levelmeter behavior.
public struct MuiNumericState
{
	public uint Minimum;
	public uint Maximum;
	public uint Value;
	public uint Default;
	public uint Reverse;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNumericStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x4D4E5354u; // 'MNST'

	internal uint Magic;
	internal uint Minimum;
	internal uint Maximum;
	internal uint Value;
	internal uint Default;
	internal uint Reverse;
}

internal enum MuiNumericStateField : byte
{
	Magic,
	Minimum,
	Maximum,
	Value,
	Default,
	Reverse,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNumericStateFieldCursor
{
	internal APTR Record;
	internal MuiNumericStateField Field;
}

internal static class MuiNumericStateFieldCursorCodec
{
	private static bool TryResolve(MuiNumericStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiNumericStateField.Magic => 0,
			MuiNumericStateField.Minimum => 4,
			MuiNumericStateField.Maximum => 8,
			MuiNumericStateField.Value => 12,
			MuiNumericStateField.Default => 16,
			MuiNumericStateField.Reverse => 20,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNumericStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiNumericStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiNumericStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiNumericStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiNumericStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiNumericStateRecord.Size) ||
			!MuiNumericStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiNumericStateField.Magic, out var magic) ||
			magic != MuiNumericStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiNumericStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Minimum, out value.Minimum) ||
			!MuiNumericStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Maximum, out value.Maximum) ||
			!MuiNumericStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Value, out value.Value) ||
			!MuiNumericStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Default, out value.Default) ||
			!MuiNumericStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Reverse, out value.Reverse)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiNumericStateRecord.Size) || value.Magic !=
			MuiNumericStateRecord.Cookie) return false;
		return MuiNumericStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiNumericStateField.Magic, value.Magic) &&
			MuiNumericStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Minimum, value.Minimum) &&
			MuiNumericStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Maximum, value.Maximum) &&
			MuiNumericStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Value, value.Value) &&
			MuiNumericStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Default, value.Default) &&
			MuiNumericStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Reverse, value.Reverse);
	}
}
