/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The result published by MUIM_List_TestPos.  The public MorphOS record is a
// fixed 12-byte value: a signed entry index followed by the selected column,
// outside-cell flags, and cell-relative offsets.  Keep the value named in the
// core; only the codec below knows its packed guest representation.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListTestPosResult
{
	internal const uint Size = 12;
	internal const ushort FlagAbove = 1;
	internal const ushort FlagBelow = 2;
	internal const ushort FlagLeft = 4;
	internal const ushort FlagRight = 8;

	internal int Entry;
	internal short Column;
	internal ushort Flags;
	internal short XOffset;
	internal short YOffset;
}

internal enum MuiListTestPosResultField : byte
{
	Entry,
	Column,
	Flags,
	XOffset,
	YOffset,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListTestPosResultFieldCursor
{
	internal APTR Record;
	internal MuiListTestPosResultField Field;
}

internal static class MuiListTestPosResultFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListTestPosResultFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListTestPosResultMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);
}

// Struct-first adapter for the mixed-width MUIM_List_TestPos result. The
// result remains a named semantic struct; only this bounded boundary knows
// the 68k byte/word/long slots.
internal static class MuiListTestPosResultMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiListTestPosResultField field,
		out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = field switch
		{
			MuiListTestPosResultField.Entry => 4u,
			MuiListTestPosResultField.Column => 2u,
			MuiListTestPosResultField.Flags => 2u,
			MuiListTestPosResultField.XOffset => 2u,
			MuiListTestPosResultField.YOffset => 2u,
			_ => 0u,
		};
		if (size == 0) return false;
		var skips = field switch
		{
			MuiListTestPosResultField.Entry => 0u,
			MuiListTestPosResultField.Column => 1u,
			MuiListTestPosResultField.Flags => 2u,
			MuiListTestPosResultField.XOffset => 3u,
			MuiListTestPosResultField.YOffset => 4u,
			_ => uint.MaxValue,
		};
		if (skips == uint.MaxValue) return false;
		for (var i = 0u; i < skips; i++)
		{
			var skipSize = i == 0 ? 4u : 2u;
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				skipSize, out _)) return false;
		}
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor, size,
			out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiListTestPosResult.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address, out _))
			return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (field != MuiListTestPosResultField.Entry ||
			!MuiListTestPosResultCodec.TryReadRecord(ref platform, record,
				out var result)) return false;
		value = unchecked((uint)result.Entry);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (field == MuiListTestPosResultField.Entry ||
			!MuiListTestPosResultCodec.TryReadRecord(ref platform, record,
				out var result)) return false;
		if (field == MuiListTestPosResultField.Column)
			value = unchecked((ushort)result.Column);
		else if (field == MuiListTestPosResultField.Flags)
			value = result.Flags;
		else if (field == MuiListTestPosResultField.XOffset)
			value = unchecked((ushort)result.XOffset);
		else if (field == MuiListTestPosResultField.YOffset)
			value = unchecked((ushort)result.YOffset);
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (field != MuiListTestPosResultField.Entry ||
			!MuiListTestPosResultCodec.TryReadRecord(ref platform, record,
				out var result)) return false;
		result.Entry = unchecked((int)value);
		return MuiListTestPosResultCodec.WriteRecord(ref platform, record, result);
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (field == MuiListTestPosResultField.Entry ||
			!MuiListTestPosResultCodec.TryReadRecord(ref platform, record,
				out var result)) return false;
		if (field == MuiListTestPosResultField.Column)
			result.Column = unchecked((short)value);
		else if (field == MuiListTestPosResultField.Flags)
			result.Flags = value;
		else if (field == MuiListTestPosResultField.XOffset)
			result.XOffset = unchecked((short)value);
		else if (field == MuiListTestPosResultField.YOffset)
			result.YOffset = unchecked((short)value);
		else return false;
		return MuiListTestPosResultCodec.WriteRecord(ref platform, record, result);
	}
}

