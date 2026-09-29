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

// Fixed routing state is transferred as a named record. The bounded cursor
// walks the complete packed struct before selecting a field; offset constants
// remain ABI documentation/compatibility aliases only.
internal static class MuiApplicationMessageRoutingStateRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiApplicationMessageRoutingStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiApplicationMessageRoutingStateField.Magic:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationMessageRoutingStateRecord.FieldSize, out address);
			case MuiApplicationMessageRoutingStateField.AppMessage:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationMessageRoutingStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationMessageRoutingStateRecord.FieldSize, out address);
			case MuiApplicationMessageRoutingStateField.WindowAppWindow:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationMessageRoutingStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationMessageRoutingStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationMessageRoutingStateRecord.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiApplicationMessageRoutingStateRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address))
			return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationMessageRoutingStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationMessageRoutingStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationMessageRoutingStateField.AppMessage)
			value = state.AppMessage.Raw;
		else if (field == MuiApplicationMessageRoutingStateField.WindowAppWindow)
			value = state.WindowAppWindow;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMessageRoutingStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationMessageRoutingStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationMessageRoutingStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationMessageRoutingStateField.AppMessage)
			state.AppMessage = APTR.FromPointer(value);
		else if (field == MuiApplicationMessageRoutingStateField.WindowAppWindow)
			state.WindowAppWindow = value;
		else return false;
		return MuiApplicationMessageRoutingStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationMessageRoutingStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationMessageRoutingStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var appMessage) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.WindowAppWindow)) return false;
		value.AppMessage = APTR.FromPointer(appMessage);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationMessageRoutingStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AppMessage.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.WindowAppWindow) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationMessageRoutingStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationMessageRoutingStateAdmission.Validate(
			ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
