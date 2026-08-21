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
	internal const uint Cookie = 0x41435456u; // "ACTV"
	internal uint Signature;
	internal uint Active;
	internal uint Flags;
	internal uint Generation;
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
	private static bool TryResolve(MuiAreaActivationStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaActivationStateField.Signature:
				offset = 0;
				return true;
			case MuiAreaActivationStateField.Active:
				offset = 4;
				return true;
			case MuiAreaActivationStateField.Flags:
				offset = 8;
				return true;
			case MuiAreaActivationStateField.Generation:
				offset = 12;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaActivationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiAreaActivationStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaActivationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaActivationStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaActivationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaActivationStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiAreaActivationStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaActivationStateRecord.Size) ||
			!MuiAreaActivationStateFieldCursorCodec.TryRead(ref platform, address,
				MuiAreaActivationStateField.Signature, out value.Signature) ||
			!MuiAreaActivationStateFieldCursorCodec.TryRead(ref platform, address,
				MuiAreaActivationStateField.Active, out value.Active) ||
			!MuiAreaActivationStateFieldCursorCodec.TryRead(ref platform, address,
				MuiAreaActivationStateField.Flags, out value.Flags) ||
			!MuiAreaActivationStateFieldCursorCodec.TryRead(ref platform, address,
				MuiAreaActivationStateField.Generation, out value.Generation) ||
			value.Signature != MuiAreaActivationStateRecord.Cookie)
			return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaActivationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || value.Signature != MuiAreaActivationStateRecord.Cookie ||
			!platform.IsMapped(address, MuiAreaActivationStateRecord.Size))
			return false;
		return MuiAreaActivationStateFieldCursorCodec.TryWrite(ref platform,
			address, MuiAreaActivationStateField.Signature, value.Signature) &&
			MuiAreaActivationStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiAreaActivationStateField.Active, value.Active) &&
			MuiAreaActivationStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiAreaActivationStateField.Flags, value.Flags) &&
			MuiAreaActivationStateFieldCursorCodec.TryWrite(ref platform, address,
				MuiAreaActivationStateField.Generation, value.Generation);
	}
}