internal static class MuiListTestPosResultCodec
{
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR storage, MuiListTestPosResult value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, storage,
			MuiListTestPosResult.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Entry)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.Column)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Flags) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.XOffset)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.YOffset))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiListTestPosResult value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, storage,
			MuiListTestPosResult.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var entry) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var column) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var flags) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var xOffset) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var yOffset)) return false;
		value.Entry = unchecked((int)entry);
		value.Column = unchecked((short)column);
		value.Flags = flags;
		value.XOffset = unchecked((short)xOffset);
		value.YOffset = unchecked((short)yOffset);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListTestPosResult value) where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, storage, value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListTestPosResult value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, storage, out value);
}

// The selection and NextSelected APIs exchange a caller-owned LONG through a
// pointer. Keep that four-byte storage contract named at the boundary so list
// logic does not depend on an unexplained offset zero or a repeated size
// literal. The wire value remains a 68k ULONG; callers interpret signed
// sentinel values where the MorphOS API defines them.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListScalarStorageRecord
{
	internal const uint Size = 4;

	internal uint Value;
}

internal static class MuiListScalarStorageRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || !platform.IsMapped(record,
			MuiListScalarStorageRecord.Size)) return false;
		address = record;
		return platform.IsMapped(address, MuiListScalarStorageRecord.Size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, out _)) return false;
		return MuiListScalarStorageCodec.TryReadValue(ref platform, record,
			out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, out _)) return false;
		return MuiListScalarStorageCodec.WriteValue(ref platform, record, value);
	}
}

internal static class MuiListScalarStorageCodec
{
	// A one-ULONG record is passed as a scalar at the native ABI seam. Keep the
	// named record for semantic callers, but expose scalar helpers so the
	// freestanding compiler never mistakes the value for a guest pointer.
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR storage, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, value);

	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR storage, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, storage,
			out value);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR storage, ref MuiListScalarStorageRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, storage, value.Value);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiListScalarStorageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadValue(ref platform, storage, out var raw)) return false;
		value.Value = raw;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		ref MuiListScalarStorageRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, storage, value.Value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListScalarStorageRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, storage, out value);
}

// MorphOS MUI V6 display hooks receive the zero-based row number in the ULONG
// immediately preceding the display-column pointer.  Keep that ABI value
// named rather than making ListCore pass an unexplained four-byte slot around.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListDisplayRowRecord
{
	internal const uint Size = 4;

	internal int Row;
}

internal static class MuiListDisplayRowRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || !platform.IsMapped(record,
			MuiListDisplayRowRecord.Size)) return false;
		address = record;
		return platform.IsMapped(address, MuiListDisplayRowRecord.Size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, out _)) return false;
		if (!MuiListDisplayRowRecordCodec.TryReadValue(ref platform, record,
			out var row)) return false;
		value = unchecked((uint)row);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, out _)) return false;
		return MuiListDisplayRowRecordCodec.WriteValue(ref platform, record,
			value);
	}
}

internal static class MuiListDisplayRowRecordCodec
{
	// Keep the one-ULONG row value scalar at the native ABI seam; the named
	// record remains the semantic host representation and compatibility view.
	internal static bool WriteValue<TPlatform>(ref TPlatform platform,
		APTR storage, uint row)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestUlongStorageCodec.WriteValue(ref platform, storage, row);

	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR storage, out uint row)
		where TPlatform : struct, IMuiGuestMemory
	{
		row = 0;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, storage,
			out row);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR storage, ref MuiListDisplayRowRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, storage, unchecked((uint)value.Row));

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiListDisplayRowRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadValue(ref platform, storage, out var row)) return false;
		value.Row = unchecked((int)row);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		ref MuiListDisplayRowRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteValue(ref platform, storage, unchecked((uint)value.Row));

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListDisplayRowRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, storage, out value);
}

