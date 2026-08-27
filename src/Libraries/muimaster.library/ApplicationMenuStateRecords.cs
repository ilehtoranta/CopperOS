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

// Fixed application menu state is read and written as a named value. Keep the
// packed guest positions in this ABI adapter; production consumers do not
// select fields through the compatibility cursor.
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
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationMenuStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMenuStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationMenuStateField.Magic,
			out var magic) ||
			!MuiApplicationMenuStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationMenuStateField.MenuAction,
				out value.MenuAction) ||
			!MuiApplicationMenuStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationMenuStateField.MenuHelp,
				out value.MenuHelp)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationMenuStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationMenuStateRecord.Size) ||
			!MuiApplicationMenuStateAdmission.Validate(value)) return false;
		return MuiApplicationMenuStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationMenuStateField.Magic,
			value.Magic) &&
			MuiApplicationMenuStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationMenuStateField.MenuAction,
				value.MenuAction) &&
			MuiApplicationMenuStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationMenuStateField.MenuHelp,
				value.MenuHelp);
	}
}
