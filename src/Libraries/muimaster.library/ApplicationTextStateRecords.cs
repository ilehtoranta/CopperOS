/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Mutable caller-owned Application text pointers. The strings remain in guest
// memory; this record only retains their validated APTR values.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationTextStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HelpFileOffset = 4;
	internal const uint IconifyTitleOffset = 8;
	internal const uint Cookie = 0x41545354u; // 'ATST'

	internal uint Magic;
	internal APTR HelpFile;
	internal APTR IconifyTitle;
}

// Admission for mutable caller-owned Application text pointers.  NULL is the
// explicit empty value; a non-NULL pointer must resolve to a bounded C string.
internal static class MuiApplicationTextStateAdmission
{
	internal const uint MaximumStringLength = 65536;

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationTextStateRecord.Cookie &&
		IsCString(ref platform, value.HelpFile) &&
		IsCString(ref platform, value.IconifyTitle);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;

	private static bool IsCString<TPlatform>(ref TPlatform platform, APTR value)
		where TPlatform : struct, IMuiGuestMemory => value.IsNull ||
		CStringCodec.TryReadLength(ref platform, value, MaximumStringLength, out _);
}

internal enum MuiApplicationTextStateField : byte
{
	Magic,
	HelpFile,
	IconifyTitle,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationTextStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationTextStateField Field;
}

internal static class MuiApplicationTextStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationTextStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationTextStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationTextStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationTextStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application text state is transferred as a named record. The bounded
// cursor walks the complete packed struct before selecting a field; offset
// constants remain ABI documentation/compatibility aliases only.
internal static class MuiApplicationTextStateRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiApplicationTextStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiApplicationTextStateField.Magic:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationTextStateRecord.FieldSize, out address);
			case MuiApplicationTextStateField.HelpFile:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationTextStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationTextStateRecord.FieldSize, out address);
			case MuiApplicationTextStateField.IconifyTitle:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationTextStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationTextStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationTextStateRecord.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationTextStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiApplicationTextStateRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address))
			return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationTextStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationTextStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationTextStateField.HelpFile)
			value = state.HelpFile.Raw;
		else if (field == MuiApplicationTextStateField.IconifyTitle)
			value = state.IconifyTitle.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationTextStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationTextStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationTextStateField.HelpFile)
			state.HelpFile = APTR.FromPointer(value);
		else if (field == MuiApplicationTextStateField.IconifyTitle)
			state.IconifyTitle = APTR.FromPointer(value);
		else return false;
		return MuiApplicationTextStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationTextStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationTextStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var helpFile) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var iconifyTitle)) return false;
		value.HelpFile = APTR.FromPointer(helpFile);
		value.IconifyTitle = APTR.FromPointer(iconifyTitle);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationTextStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HelpFile.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.IconifyTitle.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationTextStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationTextStateAdmission.Validate(ref platform,
			value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
