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

// Struct-first adapter for the mixed-width MUIM_List_TestPos result. The
// result remains a named semantic struct; only this bounded boundary knows
// the 68k byte/word/long slots.
internal static class MuiListTestPosResultMemoryCodec
{
	private static bool TryResolve(MuiListTestPosResultField field,
		out uint offset, out uint size)
	{
		switch (field)
		{
			case MuiListTestPosResultField.Entry:
				offset = 0; size = 4; return true;
			case MuiListTestPosResultField.Column:
				offset = 4; size = 2; return true;
			case MuiListTestPosResultField.Flags:
				offset = 6; size = 2; return true;
			case MuiListTestPosResultField.XOffset:
				offset = 8; size = 2; return true;
			case MuiListTestPosResultField.YOffset:
				offset = 10; size = 2; return true;
		}
		offset = 0;
		size = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var size) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiListTestPosResult.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address) ||
			field != MuiListTestPosResultField.Entry) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address) ||
			field == MuiListTestPosResultField.Entry) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address) ||
			field != MuiListTestPosResultField.Entry) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiListTestPosResultField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address) ||
			field == MuiListTestPosResultField.Entry) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}
}

internal static class MuiListTestPosResultCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListTestPosResult value) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListTestPosResultMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListTestPosResultField.Entry, unchecked((uint)value.Entry)) &&
			MuiListTestPosResultMemoryCodec.TryWriteUInt16(ref platform, storage,
			MuiListTestPosResultField.Column, unchecked((ushort)value.Column)) &&
			MuiListTestPosResultMemoryCodec.TryWriteUInt16(ref platform, storage,
				MuiListTestPosResultField.Flags, value.Flags) &&
			MuiListTestPosResultMemoryCodec.TryWriteUInt16(ref platform, storage,
				MuiListTestPosResultField.XOffset, unchecked((ushort)value.XOffset)) &&
			MuiListTestPosResultMemoryCodec.TryWriteUInt16(ref platform, storage,
				MuiListTestPosResultField.YOffset, unchecked((ushort)value.YOffset));
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListTestPosResult value) where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiListTestPosResultMemoryCodec.TryReadUInt32(ref platform, storage,
			MuiListTestPosResultField.Entry, out var entry) ||
			!MuiListTestPosResultMemoryCodec.TryReadUInt16(ref platform, storage,
				MuiListTestPosResultField.Column, out var column) ||
			!MuiListTestPosResultMemoryCodec.TryReadUInt16(ref platform, storage,
				MuiListTestPosResultField.Flags, out value.Flags) ||
			!MuiListTestPosResultMemoryCodec.TryReadUInt16(ref platform, storage,
				MuiListTestPosResultField.XOffset, out var xOffset) ||
			!MuiListTestPosResultMemoryCodec.TryReadUInt16(ref platform, storage,
				MuiListTestPosResultField.YOffset, out var yOffset)) return false;
		value.Entry = unchecked((int)entry);
		value.Column = unchecked((short)column);
		value.XOffset = unchecked((short)xOffset);
		value.YOffset = unchecked((short)yOffset);
		return true;
	}
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
		value = platform.ReadUInt32(record, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, out _)) return false;
		platform.WriteUInt32(record, 0, value);
		return true;
	}
}

internal static class MuiListScalarStorageCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListScalarStorageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListScalarStorageRecordMemoryCodec.TryWriteUInt32(ref platform,
			storage, value.Value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListScalarStorageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiListScalarStorageRecordMemoryCodec.TryReadUInt32(ref platform,
			storage, out value.Value);
	}
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
		value = platform.ReadUInt32(record, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, out _)) return false;
		platform.WriteUInt32(record, 0, value);
		return true;
	}
}

internal static class MuiListDisplayRowRecordCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListDisplayRowRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListDisplayRowRecordMemoryCodec.TryWriteUInt32(ref platform,
			storage, unchecked((uint)value.Row));
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		out MuiListDisplayRowRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiListDisplayRowRecordMemoryCodec.TryReadUInt32(ref platform,
			storage, out var row)) return false;
		value.Row = unchecked((int)row);
		return true;
	}
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

