/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Prop/Scrollbar range state.  The ULONG fields retain MorphOS
// semantics while movement, clamping, and drawing consume one named value.
public struct MuiPropRangeState
{
	public uint Entries;
	public uint Visible;
	public uint First;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPropRangeStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4D505247u; // 'MPRG'

	internal uint Magic;
	internal uint Entries;
	internal uint Visible;
	internal uint First;
}

internal enum MuiPropRangeStateField : byte
{
	Magic,
	Entries,
	Visible,
	First,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPropRangeStateFieldCursor
{
	internal APTR Record;
	internal MuiPropRangeStateField Field;
}

internal static class MuiPropRangeStateFieldCursorCodec
{
	private static bool TryResolve(MuiPropRangeStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiPropRangeStateField.Magic => 0,
			MuiPropRangeStateField.Entries => 4,
			MuiPropRangeStateField.Visible => 8,
			MuiPropRangeStateField.First => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropRangeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiPropRangeStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropRangeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiPropRangeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropRangeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiPropRangeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiPropRangeStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiPropRangeStateRecord.Size) ||
			!MuiPropRangeStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiPropRangeStateField.Magic, out var magic) ||
			magic != MuiPropRangeStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiPropRangeStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiPropRangeStateField.Entries, out value.Entries) &&
			MuiPropRangeStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiPropRangeStateField.Visible, out value.Visible) &&
			MuiPropRangeStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiPropRangeStateField.First, out value.First);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiPropRangeStateRecord.Size) || value.Magic !=
			MuiPropRangeStateRecord.Cookie) return false;
		return MuiPropRangeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiPropRangeStateField.Magic, value.Magic) &&
			MuiPropRangeStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiPropRangeStateField.Entries, value.Entries) &&
			MuiPropRangeStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiPropRangeStateField.Visible, value.Visible) &&
			MuiPropRangeStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiPropRangeStateField.First, value.First);
	}
}
