/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The nm_Type byte is a fixed prefix of the packed MorphOS NewMenu record.
// Keep its semantic interpretation beside the record boundary so menu
// construction never has to repeat bit masks in consumer code.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNewMenuTypeRecord
{
	internal const uint Size = 1;
	internal const byte End = 0;
	internal const byte Title = 1;
	internal const byte Item = 2;
	internal const byte Sub = 3;
	internal const byte Ignore = 64;
	internal const byte Image = 128;

	internal byte Type;
}

internal enum MuiNewMenuEntryKind : byte
{
	Invalid,
	End,
	Title,
	Item,
	Sub,
	Ignored,
	ImageItem,
	ImageSub,
	ImageUnsupported,
}

internal static class MuiNewMenuTypeRecordCodec
{
	internal static bool TryReadType<TPlatform>(ref TPlatform platform,
		APTR address, out byte type)
		where TPlatform : struct, IMuiGuestMemory
	{
		type = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNewMenuTypeRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out type)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiNewMenuTypeRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadType(ref platform, address, out var type)) return false;
		value.Type = type;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNewMenuTypeRecord value)
	where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryClassify(byte type, out MuiNewMenuEntryKind kind)
	{
		// NM_IGNORE is a modifier and takes precedence over all other type
		// values, matching the MorphOS/GadTools traversal contract.
		if ((type & MuiNewMenuTypeRecord.Ignore) != 0)
		{
			kind = MuiNewMenuEntryKind.Ignored;
			return true;
		}
		if (type == MuiNewMenuTypeRecord.End)
		{
			kind = MuiNewMenuEntryKind.End;
			return true;
		}
		if (type == MuiNewMenuTypeRecord.Title)
		{
			kind = MuiNewMenuEntryKind.Title;
			return true;
		}
		if (type == MuiNewMenuTypeRecord.Item)
		{
			kind = MuiNewMenuEntryKind.Item;
			return true;
		}
		if (type == MuiNewMenuTypeRecord.Sub)
		{
			kind = MuiNewMenuEntryKind.Sub;
			return true;
		}
		if (type == (MuiNewMenuTypeRecord.Item | MuiNewMenuTypeRecord.Image))
		{
			kind = MuiNewMenuEntryKind.ImageItem;
			return true;
		}
		if (type == (MuiNewMenuTypeRecord.Sub | MuiNewMenuTypeRecord.Image))
		{
			kind = MuiNewMenuEntryKind.ImageSub;
			return true;
		}
		// Preserve the old validator's explicit image rejection for malformed
		// image combinations as well; callers must not reinterpret their label
		// field as a text pointer while probing an unsupported image entry.
		if ((type & MuiNewMenuTypeRecord.Image) != 0)
		{
			kind = MuiNewMenuEntryKind.ImageUnsupported;
			return true;
		}
		kind = MuiNewMenuEntryKind.Invalid;
		return false;
	}

	internal static bool TryClassify<TPlatform>(ref TPlatform platform,
		APTR address, out MuiNewMenuEntryKind kind)
		where TPlatform : struct, IMuiGuestMemory
	{
		kind = MuiNewMenuEntryKind.Invalid;
		return TryReadType(ref platform, address, out var type) &&
			TryClassify(type, out kind);
	}
}
