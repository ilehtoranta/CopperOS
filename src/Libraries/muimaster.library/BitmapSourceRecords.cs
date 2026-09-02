/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of a Bitmap.mui BitMap or Bodychunk.mui Body pointer.
// The source remains caller-owned; setup may derive a separate remapped buffer.
public struct MuiBitmapSourceState
{
	public APTR Source;
}

// Guest-resident source state shared by the two bitmap-family source attrs.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapSourceStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint SourceOffset = 4;
	internal const uint Cookie = 0x4D42534Fu; // 'MBSO'

	internal uint Magic;
	internal APTR Source;
}

internal enum MuiBitmapSourceStateField : byte
{
	Magic,
	Source,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapSourceStateFieldCursor
{
	internal APTR Record;
	internal MuiBitmapSourceStateField Field;
}

internal static class MuiBitmapSourceStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapSourceStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapSourceStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapSourceStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiBitmapSourceStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapSourceStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBitmapSourceStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Bitmap source consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiBitmapSourceStateRecordMemoryCodec
{
	private static bool TryResolve(MuiBitmapSourceStateField field,
		out uint offset)
	{
		if (field == MuiBitmapSourceStateField.Magic)
			offset = MuiBitmapSourceStateRecord.MagicOffset;
		else if (field == MuiBitmapSourceStateField.Source)
			offset = MuiBitmapSourceStateRecord.SourceOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapSourceStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiBitmapSourceStateRecord.Size) &&
			platform.IsMapped(address, MuiBitmapSourceStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapSourceStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiBitmapSourceStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBitmapSourceStateField.Magic)
			value = state.Magic;
		else if (field == MuiBitmapSourceStateField.Source)
			value = state.Source.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapSourceStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBitmapSourceStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBitmapSourceStateField.Magic)
			state.Magic = value;
		else if (field == MuiBitmapSourceStateField.Source)
			state.Source = APTR.FromPointer(value);
		else return false;
		return MuiBitmapSourceStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	// Legacy raw-offset adapter retained for bounded diagnostics and older
	// callers. Typed field access above is the preferred API.
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiBitmapSourceStateRecord.Size -
			MuiBitmapSourceStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiBitmapSourceStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiBitmapSourceStateRecord.FieldSize);
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

internal static class MuiBitmapSourceStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBitmapSourceStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var source)) return false;
		value.Source = APTR.FromPointer(source);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBitmapSourceStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Source.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiBitmapSourceStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiBitmapSourceStateAdmission.Validate(ref platform,
			value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiBitmapSourceStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiBitmapSourceStateRecord.Cookie &&
		(value.Source.IsNull || platform.IsMapped(value.Source, 4));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
