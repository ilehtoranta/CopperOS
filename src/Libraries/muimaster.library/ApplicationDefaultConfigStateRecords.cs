/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// DefaultConfigItem result state. The platform supplies the value; the guest
// record retains the requested ID, accepted value, and saturating request
// counter without introducing a managed configuration store.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationDefaultConfigStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x41444354u; // 'ADCT'

	internal uint Magic;
	internal uint ConfigId;
	internal uint Value;
	internal uint Requests;
}

internal enum MuiApplicationDefaultConfigStateField : byte
{
	Magic,
	ConfigId,
	Value,
	Requests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationDefaultConfigStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationDefaultConfigStateField Field;
}

internal static class MuiApplicationDefaultConfigStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationDefaultConfigStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationDefaultConfigStateField.Magic:
			case MuiApplicationDefaultConfigStateField.ConfigId:
			case MuiApplicationDefaultConfigStateField.Value:
			case MuiApplicationDefaultConfigStateField.Requests:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationDefaultConfigStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationDefaultConfigStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationDefaultConfigStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationDefaultConfigStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationDefaultConfigStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationDefaultConfigStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationDefaultConfigStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationDefaultConfigStateRecord.Size) ||
			!MuiApplicationDefaultConfigStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationDefaultConfigStateField.Magic, out var magic) ||
			magic != MuiApplicationDefaultConfigStateRecord.Cookie ||
			!MuiApplicationDefaultConfigStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationDefaultConfigStateField.ConfigId,
				out value.ConfigId) ||
			!MuiApplicationDefaultConfigStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationDefaultConfigStateField.Value, out value.Value) ||
			!MuiApplicationDefaultConfigStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationDefaultConfigStateField.Requests,
				out value.Requests)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationDefaultConfigStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationDefaultConfigStateRecord.Size) || value.Magic !=
			MuiApplicationDefaultConfigStateRecord.Cookie) return false;
		return MuiApplicationDefaultConfigStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationDefaultConfigStateField.Magic, value.Magic) &&
			MuiApplicationDefaultConfigStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationDefaultConfigStateField.ConfigId, value.ConfigId) &&
			MuiApplicationDefaultConfigStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationDefaultConfigStateField.Value, value.Value) &&
			MuiApplicationDefaultConfigStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationDefaultConfigStateField.Requests, value.Requests);
	}
}
