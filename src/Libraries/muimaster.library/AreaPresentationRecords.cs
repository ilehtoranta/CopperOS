/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Area-derived presentation policy. The values retain MorphOS ULONG
// semantics while common-control input, sizing, and drawing consume one named
// guest-resident record.
public struct MuiAreaPresentationState
{
	public uint Disabled;
	public uint ShowMe;
	public uint Background;
	public uint Frame;
	public uint CustomBackfill;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaPresentationStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x4D415052u; // 'MAPR'

	internal uint Magic;
	internal uint Disabled;
	internal uint ShowMe;
	internal uint Background;
	internal uint Frame;
	internal uint CustomBackfill;
}

internal enum MuiAreaPresentationStateField : byte
{
	Magic,
	Disabled,
	ShowMe,
	Background,
	Frame,
	CustomBackfill,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaPresentationStateField Field;
}

internal static class MuiAreaPresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaPresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaPresentationStateField.Magic => 0,
			MuiAreaPresentationStateField.Disabled => 4,
			MuiAreaPresentationStateField.ShowMe => 8,
			MuiAreaPresentationStateField.Background => 12,
			MuiAreaPresentationStateField.Frame => 16,
			MuiAreaPresentationStateField.CustomBackfill => 20,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiAreaPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaPresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaPresentationStateRecord.Size) ||
			!MuiAreaPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaPresentationStateField.Magic, out var magic) ||
			magic != MuiAreaPresentationStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiAreaPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.Disabled, out value.Disabled) &&
			MuiAreaPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.ShowMe, out value.ShowMe) &&
			MuiAreaPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.Background,
			out value.Background) &&
			MuiAreaPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.Frame, out value.Frame) &&
			MuiAreaPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiAreaPresentationStateField.CustomBackfill,
			out value.CustomBackfill);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaPresentationStateRecord.Size) || value.Magic !=
			MuiAreaPresentationStateRecord.Cookie) return false;
		return MuiAreaPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.Magic, value.Magic) &&
			MuiAreaPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.Disabled, value.Disabled) &&
			MuiAreaPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.ShowMe, value.ShowMe) &&
			MuiAreaPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.Background, value.Background) &&
			MuiAreaPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.Frame, value.Frame) &&
			MuiAreaPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaPresentationStateField.CustomBackfill,
			value.CustomBackfill);
	}
}
