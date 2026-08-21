/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui spell-checking policy.  MorphOS exposes this as a
// BOOL, but keeping the policy in a named record gives construction, Get, and
// mutation one validated seam without introducing a managed dictionary or a
// private widget offset.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringSpellCheckingStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D535043u; // 'MSPC'

	internal uint Magic;
	internal uint Enabled;
}

internal enum MuiStringSpellCheckingStateField : byte
{
	Magic,
	Enabled,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringSpellCheckingStateFieldCursor
{
	internal APTR Record;
	internal MuiStringSpellCheckingStateField Field;
}

internal static class MuiStringSpellCheckingStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringSpellCheckingStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringSpellCheckingStateField.Magic => 0,
			MuiStringSpellCheckingStateField.Enabled => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringSpellCheckingStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringSpellCheckingStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringSpellCheckingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringSpellCheckingStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringSpellCheckingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringSpellCheckingStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringSpellCheckingStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringSpellCheckingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringSpellCheckingStateRecord.Size) ||
			!MuiStringSpellCheckingStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringSpellCheckingStateField.Magic, out var magic) ||
			magic != MuiStringSpellCheckingStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiStringSpellCheckingStateFieldCursorCodec.TryReadUInt32(
			ref platform, address,
			MuiStringSpellCheckingStateField.Enabled, out value.Enabled);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringSpellCheckingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringSpellCheckingStateRecord.Size) || value.Magic !=
			MuiStringSpellCheckingStateRecord.Cookie) return false;
		return MuiStringSpellCheckingStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringSpellCheckingStateField.Magic, value.Magic) &&
			MuiStringSpellCheckingStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringSpellCheckingStateField.Enabled, value.Enabled);
	}
}
