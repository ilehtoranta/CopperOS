/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// OpenConfigWindow request state. Flags retain the MorphOS ULONG payload while
// ClassId remains a caller-owned validated guest C-string pointer.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationConfigWindowStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x41435754u; // 'ACWT'

	internal uint Magic;
	internal uint Flags;
	internal APTR ClassId;
	internal uint Requests;
}

internal enum MuiApplicationConfigWindowStateField : byte
{
	Magic,
	Flags,
	ClassId,
	Requests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationConfigWindowStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationConfigWindowStateField Field;
}

internal static class MuiApplicationConfigWindowStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationConfigWindowStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationConfigWindowStateField.Magic:
			case MuiApplicationConfigWindowStateField.Flags:
			case MuiApplicationConfigWindowStateField.ClassId:
			case MuiApplicationConfigWindowStateField.Requests:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationConfigWindowStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationConfigWindowStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationConfigWindowStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationConfigWindowStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationConfigWindowStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationConfigWindowStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationConfigWindowStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationConfigWindowStateRecord.Size) ||
			!MuiApplicationConfigWindowStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationConfigWindowStateField.Magic, out var magic) ||
			magic != MuiApplicationConfigWindowStateRecord.Cookie ||
			!MuiApplicationConfigWindowStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationConfigWindowStateField.Flags, out value.Flags) ||
			!MuiApplicationConfigWindowStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationConfigWindowStateField.ClassId, out var classId) ||
			!MuiApplicationConfigWindowStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationConfigWindowStateField.Requests,
				out value.Requests)) return false;
		value.Magic = magic;
		value.ClassId = APTR.FromPointer(classId);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationConfigWindowStateRecord.Size) || value.Magic !=
			MuiApplicationConfigWindowStateRecord.Cookie) return false;
		return MuiApplicationConfigWindowStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationConfigWindowStateField.Magic, value.Magic) &&
			MuiApplicationConfigWindowStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationConfigWindowStateField.Flags, value.Flags) &&
			MuiApplicationConfigWindowStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationConfigWindowStateField.ClassId, value.ClassId.Raw) &&
			MuiApplicationConfigWindowStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationConfigWindowStateField.Requests, value.Requests);
	}
}
