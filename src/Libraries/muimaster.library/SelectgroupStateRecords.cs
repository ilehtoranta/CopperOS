/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Selectgroup.Active is a signed selector at the API boundary but the stored
// state is the canonical non-negative child index.  Keep that state in one
// fixed-width guest record; selector interpretation stays in SelectgroupCore.
public struct MuiSelectgroupActiveState
{
	public uint Active;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSelectgroupActiveStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x534C4750u; // 'SLGP'

	internal uint Magic;
	internal uint Active;
}

internal enum MuiSelectgroupActiveStateField : byte
{
	Magic,
	Active,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSelectgroupActiveStateFieldCursor
{
	internal APTR Record;
	internal MuiSelectgroupActiveStateField Field;
}

internal static class MuiSelectgroupActiveStateFieldCursorCodec
{
	private static bool TryResolve(MuiSelectgroupActiveStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiSelectgroupActiveStateField.Magic:
			case MuiSelectgroupActiveStateField.Active:
				offset = (uint)field * 4;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSelectgroupActiveStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiSelectgroupActiveStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiSelectgroupActiveStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSelectgroupActiveStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiSelectgroupActiveStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiSelectgroupActiveStateRecord.Size) ||
			!MuiSelectgroupActiveStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiSelectgroupActiveStateField.Magic, out var magic) ||
			magic != MuiSelectgroupActiveStateRecord.Cookie ||
			!MuiSelectgroupActiveStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiSelectgroupActiveStateField.Active, out var active))
			return false;
		value.Magic = magic;
		value.Active = active;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiSelectgroupActiveStateRecord.Size) || value.Magic !=
			MuiSelectgroupActiveStateRecord.Cookie) return false;
		return MuiSelectgroupActiveStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiSelectgroupActiveStateField.Magic,
			value.Magic) &&
			MuiSelectgroupActiveStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiSelectgroupActiveStateField.Active,
				value.Active);
	}
}