// The stable IntuiMessage fields needed by a Listview mouse path.  The full
// Intuition envelope is larger, but these fields are fixed by the 68k ABI and
// are all that Listview consumes.  The decoder validates the complete prefix
// before exposing the typed value, so malformed guest pointers are rejected
// without a partial event.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiIntuiPointerMessage
{
	internal const uint MinimumSize = 0x24;
	// Intuition appends Seconds/Micros to the pointer envelope.  The
	// timestamp is optional for callers that only provide the historical
	// 0x24-byte prefix, so keep its admission boundary named separately.
	internal const uint TimestampSize = 0x2C;
	internal const uint ClassOffset = 0x14;
	internal const uint CodeOffset = 0x18;
	internal const uint QualifierOffset = 0x1A;
	internal const uint IAddressOffset = 0x1C;
	internal const uint MouseXOffset = 0x20;
	internal const uint MouseYOffset = 0x22;
	internal const uint SecondsOffset = 0x24;
	internal const uint MicrosOffset = 0x28;

	internal uint Class;
	internal ushort Code;
	internal ushort Qualifier;
	internal uint IAddress;
	internal short MouseX;
	internal short MouseY;
	internal uint Seconds;
	internal uint Micros;
	internal uint TimestampValid;
}

// The raw-key provider needs the stable Class/Code/Qualifier prefix of an
// IntuiMessage. Keep that shorter admission boundary named rather than forcing
// it through the pointer envelope and accidentally tightening the historical
// 0x1c-byte raw-key requirement.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiIntuiRawKeyMessage
{
	internal const uint Size = 0x1C;
	internal const uint ClassOffset = 0x14;
	internal const uint CodeOffset = 0x18;
	internal const uint QualifierOffset = 0x1A;

	internal uint Class;
	internal ushort Code;
	internal ushort Qualifier;
}

internal enum MuiIntuiPointerMessageField : byte
{
	Class,
	Code,
	Qualifier,
	IAddress,
	MouseX,
	MouseY,
	Seconds,
	Micros,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiIntuiPointerMessageFieldCursor
{
	internal APTR Record;
	internal MuiIntuiPointerMessageField Field;
}

internal static class MuiIntuiPointerMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiIntuiPointerMessageFieldCursor cursor, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory =>
		MuiIntuiPointerMessageMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address, out fieldSize);
}

// Struct-first adapter for the stable IntuiMessage pointer envelope. The
// semantic message remains a named value; this bounded adapter is the only
// place that translates its MorphOS byte/word/long guest representation.
internal static class MuiIntuiPointerMessageMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiIntuiPointerMessageField field,
		out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		var skips = (uint)field;
		if (skips > (uint)MuiIntuiPointerMessageField.Micros) return false;
		if (field == MuiIntuiPointerMessageField.Class ||
			field == MuiIntuiPointerMessageField.IAddress ||
			field == MuiIntuiPointerMessageField.Seconds ||
			field == MuiIntuiPointerMessageField.Micros)
			fieldSize = 4u;
		else if (field == MuiIntuiPointerMessageField.Code ||
			field == MuiIntuiPointerMessageField.Qualifier ||
			field == MuiIntuiPointerMessageField.MouseX ||
			field == MuiIntuiPointerMessageField.MouseY)
			fieldSize = 2u;
		else return false;
		for (var i = 0u; i < skips; i++)
		{
			var skipSize = 2u;
			if (i == 0u || i == 3u || i >= 6u) skipSize = 4u;
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				skipSize, out _)) return false;
		}
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor, fieldSize,
			out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		var recordSize = field == MuiIntuiPointerMessageField.Seconds ||
			field == MuiIntuiPointerMessageField.Micros
			? MuiIntuiPointerMessage.TimestampSize
			: MuiIntuiPointerMessage.MinimumSize;
		var payloadSize = recordSize - MuiIntuiPointerMessage.ClassOffset;
		if (!MuiIntuiMessageCodec.TryCreatePayloadCursor(ref platform, record, payloadSize,
			out var cursor) || !TryTakeField(ref platform, ref cursor, field,
			out address, out fieldSize)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref platform, record,
			out var message)) return false;
		if (field == MuiIntuiPointerMessageField.Class)
			value = message.Class;
		else if (field == MuiIntuiPointerMessageField.IAddress)
			value = message.IAddress;
		else if (field == MuiIntuiPointerMessageField.Seconds)
		{
			if (message.TimestampValid == 0) return false;
			value = message.Seconds;
		}
		else if (field == MuiIntuiPointerMessageField.Micros)
		{
			if (message.TimestampValid == 0) return false;
			value = message.Micros;
		}
		else return false;
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref platform, record,
			out var message)) return false;
		if (field == MuiIntuiPointerMessageField.Code)
			value = message.Code;
		else if (field == MuiIntuiPointerMessageField.Qualifier)
			value = message.Qualifier;
		else if (field == MuiIntuiPointerMessageField.MouseX)
			value = unchecked((ushort)message.MouseX);
		else if (field == MuiIntuiPointerMessageField.MouseY)
			value = unchecked((ushort)message.MouseY);
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref platform, record,
			out var message)) return false;
		if (field == MuiIntuiPointerMessageField.Class)
			message.Class = value;
		else if (field == MuiIntuiPointerMessageField.IAddress)
			message.IAddress = value;
		else if (field == MuiIntuiPointerMessageField.Seconds)
		{
			if (message.TimestampValid == 0) return false;
			message.Seconds = value;
		}
		else if (field == MuiIntuiPointerMessageField.Micros)
		{
			if (message.TimestampValid == 0) return false;
			message.Micros = value;
		}
		else return false;
		return MuiIntuiMessageCodec.WritePointerRecord(ref platform, record,
			message);
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiIntuiMessageCodec.TryReadPointerRecord(ref platform, record,
			out var message)) return false;
		if (field == MuiIntuiPointerMessageField.Code)
			message.Code = value;
		else if (field == MuiIntuiPointerMessageField.Qualifier)
			message.Qualifier = value;
		else if (field == MuiIntuiPointerMessageField.MouseX)
			message.MouseX = unchecked((short)value);
		else if (field == MuiIntuiPointerMessageField.MouseY)
			message.MouseY = unchecked((short)value);
		else return false;
		return MuiIntuiMessageCodec.WritePointerRecord(ref platform, record,
			message);
	}
}

