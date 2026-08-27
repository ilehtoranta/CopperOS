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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint EntriesOffset = 4;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiChoiceEntriesStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiChoiceEntriesStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntriesStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiChoiceEntriesStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntriesStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiChoiceEntriesStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Choice/Radio consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiChoiceEntriesStateRecordMemoryCodec
{
	private static bool TryResolve(MuiChoiceEntriesStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiChoiceEntriesStateField.Magic:
				offset = MuiChoiceEntriesStateRecord.MagicOffset;
				return true;
			case MuiChoiceEntriesStateField.Entries:
				offset = MuiChoiceEntriesStateRecord.EntriesOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntriesStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
				MuiChoiceEntriesStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiChoiceEntriesStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntriesStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceEntriesStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiChoiceEntriesStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiChoiceEntriesStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiChoiceEntriesStateField.Magic, out var magic) ||
			!MuiChoiceEntriesStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiChoiceEntriesStateField.Entries, out var entries)) return false;
		value.Magic = magic;
		value.Entries = APTR.FromPointer(entries);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiChoiceEntriesStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiChoiceEntriesStateAdmission.Validate(ref platform, value)) return false;
		return MuiChoiceEntriesStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceEntriesStateField.Magic, value.Magic) &&
			MuiChoiceEntriesStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceEntriesStateField.Entries, value.Entries.Raw);
	}
}

internal static class MuiChoiceEntriesStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiChoiceEntriesState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Entries.IsNull) return true;
		var cursor = default(MuiChoiceEntryCursor);
		cursor.Base = value.Entries;
		for (var index = 0u; index < MuiChoiceEntryCursor.MaximumEntries;
			index++)
		{
			cursor.Index = index;
			if (!MuiChoiceEntryCursorCodec.TryGetEntry(ref platform, cursor,
				out var slot) || !MuiChoiceEntryCodec.TryRead(ref platform, slot,
				out var entry)) return false;
			if (entry.Text.IsNull) return true;
		}
		return false;
	}

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Magic != MuiChoiceEntriesStateRecord.Cookie) return false;
		var state = default(MuiChoiceEntriesState);
		state.Entries = value.Entries;
		return Validate(ref platform, state);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
