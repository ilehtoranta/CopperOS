/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application menu event state. MenuAction is the selected item UserData;
// MenuHelp is the getter-only help UserData published by menu transport.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMenuStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint MenuActionOffset = 4;
	internal const uint MenuHelpOffset = 8;
	internal const uint Cookie = 0x414D5354u; // 'AMST'

	internal uint Magic;
	internal uint MenuAction;
	internal uint MenuHelp;
}

// Application menu event state contains opaque MorphOS ULONG UserData values;
// admission owns only the record cookie and live Application capability. The
// values retain their complete range and are never interpreted as pointers.
internal static class MuiApplicationMenuStateAdmission
{
	internal static bool Validate(MuiApplicationMenuStateRecord value) =>
		value.Magic == MuiApplicationMenuStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
}

internal enum MuiApplicationMenuStateField : byte
{
	Magic,
	MenuAction,
	MenuHelp,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMenuStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationMenuStateField Field;
}

internal static class MuiApplicationMenuStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationMenuStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationMenuStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMenuStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationMenuStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application menu state is transferred as a named record. Numeric
// guest positions are confined to the bounded ABI adapter; production
// consumers exchange the declaration-order struct through the sequential
// cursor below.
internal static class MuiApplicationMenuStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationMenuStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationMenuStateField.Magic:
				offset = MuiApplicationMenuStateRecord.MagicOffset;
				return true;
			case MuiApplicationMenuStateField.MenuAction:
				offset = MuiApplicationMenuStateRecord.MenuActionOffset;
				return true;
			case MuiApplicationMenuStateField.MenuHelp:
				offset = MuiApplicationMenuStateRecord.MenuHelpOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMenuStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationMenuStateRecord.Size) &&
			platform.IsMapped(address, MuiApplicationMenuStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMenuStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationMenuStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationMenuStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationMenuStateField.MenuAction)
			value = state.MenuAction;
		else if (field == MuiApplicationMenuStateField.MenuHelp)
			value = state.MenuHelp;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationMenuStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationMenuStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationMenuStateField.MenuAction)
			state.MenuAction = value;
		else if (field == MuiApplicationMenuStateField.MenuHelp)
			state.MenuHelp = value;
		else return false;
		return MuiApplicationMenuStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationMenuStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationMenuStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MenuAction) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MenuHelp)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationMenuStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MenuAction) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MenuHelp) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationMenuStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationMenuStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
