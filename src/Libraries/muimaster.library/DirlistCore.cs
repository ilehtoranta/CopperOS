/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Dirlist filtering is a single policy surface even though MorphOS exposes it
// as several attributes. Keep the owned pattern pointers and scalar filters
// together so scans do not reread a partially updated set of fields.
public struct MuiDirlistFilterState
{
	public APTR AcceptPattern;
	public APTR RejectPattern;
	public APTR Pattern;
	public uint DrawersOnly;
	public uint FilesOnly;
	public uint FilterDrawers;
	public uint MultiSelDirs;
	public uint RejectIcons;
	public uint ExAllType;
	public APTR FilterHook;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistFilterStateRecord
{
	internal const uint Size = 44;
	internal const uint Cookie = 0x444C464Cu; // 'DLFL'
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint AcceptPatternOffset = 4;
	internal const uint RejectPatternOffset = 8;
	internal const uint PatternOffset = 12;
	internal const uint DrawersOnlyOffset = 16;
	internal const uint FilesOnlyOffset = 20;
	internal const uint FilterDrawersOffset = 24;
	internal const uint MultiSelDirsOffset = 28;
	internal const uint RejectIconsOffset = 32;
	internal const uint ExAllTypeOffset = 36;
	internal const uint FilterHookOffset = 40;

	internal uint Magic;
	internal APTR AcceptPattern;
	internal APTR RejectPattern;
	internal APTR Pattern;
	internal uint DrawersOnly;
	internal uint FilesOnly;
	internal uint FilterDrawers;
	internal uint MultiSelDirs;
	internal uint RejectIcons;
	internal uint ExAllType;
	internal APTR FilterHook;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistScanStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x444C5343u; // 'DLSC'
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint StatusOffset = 4;
	internal const uint NumFilesOffset = 8;
	internal const uint NumDrawersOffset = 12;
	internal const uint NumBytesOffset = 16;
	internal const uint IoErrOffset = 20;

	internal uint Magic;
	internal uint Status;
	internal uint NumFiles;
	internal uint NumDrawers;
	internal uint NumBytes;
	internal int IoErr;
}

// Sorting is another shared Dirlist/Volumelist policy surface. Keep selector
// values canonical in a named record before the allocation-free reorder pass.
public struct MuiDirlistSortState
{
	public uint SortType;
	public uint SortDirs;
	public uint SortHighLow;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistSortStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x444C5354u; // 'DLST'
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint SortTypeOffset = 4;
	internal const uint SortDirsOffset = 8;
	internal const uint SortHighLowOffset = 12;

	internal uint Magic;
	internal uint SortType;
	internal uint SortDirs;
	internal uint SortHighLow;
}

// Pure admission for the shared Dirlist/Volumelist policy records. The
// structural codecs below own the packed guest layout; this small validator
// owns only the MorphOS selector/BOOL/status invariants. Object-owned pointer
// identity is checked by MuiDirlistCore when the record is read in context.
internal static class MuiDirlistStateAdmission
{
	internal static bool ValidateSort(MuiDirlistSortStateRecord value) =>
		value.Magic == MuiDirlistSortStateRecord.Cookie &&
		value.SortType <= 5 && value.SortDirs <= 2 &&
		value.SortHighLow <= 1;

	internal static bool ValidateFilter(MuiDirlistFilterStateRecord value) =>
		value.Magic == MuiDirlistFilterStateRecord.Cookie &&
		value.DrawersOnly <= 1 && value.FilesOnly <= 1 &&
		value.FilterDrawers <= 1 && value.MultiSelDirs <= 1 &&
		value.RejectIcons <= 1;

	internal static bool ValidateScan(MuiDirlistScanStateRecord value) =>
		value.Magic == MuiDirlistScanStateRecord.Cookie &&
		value.Status <= MuiDirlistCore.StatusValid &&
		value.NumFiles <= 65536 && value.NumDrawers <= 65536 &&
		value.NumFiles <= 65536 - value.NumDrawers;
}

// Scan publication is kept as one named result record so status, counters,
// byte totals, and the captured AmigaDOS IoErr cannot become inconsistent at
// a valid/invalid transition.
public struct MuiDirlistScanState
{
	public uint Status;
	public uint NumFiles;
	public uint NumDrawers;
	public uint NumBytes;
	public int IoErr;
}

// Named view of an owned, guest-resident FileInfoBlock-like entry. The record
// contains variable-length inline strings, so the codec exposes their guest
// pointers and validated lengths instead of making callers repeat byte
// offsets. The Address field identifies the backing guest record for mutation.
public struct MuiDirlistEntryState
{
	public APTR Address;
	public uint RecordSize;
	public int Type;
	public uint SizeLow;
	public uint SizeHigh;
	public uint Protection;
	public uint Days;
	public uint Mins;
	public uint Ticks;
	public APTR Name;
	public uint NameLength;
	public APTR Comment;
	public uint CommentLength;
}

// Named view of the fixed ExAll-like scratch payload supplied by the
// directory capability. The payload is transient; string pointers remain in
// guest memory and are consumed only while the scratch block is live.
public struct MuiDirlistScanEntryState
{
	public APTR Address;
	public int Type;
	public uint SizeLow;
	public uint SizeHigh;
	public uint Protection;
	public uint Days;
	public uint Mins;
	public uint Ticks;
	public APTR Name;
	public uint NameLength;
	public APTR Comment;
	public uint CommentLength;
}

// Named view of the guest QUAD retained by MUIA_Dirlist_NumBytes64. The
// high/low words stay explicit so the 68k ABI remains visible without making
// callers depend on raw word offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiDirlistByteTotalState
{
	public const uint Size = 8;
	public const uint FieldSize = 4;
	public const uint HighOffset = 0;
	public const uint LowOffset = 4;

	public uint High;
	public uint Low;
}

internal static class MuiDirlistByteTotalCodec
{
	// The QUAD is a declaration-ordered two-LONG record. Keep the field
	// adapter below available for compatibility callers, but make production
	// reads and writes consume the complete named record through one cursor.
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDirlistByteTotalState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistByteTotalState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var high) || !MuiGuestStructCursor.TryReadUInt32(ref platform,
				ref cursor, out var low) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.High = high;
		value.Low = low;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDirlistByteTotalState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistByteTotalState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.High) || !MuiGuestStructCursor.TryWriteUInt32(ref platform,
				ref cursor, value.Low)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Fixed header of an owned variable-length FileInfoBlock-like entry. The
// inline name/comment payload follows this record; only that variable tail
// remains outside the codec.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistEntryWireState
{
	internal const uint Size = 36;
	internal const uint NameOffset = 36;
	internal const uint FieldSize = 4;
	internal const uint RecordSizeOffset = 0;
	internal const uint TypeOffset = 4;
	internal const uint SizeLowOffset = 8;
	internal const uint SizeHighOffset = 12;
	internal const uint ProtectionOffset = 16;
	internal const uint DaysOffset = 20;
	internal const uint MinsOffset = 24;
	internal const uint TicksOffset = 28;
	internal const uint CommentOffsetOffset = 32;

	internal uint RecordSize;
	internal int Type;
	internal uint SizeLow;
	internal uint SizeHigh;
	internal uint Protection;
	internal uint Days;
	internal uint Mins;
	internal uint Ticks;
	internal uint CommentOffset;
}

internal static class MuiDirlistEntryWireCodec
{
	// The fixed FileInfoBlock-like header is exchanged in declaration order.
	// Inline name/comment bytes remain a variable tail owned by Dirlist; this
	// codec covers only the complete named header record.
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDirlistEntryWireState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistEntryWireState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var recordSize) || !MuiGuestStructCursor.TryReadUInt32(
				ref platform, ref cursor, out var type) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var sizeLow) || !MuiGuestStructCursor.TryReadUInt32(
				ref platform, ref cursor, out var sizeHigh) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var protection) || !MuiGuestStructCursor.TryReadUInt32(
				ref platform, ref cursor, out var days) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var mins) || !MuiGuestStructCursor.TryReadUInt32(
				ref platform, ref cursor, out var ticks) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var commentOffset) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.RecordSize = recordSize;
		value.Type = unchecked((int)type);
		value.SizeLow = sizeLow;
		value.SizeHigh = sizeHigh;
		value.Protection = protection;
		value.Days = days;
		value.Mins = mins;
		value.Ticks = ticks;
		value.CommentOffset = commentOffset;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDirlistEntryWireState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistEntryWireState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.RecordSize) || !MuiGuestStructCursor.TryWriteUInt32(
				ref platform, ref cursor, unchecked((uint)value.Type)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SizeLow) || !MuiGuestStructCursor.TryWriteUInt32(
				ref platform, ref cursor, value.SizeHigh) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Protection) || !MuiGuestStructCursor.TryWriteUInt32(
				ref platform, ref cursor, value.Days) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Mins) || !MuiGuestStructCursor.TryWriteUInt32(
				ref platform, ref cursor, value.Ticks) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.CommentOffset)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Fixed fields of the transient ExAll-like directory-capability payload. The
// name and comment arrays begin at the documented offsets after this header.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistScanEntryWireState
{
	internal const uint Size = 28;
	internal const uint NameOffset = 28;
	internal const uint CommentOffset = 136;
	internal const uint TotalSize = 224;
	internal const uint FieldSize = 4;
	internal const uint TypeOffset = 0;
	internal const uint SizeLowOffset = 4;
	internal const uint SizeHighOffset = 8;
	internal const uint ProtectionOffset = 12;
	internal const uint DaysOffset = 16;
	internal const uint MinsOffset = 20;
	internal const uint TicksOffset = 24;

	internal int Type;
	internal uint SizeLow;
	internal uint SizeHigh;
	internal uint Protection;
	internal uint Days;
	internal uint Mins;
	internal uint Ticks;
}

internal enum MuiDirlistRecordKind : byte
{
	ByteTotal,
	EntryWire,
	ScanEntryWire,
	SortState,
	FilterState,
	ScanState,
}

