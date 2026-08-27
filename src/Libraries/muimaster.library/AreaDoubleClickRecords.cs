/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_DoubleClick is a getter-only signed LONG Area signal. Keep the full
// 32-bit value in a named state record so event producers and OM_GET share one
// typed contract without exposing a private object-layout offset.
public struct MuiAreaDoubleClickStateInput
{
	public int Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoubleClickStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ValueOffset = 4;
	internal const uint GenerationOffset = 8;
	internal const uint Cookie = 0x4144434Cu; // 'ADCL'

	internal uint Magic;
	internal int Value;
	internal uint Generation;
}

// DoubleClick is a getter-only signed LONG signal.  Its value is unrestricted
// and remains lossless; admission protects the named record's identity and
// initialization lifetime and verifies live ownership at consumer seams.
internal static class MuiAreaDoubleClickStateAdmission
{
	internal static bool Validate(MuiAreaDoubleClickStateRecord value) =>
		value.Magic == MuiAreaDoubleClickStateRecord.Cookie &&
		value.Generation != 0;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaDoubleClickStateField : byte
{
	Magic,
	Value,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDoubleClickStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaDoubleClickStateField Field;
}

internal static class MuiAreaDoubleClickStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDoubleClickStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoubleClickStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoubleClickStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaDoubleClickStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area DoubleClick state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaDoubleClickStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaDoubleClickStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaDoubleClickStateField.Magic:
				offset = MuiAreaDoubleClickStateRecord.MagicOffset;
				return true;
			case MuiAreaDoubleClickStateField.Value:
				offset = MuiAreaDoubleClickStateRecord.ValueOffset;
				return true;
			case MuiAreaDoubleClickStateField.Generation:
				offset = MuiAreaDoubleClickStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaDoubleClickStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaDoubleClickStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDoubleClickStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaDoubleClickStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaDoubleClickStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaDoubleClickStateField.Magic,
			out value.Magic) ||
			!MuiAreaDoubleClickStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiAreaDoubleClickStateField.Value,
				out var rawValue) ||
			!MuiAreaDoubleClickStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiAreaDoubleClickStateField.Generation,
				out value.Generation)) return false;
		value.Value = unchecked((int)rawValue);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaDoubleClickStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDoubleClickStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDoubleClickStateRecord.Size) || value.Magic !=
			MuiAreaDoubleClickStateRecord.Cookie ||
			!MuiAreaDoubleClickStateAdmission.Validate(value)) return false;
		return MuiAreaDoubleClickStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaDoubleClickStateField.Magic, value.Magic) &&
			MuiAreaDoubleClickStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaDoubleClickStateField.Value,
				unchecked((uint)value.Value)) &&
			MuiAreaDoubleClickStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaDoubleClickStateField.Generation, value.Generation);
	}
}
