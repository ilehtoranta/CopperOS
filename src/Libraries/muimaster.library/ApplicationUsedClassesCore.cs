/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest representation of MUIA_Application_UsedClasses: a NULL-terminated
// vector of STRPTR values. Keeping the cursor as a named record makes the
// pointer arithmetic explicit and keeps managed collections out of the path.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationUsedClassesVectorCursor
{
	public const uint EntrySize = MuiApplicationUsedClassesVectorEntry.Size;
	public APTR Base;
	public uint Index;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationUsedClassesVectorEntry
{
	internal const uint Size = 4;
	internal const uint FieldSize = 4;
	internal const uint NameOffset = 0;
	internal APTR Name;
}

internal enum MuiApplicationUsedClassesVectorEntryField : byte
{
	Name,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationUsedClassesVectorEntryFieldCursor
{
	internal APTR Record;
	internal MuiApplicationUsedClassesVectorEntryField Field;
}

// Struct-first guest-memory adapter for one caller-owned UsedClasses slot.
// The NULL-terminated vector walker remains separate; this codec owns the
// complete 4-byte entry admission and named Name field translation.
internal static class MuiApplicationUsedClassesVectorEntryMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesVectorEntryField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (field != MuiApplicationUsedClassesVectorEntryField.Name ||
			record.IsNull || record.Raw > uint.MaxValue -
			MuiApplicationUsedClassesVectorEntry.NameOffset ||
			!platform.IsMapped(record, MuiApplicationUsedClassesVectorEntry.Size))
			return false;
		address = APTR.FromPointer(record.Raw +
			MuiApplicationUsedClassesVectorEntry.NameOffset);
		return platform.IsMapped(address,
			MuiApplicationUsedClassesVectorEntry.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesVectorEntryField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesVectorEntryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for existing typed cursor diagnostics.
internal static class MuiApplicationUsedClassesVectorEntryFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorEntryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationUsedClassesVectorEntryMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesVectorEntryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationUsedClassesVectorEntryMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesVectorEntryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationUsedClassesVectorEntryMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

// Sequential codec for one complete caller-owned UsedClasses vector element.
// The element is a single MorphOS STRPTR/ULONG slot; the shared named ULONG
// codec retains the full 32-bit pointer range on the freestanding path.
internal static class MuiApplicationUsedClassesVectorEntryStructCodec
{
	private static bool TryReadPointer<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiApplicationUsedClassesVectorEntry.Size, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	private static bool TryWritePointer<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiApplicationUsedClassesVectorEntry.Size, out var address))
			return false;
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationUsedClassesVectorEntry.Size, out var cursor) ||
			!TryReadPointer(ref platform, ref cursor, out var name) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Name = APTR.FromPointer(name);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationUsedClassesVectorEntry.Size, out var cursor) ||
			!TryWritePointer(ref platform, ref cursor, value.Name.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

internal static class MuiApplicationUsedClassesVectorEntryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationUsedClassesVectorEntryStructCodec.TryRead(ref platform,
			address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationUsedClassesVectorEntryStructCodec.Write(ref platform,
			address, value);
}

internal static class MuiApplicationUsedClassesVectorCodec
{
	internal static bool TryAdvance(ref MuiApplicationUsedClassesVectorCursor cursor,
		uint items)
	{
		if (items == 0 || cursor.Index > uint.MaxValue - items)
			return false;
		var next = cursor.Index + items;
		if (next > MuiHeadlessLayout.MaximumTraversal) return false;
		cursor.Index = next;
		return true;
	}

	// The one-field scalar projection keeps the validator on the complete named
	// entry record; slot arithmetic remains in the bounded vector adapter.
	internal static bool TryReadName<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out uint rawName)
		where TPlatform : struct, IMuiGuestMemory
	{
		rawName = 0;
		if (!MuiApplicationUsedClassesVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out var address)) return false;
		if (!MuiApplicationUsedClassesVectorEntryCodec.TryRead(ref platform,
			address, out var value)) return false;
		rawName = value.Name.Raw;
		return true;
	}

	internal static bool TryReadName<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorCursor cursor, out uint rawName)
		where TPlatform : struct, IMuiGuestMemory
	{
		rawName = 0;
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		if (!MuiApplicationUsedClassesVectorEntryCodec.TryRead(ref platform,
			address, out var value)) return false;
		rawName = value.Name.Raw;
		return true;
	}

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationUsedClassesVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorCursor cursor,
		out MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadName(ref platform, cursor, out var rawName)) return false;
		value.Name = APTR.FromPointer(rawName);
		return true;
	}

	internal static bool TryWriteName<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorCursor cursor, uint rawName)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		var value = default(MuiApplicationUsedClassesVectorEntry);
		value.Name = APTR.FromPointer(rawName);
		return MuiApplicationUsedClassesVectorEntryCodec.Write(ref platform,
			address, value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorCursor cursor,
		MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryWriteName(ref platform, cursor, value.Name.Raw);

	// Complete named-record bridge for indexed consumers. Slot arithmetic and
	// complete-entry mapping remain owned by the bounded memory adapter.
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR vector, uint index,
		out MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadName(ref platform, vector, index, out var rawName))
		{
			value = default;
			return false;
		}
		value.Name = APTR.FromPointer(rawName);
		return true;
	}

	internal static bool TryWriteName<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, uint rawName)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationUsedClassesVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out var address)) return false;
		var value = default(MuiApplicationUsedClassesVectorEntry);
		value.Name = APTR.FromPointer(rawName);
		return MuiApplicationUsedClassesVectorEntryCodec.Write(ref platform,
			address, value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR vector, uint index,
		MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryWriteName(ref platform, vector, index, value.Name.Raw);
	}

	internal static bool TryValidate<TPlatform>(ref TPlatform platform,
		APTR vector) where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationUsedClassesVectorMemoryCodec.TryValidate(ref platform,
			vector);
}

// Struct-first guest-memory adapter for indexed UsedClasses pointer vectors.
// The complete 4-byte named entry is admitted before its STRPTR field is
// decoded; cursor callers remain on the same sequential record codec.
internal static class MuiApplicationUsedClassesVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >
			(uint.MaxValue - vector.Raw) / MuiApplicationUsedClassesVectorEntry.Size)
			return false;
		var offset = index * MuiApplicationUsedClassesVectorEntry.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationUsedClassesVectorEntry.Size);
	}

	// Validate the complete caller-owned NULL-terminated vector while keeping
	// entry addressing and bounds in this struct-backed memory adapter.
	internal static bool TryValidate<TPlatform>(ref TPlatform platform,
		APTR vector) where TPlatform : struct, IMuiGuestMemory
	{
		if (vector.IsNull) return true;
		var cursor = default(MuiApplicationUsedClassesVectorCursor);
		cursor.Base = vector;
		while (cursor.Index < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationUsedClassesVectorCodec.TryReadName(ref platform,
				cursor, out var rawEntry)) return false;
			var entry = APTR.FromPointer(rawEntry);
			if (entry.IsNull) return true;
			if (!CStringCodec.TryReadLength(ref platform, entry, 65536,
				out _)) return false;
			if (!MuiApplicationUsedClassesVectorCodec.TryAdvance(ref cursor, 1))
				return false;
		}
		return false;
	}
}