// Struct-first adapter for the stable IntuiMessage pointer envelope. The
// semantic message remains a named value; this bounded adapter is the only
// place that translates its MorphOS byte/word/long guest representation.
internal static class MuiIntuiPointerMessageMemoryCodec
{
	private static bool TryResolve(MuiIntuiPointerMessageField field,
		out uint offset, out uint recordSize, out uint fieldSize)
	{
		offset = 0;
		recordSize = MuiIntuiPointerMessage.MinimumSize;
		fieldSize = 0;
		switch (field)
		{
			case MuiIntuiPointerMessageField.Class:
				offset = MuiIntuiPointerMessage.ClassOffset;
				fieldSize = 4;
				return true;
			case MuiIntuiPointerMessageField.Code:
				offset = MuiIntuiPointerMessage.CodeOffset;
				fieldSize = 2;
				return true;
			case MuiIntuiPointerMessageField.Qualifier:
				offset = MuiIntuiPointerMessage.QualifierOffset;
				fieldSize = 2;
				return true;
			case MuiIntuiPointerMessageField.IAddress:
				offset = MuiIntuiPointerMessage.IAddressOffset;
				fieldSize = 4;
				return true;
			case MuiIntuiPointerMessageField.MouseX:
				offset = MuiIntuiPointerMessage.MouseXOffset;
				fieldSize = 2;
				return true;
			case MuiIntuiPointerMessageField.MouseY:
				offset = MuiIntuiPointerMessage.MouseYOffset;
				fieldSize = 2;
				return true;
			case MuiIntuiPointerMessageField.Seconds:
				offset = MuiIntuiPointerMessage.SecondsOffset;
				recordSize = MuiIntuiPointerMessage.TimestampSize;
				fieldSize = 4;
				return true;
			case MuiIntuiPointerMessageField.Micros:
				offset = MuiIntuiPointerMessage.MicrosOffset;
				recordSize = MuiIntuiPointerMessage.TimestampSize;
				fieldSize = 4;
				return true;
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(field, out var offset, out var recordSize,
			out fieldSize) || record.IsNull || record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, recordSize)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != 2) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiPointerMessageField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != 2) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}
}

internal enum MuiIntuiRawKeyMessageField : byte
{
	Class,
	Code,
	Qualifier,
}

// Struct-first adapter for the shorter raw-key envelope. Keeping it separate
// prevents callers from accidentally requiring the optional pointer-message
// suffix when MorphOS only supplied the raw-key prefix.
internal static class MuiIntuiRawKeyMessageMemoryCodec
{
	private static bool TryResolve(MuiIntuiRawKeyMessageField field,
		out uint offset, out uint fieldSize)
	{
		switch (field)
		{
			case MuiIntuiRawKeyMessageField.Class:
				offset = MuiIntuiRawKeyMessage.ClassOffset;
				fieldSize = 4;
				return true;
			case MuiIntuiRawKeyMessageField.Code:
				offset = MuiIntuiRawKeyMessage.CodeOffset;
				fieldSize = 2;
				return true;
			case MuiIntuiRawKeyMessageField.Qualifier:
				offset = MuiIntuiRawKeyMessage.QualifierOffset;
				fieldSize = 2;
				return true;
		}
		offset = 0;
		fieldSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(field, out var offset, out fieldSize) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiIntuiRawKeyMessage.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != 2) return false;
		value = platform.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiIntuiRawKeyMessageField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address,
			out var fieldSize) || fieldSize != 2) return false;
		platform.WriteUInt16(address, 0, value);
		return true;
	}
}

internal static class MuiIntuiMessageCodec
{
	// IDCMP_RAWKEY is the only IntuiMessage class consumed by the bounded
	// Keyadjust text path. Keep this ABI fact beside the typed decoder rather
	// than making platform providers repeat a numeric class check and message
	// offsets.
	internal const uint RawKeyClass = 0x00000400u;

