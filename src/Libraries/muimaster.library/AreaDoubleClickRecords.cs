/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_DoubleClick is a getter-only signed LONG Area signal. Keep the full
// 32-bit value in a named state record so event producers and OM_GET share one
// typed contract without exposing a private object-layout offset.
public struct MuiAreaDoubleClickStateInput
{
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoubleClickStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4144434Cu; // 'ADCL'

	internal uint Magic;
	internal int Value;
	internal uint Generation;
}

internal enum MuiAreaDoubleClickStateField : byte
{
	Magic,
	Value,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoubleClickStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaDoubleClickStateField Field;
}

internal static class MuiAreaDoubleClickStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaDoubleClickStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaDoubleClickStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaDoubleClickStateField.Value:
				offset = 4;
				return true;
			case MuiAreaDoubleClickStateField.Generation:
				offset = 8;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDoubleClickStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaDoubleClickStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaDoubleClickStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDoubleClickStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaDoubleClickStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDoubleClickStateRecord.Size) ||
			!MuiAreaDoubleClickStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDoubleClickStateField.Magic, out var magic) ||
			magic != MuiAreaDoubleClickStateRecord.Cookie ||
			!MuiAreaDoubleClickStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDoubleClickStateField.Value, out var rawValue) ||
			!MuiAreaDoubleClickStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDoubleClickStateField.Generation,
				out var generation)) return false;
		value.Magic = magic;
		value.Value = unchecked((int)rawValue);
		value.Generation = generation;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDoubleClickStateRecord.Size) || value.Magic !=
			MuiAreaDoubleClickStateRecord.Cookie) return false;
		return MuiAreaDoubleClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDoubleClickStateField.Magic, value.Magic) &&
			MuiAreaDoubleClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDoubleClickStateField.Value,
			unchecked((uint)value.Value)) &&
			MuiAreaDoubleClickStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDoubleClickStateField.Generation, value.Generation);
	}
}
