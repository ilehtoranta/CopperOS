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
	{
		return MuiScalePresentationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

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
	private static bool TryResolve(MuiScalePresentationStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiScalePresentationStateField.Magic:
				offset = MuiScalePresentationStateRecord.MagicOffset;
				return true;
			case MuiScalePresentationStateField.Horizontal:
				offset = MuiScalePresentationStateRecord.HorizontalOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiScalePresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiScalePresentationStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScalePresentationStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiScalePresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiScalePresentationStateField.Magic, out value.Magic) &&
			MuiScalePresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiScalePresentationStateField.Horizontal, out value.Horizontal);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiScalePresentationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScalePresentationStateAdmission.Validate(value)) return false;
		return MuiScalePresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiScalePresentationStateField.Magic, value.Magic) &&
			MuiScalePresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiScalePresentationStateField.Horizontal, value.Horizontal);
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
