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
	// The command payload is a complete one-ULONG named record. Keep the
	// address-only helper below for compatibility diagnostics, but exchange the
	// production value through the shared packed ULONG codec.
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint command)
		where TPlatform : struct, IMuiGuestMemory
	{
		command = 0;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out command);
	}

	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint command)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, address, command);
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringEditCommandRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringEditCommandRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var command) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		record.Command = command;
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageMemoryCodec.TryGetAddress(ref platform, record,
			MuiGuestUlongStorageField.Value, out address);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringEditCommandRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, address, record.Command);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringEditCommandRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		return TryReadValue(ref platform, address, out record.Command);
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
	private static bool TryResolveFieldIndex(MuiStringEditRecordField field,
		out uint index, out uint fieldSize)
	{
		if (field == MuiStringEditRecordField.Gadget) index = 0;
		else if (field == MuiStringEditRecordField.StringInfo) index = 1;
		else if (field == MuiStringEditRecordField.WorkBuffer) index = 2;
		else if (field == MuiStringEditRecordField.PrevBuffer) index = 3;
		else if (field == MuiStringEditRecordField.Modes) index = 4;
		else if (field == MuiStringEditRecordField.InputEvent) index = 5;
		else if (field == MuiStringEditRecordField.Code) index = 6;
		else if (field == MuiStringEditRecordField.BufferPos) index = 7;
		else if (field == MuiStringEditRecordField.NumChars) index = 8;
		else if (field == MuiStringEditRecordField.Actions) index = 9;
		else if (field == MuiStringEditRecordField.LongInt) index = 10;
		else if (field == MuiStringEditRecordField.GadgetInfo) index = 11;
		else if (field == MuiStringEditRecordField.EditOp) index = 12;
		else
		{
			index = uint.MaxValue;
			fieldSize = 0;
			return false;
		}
		fieldSize = index >= 6 && index <= 8 || index == 12
			? MuiStringEditWorkRecord.WordFieldSize
			: MuiStringEditWorkRecord.LongFieldSize;
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringEditRecordField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringEditRecordFieldCursor);
		cursor.Address = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out fieldSize);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringEditRecordFieldCursor cursor, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index,
			out fieldSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Address,
				MuiStringEditWorkRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			var currentSize = MuiStringEditWorkRecord.LongFieldSize;
			if (current >= 6 && current <= 8 || current == 12)
				currentSize = MuiStringEditWorkRecord.WordFieldSize;
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				currentSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				return true;
			}
		}
		return false;
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
	// The SGWork record is exchanged once as a complete named value. The
	// field/offset adapter above remains a diagnostic compatibility surface;
	// callback and edit paths do not re-read individual numeric positions.
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
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringEditWorkRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadget) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var stringInfo) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var workBuffer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var prevBuffer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Modes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var inputEvent) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var code) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var bufferPos) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var numChars) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out record.Actions) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var longInt) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var gadgetInfo) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var editOp) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		record.Gadget = APTR.FromPointer(gadget);
		record.StringInfo = APTR.FromPointer(stringInfo);
		record.WorkBuffer = APTR.FromPointer(workBuffer);
		record.PrevBuffer = APTR.FromPointer(prevBuffer);
		record.InputEvent = APTR.FromPointer(inputEvent);
		record.Code = code;
		record.BufferPos = unchecked((short)bufferPos);
		record.NumChars = unchecked((short)numChars);
		record.LongInt = unchecked((int)longInt);
		record.GadgetInfo = APTR.FromPointer(gadgetInfo);
		record.EditOp = editOp;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiStringEditWorkRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringEditWorkRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Gadget.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.StringInfo.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.WorkBuffer.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.PrevBuffer.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Modes) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.InputEvent.Raw) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				record.Code) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)record.BufferPos)) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)record.NumChars)) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Actions) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)record.LongInt)) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.GadgetInfo.Raw) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				record.EditOp) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}
}
