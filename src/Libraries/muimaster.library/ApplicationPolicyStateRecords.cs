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
	internal const uint Cookie = 0x41504F4Cu; // 'APOL'

	internal uint Magic;
	internal uint UseRexx;
	internal uint UseCommodities;
	internal uint UseScreenNotify;
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
	private static bool TryResolve(MuiApplicationPolicyStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationPolicyStateField.Magic:
			case MuiApplicationPolicyStateField.UseRexx:
			case MuiApplicationPolicyStateField.UseCommodities:
			case MuiApplicationPolicyStateField.UseScreenNotify:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationPolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationPolicyStateRecord.Size) ||
			!MuiApplicationPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationPolicyStateField.Magic, out var magic) ||
			magic != MuiApplicationPolicyStateRecord.Cookie ||
			!MuiApplicationPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationPolicyStateField.UseRexx, out value.UseRexx) ||
			!MuiApplicationPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationPolicyStateField.UseCommodities,
				out value.UseCommodities) ||
			!MuiApplicationPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationPolicyStateField.UseScreenNotify,
				out value.UseScreenNotify)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationPolicyStateRecord.Size) || value.Magic !=
			MuiApplicationPolicyStateRecord.Cookie) return false;
		return MuiApplicationPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationPolicyStateField.Magic,
			value.Magic) &&
			MuiApplicationPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationPolicyStateField.UseRexx,
			value.UseRexx) &&
			MuiApplicationPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationPolicyStateField.UseCommodities,
			value.UseCommodities) &&
			MuiApplicationPolicyStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationPolicyStateField.UseScreenNotify,
			value.UseScreenNotify);
	}
}
