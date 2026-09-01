/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Named cursor for decimal String Integer64 text. The parser/stringifier carry
// only a guest text base, logical index, and validated span length; byte
// address formation, the 64 KiB ceiling, overflow checks, and map admission
// remain in this value-type adapter.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringInteger64TextByteCursor
{
	internal const uint MaximumLength = 65536;
	internal APTR Base;
	internal uint Index;
	internal uint Length;
}

internal static class MuiStringInteger64TextByteCursorCodec
{
	internal static bool TryReadAt<TPlatform>(ref TPlatform platform,
		APTR baseAddress, uint length, int index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (index < 0) return false;
		var cursor = default(MuiStringInteger64TextByteCursor);
		cursor.Base = baseAddress;
		cursor.Index = (uint)index;
		cursor.Length = length;
		return TryReadByte(ref platform, cursor, out value);
	}

	internal static bool TryGetByte<TPlatform>(ref TPlatform platform,
		MuiStringInteger64TextByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiStringInteger64TextByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryReadByte(ref platform, shared,
			out value);
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		MuiStringInteger64TextByteCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryWriteByte(ref platform, shared,
			value);
	}
}

// MorphOS QUAD is a signed 64-bit guest value.  Keep the wire representation
// as two named ULONGs so the String core never depends on a managed Int64
// object or runtime conversion helper.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiStringInteger64Value
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint HighOffset = 0;
	public const uint LowOffset = 4;
	public uint High;
	public uint Low;
}

internal enum MuiStringInteger64Field : byte
{
	High,
	Low,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringInteger64FieldCursor
{
	internal APTR Record;
	internal MuiStringInteger64Field Field;
}

// Struct-first guest-memory adapter for the MorphOS QUAD record. Arithmetic
// and semantic code use the named High/Low fields; this bounded seam owns the
// packed guest translation and rejects incomplete records.
internal static class MuiStringInteger64ValueMemoryCodec
{
	private static bool TryResolve(MuiStringInteger64Field field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringInteger64Field.High => MuiStringInteger64Value.HighOffset,
			MuiStringInteger64Field.Low => MuiStringInteger64Value.LowOffset,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteger64Field field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiStringInteger64Value.Size))
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiStringInteger64Value.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteger64Field field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteger64Field field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for existing typed cursor diagnostics.
internal static class MuiStringInteger64FieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringInteger64FieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStringInteger64ValueMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteger64Field field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStringInteger64ValueMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteger64Field field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiStringInteger64ValueMemoryCodec.TryWriteUInt32(ref platform, record, field,
		value);
}

// Sequential codec for the complete MorphOS QUAD record. The wire value is
// two declaration-ordered ULONGs; byte-preserving scalar helpers retain the
// full 32-bit range on the freestanding generic-interface path, including
// signed values with bit 31 set.
internal static class MuiStringInteger64ValueStructCodec
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool TryReadUlong<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
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
		var first = platform.ReadUInt8(firstAddress, 0);
		var second = platform.ReadUInt8(secondAddress, 0);
		var third = platform.ReadUInt8(thirdAddress, 0);
		var fourth = platform.ReadUInt8(fourthAddress, 0);
		value = ((uint)first << 24) | ((uint)second << 16) |
			((uint)third << 8) | fourth;
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool TryWriteUlong<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
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
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringInteger64Value value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringInteger64Value.Size, out var cursor) ||
			!TryReadUlong(ref platform, ref cursor, out var high) ||
			!TryReadUlong(ref platform, ref cursor, out var low) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.High = high;
		value.Low = low;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringInteger64Value value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiStringInteger64Value.Size, out var cursor) ||
			!TryWriteUlong(ref platform, ref cursor, value.High) ||
			!TryWriteUlong(ref platform, ref cursor, value.Low)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// The String attribute itself is a caller-facing pointer to the QUAD record.
// The live pointer is retained in this named state record after the value has
// been copied into the object's guest dataspace.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiStringInteger64State
{
	public APTR Value;
}

internal static class MuiStringInteger64StateAdmission
{
	// The structural domain is the complete 32-bit APTR space; mapping is a
	// live-memory property checked only when an owning object is available.
	internal static bool Validate(APTR value) => value.IsNull || value.IsNotNull;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull &&
		(value.IsNull || platform.IsMapped(value, MuiStringInteger64Value.Size));
}