internal enum MuiIntuiRawKeyMessageField : byte
{
	Class,
	Code,
	Qualifier,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiIntuiRawKeyMessageFieldCursor
{
	internal APTR Record;
	internal MuiIntuiRawKeyMessageField Field;
}

internal static class MuiIntuiRawKeyMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiIntuiRawKeyMessageFieldCursor cursor, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory =>
		MuiIntuiRawKeyMessageMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address, out fieldSize);
}

// Struct-first adapter for the shorter raw-key envelope. Keeping it separate
// prevents callers from accidentally requiring the optional pointer-message
// suffix when MorphOS only supplied the raw-key prefix.
internal static class MuiIntuiRawKeyMessageMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiIntuiRawKeyMessageField field,
		out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		var skips = (uint)field;
		if (skips > (uint)MuiIntuiRawKeyMessageField.Qualifier) return false;
		if (field == MuiIntuiRawKeyMessageField.Class)
			fieldSize = 4u;
		else if (field == MuiIntuiRawKeyMessageField.Code ||
			field == MuiIntuiRawKeyMessageField.Qualifier)
			fieldSize = 2u;
		else return false;
		for (var i = 0u; i < skips; i++)
		{
			var skipSize = i == 0u ? 4u : 2u;
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				skipSize, out _)) return false;
		}
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor, fieldSize,
			out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		var payloadSize = MuiIntuiRawKeyMessage.Size -
			MuiIntuiPointerMessage.ClassOffset;
		if (!MuiIntuiMessageCodec.TryCreatePayloadCursor(ref platform, record,
			payloadSize, out var cursor) || !TryTakeField(ref platform, ref cursor,
			field, out address, out fieldSize)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (field != MuiIntuiRawKeyMessageField.Class ||
			!MuiIntuiMessageCodec.TryReadRawKeyRecordUnchecked(ref platform,
				record, out var message)) return false;
		value = message.Class;
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (field == MuiIntuiRawKeyMessageField.Class ||
			!MuiIntuiMessageCodec.TryReadRawKeyRecordUnchecked(ref platform,
				record, out var message)) return false;
		if (field == MuiIntuiRawKeyMessageField.Code)
			value = message.Code;
		else if (field == MuiIntuiRawKeyMessageField.Qualifier)
			value = message.Qualifier;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (field != MuiIntuiRawKeyMessageField.Class ||
			!MuiIntuiMessageCodec.TryReadRawKeyRecordUnchecked(ref platform,
				record, out var message)) return false;
		message.Class = value;
		return MuiIntuiMessageCodec.WriteRawKeyRecord(ref platform, record,
			message);
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (field == MuiIntuiRawKeyMessageField.Class ||
			!MuiIntuiMessageCodec.TryReadRawKeyRecordUnchecked(ref platform,
				record, out var message)) return false;
		if (field == MuiIntuiRawKeyMessageField.Code)
			message.Code = value;
		else if (field == MuiIntuiRawKeyMessageField.Qualifier)
			message.Qualifier = value;
		else return false;
		return MuiIntuiMessageCodec.WriteRawKeyRecord(ref platform, record,
			message);
	}
}

