/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the Image.mui tagged Image_Spec value and its
// ImageBuiltinSpec fallback. Raw is either a builtin image number or a guest
// STRPTR to a "kind:value" specification; Builtin is retained separately so
// absent Image_Spec does not become builtin image zero.
public struct MuiImageSpecState
{
	public bool Present;
	public uint Raw;
	public bool BuiltinPresent;
	public uint Builtin;
}

// Guest-resident Image spec union. Presence is kept separately for both
// attributes so an absent Image_Spec does not become builtin image zero during
// rendering and a supplied builtin value zero remains representable.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageSpecStateRecord
{
	internal const uint Size = 20;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint PresentOffset = 4;
	internal const uint RawOffset = 8;
	internal const uint BuiltinPresentOffset = 12;
	internal const uint BuiltinOffset = 16;
	internal const uint Cookie = 0x4D495350u; // 'MISP'

	internal uint Magic;
	internal uint Present;
	internal uint Raw;
	internal uint BuiltinPresent;
	internal uint Builtin;
}

internal enum MuiImageSpecStateField : byte
{
	Magic,
	Present,
	Raw,
	BuiltinPresent,
	Builtin,
}

internal static class MuiImageSpecStateAdmission
{
	private const uint BuiltinImageMaximum = 0x00000093;

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Magic != MuiImageSpecStateRecord.Cookie ||
			value.Present > 1 || value.BuiltinPresent > 1) return false;
		if (value.Present == 0 || value.Raw <= BuiltinImageMaximum) return true;
		return platform.IsMapped(APTR.FromPointer(value.Raw), 2);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageSpecStateFieldCursor
{
	internal APTR Record;
	internal MuiImageSpecStateField Field;
}

internal static class MuiImageSpecStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageSpecStateField field,
		out uint offset)
		{
			if (field == MuiImageSpecStateField.Magic)
				offset = MuiImageSpecStateRecord.MagicOffset;
			else if (field == MuiImageSpecStateField.Present)
				offset = MuiImageSpecStateRecord.PresentOffset;
			else if (field == MuiImageSpecStateField.Raw)
				offset = MuiImageSpecStateRecord.RawOffset;
			else if (field == MuiImageSpecStateField.BuiltinPresent)
				offset = MuiImageSpecStateRecord.BuiltinPresentOffset;
			else if (field == MuiImageSpecStateField.Builtin)
				offset = MuiImageSpecStateRecord.BuiltinOffset;
			else
			{
				offset = 0;
				return false;
			}
			return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageSpecStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiImageSpecStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiImageSpecStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageSpecStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageSpecStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageSpecStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageSpecStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Image consumers use the named spec
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiImageSpecStateRecordMemoryCodec
{
	private static bool TryResolve(MuiImageSpecStateField field,
		out uint offset)
	{
		if (field == MuiImageSpecStateField.Magic)
			offset = MuiImageSpecStateRecord.MagicOffset;
		else if (field == MuiImageSpecStateField.Present)
			offset = MuiImageSpecStateRecord.PresentOffset;
		else if (field == MuiImageSpecStateField.Raw)
			offset = MuiImageSpecStateRecord.RawOffset;
		else if (field == MuiImageSpecStateField.BuiltinPresent)
			offset = MuiImageSpecStateRecord.BuiltinPresentOffset;
		else if (field == MuiImageSpecStateField.Builtin)
			offset = MuiImageSpecStateRecord.BuiltinOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageSpecStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageSpecStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiImageSpecStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiImageSpecStateField.Magic)
			value = state.Magic;
		else if (field == MuiImageSpecStateField.Present)
			value = state.Present;
		else if (field == MuiImageSpecStateField.Raw)
			value = state.Raw;
		else if (field == MuiImageSpecStateField.BuiltinPresent)
			value = state.BuiltinPresent;
		else if (field == MuiImageSpecStateField.Builtin)
			value = state.Builtin;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageSpecStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiImageSpecStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiImageSpecStateField.Magic)
			state.Magic = value;
		else if (field == MuiImageSpecStateField.Present)
			state.Present = value;
		else if (field == MuiImageSpecStateField.Raw)
			state.Raw = value;
		else if (field == MuiImageSpecStateField.BuiltinPresent)
			state.BuiltinPresent = value;
		else if (field == MuiImageSpecStateField.Builtin)
			state.Builtin = value;
		else return false;
		return MuiImageSpecStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiImageSpecStateRecord.Size -
			MuiImageSpecStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiImageSpecStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiImageSpecStateRecord.FieldSize);
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

internal static class MuiImageSpecStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageSpecStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Present) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Raw) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.BuiltinPresent) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Builtin) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageSpecStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Present) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.BuiltinPresent) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Builtin) &&
			MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiImageSpecStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiImageSpecStateAdmission.Validate(ref platform, value) &&
		WriteRecord(ref platform, address, value);
}
