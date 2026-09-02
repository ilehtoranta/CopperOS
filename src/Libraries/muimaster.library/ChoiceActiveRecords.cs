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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ActiveOffset = 4;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiChoiceActiveStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiChoiceActiveStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceActiveStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiChoiceActiveStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceActiveStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiChoiceActiveStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Choice/Radio consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiChoiceActiveStateRecordMemoryCodec
{
	private static bool TryResolve(MuiChoiceActiveStateField field,
		out uint offset)
	{
		if (field == MuiChoiceActiveStateField.Magic)
			offset = MuiChoiceActiveStateRecord.MagicOffset;
		else if (field == MuiChoiceActiveStateField.Active)
			offset = MuiChoiceActiveStateRecord.ActiveOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceActiveStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiChoiceActiveStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiChoiceActiveStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceActiveStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiChoiceActiveStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiChoiceActiveStateField.Magic)
			value = state.Magic;
		else if (field == MuiChoiceActiveStateField.Active)
			value = state.Active;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceActiveStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiChoiceActiveStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiChoiceActiveStateField.Magic)
			state.Magic = value;
		else if (field == MuiChoiceActiveStateField.Active)
			state.Active = value;
		else return false;
		return MuiChoiceActiveStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiChoiceActiveStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiChoiceActiveStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiChoiceActiveStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Active) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiChoiceActiveStateAdmission.ValidateRecord(value.Magic, value.Active);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiChoiceActiveStateAdmission.ValidateRecord(value.Magic,
			value.Active)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiChoiceActiveStateAdmission
{
	internal static bool ValidateActive(uint active) =>
		(active & 0x80000000u) == 0;

	internal static bool Validate(MuiChoiceActiveState value) =>
		ValidateActive(value.Active);

	internal static bool ValidateRecord(uint magic, uint active) =>
		magic == MuiChoiceActiveStateRecord.Cookie && ValidateActive(active);

	internal static bool Validate(MuiChoiceActiveStateRecord value) =>
		ValidateRecord(value.Magic, value.Active);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		ValidateRecord(value.Magic, value.Active) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