internal enum MuiDirlistRecordField : byte
{
	High,
	Low,
	RecordSize,
	Type,
	SizeLow,
	SizeHigh,
	Protection,
	Days,
	Mins,
	Ticks,
	CommentOffset,
	Magic,
	SortTypeValue,
	SortDirsValue,
	SortHighLowValue,
	AcceptPatternValue,
	RejectPatternValue,
	PatternValue,
	DrawersOnlyValue,
	FilesOnlyValue,
	FilterDrawersValue,
	MultiSelDirsValue,
	RejectIconsValue,
	ExAllTypeValue,
	FilterHookValue,
	StatusValue,
	NumFilesValue,
	NumDrawersValue,
	NumBytesValue,
	IoErrValue,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiDirlistRecordFieldCursor
{
	internal APTR Address;
	internal MuiDirlistRecordKind Record;
	internal MuiDirlistRecordField Field;
}

internal static class MuiDirlistRecordMemoryCodec
{
	private static bool TryResolve(MuiDirlistRecordKind record,
		MuiDirlistRecordField field, out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		if (record == MuiDirlistRecordKind.ByteTotal)
		{
			size = MuiDirlistByteTotalState.Size;
			if (field == MuiDirlistRecordField.High)
				offset = MuiDirlistByteTotalState.HighOffset;
			else if (field == MuiDirlistRecordField.Low)
				offset = MuiDirlistByteTotalState.LowOffset;
			else return false;
			return true;
		}
		if (record == MuiDirlistRecordKind.EntryWire)
		{
			size = MuiDirlistEntryWireState.Size;
			if (field == MuiDirlistRecordField.RecordSize)
				offset = MuiDirlistEntryWireState.RecordSizeOffset;
			else if (field == MuiDirlistRecordField.Type)
				offset = MuiDirlistEntryWireState.TypeOffset;
			else if (field == MuiDirlistRecordField.SizeLow)
				offset = MuiDirlistEntryWireState.SizeLowOffset;
			else if (field == MuiDirlistRecordField.SizeHigh)
				offset = MuiDirlistEntryWireState.SizeHighOffset;
			else if (field == MuiDirlistRecordField.Protection)
				offset = MuiDirlistEntryWireState.ProtectionOffset;
			else if (field == MuiDirlistRecordField.Days)
				offset = MuiDirlistEntryWireState.DaysOffset;
			else if (field == MuiDirlistRecordField.Mins)
				offset = MuiDirlistEntryWireState.MinsOffset;
			else if (field == MuiDirlistRecordField.Ticks)
				offset = MuiDirlistEntryWireState.TicksOffset;
			else if (field == MuiDirlistRecordField.CommentOffset)
				offset = MuiDirlistEntryWireState.CommentOffsetOffset;
			else return false;
			return true;
		}
		if (record == MuiDirlistRecordKind.ScanEntryWire)
		{
			size = MuiDirlistScanEntryWireState.Size;
			if (field == MuiDirlistRecordField.Type)
				offset = MuiDirlistScanEntryWireState.TypeOffset;
			else if (field == MuiDirlistRecordField.SizeLow)
				offset = MuiDirlistScanEntryWireState.SizeLowOffset;
			else if (field == MuiDirlistRecordField.SizeHigh)
				offset = MuiDirlistScanEntryWireState.SizeHighOffset;
			else if (field == MuiDirlistRecordField.Protection)
				offset = MuiDirlistScanEntryWireState.ProtectionOffset;
			else if (field == MuiDirlistRecordField.Days)
				offset = MuiDirlistScanEntryWireState.DaysOffset;
			else if (field == MuiDirlistRecordField.Mins)
				offset = MuiDirlistScanEntryWireState.MinsOffset;
			else if (field == MuiDirlistRecordField.Ticks)
				offset = MuiDirlistScanEntryWireState.TicksOffset;
			else return false;
			return true;
		}
		if (record == MuiDirlistRecordKind.SortState)
		{
			size = MuiDirlistSortStateRecord.Size;
			if (field == MuiDirlistRecordField.Magic)
				offset = MuiDirlistSortStateRecord.MagicOffset;
			else if (field == MuiDirlistRecordField.SortTypeValue)
				offset = MuiDirlistSortStateRecord.SortTypeOffset;
			else if (field == MuiDirlistRecordField.SortDirsValue)
				offset = MuiDirlistSortStateRecord.SortDirsOffset;
			else if (field == MuiDirlistRecordField.SortHighLowValue)
				offset = MuiDirlistSortStateRecord.SortHighLowOffset;
			else return false;
			return true;
		}
		if (record == MuiDirlistRecordKind.FilterState)
		{
			size = MuiDirlistFilterStateRecord.Size;
			if (field == MuiDirlistRecordField.Magic)
				offset = MuiDirlistFilterStateRecord.MagicOffset;
			else if (field == MuiDirlistRecordField.AcceptPatternValue)
				offset = MuiDirlistFilterStateRecord.AcceptPatternOffset;
			else if (field == MuiDirlistRecordField.RejectPatternValue)
				offset = MuiDirlistFilterStateRecord.RejectPatternOffset;
			else if (field == MuiDirlistRecordField.PatternValue)
				offset = MuiDirlistFilterStateRecord.PatternOffset;
			else if (field == MuiDirlistRecordField.DrawersOnlyValue)
				offset = MuiDirlistFilterStateRecord.DrawersOnlyOffset;
			else if (field == MuiDirlistRecordField.FilesOnlyValue)
				offset = MuiDirlistFilterStateRecord.FilesOnlyOffset;
			else if (field == MuiDirlistRecordField.FilterDrawersValue)
				offset = MuiDirlistFilterStateRecord.FilterDrawersOffset;
			else if (field == MuiDirlistRecordField.MultiSelDirsValue)
				offset = MuiDirlistFilterStateRecord.MultiSelDirsOffset;
			else if (field == MuiDirlistRecordField.RejectIconsValue)
				offset = MuiDirlistFilterStateRecord.RejectIconsOffset;
			else if (field == MuiDirlistRecordField.ExAllTypeValue)
				offset = MuiDirlistFilterStateRecord.ExAllTypeOffset;
			else if (field == MuiDirlistRecordField.FilterHookValue)
				offset = MuiDirlistFilterStateRecord.FilterHookOffset;
			else return false;
			return true;
		}
		if (record == MuiDirlistRecordKind.ScanState)
		{
			size = MuiDirlistScanStateRecord.Size;
			if (field == MuiDirlistRecordField.Magic)
				offset = MuiDirlistScanStateRecord.MagicOffset;
			else if (field == MuiDirlistRecordField.StatusValue)
				offset = MuiDirlistScanStateRecord.StatusOffset;
			else if (field == MuiDirlistRecordField.NumFilesValue)
				offset = MuiDirlistScanStateRecord.NumFilesOffset;
			else if (field == MuiDirlistRecordField.NumDrawersValue)
				offset = MuiDirlistScanStateRecord.NumDrawersOffset;
			else if (field == MuiDirlistRecordField.NumBytesValue)
				offset = MuiDirlistScanStateRecord.NumBytesOffset;
			else if (field == MuiDirlistRecordField.IoErrValue)
				offset = MuiDirlistScanStateRecord.IoErrOffset;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR recordAddress, MuiDirlistRecordKind record,
		MuiDirlistRecordField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(record, field, out var offset, out var size) ||
			recordAddress.IsNull || recordAddress.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(recordAddress, size)) return false;
		address = APTR.FromPointer(recordAddress.Raw + offset);
		return platform.IsMapped(address, MuiDirlistByteTotalState.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiDirlistRecordKind record, MuiDirlistRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiDirlistRecordKind record, MuiDirlistRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Dirlist record cursor. Live record codecs use the direct memory adapter.
internal static class MuiDirlistRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiDirlistRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiDirlistRecordMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiDirlistRecordKind record, MuiDirlistRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiDirlistRecordMemoryCodec.TryReadUInt32(ref platform, address, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiDirlistRecordKind record, MuiDirlistRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiDirlistRecordMemoryCodec.TryWriteUInt32(ref platform, address, record,
			field, value);
}

internal static class MuiDirlistSortStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiDirlistSortStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistSortStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SortType) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SortDirs) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SortHighLow) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiDirlistSortStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistSortStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SortType) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SortDirs) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SortHighLow) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiDirlistSortStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDirlistSortStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadStructural(ref platform, address, out value) &&
			MuiDirlistStateAdmission.ValidateSort(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDirlistSortStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiDirlistStateAdmission.ValidateSort(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiDirlistFilterStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistFilterStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var acceptPattern) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rejectPattern) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var pattern) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.DrawersOnly) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FilesOnly) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.FilterDrawers) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MultiSelDirs) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.RejectIcons) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ExAllType) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var filterHook) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.AcceptPattern = APTR.FromPointer(acceptPattern);
		value.RejectPattern = APTR.FromPointer(rejectPattern);
		value.Pattern = APTR.FromPointer(pattern);
		value.FilterHook = APTR.FromPointer(filterHook);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistFilterStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AcceptPattern.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.RejectPattern.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Pattern.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.DrawersOnly) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FilesOnly) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FilterDrawers) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MultiSelDirs) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.RejectIcons) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ExAllType) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.FilterHook.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadStructural(ref platform, address, out value) &&
			MuiDirlistStateAdmission.ValidateFilter(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiDirlistStateAdmission.ValidateFilter(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiDirlistScanStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiDirlistScanStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistScanStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Status) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.NumFiles) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.NumDrawers) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.NumBytes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var ioErr) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.IoErr = unchecked((int)ioErr);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiDirlistScanStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistScanStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Status) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NumFiles) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NumDrawers) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NumBytes) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.IoErr)) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiDirlistScanStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDirlistScanStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadStructural(ref platform, address, out value) &&
			MuiDirlistStateAdmission.ValidateScan(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDirlistScanStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiDirlistStateAdmission.ValidateScan(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiDirlistScanEntryWireCodec
{
	// The transient ExAll header is a declaration-ordered sequence of ULONGs;
	// its inline name/comment payload remains outside this fixed record.
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiDirlistScanEntryWireState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistScanEntryWireState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var type) || !MuiGuestStructCursor.TryReadUInt32(ref platform,
				ref cursor, out var sizeLow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var sizeHigh) || !MuiGuestStructCursor.TryReadUInt32(
				ref platform, ref cursor, out var protection) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var days) || !MuiGuestStructCursor.TryReadUInt32(ref platform,
				ref cursor, out var mins) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var ticks) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Type = unchecked((int)type);
		value.SizeLow = sizeLow;
		value.SizeHigh = sizeHigh;
		value.Protection = protection;
		value.Days = days;
		value.Mins = mins;
		value.Ticks = ticks;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiDirlistScanEntryWireState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiDirlistScanEntryWireState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Type)) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SizeLow) || !MuiGuestStructCursor.TryWriteUInt32(
				ref platform, ref cursor, value.SizeHigh) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Protection) || !MuiGuestStructCursor.TryWriteUInt32(
				ref platform, ref cursor, value.Days) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Mins) || !MuiGuestStructCursor.TryWriteUInt32(
				ref platform, ref cursor, value.Ticks)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}
}

