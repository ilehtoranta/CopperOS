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

// Fixed initializer-policy state is read and written as a named value. Keep
// the packed guest positions in this ABI adapter; production consumers do not
// select fields through the compatibility cursor.
internal static class MuiApplicationPolicyStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationPolicyStateField.Magic:
				offset = MuiApplicationPolicyStateRecord.MagicOffset;
				return true;
			case MuiApplicationPolicyStateField.UseRexx:
				offset = MuiApplicationPolicyStateRecord.UseRexxOffset;
				return true;
			case MuiApplicationPolicyStateField.UseCommodities:
				offset = MuiApplicationPolicyStateRecord.UseCommoditiesOffset;
				return true;
			case MuiApplicationPolicyStateField.UseScreenNotify:
				offset = MuiApplicationPolicyStateRecord.UseScreenNotifyOffset;
				return true;
		}
		offset = 0;
		return false;
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
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationPolicyStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationPolicyStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationPolicyStateField.Magic,
			out var magic) ||
			!MuiApplicationPolicyStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationPolicyStateField.UseRexx,
				out value.UseRexx) ||
			!MuiApplicationPolicyStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationPolicyStateField.UseCommodities,
				out value.UseCommodities) ||
			!MuiApplicationPolicyStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationPolicyStateField.UseScreenNotify,
				out value.UseScreenNotify)) return false;
		value.Magic = magic;
		return true;
	}

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
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationPolicyStateRecord.Size) ||
			!MuiApplicationPolicyStateAdmission.Validate(value)) return false;
		return MuiApplicationPolicyStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationPolicyStateField.Magic,
			value.Magic) &&
			MuiApplicationPolicyStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationPolicyStateField.UseRexx,
				value.UseRexx) &&
			MuiApplicationPolicyStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationPolicyStateField.UseCommodities,
				value.UseCommodities) &&
			MuiApplicationPolicyStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationPolicyStateField.UseScreenNotify,
				value.UseScreenNotify);
	}
}
