/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Fixed Misc-specialist class ids are represented by one packed record. The
// largest MorphOS id is 21 bytes; shorter ids use only the leading words and
// the codec composes their final partial word without exposing offsets to the
// classifier.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiMiscSpecialistClassNameRecord
{
	internal const uint MaxSize = 21;

	internal uint Word0;
	internal uint Word1;
	internal uint Word2;
	internal uint Word3;
	internal uint Word4;
	internal byte Terminator;
}

internal static class MuiMiscSpecialistClassNameRecordCodec
{
	private static bool IsSupportedSize(uint size) =>
		size == 10 || size == 13 || size == 14 || size == 16 || size == 21;

	internal static bool TryMatch<TPlatform>(ref TPlatform platform, APTR address,
		uint size, uint word0, uint word1, uint word2, uint word3, uint word4)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, size, out var value)) return false;
		if (value.Word0 != word0 || value.Word1 != word1 || value.Word2 != word2)
			return false;
		switch (size)
		{
			case 10:
				return (value.Word2 & 0xFFFF0000u) == (word2 & 0xFFFF0000u);
			case 13:
				return value.Word3 == word3;
			case 14:
				return value.Word3 == word3;
			case 16:
				return value.Word3 == word3;
			case 21:
				return value.Word3 == word3 && value.Word4 == word4 &&
					value.Terminator == 0;
			default:
				return false;
		}
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, uint size, out MuiMiscSpecialistClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!IsSupportedSize(size) || !MuiGuestStructCursor.TryCreate(ref platform,
			address, size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Word1)) return false;
		switch (size)
		{
			case 10:
				if (!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
					out var panel0) ||
					!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
						out var panel1)) return false;
				value.Word2 = ((uint)panel0 << 24) | ((uint)panel1 << 16);
				break;
			case 13:
				if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word2) ||
					!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
						out var partial0)) return false;
				value.Word3 = (uint)partial0 << 24;
				break;
			case 14:
				if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word2) ||
					!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
						out var partial1) ||
					!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
						out var partial2)) return false;
				value.Word3 = ((uint)partial1 << 24) | ((uint)partial2 << 16);
				break;
			case 16:
				if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word2) ||
					!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
						out value.Word3)) return false;
				break;
			case 21:
				if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word2) ||
					!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
						out value.Word3) ||
					!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
						out value.Word4) ||
					!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
						out value.Terminator)) return false;
				break;
			default:
				return false;
		}
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, uint size, MuiMiscSpecialistClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsSupportedSize(size) || !MuiGuestStructCursor.TryCreate(ref platform,
			address, size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word0) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word1)) return false;
		switch (size)
		{
			case 10:
				if (!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
					(byte)(value.Word2 >> 24)) ||
					!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
						(byte)(value.Word2 >> 16))) return false;
				break;
			case 13:
				if (!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
					value.Word2) ||
					!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
						(byte)(value.Word3 >> 24))) return false;
				break;
			case 14:
				if (!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
					value.Word2) ||
					!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
						(byte)(value.Word3 >> 24)) ||
					!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
						(byte)(value.Word3 >> 16))) return false;
				break;
			case 16:
				if (!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
					value.Word2) ||
					!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
						value.Word3)) return false;
				break;
			case 21:
				if (!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
					value.Word2) ||
					!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
						value.Word3) ||
					!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
						value.Word4) ||
					!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
						value.Terminator)) return false;
				break;
			default:
				return false;
		}
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		uint size, out MuiMiscSpecialistClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, size, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		uint size, MuiMiscSpecialistClassNameRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, size, value);
}