// Dirlist.mui (autodoc MUI_Dirlist.doc). Dirlist is a subclass of List that
// shows the entries of a directory. It is built directly on the shared
// MuiListCore backbone: each displayed entry is an owned, guest-resident
// FileInfoBlock-like record (see the Fib* layout below) placed through the
// backbone's SlotOwnedRecord ownership, so clear/disposal frees every record
// automatically and failure-atomically.
//
// Directory scanning is synchronous and bounded, delegated to the narrow
// IMuiDirectoryCapability seam that every collection platform now aggregates.
// A scan that cannot start (missing/unreadable directory) or fails mid-way
// leaves the object in a clean state: the list is emptied, MUIA_Dirlist_Status
// becomes MUIV_Dirlist_Status_Invalid, the counters are zeroed and the
// dos.library IoErr() value is captured for MUIM_Dirlist_* callers. No managed
// allocations, arrays, collections, delegates, LINQ or exceptions are used;
// every mutation flows through the guest-memory platform seam.
public static class MuiDirlistCore
{
	// ---- Attribute identifiers (autodoc MUI_Dirlist.doc) ---------------------
	private const uint AcceptPattern = 0x8042760au; // [IS.] STRPTR
	private const uint Directory = 0x8042ea41u;     // [ISG] STRPTR
	private const uint DrawersOnly = 0x8042b379u;   // [IS.] BOOL
	private const uint ExAllType = 0x8042cd7cu;     // [I.G] ULONG
	private const uint FilesOnly = 0x8042896au;     // [IS.] BOOL
	private const uint FilterDrawers = 0x80424ad2u; // [IS.] BOOL
	private const uint FilterHook = 0x8042ae19u;    // [IS.] struct Hook *
	private const uint MultiSelDirs = 0x80428653u;  // [IS.] BOOL
	private const uint NumBytes = 0x80429e26u;      // [..G] LONG
	private const uint NumBytes64 = 0x80428050u;    // [..G] QUAD *
	private const uint NumDrawers = 0x80429cb8u;    // [..G] LONG
	private const uint NumFiles = 0x8042a6f0u;      // [..G] LONG
	private const uint Path = 0x80426176u;          // [..G] STRPTR
	private const uint Pattern = 0x8042c761u;       // [IS.] STRPTR
	private const uint RejectIcons = 0x80424808u;   // [IS.] BOOL
	private const uint RejectPattern = 0x804259c7u; // [IS.] STRPTR
	private const uint SortDirs = 0x8042bbb9u;      // [IS.] LONG
	private const uint SortHighLow = 0x80421896u;   // [IS.] BOOL
	private const uint SortType = 0x804228bcu;      // [IS.] LONG
	private const uint Status = 0x804240deu;        // [..G] LONG

	// MUIA_List_Active, needed to compute MUIA_Dirlist_Path.
	private const uint ListActive = 0x8042391cu;

	// ---- Status / selector values --------------------------------------------
	public const uint StatusInvalid = 0;
	public const uint StatusReading = 1;
	public const uint StatusValid = 2;

	private const uint SortTypeName = 0;
	private const uint SortTypeDate = 1;
	private const uint SortTypeSize = 2;
	private const uint SortTypeComment = 3;
	private const uint SortTypeFlags = 4;
	private const uint SortTypeType = 5;

	private const uint SortDirsFirst = 0;
	private const uint SortDirsLast = 1;
	private const uint SortDirsMix = 2;

	// AmigaDOS IoErr() codes used by the clean-failure paths.
	private const int ErrorObjectNotFound = 205;
	private const int ErrorNoFreeStore = 103;

	// ---- Private guest dataspace keys (owned copies) -------------------------
	private const uint DirectoryKey = 0x0D100001u;
	private const uint AcceptKey = 0x0D100002u;
	private const uint RejectKey = 0x0D100003u;
	private const uint PatternKey = 0x0D100004u;
	private const uint NumBytes64Key = 0x0D100005u;
	private const uint PathKey = 0x0D100006u;
	private const uint SortStateKey = 0x0D100007u;
	private const uint FilterStateKey = 0x0D100008u;
	private const uint ScanStateKey = 0x0D100009u;
	// Private per-object attribute holding the last IoErr() value.
	private const uint IoErrKey = 0x0D100010u;

	// ---- Bounds --------------------------------------------------------------
	private const uint MaxName = 108;
	private const uint MaxComment = 80;
	private const uint MaxEntryRecord = 228;
	private const uint MaxPath = 1024;
	private const uint MaxPattern = 256;
	private const int MaxScanEntries = 65536;

	// ---- Scan-entry scratch layout (written by IMuiDirectoryCapability) ------
	public const int ScanType = 0;
	public const int ScanSizeLow = 4;
	public const int ScanSizeHigh = 8;
	public const int ScanProtection = 12;
	public const int ScanDays = 16;
	public const int ScanMins = 20;
	public const int ScanTicks = 24;
	public const int ScanName = (int)MuiDirlistScanEntryWireState.NameOffset;
	public const int ScanComment = (int)MuiDirlistScanEntryWireState.CommentOffset;
	public const uint ScanEntrySize = MuiDirlistScanEntryWireState.TotalSize;
	public const uint ByteTotalSize = MuiDirlistByteTotalState.Size;

	// Public field offsets for entry inspection (GetEntry returns the record).
	public const int EntryTypeOffset = 4;
	public const int EntrySizeOffset = 8;
	public const int EntryProtectionOffset = 16;
	public const int EntryNameOffset = (int)MuiDirlistEntryWireState.NameOffset;

	// ---- Construction --------------------------------------------------------

