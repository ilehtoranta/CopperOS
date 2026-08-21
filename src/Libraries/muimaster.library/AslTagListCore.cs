/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAslTagItemRecord
{
	internal const uint Size = 8;
	internal uint Tag;
	internal uint Data;
}

internal enum MuiAslTagItemField : byte
{
	Tag,
	Data,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAslTagItemFieldCursor
{
	internal APTR Record;
	internal MuiAslTagItemField Field;
}

internal static class MuiAslTagItemFieldCursorCodec
{
	private static bool TryResolve(MuiAslTagItemField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAslTagItemField.Tag:
				offset = 0;
				break;
			case MuiAslTagItemField.Data:
				offset = 4;
				break;
			default:
				offset = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAslTagItemFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			(cursor.Record.Raw & 1u) != 0 ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslTagItemField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAslTagItemFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslTagItemField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAslTagItemFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAslTagItemCursor
{
	internal const uint EntrySize = MuiAslTagItemRecord.Size;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiAslTagItemVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiAslTagItemCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiAslTagItemCursor.EntrySize) return false;
		var offset = cursor.Index * MuiAslTagItemCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address, MuiAslTagItemCursor.EntrySize);
	}

	internal static bool TryAdvance(ref MuiAslTagItemCursor cursor,
		uint items)
	{
		if (items == 0 || items > uint.MaxValue /
			MuiAslTagItemCursor.EntrySize || cursor.Index >
			uint.MaxValue - items) return false;
		cursor.Index += items;
		return true;
	}
}

// Central codec for the standard 8-byte TagItem guest record. The walker uses
// named Tag/Data fields; only this adapter knows the packed wire offsets.
internal static class MuiAslTagItemCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAslTagItemRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || (address.Raw & 1u) != 0 ||
			address.Raw > uint.MaxValue - MuiAslTagItemRecord.Size ||
			!platform.IsMapped(address, MuiAslTagItemRecord.Size)) return false;
		return MuiAslTagItemFieldCursorCodec.TryRead(ref platform, address,
			MuiAslTagItemField.Tag, out record.Tag) &&
			MuiAslTagItemFieldCursorCodec.TryRead(ref platform, address,
				MuiAslTagItemField.Data, out record.Data);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAslTagItemRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || (address.Raw & 1u) != 0 ||
			address.Raw > uint.MaxValue - MuiAslTagItemRecord.Size ||
			!platform.IsMapped(address, MuiAslTagItemRecord.Size)) return false;
		return MuiAslTagItemFieldCursorCodec.TryWrite(ref platform, address,
			MuiAslTagItemField.Tag, record.Tag) &&
			MuiAslTagItemFieldCursorCodec.TryWrite(ref platform, address,
				MuiAslTagItemField.Data, record.Data);
	}
}

// Bounded guest TagItem traversal for the ASL-facing MUI entry points. The
// walker implements the standard control tags without copying caller memory:
// TAG_DONE, TAG_IGNORE, TAG_MORE, and TAG_SKIP. Payload tags remain opaque to
// this layer and are forwarded unchanged to the ASL capability.
public static class MuiAslTagListCore
{
	public const uint TagDone = 0;
	public const uint TagIgnore = 1;
	public const uint TagMore = 2;
	public const uint TagSkip = 3;
	public const uint TagItemSize = MuiAslTagItemRecord.Size;
	public const uint MaximumSteps = 65535;

	public static bool Validate<TPlatform>(ref TPlatform platform, APTR tags)
		where TPlatform : struct, IMuiServicePlatform
	{
		uint ignored;
		bool found;
		return TryFind(ref platform, tags, 0xFFFFFFFFu, 0, out ignored,
			out found);
	}

	// Returns the first effective payload tag. A missing tag is not malformed;
	// the default value is returned and the boolean result remains true.
	public static bool TryGetData<TPlatform>(ref TPlatform platform, APTR tags,
		uint requestedTag, uint defaultValue, out uint data)
		where TPlatform : struct, IMuiServicePlatform
	{
		bool found;
		if (!TryFind(ref platform, tags, requestedTag, defaultValue, out data,
			out found)) return false;
		return true;
	}

	private static bool TryFind<TPlatform>(ref TPlatform platform, APTR tags,
		uint requestedTag, uint defaultValue, out uint result, out bool found)
		where TPlatform : struct, IMuiServicePlatform
	{
		result = defaultValue;
		found = false;
		if (tags.IsNull) return true;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint steps = 0;
		while (cursor.Base.IsNotNull && steps++ < MaximumSteps)
		{
			if (!MuiAslTagItemVectorCodec.TryGetEntry(ref platform, cursor,
				out var current) || !MuiAslTagItemCodec.TryRead(ref platform, current,
				out var item)) return false;
			var tag = item.Tag;
			var data = item.Data;
			if (tag == TagDone) return true;
			if (tag == TagMore)
			{
				if (data == 0) return true;
				cursor.Base = APTR.FromPointer(data);
				cursor.Index = 0;
				continue;
			}
			if (tag == TagSkip)
			{
				if (data == uint.MaxValue ||
					!MuiAslTagItemVectorCodec.TryAdvance(ref cursor,
						data + 1u)) return false;
				continue;
			}
			if (tag == TagIgnore)
			{
				if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
					return false;
				continue;
			}
			if (tag == requestedTag)
			{
				result = data;
				found = true;
				return true;
			}
			if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1))
				return false;
		}
		return false;
	}
}
