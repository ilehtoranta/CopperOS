/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of Image.mui's initializer-only font-match policy.
// These values are MorphOS BOOL/ULONG scalars and do not require a managed
// font object or a host-side text representation.
public struct MuiImageFontMatchState
{
	public uint Match;
	public uint Height;
	public uint Width;
}

// Guest-resident scalar FontMatch policy. The optional FontMatchString pointer
// remains in MuiImageFontMatchStringStateRecord because it has independent
// caller-owned pointer validation and [IS.] mutation semantics.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageFontMatchStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint MatchOffset = 4;
	internal const uint HeightOffset = 8;
	internal const uint WidthOffset = 12;
	internal const uint Cookie = 0x4D494653u; // 'MIFS'

	internal uint Magic;
	internal uint Match;
	internal uint Height;
	internal uint Width;
}

internal static class MuiImageFontMatchStateAdmission
{
	internal static bool Validate(MuiImageFontMatchStateRecord value) =>
		value.Magic == MuiImageFontMatchStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiImageFontMatchStateField : byte
{
	Magic,
	Match,
	Height,
	Width,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageFontMatchStateFieldCursor
{
	internal APTR Record;
	internal MuiImageFontMatchStateField Field;
}

internal static class MuiImageFontMatchStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageFontMatchStateField field,
		out uint offset)
	{
		if (field == MuiImageFontMatchStateField.Magic)
			offset = MuiImageFontMatchStateRecord.MagicOffset;
		else if (field == MuiImageFontMatchStateField.Match)
			offset = MuiImageFontMatchStateRecord.MatchOffset;
		else if (field == MuiImageFontMatchStateField.Height)
			offset = MuiImageFontMatchStateRecord.HeightOffset;
		else if (field == MuiImageFontMatchStateField.Width)
			offset = MuiImageFontMatchStateRecord.WidthOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageFontMatchStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiImageFontMatchStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiImageFontMatchStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageFontMatchStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageFontMatchStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Font-match scalar consumers use the
// named record; this bounded adapter is the only layer that translates its
// fixed guest layout into addresses. The cursor codec remains for compatibility
// and malformed-state diagnostics.
internal static class MuiImageFontMatchStateRecordMemoryCodec
{
	private static bool TryResolve(MuiImageFontMatchStateField field,
		out uint offset)
	{
		if (field == MuiImageFontMatchStateField.Magic)
			offset = MuiImageFontMatchStateRecord.MagicOffset;
		else if (field == MuiImageFontMatchStateField.Match)
			offset = MuiImageFontMatchStateRecord.MatchOffset;
		else if (field == MuiImageFontMatchStateField.Height)
			offset = MuiImageFontMatchStateRecord.HeightOffset;
		else if (field == MuiImageFontMatchStateField.Width)
			offset = MuiImageFontMatchStateRecord.WidthOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiImageFontMatchStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiImageFontMatchStateField.Magic)
			value = state.Magic;
		else if (field == MuiImageFontMatchStateField.Match)
			value = state.Match;
		else if (field == MuiImageFontMatchStateField.Height)
			value = state.Height;
		else if (field == MuiImageFontMatchStateField.Width)
			value = state.Width;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiImageFontMatchStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiImageFontMatchStateField.Magic)
			state.Magic = value;
		else if (field == MuiImageFontMatchStateField.Match)
			state.Match = value;
		else if (field == MuiImageFontMatchStateField.Height)
			state.Height = value;
		else if (field == MuiImageFontMatchStateField.Width)
			state.Width = value;
		else return false;
		return MuiImageFontMatchStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiImageFontMatchStateRecord.Size -
			MuiImageFontMatchStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiImageFontMatchStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiImageFontMatchStateRecord.FieldSize);
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

internal static class MuiImageFontMatchStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageFontMatchStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Match) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Height) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Width) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageFontMatchStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Match) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Height) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Width) &&
			MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiImageFontMatchStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiImageFontMatchStateAdmission.Validate(value) &&
		WriteRecord(ref platform, address, value);
}
