/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Named bounded byte view for requester format strings and %s source text.
// The formatter carries logical length separately from guest addresses; this
// adapter centralizes index/overflow/mapping checks for the MC68000 path.
[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterFormatByteCursor
{
	internal const uint MaximumLength = MuiRequesterPayloadCore.MaximumStringLength;
	internal APTR Base;
	internal uint Index;
	internal uint Length;
}

internal static class MuiRequesterFormatByteCursorCodec
{
	internal static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR baseAddress, uint length, out MuiRequesterFormatByteCursor cursor)
		where TPlatform : struct, IMuiGuestMemory
	{
		cursor = default;
		if (baseAddress.IsNull || length == 0 ||
			length > MuiRequesterFormatByteCursor.MaximumLength)
			return false;
		cursor.Base = baseAddress;
		cursor.Length = length;
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRequesterFormatByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Length > MuiRequesterFormatByteCursor.MaximumLength)
			return false;
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiRequesterFormatByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}
}

// Named bounded destination cursor for requester printf materialization.
// Append operations carry the guest base, logical output index, and capacity;
// range, overflow, and mapped-byte checks stay in this adapter.
[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterOutputByteCursor
{
	internal const uint MaximumLength = MuiRequesterPayloadCore.MaximumStringLength + 1;
	internal APTR Base;
	internal uint Index;
	internal uint Capacity;
}

internal static class MuiRequesterOutputByteCursorCodec
{
	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		MuiRequesterOutputByteCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (cursor.Capacity > MuiRequesterOutputByteCursor.MaximumLength)
			return false;
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Capacity;
		return MuiCStringByteCursorCodec.TryWriteByte(ref platform, shared,
			value);
	}
}

// Requester formatting parameters are caller-owned ULONG values. Keep each
// four-byte element as a named wire slot so formatting never repeats the
// parameter vector's raw value offset at the conversion call sites.
[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterParameterSlot
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint ValueOffset = 0;
	internal uint Value;
}

internal enum MuiRequesterParameterSlotField : byte
{
	Value,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterParameterSlotFieldCursor
{
	internal APTR Record;
	internal MuiRequesterParameterSlotField Field;
}

internal static class MuiRequesterParameterSlotFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRequesterParameterSlotFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiRequesterParameterSlotMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRequesterParameterSlotField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiRequesterParameterSlotMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRequesterParameterSlotField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiRequesterParameterSlotMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

// Struct-first guest-memory adapter for one caller-owned ULONG parameter
// slot. The named slot owns the wire position; this bounded adapter is the
// only place that turns it into a guest address.
internal static class MuiRequesterParameterSlotMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiRequesterParameterSlotField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field != MuiRequesterParameterSlotField.Value || record.IsNull ||
			!platform.IsMapped(record, MuiRequesterParameterSlot.Size)) return false;
		address = APTR.FromPointer(record.Raw +
			MuiRequesterParameterSlot.ValueOffset);
		return platform.IsMapped(address, MuiRequesterParameterSlot.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRequesterParameterSlotField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRequesterParameterSlotField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiRequesterParameterSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRequesterParameterSlot slot)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiRequesterParameterSlotStructCodec.TryRead(ref platform, address,
			out slot);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRequesterParameterSlot slot)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiRequesterParameterSlotStructCodec.Write(ref platform, address, slot);
}

