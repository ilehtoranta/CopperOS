/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Scale-only presentation state. The BOOL orientation value remains
// MorphOS-compatible while construction, runtime mutation, and drawing use a
// named value rather than a repeated scalar lookup.
public struct MuiScalePresentationState
{
	public uint Horizontal;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScalePresentationStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HorizontalOffset = 4;
	internal const uint Cookie = 0x4D53434Cu; // 'MSCL'

	internal uint Magic;
	internal uint Horizontal;
}

internal enum MuiScalePresentationStateField : byte
{
	Magic,
	Horizontal,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScalePresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiScalePresentationStateField Field;
}

internal static class MuiScalePresentationStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScalePresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScalePresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiScalePresentationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScalePresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScalePresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Scale consumers use the named
// presentation record; this bounded adapter is the only layer that translates
// its fixed guest layout into addresses. The cursor codec remains available
// for compatibility and malformed-state diagnostics.
internal static class MuiScalePresentationStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiScalePresentationStateField field,
		out uint index)
	{
		if (field == MuiScalePresentationStateField.Magic)
			index = 0;
		else if (field == MuiScalePresentationStateField.Horizontal)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScalePresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScalePresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiScalePresentationStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiScalePresentationStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiScalePresentationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiScalePresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiScalePresentationStateField.Magic)
			value = state.Magic;
		else if (field == MuiScalePresentationStateField.Horizontal)
			value = state.Horizontal;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScalePresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiScalePresentationStateField.Magic)
			state.Magic = value;
		else if (field == MuiScalePresentationStateField.Horizontal)
			state.Horizontal = value;
		else return false;
		return MuiScalePresentationStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiScalePresentationStateRecordCodec
{
	// Production access is sequential and struct-shaped. The field-address
	// adapters above remain available for compatibility diagnostics only.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiScalePresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var horizontal) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Magic = magic;
		value.Horizontal = horizontal;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiScalePresentationStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiScalePresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Horizontal)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScalePresentationStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

// Keep the wire field lossless for malformed-state diagnostics. Scale's
// Horizontal value is a MorphOS BOOL and must be canonical before layout or
// drawing consumers use the named presentation state.
internal static class MuiScalePresentationStateAdmission
{
	internal static bool Validate(MuiScalePresentationStateRecord value) =>
		value.Magic == MuiScalePresentationStateRecord.Cookie &&
		value.Horizontal <= 1;

	internal static bool Validate(MuiScalePresentationState value) =>
		value.Horizontal <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing Scale presentation call sites. New state
// boundaries use MuiScalePresentationStateAdmission directly so live
// ownership is explicit.
internal static class MuiScalePresentationStateValidation
{
	internal static bool IsValidRecord(MuiScalePresentationStateRecord value) =>
		MuiScalePresentationStateAdmission.Validate(value);

	internal static bool IsValidState(MuiScalePresentationState value) =>
		MuiScalePresentationStateAdmission.Validate(value);
}
