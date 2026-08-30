/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

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
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringInteger64Value.Size)) return false;
		if (!MuiStringInteger64ValueMemoryCodec.TryReadUInt32(ref platform,
			address, MuiStringInteger64Field.High, out value.High) ||
			!MuiStringInteger64ValueMemoryCodec.TryReadUInt32(ref platform, address,
				MuiStringInteger64Field.Low, out value.Low)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringInteger64Value value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringInteger64Value.Size)) return false;
		return MuiStringInteger64ValueMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiStringInteger64Field.High, value.High) &&
			MuiStringInteger64ValueMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiStringInteger64Field.Low, value.Low);
	}

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
		if (!platform.IsMapped(source, 1)) return false;
		var first = platform.ReadUInt8(source, 0);
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
			if (!platform.IsMapped(source, unchecked((uint)index + 1)))
				return false;
			var ch = platform.ReadUInt8(source, index);
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
			!platform.IsMapped(destination, unchecked((uint)capacity))) return -1;
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
			platform.WriteUInt8(destination, 0, (byte)'0');
			platform.WriteUInt8(destination, 1, 0);
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
			platform.WriteUInt8(destination, count++, unchecked((byte)('0' + remainder)));
		}
		if (negative)
		{
			for (var index = count; index >= 0; index--)
				platform.WriteUInt8(destination, index + 1,
					platform.ReadUInt8(destination, index));
			platform.WriteUInt8(destination, 0, (byte)'-');
			count++;
		}
		var left = negative ? 1 : 0;
		var right = count - 1;
		for (; left < right; left++, right--)
		{
			var leftValue = platform.ReadUInt8(destination, left);
			var rightValue = platform.ReadUInt8(destination, right);
			platform.WriteUInt8(destination, left, rightValue);
			platform.WriteUInt8(destination, right, leftValue);
		}
		platform.WriteUInt8(destination, count, 0);
		return count;
	}
}