// Sequential codec for one caller-owned ULONG parameter slot. The slot is a
// named vector element; the legacy field adapter is retained only for
// diagnostics while formatting consumes the complete value record here.
internal static class MuiRequesterParameterSlotStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR address, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiRequesterParameterSlot.Size, out var cursor)) return false;
		// Keep complete-record admission in the sequential cursor. Read the ULONG
		// as bytes because the freestanding compiler has a known generic-interface
		// ABI lowering defect for 32-bit values with bit 31 set; byte scalars keep
		// the full C ULONG range without changing the record representation.
		byte first;
		byte second;
		byte third;
		byte fourth;
		APTR firstAddress;
		APTR secondAddress;
		APTR thirdAddress;
		APTR fourthAddress;
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 1,
			out firstAddress) || !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, 1, out secondAddress) || !MuiGuestStructCursor.TryTake(
			ref platform, ref cursor, 1, out thirdAddress) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 1,
				out fourthAddress)) return false;
		first = platform.ReadUInt8(firstAddress, 0);
		second = platform.ReadUInt8(secondAddress, 0);
		third = platform.ReadUInt8(thirdAddress, 0);
		fourth = platform.ReadUInt8(fourthAddress, 0);
		value = ((uint)first << 24) | ((uint)second << 16) |
			((uint)third << 8) | fourth;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRequesterParameterSlot slot)
		where TPlatform : struct, IMuiGuestMemory
	{
		slot = default;
		uint rawValue;
		if (!TryReadValue(ref platform, address, out rawValue)) return false;
		slot.Value = rawValue;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
		APTR address, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiRequesterParameterSlot.Size, out var cursor)) return false;
		APTR firstAddress;
		APTR secondAddress;
		APTR thirdAddress;
		APTR fourthAddress;
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 1,
			out firstAddress) || !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, 1, out secondAddress) || !MuiGuestStructCursor.TryTake(
			ref platform, ref cursor, 1, out thirdAddress) ||
			!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 1,
				out fourthAddress)) return false;
		platform.WriteUInt8(firstAddress, 0, (byte)(value >> 24));
		platform.WriteUInt8(secondAddress, 0, (byte)(value >> 16));
		platform.WriteUInt8(thirdAddress, 0, (byte)(value >> 8));
		platform.WriteUInt8(fourthAddress, 0, (byte)value);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRequesterParameterSlot slot)
		where TPlatform : struct, IMuiGuestMemory
		=> TryWriteValue(ref platform, address, slot.Value);
}

// Named cursor for the caller-owned ULONG vector consumed by requester format
// conversions. The bounded index and slot size stay with the vector view so
// formatting code never repeats raw byte arithmetic.
[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterParameterCursor
{
	internal const uint EntrySize = MuiRequesterParameterSlot.Size;
	internal const uint MaximumEntries =
		MuiRequesterPayloadCore.MaximumFormatParameters;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiRequesterParameterCursorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiRequesterParameterCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiRequesterParameterVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	// Formatter consumers exchange the complete named parameter slot through
	// this cursor; the one-ULONG scalar remains confined to the slot codec for
	// freestanding native ABI stability.
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		MuiRequesterParameterCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiRequesterParameterSlotStructCodec.TryReadValue(ref platform,
			address, out value);
	}

	internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
		MuiRequesterParameterCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiRequesterParameterSlotStructCodec.TryWriteValue(ref platform,
			address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiRequesterParameterCursor cursor,
		out MuiRequesterParameterSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadValue(ref platform, cursor, out var rawValue)) return false;
		value.Value = rawValue;
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiRequesterParameterCursor cursor, MuiRequesterParameterSlot value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryWriteValue(ref platform, cursor, value.Value);
}

// Struct-first guest-memory adapter for the caller-owned requester ULONG
// vector. The MorphOS parameter bound and complete named slot admission stay
// in this ABI boundary; formatter code receives only validated slots.
internal static class MuiRequesterParameterVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >=
			MuiRequesterParameterCursor.MaximumEntries || index >
			(uint.MaxValue - vector.Raw) / MuiRequesterParameterSlot.Size)
			return false;
		var offset = index * MuiRequesterParameterSlot.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiRequesterParameterSlot.Size);
	}
}

// Production bridge for the caller-owned requester parameter vector. Index
// arithmetic remains inside the bounded vector adapter; formatting consumes a
// semantic ULONG or its complete named slot rather than exposing slot
// addresses at each conversion site.
internal static class MuiRequesterParameterVectorCodec
{
	internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiRequesterParameterVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out var address)) return false;
		return MuiRequesterParameterSlotStructCodec.TryReadValue(ref platform,
			address, out value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR vector,
		uint index, out MuiRequesterParameterSlot value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadValue(ref platform, vector, index, out var rawValue))
			return false;
		value.Value = rawValue;
		return true;
	}

	internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiRequesterParameterVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out var address)) return false;
		return MuiRequesterParameterSlotStructCodec.TryWriteValue(ref platform,
			address, value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR vector,
		uint index, MuiRequesterParameterSlot value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryWriteValue(ref platform, vector, index, value.Value);
}

