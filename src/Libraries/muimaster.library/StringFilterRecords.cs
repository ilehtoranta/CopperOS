/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui character-set filters.  Accept and Reject remain
// caller-owned [ISG] strings; this record names the pair without copying them
// into managed memory or exposing a private String layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringFilterStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D534652u; // 'MSFR'

	internal uint Magic;
	internal APTR Accept;
	internal APTR Reject;
}

internal enum MuiStringFilterStateField : byte
{
	Magic,
	Accept,
	Reject,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringFilterStateFieldCursor
{
	internal APTR Record;
	internal MuiStringFilterStateField Field;
}

internal static class MuiStringFilterStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringFilterStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringFilterStateField.Magic => 0,
			MuiStringFilterStateField.Accept => 4,
			MuiStringFilterStateField.Reject => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringFilterStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringFilterStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringFilterStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringFilterStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringFilterStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringFilterStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringFilterStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringFilterStateRecord.Size) ||
			!MuiStringFilterStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringFilterStateField.Magic, out var magic) ||
			magic != MuiStringFilterStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiStringFilterStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringFilterStateField.Accept, out var accept) ||
			!MuiStringFilterStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringFilterStateField.Reject, out var reject))
			return false;
		value.Accept = APTR.FromPointer(accept);
		value.Reject = APTR.FromPointer(reject);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringFilterStateRecord.Size) || value.Magic !=
			MuiStringFilterStateRecord.Cookie) return false;
		return MuiStringFilterStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringFilterStateField.Magic, value.Magic) &&
			MuiStringFilterStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringFilterStateField.Accept, value.Accept.Raw) &&
			MuiStringFilterStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringFilterStateField.Reject, value.Reject.Raw);
	}
}
