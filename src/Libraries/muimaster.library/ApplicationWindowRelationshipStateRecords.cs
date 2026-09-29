/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application_Window initializer relationship state. LastWindow mirrors the
// public attribute projection; AddedCount records accepted repeated
// initializer occurrences without introducing a managed collection.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowRelationshipStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint LastWindowOffset = 4;
	internal const uint AddedCountOffset = 8;
	internal const uint Cookie = 0x41575254u; // 'AWRT'

	internal uint Magic;
	internal APTR LastWindow;
	internal uint AddedCount;
}

// The repeated Application_Window initializer retains the last caller-owned
// MUI Window pointer and a saturating accepted-count. NULL is valid before any
// window is attached; non-NULL pointers must be mapped, and live admission
// additionally requires the pointer to be a direct child of the Application.
internal static class MuiApplicationWindowRelationshipStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationWindowRelationshipStateRecord.Cookie &&
		(value.LastWindow.IsNull || platform.IsMapped(value.LastWindow, 1));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application,
		MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(ref platform, value) ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		if (value.LastWindow.IsNull) return true;
		if (MuiHeadlessObjectCore.FindObject(ref platform, state,
			value.LastWindow).IsNull) return false;
		return MuiHeadlessObjectCore.ParentObject(ref platform, state,
			value.LastWindow).Raw == application.Raw;
	}
}

internal enum MuiApplicationWindowRelationshipStateField : byte
{
	Magic,
	LastWindow,
	AddedCount,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowRelationshipStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationWindowRelationshipStateField Field;
}

internal static class MuiApplicationWindowRelationshipStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowRelationshipStateFieldCursor cursor,
		out APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowRelationshipStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowRelationshipStateField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowRelationshipStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowRelationshipStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowRelationshipStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed Application_Window relationship state is transferred as a named
// record. The bounded cursor walks the complete packed struct before selecting
// a field; offset constants remain ABI documentation/compatibility aliases.
internal static class MuiApplicationWindowRelationshipStateRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor,
		MuiApplicationWindowRelationshipStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiApplicationWindowRelationshipStateField.Magic:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowRelationshipStateRecord.FieldSize, out address);
			case MuiApplicationWindowRelationshipStateField.LastWindow:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowRelationshipStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowRelationshipStateRecord.FieldSize, out address);
			case MuiApplicationWindowRelationshipStateField.AddedCount:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowRelationshipStateRecord.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowRelationshipStateRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowRelationshipStateRecord.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowRelationshipStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiApplicationWindowRelationshipStateRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address))
			return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowRelationshipStateField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationWindowRelationshipStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationWindowRelationshipStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationWindowRelationshipStateField.LastWindow)
			value = state.LastWindow.Raw;
		else if (field == MuiApplicationWindowRelationshipStateField.AddedCount)
			value = state.AddedCount;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowRelationshipStateField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationWindowRelationshipStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationWindowRelationshipStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationWindowRelationshipStateField.LastWindow)
			state.LastWindow = APTR.FromPointer(value);
		else if (field == MuiApplicationWindowRelationshipStateField.AddedCount)
			state.AddedCount = value;
		else return false;
		return MuiApplicationWindowRelationshipStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationWindowRelationshipStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationWindowRelationshipStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var lastWindow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.AddedCount)) return false;
		value.LastWindow = APTR.FromPointer(lastWindow);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationWindowRelationshipStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.LastWindow.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.AddedCount) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationWindowRelationshipStateAdmission.Validate(ref platform,
			value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationWindowRelationshipStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationWindowRelationshipStateAdmission
			.Validate(ref platform, value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