// Bounded, guest-resident execution of the printf-shaped format payload used
// by MUI_RequestA and MUI_RequestObjectA. The requester API carries a ULONG
// vector rather than a managed argument list, so this formatter consumes that
// vector directly and materializes one temporary C string for the synchronous
// platform call. It deliberately supports the integer/string subset that can
// be represented without locale, floating point, callbacks, or a host runtime.
// Unsupported conversions fail before the platform capability is entered.
public static class MuiRequesterFormatCore
{
	public const uint MaximumOutputLength = MuiRequesterPayloadCore.MaximumStringLength;

	private const uint FlagLeft = 1;
	private const uint FlagPlus = 2;
	private const uint FlagSpace = 4;
	private const uint FlagZero = 8;
	private const uint FlagQuote = 16;

	public static bool TryMaterialize<TPlatform>(ref TPlatform platform,
		APTR format, APTR parameters, out APTR materialized,
		out uint allocationSize) where TPlatform : struct, IMuiHeadlessPlatform
	{
		materialized = format;
		allocationSize = 0;
		if (format.IsNull) return true;
		if (!CStringCodec.TryReadLength(ref platform, format,
			MuiRequesterPayloadCore.MaximumStringLength, out var length))
			return false;

		if (!ContainsConversion(ref platform, format, length)) return true;
		var output = MuiHeadlessMemory.Allocate(ref platform,
			MaximumOutputLength + 1);
		if (output.IsNull) return false;
		uint outputLength = 0;
		uint parameterIndex = 0;
		if (!FormatInto(ref platform, format, length, parameters, ref parameterIndex,
			output, ref outputLength))
		{
			platform.Free(output, MaximumOutputLength + 1);
			return false;
		}
		materialized = output;
		allocationSize = MaximumOutputLength + 1;
		return true;
	}

