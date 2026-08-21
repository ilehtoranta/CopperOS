/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Caller-owned String.mui edit-hook state. The Hook pointer is a guest
// struct Hook and LonelyEditHook is a MorphOS BOOL; keeping them together
// avoids positional fields in a private String instance record.
public struct MuiStringEditHookState
{
	public APTR EditHook;
	public uint LonelyEditHook;
}

// Fixed guest SGWork record passed to MUIA_String_EditHook. The fields mirror
// intuition/sghooks.h in guest order; only the callback codec knows the wire
// layout, while consumers use named fields.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringEditWorkRecord
{
	internal const uint Size = 44;
	internal APTR Gadget;
	internal APTR StringInfo;
	internal APTR WorkBuffer;
	internal APTR PrevBuffer;
	internal uint Modes;
	internal APTR InputEvent;
	internal ushort Code;
	internal short BufferPos;
	internal short NumChars;
	internal uint Actions;
	internal int LongInt;
	internal APTR GadgetInfo;
	internal ushort EditOp;
}

internal enum MuiStringEditRecordField : byte
{
	Gadget,
	StringInfo,
	WorkBuffer,
	PrevBuffer,
	Modes,
	InputEvent,
	Code,
	BufferPos,
	NumChars,
	Actions,
	LongInt,
	GadgetInfo,
	EditOp,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringEditRecordFieldCursor
{
	internal APTR Address;
	internal MuiStringEditRecordField Field;
}

internal static class MuiStringEditRecordFieldCursorCodec
{
	private static bool TryResolve(MuiStringEditRecordField field,
		out uint offset, out uint fieldSize)
	{
		offset = field switch
		{
			MuiStringEditRecordField.Gadget => 0,
			MuiStringEditRecordField.StringInfo => 4,
			MuiStringEditRecordField.WorkBuffer => 8,
			MuiStringEditRecordField.PrevBuffer => 12,
			MuiStringEditRecordField.Modes => 16,
			MuiStringEditRecordField.InputEvent => 20,
			MuiStringEditRecordField.Code => 24,
			MuiStringEditRecordField.BufferPos => 26,
			MuiStringEditRecordField.NumChars => 28,
			MuiStringEditRecordField.Actions => 30,
			MuiStringEditRecordField.LongInt => 34,
			MuiStringEditRecordField.GadgetInfo => 38,
			MuiStringEditRecordField.EditOp => 42,
			_ => uint.MaxValue,
		};
		fieldSize = field == MuiStringEditRecordField.Code ||
			field == MuiStringEditRecordField.BufferPos ||
			field == MuiStringEditRecordField.NumChars ||
			field == MuiStringEditRecordField.EditOp ? 2u : 4u;
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringEditRecordFieldCursor cursor, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(cursor.Field, out var offset, out fieldSize) ||
			cursor.Address.IsNull || cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiStringEditWorkRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringEditRecordFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringEditRecordFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringEditRecordFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 2) return false;
		value = platform.ReadUInt16(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringEditRecordFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress,
			out var fieldSize) || fieldSize != 2) return false;
		platform.WriteUInt16(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiStringEditWorkCodec
{
	internal const uint CommandKey = 1;
	internal const uint ActionUse = 0x00000001;
	internal const uint ActionEnd = 0x00000002;
	internal const uint ActionBeep = 0x00000004;
	internal const uint ActionReuse = 0x00000008;
	internal const uint ActionRedisplay = 0x00000010;
	internal const uint ActionNextActive = 0x00000020;
	internal const uint ActionPreviousActive = 0x00000040;

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringEditWorkRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringEditWorkRecord.Size)) return false;
		if (!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringEditRecordField.Gadget, out var gadget) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.StringInfo,
				out var stringInfo) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.WorkBuffer,
				out var workBuffer) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.PrevBuffer,
				out var prevBuffer) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.Modes, out record.Modes) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.InputEvent,
				out var inputEvent) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt16(ref platform,
				address, MuiStringEditRecordField.Code, out record.Code) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt16(ref platform,
				address, MuiStringEditRecordField.BufferPos,
				out var bufferPos) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt16(ref platform,
				address, MuiStringEditRecordField.NumChars,
				out var numChars) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.Actions, out record.Actions) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.LongInt, out var longInt) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.GadgetInfo,
				out var gadgetInfo) ||
			!MuiStringEditRecordFieldCursorCodec.TryReadUInt16(ref platform,
				address, MuiStringEditRecordField.EditOp, out record.EditOp))
			return false;
		record.Gadget = APTR.FromPointer(gadget);
		record.StringInfo = APTR.FromPointer(stringInfo);
		record.WorkBuffer = APTR.FromPointer(workBuffer);
		record.PrevBuffer = APTR.FromPointer(prevBuffer);
		record.InputEvent = APTR.FromPointer(inputEvent);
		record.BufferPos = unchecked((short)bufferPos);
		record.NumChars = unchecked((short)numChars);
		record.LongInt = unchecked((int)longInt);
		record.GadgetInfo = APTR.FromPointer(gadgetInfo);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditWorkRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringEditWorkRecord.Size)) return false;
		return MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringEditRecordField.Gadget, record.Gadget.Raw) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.StringInfo,
				record.StringInfo.Raw) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.WorkBuffer,
				record.WorkBuffer.Raw) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.PrevBuffer,
				record.PrevBuffer.Raw) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.Modes, record.Modes) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.InputEvent,
				record.InputEvent.Raw) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt16(ref platform,
				address, MuiStringEditRecordField.Code, record.Code) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt16(ref platform,
				address, MuiStringEditRecordField.BufferPos,
				unchecked((ushort)record.BufferPos)) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt16(ref platform,
				address, MuiStringEditRecordField.NumChars,
				unchecked((ushort)record.NumChars)) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.Actions, record.Actions) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.LongInt,
				unchecked((uint)record.LongInt)) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.GadgetInfo,
				record.GadgetInfo.Raw) &&
			MuiStringEditRecordFieldCursorCodec.TryWriteUInt16(ref platform,
				address, MuiStringEditRecordField.EditOp, record.EditOp);
	}
}
