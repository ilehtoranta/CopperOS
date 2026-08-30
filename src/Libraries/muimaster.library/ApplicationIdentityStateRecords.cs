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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint AuthorOffset = 4;
	internal const uint BaseOffset = 8;
	internal const uint CopyrightOffset = 12;
	internal const uint DescriptionOffset = 16;
	internal const uint TitleOffset = 20;
	internal const uint VersionOffset = 24;
	internal const uint Cookie = 0x41495354u; // 'AIST'

	internal uint Magic;
	internal APTR Author;
	internal APTR Base;
	internal APTR Copyright;
	internal APTR Description;
	internal APTR Title;
	internal APTR Version;
}

// Admission for caller-owned MorphOS application identity strings.  The
// record carries guest capabilities only; every non-NULL pointer must resolve
// to a bounded guest C string before a consumer may use the record.
internal static class MuiApplicationIdentityStateAdmission
{
	internal const uint MaximumStringLength = 65536;

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationIdentityStateRecord.Cookie &&
		IsCString(ref platform, value.Author) &&
		IsCString(ref platform, value.Base) &&
		IsCString(ref platform, value.Copyright) &&
		IsCString(ref platform, value.Description) &&
		IsCString(ref platform, value.Title) &&
		IsCString(ref platform, value.Version);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;

	private static bool IsCString<TPlatform>(ref TPlatform platform, APTR value)
		where TPlatform : struct, IMuiGuestMemory => value.IsNull ||
		CStringCodec.TryReadLength(ref platform, value, MaximumStringLength, out _);
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationIdentityStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationIdentityStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationIdentityStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationIdentityStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationIdentityStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationIdentityStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application identity state is transferred as a named record. Numeric
// guest positions are confined to this ABI adapter; the compatibility cursor
// above remains available only to legacy callers and malformed-state tests.
internal static class MuiApplicationIdentityStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationIdentityStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationIdentityStateField.Magic:
				offset = MuiApplicationIdentityStateRecord.MagicOffset;
				return true;
			case MuiApplicationIdentityStateField.Author:
				offset = MuiApplicationIdentityStateRecord.AuthorOffset;
				return true;
			case MuiApplicationIdentityStateField.Base:
				offset = MuiApplicationIdentityStateRecord.BaseOffset;
				return true;
			case MuiApplicationIdentityStateField.Copyright:
				offset = MuiApplicationIdentityStateRecord.CopyrightOffset;
				return true;
			case MuiApplicationIdentityStateField.Description:
				offset = MuiApplicationIdentityStateRecord.DescriptionOffset;
				return true;
			case MuiApplicationIdentityStateField.Title:
				offset = MuiApplicationIdentityStateRecord.TitleOffset;
				return true;
			case MuiApplicationIdentityStateField.Version:
				offset = MuiApplicationIdentityStateRecord.VersionOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationIdentityStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationIdentityStateRecord.Size) &&
			platform.IsMapped(address, MuiApplicationIdentityStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationIdentityStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationIdentityStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationIdentityStateRecordCodec
{
	// Application identity is a fixed seven-ULONG pointer record. Exchange all
	// fields sequentially as one named struct; C-string admission stays in the
	// dedicated validation layer.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationIdentityStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var author) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var @base) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var copyright) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var description) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var title) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var version)) return false;
		value.Author = APTR.FromPointer(author);
		value.Base = APTR.FromPointer(@base);
		value.Copyright = APTR.FromPointer(copyright);
		value.Description = APTR.FromPointer(description);
		value.Title = APTR.FromPointer(title);
		value.Version = APTR.FromPointer(version);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationIdentityStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Author.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Base.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Copyright.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Description.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Title.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Version.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationIdentityStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationIdentityStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationIdentityStateRecord.Size) ||
			!MuiApplicationIdentityStateAdmission.Validate(ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