	private static bool ContainsConversion<TPlatform>(ref TPlatform platform,
		APTR format, uint length) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var index = 0u;
		while (index < length)
		{
			if (Read(ref platform, format, index++) != (byte)'%') continue;
			if (index >= length) return true;
			if (Read(ref platform, format, index) == (byte)'%')
			{
				index++;
				continue;
			}
			return true;
		}
		return false;
	}

	private static bool FormatInto<TPlatform>(ref TPlatform platform, APTR format,
		uint length, APTR parameters, ref uint parameterIndex, APTR output,
		ref uint outputLength) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var index = 0u;
		while (index < length)
		{
			var value = Read(ref platform, format, index++);
			if (value != (byte)'%')
			{
				if (!Append(ref platform, output, ref outputLength, value)) return false;
				continue;
			}
			if (index >= length) return false;
			if (Read(ref platform, format, index) == (byte)'%')
			{
				index++;
				if (!Append(ref platform, output, ref outputLength, (byte)'%'))
					return false;
				continue;
			}

			uint flags = 0;
			while (index < length)
			{
				var flag = Read(ref platform, format, index);
				var bit = flag == (byte)'-' ? FlagLeft :
					flag == (byte)'+' ? FlagPlus :
					flag == (byte)' ' ? FlagSpace :
					flag == (byte)'0' ? FlagZero :
					flag == (byte)'\'' ? FlagQuote : 0u;
				if (bit == 0) break;
				flags |= bit;
				index++;
			}

			uint width = 0;
			if (index < length && Read(ref platform, format, index) == (byte)'*')
			{
				if (!ReadParameter(ref platform, parameters, ref parameterIndex,
					out var widthValue)) return false;
				index++;
				if ((widthValue & 0x80000000u) != 0)
				{
					flags |= FlagLeft;
					width = 0u - widthValue;
				}
				else width = widthValue;
			}
			else if (!ReadDigits(ref platform, format, length, ref index, out width))
				return false;
			if (width > MaximumOutputLength) return false;

			bool hasPrecision = false;
			uint precision = 0;
			if (index < length && Read(ref platform, format, index) == (byte)'.')
			{
				hasPrecision = true;
				index++;
				if (index < length && Read(ref platform, format, index) == (byte)'*')
				{
					if (!ReadParameter(ref platform, parameters, ref parameterIndex,
						out var precisionValue)) return false;
					index++;
					if ((precisionValue & 0x80000000u) != 0)
						hasPrecision = false;
					else precision = precisionValue;
				}
				else if (!ReadDigits(ref platform, format, length, ref index,
					out precision)) return false;
				if (precision > MaximumOutputLength) return false;
			}

			// h, hh, l, ll, j, z, t and L are ABI spelling, not a second
			// storage type: MUI's parameter vector is ULONG based. Consume them
			// so the supported conversion has the same wire shape.
			if (index < length && IsLengthPrefix(Read(ref platform, format, index)))
			{
				var lengthPrefix = Read(ref platform, format, index++);
				if (index < length && ((lengthPrefix == (byte)'h' &&
					Read(ref platform, format, index) == (byte)'h') ||
					(lengthPrefix == (byte)'l' &&
					Read(ref platform, format, index) == (byte)'l')))
					index++;
			}
			if (index >= length) return false;
			var conversion = Read(ref platform, format, index++);
			if (!IsSupported(conversion)) return false;

			if (conversion == (byte)'s')
			{
				if (!ReadParameter(ref platform, parameters, ref parameterIndex,
					out var stringRaw) || stringRaw == 0) return false;
				var stringAddress = APTR.FromPointer(stringRaw);
				if (!CStringCodec.TryReadLength(ref platform, stringAddress,
					MuiRequesterPayloadCore.MaximumStringLength, out var stringLength))
					return false;
				if (hasPrecision && stringLength > precision) stringLength = precision;
				if (!AppendString(ref platform, stringAddress, stringLength, output,
					ref outputLength, width, (flags & FlagLeft) != 0)) return false;
				continue;
			}

			if (!ReadParameter(ref platform, parameters, ref parameterIndex,
				out var raw)) return false;
			if (conversion == (byte)'c')
			{
				if (!AppendPadding(ref platform, output, ref outputLength,
					width, 1, (flags & FlagLeft) != 0)) return false;
				if (!Append(ref platform, output, ref outputLength,
					unchecked((byte)raw))) return false;
				if ((flags & FlagLeft) != 0 && width > 1 &&
					!AppendSpaces(ref platform, output, ref outputLength, width - 1))
					return false;
				continue;
			}

			var radix = conversion == (byte)'x' || conversion == (byte)'X' ||
				conversion == (byte)'p' ? 16u :
				conversion == (byte)'b' ? 2u : 10u;
			var upper = conversion == (byte)'X';
			var signed = conversion == (byte)'d' || conversion == (byte)'i';
			var negative = signed && (raw & 0x80000000u) != 0;
			var magnitude = negative ? 0u - raw : raw;
			var digits = DigitCount(magnitude, radix);
			if (hasPrecision && precision > digits) digits = precision;
			var prefix = negative || (signed && (flags & FlagPlus) != 0) ||
				(signed && (flags & FlagSpace) != 0) ? 1u : 0u;
			if (!AppendNumeric(ref platform, output, ref outputLength, magnitude,
				radix, upper, digits, width, prefix, flags, negative)) return false;
		}
		return Append(ref platform, output, ref outputLength, 0);
	}

	private static bool ReadDigits<TPlatform>(ref TPlatform platform, APTR format,
		uint length, ref uint index, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		while (index < length)
		{
			var digit = Read(ref platform, format, index);
			if (digit < (byte)'0' || digit > (byte)'9') break;
			var next = value * 10u + (uint)(digit - (byte)'0');
			if (next < value || next > MaximumOutputLength) return false;
			value = next;
			index++;
		}
		return true;
	}

	private static bool ReadParameter<TPlatform>(ref TPlatform platform,
		APTR parameters, ref uint index, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (parameters.IsNull || index >= MuiRequesterPayloadCore.MaximumFormatParameters)
			return false;
		var cursor = default(MuiRequesterParameterCursor);
		cursor.Base = parameters;
		cursor.Index = index;
		if (!MuiRequesterParameterCursorCodec.TryReadValue(ref platform, cursor,
			out value)) return false;
		index++;
		return true;
	}

	private static bool AppendString<TPlatform>(ref TPlatform platform, APTR source,
		uint length, APTR output, ref uint outputLength, uint width, bool left)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!AppendPadding(ref platform, output, ref outputLength, width, length,
			left)) return false;
		var sourceCursor = default(MuiRequesterFormatByteCursor);
		if (length != 0 && !MuiRequesterFormatByteCursorCodec.TryCreate(
			ref platform, source, length, out sourceCursor)) return false;
		for (var index = 0u; index < length; index++)
		{
			sourceCursor.Index = index;
			if (!MuiRequesterFormatByteCursorCodec.TryReadByte(ref platform,
				sourceCursor, out var value) ||
				!Append(ref platform, output, ref outputLength, value)) return false;
		}
		return !left || width <= length || AppendSpaces(ref platform, output,
			ref outputLength, width - length);
	}

	private static bool AppendNumeric<TPlatform>(ref TPlatform platform, APTR output,
		ref uint outputLength, uint value, uint radix, bool upper, uint digits,
		uint width, uint prefix, uint flags, bool negative)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var zeroPad = (flags & FlagZero) != 0 && (flags & FlagLeft) == 0;
		var total = digits + prefix;
		if (total > MaximumOutputLength || width > MaximumOutputLength) return false;
		if (!zeroPad && (flags & FlagLeft) == 0 && width > total &&
			!AppendSpaces(ref platform, output, ref outputLength, width - total)) return false;
		if (prefix != 0)
		{
			var sign = negative ? (byte)'-' : (flags & FlagPlus) != 0 ? (byte)'+' :
				(byte)' ';
			if (!Append(ref platform, output, ref outputLength, sign)) return false;
		}
		if (zeroPad && width > total &&
			!AppendZeros(ref platform, output, ref outputLength, width - total)) return false;
		var divisor = 1u;
		var power = digits;
		while (power > 1)
		{
			divisor *= radix;
			power--;
		}
		var remaining = digits;
		while (remaining != 0)
		{
			var digit = (value / divisor) % radix;
			var character = digit < 10 ? (byte)('0' + digit) :
				(byte)((upper ? 'A' : 'a') + digit - 10);
			if (!Append(ref platform, output, ref outputLength, character)) return false;
			remaining--;
			if (divisor == 1) break;
			divisor /= radix;
		}
		if ((flags & FlagLeft) != 0 && width > total &&
			!AppendSpaces(ref platform, output, ref outputLength, width - total)) return false;
		return true;
	}

	private static uint DigitCount(uint value, uint radix)
	{
		var count = 1u;
		var remaining = value;
		while (remaining >= radix)
		{
			remaining /= radix;
			count++;
		}
		return count;
	}

	private static bool AppendPadding<TPlatform>(ref TPlatform platform, APTR output,
		ref uint outputLength, uint width, uint content,
		bool left) where TPlatform : struct, IMuiHeadlessPlatform =>
		left || width <= content || AppendSpaces(ref platform, output,
			ref outputLength, width - content);

	private static bool AppendSpaces<TPlatform>(ref TPlatform platform, APTR output,
		ref uint outputLength, uint count)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		AppendRepeated(ref platform, output, ref outputLength, count, (byte)' ');

	private static bool AppendZeros<TPlatform>(ref TPlatform platform, APTR output,
		ref uint outputLength, uint count)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		AppendRepeated(ref platform, output, ref outputLength, count, (byte)'0');

	private static bool AppendRepeated<TPlatform>(ref TPlatform platform, APTR output,
		ref uint outputLength, uint count, byte value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var remaining = count;
		while (remaining != 0)
		{
			if (!Append(ref platform, output, ref outputLength, value)) return false;
			remaining--;
		}
		return true;
	}

	private static bool Append<TPlatform>(ref TPlatform platform, APTR output,
		ref uint outputLength, byte value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (outputLength >= MaximumOutputLength) return false;
		var cursor = default(MuiRequesterOutputByteCursor);
		cursor.Base = output;
		cursor.Index = outputLength;
		cursor.Capacity = MaximumOutputLength + 1;
		if (!MuiRequesterOutputByteCursorCodec.TryWriteByte(ref platform, cursor,
			value)) return false;
		outputLength++;
		return true;
	}

	private static byte Read<TPlatform>(ref TPlatform platform, APTR format,
		uint index) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cursor = default(MuiRequesterFormatByteCursor);
		cursor.Base = format;
		cursor.Length = MuiRequesterFormatByteCursor.MaximumLength;
		cursor.Index = index;
		return MuiRequesterFormatByteCursorCodec.TryReadByte(ref platform, cursor,
			out var value) ? value : (byte)0;
	}

	private static bool IsLengthPrefix(byte value) => value == (byte)'h' ||
		value == (byte)'l' || value == (byte)'j' || value == (byte)'z' ||
		value == (byte)'t' || value == (byte)'L';

	private static bool IsSupported(byte value) => value == (byte)'b' ||
		value == (byte)'c' || value == (byte)'d' || value == (byte)'i' ||
		value == (byte)'p' || value == (byte)'s' || value == (byte)'u' ||
		value == (byte)'x' || value == (byte)'X';
}