internal static class MuiIntuiMessageCodec
{
	// IDCMP_RAWKEY is the only IntuiMessage class consumed by the bounded
	// Keyadjust text path. Keep this ABI fact beside the typed decoder rather
	// than making platform providers repeat a numeric class check and message
	// offsets.
	internal const uint RawKeyClass = 0x00000400u;
	// MorphOS IECODE_UP_PREFIX marks a key-up code in InputEvent.Code and the
	// corresponding IntuiMessage.Code field.
	internal const ushort RawKeyUpPrefix = 0x0080;

	internal static bool IsRawKeyRelease(ushort code) =>
		code <= byte.MaxValue && (code & RawKeyUpPrefix) != 0;

	// The Intuition message envelope has a stable, ABI-defined prefix before
	// these named payload records. This helper confines that one wire offset to
	// the boundary; all payload fields are then exchanged sequentially.
	internal static bool TryCreatePayloadCursor<TPlatform>(ref TPlatform platform,
		APTR message, uint payloadSize, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory
	{
		cursor = default;
		if ((message.Raw & 1u) != 0 || message.IsNull ||
			message.Raw > uint.MaxValue - MuiIntuiPointerMessage.ClassOffset ||
			payloadSize > uint.MaxValue - MuiIntuiPointerMessage.ClassOffset ||
			!platform.IsMapped(message,
				MuiIntuiPointerMessage.ClassOffset + payloadSize))
			return false;
		return MuiGuestStructCursor.TryCreate(ref platform,
			APTR.FromPointer(message.Raw + MuiIntuiPointerMessage.ClassOffset),
			payloadSize, out cursor);
	}

	internal static bool TryReadPointerRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiIntuiPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		var timestamp = platform.IsMapped(message,
			MuiIntuiPointerMessage.TimestampSize);
		var payloadSize = timestamp
			? MuiIntuiPointerMessage.TimestampSize -
				MuiIntuiPointerMessage.ClassOffset
			: MuiIntuiPointerMessage.MinimumSize -
				MuiIntuiPointerMessage.ClassOffset;
		if (!TryCreatePayloadCursor(ref platform, message, payloadSize,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var messageClass) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var code) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var qualifier) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var iAddress) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var mouseX) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var mouseY)) return false;
		value.Class = messageClass;
		value.Code = code;
		value.Qualifier = qualifier;
		value.IAddress = iAddress;
		value.MouseX = unchecked((short)mouseX);
		value.MouseY = unchecked((short)mouseY);
		if (timestamp &&
			(!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Seconds) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Micros))) return false;
		value.TimestampValid = timestamp ? 1u : 0u;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WritePointerRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiIntuiPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var timestamp = platform.IsMapped(message,
			MuiIntuiPointerMessage.TimestampSize);
		var payloadSize = timestamp
			? MuiIntuiPointerMessage.TimestampSize -
				MuiIntuiPointerMessage.ClassOffset
			: MuiIntuiPointerMessage.MinimumSize -
				MuiIntuiPointerMessage.ClassOffset;
		if (!TryCreatePayloadCursor(ref platform, message, payloadSize,
			out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Class) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Code) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Qualifier) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.IAddress) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.MouseX)) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.MouseY))) return false;
		if (timestamp &&
			(!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Seconds) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Micros))) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRawKeyRecord<TPlatform>(ref TPlatform platform,
		APTR message, out MuiIntuiRawKeyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRawKeyRecordCore(ref platform, message, true, out value);
	}

	// The field adapter historically exposed the raw prefix without validating
	// IDCMP class. Keep that compatibility behavior while still decoding the
	// complete named record; the public raw-key reader above remains strict.
	internal static bool TryReadRawKeyRecordUnchecked<TPlatform>(
		ref TPlatform platform, APTR message, out MuiIntuiRawKeyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRawKeyRecordCore(ref platform, message, false, out value);
	}

	private static bool TryReadRawKeyRecordCore<TPlatform>(ref TPlatform platform,
		APTR message, bool requireRawKeyClass, out MuiIntuiRawKeyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryCreatePayloadCursor(ref platform, message,
			MuiIntuiRawKeyMessage.Size - MuiIntuiRawKeyMessage.ClassOffset,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var messageClass) || (requireRawKeyClass &&
				messageClass != RawKeyClass) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var code) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var qualifier) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Class = messageClass;
		value.Code = code;
		value.Qualifier = qualifier;
		return true;
	}

	internal static bool WriteRawKeyRecord<TPlatform>(ref TPlatform platform,
		APTR message, MuiIntuiRawKeyMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryCreatePayloadCursor(ref platform, message,
			MuiIntuiRawKeyMessage.Size - MuiIntuiRawKeyMessage.ClassOffset,
			out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Class) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Code) ||
			!MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Qualifier)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiIntuiPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadPointerRecord(ref platform, message, out value);

	internal static bool TryReadRawKey<TPlatform>(ref TPlatform platform,
		APTR message, out MuiIntuiRawKeyMessage value)
	where TPlatform : struct, IMuiGuestMemory
		=> TryReadRawKeyRecord(ref platform, message, out value);

	internal static bool TryReadRawKeyCode<TPlatform>(ref TPlatform platform,
		APTR message, out ushort code)
		where TPlatform : struct, IMuiGuestMemory
	{
		code = 0;
		if (!TryReadRawKey(ref platform, message, out var value)) return false;
		code = value.Code;
		return true;
	}

	internal static bool WriteRawKey<TPlatform>(ref TPlatform platform,
		APTR message, uint messageClass, ushort code)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRawKey(ref platform, message, messageClass, code, 0);

	internal static bool WriteRawKey<TPlatform>(ref TPlatform platform,
		APTR message, uint messageClass, ushort code, ushort qualifier)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiIntuiRawKeyMessage);
		value.Class = messageClass;
		value.Code = code;
		value.Qualifier = qualifier;
		return WriteRawKeyRecord(ref platform, message, value);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint messageClass, ushort code, ushort qualifier,
		uint iAddress, short mouseX, short mouseY)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiIntuiPointerMessage);
		value.Class = messageClass;
		value.Code = code;
		value.Qualifier = qualifier;
		value.IAddress = iAddress;
		value.MouseX = mouseX;
		value.MouseY = mouseY;
		return WritePointerRecord(ref platform, message, value);
	}

	internal static bool WritePointerWithTime<TPlatform>(ref TPlatform platform,
		APTR message, uint messageClass, ushort code, ushort qualifier,
		uint iAddress, short mouseX, short mouseY, uint seconds, uint micros)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiIntuiPointerMessage);
		value.Class = messageClass;
		value.Code = code;
		value.Qualifier = qualifier;
		value.IAddress = iAddress;
		value.MouseX = mouseX;
		value.MouseY = mouseY;
		value.Seconds = seconds;
		value.Micros = micros;
		value.TimestampValid = 1;
		return WritePointerRecord(ref platform, message, value);
	}
}

