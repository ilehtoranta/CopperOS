/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_Image_Spec begins with a fixed kind byte and ':' separator. The
// remainder is a variable payload parsed by CommonControlCore.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct MuiImageSpecPrefixRecord
{
	internal const uint Size = 2;
	internal byte Kind;
	internal byte Separator;
}

// Named bounded cursor for the variable payload following an Image.mui
// `kind:` prefix. The parser carries a guest base, logical index, and span
// length; payload range, overflow, and mapped-byte checks remain in this
// value-type adapter.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageSpecByteCursor
{
	internal const uint MaximumLength = 512;
	internal APTR Base;
	internal uint Index;
	internal uint Length;
}

internal static class MuiImageSpecByteCursorCodec
{
	internal static bool TryGetRange<TPlatform>(ref TPlatform platform,
		MuiImageSpecByteCursor cursor, uint byteCount, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Length > MuiImageSpecByteCursor.MaximumLength) return false;
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Length;
		return MuiCStringByteCursorCodec.TryGetRange(ref platform, shared,
			byteCount, out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiImageSpecByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetRange(ref platform, cursor, 1, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}
}

internal static class MuiImageSpecPrefixRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiImageSpecPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageSpecPrefixRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var kind) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var separator)) return false;
		value.Kind = kind;
		value.Separator = separator;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiImageSpecPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiImageSpecPrefixRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Kind) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Separator)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageSpecPrefixRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);
}
