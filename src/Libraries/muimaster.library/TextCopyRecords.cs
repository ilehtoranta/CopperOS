/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_Text_Copy is a public BOOL policy. Keep the normalized value in a
// named semantic view so Get/OM_GET, construction, and Text_Contents mutation
// share one source without exposing managed state or object offsets.
public struct MuiTextCopyState
{
	public uint Copy;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextCopyStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D544350u; // 'MTCP'

	internal uint Magic;
	internal uint Copy;
}

internal enum MuiTextCopyStateField : byte
{
	Magic,
	Copy,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextCopyStateFieldCursor
{
	internal APTR Record;
	internal MuiTextCopyStateField Field;
}

internal static class MuiTextCopyStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextCopyStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextCopyStateField.Magic => 0,
			MuiTextCopyStateField.Copy => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextCopyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiTextCopyStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextCopyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextCopyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextCopyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiTextCopyStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextCopyStateRecord.Size) ||
			!MuiTextCopyStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiTextCopyStateField.Magic, out var magic) ||
			magic != MuiTextCopyStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiTextCopyStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextCopyStateField.Copy, out value.Copy);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextCopyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextCopyStateRecord.Size) || value.Magic !=
			MuiTextCopyStateRecord.Cookie) return false;
		return MuiTextCopyStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextCopyStateField.Magic, value.Magic) &&
			MuiTextCopyStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiTextCopyStateField.Copy, value.Copy);
	}
}

// Keep the wire value lossless for malformed-state diagnostics. MUIA_Text_Copy
// is a MorphOS BOOL and must be canonical before ownership or getter consumers
// use the named policy state.
internal static class MuiTextCopyStateValidation
{
	internal static bool IsValidRecord(MuiTextCopyStateRecord value) =>
		value.Copy <= 1;

	internal static bool IsValidState(MuiTextCopyState value) =>
		value.Copy <= 1;
}
