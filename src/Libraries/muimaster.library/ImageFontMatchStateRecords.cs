/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of Image.mui's initializer-only font-match policy.
// These values are MorphOS BOOL/ULONG scalars and do not require a managed
// font object or a host-side text representation.
public struct MuiImageFontMatchState
{
	public uint Match;
	public uint Height;
	public uint Width;
}

// Guest-resident scalar FontMatch policy. The optional FontMatchString pointer
// remains in MuiImageFontMatchStringStateRecord because it has independent
// caller-owned pointer validation and [IS.] mutation semantics.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageFontMatchStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4D494653u; // 'MIFS'

	internal uint Magic;
	internal uint Match;
	internal uint Height;
	internal uint Width;
}

internal enum MuiImageFontMatchStateField : byte
{
	Magic,
	Match,
	Height,
	Width,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageFontMatchStateFieldCursor
{
	internal APTR Record;
	internal MuiImageFontMatchStateField Field;
}

internal static class MuiImageFontMatchStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageFontMatchStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiImageFontMatchStateField.Magic => 0,
			MuiImageFontMatchStateField.Match => 4,
			MuiImageFontMatchStateField.Height => 8,
			MuiImageFontMatchStateField.Width => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageFontMatchStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiImageFontMatchStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageFontMatchStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageFontMatchStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiImageFontMatchStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageFontMatchStateRecord.Size) ||
			!MuiImageFontMatchStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiImageFontMatchStateField.Magic, out var magic) ||
			magic != MuiImageFontMatchStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiImageFontMatchStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageFontMatchStateField.Match, out value.Match) &&
			MuiImageFontMatchStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageFontMatchStateField.Height, out value.Height) &&
			MuiImageFontMatchStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageFontMatchStateField.Width, out value.Width);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageFontMatchStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageFontMatchStateRecord.Size) || value.Magic !=
			MuiImageFontMatchStateRecord.Cookie) return false;
		return MuiImageFontMatchStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiImageFontMatchStateField.Magic, value.Magic) &&
			MuiImageFontMatchStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiImageFontMatchStateField.Match, value.Match) &&
			MuiImageFontMatchStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiImageFontMatchStateField.Height, value.Height) &&
			MuiImageFontMatchStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiImageFontMatchStateField.Width, value.Width);
	}
}
