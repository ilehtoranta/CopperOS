/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Scrollbar group geometry state.  The fields retain MorphOS ULONG
// semantics while child construction, layout, and drawing consume one named
// value instead of repeatedly decoding the Group/Scrollbar attributes.
public struct MuiScrollbarLayoutState
{
	public uint Horizontal;
	public uint Type;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollbarLayoutStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D534C59u; // 'MSLY'

	internal uint Magic;
	internal uint Horizontal;
	internal uint Type;
}

internal enum MuiScrollbarLayoutStateField : byte
{
	Magic,
	Horizontal,
	Type,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollbarLayoutStateFieldCursor
{
	internal APTR Record;
	internal MuiScrollbarLayoutStateField Field;
}

internal static class MuiScrollbarLayoutStateFieldCursorCodec
{
	private static bool TryResolve(MuiScrollbarLayoutStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiScrollbarLayoutStateField.Magic => 0,
			MuiScrollbarLayoutStateField.Horizontal => 4,
			MuiScrollbarLayoutStateField.Type => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollbarLayoutStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiScrollbarLayoutStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiScrollbarLayoutStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollbarLayoutStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScrollbarLayoutStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScrollbarLayoutStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollbarLayoutStateRecord.Size) ||
			!MuiScrollbarLayoutStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollbarLayoutStateField.Magic, out var magic) ||
			magic != MuiScrollbarLayoutStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiScrollbarLayoutStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiScrollbarLayoutStateField.Horizontal, out value.Horizontal) &&
			MuiScrollbarLayoutStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiScrollbarLayoutStateField.Type, out value.Type);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollbarLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollbarLayoutStateRecord.Size) || value.Magic !=
			MuiScrollbarLayoutStateRecord.Cookie) return false;
		return MuiScrollbarLayoutStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiScrollbarLayoutStateField.Magic, value.Magic) &&
			MuiScrollbarLayoutStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiScrollbarLayoutStateField.Horizontal, value.Horizontal) &&
			MuiScrollbarLayoutStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiScrollbarLayoutStateField.Type, value.Type);
	}
}
