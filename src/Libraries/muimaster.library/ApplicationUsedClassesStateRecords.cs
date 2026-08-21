/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Owning Application UsedClasses vector pointer. The vector entries and
// strings remain caller-owned guest memory and are validated by the existing
// named vector codec before this record is published.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationUsedClassesStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x41554354u; // 'AUCT'

	internal uint Magic;
	internal APTR Vector;
}

internal enum MuiApplicationUsedClassesStateField : byte
{
	Magic,
	Vector,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationUsedClassesStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationUsedClassesStateField Field;
}

internal static class MuiApplicationUsedClassesStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationUsedClassesStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationUsedClassesStateField.Magic:
			case MuiApplicationUsedClassesStateField.Vector:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationUsedClassesStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationUsedClassesStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationUsedClassesStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationUsedClassesStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationUsedClassesStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationUsedClassesStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationUsedClassesStateRecord.Size) ||
			!MuiApplicationUsedClassesStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationUsedClassesStateField.Magic, out var magic) ||
			magic != MuiApplicationUsedClassesStateRecord.Cookie ||
			!MuiApplicationUsedClassesStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationUsedClassesStateField.Vector, out var vector))
			return false;
		value.Magic = magic;
		value.Vector = APTR.FromPointer(vector);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationUsedClassesStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationUsedClassesStateRecord.Size) || value.Magic !=
			MuiApplicationUsedClassesStateRecord.Cookie) return false;
		return MuiApplicationUsedClassesStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationUsedClassesStateField.Magic,
			value.Magic) &&
			MuiApplicationUsedClassesStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationUsedClassesStateField.Vector,
			value.Vector.Raw);
	}
}
