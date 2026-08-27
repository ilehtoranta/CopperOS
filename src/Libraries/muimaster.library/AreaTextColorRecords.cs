/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public Area projection of MorphOS MUIA_TextColor. Color is a packed
// 00RRGGBB value; Active is non-zero only during MUIM_Setup..MUIM_Cleanup.
public struct MuiAreaTextColorState
{
	public uint Color;
	public uint Active;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTextColorStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ColorOffset = 4;
	internal const uint ActiveOffset = 8;
	internal const uint GenerationOffset = 12;
	internal const uint Cookie = 0x4D544352u; // 'MTCR'

	internal uint Magic;
	internal uint Color;
	internal uint Active;
	internal uint Generation;
}

// TextColor is a setup-scoped packed RGB value. The color is constrained to
// the documented 24-bit payload, Active is a canonical BOOL, and generation
// identifies the current setup/cleanup publication. Live consumers additionally
// require ownership by the current object.
internal static class MuiAreaTextColorStateAdmission
{
	internal static bool Validate(MuiAreaTextColorStateRecord value) =>
		value.Magic == MuiAreaTextColorStateRecord.Cookie &&
		value.Color <= 0x00FFFFFFu && value.Active <= 1 &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaTextColorStateField : byte
{
	Magic,
	Color,
	Active,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTextColorStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaTextColorStateField Field;
}

internal static class MuiAreaTextColorStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaTextColorStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaTextColorStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaTextColorStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area TextColor state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaTextColorStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaTextColorStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaTextColorStateField.Magic:
				offset = MuiAreaTextColorStateRecord.MagicOffset;
				return true;
			case MuiAreaTextColorStateField.Color:
				offset = MuiAreaTextColorStateRecord.ColorOffset;
				return true;
			case MuiAreaTextColorStateField.Active:
				offset = MuiAreaTextColorStateRecord.ActiveOffset;
				return true;
			case MuiAreaTextColorStateField.Generation:
				offset = MuiAreaTextColorStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaTextColorStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaTextColorStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaTextColorStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaTextColorStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiAreaTextColorStateField.Magic, out value.Magic) ||
			!MuiAreaTextColorStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaTextColorStateField.Color, out value.Color) ||
			!MuiAreaTextColorStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaTextColorStateField.Active, out value.Active) ||
			!MuiAreaTextColorStateRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiAreaTextColorStateField.Generation, out value.Generation))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaTextColorStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTextColorStateRecord.Size) || value.Magic !=
			MuiAreaTextColorStateRecord.Cookie ||
			!MuiAreaTextColorStateAdmission.Validate(value)) return false;
		return MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaTextColorStateField.Magic, value.Magic) &&
			MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaTextColorStateField.Color, value.Color) &&
			MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaTextColorStateField.Active, value.Active) &&
			MuiAreaTextColorStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaTextColorStateField.Generation, value.Generation);
	}
}