// Guest-resident state for the bounded Listview drag-sort path.  Source and
// target are list row indices; coordinates are retained only to make the
// current drag transition observable without keeping a managed event object.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewDragState
{
	internal const uint Size = 32;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint SourceOffset = 4;
	internal const uint TargetOffset = 8;
	internal const uint StartXOffset = 12;
	internal const uint StartYOffset = 16;
	internal const uint LastXOffset = 20;
	internal const uint LastYOffset = 24;
	internal const uint FlagsOffset = 28;
	internal const uint ActiveFlag = 1;
	internal const uint MovedFlag = 2;
	internal const uint CapturedFlag = 4;

	internal uint Magic;
	internal int Source;
	internal int Target;
	internal int StartX;
	internal int StartY;
	internal int LastX;
	internal int LastY;
	internal uint Flags;
}

internal enum MuiListviewDragStateField : byte
{
	Magic,
	Source,
	Target,
	StartX,
	StartY,
	LastX,
	LastY,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewDragStateFieldCursor
{
	internal APTR Record;
	internal MuiListviewDragStateField Field;
}

internal static class MuiListviewDragStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewDragStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewDragStateMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);
}

// Struct-first adapter for the guest-resident Listview drag state. The drag
// lifecycle consumes a named semantic value; this bounded adapter is the only
// layer that translates its eight 68k LONG fields.
internal static class MuiListviewDragStateMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiListviewDragStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		var skips = (uint)field;
		if (skips > (uint)MuiListviewDragStateField.Flags) return false;
		for (var i = 0u; i < skips; i++)
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiListviewDragState.FieldSize, out _)) return false;
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiListviewDragState.FieldSize, out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewDragStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiListviewDragState.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewDragStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiListviewDragStateCodec.TryReadRecord(ref platform, record,
			out var state)) return false;
		if (field == MuiListviewDragStateField.Magic)
			value = state.Magic;
		else if (field == MuiListviewDragStateField.Source)
			value = unchecked((uint)state.Source);
		else if (field == MuiListviewDragStateField.Target)
			value = unchecked((uint)state.Target);
		else if (field == MuiListviewDragStateField.StartX)
			value = unchecked((uint)state.StartX);
		else if (field == MuiListviewDragStateField.StartY)
			value = unchecked((uint)state.StartY);
		else if (field == MuiListviewDragStateField.LastX)
			value = unchecked((uint)state.LastX);
		else if (field == MuiListviewDragStateField.LastY)
			value = unchecked((uint)state.LastY);
		else if (field == MuiListviewDragStateField.Flags)
			value = state.Flags;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewDragStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListviewDragStateCodec.TryReadRecord(ref platform, record,
			out var state)) return false;
		if (field == MuiListviewDragStateField.Magic)
			state.Magic = value;
		else if (field == MuiListviewDragStateField.Source)
			state.Source = unchecked((int)value);
		else if (field == MuiListviewDragStateField.Target)
			state.Target = unchecked((int)value);
		else if (field == MuiListviewDragStateField.StartX)
			state.StartX = unchecked((int)value);
		else if (field == MuiListviewDragStateField.StartY)
			state.StartY = unchecked((int)value);
		else if (field == MuiListviewDragStateField.LastX)
			state.LastX = unchecked((int)value);
		else if (field == MuiListviewDragStateField.LastY)
			state.LastY = unchecked((int)value);
		else if (field == MuiListviewDragStateField.Flags)
			state.Flags = value;
		else return false;
		return MuiListviewDragStateCodec.WriteRecord(ref platform, record, state);
	}
}

