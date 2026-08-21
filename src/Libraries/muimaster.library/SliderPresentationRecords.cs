/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Slider-only presentation state. Numeric values remain in
// MuiNumericState; this record carries the orientation and quiet-display
// policy that are consumed together by Slider layout and drawing.
public struct MuiSliderPresentationState
{
	public uint Horizontal;
	public uint Quiet;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSliderPresentationStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D534C44u; // 'MSLD'

	internal uint Magic;
	internal uint Horizontal;
	internal uint Quiet;
}

internal enum MuiSliderPresentationStateField : byte
{
	Magic,
	Horizontal,
	Quiet,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSliderPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiSliderPresentationStateField Field;
}

internal static class MuiSliderPresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiSliderPresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiSliderPresentationStateField.Magic => 0,
			MuiSliderPresentationStateField.Horizontal => 4,
			MuiSliderPresentationStateField.Quiet => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSliderPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiSliderPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiSliderPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSliderPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiSliderPresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiSliderPresentationStateRecord.Size) ||
			!MuiSliderPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiSliderPresentationStateField.Magic, out var magic) ||
			magic != MuiSliderPresentationStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiSliderPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiSliderPresentationStateField.Horizontal, out value.Horizontal) &&
			MuiSliderPresentationStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiSliderPresentationStateField.Quiet, out value.Quiet);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiSliderPresentationStateRecord.Size) || value.Magic !=
			MuiSliderPresentationStateRecord.Cookie) return false;
		return MuiSliderPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiSliderPresentationStateField.Magic, value.Magic) &&
			MuiSliderPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiSliderPresentationStateField.Horizontal, value.Horizontal) &&
			MuiSliderPresentationStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiSliderPresentationStateField.Quiet, value.Quiet);
	}
}
