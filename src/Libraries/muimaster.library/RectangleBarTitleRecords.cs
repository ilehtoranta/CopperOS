/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the optional Rectangle.mui bar title pointer.
public struct MuiRectangleBarTitleState
{
	public bool Present;
	public APTR Title;
}

// Guest-resident title state. Presence remains separate from the pointer so
// an absent title is not confused with a present NULL value.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRectangleBarTitleStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D524254u; // 'MRBT'

	internal uint Magic;
	internal uint Present;
	internal APTR Title;
}

internal static class MuiRectangleBarTitleStateAdmission
{
	internal static bool Validate(MuiRectangleBarTitleStateRecord value) =>
		value.Magic == MuiRectangleBarTitleStateRecord.Cookie &&
		value.Present <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiRectangleBarTitleStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Title.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Title, 4096, out _);
	}
}

internal enum MuiRectangleBarTitleStateField : byte
{
	Magic,
	Present,
	Title,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRectangleBarTitleStateFieldCursor
{
	internal APTR Record;
	internal MuiRectangleBarTitleStateField Field;
}

internal static class MuiRectangleBarTitleStateFieldCursorCodec
{
	private static bool TryResolve(MuiRectangleBarTitleStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiRectangleBarTitleStateField.Magic => 0,
			MuiRectangleBarTitleStateField.Present => 4,
			MuiRectangleBarTitleStateField.Title => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRectangleBarTitleStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiRectangleBarTitleStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectangleBarTitleStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiRectangleBarTitleStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectangleBarTitleStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiRectangleBarTitleStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Presence and title remain named
// semantic fields; fixed guest-layout translation is bounded here.
internal static class MuiRectangleBarTitleStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiRectangleBarTitleStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiRectangleBarTitleStateRecord.Size)) return false;
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

internal static class MuiRectangleBarTitleStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiRectangleBarTitleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiRectangleBarTitleStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiRectangleBarTitleStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Present) ||
			!MuiRectangleBarTitleStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out var title))
			return false;
		value.Title = APTR.FromPointer(title);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRectangleBarTitleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiRectangleBarTitleStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRectangleBarTitleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiRectangleBarTitleStateAdmission.Validate(value)) return false;
		return MuiRectangleBarTitleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiRectangleBarTitleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 4, value.Present) &&
			MuiRectangleBarTitleStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 8, value.Title.Raw);
	}
}
