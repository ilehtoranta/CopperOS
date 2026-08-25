/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_CycleChain is a signed LONG Area policy. Keep its full bit pattern in
// a named value record; Window_SetCycleChain's object vector is a separate
// window-level method boundary.
public struct MuiAreaCycleChainStateInput
{
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCycleChainStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41434359u; // 'ACCY'

	internal uint Magic;
	internal int Value;
	internal uint Generation;
}

internal enum MuiAreaCycleChainStateField : byte
{
	Magic,
	Value,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCycleChainStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaCycleChainStateField Field;
}

internal static class MuiAreaCycleChainStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaCycleChainStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaCycleChainStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaCycleChainStateField.Value:
				offset = 4;
				return true;
			case MuiAreaCycleChainStateField.Generation:
				offset = 8;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCycleChainStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaCycleChainStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCycleChainStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaCycleChainStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCycleChainStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaCycleChainStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaCycleChainStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCycleChainStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaCycleChainStateRecord.Size) ||
			!MuiAreaCycleChainStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCycleChainStateField.Magic, out var magic) ||
			magic != MuiAreaCycleChainStateRecord.Cookie ||
			!MuiAreaCycleChainStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCycleChainStateField.Value, out var rawValue) ||
			!MuiAreaCycleChainStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCycleChainStateField.Generation,
				out var generation)) return false;
		value.Magic = magic;
		value.Value = unchecked((int)rawValue);
		value.Generation = generation;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCycleChainStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaCycleChainStateRecord.Size) || value.Magic !=
			MuiAreaCycleChainStateRecord.Cookie) return false;
		return MuiAreaCycleChainStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaCycleChainStateField.Magic, value.Magic) &&
			MuiAreaCycleChainStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaCycleChainStateField.Value,
			unchecked((uint)value.Value)) &&
			MuiAreaCycleChainStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaCycleChainStateField.Generation, value.Generation);
	}
}
