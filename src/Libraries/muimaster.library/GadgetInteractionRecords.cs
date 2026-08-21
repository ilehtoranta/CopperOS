/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Gadget interaction state. The ULONG fields retain MorphOS semantics while
// keyboard activation, selected/pressed transitions, and selected-visual policy
// consume one named value.
public struct MuiGadgetInteractionState
{
	public uint InputMode;
	public uint Selected;
	public uint Pressed;
	public uint ShowSelState;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGadgetInteractionStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x4D474454u; // 'MGDT'

	internal uint Magic;
	internal uint InputMode;
	internal uint Selected;
	internal uint Pressed;
	internal uint ShowSelState;
}

internal enum MuiGadgetInteractionStateField : byte
{
	Magic,
	InputMode,
	Selected,
	Pressed,
	ShowSelState,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGadgetInteractionStateFieldCursor
{
	internal APTR Record;
	internal MuiGadgetInteractionStateField Field;
}

internal static class MuiGadgetInteractionStateFieldCursorCodec
{
	private static bool TryResolve(MuiGadgetInteractionStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiGadgetInteractionStateField.Magic => 0,
			MuiGadgetInteractionStateField.InputMode => 4,
			MuiGadgetInteractionStateField.Selected => 8,
			MuiGadgetInteractionStateField.Pressed => 12,
			MuiGadgetInteractionStateField.ShowSelState => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGadgetInteractionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiGadgetInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGadgetInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGadgetInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGadgetInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGadgetInteractionStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGadgetInteractionStateRecord.Size) ||
			!MuiGadgetInteractionStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGadgetInteractionStateField.Magic, out var magic) ||
			magic != MuiGadgetInteractionStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiGadgetInteractionStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGadgetInteractionStateField.InputMode, out value.InputMode) &&
			MuiGadgetInteractionStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGadgetInteractionStateField.Selected, out value.Selected) &&
			MuiGadgetInteractionStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGadgetInteractionStateField.Pressed, out value.Pressed) &&
			MuiGadgetInteractionStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGadgetInteractionStateField.ShowSelState,
			out value.ShowSelState);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGadgetInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGadgetInteractionStateRecord.Size) || value.Magic !=
			MuiGadgetInteractionStateRecord.Cookie) return false;
		return MuiGadgetInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGadgetInteractionStateField.Magic, value.Magic) &&
			MuiGadgetInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGadgetInteractionStateField.InputMode, value.InputMode) &&
			MuiGadgetInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGadgetInteractionStateField.Selected, value.Selected) &&
			MuiGadgetInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGadgetInteractionStateField.Pressed, value.Pressed) &&
			MuiGadgetInteractionStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiGadgetInteractionStateField.ShowSelState,
			value.ShowSelState);
	}
}
