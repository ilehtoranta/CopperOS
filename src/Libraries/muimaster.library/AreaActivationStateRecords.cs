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
		if (field == MuiAreaActivationStateField.Signature)
			offset = MuiAreaActivationStateRecord.SignatureOffset;
		else if (field == MuiAreaActivationStateField.Active)
			offset = MuiAreaActivationStateRecord.ActiveOffset;
		else if (field == MuiAreaActivationStateField.Flags)
			offset = MuiAreaActivationStateRecord.FlagsOffset;
		else if (field == MuiAreaActivationStateField.Generation)
			offset = MuiAreaActivationStateRecord.GenerationOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
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
		if (!MuiAreaActivationStateCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaActivationStateField.Signature)
			value = state.Signature;
		else if (field == MuiAreaActivationStateField.Active)
			value = state.Active;
		else if (field == MuiAreaActivationStateField.Flags)
			value = state.Flags;
		else if (field == MuiAreaActivationStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaActivationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaActivationStateCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAreaActivationStateField.Signature)
			state.Signature = value;
		else if (field == MuiAreaActivationStateField.Active)
			state.Active = value;
		else if (field == MuiAreaActivationStateField.Flags)
			state.Flags = value;
		else if (field == MuiAreaActivationStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaActivationStateCodec.WriteRecord(ref platform, record, state);
	}
}

internal static class MuiAreaActivationStateCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaActivationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Signature) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaActivationStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Signature) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Active) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiAreaActivationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaActivationStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaActivationStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
