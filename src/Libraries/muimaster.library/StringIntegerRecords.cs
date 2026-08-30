/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of MUIA_String_Integer.  The guest attribute remains a
// ULONG ABI value, while consumers can use the signed value without a managed
// numeric object or a private String offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiStringIntegerState
{
	public int Value;
}

// Guest-resident signed String.mui integer state.  Contents remain the source
// text; this record is the canonical parsed value exposed by Integer Get.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringIntegerStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ValueOffset = 4;
	internal const uint Cookie = 0x4D53494Eu; // 'MSIN'

	internal uint Magic;
	internal int Value;
}

internal static class MuiStringIntegerStateAdmission
{
	internal static bool Validate(MuiStringIntegerStateRecord value) =>
		value.Magic == MuiStringIntegerStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringIntegerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiStringIntegerStateField : byte
{
	Magic,
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringIntegerStateFieldCursor
{
	internal APTR Record;
	internal MuiStringIntegerStateField Field;
}

internal static class MuiStringIntegerStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringIntegerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringIntegerStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringIntegerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringIntegerStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringIntegerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiStringIntegerStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. String integer consumers use the named
// signed-value record; this bounded adapter is the only layer that translates
// its fixed guest layout into addresses. The cursor codec remains available
// for compatibility and malformed-state diagnostics.
internal static class MuiStringIntegerStateRecordMemoryCodec
{
	private static bool TryResolve(MuiStringIntegerStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringIntegerStateField.Magic:
				offset = MuiStringIntegerStateRecord.MagicOffset;
				return true;
			case MuiStringIntegerStateField.Value:
				offset = MuiStringIntegerStateRecord.ValueOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringIntegerStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringIntegerStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringIntegerStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringIntegerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringIntegerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringIntegerStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringIntegerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringIntegerStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var raw) || !MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Value = unchecked((int)raw);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringIntegerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringIntegerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiStringIntegerStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiStringIntegerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringIntegerStateAdmission.Validate(value) &&
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringIntegerStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Value)) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringIntegerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
