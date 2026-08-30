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
	internal const uint FieldSize = 4;
	internal const uint TagOffset = 0;
	internal const uint DataOffset = 4;
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

// Struct-first guest-memory adapter for one standard 8-byte TagItem record.
// The vector walker remains separate; every field access first admits the
// complete named record and rejects odd, null, or truncated addresses.
internal static class MuiAslTagItemMessageMemoryCodec
{
	private static bool TryResolve(MuiAslTagItemField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAslTagItemField.Tag:
				offset = MuiAslTagItemRecord.TagOffset;
				break;
			case MuiAslTagItemField.Data:
				offset = MuiAslTagItemRecord.DataOffset;
				break;
			default:
				offset = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslTagItemField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			(record.Raw & 1u) != 0 || record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiAslTagItemRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiAslTagItemRecord.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslTagItemField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslTagItemField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for typed cursor callers; the record codec
// and tag-list walker route through the named-record adapter above.
internal static class MuiAslTagItemFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAslTagItemFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAslTagItemMessageMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslTagItemField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAslTagItemMessageMemoryCodec.TryRead(ref platform, record, field,
			out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiAslTagItemField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAslTagItemMessageMemoryCodec.TryWrite(ref platform, record, field,
			value);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAslTagItemCursor
{
	internal const uint EntrySize = MuiAslTagItemRecord.Size;
	internal APTR Base;
	internal uint Index;
}

// Struct-first guest-memory adapter for indexed TagItem vectors. The cursor
// wrapper below is retained for typed callers, but production walkers resolve
// entries through this adapter so complete named records are admitted at the
// guest-memory boundary before any field codec is invoked.
internal static class MuiAslTagItemVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >
			(uint.MaxValue - vector.Raw) / MuiAslTagItemRecord.Size)
			return false;
		var offset = index * MuiAslTagItemRecord.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiAslTagItemRecord.Size);
	}

	internal static bool TryAdvance(ref MuiAslTagItemCursor cursor,
		uint items)
	{
		if (items == 0 || items > uint.MaxValue /
			MuiAslTagItemRecord.Size || cursor.Index > uint.MaxValue - items)
			return false;
		cursor.Index += items;
		return true;
	}
}

internal static class MuiAslTagItemVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiAslTagItemCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	internal static bool TryAdvance(ref MuiAslTagItemCursor cursor,
		uint items)
		=> MuiAslTagItemVectorMemoryCodec.TryAdvance(ref cursor, items);
}

// Central codec for the standard 8-byte TagItem guest record. The walker uses
// named Tag/Data fields; only this adapter knows the packed wire offsets.
internal static class MuiAslTagItemCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAslTagItemRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		record = default;
		if (address.IsNull || (address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiAslTagItemRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var tag) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var data)) return false;
		record.Tag = tag;
		record.Data = data;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAslTagItemRecord record)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || (address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiAslTagItemRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Tag) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				record.Data)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAslTagItemRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out record);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAslTagItemRecord record)
		where TPlatform : struct, IMuiGuestMemory
		=> WriteRecord(ref platform, address, record);
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
			if (!MuiAslTagItemVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Base, cursor.Index,
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
					!MuiAslTagItemVectorMemoryCodec.TryAdvance(ref cursor,
						data + 1u)) return false;
				continue;
			}
			if (tag == TagIgnore)
			{
				if (!MuiAslTagItemVectorMemoryCodec.TryAdvance(ref cursor, 1))
					return false;
				continue;
			}
			if (tag == requestedTag)
			{
				result = data;
				found = true;
				return true;
			}
			if (!MuiAslTagItemVectorMemoryCodec.TryAdvance(ref cursor, 1))
				return false;
		}
		return false;
	}
}
