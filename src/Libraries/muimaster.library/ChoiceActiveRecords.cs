/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the active Cycle/Radio entry.  The index remains a
// 32-bit MUI value, including the signed -1/-2 Cycle navigation selectors at
// the Set() boundary; the named record stores the normalized active index.
public struct MuiChoiceActiveState
{
	public uint Active;
}

// Guest-resident choice-active state.  Keeping this separate from the entry
// vector preserves the MorphOS attribute model: Entries is a caller-owned
// STRPTR vector while Active is an object-owned scalar relationship.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiChoiceActiveStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D434153u; // 'MCAS'

	internal uint Magic;
	internal uint Active;
}

internal enum MuiChoiceActiveStateField : byte
{
	Magic,
	Active,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiChoiceActiveStateFieldCursor
{
	internal APTR Record;
	internal MuiChoiceActiveStateField Field;
}

internal static class MuiChoiceActiveStateFieldCursorCodec
{
	private static bool TryResolve(MuiChoiceActiveStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiChoiceActiveStateField.Magic => 0,
			MuiChoiceActiveStateField.Active => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiChoiceActiveStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiChoiceActiveStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceActiveStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiChoiceActiveStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceActiveStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiChoiceActiveStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiChoiceActiveStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiChoiceActiveStateRecord.Size) ||
			!MuiChoiceActiveStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiChoiceActiveStateField.Magic, out var magic) ||
			magic != MuiChoiceActiveStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiChoiceActiveStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiChoiceActiveStateField.Active, out value.Active);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiChoiceActiveStateRecord.Size) || value.Magic !=
			MuiChoiceActiveStateRecord.Cookie) return false;
		return MuiChoiceActiveStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceActiveStateField.Magic, value.Magic) &&
			MuiChoiceActiveStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceActiveStateField.Active, value.Active);
	}
}
