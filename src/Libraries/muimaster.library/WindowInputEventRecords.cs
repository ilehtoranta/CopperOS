/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The Intuition InputEvent is a fixed-size packed guest record. Keep the
// public value typed as Amiga.InputEvent; only this bounded adapter knows the
// packed guest representation required to cross the memory interface. MUI
// window state stores the validated pointer, never a managed copy of this
// value.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiWindowInputEventRecord
{
	internal const uint Size = 22;

	internal APTR NextEvent;
	internal byte Class;
	internal byte SubClass;
	internal ushort Code;
	internal ushort Qualifier;
	internal int Position;
	internal uint Seconds;
	internal uint Microseconds;
}

// IDCMP_RAWKEY's IntuiMessage.IAddress points to a guest APTR containing the
// dead-key event-address value. Keep that indirection named and bounded rather
// than treating the message field as an InputEvent position directly.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowInputEventAddressRecord
{
	internal const uint Size = 4;
	internal APTR Address;
}

internal static class MuiWindowInputEventAddressCodec
{
	internal static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MuiWindowInputEventAddressRecord value)
		where TMemory : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiWindowInputEventAddressRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var eventAddress) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Address = APTR.FromPointer(eventAddress);
		return true;
	}

	internal static bool Write<TMemory>(ref TMemory memory, APTR address,
		MuiWindowInputEventAddressRecord value)
		where TMemory : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address,
			MuiWindowInputEventAddressRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.Address.Raw) && MuiGuestStructCursor.IsComplete(cursor);
}

internal enum MuiWindowInputEventField : byte
{
	NextEvent,
	Class,
	SubClass,
	Code,
	Qualifier,
	Position,
	Seconds,
	Microseconds,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowInputEventFieldCursor
{
	internal APTR Record;
	internal MuiWindowInputEventField Field;
}

internal static class MuiWindowInputEventMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiWindowInputEventField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		var selected = (uint)field;
		if (selected > (uint)MuiWindowInputEventField.Microseconds) return false;
		for (var index = 0u; index <= selected; index++)
		{
			var size = 4u;
			if (index == (uint)MuiWindowInputEventField.Class ||
				index == (uint)MuiWindowInputEventField.SubClass) size = 1;
			else if (index == (uint)MuiWindowInputEventField.Code ||
				index == (uint)MuiWindowInputEventField.Qualifier) size = 2;
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, size,
				out var candidate)) return false;
			if (index == selected)
			{
				address = candidate;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiWindowInputEventRecord.Size, out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowInputEventRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		switch (field)
		{
			case MuiWindowInputEventField.NextEvent:
				value = state.NextEvent.Raw; return true;
			case MuiWindowInputEventField.Position:
				value = unchecked((uint)state.Position); return true;
			case MuiWindowInputEventField.Seconds:
				value = state.Seconds; return true;
			case MuiWindowInputEventField.Microseconds:
				value = state.Microseconds; return true;
		}
		return false;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowInputEventRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowInputEventField.Code) value = state.Code;
		else if (field == MuiWindowInputEventField.Qualifier) value = state.Qualifier;
		else return false;
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowInputEventRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowInputEventField.Class) value = state.Class;
		else if (field == MuiWindowInputEventField.SubClass) value = state.SubClass;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowInputEventRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowInputEventField.NextEvent)
			state.NextEvent = APTR.FromPointer(value);
		else if (field == MuiWindowInputEventField.Position)
			state.Position = unchecked((int)value);
		else if (field == MuiWindowInputEventField.Seconds)
			state.Seconds = value;
		else if (field == MuiWindowInputEventField.Microseconds)
			state.Microseconds = value;
		else return false;
		return MuiWindowInputEventRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowInputEventRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowInputEventField.Code) state.Code = value;
		else if (field == MuiWindowInputEventField.Qualifier) state.Qualifier = value;
		else return false;
		return MuiWindowInputEventRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowInputEventRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowInputEventField.Class) state.Class = value;
		else if (field == MuiWindowInputEventField.SubClass) state.SubClass = value;
		else return false;
		return MuiWindowInputEventRecordCodec.WriteStructural(ref platform,
			record, state);
	}
}

internal static class MuiWindowInputEventFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowInputEventFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiWindowInputEventMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);
}

internal static class MuiWindowInputEventRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWindowInputEventRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowInputEventRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var nextEvent) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var @class) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var subClass) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var code) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var qualifier) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var position) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var seconds) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var microseconds)) return false;
		value.NextEvent = APTR.FromPointer(nextEvent);
		value.Class = @class;
		value.SubClass = subClass;
		value.Code = code;
		value.Qualifier = qualifier;
		value.Position = unchecked((int)position);
		value.Seconds = seconds;
		value.Microseconds = microseconds;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowInputEventRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowInputEventRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NextEvent.Raw) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.Class) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			value.SubClass) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			value.Code) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			value.Qualifier) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Position)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Seconds) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Microseconds) && MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiWindowInputEventCodec
{
	// Sequential named-struct path used by Window event dispatch. The mixed
	// byte/word/LONG InputEvent fields are consumed in declaration order; the
	// public Amiga.InputEvent remains a semantic projection of the packed
	// internal record.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowInputEventRecordCodec.TryReadStructural(ref platform,
			address, out var record)) return false;
		value.NextEvent = record.NextEvent;
		value.Class = (InputEventClass)record.Class;
		value.SubClass = (InputEventSubClass)record.SubClass;
		value.Code = record.Code;
		value.Qualifier = (InputEventQualifier)record.Qualifier;
		value.Position = record.Position;
		value.TimeStamp.Seconds = record.Seconds;
		value.TimeStamp.Microseconds = record.Microseconds;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiWindowInputEventRecord);
		record.NextEvent = value.NextEvent;
		record.Class = (byte)value.Class;
		record.SubClass = (byte)value.SubClass;
		record.Code = value.Code;
		record.Qualifier = (ushort)value.Qualifier;
		record.Position = value.Position;
		record.Seconds = value.TimeStamp.Seconds;
		record.Microseconds = value.TimeStamp.Microseconds;
		return MuiWindowInputEventRecordCodec.WriteStructural(ref platform,
			address, record);
	}

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, InputEvent value)
		where TPlatform : struct, IMuiGuestMemory => WriteRecord(ref platform,
			address, value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
