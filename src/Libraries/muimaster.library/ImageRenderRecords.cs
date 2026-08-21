/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Image selection, selected-visual, and free-axis policy shared by layout,
// drawing, and input.
// These values remain ULONG-compatible with MorphOS while callers consume one
// named semantic state instead of separate private widget slots.
public struct MuiImageRenderState
{
	public uint ImageState;
	public uint Selected;
	public uint FreeHoriz;
	public uint FreeVert;
	public uint ShowSelState;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageRenderStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x4D495253u; // 'MIRS'

	internal uint Magic;
	internal uint ImageState;
	internal uint Selected;
	internal uint FreeHoriz;
	internal uint FreeVert;
	internal uint ShowSelState;
}

internal enum MuiImageRenderStateField : byte
{
	Magic,
	ImageState,
	Selected,
	FreeHoriz,
	FreeVert,
	ShowSelState,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageRenderStateFieldCursor
{
	internal APTR Record;
	internal MuiImageRenderStateField Field;
}

internal static class MuiImageRenderStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageRenderStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiImageRenderStateField.Magic => 0,
			MuiImageRenderStateField.ImageState => 4,
			MuiImageRenderStateField.Selected => 8,
			MuiImageRenderStateField.FreeHoriz => 12,
			MuiImageRenderStateField.FreeVert => 16,
			MuiImageRenderStateField.ShowSelState => 20,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageRenderStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiImageRenderStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageRenderStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageRenderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageRenderStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageRenderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiImageRenderStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageRenderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageRenderStateRecord.Size) ||
			!MuiImageRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiImageRenderStateField.Magic, out var magic) ||
			magic != MuiImageRenderStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiImageRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageRenderStateField.ImageState, out value.ImageState) ||
			!MuiImageRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageRenderStateField.Selected, out value.Selected) ||
			!MuiImageRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageRenderStateField.FreeHoriz, out value.FreeHoriz) ||
			!MuiImageRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageRenderStateField.FreeVert, out value.FreeVert))
			return false;
		return MuiImageRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageRenderStateField.ShowSelState,
			out value.ShowSelState);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageRenderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageRenderStateRecord.Size) || value.Magic !=
			MuiImageRenderStateRecord.Cookie) return false;
		return MuiImageRenderStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiImageRenderStateField.Magic, value.Magic) &&
			MuiImageRenderStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageRenderStateField.ImageState, value.ImageState) &&
			MuiImageRenderStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageRenderStateField.Selected, value.Selected) &&
			MuiImageRenderStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageRenderStateField.FreeHoriz, value.FreeHoriz) &&
			MuiImageRenderStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageRenderStateField.FreeVert, value.FreeVert) &&
			MuiImageRenderStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageRenderStateField.ShowSelState, value.ShowSelState);
	}
}
