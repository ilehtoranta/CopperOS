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
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiChoiceEntriesStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var entries)) return false;
		value.Entries = APTR.FromPointer(entries);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiChoiceEntriesStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Entries.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiChoiceEntriesStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiChoiceEntriesStateAdmission.Validate(ref platform,
			value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiChoiceEntriesStateAdmission
{
	private static bool ValidateVector<TPlatform>(ref TPlatform platform,
		APTR entries)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (entries.IsNull) return true;
		for (var index = 0u; index < MuiChoiceEntryCursor.MaximumEntries;
			index++)
		{
			// Exchange the named entry's Text field through the bounded vector
			// bridge.  This one-field primitive projection retains the complete
			// struct-owned bounds while avoiding a CopperSharp 68k limitation
			// with out one-field APTR records at high-bit values.
			if (!MuiChoiceEntryVectorCodec.TryReadValue(ref platform, entries,
				index, out var rawText)) return false;
			if (rawText == 0) return true;
		}
		return false;
	}

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiChoiceEntriesState value)
		where TPlatform : struct, IMuiGuestMemory
		=> ValidateVector(ref platform, value.Entries);

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Magic != MuiChoiceEntriesStateRecord.Cookie) return false;
		return ValidateVector(ref platform, value.Entries);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiChoiceEntriesStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
