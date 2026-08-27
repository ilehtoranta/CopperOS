/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Transient AppMessage delivery and Window_AppWindow participation.  The
// message pointer remains valid only during synchronous publication; both
// values are retained in a named guest snapshot rather than a managed mirror.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMessageRoutingStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint AppMessageOffset = 4;
	internal const uint WindowAppWindowOffset = 8;
	internal const uint Cookie = 0x414D5254u; // 'AMRT'

	internal uint Magic;
	internal APTR AppMessage;
	internal uint WindowAppWindow;
}

// AppMessage is a transient, caller-owned Exec record.  The routing sidecar
// may retain it only while the named message remains structurally valid; the
// WindowAppWindow projection is a MorphOS BOOL rather than an arbitrary ULONG.
internal static class MuiApplicationMessageRoutingStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationMessageRoutingStateRecord.Cookie &&
		value.WindowAppWindow <= 1 &&
		(value.AppMessage.IsNull ||
			MuiApplicationMessageCore.ValidateMessage(ref platform,
				value.AppMessage));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner,
		MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && owner.IsNotNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull;
}

internal enum MuiApplicationMessageRoutingStateField : byte
{
	Magic,
	AppMessage,
	WindowAppWindow,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMessageRoutingStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationMessageRoutingStateField Field;
}

internal static class MuiApplicationMessageRoutingStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationMessageRoutingStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationMessageRoutingStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationMessageRoutingStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationMessageRoutingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed routing state is read and written as a named value. Keep packed guest
// positions in this bounded ABI adapter; production consumers do not select
// numeric slots directly.
internal static class MuiApplicationMessageRoutingStateRecordMemoryCodec
{
	private static bool TryResolve(
		MuiApplicationMessageRoutingStateField field, out uint offset)
	{
		switch (field)
		{
			case MuiApplicationMessageRoutingStateField.Magic:
				offset = MuiApplicationMessageRoutingStateRecord.MagicOffset;
				return true;
			case MuiApplicationMessageRoutingStateField.AppMessage:
				offset = MuiApplicationMessageRoutingStateRecord.AppMessageOffset;
				return true;
			case MuiApplicationMessageRoutingStateField.WindowAppWindow:
				offset = MuiApplicationMessageRoutingStateRecord.WindowAppWindowOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record,
			MuiApplicationMessageRoutingStateRecord.Size) &&
			platform.IsMapped(address,
				MuiApplicationMessageRoutingStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationMessageRoutingStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationMessageRoutingStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address,
			MuiApplicationMessageRoutingStateField.Magic, out var magic) ||
			!MuiApplicationMessageRoutingStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.AppMessage,
				out var appMessage) ||
			!MuiApplicationMessageRoutingStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.WindowAppWindow,
				out value.WindowAppWindow)) return false;
		value.Magic = magic;
		value.AppMessage = APTR.FromPointer(appMessage);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationMessageRoutingStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationMessageRoutingStateRecord.Size) ||
			!MuiApplicationMessageRoutingStateAdmission.Validate(ref platform, value))
			return false;
		return MuiApplicationMessageRoutingStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationMessageRoutingStateField.Magic,
			value.Magic) &&
			MuiApplicationMessageRoutingStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.AppMessage,
				value.AppMessage.Raw) &&
			MuiApplicationMessageRoutingStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationMessageRoutingStateField.WindowAppWindow,
				value.WindowAppWindow);
	}
}
