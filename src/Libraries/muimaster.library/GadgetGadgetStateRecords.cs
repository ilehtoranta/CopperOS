/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the getter-only MUIA_Gadget_Gadget relationship.
// The pointer is caller-owned guest state; no managed gadget wrapper is
// created or retained.
public struct MuiGadgetGadgetState
{
	public APTR Gadget;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGadgetGadgetStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D474744u; // 'MGGD'

	internal uint Magic;
	internal APTR Gadget;
}

internal enum MuiGadgetGadgetStateField : byte
{
	Magic,
	Gadget,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGadgetGadgetStateFieldCursor
{
	internal APTR Record;
	internal MuiGadgetGadgetStateField Field;
}

internal static class MuiGadgetGadgetStateFieldCursorCodec
{
	private static bool TryResolve(MuiGadgetGadgetStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiGadgetGadgetStateField.Magic => 0,
			MuiGadgetGadgetStateField.Gadget => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGadgetGadgetStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiGadgetGadgetStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetGadgetStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGadgetGadgetStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetGadgetStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGadgetGadgetStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGadgetGadgetStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGadgetGadgetStateRecord.Size) ||
			!MuiGadgetGadgetStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGadgetGadgetStateField.Magic, out var magic) ||
			magic != MuiGadgetGadgetStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiGadgetGadgetStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGadgetGadgetStateField.Gadget, out var gadget)) return false;
		value.Gadget = APTR.FromPointer(gadget);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGadgetGadgetStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGadgetGadgetStateRecord.Size) || value.Magic !=
			MuiGadgetGadgetStateRecord.Cookie) return false;
		return MuiGadgetGadgetStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGadgetGadgetStateField.Magic, value.Magic) &&
			MuiGadgetGadgetStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGadgetGadgetStateField.Gadget, value.Gadget.Raw);
	}
}
