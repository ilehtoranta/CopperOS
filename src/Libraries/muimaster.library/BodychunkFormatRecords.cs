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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint CompressionOffset = 4;
	internal const uint DepthOffset = 8;
	internal const uint MaskingOffset = 12;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBodychunkFormatStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBodychunkFormatStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiBodychunkFormatStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBodychunkFormatStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Bodychunk format consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiBodychunkFormatStateRecordMemoryCodec
{
	private static bool TryResolve(MuiBodychunkFormatStateField field,
		out uint offset)
	{
		if (field == MuiBodychunkFormatStateField.Magic)
			offset = MuiBodychunkFormatStateRecord.MagicOffset;
		else if (field == MuiBodychunkFormatStateField.Compression)
			offset = MuiBodychunkFormatStateRecord.CompressionOffset;
		else if (field == MuiBodychunkFormatStateField.Depth)
			offset = MuiBodychunkFormatStateRecord.DepthOffset;
		else if (field == MuiBodychunkFormatStateField.Masking)
			offset = MuiBodychunkFormatStateRecord.MaskingOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiBodychunkFormatStateRecord.Size) &&
			platform.IsMapped(address, MuiBodychunkFormatStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiBodychunkFormatStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBodychunkFormatStateField.Magic)
			value = state.Magic;
		else if (field == MuiBodychunkFormatStateField.Compression)
			value = state.Compression;
		else if (field == MuiBodychunkFormatStateField.Depth)
			value = state.Depth;
		else if (field == MuiBodychunkFormatStateField.Masking)
			value = state.Masking;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBodychunkFormatStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBodychunkFormatStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBodychunkFormatStateField.Magic)
			state.Magic = value;
		else if (field == MuiBodychunkFormatStateField.Compression)
			state.Compression = value;
		else if (field == MuiBodychunkFormatStateField.Depth)
			state.Depth = value;
		else if (field == MuiBodychunkFormatStateField.Masking)
			state.Masking = value;
		else return false;
		return MuiBodychunkFormatStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	// Legacy raw-offset adapter retained for bounded diagnostics and older
	// callers. Typed field access above is the preferred API.
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiBodychunkFormatStateRecord.Size -
			MuiBodychunkFormatStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiBodychunkFormatStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiBodychunkFormatStateRecord.FieldSize);
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
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBodychunkFormatStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Compression) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Depth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Masking)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBodychunkFormatStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Compression) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Depth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Masking) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiBodychunkFormatStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBodychunkFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiBodychunkFormatStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
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
