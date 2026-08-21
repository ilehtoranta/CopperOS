/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Rectangle decorative-bar state. The ULONG flags retain MorphOS semantics
// while construction and drawing consume one named presentation value.
public struct MuiRectanglePresentationState
{
	public uint HorizontalBar;
	public uint VerticalBar;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRectanglePresentationStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D525443u; // 'MRTC'

	internal uint Magic;
	internal uint HorizontalBar;
	internal uint VerticalBar;
}

internal enum MuiRectanglePresentationStateField : byte
{
	Magic,
	HorizontalBar,
	VerticalBar,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRectanglePresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiRectanglePresentationStateField Field;
}

internal static class MuiRectanglePresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiRectanglePresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiRectanglePresentationStateField.Magic => 0,
			MuiRectanglePresentationStateField.HorizontalBar => 4,
			MuiRectanglePresentationStateField.VerticalBar => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRectanglePresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiRectanglePresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectanglePresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiRectanglePresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectanglePresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiRectanglePresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiRectanglePresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiRectanglePresentationStateRecord.Size) ||
			!MuiRectanglePresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiRectanglePresentationStateField.Magic, out var magic) ||
			magic != MuiRectanglePresentationStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiRectanglePresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiRectanglePresentationStateField.HorizontalBar,
			out value.HorizontalBar) &&
			MuiRectanglePresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiRectanglePresentationStateField.VerticalBar,
			out value.VerticalBar);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRectanglePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiRectanglePresentationStateRecord.Size) || value.Magic !=
			MuiRectanglePresentationStateRecord.Cookie) return false;
		return MuiRectanglePresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiRectanglePresentationStateField.Magic, value.Magic) &&
			MuiRectanglePresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiRectanglePresentationStateField.HorizontalBar,
			value.HorizontalBar) &&
			MuiRectanglePresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiRectanglePresentationStateField.VerticalBar,
			value.VerticalBar);
	}
}