internal static class MuiListviewDragStateCodec
{
	internal const uint Cookie = 0x4C564447u; // 'LVDG'

	// Production access is sequential and struct-shaped. The field-address
	// adapter remains available only for compatibility diagnostics.
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR storage, MuiListviewDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((storage.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, storage,
				MuiListviewDragState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Source)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Target)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.StartX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.StartY)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.LastX)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.LastY)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiListviewDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((storage.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, storage,
				MuiListviewDragState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var source) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var target) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var lastX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var lastY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var flags) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Magic = magic;
		value.Source = unchecked((int)source);
		value.Target = unchecked((int)target);
		value.StartX = unchecked((int)startX);
		value.StartY = unchecked((int)startY);
		value.LastX = unchecked((int)lastX);
		value.LastY = unchecked((int)lastY);
		value.Flags = flags;
		return true;
	}

	internal static void Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListviewDragState value) where TPlatform : struct, IMuiGuestMemory
	{
		_ = WriteRecord(ref platform, storage, value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR storage, MuiListviewDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return WriteRecord(ref platform, storage, value) &&
			TryReadRecord(ref platform, storage, out _);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiListviewDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRecord(ref platform, storage, out value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListviewDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, storage, out value) &&
		value.Magic == Cookie;

	internal static void Clear<TPlatform>(ref TPlatform platform, APTR storage)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListviewDragState.Size)) return;
		platform.Clear(storage, MuiListviewDragState.Size);
	}
}
