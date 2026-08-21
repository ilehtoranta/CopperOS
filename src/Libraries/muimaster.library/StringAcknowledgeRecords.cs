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

internal static class MuiStringAcknowledgeStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringAcknowledgeStateRecord.Size) ||
			!MuiStringAcknowledgeStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringAcknowledgeStateField.Magic, out var magic) ||
			magic != MuiStringAcknowledgeStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiStringAcknowledgeStateFieldCursorCodec.TryReadUInt32(
			ref platform, address,
			MuiStringAcknowledgeStateField.Contents, out var contents))
			return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringAcknowledgeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringAcknowledgeStateRecord.Size) || value.Magic !=
			MuiStringAcknowledgeStateRecord.Cookie) return false;
		return MuiStringAcknowledgeStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringAcknowledgeStateField.Magic, value.Magic) &&
			MuiStringAcknowledgeStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringAcknowledgeStateField.Contents, value.Contents.Raw);
	}
}
