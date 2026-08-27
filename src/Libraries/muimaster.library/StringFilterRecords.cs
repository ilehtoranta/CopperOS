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

internal static class MuiStringFilterStateAdmission
{
	internal static bool Validate(MuiStringFilterStateRecord value) =>
		value.Magic == MuiStringFilterStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringFilterStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull &&
		(value.Accept.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Accept, 4096, out _)) &&
		(value.Reject.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Reject, 4096, out _));
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

// Struct-first guest-memory adapter. Accept and Reject remain named semantic
// pointers; this bounded adapter owns fixed guest-layout translation.
internal static class MuiStringFilterStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringFilterStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringFilterStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringFilterStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringFilterStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiStringFilterStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var accept) ||
			!MuiStringFilterStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out var reject)) return false;
		value.Accept = APTR.FromPointer(accept);
		value.Reject = APTR.FromPointer(reject);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiStringFilterStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringFilterStateAdmission.Validate(value)) return false;
		return MuiStringFilterStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiStringFilterStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.Accept.Raw) &&
			MuiStringFilterStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.Reject.Raw);
	}
}
