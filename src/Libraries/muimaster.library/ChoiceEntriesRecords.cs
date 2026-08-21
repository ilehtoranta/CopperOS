/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the caller-owned NULL-terminated Cycle/Radio
// STRPTR vector.  The vector itself stays in guest memory; this record gives
// the object a stable named relationship without introducing a managed
// collection or a private control offset.
public struct MuiChoiceEntriesState
{
	public APTR Entries;
}

// Guest-resident choice-entry relationship.  Entries are caller-owned and
// therefore are not copied; the control core validates the bounded vector
// before publishing it here.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiChoiceEntriesStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D434553u; // 'MCES'

	internal uint Magic;
	internal APTR Entries;
}

internal enum MuiChoiceEntriesStateField : byte
{
	Magic,
	Entries,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiChoiceEntriesStateFieldCursor
{
	internal APTR Record;
	internal MuiChoiceEntriesStateField Field;
}

internal static class MuiChoiceEntriesStateFieldCursorCodec
{
	private static bool TryResolve(MuiChoiceEntriesStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiChoiceEntriesStateField.Magic => 0,
			MuiChoiceEntriesStateField.Entries => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiChoiceEntriesStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiChoiceEntriesStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntriesStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiChoiceEntriesStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntriesStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiChoiceEntriesStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiChoiceEntriesStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiChoiceEntriesStateRecord.Size) ||
			!MuiChoiceEntriesStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiChoiceEntriesStateField.Magic, out var magic) ||
			magic != MuiChoiceEntriesStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiChoiceEntriesStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiChoiceEntriesStateField.Entries, out var entries))
			return false;
		value.Entries = APTR.FromPointer(entries);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiChoiceEntriesStateRecord.Size) || value.Magic !=
			MuiChoiceEntriesStateRecord.Cookie) return false;
		return MuiChoiceEntriesStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceEntriesStateField.Magic, value.Magic) &&
			MuiChoiceEntriesStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceEntriesStateField.Entries, value.Entries.Raw);
	}
}
