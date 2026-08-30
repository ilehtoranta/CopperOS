/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// The Intuition InputEvent is a fixed-size guest record. Keep the public value
// typed as Amiga.InputEvent; only this bounded adapter knows the packed guest
// representation required to cross the memory interface. MUI window state
// stores the validated pointer, never a managed copy of this value.
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

internal static class MuiWindowInputEventMemoryCodec
{
	private static bool TryResolve(MuiWindowInputEventField field,
		out uint offset, out uint size)
	{
		switch (field)
		{
			case MuiWindowInputEventField.NextEvent:
				offset = 0; size = 4; return true;
			case MuiWindowInputEventField.Class:
				offset = 4; size = 1; return true;
			case MuiWindowInputEventField.SubClass:
				offset = 5; size = 1; return true;
			case MuiWindowInputEventField.Code:
				offset = 6; size = 2; return true;
			case MuiWindowInputEventField.Qualifier:
				offset = 8; size = 2; return true;
			case MuiWindowInputEventField.Position:
				offset = 10; size = 4; return true;
			case MuiWindowInputEventField.Seconds:
				offset = 14; size = 4; return true;
			case MuiWindowInputEventField.Microseconds:
				offset = 18; size = 4; return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			InputEvent.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInputEventField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

internal static class MuiWindowInputEventCodec
{
	// Sequential named-struct path used by Window event dispatch. The mixed
	// byte/word/LONG InputEvent fields are consumed in declaration order; the
	// field adapter remains only for compatibility diagnostics.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			InputEvent.Size, out var cursor) ||
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
				out var microseconds) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.NextEvent = APTR.FromPointer(nextEvent);
		value.Class = (InputEventClass)@class;
		value.SubClass = (InputEventSubClass)subClass;
		value.Code = code;
		value.Qualifier = (InputEventQualifier)qualifier;
		value.Position = unchecked((int)position);
		value.TimeStamp.Seconds = seconds;
		value.TimeStamp.Microseconds = microseconds;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, InputEvent value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			InputEvent.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NextEvent.Raw) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			(byte)value.Class) &&
		MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
			(byte)value.SubClass) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			value.Code) &&
		MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
			(ushort)value.Qualifier) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Position)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.TimeStamp.Seconds) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.TimeStamp.Microseconds) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		InputEvent value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, value);
}
