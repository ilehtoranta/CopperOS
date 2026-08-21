/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared fixed-width sleep state. Window owners use SavedDisabled to restore
// their prior disabled value; application owners leave that field zero.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSleepStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x534C5053u; // 'SLPS'

	internal uint Magic;
	internal uint Depth;
	internal uint SavedDisabled;
	internal uint Request;
}

internal enum MuiSleepStateField : byte
{
	Magic,
	Depth,
	SavedDisabled,
	Request,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSleepStateFieldCursor
{
	internal APTR Record;
	internal MuiSleepStateField Field;
}

internal static class MuiSleepStateFieldCursorCodec
{
	private static bool TryResolve(MuiSleepStateField field, out uint offset)
	{
		switch (field)
		{
			case MuiSleepStateField.Magic:
			case MuiSleepStateField.Depth:
			case MuiSleepStateField.SavedDisabled:
			case MuiSleepStateField.Request:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSleepStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiSleepStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiSleepStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSleepStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiSleepStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiSleepStateRecord.Size) ||
			!MuiSleepStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiSleepStateField.Magic, out var magic) ||
			magic != MuiSleepStateRecord.Cookie ||
			!MuiSleepStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiSleepStateField.Depth, out value.Depth) ||
			!MuiSleepStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiSleepStateField.SavedDisabled, out value.SavedDisabled) ||
			!MuiSleepStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiSleepStateField.Request, out value.Request)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiSleepStateRecord.Size) || value.Magic != MuiSleepStateRecord.Cookie)
			return false;
		return MuiSleepStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiSleepStateField.Magic, value.Magic) &&
			MuiSleepStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiSleepStateField.Depth, value.Depth) &&
			MuiSleepStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiSleepStateField.SavedDisabled, value.SavedDisabled) &&
			MuiSleepStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiSleepStateField.Request, value.Request);
	}
}
