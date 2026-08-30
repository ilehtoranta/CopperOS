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
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiStringEditHookState
{
	public APTR EditHook;
	public uint LonelyEditHook;
}

// Named platform request for SGWork's beep action. The provider resolves the
// owning screen/window from the MUI object; the guest hook never receives a
// host UI object or a managed callback. InputEvent remains the caller-owned
// Intuition message supplied to the edit hook.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiStringEditBeepRequest
{
	public APTR Object;
	public APTR InputEvent;
	public uint Accepted;
}

// Named platform request for SGWork's SGA_REUSE action. Reuse is valid only
// together with SGA_END; a provider may consume the caller-owned event after
// the current gadget has been deactivated and any next/previous focus
// selection has been applied. When it declines, the core queues the event in
// the owning Window's named guest record. Window and MuiKey are named context
// from the current MUI dispatch, allowing either path without offset guesses.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiStringEditReuseRequest
{
	public APTR Object;
	public APTR Window;
	public APTR InputEvent;
	public int MuiKey;
	public ushort Code;
	public uint Accepted;
}

// Fixed ULONG command payload passed as A1 to a String.mui edit hook. The
// numeric wire position is confined to this bounded adapter; callback code
// exchanges the named command record instead of a scalar offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringEditCommandRecord
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint CommandOffset = 0;
	internal uint Command;
}

internal static class MuiStringEditCommandCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || record.Raw > uint.MaxValue -
			MuiStringEditCommandRecord.CommandOffset || !platform.IsMapped(record,
			MuiStringEditCommandRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw +
			MuiStringEditCommandRecord.CommandOffset);
		return platform.IsMapped(address, MuiStringEditCommandRecord.FieldSize);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringEditCommandRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress,
			unchecked((int)MuiStringEditCommandRecord.CommandOffset),
			record.Command);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringEditCommandRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!TryGetAddress(ref platform, address, out var fieldAddress))
			return false;
		record.Command = platform.ReadUInt32(fieldAddress,
			unchecked((int)MuiStringEditCommandRecord.CommandOffset));
		return true;
	}
}

