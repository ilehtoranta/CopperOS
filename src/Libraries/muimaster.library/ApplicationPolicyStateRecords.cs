/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Initializer-only Application policy BOOLs. Values are canonical MorphOS
// ULONG booleans and remain projected to the public attributes.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPolicyStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint UseRexxOffset = 4;
	internal const uint UseCommoditiesOffset = 8;
	internal const uint UseScreenNotifyOffset = 12;
	internal const uint Cookie = 0x41504F4Cu; // 'APOL'

	internal uint Magic;
	internal uint UseRexx;
	internal uint UseCommodities;
	internal uint UseScreenNotify;
}

// The structural codec owns the packed guest representation. This admission
// boundary owns only the canonical MorphOS ULONG BOOL invariants for the
// initializer policy record.
internal static class MuiApplicationPolicyStateAdmission
{
	internal static bool Validate(MuiApplicationPolicyStateRecord value) =>
		value.Magic == MuiApplicationPolicyStateRecord.Cookie &&
		value.UseRexx <= 1 && value.UseCommodities <= 1 &&
		value.UseScreenNotify <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
}

internal enum MuiApplicationPolicyStateField : byte
{
	Magic,
	UseRexx,
	UseCommodities,
	UseScreenNotify,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationPolicyStateField Field;
}

internal static class MuiApplicationPolicyStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationPolicyStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed initializer-policy state is transferred as a named record. Numeric
// guest positions are confined to the bounded ABI adapter; production
// consumers exchange the declaration-order struct through the sequential
// cursor below.
internal static class MuiApplicationPolicyStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationPolicyStateField field,
		out uint offset)
	{
		if (field == MuiApplicationPolicyStateField.Magic)
			offset = MuiApplicationPolicyStateRecord.MagicOffset;
		else if (field == MuiApplicationPolicyStateField.UseRexx)
			offset = MuiApplicationPolicyStateRecord.UseRexxOffset;
		else if (field == MuiApplicationPolicyStateField.UseCommodities)
			offset = MuiApplicationPolicyStateRecord.UseCommoditiesOffset;
		else if (field == MuiApplicationPolicyStateField.UseScreenNotify)
			offset = MuiApplicationPolicyStateRecord.UseScreenNotifyOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationPolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationPolicyStateRecord.Size) &&
			platform.IsMapped(address, MuiApplicationPolicyStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationPolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationPolicyStateField.UseRexx)
			value = state.UseRexx;
		else if (field == MuiApplicationPolicyStateField.UseCommodities)
			value = state.UseCommodities;
		else if (field == MuiApplicationPolicyStateField.UseScreenNotify)
			value = state.UseScreenNotify;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationPolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationPolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationPolicyStateField.UseRexx)
			state.UseRexx = value;
		else if (field == MuiApplicationPolicyStateField.UseCommodities)
			state.UseCommodities = value;
		else if (field == MuiApplicationPolicyStateField.UseScreenNotify)
			state.UseScreenNotify = value;
		else return false;
		return MuiApplicationPolicyStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationPolicyStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationPolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UseRexx) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UseCommodities) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.UseScreenNotify)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationPolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseRexx) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseCommodities) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.UseScreenNotify) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadStructural(ref platform, address, out value) &&
			MuiApplicationPolicyStateAdmission.Validate(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationPolicyStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