	internal static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		APTR message, out MuiIntuiPointerMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiIntuiPointerMessage.MinimumSize)) return false;
		if (!MuiIntuiPointerMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiIntuiPointerMessageField.Class, out value.Class) ||
			!MuiIntuiPointerMessageMemoryCodec.TryReadUInt16(ref platform,
				message, MuiIntuiPointerMessageField.Code, out value.Code) ||
			!MuiIntuiPointerMessageMemoryCodec.TryReadUInt16(ref platform,
				message, MuiIntuiPointerMessageField.Qualifier, out value.Qualifier) ||
			!MuiIntuiPointerMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiIntuiPointerMessageField.IAddress, out value.IAddress) ||
			!MuiIntuiPointerMessageMemoryCodec.TryReadUInt16(ref platform,
				message, MuiIntuiPointerMessageField.MouseX, out var mouseX) ||
			!MuiIntuiPointerMessageMemoryCodec.TryReadUInt16(ref platform,
				message, MuiIntuiPointerMessageField.MouseY, out var mouseY)) return false;
		value.MouseX = unchecked((short)mouseX);
		value.MouseY = unchecked((short)mouseY);
		// Seconds/Micros are an optional extension of the stable pointer prefix.
		// A short synthetic IntuiMessage remains valid; double-click policy simply
		// treats it as an untimed single click rather than guessing from host time.
		if (MuiIntuiPointerMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiIntuiPointerMessageField.Seconds, out value.Seconds) &&
			MuiIntuiPointerMessageMemoryCodec.TryReadUInt32(ref platform, message,
			MuiIntuiPointerMessageField.Micros, out value.Micros))
			value.TimestampValid = 1;
		return true;
	}

	internal static bool TryReadRawKey<TPlatform>(ref TPlatform platform,
		APTR message, out MuiIntuiRawKeyMessage value)
	where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		uint messageClass;
		ushort code;
		ushort qualifier;
		if (message.IsNull || !platform.IsMapped(message,
			MuiIntuiRawKeyMessage.Size) ||
			!MuiIntuiRawKeyMessageMemoryCodec.TryReadUInt32(ref platform,
				message, MuiIntuiRawKeyMessageField.Class, out messageClass) ||
			messageClass != RawKeyClass ||
			!MuiIntuiRawKeyMessageMemoryCodec.TryReadUInt16(ref platform,
				message, MuiIntuiRawKeyMessageField.Code, out code) ||
			!MuiIntuiRawKeyMessageMemoryCodec.TryReadUInt16(ref platform,
				message, MuiIntuiRawKeyMessageField.Qualifier, out qualifier)) return false;
		value.Class = messageClass;
		value.Code = code;
		value.Qualifier = qualifier;
		return true;
	}

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
		if (message.IsNull || !platform.IsMapped(message,
			MuiIntuiRawKeyMessage.Size)) return false;
		return MuiIntuiRawKeyMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiIntuiRawKeyMessageField.Class, messageClass) &&
			MuiIntuiRawKeyMessageMemoryCodec.TryWriteUInt16(ref platform,
				message, MuiIntuiRawKeyMessageField.Code, code) &&
			MuiIntuiRawKeyMessageMemoryCodec.TryWriteUInt16(ref platform,
				message, MuiIntuiRawKeyMessageField.Qualifier, qualifier);
	}

	internal static bool WritePointer<TPlatform>(ref TPlatform platform,
		APTR message, uint messageClass, ushort code, ushort qualifier,
		uint iAddress, short mouseX, short mouseY)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiIntuiPointerMessage.MinimumSize)) return false;
		var written = MuiIntuiPointerMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiIntuiPointerMessageField.Class, messageClass) &&
			MuiIntuiPointerMessageMemoryCodec.TryWriteUInt16(ref platform,
				message, MuiIntuiPointerMessageField.Code, code) &&
			MuiIntuiPointerMessageMemoryCodec.TryWriteUInt16(ref platform,
				message, MuiIntuiPointerMessageField.Qualifier, qualifier) &&
			MuiIntuiPointerMessageMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiIntuiPointerMessageField.IAddress, iAddress) &&
			MuiIntuiPointerMessageMemoryCodec.TryWriteUInt16(ref platform,
				message, MuiIntuiPointerMessageField.MouseX, unchecked((ushort)mouseX)) &&
			MuiIntuiPointerMessageMemoryCodec.TryWriteUInt16(ref platform,
				message, MuiIntuiPointerMessageField.MouseY, unchecked((ushort)mouseY));
		if (!written) return false;
		// Reusing a caller-owned envelope must not retain a previous timestamp.
		// A full IntuiMessage maps the optional suffix, so clear it to the
		// canonical zero value when this prefix-only helper is used.
		if (platform.IsMapped(message, MuiIntuiPointerMessage.TimestampSize))
			return MuiIntuiPointerMessageMemoryCodec.TryWriteUInt32(ref platform,
				message, MuiIntuiPointerMessageField.Seconds, 0) &&
				MuiIntuiPointerMessageMemoryCodec.TryWriteUInt32(ref platform,
					message, MuiIntuiPointerMessageField.Micros, 0);
		return true;
	}

	internal static bool WritePointerWithTime<TPlatform>(ref TPlatform platform,
		APTR message, uint messageClass, ushort code, ushort qualifier,
		uint iAddress, short mouseX, short mouseY, uint seconds, uint micros)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!WritePointer(ref platform, message, messageClass, code, qualifier,
			iAddress, mouseX, mouseY) ||
			!platform.IsMapped(message, MuiIntuiPointerMessage.TimestampSize))
			return false;
		return MuiIntuiPointerMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiIntuiPointerMessageField.Seconds, seconds) &&
			MuiIntuiPointerMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiIntuiPointerMessageField.Micros, micros);
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

