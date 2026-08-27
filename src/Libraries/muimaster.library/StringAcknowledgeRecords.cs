/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident getter-only String.mui acknowledgement state.  The pointer
// names the current owned contents buffer; it is not a hidden widget offset or
// a managed copy of the text.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringAcknowledgeStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D534143u; // 'MSAC'

	internal uint Magic;
	internal APTR Contents;
}

internal static class MuiStringAcknowledgeStateAdmission
{
	internal const int MaximumLength = 4096;

	internal static bool Validate(MuiStringAcknowledgeStateRecord value) =>
		value.Magic == MuiStringAcknowledgeStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Contents.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Contents, MaximumLength, out _);
	}
}

internal enum MuiStringAcknowledgeStateField : byte
{
	Magic,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringAcknowledgeStateFieldCursor
{
	internal APTR Record;
	internal MuiStringAcknowledgeStateField Field;
}

internal static class MuiStringAcknowledgeStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringAcknowledgeStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringAcknowledgeStateField.Magic => 0,
			MuiStringAcknowledgeStateField.Contents => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringAcknowledgeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringAcknowledgeStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAcknowledgeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringAcknowledgeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringAcknowledgeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringAcknowledgeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Acknowledge consumers use the named
// contents record; this bounded adapter is the only layer that translates its
// fixed guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiStringAcknowledgeStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiStringAcknowledgeStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiStringAcknowledgeStateRecord.Size)) return false;
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

internal static class MuiStringAcknowledgeStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiStringAcknowledgeStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 0, out value.Magic) ||
			!MuiStringAcknowledgeStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 4, out var contents)) return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiStringAcknowledgeStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringAcknowledgeStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiStringAcknowledgeStateAdmission.Validate(value)) return false;
		return MuiStringAcknowledgeStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiStringAcknowledgeStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, 4, value.Contents.Raw);
	}
}
