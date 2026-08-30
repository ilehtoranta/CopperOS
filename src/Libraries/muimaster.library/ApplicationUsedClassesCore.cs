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

internal static class MuiApplicationUsedClassesVectorEntryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationUsedClassesVectorEntryMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiApplicationUsedClassesVectorEntryField.Name, out var name)) return false;
		value.Name = APTR.FromPointer(name);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationUsedClassesVectorEntryMemoryCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationUsedClassesVectorEntryField.Name, value.Name.Raw);
	}
}

internal static class MuiApplicationUsedClassesVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationUsedClassesVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	internal static bool TryValidate<TPlatform>(ref TPlatform platform,
		APTR vector) where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationUsedClassesVectorMemoryCodec.TryValidate(ref platform,
			vector);
}

// Struct-first guest-memory adapter for indexed UsedClasses pointer vectors.
// The complete 4-byte named entry is admitted before its STRPTR field is
// decoded; cursor callers continue through the compatibility codec above.
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
			if (!TryGetEntry(ref platform, cursor.Base, cursor.Index,
				out var slot)) return false;
			if (!MuiApplicationUsedClassesVectorEntryCodec.TryRead(ref platform,
				slot, out var entryValue)) return false;
			var entry = entryValue.Name;
			if (entry.IsNull) return true;
			if (!CStringCodec.TryReadLength(ref platform, entry, 65536,
				out _)) return false;
			cursor.Index++;
		}
		return false;
	}
}
