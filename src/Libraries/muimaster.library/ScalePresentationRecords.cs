/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Scale-only presentation state. The ULONG orientation flag remains
// MorphOS-compatible while construction, runtime mutation, and drawing use a
// named value rather than a repeated scalar lookup.
public struct MuiScalePresentationState
{
	public uint Horizontal;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScalePresentationStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D53434Cu; // 'MSCL'

	internal uint Magic;
	internal uint Horizontal;
}

internal enum MuiScalePresentationStateField : byte
{
	Magic,
	Horizontal,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScalePresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiScalePresentationStateField Field;
}

internal static class MuiScalePresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiScalePresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiScalePresentationStateField.Magic => 0,
			MuiScalePresentationStateField.Horizontal => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScalePresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiScalePresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiScalePresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScalePresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScalePresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScalePresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiScalePresentationStateRecord.Size) ||
			!MuiScalePresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScalePresentationStateField.Magic, out var magic) ||
			magic != MuiScalePresentationStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiScalePresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiScalePresentationStateField.Horizontal,
			out value.Horizontal);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScalePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiScalePresentationStateRecord.Size) || value.Magic !=
			MuiScalePresentationStateRecord.Cookie) return false;
		return MuiScalePresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiScalePresentationStateField.Magic, value.Magic) &&
			MuiScalePresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiScalePresentationStateField.Horizontal,
			value.Horizontal);
	}
}
