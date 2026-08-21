/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Initializer-only caller-owned Application identity strings. The record
// retains validated guest pointers and never copies text into managed memory.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationIdentityStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x41495354u; // 'AIST'

	internal uint Magic;
	internal APTR Author;
	internal APTR Base;
	internal APTR Copyright;
	internal APTR Description;
	internal APTR Title;
	internal APTR Version;
}

internal enum MuiApplicationIdentityStateField : byte
{
	Magic,
	Author,
	Base,
	Copyright,
	Description,
	Title,
	Version,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationIdentityStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationIdentityStateField Field;
}

internal static class MuiApplicationIdentityStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationIdentityStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationIdentityStateField.Magic:
			case MuiApplicationIdentityStateField.Author:
			case MuiApplicationIdentityStateField.Base:
			case MuiApplicationIdentityStateField.Copyright:
			case MuiApplicationIdentityStateField.Description:
			case MuiApplicationIdentityStateField.Title:
			case MuiApplicationIdentityStateField.Version:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationIdentityStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationIdentityStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationIdentityStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationIdentityStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationIdentityStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationIdentityStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationIdentityStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationIdentityStateRecord.Size) ||
			!MuiApplicationIdentityStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationIdentityStateField.Magic, out var magic) ||
			magic != MuiApplicationIdentityStateRecord.Cookie ||
			!MuiApplicationIdentityStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationIdentityStateField.Author, out var author) ||
			!MuiApplicationIdentityStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationIdentityStateField.Base, out var @base) ||
			!MuiApplicationIdentityStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationIdentityStateField.Copyright,
				out var copyright) ||
			!MuiApplicationIdentityStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationIdentityStateField.Description,
				out var description) ||
			!MuiApplicationIdentityStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationIdentityStateField.Title, out var title) ||
			!MuiApplicationIdentityStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationIdentityStateField.Version, out var version))
			return false;
		value.Magic = magic;
		value.Author = APTR.FromPointer(author);
		value.Base = APTR.FromPointer(@base);
		value.Copyright = APTR.FromPointer(copyright);
		value.Description = APTR.FromPointer(description);
		value.Title = APTR.FromPointer(title);
		value.Version = APTR.FromPointer(version);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationIdentityStateRecord.Size) || value.Magic !=
			MuiApplicationIdentityStateRecord.Cookie) return false;
		return MuiApplicationIdentityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Magic,
			value.Magic) &&
			MuiApplicationIdentityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Author,
			value.Author.Raw) &&
			MuiApplicationIdentityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Base,
			value.Base.Raw) &&
			MuiApplicationIdentityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Copyright,
			value.Copyright.Raw) &&
			MuiApplicationIdentityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Description,
			value.Description.Raw) &&
			MuiApplicationIdentityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Title,
			value.Title.Raw) &&
			MuiApplicationIdentityStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationIdentityStateField.Version,
			value.Version.Raw);
	}
}
