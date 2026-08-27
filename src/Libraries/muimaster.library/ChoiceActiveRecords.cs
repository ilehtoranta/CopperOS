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
		switch (field)
		{
			case MuiChoiceActiveStateField.Magic:
				offset = MuiChoiceActiveStateRecord.MagicOffset;
				return true;
			case MuiChoiceActiveStateField.Active:
				offset = MuiChoiceActiveStateRecord.ActiveOffset;
				return true;
		}
		offset = 0;
		return false;
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
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiChoiceActiveStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiChoiceActiveStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiChoiceActiveStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiChoiceActiveStateField.Magic, out value.Magic) &&
			MuiChoiceActiveStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiChoiceActiveStateField.Active, out value.Active);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiChoiceActiveStateAdmission.ValidateRecord(value.Magic, value.Active);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiChoiceActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiChoiceActiveStateAdmission.ValidateRecord(value.Magic,
			value.Active)) return false;
		return MuiChoiceActiveStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceActiveStateField.Magic, value.Magic) &&
			MuiChoiceActiveStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiChoiceActiveStateField.Active, value.Active);
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
