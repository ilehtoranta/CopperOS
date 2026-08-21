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

internal static class MuiApplicationUsedClassesVectorEntryFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorEntryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Field != MuiApplicationUsedClassesVectorEntryField.Name ||
			cursor.Record.IsNull || !platform.IsMapped(cursor.Record,
				MuiApplicationUsedClassesVectorEntry.Size)) return false;
		address = cursor.Record;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesVectorEntryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationUsedClassesVectorEntryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesVectorEntryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationUsedClassesVectorEntryFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationUsedClassesVectorEntryCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationUsedClassesVectorEntryFieldCursorCodec.TryReadUInt32(
			ref platform, address,
			MuiApplicationUsedClassesVectorEntryField.Name, out var name)) return false;
		value.Name = APTR.FromPointer(name);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationUsedClassesVectorEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationUsedClassesVectorEntryFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationUsedClassesVectorEntryField.Name, value.Name.Raw);
	}
}

internal static class MuiApplicationUsedClassesVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiApplicationUsedClassesVectorCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiApplicationUsedClassesVectorCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationUsedClassesVectorCursor.EntrySize);
	}

	internal static bool TryValidate<TPlatform>(ref TPlatform platform,
		APTR vector) where TPlatform : struct, IMuiGuestMemory
	{
		if (vector.IsNull) return true;
		var cursor = default(MuiApplicationUsedClassesVectorCursor);
		cursor.Base = vector;
		while (cursor.Index < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!TryGetEntry(ref platform, cursor, out var slot)) return false;
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
