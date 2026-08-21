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
	internal const uint Cookie = 0x41574754u; // 'AWGT'

	internal uint Magic;
	internal uint Weight;
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
	private static bool TryResolve(MuiAreaWeightStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaWeightStateField.Magic => 0,
			MuiAreaWeightStateField.Weight => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaWeightStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiAreaWeightStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaWeightStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaWeightStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaWeightStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaWeightStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaWeightStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaWeightStateRecord.Size) ||
			!MuiAreaWeightStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaWeightStateField.Magic, out var magic) ||
			magic != MuiAreaWeightStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiAreaWeightStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAreaWeightStateField.Weight, out value.Weight);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaWeightStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaWeightStateRecord.Size) || value.Magic !=
			MuiAreaWeightStateRecord.Cookie) return false;
		return MuiAreaWeightStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaWeightStateField.Magic, value.Magic) &&
			MuiAreaWeightStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiAreaWeightStateField.Weight, value.Weight);
	}
}
