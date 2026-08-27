/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_BuiltinFont is an unsigned ABI value whose documented selectors are
// negative signed constants (MUIV_BuiltinFont_*). Keep the raw ULONG bit
// pattern so the guest can distinguish an explicit Inherit selector from an
// absent tag without relying on a private object offset.
public struct MuiAreaBuiltinFontState
{
	public uint Selector;
	public uint Present;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaBuiltinFontStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint SelectorOffset = 4;
	internal const uint PresentOffset = 8;
	internal const uint GenerationOffset = 12;
	internal const uint Cookie = 0x4D424652u; // 'MBFR'

	internal uint Magic;
	internal uint Selector;
	internal uint Present;
	internal uint Generation;
}

// A BuiltinFont record is a persisted projection, not an arbitrary scratch
// buffer.  Keep the MorphOS selector as a lossless ULONG (the documented
// MUIV_BuiltinFont values are signed constants) while admitting only the
// canonical presence flag and a non-zero generation.  Live ownership is
// checked at the consumer boundary so a stale Dataspace block cannot be
// mistaken for the current object's state.
internal static class MuiAreaBuiltinFontStateAdmission
{
	internal static bool Validate(MuiAreaBuiltinFontState value) =>
		value.Present <= 1 && value.Generation != 0;

	internal static bool Validate(MuiAreaBuiltinFontStateRecord value)
	{
		if (value.Magic != MuiAreaBuiltinFontStateRecord.Cookie) return false;
		var state = default(MuiAreaBuiltinFontState);
		state.Selector = value.Selector;
		state.Present = value.Present;
		state.Generation = value.Generation;
		return Validate(state);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaBuiltinFontStateField : byte
{
	Magic,
	Selector,
	Present,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaBuiltinFontStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaBuiltinFontStateField Field;
}

internal static class MuiAreaBuiltinFontStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaBuiltinFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaBuiltinFontStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaBuiltinFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaBuiltinFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area BuiltinFont state is transferred as a named record. Numeric
// guest positions are confined to this ABI adapter; the compatibility cursor
// above remains available only to legacy callers and malformed-state
// diagnostics.
internal static class MuiAreaBuiltinFontStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaBuiltinFontStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaBuiltinFontStateField.Magic:
				offset = MuiAreaBuiltinFontStateRecord.MagicOffset;
				return true;
			case MuiAreaBuiltinFontStateField.Selector:
				offset = MuiAreaBuiltinFontStateRecord.SelectorOffset;
				return true;
			case MuiAreaBuiltinFontStateField.Present:
				offset = MuiAreaBuiltinFontStateRecord.PresentOffset;
				return true;
			case MuiAreaBuiltinFontStateField.Generation:
				offset = MuiAreaBuiltinFontStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaBuiltinFontStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaBuiltinFontStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaBuiltinFontStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaBuiltinFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaBuiltinFontStateField.Magic, out value.Magic) ||
			!MuiAreaBuiltinFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Selector, out value.Selector) ||
			!MuiAreaBuiltinFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Present, out value.Present) ||
			!MuiAreaBuiltinFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Generation, out value.Generation))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaBuiltinFontStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaBuiltinFontStateRecord.Size) || value.Magic !=
			MuiAreaBuiltinFontStateRecord.Cookie ||
			!MuiAreaBuiltinFontStateAdmission.Validate(value)) return false;
		return MuiAreaBuiltinFontStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaBuiltinFontStateField.Magic, value.Magic) &&
			MuiAreaBuiltinFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Selector, value.Selector) &&
			MuiAreaBuiltinFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Present, value.Present) &&
			MuiAreaBuiltinFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Generation, value.Generation);
	}
}
