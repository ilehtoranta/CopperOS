/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The MorphOS Area docs define MUIA_Font and MUIA_CustomFont as competing
// choices: whichever setter runs last overrides the other. Keep that choice
// as a named guest record instead of inferring it from a private object layout.
public enum MuiAreaFontSelectionKind : uint
{
	None = 0,
	Font = 1,
	CustomFont = 2,
}

public struct MuiAreaFontSelectionState
{
	public MuiAreaFontSelectionKind Active;
	public APTR Source;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFontSelectionStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ActiveOffset = 4;
	internal const uint SourceOffset = 8;
	internal const uint GenerationOffset = 12;
	internal const uint Cookie = 0x4146534Cu; // 'AFSL'

	internal uint Magic;
	internal uint Active;
	internal APTR Source;
	internal uint Generation;
}

// The record captures the last-writer rule between MUIA_Font and
// MUIA_CustomFont.  Active is a closed semantic choice; Source remains an
// opaque caller-owned pointer and is preserved losslessly.  A non-zero
// generation distinguishes an initialized record from arbitrary guest
// memory, while live-owner validation keeps stale Dataspace state out of
// font-resolution consumers.
internal static class MuiAreaFontSelectionStateAdmission
{
	internal static bool Validate(MuiAreaFontSelectionState value) =>
		value.Active <= MuiAreaFontSelectionKind.CustomFont &&
		value.Generation != 0 &&
		(value.Active != MuiAreaFontSelectionKind.None || value.Source.IsNull);

	internal static bool Validate(MuiAreaFontSelectionStateRecord value)
	{
		if (value.Magic != MuiAreaFontSelectionStateRecord.Cookie) return false;
		var state = default(MuiAreaFontSelectionState);
		state.Active = (MuiAreaFontSelectionKind)value.Active;
		state.Source = value.Source;
		state.Generation = value.Generation;
		return Validate(state);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaFontSelectionStateField : byte
{
	Magic,
	Active,
	Source,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaFontSelectionStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaFontSelectionStateField Field;
}

internal static class MuiAreaFontSelectionStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFontSelectionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaFontSelectionStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaFontSelectionStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area FontSelection state is transferred as a named record. Numeric
// guest positions are confined to this ABI adapter; the compatibility cursor
// above remains available only to legacy callers and malformed-state
// diagnostics.
internal static class MuiAreaFontSelectionStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaFontSelectionStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaFontSelectionStateField.Magic:
				offset = MuiAreaFontSelectionStateRecord.MagicOffset;
				return true;
			case MuiAreaFontSelectionStateField.Active:
				offset = MuiAreaFontSelectionStateRecord.ActiveOffset;
				return true;
			case MuiAreaFontSelectionStateField.Source:
				offset = MuiAreaFontSelectionStateRecord.SourceOffset;
				return true;
			case MuiAreaFontSelectionStateField.Generation:
				offset = MuiAreaFontSelectionStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaFontSelectionStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaFontSelectionStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaFontSelectionStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaFontSelectionStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var source) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		value.Source = APTR.FromPointer(source);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaFontSelectionStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Active) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Source.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaFontSelectionStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaFontSelectionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaFontSelectionStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
