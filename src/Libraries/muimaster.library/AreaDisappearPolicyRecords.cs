/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The MorphOS disappearance priorities are signed LONG values.  Keep both
// related inputs in one fixed-width guest record so public Get/Set and future
// group layout selection use the same state without exposing object offsets.
public struct MuiAreaDisappearPolicyStateInput
{
	public int HorizDisappear;
	public int VertDisappear;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDisappearPolicyStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41445052u; // 'ADPR'

	internal uint Magic;
	internal int HorizDisappear;
	internal int VertDisappear;
}

internal enum MuiAreaDisappearPolicyStateField : byte
{
	Magic,
	HorizDisappear,
	VertDisappear,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDisappearPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaDisappearPolicyStateField Field;
}

internal static class MuiAreaDisappearPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaDisappearPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaDisappearPolicyStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaDisappearPolicyStateField.HorizDisappear:
				offset = 4;
				return true;
			case MuiAreaDisappearPolicyStateField.VertDisappear:
				offset = 8;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDisappearPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiAreaDisappearPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDisappearPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaDisappearPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDisappearPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDisappearPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaDisappearPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDisappearPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDisappearPolicyStateRecord.Size) ||
			!MuiAreaDisappearPolicyStateFieldCursorCodec.TryReadUInt32(
				ref platform, address, MuiAreaDisappearPolicyStateField.Magic,
				out var magic) || magic != MuiAreaDisappearPolicyStateRecord.Cookie ||
			!MuiAreaDisappearPolicyStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiAreaDisappearPolicyStateField.HorizDisappear, out var horizontal) ||
			!MuiAreaDisappearPolicyStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiAreaDisappearPolicyStateField.VertDisappear, out var vertical))
			return false;
		value.Magic = magic;
		value.HorizDisappear = unchecked((int)horizontal);
		value.VertDisappear = unchecked((int)vertical);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDisappearPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDisappearPolicyStateRecord.Size) || value.Magic !=
			MuiAreaDisappearPolicyStateRecord.Cookie) return false;
		return MuiAreaDisappearPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiAreaDisappearPolicyStateField.Magic,
			value.Magic) &&
			MuiAreaDisappearPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiAreaDisappearPolicyStateField.HorizDisappear,
				unchecked((uint)value.HorizDisappear)) &&
			MuiAreaDisappearPolicyStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiAreaDisappearPolicyStateField.VertDisappear,
				unchecked((uint)value.VertDisappear));
	}
}

