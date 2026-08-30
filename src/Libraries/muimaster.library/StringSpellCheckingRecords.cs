/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui spell-checking policy.  MorphOS exposes this as a
// BOOL, but keeping the policy in a named record gives construction, Get, and
// mutation one validated seam without introducing a managed dictionary or a
// private widget offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringSpellCheckingStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint EnabledOffset = 4;
	internal const uint Cookie = 0x4D535043u; // 'MSPC'

	internal uint Magic;
	internal uint Enabled;
}

internal static class MuiStringSpellCheckingStateAdmission
{
	internal static bool Validate(MuiStringSpellCheckingStateRecord value) =>
		value.Magic == MuiStringSpellCheckingStateRecord.Cookie &&
		value.Enabled <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringSpellCheckingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiStringSpellCheckingStateField : byte
{
	Magic,
	Enabled,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringSpellCheckingStateFieldCursor
{
	internal APTR Record;
	internal MuiStringSpellCheckingStateField Field;
}

internal static class MuiStringSpellCheckingStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringSpellCheckingStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringSpellCheckingStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringSpellCheckingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringSpellCheckingStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringSpellCheckingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringSpellCheckingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter. The named BOOL policy remains the
// semantic record; this bounded adapter owns the fixed guest translation.
// The cursor codec remains available for compatibility and malformed-state
// diagnostics.
internal static class MuiStringSpellCheckingStateRecordMemoryCodec
{
	private static bool TryResolve(MuiStringSpellCheckingStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringSpellCheckingStateField.Magic:
				offset = MuiStringSpellCheckingStateRecord.MagicOffset;
				return true;
			case MuiStringSpellCheckingStateField.Enabled:
				offset = MuiStringSpellCheckingStateRecord.EnabledOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringSpellCheckingStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringSpellCheckingStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringSpellCheckingStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringSpellCheckingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringSpellCheckingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringSpellCheckingStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringSpellCheckingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringSpellCheckingStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Enabled) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringSpellCheckingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringSpellCheckingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringSpellCheckingStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringSpellCheckingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringSpellCheckingStateAdmission.Validate(value) &&
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringSpellCheckingStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Enabled) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringSpellCheckingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
