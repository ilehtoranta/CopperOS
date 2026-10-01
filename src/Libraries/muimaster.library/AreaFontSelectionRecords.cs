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
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFontSelectionStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaFontSelectionStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

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

// Fixed Area FontSelection state is transferred as a named record. The bounded
// cursor walks the complete packed struct before selecting a field; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
internal static class MuiAreaFontSelectionStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaFontSelectionStateField field,
		out uint index)
	{
		if (field == MuiAreaFontSelectionStateField.Magic) index = 0;
		else if (field == MuiAreaFontSelectionStateField.Active) index = 1;
		else if (field == MuiAreaFontSelectionStateField.Source) index = 2;
		else if (field == MuiAreaFontSelectionStateField.Generation) index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaFontSelectionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaFontSelectionStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaFontSelectionStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaFontSelectionStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaFontSelectionStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaFontSelectionStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaFontSelectionStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaFontSelectionStateField.Active)
			value = state.Active;
		else if (field == MuiAreaFontSelectionStateField.Source)
			value = state.Source.Raw;
		else if (field == MuiAreaFontSelectionStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaFontSelectionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaFontSelectionStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaFontSelectionStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaFontSelectionStateField.Active)
			state.Active = value;
		else if (field == MuiAreaFontSelectionStateField.Source)
			state.Source = APTR.FromPointer(value);
		else if (field == MuiAreaFontSelectionStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaFontSelectionStateRecordCodec.WriteRecord(ref platform, record,
			state);
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
