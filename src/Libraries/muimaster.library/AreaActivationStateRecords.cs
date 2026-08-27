/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident state for the Area activation lifecycle.  Active and Flags
// are one logical transition and therefore cross the Dataspace boundary as a
// single named record instead of two private attribute slots.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaActivationStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint SignatureOffset = 0;
	internal const uint ActiveOffset = 4;
	internal const uint FlagsOffset = 8;
	internal const uint GenerationOffset = 12;
	internal const uint Cookie = 0x41435456u; // "ACTV"
	internal uint Signature;
	internal uint Active;
	internal uint Flags;
	internal uint Generation;
}

// Active is a MorphOS BOOL projection and must remain canonical. Flags and
// Generation retain their complete ULONG ranges; the record is published only
// for a live Area object so malformed activation state cannot drive consumers.
internal static class MuiAreaActivationStateAdmission
{
	internal static bool Validate(MuiAreaActivationStateRecord value) =>
		value.Signature == MuiAreaActivationStateRecord.Cookie &&
		value.Active <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaActivationStateField : byte
{
	Signature,
	Active,
	Flags,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaActivationStateFieldCursor
{
	internal APTR Address;
	internal MuiAreaActivationStateField Field;
}

internal static class MuiAreaActivationStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaActivationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaActivationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaActivationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaActivationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, field, out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaActivationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaActivationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, field, value);
	}
}

// Fixed Area activation state is transferred as a named record. Numeric guest
// positions are confined to this ABI adapter; the compatibility cursor above
// remains available only to legacy callers and malformed-state diagnostics.
internal static class MuiAreaActivationStateRecordMemoryCodec
{
	private static bool TryResolve(MuiAreaActivationStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaActivationStateField.Signature:
				offset = MuiAreaActivationStateRecord.SignatureOffset;
				return true;
			case MuiAreaActivationStateField.Active:
				offset = MuiAreaActivationStateRecord.ActiveOffset;
				return true;
			case MuiAreaActivationStateField.Flags:
				offset = MuiAreaActivationStateRecord.FlagsOffset;
				return true;
			case MuiAreaActivationStateField.Generation:
				offset = MuiAreaActivationStateRecord.GenerationOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaActivationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiAreaActivationStateRecord.Size) &&
			platform.IsMapped(address, MuiAreaActivationStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaActivationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaActivationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaActivationStateCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAreaActivationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiAreaActivationStateField.Signature,
			out value.Signature) ||
			!MuiAreaActivationStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiAreaActivationStateField.Active,
				out value.Active) ||
			!MuiAreaActivationStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiAreaActivationStateField.Flags,
				out value.Flags) ||
			!MuiAreaActivationStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiAreaActivationStateField.Generation,
				out value.Generation))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiAreaActivationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaActivationStateAdmission.Validate(value) ||
			!platform.IsMapped(address, MuiAreaActivationStateRecord.Size))
			return false;
		return MuiAreaActivationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiAreaActivationStateField.Signature,
			value.Signature) &&
			MuiAreaActivationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaActivationStateField.Active, value.Active) &&
			MuiAreaActivationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaActivationStateField.Flags, value.Flags) &&
			MuiAreaActivationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiAreaActivationStateField.Generation, value.Generation);
	}
}
