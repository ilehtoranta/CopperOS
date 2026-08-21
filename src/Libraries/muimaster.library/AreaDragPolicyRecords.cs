/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Area drag policy. The two BOOL inputs stay together in one
// guest-resident record so drag dispatch and public getters consume the same
// state shape.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragPolicyStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41445250u; // 'ADRP'

	internal uint Magic;
	internal uint Draggable;
	internal uint Dropable;
}

internal enum MuiAreaDragPolicyStateField : byte
{
	Magic,
	Draggable,
	Dropable,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaDragPolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaDragPolicyStateField Field;
}

internal static class MuiAreaDragPolicyStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaDragPolicyStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaDragPolicyStateField.Magic => 0,
			MuiAreaDragPolicyStateField.Draggable => 4,
			MuiAreaDragPolicyStateField.Dropable => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaDragPolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaDragPolicyStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragPolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaDragPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaDragPolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaDragPolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaDragPolicyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaDragPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDragPolicyStateRecord.Size) ||
			!MuiAreaDragPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDragPolicyStateField.Magic, out var magic) ||
			magic != MuiAreaDragPolicyStateRecord.Cookie ||
			!MuiAreaDragPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDragPolicyStateField.Draggable,
				out value.Draggable) ||
			!MuiAreaDragPolicyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaDragPolicyStateField.Dropable,
				out value.Dropable)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaDragPolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaDragPolicyStateRecord.Size) || value.Magic !=
			MuiAreaDragPolicyStateRecord.Cookie) return false;
		return MuiAreaDragPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDragPolicyStateField.Magic, value.Magic) &&
			MuiAreaDragPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDragPolicyStateField.Draggable, value.Draggable) &&
			MuiAreaDragPolicyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaDragPolicyStateField.Dropable, value.Dropable);
	}
}