internal static class MuiStringInteger64Codec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringInteger64Value value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringInteger64ValueStructCodec.TryRead(ref platform, address,
			out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringInteger64Value value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiStringInteger64ValueStructCodec.Write(ref platform, address, value);

	// Parse the bounded C string used by MUIA_String_Contents into a signed
	// QUAD.  The arithmetic is four 16-bit limbs: it is deliberately expressed
	// in terms of named ULONG fields rather than a managed Int64/UInt64 helper.
	internal static bool TryParse<TPlatform>(ref TPlatform platform, APTR source,
		out MuiStringInteger64Value value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (source.IsNull) return false;
		var index = 0;
		var negative = false;
		var sourceCursor = default(MuiStringInteger64TextByteCursor);
		sourceCursor.Base = source;
		sourceCursor.Length = 4096;
		sourceCursor.Index = 0;
		if (!MuiStringInteger64TextByteCursorCodec.TryReadByte(ref platform,
			sourceCursor, out var first)) return false;
		if (first == (byte)'-') { negative = true; index++; }
		else if (first == (byte)'+') index++;
		var digits = 0;
		var terminated = false;
		uint limb0 = 0;
		uint limb1 = 0;
		uint limb2 = 0;
		uint limb3 = 0;
		for (; index < 4096; index++)
		{
			sourceCursor.Index = (uint)index;
			if (!MuiStringInteger64TextByteCursorCodec.TryReadByte(ref platform,
				sourceCursor, out var ch)) return false;
			if (ch == 0) { terminated = true; break; }
			if (ch < (byte)'0' || ch > (byte)'9') return false;
			digits++;
			var carry = (uint)(ch - (byte)'0');
			var product = limb0 * 10u + carry;
			limb0 = product & 0xFFFFu;
			carry = product >> 16;
			product = limb1 * 10u + carry;
			limb1 = product & 0xFFFFu;
			carry = product >> 16;
			product = limb2 * 10u + carry;
			limb2 = product & 0xFFFFu;
			carry = product >> 16;
			product = limb3 * 10u + carry;
			limb3 = product & 0xFFFFu;
			if ((product >> 16) != 0) return false;
		}
		if (digits == 0 || !terminated) return false;
		var high = (limb3 << 16) | limb2;
		var low = (limb1 << 16) | limb0;
		// Positive QUADs have a clear sign bit.  A negative magnitude may use
		// exactly 0x80000000:00000000 (LONG_MIN), but no larger magnitude.
		if ((!negative && high > 0x7FFFFFFFu) ||
			(negative && (high > 0x80000000u ||
				(high == 0x80000000u && low != 0)))) return false;
		if (negative)
		{
			low = unchecked(~low + 1u);
			high = unchecked(~high + (low == 0 ? 1u : 0u));
		}
		value.High = high;
		value.Low = low;
		return true;
	}

	// Render a signed QUAD into an existing guest C string buffer.  Division by
	// ten is performed on four 16-bit limbs and the digits are reversed in place,
	// so this path allocates neither managed arrays nor runtime numeric objects.
	internal static int Stringify<TPlatform>(ref TPlatform platform, APTR destination,
		int capacity, MuiStringInteger64Value value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (destination.IsNull || capacity < 2 ||
			(uint)capacity > MuiStringInteger64TextByteCursor.MaximumLength ||
			!platform.IsMapped(destination, unchecked((uint)capacity))) return -1;
		var destinationCursor = default(MuiStringInteger64TextByteCursor);
		destinationCursor.Base = destination;
		destinationCursor.Length = unchecked((uint)capacity);
		var high = value.High;
		var low = value.Low;
		var negative = (high & 0x80000000u) != 0;
		if (negative)
		{
			low = unchecked(~low + 1u);
			high = unchecked(~high + (low == 0 ? 1u : 0u));
		}
		var count = 0;
		if (high == 0 && low == 0)
		{
			if (capacity < 2) return -1;
			destinationCursor.Index = 0;
			if (!MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
				destinationCursor, (byte)'0')) return -1;
			destinationCursor.Index = 1;
			if (!MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
				destinationCursor, 0)) return -1;
			return 1;
		}
		while (high != 0 || low != 0)
		{
			if (count >= capacity - (negative ? 2 : 1)) return -1;
			var part3 = high >> 16;
			var part2 = high & 0xFFFFu;
			var part1 = low >> 16;
			var part0 = low & 0xFFFFu;
			uint remainder = 0;
			var current = remainder * 65536u + part3;
			var quotient3 = current / 10u;
			remainder = current % 10u;
			current = remainder * 65536u + part2;
			var quotient2 = current / 10u;
			remainder = current % 10u;
			current = remainder * 65536u + part1;
			var quotient1 = current / 10u;
			remainder = current % 10u;
			current = remainder * 65536u + part0;
			var quotient0 = current / 10u;
			remainder = current % 10u;
			high = (quotient3 << 16) | quotient2;
			low = (quotient1 << 16) | quotient0;
			destinationCursor.Index = (uint)count;
			if (!MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
				destinationCursor, unchecked((byte)('0' + remainder)))) return -1;
			count++;
		}
		if (negative)
		{
			for (var index = count; index >= 0; index--)
			{
				destinationCursor.Index = (uint)index;
				if (!MuiStringInteger64TextByteCursorCodec.TryReadByte(ref platform,
					destinationCursor, out var digit)) return -1;
				destinationCursor.Index = (uint)(index + 1);
				if (!MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
					destinationCursor, digit)) return -1;
			}
			destinationCursor.Index = 0;
			if (!MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
				destinationCursor, (byte)'-')) return -1;
			count++;
		}
		var left = negative ? 1 : 0;
		var right = count - 1;
		for (; left < right; left++, right--)
		{
			destinationCursor.Index = (uint)left;
			if (!MuiStringInteger64TextByteCursorCodec.TryReadByte(ref platform,
				destinationCursor, out var leftValue)) return -1;
			destinationCursor.Index = (uint)right;
			if (!MuiStringInteger64TextByteCursorCodec.TryReadByte(ref platform,
				destinationCursor, out var rightValue)) return -1;
			destinationCursor.Index = (uint)left;
			if (!MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
				destinationCursor, rightValue)) return -1;
			destinationCursor.Index = (uint)right;
			if (!MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
				destinationCursor, leftValue)) return -1;
		}
		destinationCursor.Index = (uint)count;
		if (!MuiStringInteger64TextByteCursorCodec.TryWriteByte(ref platform,
			destinationCursor, 0)) return -1;
		return count;
	}
}