	// Create a Dirlist, failure-atomically. The shared List backbone is
	// constructed, defaults applied, creation-time Directory/pattern strings are
	// copied into owned buffers, and (if a directory was supplied) an initial
	// synchronous scan is performed. A scan failure is a normal Invalid state,
	// not a construction error; only an allocation failure fails construction.
	public static APTR CreateDirlist<TPlatform>(ref TPlatform platform, APTR state,
		APTR classRecord, APTR tags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.ClassifyRecord(ref platform, classRecord) !=
			MuiCollectionClass.Dirlist) return APTR.Null;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull) return APTR.Null;
		if (!MuiListCore.Construct(ref platform, state, classRecord, obj) ||
			!Setup(ref platform, state, obj))
		{
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		return obj;
	}

	private static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListCore.HasBackbone(ref platform, state, obj)) return false;
		EnsureDefault(ref platform, state, obj, SortType, SortTypeName);
		EnsureDefault(ref platform, state, obj, SortDirs, SortDirsFirst);
		EnsureDefault(ref platform, state, obj, SortHighLow, 0);
		NormalizeSortState(ref platform, state, obj);
		if (!EnsureSortStateRecord(ref platform, state, obj)) return false;
		PublishScanState(ref platform, state, obj, default, false);

		if (!OwnCreationString(ref platform, state, obj, AcceptPattern, AcceptKey) ||
			!OwnCreationString(ref platform, state, obj, RejectPattern, RejectKey) ||
			!OwnCreationString(ref platform, state, obj, Pattern, PatternKey))
			return false;
		NormalizeFilterState(ref platform, state, obj);
		if (!EnsureFilterStateRecord(ref platform, state, obj)) return false;

		var rawDir = APTR.FromPointer(Read(ref platform, state, obj, Directory, 0));
		if (rawDir.IsNull) return true;
		if (!OwnString(ref platform, state, obj, DirectoryKey, rawDir, MaxPath))
			return false;
		SetInternal(ref platform, state, obj, Directory,
			MuiStoreCore.DataspaceFind(ref platform, state, obj, DirectoryKey).Raw);
		// Ignore the scan result: a bad directory is a valid Invalid state.
		ReRead(ref platform, state, obj);
		return true;
	}

	internal static bool IsFilterAttribute(uint attribute) =>
		attribute == AcceptPattern || attribute == RejectPattern ||
		attribute == Pattern || attribute == DrawersOnly ||
		attribute == FilesOnly || attribute == FilterDrawers ||
		attribute == MultiSelDirs || attribute == RejectIcons ||
		attribute == ExAllType || attribute == FilterHook;

	// Public getter projection for the shared Dirlist/Volumelist policy surface.
	// The named records are authoritative once construction has completed;
	// bootstrap-only reads stay on GetRawAttribute below so this route cannot
	// recurse while those records are being created or synchronized.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Directory || attribute == NumBytes64 ||
		attribute == Status || attribute == NumFiles ||
		attribute == NumDrawers || attribute == NumBytes ||
		attribute == Path || IsFilterAttribute(attribute) ||
		IsSortAttribute(attribute);

	private static bool ValidateOwnedPattern<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint key, APTR value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var expected = MuiStoreCore.DataspaceFind(ref platform, state, obj, key);
		if (expected.Raw != value.Raw) return false;
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj, key);
		if (value.IsNull) return length == 0;
		return length > 0 && length <= MaxPattern + 1 &&
			CStringCodec.TryReadLength(ref platform, value, MaxPattern + 1,
				out _);
	}

	private static bool ValidateFilterStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiDirlistStateAdmission.ValidateFilter(value) &&
		ValidateOwnedPattern(ref platform, state, obj, AcceptKey,
			value.AcceptPattern) &&
		ValidateOwnedPattern(ref platform, state, obj, RejectKey,
			value.RejectPattern) &&
		ValidateOwnedPattern(ref platform, state, obj, PatternKey,
			value.Pattern);

	private static bool HasFilterStateStorage<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiStoreCore.DataspaceLength(ref platform, state, obj,
			FilterStateKey) == unchecked((int)MuiDirlistFilterStateRecord.Size);

	private static bool HasSortStateStorage<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiStoreCore.DataspaceLength(ref platform, state, obj,
			SortStateKey) == unchecked((int)MuiDirlistSortStateRecord.Size);

	private static bool HasScanStateStorage<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ScanStateKey) == unchecked((int)MuiDirlistScanStateRecord.Size);

	private static bool TryReadFilterStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			FilterStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			FilterStateKey) != unchecked((int)MuiDirlistFilterStateRecord.Size))
			return false;
		return MuiDirlistFilterStateRecordCodec.TryRead(ref platform, block,
			out value) && ValidateFilterStateRecord(ref platform, state, obj,
			value);
	}

	private static void FillFilterStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, ref MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value.Magic = MuiDirlistFilterStateRecord.Cookie;
		value.AcceptPattern = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			AcceptKey);
		value.RejectPattern = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RejectKey);
		value.Pattern = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PatternKey);
		value.DrawersOnly = Read(ref platform, state, obj, DrawersOnly, 0) == 0 ?
			0u : 1u;
		value.FilesOnly = Read(ref platform, state, obj, FilesOnly, 0) == 0 ?
			0u : 1u;
		value.FilterDrawers = Read(ref platform, state, obj, FilterDrawers, 0) ==
			0 ? 0u : 1u;
		value.MultiSelDirs = Read(ref platform, state, obj, MultiSelDirs, 0) ==
			0 ? 0u : 1u;
		value.RejectIcons = Read(ref platform, state, obj, RejectIcons, 0) == 0 ?
			0u : 1u;
		value.ExAllType = Read(ref platform, state, obj, ExAllType, 0);
		value.FilterHook = APTR.FromPointer(Read(ref platform, state, obj,
			FilterHook, 0));
	}

	private static bool EnsureFilterStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadFilterStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiDirlistFilterStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiDirlistFilterStateRecord.Size);
		var value = default(MuiDirlistFilterStateRecord);
		FillFilterStateRecord(ref platform, state, obj, ref value);
		var written = MuiDirlistFilterStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			FilterStateKey, scratch,
			unchecked((int)MuiDirlistFilterStateRecord.Size));
		platform.Clear(scratch, MuiDirlistFilterStateRecord.Size);
		platform.Free(scratch, MuiDirlistFilterStateRecord.Size);
		return added;
	}

	private static bool SyncFilterStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureFilterStateRecord(ref platform, state, obj) ||
			!TryReadFilterStateRecord(ref platform, state, obj, out var value))
			return false;
		FillFilterStateRecord(ref platform, state, obj, ref value);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			FilterStateKey);
		return MuiDirlistFilterStateRecordCodec.Write(ref platform, block, value);
	}

	internal static bool TryGetFilterStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiDirlistFilterStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadFilterStateRecord(ref platform, state, obj, out value);

	// Public struct-first seam shared by Dirlist and Volumelist filtering. The
	// pattern pointers are the object-owned guest copies, not caller buffers.
	public static bool TryReadFilterState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiDirlistFilterState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var cls = MuiListCore.Classify(ref platform, state, obj);
		if (cls != MuiCollectionClass.Dirlist &&
			cls != MuiCollectionClass.Volumelist) return false;
		if (TryReadFilterStateRecord(ref platform, state, obj, out var stored))
		{
			result.AcceptPattern = stored.AcceptPattern;
			result.RejectPattern = stored.RejectPattern;
			result.Pattern = stored.Pattern;
			result.DrawersOnly = stored.DrawersOnly;
			result.FilesOnly = stored.FilesOnly;
			result.FilterDrawers = stored.FilterDrawers;
			result.MultiSelDirs = stored.MultiSelDirs;
			result.RejectIcons = stored.RejectIcons;
			result.ExAllType = stored.ExAllType;
			result.FilterHook = stored.FilterHook;
			return true;
		}
		// A correctly sized record that fails admission is malformed, not a
		// bootstrap miss. Do not fall back to raw attributes and let invalid
		// policy reach filtering or hook dispatch.
		if (HasFilterStateStorage(ref platform, state, obj)) return false;
		result.AcceptPattern = MuiStoreCore.DataspaceFind(ref platform, state,
			obj, AcceptKey);
		result.RejectPattern = MuiStoreCore.DataspaceFind(ref platform, state,
			obj, RejectKey);
		result.Pattern = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PatternKey);
		result.DrawersOnly = Read(ref platform, state, obj, DrawersOnly, 0) == 0 ?
			0u : 1u;
		result.FilesOnly = Read(ref platform, state, obj, FilesOnly, 0) == 0 ?
			0u : 1u;
		result.FilterDrawers = Read(ref platform, state, obj, FilterDrawers, 0) == 0 ?
			0u : 1u;
		result.MultiSelDirs = Read(ref platform, state, obj, MultiSelDirs, 0) == 0 ?
			0u : 1u;
		result.RejectIcons = Read(ref platform, state, obj, RejectIcons, 0) == 0 ?
			0u : 1u;
		result.ExAllType = Read(ref platform, state, obj, ExAllType, 0);
		result.FilterHook = APTR.FromPointer(Read(ref platform, state, obj,
			FilterHook, 0));
		return true;
	}

	private static void NormalizeFilterState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		SetInternal(ref platform, state, obj, DrawersOnly,
			Read(ref platform, state, obj, DrawersOnly, 0) == 0 ? 0u : 1u);
		SetInternal(ref platform, state, obj, FilesOnly,
			Read(ref platform, state, obj, FilesOnly, 0) == 0 ? 0u : 1u);
		SetInternal(ref platform, state, obj, FilterDrawers,
			Read(ref platform, state, obj, FilterDrawers, 0) == 0 ? 0u : 1u);
		SetInternal(ref platform, state, obj, MultiSelDirs,
			Read(ref platform, state, obj, MultiSelDirs, 0) == 0 ? 0u : 1u);
		SetInternal(ref platform, state, obj, RejectIcons,
			Read(ref platform, state, obj, RejectIcons, 0) == 0 ? 0u : 1u);
	}

	private static bool TryReadSortStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiDirlistSortStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SortStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			SortStateKey) != unchecked((int)MuiDirlistSortStateRecord.Size))
			return false;
		return MuiDirlistSortStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureSortStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadSortStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiDirlistSortStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiDirlistSortStateRecord.Size);
		var value = default(MuiDirlistSortStateRecord);
		value.Magic = MuiDirlistSortStateRecord.Cookie;
		value.SortType = NormalizeSortType(Read(ref platform, state, obj,
			SortType, SortTypeName));
		value.SortDirs = NormalizeSortDirs(Read(ref platform, state, obj,
			SortDirs, SortDirsFirst));
		value.SortHighLow = Read(ref platform, state, obj, SortHighLow, 0) == 0 ?
			0u : 1u;
		var written = MuiDirlistSortStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			SortStateKey, scratch,
			unchecked((int)MuiDirlistSortStateRecord.Size));
		platform.Clear(scratch, MuiDirlistSortStateRecord.Size);
		platform.Free(scratch, MuiDirlistSortStateRecord.Size);
		return added;
	}

	private static bool SyncSortStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureSortStateRecord(ref platform, state, obj) ||
			!TryReadSortStateRecord(ref platform, state, obj, out var value))
			return false;
		value.SortType = NormalizeSortType(Read(ref platform, state, obj,
			SortType, SortTypeName));
		value.SortDirs = NormalizeSortDirs(Read(ref platform, state, obj,
			SortDirs, SortDirsFirst));
		value.SortHighLow = Read(ref platform, state, obj, SortHighLow, 0) == 0 ?
			0u : 1u;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			SortStateKey);
		return MuiDirlistSortStateRecordCodec.Write(ref platform, block, value);
	}

	internal static bool TryGetSortStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiDirlistSortStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadSortStateRecord(ref platform, state, obj, out value);

	internal static bool IsSortAttribute(uint attribute) =>
		attribute == SortType || attribute == SortDirs ||
		attribute == SortHighLow;

	public static bool TryReadSortState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiDirlistSortState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var cls = MuiListCore.Classify(ref platform, state, obj);
		if (cls != MuiCollectionClass.Dirlist &&
			cls != MuiCollectionClass.Volumelist) return false;
		if (TryReadSortStateRecord(ref platform, state, obj, out var stored))
		{
			result.SortType = stored.SortType;
			result.SortDirs = stored.SortDirs;
			result.SortHighLow = stored.SortHighLow;
			return true;
		}
		if (HasSortStateStorage(ref platform, state, obj)) return false;
		result.SortType = NormalizeSortType(Read(ref platform, state, obj,
			SortType, SortTypeName));
		result.SortDirs = NormalizeSortDirs(Read(ref platform, state, obj,
			SortDirs, SortDirsFirst));
		result.SortHighLow = Read(ref platform, state, obj, SortHighLow, 0) == 0 ?
			0u : 1u;
		return true;
	}

	private static void NormalizeSortState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		SetInternal(ref platform, state, obj, SortType,
			NormalizeSortType(Read(ref platform, state, obj, SortType,
				SortTypeName)));
		SetInternal(ref platform, state, obj, SortDirs,
			NormalizeSortDirs(Read(ref platform, state, obj, SortDirs,
				SortDirsFirst)));
		SetInternal(ref platform, state, obj, SortHighLow,
			Read(ref platform, state, obj, SortHighLow, 0) == 0 ? 0u : 1u);
	}

	private static uint NormalizeSortType(uint value) =>
		value <= SortTypeType ? value : SortTypeName;

	private static uint NormalizeSortDirs(uint value) =>
		value <= SortDirsMix ? value : SortDirsFirst;

	private static bool TryReadScanStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiDirlistScanStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ScanStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ScanStateKey) != unchecked((int)MuiDirlistScanStateRecord.Size))
			return false;
		return MuiDirlistScanStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureScanStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadScanStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiDirlistScanStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiDirlistScanStateRecord.Size);
		var value = default(MuiDirlistScanStateRecord);
		value.Magic = MuiDirlistScanStateRecord.Cookie;
		value.Status = Read(ref platform, state, obj, Status, StatusInvalid);
		value.NumFiles = Read(ref platform, state, obj, NumFiles, 0);
		value.NumDrawers = Read(ref platform, state, obj, NumDrawers, 0);
		value.NumBytes = Read(ref platform, state, obj, NumBytes, 0);
		value.IoErr = unchecked((int)Read(ref platform, state, obj, IoErrKey, 0));
		var written = MuiDirlistScanStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ScanStateKey, scratch,
			unchecked((int)MuiDirlistScanStateRecord.Size));
		platform.Clear(scratch, MuiDirlistScanStateRecord.Size);
		platform.Free(scratch, MuiDirlistScanStateRecord.Size);
		return added;
	}

	internal static bool TryGetScanStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiDirlistScanStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadScanStateRecord(ref platform, state, obj, out value);

	public static bool TryReadScanState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiDirlistScanState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var cls = MuiListCore.Classify(ref platform, state, obj);
		if (cls != MuiCollectionClass.Dirlist &&
			cls != MuiCollectionClass.Volumelist) return false;
		if (TryReadScanStateRecord(ref platform, state, obj, out var stored))
		{
			result.Status = stored.Status;
			result.NumFiles = stored.NumFiles;
			result.NumDrawers = stored.NumDrawers;
			result.NumBytes = stored.NumBytes;
			result.IoErr = stored.IoErr;
			return true;
		}
		if (HasScanStateStorage(ref platform, state, obj)) return false;
		result.Status = Read(ref platform, state, obj, Status, StatusInvalid);
		result.NumFiles = Read(ref platform, state, obj, NumFiles, 0);
		result.NumDrawers = Read(ref platform, state, obj, NumDrawers, 0);
		result.NumBytes = Read(ref platform, state, obj, NumBytes, 0);
		result.IoErr = unchecked((int)Read(ref platform, state, obj, IoErrKey,
			0));
		return true;
	}

	// Decode one owned FileInfoBlock-like entry through a bounded named record.
	// This is the sole reader for the variable-length entry fields used by
	// sorting, path construction, mutation, and public entry inspection.
	public static bool TryReadEntryState<TPlatform>(ref TPlatform platform,
		APTR entry, out MuiDirlistEntryState result)
		where TPlatform : struct, IMuiGuestMemory
	{
		result = default;
		if (entry.IsNull || entry.Raw > uint.MaxValue -
			MuiDirlistEntryWireState.NameOffset ||
			!MuiDirlistEntryWireCodec.TryRead(ref platform, entry,
				out var wire)) return false;
		var recordSize = wire.RecordSize;
		if (recordSize < MuiDirlistEntryWireState.NameOffset + 1 ||
			recordSize > MaxEntryRecord ||
			!platform.IsMapped(entry, recordSize)) return false;

		var name = APTR.FromPointer(entry.Raw + MuiDirlistEntryWireState.NameOffset);
		var nameLimit = recordSize - MuiDirlistEntryWireState.NameOffset;
		if (nameLimit > MaxName) nameLimit = MaxName;
		if (nameLimit == 0 || !CStringCodec.TryReadLength(ref platform, name,
			nameLimit, out var nameLength)) return false;

		var commentOffset = wire.CommentOffset;
		APTR comment;
		uint commentLength;
		if (commentOffset == 0)
		{
			// Keep compatibility with the historical fallback used by the
			// comparison path for records without a separate comment string.
			comment = name;
			commentLength = nameLength;
		}
		else
		{
			var minimumComment = MuiDirlistEntryWireState.NameOffset +
				nameLength + 1;
			if (commentOffset < minimumComment || commentOffset >= recordSize)
				return false;
			var commentLimit = recordSize - commentOffset;
			if (commentLimit > MaxComment + 1) commentLimit = MaxComment + 1;
			comment = APTR.FromPointer(entry.Raw + commentOffset);
			if (!CStringCodec.TryReadLength(ref platform, comment, commentLimit,
				out commentLength)) return false;
		}

		result.Address = entry;
		result.RecordSize = recordSize;
		result.Type = wire.Type;
		result.SizeLow = wire.SizeLow;
		result.SizeHigh = wire.SizeHigh;
		result.Protection = wire.Protection;
		result.Days = wire.Days;
		result.Mins = wire.Mins;
		result.Ticks = wire.Ticks;
		result.Name = name;
		result.NameLength = nameLength;
		result.Comment = comment;
		result.CommentLength = commentLength;
		return true;
	}

	// Write one bounded owned FileInfoBlock-like record from a named target
	// state and source strings. The fixed ABI offsets live only in this codec;
	// callers retain typed fields and guest pointers.
	private static bool WriteEntryRecord<TPlatform>(ref TPlatform platform,
		MuiDirlistEntryState target, APTR sourceName, APTR sourceComment)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (target.Address.IsNull || target.RecordSize <
			MuiDirlistEntryWireState.NameOffset + 1 ||
			target.RecordSize > MaxEntryRecord ||
			!platform.IsMapped(target.Address, target.RecordSize) ||
			sourceName.IsNull || target.Name.IsNull || target.Comment.IsNull ||
			target.NameLength > MaxName || target.CommentLength > MaxComment ||
			!CStringCodec.TryReadLength(ref platform, sourceName, MaxName,
				out var nameLength) || nameLength != target.NameLength)
			return false;
		if (sourceComment.IsNotNull &&
			(!CStringCodec.TryReadLength(ref platform, sourceComment, MaxComment,
				out var commentLength) || commentLength != target.CommentLength))
			return false;
		if (sourceComment.IsNull && target.CommentLength != 0) return false;
		if (target.Name.Raw < target.Address.Raw ||
			target.Comment.Raw < target.Address.Raw ||
			target.Name.Raw - target.Address.Raw !=
				MuiDirlistEntryWireState.NameOffset)
			return false;
		var commentOffset = target.Comment.Raw - target.Address.Raw;
		if (commentOffset < MuiDirlistEntryWireState.NameOffset +
			target.NameLength + 1 ||
			commentOffset >= target.RecordSize ||
			target.RecordSize - commentOffset < target.CommentLength + 1)
			return false;

		var wire = default(MuiDirlistEntryWireState);
		wire.RecordSize = target.RecordSize;
		wire.Type = target.Type;
		wire.SizeLow = target.SizeLow;
		wire.SizeHigh = target.SizeHigh;
		wire.Protection = target.Protection;
		wire.Days = target.Days;
		wire.Mins = target.Mins;
		wire.Ticks = target.Ticks;
		wire.CommentOffset = commentOffset;
		if (!MuiDirlistEntryWireCodec.Write(ref platform, target.Address, wire))
			return false;
		for (var i = 0u; i < target.NameLength; i++)
			platform.WriteUInt8(target.Name, (int)i,
				platform.ReadUInt8(sourceName, (int)i));
		platform.WriteUInt8(target.Name, (int)target.NameLength, 0);
		for (var i = 0u; i < target.CommentLength; i++)
			platform.WriteUInt8(target.Comment, (int)i,
				platform.ReadUInt8(sourceComment, (int)i));
		platform.WriteUInt8(target.Comment, (int)target.CommentLength, 0);
		return true;
	}

	private static bool WriteEntryProtection<TPlatform>(ref TPlatform platform,
		MuiDirlistEntryState entry, uint protection)
		where TPlatform : struct, IMuiGuestMemory
	{
		entry.Protection = protection;
		return WriteEntryRecord(ref platform, entry, entry.Name, entry.Comment);
	}

	// Decode the bounded fixed-size scratch payload returned by the directory
	// capability. A malformed name rejects the entry; a malformed comment is
	// treated as an empty comment for compatibility with the existing builder.
	public static bool TryReadScanEntryState<TPlatform>(ref TPlatform platform,
		APTR scratch, out MuiDirlistScanEntryState result)
		where TPlatform : struct, IMuiGuestMemory
	{
		result = default;
		if (scratch.IsNull || scratch.Raw > uint.MaxValue - ScanEntrySize ||
			!MuiDirlistScanEntryWireCodec.TryRead(ref platform, scratch,
				out var wire) || !platform.IsMapped(scratch, ScanEntrySize))
			return false;
		var name = APTR.FromPointer(scratch.Raw +
			MuiDirlistScanEntryWireState.NameOffset);
		if (!CStringCodec.TryReadLength(ref platform, name, MaxName,
			out var nameLength)) return false;
		var comment = APTR.FromPointer(scratch.Raw +
			MuiDirlistScanEntryWireState.CommentOffset);
		var commentLength = 0u;
		if (!CStringCodec.TryReadLength(ref platform, comment, MaxComment,
			out commentLength)) comment = APTR.Null;

		result.Address = scratch;
		result.Type = wire.Type;
		result.SizeLow = wire.SizeLow;
		result.SizeHigh = wire.SizeHigh;
		result.Protection = wire.Protection;
		result.Days = wire.Days;
		result.Mins = wire.Mins;
		result.Ticks = wire.Ticks;
		result.Name = name;
		result.NameLength = nameLength;
		result.Comment = comment;
		result.CommentLength = commentLength;
		return true;
	}

	// Write a named scan-entry state back to the fixed capability payload. The
	// source strings may reside in the same scratch block; all fixed fields are
	// written before the variable bytes are copied.
	public static bool WriteScanEntryState<TPlatform>(ref TPlatform platform,
		APTR scratch, MuiDirlistScanEntryState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (scratch.IsNull || scratch.Raw > uint.MaxValue - ScanEntrySize ||
			!platform.IsMapped(scratch, ScanEntrySize) || value.Name.IsNull ||
			!CStringCodec.TryReadLength(ref platform, value.Name, MaxName,
				out var nameLength)) return false;
		var commentLength = 0u;
		if (value.Comment.IsNotNull && !CStringCodec.TryReadLength(ref platform,
			value.Comment, MaxComment, out commentLength)) commentLength = 0;
		if (nameLength != value.NameLength) return false;

		var wire = default(MuiDirlistScanEntryWireState);
		wire.Type = value.Type;
		wire.SizeLow = value.SizeLow;
		wire.SizeHigh = value.SizeHigh;
		wire.Protection = value.Protection;
		wire.Days = value.Days;
		wire.Mins = value.Mins;
		wire.Ticks = value.Ticks;
		if (!MuiDirlistScanEntryWireCodec.Write(ref platform, scratch, wire))
			return false;
		var name = APTR.FromPointer(scratch.Raw +
			MuiDirlistScanEntryWireState.NameOffset);
		for (var i = 0u; i < nameLength; i++)
			platform.WriteUInt8(name, (int)i,
				platform.ReadUInt8(value.Name, (int)i));
		platform.WriteUInt8(name, (int)nameLength, 0);
		var comment = APTR.FromPointer(scratch.Raw +
			MuiDirlistScanEntryWireState.CommentOffset);
		for (var i = 0u; i < commentLength; i++)
			platform.WriteUInt8(comment, (int)i,
				platform.ReadUInt8(value.Comment, (int)i));
		platform.WriteUInt8(comment, (int)commentLength, 0);
		return true;
	}

	// Emit the deterministic example-volume name without exposing its fixed
	// scratch offsets to the Volumelist producer.
	internal static bool WriteExampleVolumeEntry<TPlatform>(ref TPlatform platform,
		APTR scratch, byte digit) where TPlatform : struct, IMuiGuestMemory
	{
		if (scratch.IsNull || scratch.Raw > uint.MaxValue - ScanEntrySize ||
			!platform.IsMapped(scratch, ScanEntrySize)) return false;
		var wire = default(MuiDirlistScanEntryWireState);
		if (!MuiDirlistScanEntryWireCodec.TryRead(ref platform, scratch,
			out wire)) return false;
		wire.Type = 2;
		if (!MuiDirlistScanEntryWireCodec.Write(ref platform, scratch, wire))
			return false;
		var name = APTR.FromPointer(scratch.Raw +
			MuiDirlistScanEntryWireState.NameOffset);
		return MuiDirlistExampleVolumeNameRecordCodec.WriteRecord(ref platform, name,
			MuiDirlistExampleVolumeNameRecord.Create(digit));
	}

	// Decode the fixed 8-byte guest QUAD used for the 64-bit byte total.
	public static bool TryReadByteTotalState<TPlatform>(ref TPlatform platform,
		APTR total, out MuiDirlistByteTotalState result)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDirlistByteTotalCodec.TryRead(ref platform, total,
			out result);
	}

	// Write the fixed guest QUAD through the same named state boundary.
	public static bool WriteByteTotalState<TPlatform>(ref TPlatform platform,
		APTR total, MuiDirlistByteTotalState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiDirlistByteTotalCodec.Write(ref platform, total, value);
	}

	internal static void PublishScanState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiDirlistScanState value, bool notifyStatus)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		SetInternal(ref platform, state, obj, NumFiles, value.NumFiles);
		SetInternal(ref platform, state, obj, NumDrawers, value.NumDrawers);
		SetInternal(ref platform, state, obj, NumBytes, value.NumBytes);
		SetInternal(ref platform, state, obj, IoErrKey,
			unchecked((uint)value.IoErr));
		if (notifyStatus)
			SetNotify(ref platform, state, obj, Status, value.Status);
		else SetInternal(ref platform, state, obj, Status, value.Status);
		if (!EnsureScanStateRecord(ref platform, state, obj)) return;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ScanStateKey);
		var stored = default(MuiDirlistScanStateRecord);
		stored.Magic = MuiDirlistScanStateRecord.Cookie;
		stored.Status = value.Status;
		stored.NumFiles = value.NumFiles;
		stored.NumDrawers = value.NumDrawers;
		stored.NumBytes = value.NumBytes;
		stored.IoErr = value.IoErr;
		MuiDirlistScanStateRecordCodec.Write(ref platform, block, stored);
	}

	internal static void PublishScanStatus<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint status, bool notifyStatus)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadScanState(ref platform, state, obj, out var scan))
		{
			scan.Status = status;
			PublishScanState(ref platform, state, obj, scan, notifyStatus);
			return;
		}
		if (notifyStatus) SetNotify(ref platform, state, obj, Status, status);
		else SetInternal(ref platform, state, obj, Status, status);
	}

	// ---- Attribute access ----------------------------------------------------

	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (IsFilterAttribute(attribute))
		{
			if (!TryReadFilterState(ref platform, state, obj, out var filter))
			{
				value = 0;
				return false;
			}
			value = attribute == AcceptPattern ? filter.AcceptPattern.Raw :
				attribute == RejectPattern ? filter.RejectPattern.Raw :
				attribute == Pattern ? filter.Pattern.Raw :
				attribute == DrawersOnly ? filter.DrawersOnly :
				attribute == FilesOnly ? filter.FilesOnly :
				attribute == FilterDrawers ? filter.FilterDrawers :
				attribute == MultiSelDirs ? filter.MultiSelDirs :
				attribute == RejectIcons ? filter.RejectIcons :
				attribute == ExAllType ? filter.ExAllType :
				filter.FilterHook.Raw;
			return true;
		}
		if (IsSortAttribute(attribute))
		{
			if (!TryReadSortState(ref platform, state, obj, out var sort))
			{
				value = 0;
				return false;
			}
			value = attribute == SortType ? sort.SortType :
				attribute == SortDirs ? sort.SortDirs : sort.SortHighLow;
			return true;
		}
		if (attribute == Directory)
		{
			value = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				DirectoryKey).Raw;
			return true;
		}
		if (attribute == NumBytes64)
		{
			value = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				NumBytes64Key).Raw;
			return true;
		}
		if (attribute == Status || attribute == NumFiles ||
			attribute == NumDrawers || attribute == NumBytes)
		{
			if (!TryReadScanState(ref platform, state, obj, out var scan))
			{
				value = 0;
				return false;
			}
			value = attribute == Status ? scan.Status :
				attribute == NumFiles ? scan.NumFiles :
				attribute == NumDrawers ? scan.NumDrawers : scan.NumBytes;
			return true;
		}
		if (attribute == Path)
		{
			value = ComputePath(ref platform, state, obj).Raw;
			return true;
		}
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out value);
	}

	// Set a Dirlist attribute. Directory triggers a rescan; pattern strings are
	// copied into owned buffers; sort attributes re-order an already valid list;
	// filter flags take effect on the next scan. Everything else falls through
	// to the generic object store.
	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (attribute == Directory)
			return SetDirectory(ref platform, state, obj, APTR.FromPointer(value));
		if (attribute == AcceptPattern)
			return SetOwnedPattern(ref platform, state, obj, AcceptKey,
				APTR.FromPointer(value));
		if (attribute == RejectPattern)
			return SetOwnedPattern(ref platform, state, obj, RejectKey,
				APTR.FromPointer(value));
		if (attribute == Pattern)
			return SetOwnedPattern(ref platform, state, obj, PatternKey,
				APTR.FromPointer(value));
		if (attribute == SortType || attribute == SortDirs ||
			attribute == SortHighLow)
		{
			var normalized = attribute == SortType ? NormalizeSortType(value) :
				attribute == SortDirs ? NormalizeSortDirs(value) :
				value == 0 ? 0u : 1u;
			SetInternal(ref platform, state, obj, attribute, normalized);
			if (!SyncSortStateRecord(ref platform, state, obj)) return false;
			if (TryReadScanState(ref platform, state, obj, out var scan) &&
				scan.Status == StatusValid)
				SortEntries(ref platform, state, obj);
			return true;
		}
		if (attribute == DrawersOnly || attribute == FilesOnly ||
			attribute == RejectIcons || attribute == FilterDrawers ||
			attribute == MultiSelDirs || attribute == ExAllType ||
			attribute == FilterHook)
		{
			var normalized = attribute == DrawersOnly || attribute == FilesOnly ||
				attribute == RejectIcons || attribute == FilterDrawers ||
				attribute == MultiSelDirs ? (value == 0 ? 0u : 1u) : value;
			SetInternal(ref platform, state, obj, attribute, normalized);
			return SyncFilterStateRecord(ref platform, state, obj);
		}
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			attribute, value, false);
	}

	// Keep OM_SET distinct from the internal policy setter. MorphOS publishes
	// counters, status and Path as getters only, while ExAllType is an
	// initializer/getter. Rejecting them here prevents the generic raw store
	// from accepting writes that the named scan records would never consume.
	public static bool SetRuntimeAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (attribute == ExAllType || attribute == NumBytes ||
			attribute == NumBytes64 || attribute == NumDrawers ||
			attribute == NumFiles || attribute == Path || attribute == Status)
			return false;
		return SetAttribute(ref platform, state, obj, attribute, value);
	}

	private static bool SetDirectory<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR directory) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (directory.IsNull)
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, DirectoryKey);
			SetInternal(ref platform, state, obj, Directory, 0);
			MuiListCore.Clear(ref platform, state, obj);
			MarkInvalid(ref platform, state, obj, 0);
			return true;
		}
		if (!OwnString(ref platform, state, obj, DirectoryKey, directory, MaxPath))
			return false;
		SetInternal(ref platform, state, obj, Directory,
			MuiStoreCore.DataspaceFind(ref platform, state, obj, DirectoryKey).Raw);
		return ReRead(ref platform, state, obj);
	}

	private static bool SetOwnedPattern<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint key, APTR pattern)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (pattern.IsNull)
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, key);
			return SyncFilterStateRecord(ref platform, state, obj);
		}
		// Since V20 empty patterns behave like no pattern string.
		if (!CStringCodec.TryReadLength(ref platform, pattern, MaxPattern,
			out var length)) return false;
		if (length == 0)
		{
			MuiStoreCore.DataspaceRemove(ref platform, state, obj, key);
			return SyncFilterStateRecord(ref platform, state, obj);
		}
		return MuiStoreCore.DataspaceAdd(ref platform, state, obj, key, pattern,
			(int)(length + 1)) && SyncFilterStateRecord(ref platform, state, obj);
	}

	// ---- MUIM_Dirlist_ReRead -------------------------------------------------

	// Re-read the current directory synchronously. Returns true when the scan
	// produced a valid listing; false (with Status Invalid and IoErr captured)
	// when the directory is missing, unreadable, or a mid-scan failure occurs.
	public static bool ReRead<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiListCore.HasBackbone(ref platform, state, obj)) return false;
		var path = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DirectoryKey);
		if (path.IsNull)
		{
			MuiListCore.Clear(ref platform, state, obj);
			MarkInvalid(ref platform, state, obj, 0);
			return false;
		}
		PublishScanStatus(ref platform, state, obj, StatusReading, false);
		var count = platform.DirectoryScan(path);
		if (count < 0)
		{
			MuiListCore.Clear(ref platform, state, obj);
			MarkInvalid(ref platform, state, obj, platform.DirectoryError());
			return false;
		}
		MuiListCore.Clear(ref platform, state, obj);

		var scratch = MuiHeadlessMemory.Allocate(ref platform, ScanEntrySize);
		if (scratch.IsNull)
		{
			MarkInvalid(ref platform, state, obj, ErrorNoFreeStore);
			return false;
		}
		uint numFiles = 0;
		uint numDrawers = 0;
		uint totalLow = 0;
		uint totalHigh = 0;
		var limit = count > MaxScanEntries ? MaxScanEntries : count;
		for (var i = 0; i < limit; i++)
		{
			if (!platform.DirectoryEntry(path, i, scratch))
			{
				FreeScratch(ref platform, scratch);
				MuiListCore.Clear(ref platform, state, obj);
				MarkInvalid(ref platform, state, obj, platform.DirectoryError());
				return false;
			}
			if (!TryReadScanEntryState(ref platform, scratch, out var scanEntry))
			{
				FreeScratch(ref platform, scratch);
				MuiListCore.Clear(ref platform, state, obj);
				MarkInvalid(ref platform, state, obj, ErrorObjectNotFound);
				return false;
			}
			var isDir = scanEntry.Type >= 0;
			if (!Accept(ref platform, state, obj, scratch, scanEntry, isDir))
				continue;
			var record = BuildRecord(ref platform, scratch);
			if (record.IsNull || !MuiListCore.AppendOwnedRecord(ref platform, state,
				obj, record))
			{
				if (record.IsNotNull) FreeRecord(ref platform, record);
				FreeScratch(ref platform, scratch);
				MuiListCore.Clear(ref platform, state, obj);
				MarkInvalid(ref platform, state, obj, ErrorNoFreeStore);
				return false;
			}
			if (isDir) numDrawers++;
			else
			{
				numFiles++;
				var size = scanEntry.SizeLow;
				var newLow = unchecked(totalLow + size);
				if (newLow < totalLow) totalHigh++;
				totalLow = newLow;
				totalHigh += scanEntry.SizeHigh;
			}
		}
		FreeScratch(ref platform, scratch);
		SortEntries(ref platform, state, obj);
		var scan = default(MuiDirlistScanState);
		scan.Status = StatusValid;
		scan.NumFiles = numFiles;
		scan.NumDrawers = numDrawers;
		scan.NumBytes = totalLow;
		scan.IoErr = 0;
		PublishScanState(ref platform, state, obj, scan, true);
		StoreBytes64(ref platform, state, obj, totalHigh, totalLow);
		return true;
	}

	// ---- MUIM_Dirlist_Rename / SetComment / SetProtection --------------------

	public static int Rename<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int row, APTR newName)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var entry = MuiListCore.GetEntry(ref platform, state, obj, row, APTR.Null);
		if (entry.IsNull) return Fail(ref platform, state, obj, ErrorObjectNotFound);
		if (!TryReadEntryState(ref platform, entry, out var entryState))
			return Fail(ref platform, state, obj, ErrorObjectNotFound);
		var path = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DirectoryKey);
		var err = platform.DirectoryRename(path,
			entryState.Name, newName);
		PublishIoErr(ref platform, state, obj, err);
		return err;
	}

	public static int SetComment<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int row, APTR comment)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var entry = MuiListCore.GetEntry(ref platform, state, obj, row, APTR.Null);
		if (entry.IsNull) return Fail(ref platform, state, obj, ErrorObjectNotFound);
		if (!TryReadEntryState(ref platform, entry, out var entryState))
			return Fail(ref platform, state, obj, ErrorObjectNotFound);
		var path = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DirectoryKey);
		var err = platform.DirectorySetComment(path,
			entryState.Name, comment);
		PublishIoErr(ref platform, state, obj, err);
		return err;
	}

	public static int SetProtection<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int row, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var entry = MuiListCore.GetEntry(ref platform, state, obj, row, APTR.Null);
		if (entry.IsNull) return Fail(ref platform, state, obj, ErrorObjectNotFound);
		if (!TryReadEntryState(ref platform, entry, out var entryState))
			return Fail(ref platform, state, obj, ErrorObjectNotFound);
		var path = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DirectoryKey);
		var err = platform.DirectorySetProtection(path,
			entryState.Name, flags);
		if (err == 0 && !WriteEntryProtection(ref platform, entryState, flags))
			err = ErrorNoFreeStore;
		PublishIoErr(ref platform, state, obj, err);
		return err;
	}

	// Most recent IoErr() value captured by a scan or mutator.
	public static int IoErr<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadScanState(ref platform, state, obj, out var scan) ?
			scan.IoErr : 0;

	// The comment string stored inside an owned Dirlist entry record.
	public static APTR EntryComment<TPlatform>(ref TPlatform platform, APTR entry)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadEntryState(ref platform, entry, out var entryState) ?
			entryState.Comment : APTR.Null;
	}

	// ---- Shared record building / sorting (also used by Volumelist) ----------

	// Build an owned, self-describing FileInfoBlock-like record from a scan
	// scratch block. Returns Null on bounds or allocation failure.
	internal static APTR BuildRecord<TPlatform>(ref TPlatform platform,
		APTR scratch) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadScanEntryState(ref platform, scratch, out var scanEntry))
			return APTR.Null;
		var source = default(MuiDirlistEntryState);
		source.Type = scanEntry.Type;
		source.SizeLow = scanEntry.SizeLow;
		source.SizeHigh = scanEntry.SizeHigh;
		source.Protection = scanEntry.Protection;
		source.Days = scanEntry.Days;
		source.Mins = scanEntry.Mins;
		source.Ticks = scanEntry.Ticks;
		source.Name = scanEntry.Name;
		source.NameLength = scanEntry.NameLength;
		source.Comment = scanEntry.Comment;
		source.CommentLength = scanEntry.CommentLength;

		var raw = MuiDirlistEntryWireState.NameOffset + source.NameLength + 1 +
			source.CommentLength + 1;
		var recordSize = (raw + 3u) & ~3u;
		var record = MuiHeadlessMemory.Allocate(ref platform, recordSize);
		if (record.IsNull) return APTR.Null;
		var target = source;
		target.Address = record;
		target.RecordSize = recordSize;
		target.Name = APTR.FromPointer(record.Raw +
			MuiDirlistEntryWireState.NameOffset);
		var commentOffset = MuiDirlistEntryWireState.NameOffset +
			target.NameLength + 1;
		target.Comment = APTR.FromPointer(record.Raw + commentOffset);
		if (!WriteEntryRecord(ref platform, target, source.Name, source.Comment))
		{
			platform.Clear(record, recordSize);
			platform.Free(record, recordSize);
			return APTR.Null;
		}
		return record;
	}

	// Sort the current entries in place using the Dirlist sort attributes. The
	// pass is an allocation-free selection sort over the shared backbone using
	// GetEntry/Exchange, so it composes with the owned-record ownership.
	internal static void SortEntries<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = MuiListCore.EntryCount(ref platform, state, obj);
		if (count < 2) return;
		if (!TryReadSortState(ref platform, state, obj, out var sortState))
			return;
		var sortType = sortState.SortType;
		var sortDirs = sortState.SortDirs;
		var highLow = sortState.SortHighLow != 0;
		for (var i = 0u; i + 1 < count; i++)
		{
			var best = i;
			var bestEntry = MuiListCore.GetEntry(ref platform, state, obj, (int)i,
				APTR.Null);
			for (var j = i + 1; j < count; j++)
			{
				var candidate = MuiListCore.GetEntry(ref platform, state, obj,
					(int)j, APTR.Null);
				if (Compare(ref platform, candidate, bestEntry, sortType, sortDirs,
					highLow) < 0)
				{
					best = j;
					bestEntry = candidate;
				}
			}
			if (best != i)
				MuiListCore.Exchange(ref platform, state, obj, (int)i, (int)best);
		}
	}

	// ---- Filtering -----------------------------------------------------------

	private static bool Accept<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR scratch, MuiDirlistScanEntryState entry, bool isDir)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadFilterState(ref platform, state, obj, out var filter))
			return false;
		if (filter.FilterHook.IsNotNull)
		{
			// A FilterHook overrides every other filter attribute (autodoc). The
			// hook receives the dirlist object and the ExAllData-like scratch and
			// returns TRUE to include the entry. A0 = hook base (so h_Data is
			// reachable), A2 = object, A1 = scratch record.
			return platform.InvokeHook(filter.FilterHook, obj, scratch) != 0;
		}
		if (filter.DrawersOnly != 0 && !isDir)
			return false;
		if (filter.FilesOnly != 0 && isDir)
			return false;
		var name = entry.Name;
		if (!isDir && filter.RejectIcons != 0 &&
			HasInfoSuffix(ref platform, name)) return false;
		var filterThis = !isDir ||
			filter.FilterDrawers != 0;
		if (filterThis)
		{
			var reject = filter.RejectPattern;
			if (reject.IsNotNull && MatchPattern(ref platform, reject, name))
				return false;
			var accept = filter.AcceptPattern.IsNotNull ?
				filter.AcceptPattern : filter.Pattern;
			if (accept.IsNotNull && !MatchPattern(ref platform, accept, name))
				return false;
		}
		return true;
	}

	private static bool HasInfoSuffix<TPlatform>(ref TPlatform platform, APTR name)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!CStringCodec.TryReadLength(ref platform, name, MaxName, out var length)
			|| length < 5) return false;
		return Lower(platform.ReadUInt8(name, (int)length - 5)) == (byte)'.' &&
			Lower(platform.ReadUInt8(name, (int)length - 4)) == (byte)'i' &&
			Lower(platform.ReadUInt8(name, (int)length - 3)) == (byte)'n' &&
			Lower(platform.ReadUInt8(name, (int)length - 2)) == (byte)'f' &&
			Lower(platform.ReadUInt8(name, (int)length - 1)) == (byte)'o';
	}

	// A bounded, case-insensitive AmigaDOS-style pattern matcher supporting the
	// common wildcards: '#?' and '*' match any sequence, '?' matches a single
	// character, everything else is a literal. This is a pragmatic subset, not
	// a full ParsePatternNoCase() implementation.
	private static bool MatchPattern<TPlatform>(ref TPlatform platform,
		APTR pattern, APTR name) where TPlatform : struct, IMuiGuestMemory
	{
		if (!CStringCodec.TryReadLength(ref platform, pattern, MaxPattern,
			out var patternLength)) return false;
		if (!CStringCodec.TryReadLength(ref platform, name, MaxName,
			out var nameLength)) return false;
		return Match(ref platform, pattern, 0, (int)patternLength, name, 0,
			(int)nameLength);
	}

	private static bool Match<TPlatform>(ref TPlatform platform, APTR pattern,
		int pi, int pLen, APTR name, int ni, int nLen)
		where TPlatform : struct, IMuiGuestMemory
	{
		var p = pi;
		var n = ni;
		while (p < pLen)
		{
			var pc = platform.ReadUInt8(pattern, p);
			var isAny = pc == (byte)'*' ||
				(pc == (byte)'#' && p + 1 < pLen &&
					platform.ReadUInt8(pattern, p + 1) == (byte)'?');
			if (isAny)
			{
				var advance = pc == (byte)'*' ? 1 : 2;
				// Match zero or more characters, greedily via recursion.
				for (var k = n; k <= nLen; k++)
					if (Match(ref platform, pattern, p + advance, pLen, name, k,
						nLen)) return true;
				return false;
			}
			if (n >= nLen) return false;
			var nc = platform.ReadUInt8(name, n);
			if (pc != (byte)'?' && Lower(pc) != Lower(nc)) return false;
			p++;
			n++;
		}
		return n == nLen;
	}

	// ---- Comparison ----------------------------------------------------------

	private static int Compare<TPlatform>(ref TPlatform platform, APTR a, APTR b,
		uint sortType, uint sortDirs, bool highLow)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadEntryState(ref platform, a, out var aState) ||
			!TryReadEntryState(ref platform, b, out var bState)) return 0;
		var aDir = aState.Type >= 0;
		var bDir = bState.Type >= 0;
		if (sortDirs != SortDirsMix && aDir != bDir)
			return sortDirs == SortDirsLast ? (aDir ? 1 : -1) : (aDir ? -1 : 1);
		var result = CompareField(ref platform, aState, bState, sortType);
		return highLow ? -result : result;
	}

	private static int CompareField<TPlatform>(ref TPlatform platform,
		MuiDirlistEntryState a, MuiDirlistEntryState b, uint sortType)
		where TPlatform : struct, IMuiGuestMemory
	{
		switch (sortType)
		{
			case SortTypeSize:
				return CompareSize(a, b);
			case SortTypeDate:
				return CompareDate(a, b);
			case SortTypeFlags:
				return CompareUnsigned(a.Protection, b.Protection);
			case SortTypeType:
				return CompareSigned(a.Type, b.Type);
			case SortTypeComment:
				return CompareStrings(ref platform,
					a.Comment, b.Comment);
			default: // SortTypeName and unknown types sort by name.
				return CompareStrings(ref platform,
					a.Name, b.Name);
		}
	}

	private static int CompareDate(MuiDirlistEntryState a,
		MuiDirlistEntryState b)
	{
		var days = CompareUnsigned(a.Days, b.Days);
		if (days != 0) return days;
		var mins = CompareUnsigned(a.Mins, b.Mins);
		if (mins != 0) return mins;
		return CompareUnsigned(a.Ticks, b.Ticks);
	}

	private static int CompareSize(MuiDirlistEntryState a,
		MuiDirlistEntryState b)
	{
		var high = CompareUnsigned(a.SizeHigh, b.SizeHigh);
		return high != 0 ? high
			: CompareUnsigned(a.SizeLow, b.SizeLow);
	}

	private static int CompareUnsigned(uint left, uint right) =>
		left == right ? 0 : left < right ? -1 : 1;

	private static int CompareSigned(int left, int right) =>
		left == right ? 0 : left < right ? -1 : 1;

	private static int CompareStrings<TPlatform>(ref TPlatform platform, APTR left,
		APTR right) where TPlatform : struct, IMuiGuestMemory
	{
		if (left.Raw == right.Raw) return 0;
		for (var i = 0u; i < MaxName + MaxComment; i++)
		{
			var la = APTR.FromPointer(left.Raw + i);
			var ra = APTR.FromPointer(right.Raw + i);
			if (!platform.IsMapped(la, 1) || !platform.IsMapped(ra, 1)) return 0;
			var lb = Lower(platform.ReadUInt8(la, 0));
			var rb = Lower(platform.ReadUInt8(ra, 0));
			if (lb != rb) return lb < rb ? -1 : 1;
			if (lb == 0) return 0;
		}
		return 0;
	}

	// ---- Path computation ----------------------------------------------------

	private static APTR ComputePath<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadScanState(ref platform, state, obj, out var scan) ||
			scan.Status != StatusValid)
			return APTR.Null;
		var active = MuiListCore.ActiveRow(ref platform, state, obj);
		if (active < 0) return APTR.Null;
		var entry = MuiListCore.GetEntry(ref platform, state, obj, active,
			APTR.Null);
		if (entry.IsNull || !TryReadEntryState(ref platform, entry,
			out var entryState)) return APTR.Null;
		var dir = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			DirectoryKey);
		var name = entryState.Name;
		uint dirLength = 0;
		if (dir.IsNotNull && !CStringCodec.TryReadLength(ref platform, dir, MaxPath,
			out dirLength)) return APTR.Null;
		if (!CStringCodec.TryReadLength(ref platform, name, MaxName,
			out var nameLength)) return APTR.Null;
		var separator = dirLength != 0 && !EndsWithSeparator(ref platform, dir,
			dirLength);
		var total = dirLength + (separator ? 1u : 0u) + nameLength;
		var scratch = MuiHeadlessMemory.Allocate(ref platform, total + 1);
		if (scratch.IsNull) return APTR.Null;
		var cursor = 0u;
		for (var i = 0u; i < dirLength; i++)
			platform.WriteUInt8(scratch, (int)cursor++,
				platform.ReadUInt8(dir, (int)i));
		if (separator) platform.WriteUInt8(scratch, (int)cursor++, (byte)'/');
		for (var i = 0u; i < nameLength; i++)
			platform.WriteUInt8(scratch, (int)cursor++,
				platform.ReadUInt8(name, (int)i));
		platform.WriteUInt8(scratch, (int)cursor, 0);
		var stored = MuiStoreCore.DataspaceAdd(ref platform, state, obj, PathKey,
			scratch, (int)(total + 1));
		platform.Clear(scratch, total + 1);
		platform.Free(scratch, total + 1);
		return stored ? MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PathKey) : APTR.Null;
	}

	private static bool EndsWithSeparator<TPlatform>(ref TPlatform platform,
		APTR dir, uint length) where TPlatform : struct, IMuiGuestMemory
	{
		if (length == 0) return false;
		var last = platform.ReadUInt8(dir, (int)length - 1);
		return last == (byte)':' || last == (byte)'/';
	}

	// ---- Small helpers -------------------------------------------------------

	private static void MarkInvalid<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int ioErr) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var scan = default(MuiDirlistScanState);
		scan.Status = StatusInvalid;
		scan.IoErr = ioErr;
		PublishScanState(ref platform, state, obj, scan, true);
		StoreBytes64(ref platform, state, obj, 0, 0);
	}

	private static int Fail<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int ioErr) where TPlatform : struct, IMuiHeadlessPlatform
	{
		PublishIoErr(ref platform, state, obj, ioErr);
		return ioErr;
	}

	private static void PublishIoErr<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int ioErr)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadScanState(ref platform, state, obj, out var scan))
		{
			scan.IoErr = ioErr;
			PublishScanState(ref platform, state, obj, scan, false);
			return;
		}
		SetInternal(ref platform, state, obj, IoErrKey, unchecked((uint)ioErr));
	}

	private static void StoreBytes64<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint high, uint low) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var scratch = MuiHeadlessMemory.Allocate(ref platform, ByteTotalSize);
		if (scratch.IsNull) return;
		var total = default(MuiDirlistByteTotalState);
		total.High = high;
		total.Low = low;
		if (!WriteByteTotalState(ref platform, scratch, total))
		{
			platform.Free(scratch, ByteTotalSize);
			return;
		}
		MuiStoreCore.DataspaceAdd(ref platform, state, obj, NumBytes64Key, scratch,
			(int)ByteTotalSize);
		platform.Clear(scratch, ByteTotalSize);
		platform.Free(scratch, ByteTotalSize);
	}

	private static void FreeScratch<TPlatform>(ref TPlatform platform, APTR scratch)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		platform.Clear(scratch, ScanEntrySize);
		platform.Free(scratch, ScanEntrySize);
	}

	internal static void FreeRecord<TPlatform>(ref TPlatform platform, APTR record)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadEntryState(ref platform, record, out var entry)) return;
		platform.Clear(record, entry.RecordSize);
		platform.Free(record, entry.RecordSize);
	}

	private static bool OwnCreationString<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint key)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var raw = APTR.FromPointer(Read(ref platform, state, obj, attribute, 0));
		if (raw.IsNull) return true;
		if (!CStringCodec.TryReadLength(ref platform, raw, MaxPattern,
			out var length) || length == 0) return true;
		return MuiStoreCore.DataspaceAdd(ref platform, state, obj, key, raw,
			(int)(length + 1));
	}

	private static bool OwnString<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint key, APTR source, uint maximum)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CStringCodec.TryReadLength(ref platform, source, maximum,
			out var length)) return false;
		return MuiStoreCore.DataspaceAdd(ref platform, state, obj, key, source,
			(int)(length + 1));
	}

	private static byte Lower(byte ch) =>
		ch >= (byte)'A' && ch <= (byte)'Z' ? unchecked((byte)(ch + 32)) : ch;

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;

	private static void SetInternal<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, false);

	private static void SetNotify<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var current) && current == value) return;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, true);
	}

	private static void EnsureDefault<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out _))
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
				value, false);
	}
}
