/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Named bounded byte view for requester gadget and format strings.  The
// payload validator uses the same adapter for each caller-owned text span so
// address arithmetic and mapped-byte admission are not repeated inline.
[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiRequesterPayloadByteCursor
{
	internal const uint MaximumLength = 4096;
	internal APTR Base;
	internal uint Index;
	internal uint Length;
}

internal static class MuiRequesterPayloadByteCursorCodec
{
	internal static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR baseAddress, uint length, out MuiRequesterPayloadByteCursor cursor)
		where TPlatform : struct, IMuiGuestMemory
	{
		cursor = default;
		if (baseAddress.IsNull || length == 0 ||
			length > MuiRequesterPayloadByteCursor.MaximumLength)
			return false;
		cursor.Base = baseAddress;
		cursor.Length = length;
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRequesterPayloadByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Length > MuiRequesterPayloadByteCursor.MaximumLength)
			return false;
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiRequesterPayloadByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}
}

// Bounded validation for the caller-owned payload of MUI_RequestA and
// MUI_RequestObjectA. The service verifies C-string termination in guest
// memory, measures the gadget alternatives separated by '|', and checks the
// printf-style conversion arity against the caller's ULONG vector. It does not
// copy or render any caller data.
public static class MuiRequesterPayloadCore
{
	public const uint MaximumStringLength = 4096;
	public const uint MaximumFormatParameters = 2048;

	public static bool Validate<TPlatform>(ref TPlatform platform, APTR title,
		APTR gadgets, APTR format, APTR parameters)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint ignored;
		return ValidateCString(ref platform, title) &&
			TryGetGadgetCount(ref platform, gadgets, out ignored) &&
			TryGetFormatParameterCount(ref platform, format, out var required) &&
			ValidateParameterVector(ref platform, parameters, required);
	}

	// A null gadget string describes an empty alternative set. An empty string
	// has zero alternatives; every nonempty segment contributes one result.
	public static bool TryGetGadgetCount<TPlatform>(ref TPlatform platform,
		APTR gadgets, out uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		count = 0;
		if (gadgets.IsNull) return true;
		uint length;
		if (!CStringCodec.TryReadLength(ref platform, gadgets,
			MaximumStringLength, out length)) return false;
		if (length == 0) return true;

		count = 1;
		if (!MuiRequesterPayloadByteCursorCodec.TryCreate(ref platform, gadgets,
			length, out var cursor)) return false;
		for (var index = 0u; index < length; index++)
		{
			cursor.Index = index;
			if (!MuiRequesterPayloadByteCursorCodec.TryReadByte(ref platform,
				cursor, out var value)) return false;
			if (value == (byte)'|') count++;
		}
		return true;
	}

	// Parses the bounded printf-style grammar used by the requester. A
	// conversion consumes one ULONG; '*' width and precision consume one each.
	// %% is literal and consumes none. The conversion letter is left opaque so
	// MorphOS-specific text conversions are not rejected at this boundary.
	public static bool TryGetFormatParameterCount<TPlatform>(
		ref TPlatform platform, APTR format, out uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		count = 0;
		if (format.IsNull) return true;
		uint length;
		if (!CStringCodec.TryReadLength(ref platform, format,
			MaximumStringLength, out length)) return false;

		var index = 0u;
		while (index < length)
		{
			if (ReadFormatByte(ref platform, format, index) != (byte)'%')
			{
				index++;
				continue;
			}
			index++;
			if (index >= length) return false;
			var next = ReadFormatByte(ref platform, format, index);
			if (next == (byte)'%')
			{
				index++;
				continue;
			}

			while (index < length && IsFlag(
				ReadFormatByte(ref platform, format, index))) index++;
			if (index < length && ReadFormatByte(ref platform, format, index) ==
				(byte)'*')
			{
				if (!AddParameter(ref count)) return false;
				index++;
			}
			else
			{
				while (index < length && IsDigit(
					ReadFormatByte(ref platform, format, index))) index++;
			}

			if (index < length && ReadFormatByte(ref platform, format, index) ==
				(byte)'.')
			{
				index++;
				if (index < length && ReadFormatByte(ref platform, format, index) ==
					(byte)'*')
				{
					if (!AddParameter(ref count)) return false;
					index++;
				}
				else
				{
					while (index < length && IsDigit(
						ReadFormatByte(ref platform, format, index))) index++;
				}
			}

			if (index < length && IsLengthPrefix(
				ReadFormatByte(ref platform, format, index)))
			{
				var prefix = ReadFormatByte(ref platform, format, index++);
				if (index < length && ((prefix == (byte)'h' &&
					ReadFormatByte(ref platform, format, index) == (byte)'h') ||
					(prefix == (byte)'l' && ReadFormatByte(ref platform, format,
						index) == (byte)'l')))
					index++;
			}

			if (index >= length || !IsConversionLetter(
				ReadFormatByte(ref platform, format, index))) return false;
			if (!AddParameter(ref count)) return false;
			index++;
		}
		return true;
	}

	private static bool ValidateCString<TPlatform>(ref TPlatform platform,
		APTR value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value.IsNull) return true;
		uint length;
		return CStringCodec.TryReadLength(ref platform, value,
			MaximumStringLength, out length);
	}

	private static bool ValidateParameterVector<TPlatform>(
		ref TPlatform platform, APTR parameters, uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (count == 0) return true;
		if (parameters.IsNull || (parameters.Raw & 1u) != 0) return false;
		if (count > 0x3FFFFFFFu) return false;
		var bytes = count << 2;
		return parameters.Raw <= uint.MaxValue - bytes &&
			platform.IsMapped(parameters, bytes);
	}

	private static bool AddParameter(ref uint count)
	{
		if (count >= MaximumFormatParameters) return false;
		count++;
		return true;
	}

	private static byte ReadFormatByte<TPlatform>(ref TPlatform platform,
		APTR format, uint index)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cursor = default(MuiRequesterPayloadByteCursor);
		cursor.Base = format;
		cursor.Length = MuiRequesterPayloadByteCursor.MaximumLength;
		cursor.Index = index;
		return MuiRequesterPayloadByteCursorCodec.TryReadByte(ref platform, cursor,
			out var value) ? value : (byte)0;
	}

	private static bool IsDigit(byte value) => value >= (byte)'0' &&
		value <= (byte)'9';

	private static bool IsFlag(byte value) => value == (byte)'-' ||
		value == (byte)'+' || value == (byte)' ' || value == (byte)'#' ||
		value == (byte)'0' || value == (byte)'\'';

	private static bool IsLengthPrefix(byte value) => value == (byte)'h' ||
		value == (byte)'l' || value == (byte)'j' || value == (byte)'z' ||
		value == (byte)'t' || value == (byte)'L';

	private static bool IsConversionLetter(byte value) =>
		(value >= (byte)'A' && value <= (byte)'Z') ||
		(value >= (byte)'a' && value <= (byte)'z');
}
