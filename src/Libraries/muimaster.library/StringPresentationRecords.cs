/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident String.mui presentation policy.  These initializer-oriented
// values are kept together so rendering, cursor metrics, and input encoding do
// not reconstruct policy from an undocumented private object layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringPresentationStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x4D535052u; // 'MSPR'

	internal uint Magic;
	internal uint MaxLen;
	internal uint Secret;
	internal uint Format;
	internal uint Unicode;
}

internal enum MuiStringPresentationStateField : byte
{
	Magic,
	MaxLen,
	Secret,
	Format,
	Unicode,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiStringPresentationStateField Field;
}

internal static class MuiStringPresentationStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringPresentationStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringPresentationStateField.Magic => 0,
			MuiStringPresentationStateField.MaxLen => 4,
			MuiStringPresentationStateField.Secret => 8,
			MuiStringPresentationStateField.Format => 12,
			MuiStringPresentationStateField.Unicode => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringPresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringPresentationStateRecord.Size) ||
			!MuiStringPresentationStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringPresentationStateField.Magic, out var magic) ||
			magic != MuiStringPresentationStateRecord.Cookie)
			return false;
		value.Magic = magic;
		return MuiStringPresentationStateFieldCursorCodec.TryReadUInt32(
			ref platform, address,
			MuiStringPresentationStateField.MaxLen, out value.MaxLen) &&
			MuiStringPresentationStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringPresentationStateField.Secret, out value.Secret) &&
			MuiStringPresentationStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringPresentationStateField.Format, out value.Format) &&
			MuiStringPresentationStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringPresentationStateField.Unicode, out value.Unicode);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringPresentationStateRecord.Size) || value.Magic !=
			MuiStringPresentationStateRecord.Cookie) return false;
		return MuiStringPresentationStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringPresentationStateField.Magic, value.Magic) &&
			MuiStringPresentationStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiStringPresentationStateField.MaxLen, value.MaxLen) &&
			MuiStringPresentationStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiStringPresentationStateField.Secret, value.Secret) &&
			MuiStringPresentationStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiStringPresentationStateField.Format, value.Format) &&
			MuiStringPresentationStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiStringPresentationStateField.Unicode, value.Unicode);
	}
}
