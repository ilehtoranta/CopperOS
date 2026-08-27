/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_CustomFont is a caller-owned MUI font-specification string. Keep the
// pointer and presence separate so an explicit NULL value remains distinct
// from an omitted attribute, without retaining a managed string.
public struct MuiAreaCustomFontStateInput
{
	public APTR Spec;
	public uint Present;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCustomFontStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint SpecOffset = 4;
	internal const uint PresentOffset = 8;
	internal const uint GenerationOffset = 12;
	internal const uint Cookie = 0x4143464Eu; // 'ACFN'

	internal uint Magic;
	internal APTR Spec;
	internal uint Present;
	internal uint Generation;
}

internal static class MuiAreaCustomFontStateAdmission
{
	internal static bool Validate(MuiAreaCustomFontStateRecord value) =>
		value.Magic == MuiAreaCustomFontStateRecord.Cookie &&
		value.Present <= 1 && value.Generation != 0 &&
		(value.Present != 0 || value.Spec.IsNull);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaCustomFontStateField : byte
{
	Magic,
	Spec,
	Present,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCustomFontStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaCustomFontStateField Field;
}

internal static class MuiAreaCustomFontStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCustomFontStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCustomFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaCustomFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area CustomFont state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaCustomFontStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaCustomFontStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaCustomFontStateField.Magic:
				offset = MuiAreaCustomFontStateRecord.MagicOffset;
				return true;
			case MuiAreaCustomFontStateField.Spec:
				offset = MuiAreaCustomFontStateRecord.SpecOffset;
				return true;
			case MuiAreaCustomFontStateField.Present:
				offset = MuiAreaCustomFontStateRecord.PresentOffset;
				return true;
			case MuiAreaCustomFontStateField.Generation:
				offset = MuiAreaCustomFontStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaCustomFontStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaCustomFontStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaCustomFontStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaCustomFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaCustomFontStateField.Magic, out value.Magic) ||
			!MuiAreaCustomFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Spec, out var spec) ||
			!MuiAreaCustomFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Present, out value.Present) ||
			!MuiAreaCustomFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Generation,
				out value.Generation)) return false;
		value.Spec = APTR.FromPointer(spec);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadStructural(ref platform, address, out value) &&
			MuiAreaCustomFontStateAdmission.Validate(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaCustomFontStateRecord.Size) ||
			!MuiAreaCustomFontStateAdmission.Validate(value)) return false;
		return MuiAreaCustomFontStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaCustomFontStateField.Magic,
			value.Magic) &&
			MuiAreaCustomFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Spec, value.Spec.Raw) &&
			MuiAreaCustomFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Present, value.Present) &&
			MuiAreaCustomFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Generation, value.Generation);
	}
}
