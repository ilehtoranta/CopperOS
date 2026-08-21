/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The six public Area geometry values are kept together as one semantic
// record. Signed coordinates remain explicit while guest storage preserves
// the original 32-bit ULONG representation.
public struct MuiAreaGeometryState
{
	public int Left;
	public int Top;
	public int Width;
	public int Height;
	public int Right;
	public int Bottom;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaGeometryStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x4D414745u; // 'MAGE'

	internal uint Magic;
	internal int Left;
	internal int Top;
	internal int Width;
	internal int Height;
	internal int Right;
	internal int Bottom;
}

internal enum MuiAreaGeometryStateField : byte
{
	Magic,
	Left,
	Top,
	Width,
	Height,
	Right,
	Bottom,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaGeometryStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaGeometryStateField Field;
}

internal static class MuiAreaGeometryStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaGeometryStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaGeometryStateField.Magic => 0,
			MuiAreaGeometryStateField.Left => 4,
			MuiAreaGeometryStateField.Top => 8,
			MuiAreaGeometryStateField.Width => 12,
			MuiAreaGeometryStateField.Height => 16,
			MuiAreaGeometryStateField.Right => 20,
			MuiAreaGeometryStateField.Bottom => 24,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaGeometryStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiAreaGeometryStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaGeometryStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw)) return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaGeometryStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));
}

internal static class MuiAreaGeometryStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaGeometryStateRecord.Size) ||
			!MuiAreaGeometryStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaGeometryStateField.Magic, out var magic) ||
			magic != MuiAreaGeometryStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiAreaGeometryStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiAreaGeometryStateField.Left, out value.Left) &&
			MuiAreaGeometryStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiAreaGeometryStateField.Top, out value.Top) &&
			MuiAreaGeometryStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiAreaGeometryStateField.Width, out value.Width) &&
			MuiAreaGeometryStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiAreaGeometryStateField.Height, out value.Height) &&
			MuiAreaGeometryStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiAreaGeometryStateField.Right, out value.Right) &&
			MuiAreaGeometryStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiAreaGeometryStateField.Bottom, out value.Bottom);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaGeometryStateRecord.Size) || value.Magic !=
			MuiAreaGeometryStateRecord.Cookie) return false;
		return MuiAreaGeometryStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaGeometryStateField.Magic, value.Magic) &&
			MuiAreaGeometryStateFieldCursorCodec.TryWriteInt32(ref platform, address,
				MuiAreaGeometryStateField.Left, value.Left) &&
			MuiAreaGeometryStateFieldCursorCodec.TryWriteInt32(ref platform, address,
				MuiAreaGeometryStateField.Top, value.Top) &&
			MuiAreaGeometryStateFieldCursorCodec.TryWriteInt32(ref platform, address,
				MuiAreaGeometryStateField.Width, value.Width) &&
			MuiAreaGeometryStateFieldCursorCodec.TryWriteInt32(ref platform, address,
				MuiAreaGeometryStateField.Height, value.Height) &&
			MuiAreaGeometryStateFieldCursorCodec.TryWriteInt32(ref platform, address,
				MuiAreaGeometryStateField.Right, value.Right) &&
			MuiAreaGeometryStateFieldCursorCodec.TryWriteInt32(ref platform, address,
				MuiAreaGeometryStateField.Bottom, value.Bottom);
	}
}
