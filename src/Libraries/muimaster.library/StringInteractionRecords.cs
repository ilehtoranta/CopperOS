/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui interaction policy.  The three BOOL attributes
// share one validated record so the editor's input gate and CR behavior do not
// depend on a private scalar ordering.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringInteractionStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4D534952u; // 'MSIR'

	internal uint Magic;
	internal uint Editable;
	internal uint AdvanceOnCR;
	internal uint Multiline;
}

internal enum MuiStringInteractionStateField : byte
{
	Magic,
	Editable,
	AdvanceOnCR,
	Multiline,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringInteractionStateFieldCursor
{
	internal APTR Record;
	internal MuiStringInteractionStateField Field;
}

internal static class MuiStringInteractionStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringInteractionStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringInteractionStateField.Magic => 0,
			MuiStringInteractionStateField.Editable => 4,
			MuiStringInteractionStateField.AdvanceOnCR => 8,
			MuiStringInteractionStateField.Multiline => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringInteractionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringInteractionStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringInteractionStateRecord.Size) ||
			!MuiStringInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringInteractionStateField.Magic, out var magic) ||
			magic != MuiStringInteractionStateRecord.Cookie)
			return false;
		value.Magic = magic;
		return MuiStringInteractionStateFieldCursorCodec.TryReadUInt32(
			ref platform, address,
			MuiStringInteractionStateField.Editable, out value.Editable) &&
			MuiStringInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringInteractionStateField.AdvanceOnCR,
				out value.AdvanceOnCR) &&
			MuiStringInteractionStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringInteractionStateField.Multiline, out value.Multiline);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringInteractionStateRecord.Size) || value.Magic !=
			MuiStringInteractionStateRecord.Cookie) return false;
		return MuiStringInteractionStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringInteractionStateField.Magic, value.Magic) &&
			MuiStringInteractionStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiStringInteractionStateField.Editable, value.Editable) &&
			MuiStringInteractionStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiStringInteractionStateField.AdvanceOnCR,
				value.AdvanceOnCR) &&
			MuiStringInteractionStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiStringInteractionStateField.Multiline, value.Multiline);
	}
}
