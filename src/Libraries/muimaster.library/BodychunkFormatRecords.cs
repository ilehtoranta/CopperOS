/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Semantic view of the Bodychunk.mui BODY decoding format.  The values stay
// ULONG-compatible with MorphOS, while the decoder consumes this named state
// instead of reaching into an anonymous collection of attribute slots.
public struct MuiBodychunkFormatState
{
	public uint Compression;
	public uint Depth;
	public uint Masking;
}

// Guest-resident format state retained in the object's Dataspace.  A compact
// record makes the lifetime and ABI visible without introducing managed state.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBodychunkFormatStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4D424643u; // 'MBFC'

	internal uint Magic;
	internal uint Compression;
	internal uint Depth;
	internal uint Masking;
}

internal enum MuiBodychunkFormatStateField : byte
{
	Magic,
	Compression,
	Depth,
	Masking,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBodychunkFormatStateFieldCursor
{
	internal APTR Record;
	internal MuiBodychunkFormatStateField Field;
}

internal static class MuiBodychunkFormatStateFieldCursorCodec
{
	private static bool TryResolve(MuiBodychunkFormatStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiBodychunkFormatStateField.Magic => 0,
			MuiBodychunkFormatStateField.Compression => 4,
			MuiBodychunkFormatStateField.Depth => 8,
			MuiBodychunkFormatStateField.Masking => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBodychunkFormatStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiBodychunkFormatStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBodychunkFormatStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBodychunkFormatStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Bodychunk format consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiBodychunkFormatStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiBodychunkFormatStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiBodychunkFormatStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiBodychunkFormatStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiBodychunkFormatStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) &&
			MuiBodychunkFormatStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Compression) &&
			MuiBodychunkFormatStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out value.Depth) &&
			MuiBodychunkFormatStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 12, out value.Masking);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiBodychunkFormatStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBodychunkFormatStateAdmission.Validate(value)) return false;
		return MuiBodychunkFormatStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiBodychunkFormatStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, value.Compression) &&
			MuiBodychunkFormatStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 8, value.Depth) &&
			MuiBodychunkFormatStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 12, value.Masking);
	}
}

internal static class MuiBodychunkFormatStateAdmission
{
	internal static bool Validate(MuiBodychunkFormatStateRecord value) =>
		value.Magic == MuiBodychunkFormatStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
