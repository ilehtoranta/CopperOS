/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Area weight input. Keep the public semantic value separate from the
// resolved horizontal/vertical weights in MuiAreaLayoutPolicyStateRecord: the
// MorphOS MUIA_Weight tag is one caller-facing default source.
public struct MuiAreaWeightState
{
	public uint Weight;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaWeightStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint WeightOffset = 4;
	internal const uint Cookie = 0x41574754u; // 'AWGT'

	internal uint Magic;
	internal uint Weight;
}

// Weight is an opaque MorphOS ULONG input. Admission owns the record cookie
// and live Area capability; the full Weight range remains representable for
// compatibility and raw synchronization.
internal static class MuiAreaWeightStateAdmission
{
	internal static bool Validate(MuiAreaWeightStateRecord value) =>
		value.Magic == MuiAreaWeightStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaWeightStateField : byte
{
	Magic,
	Weight,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaWeightStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaWeightStateField Field;
}

internal static class MuiAreaWeightStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaWeightStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaWeightStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaWeightStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaWeightStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaWeightStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaWeightStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area weight state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaWeightStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaWeightStateField field,
		out uint offset)
	{
		if (field == MuiAreaWeightStateField.Magic)
			offset = MuiAreaWeightStateRecord.MagicOffset;
		else if (field == MuiAreaWeightStateField.Weight)
			offset = MuiAreaWeightStateRecord.WeightOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaWeightStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaWeightStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaWeightStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaWeightStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaWeightStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaWeightStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaWeightStateField.Weight)
			value = state.Weight;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaWeightStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaWeightStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaWeightStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaWeightStateField.Weight)
			state.Weight = value;
		else return false;
		return MuiAreaWeightStateRecordCodec.WriteRecord(ref platform, record, state);
	}
}

internal static class MuiAreaWeightStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaWeightStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Weight)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaWeightStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Weight) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiAreaWeightStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaWeightStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaWeightStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