// Fixed guest SGWork record passed to MUIA_String_EditHook. The fields mirror
// intuition/sghooks.h in guest order; only the callback codec knows the wire
// layout, while consumers use named fields. BufferPos and NumChars are logical
// character counts (including in Unicode mode); WorkBuffer itself remains a
// guest C string whose UTF-8 byte length is a separate concern.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringEditWorkRecord
{
	internal const uint Size = 44;
	internal const uint LongFieldSize = 4;
	internal const uint WordFieldSize = 2;
	internal const uint GadgetOffset = 0;
	internal const uint StringInfoOffset = 4;
	internal const uint WorkBufferOffset = 8;
	internal const uint PrevBufferOffset = 12;
	internal const uint ModesOffset = 16;
	internal const uint InputEventOffset = 20;
	internal const uint CodeOffset = 24;
	internal const uint BufferPosOffset = 26;
	internal const uint NumCharsOffset = 28;
	internal const uint ActionsOffset = 30;
	internal const uint LongIntOffset = 34;
	internal const uint GadgetInfoOffset = 38;
	internal const uint EditOpOffset = 42;
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

// Struct-first guest-memory adapter for the fixed Intuition SGWork record.
// Named field sizes/positions live with the record; this bounded seam admits
// the complete record before exposing a field address.
internal static class MuiStringEditWorkRecordMemoryCodec
{
	private static bool TryResolve(MuiStringEditRecordField field,
		out uint offset, out uint fieldSize)
	{
		offset = field switch
		{
			MuiStringEditRecordField.Gadget => MuiStringEditWorkRecord.GadgetOffset,
			MuiStringEditRecordField.StringInfo => MuiStringEditWorkRecord.StringInfoOffset,
			MuiStringEditRecordField.WorkBuffer => MuiStringEditWorkRecord.WorkBufferOffset,
			MuiStringEditRecordField.PrevBuffer => MuiStringEditWorkRecord.PrevBufferOffset,
			MuiStringEditRecordField.Modes => MuiStringEditWorkRecord.ModesOffset,
			MuiStringEditRecordField.InputEvent => MuiStringEditWorkRecord.InputEventOffset,
			MuiStringEditRecordField.Code => MuiStringEditWorkRecord.CodeOffset,
			MuiStringEditRecordField.BufferPos => MuiStringEditWorkRecord.BufferPosOffset,
			MuiStringEditRecordField.NumChars => MuiStringEditWorkRecord.NumCharsOffset,
			MuiStringEditRecordField.Actions => MuiStringEditWorkRecord.ActionsOffset,
			MuiStringEditRecordField.LongInt => MuiStringEditWorkRecord.LongIntOffset,
			MuiStringEditRecordField.GadgetInfo => MuiStringEditWorkRecord.GadgetInfoOffset,
			MuiStringEditRecordField.EditOp => MuiStringEditWorkRecord.EditOpOffset,
			_ => uint.MaxValue,
		};
		fieldSize = field == MuiStringEditRecordField.Code ||
			field == MuiStringEditRecordField.BufferPos ||
			field == MuiStringEditRecordField.NumChars ||
			field == MuiStringEditRecordField.EditOp ?
			MuiStringEditWorkRecord.WordFieldSize :
			MuiStringEditWorkRecord.LongFieldSize;
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringEditRecordField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(field, out var offset, out fieldSize) ||
			record.IsNull || record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiStringEditWorkRecord.Size))
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, field, out var fieldAddress,
			out var fieldSize) || fieldSize != MuiStringEditWorkRecord.LongFieldSize)
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, field, out var fieldAddress,
			out var fieldSize) || fieldSize != MuiStringEditWorkRecord.LongFieldSize)
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, field, out var fieldAddress,
			out var fieldSize) || fieldSize != MuiStringEditWorkRecord.WordFieldSize)
			return false;
		value = platform.ReadUInt16(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, field, out var fieldAddress,
			out var fieldSize) || fieldSize != MuiStringEditWorkRecord.WordFieldSize)
			return false;
		platform.WriteUInt16(fieldAddress, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for existing typed cursor diagnostics.
internal static class MuiStringEditRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringEditRecordFieldCursor cursor, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory =>
		MuiStringEditWorkRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform, address,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			field, value);

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStringEditWorkRecordMemoryCodec.TryReadUInt16(ref platform, address,
			field, out value);

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditRecordField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStringEditWorkRecordMemoryCodec.TryWriteUInt16(ref platform, address,
			field, value);
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
		if (!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiStringEditRecordField.Gadget, out var gadget) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.StringInfo,
				out var stringInfo) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.WorkBuffer,
				out var workBuffer) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.PrevBuffer,
				out var prevBuffer) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.Modes, out record.Modes) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.InputEvent,
				out var inputEvent) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt16(ref platform,
				address, MuiStringEditRecordField.Code, out record.Code) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt16(ref platform,
				address, MuiStringEditRecordField.BufferPos,
				out var bufferPos) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt16(ref platform,
				address, MuiStringEditRecordField.NumChars,
				out var numChars) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.Actions, out record.Actions) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.LongInt, out var longInt) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiStringEditRecordField.GadgetInfo,
				out var gadgetInfo) ||
				!MuiStringEditWorkRecordMemoryCodec.TryReadUInt16(ref platform,
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
		return MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiStringEditRecordField.Gadget, record.Gadget.Raw) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.StringInfo,
				record.StringInfo.Raw) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.WorkBuffer,
				record.WorkBuffer.Raw) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.PrevBuffer,
				record.PrevBuffer.Raw) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.Modes, record.Modes) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.InputEvent,
				record.InputEvent.Raw) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt16(ref platform,
				address, MuiStringEditRecordField.Code, record.Code) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt16(ref platform,
				address, MuiStringEditRecordField.BufferPos,
				unchecked((ushort)record.BufferPos)) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt16(ref platform,
				address, MuiStringEditRecordField.NumChars,
				unchecked((ushort)record.NumChars)) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.Actions, record.Actions) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.LongInt,
				unchecked((uint)record.LongInt)) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiStringEditRecordField.GadgetInfo,
				record.GadgetInfo.Raw) &&
			MuiStringEditWorkRecordMemoryCodec.TryWriteUInt16(ref platform,
				address, MuiStringEditRecordField.EditOp, record.EditOp);
	}
}
