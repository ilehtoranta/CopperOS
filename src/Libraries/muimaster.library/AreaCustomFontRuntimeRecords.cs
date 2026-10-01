/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Runtime result owned by an Area after OpenCustomFont/Setup.  Font is an
// opaque provider handle; Spec is the caller-owned guest string from which it
// was opened.  Keeping both as APTR values makes the lifetime explicit without
// introducing a managed font object.
public struct MuiAreaCustomFontHandleState
{
	public APTR Font;
	public APTR Spec;
	public uint Generation;
	public uint Active;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiAreaCustomFontRuntimeRecord
{
	public const uint Size = 20;
	public const uint FieldSize = 4;
	public const uint MagicOffset = 0;
	public const uint FontOffset = 4;
	public const uint SpecOffset = 8;
	public const uint GenerationOffset = 12;
	public const uint ActiveOffset = 16;
	public const uint Cookie = 0x41434652u; // 'ACFR'
	internal const uint ClosingActive = uint.MaxValue;

	public uint Magic;
	public APTR Font;
	public APTR Spec;
	public uint Generation;
	public uint Active;
}

public static class MuiAreaCustomFontRuntimeStateAdmission
{
	public static bool Validate(MuiAreaCustomFontRuntimeRecord value) =>
		value.Magic == MuiAreaCustomFontRuntimeRecord.Cookie &&
		value.Active <= 1 && value.Generation != 0 &&
		(value.Active != 0 || (value.Font.IsNull && value.Spec.IsNull));

	public static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaCustomFontRuntimeRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

public enum MuiAreaCustomFontRuntimeField : byte
{
	Magic,
	Font,
	Spec,
	Generation,
	Active,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiAreaCustomFontRuntimeFieldCursor
{
	public APTR Record;
	public MuiAreaCustomFontRuntimeField Field;
}

public static class MuiAreaCustomFontRuntimeFieldCursorCodec
{
	public static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontRuntimeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	public static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontRuntimeFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaCustomFontRuntimeRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	public static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontRuntimeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCustomFontRuntimeRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	public static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontRuntimeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCustomFontRuntimeRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed CustomFont runtime state is transferred as a named record. The bounded
// cursor walks the complete packed struct before selecting a field; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
public static class MuiAreaCustomFontRuntimeRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaCustomFontRuntimeField field,
		out uint index)
	{
		if (field == MuiAreaCustomFontRuntimeField.Magic) index = 0;
		else if (field == MuiAreaCustomFontRuntimeField.Font) index = 1;
		else if (field == MuiAreaCustomFontRuntimeField.Spec) index = 2;
		else if (field == MuiAreaCustomFontRuntimeField.Generation) index = 3;
		else if (field == MuiAreaCustomFontRuntimeField.Active) index = 4;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	public static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontRuntimeField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaCustomFontRuntimeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	public static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontRuntimeFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaCustomFontRuntimeRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaCustomFontRuntimeRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaCustomFontRuntimeRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	public static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiAreaCustomFontRuntimeRecord.Size ||
			MuiAreaCustomFontRuntimeRecord.Size - offset < 4 ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaCustomFontRuntimeRecord.Size) &&
			platform.IsMapped(address, 4);
	}

	public static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontRuntimeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaCustomFontRuntimeRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaCustomFontRuntimeField.Magic)
			value = state.Magic;
		else if (field == MuiAreaCustomFontRuntimeField.Font)
			value = state.Font.Raw;
		else if (field == MuiAreaCustomFontRuntimeField.Spec)
			value = state.Spec.Raw;
		else if (field == MuiAreaCustomFontRuntimeField.Generation)
			value = state.Generation;
		else if (field == MuiAreaCustomFontRuntimeField.Active)
			value = state.Active;
		else return false;
		return true;
	}

	public static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	public static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontRuntimeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaCustomFontRuntimeRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaCustomFontRuntimeField.Magic)
			state.Magic = value;
		else if (field == MuiAreaCustomFontRuntimeField.Font)
			state.Font = APTR.FromPointer(value);
		else if (field == MuiAreaCustomFontRuntimeField.Spec)
			state.Spec = APTR.FromPointer(value);
		else if (field == MuiAreaCustomFontRuntimeField.Generation)
			state.Generation = value;
		else if (field == MuiAreaCustomFontRuntimeField.Active)
			state.Active = value;
		else return false;
		return MuiAreaCustomFontRuntimeRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	public static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address))
			return false;
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
	}
}

public static class MuiAreaCustomFontRuntimeRecordCodec
{
	public static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaCustomFontRuntimeRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCustomFontRuntimeRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var font) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var spec) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active)) return false;
		value.Font = APTR.FromPointer(font);
		value.Spec = APTR.FromPointer(spec);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	public static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaCustomFontRuntimeRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaCustomFontRuntimeRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Font.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Spec.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Active) && MuiGuestStructCursor.IsComplete(cursor);

	public static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaCustomFontRuntimeRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	public static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCustomFontRuntimeRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadStructural(ref platform, address, out value) &&
			MuiAreaCustomFontRuntimeStateAdmission.Validate(value);
	}

	public static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCustomFontRuntimeRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaCustomFontRuntimeStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