// Struct-first adapter for the guest-resident Listview drag state. The drag
// lifecycle consumes a named semantic value; this bounded adapter is the only
// layer that translates its eight 68k LONG fields.
internal static class MuiListviewDragStateMemoryCodec
{
	private static bool TryResolve(MuiListviewDragStateField field,
		out uint offset)
	{
		offset = 0;
		switch (field)
		{
			case MuiListviewDragStateField.Magic:
				offset = MuiListviewDragState.MagicOffset; return true;
			case MuiListviewDragStateField.Source:
				offset = MuiListviewDragState.SourceOffset; return true;
			case MuiListviewDragStateField.Target:
				offset = MuiListviewDragState.TargetOffset; return true;
			case MuiListviewDragStateField.StartX:
				offset = MuiListviewDragState.StartXOffset; return true;
			case MuiListviewDragStateField.StartY:
				offset = MuiListviewDragState.StartYOffset; return true;
			case MuiListviewDragStateField.LastX:
				offset = MuiListviewDragState.LastXOffset; return true;
			case MuiListviewDragStateField.LastY:
				offset = MuiListviewDragState.LastYOffset; return true;
			case MuiListviewDragStateField.Flags:
				offset = MuiListviewDragState.FlagsOffset; return true;
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewDragStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiListviewDragState.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiListviewDragState.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewDragStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewDragStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiListviewDragStateCodec
{
	internal const uint Cookie = 0x4C564447u; // 'LVDG'

	internal static void Write<TPlatform>(ref TPlatform platform, APTR storage,
		MuiListviewDragState value) where TPlatform : struct, IMuiGuestMemory
	{
		_ = MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListviewDragStateField.Magic, value.Magic);
		_ = MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListviewDragStateField.Source, unchecked((uint)value.Source));
		_ = MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListviewDragStateField.Target, unchecked((uint)value.Target));
		_ = MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListviewDragStateField.StartX, unchecked((uint)value.StartX));
		_ = MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListviewDragStateField.StartY, unchecked((uint)value.StartY));
		_ = MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListviewDragStateField.LastX, unchecked((uint)value.LastX));
		_ = MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListviewDragStateField.LastY, unchecked((uint)value.LastY));
		_ = MuiListviewDragStateMemoryCodec.TryWriteUInt32(ref platform,
			storage, MuiListviewDragStateField.Flags, value.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR storage, MuiListviewDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListviewDragState.Size)) return false;
		Write(ref platform, storage, value);
		return TryRead(ref platform, storage, out _);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR storage, out MuiListviewDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListviewDragState.Size) ||
			!MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
				storage, MuiListviewDragStateField.Magic, out var magic) ||
			!MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
				storage, MuiListviewDragStateField.Source, out var source) ||
			!MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
				storage, MuiListviewDragStateField.Target, out var target) ||
			!MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
				storage, MuiListviewDragStateField.StartX, out var startX) ||
			!MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
				storage, MuiListviewDragStateField.StartY, out var startY) ||
			!MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
				storage, MuiListviewDragStateField.LastX, out var lastX) ||
			!MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
				storage, MuiListviewDragStateField.LastY, out var lastY) ||
			!MuiListviewDragStateMemoryCodec.TryReadUInt32(ref platform,
				storage, MuiListviewDragStateField.Flags, out var flags)) return false;
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
